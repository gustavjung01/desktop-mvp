using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed record TripReconciliationTripRow(
    DeliveryTripData Data,
    string Number,
    string WarehouseDriver,
    string VehicleStatus);

public sealed class TripReconciliationLineRow : INotifyPropertyChanged
{
    private string _returnQuantity;

    public TripReconciliationLineRow(TripReconciliationLineData data)
    {
        Data = data;
        NumberItem = $"{TripReconciliationPresentation.Number(data.DeliveryOrderNumber)} · {data.Sku} — {data.ItemName}";
        SourceText = $"{data.LocationCode ?? "Vị trí gốc"}{(string.IsNullOrWhiteSpace(data.LotCode) ? string.Empty : $" · Lô {data.LotCode}")}";
        ResultText = TripReconciliationPresentation.Result(data.AttemptResult);
        Issued = TripReconciliationPresentation.Quantity(data.IssuedBaseQuantity);
        Delivered = TripReconciliationPresentation.Quantity(data.DeliveredBaseQuantity);
        Returned = TripReconciliationPresentation.Quantity(data.ReturnedBaseQuantity);
        Outstanding = TripReconciliationPresentation.Quantity(data.OutstandingBaseQuantity);
        _returnQuantity = TripReconciliationPresentation.IsPositive(data.OutstandingBaseQuantity)
            ? Outstanding
            : string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public TripReconciliationLineData Data { get; }
    public string NumberItem { get; }
    public string SourceText { get; }
    public string ResultText { get; }
    public string Issued { get; }
    public string Delivered { get; }
    public string Returned { get; }
    public string Outstanding { get; }
    public bool HasOutstanding => TripReconciliationPresentation.IsPositive(Data.OutstandingBaseQuantity);

    public string ReturnQuantity
    {
        get => _returnQuantity;
        set
        {
            if (_returnQuantity == value) return;
            _returnQuantity = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReturnQuantity)));
        }
    }
}

public sealed record TripReceiptRow(
    TripReturnReceiptData Data,
    string ReceivedAt,
    string Movement,
    string Summary);

public static class TripReconciliationPresentation
{
    private static readonly BigInteger Scale = BigInteger.Pow(10, 12);
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Result(string? value) => value switch
    {
        "delivered_full" => "Giao đủ",
        "delivered_partial" => "Giao một phần",
        "failed" => "Không giao được",
        "rescheduled" => "Hẹn giao lại",
        null or "" => "Chưa có kết quả",
        _ => value.Trim()
    };

    public static string Number(string? value, string fallback = "Thiếu mã phiếu giao") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    public static bool TryScaledQuantity(string? value, out BigInteger scaled)
    {
        scaled = BigInteger.Zero;
        var text = value?.Trim() ?? string.Empty;
        if (text.StartsWith('+')) text = text[1..];
        if (text.Length == 0) return false;
        var parts = text.Split('.', 2);
        if (parts.Length > 2 || parts[0].Length == 0 || parts[0].Any(ch => ch < '0' || ch > '9')) return false;
        var fraction = parts.Length == 2 ? parts[1] : string.Empty;
        if (fraction.Length > 12 || fraction.Any(ch => ch < '0' || ch > '9')) return false;
        if (!BigInteger.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var whole)) return false;
        var padded = fraction.PadRight(12, '0');
        var frac = padded.Length == 0 ? BigInteger.Zero : BigInteger.Parse(padded, CultureInfo.InvariantCulture);
        scaled = whole * Scale + frac;
        return true;
    }

    public static bool IsPositive(string? value) =>
        TryScaledQuantity(value, out var scaled) && scaled > BigInteger.Zero;

    public static string Quantity(string? value)
    {
        if (!TryScaledQuantity(value, out var scaled)) return value?.Trim() ?? "0";
        var whole = scaled / Scale;
        var fraction = (scaled % Scale).ToString().PadLeft(12, '0').TrimEnd('0');
        return fraction.Length == 0 ? whole.ToString() : $"{whole}.{fraction}";
    }

    public static string LocalDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Chưa ghi nhận";
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            return value.Trim();
        return TimeZoneInfo.ConvertTime(parsed, BusinessZone()).ToString("dd/MM/yyyy HH:mm", Vi);
    }

    public static string LocalInputNow() =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BusinessZone()).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public static bool TryIsoDateTime(string? value, out string iso)
    {
        iso = string.Empty;
        if (!DateTime.TryParseExact(value?.Trim(), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            return false;
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        var zone = BusinessZone();
        iso = new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        return true;
    }

    public static TripReconciliationTripRow Trip(DeliveryTripData trip) => new(
        trip,
        Number(trip.Number, "Chuyến giao"),
        $"{trip.WarehouseCode ?? trip.WarehouseName ?? "Kho"} · {trip.DriverName ?? trip.DriverCode ?? "Tài xế"}",
        $"{trip.LicensePlate ?? trip.VehicleCode ?? "Chưa rõ xe"} · {(trip.Status == "closed" ? "Đã đóng" : "Cần đối soát")}");

    public static TripReceiptRow Receipt(TripReturnReceiptData receipt) => new(
        receipt,
        LocalDateTime(receipt.ReceivedAt),
        string.IsNullOrWhiteSpace(receipt.InventoryMovementId) ? "—" : receipt.InventoryMovementId[..Math.Min(8, receipt.InventoryMovementId.Length)],
        $"{receipt.Lines.Length} dòng · {(string.IsNullOrWhiteSpace(receipt.Note) ? "Không ghi chú" : receipt.Note.Trim())}");

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
