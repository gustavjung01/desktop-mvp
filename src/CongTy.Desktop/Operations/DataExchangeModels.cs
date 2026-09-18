using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace CongTy.Desktop.Operations;

public static class DataExchangePresentation
{
    public static readonly string[] ProductColumns =
    [
        "productCode", "productName", "catalogName", "categoryCode", "brandCode", "description", "notes",
        "productIsCatalogVisible", "productIsOrderable", "productIsActive", "sku", "skuName", "variantKind",
        "isInventoryBase", "isSellable", "isCatalogVisible", "isActive", "unitCode", "conversionToBase",
        "lotTrackingMode", "expiryTrackingMode"
    ];

    public static readonly string[] ProductRequiredColumns =
    [
        "productCode", "productName", "productIsCatalogVisible", "productIsOrderable", "productIsActive",
        "sku", "skuName", "variantKind", "isInventoryBase", "isSellable", "isCatalogVisible", "isActive"
    ];

    public static readonly string[] PricingColumns = ["sku", "amountMinor"];
    public static readonly string[] StocktakeColumns = ["warehouseCode", "locationCode", "sku", "lotCode", "actualCount"];
    public static readonly string[] QuotationColumns = ["sku", "productName", "skuName", "quantity", "currencyCode", "unitPriceMinor", "lineTotalMinor", "priceListCode"];

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["productCode"] = "Mã sản phẩm",
            ["productName"] = "Tên sản phẩm",
            ["catalogName"] = "Tên hiển thị bán hàng",
            ["categoryCode"] = "Mã loại sản phẩm",
            ["brandCode"] = "Mã nhãn hàng",
            ["description"] = "Mô tả",
            ["notes"] = "Ghi chú",
            ["productIsCatalogVisible"] = "Hiển thị sản phẩm khi bán hàng",
            ["productIsOrderable"] = "Cho phép đặt hàng",
            ["productIsActive"] = "Sản phẩm đang sử dụng",
            ["sku"] = "SKU",
            ["skuName"] = "Tên SKU / quy cách",
            ["variantKind"] = "Loại SKU",
            ["isInventoryBase"] = "SKU dùng làm đơn vị tồn chuẩn",
            ["isSellable"] = "Cho phép bán SKU",
            ["isCatalogVisible"] = "Hiển thị SKU khi bán hàng",
            ["isActive"] = "SKU đang sử dụng",
            ["unitCode"] = "Đơn vị tính",
            ["conversionToBase"] = "Hệ số quy đổi về đơn vị tồn chuẩn",
            ["lotTrackingMode"] = "Quản lý theo lô",
            ["expiryTrackingMode"] = "Quản lý hạn sử dụng",
            ["locationRequired"] = "Bắt buộc chọn vị trí kho",
            ["priceListCode"] = "Mã bảng giá",
            ["priceListName"] = "Tên bảng giá",
            ["listType"] = "Loại bảng giá",
            ["currencyCode"] = "Tiền tệ",
            ["sourceKey"] = "Mã nguồn dòng giá",
            ["adjustmentType"] = "Cách tính giá",
            ["amountMinor"] = "Giá bán (VND)",
            ["rateBps"] = "Tỷ lệ",
            ["minQuantity"] = "Số lượng từ",
            ["maxQuantity"] = "Số lượng đến",
            ["effectiveFrom"] = "Hiệu lực từ",
            ["effectiveTo"] = "Hiệu lực đến",
            ["externalRuleCode"] = "Mã quy tắc ngoài",
            ["note"] = "Ghi chú",
            ["warehouseCode"] = "Mã kho",
            ["locationCode"] = "Mã vị trí",
            ["lotCode"] = "Mã lô",
            ["actualCount"] = "Số đếm thực tế",
            ["quantity"] = "Số lượng",
            ["unitPriceMinor"] = "Đơn giá",
            ["lineTotalMinor"] = "Thành tiền"
        };

    public static string Label(string column) => Labels.TryGetValue(column, out var label) ? label : column;

    public static string NormalizeHeader(string value)
    {
        var text = value.Trim();
        foreach (var pair in Labels)
        {
            if (string.Equals(pair.Value, text, StringComparison.CurrentCultureIgnoreCase))
                return pair.Key;
        }
        return text;
    }

    public static string BoolChoice(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized is "true" or "1" or "yes" or "y" or "co" or "có") return "CÓ";
        if (normalized is "false" or "0" or "no" or "n" or "khong" or "không") return "KHÔNG";
        if (normalized == "có") return "CÓ";
        if (normalized == "không") return "KHÔNG";
        return string.Empty;
    }

    public static string VariantChoice(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch
        {
            "BASE" or "ĐƠN VỊ LẺ" or "DON VI LE" => "BASE",
            "CARTON" or "THÙNG" or "THUNG" => "CARTON",
            "OTHER" or "QUY CÁCH KHÁC" or "QUY CACH KHAC" => "OTHER",
            _ => normalized
        };
    }

    public static string LotChoice(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch
        {
            "REQUIRED" or "CO" or "CÓ" or "THEO LO" or "THEO LÔ" => "CÓ",
            "NONE" or "KHONG" or "KHÔNG" or "KHONG THEO LO" or "KHÔNG THEO LÔ" => "KHÔNG",
            _ => string.Empty
        };
    }

    public static string ExpiryChoice(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch
        {
            "REQUIRED" or "BAT BUOC" or "BẮT BUỘC" => "BẮT BUỘC",
            "OPTIONAL" or "TUY CHON" or "TÙY CHỌN" or "CO THE NHAP" or "CÓ THỂ NHẬP" => "TÙY CHỌN",
            "NONE" or "KHONG" or "KHÔNG" => "KHÔNG",
            _ => string.Empty
        };
    }

    public static string DisplayCell(string column, JsonElement value)
    {
        var text = JsonText(value);
        if (value.ValueKind == JsonValueKind.True || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase)) return "Có";
        if (value.ValueKind == JsonValueKind.False || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase)) return "Không";

        var upper = text.Trim().ToUpperInvariant();
        return column switch
        {
            "variantKind" => upper switch { "BASE" => "Đơn vị lẻ", "CARTON" => "Thùng", "OTHER" => "Quy cách khác", _ => text },
            "lotTrackingMode" => upper switch { "REQUIRED" => "Có", "NONE" => "Không", _ => text },
            "expiryTrackingMode" => upper switch { "REQUIRED" => "Bắt buộc nhập", "OPTIONAL" => "Có thể nhập", "NONE" => "Không quản lý", _ => text },
            "listType" => upper switch { "BASE" => "Giá nền", "CHANNEL" => "Theo kênh", "CUSTOMER_GROUP" => "Theo nhóm khách", "CUSTOMER" => "Theo khách hàng", "PROMOTION" => "Khuyến mãi", "CUSTOM" => "Quy tắc khác", _ => text },
            "adjustmentType" => upper switch { "FIXED_PRICE" => "Giá cố định", "PERCENT_DISCOUNT" => "Giảm phần trăm", "AMOUNT_DISCOUNT" => "Giảm số tiền", "PERCENT_MARKUP" => "Tăng phần trăm", "AMOUNT_MARKUP" => "Tăng số tiền", _ => text },
            _ => text
        };
    }

    public static string JsonText(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => value.GetRawText()
        };

    public static string TrimDecimal(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
            return text;
        return number.ToString("0.############", CultureInfo.InvariantCulture);
    }

    public static string MovementType(string value) =>
        value switch
        {
            "OPENING_BALANCE" => "Tồn đầu kỳ",
            "GOODS_RECEIPT" => "Nhập hàng",
            "SUPPLIER_RETURN" => "Trả nhà cung cấp",
            "INVENTORY_TRANSFER_OUT" => "Chuyển kho đi",
            "INVENTORY_TRANSFER_IN" => "Nhận chuyển kho",
            "INVENTORY_ADJUSTMENT" => "Điều chỉnh kho",
            "STOCKTAKE_ADJUSTMENT" => "Điều chỉnh sau kiểm kho",
            "FULFILLMENT_PICK" => "Soạn hàng",
            "FULFILLMENT_REVERSE_PICK" => "Hoàn soạn hàng",
            "DELIVERY_ISSUE" => "Xuất giao hàng",
            "DELIVERY_RETURN" => "Nhập hàng giao trả về",
            "CUSTOMER_RETURN" => "Khách trả hàng",
            "PICKUP_ISSUE" => "Khách nhận tại kho",
            "MANUAL_ISSUE" => "Xuất kho thủ công",
            "MANUAL_RECEIPT" => "Nhập kho thủ công",
            "SALES_DELIVERY_ISSUE" => "Xuất bán hàng",
            "CUSTOMER_RETURN_RECEIPT" => "Nhập hàng khách trả",
            _ => "Nghiệp vụ kho khác"
        };

    public static string VietnamDateTime(string value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return value;
        return parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}

