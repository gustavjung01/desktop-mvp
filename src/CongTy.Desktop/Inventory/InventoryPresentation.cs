using System.ComponentModel;
using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record InventoryBalanceRow(
    int Stt,
    string WarehouseLocation,
    string ProductSku,
    string Lot,
    string Expiry,
    string OnHand,
    string Reserved,
    string Available,
    string PackageBreakdown,
    string SearchText,
    InventoryBalanceData Data);

public sealed record InventoryHistoryRow(
    int Stt,
    string PostedAt,
    string Movement,
    string Document,
    string Quantity,
    string StockAfter,
    string LocationLot,
    string PostedBy,
    string Reason,
    InventoryHistoryData Data);

public sealed record InventoryLotRow(
    int Stt,
    string ProductSku,
    string LotCode,
    string ManufacturedDate,
    string ExpiryDate,
    string SupplierReference,
    string CreatedAt,
    string SearchText);

public sealed record InventoryWarehouseOption(string Id, string Label);
public sealed record InventorySlowDayOption(int Days, string Label);
public sealed record InventoryWarehouseSummaryRow(int Stt, string Warehouse, string StockedSku, string ReservedSku, string Value, string Exceptions, string UpdatedAt);
public sealed record InventoryPositionRow(
    int Stt, string Warehouse, string ProductSku, string OnHand, string PackageBreakdown,
    string Reserved, string Available, string Value, string AverageCost, string CostingStatus,
    string WarehouseId, string VariantId, string BaseUnit);
public sealed record InventoryFlowRow(int Stt, string Warehouse, string ProductSku, string Opening, string Inbound, string Outbound, string Closing, string Lines);
public sealed record InventoryMovementTypeRow(int Stt, string Movement, string Documents, string Lines, string Skus);
public sealed record InventorySlowRow(int Stt, string Warehouse, string ProductSku, string OnHand, string Available, string LastOut, string Days, string Value);
public sealed record InventoryExpiryRow(int Stt, string Warehouse, string ProductSku, string Lot, string Manufactured, string Expiry, string OnHand, string Available, string Status);
public sealed record InventoryExceptionRow(int Stt, string Warehouse, string ProductSku, string Ledger, string Costing, string Difference, string Status, string Alerts);
public sealed record InventoryHoldOrderRow(int Stt, string OrderNumber, string Customer, string ProductSku, string Warehouse, string Flow, string HeldQuantity);


public sealed record InventoryExportDimensionOption(string Key, string Label);
public sealed record InventoryExportColumnDefinition(string Key, string Label, bool DefaultSelected);

public sealed class InventoryExportColumnOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public InventoryExportColumnOption(string key, string label, bool isSelected)
    {
        Key = key;
        Label = label;
        _isSelected = isSelected;
    }

    public string Key { get; }
    public string Label { get; }

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

    public event PropertyChangedEventHandler? PropertyChanged;
}

