using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed class TripReconciliationViewModel : INotifyPropertyChanged
{
    private const int IntentCacheLimit = 128;
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Đối soát cuối chuyến.";

    private readonly ITripReconciliationService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);

    private bool _loaded;
    private Task<bool>? _initialLoadTask;
    private long _accessGeneration;
    private long _loadGeneration;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private TripReconciliationTripRow? _selectedTripRow;
    private TripReconciliationData? _detail;
    private string _receivedAtText = TripReconciliationPresentation.LocalInputNow();
    private string _receiptNote = string.Empty;
    private string _closedAtText = TripReconciliationPresentation.LocalInputNow();
    private string _closeNote = string.Empty;

    public TripReconciliationViewModel(
        ITripReconciliationService service,
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

    public ObservableCollection<TripReconciliationTripRow> Trips { get; } = [];
    public ObservableCollection<TripReconciliationLineRow> Lines { get; } = [];
    public ObservableCollection<TripReceiptRow> Receipts { get; } = [];

    public bool CanRead =>
        _access.HasPermission("core.delivery-trip.read")
        && _access.HasPermission("core.delivery-trip.reconciliation-read");
    public bool CanReceive => _access.HasPermission("core.delivery-trip.return-receive");
    public bool CanClose => _access.HasPermission("core.delivery-trip.close");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoading => _busyAction == "load";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasTrips => Trips.Count > 0;
    public bool HasDetail => Detail is not null;
    public bool HasReceipts => Receipts.Count > 0;
    public bool IsDispatched => Detail?.Status == "dispatched";
    public bool IsClosed => Detail?.Status == "closed";
    public bool HasOutstanding => Lines.Any(row => row.HasOutstanding);
    public bool HasMissingResults => Lines.Any(row => string.IsNullOrWhiteSpace(row.Data.AttemptId));

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

    public string RefreshButtonText => IsLoading ? "Đang tải..." : "Tải lại";

    public TripReconciliationTripRow? SelectedTripRow
    {
        get => _selectedTripRow;
        set
        {
            if (!SetField(ref _selectedTripRow, value)) return;
            if (value is null)
            {
                Detail = null;
                return;
            }
            _ = LoadDetailAsync(value.Data.Id);
        }
    }

    public TripReconciliationData? Detail
    {
        get => _detail;
        private set
        {
            if (!SetField(ref _detail, value)) return;
            Replace(Lines, value is null ? [] : value.Lines.Select(row => new TripReconciliationLineRow(row)));
            Replace(Receipts, value is null ? [] : value.Receipts.Select(TripReconciliationPresentation.Receipt));
            SeedForm();
            RaiseDetailState();
        }
    }

    public string DetailNumber => Detail?.Number ?? "Chi tiết đối soát";
    public string DetailStatus => Detail is null
        ? string.Empty
        : Detail.Status == "closed" ? "Đã đóng" : Detail.CanClose ? "Đủ điều kiện đóng" : "Còn việc cần xử lý";
    public string WarehouseText => Detail?.WarehouseCode ?? Detail?.WarehouseName ?? "Chưa rõ";
    public string DriverText => Detail?.DriverName ?? "Chưa rõ";
    public string VehicleText => Detail?.LicensePlate ?? "Chưa rõ";
    public int OutstandingCount => Lines.Count(row => row.HasOutstanding);
    public int MissingResultCount => Lines.Count(row => string.IsNullOrWhiteSpace(row.Data.AttemptId));
    public string OutstandingSummary => HasOutstanding ? $"{OutstandingCount} dòng còn trên xe" : "Hàng trên xe đã về 0";

    public string StepOneState => HasDetail ? "Đã chọn" : "Đang thực hiện";
    public string StepTwoState => HasDetail ? "Đã kiểm tra" : "Chờ";
    public string StepThreeState => IsClosed || !HasOutstanding ? "Đã đủ" : IsDispatched ? "Cần thực hiện" : "Chờ";
    public string StepFourState => IsClosed ? "Đã đóng" : Detail?.CanClose == true ? "Sẵn sàng" : "Chờ";

    public string NextActionTitle
    {
        get
        {
            if (Detail is null) return "Chọn một chuyến cần đối soát";
            if (IsClosed) return "Chuyến đã hoàn tất đối soát";
            if (HasOutstanding) return "Nhận hàng chưa giao quay về kho";
            if (HasMissingResults) return "Bổ sung kết quả lần giao";
            if (Detail.CanClose) return "Đủ điều kiện chốt đối soát & đóng chuyến";
            return "Chuyến còn điều kiện chưa hoàn tất";
        }
    }

    public string NextActionDescription
    {
        get
        {
            if (Detail is null) return "Chọn chuyến ở danh sách để xem hàng còn trên xe và việc cần xử lý.";
            if (IsClosed) return $"Đã đóng lúc {TripReconciliationPresentation.LocalDateTime(Detail.ClosedAt)}. Lịch sử kho nhận lại vẫn được giữ.";
            if (HasOutstanding) return $"{OutstandingCount} dòng hàng vẫn còn trên xe. Kho cần xác nhận số thực nhận trước khi đóng chuyến.";
            if (HasMissingResults) return $"{MissingResultCount} dòng chưa có kết quả giao. Hoàn tất Kết quả lần giao rồi quay lại đối soát.";
            if (Detail.CanClose) return "Hàng trên xe đã về 0 và mọi phiếu đã có kết quả. Kiểm tra lần cuối rồi đóng chuyến.";
            return "Hệ thống chưa cho phép đóng chuyến. Tải lại dữ liệu và kiểm tra chênh lệch.";
        }
    }

    public string CloseBlockedReason
    {
        get
        {
            if (Detail is null || !IsDispatched || Detail.CanClose) return string.Empty;
            if (HasOutstanding) return $"Chưa thể đóng: còn {OutstandingCount} dòng hàng trên xe chưa được kho nhận lại.";
            if (HasMissingResults) return $"Chưa thể đóng: còn {MissingResultCount} dòng chưa có kết quả lần giao.";
            return "Chưa thể đóng: hệ thống chưa xác nhận đủ điều kiện đối soát.";
        }
    }

    public bool HasCloseBlockedReason => !string.IsNullOrWhiteSpace(CloseBlockedReason);
    public bool CanReceiveAction => CanReceive && IsDispatched && HasOutstanding && IsNotBusy;
    public bool CanCloseAction => CanClose && IsDispatched && Detail?.CanClose == true && IsNotBusy;
    public bool CanPrint => HasDetail && IsNotBusy;

    public string ReceivedAtText
    {
        get => _receivedAtText;
        set => SetField(ref _receivedAtText, value ?? string.Empty);
    }

    public string ReceiptNote
    {
        get => _receiptNote;
        set => SetField(ref _receiptNote, value ?? string.Empty);
    }

    public string ClosedAtText
    {
        get => _closedAtText;
        set => SetField(ref _closedAtText, value ?? string.Empty);
    }

    public string CloseNote
    {
        get => _closeNote;
        set => SetField(ref _closeNote, value ?? string.Empty);
    }

    public string ReceiveButtonText => _busyAction == "receive" ? "Đang nhập kho..." : "Xác nhận nhập hàng về kho";
    public string CloseButtonText => _busyAction == "close" ? "Đang đóng chuyến..." : "Chốt đối soát & đóng chuyến";

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);
        _initialLoadTask = LoadTripsAsync();
        try
        {
            _loaded = await _initialLoadTask.ConfigureAwait(true);
            return _loaded;
        }
        finally { _initialLoadTask = null; }
    }

    public Task<bool> RefreshAsync() => LoadTripsAsync(Detail?.Id ?? SelectedTripRow?.Data.Id);

    public async Task ReceiveReturnAsync()
    {
        var detail = Detail;
        if (detail is null || !CanReceiveAction) return;

        if (!TripReconciliationPresentation.TryIsoDateTime(ReceivedAtText, out var receivedAt))
        {
            SetErrorMessage("Thời điểm kho nhận phải theo định dạng yyyy-MM-dd HH:mm.");
            return;
        }
        var note = ReceiptNote.Trim();
        if (note.Length > 2000)
        {
            SetErrorMessage("Ghi chú tối đa 2.000 ký tự.");
            return;
        }

        var selected = new List<TripReturnReceiptInputLine>();
        foreach (var row in Lines.Where(row => row.HasOutstanding))
        {
            if (!TripReconciliationPresentation.TryScaledQuantity(row.ReturnQuantity, out var quantity))
            {
                SetErrorMessage($"Số lượng nhận lại của {row.Data.Sku} không hợp lệ.");
                return;
            }
            if (quantity <= 0) continue;
            TripReconciliationPresentation.TryScaledQuantity(row.Data.OutstandingBaseQuantity, out var outstanding);
            if (quantity > outstanding)
            {
                SetErrorMessage($"Số lượng nhận lại của {row.Data.Sku} vượt phần còn trên xe.");
                return;
            }
            selected.Add(new TripReturnReceiptInputLine(row.Data.InventoryIssueLineId, TripReconciliationPresentation.Quantity(row.ReturnQuantity)));
        }
        if (selected.Count == 0)
        {
            SetErrorMessage("Nhập ít nhất một số lượng kho thực nhận lớn hơn 0.");
            return;
        }

        var ordered = selected.OrderBy(row => row.InventoryIssueLineId, StringComparer.Ordinal).ToArray();
        var fingerprint = string.Join(".", ordered.Select(row => $"{row.InventoryIssueLineId}-{row.ReturnedBaseQuantity}"));
        var intent = Intent("receive", detail.Id, receivedAt, note, fingerprint);

        SetBusy("receive");
        ClearMessage();
        try
        {
            var result = await _service.ReceiveReturnAsync(
                detail.Id,
                new TripReturnReceiptRequest(receivedAt, EmptyToNull(note), ordered),
                KeyFor(intent)).ConfigureAwait(true);
            _intentKeys.Remove(intent);
            Detail = result.Trip;
            ReceiptNote = string.Empty;
            SetNotice(result.Replayed
                ? "Yêu cầu nhập hàng đã được xử lý trước đó; dữ liệu đối soát được tải lại."
                : "Đã ghi nhận hàng quay về kho và cập nhật tồn kho.");
        }
        catch (Exception exception) { SetError(exception); }
        finally { SetBusy(null); }
    }

    public async Task CloseTripAsync()
    {
        var detail = Detail;
        if (detail is null || !CanCloseAction) return;

        if (!TripReconciliationPresentation.TryIsoDateTime(ClosedAtText, out var closedAt))
        {
            SetErrorMessage("Thời điểm đóng chuyến phải theo định dạng yyyy-MM-dd HH:mm.");
            return;
        }
        var note = CloseNote.Trim();
        if (note.Length > 2000)
        {
            SetErrorMessage("Ghi chú tối đa 2.000 ký tự.");
            return;
        }

        var intent = Intent("close", detail.Id, closedAt, note);
        SetBusy("close");
        ClearMessage();
        try
        {
            var result = await _service.CloseAsync(
                detail.Id,
                new TripCloseRequest(closedAt, EmptyToNull(note)),
                KeyFor(intent)).ConfigureAwait(true);
            _intentKeys.Remove(intent);
            Detail = result.Trip;
            await ReloadQueueAsync().ConfigureAwait(true);
            _selectedTripRow = Trips.FirstOrDefault(row => row.Data.Id == result.Trip.Id);
            OnPropertyChanged(nameof(SelectedTripRow));
            SetNotice(result.Replayed
                ? "Yêu cầu đóng chuyến đã được xử lý trước đó; dữ liệu được tải lại."
                : "Chuyến đã được đóng sau khi đối soát đủ.");
        }
        catch (Exception exception) { SetError(exception); }
        finally { SetBusy(null); }
    }

    private async Task<bool> LoadTripsAsync(string? preferredTripId = null)
    {
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetErrorMessage(ReadDenied);
            return false;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        SetBusy("load");
        ClearMessage();
        try
        {
            await ReloadQueueAsync().ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead) return false;

            var selected = string.IsNullOrWhiteSpace(preferredTripId)
                ? null
                : Trips.FirstOrDefault(row => row.Data.Id == preferredTripId);
            _selectedTripRow = selected;
            OnPropertyChanged(nameof(SelectedTripRow));
            if (selected is null) Detail = null;
            else await LoadDetailAsync(selected.Data.Id, request).ConfigureAwait(true);

            _loaded = true;
            return true;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration) SetError(exception);
            return false;
        }
        finally
        {
            if (request == _loadGeneration && _busyAction == "load") SetBusy(null);
        }
    }

    private async Task ReloadQueueAsync()
    {
        var rows = await _service.ListTripsAsync().ConfigureAwait(true);
        Replace(Trips, rows
            .Where(row => row.Status is "dispatched" or "closed")
            .Select(TripReconciliationPresentation.Trip));
        OnPropertyChanged(nameof(HasTrips));
    }

    private async Task LoadDetailAsync(string tripId, long? parentRequest = null)
    {
        var accessGeneration = _accessGeneration;
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
            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead) return;
            Detail = detail;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration) SetError(exception);
        }
        finally
        {
            if (ownsBusy && request == _loadGeneration && _busyAction == $"detail-{tripId}") SetBusy(null);
        }
    }

    private void SeedForm()
    {
        _receivedAtText = TripReconciliationPresentation.LocalInputNow();
        _closedAtText = TripReconciliationPresentation.LocalInputNow();
        _receiptNote = string.Empty;
        _closeNote = string.Empty;
        OnPropertyChanged(nameof(ReceivedAtText));
        OnPropertyChanged(nameof(ClosedAtText));
        OnPropertyChanged(nameof(ReceiptNote));
        OnPropertyChanged(nameof(CloseNote));
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
        var key = _idempotencyKeys.Create($"trip-reconciliation-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private void Reset()
    {
        Trips.Clear();
        Lines.Clear();
        Receipts.Clear();
        _selectedTripRow = null;
        _detail = null;
        ClearMessage();
        OnPropertyChanged(nameof(SelectedTripRow));
        RaiseDetailState();
    }

    private void SetBusy(string? action)
    {
        if (string.Equals(_busyAction, action, StringComparison.Ordinal)) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(RefreshButtonText));
        OnPropertyChanged(nameof(ReceiveButtonText));
        OnPropertyChanged(nameof(CloseButtonText));
        RaiseDetailState();
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanReceive));
        OnPropertyChanged(nameof(CanClose));
        RaiseDetailState();
    }

    private void RaiseDetailState()
    {
        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(HasReceipts));
        OnPropertyChanged(nameof(IsDispatched));
        OnPropertyChanged(nameof(IsClosed));
        OnPropertyChanged(nameof(HasOutstanding));
        OnPropertyChanged(nameof(HasMissingResults));
        OnPropertyChanged(nameof(DetailNumber));
        OnPropertyChanged(nameof(DetailStatus));
        OnPropertyChanged(nameof(WarehouseText));
        OnPropertyChanged(nameof(DriverText));
        OnPropertyChanged(nameof(VehicleText));
        OnPropertyChanged(nameof(OutstandingCount));
        OnPropertyChanged(nameof(MissingResultCount));
        OnPropertyChanged(nameof(OutstandingSummary));
        OnPropertyChanged(nameof(StepOneState));
        OnPropertyChanged(nameof(StepTwoState));
        OnPropertyChanged(nameof(StepThreeState));
        OnPropertyChanged(nameof(StepFourState));
        OnPropertyChanged(nameof(NextActionTitle));
        OnPropertyChanged(nameof(NextActionDescription));
        OnPropertyChanged(nameof(CloseBlockedReason));
        OnPropertyChanged(nameof(HasCloseBlockedReason));
        OnPropertyChanged(nameof(CanReceiveAction));
        OnPropertyChanged(nameof(CanCloseAction));
        OnPropertyChanged(nameof(CanPrint));
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
        if (exception is CanonicalApiException api)
        {
            var message = api.Code switch
            {
                "DELIVERY_ATTEMPT_REQUIRED" or "TRIP_CLOSE_MISSING_ATTEMPTS" => "Còn phiếu chưa có kết quả lần giao.",
                "RETURN_QUANTITY_EXCEEDS_OUTSTANDING" => "Số lượng kho nhận vượt phần hàng còn trên xe.",
                "RETURN_LINEAGE_MISMATCH" => "Dòng hàng nhận lại không thuộc chuyến đang đối soát.",
                "TRIP_CLOSE_UNRECONCILED_STOCK" => "Chưa thể đóng chuyến vì vẫn còn hàng chưa được đối soát.",
                "INVALID_TRIP_STATUS_TRANSITION" => "Chuyến không còn ở trạng thái cho phép thao tác này.",
                _ => CanonicalErrorMessages.ToOfficeMessage(api)
            };
            SetErrorMessage(CanonicalErrorMessages.WithRequestId(message, api.RequestId));
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
