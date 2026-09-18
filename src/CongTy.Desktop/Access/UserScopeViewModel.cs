using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed class UserScopeViewModel : INotifyPropertyChanged
{
    private const string UserRead = "core.user.read";
    private const string UserRoleWrite = "core.user-role.write";
    private const string BranchRead = "core.branch.read";
    private const string WarehouseRead = "core.warehouse.read";

    private readonly IUserDirectoryReadService _userReadService;
    private readonly IUserScopeService _scopeService;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly List<AccessUserData> _users = [];
    private readonly List<UserScopeBranchData> _branches = [];
    private readonly List<UserScopeWarehouseData> _warehouses = [];
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);

    private bool _loaded;
    private bool _isBusy;
    private bool _syncingSelection;
    private string _searchTerm = string.Empty;
    private string _errorMessage = string.Empty;
    private string _noticeMessage = string.Empty;
    private UserScopeUserView? _selectedUser;
    private long _accessGeneration;
    private long _loadGeneration;
    private CancellationTokenSource? _loadCts;

    public UserScopeViewModel(
        IUserDirectoryReadService userReadService,
        IUserScopeService scopeService,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _userReadService = userReadService;
        _scopeService = scopeService;
        _idempotencyKeys = idempotencyKeys;
        _access = access;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loadCts?.Cancel();
            _loadCts = null;
            _loaded = false;
            _users.Clear();
            _branches.Clear();
            _warehouses.Clear();
            _mutationKeys.Clear();
            VisibleUsers.Clear();
            BranchOptions.Clear();
            WarehouseOptions.Clear();
            SelectedUser = null;
            ErrorMessage = string.Empty;
            NoticeMessage = string.Empty;
            IsBusy = false;
            RaiseAccess();
            if (CanViewUsers && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<UserScopeUserView> VisibleUsers { get; } = [];
    public ObservableCollection<UserScopeBranchOptionView> BranchOptions { get; } = [];
    public ObservableCollection<UserScopeWarehouseOptionView> WarehouseOptions { get; } = [];

    public bool CanViewUsers => _access.HasPermission(UserRead);
    public bool CanReadBranches => _access.HasPermission(BranchRead);
    public bool CanReadWarehouses => _access.HasPermission(WarehouseRead);
    public bool CanWriteScopes => _access.HasPermission(UserRoleWrite);
    public bool CanLoadScopeData => CanViewUsers && CanReadBranches && CanReadWarehouses;
    public bool CanSaveScopes =>
        HasSelectedUser
        && !OwnerFullScope
        && CanWriteScopes
        && CanReadBranches
        && CanReadWarehouses
        && !IsBusy;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CanSaveScopes));
        }
    }

    public string SaveButtonText => IsBusy ? "Đang lưu…" : "Lưu phạm vi";

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (!SetField(ref _searchTerm, value ?? string.Empty)) return;
            ApplyUserFilter();
        }
    }

    public UserScopeUserView? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (ReferenceEquals(_selectedUser, value)) return;
            _selectedUser = value;
            OnPropertyChanged();
            LoadSelectedUserScopes();
            RaiseSelected();
        }
    }

    public bool HasSelectedUser => SelectedUser is not null;
    public bool OwnerFullScope => SelectedUser?.IsOwner == true;
    public bool ShowEditableScopes => HasSelectedUser && !OwnerFullScope;
    public string SelectedUserName => SelectedUser?.PrimaryText ?? string.Empty;
    public string SelectedLoginName => SelectedUser?.Source.LoginName ?? string.Empty;
    public string EffectiveWarehouseCountText =>
        OwnerFullScope
            ? _warehouses.Count.ToString("N0")
            : WarehouseOptions.Count(option => option.IsSelected).ToString("N0");
    public string BranchSelectionCountText =>
        $"{BranchOptions.Count(option => option.IsSelected):N0}/{BranchOptions.Count:N0}";
    public string WarehouseSelectionCountText =>
        $"{WarehouseOptions.Count(option => option.IsSelected):N0}/{WarehouseOptions.Count:N0}";
    public string ScopeWarningText =>
        WarehouseOptions.Any(option => option.IsSelected)
            ? "Chỉ các kho được chọn bên dưới mới xuất hiện trong dữ liệu nghiệp vụ của tài khoản này."
            : "Chưa cấp kho: tài khoản sẽ không thấy Đơn mua hàng, Phiếu nhận hàng và dữ liệu theo kho.";

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (!SetField(ref _errorMessage, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string NoticeMessage
    {
        get => _noticeMessage;
        private set
        {
            if (!SetField(ref _noticeMessage, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasNotice));
        }
    }

    public bool HasNotice => !string.IsNullOrWhiteSpace(NoticeMessage);
    public bool IsEmpty => _loaded && VisibleUsers.Count == 0;
    public string EmptyText => IsEmpty ? "Không có tài khoản phù hợp." : string.Empty;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanViewUsers || !_access.Current.IsAuthenticated) return;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (!CanViewUsers || !_access.Current.IsAuthenticated) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var accessGeneration = _accessGeneration;
        var loadGeneration = ++_loadGeneration;
        var selectedId = SelectedUser?.Id;

        IsBusy = true;
        ErrorMessage = string.Empty;
        NoticeMessage = string.Empty;

        try
        {
            var errors = new List<string>();
            var usersTask = TryLoadAsync(
                () => _userReadService.ListUsersAsync(token),
                "Không tải được danh sách người dùng.",
                errors);

            Task<IReadOnlyList<UserScopeBranchData>?> branchesTask = CanReadBranches
                ? TryLoadAsync(
                    () => _scopeService.ListBranchesAsync(token),
                    "Không tải được danh sách chi nhánh.",
                    errors)
                : Task.FromResult<IReadOnlyList<UserScopeBranchData>?>(null);

            Task<IReadOnlyList<UserScopeWarehouseData>?> warehousesTask = CanReadWarehouses
                ? TryLoadAsync(
                    () => _scopeService.ListWarehousesAsync(token),
                    "Không tải được danh sách kho hàng.",
                    errors)
                : Task.FromResult<IReadOnlyList<UserScopeWarehouseData>?>(null);

            if (!CanReadBranches)
                errors.Add("Tài khoản chưa được cấp quyền xem chi nhánh.");
            if (!CanReadWarehouses)
                errors.Add("Tài khoản chưa được cấp quyền xem kho hàng.");

            var users = await usersTask.ConfigureAwait(false);
            var branches = await branchesTask.ConfigureAwait(false);
            var warehouses = await warehousesTask.ConfigureAwait(false);

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

                if (branches is not null)
                {
                    _branches.Clear();
                    _branches.AddRange(branches);
                }

                if (warehouses is not null)
                {
                    _warehouses.Clear();
                    _warehouses.AddRange(warehouses);
                }

                ApplyUserFilter(selectedId);
                ErrorMessage = string.Join(" · ", errors.Distinct(StringComparer.Ordinal));
                RaiseSelected();
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

    public async Task SaveScopesAsync()
    {
        if (!CanSaveScopes || SelectedUser is null) return;

        var branchIds = BranchOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Id)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var warehouseIds = WarehouseOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Id)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        var request = new UserScopeReplaceRequest(
            new UserScopeSet(branchIds, warehouseIds, []));
        var intent = ScopeIntent(SelectedUser.Id, request);
        var key = MutationKey(intent);

        IsBusy = true;
        ErrorMessage = string.Empty;
        NoticeMessage = string.Empty;

        try
        {
            var saved = await _scopeService.ReplaceScopesAsync(
                SelectedUser.Id,
                request,
                key).ConfigureAwait(true);
            ForgetMutationKey(intent);

            var currentIndex = _users.FindIndex(user =>
                string.Equals(user.Id, SelectedUser.Id, StringComparison.Ordinal));
            if (currentIndex >= 0)
            {
                var updated = _users[currentIndex] with
                {
                    BranchIds = saved.Scopes.BranchIds,
                    WarehouseIds = saved.Scopes.WarehouseIds
                };
                _users[currentIndex] = updated;
                SelectedUser.UpdateSource(updated);
            }

            ApplySelectionState(new UserScopeSelectionState(
                saved.Scopes.BranchIds,
                saved.Scopes.WarehouseIds));

            NoticeMessage = saved.Scopes.WarehouseIds.Length == 0
                ? "Đã lưu phạm vi trống. Tài khoản này sẽ không thấy chứng từ theo kho cho tới khi được cấp lại."
                : $"Đã cấp {saved.Scopes.WarehouseIds.Length:N0} kho cho {SelectedUser.Source.LoginName}.";
        }
        catch (CanonicalApiException exception)
        {
            ErrorMessage = ScopeMutationError(exception);
        }
        catch (Exception)
        {
            ErrorMessage = "Không cập nhật được phạm vi người dùng. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyUserFilter(string? preferredUserId = null)
    {
        var term = EmployeeDirectoryPresentation.NormalizeSearch(SearchTerm);
        var selectedId = preferredUserId ?? SelectedUser?.Id;

        var rows = _users
            .Where(user =>
            {
                if (string.IsNullOrWhiteSpace(term)) return true;
                var haystack = EmployeeDirectoryPresentation.NormalizeSearch(
                    $"{user.LoginName} {user.EmployeeCode} {user.EmployeeFullName}");
                return haystack.Contains(term, StringComparison.Ordinal);
            })
            .OrderBy(user => user.LoginName, StringComparer.OrdinalIgnoreCase)
            .Select(user => new UserScopeUserView(user))
            .ToArray();

        VisibleUsers.Clear();
        foreach (var row in rows) VisibleUsers.Add(row);

        SelectedUser = VisibleUsers.FirstOrDefault(row =>
                string.Equals(row.Id, selectedId, StringComparison.Ordinal))
            ?? VisibleUsers.FirstOrDefault();

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
    }

    private void LoadSelectedUserScopes()
    {
        BranchOptions.Clear();
        WarehouseOptions.Clear();

        if (SelectedUser is null)
        {
            RaiseSelectionCounts();
            return;
        }

        var selectedBranches = SelectedUser.Source.BranchIds.ToHashSet(StringComparer.Ordinal);
        var selectedWarehouses = SelectedUser.Source.WarehouseIds.ToHashSet(StringComparer.Ordinal);
        var branchMap = _branches.ToDictionary(branch => branch.Id, branch => branch, StringComparer.Ordinal);

        _syncingSelection = true;
        try
        {
            foreach (var branch in _branches.OrderBy(branch => branch.Code, StringComparer.OrdinalIgnoreCase))
            {
                var option = new UserScopeBranchOptionView(
                    branch,
                    selectedBranches.Contains(branch.Id));
                option.PropertyChanged += BranchOptionChanged;
                BranchOptions.Add(option);
            }

            foreach (var warehouse in _warehouses
                         .OrderBy(warehouse =>
                             branchMap.TryGetValue(warehouse.BranchId, out var branch)
                                 ? branch.Code
                                 : string.Empty,
                             StringComparer.OrdinalIgnoreCase)
                         .ThenBy(warehouse => warehouse.Code, StringComparer.OrdinalIgnoreCase))
            {
                var branchName = branchMap.TryGetValue(warehouse.BranchId, out var branch)
                    ? branch.Name
                    : "Không rõ chi nhánh";
                var option = new UserScopeWarehouseOptionView(
                    warehouse,
                    branchName,
                    selectedWarehouses.Contains(warehouse.Id));
                option.PropertyChanged += WarehouseOptionChanged;
                WarehouseOptions.Add(option);
            }
        }
        finally
        {
            _syncingSelection = false;
        }

        RaiseSelectionCounts();
    }

    private void BranchOptionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_syncingSelection
            || e.PropertyName != nameof(UserScopeBranchOptionView.IsSelected)
            || sender is not UserScopeBranchOptionView option)
            return;

        var state = UserScopeSelectionRules.SetBranch(
            option.Id,
            option.IsSelected,
            BranchOptions.Where(item => item.IsSelected).Select(item => item.Id),
            WarehouseOptions.Where(item => item.IsSelected).Select(item => item.Id),
            _warehouses);
        ApplySelectionState(state);
    }

    private void WarehouseOptionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_syncingSelection
            || e.PropertyName != nameof(UserScopeWarehouseOptionView.IsSelected)
            || sender is not UserScopeWarehouseOptionView option)
            return;

        var state = UserScopeSelectionRules.SetWarehouse(
            option.Source,
            option.IsSelected,
            BranchOptions.Where(item => item.IsSelected).Select(item => item.Id),
            WarehouseOptions.Where(item => item.IsSelected).Select(item => item.Id));
        ApplySelectionState(state);
    }

    private void ApplySelectionState(UserScopeSelectionState state)
    {
        var branches = state.BranchIds.ToHashSet(StringComparer.Ordinal);
        var warehouses = state.WarehouseIds.ToHashSet(StringComparer.Ordinal);

        _syncingSelection = true;
        try
        {
            foreach (var option in BranchOptions)
                option.IsSelected = branches.Contains(option.Id);
            foreach (var option in WarehouseOptions)
                option.IsSelected = warehouses.Contains(option.Id);
        }
        finally
        {
            _syncingSelection = false;
        }

        RaiseSelectionCounts();
    }

    private void RaiseSelectionCounts()
    {
        OnPropertyChanged(nameof(EffectiveWarehouseCountText));
        OnPropertyChanged(nameof(BranchSelectionCountText));
        OnPropertyChanged(nameof(WarehouseSelectionCountText));
        OnPropertyChanged(nameof(ScopeWarningText));
        OnPropertyChanged(nameof(CanSaveScopes));
    }

    private void RaiseSelected()
    {
        OnPropertyChanged(nameof(HasSelectedUser));
        OnPropertyChanged(nameof(OwnerFullScope));
        OnPropertyChanged(nameof(ShowEditableScopes));
        OnPropertyChanged(nameof(SelectedUserName));
        OnPropertyChanged(nameof(SelectedLoginName));
        OnPropertyChanged(nameof(EffectiveWarehouseCountText));
        OnPropertyChanged(nameof(CanSaveScopes));
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(CanViewUsers));
        OnPropertyChanged(nameof(CanReadBranches));
        OnPropertyChanged(nameof(CanReadWarehouses));
        OnPropertyChanged(nameof(CanWriteScopes));
        OnPropertyChanged(nameof(CanLoadScopeData));
        OnPropertyChanged(nameof(CanSaveScopes));
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

    private string MutationKey(string intent)
    {
        if (_mutationKeys.TryGetValue(intent, out var existing)) return existing;

        var created = _idempotencyKeys.Create("access-user-scopes");
        if (!_idempotencyKeys.IsValid(created))
            throw new InvalidOperationException("Không tạo được khóa chống xử lý trùng hợp lệ.");

        _mutationKeys[intent] = created;
        return created;
    }

    private void ForgetMutationKey(string intent) => _mutationKeys.Remove(intent);

    private static string ScopeIntent(string userId, UserScopeReplaceRequest request) =>
        $"scopes|{userId}|{string.Join(",", request.Scopes.BranchIds)}|{string.Join(",", request.Scopes.WarehouseIds)}";

    private static string ScopeMutationError(CanonicalApiException exception)
    {
        var message = exception.Code.ToUpperInvariant() switch
        {
            "SECURITY_OWNER_PROTECTED" =>
                "Tài khoản quản trị cấp cao có phạm vi toàn Công Ty và không cấp tay tại đây.",
            "INVALID_SCOPE" =>
                "Phạm vi chi nhánh hoặc kho không hợp lệ. Vui lòng tải lại dữ liệu.",
            "SCOPE_OUTSIDE_INSTALLATION" =>
                "Có chi nhánh hoặc kho không còn thuộc Công Ty. Vui lòng tải lại dữ liệu.",
            "TERRITORY_SCOPE_NOT_CONFIGURED" =>
                "Phạm vi địa bàn chưa sẵn sàng để cấp quyền.",
            "USER_NOT_FOUND" =>
                "Người dùng không còn tồn tại hoặc đã ngừng hoạt động.",
            _ => CanonicalErrorMessages.ToOfficeMessage(exception)
        };
        return CanonicalErrorMessages.WithRequestId(message, exception.RequestId);
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
