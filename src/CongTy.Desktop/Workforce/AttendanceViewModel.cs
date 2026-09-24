using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed class AttendanceViewModel : INotifyPropertyChanged
{
    private const string SelfReadPermission = "core.attendance.self.read";
    private const string SelfRecordPermission = "core.attendance.self.record";
    private const string PointManagePermission = "core.attendance-point.manage";

    private readonly IAttendanceService _service;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _qrTimer;

    private AttendanceTodayData? _today;
    private AttendancePointManagementData? _management;
    private AttendanceQrTokenData? _qrToken;
    private bool _loaded;
    private bool _isBusy;
    private bool _qrRefreshInFlight;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _qrInput = string.Empty;
    private AttendanceOption? _selectedExitReason;
    private string _exitNote = string.Empty;
    private AttendanceBranchOption? _selectedWorkplace;
    private ImageSource? _qrImage;

    public AttendanceViewModel(
        IAttendanceService service,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _service = service;
        _idempotencyKeys = idempotencyKeys;
        _access = access;

        ExitReasons =
        [
            new("END_WORK", "Kết thúc ngày làm việc"),
            new("WORK_BUSINESS", "Ra ngoài làm việc"),
            new("PERSONAL", "Ra ngoài vì việc cá nhân"),
            new("BREAK", "Nghỉ giữa ca"),
            new("OTHER", "Lý do khác"),
        ];

        _qrTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _qrTimer.Tick += QrTimerOnTick;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            _today = null;
            _management = null;
            _mutationKeys.Clear();
            TodayEvents.Clear();
            WorkplaceOptions.Clear();
            SelectedWorkplace = null;
            ClearQrToken();
            QrInput = string.Empty;
            SelectedExitReason = null;
            ExitNote = string.Empty;
            SetMessage(string.Empty, false);
            RaiseAccess();
            RaiseToday();
            RaiseManagement();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AttendanceEventRowView> TodayEvents { get; } = [];
    public ObservableCollection<AttendanceBranchOption> WorkplaceOptions { get; } = [];
    public IReadOnlyList<AttendanceOption> ExitReasons { get; }

    public bool CanReadToday => _access.HasPermission(SelfReadPermission);
    public bool CanRecord => _access.HasPermission(SelfRecordPermission);
    public bool CanManagePoints => _access.HasPermission(PointManagePermission);
    public bool CanView => CanReadToday || CanRecord || CanManagePoints;
    public bool CanRefresh => CanView && !IsBusy;
    public bool CanRecordWithoutRead => CanRecord && !CanReadToday;
    public bool ShowManagement => CanManagePoints;
    public bool ShowLoading => IsBusy && !_loaded;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasToday => _today is not null;
    public bool NoEvents => _today is not null && TodayEvents.Count == 0;

    public string WorkDateText => WorkSchedulePresentation.DateText(_today?.WorkDate);
    public string EmployeeText => _today is null
        ? "Chưa xác định hồ sơ nhân sự"
        : $"{_today.Employee.FullName} · {_today.Employee.Code}";
    public string WorkplaceText => _today is null
        ? "—"
        : string.IsNullOrWhiteSpace(_today.Employee.BranchName)
            ? "Chưa được phân công"
            : _today.Employee.BranchName!;
    public string StatusText => AttendancePresentation.StatusLabel(_today?.Status);
    public string NextActionText => AttendancePresentation.NextActionLabel(_today?.NextAction);
    public string PolicyText => _today?.Policy.Name ?? "—";
    public string AttendanceMethodText => AttendancePresentation.AttendanceMethodLabel(_today?.Policy.AttendanceMethod);
    public string ExpectedStartText => WorkSchedulePresentation.DateTimeText(_today?.ExpectedStartAt, _today?.Policy.Timezone);
    public string ExpectedEndText => WorkSchedulePresentation.DateTimeText(_today?.ExpectedEndAt, _today?.Policy.Timezone);
    public string ScheduleText => _today?.Schedule is null
        ? "Không có lịch riêng"
        : _today.Schedule.Kind == "OFF" ? "Ngày nghỉ" : "Ngày làm việc";

    public bool ShowQrRecord => _today is not null && QrAllowed(_today.Policy.AttendanceMethod);
    public bool ShowFaceNotice => _today is not null && FaceAllowed(_today.Policy.AttendanceMethod);
    public bool ShowManualRecord => _today is not null && ManualAllowed(_today.Policy.AttendanceMethod);
    public bool ShowExitReason => string.Equals(_today?.NextAction, "EXIT", StringComparison.Ordinal);
    public bool ShowOtherExitNote => string.Equals(SelectedExitReason?.Key, "OTHER", StringComparison.Ordinal);
    public bool ShowPresenceNotice => string.Equals(_today?.Policy.AttendanceBasis, "PRESENCE", StringComparison.Ordinal);
    public bool HasNextAction => !string.IsNullOrWhiteSpace(_today?.NextAction);

    public bool CanSubmitQr =>
        CanRecord
        && !IsBusy
        && _today is not null
        && HasNextAction
        && ShowQrRecord
        && !string.IsNullOrWhiteSpace(QrInput)
        && ExitSelectionReady();

    public bool CanSubmitManual =>
        CanRecord
        && !IsBusy
        && _today is not null
        && HasNextAction
        && ShowManualRecord
        && ExitSelectionReady();

    public bool CanShowWorkplaceQr =>
        CanManagePoints
        && !IsBusy
        && SelectedWorkplace is not null;

    public bool HasQrToken => _qrToken is not null && QrImage is not null;
    public string QrWorkplaceText => _qrToken?.BranchName ?? _qrToken?.PointName ?? "—";
    public string QrRemainingText
    {
        get
        {
            if (_qrToken is null) return string.Empty;
            var seconds = RemainingQrSeconds();
            return seconds <= 0
                ? "Mã đã hết hạn"
                : $"Còn hiệu lực khoảng {seconds} giây";
        }
    }

    public string QrExpiresText => _qrToken is null
        ? string.Empty
        : $"Hết hạn: {WorkSchedulePresentation.DateTimeText(_qrToken.ExpiresAt, "Asia/Ho_Chi_Minh")}";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanRefresh));
            OnPropertyChanged(nameof(CanSubmitQr));
            OnPropertyChanged(nameof(CanSubmitManual));
            OnPropertyChanged(nameof(CanShowWorkplaceQr));
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

    public string QrInput
    {
        get => _qrInput;
        set
        {
            if (!SetField(ref _qrInput, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitQr));
        }
    }

    public AttendanceOption? SelectedExitReason
    {
        get => _selectedExitReason;
        set
        {
            if (!SetField(ref _selectedExitReason, value)) return;
            if (!ShowOtherExitNote && !string.IsNullOrWhiteSpace(ExitNote))
                ExitNote = string.Empty;
            OnPropertyChanged(nameof(ShowOtherExitNote));
            OnPropertyChanged(nameof(CanSubmitQr));
            OnPropertyChanged(nameof(CanSubmitManual));
        }
    }

    public string ExitNote
    {
        get => _exitNote;
        set
        {
            if (!SetField(ref _exitNote, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitQr));
            OnPropertyChanged(nameof(CanSubmitManual));
        }
    }

    public AttendanceBranchOption? SelectedWorkplace
    {
        get => _selectedWorkplace;
        set
        {
            if (Equals(_selectedWorkplace, value)) return;
            _selectedWorkplace = value;
            OnPropertyChanged();
            ClearQrToken();
            OnPropertyChanged(nameof(CanShowWorkplaceQr));
        }
    }

    public ImageSource? QrImage
    {
        get => _qrImage;
        private set => SetField(ref _qrImage, value);
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanView) return;
        await ReloadAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync() =>
        await ReloadAsync().ConfigureAwait(true);

    public async Task RefreshTodayAsync()
    {
        if (!CanReadToday || IsBusy) return;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await ReloadTodayInternalAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tải được trạng thái chấm công."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SubmitQrAsync()
    {
        if (!CanSubmitQr) return;
        if (!TryBuildRecordRequest("QR", QrInput.Trim(), out var request, out var validation))
        {
            SetMessage(validation, true);
            return;
        }
        await RecordAsync(request!).ConfigureAwait(true);
    }

    public async Task SubmitManualAsync()
    {
        if (!CanSubmitManual) return;
        if (!TryBuildRecordRequest("MANUAL", null, out var request, out var validation))
        {
            SetMessage(validation, true);
            return;
        }
        await RecordAsync(request!).ConfigureAwait(true);
    }

    public async Task ShowWorkplaceQrAsync()
    {
        if (!CanShowWorkplaceQr || _management is null || SelectedWorkplace is null) return;

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var point = _management.Points.FirstOrDefault(item =>
                item.IsActive
                && string.Equals(item.BranchId, SelectedWorkplace.Id, StringComparison.Ordinal));

            if (point is null)
            {
                var request = new CreateAttendancePointRequest(SelectedWorkplace.Id);
                var slot = MutationSlot("attendance-point-create", request);
                var key = MutationKey(slot, "desktop-attendance-point-create");
                point = await _service.CreatePointAsync(request, key).ConfigureAwait(true);
                _mutationKeys.Remove(slot);
                await ReloadManagementInternalAsync().ConfigureAwait(true);
            }

            await IssueQrTokenInternalAsync(point.Id).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không hiển thị được mã QR chấm công."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RefreshQrAsync()
    {
        if (!CanManagePoints || IsBusy || string.IsNullOrWhiteSpace(_qrToken?.AttendancePointId)) return;

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await IssueQrTokenInternalAsync(_qrToken.AttendancePointId).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không làm mới được mã QR chấm công."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ClearQrToken()
    {
        _qrToken = null;
        QrImage = null;
        _qrTimer.Stop();
        OnPropertyChanged(nameof(HasQrToken));
        OnPropertyChanged(nameof(QrWorkplaceText));
        OnPropertyChanged(nameof(QrRemainingText));
        OnPropertyChanged(nameof(QrExpiresText));
    }

    private async Task ReloadAsync()
    {
        if (!CanView || IsBusy) return;

        IsBusy = true;
        SetMessage(string.Empty, false);
        var errors = new List<string>();
        try
        {
            if (CanReadToday)
            {
                try { await ReloadTodayInternalAsync().ConfigureAwait(true); }
                catch (Exception exception) { errors.Add(PublicError(exception, "Không tải được trạng thái chấm công.")); }
            }
            else
            {
                ApplyToday(null);
            }

            if (CanManagePoints)
            {
                try { await ReloadManagementInternalAsync().ConfigureAwait(true); }
                catch (Exception exception) { errors.Add(PublicError(exception, "Không tải được danh sách nơi làm việc.")); }
            }
            else
            {
                ApplyManagement(null);
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

    private async Task ReloadTodayInternalAsync()
    {
        var today = await _service.GetTodayAsync().ConfigureAwait(true);
        ApplyToday(today);
    }

    private async Task ReloadManagementInternalAsync()
    {
        var management = await _service.GetPointManagementAsync().ConfigureAwait(true);
        ApplyManagement(management);
    }

    private async Task RecordAsync(AttendanceRecordRequest request)
    {
        var slot = MutationSlot("attendance-record", request);
        var key = MutationKey(slot, "desktop-attendance-record");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var result = await _service.RecordAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);

            QrInput = string.Empty;
            SelectedExitReason = null;
            ExitNote = string.Empty;
            await ReloadTodayInternalAsync().ConfigureAwait(true);

            var place = result.Point?.BranchName ?? result.Point?.Name;
            var placeText = string.IsNullOrWhiteSpace(place)
                ? request.Method == "MANUAL" ? " bằng chấm công trực tiếp" : string.Empty
                : $" tại {place}";
            SetMessage(
                $"Đã ghi nhận {AttendancePresentation.EventLabel(result.Event).ToLowerInvariant()} lúc " +
                $"{WorkSchedulePresentation.DateTimeText(result.Event.OccurredAt, _today?.Policy.Timezone)}{placeText}.",
                false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không ghi nhận được chấm công."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task IssueQrTokenInternalAsync(string attendancePointId)
    {
        var request = new CreateAttendanceQrTokenRequest(attendancePointId);
        var slot = MutationSlot("attendance-qr-token", request);
        var key = MutationKey(slot, "desktop-attendance-qr-token");
        var token = await _service.IssueQrTokenAsync(request, key).ConfigureAwait(true);
        _mutationKeys.Remove(slot);

        _qrToken = token;
        QrImage = AttendancePresentation.RenderQr(token.QrPayload);
        _qrTimer.Start();
        OnPropertyChanged(nameof(HasQrToken));
        OnPropertyChanged(nameof(QrWorkplaceText));
        OnPropertyChanged(nameof(QrRemainingText));
        OnPropertyChanged(nameof(QrExpiresText));
    }

    private void ApplyToday(AttendanceTodayData? today)
    {
        _today = today;
        TodayEvents.Clear();
        if (today is not null)
        {
            foreach (var item in today.Events.OrderBy(item => item.OccurredAt, StringComparer.Ordinal))
                TodayEvents.Add(AttendancePresentation.EventRow(item, today.Policy.Timezone));
        }

        SelectedExitReason = null;
        ExitNote = string.Empty;
        RaiseToday();
    }

    private void ApplyManagement(AttendancePointManagementData? management)
    {
        _management = management;
        var selectedId = SelectedWorkplace?.Id;
        WorkplaceOptions.Clear();

        if (management is not null)
        {
            foreach (var branch in management.Branches
                         .Where(item => item.IsActive)
                         .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
                WorkplaceOptions.Add(new AttendanceBranchOption(branch.Id, branch.Name));
        }

        SelectedWorkplace = WorkplaceOptions.FirstOrDefault(item =>
                                string.Equals(item.Id, selectedId, StringComparison.Ordinal))
                            ?? WorkplaceOptions.FirstOrDefault();
        RaiseManagement();
    }

    private bool TryBuildRecordRequest(
        string method,
        string? qrPayload,
        out AttendanceRecordRequest? request,
        out string validation)
    {
        request = null;
        validation = string.Empty;

        if (_today is null || string.IsNullOrWhiteSpace(_today.NextAction))
        {
            validation = "Ngày làm việc này không còn thao tác chấm công cần ghi nhận.";
            return false;
        }

        if (method == "QR" && string.IsNullOrWhiteSpace(qrPayload))
        {
            validation = "Quét hoặc nhập mã QR chấm công.";
            return false;
        }

        string? exitReason = null;
        string? note = null;
        if (string.Equals(_today.NextAction, "EXIT", StringComparison.Ordinal))
        {
            exitReason = SelectedExitReason?.Key;
            if (string.IsNullOrWhiteSpace(exitReason))
            {
                validation = "Chọn lý do rời nơi làm việc.";
                return false;
            }

            if (string.Equals(exitReason, "OTHER", StringComparison.Ordinal))
            {
                note = ExitNote.Trim();
                if (string.IsNullOrWhiteSpace(note))
                {
                    validation = "Ghi rõ lý do rời nơi làm việc.";
                    return false;
                }
                if (note.Length > 1024)
                {
                    validation = "Lý do tối đa 1024 ký tự.";
                    return false;
                }
            }
        }

        request = new AttendanceRecordRequest(method, qrPayload, exitReason, note);
        return true;
    }

    private bool ExitSelectionReady()
    {
        if (!ShowExitReason) return true;
        if (SelectedExitReason is null) return false;
        return !ShowOtherExitNote || !string.IsNullOrWhiteSpace(ExitNote);
    }

    private static bool QrAllowed(string? method) =>
        method is "QR" or "BOTH" or "QR_FACE" or "ALL";

    private static bool FaceAllowed(string? method) =>
        method is "FACE" or "QR_FACE" or "FACE_MANUAL" or "ALL";

    private static bool ManualAllowed(string? method) =>
        method is "MANUAL" or "BOTH" or "FACE_MANUAL" or "ALL";

    private int RemainingQrSeconds()
    {
        if (_qrToken is null
            || !DateTimeOffset.TryParse(
                _qrToken.ExpiresAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var expires))
            return 0;

        return Math.Max(0, (int)Math.Ceiling((expires - DateTimeOffset.UtcNow).TotalSeconds));
    }

    private async void QrTimerOnTick(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(QrRemainingText));
        if (_qrToken is null || _qrRefreshInFlight || IsBusy) return;

        var remaining = RemainingQrSeconds();
        if (remaining > 15) return;
        if (string.IsNullOrWhiteSpace(_qrToken.AttendancePointId)) return;

        _qrRefreshInFlight = true;
        try
        {
            await IssueQrTokenInternalAsync(_qrToken.AttendancePointId).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            _qrTimer.Stop();
            SetMessage(PublicError(exception, "Không tự làm mới được mã QR chấm công."), true);
        }
        finally
        {
            _qrRefreshInFlight = false;
        }
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

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(CanReadToday));
        OnPropertyChanged(nameof(CanRecord));
        OnPropertyChanged(nameof(CanManagePoints));
        OnPropertyChanged(nameof(CanView));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanRecordWithoutRead));
        OnPropertyChanged(nameof(ShowManagement));
        RaiseTodayActions();
        OnPropertyChanged(nameof(CanShowWorkplaceQr));
    }

    private void RaiseToday()
    {
        OnPropertyChanged(nameof(HasToday));
        OnPropertyChanged(nameof(NoEvents));
        OnPropertyChanged(nameof(WorkDateText));
        OnPropertyChanged(nameof(EmployeeText));
        OnPropertyChanged(nameof(WorkplaceText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(NextActionText));
        OnPropertyChanged(nameof(PolicyText));
        OnPropertyChanged(nameof(AttendanceMethodText));
        OnPropertyChanged(nameof(ExpectedStartText));
        OnPropertyChanged(nameof(ExpectedEndText));
        OnPropertyChanged(nameof(ScheduleText));
        OnPropertyChanged(nameof(ShowQrRecord));
        OnPropertyChanged(nameof(ShowFaceNotice));
        OnPropertyChanged(nameof(ShowManualRecord));
        OnPropertyChanged(nameof(ShowExitReason));
        OnPropertyChanged(nameof(ShowPresenceNotice));
        OnPropertyChanged(nameof(HasNextAction));
        RaiseTodayActions();
    }

    private void RaiseTodayActions()
    {
        OnPropertyChanged(nameof(CanSubmitQr));
        OnPropertyChanged(nameof(CanSubmitManual));
    }

    private void RaiseManagement()
    {
        OnPropertyChanged(nameof(CanShowWorkplaceQr));
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
