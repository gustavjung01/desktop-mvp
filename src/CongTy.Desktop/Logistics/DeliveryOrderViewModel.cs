using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed class DeliveryOrderViewModel : INotifyPropertyChanged
{
    private const int IdempotencyIntentCacheLimit = 256;
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Phiếu giao hàng.";

    private readonly IDeliveryOrderService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private bool _loaded;
    private bool _tabInitialized;
    private long _accessGeneration;
    private long _loadGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private int _activeTabIndex;
    private DeliveryEligibilityGroupRow? _selectedEligibilityGroup;
    private DeliveryOrderListRow? _selectedOrder;
    private DeliveryOrderData? _selectedOrderDetail;
    private string _cancelReason = string.Empty;
    private string _receiverName = string.Empty;
    private string _receiverNote = string.Empty;
    private string _reversalReason = string.Empty;
    private string _selectedPrintVariant = "Phiếu giao hàng";
    private string? _pickupAttemptAt;
    private string? _manualAttemptAt;

    public DeliveryOrderViewModel(
        IDeliveryOrderService service,
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
            _tabInitialized = false;
            _initialLoadTask = null;
            Reset();
            RaisePermissions();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DeliveryEligibilityGroupRow> EligibilityGroups { get; } = [];
    public ObservableCollection<DeliveryOrderListRow> Orders { get; } = [];
    public ObservableCollection<DeliveryOrderLineRow> OrderLines { get; } = [];
    public IReadOnlyList<string> PrintVariants { get; } = ["Phiếu giao hàng", "Phiếu đóng gói"];

    public bool CanRead => _access.HasPermission("core.delivery-order.read");
    public bool CanCreate => _access.HasPermission("core.delivery-order.create");
    public bool CanConfirm => _access.HasPermission("core.delivery-order.confirm");
    public bool CanCancel => _access.HasPermission("core.delivery-order.cancel");
    public bool CanPickupHandover => _access.HasPermission("core.delivery-order.pickup-handover");
    public bool CanManualHandover => _access.HasPermission("core.delivery-order.manual-handover");
    public bool CanReverseInventoryIssue => _access.HasPermission("core.delivery-order.reverse-inventory-issue");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoading => _busyAction == "load";
    public string RefreshButtonText => IsLoading ? "Đang tải..." : "Làm mới";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasEligibility => EligibilityGroups.Count > 0;
    public bool HasOrders => Orders.Count > 0;
    public bool HasSelectedGroup => SelectedEligibilityGroup is not null;
    public bool HasSelectedOrder => SelectedOrderDetail is not null;

    public string EligibleCount => EligibilityGroups.Count.ToString(CultureInfo.InvariantCulture);
    public string DraftCount => Orders.Count(row => row.Data.Status == "draft").ToString(CultureInfo.InvariantCulture);
    public string ReadyCount => Orders.Count(row => row.Data.Status == "ready_to_dispatch").ToString(CultureInfo.InvariantCulture);
    public string IssuedCount => Orders.Count(row => row.Data.Status is "dispatched" or "handed_over").ToString(CultureInfo.InvariantCulture);

    public string Message
    {
        get => _message;
        private set
        {
            if (SetField(ref _message, value ?? string.Empty)) OnPropertyChanged(nameof(HasMessage));
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set => SetField(ref _activeTabIndex, Math.Clamp(value, 0, 1));
    }

    public DeliveryEligibilityGroupRow? SelectedEligibilityGroup
    {
        get => _selectedEligibilityGroup;
        set
        {
            if (!SetField(ref _selectedEligibilityGroup, value)) return;
            OnPropertyChanged(nameof(HasSelectedGroup));
            OnPropertyChanged(nameof(CanCreateSelected));
        }
    }

    public DeliveryOrderListRow? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (!SetField(ref _selectedOrder, value)) return;
            if (value is not null) _ = LoadDetailAsync(value.Data.Id);
        }
    }

    public DeliveryOrderData? SelectedOrderDetail
    {
        get => _selectedOrderDetail;
        private set
        {
            if (!SetField(ref _selectedOrderDetail, value)) return;
            Replace(OrderLines, value?.Lines.Select(DeliveryOrderPresentation.Line) ?? []);
            CancelReason = string.Empty;
            ReceiverName = string.Empty;
            ReceiverNote = string.Empty;
            ReversalReason = string.Empty;
            _pickupAttemptAt = null;
            _manualAttemptAt = null;
            RaiseSelectedOrderState();
        }
    }

    public string SelectedOrderNumber => string.IsNullOrWhiteSpace(SelectedOrderDetail?.Number) ? "Chứng từ nháp" : SelectedOrderDetail.Number;
    public string SelectedOrderCustomer => SelectedOrderDetail is null ? string.Empty : $"{SelectedOrderDetail.CustomerCode} — {SelectedOrderDetail.CustomerName}";
    public string SelectedOrderSource => SelectedOrderDetail is null ? string.Empty : $"{(string.IsNullOrWhiteSpace(SelectedOrderDetail.SalesOrderNumber) ? "Đơn bán hàng" : SelectedOrderDetail.SalesOrderNumber)} · {SelectedOrderDetail.WarehouseCode}";
    public string SelectedOrderStatus => DeliveryOrderPresentation.Status(SelectedOrderDetail?.Status);
    public string SelectedOrderMode => DeliveryOrderPresentation.HandoverMode(SelectedOrderDetail?.HandoverMode);
    public string SelectedOrderCancellation => string.IsNullOrWhiteSpace(SelectedOrderDetail?.CancellationReason) ? string.Empty : $"Lý do hủy: {SelectedOrderDetail.CancellationReason}";

    public string CancelReason { get => _cancelReason; set => SetField(ref _cancelReason, value ?? string.Empty); }
    public string ReceiverName { get => _receiverName; set => SetField(ref _receiverName, value ?? string.Empty); }
    public string ReceiverNote { get => _receiverNote; set => SetField(ref _receiverNote, value ?? string.Empty); }
    public string ReversalReason { get => _reversalReason; set => SetField(ref _reversalReason, value ?? string.Empty); }
    public string SelectedPrintVariant { get => _selectedPrintVariant; set => SetField(ref _selectedPrintVariant, value ?? "Phiếu giao hàng"); }

    public bool ShowDraftActions => SelectedOrderDetail?.Status == "draft";
    public bool ShowPickupActions => CanPickupHandover
        && SelectedOrderDetail?.Status == "ready_to_dispatch"
        && SelectedOrderDetail.HandoverMode == "PICKUP";
    public bool ShowManualActions => CanManualHandover
        && SelectedOrderDetail?.Status == "ready_to_dispatch"
        && SelectedOrderDetail.HandoverMode == "DELIVERY";
    public bool ShowReverseActions => CanReverseInventoryIssue
        && SelectedOrderDetail?.Status is "dispatched" or "handed_over";
    public bool ShowPrint => SelectedOrderDetail is not null
        && SelectedOrderDetail.Status != "draft"
        && !string.IsNullOrWhiteSpace(SelectedOrderDetail.Number);

    public bool CanCreateSelected => CanCreate && IsNotBusy && SelectedEligibilityGroup is not null;
    public bool CanConfirmSelected => CanConfirm && IsNotBusy && ShowDraftActions;
    public bool CanCancelSelected => CanCancel && IsNotBusy && ShowDraftActions;
    public bool CanPickupSelected => IsNotBusy && ShowPickupActions;
    public bool CanManualSelected => IsNotBusy && ShowManualActions;
    public bool CanReverseSelected => IsNotBusy && ShowReverseActions;
    public bool CanPrintSelected => IsNotBusy && ShowPrint;

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
        finally { _initialLoadTask = null; }
    }

    public Task<bool> RefreshAsync() => LoadAllAsync(SelectedOrderDetail?.Id);

    public async Task CreateAsync()
    {
        var group = SelectedEligibilityGroup;
        if (group is null || !CanCreateSelected) return;

        var lines = new List<DeliveryOrderCreateLineRequest>();
        foreach (var row in group.Lines)
        {
            if (!DeliveryOrderPresentation.TryScaledQuantity(row.Quantity, out var quantity) || quantity <= 0) continue;
            if (!DeliveryOrderPresentation.TryScaledQuantity(row.Data.AvailableForDeliveryOrderBaseQuantity, out var maximum) || quantity > maximum)
            {
                SetErrorMessage($"Số lượng bàn giao của {row.Data.Sku} không được vượt phần đã đóng gói còn khả dụng.");
                return;
            }
            lines.Add(new DeliveryOrderCreateLineRequest(row.Data.FulfillmentAllocationId, DeliveryOrderPresentation.Quantity(row.Quantity)));
        }

        if (lines.Count == 0)
        {
            SetErrorMessage("Chọn ít nhất một dòng có số lượng lớn hơn 0.");
            return;
        }

        var fingerprint = string.Join("|", lines.Select(line => $"{line.FulfillmentAllocationId}:{line.Quantity}"));
        SetBusy("create");
        ClearMessage();
        try
        {
            var result = await _service.CreateAsync(
                new DeliveryOrderCreateRequest { Lines = lines.ToArray() },
                KeyFor("create", group.Key, fingerprint)).ConfigureAwait(true);
            ActiveTabIndex = 1;
            SetNotice("Đã tạo phiếu giao hàng nháp từ phần hàng đã đóng gói.");
            await LoadAllAsync(result.DeliveryOrder.Id, preserveMessage: true).ConfigureAwait(true);
        }
        catch (Exception exception) { SetError(exception); }
        finally { SetBusy(null); }
    }

    public Task ConfirmAsync() => TransitionAsync("confirm");
    public Task CancelAsync() => TransitionAsync("cancel");
    public Task PickupHandoverAsync() => TransitionAsync("pickup-handover");
    public Task ManualHandoverAsync() => TransitionAsync("manual-handover");
    public Task ReverseInventoryIssueAsync() => TransitionAsync("reverse-inventory-issue");

    private async Task TransitionAsync(string action)
    {
        var order = SelectedOrderDetail;
        if (order is null || IsBusy) return;

        string fingerprint;
        SetBusy(action);
        ClearMessage();
        try
        {
            switch (action)
            {
                case "confirm":
                    if (!CanConfirm) return;
                    fingerprint = order.Revision;
                    await _service.ConfirmAsync(order.Id, KeyFor(action, order.Id, fingerprint)).ConfigureAwait(true);
                    SetNotice("Đã xác nhận phiếu sẵn sàng bàn giao.");
                    break;
                case "cancel":
                    if (!CanCancel) return;
                    if (string.IsNullOrWhiteSpace(CancelReason))
                    {
                        SetErrorMessage("Nhập lý do hủy chứng từ nháp.");
                        return;
                    }
                    fingerprint = CancelReason.Trim();
                    await _service.CancelAsync(
                        order.Id,
                        new DeliveryOrderCancelRequest(CancelReason.Trim()),
                        KeyFor(action, order.Id, fingerprint)).ConfigureAwait(true);
                    ActiveTabIndex = 0;
                    SetNotice("Đã hủy chứng từ nháp; phần hàng đã đóng gói trở lại hàng đợi.");
                    break;
                case "pickup-handover":
                    if (!CanPickupHandover || order.HandoverMode != "PICKUP" || order.Status != "ready_to_dispatch") return;
                    if (string.IsNullOrWhiteSpace(ReceiverName))
                    {
                        SetErrorMessage("Nhập tên người nhận hàng tại quầy.");
                        return;
                    }
                    _pickupAttemptAt ??= DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                    fingerprint = $"{order.Revision}:{ReceiverName.Trim()}:{ReceiverNote.Trim()}:{_pickupAttemptAt}";
                    await _service.PickupHandoverAsync(
                        order.Id,
                        new DeliveryOrderHandoverRequest(ReceiverName.Trim(), EmptyToNull(ReceiverNote), _pickupAttemptAt),
                        KeyFor(action, order.Id, fingerprint)).ConfigureAwait(true);
                    _pickupAttemptAt = null;
                    SetNotice("Đã xác nhận bàn giao tại quầy và ghi xuất kho.");
                    break;
                case "manual-handover":
                    if (!CanManualHandover || order.HandoverMode != "DELIVERY" || order.Status != "ready_to_dispatch") return;
                    if (string.IsNullOrWhiteSpace(ReceiverName))
                    {
                        SetErrorMessage("Nhập tên người nhận hàng giao thủ công.");
                        return;
                    }
                    _manualAttemptAt ??= DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                    fingerprint = $"{order.Revision}:{ReceiverName.Trim()}:{ReceiverNote.Trim()}:{_manualAttemptAt}";
                    await _service.ManualHandoverAsync(
                        order.Id,
                        new DeliveryOrderHandoverRequest(ReceiverName.Trim(), EmptyToNull(ReceiverNote), _manualAttemptAt),
                        KeyFor(action, order.Id, fingerprint)).ConfigureAwait(true);
                    _manualAttemptAt = null;
                    SetNotice("Đã xác nhận giao thủ công, ghi xuất kho và công nợ theo lượng thực giao.");
                    break;
                case "reverse-inventory-issue":
                    if (!CanReverseInventoryIssue || order.Status is not ("dispatched" or "handed_over")) return;
                    if (string.IsNullOrWhiteSpace(ReversalReason))
                    {
                        SetErrorMessage("Nhập lý do đảo xuất kho.");
                        return;
                    }
                    var documentDate = BusinessDate();
                    fingerprint = $"{order.Revision}:{documentDate}:{ReversalReason.Trim()}";
                    await _service.ReverseInventoryIssueAsync(
                        order.Id,
                        new DeliveryOrderReverseIssueRequest(documentDate, "OPERATOR_CORRECTION", ReversalReason.Trim()),
                        KeyFor(action, order.Id, fingerprint)).ConfigureAwait(true);
                    SetNotice("Đã tạo chứng từ đảo và đưa phiếu về trạng thái sẵn sàng.");
                    break;
                default:
                    return;
            }

            await LoadAllAsync(order.Id, preserveMessage: true).ConfigureAwait(true);
        }
        catch (Exception exception) { SetError(exception); }
        finally { SetBusy(null); }
    }

    private async Task<bool> LoadAllAsync(string? preferredOrderId = null, bool preserveMessage = false)
    {
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetErrorMessage(ReadDenied);
            return false;
        }

        var generation = _accessGeneration;
        var request = ++_loadGeneration;
        SetBusy("load");
        if (!preserveMessage) ClearMessage();

        try
        {
            var eligibilityTask = _service.ListEligibilityAsync();
            var ordersTask = _service.ListAsync();
            await Task.WhenAll(eligibilityTask, ordersTask).ConfigureAwait(true);
            if (generation != _accessGeneration || request != _loadGeneration || !CanRead) return false;

            var groups = (await eligibilityTask.ConfigureAwait(true))
                .GroupBy(row => $"{row.SalesOrderId}:{row.SalesOrderVersionId}:{row.WarehouseId}", StringComparer.Ordinal)
                .Select(group => DeliveryOrderPresentation.Group(group))
                .ToArray();
            var orders = (await ordersTask.ConfigureAwait(true))
                .Select(DeliveryOrderPresentation.Order)
                .ToArray();

            var previousGroupKey = SelectedEligibilityGroup?.Key;
            Replace(EligibilityGroups, groups);
            SelectedEligibilityGroup = EligibilityGroups.FirstOrDefault(row => row.Key == previousGroupKey) ?? EligibilityGroups.FirstOrDefault();

            var currentOrderId = preferredOrderId ?? SelectedOrderDetail?.Id ?? SelectedOrder?.Data.Id;
            Replace(Orders, orders);
            if (!_tabInitialized)
            {
                _tabInitialized = true;
                ActiveTabIndex = EligibilityGroups.Count == 0 && Orders.Count > 0 ? 1 : 0;
            }

            var target = Orders.FirstOrDefault(row => row.Data.Id == currentOrderId) ?? Orders.FirstOrDefault();
            _selectedOrder = target;
            OnPropertyChanged(nameof(SelectedOrder));
            if (target is not null) await LoadDetailAsync(target.Data.Id, request).ConfigureAwait(true);
            else SelectedOrderDetail = null;

            _loaded = true;
            RaiseSummary();
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

    private async Task LoadDetailAsync(string deliveryOrderId, long? parentRequest = null)
    {
        if (!CanRead) return;
        var generation = _accessGeneration;
        var request = parentRequest ?? ++_loadGeneration;
        var ownsBusy = parentRequest is null;
        if (ownsBusy) SetBusy($"detail-{deliveryOrderId}");

        try
        {
            var detail = await _service.GetAsync(deliveryOrderId).ConfigureAwait(true);
            if (generation != _accessGeneration || request != _loadGeneration || !CanRead) return;
            SelectedOrderDetail = detail;
        }
        catch (Exception exception)
        {
            if (generation == _accessGeneration && request == _loadGeneration) SetError(exception);
        }
        finally
        {
            if (ownsBusy && request == _loadGeneration && _busyAction == $"detail-{deliveryOrderId}") SetBusy(null);
        }
    }

    private string KeyFor(string prefix, string id, string fingerprint)
    {
        var intent = $"{prefix}:{id}:{fingerprint}";
        if (_intentKeys.TryGetValue(intent, out var existing)) return existing;
        if (_intentKeys.Count >= IdempotencyIntentCacheLimit)
        {
            var oldest = _intentKeys.Keys.FirstOrDefault();
            if (oldest is not null) _intentKeys.Remove(oldest);
        }
        var key = _idempotencyKeys.Create($"delivery-order-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private void Reset()
    {
        EligibilityGroups.Clear();
        Orders.Clear();
        OrderLines.Clear();
        _selectedEligibilityGroup = null;
        _selectedOrder = null;
        _selectedOrderDetail = null;
        _activeTabIndex = 0;
        _cancelReason = string.Empty;
        _receiverName = string.Empty;
        _receiverNote = string.Empty;
        _reversalReason = string.Empty;
        _pickupAttemptAt = null;
        _manualAttemptAt = null;
        ClearMessage();
        RaiseSummary();
        RaiseSelectedOrderState();
        OnPropertyChanged(nameof(SelectedEligibilityGroup));
        OnPropertyChanged(nameof(SelectedOrder));
        OnPropertyChanged(nameof(ActiveTabIndex));
    }

    private void SetBusy(string? action)
    {
        if (string.Equals(_busyAction, action, StringComparison.Ordinal)) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(RefreshButtonText));
        OnPropertyChanged(nameof(CanCreateSelected));
        RaiseSelectedOrderState();
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(HasEligibility));
        OnPropertyChanged(nameof(HasOrders));
        OnPropertyChanged(nameof(EligibleCount));
        OnPropertyChanged(nameof(DraftCount));
        OnPropertyChanged(nameof(ReadyCount));
        OnPropertyChanged(nameof(IssuedCount));
        OnPropertyChanged(nameof(HasSelectedGroup));
        OnPropertyChanged(nameof(HasSelectedOrder));
        OnPropertyChanged(nameof(CanCreateSelected));
    }

    private void RaiseSelectedOrderState()
    {
        OnPropertyChanged(nameof(HasSelectedOrder));
        OnPropertyChanged(nameof(SelectedOrderNumber));
        OnPropertyChanged(nameof(SelectedOrderCustomer));
        OnPropertyChanged(nameof(SelectedOrderSource));
        OnPropertyChanged(nameof(SelectedOrderStatus));
        OnPropertyChanged(nameof(SelectedOrderMode));
        OnPropertyChanged(nameof(SelectedOrderCancellation));
        OnPropertyChanged(nameof(ShowDraftActions));
        OnPropertyChanged(nameof(ShowPickupActions));
        OnPropertyChanged(nameof(ShowManualActions));
        OnPropertyChanged(nameof(ShowReverseActions));
        OnPropertyChanged(nameof(ShowPrint));
        OnPropertyChanged(nameof(CanConfirmSelected));
        OnPropertyChanged(nameof(CanCancelSelected));
        OnPropertyChanged(nameof(CanPickupSelected));
        OnPropertyChanged(nameof(CanManualSelected));
        OnPropertyChanged(nameof(CanReverseSelected));
        OnPropertyChanged(nameof(CanPrintSelected));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanConfirm));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CanPickupHandover));
        OnPropertyChanged(nameof(CanManualHandover));
        OnPropertyChanged(nameof(CanReverseInventoryIssue));
        OnPropertyChanged(nameof(CanCreateSelected));
        RaiseSelectedOrderState();
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

    private void SetError(Exception exception) =>
        SetErrorMessage(exception is CanonicalApiException apiException
            ? CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(apiException), apiException.RequestId)
            : exception.Message);

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BusinessDate()
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        catch
        {
            return DateTimeOffset.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }

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
