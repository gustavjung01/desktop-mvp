using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public sealed class PurchaseOrderViewModel : INotifyPropertyChanged
{
    private const string Read = "core.purchase-order.read";
    private const string Create = "core.purchase-order.create";
    private const string Update = "core.purchase-order.update";
    private const string Submit = "core.purchase-order.submit";
    private const string Approve = "core.purchase-order.approve";
    private const string Cancel = "core.purchase-order.cancel";
    private const string PriceRead = "core.purchase-order.price.read";
    private const string PriceOverride = "core.purchase-order.price.override";

    private readonly IPurchaseOrderService _service;
    private readonly IPartnerService _partners;
    private readonly IInternalOrganizationService _organization;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dispatcher _uiDispatcher;
    private readonly Dictionary<string, string> _actionKeys = new(StringComparer.Ordinal);
    private readonly List<PurchaseOrderData> _allOrders = [];

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _searchText = string.Empty;
    private string _selectedStatus = "all";
    private bool _isEditorOpen;
    private bool _isDetailOpen;
    private bool _isCancelOpen;
    private PurchaseOrderData? _editingOrder;
    private PurchaseOrderData? _detailOrder;
    private PurchaseOrderData? _cancelOrder;
    private string? _draftAttemptKey;
    private string _selectedSupplierId = string.Empty;
    private string _selectedWarehouseId = string.Empty;
    private DateTime? _orderDate = DateTime.Today;
    private DateTime? _expectedDate;
    private string _supplierReference = string.Empty;
    private string _editorNote = string.Empty;
    private string _skuSearchText = string.Empty;
    private string _cancelReason = string.Empty;
    private string _bulkText = string.Empty;
    private string _bulkOverrideReason = string.Empty;
    private string _detailTitle = string.Empty;
    private string _detailStatus = string.Empty;
    private string _detailSupplier = string.Empty;
    private string _detailWarehouse = string.Empty;
    private string _detailDates = string.Empty;
    private string _detailReference = string.Empty;
    private string _detailReceiptSummary = string.Empty;
    private string _detailSubtotal = string.Empty;
    private string _detailDiscount = string.Empty;
    private string _detailTax = string.Empty;
    private string _detailTotal = string.Empty;

    public PurchaseOrderViewModel(
        IPurchaseOrderService service,
        IPartnerService partners,
        IInternalOrganizationService organization,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _partners = partners;
        _organization = organization;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        _uiDispatcher = Dispatcher.CurrentDispatcher;
        StatusOptions.Add(new PurchaseOrderStatusOption("all", "Tất cả trạng thái"));
        foreach (var item in PurchaseOrderPresentation.StatusLabels)
        {
            StatusOptions.Add(new PurchaseOrderStatusOption(item.Key, item.Value));
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

    public ObservableCollection<PurchaseOrderRow> Rows { get; } = [];
    public ObservableCollection<PurchaseOrderStatusOption> StatusOptions { get; } = [];
    public ObservableCollection<PurchaseOrderLookupOption> Suppliers { get; } = [];
    public ObservableCollection<PurchaseOrderLookupOption> Warehouses { get; } = [];
    public ObservableCollection<PurchaseOrderSkuResultRow> SkuResults { get; } = [];
    public ObservableCollection<PurchaseOrderEditorLine> EditorLines { get; } = [];
    public ObservableCollection<PurchaseOrderReceiptRow> DetailReceipts { get; } = [];
    public ObservableCollection<PurchaseOrderDetailLineRow> DetailLines { get; } = [];

    public bool HasSkuResults => SkuResults.Count > 0;

    public bool CanRead => _access.HasPermission(Read);
    public bool CanCreate => _access.HasPermission(Create);
    public bool CanReadPrice => _access.HasPermission(PriceRead);
    public bool CanOverridePrice => _access.HasPermission(PriceOverride);
    public bool CanSaveDraft => !_isBusy && IsEditorOpen && (_editingOrder is null ? CanCreate : _access.HasPermission(Update));
    public bool CanSearchSku => IsEditorOpen && !_isBusy;
    public string CreateButtonText => "Tạo đơn mua hàng";
    public string RefreshButtonText => IsBusy ? "Đang cập nhật…" : "Cập nhật dữ liệu";
    public string SaveButtonText => IsBusy ? "Đang lưu…" : _editingOrder is null ? "Lưu đơn nháp" : "Lưu thay đổi";
    public string EditorTitle => _editingOrder is null ? "Đơn mua hàng mới" : $"Chỉnh sửa {_editingOrder.Number ?? "đơn chưa cấp số"}";
    public string EditorModeText => _editingOrder is null ? "TẠO MỚI" : "CHỈNH SỬA BẢN NHÁP";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!Set(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(RefreshButtonText));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CanSaveDraft));
            OnPropertyChanged(nameof(CanSearchSku));
        }
    }

    public string Message
    {
        get => _message;
        private set => Set(ref _message, value ?? string.Empty);
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => Set(ref _messageIsError, value);
    }

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

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        private set => Set(ref _isEditorOpen, value);
    }

    public bool IsDetailOpen
    {
        get => _isDetailOpen;
        private set => Set(ref _isDetailOpen, value);
    }

    public bool IsCancelOpen
    {
        get => _isCancelOpen;
        private set => Set(ref _isCancelOpen, value);
    }

    public string SelectedSupplierId { get => _selectedSupplierId; set { if (Set(ref _selectedSupplierId, value ?? string.Empty)) MarkDraftChanged(); } }
    public string SelectedWarehouseId { get => _selectedWarehouseId; set { if (Set(ref _selectedWarehouseId, value ?? string.Empty)) MarkDraftChanged(); } }
    public DateTime? OrderDate { get => _orderDate; set { if (Set(ref _orderDate, value)) MarkDraftChanged(); } }
    public DateTime? ExpectedDate { get => _expectedDate; set { if (Set(ref _expectedDate, value)) MarkDraftChanged(); } }
    public string SupplierReference { get => _supplierReference; set { if (Set(ref _supplierReference, value ?? string.Empty)) MarkDraftChanged(); } }
    public string EditorNote { get => _editorNote; set { if (Set(ref _editorNote, value ?? string.Empty)) MarkDraftChanged(); } }
    public string SkuSearchText { get => _skuSearchText; set => Set(ref _skuSearchText, value ?? string.Empty); }
    public string CancelReason { get => _cancelReason; set => Set(ref _cancelReason, value ?? string.Empty); }
    public string BulkText { get => _bulkText; set => Set(ref _bulkText, value ?? string.Empty); }
    public string BulkOverrideReason { get => _bulkOverrideReason; set => Set(ref _bulkOverrideReason, value ?? string.Empty); }

    public string TotalCount => PurchaseOrderPresentation.Number(_allOrders.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string DraftCount => PurchaseOrderPresentation.Number(_allOrders.Count(x => x.Status == "draft").ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string PendingCount => PurchaseOrderPresentation.Number(_allOrders.Count(x => x.Status == "pending_approval").ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string VisibleCountText => $"{Rows.Count} đơn";

    public string DetailTitle { get => _detailTitle; private set => Set(ref _detailTitle, value); }
    public string DetailStatus { get => _detailStatus; private set => Set(ref _detailStatus, value); }
    public string DetailSupplier { get => _detailSupplier; private set => Set(ref _detailSupplier, value); }
    public string DetailWarehouse { get => _detailWarehouse; private set => Set(ref _detailWarehouse, value); }
    public string DetailDates { get => _detailDates; private set => Set(ref _detailDates, value); }
    public string DetailReference { get => _detailReference; private set => Set(ref _detailReference, value); }
    public string DetailReceiptSummary { get => _detailReceiptSummary; private set => Set(ref _detailReceiptSummary, value); }
    public string DetailSubtotal { get => _detailSubtotal; private set => Set(ref _detailSubtotal, value); }
    public string DetailDiscount { get => _detailDiscount; private set => Set(ref _detailDiscount, value); }
    public string DetailTax { get => _detailTax; private set => Set(ref _detailTax, value); }
    public string DetailTotal { get => _detailTotal; private set => Set(ref _detailTotal, value); }

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
            SetError("Tài khoản chưa được cấp quyền xem Đơn mua hàng.");
            return;
        }

        IsBusy = true;
        try
        {
            var ordersTask = _service.ListAsync();
            var suppliersTask = _partners.ListSuppliersAsync();
            var warehousesTask = _organization.ListWarehousesAsync();
            await Task.WhenAll(ordersTask, suppliersTask, warehousesTask).ConfigureAwait(true);

            _allOrders.Clear();
            _allOrders.AddRange(await ordersTask.ConfigureAwait(true));

            Replace(
                Suppliers,
                (await suppliersTask.ConfigureAwait(true))
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new PurchaseOrderLookupOption(x.Id, $"{x.Code} — {x.Name}")));

            Replace(
                Warehouses,
                (await warehousesTask.ConfigureAwait(true))
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new PurchaseOrderLookupOption(x.Id, $"{x.Code} — {x.Name}")));

            ApplyFilter();
            _loaded = true;
            SetNotice("Danh sách đơn mua hàng và dữ liệu tạo đơn đã được cập nhật.");
        }
        catch (CanonicalApiException exception)
        {
            SetError(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId));
        }
        catch (Exception exception)
        {
            SetError(exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void BeginCreate()
    {
        if (!CanCreate)
        {
            SetError("Tài khoản chưa được cấp quyền tạo Đơn mua hàng.");
            return;
        }

        if (Suppliers.Count == 0 || Warehouses.Count == 0)
        {
            SetError("Chưa đủ dữ liệu nhà cung cấp hoặc kho nhận để tạo đơn.");
            return;
        }

        _editingOrder = null;
        SelectedSupplierId = string.Empty;
        SelectedWarehouseId = string.Empty;
        OrderDate = DateTime.Today;
        ExpectedDate = null;
        SupplierReference = string.Empty;
        EditorNote = string.Empty;
        EditorLines.Clear();
        SkuResults.Clear();
        SkuSearchText = string.Empty;
        BulkText = string.Empty;
        BulkOverrideReason = string.Empty;
        _draftAttemptKey = _idempotencyKeys.Create("purchase-order-save");
        IsEditorOpen = true;
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorModeText));
        OnPropertyChanged(nameof(CanSaveDraft));
        Message = string.Empty;
    }

    public async Task BeginEditAsync(PurchaseOrderRow row)
    {
        if (!row.CanEdit) return;
        await LoadEditorAsync(row.Data.Id).ConfigureAwait(true);
    }

    private async Task LoadEditorAsync(string id)
    {
        IsBusy = true;
        try
        {
            var order = await _service.GetAsync(id).ConfigureAwait(true);
            _editingOrder = order;
            _draftAttemptKey = _idempotencyKeys.Create("purchase-order-save");
            _selectedSupplierId = order.SupplierId;
            _selectedWarehouseId = order.WarehouseId;
            _orderDate = DateTime.TryParse(order.PlacedAt, out var placed) ? placed : DateTime.Today;
            _expectedDate = DateTime.TryParse(order.ExpectedAt, out var expected) ? expected : null;
            _supplierReference = order.SupplierReference ?? string.Empty;
            _editorNote = order.Note ?? string.Empty;
            EditorLines.Clear();
            foreach (var line in order.Lines)
            {
                AddEditorLine(new PurchaseOrderEditorLine
                {
                    VariantId = line.VariantId,
                    Sku = line.SkuCode,
                    ItemName = line.ItemName,
                    UnitId = line.UnitId,
                    UnitCode = line.UnitCode,
                    ConversionToBase = line.ConversionToBase,
                    Quantity = line.Quantity,
                    UnitPrice = CanReadPrice ? line.UnitPrice ?? string.Empty : string.Empty,
                    PriceStatus = line.PriceStatus ?? (string.IsNullOrWhiteSpace(line.UnitPrice) ? "NOT_FOUND" : "RESOLVED"),
                    ManualPrice = string.Equals(line.PurchasePriceSource, "MANUAL_OVERRIDE", StringComparison.Ordinal),
                    DiscountMode = line.DiscountMode ?? "TOTAL_AMOUNT",
                    DiscountValue = line.DiscountValue ?? "0",
                    TaxRate = line.TaxRate ?? "0",
                    OverrideReason = line.PriceOverrideReason ?? string.Empty,
                    Note = line.Note ?? string.Empty
                });
            }

            SkuResults.Clear();
            SkuSearchText = string.Empty;
            BulkText = string.Empty;
            BulkOverrideReason = string.Empty;
            IsEditorOpen = true;
            OnPropertyChanged(nameof(SelectedSupplierId));
            OnPropertyChanged(nameof(SelectedWarehouseId));
            OnPropertyChanged(nameof(OrderDate));
            OnPropertyChanged(nameof(ExpectedDate));
            OnPropertyChanged(nameof(SupplierReference));
            OnPropertyChanged(nameof(EditorNote));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(EditorModeText));
            OnPropertyChanged(nameof(CanSaveDraft));
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không tải được chi tiết đơn mua hàng.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CloseEditor()
    {
        if (IsBusy) return;
        IsEditorOpen = false;
        _editingOrder = null;
        _draftAttemptKey = null;
        EditorLines.Clear();
        SkuResults.Clear();
        OnPropertyChanged(nameof(HasSkuResults));
    }

    public async Task SearchSkuAsync()
    {
        if (!CanSearchSku) return;
        var term = SkuSearchText.Trim();
        if (term.Length < 1)
        {
            SkuResults.Clear();
            OnPropertyChanged(nameof(HasSkuResults));
            return;
        }

        IsBusy = true;
        try
        {
            var options = await _service.SearchSkuAsync(term, 30, 0).ConfigureAwait(true);
            Replace(
                SkuResults,
                options.Select(x => new PurchaseOrderSkuResultRow(
                    x,
                    x.Sku,
                    string.IsNullOrWhiteSpace(x.VariantName) ? x.ProductName : $"{x.ProductName} · {x.VariantName}",
                    x.UnitCode ?? x.UnitName ?? "—",
                    x.Eligibility.Message,
                    x.Eligibility.Selectable)));
            OnPropertyChanged(nameof(HasSkuResults));
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không tìm được SKU.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task BrowseSkuAsync()
    {
        if (!CanSearchSku) return;
        IsBusy = true;
        try
        {
            var options = await _service.SearchSkuAsync(string.Empty, 50, 0).ConfigureAwait(true);
            Replace(
                SkuResults,
                options.Select(x => new PurchaseOrderSkuResultRow(
                    x,
                    x.Sku,
                    string.IsNullOrWhiteSpace(x.VariantName) ? x.ProductName : $"{x.ProductName} · {x.VariantName}",
                    x.UnitCode ?? x.UnitName ?? "—",
                    x.Eligibility.Message,
                    x.Eligibility.Selectable)));
            OnPropertyChanged(nameof(HasSkuResults));
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không tải được danh mục SKU mua hàng.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task AddSkuAsync(PurchaseOrderSkuResultRow result)
    {
        if (!result.Selectable || EditorLines.Any(x => x.VariantId == result.Data.Id))
        {
            SetError(result.Selectable ? "SKU này đã có trong đơn." : result.Eligibility);
            return;
        }

        if (string.IsNullOrWhiteSpace(result.Data.UnitId))
        {
            SetError("SKU chưa có đơn vị mua hợp lệ.");
            return;
        }

        var line = new PurchaseOrderEditorLine
        {
            VariantId = result.Data.Id,
            Sku = result.Data.Sku,
            ItemName = string.IsNullOrWhiteSpace(result.Data.VariantName)
                ? result.Data.ProductName
                : $"{result.Data.ProductName} · {result.Data.VariantName}",
            UnitId = result.Data.UnitId,
            UnitCode = result.Data.UnitCode ?? result.Data.UnitName ?? "—",
            ConversionToBase = result.Data.ConversionToBase ?? "1",
            Quantity = "1"
        };
        AddEditorLine(line);
        MarkDraftChanged();
        await ResolvePriceAsync(line).ConfigureAwait(true);
    }

    public void RemoveLine(PurchaseOrderEditorLine line)
    {
        if (EditorLines.Remove(line))
        {
            line.Changed -= EditorLineChanged;
            MarkDraftChanged();
        }
    }

    public async Task ImportBulkAsync(IReadOnlyList<PurchaseOrderBulkInputRow> rows)
    {
        if (rows.Count == 0)
        {
            SetError("Tệp hoặc dữ liệu dán chưa có dòng nào để nhập.");
            return;
        }

        if (rows.Count > 500)
        {
            SetError("Một đơn mua hàng không được vượt quá 500 dòng SKU.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedSupplierId))
        {
            SetError("Vui lòng chọn nhà cung cấp trước khi nhập nhiều dòng.");
            return;
        }

        IsBusy = true;
        try
        {
            var added = 0;
            var problems = new List<string>();
            foreach (var row in rows)
            {
                if (!PurchaseOrderPresentation.TryPositiveDecimal(row.Quantity, out _))
                {
                    problems.Add($"Dòng {row.RowNumber}: số lượng phải lớn hơn 0.");
                    continue;
                }

                var options = await _service.SearchSkuAsync(row.Sku, 20, 0).ConfigureAwait(true);
                var option = options.FirstOrDefault(x =>
                    string.Equals(x.Sku, row.Sku, StringComparison.OrdinalIgnoreCase)
                    && x.Eligibility.Selectable
                    && !string.IsNullOrWhiteSpace(x.UnitId));
                if (option is null)
                {
                    problems.Add($"Dòng {row.RowNumber}: không tìm thấy SKU có thể mua {row.Sku}.");
                    continue;
                }

                if (EditorLines.Any(x => x.VariantId == option.Id))
                {
                    problems.Add($"Dòng {row.RowNumber}: SKU {row.Sku} đã có trong đơn.");
                    continue;
                }

                var line = new PurchaseOrderEditorLine
                {
                    VariantId = option.Id,
                    Sku = option.Sku,
                    ItemName = string.IsNullOrWhiteSpace(option.VariantName) ? option.ProductName : $"{option.ProductName} · {option.VariantName}",
                    UnitId = option.UnitId!,
                    UnitCode = option.UnitCode ?? option.UnitName ?? "—",
                    ConversionToBase = option.ConversionToBase ?? "1",
                    Quantity = row.Quantity,
                    DiscountMode = row.DiscountMode,
                    DiscountValue = row.DiscountValue,
                    TaxRate = row.TaxRate,
                    Note = row.Note
                };

                if (!string.IsNullOrWhiteSpace(row.UnitPrice))
                {
                    if (!CanOverridePrice)
                    {
                        problems.Add($"Dòng {row.RowNumber}: tài khoản chưa được cấp quyền nhập giá mua thủ công.");
                        continue;
                    }

                    if (!PurchaseOrderPresentation.TryPositiveDecimal(row.UnitPrice, out _))
                    {
                        problems.Add($"Dòng {row.RowNumber}: đơn giá thủ công phải lớn hơn 0.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(BulkOverrideReason))
                    {
                        problems.Add("Nhập nhiều dòng có giá thủ công cần lý do áp dụng giá.");
                        break;
                    }

                    line.ManualPrice = true;
                    line.UnitPrice = row.UnitPrice;
                    line.OverrideReason = BulkOverrideReason.Trim();
                    line.PriceStatus = "RESOLVED";
                }
                else
                {
                    await ResolvePriceAsync(line).ConfigureAwait(true);
                }

                AddEditorLine(line);
                added++;
            }

            MarkDraftChanged();
            if (problems.Count == 0)
            {
                SetNotice($"Đã thêm {added} dòng vào đơn mua hàng.");
            }
            else
            {
                SetError($"Đã thêm {added} dòng. {string.Join(" ", problems.Take(6))}{(problems.Count > 6 ? $" Còn {problems.Count - 6} lỗi khác." : string.Empty)}");
            }
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không nhập được dữ liệu nhiều dòng.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ImportBulkTextAsync()
    {
        var rows = PurchaseOrderBulkImport.ParseText(BulkText);
        await ImportBulkAsync(rows).ConfigureAwait(true);
    }

    public async Task RefreshPricesAsync()
    {
        foreach (var line in EditorLines.ToArray())
        {
            if (!line.ManualPrice)
            {
                await ResolvePriceAsync(line).ConfigureAwait(true);
            }
        }
    }

    public async Task ResolvePriceAsync(PurchaseOrderEditorLine line)
    {
        if (string.IsNullOrWhiteSpace(SelectedSupplierId)
            || OrderDate is null
            || string.IsNullOrWhiteSpace(line.UnitId)
            || !PurchaseOrderPresentation.TryPositiveDecimal(line.Quantity, out _))
        {
            line.PriceStatus = "NOT_FOUND";
            if (!line.ManualPrice) line.UnitPrice = string.Empty;
            return;
        }

        try
        {
            var resolution = await _service.ResolvePriceAsync(
                new SupplierPurchasePriceResolveRequest(
                    SelectedSupplierId,
                    line.VariantId,
                    line.UnitId,
                    line.Quantity.Trim(),
                    _editingOrder?.Currency ?? "VND",
                    OrderDate.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)))
                .ConfigureAwait(true);

            if (resolution.Status == "RESOLVED")
            {
                line.PriceStatus = "RESOLVED";
                if (CanReadPrice && resolution.Price is not null)
                {
                    line.UnitPrice = resolution.Price.UnitPrice;
                }
                line.ManualPrice = false;
                line.OverrideReason = string.Empty;
            }
            else
            {
                line.PriceStatus = "NOT_FOUND";
                if (!line.ManualPrice) line.UnitPrice = string.Empty;
            }
        }
        catch
        {
            line.PriceStatus = "NOT_FOUND";
            if (!line.ManualPrice) line.UnitPrice = string.Empty;
        }
    }

    public async Task SaveAsync()
    {
        if (!CanSaveDraft) return;
        var validation = ValidateDraft();
        if (validation is not null)
        {
            SetError(validation);
            return;
        }

        _draftAttemptKey ??= _idempotencyKeys.Create("purchase-order-save");
        var request = new PurchaseOrderDraftRequest
        {
            SupplierId = SelectedSupplierId,
            WarehouseId = SelectedWarehouseId,
            OrderDate = OrderDate!.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ExpectedDate = ExpectedDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            SupplierReference = NullIfWhiteSpace(SupplierReference),
            CurrencyCode = _editingOrder?.Currency ?? "VND",
            Note = NullIfWhiteSpace(EditorNote),
            ExpectedRevision = _editingOrder?.Revision,
            Lines = EditorLines.Select(ToDraftLine).ToArray()
        };

        IsBusy = true;
        try
        {
            var saved = _editingOrder is null
                ? await _service.CreateDraftAsync(request, _draftAttemptKey).ConfigureAwait(true)
                : await _service.UpdateDraftAsync(_editingOrder.Id, request, _draftAttemptKey).ConfigureAwait(true);
            Upsert(saved);
            IsEditorOpen = false;
            _editingOrder = null;
            _draftAttemptKey = null;
            EditorLines.Clear();
            SkuResults.Clear();
            ApplyFilter();
            SetNotice(saved.Number is null ? "Đã lưu đơn mua hàng nháp." : $"Đã cập nhật {saved.Number}.");
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không lưu được đơn mua hàng.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ShowDetailAsync(PurchaseOrderRow row)
    {
        IsBusy = true;
        try
        {
            var detail = await _service.GetAsync(row.Data.Id).ConfigureAwait(true);
            _detailOrder = detail;
            var receipts = await _service.ListReceiptsAsync(detail.Id).ConfigureAwait(true);
            DetailTitle = detail.Number ?? "Đơn chưa cấp số";
            DetailStatus = PurchaseOrderPresentation.Status(detail.Status);
            DetailSupplier = $"{detail.SupplierCode ?? "—"} — {detail.SupplierName}";
            DetailWarehouse = $"{detail.WarehouseCode ?? "—"} — {detail.WarehouseName}";
            DetailDates = $"{PurchaseOrderPresentation.Date(detail.PlacedAt)} → {PurchaseOrderPresentation.Date(detail.ExpectedAt)}";
            DetailReference = detail.SupplierReference ?? "Không có";
            DetailReceiptSummary = $"{receipts.Count} phiếu · Thực nhận {PurchaseOrderPresentation.Number(detail.ReceivedQuantityTotal ?? "0")} · Còn lại {PurchaseOrderPresentation.Number(detail.RemainingQuantityTotal ?? "0")}";
            DetailSubtotal = PurchaseOrderPresentation.Money(detail.Subtotal, detail.Currency);
            DetailDiscount = PurchaseOrderPresentation.Money(detail.DiscountTotal, detail.Currency);
            DetailTax = PurchaseOrderPresentation.Money(detail.TaxTotal, detail.Currency);
            DetailTotal = PurchaseOrderPresentation.Money(detail.Total, detail.Currency);

            Replace(
                DetailReceipts,
                receipts.Select(x => new PurchaseOrderReceiptRow(
                    x.DocumentNumber ?? "Chưa cấp số",
                    PurchaseOrderPresentation.Date(x.ReceiptDate),
                    PurchaseOrderPresentation.Status(x.Status),
                    x.SupplierDeliveryReference ?? "Không có",
                    PurchaseOrderPresentation.Number(x.ReceivedQuantityTotal))));

            Replace(
                DetailLines,
                detail.Lines.Select(x => new PurchaseOrderDetailLineRow(
                    x.SkuCode,
                    x.ItemName,
                    PurchaseOrderPresentation.Number(x.Quantity),
                    PurchaseOrderPresentation.Number(x.ReceivedQuantity ?? "0"),
                    PurchaseOrderPresentation.Number(x.AcceptedQuantity ?? "0"),
                    PurchaseOrderPresentation.Number(x.RejectedQuantity ?? "0"),
                    PurchaseOrderPresentation.Number(x.ShortageClosedQuantity ?? "0"),
                    PurchaseOrderPresentation.Number(x.RemainingQuantity ?? x.Quantity),
                    x.UnitCode,
                    PurchaseOrderPresentation.Number(x.ConversionToBase),
                    CanReadPrice ? PurchaseOrderPresentation.Money(x.UnitPrice, detail.Currency) : "Ẩn theo quyền",
                    CanReadPrice ? PurchaseOrderPresentation.Money(x.DiscountAmount, detail.Currency) : "Ẩn theo quyền",
                    CanReadPrice ? PurchaseOrderPresentation.Money(x.TaxAmount, detail.Currency) : "Ẩn theo quyền",
                    CanReadPrice ? PurchaseOrderPresentation.Money(x.LineTotal, detail.Currency) : "Ẩn theo quyền")));

            IsDetailOpen = true;
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không tải được chi tiết đơn mua hàng.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CloseDetail() => IsDetailOpen = false;

    public async Task SubmitAsync(PurchaseOrderRow row) =>
        await RunActionAsync("submit", row.Data, null).ConfigureAwait(true);

    public async Task ApproveAsync(PurchaseOrderRow row) =>
        await RunActionAsync("approve", row.Data, null).ConfigureAwait(true);

    public void BeginCancel(PurchaseOrderRow row)
    {
        if (!row.CanCancel) return;
        _cancelOrder = row.Data;
        CancelReason = string.Empty;
        IsCancelOpen = true;
    }

    public void CloseCancel()
    {
        if (IsBusy) return;
        IsCancelOpen = false;
        _cancelOrder = null;
        CancelReason = string.Empty;
    }

    public async Task ConfirmCancelAsync()
    {
        if (_cancelOrder is null) return;
        if (string.IsNullOrWhiteSpace(CancelReason))
        {
            SetError("Vui lòng nhập lý do hủy đơn.");
            return;
        }

        var order = _cancelOrder;
        await RunActionAsync("cancel", order, CancelReason.Trim()).ConfigureAwait(true);
        if (!MessageIsError) CloseCancel();
    }

    public async Task<PurchaseOrderData?> GetForPrintAsync(PurchaseOrderRow row)
    {
        try
        {
            return await _service.GetAsync(row.Data.Id).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không tải được dữ liệu in.");
            return null;
        }
    }

    public void MarkDraftChanged()
    {
        if (!IsEditorOpen) return;
        _draftAttemptKey = _idempotencyKeys.Create("purchase-order-save");
    }

    private async Task RunActionAsync(string action, PurchaseOrderData order, string? reason)
    {
        if (IsBusy) return;
        var identity = $"{action}|{order.Id}|{order.Revision}|{reason ?? string.Empty}";
        if (!_actionKeys.TryGetValue(identity, out var key))
        {
            key = _idempotencyKeys.Create($"purchase-order-{action}");
            _actionKeys[identity] = key;
        }

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;
        try
        {
            var updated = action switch
            {
                "submit" => await _service.SubmitAsync(order.Id, order.Revision, key).ConfigureAwait(true),
                "approve" => await _service.ApproveAsync(order.Id, order.Revision, key).ConfigureAwait(true),
                "cancel" => await _service.CancelAsync(order.Id, order.Revision, reason ?? string.Empty, key).ConfigureAwait(true),
                _ => throw new InvalidOperationException("Nghiệp vụ đơn mua hàng không hợp lệ.")
            };

            _actionKeys.Remove(identity);
            Upsert(updated);
            ApplyFilter();
            SetNotice(action switch
            {
                "submit" => "Đơn mua hàng đã được gửi duyệt.",
                "approve" => $"Đơn mua hàng đã được duyệt với số {updated.Number}.",
                _ => "Đơn mua hàng đã được hủy."
            });
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không cập nhật được trạng thái đơn mua hàng.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private PurchaseOrderDraftLineRequest ToDraftLine(PurchaseOrderEditorLine line)
    {
        var baseLine = new PurchaseOrderDraftLineRequest
        {
            VariantId = line.VariantId,
            Quantity = line.Quantity.Trim(),
            Note = line.Note.Trim()
        };
        return line.ManualPrice
            ? baseLine with
            {
                UnitPrice = line.UnitPrice.Trim(),
                DiscountMode = line.DiscountMode,
                DiscountValue = string.IsNullOrWhiteSpace(line.DiscountValue) ? "0" : line.DiscountValue.Trim(),
                TaxRate = string.IsNullOrWhiteSpace(line.TaxRate) ? "0" : line.TaxRate.Trim(),
                PriceOverrideReason = line.OverrideReason.Trim()
            }
            : baseLine;
    }

    private string? ValidateDraft()
    {
        if (string.IsNullOrWhiteSpace(SelectedSupplierId)) return "Vui lòng chọn nhà cung cấp.";
        if (string.IsNullOrWhiteSpace(SelectedWarehouseId)) return "Vui lòng chọn kho nhận.";
        if (OrderDate is null) return "Vui lòng chọn ngày đặt hàng.";
        if (ExpectedDate is not null && ExpectedDate.Value.Date < OrderDate.Value.Date) return "Ngày dự kiến nhận không được trước ngày đặt hàng.";
        if (EditorLines.Count == 0) return "Đơn mua hàng phải có ít nhất một dòng SKU.";
        if (EditorLines.Select(x => x.VariantId).Distinct(StringComparer.Ordinal).Count() != EditorLines.Count) return "Một SKU chỉ được xuất hiện một lần trong đơn.";

        for (var index = 0; index < EditorLines.Count; index++)
        {
            var line = EditorLines[index];
            if (!PurchaseOrderPresentation.TryPositiveDecimal(line.Quantity, out _)) return $"Số lượng dòng {index + 1} phải lớn hơn 0.";
            if (line.ManualPrice)
            {
                if (!CanOverridePrice) return $"Dòng {index + 1}: tài khoản chưa được cấp quyền nhập giá mua thủ công.";
                if (!PurchaseOrderPresentation.TryPositiveDecimal(line.UnitPrice, out _)) return $"Dòng {index + 1}: giá nhập thủ công phải lớn hơn 0.";
                if (string.IsNullOrWhiteSpace(line.OverrideReason)) return $"Dòng {index + 1}: phải nhập lý do thay giá mua.";
            }
            else if (line.PriceStatus != "RESOLVED")
            {
                return $"Dòng {index + 1}: chưa có giá mua hợp lệ. Hãy thiết lập Bảng giá mua hoặc dùng quyền nhập giá thủ công.";
            }
        }

        return null;
    }

    private void ApplyFilter()
    {
        var term = SearchText.Trim();
        var status = SelectedStatus;
        var filtered = _allOrders
            .Where(x => status == "all" || x.Status == status)
            .Where(x =>
            {
                if (term.Length == 0) return true;
                var search = string.Join(" ", new[]
                {
                    x.Number,
                    x.SupplierReference,
                    x.SupplierCode,
                    x.SupplierName,
                    x.WarehouseCode,
                    x.WarehouseName
                }.Where(value => !string.IsNullOrWhiteSpace(value)));
                return search.Contains(term, StringComparison.OrdinalIgnoreCase);
            })
            .OrderByDescending(x => x.UpdatedAt, StringComparer.Ordinal)
            .ToArray();

        Rows.Clear();
        for (var i = 0; i < filtered.Length; i++)
        {
            var order = filtered[i];
            Rows.Add(new PurchaseOrderRow(
                order,
                i + 1,
                order.Number ?? "Chưa cấp số",
                PurchaseOrderPresentation.Date(order.PlacedAt),
                order.SupplierName,
                order.WarehouseName,
                PurchaseOrderPresentation.Number(order.LineCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                CanReadPrice ? PurchaseOrderPresentation.Money(order.Total, order.Currency) : "Ẩn theo quyền",
                PurchaseOrderPresentation.Status(order.Status),
                PurchaseOrderPresentation.Date(order.UpdatedAt),
                CanRead,
                CanRead && !string.IsNullOrWhiteSpace(order.Number),
                order.Status == "draft" && _access.HasPermission(Update),
                order.Status == "draft" && _access.HasPermission(Submit),
                order.Status == "pending_approval" && _access.HasPermission(Approve) && CanReadPrice,
                new[] { "draft", "pending_approval", "approved" }.Contains(order.Status, StringComparer.Ordinal) && _access.HasPermission(Cancel)));
        }

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(DraftCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(VisibleCountText));
    }

    private void Upsert(PurchaseOrderData order)
    {
        var index = _allOrders.FindIndex(x => x.Id == order.Id);
        if (index < 0) _allOrders.Insert(0, order);
        else _allOrders[index] = order;
    }

    private void AddEditorLine(PurchaseOrderEditorLine line)
    {
        line.Changed += EditorLineChanged;
        EditorLines.Add(line);
    }

    private void EditorLineChanged(object? sender, EventArgs e) => MarkDraftChanged();

    private void HandleAccessChanged()
    {
        _loaded = false;
        RaisePermissions();
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanReadPrice));
        OnPropertyChanged(nameof(CanOverridePrice));
        OnPropertyChanged(nameof(CanSaveDraft));
        ApplyFilter();
    }

    private void SetNotice(string message)
    {
        Message = message;
        MessageIsError = false;
    }

    private void SetError(string message)
    {
        Message = message;
        MessageIsError = true;
    }

    private void HandleException(Exception exception, string fallback)
    {
        if (exception is CanonicalApiException api)
        {
            SetError(CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(api), api.RequestId));
            return;
        }

        SetError(string.IsNullOrWhiteSpace(exception.Message) ? fallback : exception.Message);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
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
