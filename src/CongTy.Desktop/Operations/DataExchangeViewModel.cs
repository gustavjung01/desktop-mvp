using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Operations;

public sealed partial class DataExchangeViewModel : INotifyPropertyChanged
{
    private const string ProductRead = "core.product.read";
    private const string ProductWrite = "core.product.write";
    private const string TrackingRead = "core.inventory-tracking-policy.read";
    private const string TrackingManage = "core.inventory-tracking-policy.manage";
    private const string PriceRead = "core.price.read";
    private const string PriceWrite = "core.price.write";
    private const string StocktakeRead = "core.stocktake.read";
    private const string StocktakeCreate = "core.stocktake.create";
    private const string StocktakeCount = "core.stocktake.count";
    private const string InventoryRead = "core.inventory.read";
    private const string CustomerRead = "core.customer.read";
    private const int MovementPageSize = 500;
    private const int MovementExportPageSize = 1000;
    private const int MovementExportLimit = 100_000;

    private readonly IDataExchangeService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, OperationKeyState> _operationKeys = new(StringComparer.Ordinal);

    private ProductData[] _products = [];
    private ProductCategoryData[] _categories = [];
    private ProductBrandData[] _brands = [];
    private ProductUnitData[] _units = [];
    private PriceListData[] _priceLists = [];
    private SalesChannelData[] _channels = [];
    private DataExchangeCustomerGroupData[] _groups = [];
    private DataExchangeCustomerData[] _customers = [];
    private InventoryBalanceData[] _balances = [];

    private bool _loaded;
    private bool _isBusy;
    private bool _messageIsError;
    private string _message = string.Empty;
    private int _selectedTabIndex;
    private string? _pendingKind;
    private string? _pendingFileName;
    private string? _pendingFormat;
    private string? _importOperationKey;
    private string _selectedPriceListId = string.Empty;
    private string _selectedWarehouseId = string.Empty;
    private string _quotationScope = "all";
    private string _quotationCategoryId = string.Empty;
    private string _quotationSkus = string.Empty;
    private string _quotationChannelId = string.Empty;
    private string _quotationCustomerGroupId = string.Empty;
    private string _quotationCustomerId = string.Empty;
    private string _quotationQuantity = "1";
    private DataExchangeBalanceChoice? _selectedBalance;
    private bool _movementHasMore;
    private string _bulkField = "categoryCode";
    private string _bulkValue = string.Empty;
    private bool _suppressPendingChange;

