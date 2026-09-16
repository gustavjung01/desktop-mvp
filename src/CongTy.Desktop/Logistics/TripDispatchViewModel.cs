using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed class TripDispatchViewModel : INotifyPropertyChanged
{
    private const int IntentCacheLimit = 128;
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Bàn giao và xuất phát.";

    private readonly ITripDispatchService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private readonly List<DeliveryTripData> _trips = [];

    private bool _loaded;
    private Task<bool>? _initialLoadTask;
    private long _accessGeneration;
    private long _loadGeneration;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private TripDispatchListRow? _selectedTripRow;
    private TripDispatchData? _selectedTrip;
    private bool _primaryReceiverMode = true;
    private string _otherReceiverName = string.Empty;
    private string _dispatchedAtText = TripDispatchPresentation.LocalInputNow();
    private string _handoverNote = string.Empty;

    public TripDispatchViewModel(
        ITripDispatchService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            Reset();
            RaisePermissions();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TripDispatchListRow> Trips { get; } = [];
    public ObservableCollection<TripDispatchStopRow> Stops { get; } = [];
    public ObservableCollection<TripDispatchMovementRow> Movements { get; } = [];

    public bool CanRead => _access.HasPermission("core.delivery-trip.read");
    public bool CanDispatch => _access.HasPermission("core.delivery-trip.dispatch");
    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoading => _busyAction == "load";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasTrips => Trips.Count > 0;
    public bool HasSelectedTrip => SelectedTrip is not null;
    public bool HasStops => Stops.Count > 0;
    public bool HasMovements => Movements.Count > 0;

    public string RefreshButtonText => IsLoading ? "Đang tải..." : "Tải lại";

    public string Message
    {
        get => _message;
        private set
        {
            if (SetField(ref _message, value ?? string.Empty))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public TripDispatchListRow? SelectedTripRow
    {
        get => _selectedTripRow;
        set
        {
            if (!SetField(ref _selectedTripRow, value)) return;
            if (value is not null) _ = LoadTripAsync(value.Data.Id);
        }
    }

    public TripDispatchData? SelectedTrip
    {
        get => _selectedTrip;
        private set
        {
            if (!SetField(ref _selectedTrip, value)) return;
            Replace(Stops, value is null ? [] : TripDispatchPresentation.Stops(value));
            Replace(Movements, value is null ? [] : TripDispatchPresentation.Movements(value));
            SeedDispatchForm(value);
            RaiseSelectedState();
        }
    }

    public string SelectedTripNumber => SelectedTrip?.Number ?? "Chọn một chuyến";
    public string SelectedTripStatus => TripDispatchPresentation.Status(SelectedTrip?.Status);
    public string WarehouseText => SelectedTrip is null
        ? "—"
        : TripDispatchPresentation.Number(SelectedTrip.WarehouseCode ?? SelectedTrip.WarehouseName, "Kho được cấp quyền");
    public string VehicleText => SelectedTrip is null
        ? "—"
        : TripDispatchPresentation.Number(SelectedTrip.LicensePlate ?? SelectedTrip.VehicleCode, "—");
    public string DriverText => SelectedTrip is null
        ? "—"
        : TripDispatchPresentation.Number(SelectedTrip.DriverName ?? SelectedTrip.DriverCode, "—");
    public string DriverIdentityText
    {
        get
        {
            if (SelectedTrip is null) return "Chưa có tài xế chính";
            var name = SelectedTrip.DriverName?.Trim();
            var code = SelectedTrip.DriverCode?.Trim();
            if (string.IsNullOrWhiteSpace(name)) return "Chưa có tài xế chính";
            return string.IsNullOrWhiteSpace(code) ? name : $"{code} · {name}";
        }
    }
    public string WorkloadText => SelectedTrip is null
        ? "—"
        : $"{SelectedTrip.Stops.Length} điểm · {SelectedTrip.Stops.Sum(stop => stop.Assignments.Length)} phiếu";

    public bool IsLockedSelected => SelectedTrip?.Status == "locked";
    public bool IsDispatchedSelected => SelectedTrip?.Status == "dispatched";
    public bool IsPrimaryReceiverMode => _primaryReceiverMode;
    public bool IsOtherReceiverMode => !_primaryReceiverMode;
    public bool CanEditDispatchForm => CanDispatch && IsLockedSelected && IsNotBusy;
    public bool CanChoosePrimaryReceiver =>
        CanEditDispatchForm && !string.IsNullOrWhiteSpace(SelectedTrip?.DriverName);
    public bool CanChooseOtherReceiver => CanEditDispatchForm;
    public bool ShowPrimaryReceiverHint => IsLockedSelected && IsPrimaryReceiverMode;
    public bool ShowOtherReceiverField => IsLockedSelected && IsOtherReceiverMode;
    public bool ShowDispatchForm => IsLockedSelected;
    public bool ShowDispatchedSummary => IsDispatchedSelected;
    public bool CanPrintSelected => HasSelectedTrip && SelectedTrip?.Status is "locked" or "dispatched";

    public string OtherReceiverName
    {
        get => _otherReceiverName;
        set
        {
            if (!SetField(ref _otherReceiverName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitDispatch));
        }
    }

    public string DispatchedAtText
    {
        get => _dispatchedAtText;
        set
        {
            if (!SetField(ref _dispatchedAtText, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitDispatch));
        }
    }

    public string HandoverNote
    {
        get => _handoverNote;
        set => SetField(ref _handoverNote, value ?? string.Empty);
    }

    public string DispatchButtonText => _busyAction == "dispatch"
        ? "Đang bàn giao..."
        : "Bàn giao và cho xe xuất phát";

    public bool CanSubmitDispatch =>
        CanDispatch
        && IsLockedSelected
        && IsNotBusy
        && !string.IsNullOrWhiteSpace(DispatchedAtText)
        && (IsPrimaryReceiverMode
            ? !string.IsNullOrWhiteSpace(SelectedTrip?.DriverName)
            : !string.IsNullOrWhiteSpace(OtherReceiverName));

    public string DispatchedAtDisplay => TripDispatchPresentation.LocalDateTime(SelectedTrip?.DispatchedAt);
    public string HandoverReceiverDisplay => TripDispatchPresentation.Number(SelectedTrip?.HandoverReceiverName, "—");
    public string DispatchReferenceDisplay => TripDispatchPresentation.Number(SelectedTrip?.DispatchId, "—");
    public string HandoverNoteDisplay => string.IsNullOrWhiteSpace(SelectedTrip?.HandoverNote)
        ? "Không có ghi chú bàn giao."
        : SelectedTrip!.HandoverNote!.Trim();

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);
        _initialLoadTask = LoadAllAsync();
        try
        {
            _loaded = await _initialLoadTask.ConfigureAwait(true);
            return _loaded;
        }
        finally
        {
            _initialLoadTask = null;
        }
    }

    public Task<bool> RefreshAsync() => LoadAllAsync(SelectedTrip?.Id);

    public void UsePrimaryReceiver()
    {
        if (!IsLockedSelected || string.IsNullOrWhiteSpace(SelectedTrip?.DriverName)) return;
        if (_primaryReceiverMode) return;
        _primaryReceiverMode = true;
        RaiseReceiverMode();
    }

    public void UseOtherReceiver()
    {
        if (!IsLockedSelected) return;
        if (!_primaryReceiverMode) return;
        _primaryReceiverMode = false;
        RaiseReceiverMode();
    }

    public async Task DispatchAsync()
    {
        var trip = SelectedTrip;
        if (trip is null || !CanDispatch || trip.Status != "locked" || IsBusy) return;

        var receiver = IsPrimaryReceiverMode
            ? trip.DriverName?.Trim() ?? string.Empty
            : OtherReceiverName.Trim();
        if (string.IsNullOrWhiteSpace(receiver))
        {
            SetErrorMessage(IsPrimaryReceiverMode
                ? "Chuyến cần có tài xế chính và thời điểm xe xuất phát."
                : "Cần nhập người nhận bàn giao khác và thời điểm xe xuất phát.");
            return;
        }
        if (receiver.Length > 256)
        {
            SetErrorMessage("Tên người nhận bàn giao tối đa 256 ký tự.");
            return;
        }
        if (!TripDispatchPresentation.TryIsoDateTime(DispatchedAtText, out var dispatchedAt))
        {
            SetErrorMessage("Thời điểm xe xuất phát phải theo định dạng yyyy-MM-dd HH:mm.");
            return;
        }
        var note = HandoverNote.Trim();
        if (note.Length > 2000)
        {
            SetErrorMessage("Ghi chú bàn giao tối đa 2.000 ký tự.");
            return;
        }

        var intent = Intent("dispatch", trip.Id, dispatchedAt, receiver, note);
        SetBusy("dispatch");
        ClearMessage();
        try
        {
            var result = await _service.DispatchAsync(
                trip.Id,
                new TripDispatchRequest(dispatchedAt, receiver, EmptyToNull(note)),
                KeyFor(intent)).ConfigureAwait(true);

            _intentKeys.Remove(intent);
            SelectedTrip = result.Trip;
            await LoadQueueAsync().ConfigureAwait(true);
            _selectedTripRow = Trips.FirstOrDefault(row => row.Data.Id == result.Trip.Id);
            OnPropertyChanged(nameof(SelectedTripRow));
            SetNotice(result.Replayed
                ? "Yêu cầu đã được xử lý trước đó; dữ liệu chuyến được tải lại."
                : $"Đã bàn giao {result.Trip.DispatchItems.Length} phiếu và cho chuyến xuất phát.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    private async Task<bool> LoadAllAsync(string? preferredTripId = null)
    {
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetErrorMessage(ReadDenied);
            return false;
        }

        var generation = _accessGeneration;
        var request = ++_loadGeneration;
        var selectedId = preferredTripId ?? SelectedTrip?.Id ?? SelectedTripRow?.Data.Id;
        SetBusy("load");
        ClearMessage();

        try
        {
            await LoadQueueAsync().ConfigureAwait(true);
            if (generation != _accessGeneration || request != _loadGeneration || !CanRead) return false;

            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                _selectedTripRow = Trips.FirstOrDefault(row => row.Data.Id == selectedId);
                OnPropertyChanged(nameof(SelectedTripRow));
                if (_selectedTripRow is not null)
                    await LoadTripAsync(_selectedTripRow.Data.Id, request).ConfigureAwait(true);
                else
                    SelectedTrip = null;
            }
            else
            {
                _selectedTripRow = null;
                OnPropertyChanged(nameof(SelectedTripRow));
                SelectedTrip = null;
            }

            _loaded = true;
            return true;
        }
        catch (Exception exception)
        {
            if (generation == _accessGeneration && request == _loadGeneration) SetError(exception);
            return false;
        }
        finally
        {
            if (request == _loadGeneration && _busyAction == "load") SetBusy(null);
        }
    }

    private async Task LoadQueueAsync()
    {
        var rows = await _service.ListTripsAsync().ConfigureAwait(true);
        _trips.Clear();
        _trips.AddRange(rows.Where(row => row.Status is "locked" or "dispatched"));
        Replace(Trips, _trips.Select(TripDispatchPresentation.ListRow));
        OnPropertyChanged(nameof(HasTrips));
    }

    private async Task LoadTripAsync(string tripId, long? parentRequest = null)
    {
        var generation = _accessGeneration;
        var request = parentRequest ?? ++_loadGeneration;
        var ownsBusy = parentRequest is null;
        if (ownsBusy)
        {
            SetBusy($"detail-{tripId}");
            ClearMessage();
        }

        try
        {
            var detail = await _service.GetAsync(tripId).ConfigureAwait(true);
            if (generation != _accessGeneration || request != _loadGeneration || !CanRead) return;
            if (detail.Status is not ("locked" or "dispatched"))
            {
                SelectedTrip = null;
                SetErrorMessage("Chuyến không còn ở trạng thái chờ bàn giao hoặc đã xuất phát.");
                await LoadQueueAsync().ConfigureAwait(true);
                return;
            }
            SelectedTrip = detail;
        }
        catch (Exception exception)
        {
            if (generation == _accessGeneration && request == _loadGeneration) SetError(exception);
        }
        finally
        {
            if (ownsBusy && request == _loadGeneration && _busyAction == $"detail-{tripId}") SetBusy(null);
        }
    }

    private void SeedDispatchForm(TripDispatchData? trip)
    {
        if (trip is null)
        {
            _primaryReceiverMode = true;
            _otherReceiverName = string.Empty;
            _dispatchedAtText = TripDispatchPresentation.LocalInputNow();
            _handoverNote = string.Empty;
            RaiseReceiverMode();
            OnPropertyChanged(nameof(OtherReceiverName));
            OnPropertyChanged(nameof(DispatchedAtText));
            OnPropertyChanged(nameof(HandoverNote));
            return;
        }

        var primary = trip.DriverName?.Trim() ?? string.Empty;
        var stored = trip.HandoverReceiverName?.Trim() ?? string.Empty;
        _primaryReceiverMode = !string.IsNullOrWhiteSpace(primary)
            && (string.IsNullOrWhiteSpace(stored) || string.Equals(primary, stored, StringComparison.Ordinal));
        _otherReceiverName = _primaryReceiverMode ? string.Empty : stored;
        _dispatchedAtText = trip.Status == "dispatched"
            ? TripDispatchPresentation.LocalInput(trip.DispatchedAt)
            : TripDispatchPresentation.LocalInputNow();
        _handoverNote = trip.HandoverNote ?? string.Empty;
        RaiseReceiverMode();
        OnPropertyChanged(nameof(OtherReceiverName));
        OnPropertyChanged(nameof(DispatchedAtText));
        OnPropertyChanged(nameof(HandoverNote));
    }

    private string Intent(string prefix, params string?[] parts) =>
        $"{prefix}|{string.Join("|", parts.Select(value => value?.Trim() ?? string.Empty))}";

    private string KeyFor(string intent)
    {
        if (_intentKeys.TryGetValue(intent, out var existing)) return existing;

        if (_intentKeys.Count >= IntentCacheLimit)
        {
            var oldest = _intentKeys.Keys.FirstOrDefault();
            if (oldest is not null) _intentKeys.Remove(oldest);
        }

        var prefix = intent.Split('|', 2)[0];
        var key = _idempotencyKeys.Create($"trip-dispatch-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private void Reset()
    {
        _trips.Clear();
        Trips.Clear();
        Stops.Clear();
        Movements.Clear();
        _selectedTripRow = null;
        _selectedTrip = null;
        _primaryReceiverMode = true;
        _otherReceiverName = string.Empty;
        _dispatchedAtText = TripDispatchPresentation.LocalInputNow();
        _handoverNote = string.Empty;
        ClearMessage();
        OnPropertyChanged(nameof(SelectedTripRow));
        RaiseSelectedState();
    }

    private void SetBusy(string? action)
    {
        if (string.Equals(_busyAction, action, StringComparison.Ordinal)) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(RefreshButtonText));
        OnPropertyChanged(nameof(DispatchButtonText));
        RaiseSelectedState();
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanDispatch));
        RaiseSelectedState();
    }

    private void RaiseReceiverMode()
    {
        OnPropertyChanged(nameof(IsPrimaryReceiverMode));
        OnPropertyChanged(nameof(IsOtherReceiverMode));
        OnPropertyChanged(nameof(CanEditDispatchForm));
        OnPropertyChanged(nameof(CanChoosePrimaryReceiver));
        OnPropertyChanged(nameof(CanChooseOtherReceiver));
        OnPropertyChanged(nameof(ShowPrimaryReceiverHint));
        OnPropertyChanged(nameof(ShowOtherReceiverField));
        OnPropertyChanged(nameof(CanSubmitDispatch));
    }

    private void RaiseSelectedState()
    {
        OnPropertyChanged(nameof(HasSelectedTrip));
        OnPropertyChanged(nameof(HasStops));
        OnPropertyChanged(nameof(HasMovements));
        OnPropertyChanged(nameof(SelectedTripNumber));
        OnPropertyChanged(nameof(SelectedTripStatus));
        OnPropertyChanged(nameof(WarehouseText));
        OnPropertyChanged(nameof(VehicleText));
        OnPropertyChanged(nameof(DriverText));
        OnPropertyChanged(nameof(DriverIdentityText));
        OnPropertyChanged(nameof(WorkloadText));
        OnPropertyChanged(nameof(IsLockedSelected));
        OnPropertyChanged(nameof(IsDispatchedSelected));
        OnPropertyChanged(nameof(ShowDispatchForm));
        OnPropertyChanged(nameof(ShowDispatchedSummary));
        OnPropertyChanged(nameof(CanPrintSelected));
        OnPropertyChanged(nameof(DispatchedAtDisplay));
        OnPropertyChanged(nameof(HandoverReceiverDisplay));
        OnPropertyChanged(nameof(DispatchReferenceDisplay));
        OnPropertyChanged(nameof(HandoverNoteDisplay));
        RaiseReceiverMode();
    }

    private void ClearMessage()
    {
        MessageIsError = false;
        Message = string.Empty;
    }

    private void SetNotice(string value)
    {
        MessageIsError = false;
        Message = value;
    }

    private void SetErrorMessage(string value)
    {
        MessageIsError = true;
        Message = value;
    }

    private void SetError(Exception exception)
    {
        if (MessageIsError && !string.IsNullOrWhiteSpace(Message)) return;
        if (exception is CanonicalApiException api)
        {
            var officeMessage = api.Code switch
            {
                "INVALID_TRIP_STATUS_TRANSITION" => "Chuyến phải ở trạng thái chờ bàn giao trước khi cho xe xuất phát.",
                "LOGISTICS_VEHICLE_NOT_AVAILABLE" => "Phương tiện của chuyến không còn sẵn sàng để xuất phát.",
                "LOGISTICS_DRIVER_NOT_AVAILABLE" => "Tài xế chính của chuyến không còn sẵn sàng.",
                "DELIVERY_TRIP_ASSIGNMENT_REQUIRED" => "Chuyến cần có ít nhất một phiếu giao trước khi bàn giao.",
                "DELIVERY_ORDER_NOT_ELIGIBLE" or "DELIVERY_ORDER_NOT_READY" => "Có phiếu giao không còn đủ điều kiện để xuất kho theo chuyến.",
                "DELIVERY_ORDER_ALREADY_ISSUED" or "DELIVERY_ORDER_ALREADY_DISPATCHED" => "Có phiếu giao đã được ghi xuất kho trước đó.",
                "INSUFFICIENT_INVENTORY" => "Tồn kho không đủ để bàn giao toàn bộ chuyến.",
                "TRIP_DISPATCH_RECONCILIATION_FAILED" => "Dữ liệu bàn giao chưa khớp với các phiếu được gán trong chuyến.",
                _ => CanonicalErrorMessages.ToOfficeMessage(api)
            };
            SetErrorMessage(CanonicalErrorMessages.WithRequestId(officeMessage, api.RequestId));
            return;
        }
        SetErrorMessage(exception.Message);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
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
