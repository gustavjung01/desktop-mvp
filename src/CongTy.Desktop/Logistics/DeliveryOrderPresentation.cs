using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed class DeliveryEligibilityLineRow : INotifyPropertyChanged
{
    private string _quantity;

    public DeliveryEligibilityLineRow(DeliveryOrderEligibilityData data)
    {
        Data = data;
        _quantity = DeliveryOrderPresentation.Quantity(data.AvailableForDeliveryOrderBaseQuantity);
    }

    public DeliveryOrderEligibilityData Data { get; }
    public string Product => $"{Data.Sku} — {Data.ItemName}";
    public string LocationLot => $"{(string.IsNullOrWhiteSpace(Data.LocationCode) ? "Không vị trí" : Data.LocationCode)} · Lô {(string.IsNullOrWhiteSpace(Data.LotCode) ? "Không lô" : Data.LotCode)}";
    public string PackedInfo => $"Đã gói {DeliveryOrderPresentation.Quantity(Data.PackedBaseQuantity)} · Đã dùng {DeliveryOrderPresentation.Quantity(Data.ClaimedBaseQuantity)} · Còn thiếu {DeliveryOrderPresentation.Quantity(Data.BackorderedBaseQuantity)}";
    public string Maximum => $"Tối đa {DeliveryOrderPresentation.Quantity(Data.AvailableForDeliveryOrderBaseQuantity)} {Data.UnitCode}";

    public string Quantity
    {
        get => _quantity;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_quantity, next, StringComparison.Ordinal)) return;
            _quantity = next;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed record DeliveryEligibilityGroupRow(
    string Key,
    string SalesOrder,
    string Customer,
    string Warehouse,
    string HandoverMode,
    string Date,
    string Summary,
    IReadOnlyList<DeliveryEligibilityLineRow> Lines);

public sealed record DeliveryOrderListRow(
    DeliveryOrderData Data,
    string Number,
    string Status,
    string Customer,
    string Source,
    string Summary);

public sealed record DeliveryOrderLineRow(
    string Product,
    string LocationLot,
    string Quantity);

public static class DeliveryOrderPresentation
{
    private const int QuantityScale = 12;
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Quantity(string? value)
    {
        var normalized = StringValue(value, "0");
        if (!normalized.Contains('.', StringComparison.Ordinal)) return normalized;
        return normalized.TrimEnd('0').TrimEnd('.');
    }

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Chưa đặt";
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToString("dd/MM/yyyy", Vi);
        return value.Trim();
    }

    public static string Status(string? value) => value switch
    {
        "draft" => "Nháp",
        "ready_to_dispatch" => "Sẵn sàng bàn giao",
        "dispatched" => "Đã xuất theo chuyến",
        "handed_over" => "Đã bàn giao",
        "cancelled" => "Đã hủy",
        _ => "Không xác định"
    };

    public static string HandoverMode(string? value) =>
        string.Equals(value, "PICKUP", StringComparison.Ordinal) ? "Nhận tại quầy" : "Giao hàng";

    public static bool TryScaledQuantity(string? value, out BigInteger scaled)
    {
        scaled = BigInteger.Zero;
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0) return false;
        var parts = normalized.Split('.');
        if (parts.Length > 2 || parts[0].Length == 0 || !parts[0].All(char.IsDigit)) return false;
        var fraction = parts.Length == 2 ? parts[1] : string.Empty;
        if (fraction.Length > QuantityScale || (fraction.Length > 0 && !fraction.All(char.IsDigit))) return false;
        if (!BigInteger.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var whole)) return false;
        var scale = BigInteger.Pow(10, QuantityScale);
        var fractionValue = fraction.Length == 0
            ? BigInteger.Zero
            : BigInteger.Parse(fraction.PadRight(QuantityScale, '0'), CultureInfo.InvariantCulture);
        scaled = whole * scale + fractionValue;
        return true;
    }

    public static DeliveryEligibilityGroupRow Group(IEnumerable<DeliveryOrderEligibilityData> rows)
    {
        var array = rows.ToArray();
        var first = array[0];
        return new DeliveryEligibilityGroupRow(
            $"{first.SalesOrderId}:{first.SalesOrderVersionId}:{first.WarehouseId}",
            StringValue(first.SalesOrderNumber, "Đơn bán hàng"),
            $"{first.CustomerCode} — {first.CustomerName}",
            $"{first.WarehouseCode} — {first.WarehouseName}",
            HandoverMode(first.HandoverMode),
            Date(first.RequestedDeliveryDate),
            $"{array.Length} dòng",
            array.Select(row => new DeliveryEligibilityLineRow(row)).ToArray());
    }

    public static DeliveryOrderListRow Order(DeliveryOrderData data) => new(
        data,
        StringValue(data.Number, "Chứng từ nháp"),
        Status(data.Status),
        $"{data.CustomerCode} — {data.CustomerName}",
        $"{StringValue(data.SalesOrderNumber, "Đơn bán hàng")} · {data.WarehouseCode}",
        $"{data.LineCount ?? 0} dòng · {Quantity(data.TotalBaseQuantity)}");

    public static DeliveryOrderLineRow Line(DeliveryOrderLineData data) => new(
        $"{data.Sku} — {data.ItemName}",
        $"{StringValue(data.LocationCode, "Không vị trí")} · Lô {StringValue(data.LotCode, "Không lô")}",
        $"{Quantity(data.DeliveryBaseQuantity)} {data.UnitCode}");

    private static string StringValue(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
