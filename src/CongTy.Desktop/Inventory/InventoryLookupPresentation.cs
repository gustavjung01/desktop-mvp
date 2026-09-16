using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record InventoryLookupBalanceRow(int Sequence, InventoryBalanceData Data)
{
    public string Warehouse => $"{Data.WarehouseCode} · {Data.WarehouseName}";
    public string Location => string.IsNullOrWhiteSpace(Data.LocationCode)
        ? "Không vị trí"
        : string.IsNullOrWhiteSpace(Data.LocationName)
            ? Data.LocationCode!
            : $"{Data.LocationCode} · {Data.LocationName}";
    public string Product => Data.ProductName;
    public string BaseSku => Data.BaseSku;
    public string PackageSku => !string.IsNullOrWhiteSpace(Data.PackageSku) && Data.PackageSku != Data.BaseSku
        ? $"SKU thùng: {Data.PackageSku}"
        : string.Empty;
    public string Variant => Data.BaseVariantName ?? string.Empty;
    public string PackageRule => InventoryLookupPresentation.PackageRule(Data);
    public string Lot => Data.LotCode ?? "—";
    public string Expiry => InventoryLookupPresentation.Date(Data.ExpiryDate);
    public string OnHand => InventoryLookupPresentation.QuantityWithUnit(Data.OnHandQuantity, Data);
    public string OnHandBreakdown => InventoryLookupPresentation.PackageBreakdown(Data.OnHandQuantity, Data);
    public string Reserved => InventoryLookupPresentation.QuantityWithUnit(Data.ReservedQuantity, Data);
    public string ReservedBreakdown => InventoryLookupPresentation.PackageBreakdown(Data.ReservedQuantity, Data);
    public string Available => InventoryLookupPresentation.QuantityWithUnit(Data.AvailableQuantity, Data);
    public string AvailableBreakdown => InventoryLookupPresentation.PackageBreakdown(Data.AvailableQuantity, Data);
}

public sealed record InventoryLookupHistoryRow(InventoryHistoryData Data)
{
    public string PostedAt => InventoryLookupPresentation.DateTimeText(Data.PostedAt);
    public string Employee => string.IsNullOrWhiteSpace(Data.PostedByName) ? "Hệ thống" : Data.PostedByName!;
    public string Movement => InventoryLookupPresentation.Movement(Data.MovementType, Data.BaseQuantityDelta);
    public string Quantity { get; init; } = string.Empty;
    public string StockAfter { get; init; } = string.Empty;
    public string DocumentNumber => Data.SourceDocumentNumber ?? Data.DocumentNumber ?? string.Empty;
    public string Warehouse => $"{Data.WarehouseCode} · {Data.WarehouseName}";
    public bool HasDocument => !string.IsNullOrWhiteSpace(DocumentNumber);
}

