using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed record TripDispatchListRow(
    DeliveryTripData Data,
    string Number,
    string WarehouseStatus,
    string Workload,
    string VehicleDriver);

public sealed record TripDispatchAssignmentRow(
    TripDispatchAssignmentData Data,
    string Number,
    string Customer);

public sealed record TripDispatchStopRow(
    TripDispatchStopData Data,
    string Title,
    IReadOnlyList<TripDispatchAssignmentRow> Assignments);

public sealed record TripDispatchMovementRow(
    TripDispatchItemData Data,
    string Number,
    string Customer,
    string MovementReference);

public static class TripDispatchPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Status(string? value) => value switch
    {
        "locked" => "Chờ bàn giao",
        "dispatched" => "Đã xuất phát",
        "draft" => "Nháp",
        "planned" => "Đã lập kế hoạch",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string Number(string? value, string fallback = "Thiếu mã phiếu giao") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    public static string LocalDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Chưa ghi nhận";
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return value.Trim();

        return InBusinessZone(parsed).ToString("dd/MM/yyyy HH:mm", Vi);
    }

    public static string LocalInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return LocalInputNow();
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return LocalInputNow();

        return InBusinessZone(parsed).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    public static string LocalInputNow() =>
        InBusinessZone(DateTimeOffset.UtcNow).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public static bool TryIsoDateTime(string? value, out string iso)
    {
        iso = string.Empty;
        if (!DateTime.TryParseExact(
                value?.Trim(),
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var local))
            return false;

        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        var zone = BusinessZone();
        iso = new DateTimeOffset(local, zone.GetUtcOffset(local))
            .ToUniversalTime()
            .ToString("O", CultureInfo.InvariantCulture);
        return true;
    }

    public static TripDispatchListRow ListRow(DeliveryTripData trip) => new(
        trip,
        Number(trip.Number, "Chuyến giao"),
        $"{Number(trip.WarehouseCode, "Kho")} · {Status(trip.Status)}",
        $"{trip.StopCount ?? 0} điểm · {trip.AssignmentCount ?? 0} phiếu",
        $"{Number(trip.LicensePlate ?? trip.VehicleCode, "Chưa rõ xe")} · {Number(trip.DriverName, "Chưa rõ tài xế")}");

    public static IReadOnlyList<TripDispatchStopRow> Stops(TripDispatchData trip) =>
        trip.Stops
            .OrderBy(stop => stop.Sequence)
            .Select(stop => new TripDispatchStopRow(
                stop,
                $"Điểm {stop.Sequence}",
                stop.Assignments.Select(assignment => new TripDispatchAssignmentRow(
                    assignment,
                    Number(assignment.DeliveryOrderNumber),
                    $"{Number(assignment.CustomerCode, "Khách")} · {assignment.CustomerName ?? string.Empty}".TrimEnd(' ', '·'))).ToArray()))
            .ToArray();

    public static IReadOnlyList<TripDispatchMovementRow> Movements(TripDispatchData trip) =>
        trip.DispatchItems.Select(item => new TripDispatchMovementRow(
            item,
            Number(item.DeliveryOrderNumber),
            $"{Number(item.CustomerCode, "Khách")} · {item.CustomerName ?? string.Empty}".TrimEnd(' ', '·'),
            item.InventoryMovementId)).ToArray();

    private static DateTimeOffset InBusinessZone(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, BusinessZone());

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
