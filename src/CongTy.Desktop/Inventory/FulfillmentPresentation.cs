using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record FulfillmentFilterOption(string Id, string Label);

public sealed record FulfillmentOrderRow(
    int Stt,
    string SalesOrderId,
    string OrderNumber,
    string CustomerName,
    string CustomerCode,
    string OrderTotal,
    string Channel,
    string Warehouse,
    string RequestedDeliveryDate,
    string Status,
    string StatusBucket,
    IReadOnlyList<FulfillmentWorkItemData> Items,
    string SearchText);

public sealed class FulfillmentProductRow : INotifyPropertyChanged
{
    private string _allocationQuantity = string.Empty;

    public FulfillmentProductRow(int stt, FulfillmentWorkItemData data, bool canAllocate, string? outcomeStatus = null)
    {
        Stt = stt;
        Data = data;
        CanAllocate = canAllocate;
        OutcomeStatus = outcomeStatus;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Stt { get; }
    public FulfillmentWorkItemData Data { get; }
    public string ProductSku => $"{Data.ItemName}{Environment.NewLine}{Data.Sku}";
    public string Ordered => FulfillmentPresentation.OrderedQuantityLabel(Data);
    public string Allocated => FulfillmentPresentation.QuantityWithUnit(Data.AllocatedBaseQuantity, Data.BaseUnitCode);
    public string Remaining => FulfillmentPresentation.QuantityWithUnit(
        FulfillmentPresentation.QuantityDifference(Data.OrderedBaseQuantity, Data.AllocatedBaseQuantity),
        Data.BaseUnitCode);
    public string OnHand => FulfillmentPresentation.QuantityWithUnit(Data.WarehouseOnHandBaseQuantity, Data.BaseUnitCode);
    public string HeldOthers => FulfillmentPresentation.QuantityWithUnit(Data.WarehouseHeldByOthersBaseQuantity, Data.BaseUnitCode);
    public string Available => FulfillmentPresentation.QuantityWithUnit(Data.WarehouseAvailableBaseQuantity, Data.BaseUnitCode);
    public string Status => string.IsNullOrWhiteSpace(OutcomeStatus)
        ? FulfillmentPresentation.StatusLabel(Data.FulfillmentStatus)
        : OutcomeStatus;
    public string? OutcomeStatus { get; }
    public bool HasRemaining => FulfillmentPresentation.IsPositive(
        FulfillmentPresentation.QuantityDifference(Data.OrderedBaseQuantity, Data.AllocatedBaseQuantity));
    public bool CanAllocate { get; }
    public bool CanAutoAllocate => CanAllocate && HasRemaining;
    public bool CanAllocateQuantity => CanAutoAllocate && !string.IsNullOrWhiteSpace(AllocationQuantity);

    public string AllocationQuantity
    {
        get => _allocationQuantity;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_allocationQuantity, next, StringComparison.Ordinal)) return;
            _allocationQuantity = next;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllocationQuantity)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAllocateQuantity)));
        }
    }
}

public sealed record FulfillmentCandidateRow(
    int Stt,
    string Location,
    string Lot,
    string Available,
    string ExpiryReceived,
    FulfillmentCandidateData Data);

public sealed record FulfillmentAllocationRow(
    int Stt,
    string LocationLot,
    string Allocated,
    string Picked,
    string Packed,
    string Status,
    string PickAction,
    string PackAction,
    string PickRemaining,
    string PackRemaining,
    bool CanPick,
    bool CanPack,
    FulfillmentAllocationData Data);

public static partial class FulfillmentPresentation
{
    private const int ScaleDigits = 12;
    private static readonly BigInteger Scale = BigInteger.Pow(10, ScaleDigits);
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string FormatQuantity(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        var dot = normalized.IndexOf('.');
        if (dot < 0) return normalized;
        var trimmed = normalized.TrimEnd('0').TrimEnd('.');
        return trimmed.Length == 0 ? "0" : trimmed;
    }

    public static string QuantityWithUnit(string? value, string? unit)
    {
        var quantity = FormatQuantity(value);
        return string.IsNullOrWhiteSpace(unit) ? quantity : $"{quantity} {unit.Trim()}";
    }

    public static string QuantityDifference(string? left, string? right)
    {
        if (!TryScaled(left, out var lhs) || !TryScaled(right, out var rhs)) return "0";
        var result = BigInteger.Max(BigInteger.Zero, lhs - rhs);
        return ScaledToQuantity(result);
    }

