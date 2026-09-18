using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed record RoleStatusFilterOption(string Key, string Label)
{
    public override string ToString() => Label;
}

public sealed class AccessRolesViewModel : INotifyPropertyChanged
{
    private const string PermissionRead = "core.permission.read";
    private const string RoleRead = "core.role.read";
    private const string RoleWrite = "core.role.write";

    private readonly IAccessRoleReadService _readService;
    private readonly IAccessRoleMutationService _mutationService;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly List<AccessRoleData> _roles = [];
    private readonly List<AccessPermissionData> _permissions = [];
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _searchTerm = string.Empty;
    private RoleStatusFilterOption _selectedStatusFilter;
    private bool _isEditorOpen;
    private bool _isCreateMode;
    private AccessRoleData? _editingRole;
    private string _draftCode = string.Empty;
    private string _draftName = string.Empty;
    private string _draftDescription = string.Empty;
    private bool _draftIsActive = true;
    private bool _draftWebLoginChallengeRequired;
    private RolePresetOption _selectedPreset;
    private string _presetDescription = string.Empty;
    private string _editorMessage = string.Empty;
    private bool _editorMessageIsError;
    private bool _hasConflict;
    private bool _isToggleConfirmOpen;
    private AccessRoleData? _pendingToggleRole;
    private bool _pendingToggleNextActive;
    private string _toggleMessage = string.Empty;
    private long _accessGeneration;
    private long _loadGeneration;
    private CancellationTokenSource? _loadCts;

