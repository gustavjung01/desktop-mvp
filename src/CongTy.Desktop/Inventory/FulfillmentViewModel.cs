using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed class FulfillmentViewModel : INotifyPropertyChanged
{
    private const int IdempotencyIntentCacheLimit = 256;
    private readonly IFulfillmentService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly List<FulfillmentOrderRow> _allOrders = [];
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private const string ReadAccessDeniedMessage = "Tài khoản chưa được cấp quyền xem Chuẩn bị hàng.";
    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _search = string.Empty;
    private string _selectedChannelId = "all";
    private string _selectedStatusId = "all";
    private string _selectedWarehouseId = "all";
    private FulfillmentOrderRow? _selectedOrder;
    private FulfillmentProductRow? _selectedProduct;
    private FulfillmentSuggestionData? _detail;
    private FulfillmentOrderAllocationResultData? _orderAllocationResult;
    private int _detailRequestVersion;
    private bool _isHoldDetailOpen;
    private bool _isHoldBusy;
    private string _holdSummary = string.Empty;
    private string _holdStatus = string.Empty;

    public FulfillmentViewModel(
        IFulfillmentService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        StatusOptions.Add(new FulfillmentFilterOption("all", "Tất cả"));
        StatusOptions.Add(new FulfillmentFilterOption("waiting", "Chờ phân bổ"));
        StatusOptions.Add(new FulfillmentFilterOption("allocated", "Đã phân bổ"));
        StatusOptions.Add(new FulfillmentFilterOption("picking", "Đang soạn"));
        StatusOptions.Add(new FulfillmentFilterOption("packing", "Đóng gói"));
        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loaded = false;
            ResetSessionData();
            RaisePermissions();
            if (CanRead || !_access.Current.IsAuthenticated)
            {
                if (string.Equals(Message, ReadAccessDeniedMessage, StringComparison.Ordinal)
                    || !_access.Current.IsAuthenticated)
                {
                    ClearMessage();
                }
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FulfillmentOrderRow> VisibleOrders { get; } = [];
    public ObservableCollection<FulfillmentProductRow> ProductRows { get; } = [];
    public ObservableCollection<FulfillmentCandidateRow> CandidateRows { get; } = [];
    public ObservableCollection<FulfillmentAllocationRow> AllocationRows { get; } = [];
    public ObservableCollection<FulfillmentFilterOption> ChannelOptions { get; } = [];
    public ObservableCollection<FulfillmentFilterOption> StatusOptions { get; } = [];
    public ObservableCollection<FulfillmentFilterOption> WarehouseOptions { get; } = [];
    public ObservableCollection<InventoryHoldOrderRow> HoldOrders { get; } = [];

    public bool CanRead => _access.HasPermission("core.fulfillment.read") || _access.HasPermission("core.fulfillment.pick");
    public bool CanAllocateAction => _access.HasPermission("core.fulfillment.allocate");
    public bool CanPickAction => _access.HasPermission("core.fulfillment.pick");
    public bool CanPackAction => _access.HasPermission("core.fulfillment.pack");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoadingWork => _busyAction == "load";
    public bool IsDetailLoading => _busyAction?.StartsWith("detail-", StringComparison.Ordinal) == true;
    public string RefreshButtonText => _busyAction == "load" ? "Đang tải..." : "Làm mới";
    public string AllocateOrderButtonText => _busyAction == "allocate-order" ? "Đang phân bổ..." : "Phân bổ toàn đơn";
    public bool CanAutoAllocateOrder => !IsBusy
        && CanAllocateAction
        && SelectedOrder is not null
        && SelectedOrder.Items.Any(item => FulfillmentPresentation.IsPositive(
            FulfillmentPresentation.QuantityDifference(item.OrderedBaseQuantity, item.AllocatedBaseQuantity)));

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

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public string Search
    {
        get => _search;
        set
        {
            if (SetField(ref _search, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasFilters));
                ApplyFilters();
            }
        }
    }

    public string SelectedChannelId
    {
        get => _selectedChannelId;
        set
        {
            if (SetField(ref _selectedChannelId, string.IsNullOrWhiteSpace(value) ? "all" : value))
            {
                OnPropertyChanged(nameof(HasFilters));
                ApplyFilters();
            }
        }
    }

    public string SelectedStatusId
    {
        get => _selectedStatusId;
        set
        {
            if (SetField(ref _selectedStatusId, string.IsNullOrWhiteSpace(value) ? "all" : value))
            {
                OnPropertyChanged(nameof(HasFilters));
                ApplyFilters();
            }
        }
    }

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            if (SetField(ref _selectedWarehouseId, string.IsNullOrWhiteSpace(value) ? "all" : value))
            {
                OnPropertyChanged(nameof(HasFilters));
                ApplyFilters();
            }
        }
    }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search)
        || SelectedChannelId != "all"
        || SelectedStatusId != "all"
        || SelectedWarehouseId != "all";

    public FulfillmentOrderRow? SelectedOrder
    {
        get => _selectedOrder;
        private set
        {
            if (!SetField(ref _selectedOrder, value)) return;
            OnPropertyChanged(nameof(OrderNumber));
            OnPropertyChanged(nameof(OrderCustomer));
            OnPropertyChanged(nameof(OrderMeta));
            OnPropertyChanged(nameof(OrderTotal));
            OnPropertyChanged(nameof(OrderFinancialBreakdown));
            OnPropertyChanged(nameof(CanAutoAllocateOrder));
        }
    }

    public FulfillmentProductRow? SelectedProduct
    {
        get => _selectedProduct;
        private set
        {
            if (!SetField(ref _selectedProduct, value)) return;
            NotifyProductDetail();
        }
    }

    public string SummaryOrders { get; private set; } = "0/0";
    public string SummaryValue { get; private set; } = "0 ₫";
    public string SummaryWaiting { get; private set; } = "0";
    public string SummaryInProgress { get; private set; } = "0";
    public string SummaryPacked { get; private set; } = "0";
    public string QueueSummary => $"{SummaryOrders} đơn · {SummaryValue}";

    public string OrderNumber => SelectedOrder?.OrderNumber ?? "Đơn chưa có số";
    public string OrderCustomer => SelectedOrder?.CustomerName ?? string.Empty;
    public string OrderMeta
    {
        get
        {
            if (SelectedOrder is null) return string.Empty;
            var item = SelectedOrder.Items.FirstOrDefault();
            if (item is null) return string.Empty;
            var channel = InventoryPresentation.First(item.SalesChannelName, item.SalesChannelCode, "Chưa có kênh bán");
            return $"{item.CustomerCode} · {channel} · {item.WarehouseName} · Giao {FulfillmentPresentation.Date(item.RequestedDeliveryDate)}";
        }
    }
    public string OrderTotal => SelectedOrder?.OrderTotal ?? "Chưa có tổng";
    public string OrderFinancialBreakdown
    {
        get
        {
            var item = SelectedOrder?.Items.FirstOrDefault();
            return item is null
                ? string.Empty
                : $"Tạm tính {FulfillmentPresentation.Money(item.OrderSubtotal)} · CK {FulfillmentPresentation.Money(item.OrderDiscountTotal)} · Thuế {FulfillmentPresentation.Money(item.OrderTaxTotal)}";
        }
    }

    public string OrderAllocationSummary { get; private set; } = string.Empty;
    public bool HasOrderAllocationSummary => !string.IsNullOrWhiteSpace(OrderAllocationSummary);

    public string SelectedProductTitle => SelectedProduct?.Data.ItemName ?? string.Empty;
    public string SelectedProductMeta
    {
        get
        {
            var item = SelectedProduct?.Data;
            return item is null
                ? string.Empty
                : $"{item.Sku} · Khách đặt {FulfillmentPresentation.QuantityWithUnit(item.OrderedQuantity, item.OrderedUnitCode)} · Kho xử lý {FulfillmentPresentation.QuantityWithUnit(item.OrderedBaseQuantity, item.BaseUnitCode)}";
        }
    }
    public string SelectedProductStatus => SelectedProduct?.Status ?? string.Empty;
    public string ProductOnHand => MetricQuantity(_detail?.WarehouseOnHandBaseQuantity, SelectedProduct?.Data.WarehouseOnHandBaseQuantity);
    public string ProductHeldOthers => MetricQuantity(_detail?.WarehouseHeldByOthersBaseQuantity, SelectedProduct?.Data.WarehouseHeldByOthersBaseQuantity);
    public string ProductAvailable => MetricQuantity(_detail?.WarehouseAvailableBaseQuantity, SelectedProduct?.Data.WarehouseAvailableBaseQuantity);
    public string ProductNeed => MetricQuantity(SelectedProduct?.Data.OrderedBaseQuantity);
    public string ProductAllocated => MetricQuantity(SelectedProduct?.Data.AllocatedBaseQuantity);
    public string ProductRemaining => SelectedProduct is null
        ? "0"
        : FulfillmentPresentation.QuantityWithUnit(
            FulfillmentPresentation.QuantityDifference(SelectedProduct.Data.OrderedBaseQuantity, SelectedProduct.Data.AllocatedBaseQuantity),
            SelectedProduct.Data.BaseUnitCode);
    public string CandidateCountText => $"{CandidateRows.Count} vị trí";
    public string AllocationCountText => $"{AllocationRows.Count} dòng";

    public bool IsHoldDetailOpen { get => _isHoldDetailOpen; private set => SetField(ref _isHoldDetailOpen, value); }
    public bool IsHoldBusy { get => _isHoldBusy; private set => SetField(ref _isHoldBusy, value); }
    public string HoldSummary { get => _holdSummary; private set => SetField(ref _holdSummary, value ?? string.Empty); }
    public string HoldStatus { get => _holdStatus; private set => SetField(ref _holdStatus, value ?? string.Empty); }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        if (!CanRead)
        {
            if (_access.Current.IsAuthenticated)
            {
                SetErrorMessage(ReadAccessDeniedMessage);
            }
            return;
        }

        var loadTask = _initialLoadTask ??= LoadWorkAsync();
        try
        {
            _loaded = await loadTask.ConfigureAwait(true);
        }
        finally
        {
            if (ReferenceEquals(_initialLoadTask, loadTask))
            {
                _initialLoadTask = null;
            }
        }
    }

    public async Task RefreshAsync()
    {
        await LoadWorkAsync(SelectedProduct?.Data.FulfillmentDemandId).ConfigureAwait(true);
    }

    public void ResetFilters()
    {
        _search = string.Empty;
        _selectedChannelId = "all";
        _selectedStatusId = "all";
        _selectedWarehouseId = "all";
        OnPropertyChanged(nameof(Search));
        OnPropertyChanged(nameof(SelectedChannelId));
        OnPropertyChanged(nameof(SelectedStatusId));
        OnPropertyChanged(nameof(SelectedWarehouseId));
        OnPropertyChanged(nameof(HasFilters));
        ApplyFilters();
    }

    public async Task SelectOrderAsync(FulfillmentOrderRow? row, string? preferredDemandId = null)
    {
        if (row is null)
        {
            SelectedOrder = null;
            ProductRows.Clear();
            SelectedProduct = null;
            ClearDetail();
            return;
        }

        var changed = SelectedOrder?.SalesOrderId != row.SalesOrderId;
        SelectedOrder = row;
        if (changed)
        {
            _orderAllocationResult = null;
            OrderAllocationSummary = string.Empty;
            OnPropertyChanged(nameof(OrderAllocationSummary));
            OnPropertyChanged(nameof(HasOrderAllocationSummary));
        }
        RebuildProducts();

        var product = !string.IsNullOrWhiteSpace(preferredDemandId)
            ? ProductRows.FirstOrDefault(x => x.Data.FulfillmentDemandId == preferredDemandId)
            : ProductRows.FirstOrDefault();
        await SelectProductAsync(product).ConfigureAwait(true);
    }

    public async Task SelectProductAsync(FulfillmentProductRow? row)
    {
        SelectedProduct = row;
        ClearDetail();
        if (row is null || !CanRead) return;
        await LoadDetailAsync(row).ConfigureAwait(true);
    }

    public async Task AutoAllocateOrderAsync()
    {
        if (!CanAutoAllocateOrder || SelectedOrder is null) return;
        var order = SelectedOrder;
        var fingerprint = string.Join("_", order.Items.Select(item =>
            $"{item.FulfillmentDemandId}.{item.OrderedBaseQuantity}.{item.AllocatedBaseQuantity}"));
        SetBusy("allocate-order");
        ClearMessage();
        try
        {
            var result = await _service.AllocateOrderAsync(
                order.SalesOrderId,
                KeyFor("allocate-order", order.SalesOrderId, fingerprint)).ConfigureAwait(true);
            _orderAllocationResult = result;
            OrderAllocationSummary =
                $"{result.Summary.ReadyLines} dòng đủ · {result.Summary.ShortageLines} dòng chưa đủ hàng · {result.Summary.NeedsAttentionLines} dòng chưa phân bổ hết";
            OnPropertyChanged(nameof(OrderAllocationSummary));
            OnPropertyChanged(nameof(HasOrderAllocationSummary));
            SetNotice($"Phân bổ toàn đơn: {result.Summary.ReadyLines} dòng đủ, {result.Summary.ShortageLines} dòng chưa đủ hàng, {result.Summary.NeedsAttentionLines} dòng chưa phân bổ hết.");
            var focus = result.Lines.FirstOrDefault(line => line.Outcome == "NEEDS_ATTENTION")
                ?? result.Lines.FirstOrDefault(line => line.Outcome == "SHORTAGE");
            await LoadWorkAsync(focus?.FulfillmentDemandId ?? SelectedProduct?.Data.FulfillmentDemandId, preserveMessage: true)
                .ConfigureAwait(true);
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

    public async Task AllocateProductAsync(FulfillmentProductRow? row, bool full)
    {
        if (row is null || !CanAllocateAction || !row.HasRemaining || IsBusy) return;
        var remaining = FulfillmentPresentation.QuantityDifference(row.Data.OrderedBaseQuantity, row.Data.AllocatedBaseQuantity);
        var quantity = row.AllocationQuantity.Trim();
        if (!full)
        {
            if (!FulfillmentPresentation.IsValidPositiveQuantity(quantity))
            {
                SetErrorMessage("Nhập số lượng phân bổ lớn hơn 0.");
                return;
            }
            if (FulfillmentPresentation.IsGreaterThan(quantity, remaining))
            {
                SetErrorMessage($"Số lượng phân bổ không được vượt {FulfillmentPresentation.QuantityWithUnit(remaining, row.Data.BaseUnitCode)}.");
                return;
            }
        }

        var fingerprint = full
            ? $"{row.Data.OrderedBaseQuantity}:{row.Data.AllocatedBaseQuantity}:full"
            : $"{row.Data.AllocatedBaseQuantity}:{quantity}";
        SetBusy($"allocate-{row.Data.FulfillmentDemandId}");
        ClearMessage();
        try
        {
            await _service.AllocateDemandAsync(
                row.Data.FulfillmentDemandId,
                full ? "AUTO" : "QUANTITY",
                full ? null : quantity,
                KeyFor("allocate", row.Data.FulfillmentDemandId, fingerprint)).ConfigureAwait(true);
            row.AllocationQuantity = string.Empty;
            SetNotice(full
                ? $"Đã phân bổ tối đa cho {row.Data.Sku}."
                : $"Đã phân bổ {FulfillmentPresentation.QuantityWithUnit(quantity, row.Data.BaseUnitCode)} cho {row.Data.Sku}.");
            await LoadWorkAsync(row.Data.FulfillmentDemandId, preserveMessage: true).ConfigureAwait(true);
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

    public async Task UpdateProgressAsync(FulfillmentAllocationRow? row, bool pack)
    {
        if (row is null || IsBusy) return;
        if (pack && !CanPackAction) return;
        if (!pack && !CanPickAction) return;

        var quantity = pack ? row.PackRemaining : row.PickRemaining;
        if (!FulfillmentPresentation.IsPositive(quantity)) return;
        var current = pack ? row.Data.PackedBaseQuantity : row.Data.PickedBaseQuantity;
        var action = pack ? "pack" : "pick";
        SetBusy($"{action}-{row.Data.Id}");
        ClearMessage();
        try
        {
            var key = KeyFor(action, row.Data.Id, $"{current}:{quantity}");
            if (pack)
                await _service.PackAsync(row.Data.Id, quantity, key).ConfigureAwait(true);
            else
                await _service.PickAsync(row.Data.Id, quantity, key).ConfigureAwait(true);

            SetNotice(pack
                ? "Đã xác nhận đóng gói phần hàng đã soạn."
                : "Đã xác nhận soạn phần hàng còn lại.");
            await LoadWorkAsync(SelectedProduct?.Data.FulfillmentDemandId, preserveMessage: true).ConfigureAwait(true);
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

    public Task OpenProductHoldsAsync(FulfillmentProductRow? row) =>
        OpenHoldsAsync(row?.Data);

    public Task OpenSelectedProductHoldsAsync() =>
        OpenHoldsAsync(SelectedProduct?.Data);

    public void CloseHolds()
    {
        IsHoldDetailOpen = false;
        HoldStatus = string.Empty;
        HoldOrders.Clear();
    }

    private async Task OpenHoldsAsync(FulfillmentWorkItemData? item)
    {
        if (item is null || IsHoldBusy) return;
        IsHoldDetailOpen = true;
        IsHoldBusy = true;
        HoldOrders.Clear();
        HoldSummary = $"Đơn khác đang giữ: {FulfillmentPresentation.QuantityWithUnit(item.WarehouseHeldByOthersBaseQuantity, item.BaseUnitCode)}";
        HoldStatus = "Đang tải danh sách đơn khác đang giữ hàng…";
        try
        {
            var data = await _service.GetHoldBreakdownAsync(
                item.WarehouseId,
                item.BaseVariantId,
                item.SalesOrderId).ConfigureAwait(true);
            HoldSummary = $"Đơn khác đang giữ: {FulfillmentPresentation.QuantityWithUnit(data.HeldBaseQuantity, item.BaseUnitCode)}";
            Replace(HoldOrders, data.Orders.Select((order, index) =>
            {
                var unit = InventoryPresentation.First(order.BaseUnitName, order.BaseUnitCode, item.BaseUnitCode);
                var sku = order.BaseSku == order.SalesSku || string.IsNullOrWhiteSpace(order.BaseSku)
                    ? order.SalesSku
                    : $"{order.SalesSku} → {order.BaseSku}";
                return new InventoryHoldOrderRow(
                    index + 1,
                    InventoryPresentation.First(order.OrderNumber, "—"),
                    InventoryPresentation.First(order.CustomerName, "Khách hàng"),
                    sku,
                    InventoryPresentation.First(order.WarehouseCode, order.WarehouseName, "—"),
                    InventoryPresentation.HoldFlow(order.DeliveryMode, order.DeliveryExecutionMode),
                    FulfillmentPresentation.QuantityWithUnit(order.HeldBaseQuantity, unit));
            }));
            HoldStatus = HoldOrders.Count == 0 ? "Không có đơn khác đang giữ hàng." : string.Empty;
        }
        catch (Exception exception)
        {
            HoldStatus = ErrorText(exception);
        }
        finally
        {
            IsHoldBusy = false;
        }
    }

    private async Task<bool> LoadWorkAsync(string? preferredDemandId = null, bool preserveMessage = false)
    {
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated)
            {
                SetErrorMessage(ReadAccessDeniedMessage);
            }
            return false;
        }

        var accessGeneration = _accessGeneration;
        SetBusy("load");
        if (!preserveMessage) ClearMessage();
        try
        {
            var work = await _service.ListWorkAsync().ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || !CanRead)
            {
                return false;
            }
            BuildOrderSource(work);
            BuildFilterOptions();
            ApplyFilters(selectFirst: false);

            var preferredOrder = !string.IsNullOrWhiteSpace(preferredDemandId)
                ? _allOrders.FirstOrDefault(order => order.Items.Any(item => item.FulfillmentDemandId == preferredDemandId))
                : null;
            var targetOrder = preferredOrder
                ?? (SelectedOrder is not null ? _allOrders.FirstOrDefault(order => order.SalesOrderId == SelectedOrder.SalesOrderId) : null)
                ?? VisibleOrders.FirstOrDefault();

            SetBusy(null);
            await SelectOrderAsync(targetOrder, preferredDemandId).ConfigureAwait(true);
            return true;
        }
        catch (Exception exception)
        {
            _loaded = false;
            SetError(exception);
            return false;
        }
        finally
        {
            if (_busyAction == "load") SetBusy(null);
        }
    }

    private void ResetSessionData()
    {
        _allOrders.Clear();
        VisibleOrders.Clear();
        ProductRows.Clear();
        CandidateRows.Clear();
        AllocationRows.Clear();
        ChannelOptions.Clear();
        WarehouseOptions.Clear();
        HoldOrders.Clear();
        SelectedOrder = null;
        SelectedProduct = null;
        _detail = null;
        _orderAllocationResult = null;
        _detailRequestVersion++;
        IsHoldDetailOpen = false;
        IsHoldBusy = false;
        HoldSummary = string.Empty;
        HoldStatus = string.Empty;
        OrderAllocationSummary = string.Empty;
        SummaryOrders = "0/0";
        SummaryValue = "0 ₫";
        SummaryWaiting = "0";
        SummaryInProgress = "0";
        SummaryPacked = "0";
        OnPropertyChanged(nameof(OrderAllocationSummary));
        OnPropertyChanged(nameof(HasOrderAllocationSummary));
        OnPropertyChanged(nameof(SummaryOrders));
        OnPropertyChanged(nameof(SummaryValue));
        OnPropertyChanged(nameof(SummaryWaiting));
        OnPropertyChanged(nameof(SummaryInProgress));
        OnPropertyChanged(nameof(SummaryPacked));
        OnPropertyChanged(nameof(QueueSummary));
        OnPropertyChanged(nameof(CandidateCountText));
        OnPropertyChanged(nameof(AllocationCountText));
        NotifyProductDetail();
    }

    private async Task LoadDetailAsync(FulfillmentProductRow row)
    {
        var requestVersion = ++_detailRequestVersion;
        var previousBusy = _busyAction;
        if (previousBusy is null) SetBusy($"detail-{row.Data.FulfillmentDemandId}");
        try
        {
            var detail = await _service.GetSuggestionsAsync(row.Data.FulfillmentDemandId).ConfigureAwait(true);
            if (requestVersion != _detailRequestVersion
                || SelectedProduct?.Data.FulfillmentDemandId != row.Data.FulfillmentDemandId) return;
            _detail = detail;
            RebuildCandidates();
            RebuildAllocations();
            NotifyProductDetail();
        }
        catch (Exception exception)
        {
            if (requestVersion == _detailRequestVersion) SetError(exception);
        }
        finally
        {
            if (previousBusy is null && _busyAction == $"detail-{row.Data.FulfillmentDemandId}") SetBusy(null);
        }
    }

    private void BuildOrderSource(IReadOnlyList<FulfillmentWorkItemData> work)
    {
        var groups = new Dictionary<string, List<FulfillmentWorkItemData>>(StringComparer.Ordinal);
        foreach (var item in work)
        {
            if (!groups.TryGetValue(item.SalesOrderId, out var rows))
            {
                rows = [];
                groups[item.SalesOrderId] = rows;
            }
            rows.Add(item);
        }

        _allOrders.Clear();
        var stt = 1;
        foreach (var pair in groups)
        {
            var items = pair.Value.OrderBy(item => item.LineNumber).ToArray();
            var first = items[0];
            var channel = InventoryPresentation.First(first.SalesChannelCode, "Chưa có kênh");
            var status = FulfillmentPresentation.StatusLabel(first.FulfillmentStatus);
            var search = string.Join(" ", new[]
            {
                first.OrderNumber,
                first.CustomerCode,
                first.CustomerName,
                first.WarehouseCode,
                first.WarehouseName,
                first.SalesChannelCode,
                first.SalesChannelName,
                status
            }.Concat(items.SelectMany(item => new[] { item.Sku, item.ItemName, FulfillmentPresentation.StatusLabel(item.FulfillmentStatus) }))
             .Where(value => !string.IsNullOrWhiteSpace(value))).ToLocaleLowerInvariant();

            _allOrders.Add(new FulfillmentOrderRow(
                stt++,
                pair.Key,
                InventoryPresentation.First(first.OrderNumber, "Đơn chưa có số"),
                first.CustomerName,
                first.CustomerCode,
                FulfillmentPresentation.Money(first.OrderTotal),
                channel,
                $"{first.WarehouseCode} — {first.WarehouseName}",
                FulfillmentPresentation.Date(first.RequestedDeliveryDate),
                status,
                FulfillmentPresentation.StatusBucket(first.FulfillmentStatus),
                items,
                search));
        }
    }

    private void BuildFilterOptions()
    {
        var channels = _allOrders
            .Select(order => order.Items[0])
            .GroupBy(item => string.IsNullOrWhiteSpace(item.SalesChannelCode) ? "__unassigned__" : item.SalesChannelCode.Trim(), StringComparer.Ordinal)
            .Select(group =>
            {
                var item = group.First();
                var label = !string.IsNullOrWhiteSpace(item.SalesChannelName)
                    ? $"{InventoryPresentation.First(item.SalesChannelCode, "Chưa có mã")} — {item.SalesChannelName}"
                    : "Chưa có kênh bán";
                return new FulfillmentFilterOption(group.Key, label);
            })
            .OrderBy(option => option.Label, StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), false))
            .ToArray();

        var warehouses = _allOrders
            .Select(order => order.Items[0])
            .GroupBy(item => item.WarehouseId, StringComparer.Ordinal)
            .Select(group =>
            {
                var item = group.First();
                return new FulfillmentFilterOption(group.Key, $"{item.WarehouseCode} — {item.WarehouseName}");
            })
            .OrderBy(option => option.Label, StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), false))
            .ToArray();

        Replace(ChannelOptions, new[] { new FulfillmentFilterOption("all", "Tất cả kênh") }.Concat(channels));
        Replace(WarehouseOptions, new[] { new FulfillmentFilterOption("all", "Tất cả kho") }.Concat(warehouses));

        if (!ChannelOptions.Any(option => option.Id == SelectedChannelId)) _selectedChannelId = "all";
        if (!WarehouseOptions.Any(option => option.Id == SelectedWarehouseId)) _selectedWarehouseId = "all";
        OnPropertyChanged(nameof(SelectedChannelId));
        OnPropertyChanged(nameof(SelectedWarehouseId));
        OnPropertyChanged(nameof(HasFilters));
    }

    private void ApplyFilters(bool selectFirst = true)
    {
        var term = Search.Trim().ToLocaleLowerInvariant();
        var next = _allOrders.Where(order =>
        {
            var first = order.Items[0];
            var channelMatches = SelectedChannelId == "all"
                || (SelectedChannelId == "__unassigned__"
                    ? string.IsNullOrWhiteSpace(first.SalesChannelCode)
                    : string.Equals(first.SalesChannelCode, SelectedChannelId, StringComparison.Ordinal));
            var statusMatches = SelectedStatusId == "all"
                || string.Equals(order.StatusBucket, SelectedStatusId, StringComparison.Ordinal);
            var warehouseMatches = SelectedWarehouseId == "all"
                || string.Equals(first.WarehouseId, SelectedWarehouseId, StringComparison.Ordinal);
            var termMatches = term.Length == 0 || order.SearchText.Contains(term, StringComparison.Ordinal);
            return channelMatches && statusMatches && warehouseMatches && termMatches;
        }).Select((order, index) => order with { Stt = index + 1 }).ToArray();

        Replace(VisibleOrders, next);
        RecalculateSummary();

        if (!selectFirst) return;
        if (SelectedOrder is not null && next.Any(order => order.SalesOrderId == SelectedOrder.SalesOrderId)) return;
        _ = SelectOrderAsync(next.FirstOrDefault());
    }

    private void RecalculateSummary()
    {
        SummaryOrders = $"{VisibleOrders.Count}/{_allOrders.Count}";
        decimal total = 0;
        foreach (var order in VisibleOrders)
        {
            var raw = order.Items.FirstOrDefault()?.OrderTotal;
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)) total += amount;
        }
        SummaryValue = FulfillmentPresentation.Money(total.ToString(CultureInfo.InvariantCulture));
        SummaryWaiting = VisibleOrders.Count(order => order.StatusBucket == "waiting").ToString(CultureInfo.InvariantCulture);
        SummaryInProgress = VisibleOrders.Count(order => order.StatusBucket is "allocated" or "picking").ToString(CultureInfo.InvariantCulture);
        SummaryPacked = VisibleOrders.Count(order => order.StatusBucket == "packing").ToString(CultureInfo.InvariantCulture);
        OnPropertyChanged(nameof(SummaryOrders));
        OnPropertyChanged(nameof(SummaryValue));
        OnPropertyChanged(nameof(SummaryWaiting));
        OnPropertyChanged(nameof(SummaryInProgress));
        OnPropertyChanged(nameof(SummaryPacked));
        OnPropertyChanged(nameof(QueueSummary));
    }

    private void RebuildProducts()
    {
        if (SelectedOrder is null)
        {
            ProductRows.Clear();
            return;
        }

        var outcomes = _orderAllocationResult?.SalesOrderId == SelectedOrder.SalesOrderId
            ? _orderAllocationResult.Lines.ToDictionary(line => line.FulfillmentDemandId, StringComparer.Ordinal)
            : new Dictionary<string, FulfillmentOrderAllocationLineData>(StringComparer.Ordinal);

        Replace(ProductRows, SelectedOrder.Items.Select((item, index) =>
        {
            outcomes.TryGetValue(item.FulfillmentDemandId, out var outcome);
            return new FulfillmentProductRow(
                index + 1,
                item,
                CanAllocateAction,
                outcome is null ? null : FulfillmentPresentation.OutcomeLabel(outcome.Outcome));
        }));
        OnPropertyChanged(nameof(CanAutoAllocateOrder));
    }

    private void RebuildCandidates()
    {
        var unit = SelectedProduct?.Data.BaseUnitCode;
        Replace(CandidateRows, (_detail?.Candidates ?? [])
            .Take(12)
            .Select((candidate, index) => new FulfillmentCandidateRow(
                index + 1,
                InventoryPresentation.First(candidate.LocationCode, "Chưa có vị trí"),
                InventoryPresentation.First(candidate.LotCode, "Không theo lô"),
                FulfillmentPresentation.QuantityWithUnit(candidate.AvailableBaseQuantity, unit),
                FulfillmentPresentation.CandidateDate(candidate),
                candidate)));
        OnPropertyChanged(nameof(CandidateCountText));
    }

    private void RebuildAllocations()
    {
        var unit = SelectedProduct?.Data.BaseUnitCode;
        Replace(AllocationRows, (_detail?.Allocations ?? []).Select((allocation, index) =>
        {
            var pickRemaining = FulfillmentPresentation.QuantityDifference(allocation.AllocatedBaseQuantity, allocation.PickedBaseQuantity);
            var packRemaining = FulfillmentPresentation.QuantityDifference(allocation.PickedBaseQuantity, allocation.PackedBaseQuantity);
            var location = InventoryPresentation.First(allocation.LocationCode, "Chưa có vị trí");
            var lot = InventoryPresentation.First(allocation.LotCode, "Không theo lô");
            if (!string.IsNullOrWhiteSpace(allocation.ExpiryDate)) lot += $" · HSD {FulfillmentPresentation.Date(allocation.ExpiryDate)}";
            return new FulfillmentAllocationRow(
                index + 1,
                $"{location}{Environment.NewLine}{lot}",
                FulfillmentPresentation.QuantityWithUnit(allocation.AllocatedBaseQuantity, unit),
                FulfillmentPresentation.QuantityWithUnit(allocation.PickedBaseQuantity, unit),
                FulfillmentPresentation.QuantityWithUnit(allocation.PackedBaseQuantity, unit),
                FulfillmentPresentation.AllocationStatus(allocation),
                $"Soạn {FulfillmentPresentation.QuantityWithUnit(pickRemaining, unit)}",
                $"Đóng gói {FulfillmentPresentation.QuantityWithUnit(packRemaining, unit)}",
                pickRemaining,
                packRemaining,
                CanPickAction && FulfillmentPresentation.IsPositive(pickRemaining),
                CanPackAction && FulfillmentPresentation.IsPositive(packRemaining),
                allocation);
        }));
        OnPropertyChanged(nameof(AllocationCountText));
    }

    private void ClearDetail()
    {
        _detailRequestVersion++;
        _detail = null;
        CandidateRows.Clear();
        AllocationRows.Clear();
        OnPropertyChanged(nameof(CandidateCountText));
        OnPropertyChanged(nameof(AllocationCountText));
        NotifyProductDetail();
    }

    private void NotifyProductDetail()
    {
        OnPropertyChanged(nameof(SelectedProductTitle));
        OnPropertyChanged(nameof(SelectedProductMeta));
        OnPropertyChanged(nameof(SelectedProductStatus));
        OnPropertyChanged(nameof(ProductOnHand));
        OnPropertyChanged(nameof(ProductHeldOthers));
        OnPropertyChanged(nameof(ProductAvailable));
        OnPropertyChanged(nameof(ProductNeed));
        OnPropertyChanged(nameof(ProductAllocated));
        OnPropertyChanged(nameof(ProductRemaining));
    }

    private string MetricQuantity(string? preferred, string? fallback = null)
    {
        var item = SelectedProduct?.Data;
        return item is null
            ? "0"
            : FulfillmentPresentation.QuantityWithUnit(
                string.IsNullOrWhiteSpace(preferred) ? fallback : preferred,
                item.BaseUnitCode);
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
        var key = _idempotencyKeys.Create($"fulfillment-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private void SetBusy(string? action)
    {
        if (string.Equals(_busyAction, action, StringComparison.Ordinal)) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoadingWork));
        OnPropertyChanged(nameof(IsDetailLoading));
        OnPropertyChanged(nameof(RefreshButtonText));
        OnPropertyChanged(nameof(AllocateOrderButtonText));
        OnPropertyChanged(nameof(CanAutoAllocateOrder));
    }

    private void ClearMessage()
    {
        Message = string.Empty;
        MessageIsError = false;
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
        SetErrorMessage(ErrorText(exception));

    private static string ErrorText(Exception exception) =>
        exception is CanonicalApiException apiException
            ? CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(apiException), apiException.RequestId)
            : exception.Message;

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanAllocateAction));
        OnPropertyChanged(nameof(CanPickAction));
        OnPropertyChanged(nameof(CanPackAction));
        OnPropertyChanged(nameof(CanAutoAllocateOrder));
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

file static class FulfillmentStringExtensions
{
    public static string ToLocaleLowerInvariant(this string value) =>
        value.ToLower(CultureInfo.GetCultureInfo("vi-VN"));
}