    public DataExchangeViewModel(
        IDataExchangeService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        foreach (var column in DataExchangePresentation.ProductColumns)
            ProductExportColumns.Add(new DataExchangeExportColumn(column, DataExchangePresentation.Label(column), true));

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            IsBusy = false;
            ClearReferenceData();
            ClearPendingImport();
            QuotationRows.Clear();
            MovementRows.Clear();
            SelectedBalance = null;
            Message = string.Empty;
            RaisePermissions();
            if (_access.Current.IsAuthenticated && CanOpen) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DataExchangeChoice> PriceLists { get; } = [];
    public ObservableCollection<DataExchangeChoice> Warehouses { get; } = [];
    public ObservableCollection<DataExchangeChoice> Categories { get; } = [];
    public ObservableCollection<DataExchangeChoice> Channels { get; } = [];
    public ObservableCollection<DataExchangeChoice> CustomerGroups { get; } = [];
    public ObservableCollection<DataExchangeChoice> Customers { get; } = [];
    public ObservableCollection<DataExchangeChoice> Units { get; } = [];
    public ObservableCollection<DataExchangeBalanceChoice> BalanceChoices { get; } = [];
    public ObservableCollection<DataExchangeImportRow> PendingRows { get; } = [];
    public ObservableCollection<DataExchangeQuotationRow> QuotationRows { get; } = [];
    public ObservableCollection<DataExchangeMovementRow> MovementRows { get; } = [];
    public ObservableCollection<DataExchangeExportColumn> ProductExportColumns { get; } = [];

    public IReadOnlyList<DataExchangeChoice> BooleanChoices { get; } =
    [
        new("CÓ", "Có"),
        new("KHÔNG", "Không")
    ];

    public IReadOnlyList<DataExchangeChoice> VariantChoices { get; } =
    [
        new("BASE", "Đơn vị lẻ"),
        new("CARTON", "Thùng"),
        new("OTHER", "Quy cách khác")
    ];

    public IReadOnlyList<DataExchangeChoice> LotChoices { get; } =
    [
        new("CÓ", "Có"),
        new("KHÔNG", "Không")
    ];

    public IReadOnlyList<DataExchangeChoice> ExpiryChoices { get; } =
    [
        new("KHÔNG", "Không quản lý"),
        new("TÙY CHỌN", "Có thể nhập"),
        new("BẮT BUỘC", "Bắt buộc nhập")
    ];

    public IReadOnlyList<DataExchangeChoice> QuotationScopes { get; } =
    [
        new("all", "Tất cả SKU đang bán"),
        new("category", "Theo ngành hoặc nhóm"),
        new("sku", "Danh sách SKU")
    ];

    public IReadOnlyList<DataExchangeChoice> BulkFields { get; } =
    [
        new("categoryCode", "Loại sản phẩm"),
        new("brandCode", "Nhãn hàng"),
        new("variantKind", "Loại SKU"),
        new("unitCode", "Đơn vị tính"),
        new("conversionToBase", "Hệ số quy đổi"),
        new("lotTrackingMode", "Quản lý lô"),
        new("expiryTrackingMode", "Hạn sử dụng"),
        new("locationRequired", "Vị trí kho"),
        new("productIsCatalogVisible", "Hiển thị sản phẩm"),
        new("productIsOrderable", "Cho đặt hàng"),
        new("productIsActive", "Sản phẩm sử dụng"),
        new("isSellable", "Cho bán SKU"),
        new("isCatalogVisible", "Hiển thị SKU"),
        new("isActive", "SKU sử dụng")
    ];

    public bool CanOpen =>
        CanExportProducts || CanImportProducts || CanExportPricing || CanImportPricing
        || CanExportStocktake || CanImportStocktake || CanQuotation || CanMovements;

    public bool CanExportProducts => _access.HasPermission(ProductRead) && _access.HasPermission(TrackingRead);
    public bool CanImportProducts => _access.HasPermission(ProductWrite) && _access.HasPermission(TrackingManage);
    public bool CanExportPricing => _access.HasPermission(PriceRead);
    public bool CanImportPricing => _access.HasPermission(PriceWrite);
    public bool CanExportStocktake => _access.HasPermission(StocktakeRead);
    public bool CanImportStocktake => _access.HasPermission(StocktakeCreate) && _access.HasPermission(StocktakeCount);
    public bool CanQuotation => _access.HasPermission(ProductRead) && _access.HasPermission(PriceRead);
    public bool CanMovements => _access.HasPermission(InventoryRead);

    public bool CanUseProductExport => CanExportProducts && !IsBusy;
    public bool CanUseProductImport => CanImportProducts && !IsBusy;
    public bool CanUsePricingExport => CanExportPricing && !IsBusy && !string.IsNullOrWhiteSpace(SelectedPriceListId);
    public bool CanUsePricingImport => CanImportPricing && !IsBusy && !string.IsNullOrWhiteSpace(SelectedPriceListId);
    public bool CanUseStocktakeExport => CanExportStocktake && !IsBusy && !string.IsNullOrWhiteSpace(SelectedWarehouseId);
    public bool CanUseStocktakeImport => CanImportStocktake && !IsBusy;
    public bool CanUseQuotation => CanQuotation && !IsBusy;
    public bool CanUseQuotationExport => !IsBusy && QuotationRows.Count > 0;
    public bool CanUseMovements => CanMovements && !IsBusy && SelectedBalance is not null;
    public bool CanLoadMoreMovements => CanMovements && !IsBusy && MovementHasMore && SelectedBalance is not null;
    public bool CanConfirmImport => !IsBusy && PendingRows.Count > 0 && PendingKind switch
    {
        "products" => CanImportProducts,
        "pricing" => CanImportPricing && !string.IsNullOrWhiteSpace(SelectedPriceListId),
        "stocktake" => CanImportStocktake,
        _ => false
    };

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
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

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaisePermissions();
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (!SetField(ref _selectedTabIndex, Math.Clamp(value, 0, 5))) return;
            ClearPendingImport();
            SetMessage(string.Empty);
            OnPropertyChanged(nameof(IsOfficeFormsTab));
            OnPropertyChanged(nameof(CanRefresh));
            if (!IsOfficeFormsTab && _access.Current.IsAuthenticated && CanOpen)
                _ = EnsureLoadedAsync();
        }
    }

    public bool IsOfficeFormsTab => SelectedTabIndex == 5;
    public bool CanRefresh => CanOpen && !IsOfficeFormsTab && !IsBusy;

    public string? PendingKind
    {
        get => _pendingKind;
        private set
        {
            if (!SetField(ref _pendingKind, value)) return;
            OnPropertyChanged(nameof(ShowProductPreview));
            OnPropertyChanged(nameof(ShowPricingPreview));
            OnPropertyChanged(nameof(ShowStocktakePreview));
            OnPropertyChanged(nameof(CanConfirmImport));
        }
    }

    public string? PendingFileName
    {
        get => _pendingFileName;
        private set => SetField(ref _pendingFileName, value);
    }

    public bool ShowProductPreview => PendingKind == "products";
    public bool ShowPricingPreview => PendingKind == "pricing";
    public bool ShowStocktakePreview => PendingKind == "stocktake";

    public string SelectedPriceListId
    {
        get => _selectedPriceListId;
        set
        {
            if (!SetField(ref _selectedPriceListId, value ?? string.Empty)) return;
            if (PendingKind == "pricing") ClearPendingImport();
            OnPropertyChanged(nameof(CanUsePricingExport));
            OnPropertyChanged(nameof(CanUsePricingImport));
            OnPropertyChanged(nameof(CanConfirmImport));
        }
    }

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            if (!SetField(ref _selectedWarehouseId, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanUseStocktakeExport));
        }
    }

    public string QuotationScope
    {
        get => _quotationScope;
        set
        {
            if (!SetField(ref _quotationScope, value ?? "all")) return;
            OnPropertyChanged(nameof(IsQuotationCategoryScope));
            OnPropertyChanged(nameof(IsQuotationSkuScope));
        }
    }

    public bool IsQuotationCategoryScope => QuotationScope == "category";
    public bool IsQuotationSkuScope => QuotationScope == "sku";
    public string QuotationCategoryId { get => _quotationCategoryId; set => SetField(ref _quotationCategoryId, value ?? string.Empty); }
    public string QuotationSkus { get => _quotationSkus; set => SetField(ref _quotationSkus, value ?? string.Empty); }
    public string QuotationChannelId { get => _quotationChannelId; set => SetField(ref _quotationChannelId, value ?? string.Empty); }
    public string QuotationCustomerGroupId { get => _quotationCustomerGroupId; set => SetField(ref _quotationCustomerGroupId, value ?? string.Empty); }
    public string QuotationCustomerId { get => _quotationCustomerId; set => SetField(ref _quotationCustomerId, value ?? string.Empty); }
    public string QuotationQuantity { get => _quotationQuantity; set => SetField(ref _quotationQuantity, value ?? string.Empty); }

    public DataExchangeBalanceChoice? SelectedBalance
    {
        get => _selectedBalance;
        set
        {
            if (!SetField(ref _selectedBalance, value)) return;
            MovementRows.Clear();
            MovementHasMore = false;
            OnPropertyChanged(nameof(SelectedOnHand));
            OnPropertyChanged(nameof(SelectedReserved));
            OnPropertyChanged(nameof(SelectedAvailable));
            OnPropertyChanged(nameof(CanUseMovements));
            OnPropertyChanged(nameof(CanLoadMoreMovements));
        }
    }

    public string SelectedOnHand => SelectedBalance is null ? "—" : DataExchangePresentation.TrimDecimal(SelectedBalance.Balance.OnHandQuantity);
    public string SelectedReserved => SelectedBalance is null ? "—" : DataExchangePresentation.TrimDecimal(SelectedBalance.Balance.ReservedQuantity);
    public string SelectedAvailable => SelectedBalance is null ? "—" : DataExchangePresentation.TrimDecimal(SelectedBalance.Balance.AvailableQuantity);

    public bool MovementHasMore
    {
        get => _movementHasMore;
        private set
        {
            if (!SetField(ref _movementHasMore, value)) return;
            OnPropertyChanged(nameof(CanLoadMoreMovements));
        }
    }

    public string BulkField { get => _bulkField; set => SetField(ref _bulkField, value ?? "categoryCode"); }
    public string BulkValue { get => _bulkValue; set => SetField(ref _bulkValue, value ?? string.Empty); }
    public int SelectedPendingCount => PendingRows.Count(row => row.IsSelected);

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || IsBusy || !CanOpen || IsOfficeFormsTab) return;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (IsBusy || !CanOpen || IsOfficeFormsTab) return;
        Begin();
        try
        {
            if (_access.HasPermission(ProductRead) || CanImportProducts)
            {
                var productTask = _service.ListProductsAsync();
                var categoryTask = _service.ListCategoriesAsync();
                var brandTask = _service.ListBrandsAsync();
                var unitTask = _service.ListUnitsAsync();
                await Task.WhenAll(productTask, categoryTask, brandTask, unitTask);
                _products = [.. await productTask];
                _categories = [.. await categoryTask];
                _brands = [.. await brandTask];
                _units = [.. await unitTask];
            }

            if (_access.HasPermission(PriceRead) || CanImportPricing)
            {
                var priceTask = _service.ListPriceListsAsync();
                var channelTask = _service.ListSalesChannelsAsync();
                await Task.WhenAll(priceTask, channelTask);
                _priceLists = [.. await priceTask];
                _channels = [.. await channelTask];
            }

            if (_access.HasPermission(CustomerRead))
            {
                var groupTask = _service.ListCustomerGroupsAsync();
                var customerTask = _service.ListCustomersAsync();
                await Task.WhenAll(groupTask, customerTask);
                _groups = [.. await groupTask];
                _customers = [.. await customerTask];
            }

            if (_access.HasPermission(InventoryRead))
                _balances = [.. await _service.ListBalancesAsync()];

            RebuildChoices();
            _loaded = true;
            SetMessage("Đã cập nhật dữ liệu nền.");
        }
        catch (Exception exception)
        {
            Fail(exception, "Không tải được dữ liệu nền.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task PrepareImportAsync(string kind, string filePath)
    {
        if (IsBusy) return;
        EnsureImportPermission(kind);
        Begin();
        ClearPendingImport();
        try
        {
            var required = kind switch
            {
                "products" => DataExchangePresentation.ProductRequiredColumns,
                "pricing" => DataExchangePresentation.PricingColumns,
                "stocktake" => DataExchangePresentation.StocktakeColumns,
                _ => throw new ArgumentException("Nhóm dữ liệu nhập không hợp lệ.", nameof(kind))
            };
            var rows = await DataExchangeFileHelper.ReadAsync(filePath, required);
            _suppressPendingChange = true;
            try
            {
                foreach (var values in rows)
                {
                    var row = DataExchangeImportRow.From(values);
                    row.Changed += PendingRowChanged;
                    row.PropertyChanged += PendingRowPropertyChanged;
                    PendingRows.Add(row);
                }
            }
            finally
            {
                _suppressPendingChange = false;
            }

            PendingKind = kind;
            PendingFileName = Path.GetFileName(filePath);
            _pendingFormat = filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? "xlsx" : "csv";
            _importOperationKey = _idempotencyKeys.Create(ImportScope(kind));
            SetMessage($"Đã đọc {PendingRows.Count} dòng từ “{PendingFileName}”. Kiểm tra bảng xem trước rồi bấm Xác nhận nhập.");
            RaisePermissions();
        }
        catch (Exception exception)
        {
            ClearPendingImport();
            Fail(exception, "Không đọc được tệp dữ liệu.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CancelPendingImport()
    {
        ClearPendingImport();
        SetMessage(string.Empty);
    }

    public async Task ConfirmPendingImportAsync()
    {
        if (!CanConfirmImport || PendingKind is null || _pendingFormat is null) return;
        var operationKey = _importOperationKey ??= _idempotencyKeys.Create(ImportScope(PendingKind));
        Begin();
        try
        {
            var rows = PendingRows.Select(row => row.ToDictionary()).ToArray();
            if (PendingKind == "products")
            {
                ValidateProductRows(PendingRows);
                var result = await _service.ImportProductsAsync(_pendingFormat, rows, operationKey);
                var imported = result.Import?.Imported ?? PendingRows.Count;
                var configured = result.Onboarding?.VariantsConfigured ?? 0;
                var policies = result.Onboarding?.PoliciesConfigured ?? 0;
                ClearPendingImport();
                await RefreshAfterImportAsync();
                SetMessage($"Đã nhập {imported} sản phẩm/SKU; đã gắn đơn vị cho {configured} SKU và thiết lập chính sách kho cho {policies} SKU.");
                return;
            }

            if (PendingKind == "pricing")
            {
                var list = SelectedPriceList();
                if (PendingRows.Count > 2000) throw new InvalidOperationException("Mỗi lần chỉ nhập tối đa 2.000 dòng giá.");
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var items = new List<DataExchangePricingImportItemRequest>();
                for (var index = 0; index < PendingRows.Count; index++)
                {
                    var row = PendingRows[index];
                    var sku = row.Sku.Trim().ToUpperInvariant();
                    var amount = row.AmountMinor.Trim();
                    if (sku.Length == 0) throw new InvalidOperationException($"Dòng {index + 2}: SKU đang trống.");
                    if (!seen.Add(sku)) throw new InvalidOperationException($"Dòng {index + 2}: SKU {sku} bị lặp trong file.");
                    if (!IntegerAmount().IsMatch(amount)) throw new InvalidOperationException($"Dòng {index + 2} · SKU {sku}: Giá bán phải là số nguyên không âm.");
                    items.Add(new DataExchangePricingImportItemRequest(
                        list.Code, sku, "FIXED_PRICE", amount, "0", null, null, null, "IMPORT", null, true));
                }

                var request = new DataExchangePricingImportRequest(true, operationKey, items);
                var result = await _service.ImportPricingAsync(request, operationKey);
                ClearPendingImport();
                SetMessage($"Đã cập nhật {result.ItemsUpdated} SKU, tạo mới {result.ItemsCreated} dòng giá theo SKU trong {list.Code}.");
                return;
            }

            if (PendingRows.Count > 500) throw new InvalidOperationException("Mỗi đợt kiểm kê tối đa 500 dòng.");
            for (var index = 0; index < PendingRows.Count; index++)
                _ = ExactQuantity(PendingRows[index].ActualCount, $"Dòng {index + 2} · Số đếm thực tế", 12);

            var stocktake = await _service.ImportStocktakeAsync(_pendingFormat, rows, operationKey);
            var number = stocktake.Stocktake.StocktakeNumber;
            ClearPendingImport();
            SetMessage($"Đã tạo phiếu kiểm kê {number} và ghi số đếm. Chưa gửi duyệt, chưa ghi sổ tồn.");
        }
        catch (Exception exception)
        {
            Fail(exception, "Không nhập được dữ liệu.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void WriteProductTemplate(string format, string filePath)
    {
        DataExchangeFileHelper.Write(filePath, "Sản phẩm SKU", DataExchangePresentation.ProductColumns, [], format);
        SetMessage($"Đã tạo mẫu {format.ToUpperInvariant()} cho sản phẩm/SKU.");
    }

    public void WritePricingTemplate(string format, string filePath)
    {
        DataExchangeFileHelper.Write(filePath, "Mẫu cập nhật giá", DataExchangePresentation.PricingColumns, [], format);
        SetMessage($"Đã tạo mẫu {format.ToUpperInvariant()} gồm đúng 2 cột: SKU và Giá bán (VND).");
    }

    public async Task ExportProductsAsync(string format, string filePath)
    {
        if (!CanUseProductExport) return;
        Begin();
        var fingerprint = format;
        try
        {
            var key = OperationKey("product-export", "product-export", fingerprint);
            var result = await _service.ExportProductsAsync(format, key);
            var selected = ProductExportColumns.Where(item => item.IsSelected)
                .Select(item => item.Id)
                .Where(column => result.Columns.Contains(column, StringComparer.Ordinal))
                .ToArray();
            if (selected.Length == 0) throw new InvalidOperationException("Chọn ít nhất một thông tin để xuất.");

            var rows = result.Rows.Select(row => selected.Select(column =>
                row.TryGetValue(column, out var value) ? DataExchangePresentation.DisplayCell(column, value) : string.Empty).ToArray()).ToArray();
            DataExchangeFileHelper.Write(filePath, "Sản phẩm SKU", selected, rows, format);
            CompleteOperation("product-export");
            SetMessage($"Đã xuất {rows.Length} dòng SKU.");
        }
        catch (Exception exception)
        {
            Fail(exception, "Không xuất được sản phẩm/SKU.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportPricingAsync(string format, string filePath)
    {
        if (!CanUsePricingExport) return;
        Begin();
        try
        {
            var list = SelectedPriceList();
            var fingerprint = $"{list.Id}|{format}";
            var key = OperationKey("pricing-export", "pricing-export", fingerprint);
            var result = await _service.ExportPricingAsync(format, key);
            var rows = result.Rows
                .Where(row => PricingRowMatchesList(row, list.Code))
                .Select(row => new[]
                {
                    ReadJson(row, "sku"),
                    ReadJson(row, "amountMinor")
                })
                .ToArray();
            DataExchangeFileHelper.Write(filePath, "Cập nhật giá", DataExchangePresentation.PricingColumns, rows, format);
            CompleteOperation("pricing-export");
            SetMessage($"Đã xuất {rows.Length} SKU đang có giá trong {list.Code}.");
        }
        catch (Exception exception)
        {
            Fail(exception, "Không xuất được giá hiện tại.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportStocktakeAsync(string format, string filePath)
    {
        if (!CanUseStocktakeExport) return;
        Begin();
        try
        {
            var warehouse = _balances.FirstOrDefault(item => item.WarehouseId == SelectedWarehouseId)
                ?? throw new InvalidOperationException("Chọn kho trước khi tải file kiểm kê.");
            var fingerprint = $"{warehouse.WarehouseId}|{format}";
            var key = OperationKey("stocktake-export", "stocktake-export", fingerprint);
            var result = await _service.ExportStocktakeAsync(warehouse.WarehouseId, format, key);
            var selected = DataExchangePresentation.StocktakeColumns.Where(column => result.Columns.Contains(column, StringComparer.Ordinal)).ToArray();
            var rows = result.Rows.Select(row => selected.Select(column => ReadJson(row, column)).ToArray()).ToArray();
            DataExchangeFileHelper.Write(filePath, "Kiểm kê thực tế", selected, rows, format);
            CompleteOperation("stocktake-export");
            SetMessage($"Đã tạo file kiểm kê gồm {rows.Length} dòng; file không hiển thị số tồn hệ thống để bảo đảm kiểm kê độc lập.");
        }
        catch (Exception exception)
        {
            Fail(exception, "Không tạo được file kiểm kê.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task BuildQuotationAsync()
    {
        if (!CanUseQuotation) return;
        Begin();
        try
        {
            var quantity = ExactQuantity(QuotationQuantity, "Số lượng", 6);
            var manualSkus = QuotationSkus
                .Split([' ', ',', ';', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var selectedProducts = _products.Where(product => product.IsActive);
            if (QuotationScope == "category")
            {
                if (string.IsNullOrWhiteSpace(QuotationCategoryId))
                    throw new InvalidOperationException("Chọn ngành hoặc nhóm sản phẩm.");
                selectedProducts = selectedProducts.Where(product => product.CategoryId == QuotationCategoryId);
            }

            var variants = await Task.WhenAll(selectedProducts.Select(product => _service.ListVariantsAsync(product.Id)));
            var skus = variants.SelectMany(items => items)
                .Where(variant => variant.IsActive && variant.IsSellable
                    && (QuotationScope != "sku" || manualSkus.Contains(variant.Sku)))
                .Select(variant => variant.Sku.Trim().ToUpperInvariant())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (QuotationScope == "sku")
            {
                var found = skus.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var missing = manualSkus.Where(sku => !found.Contains(sku)).ToArray();
                if (missing.Length > 0)
                    throw new InvalidOperationException($"Không tìm thấy SKU đang bán: {string.Join(", ", missing)}.");
            }

            if (skus.Length == 0) throw new InvalidOperationException("Không có SKU phù hợp để lập báo giá.");
            if (skus.Length > 1000) throw new InvalidOperationException("Báo giá tối đa 1.000 SKU mỗi lần.");

            var request = new DataExchangeQuotationRequest(
                skus,
                quantity,
                "VND",
                EmptyToNull(QuotationChannelId),
                EmptyToNull(QuotationCustomerGroupId),
                EmptyToNull(QuotationCustomerId),
                "tabular");
            var fingerprint = string.Join("|", skus) + $"|{quantity}|{QuotationChannelId}|{QuotationCustomerGroupId}|{QuotationCustomerId}";
            var key = OperationKey("quotation", "quotation", fingerprint);
            var result = await _service.BuildQuotationAsync(request, key);

            QuotationRows.Clear();
            foreach (var row in result.Rows)
            {
                QuotationRows.Add(new DataExchangeQuotationRow(
                    ReadJson(row, "sku"),
                    ReadJson(row, "skuName"),
                    ReadJson(row, "productName"),
                    ReadJson(row, "quantity"),
                    ReadJson(row, "unitPriceMinor"),
                    ReadJson(row, "lineTotalMinor"),
                    ReadJson(row, "priceListCode"),
                    ReadJson(row, "currencyCode")));
            }
            CompleteOperation("quotation");
            OnPropertyChanged(nameof(CanUseQuotationExport));
            SetMessage($"Đã tính giá cho {QuotationRows.Count} SKU.");
        }
        catch (Exception exception)
        {
            Fail(exception, "Không tính được báo giá.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ExportQuotation(string format, string filePath)
    {
        if (QuotationRows.Count == 0) throw new InvalidOperationException("Hãy tính báo giá trước khi xuất file.");
        var rows = QuotationRows.Select(row => new[]
        {
            row.Sku, row.ProductName, row.SkuName, row.Quantity, row.CurrencyCode, row.UnitPrice, row.LineTotal, row.PriceListCode
        }).ToArray();
        DataExchangeFileHelper.Write(filePath, "Báo giá", DataExchangePresentation.QuotationColumns, rows, format);
        SetMessage($"Đã xuất {rows.Length} dòng báo giá.");
    }

    public async Task LoadMovementsAsync(bool append)
    {
        if (!CanUseMovements || SelectedBalance is null) return;
        Begin();
        try
        {
            var balance = SelectedBalance.Balance;
            var offset = append ? MovementRows.Count : 0;
            var data = await _service.ListMovementsAsync(
                balance.WarehouseId,
                balance.BaseVariantId,
                balance.LocationId,
                balance.LotId,
                MovementPageSize,
                offset);

            decimal running;
            if (append)
            {
                running = ParseDecimal(balance.OnHandQuantity, "Tồn hiện tại");
                foreach (var existing in MovementRows)
                    running -= ParseDecimal(existing.QuantityDelta, "Biến động");
            }
            else
            {
                MovementRows.Clear();
                running = ParseDecimal(balance.OnHandQuantity, "Tồn hiện tại");
            }

            foreach (var item in data)
            {
                var stockAfter = running;
                var delta = ParseDecimal(item.BaseQuantityDelta, "Biến động");
                MovementRows.Add(ToMovementRow(item, stockAfter));
                running -= delta;
            }
            MovementHasMore = data.Count == MovementPageSize;
            SetMessage($"Đã tải {MovementRows.Count} lần biến động của {balance.BaseSku}.");
        }
        catch (Exception exception)
        {
            Fail(exception, "Không tải được biến động kho.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportMovementsAsync(string format, string filePath)
    {
        if (!CanUseMovements || SelectedBalance is null) return;
        Begin();
        try
        {
            var balance = SelectedBalance.Balance;
            var all = new List<DataExchangeMovementData>();
            for (var offset = 0; ; offset += MovementExportPageSize)
            {
                var page = await _service.ListMovementsAsync(
                    balance.WarehouseId,
                    balance.BaseVariantId,
                    balance.LocationId,
                    balance.LotId,
                    MovementExportPageSize,
                    offset);
                all.AddRange(page);
                if (all.Count > MovementExportLimit)
                    throw new InvalidOperationException("Biến động kho vượt giới hạn xuất file. Hãy thu hẹp phạm vi lô hoặc vị trí trước khi xuất.");
                if (page.Count < MovementExportPageSize) break;
            }

            var running = all.Sum(item => ParseDecimal(item.BaseQuantityDelta, "Biến động"));
            var rows = new List<string[]>();
            foreach (var item in all)
            {
                var stockAfter = running;
                var delta = ParseDecimal(item.BaseQuantityDelta, "Biến động");
                rows.Add(
                [
                    DataExchangePresentation.VietnamDateTime(item.PostedAt),
                    Document(item),
                    DataExchangePresentation.MovementType(item.MovementType),
                    item.Direction == "IN" ? "Nhập" : "Xuất",
                    FormatDecimal(delta),
                    FormatDecimal(stockAfter),
                    item.LotCode ?? string.Empty,
                    item.SourceLineReference ?? string.Empty
                ]);
                running -= delta;
            }

            var columns = new[] { "Thời gian", "Chứng từ", "Loại nghiệp vụ", "Chiều", "Biến động", "Tồn sau", "Mã lô", "Tham chiếu dòng" };
            WriteLiteralHeaders(filePath, "Biến động kho", columns, rows, format);
            SetMessage($"Đã xuất biến động kho của {balance.BaseSku}.");
        }
        catch (Exception exception)
        {
            Fail(exception, "Không xuất được biến động kho.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectPendingRows(string mode)
    {
        if (PendingKind != "products") return;
        _suppressPendingChange = true;
        try
        {
            foreach (var row in PendingRows)
            {
                row.IsSelected = mode switch
                {
                    "all" => true,
                    "base" => DataExchangePresentation.BoolChoice(row.IsInventoryBase) == "CÓ",
                    "carton" => DataExchangePresentation.VariantChoice(row.VariantKind) == "CARTON",
                    "none" => false,
                    _ => row.IsSelected
                };
            }
        }
        finally
        {
            _suppressPendingChange = false;
        }
        OnPropertyChanged(nameof(SelectedPendingCount));
    }

    public void ExpandSameProductSelection()
    {
        var codes = PendingRows.Where(row => row.IsSelected)
            .Select(row => row.ProductCode.Trim().ToUpperInvariant())
            .Where(code => code.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (codes.Count == 0) return;
        foreach (var row in PendingRows)
            if (codes.Contains(row.ProductCode.Trim())) row.IsSelected = true;
        OnPropertyChanged(nameof(SelectedPendingCount));
    }

    public void ApplyBulkValue()
    {
        if (PendingKind != "products" || string.IsNullOrWhiteSpace(BulkValue)) return;
        var selected = PendingRows.Where(row => row.IsSelected).ToArray();
        if (selected.Length == 0) return;

        var productFields = new HashSet<string>(["categoryCode", "brandCode", "productIsCatalogVisible", "productIsOrderable", "productIsActive"], StringComparer.Ordinal);
        var policyFields = new HashSet<string>(["lotTrackingMode", "expiryTrackingMode", "locationRequired"], StringComparer.Ordinal);
        IEnumerable<DataExchangeImportRow> targets = selected;
        if (productFields.Contains(BulkField))
        {
            var productCodes = selected.Select(row => row.ProductCode.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            targets = PendingRows.Where(row => productCodes.Contains(row.ProductCode.Trim()));
        }
        if (policyFields.Contains(BulkField))
            targets = targets.Where(row => DataExchangePresentation.BoolChoice(row.IsInventoryBase) == "CÓ");

        var normalized = NormalizeBulkValue(BulkField, BulkValue);
        foreach (var row in targets) row.SetValue(BulkField, normalized);
        SetMessage($"Đã áp dụng “{BulkValue}” cho {selected.Length} dòng đã chọn.");
    }

    private void ValidateProductRows(IReadOnlyList<DataExchangeImportRow> rows)
    {
        var activeUnits = _units.Where(unit => unit.IsActive).Select(unit => unit.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var categoryCodes = _categories.Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var brandCodes = _brands.Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var identities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var line = index + 2;
            var productCode = row.ProductCode.Trim().ToUpperInvariant();
            var sku = row.Sku.Trim().ToUpperInvariant();
            if (productCode.Length == 0 || string.IsNullOrWhiteSpace(row.ProductName))
                throw new InvalidOperationException($"Dòng {line}: cần có Mã sản phẩm và Tên sản phẩm.");

            var categoryCode = row.CategoryCode.Trim().ToUpperInvariant();
            var brandCode = row.BrandCode.Trim().ToUpperInvariant();
            if (categoryCode.Length > 0 && !categoryCodes.Contains(categoryCode))
                throw new InvalidOperationException($"Dòng {line} · Mã SP {productCode}: Loại sản phẩm “{categoryCode}” không tồn tại.");
            if (brandCode.Length > 0 && !brandCodes.Contains(brandCode))
                throw new InvalidOperationException($"Dòng {line} · Mã SP {productCode}: Nhãn hàng “{brandCode}” không tồn tại.");

            foreach (var (field, value) in new[]
            {
                ("Hiển thị sản phẩm khi bán hàng", row.ProductIsCatalogVisible),
                ("Cho phép đặt hàng", row.ProductIsOrderable),
                ("Sản phẩm đang sử dụng", row.ProductIsActive)
            })
                if (DataExchangePresentation.BoolChoice(value).Length == 0)
                    throw new InvalidOperationException($"Dòng {line}: chọn Có hoặc Không ở “{field}”.");

            var identity = string.Join("\u001f",
                row.ProductName.Trim(), row.CatalogName.Trim(), categoryCode, brandCode,
                row.Description.Trim(), row.Notes.Trim(),
                DataExchangePresentation.BoolChoice(row.ProductIsCatalogVisible),
                DataExchangePresentation.BoolChoice(row.ProductIsOrderable),
                DataExchangePresentation.BoolChoice(row.ProductIsActive));
            if (identities.TryGetValue(productCode, out var existing) && existing != identity)
                throw new InvalidOperationException($"Mã SP {productCode}: thông tin cấp sản phẩm đang không đồng nhất giữa các dòng SKU.");
            identities[productCode] = identity;

            if (sku.Length == 0) continue;
            if (string.IsNullOrWhiteSpace(row.SkuName))
                throw new InvalidOperationException($"Dòng {line} · SKU {sku}: cần có Tên SKU / quy cách.");
            if (DataExchangePresentation.VariantChoice(row.VariantKind) is not ("BASE" or "CARTON" or "OTHER"))
                throw new InvalidOperationException($"Dòng {line} · SKU {sku}: chọn Loại SKU.");

            foreach (var (field, value) in new[]
            {
                ("Cho phép bán SKU", row.IsSellable),
                ("Hiển thị SKU khi bán hàng", row.IsCatalogVisible),
                ("SKU đang sử dụng", row.IsActive)
            })
                if (DataExchangePresentation.BoolChoice(value).Length == 0)
                    throw new InvalidOperationException($"Dòng {line} · SKU {sku}: chọn Có hoặc Không ở “{field}”.");

            var unitCode = row.UnitCode.Trim().ToUpperInvariant();
            if (unitCode.Length == 0)
                throw new InvalidOperationException($"Dòng {line} · SKU {sku}: chưa chọn Đơn vị tính.");
            if (!activeUnits.Contains(unitCode))
                throw new InvalidOperationException($"Dòng {line} · SKU {sku}: Đơn vị tính “{unitCode}” chưa có hoặc đã ngừng sử dụng.");

            var conversion = row.ConversionToBase.Trim();
            if (!PositiveConversion().IsMatch(conversion) || decimal.Parse(conversion, CultureInfo.InvariantCulture) <= 0)
                throw new InvalidOperationException($"Dòng {line} · SKU {sku}: Hệ số quy đổi phải lớn hơn 0.");

            var inventoryBase = DataExchangePresentation.BoolChoice(row.IsInventoryBase);
            if (inventoryBase.Length == 0)
                throw new InvalidOperationException($"Dòng {line} · SKU {sku}: chọn Có hoặc Không ở “SKU dùng làm đơn vị tồn chuẩn”.");

            if (inventoryBase == "CÓ")
            {
                if (decimal.Parse(conversion, CultureInfo.InvariantCulture) != 1m)
                    throw new InvalidOperationException($"Dòng {line} · SKU {sku}: SKU dùng làm đơn vị tồn chuẩn phải có Hệ số quy đổi = 1.");
                var lot = DataExchangePresentation.LotChoice(row.LotTrackingMode);
                var expiry = DataExchangePresentation.ExpiryChoice(row.ExpiryTrackingMode);
                var location = DataExchangePresentation.BoolChoice(row.LocationRequired);
                if (lot.Length == 0) throw new InvalidOperationException($"Dòng {line} · SKU {sku}: chọn Có hoặc Không ở “Quản lý theo lô”.");
                if (expiry.Length == 0) throw new InvalidOperationException($"Dòng {line} · SKU {sku}: chọn cách “Quản lý hạn sử dụng”.");
                if (location.Length == 0) throw new InvalidOperationException($"Dòng {line} · SKU {sku}: chọn Có hoặc Không ở “Bắt buộc chọn vị trí kho”.");
                if (expiry != "KHÔNG" && lot != "CÓ")
                    throw new InvalidOperationException($"Dòng {line} · SKU {sku}: muốn quản lý hạn sử dụng thì phải bật Quản lý theo lô.");
            }
        }
    }

    private async Task RefreshAfterImportAsync()
    {
        _loaded = false;
        IsBusy = false;
        await RefreshAsync();
    }

    private void RebuildChoices()
    {
        Replace(Units, _units.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new DataExchangeChoice(item.Code.ToUpperInvariant(), $"{item.Name} — {item.Code}")));
        Replace(Categories, _categories.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new DataExchangeChoice(item.Id, $"{item.Code} · {item.Name}")));
        Replace(PriceLists, _priceLists.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new DataExchangeChoice(item.Id, $"{item.Code} · {item.Name}")));
        Replace(Channels, _channels.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new DataExchangeChoice(item.Id, $"{item.Code} · {item.Name}")));
        Replace(CustomerGroups, _groups.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new DataExchangeChoice(item.Id, $"{item.Code} · {item.Name}")));
        Replace(Customers, _customers.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new DataExchangeChoice(item.Id, $"{item.Code} · {item.Name}")));

        var warehouseMap = _balances
            .GroupBy(item => item.WarehouseId)
            .Select(group => group.First())
            .OrderBy(item => item.WarehouseCode)
            .ToArray();
        Replace(Warehouses, warehouseMap.Select(item => new DataExchangeChoice(item.WarehouseId, $"{item.WarehouseCode} · {item.WarehouseName}")));

        BalanceChoices.Clear();
        foreach (var item in _balances.OrderBy(item => item.WarehouseCode).ThenBy(item => item.LocationCode).ThenBy(item => item.BaseSku).ThenBy(item => item.LotCode))
        {
            var key = string.Join("|", item.WarehouseCode, item.LocationCode ?? string.Empty, item.BaseSku, item.LotCode ?? string.Empty);
            var display = $"{item.WarehouseCode} · {item.LocationCode ?? "Không vị trí"} · {item.BaseSku}"
                + (string.IsNullOrWhiteSpace(item.LotCode) ? string.Empty : $" · {item.LotCode}")
                + $" · tồn {DataExchangePresentation.TrimDecimal(item.OnHandQuantity)}";
            BalanceChoices.Add(new DataExchangeBalanceChoice(key, display, item));
        }

        if (string.IsNullOrWhiteSpace(SelectedPriceListId))
            SelectedPriceListId = _priceLists.FirstOrDefault(item => item.IsActive && item.ListType == "BASE")?.Id
                ?? _priceLists.FirstOrDefault(item => item.IsActive)?.Id
                ?? string.Empty;
        if (string.IsNullOrWhiteSpace(SelectedWarehouseId))
            SelectedWarehouseId = warehouseMap.FirstOrDefault()?.WarehouseId ?? string.Empty;
    }

    private void ClearReferenceData()
    {
        _products = [];
        _categories = [];
        _brands = [];
        _units = [];
        _priceLists = [];
        _channels = [];
        _groups = [];
        _customers = [];
        _balances = [];
        PriceLists.Clear();
        Warehouses.Clear();
        Categories.Clear();
        Channels.Clear();
        CustomerGroups.Clear();
        Customers.Clear();
        Units.Clear();
        BalanceChoices.Clear();
        SelectedPriceListId = string.Empty;
        SelectedWarehouseId = string.Empty;
    }

    private void ClearPendingImport()
    {
        foreach (var row in PendingRows)
        {
            row.Changed -= PendingRowChanged;
            row.PropertyChanged -= PendingRowPropertyChanged;
        }
        PendingRows.Clear();
        PendingKind = null;
        PendingFileName = null;
        _pendingFormat = null;
        _importOperationKey = null;
        BulkValue = string.Empty;
        OnPropertyChanged(nameof(SelectedPendingCount));
        OnPropertyChanged(nameof(CanConfirmImport));
    }

    private void PendingRowChanged(object? sender, EventArgs e)
    {
        if (_suppressPendingChange || PendingKind is null) return;
        _importOperationKey = _idempotencyKeys.Create(ImportScope(PendingKind));
    }

    private void PendingRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DataExchangeImportRow.IsSelected))
            OnPropertyChanged(nameof(SelectedPendingCount));
    }

    private void EnsureImportPermission(string kind)
    {
        var allowed = kind switch
        {
            "products" => CanImportProducts,
            "pricing" => CanImportPricing,
            "stocktake" => CanImportStocktake,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException("Tài khoản chưa được cấp quyền thực hiện thao tác này.");
    }

    private PriceListData SelectedPriceList()
    {
        var list = _priceLists.FirstOrDefault(item => item.Id == SelectedPriceListId)
            ?? throw new InvalidOperationException("Chọn bảng giá hoặc chương trình cần cập nhật.");
        if (!list.IsActive) throw new InvalidOperationException("Bảng giá hoặc chương trình đã ngừng sử dụng.");
        return list;
    }

    private static bool PricingRowMatchesList(IReadOnlyDictionary<string, JsonElement> row, string code)
    {
        var active = ReadJson(row, "isActive");
        return string.Equals(ReadJson(row, "priceListCode"), code, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ReadJson(row, "adjustmentType"), "FIXED_PRICE", StringComparison.OrdinalIgnoreCase)
            && DataExchangePresentation.TrimDecimal(ReadJson(row, "minQuantity")) == "0"
            && string.IsNullOrWhiteSpace(ReadJson(row, "maxQuantity"))
            && string.IsNullOrWhiteSpace(ReadJson(row, "effectiveFrom"))
            && string.IsNullOrWhiteSpace(ReadJson(row, "effectiveTo"))
            && (active == "true" || active == "True" || active == "1");
    }

    private static string ReadJson(IReadOnlyDictionary<string, JsonElement> row, string key) =>
        row.TryGetValue(key, out var value) ? DataExchangePresentation.JsonText(value) : string.Empty;

    private static string ImportScope(string kind) =>
        kind switch
        {
            "products" => "product-file",
            "pricing" => "price-file",
            "stocktake" => "stocktake-file",
            _ => "data-file"
        };

    private string OperationKey(string slot, string scope, string fingerprint)
    {
        if (_operationKeys.TryGetValue(slot, out var current) && current.Fingerprint == fingerprint)
            return current.Key;
        var key = _idempotencyKeys.Create(scope);
        _operationKeys[slot] = new OperationKeyState(fingerprint, key);
        return key;
    }

    private void CompleteOperation(string slot) => _operationKeys.Remove(slot);

    private void Begin()
    {
        IsBusy = true;
        SetMessage(string.Empty);
    }

    private void Fail(Exception exception, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(exception.Message) ? fallback : Humanize(exception.Message);
        SetMessage(text, isError: true);
    }

    private void SetMessage(string value, bool isError = false)
    {
        MessageIsError = isError;
        Message = value;
    }

    private void RaisePermissions()
    {
        foreach (var name in new[]
        {
            nameof(CanOpen), nameof(CanExportProducts), nameof(CanImportProducts), nameof(CanExportPricing),
            nameof(CanImportPricing), nameof(CanExportStocktake), nameof(CanImportStocktake), nameof(CanQuotation),
            nameof(CanMovements), nameof(CanUseProductExport), nameof(CanUseProductImport), nameof(CanUsePricingExport),
            nameof(CanUsePricingImport), nameof(CanUseStocktakeExport), nameof(CanUseStocktakeImport), nameof(CanUseQuotation),
            nameof(CanUseQuotationExport), nameof(CanUseMovements), nameof(CanLoadMoreMovements), nameof(CanConfirmImport),
            nameof(IsOfficeFormsTab), nameof(CanRefresh)
        }) OnPropertyChanged(name);
    }

    private static DataExchangeMovementRow ToMovementRow(DataExchangeMovementData item, decimal stockAfter)
    {
        var delta = ParseDecimal(item.BaseQuantityDelta, "Biến động");
        return new DataExchangeMovementRow(
            item.MovementId,
            DataExchangePresentation.VietnamDateTime(item.PostedAt),
            Document(item),
            DataExchangePresentation.MovementType(item.MovementType),
            item.Direction == "IN" ? "Nhập" : "Xuất",
            FormatDecimal(delta),
            FormatDecimal(stockAfter),
            item.LotCode ?? string.Empty,
            item.SourceLineReference ?? string.Empty);
    }

    private static string Document(DataExchangeMovementData item) =>
        item.SourceDocumentNumber ?? item.DocumentNumber ?? item.SourceDocumentType ?? "—";

    private static decimal ParseDecimal(string value, string field)
    {
        if (!decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
            throw new InvalidOperationException($"{field} không hợp lệ.");
        return parsed;
    }

    private static string FormatDecimal(decimal value) =>
        value.ToString("0.############", CultureInfo.InvariantCulture);

    private static string ExactQuantity(string value, string field, int scale)
    {
        var pattern = new Regex($"^(0|[1-9]\\d{{0,13}})(?:\\.\\d{{1,{scale}}})?$", RegexOptions.CultureInvariant);
        var normalized = value.Trim();
        if (!pattern.IsMatch(normalized))
            throw new InvalidOperationException($"{field} phải là số không âm, tối đa {scale} số lẻ.");
        return normalized;
    }

    private static string NormalizeBulkValue(string field, string value) =>
        field switch
        {
            "variantKind" => DataExchangePresentation.VariantChoice(value),
            "lotTrackingMode" => DataExchangePresentation.LotChoice(value),
            "expiryTrackingMode" => DataExchangePresentation.ExpiryChoice(value),
            "productIsCatalogVisible" or "productIsOrderable" or "productIsActive" or "isInventoryBase"
                or "isSellable" or "isCatalogVisible" or "isActive" or "locationRequired" => DataExchangePresentation.BoolChoice(value),
            _ => value.Trim()
        };

    private static string Humanize(string value) =>
        value.Replace("unitCode", "Đơn vị tính", StringComparison.Ordinal)
            .Replace("conversionToBase", "Hệ số quy đổi", StringComparison.Ordinal)
            .Replace("isInventoryBase", "SKU dùng làm đơn vị tồn chuẩn", StringComparison.Ordinal)
            .Replace("lotTrackingMode", "Quản lý theo lô", StringComparison.Ordinal)
            .Replace("expiryTrackingMode", "Quản lý hạn sử dụng", StringComparison.Ordinal)
            .Replace("priceListCode", "Mã bảng giá", StringComparison.Ordinal)
            .Replace("actualCount", "Số đếm thực tế", StringComparison.Ordinal)
            .Replace("productName", "Tên sản phẩm", StringComparison.Ordinal)
            .Replace("skuName", "Tên SKU / quy cách", StringComparison.Ordinal)
            .Replace("canonical", "chuẩn của hệ thống", StringComparison.OrdinalIgnoreCase)
            .Replace("legacy", "dữ liệu cũ", StringComparison.OrdinalIgnoreCase);

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Replace(ObservableCollection<DataExchangeChoice> target, IEnumerable<DataExchangeChoice> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    private static void WriteLiteralHeaders(
        string filePath,
        string sheetName,
        IReadOnlyList<string> literalHeaders,
        IReadOnlyList<string[]> rows,
        string format) =>
        DataExchangeFileHelper.Write(filePath, sheetName, literalHeaders, rows, format);

    private void RunOnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher is null || Application.Current.Dispatcher.CheckAccess())
            action();
        else
            Application.Current.Dispatcher.Invoke(action);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed record OperationKeyState(string Fingerprint, string Key);

    [GeneratedRegex("^(?:0|[1-9]\\d{0,18})$", RegexOptions.CultureInvariant)]
    private static partial Regex IntegerAmount();

    [GeneratedRegex("^(?:0|[1-9]\\d{0,13})(?:\\.\\d{1,6})?$", RegexOptions.CultureInvariant)]
    private static partial Regex PositiveConversion();
}

public sealed class DataExchangeExportColumn : INotifyPropertyChanged
{
    private bool _isSelected;

    public DataExchangeExportColumn(string id, string display, bool isSelected)
    {
        Id = id;
        Display = display;
        _isSelected = isSelected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string Id { get; }
    public string Display { get; }

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