public static class InventoryPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");


    public static readonly IReadOnlyList<InventoryExportDimensionOption> ExportDimensions =
    [
        new("overview", "Tổng quan theo kho"),
        new("positions", "Tồn hiện tại"),
        new("movement", "Nhập – xuất – tồn theo kỳ"),
        new("slow-moving", "Hàng chậm luân chuyển"),
        new("lots", "Lô & hạn dùng"),
        new("exceptions", "Cần kiểm tra")
    ];

    private static readonly IReadOnlyDictionary<string, InventoryExportColumnDefinition[]> ExportDefinitions =
        new Dictionary<string, InventoryExportColumnDefinition[]>(StringComparer.Ordinal)
        {
            ["overview"] =
            [
                new("warehouseCode", "Mã kho", true),
                new("warehouseName", "Tên kho", true),
                new("stockedSkuCount", "Mã hàng có tồn", true),
                new("reservedSkuCount", "Mã hàng có giữ", true),
                new("inventoryValueVnd", "Giá trị tồn (VND)", true),
                new("costingExceptionCount", "Cần kiểm tra giá vốn", true),
                new("quantityProjectedThrough", "Cập nhật tồn đến", true)
            ],
            ["positions"] =
            [
                new("warehouseCode", "Mã kho", true),
                new("warehouseName", "Tên kho", false),
                new("productCode", "Mã sản phẩm", true),
                new("productName", "Tên sản phẩm", true),
                new("sku", "SKU", true),
                new("unitName", "Đơn vị tính", true),
                new("onHandQuantity", "Tồn kho", true),
                new("reservedQuantity", "Đã giữ cho đơn", true),
                new("availableQuantity", "Có thể xuất", true),
                new("inventoryValue", "Giá trị tồn", true),
                new("averageUnitCost", "Giá bình quân", true),
                new("costingStatus", "Tình trạng giá vốn", true),
                new("projectedThrough", "Dữ liệu tồn đến", false)
            ],
            ["movement"] =
            [
                new("warehouseCode", "Mã kho", true),
                new("warehouseName", "Tên kho", false),
                new("productCode", "Mã sản phẩm", true),
                new("productName", "Tên sản phẩm", true),
                new("sku", "SKU", true),
                new("unitName", "Đơn vị tính", true),
                new("openingQuantity", "Đầu kỳ", true),
                new("inboundQuantity", "Nhập", true),
                new("outboundQuantity", "Xuất", true),
                new("closingQuantity", "Cuối kỳ", true),
                new("movementLineCount", "Dòng nghiệp vụ", true),
                new("lastPostedAt", "Phát sinh gần nhất", false)
            ],
            ["slow-moving"] =
            [
                new("warehouseCode", "Mã kho", true),
                new("warehouseName", "Tên kho", false),
                new("productCode", "Mã sản phẩm", true),
                new("productName", "Tên sản phẩm", true),
                new("sku", "SKU", true),
                new("unitName", "Đơn vị tính", true),
                new("onHandQuantity", "Tồn kho", true),
                new("reservedQuantity", "Đã giữ cho đơn", false),
                new("availableQuantity", "Có thể xuất", true),
                new("lastOutDate", "Lần xuất cuối", true),
                new("daysSinceOutbound", "Số ngày chưa xuất", true),
                new("inventoryValueVnd", "Giá trị tồn (VND)", true)
            ],
            ["lots"] =
            [
                new("warehouseCode", "Mã kho", true),
                new("warehouseName", "Tên kho", false),
                new("productCode", "Mã sản phẩm", true),
                new("productName", "Tên sản phẩm", true),
                new("sku", "SKU", true),
                new("unitName", "Đơn vị tính", true),
                new("lotCode", "Mã lô", true),
                new("manufacturedDate", "Ngày sản xuất", true),
                new("expiryDate", "Hạn sử dụng", true),
                new("onHandQuantity", "Tồn kho", true),
                new("reservedQuantity", "Đã giữ cho đơn", false),
                new("availableQuantity", "Có thể xuất", true),
                new("manufacturedAgeDays", "Tuổi lô (ngày)", false),
                new("daysToExpiry", "Còn lại đến hạn (ngày)", false),
                new("expiryStatus", "Trạng thái hạn dùng", true)
            ],
            ["exceptions"] =
            [
                new("warehouseCode", "Mã kho", true),
                new("warehouseName", "Tên kho", false),
                new("productCode", "Mã sản phẩm", true),
                new("productName", "Tên sản phẩm", true),
                new("sku", "SKU", true),
                new("unitName", "Đơn vị tính", true),
                new("ledgerQuantity", "Số lượng sổ kho", true),
                new("costingQuantity", "Số lượng tính giá", true),
                new("quantityDifference", "Chênh lệch", true),
                new("inventoryValueVnd", "Giá trị tồn (VND)", false),
                new("averageUnitCost", "Giá bình quân", false),
                new("costingStatus", "Tình trạng giá vốn", true),
                new("anomalyCount", "Số cảnh báo", true),
                new("reconciliationStatus", "Trạng thái đối soát", true)
            ]
        };

    public static IReadOnlyList<InventoryExportColumnDefinition> ExportColumns(string dimension) =>
        ExportDefinitions.TryGetValue(dimension, out var columns)
            ? columns
            : ExportDefinitions["overview"];

    public static string Quantity(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            return string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        return number.ToString("#,##0.######", Vi);
    }

    public static string Money(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            return "—";
        return $"{number.ToString("#,##0", Vi)} ₫";
    }

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date.ToString("dd/MM/yyyy", Vi)
            : value;
    }

    public static string DateTimeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            return value;
        var local = TimeZoneInfo.ConvertTime(date, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        return local.ToString("dd/MM/yyyy HH:mm", Vi);
    }

    public static string Unit(InventoryBalanceData? row) =>
        row is null ? string.Empty : First(row.BaseUnitName, row.BaseUnitSymbol, row.BaseUnitCode);

    public static string QuantityWithUnit(string? value, InventoryBalanceData row)
    {
        var unit = Unit(row);
        return string.IsNullOrWhiteSpace(unit) ? Quantity(value) : $"{Quantity(value)} {unit}";
    }

    public static string PackageBreakdown(InventoryBalanceData row) =>
        PackageBreakdown(row.OnHandQuantity, row);

    public static string PackageBreakdown(string? quantityValue, InventoryBalanceData? row)
    {
        if (row is null) return "—";
        var packageUnit = First(row.PackageUnitName, row.PackageUnitSymbol, row.PackageUnitCode);
        var baseUnit = Unit(row);
        if (string.IsNullOrWhiteSpace(packageUnit)
            || string.IsNullOrWhiteSpace(baseUnit)
            || !decimal.TryParse(row.PackageConversionToBase, NumberStyles.Number, CultureInfo.InvariantCulture, out var conversion)
            || !decimal.TryParse(quantityValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity)
            || conversion <= 1m
            || quantity < conversion)
            return "—";

        var packages = decimal.Floor(quantity / conversion);
        var remainder = quantity - packages * conversion;
        return remainder == 0m
            ? $"{Quantity(packages.ToString(CultureInfo.InvariantCulture))} {packageUnit}"
            : $"{Quantity(packages.ToString(CultureInfo.InvariantCulture))} {packageUnit} + {Quantity(remainder.ToString(CultureInfo.InvariantCulture))} {baseUnit}";
    }

    public static string ProductSkuWithUnit(InventoryBalanceData? row, string sku)
    {
        var actualSku = First(row?.BaseSku, sku);
        var label = ProductSku(First(row?.ProductName, actualSku), actualSku);
        var baseUnit = Unit(row);
        if (string.IsNullOrWhiteSpace(baseUnit)) return label;

        var packageUnit = row is null ? string.Empty : First(row.PackageUnitName, row.PackageUnitSymbol, row.PackageUnitCode);
        var unitText = $"ĐVT: {baseUnit}";
        if (!string.IsNullOrWhiteSpace(packageUnit) && !string.IsNullOrWhiteSpace(row?.PackageConversionToBase))
            unitText += $" · 1 {packageUnit} = {Quantity(row.PackageConversionToBase)} {baseUnit}";
        return $"{label} · {unitText}";
    }

    public static string DateWithAge(string? value, string? ageDays)
    {
        var date = Date(value);
        return string.IsNullOrWhiteSpace(ageDays) || date == "—" ? date : $"{date} ({ageDays} ngày)";
    }

    public static string HoldFlow(string? deliveryMode, string? deliveryExecutionMode) => deliveryMode switch
    {
        "PICKUP" => "Khách nhận tại kho",
        "DELIVERY" when deliveryExecutionMode == "MANUAL" => "Giao thủ công",
        "DELIVERY" => "Giao theo chuyến",
        _ => "Đơn bán"
    };

    public static string Movement(string value) => value switch
    {
        "SALES_DELIVERY_ISSUE" => "Xuất kho giao khách",
        "PURCHASE_RECEIPT" => "Nhập hàng",
        "GOODS_RECEIPT" => "Nhập hàng",
        "SUPPLIER_RETURN" => "Xuất trả nhà cung cấp",
        "TRANSFER_ISSUE" or "INVENTORY_TRANSFER_OUT" => "Xuất chuyển kho",
        "TRANSFER_RECEIPT" or "INVENTORY_TRANSFER_IN" => "Nhập chuyển kho",
        "OPENING_BALANCE" => "Thiết lập tồn đầu kỳ",
        "MANUAL_INBOUND" or "MANUAL_RECEIPT" => "Nhập kho thủ công",
        "MANUAL_ISSUE" => "Xuất kho thủ công",
        "STOCKTAKE_ADJUSTMENT" => "Cân bằng sau kiểm kê",
        "STOCKTAKE_ADJUSTMENT_REVERSAL" => "Hoàn tác cân bằng kiểm kê",
        "LOGISTICS_TRIP_RETURN" or "DELIVERY_RETURN" => "Nhập hàng hoàn",
        "DELIVERY_ISSUE" => "Xuất giao hàng",
        "CUSTOMER_RETURN" => "Khách trả hàng",
        "PICKUP_ISSUE" => "Khách nhận tại kho",
        "REVERSAL" => "Hoàn tác giao dịch kho",
        _ when value.StartsWith("MANUAL_ADJUSTMENT_", StringComparison.Ordinal) => "Điều chỉnh tồn kho",
        _ => "Nghiệp vụ kho khác"
    };

    public static string ReportMovement(string value) => value switch
    {
        "OPENING_BALANCE" => "Tồn đầu kỳ",
        "GOODS_RECEIPT" or "PURCHASE_RECEIPT" => "Nhập hàng",
        "SUPPLIER_RETURN" => "Trả nhà cung cấp",
        "INVENTORY_TRANSFER_OUT" or "TRANSFER_ISSUE" => "Chuyển kho đi",
        "INVENTORY_TRANSFER_IN" or "TRANSFER_RECEIPT" => "Nhận chuyển kho",
        "INVENTORY_ADJUSTMENT" => "Điều chỉnh kho",
        "STOCKTAKE_ADJUSTMENT" => "Điều chỉnh sau kiểm kho",
        "FULFILLMENT_PICK" => "Soạn hàng",
        "FULFILLMENT_REVERSE_PICK" => "Hoàn soạn hàng",
        "DELIVERY_ISSUE" or "SALES_DELIVERY_ISSUE" => "Xuất giao hàng",
        "DELIVERY_RETURN" or "LOGISTICS_TRIP_RETURN" => "Nhập hàng giao trả về",
        "CUSTOMER_RETURN" => "Khách trả hàng",
        "PICKUP_ISSUE" => "Khách nhận tại kho",
        "MANUAL_ISSUE" => "Xuất kho thủ công",
        "MANUAL_RECEIPT" or "MANUAL_INBOUND" => "Nhập kho thủ công",
        _ => "Nghiệp vụ kho khác"
    };

    public static string CostingStatus(string value) => value switch
    {
        "COSTED" => "Đã tính giá vốn",
        "PENDING" => "Chờ tính giá vốn",
        _ => "Cần kiểm tra giá vốn"
    };

    public static string ReconciliationStatus(string value) => value switch
    {
        "OK" => "Khớp",
        "QUANTITY_MISMATCH" => "Lệch số lượng",
        "COSTING_NOT_READY" => "Chưa đủ dữ liệu giá vốn",
        "MISSING_COST" => "Thiếu giá vốn",
        _ => "Cần kiểm tra"
    };

    public static string ExpiryStatus(string value) => value switch
    {
        "EXPIRED" => "Đã hết hạn",
        "EXPIRING_30_DAYS" => "Hết hạn trong 30 ngày",
        "EXPIRING_90_DAYS" => "Hết hạn trong 90 ngày",
        "ACTIVE" => "Còn hạn",
        "NO_EXPIRY" => "Không có hạn",
        _ => "Cần kiểm tra"
    };

    public static string ProductSku(string productName, string sku) =>
        $"{First(productName, sku)}{Environment.NewLine}Mã hàng: {sku}";

    public static string First(params string?[] values) =>
        values.Select(value => value?.Trim()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}
