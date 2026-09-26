using System.Globalization;
using System.Text.Json;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class TimesheetViewModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceAdjustmentService _adjustmentService;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _dayMutationKeys = new(StringComparer.Ordinal);

    private bool _dayActionBusy;
    private bool _quickExitOpen;
    private TimesheetQuickExitReasonOption? _selectedQuickExitReason;
    private string _quickExitNote = string.Empty;
    private bool _dayAdjustmentOpen;
    private string _dayAdjustmentCheckIn = string.Empty;
    private string _dayAdjustmentCheckOut = string.Empty;
    private string _dayAdjustmentReason = string.Empty;
    private string _noticeMessage = string.Empty;

    public IReadOnlyList<TimesheetQuickExitReasonOption> QuickExitReasons { get; } =
    [
        new("WORK_BUSINESS", "Ra ngoài làm việc"),
        new("PERSONAL", "Ra ngoài việc cá nhân"),
        new("BREAK", "Nghỉ giữa ca"),
        new("OTHER", "Lý do khác"),
    ];

    public bool IsDayActionBusy
    {
        get => _dayActionBusy;
        private set
        {
            if (!SetField(ref _dayActionBusy, value)) return;
            RaiseDayActionState();
        }
    }

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

    public bool SelectedIsToday =>
        SelectedDay is not null
        && DateTime.TryParseExact(
            SelectedDay.WorkDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var selectedDate)
        && selectedDate.Date == WorkSchedulePresentation.BusinessToday();

    public bool CanOperateSelectedDayNow =>
        SelectedDay is not null
        && SelectedIsToday
        && _data is { Capabilities.CanManage: true } data
        && (SelectedDay.PeriodLock is null || data.Capabilities.CanLock);

    public bool CanAdjustSelectedDay =>
        SelectedDay is not null
        && _data is { Capabilities.CanManage: true } data
        && (SelectedDay.PeriodLock is null || data.Capabilities.CanLock);

    public bool CanUseDayActionButtons => CanOperateSelectedDayNow && !IsDayActionBusy;
    public bool CanToggleDayAdjustment => CanAdjustSelectedDay && !IsDayActionBusy;
    public bool SelectedNeedsCheckIn => SelectedDay is not null && string.IsNullOrWhiteSpace(SelectedDay.CheckInAt);
    public bool SelectedNeedsCheckOut =>
        SelectedDay is not null
        && !string.IsNullOrWhiteSpace(SelectedDay.CheckInAt)
        && string.IsNullOrWhiteSpace(SelectedDay.CheckOutAt);

    public bool SelectedExternalWorkPending
    {
        get
        {
            var latest = SelectedDay?.Events.LastOrDefault();
            return latest?.EventType == "TEMP_EXIT"
                   && latest.MovementReason == "WORK_BUSINESS";
        }
    }

    public bool ShowQuickCheckIn => CanOperateSelectedDayNow && SelectedNeedsCheckIn;
    public bool ShowQuickExitAndEndWork =>
        CanOperateSelectedDayNow && SelectedNeedsCheckOut && !SelectedExternalWorkPending;
    public bool ShowQuickExternalActions =>
        CanOperateSelectedDayNow && SelectedExternalWorkPending;
    public bool ShowQuickComplete =>
        CanOperateSelectedDayNow && !SelectedNeedsCheckIn && !SelectedNeedsCheckOut;

    public string DayQuickHint => SelectedIsToday
        ? "Ghi nhận trực tiếp ngay tại Bảng công."
        : "Ngày cũ chỉ sửa bằng điều chỉnh có lưu lịch sử.";

    public bool ShowQuickExitPanel =>
        _quickExitOpen && CanOperateSelectedDayNow && SelectedNeedsCheckOut && !SelectedExternalWorkPending;

    public TimesheetQuickExitReasonOption? SelectedQuickExitReason
    {
        get => _selectedQuickExitReason;
        set
        {
            if (!SetField(ref _selectedQuickExitReason, value)) return;
            OnPropertyChanged(nameof(ShowQuickExitNote));
            OnPropertyChanged(nameof(CanSubmitQuickExit));
        }
    }

    public string QuickExitNote
    {
        get => _quickExitNote;
        set
        {
            if (!SetField(ref _quickExitNote, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitQuickExit));
        }
    }

    public bool ShowQuickExitNote => SelectedQuickExitReason?.Key == "OTHER";
    public bool CanSubmitQuickExit =>
        ShowQuickExitPanel
        && !IsDayActionBusy
        && SelectedQuickExitReason is not null
        && (SelectedQuickExitReason.Key != "OTHER" || !string.IsNullOrWhiteSpace(QuickExitNote));

    public bool ShowDayAdjustmentEditor => _dayAdjustmentOpen && CanAdjustSelectedDay;
    public string DayAdjustmentToggleText => _dayAdjustmentOpen ? "ĐÓNG SỬA GIỜ" : "SỬA GIỜ VÀO / RA";

    public string DayAdjustmentCheckIn
    {
        get => _dayAdjustmentCheckIn;
        set
        {
            if (!SetField(ref _dayAdjustmentCheckIn, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveDayAdjustment));
        }
    }

    public string DayAdjustmentCheckOut
    {
        get => _dayAdjustmentCheckOut;
        set
        {
            if (!SetField(ref _dayAdjustmentCheckOut, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveDayAdjustment));
        }
    }

    public string DayAdjustmentReason
    {
        get => _dayAdjustmentReason;
        set
        {
            if (!SetField(ref _dayAdjustmentReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveDayAdjustment));
        }
    }

    public bool CanSaveDayAdjustment =>
        ShowDayAdjustmentEditor
        && !IsDayActionBusy
        && (!string.IsNullOrWhiteSpace(DayAdjustmentCheckIn) || !string.IsNullOrWhiteSpace(DayAdjustmentCheckOut))
        && !string.IsNullOrWhiteSpace(DayAdjustmentReason);

    public string DayAdjustmentSaveText => IsDayActionBusy ? "ĐANG LƯU…" : "LƯU ĐIỀU CHỈNH";

    public void ToggleQuickExit()
    {
        if (!CanOperateSelectedDayNow || !SelectedNeedsCheckOut || SelectedExternalWorkPending || IsDayActionBusy)
            return;

        _quickExitOpen = !_quickExitOpen;
        OnPropertyChanged(nameof(ShowQuickExitPanel));
        OnPropertyChanged(nameof(CanSubmitQuickExit));
    }

    public void ToggleDayAdjustment()
    {
        if (!CanAdjustSelectedDay || IsDayActionBusy) return;
        _dayAdjustmentOpen = !_dayAdjustmentOpen;
        OnPropertyChanged(nameof(ShowDayAdjustmentEditor));
        OnPropertyChanged(nameof(DayAdjustmentToggleText));
        OnPropertyChanged(nameof(CanSaveDayAdjustment));
    }

    public Task SubmitQuickAttendanceAsync(string action) =>
        SubmitQuickAttendanceCoreAsync(action, null, null);

    public Task SubmitQuickExitAsync()
    {
        if (!CanSubmitQuickExit || SelectedQuickExitReason is null)
            return Task.CompletedTask;

        return SubmitQuickAttendanceCoreAsync(
            "TEMP_EXIT",
            SelectedQuickExitReason.Key,
            QuickExitNote);
    }

    public async Task SaveDayAdjustmentAsync()
    {
        if (!CanAdjustSelectedDay || SelectedDay is null || IsDayActionBusy)
            return;

        if (!TryBuildDayAdjustment(
                SelectedDay,
                DayAdjustmentCheckIn,
                DayAdjustmentCheckOut,
                DayAdjustmentReason,
                out var request,
                out var validation))
        {
            NoticeMessage = string.Empty;
            ErrorMessage = validation;
            return;
        }

        var (slot, key) = StableDayMutationKey(
            "timesheet-attendance-direct-adjustment",
            request!);

        IsDayActionBusy = true;
        ErrorMessage = string.Empty;
        NoticeMessage = string.Empty;
        try
        {
            await _adjustmentService.DirectAsync(request!, key).ConfigureAwait(true);
            _dayMutationKeys.Remove(slot);
            NoticeMessage = "Đã cập nhật giờ công và lưu lịch sử điều chỉnh.";
            await ReloadAfterDayMutationAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ErrorMessage = PublicError(exception, "Không điều chỉnh được ngày công.");
        }
        finally
        {
            IsDayActionBusy = false;
        }
    }

    private async Task SubmitQuickAttendanceCoreAsync(
        string action,
        string? exitReason,
        string? note)
    {
        if (!CanOperateSelectedDayNow || SelectedDay is null || IsDayActionBusy)
            return;

        var normalizedAction = (action ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedAction is not ("CHECK_IN" or "CHECK_OUT" or "TEMP_EXIT" or "RETURN" or "END_EXTERNAL_WORK"))
        {
            ErrorMessage = "Thao tác chấm công không hợp lệ.";
            return;
        }

        var normalizedExitReason = string.IsNullOrWhiteSpace(exitReason)
            ? null
            : exitReason.Trim().ToUpperInvariant();
        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (normalizedAction == "TEMP_EXIT")
        {
            if (normalizedExitReason is not ("WORK_BUSINESS" or "PERSONAL" or "BREAK" or "OTHER"))
            {
                ErrorMessage = "Vui lòng chọn mục đích ra ngoài.";
                return;
            }

            if (normalizedExitReason == "OTHER" && string.IsNullOrWhiteSpace(normalizedNote))
            {
                ErrorMessage = "Lý do khác phải có ghi chú.";
                return;
            }

            if ((normalizedNote?.Length ?? 0) > 1024)
            {
                ErrorMessage = "Ghi chú ra ngoài tối đa 1.024 ký tự.";
                return;
            }
        }
        else
        {
            normalizedExitReason = null;
            normalizedNote = null;
        }

        var payload = new ManagedManualAttendanceRequest(
            SelectedDay.Employee.Id,
            normalizedAction,
            normalizedExitReason,
            normalizedNote);
        var (slot, key) = StableDayMutationKey(
            "timesheet-attendance-quick-action",
            new QuickAttendanceOperation(SelectedDay.WorkDate, payload));

        IsDayActionBusy = true;
        ErrorMessage = string.Empty;
        NoticeMessage = string.Empty;
        try
        {
            await _attendanceService.RecordManagedManualAsync(payload, key).ConfigureAwait(true);
            _dayMutationKeys.Remove(slot);
            NoticeMessage = normalizedAction == "END_EXTERNAL_WORK"
                ? "Đã kết thúc công việc bên ngoài."
                : "Đã ghi nhận chấm công.";
            await ReloadAfterDayMutationAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ErrorMessage = PublicError(exception, "Không ghi nhận được chấm công.");
        }
        finally
        {
            IsDayActionBusy = false;
        }
    }

    private void PrepareDayActions(AttendanceTimesheetDayData day)
    {
        _quickExitOpen = false;
        _selectedQuickExitReason = null;
        _quickExitNote = string.Empty;
        _dayAdjustmentOpen = false;
        _dayAdjustmentCheckIn = string.IsNullOrWhiteSpace(day.CheckInAt)
            ? string.Empty
            : TimesheetPresentation.Clock(day.CheckInAt, day.Policy?.Timezone);
        _dayAdjustmentCheckOut = string.IsNullOrWhiteSpace(day.CheckOutAt)
            ? string.Empty
            : TimesheetPresentation.Clock(day.CheckOutAt, day.Policy?.Timezone);
        _dayAdjustmentReason = string.Empty;
        RaiseDayActionState();
    }

    private void ResetDayActions()
    {
        _quickExitOpen = false;
        _selectedQuickExitReason = null;
        _quickExitNote = string.Empty;
        _dayAdjustmentOpen = false;
        _dayAdjustmentCheckIn = string.Empty;
        _dayAdjustmentCheckOut = string.Empty;
        _dayAdjustmentReason = string.Empty;
        RaiseDayActionState();
    }

    private async Task ReloadAfterDayMutationAsync()
    {
        var employeeId = SelectedDay?.Employee.Id;
        var workDate = SelectedDay?.WorkDate;
        var offset = _data?.Pagination.Offset ?? 0;

        await LoadAsync(offset).ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(workDate))
            return;

        var employee = EmployeeRows.FirstOrDefault(row =>
            string.Equals(row.Source.Employee.Id, employeeId, StringComparison.Ordinal));
        if (employee is null) return;

        OpenEmployee(employee);
        var day = employee.Source.Days.FirstOrDefault(item =>
            string.Equals(item.WorkDate, workDate, StringComparison.Ordinal));
        if (day is not null) OpenDay(day);
    }

    private (string Slot, string Key) StableDayMutationKey<T>(string operation, T payload)
    {
        var slot = $"{operation}|{JsonSerializer.Serialize(payload)}";
        if (_dayMutationKeys.TryGetValue(slot, out var existing))
            return (slot, existing);

        var key = _idempotencyKeys.Create(operation);
        _dayMutationKeys[slot] = key;
        return (slot, key);
    }

    private static bool TryBuildDayAdjustment(
        AttendanceTimesheetDayData day,
        string rawCheckIn,
        string rawCheckOut,
        string rawReason,
        out DirectAttendanceAdjustmentRequest? request,
        out string validation)
    {
        request = null;
        validation = string.Empty;

        if (!DateTime.TryParseExact(
                day.WorkDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var workDate))
        {
            validation = "Ngày công không hợp lệ.";
            return false;
        }

        var checkInClock = rawCheckIn.Trim();
        var checkOutClock = rawCheckOut.Trim();
        if (string.IsNullOrWhiteSpace(checkInClock) && string.IsNullOrWhiteSpace(checkOutClock))
        {
            validation = "Cần nhập ít nhất giờ vào hoặc giờ ra.";
            return false;
        }

        var reason = rawReason.Trim();
        if (reason.Length is < 1 or > 1000)
        {
            validation = "Lý do điều chỉnh là bắt buộc và tối đa 1.000 ký tự.";
            return false;
        }

        try
        {
            var timezone = day.Policy?.Timezone ?? "Asia/Ho_Chi_Minh";
            var checkIn = string.IsNullOrWhiteSpace(checkInClock)
                ? null
                : WorkSchedulePresentation.ToIso(workDate, NormalizeDayClock(checkInClock), timezone, false);
            var checkOut = string.IsNullOrWhiteSpace(checkOutClock)
                ? null
                : WorkSchedulePresentation.ToIso(workDate, NormalizeDayClock(checkOutClock), timezone, false);

            if (checkIn is not null
                && checkOut is not null
                && DateTimeOffset.Parse(checkOut, CultureInfo.InvariantCulture)
                   < DateTimeOffset.Parse(checkIn, CultureInfo.InvariantCulture))
            {
                validation = "Giờ ra không được sớm hơn giờ vào.";
                return false;
            }

            request = new DirectAttendanceAdjustmentRequest(
                day.Employee.Id,
                day.WorkDate,
                checkIn,
                checkOut,
                reason);
            return true;
        }
        catch (InvalidOperationException)
        {
            validation = "Giờ điều chỉnh phải theo định dạng HH:mm.";
            return false;
        }
    }

    private static string NormalizeDayClock(string value)
    {
        var candidate = value.Trim();
        if (TimeOnly.TryParseExact(candidate, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var shortTime))
            return shortTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        if (TimeOnly.TryParseExact(candidate, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return time.ToString("HH:mm", CultureInfo.InvariantCulture);
        throw new InvalidOperationException("Giờ không hợp lệ.");
    }

    private sealed record QuickAttendanceOperation(
        string WorkDate,
        ManagedManualAttendanceRequest Payload);

    private void RaiseDayActionState()
    {
        OnPropertyChanged(nameof(IsDayActionBusy));
        OnPropertyChanged(nameof(SelectedIsToday));
        OnPropertyChanged(nameof(CanOperateSelectedDayNow));
        OnPropertyChanged(nameof(CanAdjustSelectedDay));
        OnPropertyChanged(nameof(CanUseDayActionButtons));
        OnPropertyChanged(nameof(CanToggleDayAdjustment));
        OnPropertyChanged(nameof(SelectedNeedsCheckIn));
        OnPropertyChanged(nameof(SelectedNeedsCheckOut));
        OnPropertyChanged(nameof(SelectedExternalWorkPending));
        OnPropertyChanged(nameof(ShowQuickCheckIn));
        OnPropertyChanged(nameof(ShowQuickExitAndEndWork));
        OnPropertyChanged(nameof(ShowQuickExternalActions));
        OnPropertyChanged(nameof(ShowQuickComplete));
        OnPropertyChanged(nameof(DayQuickHint));
        OnPropertyChanged(nameof(ShowQuickExitPanel));
        OnPropertyChanged(nameof(SelectedQuickExitReason));
        OnPropertyChanged(nameof(QuickExitNote));
        OnPropertyChanged(nameof(ShowQuickExitNote));
        OnPropertyChanged(nameof(CanSubmitQuickExit));
        OnPropertyChanged(nameof(ShowDayAdjustmentEditor));
        OnPropertyChanged(nameof(DayAdjustmentToggleText));
        OnPropertyChanged(nameof(DayAdjustmentCheckIn));
        OnPropertyChanged(nameof(DayAdjustmentCheckOut));
        OnPropertyChanged(nameof(DayAdjustmentReason));
        OnPropertyChanged(nameof(CanSaveDayAdjustment));
        OnPropertyChanged(nameof(DayAdjustmentSaveText));
    }
}

public sealed record TimesheetQuickExitReasonOption(string Key, string Label)
{
    public override string ToString() => Label;
}
