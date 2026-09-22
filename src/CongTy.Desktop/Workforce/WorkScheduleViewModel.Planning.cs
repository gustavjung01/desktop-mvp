using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class WorkScheduleViewModel
{
    private SchedulePlanningCatalogData _planningCatalog = new();
    private int _planningTabIndex;

    private string _shiftId = string.Empty;
    private string _shiftCode = string.Empty;
    private string _shiftName = string.Empty;
    private string _shiftStartTime = "08:00";
    private string _shiftEndTime = "17:00";
    private string _shiftBreakMinutes = "60";
    private bool _shiftIsActive = true;
    private string _shiftMessage = string.Empty;
    private bool _shiftMessageIsError;

    private string _weekId = string.Empty;
    private string _weekCode = string.Empty;
    private string _weekName = string.Empty;
    private bool _weekIsActive = true;
    private string _weekMessage = string.Empty;
    private bool _weekMessageIsError;

    private DateTime? _calendarDate;
    private string _calendarKind = "PUBLIC_HOLIDAY";
    private string _calendarName = string.Empty;
    private bool _calendarIsActive = true;
    private bool _calendarEditing;
    private string _calendarMessage = string.Empty;
    private bool _calendarMessageIsError;

    private string _bulkMode = "APPLY";
    private string _bulkWeekTemplateId = string.Empty;
    private DateTime? _bulkFromDate;
    private DateTime? _bulkToDate;
    private DateTime? _bulkSourceFrom;
    private DateTime? _bulkSourceTo;
    private DateTime? _bulkTargetFrom;
    private string _bulkReason = string.Empty;
    private string _bulkMessage = string.Empty;
    private bool _bulkMessageIsError;

    public ObservableCollection<ShiftTemplateRowView> ShiftRows { get; } = [];
    public ObservableCollection<WeekTemplateRowView> WeekRows { get; } = [];
    public ObservableCollection<CalendarDayRowView> CalendarRows { get; } = [];
    public ObservableCollection<WorkSchedulePolicyOption> ShiftOptions { get; } = [];
    public ObservableCollection<WorkSchedulePolicyOption> WeekTemplateOptions { get; } = [];
    public ObservableCollection<WeekDayDraftViewModel> WeekDayRows { get; } = [];
    public ObservableCollection<BulkEmployeeRowViewModel> BulkEmployees { get; } = [];

    public IReadOnlyList<WorkScheduleKindOption> WeekDayKinds { get; } =
    [
        new("WORK", "Ngày làm việc"),
        new("OFF", "Ngày nghỉ")
    ];

    public IReadOnlyList<WorkScheduleKindOption> CalendarKinds { get; } =
    [
        new("PUBLIC_HOLIDAY", "Ngày lễ"),
        new("COMPANY_DAY_OFF", "Ngày nghỉ Công Ty")
    ];

    public int PlanningTabIndex
    {
        get => _planningTabIndex;
        set => SetField(ref _planningTabIndex, value);
    }

    public bool CanManagePlanning => CanManageSchedules && !IsBusy;
    public bool HasShiftMessage => !string.IsNullOrWhiteSpace(ShiftMessage);
    public bool HasWeekMessage => !string.IsNullOrWhiteSpace(WeekMessage);
    public bool HasCalendarMessage => !string.IsNullOrWhiteSpace(CalendarMessage);
    public bool HasBulkMessage => !string.IsNullOrWhiteSpace(BulkMessage);
    public bool IsShiftEmpty => ShiftRows.Count == 0;
    public bool IsWeekEmpty => WeekRows.Count == 0;
    public bool IsCalendarEmpty => CalendarRows.Count == 0;
    public bool IsBulkEmployeeEmpty => BulkEmployees.Count == 0;
    public bool CanChangeCalendarDate => CanManagePlanning && !CalendarEditing;
    public bool IsBulkApplyMode => string.Equals(BulkMode, "APPLY", StringComparison.Ordinal);
    public bool IsBulkCopyMode => string.Equals(BulkMode, "COPY", StringComparison.Ordinal);
    public bool CanSaveShift => CanManagePlanning && ShiftDraftValid();
    public bool CanSaveWeek => CanManagePlanning && WeekDraftValid();
    public bool CanSaveCalendar => CanManagePlanning && CalendarDraftValid();
    public bool CanRunBulk => CanManagePlanning && BulkDraftValid();
    public string BulkActionText => IsBusy ? "ĐANG XỬ LÝ…" : IsBulkApplyMode ? "XẾP LỊCH HÀNG LOẠT" : "SAO CHÉP LỊCH";

    public string ShiftCode
    {
        get => _shiftCode;
        set
        {
            var normalized = NormalizeCode(value);
            if (!SetField(ref _shiftCode, normalized)) return;
            OnPropertyChanged(nameof(CanSaveShift));
        }
    }

    public string ShiftName
    {
        get => _shiftName;
        set
        {
            if (!SetField(ref _shiftName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveShift));
        }
    }

    public string ShiftStartTime
    {
        get => _shiftStartTime;
        set
        {
            if (!SetField(ref _shiftStartTime, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveShift));
        }
    }

    public string ShiftEndTime
    {
        get => _shiftEndTime;
        set
        {
            if (!SetField(ref _shiftEndTime, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveShift));
        }
    }

    public string ShiftBreakMinutes
    {
        get => _shiftBreakMinutes;
        set
        {
            if (!SetField(ref _shiftBreakMinutes, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveShift));
        }
    }

    public bool ShiftIsActive
    {
        get => _shiftIsActive;
        set
        {
            if (!SetField(ref _shiftIsActive, value)) return;
            OnPropertyChanged(nameof(CanSaveShift));
        }
    }

    public string ShiftMessage
    {
        get => _shiftMessage;
        private set
        {
            if (!SetField(ref _shiftMessage, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasShiftMessage));
        }
    }

    public bool ShiftMessageIsError
    {
        get => _shiftMessageIsError;
        private set => SetField(ref _shiftMessageIsError, value);
    }

    public string WeekCode
    {
        get => _weekCode;
        set
        {
            var normalized = NormalizeCode(value);
            if (!SetField(ref _weekCode, normalized)) return;
            OnPropertyChanged(nameof(CanSaveWeek));
        }
    }

    public string WeekName
    {
        get => _weekName;
        set
        {
            if (!SetField(ref _weekName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveWeek));
        }
    }

    public bool WeekIsActive
    {
        get => _weekIsActive;
        set
        {
            if (!SetField(ref _weekIsActive, value)) return;
            OnPropertyChanged(nameof(CanSaveWeek));
        }
    }

    public string WeekMessage
    {
        get => _weekMessage;
        private set
        {
            if (!SetField(ref _weekMessage, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasWeekMessage));
        }
    }

    public bool WeekMessageIsError
    {
        get => _weekMessageIsError;
        private set => SetField(ref _weekMessageIsError, value);
    }

    public DateTime? CalendarDate
    {
        get => _calendarDate;
        set
        {
            if (!SetField(ref _calendarDate, value)) return;
            OnPropertyChanged(nameof(CanSaveCalendar));
        }
    }

    public string CalendarKind
    {
        get => _calendarKind;
        set
        {
            if (!SetField(ref _calendarKind, value ?? "PUBLIC_HOLIDAY")) return;
            OnPropertyChanged(nameof(CanSaveCalendar));
        }
    }

    public string CalendarName
    {
        get => _calendarName;
        set
        {
            if (!SetField(ref _calendarName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveCalendar));
        }
    }

    public bool CalendarIsActive
    {
        get => _calendarIsActive;
        set
        {
            if (!SetField(ref _calendarIsActive, value)) return;
            OnPropertyChanged(nameof(CanSaveCalendar));
        }
    }

    public bool CalendarEditing
    {
        get => _calendarEditing;
        private set
        {
            if (!SetField(ref _calendarEditing, value)) return;
            OnPropertyChanged(nameof(CanChangeCalendarDate));
        }
    }

    public string CalendarMessage
    {
        get => _calendarMessage;
        private set
        {
            if (!SetField(ref _calendarMessage, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasCalendarMessage));
        }
    }

    public bool CalendarMessageIsError
    {
        get => _calendarMessageIsError;
        private set => SetField(ref _calendarMessageIsError, value);
    }

    public string BulkMode
    {
        get => _bulkMode;
        set
        {
            if (!SetField(ref _bulkMode, value ?? "APPLY")) return;
            OnPropertyChanged(nameof(IsBulkApplyMode));
            OnPropertyChanged(nameof(IsBulkCopyMode));
            OnPropertyChanged(nameof(CanRunBulk));
            OnPropertyChanged(nameof(BulkActionText));
        }
    }

    public string BulkWeekTemplateId
    {
        get => _bulkWeekTemplateId;
        set
        {
            if (!SetField(ref _bulkWeekTemplateId, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanRunBulk));
        }
    }

    public DateTime? BulkFromDate
    {
        get => _bulkFromDate;
        set
        {
            if (!SetField(ref _bulkFromDate, value)) return;
            OnPropertyChanged(nameof(CanRunBulk));
        }
    }

    public DateTime? BulkToDate
    {
        get => _bulkToDate;
        set
        {
            if (!SetField(ref _bulkToDate, value)) return;
            OnPropertyChanged(nameof(CanRunBulk));
        }
    }

    public DateTime? BulkSourceFrom
    {
        get => _bulkSourceFrom;
        set
        {
            if (!SetField(ref _bulkSourceFrom, value)) return;
            OnPropertyChanged(nameof(CanRunBulk));
        }
    }

    public DateTime? BulkSourceTo
    {
        get => _bulkSourceTo;
        set
        {
            if (!SetField(ref _bulkSourceTo, value)) return;
            OnPropertyChanged(nameof(CanRunBulk));
        }
    }

    public DateTime? BulkTargetFrom
    {
        get => _bulkTargetFrom;
        set
        {
            if (!SetField(ref _bulkTargetFrom, value)) return;
            OnPropertyChanged(nameof(CanRunBulk));
        }
    }

    public string BulkReason
    {
        get => _bulkReason;
        set
        {
            if (!SetField(ref _bulkReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanRunBulk));
        }
    }

    public string BulkMessage
    {
        get => _bulkMessage;
        private set
        {
            if (!SetField(ref _bulkMessage, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasBulkMessage));
        }
    }

    public bool BulkMessageIsError
    {
        get => _bulkMessageIsError;
        private set => SetField(ref _bulkMessageIsError, value);
    }

    private void InitializePlanningDrafts()
    {
        ResetWeekDraft();
        CalendarDate = WorkSchedulePresentation.BusinessToday().AddDays(1);
        BulkFromDate = WorkSchedulePresentation.BusinessToday().AddDays(1);
        BulkToDate = WorkSchedulePresentation.BusinessToday().AddDays(7);
        BulkSourceFrom = WorkSchedulePresentation.BusinessToday().AddDays(-7);
        BulkSourceTo = WorkSchedulePresentation.BusinessToday().AddDays(-1);
        BulkTargetFrom = WorkSchedulePresentation.BusinessToday().AddDays(1);
    }

    private void ApplyPlanningCatalog(SchedulePlanningCatalogData catalog)
    {
        _planningCatalog = catalog ?? new SchedulePlanningCatalogData();

        ShiftRows.Clear();
        foreach (var item in _planningCatalog.ShiftTemplates
                     .OrderByDescending(item => item.IsActive)
                     .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase))
        {
            ShiftRows.Add(new ShiftTemplateRowView(
                item,
                item.Code,
                item.Name,
                $"{WorkSchedulePresentation.ClockText(item.StartTime)} – {WorkSchedulePresentation.ClockText(item.EndTime)}",
                $"{item.BreakMinutes} phút",
                item.IsActive ? "Đang dùng" : "Ngừng dùng"));
        }

        WeekRows.Clear();
        foreach (var item in _planningCatalog.WeekTemplates
                     .OrderByDescending(item => item.IsActive)
                     .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase))
        {
            var workDays = item.Days.Count(day => string.Equals(day.ScheduleKind, "WORK", StringComparison.OrdinalIgnoreCase));
            WeekRows.Add(new WeekTemplateRowView(
                item,
                item.Code,
                item.Name,
                $"{workDays}/7 ngày làm việc",
                item.IsActive ? "Đang dùng" : "Ngừng dùng"));
        }

        CalendarRows.Clear();
        var today = WorkSchedulePresentation.BusinessToday();
        foreach (var item in _planningCatalog.CalendarDays
                     .OrderBy(item => item.CalendarDate, StringComparer.Ordinal))
        {
            var date = ParseDate(item.CalendarDate);
            CalendarRows.Add(new CalendarDayRowView(
                item,
                WorkSchedulePresentation.DateText(item.CalendarDate),
                item.Name,
                string.Equals(item.CalendarKind, "PUBLIC_HOLIDAY", StringComparison.OrdinalIgnoreCase)
                    ? "Ngày lễ"
                    : "Ngày nghỉ Công Ty",
                item.IsActive ? "Áp dụng" : "Ngừng áp dụng",
                CanManageSchedules && date is not null && date.Value.Date > today));
        }

        RebuildPlanningOptions();

        OnPropertyChanged(nameof(IsShiftEmpty));
        OnPropertyChanged(nameof(IsWeekEmpty));
        OnPropertyChanged(nameof(IsCalendarEmpty));
        RaisePlanningCanExecute();
    }

    private void RebuildPlanningOptions()
    {
        var weekId = BulkWeekTemplateId;

        ShiftOptions.Clear();
        foreach (var item in _planningCatalog.ShiftTemplates.Where(item => item.IsActive).OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase))
            ShiftOptions.Add(new WorkSchedulePolicyOption(item.Id, $"{item.Code} · {item.Name}", string.Empty));

        WeekTemplateOptions.Clear();
        foreach (var item in _planningCatalog.WeekTemplates.Where(item => item.IsActive).OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase))
            WeekTemplateOptions.Add(new WorkSchedulePolicyOption(item.Id, $"{item.Code} · {item.Name}", string.Empty));

        if (!string.IsNullOrWhiteSpace(weekId) && WeekTemplateOptions.Any(item => item.Id == weekId))
            BulkWeekTemplateId = weekId;
        else if (WeekTemplateOptions.Count > 0)
            BulkWeekTemplateId = WeekTemplateOptions[0].Id;

        foreach (var row in WeekDayRows)
            row.RefreshShiftOptions(ShiftOptions);
    }

    private void RebuildBulkEmployees()
    {
        var selected = BulkEmployees.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        BulkEmployees.Clear();
        foreach (var employee in _employees.OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase))
        {
            var row = new BulkEmployeeRowViewModel(employee.Id, employee.Code, employee.FullName, selected.Contains(employee.Id));
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(BulkEmployeeRowViewModel.IsSelected))
                    OnPropertyChanged(nameof(CanRunBulk));
            };
            BulkEmployees.Add(row);
        }
        OnPropertyChanged(nameof(IsBulkEmployeeEmpty));
        OnPropertyChanged(nameof(CanRunBulk));
    }

    public void NewShift()
    {
        _shiftId = string.Empty;
        ShiftCode = string.Empty;
        ShiftName = string.Empty;
        ShiftStartTime = "08:00";
        ShiftEndTime = "17:00";
        ShiftBreakMinutes = "60";
        ShiftIsActive = true;
        ShiftMessage = string.Empty;
        ShiftMessageIsError = false;
    }

    public void EditShift(ShiftTemplateRowView row)
    {
        var item = row.Source;
        _shiftId = item.Id;
        ShiftCode = item.Code;
        ShiftName = item.Name;
        ShiftStartTime = WorkSchedulePresentation.ClockText(item.StartTime);
        ShiftEndTime = WorkSchedulePresentation.ClockText(item.EndTime);
        ShiftBreakMinutes = item.BreakMinutes.ToString(CultureInfo.InvariantCulture);
        ShiftIsActive = item.IsActive;
        ShiftMessage = string.Empty;
        ShiftMessageIsError = false;
    }

    public async Task SaveShiftAsync()
    {
        if (!CanSaveShift) return;
        _ = int.TryParse(ShiftBreakMinutes, NumberStyles.Integer, CultureInfo.InvariantCulture, out var breakMinutes);
        var request = new SaveShiftTemplateRequest(
            "SAVE_SHIFT_TEMPLATE",
            EmptyToNull(_shiftId),
            ShiftCode.Trim(),
            ShiftName.Trim(),
            ShiftStartTime.Trim(),
            ShiftEndTime.Trim(),
            breakMinutes,
            ShiftIsActive);
        var slot = MutationSlot("planning-shift", request);
        var key = MutationKey(slot, "work-shift-template-save");

        IsBusy = true;
        SetShiftMessage(string.Empty, false);
        try
        {
            await _service.SaveShiftTemplateAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            NewShift();
            await ReloadPlanningCatalogCoreAsync().ConfigureAwait(true);
            SetShiftMessage(string.IsNullOrWhiteSpace(request.Id) ? "Đã tạo ca mẫu." : "Đã cập nhật ca mẫu.", false);
        }
        catch (Exception exception)
        {
            SetShiftMessage(PublicError(exception, "Không lưu được ca mẫu."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void NewWeek()
    {
        _weekId = string.Empty;
        WeekCode = string.Empty;
        WeekName = string.Empty;
        WeekIsActive = true;
        ResetWeekDraft();
        WeekMessage = string.Empty;
        WeekMessageIsError = false;
    }

    public void EditWeek(WeekTemplateRowView row)
    {
        var item = row.Source;
        _weekId = item.Id;
        WeekCode = item.Code;
        WeekName = item.Name;
        WeekIsActive = item.IsActive;

        var map = item.Days.ToDictionary(day => day.Weekday, day => day);
        foreach (var day in WeekDayRows)
        {
            if (map.TryGetValue(day.Weekday, out var source))
            {
                day.ScheduleKind = source.ScheduleKind;
                day.ShiftTemplateId = source.ShiftTemplateId ?? string.Empty;
            }
            else
            {
                day.ScheduleKind = "OFF";
                day.ShiftTemplateId = string.Empty;
            }
        }

        WeekMessage = string.Empty;
        WeekMessageIsError = false;
    }

    public async Task SaveWeekAsync()
    {
        if (!CanSaveWeek) return;

        var request = new SaveWeekTemplateRequest(
            "SAVE_WEEK_TEMPLATE",
            EmptyToNull(_weekId),
            WeekCode.Trim(),
            WeekName.Trim(),
            WeekIsActive,
            WeekDayRows
                .OrderBy(item => item.Weekday)
                .Select(item => new WorkWeekTemplateDayRequest(
                    item.Weekday,
                    item.ScheduleKind,
                    string.Equals(item.ScheduleKind, "WORK", StringComparison.Ordinal) ? EmptyToNull(item.ShiftTemplateId) : null))
                .ToArray());

        var slot = MutationSlot("planning-week", request);
        var key = MutationKey(slot, "work-week-template-save");

        IsBusy = true;
        SetWeekMessage(string.Empty, false);
        try
        {
            await _service.SaveWeekTemplateAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            NewWeek();
            await ReloadPlanningCatalogCoreAsync().ConfigureAwait(true);
            SetWeekMessage(string.IsNullOrWhiteSpace(request.Id) ? "Đã tạo mẫu lịch tuần." : "Đã cập nhật mẫu lịch tuần.", false);
        }
        catch (Exception exception)
        {
            SetWeekMessage(PublicError(exception, "Không lưu được mẫu lịch tuần."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void NewCalendarDay()
    {
        CalendarDate = WorkSchedulePresentation.BusinessToday().AddDays(1);
        CalendarKind = "PUBLIC_HOLIDAY";
        CalendarName = string.Empty;
        CalendarIsActive = true;
        CalendarEditing = false;
        CalendarMessage = string.Empty;
        CalendarMessageIsError = false;
    }

    public void EditCalendarDay(CalendarDayRowView row)
    {
        if (!row.CanEdit) return;
        var item = row.Source;
        CalendarDate = ParseDate(item.CalendarDate);
        CalendarKind = item.CalendarKind;
        CalendarName = item.Name;
        CalendarIsActive = item.IsActive;
        CalendarEditing = true;
        CalendarMessage = string.Empty;
        CalendarMessageIsError = false;
    }

    public async Task SaveCalendarDayAsync()
    {
        if (!CanSaveCalendar || CalendarDate is null) return;

        var request = new SaveCalendarDayRequest(
            "SAVE_CALENDAR_DAY",
            CanonicalDate(CalendarDate.Value),
            CalendarKind,
            CalendarName.Trim(),
            CalendarIsActive);
        var slot = MutationSlot("planning-calendar", request);
        var key = MutationKey(slot, "work-company-calendar-save");

        IsBusy = true;
        SetCalendarMessage(string.Empty, false);
        try
        {
            await _service.SaveCalendarDayAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            var wasEditing = CalendarEditing;
            NewCalendarDay();
            await ReloadPlanningCatalogCoreAsync().ConfigureAwait(true);
            SetCalendarMessage(wasEditing ? "Đã cập nhật ngày nghỉ." : "Đã thêm ngày nghỉ.", false);
        }
        catch (Exception exception)
        {
            SetCalendarMessage(PublicError(exception, "Không lưu được ngày nghỉ."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectBulkMode(string mode)
    {
        BulkMode = string.Equals(mode, "COPY", StringComparison.Ordinal) ? "COPY" : "APPLY";
        BulkMessage = string.Empty;
        BulkMessageIsError = false;
    }

    public void ToggleAllBulkEmployees()
    {
        var target = BulkEmployees.Count > 0 && BulkEmployees.Any(item => !item.IsSelected);
        foreach (var employee in BulkEmployees) employee.IsSelected = target;
        OnPropertyChanged(nameof(CanRunBulk));
    }

    public async Task RunBulkAsync()
    {
        if (!CanRunBulk) return;

        var ids = BulkEmployees.Where(item => item.IsSelected).Select(item => item.Id).ToArray();
        string slot;
        string key;

        IsBusy = true;
        SetBulkMessage(string.Empty, false);
        try
        {
            ScheduleBulkResultData result;
            if (IsBulkApplyMode)
            {
                var request = new ApplyWeekTemplateRequest(
                    "APPLY_WEEK_TEMPLATE",
                    BulkWeekTemplateId,
                    ids,
                    CanonicalDate(BulkFromDate!.Value),
                    CanonicalDate(BulkToDate!.Value),
                    BulkReason.Trim());
                slot = MutationSlot("planning-bulk-apply", request);
                key = MutationKey(slot, "work-week-template-apply");
                result = await _service.ApplyWeekTemplateAsync(request, key).ConfigureAwait(true);
            }
            else
            {
                var request = new CopyScheduleRequest(
                    "COPY_SCHEDULE",
                    ids,
                    CanonicalDate(BulkSourceFrom!.Value),
                    CanonicalDate(BulkSourceTo!.Value),
                    CanonicalDate(BulkTargetFrom!.Value),
                    BulkReason.Trim());
                slot = MutationSlot("planning-bulk-copy", request);
                key = MutationKey(slot, "work-schedule-copy");
                result = await _service.CopyScheduleAsync(request, key).ConfigureAwait(true);
            }

            _mutationKeys.Remove(slot);
            BulkReason = string.Empty;
            var preserved = result.SkippedOverrides > 0
                ? $" Giữ nguyên {result.SkippedOverrides} ngoại lệ cá nhân."
                : string.Empty;
            SetBulkMessage($"Đã ghi {result.AffectedCount}/{result.RequestedCount} dòng lịch.{preserved}", false);
            await ReloadSchedulesCoreAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetBulkMessage(PublicError(exception, "Không xếp được lịch."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadPlanningCatalogCoreAsync()
    {
        var catalog = await _service.GetPlanningCatalogAsync().ConfigureAwait(true);
        ApplyPlanningCatalog(catalog);
    }

    private void ResetWeekDraft()
    {
        WeekDayRows.Clear();
        foreach (var (weekday, label) in new (int, string)[]
                 {
                     (1, "Thứ Hai"), (2, "Thứ Ba"), (3, "Thứ Tư"), (4, "Thứ Năm"),
                     (5, "Thứ Sáu"), (6, "Thứ Bảy"), (0, "Chủ nhật")
                 })
        {
            var row = new WeekDayDraftViewModel(weekday, label);
            row.PropertyChanged += (_, _) => OnPropertyChanged(nameof(CanSaveWeek));
            row.RefreshShiftOptions(ShiftOptions);
            WeekDayRows.Add(row);
        }
    }

    private void ResetPlanningCatalog()
    {
        _planningCatalog = new SchedulePlanningCatalogData();
        ShiftRows.Clear();
        WeekRows.Clear();
        CalendarRows.Clear();
        ShiftOptions.Clear();
        WeekTemplateOptions.Clear();
        BulkEmployees.Clear();
        NewShift();
        NewWeek();
        NewCalendarDay();
        BulkMessage = string.Empty;
        BulkMessageIsError = false;
        OnPropertyChanged(nameof(IsShiftEmpty));
        OnPropertyChanged(nameof(IsWeekEmpty));
        OnPropertyChanged(nameof(IsCalendarEmpty));
        OnPropertyChanged(nameof(IsBulkEmployeeEmpty));
    }

    private bool ShiftDraftValid()
    {
        if (string.IsNullOrWhiteSpace(ShiftCode) || ShiftCode.Length > 64) return false;
        if (string.IsNullOrWhiteSpace(ShiftName) || ShiftName.Trim().Length > 256) return false;
        if (!TimeOnly.TryParseExact(ShiftStartTime.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return false;
        if (!TimeOnly.TryParseExact(ShiftEndTime.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return false;
        if (string.Equals(ShiftStartTime.Trim(), ShiftEndTime.Trim(), StringComparison.Ordinal)) return false;
        return int.TryParse(ShiftBreakMinutes, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            && value is >= 0 and <= 720;
    }

    private bool WeekDraftValid()
    {
        if (string.IsNullOrWhiteSpace(WeekCode) || WeekCode.Length > 64) return false;
        if (string.IsNullOrWhiteSpace(WeekName) || WeekName.Trim().Length > 256) return false;
        if (WeekDayRows.Count != 7) return false;
        return WeekDayRows.All(item =>
            string.Equals(item.ScheduleKind, "OFF", StringComparison.Ordinal)
            || (string.Equals(item.ScheduleKind, "WORK", StringComparison.Ordinal)
                && ShiftOptions.Any(option => option.Id == item.ShiftTemplateId)));
    }

    private bool CalendarDraftValid()
    {
        if (CalendarDate is null || CalendarDate.Value.Date <= WorkSchedulePresentation.BusinessToday()) return false;
        if (string.IsNullOrWhiteSpace(CalendarName) || CalendarName.Trim().Length > 256) return false;
        return CalendarKinds.Any(item => item.Key == CalendarKind);
    }

    private bool BulkDraftValid()
    {
        var selected = BulkEmployees.Count(item => item.IsSelected);
        if (selected is < 1 or > 500) return false;
        if (string.IsNullOrWhiteSpace(BulkReason) || BulkReason.Trim().Length > 512) return false;

        var today = WorkSchedulePresentation.BusinessToday();
        if (IsBulkApplyMode)
        {
            if (!WeekTemplateOptions.Any(item => item.Id == BulkWeekTemplateId)) return false;
            if (BulkFromDate is null || BulkToDate is null) return false;
            if (BulkFromDate.Value.Date <= today || BulkToDate.Value.Date < BulkFromDate.Value.Date) return false;
            if ((BulkToDate.Value.Date - BulkFromDate.Value.Date).TotalDays > 92) return false;
            return true;
        }

        if (BulkSourceFrom is null || BulkSourceTo is null || BulkTargetFrom is null) return false;
        if (BulkSourceTo.Value.Date < BulkSourceFrom.Value.Date) return false;
        if ((BulkSourceTo.Value.Date - BulkSourceFrom.Value.Date).TotalDays > 30) return false;
        return BulkTargetFrom.Value.Date > today;
    }

    private static string NormalizeCode(string? value)
    {
        var candidate = (value ?? string.Empty).Trim().ToUpperInvariant();
        return new string(candidate.Where(character =>
            char.IsLetterOrDigit(character) || character is '_' or '-').Take(64).ToArray());
    }

    private void RaisePlanningCanExecute()
    {
        OnPropertyChanged(nameof(CanManagePlanning));
        OnPropertyChanged(nameof(CanSaveShift));
        OnPropertyChanged(nameof(CanSaveWeek));
        OnPropertyChanged(nameof(CanSaveCalendar));
        OnPropertyChanged(nameof(CanChangeCalendarDate));
        OnPropertyChanged(nameof(CanRunBulk));
        OnPropertyChanged(nameof(BulkActionText));
    }

    private void SetShiftMessage(string message, bool error)
    {
        ShiftMessageIsError = error;
        ShiftMessage = message;
    }

    private void SetWeekMessage(string message, bool error)
    {
        WeekMessageIsError = error;
        WeekMessage = message;
    }

    private void SetCalendarMessage(string message, bool error)
    {
        CalendarMessageIsError = error;
        CalendarMessage = message;
    }

    private void SetBulkMessage(string message, bool error)
    {
        BulkMessageIsError = error;
        BulkMessage = message;
    }
}

public sealed class WeekDayDraftViewModel : INotifyPropertyChanged
{
    private string _scheduleKind = "OFF";
    private string _shiftTemplateId = string.Empty;

    public WeekDayDraftViewModel(int weekday, string dayLabel)
    {
        Weekday = weekday;
        DayLabel = dayLabel;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Weekday { get; }
    public string DayLabel { get; }
    public ObservableCollection<WorkSchedulePolicyOption> ShiftOptions { get; } = [];
    public IReadOnlyList<WorkScheduleKindOption> ScheduleKinds { get; } =
    [
        new("WORK", "Ngày làm việc"),
        new("OFF", "Ngày nghỉ")
    ];

    public string ScheduleKind
    {
        get => _scheduleKind;
        set
        {
            var next = value ?? "OFF";
            if (_scheduleKind == next) return;
            _scheduleKind = next;
            if (string.Equals(next, "OFF", StringComparison.Ordinal))
                _shiftTemplateId = string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShiftTemplateId));
            OnPropertyChanged(nameof(IsWorkDay));
        }
    }

    public string ShiftTemplateId
    {
        get => _shiftTemplateId;
        set
        {
            if (_shiftTemplateId == (value ?? string.Empty)) return;
            _shiftTemplateId = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public bool IsWorkDay => string.Equals(ScheduleKind, "WORK", StringComparison.Ordinal);

    public void RefreshShiftOptions(IEnumerable<WorkSchedulePolicyOption> options)
    {
        var current = ShiftTemplateId;
        ShiftOptions.Clear();
        foreach (var option in options) ShiftOptions.Add(option);
        if (!string.IsNullOrWhiteSpace(current) && ShiftOptions.Any(item => item.Id == current))
            ShiftTemplateId = current;
        else if (IsWorkDay && ShiftOptions.Count > 0)
            ShiftTemplateId = ShiftOptions[0].Id;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class BulkEmployeeRowViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public BulkEmployeeRowViewModel(string id, string code, string fullName, bool isSelected)
    {
        Id = id;
        Code = code;
        FullName = fullName;
        _isSelected = isSelected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }
    public string Code { get; }
    public string FullName { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}