    public static bool IsPositive(string? value) =>
        TryScaled(value, out var scaled) && scaled > BigInteger.Zero;

    public static bool IsValidPositiveQuantity(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && QuantityPattern().IsMatch(value.Trim())
        && TryScaled(value, out var scaled)
        && scaled > BigInteger.Zero;

    public static bool IsGreaterThan(string? left, string? right) =>
        TryScaled(left, out var lhs) && TryScaled(right, out var rhs) && lhs > rhs;

    public static string OrderedQuantityLabel(FulfillmentWorkItemData item)
    {
        var ordered = QuantityWithUnit(item.OrderedQuantity, item.OrderedUnitCode);
        var baseQuantity = QuantityWithUnit(item.OrderedBaseQuantity, item.BaseUnitCode);
        return string.Equals(item.OrderedUnitCode, item.BaseUnitCode, StringComparison.Ordinal)
            && string.Equals(FormatQuantity(item.OrderedQuantity), FormatQuantity(item.OrderedBaseQuantity), StringComparison.Ordinal)
                ? baseQuantity
                : $"{ordered} → {baseQuantity}";
    }

    public static string Money(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return "Chưa có tổng";
        return $"{amount.ToString("#,##0", Vi)} ₫";
    }

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Chưa đặt";
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date.ToString("dd/MM/yyyy", Vi)
            : value.Trim();
    }

    public static string StatusLabel(string value) => value switch
    {
        "backordered" => "Chờ thêm hàng",
        "partially_reserved" => "Có hàng một phần",
        "reserved" => "Chờ phân bổ",
        "partially_allocated" => "Phân bổ một phần",
        "allocated" => "Đã phân bổ",
        "partially_picked" => "Đang soạn",
        "picked" => "Đã soạn",
        "partially_packed" => "Đang đóng gói",
        "packed" => "Đã đóng gói",
        _ => "Trạng thái khác"
    };

    public static string StatusBucket(string value) => value switch
    {
        "backordered" or "partially_reserved" or "reserved" => "waiting",
        "partially_allocated" or "allocated" => "allocated",
        "partially_picked" or "picked" => "picking",
        "partially_packed" or "packed" => "packing",
        _ => "other"
    };

    public static string OutcomeLabel(string value) => value switch
    {
        "READY" => "Đã phân bổ đủ",
        "SHORTAGE" => "Chưa đủ hàng",
        _ => "Chưa phân bổ hết"
    };

    public static string CandidateDate(FulfillmentCandidateData data) =>
        !string.IsNullOrWhiteSpace(data.ExpiryDate)
            ? $"HSD {Date(data.ExpiryDate)}"
            : $"Nhập {Date(data.FirstReceivedAt)}";

    public static string AllocationStatus(FulfillmentAllocationData data) =>
        string.Equals(data.State, "COMPLETED", StringComparison.Ordinal)
            ? "Đã đóng gói"
            : "Đang xử lý";

    private static bool TryScaled(string? value, out BigInteger scaled)
    {
        scaled = BigInteger.Zero;
        var normalized = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        var match = ScaledPattern().Match(normalized);
        if (!match.Success) return false;

        var whole = BigInteger.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var fraction = match.Groups[2].Success
            ? match.Groups[2].Value.PadRight(ScaleDigits, '0')
            : new string('0', ScaleDigits);
        scaled = whole * Scale + BigInteger.Parse(fraction, CultureInfo.InvariantCulture);
        return true;
    }

    private static string ScaledToQuantity(BigInteger value)
    {
        var whole = value / Scale;
        var fraction = (value % Scale).ToString(CultureInfo.InvariantCulture).PadLeft(ScaleDigits, '0').TrimEnd('0');
        return fraction.Length == 0 ? whole.ToString(CultureInfo.InvariantCulture) : $"{whole}.{fraction}";
    }

    [GeneratedRegex(@"^(?:0|[1-9]\d{0,17})(?:\.\d{1,12})?$", RegexOptions.CultureInvariant)]
    private static partial Regex QuantityPattern();

    [GeneratedRegex(@"^(\d+)(?:\.(\d{1,12}))?$", RegexOptions.CultureInvariant)]
    private static partial Regex ScaledPattern();
}
