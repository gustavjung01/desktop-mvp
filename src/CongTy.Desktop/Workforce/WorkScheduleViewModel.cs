using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class WorkScheduleViewModel : INotifyPropertyChanged
{
    private const string ScheduleReadPermission = "core.work-schedule.read";
    private const string ScheduleManagePermission = "core.work-schedule.manage";

    private readonly IWorkScheduleService _service;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);
    private readonly List<EmployeeDirectoryData> _employees = [];
    private readonly List<WorkPolicyData> _policies = [];
    private readonly List<WorkScheduleData> _schedules = [];

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private WorkScheduleEmployeeOption? _selectedEmployeeFilter;

    private bool _isEditorOpen;
    private WorkScheduleData? _editingSchedule;
    private string _draftEmployeeId = string.Empty;
    private string _draftWorkPolicyId = string.Empty;
    private DateTime? _draftWorkDate;
    private string _draftScheduleKind = "WORK";
    private string _draftStartTime = "08:00";
    private string _draftEndTime = "17:00";
    private bool _draftEndsNextDay;
    private string _draftOverrideReason = string.Empty;
    private string _editorMessage = string.Empty;
    private bool _editorMessageIsError;

    public WorkScheduleViewModel(
        IWorkScheduleService service,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _service = service;
        _idempotencyKeys = idempotencyKeys;
        _access = access;

        EmployeeFilters.Add(new WorkScheduleEmployeeOption(string.Empty, "Tất cả nhân sự trong phạm vi"));
        SelectedEmployeeFilter = EmployeeFilters[0];
        FromDate = WorkSchedulePresentation.BusinessToday();
        ToDate = WorkSchedulePresentation.BusinessToday().AddDays(30);

        InitializePlanningDrafts();

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            _mutationKeys.Clear();
            _employees.Clear();
            _policies.Clear();
            _schedules.Clear();
            EmployeeFilters.Clear();
            EmployeeFilters.Add(new WorkScheduleEmployeeOption(string.Empty, "Tất cả nhân sự trong phạm vi"));
            SelectedEmployeeFilter = EmployeeFilters[0];
            PolicyOptions.Clear();
            VisibleSchedules.Clear();
            ResetPlanningCatalog();
            CloseEditor();
            Message = string.Empty;
            MessageIsError = false;
            RaiseAccess();
            RaiseSummary();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<WorkScheduleRowView> VisibleSchedules { get; } = [];
    public ObservableCollection<WorkScheduleEmployeeOption> EmployeeFilters { get; } = [];
    public ObservableCollection<WorkScheduleEmployeeOption> EmployeeEditorOptions { get; } = [];
    public ObservableCollection<WorkSchedulePolicyOption> PolicyOptions { get; } = [];

    public IReadOnlyList<WorkScheduleKindOption> ScheduleKinds { get; } =
    [
        new("WORK", "Ngày làm việc"),
        new("OFF", "Ngày nghỉ")
    ];

    public bool CanViewSchedules => _access.HasPermission(ScheduleReadPermission);
    public bool CanManageSchedules => _access.HasPermission(ScheduleManagePermission);
    public bool CanRefresh => CanViewSchedules && !IsBusy;
    public bool CanOpenEditor =>
        CanManageSchedules
        && !IsBusy
        && EmployeeEditorOptions.Count > 0
        && PolicyOptions.Count > 0;
    public bool CanSaveSchedule => IsEditorOpen && CanOpenEditor && ScheduleDraftValid();
    public bool ShowLoading => IsBusy && VisibleSchedules.Count == 0;
    public bool IsEmpty => !IsBusy && VisibleSchedules.Count == 0;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasEditorMessage => !string.IsNullOrWhiteSpace(EditorMessage);
    public bool IsEditingSchedule => _editingSchedule is not null;
    public bool CanChangeScheduleIdentity => _editingSchedule is null;
    public bool ShowWorkTimeFields => string.Equals(DraftScheduleKind, "WORK", StringComparison.Ordinal);
    public string ScheduleCountText => $"{VisibleSchedules.Count} dòng lịch";
    public string EditorTitle => IsEditingSchedule ? "Điều chỉnh lịch làm việc" : "Xếp lịch cho nhân sự";
    public string EditorKicker => IsEditingSchedule ? "ĐIỀU CHỈNH CÁ NHÂN" : "XẾP LỊCH CÁ NHÂN";
    public string SaveScheduleText => IsBusy ? "ĐANG LƯU…" : "LƯU LỊCH";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanRefresh));
            OnPropertyChanged(nameof(CanOpenEditor));
            OnPropertyChanged(nameof(CanSaveSchedule));
            OnPropertyChanged(nameof(ShowLoading));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SaveScheduleText));
            RaisePlanningCanExecute();
        }
    }

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

    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetField(ref _fromDate, value);
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => SetField(ref _toDate, value);
    }

    public WorkScheduleEmployeeOption? SelectedEmployeeFilter
    {
        get => _selectedEmployeeFilter;
        set => SetField(ref _selectedEmployeeFilter, value);
    }

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        private set
        {
            if (!SetField(ref _isEditorOpen, value)) return;
            OnPropertyChanged(nameof(CanSaveSchedule));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(EditorKicker));
        }
    }

    public string DraftEmployeeId
    {
        get => _draftEmployeeId;
        set
        {
            if (!SetField(ref _draftEmployeeId, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveSchedule));
        }
    }

    public string DraftWorkPolicyId
    {
        get => _draftWorkPolicyId;
        set
        {
            if (!SetField(ref _draftWorkPolicyId, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveSchedule));
        }
    }

    public DateTime? DraftWorkDate
    {
        get => _draftWorkDate;
        set
        {
            if (!SetField(ref _draftWorkDate, value)) return;
            OnPropertyChanged(nameof(CanSaveSchedule));
        }
    }

    public string DraftScheduleKind
    {
        get => _draftScheduleKind;
        set
        {
            if (!SetField(ref _draftScheduleKind, value ?? "WORK")) return;
            OnPropertyChanged(nameof(ShowWorkTimeFields));
            OnPropertyChanged(nameof(CanSaveSchedule));
        }
    }

    public string DraftStartTime
    {
        get => _draftStartTime;
        set
        {
            if (!SetField(ref _draftStartTime, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveSchedule));
        }
    }

    public string DraftEndTime
    {
        get => _draftEndTime;
        set
        {
            if (!SetField(ref _draftEndTime, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveSchedule));
        }
    }

    public bool DraftEndsNextDay
    {
        get => _draftEndsNextDay;
        set
        {
            if (!SetField(ref _draftEndsNextDay, value)) return;
            OnPropertyChanged(nameof(CanSaveSchedule));
        }
    }

    public string DraftOverrideReason
    {
        get => _draftOverrideReason;
        set
        {
            if (!SetField(ref _draftOverrideReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveSchedule));
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

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanViewSchedules) return;
        await ReloadAllAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync() =>
        await ReloadAllAsync().ConfigureAwait(true);

    public async Task ReloadSchedulesAsync()
    {
        if (!CanViewSchedules || IsBusy) return;
        if (!TryDateRange(out var from, out var to, out var validation))
        {
            SetMessage(validation, true);
            return;
        }

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var rows = await _service.ListSchedulesAsync(
                CanonicalDate(from),
                CanonicalDate(to),
                EmptyToNull(SelectedEmployeeFilter?.Id)).ConfigureAwait(true);
            _schedules.Clear();
            _schedules.AddRange(rows);
            RebuildSchedules();
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tải được lịch làm việc."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadAllAsync()
    {
        if (!CanViewSchedules || IsBusy) return;
        if (!TryDateRange(out var from, out var to, out var validation))
        {
            SetMessage(validation, true);
            return;
        }

        IsBusy = true;
        SetMessage(string.Empty, false);
        var errors = new List<string>();
        try
        {
            try
            {
                var employees = await _service.ListEmployeesAsync().ConfigureAwait(true);
                _employees.Clear();
                _employees.AddRange(employees.Where(item => item.IsActive));
                RebuildEmployeeOptions();
            }
            catch (Exception exception)
            {
                errors.Add(PublicError(exception, "Không tải được danh sách nhân sự."));
            }

            try
            {
                var policies = await _service.ListPoliciesAsync().ConfigureAwait(true);
                _policies.Clear();
                _policies.AddRange(policies);
                RebuildPolicyOptions();
            }
            catch (Exception exception)
            {
                errors.Add(PublicError(exception, "Không tải được chính sách làm việc."));
            }

            try
            {
                var catalog = await _service.GetPlanningCatalogAsync().ConfigureAwait(true);
                ApplyPlanningCatalog(catalog);
            }
            catch (Exception exception)
            {
                errors.Add(PublicError(exception, "Không tải được thiết lập xếp lịch."));
            }

            try
            {
                var schedules = await _service.ListSchedulesAsync(
                    CanonicalDate(from),
                    CanonicalDate(to),
                    EmptyToNull(SelectedEmployeeFilter?.Id)).ConfigureAwait(true);
                _schedules.Clear();
                _schedules.AddRange(schedules);
                RebuildSchedules();
            }
            catch (Exception exception)
            {
                errors.Add(PublicError(exception, "Không tải được lịch làm việc."));
            }

            _loaded = true;
            if (errors.Count > 0)
                SetMessage(string.Join(" ", errors.Distinct()), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OpenCreate()
    {
        if (!CanOpenEditor) return;
        _editingSchedule = null;
        DraftEmployeeId = EmployeeEditorOptions.FirstOrDefault()?.Id ?? string.Empty;
        DraftWorkPolicyId = PolicyOptions.FirstOrDefault()?.Id ?? string.Empty;
        DraftWorkDate = WorkSchedulePresentation.BusinessToday().AddDays(1);
        DraftScheduleKind = "WORK";
        DraftStartTime = "08:00";
        DraftEndTime = "17:00";
        DraftEndsNextDay = false;
        DraftOverrideReason = string.Empty;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        IsEditorOpen = true;
        OnPropertyChanged(nameof(IsEditingSchedule));
        OnPropertyChanged(nameof(CanChangeScheduleIdentity));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorKicker));
    }

    public void OpenEdit(WorkScheduleRowView row)
    {
        if (!CanManageSchedules || IsBusy || !row.CanEdit) return;

        var schedule = row.Source;
        _editingSchedule = schedule;
        DraftEmployeeId = schedule.EmployeeId;
        DraftWorkPolicyId = schedule.WorkPolicyId
            ?? PolicyOptions.FirstOrDefault()?.Id
            ?? string.Empty;
        DraftWorkDate = ParseDate(schedule.WorkDate) ?? WorkSchedulePresentation.BusinessToday().AddDays(1);
        DraftScheduleKind = schedule.ScheduleKind;

        var timezone = PolicyOptions.FirstOrDefault(item => item.Id == DraftWorkPolicyId)?.Timezone
            ?? schedule.PolicyTimezone
            ?? "Asia/Ho_Chi_Minh";
        if (DateTimeOffset.TryParse(schedule.ScheduledStartAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var start))
        {
            var local = WorkSchedulePresentation.ConvertToZone(start, timezone);
            DraftStartTime = local.ToString("HH:mm", CultureInfo.InvariantCulture);
        }
        else DraftStartTime = "08:00";

        if (DateTimeOffset.TryParse(schedule.ScheduledEndAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var end))
        {
            var endLocal = WorkSchedulePresentation.ConvertToZone(end, timezone);
            DraftEndTime = endLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
            if (DateTimeOffset.TryParse(schedule.ScheduledStartAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedStart))
                DraftEndsNextDay = endLocal.Date > WorkSchedulePresentation.ConvertToZone(parsedStart, timezone).Date;
        }
        else
        {
            DraftEndTime = "17:00";
            DraftEndsNextDay = false;
        }

        DraftOverrideReason = schedule.OverrideReason ?? string.Empty;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        IsEditorOpen = true;
        OnPropertyChanged(nameof(IsEditingSchedule));
        OnPropertyChanged(nameof(CanChangeScheduleIdentity));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorKicker));
    }

    public void CloseEditor()
    {
        _editingSchedule = null;
        IsEditorOpen = false;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        OnPropertyChanged(nameof(IsEditingSchedule));
        OnPropertyChanged(nameof(CanChangeScheduleIdentity));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorKicker));
    }

    public async Task SaveScheduleAsync()
    {
        if (!CanSaveSchedule || DraftWorkDate is null) return;

        var policy = _policies.FirstOrDefault(item => item.Id == DraftWorkPolicyId);
        if (policy is null)
        {
            SetEditorMessage("Vui lòng chọn chính sách làm việc.", true);
            return;
        }

        string? startAt = null;
        string? endAt = null;
        try
        {
            if (string.Equals(DraftScheduleKind, "WORK", StringComparison.Ordinal))
            {
                startAt = WorkSchedulePresentation.ToIso(DraftWorkDate.Value.Date, DraftStartTime, policy.Timezone, false);
                endAt = WorkSchedulePresentation.ToIso(DraftWorkDate.Value.Date, DraftEndTime, policy.Timezone, DraftEndsNextDay);
                if (DateTimeOffset.Parse(endAt, CultureInfo.InvariantCulture) <= DateTimeOffset.Parse(startAt, CultureInfo.InvariantCulture))
                {
                    SetEditorMessage("Giờ kết thúc phải sau giờ bắt đầu.", true);
                    return;
                }
            }
        }
        catch (Exception exception)
        {
            SetEditorMessage(exception.Message, true);
            return;
        }

        var request = new WorkScheduleMutationRequest(
            DraftEmployeeId.Trim(),
            DraftWorkPolicyId.Trim(),
            CanonicalDate(DraftWorkDate.Value),
            DraftScheduleKind,
            startAt,
            endAt,
            DraftOverrideReason.Trim(),
            _editingSchedule?.UpdatedAt);

        var slot = MutationSlot("schedule", request);
        var key = MutationKey(slot, "work-schedule-save");

        IsBusy = true;
        SetEditorMessage(string.Empty, false);
        try
        {
            await _service.SaveScheduleAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            CloseEditor();
            SetMessage("Lịch làm việc đã được cập nhật.", false);
            await ReloadSchedulesCoreAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetEditorMessage(PublicError(exception, "Không lưu được lịch làm việc."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadSchedulesCoreAsync()
    {
        if (!TryDateRange(out var from, out var to, out _)) return;
        var rows = await _service.ListSchedulesAsync(
            CanonicalDate(from),
            CanonicalDate(to),
            EmptyToNull(SelectedEmployeeFilter?.Id)).ConfigureAwait(true);
        _schedules.Clear();
        _schedules.AddRange(rows);
        RebuildSchedules();
    }

    private void RebuildEmployeeOptions()
    {
        var filterId = SelectedEmployeeFilter?.Id ?? string.Empty;

        EmployeeFilters.Clear();
        EmployeeFilters.Add(new WorkScheduleEmployeeOption(string.Empty, "Tất cả nhân sự trong phạm vi"));
        EmployeeEditorOptions.Clear();

        foreach (var employee in _employees.OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase))
        {
            var option = new WorkScheduleEmployeeOption(employee.Id, $"{employee.Code} · {employee.FullName}");
            EmployeeFilters.Add(option);
            EmployeeEditorOptions.Add(option);
        }

        SelectedEmployeeFilter = EmployeeFilters.FirstOrDefault(item => item.Id == filterId) ?? EmployeeFilters[0];
        OnPropertyChanged(nameof(CanOpenEditor));
        OnPropertyChanged(nameof(CanSaveSchedule));
        RebuildBulkEmployees();
    }

    private void RebuildPolicyOptions()
    {
        var selectedId = DraftWorkPolicyId;

        PolicyOptions.Clear();
        foreach (var policy in _policies
                     .Where(item => item.IsActive)
                     .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                     .ThenByDescending(item => item.Version))
        {
            PolicyOptions.Add(new WorkSchedulePolicyOption(
                policy.Id,
                $"{policy.Code} v{policy.Version} · {policy.Name}",
                string.IsNullOrWhiteSpace(policy.Timezone) ? "Asia/Ho_Chi_Minh" : policy.Timezone));
        }

        if (!string.IsNullOrWhiteSpace(selectedId) && PolicyOptions.Any(item => item.Id == selectedId))
            DraftWorkPolicyId = selectedId;
        else if (PolicyOptions.Count > 0)
            DraftWorkPolicyId = PolicyOptions[0].Id;

        OnPropertyChanged(nameof(CanOpenEditor));
        OnPropertyChanged(nameof(CanSaveSchedule));
    }

    private void RebuildSchedules()
    {
        var today = WorkSchedulePresentation.BusinessToday();

        VisibleSchedules.Clear();
        foreach (var schedule in _schedules
                     .OrderBy(item => item.WorkDate, StringComparer.Ordinal)
                     .ThenBy(item => item.EmployeeCode, StringComparer.OrdinalIgnoreCase))
        {
            var policyText = string.IsNullOrWhiteSpace(schedule.PolicyName)
                ? "Chưa xác định"
                : $"{schedule.PolicyCode ?? "—"} · {schedule.PolicyName}";
            var sourceText = string.Equals(schedule.Source, "OVERRIDE", StringComparison.OrdinalIgnoreCase)
                ? "Điều chỉnh riêng"
                : "Theo lịch nền";
            var date = ParseDate(schedule.WorkDate);
            VisibleSchedules.Add(new WorkScheduleRowView(
                schedule,
                WorkSchedulePresentation.DateText(schedule.WorkDate),
                $"{schedule.EmployeeCode} · {schedule.EmployeeName}",
                policyText,
                string.Equals(schedule.ScheduleKind, "WORK", StringComparison.OrdinalIgnoreCase) ? "Ngày làm việc" : "Ngày nghỉ",
                WorkSchedulePresentation.DateTimeText(schedule.ScheduledStartAt, schedule.PolicyTimezone),
                WorkSchedulePresentation.DateTimeText(schedule.ScheduledEndAt, schedule.PolicyTimezone),
                sourceText,
                string.IsNullOrWhiteSpace(schedule.OverrideReason) ? "—" : schedule.OverrideReason.Trim(),
                CanManageSchedules && date is not null && date.Value.Date > today));
        }

        RaiseSummary();
    }

    private bool ScheduleDraftValid()
    {
        if (DraftWorkDate is null || DraftWorkDate.Value.Date <= WorkSchedulePresentation.BusinessToday()) return false;
        if (string.IsNullOrWhiteSpace(DraftEmployeeId)
            || !EmployeeEditorOptions.Any(item => item.Id == DraftEmployeeId)) return false;
        if (string.IsNullOrWhiteSpace(DraftWorkPolicyId)
            || !PolicyOptions.Any(item => item.Id == DraftWorkPolicyId)) return false;
        if (!ScheduleKinds.Any(item => item.Key == DraftScheduleKind)) return false;
        if (string.IsNullOrWhiteSpace(DraftOverrideReason) || DraftOverrideReason.Trim().Length > 512) return false;
        if (string.Equals(DraftScheduleKind, "WORK", StringComparison.Ordinal))
        {
            if (!TimeOnly.TryParseExact(DraftStartTime.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return false;
            if (!TimeOnly.TryParseExact(DraftEndTime.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return false;
        }
        return true;
    }

    private bool TryDateRange(out DateTime from, out DateTime to, out string message)
    {
        from = FromDate?.Date ?? default;
        to = ToDate?.Date ?? default;
        message = string.Empty;
        if (FromDate is null || ToDate is null || to < from)
        {
            message = "Khoảng ngày tra cứu không hợp lệ.";
            return false;
        }
        if ((to - from).TotalDays > 93)
        {
            message = "Mỗi lần chỉ tra cứu tối đa 94 ngày.";
            return false;
        }
        return true;
    }

    private string MutationKey(string slot, string scope)
    {
        if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;
        var key = _idempotencyKeys.Create(scope);
        _mutationKeys[slot] = key;
        return key;
    }

    private static string MutationSlot<T>(string kind, T payload) =>
        $"{kind}|{JsonSerializer.Serialize(payload)}";

    private static string CanonicalDate(DateTime value) =>
        value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var candidate = value.Trim();
        if (candidate.Length >= 10) candidate = candidate[..10];
        return DateTime.TryParseExact(candidate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null;
    }

    private static string? EmptyToNull(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string PublicError(Exception exception, string fallback)
    {
        if (exception is CanonicalApiException canonical)
            return CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(canonical),
                canonical.RequestId);
        return exception is InvalidOperationException && !string.IsNullOrWhiteSpace(exception.Message)
            ? exception.Message
            : fallback;
    }

    private void SetMessage(string message, bool isError)
    {
        MessageIsError = isError;
        Message = message;
    }

    private void SetEditorMessage(string message, bool isError)
    {
        EditorMessageIsError = isError;
        EditorMessage = message;
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(CanViewSchedules));
        OnPropertyChanged(nameof(CanManageSchedules));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanOpenEditor));
        OnPropertyChanged(nameof(CanSaveSchedule));
        RaisePlanningCanExecute();
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(ScheduleCountText));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private static void RunOnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            dispatcher.Invoke(action);
        else
            action();
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
