using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed class UserDirectoryViewModel : INotifyPropertyChanged
{
    private const string UserRead = "core.user.read";
    private const string UserWrite = "core.user.write";
    private const string UserRoleWrite = "core.user-role.write";
    private const string EmployeeRead = "core.employee.read";
    private const string RoleRead = "core.role.read";

    private readonly IUserDirectoryReadService _userReadService;
    private readonly IUserDirectoryMutationService _mutationService;
    private readonly IEmployeeDirectoryReadService _employeeReadService;
    private readonly IAccessRoleReadService _roleReadService;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly List<AccessUserData> _users = [];
    private readonly List<EmployeeDirectoryData> _employees = [];
    private readonly List<AccessRoleData> _roles = [];
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _searchTerm = string.Empty;
    private UserStatusFilterOption _selectedStatusFilter;
    private bool _isEditorOpen;
    private bool _isCreateMode;
    private AccessUserData? _editingUser;
    private string _draftLoginName = string.Empty;
    private string _draftEmployeeId = string.Empty;
    private string _draftPassword = string.Empty;
    private bool _draftIsActive = true;
    private string _editorMessage = string.Empty;
    private bool _editorMessageIsError;
    private bool _hasConflict;
    private bool _isToggleConfirmOpen;
    private AccessUserData? _pendingToggleUser;
    private bool _pendingToggleNextActive;
    private string _toggleMessage = string.Empty;
    private long _accessGeneration;
    private long _loadGeneration;
    private CancellationTokenSource? _loadCts;

    public UserDirectoryViewModel(
        IUserDirectoryReadService userReadService,
        IUserDirectoryMutationService mutationService,
        IEmployeeDirectoryReadService employeeReadService,
        IAccessRoleReadService roleReadService,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _userReadService = userReadService;
        _mutationService = mutationService;
        _employeeReadService = employeeReadService;
        _roleReadService = roleReadService;
        _idempotencyKeys = idempotencyKeys;
        _access = access;
        _selectedStatusFilter = StatusFilters[0];

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loadCts?.Cancel();
            _loadCts = null;
            _loaded = false;
            _users.Clear();
            _employees.Clear();
            _roles.Clear();
            _mutationKeys.Clear();
            VisibleUsers.Clear();
            EligibleEmployees.Clear();
            DraftRoleOptions.Clear();
            CloseEditor();
            CancelToggle();
            Message = string.Empty;
            MessageIsError = false;
            IsBusy = false;
            RaiseAccess();
            RaiseSummary();
            if (CanViewUsers && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<UserDirectoryRowView> VisibleUsers { get; } = [];
    public ObservableCollection<UserEmployeeOption> EligibleEmployees { get; } = [];
    public ObservableCollection<UserRoleOptionView> DraftRoleOptions { get; } = [];

    public IReadOnlyList<UserStatusFilterOption> StatusFilters { get; } =
    [
        new("all", "Tất cả"),
        new("active", "Đang hoạt động"),
        new("inactive", "Ngừng sử dụng")
    ];

    public IReadOnlyList<UserDraftStatusOption> DraftStatusOptions { get; } =
    [
        new(true, "Đang hoạt động"),
        new(false, "Ngừng sử dụng")
    ];

    public bool CanViewUsers => _access.HasPermission(UserRead);
    public bool CanWriteUsers => _access.HasPermission(UserWrite);
    public bool CanManageUserRoles => _access.HasPermission(UserRoleWrite);
    public bool CanReadEmployees => _access.HasPermission(EmployeeRead);
    public bool CanReadRoles => _access.HasPermission(RoleRead);
    public bool CanOpenCreate =>
        CanWriteUsers
        && CanManageUserRoles
        && CanReadEmployees
        && CanReadRoles
        && EligibleEmployees.Count > 0
        && !IsBusy;
    public bool CanEditUsers => (CanWriteUsers || CanManageUserRoles) && CanReadRoles && !IsBusy;
    public bool CanToggleUsers => CanWriteUsers && !IsBusy;
    public bool CanEditPassword => CanWriteUsers && IsEditorOpen && !IsBusy;
    public bool CanEditStatus => CanWriteUsers && IsEditorOpen && !IsBusy;
    public bool CanEditRoles => CanManageUserRoles && IsEditorOpen && !IsBusy;
    public bool CanConfirmToggle => IsToggleConfirmOpen && CanWriteUsers && !IsBusy;
    public bool CanPersist =>
        IsEditorOpen
        && !IsBusy
        && !HasConflict
        && (IsCreateMode ? CreateDraftIsValid() : EditDraftIsValid());

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(RefreshText));
            OnPropertyChanged(nameof(ShowLoading));
            RaiseEditorCapabilities();
            OnPropertyChanged(nameof(CanConfirmToggle));
            OnPropertyChanged(nameof(ToggleConfirmButtonText));
        }
    }

    public string RefreshText => IsBusy ? "Đang tải…" : "Tải lại";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool ShowLoading => IsBusy && !_loaded;
    public bool IsEmpty => _loaded && VisibleUsers.Count == 0;
    public string EmptyText => IsEmpty ? "Không có người dùng phù hợp." : string.Empty;

    public string Message
    {
        get => _message;
        private set
        {
            if (!SetField(ref _message, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasMessage));
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (!SetField(ref _searchTerm, value ?? string.Empty)) return;
            ApplyFilter();
        }
    }

    public UserStatusFilterOption SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (value is null || !SetField(ref _selectedStatusFilter, value)) return;
            ApplyFilter();
        }
    }

    public string TotalUsersText => _users.Count.ToString("N0");
    public string ActiveUsersText => _users.Count(user => user.IsActive).ToString("N0");
    public string InactiveUsersText => _users.Count(user => !user.IsActive).ToString("N0");
    public string VisibleUserCountText => $"{VisibleUsers.Count:N0} kết quả";
    public bool HasEligibleEmployees => EligibleEmployees.Count > 0;

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        private set
        {
            if (!SetField(ref _isEditorOpen, value)) return;
            RaiseEditorCapabilities();
        }
    }

    public bool IsCreateMode
    {
        get => _isCreateMode;
        private set
        {
            if (!SetField(ref _isCreateMode, value)) return;
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(EditorDescription));
            OnPropertyChanged(nameof(PasswordLabel));
            OnPropertyChanged(nameof(PasswordHint));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string EditorTitle => IsCreateMode ? "Thêm người dùng" : "Cập nhật người dùng";
    public string EditorDescription => IsCreateMode
        ? "Chọn nhân sự, cấp tên đăng nhập, mật khẩu và vai trò trong một lần."
        : "Cập nhật vai trò, trạng thái hoặc đặt lại mật khẩu đăng nhập.";
    public string PasswordLabel => IsCreateMode ? "Mật khẩu đăng nhập" : "Mật khẩu mới";
    public string PasswordHint => IsCreateMode
        ? "Mật khẩu này được cấp trực tiếp cho nhân viên để đăng nhập lần đầu."
        : "Nhập mật khẩu mới sẽ đồng thời thu hồi các phiên đăng nhập cũ của người dùng.";
    public string SaveButtonText => IsBusy ? "Đang lưu…" : IsCreateMode ? "Tạo tài khoản" : "Lưu";

    public string DraftLoginName
    {
        get => _draftLoginName;
        set
        {
            var normalized = NormalizeLogin(value);
            if (!SetField(ref _draftLoginName, normalized)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string DraftEmployeeId
    {
        get => _draftEmployeeId;
        set
        {
            if (!SetField(ref _draftEmployeeId, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(DraftEmployeeLabel));
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string DraftEmployeeLabel
    {
        get
        {
            var employee = _employees.FirstOrDefault(item =>
                string.Equals(item.Id, DraftEmployeeId, StringComparison.Ordinal));
            if (employee is not null) return $"{employee.FullName} — {employee.Code}";
            if (_editingUser is not null)
            {
                var name = _editingUser.EmployeeFullName?.Trim() ?? string.Empty;
                var code = _editingUser.EmployeeCode?.Trim() ?? string.Empty;
                if (name.Length > 0 && code.Length > 0) return $"{name} — {code}";
                if (name.Length > 0) return name;
            }
            return "Không xác định";
        }
    }

    public string DraftPassword
    {
        get => _draftPassword;
        set
        {
            if (!SetField(ref _draftPassword, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public bool DraftIsActive
    {
        get => _draftIsActive;
        set
        {
            if (!SetField(ref _draftIsActive, value)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public bool HasDraftRoles => DraftRoleOptions.Count > 0;

    public string EditorMessage
    {
        get => _editorMessage;
        private set
        {
            if (!SetField(ref _editorMessage, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasEditorMessage));
        }
    }

    public bool EditorMessageIsError
    {
        get => _editorMessageIsError;
        private set => SetField(ref _editorMessageIsError, value);
    }

    public bool HasEditorMessage => !string.IsNullOrWhiteSpace(EditorMessage);

    public bool HasConflict
    {
        get => _hasConflict;
        private set
        {
            if (!SetField(ref _hasConflict, value)) return;
            OnPropertyChanged(nameof(CanPersist));
            OnPropertyChanged(nameof(ShowReloadAfterConflict));
        }
    }

    public bool ShowReloadAfterConflict => HasConflict;

    public bool IsToggleConfirmOpen
    {
        get => _isToggleConfirmOpen;
        private set
        {
            if (!SetField(ref _isToggleConfirmOpen, value)) return;
            OnPropertyChanged(nameof(CanConfirmToggle));
        }
    }

    public string ToggleTitle => "Xác nhận thay đổi trạng thái";
    public string ToggleText => _pendingToggleNextActive
        ? "Đưa người dùng này vào sử dụng? Nhân sự liên kết phải đang làm việc."
        : "Ngừng sử dụng người dùng này? Các vai trò được giữ nguyên để có thể dùng lại khi cần.";
    public string ToggleConfirmButtonText => IsBusy ? "Đang xử lý…" : "Xác nhận";

    public string ToggleMessage
    {
        get => _toggleMessage;
        private set
        {
            if (!SetField(ref _toggleMessage, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasToggleMessage));
        }
    }

    public bool HasToggleMessage => !string.IsNullOrWhiteSpace(ToggleMessage);

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanViewUsers || !_access.Current.IsAuthenticated) return;
        await RefreshAsync();
    }

    public Task RefreshAsync() => RefreshCoreAsync();

    private async Task RefreshCoreAsync()
    {
        if (!CanViewUsers || !_access.Current.IsAuthenticated) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var accessGeneration = _accessGeneration;
        var loadGeneration = ++_loadGeneration;

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;

        IReadOnlyList<AccessUserData>? users = null;
        IReadOnlyList<EmployeeDirectoryData>? employees = null;
        IReadOnlyList<AccessRoleData>? roles = null;
        var errors = new List<string>();

        try
        {
            var usersTask = TryLoadAsync(
                () => _userReadService.ListUsersAsync(token),
                "Không tải được danh sách người dùng.",
                errors);

            Task<IReadOnlyList<EmployeeDirectoryData>?> employeesTask = CanReadEmployees
                ? TryLoadAsync(
                    () => _employeeReadService.ListEmployeesAsync(token),
                    "Không tải được danh mục nhân sự.",
                    errors)
                : Task.FromResult<IReadOnlyList<EmployeeDirectoryData>?>(null);

            Task<IReadOnlyList<AccessRoleData>?> rolesTask = CanReadRoles
                ? TryLoadAsync(
                    () => _roleReadService.ListRolesAsync(token),
                    "Không tải được danh sách vai trò.",
                    errors)
                : Task.FromResult<IReadOnlyList<AccessRoleData>?>(null);

            if (!CanReadEmployees)
                errors.Add("Tài khoản chưa được cấp quyền xem danh mục nhân sự.");
            if (!CanReadRoles)
                errors.Add("Tài khoản chưa được cấp quyền xem danh sách vai trò.");

            users = await usersTask.ConfigureAwait(false);
            employees = await employeesTask.ConfigureAwait(false);
            roles = await rolesTask.ConfigureAwait(false);

            if (token.IsCancellationRequested
                || accessGeneration != _accessGeneration
                || loadGeneration != _loadGeneration)
                return;

            RunOnUiThread(() =>
            {
                if (users is not null)
                {
                    _users.Clear();
                    _users.AddRange(users);
                    _loaded = true;
                }

                if (employees is not null)
                {
                    _employees.Clear();
                    _employees.AddRange(employees);
                }

                if (roles is not null)
                {
                    _roles.Clear();
                    _roles.AddRange(roles);
                }

                RebuildEligibleEmployees();
                ApplyFilter();
                RaiseSummary();
                MessageIsError = errors.Count > 0;
                Message = string.Join(" · ", errors.Distinct(StringComparer.Ordinal));
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (accessGeneration == _accessGeneration && loadGeneration == _loadGeneration)
                RunOnUiThread(() => IsBusy = false);
        }
    }

    private async Task<IReadOnlyList<T>?> TryLoadAsync<T>(
        Func<Task<IReadOnlyList<T>>> loader,
        string fallback,
        List<string> errors)
    {
        try
        {
            return await loader().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            lock (errors)
            {
                errors.Add(PublicError(exception, fallback));
            }
            return null;
        }
    }

    public void OpenCreate()
    {
        if (!CanOpenCreate) return;

        _editingUser = null;
        HasConflict = false;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        IsCreateMode = true;
        DraftLoginName = string.Empty;
        DraftEmployeeId = string.Empty;
        DraftPassword = string.Empty;
        DraftIsActive = true;
        RebuildDraftRoleOptions([]);
        IsEditorOpen = true;
    }

    public void OpenEdit(AccessUserData user)
    {
        if (!CanEditUsers) return;

        _editingUser = user;
        HasConflict = false;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        IsCreateMode = false;
        DraftLoginName = user.LoginName;
        DraftEmployeeId = user.EmployeeId ?? string.Empty;
        DraftPassword = string.Empty;
        DraftIsActive = user.IsActive;
        RebuildDraftRoleOptions(user.RoleIds);
        IsEditorOpen = true;
        OnPropertyChanged(nameof(DraftEmployeeLabel));
    }

    public void CloseEditor()
    {
        IsEditorOpen = false;
        _editingUser = null;
        HasConflict = false;
        DraftPassword = string.Empty;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        DraftRoleOptions.Clear();
        OnPropertyChanged(nameof(HasDraftRoles));
    }

    public async Task SaveAsync()
    {
        if (!CanPersist) return;

        IsBusy = true;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        HasConflict = false;

        try
        {
            if (IsCreateMode)
                await ProvisionNewUserAsync().ConfigureAwait(true);
            else
                await SaveExistingUserAsync().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ProvisionNewUserAsync()
    {
        AccessUserData? latest = null;
        var desiredRoles = SelectedRoleIds();

        try
        {
            var createRequest = new AccessUserCreateRequest(
                DraftLoginName.Trim().ToLowerInvariant(),
                DraftEmployeeId.Trim(),
                false);
            var createSlot = CreateIntent(createRequest);
            latest = await _mutationService.CreateAsync(
                createRequest,
                MutationKey(createSlot, "access-user-create")).ConfigureAwait(true);
            ForgetMutationKey(createSlot);
            UpsertUser(latest);

            var rolesRequest = new AccessUserRolesRequest(desiredRoles, latest.UpdatedAt);
            var rolesSlot = RolesIntent(latest.Id, rolesRequest);
            latest = await _mutationService.ReplaceRolesAsync(
                latest.Id,
                rolesRequest,
                MutationKey(rolesSlot, "access-user-roles")).ConfigureAwait(true);
            ForgetMutationKey(rolesSlot);
            UpsertUser(latest);

            await _mutationService.SetCredentialAsync(
                latest.Id,
                new AccessUserCredentialRequest(DraftPassword)).ConfigureAwait(true);

            if (DraftIsActive)
            {
                var statusRequest = new AccessUserStatusRequest(true, latest.UpdatedAt);
                var statusSlot = StatusIntent(latest.Id, statusRequest);
                latest = await _mutationService.UpdateStatusAsync(
                    latest.Id,
                    statusRequest,
                    MutationKey(statusSlot, "access-user-status")).ConfigureAwait(true);
                ForgetMutationKey(statusSlot);
                UpsertUser(latest);
            }

            CloseEditor();
            SetMessage(
                "Đã tạo tài khoản, mật khẩu và vai trò. Nhân viên có thể đăng nhập bằng tên đăng nhập vừa cấp.",
                false);
        }
        catch (CanonicalApiException exception) when (latest is not null)
        {
            await RecoverProvisioningAsync(latest, desiredRoles, MutationError(exception, "Không hoàn tất được việc cấp tài khoản."))
                .ConfigureAwait(true);
        }
        catch (Exception exception) when (latest is not null)
        {
            await RecoverProvisioningAsync(
                    latest,
                    desiredRoles,
                    exception is InvalidOperationException ? exception.Message : "Không hoàn tất được việc cấp tài khoản.")
                .ConfigureAwait(true);
        }
        catch (CanonicalApiException exception)
        {
            HandleEditorMutationFailure(exception, "Không tạo được tài khoản.");
        }
        catch (Exception)
        {
            SetEditorError("Không tạo được tài khoản. Vui lòng thử lại.");
        }
    }

    private async Task RecoverProvisioningAsync(
        AccessUserData lastKnown,
        string[] desiredRoles,
        string reason)
    {
        AccessUserData latest = lastKnown;
        try
        {
            var refreshed = await _userReadService.ListUsersAsync().ConfigureAwait(true);
            _users.Clear();
            _users.AddRange(refreshed);
            latest = _users.FirstOrDefault(user => string.Equals(user.Id, lastKnown.Id, StringComparison.Ordinal))
                ?? lastKnown;
        }
        catch
        {
            UpsertUser(lastKnown);
        }

        _editingUser = latest;
        IsCreateMode = false;
        DraftLoginName = latest.LoginName;
        DraftEmployeeId = latest.EmployeeId ?? DraftEmployeeId;
        RebuildDraftRoleOptions(desiredRoles);
        RebuildEligibleEmployees();
        ApplyFilter();
        RaiseSummary();
        HasConflict = false;
        EditorMessageIsError = true;
        EditorMessage =
            $"{reason} Tài khoản đã được giữ an toàn ở trạng thái hiện tại; biểu mẫu vẫn mở để hoàn tất vai trò, mật khẩu và kích hoạt.";
        OnPropertyChanged(nameof(DraftEmployeeLabel));
    }

    private async Task SaveExistingUserAsync()
    {
        if (_editingUser is null) return;

        var latest = _editingUser;
        var changed = false;
        var selectedRoles = SelectedRoleIds();

        try
        {
            if (CanManageUserRoles && !SameIds(selectedRoles, latest.RoleIds))
            {
                var rolesRequest = new AccessUserRolesRequest(selectedRoles, latest.UpdatedAt);
                var rolesSlot = RolesIntent(latest.Id, rolesRequest);
                latest = await _mutationService.ReplaceRolesAsync(
                    latest.Id,
                    rolesRequest,
                    MutationKey(rolesSlot, "access-user-roles")).ConfigureAwait(true);
                ForgetMutationKey(rolesSlot);
                _editingUser = latest;
                UpsertUser(latest);
                changed = true;
            }

            if (CanWriteUsers && DraftPassword.Length > 0)
            {
                await _mutationService.SetCredentialAsync(
                    latest.Id,
                    new AccessUserCredentialRequest(DraftPassword)).ConfigureAwait(true);
                changed = true;
            }

            if (CanWriteUsers && DraftIsActive != latest.IsActive)
            {
                var statusRequest = new AccessUserStatusRequest(DraftIsActive, latest.UpdatedAt);
                var statusSlot = StatusIntent(latest.Id, statusRequest);
                latest = await _mutationService.UpdateStatusAsync(
                    latest.Id,
                    statusRequest,
                    MutationKey(statusSlot, "access-user-status")).ConfigureAwait(true);
                ForgetMutationKey(statusSlot);
                _editingUser = latest;
                UpsertUser(latest);
                changed = true;
            }

            CloseEditor();
            SetMessage(changed ? "Đã cập nhật người dùng." : "Không có thay đổi cần lưu.", false);
        }
        catch (CanonicalApiException exception)
        {
            HandleEditorMutationFailure(exception, "Không cập nhật được người dùng.");
        }
        catch (Exception)
        {
            SetEditorError("Không cập nhật được người dùng. Vui lòng thử lại.");
        }
    }

    public async Task ReloadAfterConflictAsync()
    {
        if (!HasConflict || _editingUser is null) return;

        var userId = _editingUser.Id;
        var desiredRoles = SelectedRoleIds();
        var desiredLogin = DraftLoginName;
        var desiredEmployee = DraftEmployeeId;
        var desiredPassword = DraftPassword;
        var desiredActive = DraftIsActive;

        await RefreshCoreAsync().ConfigureAwait(true);

        var latest = _users.FirstOrDefault(user => string.Equals(user.Id, userId, StringComparison.Ordinal));
        if (latest is null)
        {
            CloseEditor();
            SetMessage("Người dùng không còn tồn tại trong danh sách hiện tại.", true);
            return;
        }

        _editingUser = latest;
        IsCreateMode = false;
        DraftLoginName = desiredLogin;
        DraftEmployeeId = desiredEmployee;
        DraftPassword = desiredPassword;
        DraftIsActive = desiredActive;
        RebuildDraftRoleOptions(desiredRoles);
        HasConflict = false;
        EditorMessageIsError = false;
        EditorMessage = "Đã tải lại phiên bản mới nhất. Vui lòng kiểm tra trước khi lưu tiếp.";
        OnPropertyChanged(nameof(DraftEmployeeLabel));
    }

    public void OpenToggle(AccessUserData user)
    {
        if (!CanToggleUsers) return;

        _pendingToggleUser = user;
        _pendingToggleNextActive = !user.IsActive;
        ToggleMessage = string.Empty;
        IsToggleConfirmOpen = true;
        OnPropertyChanged(nameof(ToggleText));
    }

    public void CancelToggle()
    {
        IsToggleConfirmOpen = false;
        _pendingToggleUser = null;
        ToggleMessage = string.Empty;
        OnPropertyChanged(nameof(ToggleText));
    }

    public async Task ConfirmToggleAsync()
    {
        if (!CanConfirmToggle || _pendingToggleUser is null) return;

        var user = _pendingToggleUser;
        var request = new AccessUserStatusRequest(_pendingToggleNextActive, user.UpdatedAt);
        var slot = StatusIntent(user.Id, request);

        IsBusy = true;
        ToggleMessage = string.Empty;
        try
        {
            var updated = await _mutationService.UpdateStatusAsync(
                user.Id,
                request,
                MutationKey(slot, "access-user-status")).ConfigureAwait(true);
            ForgetMutationKey(slot);
            UpsertUser(updated);
            var activated = _pendingToggleNextActive;
            CancelToggle();
            SetMessage(
                activated ? "Đã đưa người dùng vào sử dụng." : "Đã ngừng sử dụng người dùng.",
                false);
        }
        catch (CanonicalApiException exception)
        {
            ToggleMessage = IsOptimisticConflict(exception)
                ? CanonicalErrorMessages.WithRequestId(
                    "Người dùng vừa có thay đổi. Vui lòng hủy xác nhận và tải lại dữ liệu trước khi thử lại.",
                    exception.RequestId)
                : MutationError(exception, "Không cập nhật được trạng thái người dùng.");
        }
        catch (Exception)
        {
            ToggleMessage = "Không cập nhật được trạng thái người dùng. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void HandleEditorMutationFailure(CanonicalApiException exception, string fallback)
    {
        if (IsOptimisticConflict(exception))
        {
            HasConflict = true;
            SetEditorError(CanonicalErrorMessages.WithRequestId(
                "Người dùng vừa có thay đổi. Hãy tải lại dữ liệu trước khi lưu tiếp.",
                exception.RequestId));
            return;
        }

        SetEditorError(MutationError(exception, fallback));
    }

    private void UpsertUser(AccessUserData user)
    {
        var index = _users.FindIndex(item => string.Equals(item.Id, user.Id, StringComparison.Ordinal));
        if (index >= 0) _users[index] = user;
        else _users.Add(user);
        RebuildEligibleEmployees();
        ApplyFilter();
        RaiseSummary();
    }

    private void RebuildEligibleEmployees()
    {
        var linked = _users
            .Select(user => user.EmployeeId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        EligibleEmployees.Clear();
        foreach (var employee in _employees
                     .Where(employee => employee.IsActive && !linked.Contains(employee.Id))
                     .OrderBy(employee => employee.Code, StringComparer.OrdinalIgnoreCase))
        {
            EligibleEmployees.Add(new UserEmployeeOption(
                employee.Id,
                $"{employee.FullName} — {employee.Code}"));
        }

        OnPropertyChanged(nameof(HasEligibleEmployees));
        OnPropertyChanged(nameof(CanOpenCreate));
    }

    private void RebuildDraftRoleOptions(IEnumerable<string> selectedRoleIds)
    {
        var selected = selectedRoleIds.ToHashSet(StringComparer.Ordinal);
        var assigned = _editingUser?.RoleIds.ToHashSet(StringComparer.Ordinal) ?? [];
        var roles = _roles
            .Where(role => role.IsActive || assigned.Contains(role.Id))
            .OrderBy(role => role.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        DraftRoleOptions.Clear();
        foreach (var role in roles)
        {
            var option = new UserRoleOptionView(
                role.Id,
                role.Name,
                role.IsActive,
                selected.Contains(role.Id));
            option.PropertyChanged += DraftRoleOptionChanged;
            DraftRoleOptions.Add(option);
        }

        OnPropertyChanged(nameof(HasDraftRoles));
        OnPropertyChanged(nameof(CanPersist));
    }

    private void DraftRoleOptionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserRoleOptionView.IsSelected))
            OnPropertyChanged(nameof(CanPersist));
    }

    private string[] SelectedRoleIds() =>
        DraftRoleOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Id)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

    private bool CreateDraftIsValid() =>
        CanWriteUsers
        && CanManageUserRoles
        && LoginIsValid(DraftLoginName)
        && EligibleEmployees.Any(option => string.Equals(option.Id, DraftEmployeeId, StringComparison.Ordinal))
        && PasswordIsValid(DraftPassword)
        && SelectedRoleIds().Length > 0;

    private bool EditDraftIsValid()
    {
        if (_editingUser is null || (!CanWriteUsers && !CanManageUserRoles)) return false;
        if (DraftPassword.Length > 0 && (!CanWriteUsers || !PasswordIsValid(DraftPassword))) return false;
        return true;
    }

    private static bool LoginIsValid(string value) =>
        value.Length is >= 1 and <= 128
        && Regex.IsMatch(value, "^[a-z0-9._-]+$", RegexOptions.CultureInvariant);

    private static bool PasswordIsValid(string value) =>
        value.Length is >= 10 and <= 256;

    private static string NormalizeLogin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Trim().ToLowerInvariant();
    }

    private static bool SameIds(IEnumerable<string> left, IEnumerable<string> right)
    {
        var a = left.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var b = right.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        return a.SequenceEqual(b, StringComparer.Ordinal);
    }

    private string MutationKey(string intent, string scope)
    {
        if (_mutationKeys.TryGetValue(intent, out var existing)) return existing;

        var created = _idempotencyKeys.Create(scope);
        if (!_idempotencyKeys.IsValid(created))
            throw new InvalidOperationException("Không tạo được khóa chống xử lý trùng hợp lệ.");

        _mutationKeys[intent] = created;
        return created;
    }

    private void ForgetMutationKey(string intent) => _mutationKeys.Remove(intent);

    private static string CreateIntent(AccessUserCreateRequest request) =>
        $"create|{request.LoginName}|{request.EmployeeId}|{request.IsActive}";

    private static string RolesIntent(string userId, AccessUserRolesRequest request) =>
        $"roles|{userId}|{string.Join(",", request.RoleIds)}|{request.ExpectedUpdatedAt}";

    private static string StatusIntent(string userId, AccessUserStatusRequest request) =>
        $"status|{userId}|{request.IsActive}|{request.ExpectedUpdatedAt}";

    private static bool IsOptimisticConflict(CanonicalApiException exception) =>
        exception.StatusCode == HttpStatusCode.Conflict
        && string.Equals(exception.Code, "CONFLICT", StringComparison.OrdinalIgnoreCase);

    private static string MutationError(CanonicalApiException exception, string fallback)
    {
        var message = exception.Code.ToUpperInvariant() switch
        {
            "SECURITY_OWNER_PROTECTED" =>
                "Tài khoản Chủ sở hữu hệ thống đang được bảo vệ và chỉ người có thẩm quyền tương ứng mới được thay đổi.",
            "DUPLICATE_LOGIN" =>
                "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.",
            "DUPLICATE_EMPLOYEE" =>
                "Nhân sự này đã được liên kết với một tài khoản khác.",
            "INVALID_EMPLOYEE_ID" =>
                "Nhân sự liên kết không còn hợp lệ hoặc đã ngừng làm việc.",
            "INVALID_ROLE_ID" =>
                "Danh sách vai trò có mục không còn sử dụng. Vui lòng tải lại dữ liệu.",
            "INTERNAL_AUTH_PASSWORD_INVALID" =>
                "Mật khẩu phải có từ 10 đến 256 ký tự.",
            "NOT_FOUND" or "USER_NOT_FOUND" =>
                "Người dùng không còn tồn tại.",
            _ => CanonicalErrorMessages.ToOfficeMessage(exception)
        };

        if (string.IsNullOrWhiteSpace(message)) message = fallback;
        return CanonicalErrorMessages.WithRequestId(message, exception.RequestId);
    }

    private void ApplyFilter()
    {
        var search = EmployeeDirectoryPresentation.NormalizeSearch(SearchTerm);
        var employeeMap = _employees.ToDictionary(employee => employee.Id, employee => employee, StringComparer.Ordinal);
        var roleMap = _roles.ToDictionary(role => role.Id, role => role, StringComparer.Ordinal);

        var rows = _users
            .Where(user =>
            {
                var matchesStatus = SelectedStatusFilter.Key switch
                {
                    "active" => user.IsActive,
                    "inactive" => !user.IsActive,
                    _ => true
                };
                if (!matchesStatus) return false;

                if (string.IsNullOrWhiteSpace(search)) return true;

                employeeMap.TryGetValue(user.EmployeeId ?? string.Empty, out var employee);
                var roleText = UserDirectoryPresentation.RoleText(user, roleMap);
                var haystack = EmployeeDirectoryPresentation.NormalizeSearch(
                    $"{user.LoginName} {employee?.FullName} {employee?.Code} {user.EmployeeFullName} {user.EmployeeCode} {roleText}");
                return haystack.Contains(search, StringComparison.Ordinal);
            })
            .OrderBy(user => user.LoginName, StringComparer.OrdinalIgnoreCase)
            .Select(user =>
            {
                employeeMap.TryGetValue(user.EmployeeId ?? string.Empty, out var employee);
                var employeeName = employee?.FullName?.Trim();
                if (string.IsNullOrWhiteSpace(employeeName)) employeeName = user.EmployeeFullName?.Trim();
                if (string.IsNullOrWhiteSpace(employeeName)) employeeName = "Không xác định";

                var employeeCode = employee?.Code?.Trim();
                if (string.IsNullOrWhiteSpace(employeeCode)) employeeCode = user.EmployeeCode?.Trim();

                return new UserDirectoryRowView(
                    user,
                    user.LoginName,
                    employeeName,
                    string.IsNullOrWhiteSpace(employeeCode) ? "Chưa có mã nhân sự" : employeeCode,
                    UserDirectoryPresentation.RoleText(user, roleMap),
                    user.IsActive ? "Đang hoạt động" : "Ngừng sử dụng",
                    user.IsActive,
                    EmployeeDirectoryPresentation.DateTimeText(user.UpdatedAt),
                    user.IsActive ? "Ngừng sử dụng" : "Đưa vào sử dụng");
            })
            .ToArray();

        VisibleUsers.Clear();
        foreach (var row in rows) VisibleUsers.Add(row);

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(VisibleUserCountText));
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(CanViewUsers));
        OnPropertyChanged(nameof(CanWriteUsers));
        OnPropertyChanged(nameof(CanManageUserRoles));
        OnPropertyChanged(nameof(CanReadEmployees));
        OnPropertyChanged(nameof(CanReadRoles));
        OnPropertyChanged(nameof(CanOpenCreate));
        OnPropertyChanged(nameof(CanEditUsers));
        OnPropertyChanged(nameof(CanToggleUsers));
        RaiseEditorCapabilities();
    }

    private void RaiseEditorCapabilities()
    {
        OnPropertyChanged(nameof(CanEditPassword));
        OnPropertyChanged(nameof(CanEditStatus));
        OnPropertyChanged(nameof(CanEditRoles));
        OnPropertyChanged(nameof(CanPersist));
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(TotalUsersText));
        OnPropertyChanged(nameof(ActiveUsersText));
        OnPropertyChanged(nameof(InactiveUsersText));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
    }

    private void SetMessage(string message, bool isError)
    {
        MessageIsError = isError;
        Message = message;
    }

    private void SetEditorError(string message)
    {
        EditorMessageIsError = true;
        EditorMessage = message;
    }

    private static string PublicError(Exception exception, string fallback) =>
        exception is CanonicalApiException canonical
            ? CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(canonical),
                canonical.RequestId)
            : fallback;

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
