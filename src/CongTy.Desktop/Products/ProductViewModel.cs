using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Inventory;
using CongTy.Desktop.Partners;

namespace CongTy.Desktop.Products;

public sealed partial class ProductViewModel : INotifyPropertyChanged
{
    private enum EditorKind
    {
        None,
        Product,
        Category,
        Brand,
        Variant,
        Unit
    }

    private const string ReadDeniedMessage = "Tài khoản chưa được cấp quyền xem Danh mục sản phẩm.";
    private const int IntentCacheLimit = 256;

    private readonly IProductService _service;
    private readonly IInventoryService _inventory;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private readonly List<ProductData> _products = [];
    private readonly List<ProductCategoryData> _categories = [];
    private readonly List<ProductBrandData> _brands = [];
    private readonly List<ProductUnitData> _units = [];
    private readonly HashSet<string> _imageCodes = new(StringComparer.OrdinalIgnoreCase);

    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private int _tabIndex;
    private string _search = string.Empty;
    private string _statusFilter = "all";
    private string _catalogFilter = "all";
    private string _orderableFilter = "all";

    private EditorKind _editorKind;
    private string? _editingId;
    private string _draftCode = string.Empty;
    private string _draftName = string.Empty;
    private string _draftCatalogName = string.Empty;
    private string _draftCategoryId = string.Empty;
    private string _draftBrandId = string.Empty;
    private string _draftDescription = string.Empty;
    private string _draftNotes = string.Empty;
    private bool _draftCatalogVisible;
    private bool _draftOrderable;
    private bool _draftInventoryManaged = true;
    private bool _draftActive = true;
    private bool _draftHasActiveSellableVariant;
    private string _draftParentCategoryId = string.Empty;
    private string _draftSortOrder = "0";
    private string _draftSymbol = string.Empty;
    private string _draftUnitKind = "COUNT";
    private bool _draftAllowsFractional;
    private string _draftVariantKind = "BASE";
    private bool _draftInventoryBase;
    private bool _draftSellable = true;
    private string _draftWeightValue = string.Empty;
    private string _draftWeightUomCode = "G";
    private string _draftLotTrackingMode = "NONE";
    private string _draftExpiryTrackingMode = "NONE";

    private ProductData? _variantProduct;
    private ProductVariantData? _selectedVariant;

    // Quick setup.
    private string _quickSearch = string.Empty;
    private string _quickProductId = string.Empty;
    private bool _quickCreatingProduct;
    private string _quickProductCode = string.Empty;
    private string _quickProductName = string.Empty;
    private string _quickCatalogName = string.Empty;
    private string _quickCategoryId = string.Empty;
    private string _quickBrandId = string.Empty;
    private string _quickDescription = string.Empty;
    private string _quickNotes = string.Empty;
    private bool _quickCatalogVisible;
    private bool _quickOrderable;
    private bool _quickInventoryManaged = true;
    private bool _quickProductActive = true;
    private string _quickVariantId = string.Empty;
    private bool _quickCreatingVariant;
    private string _quickSku = string.Empty;
    private string _quickVariantName = string.Empty;
    private string _quickVariantKind = "BASE";
    private string _quickWeightValue = string.Empty;
    private string _quickWeightUomCode = "G";
    private bool _quickInventoryBase;
    private bool _quickSellable = true;
    private bool _quickVariantCatalogVisible;
    private bool _quickVariantActive = true;
    private string _quickLotTrackingMode = "NONE";
    private string _quickExpiryTrackingMode = "NONE";
    private string _quickUnitId = string.Empty;
    private string _quickConversion = string.Empty;
    private string _quickNetContentValue = string.Empty;
    private string _quickNetContentUnit = "G";
    private bool _quickPurchasable = true;
    private string _quickSourceUnitLabel = string.Empty;
    private string _quickSourcePackageDescription = string.Empty;
    private string _quickBarcode = string.Empty;
    private string _quickPriceListId = string.Empty;
    private string _quickPriceAmount = string.Empty;
    private ProductPriceItemData? _quickSimplePriceItem;
    private int _quickDirectPriceCount;
    private int _quickComplexPriceCount;

    // Unit workspace.
    private int _unitWorkspaceTabIndex;
    private string _unitProductId = string.Empty;
    private string _unitVariantId = string.Empty;
    private string _unitUnitId = string.Empty;
    private string _unitConversion = string.Empty;
    private string _unitNetContentValue = string.Empty;
    private string _unitNetContentUnit = "G";
    private bool _unitPurchasable = true;
    private string _unitSourceLabel = string.Empty;
    private string _unitPackageDescription = string.Empty;
    private string _unitBarcode = string.Empty;
    private string _normalizeQuantity = "1";
    private string _normalizeResult = string.Empty;

    // Bulk.
    private IReadOnlyList<string[]> _bulkMatrix = [];
    private string _bulkFileName = string.Empty;
    private bool _bulkHasHeader = true;
    private ProductBulkIdentificationData? _bulkIdentification;
    private ProductBulkPreviewData? _bulkPreview;
    private string? _bulkOperationKey;
    private bool _bulkApplied;