public sealed record DataExchangeChoice(string Id, string Display);
public sealed record DataExchangeColumnChoice(string Id, string Display, bool IsSelected);

public sealed class DataExchangeImportRow : INotifyPropertyChanged
{
    private readonly Dictionary<string, string> _cells = new(StringComparer.Ordinal);
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? Changed;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string ProductCode { get => Get("productCode"); set => Set("productCode", value); }
    public string ProductName { get => Get("productName"); set => Set("productName", value); }
    public string CatalogName { get => Get("catalogName"); set => Set("catalogName", value); }
    public string CategoryCode { get => Get("categoryCode"); set => Set("categoryCode", value); }
    public string BrandCode { get => Get("brandCode"); set => Set("brandCode", value); }
    public string Description { get => Get("description"); set => Set("description", value); }
    public string Notes { get => Get("notes"); set => Set("notes", value); }
    public string ProductIsCatalogVisible { get => Get("productIsCatalogVisible"); set => Set("productIsCatalogVisible", value); }
    public string ProductIsOrderable { get => Get("productIsOrderable"); set => Set("productIsOrderable", value); }
    public string ProductIsActive { get => Get("productIsActive"); set => Set("productIsActive", value); }
    public string Sku { get => Get("sku"); set => Set("sku", value); }
    public string SkuName { get => Get("skuName"); set => Set("skuName", value); }
    public string VariantKind { get => Get("variantKind"); set => Set("variantKind", value); }
    public string IsInventoryBase { get => Get("isInventoryBase"); set => Set("isInventoryBase", value); }
    public string IsSellable { get => Get("isSellable"); set => Set("isSellable", value); }
    public string IsCatalogVisible { get => Get("isCatalogVisible"); set => Set("isCatalogVisible", value); }
    public string IsActive { get => Get("isActive"); set => Set("isActive", value); }
    public string UnitCode { get => Get("unitCode"); set => Set("unitCode", value); }
    public string ConversionToBase { get => Get("conversionToBase"); set => Set("conversionToBase", value); }
    public string LotTrackingMode { get => Get("lotTrackingMode"); set => Set("lotTrackingMode", value); }
    public string ExpiryTrackingMode { get => Get("expiryTrackingMode"); set => Set("expiryTrackingMode", value); }
    public string LocationRequired { get => Get("locationRequired"); set => Set("locationRequired", value); }
    public string AmountMinor { get => Get("amountMinor"); set => Set("amountMinor", value); }
    public string WarehouseCode { get => Get("warehouseCode"); set => Set("warehouseCode", value); }
    public string LocationCode { get => Get("locationCode"); set => Set("locationCode", value); }
    public string LotCode { get => Get("lotCode"); set => Set("lotCode", value); }
    public string ActualCount { get => Get("actualCount"); set => Set("actualCount", value); }

