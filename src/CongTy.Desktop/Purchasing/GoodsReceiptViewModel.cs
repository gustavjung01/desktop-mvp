using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public sealed class GoodsReceiptViewModel : INotifyPropertyChanged
{
    private const string Read = "core.goods-receipt.read";
    private const string Create = "core.goods-receipt.create";
    private const string Update = "core.goods-receipt.update";
    private const string Post = "core.goods-receipt.post";
    private const string Reverse = "core.goods-receipt.reverse";
    private const string Variance = "core.goods-receipt.variance";
    private const string PurchaseOrderRead = "core.purchase-order.read";

    private readonly IGoodsReceiptService _service;
    private readonly IPurchaseOrderService _purchaseOrders;
    private readonly IInternalOrganizationService _organization;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dispatcher _uiDispatcher;
    private readonly List<GoodsReceiptData> _allReceipts = [];
    private readonly List<PurchaseOrderData> _allPurchaseOrders = [];
    private readonly List<WarehouseLocationData> _locations = [];
    private readonly Dictionary<string, string> _actionKeys = new(StringComparer.Ordinal);

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _searchText = string.Empty;
    private string _selectedStatus = "all";
    private bool _isEditorOpen;
    private bool _isDetailOpen;
    private bool _isReverseOpen;
    private GoodsReceiptData? _editing;
    private GoodsReceiptData? _detail;
    private GoodsReceiptData? _reversing;
    private PurchaseOrderData? _editorPurchaseOrder;
    private string? _draftAttemptKey;
    private string _selectedPurchaseOrderId = string.Empty;
    private DateTime? _receiptDate = DateTime.Today;
    private string _supplierDeliveryReference = string.Empty;
    private string _editorNote = string.Empty;
    private string _editorSupplier = "—";
    private string _editorWarehouse = "—";
    private DateTime? _reverseDate = DateTime.Today;
    private string _reverseReason = string.Empty;
    private string _detailTitle = string.Empty;
    private string _detailStatus = string.Empty;
    private string _detailPurchaseOrder = string.Empty;
    private string _detailSupplier = string.Empty;
    private string _detailWarehouse = string.Empty;
    private string _detailReceiptDate = string.Empty;
    private string _detailReference = string.Empty;
    private string _detailReceived = string.Empty;
    private string _detailAccepted = string.Empty;
    private string _detailRejected = string.Empty;
    private string _detailShortage = string.Empty;
    private string _detailReversal = string.Empty;

    public GoodsReceiptViewModel(
        IGoodsReceiptService service,
        IPurchaseOrderService purchaseOrders,
        IInternalOrganizationService organization,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _purchaseOrders = purchaseOrders;
        _organization = organization;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        _uiDispatcher = Dispatcher.CurrentDispatcher;

        StatusOptions.Add(new GoodsReceiptStatusOption("all", "Tất cả trạng thái"));
        foreach (var pair in GoodsReceiptPresentation.StatusLabels)
        {
            StatusOptions.Add(new GoodsReceiptStatusOption(pair.Key, pair.Value));
        }

        _access.Changed += (_, _) =>
        {
            if (_uiDispatcher.CheckAccess())
            {
                HandleAccessChanged();
                return;
            }

            _ = _uiDispatcher.BeginInvoke((Action)HandleAccessChanged);
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<GoodsReceiptRow> Rows { get; } = [];
    public ObservableCollection<GoodsReceiptStatusOption> StatusOptions { get; } = [];
    public ObservableCollection<GoodsReceiptPurchaseOrderOption> EligiblePurchaseOrders { get; } = [];
    public ObservableCollection<GoodsReceiptEditorLine> EditorLines { get; } = [];
    public ObservableCollection<GoodsReceiptDetailLineRow> DetailLines { get; } = [];

    public bool CanRead => _access.HasPermission(Read);
    public bool CanCreate => _access.HasPermission(Create);
    public bool CanUpdate => _access.HasPermission(Update);
    public bool CanPost => _access.HasPermission(Post);
    public bool CanReverse => _access.HasPermission(Reverse);
    public bool CanVariance => _access.HasPermission(Variance);
    public bool CanReadPurchaseOrders => _access.HasPermission(PurchaseOrderRead);
    public bool CanCreateReceipt => CanCreate && CanReadPurchaseOrders && EligiblePurchaseOrders.Count > 0;
    public bool CanSave => IsEditorOpen && !IsBusy && (_editing is null ? CanCreate : CanUpdate);
    public string RefreshButtonText => IsBusy ? "Đang cập nhật…" : "Cập nhật dữ liệu";
    public string SaveButtonText => IsBusy ? "Đang lưu…" : _editing is null ? "Tạo phiếu" : "Lưu phiếu";
    public string EditorTitle => _editing?.DocumentNumber ?? "Phiếu nhận hàng nháp";
    public string EditorModeText => _editing is null ? "TẠO PHIẾU NHẬN HÀNG" : "SỬA PHIẾU NHẬN HÀNG NHÁP";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!Set(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(RefreshButtonText));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    public string Message { get => _message; private set => Set(ref _message, value ?? string.Empty); }
    public bool MessageIsError { get => _messageIsError; private set => Set(ref _messageIsError, value); }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!Set(ref _searchText, value ?? string.Empty)) return;
            ApplyFilter();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (!Set(ref _selectedStatus, value ?? "all")) return;
            ApplyFilter();
        }
    }

    public bool IsEditorOpen { get => _isEditorOpen; private set { if (Set(ref _isEditorOpen, value)) OnPropertyChanged(nameof(CanSave)); } }
    public bool IsDetailOpen { get => _isDetailOpen; private set => Set(ref _isDetailOpen, value); }
    public bool IsReverseOpen { get => _isReverseOpen; private set => Set(ref _isReverseOpen, value); }

    public string SelectedPurchaseOrderId { get => _selectedPurchaseOrderId; set { if (Set(ref _selectedPurchaseOrderId, value ?? string.Empty)) MarkDraftChanged(); } }
    public DateTime? ReceiptDate { get => _receiptDate; set { if (Set(ref _receiptDate, value)) MarkDraftChanged(); } }
    public string SupplierDeliveryReference { get => _supplierDeliveryReference; set { if (Set(ref _supplierDeliveryReference, value ?? string.Empty)) MarkDraftChanged(); } }
    public string EditorNote { get => _editorNote; set { if (Set(ref _editorNote, value ?? string.Empty)) MarkDraftChanged(); } }
    public string EditorSupplier { get => _editorSupplier; private set => Set(ref _editorSupplier, value); }
    public string EditorWarehouse { get => _editorWarehouse; private set => Set(ref _editorWarehouse, value); }

    public DateTime? ReverseDate { get => _reverseDate; set => Set(ref _reverseDate, value); }
    public string ReverseReason { get => _reverseReason; set => Set(ref _reverseReason, value ?? string.Empty); }

    public string TotalCount => GoodsReceiptPresentation.Number(_allReceipts.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string DraftCount => GoodsReceiptPresentation.Number(_allReceipts.Count(x => x.Status == "draft").ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string PostedCount => GoodsReceiptPresentation.Number(_allReceipts.Count(x => x.Status == "posted").ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string ReversedCount => GoodsReceiptPresentation.Number(_allReceipts.Count(x => x.Status == "reversed").ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string VisibleCountText => $"{Rows.Count} phiếu";

    public string DetailTitle { get => _detailTitle; private set => Set(ref _detailTitle, value); }
    public string DetailStatus { get => _detailStatus; private set => Set(ref _detailStatus, value); }
    public string DetailPurchaseOrder { get => _detailPurchaseOrder; private set => Set(ref _detailPurchaseOrder, value); }
    public string DetailSupplier { get => _detailSupplier; private set => Set(ref _detailSupplier, value); }
    public string DetailWarehouse { get => _detailWarehouse; private set => Set(ref _detailWarehouse, value); }
    public string DetailReceiptDate { get => _detailReceiptDate; private set => Set(ref _detailReceiptDate, value); }
    public string DetailReference { get => _detailReference; private set => Set(ref _detailReference, value); }
    public string DetailReceived { get => _detailReceived; private set => Set(ref _detailReceived, value); }
    public string DetailAccepted { get => _detailAccepted; private set => Set(ref _detailAccepted, value); }
    public string DetailRejected { get => _detailRejected; private set => Set(ref _detailRejected, value); }
    public string DetailShortage { get => _detailShortage; private set => Set(ref _detailShortage, value); }
    public string DetailReversal { get => _detailReversal; private set => Set(ref _detailReversal, value); }
    public bool CanStartSupplierReturnFromDetail => CanRead && _detail?.Status == "posted";

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        if (!CanRead)
        {
            SetError("Tài khoản chưa được cấp quyền xem Phiếu nhận hàng.");
            return;
        }

        IsBusy = true;
        var problems = new List<string>();
        var receiptsLoaded = false;
        try
        {
            var receipts = await _service.ListAsync().ConfigureAwait(true);
            _allReceipts.Clear();
            _allReceipts.AddRange(receipts);
            receiptsLoaded = true;
        }
        catch (Exception exception)
        {
            problems.Add($"Phiếu nhận hàng: {OfficeMessage(exception)}");
        }

        if (CanReadPurchaseOrders)
        {
            try
            {
                var orders = await _purchaseOrders.ListAsync().ConfigureAwait(true);
                _allPurchaseOrders.Clear();
                _allPurchaseOrders.AddRange(orders);
                RebuildEligiblePurchaseOrders();
            }
            catch (Exception exception)
            {
                problems.Add($"Đơn mua hàng: {OfficeMessage(exception)}");
            }
        }
        else
        {
            _allPurchaseOrders.Clear();
            EligiblePurchaseOrders.Clear();
        }

        try
        {
            var locations = await _organization.ListLocationsAsync().ConfigureAwait(true);
            _locations.Clear();
            _locations.AddRange(locations.Where(x => x.IsActive));
        }
        catch (Exception exception)
        {
            problems.Add($"Vị trí kho: {OfficeMessage(exception)}");
        }

        ApplyFilter();
        _loaded = receiptsLoaded;
        if (problems.Count == 0)
        {
            SetNotice("Danh sách phiếu nhận hàng và dữ liệu liên quan đã được cập nhật.");
        }
        else
        {
            SetError($"Một phần dữ liệu chưa cập nhật; đang giữ dữ liệu gần nhất. {string.Join(" ", problems)}");
        }

        IsBusy = false;
    }

    public void BeginCreate()
    {
        if (!CanCreate || !CanReadPurchaseOrders)
        {
            SetError("Tài khoản chưa đủ quyền tạo Phiếu nhận hàng từ đơn mua hàng.");
            return;
        }

        if (EligiblePurchaseOrders.Count == 0)
        {
            SetError("Chưa có đơn mua hàng đã duyệt hoặc đang nhận dở để tạo phiếu.");
            return;
        }

        _editing = null;
        _editorPurchaseOrder = null;
        _selectedPurchaseOrderId = string.Empty;
        _receiptDate = DateTime.Today;
        _supplierDeliveryReference = string.Empty;
        _editorNote = string.Empty;
        EditorSupplier = "—";
        EditorWarehouse = "—";
        ClearEditorLines();
        _draftAttemptKey = _idempotencyKeys.Create("goods-receipt-save");
        IsEditorOpen = true;
        RaiseEditorFields();
        Message = string.Empty;
    }

    public async Task LoadSelectedPurchaseOrderAsync()
    {
        if (!IsEditorOpen || _editing is not null || string.IsNullOrWhiteSpace(SelectedPurchaseOrderId)) return;
        IsBusy = true;
        try
        {
            var orderTask = _purchaseOrders.GetAsync(SelectedPurchaseOrderId);
            var trackingTask = _service.GetTrackingRequirementsAsync(SelectedPurchaseOrderId);
            await Task.WhenAll(orderTask, trackingTask).ConfigureAwait(true);
            var order = await orderTask.ConfigureAwait(true);
            var requirements = await trackingTask.ConfigureAwait(true);

            if (order.Status is not ("approved" or "partially_received"))
            {
                SetError("Đơn mua hàng này không còn ở trạng thái được phép nhận hàng.");
                ClearEditorOrder();
                return;
            }

            var byLine = requirements.ToDictionary(x => x.PurchaseOrderLineId, StringComparer.Ordinal);
            foreach (var line in order.Lines)
            {
                if (!byLine.TryGetValue(line.Id, out var requirement))
                {
                    SetError($"Không xác định được yêu cầu lô/kho cho SKU {line.SkuCode}.");
                    ClearEditorOrder();
                    return;
                }

                if (requirement.TrackingPolicy is null)
                {
                    SetError($"SKU {line.SkuCode} chưa được cấu hình quản lý lô/kho. Cần cấu hình mặt hàng trước khi tạo phiếu nhận hàng.");
                    ClearEditorOrder();
                    return;
                }
            }

            _editorPurchaseOrder = order;
            EditorSupplier = $"{order.SupplierCode ?? "—"} — {order.SupplierName}";
            EditorWarehouse = $"{order.WarehouseCode ?? "—"} — {order.WarehouseName}";
            ClearEditorLines();
            var locations = LocationsFor(order.WarehouseId);
            foreach (var line in order.Lines)
            {
                var remaining = line.RemainingQuantity ?? line.Quantity;
                var requirement = byLine[line.Id];
                AddEditorLine(new GoodsReceiptEditorLine
                {
                    PurchaseOrderLineId = line.Id,
                    LineNumber = line.LineNumber,
                    Sku = line.SkuCode,
                    ItemName = line.ItemName,
                    UnitCode = line.UnitCode,
                    OrderedQuantity = line.Quantity,
                    ReceivedBefore = line.ReceivedQuantity ?? "0",
                    RemainingBefore = remaining,
                    ReceivedQuantity = remaining,
                    AcceptedQuantity = remaining,
                    RejectedQuantity = "0",
                    TrackingPolicy = requirement.TrackingPolicy,
                    Locations = locations,
                    LocationId = requirement.TrackingPolicy!.LocationRequired && locations.Count == 1 ? locations[0].Id : string.Empty
                });
            }

            MarkDraftChanged();
            SetNotice("Đã nạp đơn mua hàng và yêu cầu lô/kho.");
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
            ClearEditorOrder();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task BeginEditAsync(GoodsReceiptRow row)
    {
        if (!row.CanEdit) return;
        IsBusy = true;
        try
        {
            var detailTask = _service.GetAsync(row.Data.Id);
            var orderTask = _purchaseOrders.GetAsync(row.Data.PurchaseOrderId);
            await Task.WhenAll(detailTask, orderTask).ConfigureAwait(true);
            var detail = await detailTask.ConfigureAwait(true);
            var order = await orderTask.ConfigureAwait(true);
            LoadEditor(detail, order);
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadEditor(GoodsReceiptData detail, PurchaseOrderData order)
    {
        _editing = detail;
        _editorPurchaseOrder = order;
        _draftAttemptKey = _idempotencyKeys.Create("goods-receipt-save");
        _selectedPurchaseOrderId = detail.PurchaseOrderId;
        _receiptDate = DateTime.TryParse(detail.ReceiptDate, out var date) ? date : DateTime.Today;
        _supplierDeliveryReference = detail.SupplierDeliveryReference ?? string.Empty;
        _editorNote = detail.Note ?? string.Empty;
        EditorSupplier = $"{detail.SupplierCode ?? "—"} — {detail.SupplierName}";
        EditorWarehouse = $"{detail.WarehouseCode ?? "—"} — {detail.WarehouseName}";
        ClearEditorLines();
        var locations = LocationsFor(detail.WarehouseId);
        foreach (var line in detail.Lines)
        {
            AddEditorLine(new GoodsReceiptEditorLine
            {
                PurchaseOrderLineId = line.PurchaseOrderLineId,
                LineNumber = line.PurchaseOrderLineNumber,
                Sku = line.SkuCode,
                ItemName = line.ItemName,
                UnitCode = line.UnitCode,
                OrderedQuantity = line.OrderedQuantity,
                ReceivedBefore = line.ReceivedQuantityBefore,
                RemainingBefore = line.RemainingQuantityBefore,
                ReceivedQuantity = line.ReceivedQuantity,
                AcceptedQuantity = line.AcceptedQuantity,
                RejectedQuantity = line.RejectedQuantity,
                FinalizeLine = line.FinalizeLine,
                QualityReasonCode = line.QualityReasonCode ?? string.Empty,
                QualityNote = line.QualityNote ?? string.Empty,
                LocationId = line.LocationId ?? string.Empty,
                LotCode = line.LotCode ?? string.Empty,
                ManufacturedDate = DateTime.TryParse(line.ManufacturedDate, out var manufactured) ? manufactured : null,
                ExpiryDate = DateTime.TryParse(line.ExpiryDate, out var expiry) ? expiry : null,
                SupplierLotReference = line.SupplierLotReference ?? string.Empty,
                Note = line.Note ?? string.Empty,
                TrackingPolicy = line.TrackingPolicy,
                Locations = locations
            });
        }

        IsEditorOpen = true;
        RaiseEditorFields();
    }

    public void CloseEditor()
    {
        if (IsBusy) return;
        IsEditorOpen = false;
        _editing = null;
        _editorPurchaseOrder = null;
        _draftAttemptKey = null;
        ClearEditorLines();
    }

    public async Task SaveAsync()
    {
        if (!CanSave) return;
        var error = ValidateDraft();
        if (error is not null)
        {
            SetError(error);
            return;
        }

        _draftAttemptKey ??= _idempotencyKeys.Create("goods-receipt-save");
        var request = new GoodsReceiptDraftRequest
        {
            PurchaseOrderId = SelectedPurchaseOrderId,
            ReceiptDate = ReceiptDate!.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            SupplierDeliveryReference = NullIfWhiteSpace(SupplierDeliveryReference),
            Note = NullIfWhiteSpace(EditorNote),
            ExpectedRevision = _editing?.Revision,
            Lines = EditorLines.Select(ToDraftLine).ToArray()
        };

        IsBusy = true;
        try
        {
            var saved = _editing is null
                ? await _service.CreateAsync(request, _draftAttemptKey).ConfigureAwait(true)
                : await _service.UpdateAsync(_editing.Id, request, _draftAttemptKey).ConfigureAwait(true);
            Upsert(saved);
            IsEditorOpen = false;
            _editing = null;
            _editorPurchaseOrder = null;
            _draftAttemptKey = null;
            ClearEditorLines();
            ApplyFilter();
            SetNotice(saved.DocumentNumber is null ? "Đã lưu phiếu nhận hàng nháp." : $"Đã cập nhật {saved.DocumentNumber}.");
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ShowDetailAsync(GoodsReceiptRow row)
    {
        IsBusy = true;
        try
        {
            var detail = await _service.GetAsync(row.Data.Id).ConfigureAwait(true);
            _detail = detail;
            OnPropertyChanged(nameof(CanStartSupplierReturnFromDetail));
            DetailTitle = detail.DocumentNumber ?? "Phiếu chưa cấp số";
            DetailStatus = GoodsReceiptPresentation.Status(detail.Status);
            DetailPurchaseOrder = detail.PurchaseOrderNumber ?? "Chưa cấp số";
            DetailSupplier = $"{detail.SupplierCode ?? "—"} — {detail.SupplierName}";
            DetailWarehouse = $"{detail.WarehouseCode ?? "—"} — {detail.WarehouseName}";
            DetailReceiptDate = GoodsReceiptPresentation.Date(detail.ReceiptDate);
            DetailReference = detail.SupplierDeliveryReference ?? "Không có";
            DetailReceived = GoodsReceiptPresentation.Number(detail.ReceivedQuantityTotal);
            DetailAccepted = GoodsReceiptPresentation.Number(detail.AcceptedQuantityTotal);
            DetailRejected = GoodsReceiptPresentation.Number(detail.RejectedQuantityTotal);
            DetailShortage = GoodsReceiptPresentation.Number(detail.ShortageClosedQuantityTotal);
            DetailReversal = detail.Status == "reversed"
                ? detail.ReversalReason ?? "Không có lý do"
                : "—";

            DetailLines.Clear();
            foreach (var line in detail.Lines)
            {
                DetailLines.Add(new GoodsReceiptDetailLineRow(
                    line.SkuCode,
                    line.ItemName,
                    GoodsReceiptPresentation.Number(line.OrderedQuantity),
                    GoodsReceiptPresentation.Number(line.ReceivedQuantityBefore),
                    GoodsReceiptPresentation.Number(line.ReceivedQuantity),
                    GoodsReceiptPresentation.Number(line.AcceptedQuantity),
                    GoodsReceiptPresentation.Number(line.RejectedQuantity),
                    GoodsReceiptPresentation.Number(line.ShortageClosedQuantity),
                    GoodsReceiptPresentation.Number(line.RemainingQuantityAfter),
                    LocationDisplay(line.LocationId),
                    line.LotCode ?? "—",
                    GoodsReceiptPresentation.Date(line.ManufacturedDate),
                    GoodsReceiptPresentation.Date(line.ExpiryDate),
                    VarianceDisplay(line),
                    line.Note ?? "—"));
            }

            IsDetailOpen = true;
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CloseDetail()
    {
        IsDetailOpen = false;
        _detail = null;
        OnPropertyChanged(nameof(CanStartSupplierReturnFromDetail));
    }

    public string? GetSupplierReturnSourceId() =>
        CanStartSupplierReturnFromDetail ? _detail?.Id : null;

    public async Task<GoodsReceiptData?> GetForPrintAsync(GoodsReceiptRow row)
    {
        try
        {
            return await _service.GetAsync(row.Data.Id).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
            return null;
        }
    }

    public async Task PreparePostAsync(GoodsReceiptRow row)
    {
        if (!row.CanPost) return;
        IsBusy = true;
        try
        {
            var detail = await _service.GetAsync(row.Data.Id).ConfigureAwait(true);
            var trackingIssue = FindTrackingIssue(detail);
            if (trackingIssue is not null)
            {
                if (CanUpdate && CanReadPurchaseOrders)
                {
                    var order = await _purchaseOrders.GetAsync(detail.PurchaseOrderId).ConfigureAwait(true);
                    LoadEditor(detail, order);
                    SetError($"Chưa thể ghi sổ: {trackingIssue} Phiếu đã được mở để bổ sung.");
                }
                else
                {
                    SetError($"Chưa thể ghi sổ: {trackingIssue}");
                }
                return;
            }

            await PostAsync(detail).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PostAsync(GoodsReceiptData detail)
    {
        var identity = $"post|{detail.Id}|{detail.Revision}";
        if (!_actionKeys.TryGetValue(identity, out var key))
        {
            key = _idempotencyKeys.Create("goods-receipt-post");
            _actionKeys[identity] = key;
        }

        try
        {
            var updated = await _service.PostAsync(detail.Id, detail.Revision, key).ConfigureAwait(true);
            _actionKeys.Remove(identity);
            Upsert(updated);
            ApplyFilter();
            if (_detail?.Id == updated.Id) IsDetailOpen = false;
            SetNotice($"Phiếu nhận hàng đã được ghi sổ với số {updated.DocumentNumber}.");
            await RefreshPurchaseOrdersAsync().ConfigureAwait(true);
        }
        catch
        {
            throw;
        }
    }

    public void BeginReverse(GoodsReceiptRow row)
    {
        if (!row.CanReverse) return;
        _reversing = row.Data;
        ReverseDate = DateTime.Today;
        ReverseReason = string.Empty;
        IsReverseOpen = true;
    }

    public void CloseReverse()
    {
        if (IsBusy) return;
        IsReverseOpen = false;
        _reversing = null;
        ReverseReason = string.Empty;
    }

    public async Task ConfirmReverseAsync()
    {
        if (_reversing is null) return;
        if (ReverseDate is null)
        {
            SetError("Vui lòng chọn ngày đảo phiếu.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ReverseReason))
        {
            SetError("Vui lòng nhập lý do đảo phiếu.");
            return;
        }

        var receipt = _reversing;
        var identity = $"reverse|{receipt.Id}|{receipt.Revision}|{ReverseDate:yyyy-MM-dd}|{ReverseReason.Trim()}";
        if (!_actionKeys.TryGetValue(identity, out var key))
        {
            key = _idempotencyKeys.Create("goods-receipt-reverse");
            _actionKeys[identity] = key;
        }

        IsBusy = true;
        try
        {
            var updated = await _service.ReverseAsync(
                receipt.Id,
                receipt.Revision,
                ReverseDate.Value,
                ReverseReason.Trim(),
                key).ConfigureAwait(true);
            _actionKeys.Remove(identity);
            Upsert(updated);
            ApplyFilter();
            IsReverseOpen = false;
            _reversing = null;
            SetNotice("Phiếu nhận hàng đã được đảo.");
            await RefreshPurchaseOrdersAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void MarkDraftChanged()
    {
        if (!IsEditorOpen) return;
        _draftAttemptKey = _idempotencyKeys.Create("goods-receipt-save");
    }

    private GoodsReceiptDraftLineRequest ToDraftLine(GoodsReceiptEditorLine line)
    {
        string received;
        string accepted;
        string rejected;
        if (CanVariance)
        {
            GoodsReceiptPresentation.TryDecimal(line.AcceptedQuantity, true, out var acceptedValue);
            GoodsReceiptPresentation.TryDecimal(line.RejectedQuantity, true, out var rejectedValue);
            accepted = GoodsReceiptPresentation.ApiDecimal(acceptedValue);
            rejected = GoodsReceiptPresentation.ApiDecimal(rejectedValue);
            received = GoodsReceiptPresentation.ApiDecimal(acceptedValue + rejectedValue);
        }
        else
        {
            GoodsReceiptPresentation.TryDecimal(line.ReceivedQuantity, false, out var receivedValue);
            received = GoodsReceiptPresentation.ApiDecimal(receivedValue);
            accepted = received;
            rejected = "0";
        }

        var varianceReasonRequired = CanVariance && line.VarianceReasonRequired;
        return new GoodsReceiptDraftLineRequest
        {
            PurchaseOrderLineId = line.PurchaseOrderLineId,
            ReceivedQuantity = received,
            AcceptedQuantity = accepted,
            RejectedQuantity = rejected,
            FinalizeLine = CanVariance && line.FinalizeLine,
            QualityReasonCode = varianceReasonRequired ? NullIfWhiteSpace(line.QualityReasonCode)?.ToUpperInvariant() : null,
            QualityNote = varianceReasonRequired ? NullIfWhiteSpace(line.QualityNote) : null,
            LocationId = NullIfWhiteSpace(line.LocationId),
            LotId = null,
            LotCode = line.LotDisabled ? null : NullIfWhiteSpace(line.LotCode),
            ManufacturedDate = line.LotDisabled ? null : line.ManufacturedDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ExpiryDate = line.ExpiryDisabled ? null : line.ExpiryDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            SupplierLotReference = line.LotDisabled ? null : NullIfWhiteSpace(line.SupplierLotReference),
            Note = NullIfWhiteSpace(line.Note)
        };
    }

    private string? ValidateDraft()
    {
        if (string.IsNullOrWhiteSpace(SelectedPurchaseOrderId) || _editorPurchaseOrder is null) return "Vui lòng chọn đơn mua hàng đủ điều kiện.";
        if (ReceiptDate is null) return "Vui lòng chọn ngày nhận.";
        if (EditorLines.Count == 0) return "Đơn mua hàng chưa có dòng có thể nhận.";

        for (var index = 0; index < EditorLines.Count; index++)
        {
            var line = EditorLines[index];
            if (!GoodsReceiptPresentation.TryDecimal(line.RemainingBefore, true, out var remaining)) return $"Dòng {index + 1}: số lượng còn lại không hợp lệ.";

            if (CanVariance)
            {
                if (!GoodsReceiptPresentation.TryDecimal(line.AcceptedQuantity, true, out var accepted)
                    || !GoodsReceiptPresentation.TryDecimal(line.RejectedQuantity, true, out var rejected)
                    || accepted + rejected <= 0)
                {
                    return $"Dòng {index + 1}: phải có số lượng chấp nhận hoặc loại lớn hơn 0.";
                }

                if (accepted > remaining) return $"Dòng {index + 1}: số lượng chấp nhận vượt quá số lượng còn lại.";
                if ((rejected > 0 || line.FinalizeLine)
                    && (string.IsNullOrWhiteSpace(line.QualityReasonCode) || string.IsNullOrWhiteSpace(line.QualityNote)))
                {
                    return $"Dòng {index + 1}: hàng loại hoặc chốt thiếu phải có mã lý do và ghi chú chênh lệch.";
                }
            }
            else
            {
                if (!GoodsReceiptPresentation.TryDecimal(line.ReceivedQuantity, false, out var received))
                {
                    return $"Dòng {index + 1}: số lượng nhận phải lớn hơn 0.";
                }

                if (received > remaining) return $"Dòng {index + 1}: số lượng nhận vượt quá số lượng còn lại.";
            }
        }

        return null;
    }

    private string? FindTrackingIssue(GoodsReceiptData detail)
    {
        foreach (var line in detail.Lines)
        {
            if (!GoodsReceiptPresentation.TryDecimal(line.AcceptedQuantity, true, out var accepted) || accepted <= 0) continue;
            var policy = line.TrackingPolicy;
            if (policy is null) return $"SKU {line.SkuCode} chưa được cấu hình quản lý lô/kho.";
            if (policy.LocationRequired && string.IsNullOrWhiteSpace(line.LocationId)) return $"SKU {line.SkuCode} bắt buộc chọn vị trí kho.";
            if (string.Equals(policy.LotTrackingMode, "REQUIRED", StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(line.LotId)
                && string.IsNullOrWhiteSpace(line.LotCode)) return $"SKU {line.SkuCode} bắt buộc nhập số lô.";
            if (string.Equals(policy.ExpiryTrackingMode, "REQUIRED", StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(line.LotId)
                && string.IsNullOrWhiteSpace(line.ExpiryDate)) return $"SKU {line.SkuCode} bắt buộc nhập hạn sử dụng.";
        }

        return null;
    }

    private async Task RefreshPurchaseOrdersAsync()
    {
        if (!CanReadPurchaseOrders) return;
        try
        {
            var orders = await _purchaseOrders.ListAsync().ConfigureAwait(true);
            _allPurchaseOrders.Clear();
            _allPurchaseOrders.AddRange(orders);
            RebuildEligiblePurchaseOrders();
        }
        catch
        {
            // Giữ dữ liệu PO gần nhất; receipt mutation đã thành công.
        }
    }

    private void RebuildEligiblePurchaseOrders()
    {
        EligiblePurchaseOrders.Clear();
        foreach (var order in _allPurchaseOrders
                     .Where(x => x.Status is "approved" or "partially_received")
                     .OrderByDescending(x => x.UpdatedAt, StringComparer.Ordinal))
        {
            EligiblePurchaseOrders.Add(new GoodsReceiptPurchaseOrderOption(
                order.Id,
                $"{order.Number ?? "Chưa cấp số"} — {order.SupplierName} — {order.WarehouseName}"));
        }

        OnPropertyChanged(nameof(CanCreateReceipt));
    }

    private void ApplyFilter()
    {
        var term = SearchText.Trim();
        var visible = _allReceipts
            .Where(x => SelectedStatus == "all" || x.Status == SelectedStatus)
            .Where(x =>
            {
                if (term.Length == 0) return true;
                var header = string.Join(" ", new[]
                {
                    x.DocumentNumber, x.PurchaseOrderNumber, x.SupplierCode, x.SupplierName,
                    x.WarehouseCode, x.WarehouseName, x.SupplierDeliveryReference
                }.Where(value => !string.IsNullOrWhiteSpace(value)));
                var lines = string.Join(" ", x.Lines.SelectMany(line => new[] { line.SkuCode, line.ItemName, line.LotCode }));
                return $"{header} {lines}".Contains(term, StringComparison.OrdinalIgnoreCase);
            })
            .OrderByDescending(x => x.UpdatedAt, StringComparer.Ordinal)
            .ToArray();

        Rows.Clear();
        for (var i = 0; i < visible.Length; i++)
        {
            var receipt = visible[i];
            Rows.Add(new GoodsReceiptRow(
                receipt,
                i + 1,
                receipt.DocumentNumber ?? "Chưa cấp số",
                receipt.PurchaseOrderNumber ?? "Chưa cấp số",
                $"{receipt.SupplierCode ?? "—"} — {receipt.SupplierName}",
                $"{receipt.WarehouseCode ?? "—"} — {receipt.WarehouseName}",
                GoodsReceiptPresentation.Date(receipt.ReceiptDate),
                GoodsReceiptPresentation.Status(receipt.Status),
                GoodsReceiptPresentation.Number(receipt.LineCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                GoodsReceiptPresentation.Number(receipt.ReceivedQuantityTotal),
                CanRead,
                CanRead,
                receipt.Status == "draft" && CanUpdate && CanReadPurchaseOrders,
                receipt.Status == "draft" && CanPost,
                receipt.Status == "posted" && CanReverse));
        }

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(DraftCount));
        OnPropertyChanged(nameof(PostedCount));
        OnPropertyChanged(nameof(ReversedCount));
        OnPropertyChanged(nameof(VisibleCountText));
    }

    private IReadOnlyList<GoodsReceiptLocationOption> LocationsFor(string warehouseId) =>
        _locations
            .Where(x => x.WarehouseId == warehouseId)
            .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select(x => new GoodsReceiptLocationOption(x.Id, $"{x.Code} — {x.Name}"))
            .ToArray();

    private string LocationDisplay(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "—";
        var location = _locations.FirstOrDefault(x => x.Id == id);
        return location is null ? id : $"{location.Code} — {location.Name}";
    }

    private static string VarianceDisplay(GoodsReceiptLineData line)
    {
        var pieces = new List<string>();
        if (GoodsReceiptPresentation.TryDecimal(line.RejectedQuantity, true, out var rejected) && rejected > 0)
        {
            pieces.Add($"Loại {GoodsReceiptPresentation.Number(line.RejectedQuantity)}");
        }

        if (line.FinalizeLine || (GoodsReceiptPresentation.TryDecimal(line.ShortageClosedQuantity, true, out var shortage) && shortage > 0))
        {
            pieces.Add($"Chốt thiếu {GoodsReceiptPresentation.Number(line.ShortageClosedQuantity)}");
        }

        if (!string.IsNullOrWhiteSpace(line.QualityReasonCode)) pieces.Add(line.QualityReasonCode);
        return pieces.Count == 0 ? "Không" : string.Join(" · ", pieces);
    }

    private void ClearEditorOrder()
    {
        _editorPurchaseOrder = null;
        EditorSupplier = "—";
        EditorWarehouse = "—";
        ClearEditorLines();
    }

    private void AddEditorLine(GoodsReceiptEditorLine line)
    {
        line.Changed += EditorLineChanged;
        EditorLines.Add(line);
    }

    private void ClearEditorLines()
    {
        foreach (var line in EditorLines) line.Changed -= EditorLineChanged;
        EditorLines.Clear();
    }

    private void EditorLineChanged(object? sender, EventArgs e) => MarkDraftChanged();

    private void Upsert(GoodsReceiptData receipt)
    {
        var index = _allReceipts.FindIndex(x => x.Id == receipt.Id);
        if (index < 0) _allReceipts.Insert(0, receipt);
        else _allReceipts[index] = receipt;
    }

    private void HandleAccessChanged()
    {
        _loaded = false;
        foreach (var name in new[]
        {
            nameof(CanRead), nameof(CanCreate), nameof(CanUpdate), nameof(CanPost),
            nameof(CanReverse), nameof(CanVariance), nameof(CanReadPurchaseOrders),
            nameof(CanCreateReceipt), nameof(CanSave), nameof(CanStartSupplierReturnFromDetail)
        })
        {
            OnPropertyChanged(name);
        }

        ApplyFilter();
    }

    private void RaiseEditorFields()
    {
        OnPropertyChanged(nameof(SelectedPurchaseOrderId));
        OnPropertyChanged(nameof(ReceiptDate));
        OnPropertyChanged(nameof(SupplierDeliveryReference));
        OnPropertyChanged(nameof(EditorNote));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorModeText));
        OnPropertyChanged(nameof(CanSave));
    }

    private void SetNotice(string message) { Message = message; MessageIsError = false; }
    private void SetError(string message) { Message = message; MessageIsError = true; }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string OfficeMessage(Exception exception)
    {
        if (exception is CanonicalApiException api)
        {
            return CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(api), api.RequestId);
        }

        return string.IsNullOrWhiteSpace(exception.Message) ? "Không thực hiện được thao tác." : exception.Message;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