    public ProductViewModel(
        IProductService service,
        IDataExchangeService dataExchange,
        IInventoryService inventory,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _dataExchange = dataExchange;
        _inventory = inventory;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        StatusOptions.Add(new ProductFilterOption("all", "Tất cả trạng thái"));
        StatusOptions.Add(new ProductFilterOption("active", "Đang sử dụng"));
        StatusOptions.Add(new ProductFilterOption("inactive", "Ngừng sử dụng"));
        CatalogOptions.Add(new ProductFilterOption("all", "Tất cả hiển thị"));
        CatalogOptions.Add(new ProductFilterOption("visible", "Hiển thị bán hàng"));
        CatalogOptions.Add(new ProductFilterOption("hidden", "Không hiển thị bán hàng"));
        OrderableOptions.Add(new ProductFilterOption("all", "Tất cả đặt hàng"));
        OrderableOptions.Add(new ProductFilterOption("yes", "Có thể đặt"));
        OrderableOptions.Add(new ProductFilterOption("no", "Chưa thể đặt"));
        UnitKindOptions.Add(new ProductLookupOption("COUNT", "Đếm"));
        UnitKindOptions.Add(new ProductLookupOption("PACKAGE", "Bao gói"));
        UnitKindOptions.Add(new ProductLookupOption("WEIGHT", "Khối lượng"));
        UnitKindOptions.Add(new ProductLookupOption("VOLUME", "Thể tích"));
        UnitKindOptions.Add(new ProductLookupOption("OTHER", "Khác"));
        VariantKindOptions.Add(new ProductLookupOption("BASE", "Đơn vị lẻ"));
        VariantKindOptions.Add(new ProductLookupOption("CARTON", "Thùng"));
        VariantKindOptions.Add(new ProductLookupOption("OTHER", "Quy cách khác"));
        WeightUnitOptions.Add(new ProductLookupOption("G", "g"));
        WeightUnitOptions.Add(new ProductLookupOption("KG", "kg"));
        NetContentUnitOptions.Add(new ProductLookupOption("G", "Gam"));
        NetContentUnitOptions.Add(new ProductLookupOption("KG", "Kilôgam"));
        NetContentUnitOptions.Add(new ProductLookupOption("ML", "Mililít"));
        NetContentUnitOptions.Add(new ProductLookupOption("L", "Lít"));
        NetContentUnitOptions.Add(new ProductLookupOption("EA", "Cái"));
        NetContentUnitOptions.Add(new ProductLookupOption("OTHER", "Khác"));
        BulkMappingOptions.Add(new ProductLookupOption("IGNORE", "Bỏ qua"));
        BulkMappingOptions.Add(new ProductLookupOption("PRODUCT_NAME", "Tên sản phẩm"));
        BulkMappingOptions.Add(new ProductLookupOption("CATALOG_NAME", "Tên hiển thị bán hàng"));
        BulkMappingOptions.Add(new ProductLookupOption("CATEGORY_CODE", "Loại sản phẩm"));
        BulkMappingOptions.Add(new ProductLookupOption("BRAND_CODE", "Nhãn hàng"));
        BulkMappingOptions.Add(new ProductLookupOption("DESCRIPTION", "Mô tả"));
        BulkMappingOptions.Add(new ProductLookupOption("NOTES", "Ghi chú"));
        BulkMappingOptions.Add(new ProductLookupOption("PRODUCT_CATALOG_VISIBLE", "Hiển thị sản phẩm khi bán hàng"));
        BulkMappingOptions.Add(new ProductLookupOption("PRODUCT_ORDERABLE", "Cho phép đặt hàng"));
        BulkMappingOptions.Add(new ProductLookupOption("PRODUCT_INVENTORY_MANAGED", "Quản lý tồn kho"));
        BulkMappingOptions.Add(new ProductLookupOption("PRODUCT_ACTIVE", "Sản phẩm đang sử dụng"));
        BulkMappingOptions.Add(new ProductLookupOption("VARIANT_NAME", "Tên SKU / quy cách"));
        BulkMappingOptions.Add(new ProductLookupOption("VARIANT_KIND", "Loại SKU"));
        BulkMappingOptions.Add(new ProductLookupOption("INVENTORY_BASE", "SKU dùng làm đơn vị tồn chuẩn"));
        BulkMappingOptions.Add(new ProductLookupOption("SELLABLE", "Cho phép bán SKU"));
        BulkMappingOptions.Add(new ProductLookupOption("VARIANT_CATALOG_VISIBLE", "Hiển thị SKU khi bán hàng"));
        BulkMappingOptions.Add(new ProductLookupOption("VARIANT_ACTIVE", "SKU đang sử dụng"));
        BulkMappingOptions.Add(new ProductLookupOption("UNIT_CODE", "Đơn vị tính"));
        BulkMappingOptions.Add(new ProductLookupOption("CONVERSION_TO_BASE", "Hệ số quy đổi về đơn vị tồn chuẩn"));
        BulkMappingOptions.Add(new ProductLookupOption("PURCHASABLE", "Cho phép mua SKU"));
        BulkMappingOptions.Add(new ProductLookupOption("NET_CONTENT_VALUE", "Định lượng quy cách"));
        BulkMappingOptions.Add(new ProductLookupOption("NET_CONTENT_UOM", "Đơn vị định lượng"));
        BulkMappingOptions.Add(new ProductLookupOption("SOURCE_UNIT_LABEL", "Tên đơn vị nguồn"));
        BulkMappingOptions.Add(new ProductLookupOption("SOURCE_PACKAGE_DESCRIPTION", "Mô tả quy cách nguồn"));
        BulkMappingOptions.Add(new ProductLookupOption("WEIGHT_VALUE", "Khối lượng"));
        BulkMappingOptions.Add(new ProductLookupOption("WEIGHT_UOM", "Đơn vị khối lượng"));
        InitializeProductFileWorkspace();

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loaded = false;
            ResetSessionData();
            RaisePermissions();
            if (CanRead || !_access.Current.IsAuthenticated)
            {
                if (string.Equals(Message, ReadDeniedMessage, StringComparison.Ordinal)
                    || !_access.Current.IsAuthenticated)
                {
                    ClearMessage();
                }
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ProductFilterOption> StatusOptions { get; } = [];
    public ObservableCollection<ProductFilterOption> CatalogOptions { get; } = [];
    public ObservableCollection<ProductFilterOption> OrderableOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> CategoryOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> BrandOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> ProductOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> UnitOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> UnitKindOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> VariantKindOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> WeightUnitOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> NetContentUnitOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> PriceListOptions { get; } = [];
    public ObservableCollection<ProductLookupOption> BulkMappingOptions { get; } = [];

    public ObservableCollection<ProductRow> ProductRows { get; } = [];
    public ObservableCollection<ProductCategoryRow> CategoryRows { get; } = [];
    public ObservableCollection<ProductBrandRow> BrandRows { get; } = [];
    public ObservableCollection<ProductVariantRow> VariantRows { get; } = [];
    public ObservableCollection<ProductUnitRow> UnitRows { get; } = [];
    public ObservableCollection<ProductBarcodeRow> BarcodeRows { get; } = [];
    public ObservableCollection<ProductVariantData> QuickVariants { get; } = [];
    public ObservableCollection<ProductInventoryRow> QuickInventoryRows { get; } = [];
    public ObservableCollection<ProductVariantData> UnitVariants { get; } = [];
    public ObservableCollection<ProductBulkColumnRow> BulkColumns { get; } = [];
    public ObservableCollection<ProductBulkSourceRowView> BulkSourceRows { get; } = [];
    public ObservableCollection<ProductBulkPreviewRowView> BulkPreviewRows { get; } = [];

    public bool CanRead => _access.HasPermission("core.product.read");
    public bool CanWrite => _access.HasPermission("core.product.write");
    public bool CanReadPrice => _access.HasPermission("core.price.read");
    public bool CanWritePrice => _access.HasPermission("core.price.write");
    public bool CanReadInventory => _access.HasPermission("core.inventory.read");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoading => _busyAction == "load";
    public string RefreshText => IsLoading ? "Đang tải..." : "Làm mới";

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

    public int TabIndex
    {
        get => _tabIndex;
        set => SetField(ref _tabIndex, Math.Clamp(value, 0, 5));
    }

    public string Search { get => _search; set { if (SetField(ref _search, value ?? string.Empty)) RefreshProducts(); } }
    public string StatusFilter { get => _statusFilter; set { if (SetField(ref _statusFilter, value ?? "all")) RefreshProducts(); } }
    public string CatalogFilter { get => _catalogFilter; set { if (SetField(ref _catalogFilter, value ?? "all")) RefreshProducts(); } }
    public string OrderableFilter { get => _orderableFilter; set { if (SetField(ref _orderableFilter, value ?? "all")) RefreshProducts(); } }

    public string ProductSummary => $"{_products.Count} sản phẩm · {_products.Count(item => item.IsOrderable)} có thể đặt hàng";

    public bool IsEditorOpen => _editorKind != EditorKind.None;
    public bool IsProductEditor => _editorKind == EditorKind.Product;
    public bool IsCategoryEditor => _editorKind == EditorKind.Category;
    public bool IsBrandEditor => _editorKind == EditorKind.Brand;
    public bool IsVariantEditor => _editorKind == EditorKind.Variant;
    public bool IsUnitEditor => _editorKind == EditorKind.Unit;
    public bool IsCreateMode => _editingId is null;
    public string EditorTitle => _editorKind switch
    {
        EditorKind.Product => IsCreateMode ? "Thêm sản phẩm" : $"Sửa {DraftCode}",
        EditorKind.Category => IsCreateMode ? "Thêm loại sản phẩm" : $"Sửa {DraftCode}",
        EditorKind.Brand => IsCreateMode ? "Thêm nhãn hàng" : $"Sửa {DraftCode}",
        EditorKind.Variant => IsCreateMode ? $"Thêm SKU cho {VariantProduct?.Code ?? "sản phẩm"}" : $"Sửa {DraftCode}",
        EditorKind.Unit => IsCreateMode ? "Thêm đơn vị tính" : $"Sửa {DraftCode}",
        _ => string.Empty
    };
    public string EditorPrimaryText => _editorKind switch
    {
        EditorKind.Product => "Lưu sản phẩm",
        EditorKind.Category => "Lưu loại",
        EditorKind.Brand => "Lưu nhãn hàng",
        EditorKind.Variant => "Lưu SKU",
        EditorKind.Unit => "Lưu đơn vị",
        _ => "Lưu"
    };

    public string DraftCode { get => _draftCode; set => SetField(ref _draftCode, value ?? string.Empty); }
    public string DraftName { get => _draftName; set => SetField(ref _draftName, value ?? string.Empty); }
    public string DraftCatalogName { get => _draftCatalogName; set => SetField(ref _draftCatalogName, value ?? string.Empty); }
    public string DraftCategoryId { get => _draftCategoryId; set => SetField(ref _draftCategoryId, value ?? string.Empty); }
    public string DraftBrandId { get => _draftBrandId; set => SetField(ref _draftBrandId, value ?? string.Empty); }
    public string DraftDescription { get => _draftDescription; set => SetField(ref _draftDescription, value ?? string.Empty); }
    public string DraftNotes { get => _draftNotes; set => SetField(ref _draftNotes, value ?? string.Empty); }
    public bool DraftCatalogVisible { get => _draftCatalogVisible; set => SetField(ref _draftCatalogVisible, value); }
    public bool DraftOrderable { get => _draftOrderable; set => SetField(ref _draftOrderable, value); }
    public bool CanEnableDraftOrderable => IsProductEditor && !IsCreateMode && _draftHasActiveSellableVariant;
    public bool DraftInventoryManaged { get => _draftInventoryManaged; set => SetField(ref _draftInventoryManaged, value); }
    public bool DraftActive { get => _draftActive; set => SetField(ref _draftActive, value); }
    public string DraftParentCategoryId { get => _draftParentCategoryId; set => SetField(ref _draftParentCategoryId, value ?? string.Empty); }
    public string DraftSortOrder { get => _draftSortOrder; set => SetField(ref _draftSortOrder, value ?? "0"); }
    public string DraftSymbol { get => _draftSymbol; set => SetField(ref _draftSymbol, value ?? string.Empty); }
    public string DraftUnitKind { get => _draftUnitKind; set => SetField(ref _draftUnitKind, value ?? "COUNT"); }
    public bool DraftAllowsFractional { get => _draftAllowsFractional; set => SetField(ref _draftAllowsFractional, value); }
    public string DraftVariantKind { get => _draftVariantKind; set => SetField(ref _draftVariantKind, value ?? "BASE"); }
    public bool DraftInventoryBase
    {
        get => _draftInventoryBase;
        set
        {
            if (!SetField(ref _draftInventoryBase, value)) return;
            if (value) DraftVariantKind = "BASE";
            if (!value)
            {
                DraftLotTrackingMode = "NONE";
                DraftExpiryTrackingMode = "NONE";
            }
        }
    }
    public bool DraftSellable { get => _draftSellable; set => SetField(ref _draftSellable, value); }
    public string DraftWeightValue { get => _draftWeightValue; set => SetField(ref _draftWeightValue, value ?? string.Empty); }
    public string DraftWeightUomCode { get => _draftWeightUomCode; set => SetField(ref _draftWeightUomCode, value ?? "G"); }
    public string DraftLotTrackingMode
    {
        get => _draftLotTrackingMode;
        set
        {
            if (!SetField(ref _draftLotTrackingMode, value ?? "NONE")) return;
            if (_draftLotTrackingMode == "NONE") DraftExpiryTrackingMode = "NONE";
        }
    }
    public string DraftExpiryTrackingMode { get => _draftExpiryTrackingMode; set => SetField(ref _draftExpiryTrackingMode, value ?? "NONE"); }

    public ProductData? VariantProduct
    {
        get => _variantProduct;
        private set
        {
            if (!SetField(ref _variantProduct, value)) return;
            OnPropertyChanged(nameof(HasVariantProduct));
            OnPropertyChanged(nameof(VariantManagerTitle));
        }
    }
    public bool HasVariantProduct => VariantProduct is not null;
    public string VariantManagerTitle => VariantProduct is null ? string.Empty : $"Quản lý SKU — {VariantProduct.Code}";

    public ProductVariantData? SelectedVariant
    {
        get => _selectedVariant;
        private set => SetField(ref _selectedVariant, value);
    }

    // Quick setup properties.
    public string QuickSearch { get => _quickSearch; set { if (SetField(ref _quickSearch, value ?? string.Empty)) RefreshQuickProductOptions(); } }
    public string QuickProductId
    {
        get => _quickProductId;
        set
        {
            if (SetField(ref _quickProductId, value ?? string.Empty)) NotifyQuickStepState();
        }
    }
    public bool QuickCreatingProduct { get => _quickCreatingProduct; private set { if (SetField(ref _quickCreatingProduct, value)) NotifyQuickStepState(); } }
    public bool HasQuickProduct => !QuickCreatingProduct && !string.IsNullOrWhiteSpace(QuickProductId);
    public string QuickProductCode { get => _quickProductCode; set => SetField(ref _quickProductCode, value ?? string.Empty); }
    public string QuickProductName { get => _quickProductName; set => SetField(ref _quickProductName, value ?? string.Empty); }
    public string QuickCatalogName { get => _quickCatalogName; set => SetField(ref _quickCatalogName, value ?? string.Empty); }
    public string QuickCategoryId { get => _quickCategoryId; set => SetField(ref _quickCategoryId, value ?? string.Empty); }
    public string QuickBrandId { get => _quickBrandId; set => SetField(ref _quickBrandId, value ?? string.Empty); }
    public string QuickDescription { get => _quickDescription; set => SetField(ref _quickDescription, value ?? string.Empty); }
    public string QuickNotes { get => _quickNotes; set => SetField(ref _quickNotes, value ?? string.Empty); }
    public bool QuickCatalogVisible { get => _quickCatalogVisible; set => SetField(ref _quickCatalogVisible, value); }
    public bool QuickOrderable
    {
        get => _quickOrderable;
        set
        {
            if (SetField(ref _quickOrderable, value)) OnPropertyChanged(nameof(CanEnableQuickOrderable));
        }
    }
    public bool CanEnableQuickOrderable =>
        HasQuickProduct
        && (QuickOrderable
            || QuickVariants.Any(item =>
                item.IsActive
                && item.IsSellable
                && !string.IsNullOrWhiteSpace(item.UnitId)
                && !string.IsNullOrWhiteSpace(item.ConversionToBase)));
    public bool QuickInventoryManaged { get => _quickInventoryManaged; set => SetField(ref _quickInventoryManaged, value); }
    public bool QuickProductActive { get => _quickProductActive; set => SetField(ref _quickProductActive, value); }
    public string QuickVariantId { get => _quickVariantId; set => SetField(ref _quickVariantId, value ?? string.Empty); }
    public bool QuickCreatingVariant { get => _quickCreatingVariant; private set { if (SetField(ref _quickCreatingVariant, value)) NotifyQuickStepState(); } }
    public bool HasQuickVariant => !QuickCreatingVariant && !string.IsNullOrWhiteSpace(QuickVariantId);
    public string QuickSku { get => _quickSku; set => SetField(ref _quickSku, value ?? string.Empty); }
    public string QuickVariantName { get => _quickVariantName; set => SetField(ref _quickVariantName, value ?? string.Empty); }
    public string QuickVariantKind { get => _quickVariantKind; set => SetField(ref _quickVariantKind, value ?? "BASE"); }
    public string QuickWeightValue { get => _quickWeightValue; set => SetField(ref _quickWeightValue, value ?? string.Empty); }
    public string QuickWeightUomCode { get => _quickWeightUomCode; set => SetField(ref _quickWeightUomCode, value ?? "G"); }
    public bool QuickInventoryBase
    {
        get => _quickInventoryBase;
        set
        {
            if (!SetField(ref _quickInventoryBase, value)) return;
            if (value) QuickVariantKind = "BASE";
            if (!value)
            {
                QuickLotTrackingMode = "NONE";
                QuickExpiryTrackingMode = "NONE";
            }
        }
    }
    public bool QuickSellable { get => _quickSellable; set => SetField(ref _quickSellable, value); }
    public bool QuickVariantCatalogVisible { get => _quickVariantCatalogVisible; set => SetField(ref _quickVariantCatalogVisible, value); }
    public bool QuickVariantActive { get => _quickVariantActive; set => SetField(ref _quickVariantActive, value); }
    public string QuickLotTrackingMode
    {
        get => _quickLotTrackingMode;
        set
        {
            if (!SetField(ref _quickLotTrackingMode, value ?? "NONE")) return;
            if (_quickLotTrackingMode == "NONE") QuickExpiryTrackingMode = "NONE";
        }
    }
    public string QuickExpiryTrackingMode { get => _quickExpiryTrackingMode; set => SetField(ref _quickExpiryTrackingMode, value ?? "NONE"); }
    public string QuickUnitId { get => _quickUnitId; set => SetField(ref _quickUnitId, value ?? string.Empty); }
    public string QuickConversion { get => _quickConversion; set => SetField(ref _quickConversion, value ?? string.Empty); }
    public string QuickNetContentValue { get => _quickNetContentValue; set => SetField(ref _quickNetContentValue, value ?? string.Empty); }
    public string QuickNetContentUnit { get => _quickNetContentUnit; set => SetField(ref _quickNetContentUnit, value ?? "G"); }
    public bool QuickPurchasable { get => _quickPurchasable; set => SetField(ref _quickPurchasable, value); }
    public string QuickSourceUnitLabel { get => _quickSourceUnitLabel; set => SetField(ref _quickSourceUnitLabel, value ?? string.Empty); }
    public string QuickSourcePackageDescription { get => _quickSourcePackageDescription; set => SetField(ref _quickSourcePackageDescription, value ?? string.Empty); }
    public string QuickBarcode { get => _quickBarcode; set => SetField(ref _quickBarcode, value ?? string.Empty); }
    public string QuickPriceListId { get => _quickPriceListId; set => SetField(ref _quickPriceListId, value ?? string.Empty); }
    public string QuickPriceAmount { get => _quickPriceAmount; set => SetField(ref _quickPriceAmount, value ?? string.Empty); }
    public int QuickDirectPriceCount { get => _quickDirectPriceCount; private set => SetField(ref _quickDirectPriceCount, value); }
    public int QuickComplexPriceCount { get => _quickComplexPriceCount; private set => SetField(ref _quickComplexPriceCount, value); }
    public string QuickImageStatus => GetQuickProduct() is { } product
        ? (_imageCodes.Contains(product.Code) ? "Đã có ảnh" : "Chưa có ảnh")
        : "Chưa chọn sản phẩm";
    public string QuickImageUrl => GetQuickProduct() is { } product && _imageCodes.Contains(product.Code)
        ? $"Ảnh chính: {product.Code}.webp"
        : "Ảnh chính chưa được thiết lập.";

    // Unit workspace.
    public int UnitWorkspaceTabIndex { get => _unitWorkspaceTabIndex; set => SetField(ref _unitWorkspaceTabIndex, Math.Clamp(value, 0, 1)); }
    public string UnitProductId { get => _unitProductId; set => SetField(ref _unitProductId, value ?? string.Empty); }
    public string UnitVariantId { get => _unitVariantId; set => SetField(ref _unitVariantId, value ?? string.Empty); }
    public string UnitUnitId { get => _unitUnitId; set => SetField(ref _unitUnitId, value ?? string.Empty); }
    public string UnitConversion { get => _unitConversion; set => SetField(ref _unitConversion, value ?? string.Empty); }
    public string UnitNetContentValue { get => _unitNetContentValue; set => SetField(ref _unitNetContentValue, value ?? string.Empty); }
    public string UnitNetContentUnit { get => _unitNetContentUnit; set => SetField(ref _unitNetContentUnit, value ?? "G"); }
    public bool UnitPurchasable { get => _unitPurchasable; set => SetField(ref _unitPurchasable, value); }
    public string UnitSourceLabel { get => _unitSourceLabel; set => SetField(ref _unitSourceLabel, value ?? string.Empty); }
    public string UnitPackageDescription { get => _unitPackageDescription; set => SetField(ref _unitPackageDescription, value ?? string.Empty); }
    public string UnitBarcode { get => _unitBarcode; set => SetField(ref _unitBarcode, value ?? string.Empty); }
    public string NormalizeQuantity { get => _normalizeQuantity; set => SetField(ref _normalizeQuantity, value ?? string.Empty); }
    public string NormalizeResult { get => _normalizeResult; private set => SetField(ref _normalizeResult, value ?? string.Empty); }

    // Bulk.
    public string BulkFileName { get => _bulkFileName; private set { if (SetField(ref _bulkFileName, value ?? string.Empty)) OnPropertyChanged(nameof(HasBulkFile)); } }
    public bool HasBulkFile => !string.IsNullOrWhiteSpace(BulkFileName);
    public bool BulkHasHeader { get => _bulkHasHeader; private set => SetField(ref _bulkHasHeader, value); }
    public string BulkSummary => HasBulkFile
        ? $"{Math.Max(0, DataRows().Length)} dòng dữ liệu · {BulkColumns.Count} cột từ tệp · {_bulkIdentification?.Identified ?? 0} SKU hợp lệ"
        : "Chưa chọn tệp.";
    public bool CanBulkPreview => IsNotBusy && _bulkIdentification is not null && BulkColumns.Any(col => !col.Locked && col.Mapping != "IGNORE");
    public bool CanBulkApply => IsNotBusy && _bulkPreview is not null && !_bulkApplied && !string.IsNullOrWhiteSpace(_bulkOperationKey);

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        if (!CanRead)
        {
            if (_access.Current.IsAuthenticated) SetError(ReadDeniedMessage);
            return;
        }

        var task = _initialLoadTask ??= LoadAsync();
        try
        {
            _loaded = await task.ConfigureAwait(true);
        }
        finally
        {
            if (ReferenceEquals(_initialLoadTask, task)) _initialLoadTask = null;
        }
    }

    public async Task RefreshAsync() => _loaded = await LoadAsync().ConfigureAwait(true);

    public void ResetFilters()
    {
        _search = string.Empty;
        _statusFilter = "all";
        _catalogFilter = "all";
        _orderableFilter = "all";
        OnPropertyChanged(nameof(Search));
        OnPropertyChanged(nameof(StatusFilter));
        OnPropertyChanged(nameof(CatalogFilter));
        OnPropertyChanged(nameof(OrderableFilter));
        RefreshProducts();
    }

    public void OpenProductCreate()
    {
        if (!CanWrite || IsBusy) return;
        OpenEditor(EditorKind.Product, null);
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftCatalogName = string.Empty;
        DraftCategoryId = string.Empty;
        DraftBrandId = string.Empty;
        DraftDescription = string.Empty;
        DraftNotes = string.Empty;
        DraftCatalogVisible = false;
        DraftOrderable = false;
        _draftHasActiveSellableVariant = false;
        OnPropertyChanged(nameof(CanEnableDraftOrderable));
        DraftInventoryManaged = true;
        DraftActive = true;
    }

    public async Task OpenProductEditAsync(string id)
    {
        if (!CanWrite || IsBusy) return;
        var item = _products.FirstOrDefault(product => product.Id == id);
        if (item is null) return;

        SetBusy("product-edit");
        ClearMessage();
        try
        {
            var variants = await _service.ListVariantsAsync(item.Id).ConfigureAwait(true);
            _draftHasActiveSellableVariant = variants.Any(row => row.IsActive && row.IsSellable);
            OpenEditor(EditorKind.Product, id);
            DraftCode = item.Code;
            DraftName = item.Name;
            DraftCatalogName = item.CatalogName ?? string.Empty;
            DraftCategoryId = item.CategoryId ?? string.Empty;
            DraftBrandId = item.BrandId ?? string.Empty;
            DraftDescription = item.Description ?? string.Empty;
            DraftNotes = item.Notes ?? string.Empty;
            DraftCatalogVisible = item.IsCatalogVisible;
            DraftOrderable = item.IsOrderable;
            DraftInventoryManaged = item.IsInventoryManaged;
            DraftActive = item.IsActive;
            OnPropertyChanged(nameof(CanEnableDraftOrderable));
        }
        catch (Exception exception)
        {
            SetFailure(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public void OpenCategoryCreate()
    {
        if (!CanWrite || IsBusy) return;
        OpenEditor(EditorKind.Category, null);
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftParentCategoryId = string.Empty;
        DraftDescription = string.Empty;
        DraftSortOrder = "0";
        DraftCatalogVisible = true;
        DraftActive = true;
    }

    public void OpenCategoryEdit(string id)
    {
        if (!CanWrite || IsBusy) return;
        var item = _categories.FirstOrDefault(row => row.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.Category, id);
        DraftCode = item.Code;
        DraftName = item.Name;
        DraftParentCategoryId = item.ParentCategoryId ?? string.Empty;
        DraftDescription = item.Description ?? string.Empty;
        DraftSortOrder = item.SortOrder.ToString(CultureInfo.InvariantCulture);
        DraftCatalogVisible = item.IsCatalogVisible;
        DraftActive = item.IsActive;
    }

    public void OpenBrandCreate()
    {
        if (!CanWrite || IsBusy) return;
        OpenEditor(EditorKind.Brand, null);
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftDescription = string.Empty;
        DraftCatalogVisible = true;
        DraftActive = true;
    }

    public void OpenBrandEdit(string id)
    {
        if (!CanWrite || IsBusy) return;
        var item = _brands.FirstOrDefault(row => row.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.Brand, id);
        DraftCode = item.Code;
        DraftName = item.Name;
        DraftDescription = item.Description ?? string.Empty;
        DraftCatalogVisible = item.IsCatalogVisible;
        DraftActive = item.IsActive;
    }

    public void OpenUnitCreate()
    {
        if (!CanWrite || IsBusy) return;
        OpenEditor(EditorKind.Unit, null);
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftSymbol = string.Empty;
        DraftUnitKind = "COUNT";
        DraftAllowsFractional = false;
        DraftActive = true;
    }

    public void OpenUnitEdit(string id)
    {
        if (!CanWrite || IsBusy) return;
        var item = _units.FirstOrDefault(row => row.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.Unit, id);
        DraftCode = item.Code;
        DraftName = item.Name;
        DraftSymbol = item.Symbol ?? string.Empty;
        DraftUnitKind = item.UnitKind;
        DraftAllowsFractional = item.AllowsFractional;
        DraftActive = item.IsActive;
    }

    public async Task OpenVariantManagerAsync(string productId)
    {
        var product = _products.FirstOrDefault(item => item.Id == productId);
        if (product is null || IsBusy) return;
        VariantProduct = product;
        await LoadVariantManagerAsync(product.Id).ConfigureAwait(true);
    }

    public void CloseVariantManager()
    {
        VariantProduct = null;
        SelectedVariant = null;
        VariantRows.Clear();
    }

    public void OpenVariantCreate()
    {
        if (VariantProduct is null || !CanWrite || IsBusy) return;
        OpenEditor(EditorKind.Variant, null);
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftVariantKind = VariantRows.Count == 0 ? "BASE" : "CARTON";
        DraftInventoryBase = VariantRows.Count == 0;
        DraftSellable = true;
        DraftCatalogVisible = false;
        DraftActive = true;
        DraftWeightValue = string.Empty;
        DraftWeightUomCode = "G";
        DraftLotTrackingMode = "NONE";
        DraftExpiryTrackingMode = "NONE";
    }

    public void OpenVariantEdit(string id)
    {
        if (!CanWrite || IsBusy) return;
        var item = VariantRows.Select(row => row.Source).FirstOrDefault(row => row.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.Variant, id);
        DraftCode = item.Sku;
        DraftName = item.Name;
        DraftVariantKind = item.VariantKind;
        DraftInventoryBase = item.IsInventoryBase;
        DraftSellable = item.IsSellable;
        DraftCatalogVisible = item.IsCatalogVisible;
        DraftActive = item.IsActive;
        DraftWeightValue = item.WeightValue ?? string.Empty;
        DraftWeightUomCode = item.WeightUomCode ?? "G";
        DraftLotTrackingMode = "NONE";
        DraftExpiryTrackingMode = "NONE";
    }

    public void CancelEditor() => OpenEditor(EditorKind.None, null);

    public async Task SaveEditorAsync()
    {
        if (!IsEditorOpen || IsBusy || !CanWrite) return;
        if (string.IsNullOrWhiteSpace(DraftCode) || string.IsNullOrWhiteSpace(DraftName))
        {
            SetError("Cần nhập đầy đủ mã và tên.");
            return;
        }

        SetBusy("save-editor");
        ClearMessage();
        try
        {
            switch (_editorKind)
            {
                case EditorKind.Product:
                    await SaveProductEditorAsync().ConfigureAwait(true);
                    break;
                case EditorKind.Category:
                    await SaveCategoryEditorAsync().ConfigureAwait(true);
                    break;
                case EditorKind.Brand:
                    await SaveBrandEditorAsync().ConfigureAwait(true);
                    break;
                case EditorKind.Variant:
                    await SaveVariantEditorAsync().ConfigureAwait(true);
                    break;
                case EditorKind.Unit:
                    await SaveUnitEditorAsync().ConfigureAwait(true);
                    break;
            }
            OpenEditor(EditorKind.None, null);
        }
        catch (Exception exception)
        {
            SetFailure(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public Task ToggleProductAsync(string id) => UpdateProductStatusAsync(id);
    public Task ToggleCategoryAsync(string id) => UpdateCategoryStatusAsync(id);
    public Task ToggleBrandAsync(string id) => UpdateBrandStatusAsync(id);
    public Task ToggleUnitAsync(string id) => UpdateUnitStatusAsync(id);

    public void StartQuickProductCreate()
    {
        if (!CanWrite || IsBusy) return;
        QuickCreatingProduct = true;
        QuickProductId = string.Empty;
        ClearQuickProductDraft();
        QuickVariants.Clear();
        ClearQuickVariant();
        ClearMessage();
    }

    public async Task SelectQuickProductAsync(string? productId)
    {
        var item = _products.FirstOrDefault(product => product.Id == productId);
        if (item is null)
        {
            QuickProductId = string.Empty;
            QuickCreatingProduct = false;
            ClearQuickProductDraft();
            QuickVariants.Clear();
            ClearQuickVariant();
            return;
        }

        QuickCreatingProduct = false;
        QuickProductId = item.Id;
        ApplyQuickProduct(item);
        await LoadQuickVariantsAsync(item.Id).ConfigureAwait(true);
        NotifyQuickStepState();
    }

    public async Task SaveQuickProductAsync()
    {
        if (!CanWrite || IsBusy) return;
        if (string.IsNullOrWhiteSpace(QuickProductCode) || string.IsNullOrWhiteSpace(QuickProductName))
        {
            SetError("Cần nhập mã sản phẩm và tên sản phẩm.");
            return;
        }

        SetBusy("quick-product");
        ClearMessage();
        try
        {
            ProductData saved;
            if (QuickCreatingProduct)
            {
                var request = new ProductCreateRequest(
                    QuickProductCode.Trim().ToUpperInvariant(),
                    QuickProductName.Trim(),
                    Optional(QuickCatalogName),
                    Optional(QuickCategoryId),
                    Optional(QuickBrandId),
                    Optional(QuickDescription),
                    Optional(QuickNotes),
                    QuickCatalogVisible,
                    false,
                    QuickInventoryManaged,
                    QuickProductActive);
                saved = await _service.CreateProductAsync(
                    request,
                    KeyFor("quick-product-create", "new", Fingerprint(request))).ConfigureAwait(true);
                _products.Add(saved);
            }
            else
            {
                var current = GetQuickProduct() ?? throw new InvalidOperationException("Không còn sản phẩm đã chọn.");
                saved = await _service.UpdateProductAsync(
                    current.Id,
                    new ProductUpdateRequest(
                        QuickProductName.Trim(),
                        Optional(QuickCatalogName),
                        Optional(QuickCategoryId),
                        Optional(QuickBrandId),
                        Optional(QuickDescription),
                        Optional(QuickNotes),
                        QuickCatalogVisible,
                        QuickOrderable,
                        QuickInventoryManaged,
                        QuickProductActive,
                        current.UpdatedAt)).ConfigureAwait(true);
                ReplaceById(_products, saved, item => item.Id);
            }

            RefreshMasterPresentation();
            QuickCreatingProduct = false;
            QuickProductId = saved.Id;
            ApplyQuickProduct(saved);
            await LoadQuickVariantsAsync(saved.Id).ConfigureAwait(true);
            SetNotice(QuickVariants.Count == 0 ? "Đã lưu sản phẩm. Tiếp tục tạo SKU." : "Đã cập nhật sản phẩm.");
        }
        catch (Exception exception)
        {
            SetFailure(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public void StartQuickVariantCreate()
    {
        if (!HasQuickProduct || !CanWrite || IsBusy) return;
        QuickCreatingVariant = true;
        QuickVariantId = string.Empty;
        QuickSku = string.Empty;
        QuickVariantName = string.Empty;
        QuickVariantKind = QuickVariants.Count == 0 ? "BASE" : "CARTON";
        QuickInventoryBase = QuickVariants.Count == 0;
        QuickSellable = true;
        QuickVariantCatalogVisible = false;
        QuickVariantActive = true;
        QuickWeightValue = string.Empty;
        QuickWeightUomCode = "G";
        QuickLotTrackingMode = "NONE";
        QuickExpiryTrackingMode = "NONE";
        ClearQuickSideData();
    }

    public async Task SelectQuickVariantAsync(string? variantId)
    {
        var item = QuickVariants.FirstOrDefault(row => row.Id == variantId);
        if (item is null)
        {
            ClearQuickVariant();
            return;
        }
        QuickCreatingVariant = false;
        QuickVariantId = item.Id;
        ApplyQuickVariant(item);
        await LoadQuickSideDataAsync(item).ConfigureAwait(true);
        NotifyQuickStepState();
    }

    public async Task SaveQuickVariantAsync()
    {
        if (!CanWrite || IsBusy || !HasQuickProduct) return;
        if (string.IsNullOrWhiteSpace(QuickSku) || string.IsNullOrWhiteSpace(QuickVariantName))
        {
            SetError("Cần nhập mã SKU và tên SKU.");
            return;
        }
        var weight = NormalizeDecimal(QuickWeightValue);
        if (weight is not null && !ProductPresentation.IsPositiveDecimal(weight))
        {
            SetError("Khối lượng phải lớn hơn 0.");
            return;
        }

        var product = GetQuickProduct() ?? throw new InvalidOperationException("Không còn sản phẩm đã chọn.");
        SetBusy("quick-variant");
        ClearMessage();
        try
        {
            ProductVariantData saved;
            if (QuickCreatingVariant)
            {
                var request = new ProductVariantCreateRequest(
                    QuickSku.Trim().ToUpperInvariant(),
                    QuickVariantName.Trim(),
                    QuickVariantKind,
                    QuickInventoryBase,
                    QuickSellable,
                    QuickVariantCatalogVisible,
                    QuickVariantActive,
                    weight,
                    weight is null ? null : QuickWeightUomCode);
                saved = await _service.CreateVariantAsync(
                    product.Id,
                    request,
                    KeyFor("quick-variant-create", product.Id, Fingerprint(request))).ConfigureAwait(true);

                if (saved.IsInventoryBase && (QuickLotTrackingMode != "NONE" || QuickExpiryTrackingMode != "NONE"))
                {
                    try
                    {
                        var policy = await _service.GetTrackingPolicyAsync(saved.Id).ConfigureAwait(true);
                        await _service.UpdateTrackingPolicyAsync(
                            saved.Id,
                            new ProductTrackingPolicyUpdateRequest(
                                saved.Id,
                                QuickLotTrackingMode,
                                QuickLotTrackingMode == "NONE" ? "NONE" : QuickExpiryTrackingMode,
                                policy.Version),
                            KeyFor("quick-policy", saved.Id, $"{policy.Version}:{QuickLotTrackingMode}:{QuickExpiryTrackingMode}"))
                            .ConfigureAwait(true);
                    }
                    catch (Exception policyError)
                    {
                        SetError($"Đã tạo SKU {saved.Sku}, nhưng chưa lưu được lựa chọn quản lý lô/hạn dùng. {ErrorText(policyError)}");
                    }
                }

                QuickVariants.Add(saved);
                NotifyQuickStepState();
            }
            else
            {
                var current = QuickVariants.FirstOrDefault(row => row.Id == QuickVariantId)
                    ?? throw new InvalidOperationException("Không còn SKU đã chọn.");
                saved = await _service.UpdateVariantAsync(
                    product.Id,
                    current.Id,
                    new ProductVariantUpdateRequest(
                        QuickVariantName.Trim(),
                        QuickVariantKind,
                        QuickInventoryBase,
                        QuickSellable,
                        QuickVariantCatalogVisible,
                        QuickVariantActive,
                        weight,
                        weight is null ? null : QuickWeightUomCode,
                        current.UpdatedAt)).ConfigureAwait(true);
                ReplaceById(QuickVariants, saved, item => item.Id);
                NotifyQuickStepState();
            }

            QuickCreatingVariant = false;
            QuickVariantId = saved.Id;
            ApplyQuickVariant(saved);
            await LoadQuickSideDataAsync(saved).ConfigureAwait(true);
            if (!MessageIsError) SetNotice("Đã lưu SKU. Tiếp tục đơn vị, mã vạch và giá.");
        }
        catch (Exception exception)
        {
            SetFailure(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public async Task SaveQuickUnitAsync()
    {
        var product = GetQuickProduct();
        var variant = GetQuickVariant();
        if (product is null || variant is null || !CanWrite || IsBusy) return;
        if (string.IsNullOrWhiteSpace(QuickUnitId))
        {
            SetError("Chọn đơn vị tính cho SKU.");
            return;
        }
        var conversion = variant.IsInventoryBase ? "1" : NormalizeDecimal(QuickConversion);
        if (!ProductPresentation.IsPositiveDecimal(conversion))
        {
            SetError("Hệ số quy đổi phải lớn hơn 0.");
            return;
        }
        var net = NormalizeDecimal(QuickNetContentValue);
        if (net is not null && !ProductPresentation.IsPositiveDecimal(net))
        {
            SetError("Khối lượng/dung tích mô tả phải lớn hơn 0.");
            return;
        }

        SetBusy("quick-unit");
        ClearMessage();
        try
        {
            var saved = await _service.UpdateVariantUnitAsync(
                product.Id,
                variant.Id,
                new ProductVariantUnitUpdateRequest(
                    QuickUnitId,
                    conversion!,
                    QuickPurchasable,
                    net is null ? null : new ProductNetContentRequest(net, QuickNetContentUnit),
                    Optional(QuickSourceUnitLabel),
                    Optional(QuickSourcePackageDescription),
                    variant.UpdatedAt)).ConfigureAwait(true);
            ReplaceById(QuickVariants, saved, item => item.Id);
            ApplyQuickVariant(saved);
            NotifyQuickStepState();
            SetNotice("Đã lưu đơn vị và hệ số quy đổi.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    public async Task AddQuickBarcodeAsync()
    {
        var product = GetQuickProduct();
        var variant = GetQuickVariant();
        if (product is null || variant is null || !CanWrite || IsBusy || string.IsNullOrWhiteSpace(QuickBarcode)) return;
        SetBusy("quick-barcode");
        ClearMessage();
        try
        {
            var request = new ProductBarcodeCreateRequest(
                QuickBarcode.Trim(),
                "INTERNAL",
                BarcodeRows.All(item => !item.Source.IsActive));
            await _service.CreateBarcodeAsync(
                product.Id,
                variant.Id,
                request,
                KeyFor("quick-barcode", variant.Id, Fingerprint(request))).ConfigureAwait(true);
            QuickBarcode = string.Empty;
            await LoadBarcodesAsync(product.Id, variant.Id).ConfigureAwait(true);
            SetNotice("Đã thêm mã vạch.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    public async Task ToggleQuickBarcodeAsync(string id)
    {
        var product = GetQuickProduct();
        var variant = GetQuickVariant();
        var barcode = BarcodeRows.Select(row => row.Source).FirstOrDefault(row => row.Id == id);
        if (product is null || variant is null || barcode is null || !CanWrite || IsBusy) return;
        SetBusy("quick-barcode-toggle");
        ClearMessage();
        try
        {
            await _service.UpdateBarcodeAsync(
                product.Id,
                variant.Id,
                barcode.Id,
                new ProductBarcodeUpdateRequest(!barcode.IsActive, barcode.IsPrimary && !barcode.IsActive, barcode.UpdatedAt))
                .ConfigureAwait(true);
            await LoadBarcodesAsync(product.Id, variant.Id).ConfigureAwait(true);
            SetNotice(barcode.IsActive ? "Đã ngừng sử dụng mã vạch." : "Đã đưa mã vạch vào sử dụng.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    public async Task SelectQuickPriceListAsync(string? priceListId)
    {
        QuickPriceListId = priceListId ?? string.Empty;
        _quickSimplePriceItem = null;
        QuickDirectPriceCount = 0;
        QuickComplexPriceCount = 0;
        QuickPriceAmount = string.Empty;
        var variant = GetQuickVariant();
        if (variant is null || string.IsNullOrWhiteSpace(QuickPriceListId) || !CanReadPrice) return;

        try
        {
            var items = await _service.ListPriceItemsAsync(QuickPriceListId).ConfigureAwait(true);
            var direct = items.Where(item =>
                item.VariantId == variant.Id
                && item.AdjustmentType == "FIXED_PRICE"
                && item.IsActive
                && ProductPresentation.IsZeroQuantity(item.MinQuantity)
                && string.IsNullOrWhiteSpace(item.MaxQuantity)).ToArray();
            var directIds = direct.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
            var complex = items.Count(item => item.VariantId == variant.Id && item.IsActive && !directIds.Contains(item.Id));
            QuickDirectPriceCount = direct.Length;
            QuickComplexPriceCount = complex;
            _quickSimplePriceItem = direct.Length == 1 ? direct[0] : null;
            QuickPriceAmount = _quickSimplePriceItem?.AmountMinor ?? string.Empty;
        }
        catch (Exception exception) { SetFailure(exception); }
    }

    public async Task SaveQuickPriceAsync()
    {
        var variant = GetQuickVariant();
        if (variant is null || !CanWritePrice || IsBusy || string.IsNullOrWhiteSpace(QuickPriceListId)) return;
        if (QuickDirectPriceCount > 1)
        {
            SetError("Bảng giá này có nhiều mức giá trực tiếp cùng áp dụng cho SKU. Dùng màn Giá bán và khuyến mãi để chọn đúng dòng cần sửa.");
            return;
        }
        if (!long.TryParse(QuickPriceAmount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            SetError("Giá phải là số nguyên VND lớn hơn 0.");
            return;
        }

        SetBusy("quick-price");
        ClearMessage();
        try
        {
            ProductPriceItemData saved;
            if (_quickSimplePriceItem is not null)
            {
                saved = await _service.UpdatePriceItemAsync(
                    QuickPriceListId,
                    _quickSimplePriceItem.Id,
                    new ProductPriceItemUpdateRequest(QuickPriceAmount.Trim(), _quickSimplePriceItem.UpdatedAt))
                    .ConfigureAwait(true);
            }
            else
            {
                var request = new ProductPriceItemCreateRequest(
                    variant.Id, "FIXED_PRICE", QuickPriceAmount.Trim(), null,
                    "0", null, null, null, null, "Thiết lập nhanh theo SKU", "ADMIN", true);
                saved = await _service.CreatePriceItemAsync(
                    QuickPriceListId,
                    request,
                    KeyFor("quick-price-create", variant.Id, $"{QuickPriceListId}:{QuickPriceAmount.Trim()}"))
                    .ConfigureAwait(true);
            }
            _quickSimplePriceItem = saved;
            QuickPriceAmount = saved.AmountMinor ?? QuickPriceAmount;
            QuickDirectPriceCount = 1;
            SetNotice("Đã lưu giá SKU.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    public async Task UploadQuickImageAsync(string filePath)
    {
        var product = GetQuickProduct();
        if (product is null || !CanWrite || IsBusy) return;
        SetBusy("quick-image");
        ClearMessage();
        try
        {
            var bytes = await ProductImageProcessor.ToWebpAsync(filePath).ConfigureAwait(true);
            await _service.UploadWebpAsync(
                product.Id,
                bytes,
                KeyFor(
                    "product-image",
                    product.Id,
                    $"{product.UpdatedAt}:{bytes.LongLength}:{Convert.ToHexString(SHA256.HashData(bytes))}"))
                .ConfigureAwait(true);
            _imageCodes.Add(product.Code);
            NotifyQuickImage();
            RefreshProducts();
            SetNotice("Đã cập nhật ảnh sản phẩm.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    public async Task DeleteQuickImageAsync()
    {
        var product = GetQuickProduct();
        if (product is null || !_imageCodes.Contains(product.Code) || !CanWrite || IsBusy) return;
        SetBusy("quick-image-delete");
        ClearMessage();
        try
        {
            await _service.DeleteImageAsync(
                product.Id,
                KeyFor("product-image-delete", product.Id, product.UpdatedAt)).ConfigureAwait(true);
            _imageCodes.Remove(product.Code);
            NotifyQuickImage();
            RefreshProducts();
            SetNotice("Đã xóa ảnh sản phẩm.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    // Unit workspace.
    public async Task SelectUnitProductAsync(string? productId)
    {
        UnitProductId = productId ?? string.Empty;
        UnitVariantId = string.Empty;
        UnitVariants.Clear();
        BarcodeRows.Clear();
        NormalizeResult = string.Empty;
        if (string.IsNullOrWhiteSpace(UnitProductId)) return;
        try
        {
            Replace(UnitVariants, await _service.ListVariantsAsync(UnitProductId).ConfigureAwait(true));
        }
        catch (Exception exception) { SetFailure(exception); }
    }

    public async Task SelectUnitVariantAsync(string? variantId)
    {
        UnitVariantId = variantId ?? string.Empty;
        var variant = UnitVariants.FirstOrDefault(row => row.Id == UnitVariantId);
        if (variant is null)
        {
            BarcodeRows.Clear();
            return;
        }
        ApplyUnitSetup(variant);
        await LoadBarcodesAsync(UnitProductId, variant.Id).ConfigureAwait(true);
    }

    public async Task SaveUnitSetupAsync()
    {
        var variant = UnitVariants.FirstOrDefault(row => row.Id == UnitVariantId);
        if (variant is null || !CanWrite || IsBusy || string.IsNullOrWhiteSpace(UnitUnitId)) return;
        var conversion = variant.IsInventoryBase ? "1" : NormalizeDecimal(UnitConversion);
        if (!ProductPresentation.IsPositiveDecimal(conversion))
        {
            SetError("Hệ số quy đổi phải lớn hơn 0.");
            return;
        }
        var net = NormalizeDecimal(UnitNetContentValue);
        if (net is not null && !ProductPresentation.IsPositiveDecimal(net))
        {
            SetError("Khối lượng/dung tích mô tả phải lớn hơn 0.");
            return;
        }

        SetBusy("unit-setup");
        ClearMessage();
        try
        {
            var saved = await _service.UpdateVariantUnitAsync(
                UnitProductId,
                variant.Id,
                new ProductVariantUnitUpdateRequest(
                    UnitUnitId, conversion!, UnitPurchasable,
                    net is null ? null : new ProductNetContentRequest(net, UnitNetContentUnit),
                    Optional(UnitSourceLabel), Optional(UnitPackageDescription), variant.UpdatedAt))
                .ConfigureAwait(true);
            ReplaceById(UnitVariants, saved, item => item.Id);
            ApplyUnitSetup(saved);
            SetNotice("Đã lưu thiết lập đơn vị của SKU.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    public async Task AddUnitBarcodeAsync()
    {
        var variant = UnitVariants.FirstOrDefault(row => row.Id == UnitVariantId);
        if (variant is null || !CanWrite || IsBusy || string.IsNullOrWhiteSpace(UnitBarcode)) return;
        SetBusy("unit-barcode");
        ClearMessage();
        try
        {
            var request = new ProductBarcodeCreateRequest(UnitBarcode.Trim(), "INTERNAL", BarcodeRows.All(item => !item.Source.IsActive));
            await _service.CreateBarcodeAsync(UnitProductId, variant.Id, request,
                KeyFor("unit-barcode", variant.Id, Fingerprint(request))).ConfigureAwait(true);
            UnitBarcode = string.Empty;
            await LoadBarcodesAsync(UnitProductId, variant.Id).ConfigureAwait(true);
            SetNotice("Đã thêm mã vạch.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    public async Task ToggleUnitBarcodeAsync(string id)
    {
        var variant = UnitVariants.FirstOrDefault(row => row.Id == UnitVariantId);
        var barcode = BarcodeRows.Select(row => row.Source).FirstOrDefault(row => row.Id == id);
        if (variant is null || barcode is null || !CanWrite || IsBusy) return;
        SetBusy("unit-barcode-toggle");
        ClearMessage();
        try
        {
            await _service.UpdateBarcodeAsync(
                UnitProductId, variant.Id, barcode.Id,
                new ProductBarcodeUpdateRequest(!barcode.IsActive, barcode.IsPrimary && !barcode.IsActive, barcode.UpdatedAt))
                .ConfigureAwait(true);
            await LoadBarcodesAsync(UnitProductId, variant.Id).ConfigureAwait(true);
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    public async Task NormalizeUnitQuantityAsync()
    {
        var variant = UnitVariants.FirstOrDefault(row => row.Id == UnitVariantId);
        if (variant is null || IsBusy || !ProductPresentation.IsPositiveDecimal(NormalizeDecimal(NormalizeQuantity))) return;
        SetBusy("normalize");
        ClearMessage();
        try
        {
            var result = await _service.NormalizeQuantityAsync(UnitProductId, variant.Id, NormalizeDecimal(NormalizeQuantity)!).ConfigureAwait(true);
            NormalizeResult = $"{InventoryPresentation.Quantity(result.EnteredQuantity)} {result.UnitCode} → {InventoryPresentation.Quantity(result.BaseQuantity)} đơn vị tồn chuẩn";
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    // Bulk update.
    public async Task LoadBulkFileAsync(string filePath)
    {
        if (IsBusy) return;
        SetBusy("bulk-file");
        ClearMessage();
        try
        {
            _bulkMatrix = await SpreadsheetMatrixReader.ReadAsync(filePath).ConfigureAwait(true);
            if (_bulkMatrix.Count == 0 || _bulkMatrix.Max(row => row.Length) < 2)
                throw new InvalidOperationException("Tệp cần có cột 1 là SKU và ít nhất một cột dữ liệu.");

            var dataRowCount = Math.Max(0, _bulkMatrix.Count - 1);
            if (dataRowCount > 5_000)
                throw new InvalidOperationException("Mỗi lần chỉ cập nhật tối đa 5.000 dòng.");

            BulkFileName = Path.GetFileName(filePath);
            BulkHasHeader = true;
            BuildBulkColumns();
            await IdentifyBulkAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ResetBulkFile();
            SetFailure(exception);
        }
        finally { SetBusy(null); }
    }

    public void ResetBulkFile()
    {
        _bulkMatrix = [];
        BulkFileName = string.Empty;
        BulkColumns.Clear();
        BulkSourceRows.Clear();
        BulkPreviewRows.Clear();
        _bulkIdentification = null;
        _bulkPreview = null;
        _bulkOperationKey = null;
        _bulkApplied = false;
        NotifyBulkState();
    }

    public async Task SetBulkHeaderAsync(bool hasHeader)
    {
        if (_bulkMatrix.Count == 0) return;
        BulkHasHeader = hasHeader;
        await IdentifyBulkAsync().ConfigureAwait(true);
    }

    public void BulkMappingChanged()
    {
        _bulkPreview = null;
        _bulkOperationKey = null;
        _bulkApplied = false;
        BulkPreviewRows.Clear();
        NotifyBulkState();
    }

    public async Task PreviewBulkAsync()
    {
        if (!CanBulkPreview || IsBusy) return;
        var mappings = BulkColumns.OrderBy(col => col.Index).Select(col => col.Locked ? "SKU" : col.Mapping).ToArray();
        if (mappings.Skip(1).Where(value => value != "IGNORE").GroupBy(value => value).Any(group => group.Count() > 1))
        {
            SetError("Một thuộc tính chỉ được chọn cho một cột.");
            return;
        }
        var request = new ProductBulkUpdateRequest(true, mappings, DataRows());
        SetBusy("bulk-preview");
        ClearMessage();
        try
        {
            _bulkPreview = await _service.PreviewBulkUpdateAsync(request).ConfigureAwait(true);
            _bulkOperationKey = _bulkPreview.OperationKey;
            _bulkApplied = false;
            BuildBulkPreviewRows();
            SetNotice($"Đã đối chiếu {_bulkPreview.Rows.Length} dòng. {_bulkPreview.Skipped} dòng có lỗi sẽ được bỏ qua.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally
        {
            SetBusy(null);
            NotifyBulkState();
        }
    }

    public async Task ApplyBulkAsync()
    {
        if (!CanBulkApply || IsBusy || _bulkOperationKey is null) return;
        var mappings = BulkColumns.OrderBy(col => col.Index).Select(col => col.Locked ? "SKU" : col.Mapping).ToArray();
        var request = new ProductBulkUpdateRequest(false, mappings, DataRows());
        SetBusy("bulk-apply");
        ClearMessage();
        try
        {
            _bulkPreview = await _service.ApplyBulkUpdateAsync(request, _bulkOperationKey).ConfigureAwait(true);
            _bulkApplied = true;
            BuildBulkPreviewRows();
            await LoadAsync(preserveMessage: true).ConfigureAwait(true);
            SetNotice($"Đã cập nhật {_bulkPreview.Updated} dòng. {_bulkPreview.Skipped} dòng lỗi đã được bỏ qua.");
        }
        catch (Exception exception) { SetFailure(exception); }
        finally
        {
            SetBusy(null);
            NotifyBulkState();
        }
    }

    private async Task<bool> LoadAsync(bool preserveMessage = false)
    {
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetError(ReadDeniedMessage);
            return false;
        }
        var generation = _accessGeneration;
        SetBusy("load");
        if (!preserveMessage) ClearMessage();
        try
        {
            var productsTask = _service.ListProductsAsync();
            var categoriesTask = _service.ListCategoriesAsync();
            var brandsTask = _service.ListBrandsAsync();
            var unitsTask = _service.ListUnitsAsync();
            ProductImageIndexData? images = null;

            var products = await productsTask.ConfigureAwait(true);
            var categories = await categoriesTask.ConfigureAwait(true);
            var brands = await brandsTask.ConfigureAwait(true);
            var units = await unitsTask.ConfigureAwait(true);
            try { images = await _service.GetImageIndexAsync().ConfigureAwait(true); }
            catch (Exception exception)
            {
                if (!preserveMessage) SetError($"Danh mục sản phẩm đã tải, nhưng trạng thái ảnh chưa tải được. {ErrorText(exception)}");
            }

            if (generation != _accessGeneration || !CanRead) return false;
            Replace(_products, products);
            Replace(_categories, categories);
            Replace(_brands, brands);
            Replace(_units, units);
            _imageCodes.Clear();
            if (images is not null)
                foreach (var code in images.Codes) _imageCodes.Add(code);

            RefreshMasterPresentation();
            return true;
        }
        catch (Exception exception)
        {
            _loaded = false;
            SetFailure(exception);
            return false;
        }
        finally { if (_busyAction == "load") SetBusy(null); }
    }

    private void RefreshMasterPresentation()
    {
        RefreshLookups();
        RefreshProducts();
        RefreshCategories();
        RefreshBrands();
        RefreshUnits();
        RefreshQuickProductOptions();
        OnPropertyChanged(nameof(ProductSummary));
        NotifyQuickImage();
    }

    private void RefreshProducts()
    {
        var term = Search.Trim().ToLower(CultureInfo.GetCultureInfo("vi-VN"));
        var rows = _products
            .Where(item =>
                (StatusFilter == "all" || (StatusFilter == "active" ? item.IsActive : !item.IsActive))
                && (CatalogFilter == "all" || (CatalogFilter == "visible" ? item.IsCatalogVisible : !item.IsCatalogVisible))
                && (OrderableFilter == "all" || (OrderableFilter == "yes" ? item.IsOrderable : !item.IsOrderable))
                && (term.Length == 0 || string.Join(" ", item.Code, item.Name, item.CatalogName, item.CategoryName, item.BrandName)
                    .ToLower(CultureInfo.GetCultureInfo("vi-VN")).Contains(term, StringComparison.Ordinal)))
            .OrderBy(item => item.Code, StringComparer.Ordinal)
            .Select((item, index) => new ProductRow(
                index + 1, item.Id, item.Code, _imageCodes.Contains(item.Code) ? "Có" : "—",
                item.Name, item.CategoryName ?? "—", item.BrandName ?? "—",
                ProductPresentation.YesNo(item.IsCatalogVisible), ProductPresentation.YesNo(item.IsOrderable),
                ProductPresentation.Active(item.IsActive), ProductPresentation.Toggle(item.IsActive), item));
        Replace(ProductRows, rows);
    }

    private void RefreshCategories()
    {
        var byId = _categories.ToDictionary(item => item.Id, StringComparer.Ordinal);
        Replace(CategoryRows, _categories.OrderBy(item => item.SortOrder).ThenBy(item => item.Code, StringComparer.Ordinal)
            .Select((item, index) => new ProductCategoryRow(
                index + 1, item.Id, item.Code, item.Name,
                item.ParentCategoryId is not null && byId.TryGetValue(item.ParentCategoryId, out var parent) ? parent.Name : "—",
                item.SortOrder, ProductPresentation.YesNo(item.IsCatalogVisible), ProductPresentation.Active(item.IsActive),
                ProductPresentation.Toggle(item.IsActive), item)));
    }

    private void RefreshBrands() =>
        Replace(BrandRows, _brands.OrderBy(item => item.Code, StringComparer.Ordinal).Select((item, index) =>
            new ProductBrandRow(index + 1, item.Id, item.Code, item.Name,
                ProductPresentation.YesNo(item.IsCatalogVisible), ProductPresentation.Active(item.IsActive),
                ProductPresentation.Toggle(item.IsActive), item)));

    private void RefreshUnits() =>
        Replace(UnitRows, _units.OrderBy(item => item.Code, StringComparer.Ordinal).Select((item, index) =>
            new ProductUnitRow(index + 1, item.Id, item.Code, item.Name, item.Symbol ?? "—",
                ProductPresentation.UnitKind(item.UnitKind), ProductPresentation.YesNo(item.AllowsFractional),
                ProductPresentation.Active(item.IsActive), ProductPresentation.Toggle(item.IsActive), item)));

    private void RefreshLookups()
    {
        Replace(CategoryOptions, new[] { new ProductLookupOption("", "Chưa phân loại") }.Concat(
            _categories.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new ProductLookupOption(item.Id, $"{item.Code} — {item.Name}"))));
        Replace(BrandOptions, new[] { new ProductLookupOption("", "Chưa có nhãn hàng") }.Concat(
            _brands.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new ProductLookupOption(item.Id, $"{item.Code} — {item.Name}"))));
        Replace(ProductOptions, _products.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new ProductLookupOption(item.Id, $"{item.Code} — {item.Name}")));
        Replace(UnitOptions, _units.Where(item => item.IsActive).OrderBy(item => item.Code).Select(item => new ProductLookupOption(item.Id, $"{item.Name} ({item.Code})")));
    }

    private void RefreshQuickProductOptions()
    {
        var term = QuickSearch.Trim().ToLower(CultureInfo.GetCultureInfo("vi-VN"));
        Replace(ProductOptions, _products.Where(item =>
                item.IsActive && (term.Length == 0 || string.Join(" ", item.Code, item.Name, item.CatalogName, item.CategoryName, item.BrandName)
                    .ToLower(CultureInfo.GetCultureInfo("vi-VN")).Contains(term, StringComparison.Ordinal)))
            .OrderBy(item => item.Code)
            .Select(item => new ProductLookupOption(item.Id, $"{item.Code} — {item.Name}")));
    }

    private async Task LoadVariantManagerAsync(string productId)
    {
        SetBusy("variants");
        ClearMessage();
        try
        {
            var variants = await _service.ListVariantsAsync(productId).ConfigureAwait(true);
            Replace(VariantRows, variants.OrderBy(item => item.Sku).Select((item, index) =>
                new ProductVariantRow(index + 1, item.Id, item.Sku, item.Name,
                    ProductPresentation.VariantKind(item.VariantKind), ProductPresentation.Weight(item),
                    ProductPresentation.YesNo(item.IsInventoryBase), ProductPresentation.YesNo(item.IsSellable),
                    ProductPresentation.YesNo(item.IsCatalogVisible), ProductPresentation.Active(item.IsActive), item)));
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    private async Task LoadQuickVariantsAsync(string productId)
    {
        ClearQuickVariant();
        try
        {
            var variants = await _service.ListVariantsAsync(productId).ConfigureAwait(true);
            Replace(QuickVariants, variants.OrderBy(item => item.Sku));
            var first = QuickVariants.FirstOrDefault();
            if (first is not null) await SelectQuickVariantAsync(first.Id).ConfigureAwait(true);
        }
        catch (Exception exception) { SetFailure(exception); }
        NotifyQuickStepState();
    }

    private async Task LoadQuickSideDataAsync(ProductVariantData variant)
    {
        ClearQuickSideData();
        try { await LoadBarcodesAsync(QuickProductId, variant.Id).ConfigureAwait(true); }
        catch (Exception exception) { SetFailure(exception); }

        if (CanReadPrice)
        {
            try
            {
                var lists = await _service.ListPriceListsAsync().ConfigureAwait(true);
                Replace(PriceListOptions, lists.Where(item => item.IsActive && item.CurrencyCode == "VND")
                    .OrderBy(item => item.Priority)
                    .Select(item => new ProductLookupOption(item.Id, $"{ProductPresentation.PriceListType(item.ListType)} — {item.Name}")));
            }
            catch (Exception exception) { SetFailure(exception); }
        }

        if (CanReadInventory)
        {
            try
            {
                var balances = await _inventory.ListBalancesAsync().ConfigureAwait(true);
                Replace(QuickInventoryRows, balances
                    .Where(row => row.BaseVariantId == variant.Id)
                    .Take(8)
                    .Select((row, index) => new ProductInventoryRow(
                        index + 1, row.WarehouseName,
                        InventoryPresentation.First(row.LocationName, row.LocationCode, "—"),
                        InventoryPresentation.Quantity(row.OnHandQuantity),
                        InventoryPresentation.Quantity(row.ReservedQuantity),
                        InventoryPresentation.Quantity(row.AvailableQuantity))));
            }
            catch (Exception exception) { SetFailure(exception); }
        }
    }

    private async Task LoadBarcodesAsync(string productId, string variantId)
    {
        var barcodes = await _service.ListBarcodesAsync(productId, variantId).ConfigureAwait(true);
        Replace(BarcodeRows, barcodes.OrderByDescending(item => item.IsPrimary).ThenBy(item => item.Barcode)
            .Select((item, index) => new ProductBarcodeRow(index + 1, item.Id, item.Barcode, item.BarcodeType,
                ProductPresentation.YesNo(item.IsPrimary), ProductPresentation.Active(item.IsActive),
                ProductPresentation.Toggle(item.IsActive), item)));
    }

    private async Task SaveProductEditorAsync()
    {
        if (IsCreateMode)
        {
            var request = new ProductCreateRequest(
                DraftCode.Trim().ToUpperInvariant(), DraftName.Trim(), Optional(DraftCatalogName),
                Optional(DraftCategoryId), Optional(DraftBrandId), Optional(DraftDescription), Optional(DraftNotes),
                DraftCatalogVisible, false, DraftInventoryManaged, DraftActive);
            _products.Add(await _service.CreateProductAsync(request, KeyFor("product-create", "new", Fingerprint(request))).ConfigureAwait(true));
        }
        else
        {
            var current = _products.First(item => item.Id == _editingId);
            var saved = await _service.UpdateProductAsync(current.Id,
                new ProductUpdateRequest(
                    DraftName.Trim(), Optional(DraftCatalogName), Optional(DraftCategoryId), Optional(DraftBrandId),
                    Optional(DraftDescription), Optional(DraftNotes), DraftCatalogVisible, DraftOrderable,
                    DraftInventoryManaged, DraftActive, current.UpdatedAt)).ConfigureAwait(true);
            ReplaceById(_products, saved, item => item.Id);
        }
        RefreshMasterPresentation();
        SetNotice(IsCreateMode ? "Đã tạo sản phẩm." : "Đã cập nhật sản phẩm.");
    }

    private async Task SaveCategoryEditorAsync()
    {
        if (!int.TryParse(DraftSortOrder, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sortOrder) || sortOrder < 0)
            throw new InvalidOperationException("Thứ tự phải là số nguyên không âm.");
        if (IsCreateMode)
        {
            var request = new ProductCategoryCreateRequest(
                DraftCode.Trim().ToUpperInvariant(), DraftName.Trim(), Optional(DraftParentCategoryId),
                Optional(DraftDescription), sortOrder, DraftCatalogVisible, DraftActive);
            _categories.Add(await _service.CreateCategoryAsync(request, KeyFor("category-create", "new", Fingerprint(request))).ConfigureAwait(true));
        }
        else
        {
            var current = _categories.First(item => item.Id == _editingId);
            var saved = await _service.UpdateCategoryAsync(current.Id,
                new ProductCategoryUpdateRequest(
                    DraftName.Trim(), Optional(DraftParentCategoryId), Optional(DraftDescription),
                    sortOrder, DraftCatalogVisible, DraftActive, current.UpdatedAt)).ConfigureAwait(true);
            ReplaceById(_categories, saved, item => item.Id);
        }
        RefreshMasterPresentation();
        SetNotice("Đã lưu loại sản phẩm.");
    }

    private async Task SaveBrandEditorAsync()
    {
        if (IsCreateMode)
        {
            var request = new ProductBrandCreateRequest(
                DraftCode.Trim().ToUpperInvariant(), DraftName.Trim(), Optional(DraftDescription), DraftCatalogVisible, DraftActive);
            _brands.Add(await _service.CreateBrandAsync(request, KeyFor("brand-create", "new", Fingerprint(request))).ConfigureAwait(true));
        }
        else
        {
            var current = _brands.First(item => item.Id == _editingId);
            var saved = await _service.UpdateBrandAsync(current.Id,
                new ProductBrandUpdateRequest(DraftName.Trim(), Optional(DraftDescription), DraftCatalogVisible, DraftActive, current.UpdatedAt))
                .ConfigureAwait(true);
            ReplaceById(_brands, saved, item => item.Id);
        }
        RefreshMasterPresentation();
        SetNotice("Đã lưu nhãn hàng.");
    }

    private async Task SaveVariantEditorAsync()
    {
        if (VariantProduct is null) throw new InvalidOperationException("Không còn sản phẩm đang quản lý SKU.");
        var weight = NormalizeDecimal(DraftWeightValue);
        if (weight is not null && !ProductPresentation.IsPositiveDecimal(weight))
            throw new InvalidOperationException("Khối lượng phải lớn hơn 0.");

        ProductVariantData saved;
        if (IsCreateMode)
        {
            var request = new ProductVariantCreateRequest(
                DraftCode.Trim().ToUpperInvariant(), DraftName.Trim(), DraftVariantKind,
                DraftInventoryBase, DraftSellable, DraftCatalogVisible, DraftActive,
                weight, weight is null ? null : DraftWeightUomCode);
            saved = await _service.CreateVariantAsync(
                VariantProduct.Id, request, KeyFor("variant-create", VariantProduct.Id, Fingerprint(request))).ConfigureAwait(true);

            if (saved.IsInventoryBase && (DraftLotTrackingMode != "NONE" || DraftExpiryTrackingMode != "NONE"))
            {
                var policy = await _service.GetTrackingPolicyAsync(saved.Id).ConfigureAwait(true);
                await _service.UpdateTrackingPolicyAsync(
                    saved.Id,
                    new ProductTrackingPolicyUpdateRequest(
                        saved.Id, DraftLotTrackingMode,
                        DraftLotTrackingMode == "NONE" ? "NONE" : DraftExpiryTrackingMode,
                        policy.Version),
                    KeyFor("variant-policy", saved.Id, $"{policy.Version}:{DraftLotTrackingMode}:{DraftExpiryTrackingMode}"))
                    .ConfigureAwait(true);
            }
        }
        else
        {
            var current = VariantRows.Select(row => row.Source).First(item => item.Id == _editingId);
            saved = await _service.UpdateVariantAsync(
                VariantProduct.Id, current.Id,
                new ProductVariantUpdateRequest(
                    DraftName.Trim(), DraftVariantKind, DraftInventoryBase, DraftSellable,
                    DraftCatalogVisible, DraftActive, weight, weight is null ? null : DraftWeightUomCode,
                    current.UpdatedAt)).ConfigureAwait(true);
        }
        await LoadVariantManagerAsync(VariantProduct.Id).ConfigureAwait(true);
        SelectedVariant = saved;
        SetNotice("Đã lưu SKU.");
    }

    private async Task SaveUnitEditorAsync()
    {
        if (IsCreateMode)
        {
            var request = new ProductUnitCreateRequest(
                DraftCode.Trim().ToUpperInvariant(), DraftName.Trim(), Optional(DraftSymbol),
                DraftUnitKind, DraftAllowsFractional, DraftActive);
            _units.Add(await _service.CreateUnitAsync(request, KeyFor("unit-create", "new", Fingerprint(request))).ConfigureAwait(true));
        }
        else
        {
            var current = _units.First(item => item.Id == _editingId);
            var saved = await _service.UpdateUnitAsync(current.Id,
                new ProductUnitUpdateRequest(DraftName.Trim(), Optional(DraftSymbol), DraftUnitKind,
                    DraftAllowsFractional, DraftActive, current.UpdatedAt)).ConfigureAwait(true);
            ReplaceById(_units, saved, item => item.Id);
        }
        RefreshMasterPresentation();
        SetNotice("Đã lưu đơn vị tính.");
    }

    private async Task UpdateProductStatusAsync(string id)
    {
        var item = _products.FirstOrDefault(row => row.Id == id);
        if (item is null || !CanWrite || IsBusy) return;
        SetBusy("product-status");
        try
        {
            var saved = await _service.UpdateProductAsync(item.Id,
                new ProductUpdateRequest(item.Name, item.CatalogName, item.CategoryId, item.BrandId,
                    item.Description, item.Notes, item.IsCatalogVisible, item.IsOrderable,
                    item.IsInventoryManaged, !item.IsActive, item.UpdatedAt)).ConfigureAwait(true);
            ReplaceById(_products, saved, row => row.Id);
            RefreshMasterPresentation();
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    private async Task UpdateCategoryStatusAsync(string id)
    {
        var item = _categories.FirstOrDefault(row => row.Id == id);
        if (item is null || !CanWrite || IsBusy) return;
        SetBusy("category-status");
        try
        {
            var saved = await _service.UpdateCategoryAsync(item.Id,
                new ProductCategoryUpdateRequest(item.Name, item.ParentCategoryId, item.Description, item.SortOrder,
                    item.IsCatalogVisible, !item.IsActive, item.UpdatedAt)).ConfigureAwait(true);
            ReplaceById(_categories, saved, row => row.Id);
            RefreshMasterPresentation();
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    private async Task UpdateBrandStatusAsync(string id)
    {
        var item = _brands.FirstOrDefault(row => row.Id == id);
        if (item is null || !CanWrite || IsBusy) return;
        SetBusy("brand-status");
        try
        {
            var saved = await _service.UpdateBrandAsync(item.Id,
                new ProductBrandUpdateRequest(item.Name, item.Description, item.IsCatalogVisible, !item.IsActive, item.UpdatedAt)).ConfigureAwait(true);
            ReplaceById(_brands, saved, row => row.Id);
            RefreshMasterPresentation();
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    private async Task UpdateUnitStatusAsync(string id)
    {
        var item = _units.FirstOrDefault(row => row.Id == id);
        if (item is null || !CanWrite || IsBusy) return;
        SetBusy("unit-status");
        try
        {
            var saved = await _service.UpdateUnitAsync(item.Id,
                new ProductUnitUpdateRequest(item.Name, item.Symbol, item.UnitKind, item.AllowsFractional, !item.IsActive, item.UpdatedAt)).ConfigureAwait(true);
            ReplaceById(_units, saved, row => row.Id);
            RefreshMasterPresentation();
        }
        catch (Exception exception) { SetFailure(exception); }
        finally { SetBusy(null); }
    }

    private void ApplyQuickProduct(ProductData item)
    {
        QuickProductCode = item.Code;
        QuickProductName = item.Name;
        QuickCatalogName = item.CatalogName ?? string.Empty;
        QuickCategoryId = item.CategoryId ?? string.Empty;
        QuickBrandId = item.BrandId ?? string.Empty;
        QuickDescription = item.Description ?? string.Empty;
        QuickNotes = item.Notes ?? string.Empty;
        QuickCatalogVisible = item.IsCatalogVisible;
        QuickOrderable = item.IsOrderable;
        QuickInventoryManaged = item.IsInventoryManaged;
        QuickProductActive = item.IsActive;
        NotifyQuickImage();
    }

    private void ClearQuickProductDraft()
    {
        QuickProductCode = string.Empty;
        QuickProductName = string.Empty;
        QuickCatalogName = string.Empty;
        QuickCategoryId = string.Empty;
        QuickBrandId = string.Empty;
        QuickDescription = string.Empty;
        QuickNotes = string.Empty;
        QuickCatalogVisible = false;
        QuickOrderable = false;
        QuickInventoryManaged = true;
        QuickProductActive = true;
        NotifyQuickImage();
    }

    private void ApplyQuickVariant(ProductVariantData item)
    {
        QuickSku = item.Sku;
        QuickVariantName = item.Name;
        QuickVariantKind = item.VariantKind;
        QuickWeightValue = item.WeightValue ?? string.Empty;
        QuickWeightUomCode = item.WeightUomCode ?? "G";
        QuickInventoryBase = item.IsInventoryBase;
        QuickSellable = item.IsSellable;
        QuickVariantCatalogVisible = item.IsCatalogVisible;
        QuickVariantActive = item.IsActive;
        QuickUnitId = item.UnitId ?? string.Empty;
        QuickConversion = item.IsInventoryBase ? "1" : item.ConversionToBase ?? string.Empty;
        QuickNetContentValue = item.NetContentValue ?? string.Empty;
        QuickNetContentUnit = item.NetContentUomCode ?? "G";
        QuickPurchasable = item.IsPurchasable;
        QuickSourceUnitLabel = item.SourceUnitLabel ?? string.Empty;
        QuickSourcePackageDescription = item.SourcePackageDescription ?? string.Empty;
    }

    private void ClearQuickVariant()
    {
        QuickVariantId = string.Empty;
        QuickCreatingVariant = false;
        QuickSku = string.Empty;
        QuickVariantName = string.Empty;
        QuickVariantKind = "BASE";
        QuickWeightValue = string.Empty;
        QuickWeightUomCode = "G";
        QuickInventoryBase = false;
        QuickSellable = true;
        QuickVariantCatalogVisible = false;
        QuickVariantActive = true;
        QuickLotTrackingMode = "NONE";
        QuickExpiryTrackingMode = "NONE";
        ClearQuickSideData();
        NotifyQuickStepState();
    }

    private void ClearQuickSideData()
    {
        QuickUnitId = string.Empty;
        QuickConversion = string.Empty;
        QuickNetContentValue = string.Empty;
        QuickNetContentUnit = "G";
        QuickPurchasable = true;
        QuickSourceUnitLabel = string.Empty;
        QuickSourcePackageDescription = string.Empty;
        QuickBarcode = string.Empty;
        BarcodeRows.Clear();
        PriceListOptions.Clear();
        QuickPriceListId = string.Empty;
        QuickPriceAmount = string.Empty;
        _quickSimplePriceItem = null;
        QuickDirectPriceCount = 0;
        QuickComplexPriceCount = 0;
        QuickInventoryRows.Clear();
    }

    private void ApplyUnitSetup(ProductVariantData item)
    {
        UnitUnitId = item.UnitId ?? string.Empty;
        UnitConversion = item.IsInventoryBase ? "1" : item.ConversionToBase ?? string.Empty;
        UnitNetContentValue = item.NetContentValue ?? string.Empty;
        UnitNetContentUnit = item.NetContentUomCode ?? "G";
        UnitPurchasable = item.IsPurchasable;
        UnitSourceLabel = item.SourceUnitLabel ?? string.Empty;
        UnitPackageDescription = item.SourcePackageDescription ?? string.Empty;
        UnitBarcode = string.Empty;
        NormalizeQuantity = "1";
        NormalizeResult = string.Empty;
    }

    private void BuildBulkColumns()
    {
        BulkColumns.Clear();
        var count = _bulkMatrix.Max(row => row.Length);
        var headers = BulkHasHeader ? _bulkMatrix[0] : [];
        for (var index = 0; index < count; index++)
        {
            var title = index == 0 ? "Cột 1 · SKU" : $"Cột {index + 1}";
            if (index < headers.Length && !string.IsNullOrWhiteSpace(headers[index])) title += $" · {headers[index]}";

            var mapping = index == 0
                ? "SKU"
                : BulkHasHeader && index < headers.Length
                    ? BulkHeaderMapping(headers[index])
                    : index switch { 1 => "WEIGHT_VALUE", 2 => "WEIGHT_UOM", _ => "IGNORE" };
            BulkColumns.Add(new ProductBulkColumnRow(index, title, mapping, index == 0));
        }
        NotifyBulkState();
    }

    private static string BulkHeaderMapping(string? header)
    {
        var token = NormalizeBulkHeaderToken(header);
        return token switch
        {
            "TENSANPHAM" or "PRODUCTNAME" => "PRODUCT_NAME",
            "TENHIENTHIBANHANG" or "CATALOGNAME" => "CATALOG_NAME",
            "LOAISANPHAM" or "MALOAISANPHAM" or "CATEGORYCODE" => "CATEGORY_CODE",
            "NHANHANG" or "MANHANHANG" or "BRANDCODE" => "BRAND_CODE",
            "MOTA" or "DESCRIPTION" => "DESCRIPTION",
            "GHICHU" or "NOTES" => "NOTES",
            "HIENTHISANPHAMKHIBANHANG" or "PRODUCTISCATALOGVISIBLE" => "PRODUCT_CATALOG_VISIBLE",
            "CHOPHEPDATHANG" or "PRODUCTISORDERABLE" => "PRODUCT_ORDERABLE",
            "QUANLYTONKHO" or "ISINVENTORYMANAGED" => "PRODUCT_INVENTORY_MANAGED",
            "SANPHAMDANGSUDUNG" or "PRODUCTISACTIVE" => "PRODUCT_ACTIVE",
            "TENSKU" or "TENSKUQUYCACH" or "SKUNAME" => "VARIANT_NAME",
            "LOAISKU" or "VARIANTKIND" => "VARIANT_KIND",
            "SKUDUNGLAMDONVITONCHUAN" or "TONCHUAN" or "ISINVENTORYBASE" => "INVENTORY_BASE",
            "CHOPHEPBANSKU" or "ISSELLABLE" => "SELLABLE",
            "HIENTHISKUKHIBANHANG" or "ISCATALOGVISIBLE" => "VARIANT_CATALOG_VISIBLE",
            "SKUDANGSUDUNG" or "ISACTIVE" => "VARIANT_ACTIVE",
            "DONVITINH" or "UNITCODE" => "UNIT_CODE",
            "HESOQUYDOIVEDONVITONCHUAN" or "HESOQUYDOI" or "CONVERSIONTOBASE" => "CONVERSION_TO_BASE",
            "CHOPHEPMUASKU" or "ISPURCHASABLE" => "PURCHASABLE",
            "DINHLUONGQUYCACH" or "NETCONTENTVALUE" => "NET_CONTENT_VALUE",
            "DONVIDINHLUONG" or "NETCONTENTUOMCODE" => "NET_CONTENT_UOM",
            "TENDONVINGUON" or "SOURCEUNITLABEL" => "SOURCE_UNIT_LABEL",
            "MOTAQUYCACHNGUON" or "SOURCEPACKAGEDESCRIPTION" => "SOURCE_PACKAGE_DESCRIPTION",
            "KHOILUONG" or "WEIGHTVALUE" => "WEIGHT_VALUE",
            "DONVIKHOILUONG" or "WEIGHTUOMCODE" => "WEIGHT_UOM",
            _ => "IGNORE"
        };
    }

    private static string NormalizeBulkHeaderToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            var normalized = character is 'đ' or 'Đ' ? 'D' : char.ToUpperInvariant(character);
            if (char.IsLetterOrDigit(normalized)) builder.Append(normalized);
        }
        return builder.ToString();
    }

    private async Task IdentifyBulkAsync()
    {
        if (_bulkMatrix.Count == 0) return;
        SetBusy("bulk-identify");
        ClearMessage();
        try
        {
            _bulkIdentification = await _service.IdentifyBulkAsync(new ProductBulkIdentifyRequest(DataRows())).ConfigureAwait(true);
            BuildBulkSourceRows();
            _bulkPreview = null;
            _bulkOperationKey = null;
            _bulkApplied = false;
            BulkPreviewRows.Clear();
        }
        catch (Exception exception)
        {
            _bulkIdentification = null;
            BulkSourceRows.Clear();
            SetFailure(exception);
        }
        finally
        {
            SetBusy(null);
            NotifyBulkState();
        }
    }

    private ProductBulkSourceRow[] DataRows()
    {
        var source = (BulkHasHeader ? _bulkMatrix.Skip(1) : _bulkMatrix).ToArray();
        if (source.Length > 5_000)
            throw new InvalidOperationException("Mỗi lần chỉ cập nhật tối đa 5.000 dòng.");
        var start = BulkHasHeader ? 2 : 1;
        return source.Select((cells, index) => new ProductBulkSourceRow(start + index, cells)).ToArray();
    }

    private void BuildBulkSourceRows()
    {
        var byRow = (_bulkIdentification?.Rows ?? []).ToDictionary(row => row.RowNumber);
        Replace(BulkSourceRows, DataRows().Select(row =>
        {
            byRow.TryGetValue(row.RowNumber, out var identified);
            var error = identified?.Errors.FirstOrDefault()?.Message;
            return new ProductBulkSourceRowView(
                row.RowNumber,
                row.Cells.FirstOrDefault() ?? string.Empty,
                identified?.ProductName ?? "—",
                string.Join(" · ", row.Cells.Skip(1).Select(value => string.IsNullOrEmpty(value) ? "Trống" : value)),
                error ?? (identified?.Status == "identified" ? "Đã nhận diện" : "Chưa nhận diện"));
        }));
        OnPropertyChanged(nameof(BulkSummary));
    }

    private void BuildBulkPreviewRows()
    {
        var identifiedByRow = (_bulkIdentification?.Rows ?? []).ToDictionary(row => row.RowNumber);
        var rows = new List<ProductBulkPreviewRowView>();
        foreach (var row in _bulkPreview?.Rows ?? [])
        {
            var productName = identifiedByRow.TryGetValue(row.RowNumber, out var identified) ? identified.ProductName : "—";
            if (row.Errors.Length > 0)
            {
                rows.Add(new ProductBulkPreviewRowView(row.RowNumber, row.Sku, productName, "—", "—", "—", row.Errors[0].Message));
                continue;
            }
            if (row.Changes.Length == 0)
            {
                rows.Add(new ProductBulkPreviewRowView(row.RowNumber, row.Sku, productName, "—", "—", "—", "Không thay đổi"));
                continue;
            }
            foreach (var change in row.Changes)
            {
                rows.Add(new ProductBulkPreviewRowView(row.RowNumber, row.Sku, productName, change.Label,
                    change.OldValue, change.NewValue, _bulkApplied ? "Đã cập nhật" : "Sẽ cập nhật"));
            }
        }
        Replace(BulkPreviewRows, rows);
    }

    private void OpenEditor(EditorKind kind, string? id)
    {
        _editorKind = kind;
        _editingId = id;
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsProductEditor));
        OnPropertyChanged(nameof(IsCategoryEditor));
        OnPropertyChanged(nameof(IsBrandEditor));
        OnPropertyChanged(nameof(IsVariantEditor));
        OnPropertyChanged(nameof(IsUnitEditor));
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(CanEnableDraftOrderable));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorPrimaryText));
    }

    private ProductData? GetQuickProduct() => _products.FirstOrDefault(item => item.Id == QuickProductId);
    private ProductVariantData? GetQuickVariant() => QuickVariants.FirstOrDefault(item => item.Id == QuickVariantId);

    private string KeyFor(string operation, string id, string fingerprint)
    {
        var intent = $"{operation}:{id}:{fingerprint}";
        if (_intentKeys.TryGetValue(intent, out var existing)) return existing;
        if (_intentKeys.Count >= IntentCacheLimit)
        {
            var first = _intentKeys.Keys.FirstOrDefault();
            if (first is not null) _intentKeys.Remove(first);
        }
        var key = _idempotencyKeys.Create($"product-{operation}");
        _intentKeys[intent] = key;
        return key;
    }

    private static string Fingerprint<T>(T value) =>
        System.Text.Json.JsonSerializer.Serialize(value);

    private void ResetSessionData()
    {
        _products.Clear();
        _categories.Clear();
        _brands.Clear();
        _units.Clear();
        _imageCodes.Clear();
        _intentKeys.Clear();
        ProductRows.Clear();
        CategoryRows.Clear();
        BrandRows.Clear();
        VariantRows.Clear();
        UnitRows.Clear();
        BarcodeRows.Clear();
        CategoryOptions.Clear();
        BrandOptions.Clear();
        ProductOptions.Clear();
        UnitOptions.Clear();
        PriceListOptions.Clear();
        VariantProduct = null;
        OpenEditor(EditorKind.None, null);
        QuickProductId = string.Empty;
        QuickCreatingProduct = false;
        ClearQuickProductDraft();
        QuickVariants.Clear();
        ClearQuickVariant();
        UnitProductId = string.Empty;
        UnitVariantId = string.Empty;
        UnitVariants.Clear();
        ResetBulkFile();
        ResetProductImportWorkspace();
        OnPropertyChanged(nameof(ProductSummary));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanWrite));
        OnPropertyChanged(nameof(CanReadPrice));
        OnPropertyChanged(nameof(CanWritePrice));
        OnPropertyChanged(nameof(CanReadInventory));
        NotifyProductFileState();
    }

    private void NotifyQuickStepState()
    {
        OnPropertyChanged(nameof(HasQuickProduct));
        OnPropertyChanged(nameof(HasQuickVariant));
        OnPropertyChanged(nameof(CanEnableQuickOrderable));
    }

    private void NotifyQuickImage()
    {
        OnPropertyChanged(nameof(QuickImageStatus));
        OnPropertyChanged(nameof(QuickImageUrl));
    }

    private void NotifyBulkState()
    {
        OnPropertyChanged(nameof(BulkSummary));
        OnPropertyChanged(nameof(CanBulkPreview));
        OnPropertyChanged(nameof(CanBulkApply));
    }

    private void SetBusy(string? action)
    {
        if (_busyAction == action) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(RefreshText));
        NotifyBulkState();
        NotifyProductFileState();
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

    private void SetError(string value)
    {
        MessageIsError = true;
        Message = value;
    }

    private void SetFailure(Exception exception) => SetError(ErrorText(exception));

    private static string ErrorText(Exception exception) =>
        exception is CanonicalApiException apiException
            ? CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(apiException), apiException.RequestId)
            : exception.Message;

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizeDecimal(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Replace(',', '.');

    private static void Replace<T>(ICollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    private static void ReplaceById<T>(IList<T> list, T saved, Func<T, string> id)
    {
        var index = -1;
        for (var current = 0; current < list.Count; current++)
        {
            if (id(list[current]) == id(saved)) { index = current; break; }
        }
        if (index >= 0) list[index] = saved;
        else list.Add(saved);
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
