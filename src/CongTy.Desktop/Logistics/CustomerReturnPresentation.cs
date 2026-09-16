using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed record CustomerReturnReasonOption(string Code, string Label);

public sealed record CustomerReturnEligibilityRow(
    CustomerReturnEligibilityData Data,
    string Number,
    string Customer,
    string Item,
    string Source,
    string Quantity);

public sealed record CustomerReturnListRow(
    CustomerReturnData Data,
    string Number,
    string Customer,
    string Summary,
    string Status);

public sealed class CustomerReturnLineRow : INotifyPropertyChanged
{
    private string _acceptedQuantity;

    public CustomerReturnLineRow(CustomerReturnLineData data, bool editable)
    {
        Data = data;
        _acceptedQuantity = editable
            ? CustomerReturnPresentation.Quantity(data.RequestedBaseQuantity)
            : CustomerReturnPresentation.Quantity(data.AcceptedBaseQuantity);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public CustomerReturnLineData Data { get; }
    public string Item => $"{Data.Sku} — {Data.ItemName}";
    public string Source => $"{CustomerReturnPresentation.Number(Data.DeliveryOrderNumber, "Phiếu giao")} · {Data.LocationCode ?? "Không vị trí"} · Lô {Data.LotCode ?? "Không lô"}";
    public string Reason => $"{CustomerReturnPresentation.Reason(Data.ReasonCode)}: {Data.ReasonNote}";
    public string Requested => $"{CustomerReturnPresentation.Quantity(Data.RequestedBaseQuantity)} {Data.UnitCode}";

    public string AcceptedQuantity
    {
        get => _acceptedQuantity;
        set
        {
            if (_acceptedQuantity == value) return;
            _acceptedQuantity = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AcceptedQuantity)));
        }
    }
}

public static class CustomerReturnPresentation
{
    private static readonly BigInteger Scale = BigInteger.Pow(10, 12);

    public static IReadOnlyList<CustomerReturnReasonOption> Reasons { get; } =
    [
        new("DAMAGED_OR_UNWANTED", "Hư hỏng / không nhận"),
        new("WRONG_ITEM", "Sai hàng"),
        new("QUALITY_COMPLAINT", "Khiếu nại chất lượng"),
        new("OTHER", "Khác")
    ];

    public static string Status(string? value) => value switch
    {
        "draft" => "Nháp chờ nhận",
        "received" => "Đã nhận vào kho",
        "cancelled" => "Đã hủy",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string Reason(string? value) =>
        Reasons.FirstOrDefault(row => row.Code == value)?.Label ?? (string.IsNullOrWhiteSpace(value) ? "Khác" : value.Trim());

    public static string Number(string? value, string fallback = "Phiếu nháp") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    public static bool TryScaledQuantity(string? value, out BigInteger scaled)
    {
        scaled = BigInteger.Zero;
        var text = value?.Trim() ?? string.Empty;
        var parts = text.Split('.', 2);
        if (parts[0].Length == 0 || parts[0].Any(ch => ch < '0' || ch > '9')) return false;
        var fraction = parts.Length == 2 ? parts[1] : string.Empty;
        if (fraction.Length > 12 || fraction.Any(ch => ch < '0' || ch > '9')) return false;
        if (!BigInteger.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var whole)) return false;
        var padded = fraction.PadRight(12, '0');
        var frac = padded.Length == 0 ? BigInteger.Zero : BigInteger.Parse(padded, CultureInfo.InvariantCulture);
        scaled = whole * Scale + frac;
        return true;
    }

    public static string Quantity(string? value)
    {
        if (!TryScaledQuantity(value, out var scaled)) return value?.Trim() ?? "0";
        var whole = scaled / Scale;
        var fraction = (scaled % Scale).ToString().PadLeft(12, '0').TrimEnd('0');
        return fraction.Length == 0 ? whole.ToString() : $"{whole}.{fraction}";
    }

    public static string LocalDate() =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BusinessZone()).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static CustomerReturnEligibilityRow Eligibility(CustomerReturnEligibilityData row) => new(
        row,
        Number(row.DeliveryOrderNumber, "Phiếu giao"),
        $"{row.CustomerCode} — {row.CustomerName}",
        $"{row.Sku} — {row.ItemName}",
        $"{row.WarehouseCode} · {row.LocationCode ?? "Không vị trí"} · Lô {row.LotCode ?? "Không lô"}",
        $"Còn {Quantity(row.AvailableReturnBaseQuantity)} {row.UnitCode}");

    public static CustomerReturnListRow ListRow(CustomerReturnData row) => new(
        row,
        Number(row.Number),
        $"{row.CustomerCode} — {row.CustomerName}",
        $"{row.WarehouseCode} · {row.LineCount ?? 0} dòng · Yêu cầu {Quantity(row.RequestedBaseQuantity)}",
        Status(row.Status));

    private static TimeZoneInfo BusinessZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            catch { return TimeZoneInfo.Local; }
        }
    }
}
