using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Products;

public sealed record ProductLookupOption(string Id, string Label);
public sealed record ProductFilterOption(string Id, string Label);
public sealed record ProductRow(
    int Stt,
    string Id,
    string Code,
    string Image,
    string Name,
    string Category,
    string Brand,
    string Catalog,
    string Orderable,
    string Status,
    string ToggleAction,
    ProductData Source);

public sealed record ProductCategoryRow(
    int Stt,
    string Id,
    string Code,
    string Name,
    string Parent,
    int SortOrder,
    string Catalog,
    string Status,
    string ToggleAction,
    ProductCategoryData Source);

public sealed record ProductBrandRow(
    int Stt,
    string Id,
    string Code,
    string Name,
    string Catalog,
    string Status,
    string ToggleAction,
    ProductBrandData Source);

public sealed record ProductVariantRow(
    int Stt,
    string Id,
    string Sku,
    string Name,
    string Kind,
    string Weight,
    string InventoryBase,
    string Sellable,
    string Catalog,
    string Status,
    ProductVariantData Source);

public sealed record ProductUnitRow(
    int Stt,
    string Id,
    string Code,
    string Name,
    string Symbol,
    string Kind,
    string Fractional,
    string Status,
    string ToggleAction,
    ProductUnitData Source);

public sealed record ProductBarcodeRow(
    int Stt,
    string Id,
    string Barcode,
    string Type,
    string Primary,
    string Status,
    string ToggleAction,
    ProductBarcodeData Source);

public sealed record ProductInventoryRow(
    int Stt,
    string Warehouse,
    string Location,
    string OnHand,
    string Reserved,
    string Available);

public sealed class ProductBulkColumnRow : INotifyPropertyChanged
{
    private string _mapping = "IGNORE";

    public ProductBulkColumnRow(int index, string title, string mapping, bool locked)
    {
        Index = index;
        Title = title;
        _mapping = mapping;
        Locked = locked;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public int Index { get; }
    public string Title { get; }
    public bool Locked { get; }
    public string Mapping
    {
        get => _mapping;
        set
        {
            var next = value ?? "IGNORE";
            if (_mapping == next) return;
            _mapping = next;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mapping)));
        }
    }
}

public sealed record ProductBulkSourceRowView(
    int RowNumber,
    string Sku,
    string ProductName,
    string Values,
    string Status);

public sealed record ProductBulkPreviewRowView(
    int RowNumber,
    string Sku,
    string ProductName,
    string Field,
    string OldValue,
    string NewValue,
    string Result);

public static class ProductPresentation
{
    public static string YesNo(bool value) => value ? "Có" : "Không";
    public static string Active(bool value) => value ? "Đang sử dụng" : "Ngừng sử dụng";
    public static string Toggle(bool value) => value ? "Ngừng sử dụng" : "Đưa vào sử dụng";
    public static string Category(ProductCategoryData? item) => item is null ? "—" : $"{item.Code} — {item.Name}";
    public static string Brand(ProductBrandData? item) => item is null ? "—" : $"{item.Code} — {item.Name}";

    public static string VariantKind(string value) => value switch
    {
        "BASE" => "Đơn vị lẻ",
        "CARTON" => "Thùng",
        "OTHER" => "Quy cách khác",
        _ => "Khác"
    };

    public static string UnitKind(string value) => value switch
    {
        "COUNT" => "Đếm",
        "PACKAGE" => "Bao gói",
        "WEIGHT" => "Khối lượng",
        "VOLUME" => "Thể tích",
        "OTHER" => "Khác",
        _ => "Khác"
    };

    public static string Weight(ProductVariantData item) =>
        string.IsNullOrWhiteSpace(item.WeightValue)
            ? "—"
            : $"{Inventory.InventoryPresentation.Quantity(item.WeightValue)} {(item.WeightUomCode == "G" ? "g" : "kg")}";

    public static string MoneyMinor(string? value)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount)) return "—";
        return $"{amount.ToString("#,##0", CultureInfo.GetCultureInfo("vi-VN"))} đ";
    }

    public static string PriceListType(string value) => value switch
    {
        "BASE" => "Giá nền",
        "CHANNEL" => "Theo kênh bán",
        "CUSTOMER_GROUP" => "Theo nhóm khách",
        "CUSTOMER" => "Theo khách hàng",
        "PROMOTION" => "Khuyến mãi",
        "CUSTOM" => "Quy tắc khác",
        _ => "Bảng giá"
    };

    public static bool IsZeroQuantity(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
        && number == 0m;

    public static bool IsPositiveDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
        && number > 0m;

    public static string BulkMappingLabel(string value) => value switch
    {
        "SKU" => "SKU",
        "WEIGHT_VALUE" => "Khối lượng",
        "WEIGHT_UOM" => "Đơn vị khối lượng",
        _ => "Bỏ qua"
    };
}
