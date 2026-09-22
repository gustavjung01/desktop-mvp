using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class OvertimeCloseoutViewModel : INotifyPropertyChanged
{
    private const string OvertimeSelfRequestPermission = "core.overtime.self-request";
    private const string OvertimeReadPermission = "core.overtime.read";
    private const string OvertimeApprovePermission = "core.overtime.approve";
    private const string OvertimeConfirmPermission = "core.overtime.confirm";
    private const string AttendanceReadPermission = "core.attendance.read";
    private const string AttendanceSelfReadPermission = "core.attendance.self.read";
    private const string AttendanceReconcilePermission = "core.attendance.reconcile";
    private const string AttendanceLockPermission = "core.attendance.lock";

    private readonly IOvertimeCloseoutService _service;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);
    private readonly List<OvertimeRequestData> _overtime = [];
    private readonly List<AttendancePeriodData> _periods = [];

    private OvertimeCapabilitiesData _overtimeCapabilities = new();
    private AttendancePeriodCapabilitiesData _periodCapabilities = new();
    private OvertimePaginationData _overtimePagination = new();

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private OvertimeOption? _selectedStatus;
    private OvertimeOption? _selectedBranch;

    private DateTime? _requestWorkDate;
    private string _requestHours = "1";
    private string _requestReason = string.Empty;

    private OvertimeRowView? _selectedOvertime;
    private string _actionHours = string.Empty;
    private string _actionNote = string.Empty;

    private AttendancePeriodRowView? _selectedPeriod;
    private string _periodNote = string.Empty;
    private bool _acknowledgeWarnings;

    private AttendancePayrollInputData? _payrollInput;
    private string _payrollTitle = "Chọn kỳ đã chốt để xem đầu vào tính lương.";

    public OvertimeCloseoutViewModel(
        IOvertimeCloseoutService service,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _service = service;
        _idempotencyKeys = idempotencyKeys;
        _access = access;

        StatusOptions =
        [
            new OvertimeOption(string.Empty, "Tất cả"),
            new OvertimeOption("SUBMITTED", "Chờ duyệt"),
            new OvertimeOption("APPROVED", "Đã duyệt"),
            new OvertimeOption("REJECTED", "Từ chối"),
            new OvertimeOption("ACTUAL_RECORDED", "Đã ghi nhận thực tế"),
            new OvertimeOption("CONFIRMED", "Đã xác nhận giờ tính")
        ];
        BranchOptions.Add(new OvertimeOption(string.Empty, "Toàn bộ phạm vi được cấp"));
        SelectedStatus = StatusOptions[0];
        SelectedBranch = BranchOptions[0];

        var today = WorkSchedulePresentation.BusinessToday();
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = FromDate.Value.AddMonths(1).AddDays(-1);
        RequestWorkDate = today;

        _access.Changed += (_, _) => RunOnUiThread(ResetForAccessChange);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<OvertimeRowView> OvertimeRows { get; } = [];
    public ObservableCollection<AttendancePeriodRowView> PeriodRows { get; } = [];
    public ObservableCollection<PayrollInputRowView> PayrollRows { get; } = [];
    public ObservableCollection<OvertimeOption> BranchOptions { get; } = [];

    public IReadOnlyList<OvertimeOption> StatusOptions { get; }

    public bool HasOvertimeReadAccess =>
        _access.HasPermission(OvertimeReadPermission)
        || _access.HasPermission(OvertimeApprovePermission)
        || _access.HasPermission(OvertimeConfirmPermission)
        || _access.HasPermission(OvertimeSelfRequestPermission)
        || _access.HasPermission(AttendanceSelfReadPermission);

    public bool HasPeriodReadAccess =>
        _access.HasPermission(AttendanceReconcilePermission)
        || _access.HasPermission(AttendanceLockPermission)
        || _access.HasPermission(AttendanceReadPermission);

    public bool CanViewWorkspace => HasOvertimeReadAccess || HasPeriodReadAccess;

    public bool CanSubmitOwn =>
        _overtimeCapabilities.CanSubmitOwn
        && _access.HasPermission(OvertimeSelfRequestPermission);

    public bool CanApprove =>
        _overtimeCapabilities.CanApprove
        && _access.HasPermission(OvertimeApprovePermission);

    public bool CanConfirm =>
        _overtimeCapabilities.CanConfirm
        && _access.HasPermission(OvertimeConfirmPermission);

    public bool CanReconcile =>
        _periodCapabilities.CanReconcile
        && _access.HasPermission(AttendanceReconcilePermission);

    public bool CanClose =>
        _periodCapabilities.CanClose
        && _access.HasPermission(AttendanceLockPermission);

    public bool ShowBranchFilter => BranchOptions.Count > 1;
    public bool CanRefresh => CanViewWorkspace && !IsBusy;
    public bool ShowLoading => IsBusy && OvertimeRows.Count == 0 && PeriodRows.Count == 0;
    public bool IsOvertimeEmpty => !IsBusy && OvertimeRows.Count == 0;
    public bool IsPeriodEmpty => !IsBusy && PeriodRows.Count == 0;
    public bool IsPayrollEmpty => !IsBusy && PayrollRows.Count == 0;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool IsOvertimeActionOpen => SelectedOvertime is not null;
    public bool IsPeriodActionOpen => SelectedPeriod is not null;
    public bool HasPayrollInput => _payrollInput is not null;

    public string OvertimeCountText => $"{_overtimePagination.Total} hồ sơ";
    public string PendingCountText => $"{_overtime.Count(item => item.Status == "SUBMITTED")} chờ duyệt";
    public string ConfirmedHoursText =>
        OvertimeCloseoutPresentation.HoursText(_overtime.Where(item => item.Status == "CONFIRMED").Sum(item => item.ConfirmedMinutes ?? 0));
    public string ClosedPeriodCountText => $"{_periods.Count(item => item.Status == "CLOSED")} kỳ đã chốt";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaiseAvailability();
            RaiseEmptyStates();
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

    public OvertimeOption? SelectedStatus
    {
        get => _selectedStatus;
        set => SetField(ref _selectedStatus, value);
    }

    public OvertimeOption? SelectedBranch
    {
        get => _selectedBranch;
        set => SetField(ref _selectedBranch, value);
    }

    public DateTime? RequestWorkDate
    {
        get => _requestWorkDate;
        set
        {
            if (!SetField(ref _requestWorkDate, value)) return;
            OnPropertyChanged(nameof(CanSubmitRequest));
        }
    }

    public string RequestHours
    {
        get => _requestHours;
        set
        {
            if (!SetField(ref _requestHours, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitRequest));
        }
    }

    public string RequestReason
    {
        get => _requestReason;
        set
        {
            if (!SetField(ref _requestReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitRequest));
        }
    }

    public bool CanSubmitRequest =>
        CanSubmitOwn
        && !IsBusy
        && RequestWorkDate is not null
        && TryHours(RequestHours, allowZero: false, out _)
        && !string.IsNullOrWhiteSpace(RequestReason);

    public OvertimeRowView? SelectedOvertime
    {
        get => _selectedOvertime;
        private set
        {
            if (!SetField(ref _selectedOvertime, value)) return;
            OnPropertyChanged(nameof(IsOvertimeActionOpen));
            OnPropertyChanged(nameof(CanReviewSelected));
            OnPropertyChanged(nameof(CanRecordActualSelected));
            OnPropertyChanged(nameof(CanConfirmSelected));
            OnPropertyChanged(nameof(ActionHoursLabel));
        }
    }

    public string ActionHours
    {
        get => _actionHours;
        set => SetField(ref _actionHours, value ?? string.Empty);
    }

    public string ActionNote
    {
        get => _actionNote;
        set => SetField(ref _actionNote, value ?? string.Empty);
    }

    public bool CanReviewSelected =>
        SelectedOvertime?.Source.Status == "SUBMITTED" && CanApprove && !IsBusy;

    public bool CanRecordActualSelected =>
        SelectedOvertime?.Source.Status == "APPROVED" && CanApprove && !IsBusy;

    public bool CanConfirmSelected =>
        SelectedOvertime?.Source.Status == "ACTUAL_RECORDED" && CanConfirm && !IsBusy;

    public string ActionHoursLabel =>
        SelectedOvertime?.Source.Status == "ACTUAL_RECORDED" ? "Số giờ được tính" : "Số giờ thực tế";

    public AttendancePeriodRowView? SelectedPeriod
    {
        get => _selectedPeriod;
        private set
        {
            if (!SetField(ref _selectedPeriod, value)) return;
            OnPropertyChanged(nameof(IsPeriodActionOpen));
            OnPropertyChanged(nameof(CanReconcileSelected));
            OnPropertyChanged(nameof(CanCloseSelected));
            OnPropertyChanged(nameof(CanViewSelectedPayroll));
        }
    }

    public string PeriodNote
    {
        get => _periodNote;
        set => SetField(ref _periodNote, value ?? string.Empty);
    }

    public bool AcknowledgeWarnings
    {
        get => _acknowledgeWarnings;
        set => SetField(ref _acknowledgeWarnings, value);
    }

    public bool CanReconcileSelected =>
        SelectedPeriod is not null
        && SelectedPeriod.Source.Status != "CLOSED"
        && CanReconcile
        && !IsBusy;

    public bool CanCloseSelected =>
        SelectedPeriod?.Source.Status == "RECONCILED"
        && CanClose
        && !IsBusy;

    public bool CanViewSelectedPayroll =>
        SelectedPeriod?.Source.Status == "CLOSED"
        && HasPeriodReadAccess
        && !IsBusy;

    public string PayrollTitle
    {
        get => _payrollTitle;
        private set => SetField(ref _payrollTitle, value);
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanViewWorkspace) return;
        await ReloadAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync() => await ReloadAsync().ConfigureAwait(true);

    private async Task ReloadAsync()
    {
        if (!CanViewWorkspace || IsBusy) return;
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
            if (HasOvertimeReadAccess)
            {
                try
                {
                    var overtime = await _service.ListOvertimeAsync(
                        CanonicalDate(from),
                        CanonicalDate(to),
                        SelectedStatus?.Value,
                        null,
                        null,
                        SelectedBranch?.Value,
                        100,
                        0).ConfigureAwait(true);
                    ApplyOvertime(overtime);
                }
                catch (Exception exception)
                {
                    errors.Add(PublicError(exception, "Không tải được hồ sơ tăng ca."));
                }
            }
            else
            {
                _overtime.Clear();
                OvertimeRows.Clear();
                _overtimeCapabilities = new OvertimeCapabilitiesData();
            }

            if (HasPeriodReadAccess)
            {
                try
                {
                    var periods = await _service.ListAttendancePeriodsAsync(
                        CanonicalDate(from),
                        CanonicalDate(to),
                        SelectedBranch?.Value).ConfigureAwait(true);
                    ApplyPeriods(periods);
                }
                catch (Exception exception)
                {
                    errors.Add(PublicError(exception, "Không tải được kỳ công."));
                }
            }
            else
            {
                _periods.Clear();
                PeriodRows.Clear();
                _periodCapabilities = new AttendancePeriodCapabilitiesData();
            }

            _loaded = true;
            if (errors.Count > 0)
                SetMessage(string.Join(" ", errors.Distinct()), true);
        }
        finally
        {
            IsBusy = false;
            RaiseAccess();
            RaiseSummary();
            RaiseEmptyStates();
        }
    }

    private void ApplyOvertime(OvertimeListResponseData response)
    {
        _overtimeCapabilities = response.Capabilities ?? new OvertimeCapabilitiesData();
        _overtimePagination = response.Pagination ?? new OvertimePaginationData();
        _overtime.Clear();
        _overtime.AddRange(response.Requests ?? []);
        MergeBranches(response.Branches ?? []);
        RebuildOvertimeRows();
    }

    private void ApplyPeriods(AttendancePeriodListResponseData response)
    {
        _periodCapabilities = response.Capabilities ?? new AttendancePeriodCapabilitiesData();
        _periods.Clear();
        _periods.AddRange(response.Periods ?? []);
        MergeBranches(response.Branches ?? []);
        RebuildPeriodRows();

        if (SelectedPeriod is not null)
            SelectedPeriod = PeriodRows.FirstOrDefault(item => item.Source.Id == SelectedPeriod.Source.Id);
    }

    private void MergeBranches(IEnumerable<LeaveBranchData> branches)
    {
        var selected = SelectedBranch?.Value ?? string.Empty;
        var all = BranchOptions.Skip(1).ToDictionary(item => item.Value, item => item, StringComparer.Ordinal);
        foreach (var branch in branches)
            all[branch.Id] = new OvertimeOption(branch.Id, $"{branch.Code} · {branch.Name}");

        BranchOptions.Clear();
        BranchOptions.Add(new OvertimeOption(string.Empty, "Toàn bộ phạm vi được cấp"));
        foreach (var option in all.Values.OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase))
            BranchOptions.Add(option);
        SelectedBranch = BranchOptions.FirstOrDefault(item => item.Value == selected) ?? BranchOptions[0];
        OnPropertyChanged(nameof(ShowBranchFilter));
    }

    private void RebuildOvertimeRows()
    {
        OvertimeRows.Clear();
        foreach (var item in _overtime)
        {
            var employeeText = string.IsNullOrWhiteSpace(item.EmployeeCode) && string.IsNullOrWhiteSpace(item.EmployeeName)
                ? "Bản thân"
                : $"{item.EmployeeCode ?? "—"} · {item.EmployeeName ?? "Chưa xác định"}";
            var canProcess = (item.Status == "SUBMITTED" && CanApprove)
                || (item.Status == "APPROVED" && CanApprove)
                || (item.Status == "ACTUAL_RECORDED" && CanConfirm);
            var note = item.ReviewReason ?? item.ActualNote ?? item.ConfirmNote ?? "—";

            OvertimeRows.Add(new OvertimeRowView(
                item,
                OvertimeCloseoutPresentation.DateText(item.WorkDate),
                employeeText,
                item.BranchName ?? "—",
                OvertimeCloseoutPresentation.HoursText(item.RequestedMinutes),
                OvertimeCloseoutPresentation.HoursText(item.ActualMinutes),
                OvertimeCloseoutPresentation.HoursText(item.ConfirmedMinutes),
                OvertimeCloseoutPresentation.OvertimeStatusLabel(item.Status),
                note,
                canProcess));
        }

        if (SelectedOvertime is not null)
            SelectedOvertime = OvertimeRows.FirstOrDefault(item => item.Source.Id == SelectedOvertime.Source.Id);
    }

    private void RebuildPeriodRows()
    {
        PeriodRows.Clear();
        foreach (var item in _periods.OrderByDescending(item => item.PeriodStart, StringComparer.Ordinal))
        {
            PeriodRows.Add(new AttendancePeriodRowView(
                item,
                OvertimeCloseoutPresentation.PeriodText(item),
                OvertimeCloseoutPresentation.ScopeText(item),
                OvertimeCloseoutPresentation.PeriodStatusLabel(item.Status),
                OvertimeCloseoutPresentation.DecimalText(OvertimeCloseoutPresentation.BlockerTotal(item.IssueSummary)),
                OvertimeCloseoutPresentation.DecimalText(OvertimeCloseoutPresentation.WarningTotal(item.IssueSummary)),
                item.Revision > 0 ? item.Revision.ToString(CultureInfo.InvariantCulture) : "—",
                item.Status == "CLOSED"));
        }
    }

    private void ApplyPayroll(AttendancePayrollInputData input)
    {
        _payrollInput = input;
        PayrollRows.Clear();
        foreach (var item in input.PayrollInput.Employees ?? [])
        {
            var warning = item.ConfigurationIssueDays + item.IncompleteDays + item.ViolationDays
                + item.PendingAdjustmentDays + item.PendingLeaveDays + item.UnexcusedAbsenceDays;
            PayrollRows.Add(new PayrollInputRowView(
                item,
                $"{item.EmployeeCode} · {item.EmployeeName}",
                item.BranchName ?? "—",
                OvertimeCloseoutPresentation.DecimalText(item.WorkDays),
                OvertimeCloseoutPresentation.HoursText(item.CountedMinutes),
                OvertimeCloseoutPresentation.HoursText(item.LeaveCreditedMinutes),
                OvertimeCloseoutPresentation.HoursText(item.ConfirmedOvertimeMinutes),
                warning > 0 ? OvertimeCloseoutPresentation.DecimalText(warning) : "0"));
        }

        PayrollTitle = $"Bản chốt kỳ công lần {input.Revision} · Chỉ đọc";
        OnPropertyChanged(nameof(HasPayrollInput));
        RaiseEmptyStates();
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
        if ((to - from).TotalDays > 365)
        {
            message = "Mỗi lần chỉ xem tối đa 366 ngày.";
            return false;
        }
        return true;
    }

    private static bool TryHours(string value, bool allowZero, out int minutes)
    {
        minutes = 0;
        var candidate = value?.Trim() ?? string.Empty;
        if (!decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var hours)
            && !decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.InvariantCulture, out hours))
            return false;
        if (hours < 0 || (!allowZero && hours <= 0) || hours > 24) return false;
        var calculated = hours * 60m;
        if (calculated != decimal.Truncate(calculated)) return false;
        minutes = (int)calculated;
        return minutes >= (allowZero ? 0 : 1) && minutes <= 1440;
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

    private static string? EmptyToNull(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
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

    private void ResetForAccessChange()
    {
        _loaded = false;
        _mutationKeys.Clear();
        _overtime.Clear();
        _periods.Clear();
        _overtimeCapabilities = new OvertimeCapabilitiesData();
        _periodCapabilities = new AttendancePeriodCapabilitiesData();
        _overtimePagination = new OvertimePaginationData();
        OvertimeRows.Clear();
        PeriodRows.Clear();
        PayrollRows.Clear();
        BranchOptions.Clear();
        BranchOptions.Add(new OvertimeOption(string.Empty, "Toàn bộ phạm vi được cấp"));
        SelectedBranch = BranchOptions[0];
        SelectedOvertime = null;
        SelectedPeriod = null;
        _payrollInput = null;
        PayrollTitle = "Chọn kỳ đã chốt để xem đầu vào tính lương.";
        Message = string.Empty;
        MessageIsError = false;
        RaiseAccess();
        RaiseSummary();
        RaiseEmptyStates();
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(HasOvertimeReadAccess));
        OnPropertyChanged(nameof(HasPeriodReadAccess));
        OnPropertyChanged(nameof(CanViewWorkspace));
        OnPropertyChanged(nameof(CanSubmitOwn));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanConfirm));
        OnPropertyChanged(nameof(CanReconcile));
        OnPropertyChanged(nameof(CanClose));
        OnPropertyChanged(nameof(CanSubmitRequest));
        OnPropertyChanged(nameof(CanReviewSelected));
        OnPropertyChanged(nameof(CanRecordActualSelected));
        OnPropertyChanged(nameof(CanConfirmSelected));
        OnPropertyChanged(nameof(CanReconcileSelected));
        OnPropertyChanged(nameof(CanCloseSelected));
        OnPropertyChanged(nameof(CanViewSelectedPayroll));
        RebuildOvertimeRows();
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(OvertimeCountText));
        OnPropertyChanged(nameof(PendingCountText));
        OnPropertyChanged(nameof(ConfirmedHoursText));
        OnPropertyChanged(nameof(ClosedPeriodCountText));
    }

    private void RaiseAvailability()
    {
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanSubmitRequest));
        OnPropertyChanged(nameof(CanReviewSelected));
        OnPropertyChanged(nameof(CanRecordActualSelected));
        OnPropertyChanged(nameof(CanConfirmSelected));
        OnPropertyChanged(nameof(CanReconcileSelected));
        OnPropertyChanged(nameof(CanCloseSelected));
        OnPropertyChanged(nameof(CanViewSelectedPayroll));
    }

    private void RaiseEmptyStates()
    {
        OnPropertyChanged(nameof(ShowLoading));
        OnPropertyChanged(nameof(IsOvertimeEmpty));
        OnPropertyChanged(nameof(IsPeriodEmpty));
        OnPropertyChanged(nameof(IsPayrollEmpty));
    }

    private void SetMessage(string message, bool isError)
    {
        MessageIsError = isError;
        Message = message;
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
