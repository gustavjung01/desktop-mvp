using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed partial class EmployeeDirectoryViewModel : INotifyPropertyChanged
{
    private const string EmployeeRead = "core.employee.read";
    private const string EmployeeWrite = "core.employee.write";
    private const string BranchRead = "core.branch.read";

    private readonly IEmployeeDirectoryReadService _readService;
    private readonly IEmployeeDirectoryMutationService _mutationService;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly List<EmployeeDirectoryData> _employees = [];
    private readonly List<EmployeeDirectoryBranchData> _branches = [];
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _searchTerm = string.Empty;
    private EmployeeStatusFilterOption _selectedStatusFilter;
    private EmployeeBranchFilterOption _selectedBranchFilter;
    private bool _isEditorOpen;
    private bool _isCreateMode;
    private EmployeeDirectoryData? _editingEmployee;
    private string _draftCode = string.Empty;
    private string _draftFullName = string.Empty;
    private string _draftJobTitle = string.Empty;
    private string _draftPhone = string.Empty;
    private string _draftEmail = string.Empty;
    private string _draftBranchId = string.Empty;
    private string _editorMessage = string.Empty;
    private bool _editorMessageIsError;
    private bool _hasConflict;
    private bool _isToggleConfirmOpen;
    private EmployeeDirectoryData? _pendingToggleEmployee;
    private bool _pendingToggleNextActive;
    private string _toggleMessage = string.Empty;
    private long _accessGeneration;
    private long _loadGeneration;
    private CancellationTokenSource? _loadCts;

    public EmployeeDirectoryViewModel(
        IEmployeeDirectoryReadService readService,
        IEmployeeDirectoryMutationService mutationService,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _readService = readService;
        _mutationService = mutationService;
        _idempotencyKeys = idempotencyKeys;
        _access = access;
        _selectedStatusFilter = StatusFilters[0];
        _selectedBranchFilter = new EmployeeBranchFilterOption("all", "Tất cả chi nhánh");
        BranchFilters.Add(_selectedBranchFilter);
        BranchFilters.Add(new EmployeeBranchFilterOption("unassigned", "Chưa phân công"));
        DraftBranchOptions.Add(new EmployeeBranchOption(string.Empty, "Chưa phân công"));

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loadCts?.Cancel();
            _loadCts = null;
            _loaded = false;
            _employees.Clear();
            _branches.Clear();
            _mutationKeys.Clear();
            VisibleEmployees.Clear();
            ResetBranchOptions();
            ResetOrganization();
            CloseEditor();
            CancelToggle();
            Message = string.Empty;
            MessageIsError = false;
            IsBusy = false;
            RaiseAccess();
            RaiseSummary();
            if (CanViewEmployees && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<EmployeeDirectoryRowView> VisibleEmployees { get; } = [];
    public ObservableCollection<EmployeeBranchFilterOption> BranchFilters { get; } = [];
    public ObservableCollection<EmployeeBranchOption> DraftBranchOptions { get; } = [];

    public IReadOnlyList<EmployeeStatusFilterOption> StatusFilters { get; } =
    [
        new("all", "Tất cả trạng thái"),
        new("active", "Đang làm việc"),
        new("inactive", "Ngừng làm việc")
    ];

    public bool CanViewEmployees => _access.HasPermission(EmployeeRead);
    public bool CanWriteEmployees => _access.HasPermission(EmployeeWrite);
    public bool CanReadBranches => _access.HasPermission(BranchRead);
    public bool CanOpenEditor => CanWriteEmployees && CanReadBranches && OrganizationLoaded && !IsBusy;
    public bool CanToggleEmployees => CanWriteEmployees && !IsBusy;
    public bool CanConfirmToggle => IsToggleConfirmOpen && CanWriteEmployees && !IsBusy && ToggleDraftValid();
    public bool CanPersist =>
        CanWriteEmployees
        && CanReadBranches
        && IsEditorOpen
        && !IsBusy
        && !HasConflict
        && IsDraftValid();

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanOpenEditor));
            OnPropertyChanged(nameof(CanToggleEmployees));
            OnPropertyChanged(nameof(CanConfirmToggle));
            OnPropertyChanged(nameof(CanPersist));
            OnPropertyChanged(nameof(CanOpenOrganization));
            OnPropertyChanged(nameof(CanManageOrganization));
            OnPropertyChanged(nameof(CanSaveDepartment));
            OnPropertyChanged(nameof(CanSavePosition));
            OnPropertyChanged(nameof(ShowOrganizationBusy));
            OnPropertyChanged(nameof(OrganizationBusyText));
            OnPropertyChanged(nameof(RefreshText));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(ToggleConfirmButtonText));
            OnPropertyChanged(nameof(ShowLoading));
        }
    }

    public string RefreshText => IsBusy ? "Đang cập nhật…" : "Cập nhật dữ liệu";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool ShowLoading => IsBusy && !_loaded;
    public bool IsEmpty => _loaded && VisibleEmployees.Count == 0;
    public string EmptyText => IsEmpty ? "Không tìm thấy hồ sơ nhân sự phù hợp." : string.Empty;

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

    public EmployeeStatusFilterOption SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (value is null || !SetField(ref _selectedStatusFilter, value)) return;
            ApplyFilter();
        }
    }

    public EmployeeBranchFilterOption SelectedBranchFilter
    {
        get => _selectedBranchFilter;
        set
        {
            if (value is null || !SetField(ref _selectedBranchFilter, value)) return;
            ApplyFilter();
        }
    }

    public string TotalEmployeesText => _employees.Count.ToString("N0");
    public string ActiveEmployeesText => _employees.Count(item => item.IsActive).ToString("N0");
    public string InactiveEmployeesHint => $"{_employees.Count(item => !item.IsActive):N0} hồ sơ đã ngừng làm việc";
    public string AssignedEmployeesText => _employees.Count(item => !string.IsNullOrWhiteSpace(item.BranchId)).ToString("N0");
    public string UnassignedEmployeesHint => $"{_employees.Count(item => string.IsNullOrWhiteSpace(item.BranchId)):N0} hồ sơ chưa gắn chi nhánh";
    public string VisibleEmployeeCountText => $"{VisibleEmployees.Count:N0} hồ sơ";

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        private set
        {
            if (!SetField(ref _isEditorOpen, value)) return;
            OnPropertyChanged(nameof(CanPersist));
            OnPropertyChanged(nameof(ShowHistory));
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
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CanPersist));
            OnPropertyChanged(nameof(ShowHistory));
        }
    }

    public string EditorKicker => IsCreateMode ? "HỒ SƠ MỚI" : "CẬP NHẬT HỒ SƠ";
    public string EditorTitle => IsCreateMode ? "Thêm nhân sự" : "Chỉnh sửa nhân sự";
    public bool IsCodeEditable => IsCreateMode;
    public string SaveButtonText => IsBusy ? "Đang lưu…" : IsCreateMode ? "Tạo hồ sơ" : "Lưu thay đổi";

    public string DraftCode
    {
        get => _draftCode;
        set
        {
            if (!SetField(ref _draftCode, NormalizeCode(value))) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string DraftFullName
    {
        get => _draftFullName;
        set
        {
            if (!SetField(ref _draftFullName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string DraftJobTitle
    {
        get => _draftJobTitle;
        set
        {
            if (!SetField(ref _draftJobTitle, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanPersist));
            OnPropertyChanged(nameof(LegacyJobTitleHint));
            OnPropertyChanged(nameof(HasLegacyJobTitleHint));
        }
    }

    public string DraftPhone
    {
        get => _draftPhone;
        set
        {
            if (!SetField(ref _draftPhone, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string DraftEmail
    {
        get => _draftEmail;
        set
        {
            if (!SetField(ref _draftEmail, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string DraftBranchId
    {
        get => _draftBranchId;
        set
        {
            if (!SetField(ref _draftBranchId, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

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

    public string ToggleTitle => _pendingToggleNextActive ? "Đưa trở lại làm việc" : "Ngừng làm việc";
    public string ToggleText => _pendingToggleNextActive
        ? "Hồ sơ sẽ được đưa trở lại trạng thái đang làm việc."
        : "Hồ sơ sẽ chuyển sang trạng thái ngừng làm việc nhưng vẫn được giữ lại để đối soát và liên kết lịch sử.";
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
        if (_loaded || !CanViewEmployees || !_access.Current.IsAuthenticated) return;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (!CanViewEmployees || !_access.Current.IsAuthenticated) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var accessGeneration = _accessGeneration;
        var loadGeneration = ++_loadGeneration;

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;

        IReadOnlyList<EmployeeDirectoryData>? employees = null;
        IReadOnlyList<EmployeeDirectoryBranchData>? branches = null;
        EmployeeOrganizationCatalogData? organization = null;
        var errors = new List<string>();

        try
        {
            try
            {
                employees = await _readService.ListEmployeesAsync(token).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                errors.Add(PublicError(exception, "Không tải được danh mục nhân sự."));
            }

            try
            {
                organization = await _readService.GetOrganizationAsync(token).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                errors.Add(PublicError(exception, "Không tải được cơ cấu tổ chức."));
            }

            if (CanReadBranches)
            {
                try
                {
                    branches = await _readService.ListBranchesAsync(token).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    errors.Add(PublicError(exception, "Không tải được danh mục chi nhánh."));
                }
            }
            else
            {
                errors.Add("Tài khoản chưa được cấp quyền xem danh mục chi nhánh.");
            }

            if (token.IsCancellationRequested
                || accessGeneration != _accessGeneration
                || loadGeneration != _loadGeneration)
                return;

            RunOnUiThread(() =>
            {
                if (employees is not null)
                {
                    _employees.Clear();
                    _employees.AddRange(employees);
                    _loaded = true;
                }

                if (branches is not null)
                {
                    _branches.Clear();
                    _branches.AddRange(branches);
                }

                if (organization is not null)
                    ApplyOrganization(organization);

                RebuildBranchOptions();
                RebuildEmployeeOrganizationOptions();
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

        _editingEmployee = null;
        _editorDetail = null;
        HasConflict = false;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        ClearWorkforceHistory();
        IsCreateMode = true;
        DraftCode = string.Empty;
        DraftFullName = string.Empty;
        DraftJobTitle = string.Empty;
        DraftPhone = string.Empty;
        DraftEmail = string.Empty;
        DraftBranchId = DraftBranchOptions.FirstOrDefault(option => option.Id.Length > 0)?.Id ?? string.Empty;
        PrepareCreateWorkforceHistory();
        PrepareCreateOrganizationAssignment();
        IsEditorOpen = true;
    }

    public async Task OpenEditAsync(EmployeeDirectoryData employee)
    {
        if (!CanOpenEditor) return;

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;
        try
        {
            var detail = await _readService.GetEmployeeAsync(employee.Id).ConfigureAwait(true);
            _editingEmployee = detail;
            _editorDetail = detail;
            HasConflict = false;
            EditorMessage = string.Empty;
            EditorMessageIsError = false;
            IsCreateMode = false;
            DraftCode = detail.Code;
            DraftFullName = detail.FullName;
            DraftJobTitle = detail.JobTitle ?? string.Empty;
            DraftPhone = detail.Phone ?? string.Empty;
            DraftEmail = detail.Email ?? string.Empty;
            DraftBranchId = detail.BranchId ?? string.Empty;
            PrepareEditWorkforceHistory(detail);
            PrepareEditOrganizationAssignment(detail);
            IsEditorOpen = true;
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(
                "Không tải được lịch sử hồ sơ nhân sự.",
                exception.RequestId), true);
        }
        catch (Exception)
        {
            SetMessage("Không tải được lịch sử hồ sơ nhân sự. Vui lòng thử lại.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CloseEditor()
    {
        IsEditorOpen = false;
        _editingEmployee = null;
        ClearOrganizationAssignment();
        HasConflict = false;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
    }

    public async Task SaveAsync()
    {
        if (!CanPersist) return;

        var fullName = DraftFullName.Trim();
        var jobTitle = Optional(DraftJobTitle);
        var phone = Optional(DraftPhone);
        var email = Optional(DraftEmail)?.ToLowerInvariant();
        var branchId = string.IsNullOrWhiteSpace(DraftBranchId) ? null : DraftBranchId.Trim();

        if (IsCreateMode)
        {
            var request = new EmployeeDirectoryCreateRequest(
                DraftCode.Trim(),
                fullName,
                jobTitle,
                phone,
                email,
                branchId,
                EmptyToNull(DraftDepartmentId),
                EmptyToNull(DraftPositionId),
                EmptyToNull(DraftManagerEmployeeId),
                CanonicalDate(DraftEmploymentStartDate),
                DraftEmploymentType,
                CanonicalDate(DraftAssignmentEffectiveFrom),
                Optional(DraftAssignmentReason));
            var slot = BuildCreateSlot(request);
            var key = MutationKey(slot, "employee-create");

            IsBusy = true;
            EditorMessage = string.Empty;
            EditorMessageIsError = false;
            try
            {
                var saved = await _mutationService.CreateAsync(request, key).ConfigureAwait(true);
                _mutationKeys.Remove(slot);
                UpsertEmployee(saved);
                CloseEditor();
                SearchTerm = string.Empty;
                SelectedStatusFilter = StatusFilters[0];
                SelectedBranchFilter = BranchFilters[0];
                SetMessage("Hồ sơ nhân sự đã được tạo.", false);
            }
            catch (CanonicalApiException exception)
            {
                SetEditorError(MutationError(exception, "Không tạo được hồ sơ nhân sự."));
            }
            catch (Exception)
            {
                SetEditorError("Không tạo được hồ sơ nhân sự. Vui lòng thử lại.");
            }
            finally
            {
                IsBusy = false;
            }
            return;
        }

        if (_editingEmployee is null) return;

        var assignmentChanged =
            !string.Equals(branchId, _editingEmployee.BranchId, StringComparison.Ordinal)
            || OrganizationAssignmentChanged(_editingEmployee);
        var includeAssignment = assignmentChanged || DraftConfirmAssignment;
        var update = new EmployeeDirectoryUpdateRequest(
            fullName,
            jobTitle,
            phone,
            email,
            branchId,
            _editingEmployee.UpdatedAt,
            EmptyToNull(DraftDepartmentId),
            EmptyToNull(DraftPositionId),
            EmptyToNull(DraftManagerEmployeeId),
            DraftConfirmEmployment ? true : null,
            DraftConfirmEmployment ? CanonicalDate(DraftEmploymentStartDate) : null,
            DraftConfirmEmployment ? CanonicalDate(DraftEmploymentEndDate) : null,
            DraftConfirmEmployment ? DraftEmploymentType : null,
            DraftConfirmEmployment ? Optional(DraftEmploymentEndReason) : null,
            DraftConfirmAssignment ? true : null,
            includeAssignment ? CanonicalDate(DraftAssignmentEffectiveFrom) : null,
            includeAssignment ? Optional(DraftAssignmentReason) : null);
        var updateSlot = BuildUpdateSlot(_editingEmployee.Id, update);
        var updateKey = MutationKey(updateSlot, "employee-update");

        IsBusy = true;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        try
        {
            var saved = await _mutationService.UpdateAsync(
                _editingEmployee.Id,
                update,
                updateKey).ConfigureAwait(true);
            _mutationKeys.Remove(updateSlot);
            UpsertEmployee(saved);
            CloseEditor();
            SetMessage("Thông tin nhân sự đã được cập nhật.", false);
        }
        catch (CanonicalApiException exception) when (IsOptimisticConflict(exception))
        {
            HasConflict = true;
            SetEditorError(CanonicalErrorMessages.WithRequestId(
                "Hồ sơ nhân sự vừa có thay đổi. Vui lòng tải lại dữ liệu trước khi lưu tiếp.",
                exception.RequestId));
        }
        catch (CanonicalApiException exception)
        {
            SetEditorError(MutationError(exception, "Không lưu được hồ sơ nhân sự."));
        }
        catch (Exception)
        {
            SetEditorError("Không lưu được hồ sơ nhân sự. Vui lòng thử lại.");
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
        SetMessage("Đã tải lại dữ liệu nhân sự. Vui lòng kiểm tra trước khi thực hiện lại.", false);
    }

    public async Task OpenToggleAsync(EmployeeDirectoryData employee)
    {
        if (!CanToggleEmployees) return;

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;
        try
        {
            var detail = await _readService.GetEmployeeAsync(employee.Id).ConfigureAwait(true);
            _pendingToggleEmployee = detail;
            _pendingToggleNextActive = !detail.IsActive;
            PrepareToggleWorkforceHistory(detail);
            ToggleMessage = string.Empty;
            IsToggleConfirmOpen = true;
            OnPropertyChanged(nameof(ToggleTitle));
            OnPropertyChanged(nameof(ToggleText));
            OnPropertyChanged(nameof(ShowToggleEmploymentType));
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(
                "Không tải được lịch sử lao động trước khi đổi trạng thái.",
                exception.RequestId), true);
        }
        catch (Exception)
        {
            SetMessage("Không tải được lịch sử lao động trước khi đổi trạng thái. Vui lòng thử lại.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CancelToggle()
    {
        IsToggleConfirmOpen = false;
        _pendingToggleEmployee = null;
        ToggleMessage = string.Empty;
        ClearToggleWorkforceHistory();
        OnPropertyChanged(nameof(ToggleTitle));
        OnPropertyChanged(nameof(ToggleText));
        OnPropertyChanged(nameof(ShowToggleEmploymentType));
    }

    public async Task ConfirmToggleAsync()
    {
        if (!CanConfirmToggle || _pendingToggleEmployee is null) return;

        var employee = _pendingToggleEmployee;
        var nextActive = _pendingToggleNextActive;
        var request = new EmployeeDirectoryToggleRequest(
            nextActive,
            employee.UpdatedAt,
            CanonicalDate(ToggleEffectiveDate),
            Optional(ToggleReason),
            nextActive ? ToggleEmploymentType : null);
        var slot = $"toggle|{employee.Id}|{nextActive}|{employee.UpdatedAt}|{request.EmploymentEffectiveDate}|{request.EmploymentReason}|{request.EmploymentType}";
        var key = MutationKey(slot, "employee-status");

        IsBusy = true;
        ToggleMessage = string.Empty;
        try
        {
            var saved = await _mutationService.ToggleAsync(
                employee.Id,
                request,
                key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            UpsertEmployee(saved);
            CancelToggle();
            SetMessage(
                nextActive
                    ? "Nhân sự đã được đưa trở lại làm việc."
                    : "Nhân sự đã ngừng làm việc.",
                false);
        }
        catch (CanonicalApiException exception) when (IsOptimisticConflict(exception))
        {
            ToggleMessage = CanonicalErrorMessages.WithRequestId(
                "Hồ sơ nhân sự vừa có thay đổi. Vui lòng hủy xác nhận và cập nhật dữ liệu trước khi thử lại.",
                exception.RequestId);
        }
        catch (CanonicalApiException exception)
        {
            ToggleMessage = MutationError(exception, "Không cập nhật được trạng thái nhân sự.");
        }
        catch (Exception)
        {
            ToggleMessage = "Không cập nhật được trạng thái nhân sự. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpsertEmployee(EmployeeDirectoryData saved)
    {
        var index = _employees.FindIndex(item => string.Equals(item.Id, saved.Id, StringComparison.Ordinal));
        if (index >= 0) _employees[index] = saved;
        else _employees.Add(saved);
        ApplyFilter();
        RaiseSummary();
    }

    private void ApplyFilter()
    {
        var search = EmployeeDirectoryPresentation.NormalizeSearch(SearchTerm);
        var branchMap = _branches.ToDictionary(branch => branch.Id, branch => branch, StringComparer.Ordinal);

        var rows = _employees
            .Where(employee =>
            {
                var matchesStatus = SelectedStatusFilter.Key switch
                {
                    "active" => employee.IsActive,
                    "inactive" => !employee.IsActive,
                    _ => true
                };
                if (!matchesStatus) return false;

                var matchesBranch = SelectedBranchFilter.Key switch
                {
                    "all" => true,
                    "unassigned" => string.IsNullOrWhiteSpace(employee.BranchId),
                    _ => string.Equals(employee.BranchId, SelectedBranchFilter.Key, StringComparison.Ordinal)
                };
                if (!matchesBranch) return false;

                if (string.IsNullOrWhiteSpace(search)) return true;

                branchMap.TryGetValue(employee.BranchId ?? string.Empty, out var branch);
                var assignment = employee.CurrentAssignment;
                var haystack = EmployeeDirectoryPresentation.NormalizeSearch(
                    $"{employee.Code} {employee.FullName} {assignment?.PositionName} {employee.JobTitle} {assignment?.DepartmentName} {assignment?.ManagerName} {employee.Phone} {employee.Email} {branch?.Code} {branch?.Name}");
                return haystack.Contains(search, StringComparison.Ordinal);
            })
            .OrderBy(employee => employee.Code, StringComparer.Ordinal)
            .Select(employee =>
            {
                branchMap.TryGetValue(employee.BranchId ?? string.Empty, out var branch);
                var branchText = string.IsNullOrWhiteSpace(employee.BranchId)
                    ? "Chưa phân công chi nhánh"
                    : branch is null
                        ? "Chưa tải được thông tin chi nhánh"
                        : EmployeeDirectoryPresentation.BranchLabel(branch);

                var assignment = employee.CurrentAssignment;
                var positionText = !string.IsNullOrWhiteSpace(assignment?.PositionName)
                    ? assignment.PositionName.Trim()
                    : !string.IsNullOrWhiteSpace(employee.JobTitle)
                        ? $"{employee.JobTitle.Trim()} · chức danh cũ"
                        : "Chưa phân công vị trí";
                var departmentText = string.IsNullOrWhiteSpace(assignment?.DepartmentName)
                    ? "Chưa phân công Phòng/Bộ phận"
                    : assignment.DepartmentName.Trim();
                var managerText = string.IsNullOrWhiteSpace(assignment?.ManagerName)
                    ? "Chưa có quản lý trực tiếp"
                    : $"Quản lý: {assignment.ManagerName.Trim()}";

                return new EmployeeDirectoryRowView(
                    employee,
                    employee.Code,
                    employee.FullName,
                    positionText,
                    departmentText,
                    branchText,
                    managerText,
                    string.IsNullOrWhiteSpace(employee.Phone) ? "Chưa có số điện thoại" : employee.Phone.Trim(),
                    string.IsNullOrWhiteSpace(employee.Email) ? "Chưa có email" : employee.Email.Trim(),
                    employee.IsActive ? "Đang làm việc" : "Ngừng làm việc",
                    employee.IsActive ? "Ngừng làm việc" : "Đưa trở lại làm việc",
                    employee.IsActive,
                    EmployeeDirectoryPresentation.DateTimeText(employee.UpdatedAt));
            })
            .ToArray();

        VisibleEmployees.Clear();
        foreach (var row in rows) VisibleEmployees.Add(row);

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(VisibleEmployeeCountText));
    }

    private void RebuildBranchOptions()
    {
        var selectedKey = SelectedBranchFilter.Key;
        BranchFilters.Clear();
        BranchFilters.Add(new EmployeeBranchFilterOption("all", "Tất cả chi nhánh"));
        BranchFilters.Add(new EmployeeBranchFilterOption("unassigned", "Chưa phân công"));
        foreach (var branch in _branches.OrderBy(branch => branch.Code, StringComparer.Ordinal))
            BranchFilters.Add(new EmployeeBranchFilterOption(branch.Id, EmployeeDirectoryPresentation.BranchLabel(branch)));

        _selectedBranchFilter = BranchFilters.FirstOrDefault(option => option.Key == selectedKey) ?? BranchFilters[0];
        OnPropertyChanged(nameof(SelectedBranchFilter));

        DraftBranchOptions.Clear();
        DraftBranchOptions.Add(new EmployeeBranchOption(string.Empty, "Chưa phân công"));
        foreach (var branch in _branches
                     .Where(branch => branch.IsActive)
                     .OrderBy(branch => branch.Code, StringComparer.Ordinal))
            DraftBranchOptions.Add(new EmployeeBranchOption(branch.Id, EmployeeDirectoryPresentation.BranchLabel(branch)));
    }

    private void ResetBranchOptions()
    {
        BranchFilters.Clear();
        BranchFilters.Add(new EmployeeBranchFilterOption("all", "Tất cả chi nhánh"));
        BranchFilters.Add(new EmployeeBranchFilterOption("unassigned", "Chưa phân công"));
        _selectedBranchFilter = BranchFilters[0];
        OnPropertyChanged(nameof(SelectedBranchFilter));

        DraftBranchOptions.Clear();
        DraftBranchOptions.Add(new EmployeeBranchOption(string.Empty, "Chưa phân công"));
    }

    private bool IsDraftValid()
    {
        var fullName = DraftFullName.Trim();
        if (fullName.Length is < 1 or > 256) return false;
        if (IsCreateMode && DraftCode.Trim().Length is < 1 or > 64) return false;
        if (DraftJobTitle.Trim().Length > 128) return false;

        var phone = DraftPhone.Trim();
        if (phone.Length > 0 && !Regex.IsMatch(phone, @"^[0-9\s\-+()]{5,20}$", RegexOptions.CultureInvariant))
            return false;

        var email = DraftEmail.Trim();
        if (email.Length > 256) return false;
        if (email.Length > 0 && !Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.CultureInvariant))
            return false;

        var branchValid = string.IsNullOrWhiteSpace(DraftBranchId)
            || DraftBranchOptions.Any(option => string.Equals(option.Id, DraftBranchId, StringComparison.Ordinal));
        return branchValid && OrganizationAssignmentDraftValid() && WorkforceHistoryDraftValid();
    }

    private string MutationKey(string slot, string scope)
    {
        if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;
        var created = _idempotencyKeys.Create(scope);
        if (!_idempotencyKeys.IsValid(created))
            throw new InvalidOperationException("Không tạo được khóa chống xử lý trùng hợp lệ.");
        _mutationKeys[slot] = created;
        return created;
    }

    private static string BuildCreateSlot(EmployeeDirectoryCreateRequest request) =>
        $"create|{request.Code}|{request.FullName}|{request.JobTitle}|{request.Phone}|{request.Email}|{request.BranchId}|{request.DepartmentId}|{request.PositionId}|{request.ManagerEmployeeId}|{request.EmploymentStartDate}|{request.EmploymentType}|{request.AssignmentEffectiveFrom}|{request.AssignmentReason}";

    private static string BuildUpdateSlot(string employeeId, EmployeeDirectoryUpdateRequest request) =>
        $"update|{employeeId}|{request.FullName}|{request.JobTitle}|{request.Phone}|{request.Email}|{request.BranchId}|{request.DepartmentId}|{request.PositionId}|{request.ManagerEmployeeId}|{request.ExpectedUpdatedAt}|{request.ConfirmEmployment}|{request.EmploymentEffectiveFrom}|{request.EmploymentEffectiveTo}|{request.EmploymentType}|{request.EmploymentEndReason}|{request.ConfirmAssignment}|{request.AssignmentEffectiveFrom}|{request.AssignmentReason}";

    private static bool IsOptimisticConflict(CanonicalApiException exception) =>
        exception.StatusCode == HttpStatusCode.Conflict
        && string.Equals(exception.Code, "CONFLICT", StringComparison.OrdinalIgnoreCase);

    private static string MutationError(CanonicalApiException exception, string fallback)
    {
        if (string.Equals(exception.Code, "SECURITY_OWNER_PROTECTED", StringComparison.OrdinalIgnoreCase))
        {
            return CanonicalErrorMessages.WithRequestId(
                "Hồ sơ nhân sự của Chủ sở hữu hệ thống đang được bảo vệ và chỉ tài khoản có thẩm quyền tương ứng mới được thay đổi.",
                exception.RequestId);
        }

        var message = CanonicalErrorMessages.ToOfficeMessage(exception);
        if (string.IsNullOrWhiteSpace(message)) message = fallback;
        return CanonicalErrorMessages.WithRequestId(message, exception.RequestId);
    }

    private static string? Optional(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? EmptyToNull(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length == 0 ? null : trimmed;
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

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(CanViewEmployees));
        OnPropertyChanged(nameof(CanWriteEmployees));
        OnPropertyChanged(nameof(CanReadBranches));
        OnPropertyChanged(nameof(CanOpenEditor));
        OnPropertyChanged(nameof(CanToggleEmployees));
        OnPropertyChanged(nameof(CanConfirmToggle));
        OnPropertyChanged(nameof(CanPersist));
        OnPropertyChanged(nameof(CanOpenOrganization));
        OnPropertyChanged(nameof(CanManageOrganization));
        OnPropertyChanged(nameof(CanSaveDepartment));
        OnPropertyChanged(nameof(CanSavePosition));
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(TotalEmployeesText));
        OnPropertyChanged(nameof(ActiveEmployeesText));
        OnPropertyChanged(nameof(InactiveEmployeesHint));
        OnPropertyChanged(nameof(AssignedEmployeesText));
        OnPropertyChanged(nameof(UnassignedEmployeesHint));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
    }

    private static string NormalizeCode(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return new string(value
            .ToUpperInvariant()
            .Where(character => char.IsLetterOrDigit(character) || character is '_' or '-')
            .Take(64)
            .ToArray());
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