public static class InventoryLookupPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static bool HasDisplayableBalance(InventoryBalanceData row) =>
        Parse(row.OnHandQuantity) != 0m || Parse(row.ReservedQuantity) != 0m;

    public static string SearchText(InventoryBalanceData row) =>
        string.Join(' ', new[]
        {
            row.ProductName, row.ProductCode,
            row.WarehouseCode, row.WarehouseName,
            row.LocationCode, row.LocationName,
            row.BaseSku, row.BaseVariantName,
            row.BaseUnitCode, row.BaseUnitName,
            row.PackageSku, row.PackageVariantName,
            row.PackageUnitCode, row.PackageUnitName,
            row.LotCode, row.ExpiryDate
        }.Where(value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();

    public static string Quantity(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            return string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        return number.ToString("#,##0.############", Vi);
    }

    public static string Unit(InventoryBalanceData row) =>
        First(row.BaseUnitName, row.BaseUnitSymbol, row.BaseUnitCode);

    public static string QuantityWithUnit(string? value, InventoryBalanceData row)
    {
        var unit = Unit(row);
        return string.IsNullOrWhiteSpace(unit) ? Quantity(value) : $"{Quantity(value)} {unit}";
    }

    public static string PackageRule(InventoryBalanceData row)
    {
        var packageUnit = First(row.PackageUnitName, row.PackageUnitSymbol, row.PackageUnitCode);
        var baseUnit = Unit(row);
        if (string.IsNullOrWhiteSpace(packageUnit)
            || string.IsNullOrWhiteSpace(baseUnit)
            || !decimal.TryParse(row.PackageConversionToBase, NumberStyles.Number, CultureInfo.InvariantCulture, out var conversion)
            || conversion <= 1m)
        {
            return string.Empty;
        }

        return $"Quy cách: 1 {packageUnit} = {Quantity(row.PackageConversionToBase)} {baseUnit}";
    }

    public static string PackageBreakdown(string? value, InventoryBalanceData row)
    {
        var packageUnit = First(row.PackageUnitName, row.PackageUnitSymbol, row.PackageUnitCode);
        var baseUnit = Unit(row);
        if (string.IsNullOrWhiteSpace(packageUnit)
            || string.IsNullOrWhiteSpace(baseUnit)
            || !decimal.TryParse(row.PackageConversionToBase, NumberStyles.Number, CultureInfo.InvariantCulture, out var conversion)
            || !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity)
            || conversion <= 1m
            || quantity <= 0m
            || quantity < conversion)
        {
            return string.Empty;
        }

        var packages = decimal.Floor(quantity / conversion);
        var remainder = quantity - packages * conversion;
        return remainder == 0m
            ? $"{Quantity(packages.ToString(CultureInfo.InvariantCulture))} {packageUnit}"
            : $"{Quantity(packages.ToString(CultureInfo.InvariantCulture))} {packageUnit} + {Quantity(remainder.ToString(CultureInfo.InvariantCulture))} {baseUnit}";
    }

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Không có";
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date.ToString("dd/MM/yyyy", Vi)
            : value;
    }

    public static string DateTimeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Không có";
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            return value;
        var local = TimeZoneInfo.ConvertTime(date, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        return local.ToString("dd/MM/yyyy HH:mm", Vi);
    }

    public static string Movement(string movementType, string delta) => movementType switch
    {
        "SALES_DELIVERY_ISSUE" => "Xuất kho giao khách",
        "PURCHASE_RECEIPT" or "GOODS_RECEIPT" => "Nhập hàng",
        "SUPPLIER_RETURN" => "Xuất trả nhà cung cấp",
        "TRANSFER_ISSUE" or "INVENTORY_TRANSFER_OUT" => "Xuất chuyển kho",
        "TRANSFER_RECEIPT" or "INVENTORY_TRANSFER_IN" => "Nhập chuyển kho",
        "OPENING_BALANCE" => "Thiết lập tồn đầu kỳ",
        "MANUAL_INBOUND" or "MANUAL_RECEIPT" => "Nhập kho thủ công",
        "STOCKTAKE_ADJUSTMENT" => "Cân bằng sau kiểm kê",
        "STOCKTAKE_ADJUSTMENT_REVERSAL" => "Hoàn tác cân bằng kiểm kê",
        "LOGISTICS_TRIP_RETURN" or "DELIVERY_RETURN" => "Nhập hàng hoàn",
        "REVERSAL" => "Hoàn tác giao dịch kho",
        _ when movementType.StartsWith("MANUAL_ADJUSTMENT_", StringComparison.Ordinal) => "Điều chỉnh tồn kho",
        _ => Parse(delta) >= 0m ? "Nhập kho" : "Xuất kho"
    };

    public static string DocumentType(string? value) => value switch
    {
        "SALES_ORDER" => "Đơn bán hàng",
        "DELIVERY_ORDER" => "Phiếu giao hàng",
        "PURCHASE_RECEIPT" => "Phiếu nhận hàng",
        "SUPPLIER_RETURN" => "Phiếu trả nhà cung cấp",
        "INVENTORY_TRANSFER" => "Phiếu chuyển kho",
        "INVENTORY_TRANSFER_RECEIPT" => "Phiếu nhận chuyển kho",
        "INVENTORY_ADJUSTMENT" => "Phiếu điều chỉnh tồn",
        "OPENING_BALANCE_IMPORT" => "Thiết lập tồn đầu kỳ",
        "MANUAL_INBOUND" => "Phiếu nhập kho",
        "STOCKTAKE" => "Phiếu kiểm kê",
        "INVENTORY_REVERSAL" => "Phiếu hoàn tác kho",
        _ => "Chứng từ kho"
    };

    public static decimal SumWarehouseOnHand(
        IEnumerable<InventoryBalanceData> rows,
        InventoryBalanceData selected) =>
        rows.Where(row => row.WarehouseId == selected.WarehouseId && row.BaseVariantId == selected.BaseVariantId)
            .Sum(row => Parse(row.OnHandQuantity));

    private static decimal Parse(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : 0m;

    private static string First(params string?[] values) =>
        values.Select(value => value?.Trim()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}
