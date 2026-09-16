using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed record TripOption(string Id, string Label);

public sealed record DeliveryTripListRow(
    DeliveryTripData Data,
    string Number,
    string RouteWarehouse,
    string Summary);

public sealed record TripAssignmentRow(
    TripAssignmentData Data,
    string Number,
    string Customer);

public sealed record TripStopRow(
    TripStopData Data,
    string Title,
    string Address,
    IReadOnlyList<TripAssignmentRow> Assignments,
    bool CanMoveUp,
    bool CanMoveDown);

public sealed class EligibleDeliveryOrderRow : INotifyPropertyChanged
{
    private bool _isSelected;

    public EligibleDeliveryOrderRow(TripEligibleDeliveryOrderData data)
    {
        Data = data;
    }

    public TripEligibleDeliveryOrderData Data { get; }
    public bool CanSelect => !string.IsNullOrWhiteSpace(Data.Number);
    public string Number => TripPlanningPresentation.BusinessNumber(Data.Number, "Thiếu mã phiếu giao");
    public string Customer => $"{Data.CustomerCode} · {Data.CustomerName}";
    public string Summary => $"{TripPlanningPresentation.Quantity(Data.TotalBaseQuantity)} · {Data.LineCount} dòng";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            var next = CanSelect && value;
            if (_isSelected == next) return;
            _isSelected = next;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public static class TripPlanningPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Status(string? value) => value switch
    {
        "draft" => "Nháp",
        "planned" => "Đã lập kế hoạch",
        "locked" => "Đã khóa",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string Quantity(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        return text.Contains('.', StringComparison.Ordinal)
            ? text.TrimEnd('0').TrimEnd('.')
            : text;
    }

    public static string BusinessNumber(string? value, string missing) =>
        string.IsNullOrWhiteSpace(value) ? missing : value.Trim();

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Chưa đặt";
        return DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed.ToString("dd/MM/yyyy", Vi)
            : value.Trim();
    }

    public static string Address(IReadOnlyDictionary<string, JsonElement>? address)
    {
        if (address is null || address.Count == 0) return "Địa chỉ giao hàng đã chốt";
        var candidates = new[]
        {
            "addressLine1", "line1", "fullAddress", "address",
            "wardName", "districtName", "provinceName"
        };
        var values = new List<string>();
        foreach (var key in candidates)
        {
            if (!address.TryGetValue(key, out var element) || element.ValueKind != JsonValueKind.String) continue;
            var value = element.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value, StringComparer.Ordinal))
                values.Add(value);
        }
        return values.Count == 0 ? "Địa chỉ giao hàng đã chốt" : string.Join(", ", values);
    }

    public static string LocalDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return value.Trim();

        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTime(parsed, zone).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }
        catch
        {
            return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }
    }

    public static bool TryIsoDateTime(string? localValue, out string? iso)
    {
        iso = null;
        if (string.IsNullOrWhiteSpace(localValue)) return true;
        if (!DateTime.TryParseExact(
                localValue.Trim(),
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var local))
            return false;

        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            iso = new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }
        catch
        {
            iso = new DateTimeOffset(local).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }
        return true;
    }

    public static DeliveryTripListRow Trip(DeliveryTripData trip) => new(
        trip,
        BusinessNumber(trip.Number, "Chuyến giao"),
        $"{BusinessNumber(trip.RouteCode, "Chưa chọn tuyến")} · {BusinessNumber(trip.WarehouseCode, "Kho")}",
        $"{Status(trip.Status)} · {trip.StopCount ?? 0} điểm · {trip.AssignmentCount ?? 0} phiếu");

    public static IReadOnlyList<TripStopRow> Stops(DeliveryTripData trip)
    {
        var rows = trip.Stops.OrderBy(stop => stop.Sequence).ToArray();
        return rows.Select((stop, index) => new TripStopRow(
            stop,
            $"Điểm {stop.Sequence}",
            Address(stop.Address),
            stop.Assignments.Select(assignment => new TripAssignmentRow(
                assignment,
                BusinessNumber(assignment.DeliveryOrderNumber, "Thiếu mã phiếu giao"),
                $"{assignment.CustomerCode} · {assignment.CustomerName}")).ToArray(),
            index > 0,
            index < rows.Length - 1)).ToArray();
    }

    public static TripOption Warehouse(TripWarehouseData row) =>
        new(row.Id, $"{row.Code} · {row.Name}");

    public static TripOption Route(LogisticsRouteData row, bool includeWarehouse = false)
    {
        var suffix = includeWarehouse
            ? $" · {BusinessNumber(row.DefaultWarehouseCode, "Kho tuyến")}"
            : string.Empty;
        return new TripOption(row.Id, $"{row.Code} · {row.Name}{suffix}");
    }

    public static TripOption Vehicle(LogisticsVehicleData row) =>
        new(row.Id, $"{row.Code} · {row.LicensePlate}");

    public static TripOption Driver(LogisticsDriverData row) =>
        new(row.Id, $"{row.Code} · {row.Name}");

    public static TripOption Employee(LogisticsDriverEmployeeData row)
    {
        var suffix = string.IsNullOrWhiteSpace(row.JobTitle) ? string.Empty : $" · {row.JobTitle}";
        return new TripOption(row.Id, $"{row.Code} · {row.FullName}{suffix}");
    }
}
