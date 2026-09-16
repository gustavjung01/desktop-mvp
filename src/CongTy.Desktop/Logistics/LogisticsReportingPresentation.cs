using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed record LogisticsWarehouseOption(string Id, string Label);

public sealed record LogisticsActorRow(
    string Identity,
    string Trips,
    string Stops,
    string Orders,
    string Full,
    string Partial,
    string Failed,
    string Rescheduled,
    string OnTime,
    string AverageClosedTrip);

public sealed record LogisticsFailureReasonRow(string Result, string Reason, string Count);

public sealed record LogisticsTripRow(
    string Trip,
    string Warehouse,
    string DriverVehicle,
    string Time,
    string Status,
    string StopsOrders,
    string FullPartial,
    string FailedRescheduled,
    string OnTime,
    string Pending);

public sealed record LogisticsExceptionCard(string Label, string Value, string Hint);

public static class LogisticsReportingPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo BusinessTimezone = ResolveBusinessTimezone();

    public static string Count(string? value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed.ToString("N0", Vi)
            : string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();

    public static string Percent(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? $"{parsed.ToString("0.##", Vi)}%"
            : "—";

    public static string Duration(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? $"{parsed.ToString("0.##", Vi)} phút"
            : "—";

    public static string Timestamp(string? value)
    {
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return "—";
        }

        return TimeZoneInfo.ConvertTime(parsed, BusinessTimezone).ToString("dd/MM/yyyy HH:mm", Vi);
    }

    public static string ResultLabel(string? value) => value switch
    {
        "failed" => "Giao không thành công",
        "rescheduled" => "Hẹn giao lại",
        "delivered_full" => "Giao đủ",
        "delivered_partial" => "Giao một phần",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string StatusLabel(string? value) => value switch
    {
        "planned" => "Đã lập kế hoạch",
        "ready" => "Sẵn sàng",
        "dispatched" => "Đã xuất phát",
        "closed" => "Đã đóng chuyến",
        "cancelled" => "Đã hủy",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string ExceptionLabel(string? value) => value switch
    {
        "MISSING_PLANNED_ARRIVAL" => "Thiếu giờ dự kiến tại điểm giao",
        "PENDING_DELIVERY_RESULT" => "Phiếu đã xuất chuyến nhưng chưa có kết quả giao",
        _ => "Ngoại lệ cần đối chiếu"
    };

    public static LogisticsActorRow Driver(LogisticsReportingActorData row) => new(
        Identity: $"{row.DriverCode ?? "Chưa gán"}\n{row.DriverName ?? "—"}",
        Trips: Count(row.TripCount),
        Stops: Count(row.StopCount),
        Orders: Count(row.DeliveryOrderCount),
        Full: Count(row.DeliveredFullCount),
        Partial: Count(row.DeliveredPartialCount),
        Failed: Count(row.FailedCount),
        Rescheduled: Count(row.RescheduledCount),
        OnTime: Percent(row.OnTimeFullRatePercent),
        AverageClosedTrip: Duration(row.AverageClosedTripDurationMinutes));

    public static LogisticsActorRow Vehicle(LogisticsReportingActorData row)
    {
        var second = string.Join(" · ", new[] { row.LicensePlate, row.VehicleType }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return new LogisticsActorRow(
            Identity: $"{row.VehicleCode ?? "Chưa gán"}\n{(string.IsNullOrWhiteSpace(second) ? "—" : second)}",
            Trips: Count(row.TripCount),
            Stops: Count(row.StopCount),
            Orders: Count(row.DeliveryOrderCount),
            Full: Count(row.DeliveredFullCount),
            Partial: Count(row.DeliveredPartialCount),
            Failed: Count(row.FailedCount),
            Rescheduled: Count(row.RescheduledCount),
            OnTime: Percent(row.OnTimeFullRatePercent),
            AverageClosedTrip: Duration(row.AverageClosedTripDurationMinutes));
    }

    public static string ReasonLabel(string? value) => value switch
    {
        "CUSTOMER_CLOSED" => "Khách đóng cửa",
        "CUSTOMER_REFUSED" => "Khách từ chối nhận",
        "ADDRESS_ISSUE" => "Không xác định được địa chỉ",
        "REQUESTED_NEW_TIME" => "Khách yêu cầu thời gian giao khác",
        _ => string.IsNullOrWhiteSpace(value) ? "Không có lý do" : "Lý do khác"
    };

    public static LogisticsFailureReasonRow FailureReason(LogisticsReportingFailureReasonData row) => new(
        ResultLabel(row.Result),
        ReasonLabel(row.ReasonCode),
        Count(row.AttemptCount));

    public static LogisticsTripRow Trip(LogisticsReportingTripData row) => new(
        Trip: string.IsNullOrWhiteSpace(row.TripNumber) ? "—" : row.TripNumber,
        Warehouse: $"{row.WarehouseCode}\n{row.WarehouseName}".Trim(),
        DriverVehicle: $"{row.DriverCode ?? "—"} · {row.DriverName ?? "—"}\n{row.VehicleCode ?? "—"} · {row.LicensePlate ?? "—"}",
        Time: $"Kế hoạch {Timestamp(row.PlannedStartAt)}\nXuất phát {Timestamp(row.DispatchedAt)}\nĐóng chuyến {Timestamp(row.ClosedAt)}",
        Status: StatusLabel(row.Status),
        StopsOrders: $"{Count(row.StopCount)} / {Count(row.DeliveryOrderCount)}",
        FullPartial: $"{Count(row.DeliveredFullCount)} / {Count(row.DeliveredPartialCount)}",
        FailedRescheduled: $"{Count(row.FailedCount)} / {Count(row.RescheduledCount)}",
        OnTime: Percent(row.OnTimeFullRatePercent),
        Pending: Count(row.PendingResultCount));

    private static TimeZoneInfo ResolveBusinessTimezone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Local; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Local; }
    }
}