    public static DataExchangeImportRow From(IReadOnlyDictionary<string, string> values)
    {
        var row = new DataExchangeImportRow();
        foreach (var pair in values) row._cells[pair.Key] = pair.Value;
        row.NormalizeChoices();
        return row;
    }

    public Dictionary<string, string> ToDictionary() =>
        _cells.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    public string Get(string key) => _cells.TryGetValue(key, out var value) ? value : string.Empty;

    public void SetValue(string key, string value) => Set(key, value);

    private void NormalizeChoices()
    {
        foreach (var field in new[] { "productIsCatalogVisible", "productIsOrderable", "productIsActive", "isInventoryBase", "isSellable", "isCatalogVisible", "isActive", "locationRequired" })
        {
            if (_cells.TryGetValue(field, out var value))
            {
                var choice = DataExchangePresentation.BoolChoice(value);
                if (choice.Length > 0) _cells[field] = choice;
            }
        }
        if (_cells.TryGetValue("variantKind", out var variant)) _cells["variantKind"] = DataExchangePresentation.VariantChoice(variant);
        if (_cells.TryGetValue("lotTrackingMode", out var lot)) _cells["lotTrackingMode"] = DataExchangePresentation.LotChoice(lot);
        if (_cells.TryGetValue("expiryTrackingMode", out var expiry)) _cells["expiryTrackingMode"] = DataExchangePresentation.ExpiryChoice(expiry);
    }