    public AccessRolesViewModel(
        IAccessRoleReadService readService,
        IAccessRoleMutationService mutationService,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _readService = readService;
        _mutationService = mutationService;
        _idempotencyKeys = idempotencyKeys;
        _access = access;
        _selectedStatusFilter = StatusFilters[0];
        _selectedPreset = RolePresetCatalog.Options[0];
        _presetDescription = _selectedPreset.Description;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loadCts?.Cancel();
            _loadCts = null;
            _loaded = false;
            _roles.Clear();
            _permissions.Clear();
            _mutationKeys.Clear();
            VisibleRoles.Clear();
            PermissionGroups.Clear();
            CloseEditor();
            CancelToggle();
            Message = string.Empty;
            MessageIsError = false;
            IsBusy = false;
            RaiseAccess();
            RaiseSummary();
            if (CanViewRoles && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AccessRoleRowView> VisibleRoles { get; } = [];
    public ObservableCollection<PermissionGroupView> PermissionGroups { get; } = [];

    public IReadOnlyList<RoleStatusFilterOption> StatusFilters { get; } =
    [
        new("all", "Tất cả trạng thái"),
        new("active", "Đang sử dụng"),
        new("inactive", "Ngừng sử dụng")
    ];

    public IReadOnlyList<RolePresetOption> PresetOptions => RolePresetCatalog.Options;

    public bool CanViewRoles => _access.HasPermission(RoleRead);
    public bool CanReadPermissions => _access.HasPermission(PermissionRead);
    public bool CanWriteRoles => _access.HasPermission(RoleWrite);
    public bool CanOpenEditor => CanWriteRoles && CanReadPermissions && !IsBusy;
    public bool CanToggleRoles => CanWriteRoles && !IsBusy;
    public bool CanPersist =>
        CanWriteRoles
        && IsEditorOpen
        && !IsBusy
        && !HasConflict
        && DraftName.Trim().Length is > 0 and <= 256
        && DraftDescription.Length <= 512
        && (!IsCreateMode || DraftCode.Trim().Length is > 0 and <= 64);
    public bool CanConfirmToggle => IsToggleConfirmOpen && CanWriteRoles && !IsBusy;

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool ShowLoading => IsBusy && !_loaded;
    public bool IsEmpty => _loaded && VisibleRoles.Count == 0;
    public string EmptyText => IsEmpty ? "Không tìm thấy vai trò phù hợp." : string.Empty;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanOpenEditor));
            OnPropertyChanged(nameof(CanToggleRoles));
            OnPropertyChanged(nameof(CanPersist));
            OnPropertyChanged(nameof(CanConfirmToggle));
            OnPropertyChanged(nameof(RefreshText));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(ToggleConfirmButtonText));
            OnPropertyChanged(nameof(ShowLoading));
        }
    }

    public string RefreshText => IsBusy ? "Đang cập nhật…" : "Cập nhật dữ liệu";

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

    public RoleStatusFilterOption SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (value is null || !SetField(ref _selectedStatusFilter, value)) return;
            ApplyFilter();
        }
    }

    public string TotalRolesText => _roles.Count.ToString("N0");
    public string ActiveRolesText => _roles.Count(role => role.IsActive).ToString("N0");
    public string InactiveRolesHint => $"{_roles.Count(role => !role.IsActive):N0} vai trò đã ngừng sử dụng";
    public string PermissionCountText => _permissions.Count.ToString("N0");
    public string VisibleRoleCountText => $"{VisibleRoles.Count:N0} vai trò";

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        private set
        {
            if (!SetField(ref _isEditorOpen, value)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public bool IsCreateMode
    {
        get => _isCreateMode;
        private set
        {
            if (!SetField(ref _isCreateMode, value)) return;
            OnPropertyChanged(nameof(EditorKicker));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(IsCodeEditable));
            OnPropertyChanged(nameof(ShowPresetPanel));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string EditorKicker => IsCreateMode ? "VAI TRÒ MỚI" : "CẬP NHẬT VAI TRÒ";
    public string EditorTitle => IsCreateMode ? "Thêm vai trò quản trị" : "Chỉnh sửa vai trò quản trị";
    public bool IsCodeEditable => IsCreateMode;
    public bool ShowPresetPanel => IsCreateMode;
    public string SaveButtonText => IsBusy ? "Đang lưu…" : IsCreateMode ? "Tạo vai trò" : "Lưu thay đổi";

    public string DraftCode
    {
        get => _draftCode;
        set
        {
            if (!SetField(ref _draftCode, (value ?? string.Empty).ToUpperInvariant())) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string DraftName
    {
        get => _draftName;
        set
        {
            if (!SetField(ref _draftName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string DraftDescription
    {
        get => _draftDescription;
        set
        {
            if (!SetField(ref _draftDescription, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public bool DraftIsActive
    {
        get => _draftIsActive;
        set => SetField(ref _draftIsActive, value);
    }

    public bool DraftWebLoginChallengeRequired
    {
        get => _draftWebLoginChallengeRequired;
        set => SetField(ref _draftWebLoginChallengeRequired, value);
    }

    public RolePresetOption SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (value is null || !SetField(ref _selectedPreset, value)) return;
            PresetDescription = value.Description;
            if (IsCreateMode) ApplyPreset(value.Id);
        }
    }

    public string PresetDescription
    {
        get => _presetDescription;
        private set => SetField(ref _presetDescription, value);
    }

    public string SelectedPermissionCountText =>
        $"{PermissionGroups.SelectMany(group => group.Items).Count(item => item.IsSelected):N0} quyền đã chọn";

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

    public string ToggleTitle => _pendingToggleNextActive ? "Đưa vai trò vào sử dụng" : "Ngừng sử dụng vai trò";
    public string ToggleText => _pendingToggleNextActive
        ? "Vai trò sẽ trở lại trạng thái đang sử dụng."
        : "Vai trò sẽ ngừng sử dụng nhưng vẫn được giữ lại để đối soát và lịch sử chứng từ.";
    public string ToggleConfirmButtonText => IsBusy ? "Đang cập nhật…" : "Xác nhận";

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
        if (_loaded || !CanViewRoles || !_access.Current.IsAuthenticated) return;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (!CanViewRoles || !_access.Current.IsAuthenticated) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var accessGeneration = _accessGeneration;
        var loadGeneration = ++_loadGeneration;

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;

        IReadOnlyList<AccessRoleData>? roles = null;
        IReadOnlyList<AccessPermissionData>? permissions = null;
        var errors = new List<string>();

        try
        {
            try
            {
                roles = await _readService.ListRolesAsync(token).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                errors.Add(PublicError(exception, "Không tải được danh sách vai trò."));
            }

            if (CanReadPermissions)
            {
                try
                {
                    permissions = await _readService.ListPermissionsAsync(token).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    errors.Add(PublicError(exception, "Không tải được danh mục quyền."));
                }
            }
            else
            {
                errors.Add("Tài khoản chưa được cấp quyền xem danh mục quyền.");
            }

            if (token.IsCancellationRequested
                || accessGeneration != _accessGeneration
                || loadGeneration != _loadGeneration)
                return;

            RunOnUiThread(() =>
            {
                if (roles is not null)
                {
                    _roles.Clear();
                    _roles.AddRange(roles);
                }

                if (permissions is not null)
                {
                    _permissions.Clear();
                    _permissions.AddRange(permissions);
                }

                _loaded = roles is not null;
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

    public void OpenCreate()
    {
        if (!CanOpenEditor) return;

        _editingRole = null;
        HasConflict = false;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        IsCreateMode = true;
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftDescription = string.Empty;
        DraftIsActive = true;
        DraftWebLoginChallengeRequired = false;
        BuildPermissionGroups([]);
        _selectedPreset = RolePresetCatalog.Options[0];
        PresetDescription = _selectedPreset.Description;
        OnPropertyChanged(nameof(SelectedPreset));
        IsEditorOpen = true;
    }

    public void OpenEdit(AccessRoleData role)
    {
        if (!CanOpenEditor) return;

        _editingRole = role;
        HasConflict = false;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        IsCreateMode = false;
        DraftCode = role.Code;
        DraftName = role.Name;
        DraftDescription = role.Description ?? string.Empty;
        DraftIsActive = role.IsActive;
        DraftWebLoginChallengeRequired = role.WebLoginChallengeRequired;
        BuildPermissionGroups(role.PermissionKeys);
        _selectedPreset = RolePresetCatalog.Options[0];
        PresetDescription = "Vai trò hiện có được mở theo tập quyền đang lưu. Mẫu gợi ý chỉ dùng khi tạo vai trò mới.";
        OnPropertyChanged(nameof(SelectedPreset));
        IsEditorOpen = true;
    }

    public void CloseEditor()
    {
        IsEditorOpen = false;
        _editingRole = null;
        HasConflict = false;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
    }

    public async Task SaveAsync()
    {
        if (!CanPersist) return;

        var name = DraftName.Trim();
        var description = DraftDescription.Trim();
        var permissionKeys = PermissionGroups
            .SelectMany(group => group.Items)
            .Where(item => item.IsSelected)
            .Select(item => item.PermissionKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        if (IsCreateMode)
        {
            var code = DraftCode.Trim().ToUpperInvariant();
            var request = new AccessRoleCreateRequest(
                code,
                name,
                description,
                DraftIsActive,
                DraftWebLoginChallengeRequired,
                permissionKeys);
            var slot = BuildCreateSlot(request);
            var key = MutationKey(slot, "access-role-create");

            IsBusy = true;
            EditorMessage = string.Empty;
            EditorMessageIsError = false;
            try
            {
                await _mutationService.CreateAsync(request, key).ConfigureAwait(true);
                _mutationKeys.Remove(slot);
                CloseEditor();
                IsBusy = false;
                await RefreshAsync().ConfigureAwait(true);
                SetMessage("Đã tạo vai trò mới.", false);
            }
            catch (CanonicalApiException exception)
            {
                SetEditorError(CanonicalErrorMessages.WithRequestId(
                    CanonicalErrorMessages.ToOfficeMessage(exception),
                    exception.RequestId));
            }
            catch (Exception)
            {
                SetEditorError("Không tạo được vai trò. Vui lòng thử lại.");
            }
            finally
            {
                IsBusy = false;
            }
            return;
        }

        if (_editingRole is null) return;

        var requestUpdate = new AccessRoleUpdateRequest(
            name,
            description,
            DraftIsActive,
            DraftWebLoginChallengeRequired,
            permissionKeys,
            _editingRole.UpdatedAt);
        var updateSlot = BuildUpdateSlot(_editingRole.Id, requestUpdate);
        var updateKey = MutationKey(updateSlot, "access-role-update");

        IsBusy = true;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        try
        {
            await _mutationService.UpdateAsync(
                _editingRole.Id,
                requestUpdate,
                updateKey).ConfigureAwait(true);
            _mutationKeys.Remove(updateSlot);
            CloseEditor();
            IsBusy = false;
            await RefreshAsync().ConfigureAwait(true);
            SetMessage("Vai trò đã được cập nhật.", false);
        }
        catch (CanonicalApiException exception) when (IsOptimisticConflict(exception))
        {
            HasConflict = true;
            SetEditorError(CanonicalErrorMessages.WithRequestId(
                "Vai trò đang có thay đổi. Vui lòng tải lại dữ liệu trước khi lưu tiếp.",
                exception.RequestId));
        }
        catch (CanonicalApiException exception)
        {
            SetEditorError(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId));
        }
        catch (Exception)
        {
            SetEditorError("Không lưu được vai trò. Vui lòng thử lại.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ReloadAfterConflictAsync()
    {
        if (!HasConflict) return;
        _mutationKeys.Clear();
        CloseEditor();
        await RefreshAsync().ConfigureAwait(true);
        SetMessage("Đã tải lại dữ liệu vai trò. Vui lòng kiểm tra trước khi thực hiện lại.", false);
    }

    public void OpenToggle(AccessRoleData role)
    {
        if (!CanToggleRoles) return;
        _pendingToggleRole = role;
        _pendingToggleNextActive = !role.IsActive;
        ToggleMessage = string.Empty;
        IsToggleConfirmOpen = true;
        OnPropertyChanged(nameof(ToggleTitle));
        OnPropertyChanged(nameof(ToggleText));
    }

    public void CancelToggle()
    {
        IsToggleConfirmOpen = false;
        _pendingToggleRole = null;
        ToggleMessage = string.Empty;
        OnPropertyChanged(nameof(ToggleTitle));
        OnPropertyChanged(nameof(ToggleText));
    }

    public async Task ConfirmToggleAsync()
    {
        if (!CanConfirmToggle || _pendingToggleRole is null) return;

        var role = _pendingToggleRole;
        var nextActive = _pendingToggleNextActive;
        var request = new AccessRoleToggleRequest(nextActive, role.UpdatedAt);
        var slot = $"toggle|{role.Id}|{nextActive}|{role.UpdatedAt}";
        var key = MutationKey(slot, "access-role-toggle");

        IsBusy = true;
        ToggleMessage = string.Empty;
        try
        {
            await _mutationService.ToggleAsync(role.Id, request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            CancelToggle();
            IsBusy = false;
            await RefreshAsync().ConfigureAwait(true);
            SetMessage(
                nextActive
                    ? "Vai trò đã được đưa vào sử dụng."
                    : "Vai trò đã ngừng sử dụng.",
                false);
        }
        catch (CanonicalApiException exception) when (IsOptimisticConflict(exception))
        {
            ToggleMessage = CanonicalErrorMessages.WithRequestId(
                "Vai trò đang có thay đổi. Vui lòng hủy xác nhận và cập nhật dữ liệu trước khi thử lại.",
                exception.RequestId);
        }
        catch (CanonicalApiException exception)
        {
            ToggleMessage = CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId);
        }
        catch (Exception)
        {
            ToggleMessage = "Không cập nhật được trạng thái vai trò. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        var normalizedSearch = AccessRolePresentation.NormalizeSearch(SearchTerm);
        var permissionMap = _permissions.ToDictionary(
            permission => permission.PermissionKey,
            permission => permission.Label,
            StringComparer.Ordinal);

        var rows = _roles
            .Where(role =>
            {
                var matchesStatus = SelectedStatusFilter.Key switch
                {
                    "active" => role.IsActive,
                    "inactive" => !role.IsActive,
                    _ => true
                };
                if (!matchesStatus) return false;
                if (string.IsNullOrWhiteSpace(normalizedSearch)) return true;

                var permissionText = string.Join(' ', role.PermissionKeys.Select(
                    key => permissionMap.TryGetValue(key, out var label) ? label : key));
                var challengeText = role.WebLoginChallengeRequired
                    ? "đăng nhập cần mã xác nhận"
                    : "đăng nhập bằng mật khẩu";
                var haystack = AccessRolePresentation.NormalizeSearch(
                    $"{role.Code} {role.Name} {role.Description} {permissionText} {challengeText}");
                return haystack.Contains(normalizedSearch, StringComparison.Ordinal);
            })
            .OrderBy(role => role.Code, StringComparer.Ordinal)
            .Select(role => new AccessRoleRowView(
                role,
                role.Code,
                role.Name,
                string.IsNullOrWhiteSpace(role.Description) ? "Không có mô tả" : role.Description.Trim(),
                role.WebLoginChallengeRequired
                    ? "Đăng nhập trên web/ứng dụng: cần mã xác nhận"
                    : "Đăng nhập trên web/ứng dụng: dùng mật khẩu",
                $"{role.PermissionKeys.Length:N0} quyền",
                AccessRolePresentation.PermissionPreview(role, permissionMap),
                role.IsActive ? "Đang sử dụng" : "Ngừng sử dụng",
                role.IsActive,
                AccessRolePresentation.DateTimeText(role.UpdatedAt)))
            .ToArray();

        VisibleRoles.Clear();
        foreach (var row in rows) VisibleRoles.Add(row);

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(VisibleRoleCountText));
    }

    private void BuildPermissionGroups(IEnumerable<string> selectedKeys)
    {
        var selected = selectedKeys.ToHashSet(StringComparer.Ordinal);
        PermissionGroups.Clear();

        foreach (var grouping in _permissions
                     .OrderBy(permission => permission.Module, StringComparer.Ordinal)
                     .ThenBy(permission => permission.PermissionKey, StringComparer.Ordinal)
                     .GroupBy(permission => permission.Module, StringComparer.Ordinal))
        {
            var items = grouping
                .Select(permission => new PermissionOptionView(permission, selected.Contains(permission.PermissionKey)))
                .ToArray();
            foreach (var item in items)
                item.PropertyChanged += PermissionOptionPropertyChanged;

            PermissionGroups.Add(new PermissionGroupView(
                grouping.Key,
                AccessRolePresentation.ModuleLabel(grouping.Key),
                items));
        }

        OnPropertyChanged(nameof(SelectedPermissionCountText));
    }

    private void ApplyPreset(string presetId)
    {
        var selected = RolePresetCatalog.Resolve(presetId, _permissions);
        foreach (var option in PermissionGroups.SelectMany(group => group.Items))
            option.IsSelected = selected.Contains(option.PermissionKey);
        OnPropertyChanged(nameof(SelectedPermissionCountText));
        OnPropertyChanged(nameof(CanPersist));
    }

    private void PermissionOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PermissionOptionView.IsSelected)) return;
        OnPropertyChanged(nameof(SelectedPermissionCountText));
        OnPropertyChanged(nameof(CanPersist));
    }

    private string MutationKey(string slot, string scope)
    {
        if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;
        var created = _idempotencyKeys.Create(scope);
        _mutationKeys[slot] = created;
        return created;
    }

    private static string BuildCreateSlot(AccessRoleCreateRequest request) =>
        string.Join("|",
            "create",
            request.Code,
            request.Name,
            request.Description,
            request.IsActive,
            request.WebLoginChallengeRequired,
            string.Join(";", request.PermissionKeys));

    private static string BuildUpdateSlot(string roleId, AccessRoleUpdateRequest request) =>
        string.Join("|",
            "update",
            roleId,
            request.Name,
            request.Description,
            request.IsActive,
            request.WebLoginChallengeRequired,
            request.ExpectedUpdatedAt,
            string.Join(";", request.PermissionKeys));

    private static bool IsOptimisticConflict(CanonicalApiException exception) =>
        exception.StatusCode == HttpStatusCode.Conflict
        && string.Equals(exception.Code, "CONFLICT", StringComparison.Ordinal);

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(CanViewRoles));
        OnPropertyChanged(nameof(CanReadPermissions));
        OnPropertyChanged(nameof(CanWriteRoles));
        OnPropertyChanged(nameof(CanOpenEditor));
        OnPropertyChanged(nameof(CanToggleRoles));
        OnPropertyChanged(nameof(CanPersist));
        OnPropertyChanged(nameof(CanConfirmToggle));
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(TotalRolesText));
        OnPropertyChanged(nameof(ActiveRolesText));
        OnPropertyChanged(nameof(InactiveRolesHint));
        OnPropertyChanged(nameof(PermissionCountText));
        OnPropertyChanged(nameof(VisibleRoleCountText));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
    }

    private void SetMessage(string text, bool isError)
    {
        Message = text;
        MessageIsError = isError;
    }

    private void SetEditorError(string text)
    {
        EditorMessage = text;
        EditorMessageIsError = true;
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
