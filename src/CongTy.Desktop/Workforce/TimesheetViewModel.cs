using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed class TimesheetViewModel : INotifyPropertyChanged
{
    private const string SelfReadPermission = "core.attendance.self.read";
    private const string ReadPermission = "core.attendance.read";

    private readonly ITimesheetService _service;
    private readonly IAccessStateService _access;

    private AttendanceTimesheetResponseData? _data;
    private bool _loaded;
    private bool _isBusy;
    private string _errorMessage = string.Empty;
    private bool _isMonthlyView;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private DateTime? _monthDate;
    private string _employeeQuery = string.Empty;
    private TimesheetBranchOption? _selectedBranch;
    private TimesheetEmployeeRowView? _selectedEmployee;
    private AttendanceTimesheetDayData? _selectedDay;

    public TimesheetViewModel(
        ITimesheetService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;

        var today = WorkSchedulePresentation.BusinessToday();
        _fromDate = new DateTime(today.Year, today.Month, 1);
        _toDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        _monthDate = _fromDate;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            _data = null;
            EmployeeRows.Clear();
            BranchOptions.Clear();
            EmployeeDayRows.Clear();
            DayEvents.Clear();
            SelectedEmployee = null;
            SelectedDay = null;
            ErrorMessage = string.Empty;
            RaiseAll();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TimesheetEmployeeRowView> EmployeeRows { get; } = [];
    public ObservableCollection<TimesheetBranchOption> BranchOptions { get; } = [];
    public ObservableCollection<TimesheetEmployeeDayRowView> EmployeeDayRows { get; } = [];
    public ObservableCollection<TimesheetEventRowView> DayEvents { get; } = [];

    public bool CanView =>
        _access.HasPermission(ReadPermission)
        || _access.HasPermission(SelfReadPermission);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanLoad));
            OnPropertyChanged(nameof(CanPagePrevious));
            OnPropertyChanged(nameof(CanPageNext));
            OnPropertyChanged(nameof(ShowLoading));
        }
    }

    public bool CanLoad => CanView && !IsBusy;
    public bool ShowLoading => IsBusy && !_loaded;

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

    public bool IsMonthlyView
    {
        get => _isMonthlyView;
        private set
        {
            if (!SetField(ref _isMonthlyView, value)) return;
            OnPropertyChanged(nameof(IsDailyView));
            OnPropertyChanged(nameof(DailyViewLabel));
            OnPropertyChanged(nameof(MonthlyViewLabel));
            OnPropertyChanged(nameof(ShowDailyFilters));
            OnPropertyChanged(nameof(ShowMonthlyFilter));
            OnPropertyChanged(nameof(ShowDailyTable));
            OnPropertyChanged(nameof(ShowMonthlyTable));
        }
    }

    public bool IsDailyView => !IsMonthlyView;
    public string DailyViewLabel => IsDailyView ? "✓ Theo ngày" : "Theo ngày";
    public string MonthlyViewLabel => IsMonthlyView ? "✓ Theo tháng" : "Theo tháng";
    public bool ShowDailyFilters => IsDailyView;
    public bool ShowMonthlyFilter => IsMonthlyView;
    public bool ShowDailyTable => IsDailyView;
    public bool ShowMonthlyTable => IsMonthlyView;

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

    public DateTime? MonthDate
    {
        get => _monthDate;
        set
        {
            if (!SetField(ref _monthDate, value)) return;
            OnPropertyChanged(nameof(MonthText));
        }
    }

    public string MonthText
    {
        get => MonthDate?.ToString("yyyy-MM", CultureInfo.InvariantCulture) ?? string.Empty;
        set
        {
            var normalized = (value ?? string.Empty).Trim();
            if (DateTime.TryParseExact(
                    normalized + "-01",
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                MonthDate = parsed;
                return;
            }

            if (normalized.Length == 7)
                _monthDate = null;
            OnPropertyChanged();
        }
    }

    public string EmployeeQuery
    {
        get => _employeeQuery;
        set => SetField(ref _employeeQuery, value ?? string.Empty);
    }

    public TimesheetBranchOption? SelectedBranch
    {
        get => _selectedBranch;
        set => SetField(ref _selectedBranch, value);
    }

    public TimesheetEmployeeRowView? SelectedEmployee
    {
        get => _selectedEmployee;
        private set
        {
            if (!SetField(ref _selectedEmployee, value)) return;
            OnPropertyChanged(nameof(HasSelectedEmployee));
            RaiseEmployeeDetail();
        }
    }

    public bool HasSelectedEmployee => SelectedEmployee is not null;

    public AttendanceTimesheetDayData? SelectedDay
    {
        get => _selectedDay;
        private set
        {
            if (!SetField(ref _selectedDay, value)) return;
            OnPropertyChanged(nameof(HasSelectedDay));
            RaiseDayDetail();
        }
    }

    public bool HasSelectedDay => SelectedDay is not null;

    public bool CanFilterScoped => _data is not null && !_data.Scope.SelfOnly;
    public bool HasBranchFilter => CanFilterScoped && BranchOptions.Count > 1;

    public string PeriodText => _data is null ? "—" : TimesheetPresentation.PeriodText(_data.Period);
    public string ScopeText => _data is null ? "—" : TimesheetPresentation.ScopeText(_data.Scope);
    public string TotalEmployeesText => (_data?.Pagination.Total ?? 0).ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    public string PaginationText
    {
        get
        {
            if (_data is null || _data.Pagination.Total <= 0) return "0 / 0";
            var start = _data.Pagination.Offset + 1;
            var end = Math.Min(_data.Pagination.Offset + EmployeeRows.Count, _data.Pagination.Total);
            return $"{start}–{end} / {_data.Pagination.Total}";
        }
    }

    public bool CanPagePrevious => !IsBusy && (_data?.Pagination.HasPrevious ?? false);
    public bool CanPageNext => !IsBusy && (_data?.Pagination.HasNext ?? false);
    public bool NoRows => _loaded && EmployeeRows.Count == 0;

    public string EmployeeDetailTitle => SelectedEmployee?.EmployeeText ?? string.Empty;
    public string EmployeeDetailSubtitle => SelectedEmployee is null
        ? string.Empty
        : $"{SelectedEmployee.BranchText} · {TimesheetPresentation.PeriodText(SelectedEmployee.Source.Period)}";
    public string EmployeeWorkDaysText => SelectedEmployee?.WorkDaysText ?? "0";
    public string EmployeeCompletedDaysText => SelectedEmployee?.CompletedDaysText ?? "0";
    public string EmployeeLeaveText => SelectedEmployee?.LeaveText ?? "—";
    public string EmployeeCountedText => SelectedEmployee?.CountedTimeText ?? "0 phút";

    public string DayEmployeeText => SelectedDay is null
        ? string.Empty
        : $"{SelectedDay.Employee.Code} · {SelectedDay.Employee.Name}";
    public string DayDateText => SelectedDay is null ? "—" : WorkSchedulePresentation.DateText(SelectedDay.WorkDate);
    public string DayStatusText => SelectedDay is null ? "—" : TimesheetPresentation.StatusLabel(SelectedDay);
    public string DayCheckInText => SelectedDay is null ? "—" : TimesheetPresentation.Clock(SelectedDay.CheckInAt, SelectedDay.Policy?.Timezone);
    public string DayCheckOutText => SelectedDay is null ? "—" : TimesheetPresentation.Clock(SelectedDay.CheckOutAt, SelectedDay.Policy?.Timezone);
    public string DayActualText => SelectedDay is null ? "0 phút" : TimesheetPresentation.Minutes(SelectedDay.ActualMinutes);
    public string DayCountedText => SelectedDay is null ? "0 phút" : TimesheetPresentation.Minutes(SelectedDay.CountedMinutes);
    public string DayLeaveCreditedText => SelectedDay is null ? "0 phút" : TimesheetPresentation.Minutes(SelectedDay.LeaveCreditedMinutes);
    public string DayRequiredWorkText => SelectedDay is null ? "—" : TimesheetPresentation.RequiredWork(SelectedDay);
    public string DayLateText => SelectedDay is null ? "0 phút" : TimesheetPresentation.Minutes(SelectedDay.LateMinutes);
    public string DayEarlyText => SelectedDay is null ? "0 phút" : TimesheetPresentation.Minutes(SelectedDay.EarlyLeaveMinutes);
    public string DayScheduleText => SelectedDay is null
        ? "—"
        : SelectedDay.ScheduledWorkDay
            ? $"{TimesheetPresentation.Clock(SelectedDay.ExpectedStartAt, SelectedDay.Policy?.Timezone)} – {TimesheetPresentation.Clock(SelectedDay.ExpectedEndAt, SelectedDay.Policy?.Timezone)}"
            : "Không phải làm";
    public string DayLeaveSegmentText => SelectedDay is null ? "Không có" : TimesheetPresentation.LeaveSegments(SelectedDay);
    public string DayLeaveSummaryText => SelectedDay is null ? "Không có đơn nghỉ" : TimesheetPresentation.LeaveSummary(SelectedDay);
    public string DayAbsenceText => SelectedDay is not null && SelectedDay.UnexcusedAbsenceFraction > 0
        ? $"Vắng không phép: {TimesheetPresentation.DayCount(SelectedDay.UnexcusedAbsenceFraction)} ngày"
        : "Không ghi nhận vắng không phép";
    public string DayConfigurationText => SelectedDay?.ConfigurationIssue switch
    {
        "MISSING_POLICY" => "Nhân sự chưa có Chính sách làm việc hiệu lực tại ngày này.",
        "MISSING_SCHEDULE" => "Cần bổ sung lịch làm việc hoặc ca làm việc.",
        _ => "Thiết lập lịch và chính sách hợp lệ.",
    };
    public string DaySourceText => SelectedDay is null ? "—" : TimesheetPresentation.SourceSummary(SelectedDay);
    public string DayPolicyText => SelectedDay?.Policy is null
        ? "Chưa có chính sách phù hợp"
        : $"Chính sách: {SelectedDay.Policy.Code} · {SelectedDay.Policy.Name} · lần cập nhật {SelectedDay.Policy.Version}";
    public string DayAdjustmentText => SelectedDay?.Adjustment?.Status switch
    {
        "SUBMITTED" => "Điều chỉnh: Chờ duyệt",
        "APPROVED" => "Điều chỉnh: Đã duyệt",
        "REJECTED" => "Điều chỉnh: Từ chối",
        _ => "Chưa có điều chỉnh",
    };
    public string DayLockText => SelectedDay?.PeriodLock is null ? "Kỳ công đang mở" : "Đã khóa kỳ công";
    public string DayViolationExplanation => SelectedDay?.ViolationEvaluation.Explanation ?? "Chưa đánh giá";
    public string DayViolationItemsText => SelectedDay is null || SelectedDay.ViolationEvaluation.Items.Length == 0
        ? "Không có mục vi phạm."
        : string.Join(Environment.NewLine, SelectedDay.ViolationEvaluation.Items.Select(item => $"• {item.Label}: {item.Detail}"));
    public string DayPolicyThresholdText => SelectedDay?.Policy is null
        ? "Không có ngưỡng chính sách."
        : $"Ngưỡng chính sách: trễ {SelectedDay.Policy.LateGraceMinutes} phút · về sớm {SelectedDay.Policy.EarlyLeaveGraceMinutes} phút.";
    public bool NoDayEvents => SelectedDay is not null && DayEvents.Count == 0;

    public bool CanOpenViolationHandling =>
        SelectedDay?.ViolationEvaluation.Items.Length > 0;

    public bool CanOpenAdjustmentForDay =>
        SelectedDay is not null
        && _data is not null
        && (
            (_data.Capabilities.CanManage
             && (SelectedDay.PeriodLock is null || _data.Capabilities.CanLock))
            || (!_data.Capabilities.CanManage
                && _data.Capabilities.CanSubmitOwn
                && SelectedDay.PeriodLock is null)
        );

    public string AdjustmentActionText =>
        _data?.Capabilities.CanManage == true ? "ĐIỀU CHỈNH CÔNG" : "YÊU CẦU ĐIỀU CHỈNH";

    public string? AdjustmentTargetEmployeeId =>
        _data?.Capabilities.CanManage == true ? SelectedDay?.Employee.Id : null;

    public string? AdjustmentTargetWorkDate => SelectedDay?.WorkDate;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanView) return;
        await LoadAsync(0).ConfigureAwait(true);
    }

    public Task RefreshAsync() =>
        LoadAsync(_data?.Pagination.Offset ?? 0);

    public async Task SwitchDailyAsync()
    {
        if (IsDailyView || IsBusy) return;
        IsMonthlyView = false;
        await LoadAsync(0).ConfigureAwait(true);
    }

    public async Task SwitchMonthlyAsync()
    {
        if (IsMonthlyView || IsBusy) return;
        IsMonthlyView = true;
        if (FromDate is { } from)
            MonthDate = new DateTime(from.Year, from.Month, 1);
        await LoadAsync(0).ConfigureAwait(true);
    }

    public Task ApplyFiltersAsync() => LoadAsync(0);

    public Task PreviousPageAsync()
    {
        if (_data is null || !CanPagePrevious) return Task.CompletedTask;
        return LoadAsync(Math.Max(0, _data.Pagination.Offset - _data.Pagination.Limit));
    }

    public Task NextPageAsync()
    {
        if (_data is null || !CanPageNext) return Task.CompletedTask;
        return LoadAsync(_data.Pagination.Offset + _data.Pagination.Limit);
    }

    public void OpenEmployee(TimesheetEmployeeRowView? row)
    {
        if (row is null) return;
        SelectedEmployee = row;
        SelectedDay = null;
        EmployeeDayRows.Clear();
        foreach (var day in row.Source.Days.OrderBy(item => item.WorkDate, StringComparer.Ordinal))
            EmployeeDayRows.Add(ToDayRow(day));
        OnPropertyChanged(nameof(EmployeeDayRows));
    }

    public void CloseEmployee()
    {
        SelectedDay = null;
        SelectedEmployee = null;
        EmployeeDayRows.Clear();
    }

    public void OpenDay(AttendanceTimesheetDayData? day)
    {
        if (day is null) return;
        SelectedDay = day;
        DayEvents.Clear();
        foreach (var item in day.Events.OrderBy(item => item.OccurredAt, StringComparer.Ordinal))
        {
            DayEvents.Add(new TimesheetEventRowView(
                item,
                TimesheetPresentation.EventLabel(item),
                TimesheetPresentation.SourceLabel(item.Source),
                string.IsNullOrWhiteSpace(item.PointName)
                    ? item.Source == "MANUAL"
                        ? "Chấm công trực tiếp"
                        : item.Source == "FACE"
                            ? "Máy chấm công khuôn mặt"
                            : "Không ghi nhận nơi chấm công"
                    : item.PointName!,
                TimesheetPresentation.ValidationLabel(item.ValidationStatus),
                TimesheetPresentation.DateTimeText(item.OccurredAt, day.Policy?.Timezone),
                string.IsNullOrWhiteSpace(item.Note) ? "—" : item.Note!.Trim()));
        }
        OnPropertyChanged(nameof(NoDayEvents));
    }

    public void CloseDay()
    {
        SelectedDay = null;
        DayEvents.Clear();
        OnPropertyChanged(nameof(NoDayEvents));
    }

    private async Task LoadAsync(int offset)
    {
        if (!CanView || IsBusy) return;
        if (!TryResolvePeriod(out var from, out var to, out var validation))
        {
            ErrorMessage = validation;
            return;
        }

        var query = EmployeeQuery.Trim();
        if (query.Length > 80)
        {
            ErrorMessage = "Từ khóa nhân sự tối đa 80 ký tự.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var view = IsMonthlyView ? "monthly" : "employee";
            var limit = IsMonthlyView ? 20 : 100;
            var data = await _service.ListAsync(
                view,
                from,
                to,
                query,
                SelectedBranch?.Id,
                limit,
                Math.Max(0, offset)).ConfigureAwait(true);

            ApplyData(data);
            _loaded = true;
        }
        catch (Exception exception)
        {
            ErrorMessage = PublicError(exception, "Không tải được bảng công.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyData(AttendanceTimesheetResponseData data)
    {
        _data = data;

        if (DateTime.TryParseExact(data.Period.From, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
            FromDate = from;
        if (DateTime.TryParseExact(data.Period.To, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
            ToDate = to;
        if (DateTime.TryParseExact(data.Period.From, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
            MonthDate = new DateTime(month.Year, month.Month, 1);

        var selectedBranchId = SelectedBranch?.Id;
        BranchOptions.Clear();
        BranchOptions.Add(new TimesheetBranchOption(string.Empty, "Tất cả chi nhánh được cấp"));
        foreach (var branch in data.Scope.Branches.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
            BranchOptions.Add(new TimesheetBranchOption(branch.Id, $"{branch.Code} · {branch.Name}"));

        SelectedBranch = data.Scope.SelfOnly
            ? BranchOptions[0]
            : BranchOptions.FirstOrDefault(item => string.Equals(item.Id, selectedBranchId, StringComparison.Ordinal))
              ?? BranchOptions[0];

        EmployeeRows.Clear();
        foreach (var item in data.Rows)
            EmployeeRows.Add(ToEmployeeRow(item));

        CloseEmployee();

        OnPropertyChanged(nameof(CanFilterScoped));
        OnPropertyChanged(nameof(HasBranchFilter));
        OnPropertyChanged(nameof(PeriodText));
        OnPropertyChanged(nameof(ScopeText));
        OnPropertyChanged(nameof(TotalEmployeesText));
        OnPropertyChanged(nameof(PaginationText));
        OnPropertyChanged(nameof(CanPagePrevious));
        OnPropertyChanged(nameof(CanPageNext));
        OnPropertyChanged(nameof(NoRows));
    }

    private TimesheetEmployeeRowView ToEmployeeRow(AttendanceTimesheetMonthData row)
    {
        var month = MonthDate ?? FromDate ?? WorkSchedulePresentation.BusinessToday();
        return new TimesheetEmployeeRowView(
            row,
            $"{row.Employee.Code} · {row.Employee.Name}",
            string.IsNullOrWhiteSpace(row.Employee.BranchName) ? "Chưa gán chi nhánh" : row.Employee.BranchName!,
            TimesheetPresentation.DayCount(row.WorkDays),
            TimesheetPresentation.DayCount(row.CompletedDays),
            TimesheetPresentation.EmployeeLeaveSummary(row),
            TimesheetPresentation.EmployeeAttentionSummary(row),
            TimesheetPresentation.Minutes(row.CountedMinutes),
            TimesheetPresentation.AdjustmentSummary(row),
            BuildMonthCells(row, month.Year, month.Month),
            TimesheetPresentation.MonthlyViolationSummary(row),
            TimesheetPresentation.Minutes(row.LateMinutes),
            TimesheetPresentation.Minutes(row.EarlyLeaveMinutes),
            row.LockedDays.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")));
    }

    private static IReadOnlyList<TimesheetMonthDayCellView> BuildMonthCells(
        AttendanceTimesheetMonthData row,
        int year,
        int month)
    {
        var byDate = row.Days.ToDictionary(item => item.WorkDate, StringComparer.Ordinal);
        var lastDay = DateTime.DaysInMonth(year, month);
        var cells = new List<TimesheetMonthDayCellView>(31);
        for (var dayNumber = 1; dayNumber <= 31; dayNumber++)
        {
            if (dayNumber > lastDay)
            {
                cells.Add(new TimesheetMonthDayCellView(null, dayNumber, "—", "Ngày không tồn tại trong tháng", false));
                continue;
            }

            var date = $"{year:0000}-{month:00}-{dayNumber:00}";
            byDate.TryGetValue(date, out var day);
            if (day is null)
            {
                cells.Add(new TimesheetMonthDayCellView(null, dayNumber, $"{dayNumber:00}\n—", WorkSchedulePresentation.DateText(date), false));
                continue;
            }

            var compact = TimesheetPresentation.CompactStatus(day);
            cells.Add(new TimesheetMonthDayCellView(
                day,
                dayNumber,
                $"{dayNumber:00}\n{(string.IsNullOrWhiteSpace(compact) ? " " : compact)}",
                $"{WorkSchedulePresentation.DateText(date)} · {TimesheetPresentation.StatusLabel(day)}",
                true));
        }
        return cells;
    }

    private static TimesheetEmployeeDayRowView ToDayRow(AttendanceTimesheetDayData day) =>
        new(
            day,
            WorkSchedulePresentation.DateText(day.WorkDate),
            TimesheetPresentation.StatusLabel(day),
            day.ScheduledWorkDay
                ? $"{TimesheetPresentation.Clock(day.ExpectedStartAt, day.Policy?.Timezone)} – {TimesheetPresentation.Clock(day.ExpectedEndAt, day.Policy?.Timezone)}"
                : "Ngày nghỉ",
            $"{TimesheetPresentation.Clock(day.CheckInAt, day.Policy?.Timezone)} → {TimesheetPresentation.Clock(day.CheckOutAt, day.Policy?.Timezone)}",
            TimesheetPresentation.Minutes(day.CountedMinutes),
            TimesheetPresentation.DayAttentionSummary(day));

    private bool TryResolvePeriod(out string from, out string to, out string validation)
    {
        from = string.Empty;
        to = string.Empty;
        validation = string.Empty;

        if (IsMonthlyView)
        {
            var month = MonthDate ?? FromDate;
            if (month is null)
            {
                validation = "Chọn tháng cần xem.";
                return false;
            }

            var first = new DateTime(month.Value.Year, month.Value.Month, 1);
            var last = new DateTime(month.Value.Year, month.Value.Month, DateTime.DaysInMonth(month.Value.Year, month.Value.Month));
            from = first.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            to = last.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return true;
        }

        if (FromDate is null || ToDate is null)
        {
            validation = "Chọn đầy đủ Từ ngày và Đến ngày.";
            return false;
        }

        var start = FromDate.Value.Date;
        var end = ToDate.Value.Date;
        if (end < start)
        {
            validation = "Khoảng thời gian bảng công không hợp lệ.";
            return false;
        }

        if ((end - start).TotalDays + 1 > 93)
        {
            validation = "Mỗi lần chỉ xem tối đa 93 ngày bảng công.";
            return false;
        }

        from = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        to = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
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

    private void RaiseEmployeeDetail()
    {
        OnPropertyChanged(nameof(EmployeeDetailTitle));
        OnPropertyChanged(nameof(EmployeeDetailSubtitle));
        OnPropertyChanged(nameof(EmployeeWorkDaysText));
        OnPropertyChanged(nameof(EmployeeCompletedDaysText));
        OnPropertyChanged(nameof(EmployeeLeaveText));
        OnPropertyChanged(nameof(EmployeeCountedText));
    }

    private void RaiseDayDetail()
    {
        OnPropertyChanged(nameof(DayEmployeeText));
        OnPropertyChanged(nameof(DayDateText));
        OnPropertyChanged(nameof(DayStatusText));
        OnPropertyChanged(nameof(DayCheckInText));
        OnPropertyChanged(nameof(DayCheckOutText));
        OnPropertyChanged(nameof(DayActualText));
        OnPropertyChanged(nameof(DayCountedText));
        OnPropertyChanged(nameof(DayLeaveCreditedText));
        OnPropertyChanged(nameof(DayRequiredWorkText));
        OnPropertyChanged(nameof(DayLateText));
        OnPropertyChanged(nameof(DayEarlyText));
        OnPropertyChanged(nameof(DayScheduleText));
        OnPropertyChanged(nameof(DayLeaveSegmentText));
        OnPropertyChanged(nameof(DayLeaveSummaryText));
        OnPropertyChanged(nameof(DayAbsenceText));
        OnPropertyChanged(nameof(DayConfigurationText));
        OnPropertyChanged(nameof(DaySourceText));
        OnPropertyChanged(nameof(DayPolicyText));
        OnPropertyChanged(nameof(DayAdjustmentText));
        OnPropertyChanged(nameof(DayLockText));
        OnPropertyChanged(nameof(DayViolationExplanation));
        OnPropertyChanged(nameof(DayViolationItemsText));
        OnPropertyChanged(nameof(DayPolicyThresholdText));
        OnPropertyChanged(nameof(NoDayEvents));
        OnPropertyChanged(nameof(CanOpenViolationHandling));
        OnPropertyChanged(nameof(CanOpenAdjustmentForDay));
        OnPropertyChanged(nameof(AdjustmentActionText));
        OnPropertyChanged(nameof(AdjustmentTargetEmployeeId));
        OnPropertyChanged(nameof(AdjustmentTargetWorkDate));
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(CanView));
        OnPropertyChanged(nameof(CanLoad));
        OnPropertyChanged(nameof(CanFilterScoped));
        OnPropertyChanged(nameof(HasBranchFilter));
        OnPropertyChanged(nameof(PeriodText));
        OnPropertyChanged(nameof(ScopeText));
        OnPropertyChanged(nameof(TotalEmployeesText));
        OnPropertyChanged(nameof(PaginationText));
        OnPropertyChanged(nameof(CanPagePrevious));
        OnPropertyChanged(nameof(CanPageNext));
        OnPropertyChanged(nameof(NoRows));
        OnPropertyChanged(nameof(ShowLoading));
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