    private void Set(string key, string? value)
    {
        var next = value ?? string.Empty;
        if (string.Equals(Get(key), next, StringComparison.Ordinal)) return;
        _cells[key] = next;
        OnPropertyChanged(PropertyName(key));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static string PropertyName(string key) =>
        key switch
        {
            "productCode" => nameof(ProductCode),
            "productName" => nameof(ProductName),
            "catalogName" => nameof(CatalogName),
            "categoryCode" => nameof(CategoryCode),
            "brandCode" => nameof(BrandCode),
            "description" => nameof(Description),
            "notes" => nameof(Notes),
            "productIsCatalogVisible" => nameof(ProductIsCatalogVisible),
            "productIsOrderable" => nameof(ProductIsOrderable),
            "productIsActive" => nameof(ProductIsActive),
            "sku" => nameof(Sku),
            "skuName" => nameof(SkuName),
            "variantKind" => nameof(VariantKind),
            "isInventoryBase" => nameof(IsInventoryBase),
            "isSellable" => nameof(IsSellable),
            "isCatalogVisible" => nameof(IsCatalogVisible),
            "isActive" => nameof(IsActive),
            "unitCode" => nameof(UnitCode),
            "conversionToBase" => nameof(ConversionToBase),
            "lotTrackingMode" => nameof(LotTrackingMode),
            "expiryTrackingMode" => nameof(ExpiryTrackingMode),
            "locationRequired" => nameof(LocationRequired),
            "amountMinor" => nameof(AmountMinor),
            "warehouseCode" => nameof(WarehouseCode),
            "locationCode" => nameof(LocationCode),
            "lotCode" => nameof(LotCode),
            "actualCount" => nameof(ActualCount),
            _ => key
        };

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed record DataExchangeQuotationRow(
    string Sku,
    string SkuName,
    string ProductName,
    string Quantity,
    string UnitPrice,
    string LineTotal,
    string PriceListCode,
    string CurrencyCode);

public sealed record DataExchangeMovementRow(
    string Id,
    string PostedAt,
    string Document,
    string MovementType,
    string Direction,
    string QuantityDelta,
    string StockAfter,
    string LotCode,
    string SourceLineReference);

public sealed record DataExchangeBalanceChoice(
    string Key,
    string Display,
    CongTy.Contracts.InventoryBalanceData Balance);
