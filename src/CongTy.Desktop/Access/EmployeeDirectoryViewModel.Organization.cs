using System.Collections.ObjectModel;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed partial class EmployeeDirectoryViewModel
{
    private EmployeeOrganizationCatalogData _organization = new();
    private bool _organizationLoaded;
    private string _draftDepartmentId = string.Empty;
    private string _draftPositionId = string.Empty;
    private string _draftManagerEmployeeId = string.Empty;
    private bool _isOrganizationOpen;
    private string _organizationMessage = string.Empty;
    private bool _organizationMessageIsError;
    private string _departmentDraftCode = string.Empty;
    private string _departmentDraftName = string.Empty;
    private string _departmentDraftParentId = string.Empty;
    private string _positionDraftCode = string.Empty;
    private string _positionDraftName = string.Empty;
    private string _positionDraftDepartmentId = string.Empty;

    public ObservableCollection<EmployeeDepartmentOption> DraftDepartmentOptions { get; } = [];
    public ObservableCollection<EmployeePositionOption> DraftPositionOptions { get; } = [];
    public ObservableCollection<EmployeeManagerOption> DraftManagerOptions { get; } = [];
    public ObservableCollection<EmployeeDepartmentOption> DepartmentParentOptions { get; } = [];
    public ObservableCollection<EmployeeDepartmentOption> PositionDepartmentOptions { get; } = [];
    public ObservableCollection<EmployeeDepartmentRowView> DepartmentRows { get; } = [];
    public ObservableCollection<EmployeePositionRowView> PositionRows { get; } = [];

    public bool OrganizationLoaded => _organizationLoaded;
    public bool CanOpenOrganization => CanViewEmployees && !IsBusy;
    public bool CanManageOrganization => CanWriteEmployees && !IsBusy;

    public string DraftDepartmentId
    {
        get => _draftDepartmentId;
        set
        {
            if (!SetField(ref _draftDepartmentId, value ?? string.Empty)) return;
            var selectedPosition = _organization.Positions.FirstOrDefault(item => item.Id == _draftPositionId);
            if (selectedPosition?.DepartmentId is { Length: > 0 } departmentId
                && !string.Equals(departmentId, _draftDepartmentId, StringComparison.Ordinal))
            {
                _draftPositionId = string.Empty;
                OnPropertyChanged(nameof(DraftPositionId));
            }
            RebuildPositionOptions();
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public string DraftPositionId
    {
        get => _draftPositionId;
        set
        {
            if (!SetField(ref _draftPositionId, value ?? string.Empty)) return;
            var selected = _organization.Positions.FirstOrDefault(item => item.Id == _draftPositionId);
            if (selected is not null)
            {
                if (!string.IsNullOrWhiteSpace(selected.DepartmentId)
                    && !string.Equals(_draftDepartmentId, selected.DepartmentId, StringComparison.Ordinal))
                {
                    _draftDepartmentId = selected.DepartmentId;
                    OnPropertyChanged(nameof(DraftDepartmentId));
                    RebuildPositionOptions();
                }
                DraftJobTitle = selected.Name;
            }
            OnPropertyChanged(nameof(CanPersist));
            OnPropertyChanged(nameof(LegacyJobTitleHint));
            OnPropertyChanged(nameof(HasLegacyJobTitleHint));
        }
    }

    public string LegacyJobTitleHint =>
        string.IsNullOrWhiteSpace(DraftPositionId) && !string.IsNullOrWhiteSpace(DraftJobTitle)
            ? $"Chức danh cũ: {DraftJobTitle.Trim()}. Hãy chọn Vị trí công việc để chuẩn hóa."
            : string.Empty;

    public bool HasLegacyJobTitleHint => !string.IsNullOrWhiteSpace(LegacyJobTitleHint);

    public string DraftManagerEmployeeId
    {
        get => _draftManagerEmployeeId;
        set
        {
            if (!SetField(ref _draftManagerEmployeeId, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanPersist));
        }
    }

    public bool IsOrganizationOpen
    {
        get => _isOrganizationOpen;
        private set => SetField(ref _isOrganizationOpen, value);
    }

    public string OrganizationMessage
    {
        get => _organizationMessage;
        private set
        {
            if (!SetField(ref _organizationMessage, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasOrganizationMessage));
        }
    }

    public bool OrganizationMessageIsError
    {
        get => _organizationMessageIsError;
        private set => SetField(ref _organizationMessageIsError, value);
    }

    public bool HasOrganizationMessage => !string.IsNullOrWhiteSpace(OrganizationMessage);

    public string DepartmentDraftCode
    {
        get => _departmentDraftCode;
        set
        {
            if (!SetField(ref _departmentDraftCode, NormalizeCode(value))) return;
            OnPropertyChanged(nameof(CanSaveDepartment));
        }
    }

    public string DepartmentDraftName
    {
        get => _departmentDraftName;
        set
        {
            if (!SetField(ref _departmentDraftName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveDepartment));
        }
    }

    public string DepartmentDraftParentId
    {
        get => _departmentDraftParentId;
        set => SetField(ref _departmentDraftParentId, value ?? string.Empty);
    }

    public string PositionDraftCode
    {
        get => _positionDraftCode;
        set
        {
            if (!SetField(ref _positionDraftCode, NormalizeCode(value))) return;
            OnPropertyChanged(nameof(CanSavePosition));
        }
    }

    public string PositionDraftName
    {
        get => _positionDraftName;
        set
        {
            if (!SetField(ref _positionDraftName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSavePosition));
        }
    }

    public string PositionDraftDepartmentId
    {
        get => _positionDraftDepartmentId;
        set => SetField(ref _positionDraftDepartmentId, value ?? string.Empty);
    }

    public bool CanSaveDepartment =>
        CanManageOrganization
        && DepartmentDraftCode.Length is >= 1 and <= 64
        && DepartmentDraftName.Trim().Length is >= 1 and <= 256;

    public bool CanSavePosition =>
        CanManageOrganization
        && PositionDraftCode.Length is >= 1 and <= 64
        && PositionDraftName.Trim().Length is >= 1 and <= 256;

    public async Task OpenOrganizationAsync()
    {
        if (!CanOpenOrganization) return;
        ResetOrganizationDrafts();
        IsOrganizationOpen = true;
        OrganizationMessage = string.Empty;
        OrganizationMessageIsError = false;
        await ReloadOrganizationAsync().ConfigureAwait(true);
    }

    public void CloseOrganization()
    {
        IsOrganizationOpen = false;
        OrganizationMessage = string.Empty;
        OrganizationMessageIsError = false;
        ResetOrganizationDrafts();
    }

    public async Task SaveDepartmentAsync()
    {
        if (!CanSaveDepartment) return;
        var request = new EmployeeOrganizationMutationRequest(
            "DEPARTMENT",
            DepartmentDraftCode.Trim(),
            DepartmentDraftName.Trim(),
            ParentDepartmentId: EmptyToNull(DepartmentDraftParentId));
        var slot = OrganizationMutationSlot(request);
        var key = MutationKey(slot, "employee-organization-save");

        IsBusy = true;
        OrganizationMessage = string.Empty;
        OrganizationMessageIsError = false;
        try
        {
            await _mutationService.SaveOrganizationAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            DepartmentDraftCode = string.Empty;
            DepartmentDraftName = string.Empty;
            DepartmentDraftParentId = string.Empty;
            await ReloadOrganizationCoreAsync().ConfigureAwait(true);
            SetOrganizationMessage("Đã thêm Phòng/Bộ phận.", false);
        }
        catch (CanonicalApiException exception)
        {
            SetOrganizationMessage(MutationError(exception, "Không thêm được Phòng/Bộ phận."), true);
        }
        catch (Exception)
        {
            SetOrganizationMessage("Không thêm được Phòng/Bộ phận. Vui lòng thử lại.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SavePositionAsync()
    {
        if (!CanSavePosition) return;
        var request = new EmployeeOrganizationMutationRequest(
            "POSITION",
            PositionDraftCode.Trim(),
            PositionDraftName.Trim(),
            DepartmentId: EmptyToNull(PositionDraftDepartmentId));
        var slot = OrganizationMutationSlot(request);
        var key = MutationKey(slot, "employee-organization-save");

        IsBusy = true;
        OrganizationMessage = string.Empty;
        OrganizationMessageIsError = false;
        try
        {
            await _mutationService.SaveOrganizationAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            PositionDraftCode = string.Empty;
            PositionDraftName = string.Empty;
            PositionDraftDepartmentId = string.Empty;
            await ReloadOrganizationCoreAsync().ConfigureAwait(true);
            SetOrganizationMessage("Đã thêm Vị trí công việc.", false);
        }
        catch (CanonicalApiException exception)
        {
            SetOrganizationMessage(MutationError(exception, "Không thêm được Vị trí công việc."), true);
        }
        catch (Exception)
        {
            SetOrganizationMessage("Không thêm được Vị trí công việc. Vui lòng thử lại.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ToggleDepartmentAsync(EmployeeDepartmentRowView row)
    {
        if (!CanManageOrganization) return;
        var item = row.Source;
        var request = new EmployeeOrganizationMutationRequest(
            "DEPARTMENT",
            item.Code,
            item.Name,
            item.Id,
            item.ParentDepartmentId,
            IsActive: !item.IsActive,
            ExpectedUpdatedAt: item.UpdatedAt);
        await SaveOrganizationToggleAsync(
            request,
            item.IsActive ? "Đã ngừng sử dụng Phòng/Bộ phận." : "Đã đưa Phòng/Bộ phận vào sử dụng.",
            "Không cập nhật được Phòng/Bộ phận.").ConfigureAwait(true);
    }

    public async Task TogglePositionAsync(EmployeePositionRowView row)
    {
        if (!CanManageOrganization) return;
        var item = row.Source;
        var request = new EmployeeOrganizationMutationRequest(
            "POSITION",
            item.Code,
            item.Name,
            item.Id,
            DepartmentId: item.DepartmentId,
            IsActive: !item.IsActive,
            ExpectedUpdatedAt: item.UpdatedAt);
        await SaveOrganizationToggleAsync(
            request,
            item.IsActive ? "Đã ngừng sử dụng Vị trí công việc." : "Đã đưa Vị trí công việc vào sử dụng.",
            "Không cập nhật được Vị trí công việc.").ConfigureAwait(true);
    }

    private async Task SaveOrganizationToggleAsync(
        EmployeeOrganizationMutationRequest request,
        string successMessage,
        string fallback)
    {
        var slot = OrganizationMutationSlot(request);
        var key = MutationKey(slot, "employee-organization-save");
        IsBusy = true;
        OrganizationMessage = string.Empty;
        OrganizationMessageIsError = false;
        try
        {
            await _mutationService.SaveOrganizationAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            await ReloadOrganizationCoreAsync().ConfigureAwait(true);
            SetOrganizationMessage(successMessage, false);
        }
        catch (CanonicalApiException exception)
        {
            SetOrganizationMessage(MutationError(exception, fallback), true);
        }
        catch (Exception)
        {
            SetOrganizationMessage($"{fallback} Vui lòng thử lại.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadOrganizationAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await ReloadOrganizationCoreAsync().ConfigureAwait(true);
        }
        catch (CanonicalApiException exception)
        {
            SetOrganizationMessage(CanonicalErrorMessages.WithRequestId(
                "Không tải được cơ cấu tổ chức.",
                exception.RequestId), true);
        }
        catch (Exception)
        {
            SetOrganizationMessage("Không tải được cơ cấu tổ chức. Vui lòng thử lại.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadOrganizationCoreAsync()
    {
        var organization = await _readService.GetOrganizationAsync().ConfigureAwait(true);
        ApplyOrganization(organization);
        RebuildEmployeeOrganizationOptions();
        ApplyFilter();
    }

    private void ApplyOrganization(EmployeeOrganizationCatalogData organization)
    {
        _organization = organization ?? new EmployeeOrganizationCatalogData();
        _organizationLoaded = true;
        OnPropertyChanged(nameof(OrganizationLoaded));
        OnPropertyChanged(nameof(CanOpenEditor));
        OnPropertyChanged(nameof(CanPersist));

        DepartmentRows.Clear();
        foreach (var item in _organization.Departments
                     .OrderByDescending(item => item.IsActive)
                     .ThenBy(item => item.Code, StringComparer.Ordinal))
        {
            DepartmentRows.Add(new EmployeeDepartmentRowView(
                item,
                item.Code,
                item.Name,
                string.IsNullOrWhiteSpace(item.ParentName) ? "—" : item.ParentName.Trim(),
                item.IsActive ? "Đang sử dụng" : "Ngừng sử dụng",
                item.IsActive ? "Ngừng sử dụng" : "Dùng lại"));
        }

        PositionRows.Clear();
        foreach (var item in _organization.Positions
                     .OrderByDescending(item => item.IsActive)
                     .ThenBy(item => item.Code, StringComparer.Ordinal))
        {
            PositionRows.Add(new EmployeePositionRowView(
                item,
                item.Code,
                item.Name,
                string.IsNullOrWhiteSpace(item.DepartmentName) ? "Dùng chung" : item.DepartmentName.Trim(),
                item.IsActive ? "Đang sử dụng" : "Ngừng sử dụng",
                item.IsActive ? "Ngừng sử dụng" : "Dùng lại"));
        }

        DepartmentParentOptions.Clear();
        DepartmentParentOptions.Add(new EmployeeDepartmentOption(string.Empty, "Không có"));
        PositionDepartmentOptions.Clear();
        PositionDepartmentOptions.Add(new EmployeeDepartmentOption(string.Empty, "Dùng chung"));
        foreach (var item in _organization.Departments.Where(item => item.IsActive).OrderBy(item => item.Code, StringComparer.Ordinal))
        {
            var option = new EmployeeDepartmentOption(item.Id, $"{item.Code} · {item.Name}");
            DepartmentParentOptions.Add(option);
            PositionDepartmentOptions.Add(option);
        }
    }

    private void RebuildEmployeeOrganizationOptions()
    {
        var currentDepartment = _draftDepartmentId;
        DraftDepartmentOptions.Clear();
        DraftDepartmentOptions.Add(new EmployeeDepartmentOption(string.Empty, "Chưa phân công"));
        foreach (var item in _organization.Departments.Where(item => item.IsActive).OrderBy(item => item.Code, StringComparer.Ordinal))
            DraftDepartmentOptions.Add(new EmployeeDepartmentOption(item.Id, $"{item.Code} · {item.Name}"));

        if (!string.IsNullOrWhiteSpace(currentDepartment)
            && !DraftDepartmentOptions.Any(item => item.Id == currentDepartment))
        {
            var legacy = _organization.Departments.FirstOrDefault(item => item.Id == currentDepartment);
            if (legacy is not null)
                DraftDepartmentOptions.Add(new EmployeeDepartmentOption(legacy.Id, $"{legacy.Code} · {legacy.Name} · Ngừng sử dụng"));
        }

        RebuildPositionOptions();

        DraftManagerOptions.Clear();
        DraftManagerOptions.Add(new EmployeeManagerOption(string.Empty, "Chưa phân công"));
        foreach (var item in _organization.Managers
                     .Where(item => !string.Equals(item.Id, _editingEmployee?.Id, StringComparison.Ordinal))
                     .OrderBy(item => item.Code, StringComparer.Ordinal))
        {
            var suffix = string.IsNullOrWhiteSpace(item.PositionName) ? string.Empty : $" · {item.PositionName}";
            if (!item.IsActive) suffix += " · Đã nghỉ";
            DraftManagerOptions.Add(new EmployeeManagerOption(item.Id, $"{item.Code} · {item.FullName}{suffix}"));
        }

        OnPropertyChanged(nameof(DraftDepartmentId));
        OnPropertyChanged(nameof(DraftPositionId));
        OnPropertyChanged(nameof(DraftManagerEmployeeId));
    }

    private void RebuildPositionOptions()
    {
        var currentPosition = _draftPositionId;
        DraftPositionOptions.Clear();
        DraftPositionOptions.Add(new EmployeePositionOption(string.Empty, "Chưa phân công", null, string.Empty));

        foreach (var item in _organization.Positions
                     .Where(item => item.IsActive
                         && (string.IsNullOrWhiteSpace(_draftDepartmentId)
                             || string.IsNullOrWhiteSpace(item.DepartmentId)
                             || string.Equals(item.DepartmentId, _draftDepartmentId, StringComparison.Ordinal)))
                     .OrderBy(item => item.Code, StringComparer.Ordinal))
        {
            DraftPositionOptions.Add(new EmployeePositionOption(
                item.Id,
                $"{item.Code} · {item.Name}",
                item.DepartmentId,
                item.Name));
        }

        if (!string.IsNullOrWhiteSpace(currentPosition)
            && !DraftPositionOptions.Any(item => item.Id == currentPosition))
        {
            var legacy = _organization.Positions.FirstOrDefault(item => item.Id == currentPosition);
            if (legacy is not null)
            {
                DraftPositionOptions.Add(new EmployeePositionOption(
                    legacy.Id,
                    $"{legacy.Code} · {legacy.Name} · Ngừng sử dụng",
                    legacy.DepartmentId,
                    legacy.Name));
            }
        }
    }

    private void PrepareCreateOrganizationAssignment()
    {
        DraftDepartmentId = string.Empty;
        DraftPositionId = string.Empty;
        DraftManagerEmployeeId = string.Empty;
        RebuildEmployeeOrganizationOptions();
    }

    private void PrepareEditOrganizationAssignment(EmployeeDirectoryData detail)
    {
        var assignment = detail.CurrentAssignment ?? detail.AssignmentHistory.FirstOrDefault();
        _draftDepartmentId = assignment?.DepartmentId ?? string.Empty;
        _draftPositionId = assignment?.PositionId ?? string.Empty;
        _draftManagerEmployeeId = assignment?.ManagerEmployeeId ?? string.Empty;
        RebuildEmployeeOrganizationOptions();
    }

    private void ClearOrganizationAssignment()
    {
        _draftDepartmentId = string.Empty;
        _draftPositionId = string.Empty;
        _draftManagerEmployeeId = string.Empty;
        DraftDepartmentOptions.Clear();
        DraftPositionOptions.Clear();
        DraftManagerOptions.Clear();
    }

    private bool OrganizationAssignmentChanged(EmployeeDirectoryData employee)
    {
        var assignment = employee.CurrentAssignment ?? employee.AssignmentHistory.FirstOrDefault();
        return !string.Equals(EmptyToNull(DraftDepartmentId), assignment?.DepartmentId, StringComparison.Ordinal)
            || !string.Equals(EmptyToNull(DraftPositionId), assignment?.PositionId, StringComparison.Ordinal)
            || !string.Equals(EmptyToNull(DraftManagerEmployeeId), assignment?.ManagerEmployeeId, StringComparison.Ordinal);
    }

    private bool OrganizationAssignmentDraftValid()
    {
        var current = _editingEmployee?.CurrentAssignment ?? _editingEmployee?.AssignmentHistory.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(DraftDepartmentId))
        {
            var department = _organization.Departments.FirstOrDefault(item => item.Id == DraftDepartmentId);
            if (department is null) return false;
            if (!department.IsActive && !string.Equals(current?.DepartmentId, department.Id, StringComparison.Ordinal))
                return false;
        }

        HrPositionData? position = null;
        if (!string.IsNullOrWhiteSpace(DraftPositionId))
        {
            position = _organization.Positions.FirstOrDefault(item => item.Id == DraftPositionId);
            if (position is null) return false;
            if (!position.IsActive && !string.Equals(current?.PositionId, position.Id, StringComparison.Ordinal))
                return false;
            if (!string.IsNullOrWhiteSpace(position.DepartmentId)
                && !string.Equals(position.DepartmentId, EmptyToNull(DraftDepartmentId), StringComparison.Ordinal))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(DraftManagerEmployeeId))
        {
            if (string.Equals(DraftManagerEmployeeId, _editingEmployee?.Id, StringComparison.Ordinal)) return false;
            if (!_organization.Managers.Any(item => item.Id == DraftManagerEmployeeId)) return false;
        }

        return true;
    }

    private void ResetOrganization()
    {
        _organization = new EmployeeOrganizationCatalogData();
        _organizationLoaded = false;
        OnPropertyChanged(nameof(OrganizationLoaded));
        OnPropertyChanged(nameof(CanOpenEditor));
        OnPropertyChanged(nameof(CanPersist));
        DepartmentRows.Clear();
        PositionRows.Clear();
        DepartmentParentOptions.Clear();
        PositionDepartmentOptions.Clear();
        ClearOrganizationAssignment();
        ResetOrganizationDrafts();
        IsOrganizationOpen = false;
        OrganizationMessage = string.Empty;
        OrganizationMessageIsError = false;
    }

    private void ResetOrganizationDrafts()
    {
        DepartmentDraftCode = string.Empty;
        DepartmentDraftName = string.Empty;
        DepartmentDraftParentId = string.Empty;
        PositionDraftCode = string.Empty;
        PositionDraftName = string.Empty;
        PositionDraftDepartmentId = string.Empty;
    }

    private void SetOrganizationMessage(string message, bool isError)
    {
        OrganizationMessageIsError = isError;
        OrganizationMessage = message;
    }

    private static string OrganizationMutationSlot(EmployeeOrganizationMutationRequest request) =>
        $"organization|{request.Resource}|{request.Id}|{request.Code}|{request.Name}|{request.ParentDepartmentId}|{request.DepartmentId}|{request.IsActive}|{request.ExpectedUpdatedAt}";
}
