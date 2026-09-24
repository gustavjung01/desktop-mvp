using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed class WorkPolicyViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.work-policy.read";
    private const string ManagePermission = "core.work-policy.manage";

    private readonly IWorkPolicyService _service;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);
    private readonly List<WorkPolicyDetailData> _policies = [];

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private bool _isEditorOpen;
    private string? _basePolicyId;
    private string _draftCode = string.Empty;
    private string _draftName = string.Empty;
    private string _draftWorkNature = string.Empty;
    private string _draftTimeMode = "FIXED";
    private string _draftStartTime = "08:00";
    private string _draftEndTime = "17:00";
    private bool _monday = true;
    private bool _tuesday = true;
    private bool _wednesday = true;
    private bool _thursday = true;
    private bool _friday = true;
    private bool _saturday;
    private bool _sunday;
    private string _draftBreakMinutes = "60";
    private string _draftLateGraceMinutes = "0";
    private string _draftEarlyLeaveGraceMinutes = "0";
    private bool _draftOvertimeEnabled;
    private bool _draftOvertimeRequiresApproval = true;
    private bool _attendanceQr = true;
    private bool _attendanceFace;
    private bool _attendanceManual;
    private string _draftAttendanceBasis = "TIME";
    private string _draftTimezone = "Asia/Ho_Chi_Minh";
    private string _draftRoundingMinutes = "0";
    private string _draftMinimumFullDayMinutes = string.Empty;
    private string _draftMinimumHalfDayMinutes = string.Empty;
    private string _effectiveMode = "NOW";
    private DateTime? _draftEffectiveFrom = WorkSchedulePresentation.BusinessToday();
    private string _editorMessage = string.Empty;
    private bool _editorMessageIsError;

    public WorkPolicyViewModel(
        IWorkPolicyService service,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _service = service;
        _idempotencyKeys = idempotencyKeys;
        _access = access;

        TimeModes =
        [
            new("FIXED", "Giờ cố định"),
            new("SHIFT", "Theo ca"),
            new("FLEXIBLE", "Linh hoạt"),
            new("NO_ATTENDANCE", "Không bắt buộc chấm công"),
        ];
        AttendanceBases =
        [
            new("TIME", "Theo giờ vào và giờ ra"),
            new("PRESENCE", "Chỉ xác nhận có mặt"),
        ];

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            _policies.Clear();
            CurrentPolicies.Clear();
            HistoryPolicies.Clear();
            _mutationKeys.Clear();
            CloseEditor();
            Message = string.Empty;
            MessageIsError = false;
            RaiseAccess();
            RaiseSummary();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<WorkPolicyRowView> CurrentPolicies { get; } = [];
    public ObservableCollection<WorkPolicyRowView> HistoryPolicies { get; } = [];
    public IReadOnlyList<WorkPolicyOption> TimeModes { get; }
    public IReadOnlyList<WorkPolicyOption> AttendanceBases { get; }

    public bool CanView => _access.HasPermission(ReadPermission);
    public bool CanManage => _access.HasPermission(ManagePermission);
    public bool CanRefresh => CanView && !IsBusy;
    public bool CanOpenEditor => CanManage && !IsBusy;
    public bool CanSave => CanManage && !IsBusy && IsEditorOpen && ValidateDraft(out _);
    public bool ShowLoading => IsBusy && !_loaded;
    public bool IsEmpty => _loaded && CurrentPolicies.Count == 0;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasEditorMessage => !string.IsNullOrWhiteSpace(EditorMessage);
    public bool IsCodeReadOnly => !string.IsNullOrWhiteSpace(_basePolicyId);
    public bool ShowFixedHours => string.Equals(DraftTimeMode, "FIXED", StringComparison.Ordinal);
    public bool ShowAttendanceSettings => !string.Equals(DraftTimeMode, "NO_ATTENDANCE", StringComparison.Ordinal);
    public bool ShowEffectiveDate => string.Equals(_effectiveMode, "DATE", StringComparison.Ordinal);
    public string PolicyCountText => CurrentPolicies.Count.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    public string VersionCountText => HistoryPolicies.Count.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    public string EditorTitle => string.IsNullOrWhiteSpace(_basePolicyId) ? "Thêm chính sách" : "Cập nhật chính sách";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanRefresh));
            OnPropertyChanged(nameof(CanOpenEditor));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(ShowLoading));
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

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        private set
        {
            if (!SetField(ref _isEditorOpen, value)) return;
            OnPropertyChanged(nameof(CanSave));
        }
    }

    public string DraftCode
    {
        get => _draftCode;
        set => SetDraft(ref _draftCode, (value ?? string.Empty).ToUpperInvariant());
    }

    public string DraftName
    {
        get => _draftName;
        set => SetDraft(ref _draftName, value ?? string.Empty);
    }

    public string DraftWorkNature
    {
        get => _draftWorkNature;
        set => SetDraft(ref _draftWorkNature, value ?? string.Empty);
    }

    public string DraftTimeMode
    {
        get => _draftTimeMode;
        set
        {
            if (!SetDraft(ref _draftTimeMode, value ?? "FIXED")) return;
            if (string.Equals(_draftTimeMode, "NO_ATTENDANCE", StringComparison.Ordinal))
            {
                AttendanceQr = false;
                AttendanceFace = false;
                AttendanceManual = false;
                DraftAttendanceBasis = "NONE";
            }
            else if (string.Equals(DraftAttendanceBasis, "NONE", StringComparison.Ordinal))
            {
                DraftAttendanceBasis = "TIME";
                AttendanceQr = true;
            }
            OnPropertyChanged(nameof(ShowFixedHours));
            OnPropertyChanged(nameof(ShowAttendanceSettings));
        }
    }

    public string DraftStartTime { get => _draftStartTime; set => SetDraft(ref _draftStartTime, value ?? string.Empty); }
    public string DraftEndTime { get => _draftEndTime; set => SetDraft(ref _draftEndTime, value ?? string.Empty); }
    public bool Monday { get => _monday; set => SetDraft(ref _monday, value); }
    public bool Tuesday { get => _tuesday; set => SetDraft(ref _tuesday, value); }
    public bool Wednesday { get => _wednesday; set => SetDraft(ref _wednesday, value); }
    public bool Thursday { get => _thursday; set => SetDraft(ref _thursday, value); }
    public bool Friday { get => _friday; set => SetDraft(ref _friday, value); }
    public bool Saturday { get => _saturday; set => SetDraft(ref _saturday, value); }
    public bool Sunday { get => _sunday; set => SetDraft(ref _sunday, value); }
    public string DraftBreakMinutes { get => _draftBreakMinutes; set => SetDraft(ref _draftBreakMinutes, value ?? string.Empty); }
    public string DraftLateGraceMinutes { get => _draftLateGraceMinutes; set => SetDraft(ref _draftLateGraceMinutes, value ?? string.Empty); }
    public string DraftEarlyLeaveGraceMinutes { get => _draftEarlyLeaveGraceMinutes; set => SetDraft(ref _draftEarlyLeaveGraceMinutes, value ?? string.Empty); }
    public bool DraftOvertimeEnabled { get => _draftOvertimeEnabled; set => SetDraft(ref _draftOvertimeEnabled, value); }
    public bool DraftOvertimeRequiresApproval { get => _draftOvertimeRequiresApproval; set => SetDraft(ref _draftOvertimeRequiresApproval, value); }
    public bool AttendanceQr { get => _attendanceQr; set => SetDraft(ref _attendanceQr, value); }
    public bool AttendanceFace { get => _attendanceFace; set => SetDraft(ref _attendanceFace, value); }
    public bool AttendanceManual { get => _attendanceManual; set => SetDraft(ref _attendanceManual, value); }
    public string DraftAttendanceBasis { get => _draftAttendanceBasis; set => SetDraft(ref _draftAttendanceBasis, value ?? "TIME"); }
    public string DraftTimezone { get => _draftTimezone; set => SetDraft(ref _draftTimezone, value ?? string.Empty); }
    public string DraftRoundingMinutes { get => _draftRoundingMinutes; set => SetDraft(ref _draftRoundingMinutes, value ?? string.Empty); }
    public string DraftMinimumFullDayMinutes { get => _draftMinimumFullDayMinutes; set => SetDraft(ref _draftMinimumFullDayMinutes, value ?? string.Empty); }
    public string DraftMinimumHalfDayMinutes { get => _draftMinimumHalfDayMinutes; set => SetDraft(ref _draftMinimumHalfDayMinutes, value ?? string.Empty); }

    public bool ApplyNow
    {
        get => string.Equals(_effectiveMode, "NOW", StringComparison.Ordinal);
        set
        {
            if (!value || ApplyNow) return;
            _effectiveMode = "NOW";
            DraftEffectiveFrom = WorkSchedulePresentation.BusinessToday();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ApplyByDate));
            OnPropertyChanged(nameof(ShowEffectiveDate));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    public bool ApplyByDate
    {
        get => string.Equals(_effectiveMode, "DATE", StringComparison.Ordinal);
        set
        {
            if (!value || ApplyByDate) return;
            _effectiveMode = "DATE";
            if (DraftEffectiveFrom is null) DraftEffectiveFrom = WorkSchedulePresentation.BusinessToday();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ApplyNow));
            OnPropertyChanged(nameof(ShowEffectiveDate));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    public DateTime? DraftEffectiveFrom
    {
        get => _draftEffectiveFrom;
        set => SetDraft(ref _draftEffectiveFrom, value?.Date);
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
        if (_loaded || !CanView) return;
        await ReloadAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync() => await ReloadAsync().ConfigureAwait(true);

    public void OpenCreate()
    {
        if (!CanOpenEditor) return;
        ResetDraft();
        _basePolicyId = null;
        IsEditorOpen = true;
        OnPropertyChanged(nameof(IsCodeReadOnly));
        OnPropertyChanged(nameof(EditorTitle));
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
    }

    public void OpenEdit(WorkPolicyRowView row)
    {
        if (!CanOpenEditor || !row.CanEdit) return;
        var policy = row.Source;
        _basePolicyId = policy.Id;
        DraftCode = policy.Code;
        DraftName = policy.Name;
        DraftWorkNature = policy.WorkNature ?? string.Empty;
        DraftTimeMode = policy.TimeMode;
        DraftStartTime = WorkSchedulePresentation.ClockText(policy.FixedStartTime) is { } start && start != "—" ? start : string.Empty;
        DraftEndTime = WorkSchedulePresentation.ClockText(policy.FixedEndTime) is { } end && end != "—" ? end : string.Empty;
        Monday = policy.WorkingDays.Contains(1);
        Tuesday = policy.WorkingDays.Contains(2);
        Wednesday = policy.WorkingDays.Contains(3);
        Thursday = policy.WorkingDays.Contains(4);
        Friday = policy.WorkingDays.Contains(5);
        Saturday = policy.WorkingDays.Contains(6);
        Sunday = policy.WorkingDays.Contains(0);
        DraftBreakMinutes = policy.BreakMinutes.ToString(CultureInfo.InvariantCulture);
        DraftLateGraceMinutes = policy.LateGraceMinutes.ToString(CultureInfo.InvariantCulture);
        DraftEarlyLeaveGraceMinutes = policy.EarlyLeaveGraceMinutes.ToString(CultureInfo.InvariantCulture);
        DraftOvertimeEnabled = policy.OvertimeEnabled;
        DraftOvertimeRequiresApproval = policy.OvertimeRequiresApproval;
        var attendance = WorkPolicyPresentation.AttendanceChoices(policy.AttendanceMethod);
        AttendanceQr = attendance.Qr;
        AttendanceFace = attendance.Face;
        AttendanceManual = attendance.Manual;
        DraftAttendanceBasis = policy.AttendanceBasis == "NONE" ? "TIME" : policy.AttendanceBasis;
        DraftTimezone = policy.Timezone;
        DraftRoundingMinutes = policy.RoundingMinutes.ToString(CultureInfo.InvariantCulture);
        DraftMinimumFullDayMinutes = policy.MinimumFullDayMinutes?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        DraftMinimumHalfDayMinutes = policy.MinimumHalfDayMinutes?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        _effectiveMode = "NOW";
        DraftEffectiveFrom = WorkSchedulePresentation.BusinessToday();
        IsEditorOpen = true;
        OnPropertyChanged(nameof(ApplyNow));
        OnPropertyChanged(nameof(ApplyByDate));
        OnPropertyChanged(nameof(ShowEffectiveDate));
        OnPropertyChanged(nameof(IsCodeReadOnly));
        OnPropertyChanged(nameof(EditorTitle));
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
    }

    public void CloseEditor()
    {
        IsEditorOpen = false;
        _basePolicyId = null;
        EditorMessage = string.Empty;
        EditorMessageIsError = false;
        OnPropertyChanged(nameof(IsCodeReadOnly));
        OnPropertyChanged(nameof(EditorTitle));
    }

    public async Task SaveAsync()
    {
        if (!CanManage || IsBusy || !IsEditorOpen) return;
        if (!TryBuildRequest(out var request, out var validation))
        {
            SetEditorMessage(validation, true);
            return;
        }

        var slot = $"work-policy-save|{JsonSerializer.Serialize(request)}";
        var key = MutationKey(slot, "desktop-work-policy-save");
        IsBusy = true;
        SetEditorMessage(string.Empty, false);
        try
        {
            await _service.SavePolicyAsync(request!, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            CloseEditor();
            await ReloadAsync(forceWhileBusy: true).ConfigureAwait(true);
            SetMessage("Chính sách làm việc đã được lưu và lịch sử phiên bản được giữ nguyên.", false);
        }
        catch (Exception exception)
        {
            SetEditorMessage(PublicError(exception, "Không lưu được chính sách làm việc."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadAsync(bool forceWhileBusy = false)
    {
        if (!CanView || (IsBusy && !forceWhileBusy)) return;
        var ownsBusy = !IsBusy;
        if (ownsBusy) IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var policies = await _service.ListPoliciesAsync().ConfigureAwait(true);
            _policies.Clear();
            _policies.AddRange(policies);
            RebuildRows();
            _loaded = true;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tải được chính sách làm việc."), true);
        }
        finally
        {
            if (ownsBusy) IsBusy = false;
        }
    }

    private void RebuildRows()
    {
        CurrentPolicies.Clear();
        HistoryPolicies.Clear();

        var latestIds = _policies
            .GroupBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(item => item.Version)
                .ThenByDescending(item => item.EffectiveFrom, StringComparer.Ordinal)
                .First().Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var policy in _policies
                     .Where(item => latestIds.Contains(item.Id))
                     .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase))
            CurrentPolicies.Add(WorkPolicyPresentation.Row(policy, CanManage));

        foreach (var policy in _policies
                     .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                     .ThenByDescending(item => item.Version))
            HistoryPolicies.Add(WorkPolicyPresentation.Row(policy, false));

        RaiseSummary();
    }

    private bool TryBuildRequest(out SaveWorkPolicyRequest? request, out string validation)
    {
        request = null;
        validation = string.Empty;
        if (!ValidateDraft(out validation)) return false;

        if (!TryInt(DraftBreakMinutes, 0, 720, out var breakMinutes)
            || !TryInt(DraftLateGraceMinutes, 0, 240, out var late)
            || !TryInt(DraftEarlyLeaveGraceMinutes, 0, 240, out var early)
            || !TryInt(DraftRoundingMinutes, 0, 60, out var rounding))
        {
            validation = "Các giá trị phút của chính sách không hợp lệ.";
            return false;
        }

        if (!TryOptionalInt(DraftMinimumFullDayMinutes, 1, 1440, out var minimumFull)
            || !TryOptionalInt(DraftMinimumHalfDayMinutes, 1, 1440, out var minimumHalf))
        {
            validation = "Ngưỡng thời lượng ngày công phải từ 1 đến 1440 phút.";
            return false;
        }

        var noAttendance = string.Equals(DraftTimeMode, "NO_ATTENDANCE", StringComparison.Ordinal);
        var attendanceMethod = noAttendance
            ? "NONE"
            : WorkPolicyPresentation.ResolveAttendanceMethod(AttendanceQr, AttendanceFace, AttendanceManual);
        var attendanceBasis = noAttendance ? "NONE" : DraftAttendanceBasis.Trim().ToUpperInvariant();
        var effectiveFrom = (ApplyNow ? WorkSchedulePresentation.BusinessToday() : DraftEffectiveFrom!.Value.Date)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        request = new SaveWorkPolicyRequest(
            _basePolicyId,
            string.IsNullOrWhiteSpace(_basePolicyId) ? DraftCode.Trim().ToUpperInvariant() : null,
            DraftName.Trim(),
            EmptyToNull(DraftWorkNature),
            DraftTimeMode.Trim().ToUpperInvariant(),
            string.Equals(DraftTimeMode, "FIXED", StringComparison.Ordinal) ? DraftStartTime.Trim() : EmptyToNull(DraftStartTime),
            string.Equals(DraftTimeMode, "FIXED", StringComparison.Ordinal) ? DraftEndTime.Trim() : EmptyToNull(DraftEndTime),
            WorkingDays(),
            breakMinutes,
            late,
            early,
            DraftOvertimeEnabled,
            DraftOvertimeRequiresApproval,
            attendanceMethod,
            attendanceBasis,
            string.IsNullOrWhiteSpace(DraftTimezone) ? "Asia/Ho_Chi_Minh" : DraftTimezone.Trim(),
            rounding,
            minimumFull,
            minimumHalf,
            effectiveFrom);
        return true;
    }

    private bool ValidateDraft(out string validation)
    {
        validation = string.Empty;
        if (!CanManage || !IsEditorOpen)
        {
            validation = "Tài khoản chưa được cấp quyền quản lý chính sách làm việc.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(_basePolicyId) && string.IsNullOrWhiteSpace(DraftCode))
        {
            validation = "Mã chính sách là bắt buộc.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(DraftName))
        {
            validation = "Tên chính sách là bắt buộc.";
            return false;
        }
        if (WorkingDays().Length == 0)
        {
            validation = "Chọn ít nhất một ngày làm việc trong tuần.";
            return false;
        }
        if (string.Equals(DraftTimeMode, "FIXED", StringComparison.Ordinal))
        {
            if (!TimeOnly.TryParseExact(DraftStartTime.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                || !TimeOnly.TryParseExact(DraftEndTime.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                validation = "Giờ cố định phải theo định dạng HH:mm.";
                return false;
            }
        }
        if (!string.Equals(DraftTimeMode, "NO_ATTENDANCE", StringComparison.Ordinal)
            && !AttendanceQr && !AttendanceFace && !AttendanceManual)
        {
            validation = "Chọn ít nhất một phương thức chấm công.";
            return false;
        }
        if (ApplyByDate && DraftEffectiveFrom is null)
        {
            validation = "Chọn ngày áp dụng chính sách.";
            return false;
        }
        if (!string.IsNullOrWhiteSpace(_basePolicyId)
            && ApplyByDate
            && DraftEffectiveFrom!.Value.Date < WorkSchedulePresentation.BusinessToday())
        {
            validation = "Phiên bản cập nhật chỉ được áp dụng từ hôm nay trở đi.";
            return false;
        }
        return true;
    }

    private int[] WorkingDays()
    {
        var days = new List<int>();
        if (Sunday) days.Add(0);
        if (Monday) days.Add(1);
        if (Tuesday) days.Add(2);
        if (Wednesday) days.Add(3);
        if (Thursday) days.Add(4);
        if (Friday) days.Add(5);
        if (Saturday) days.Add(6);
        return [.. days];
    }

    private void ResetDraft()
    {
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftWorkNature = string.Empty;
        DraftTimeMode = "FIXED";
        DraftStartTime = "08:00";
        DraftEndTime = "17:00";
        Monday = Tuesday = Wednesday = Thursday = Friday = true;
        Saturday = Sunday = false;
        DraftBreakMinutes = "60";
        DraftLateGraceMinutes = "0";
        DraftEarlyLeaveGraceMinutes = "0";
        DraftOvertimeEnabled = false;
        DraftOvertimeRequiresApproval = true;
        AttendanceQr = true;
        AttendanceFace = false;
        AttendanceManual = false;
        DraftAttendanceBasis = "TIME";
        DraftTimezone = "Asia/Ho_Chi_Minh";
        DraftRoundingMinutes = "0";
        DraftMinimumFullDayMinutes = string.Empty;
        DraftMinimumHalfDayMinutes = string.Empty;
        _effectiveMode = "NOW";
        DraftEffectiveFrom = WorkSchedulePresentation.BusinessToday();
        OnPropertyChanged(nameof(ApplyNow));
        OnPropertyChanged(nameof(ApplyByDate));
        OnPropertyChanged(nameof(ShowEffectiveDate));
    }

    private string MutationKey(string slot, string scope)
    {
        if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;
        var key = _idempotencyKeys.Create(scope);
        _mutationKeys[slot] = key;
        return key;
    }

    private static bool TryInt(string value, int min, int max, out int parsed) =>
        int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
        && parsed >= min && parsed <= max;

    private static bool TryOptionalInt(string value, int min, int max, out int? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            || number < min || number > max)
            return false;
        parsed = number;
        return true;
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
        OnPropertyChanged(nameof(CanView));
        OnPropertyChanged(nameof(CanManage));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanOpenEditor));
        OnPropertyChanged(nameof(CanSave));
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(PolicyCountText));
        OnPropertyChanged(nameof(VersionCountText));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private bool SetDraft<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!SetField(ref field, value, propertyName)) return false;
        OnPropertyChanged(nameof(CanSave));
        return true;
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
