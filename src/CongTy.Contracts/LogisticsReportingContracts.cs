using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record LogisticsReportingFiltersData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
}

public sealed record LogisticsReportingSummaryData
{
    [JsonPropertyName("tripCount")] public string? TripCount { get; init; }
    [JsonPropertyName("stopCount")] public string? StopCount { get; init; }
    [JsonPropertyName("deliveryOrderCount")] public string? DeliveryOrderCount { get; init; }
    [JsonPropertyName("deliveredFullCount")] public string? DeliveredFullCount { get; init; }
    [JsonPropertyName("deliveredPartialCount")] public string? DeliveredPartialCount { get; init; }
    [JsonPropertyName("failedCount")] public string? FailedCount { get; init; }
    [JsonPropertyName("rescheduledCount")] public string? RescheduledCount { get; init; }
    [JsonPropertyName("onTimeEligibleFullCount")] public string? OnTimeEligibleFullCount { get; init; }
    [JsonPropertyName("fullWithoutPlanCount")] public string? FullWithoutPlanCount { get; init; }
    [JsonPropertyName("pendingResultCount")] public string? PendingResultCount { get; init; }
    [JsonPropertyName("onTimeFullRatePercent")] public string? OnTimeFullRatePercent { get; init; }
    [JsonPropertyName("slaCoveragePercent")] public string? SlaCoveragePercent { get; init; }
    [JsonPropertyName("averageClosedTripDurationMinutes")] public string? AverageClosedTripDurationMinutes { get; init; }
}

public sealed record LogisticsReportingWarehouseData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
}

public sealed record LogisticsReportingActorData
{
    [JsonPropertyName("driverProfileId")] public string? DriverProfileId { get; init; }
    [JsonPropertyName("driverCode")] public string? DriverCode { get; init; }
    [JsonPropertyName("driverName")] public string? DriverName { get; init; }
    [JsonPropertyName("vehicleId")] public string? VehicleId { get; init; }
    [JsonPropertyName("vehicleCode")] public string? VehicleCode { get; init; }
    [JsonPropertyName("licensePlate")] public string? LicensePlate { get; init; }
    [JsonPropertyName("vehicleType")] public string? VehicleType { get; init; }
    [JsonPropertyName("tripCount")] public string TripCount { get; init; } = "0";
    [JsonPropertyName("stopCount")] public string StopCount { get; init; } = "0";
    [JsonPropertyName("deliveryOrderCount")] public string DeliveryOrderCount { get; init; } = "0";
    [JsonPropertyName("deliveredFullCount")] public string DeliveredFullCount { get; init; } = "0";
    [JsonPropertyName("deliveredPartialCount")] public string DeliveredPartialCount { get; init; } = "0";
    [JsonPropertyName("failedCount")] public string FailedCount { get; init; } = "0";
    [JsonPropertyName("rescheduledCount")] public string RescheduledCount { get; init; } = "0";
    [JsonPropertyName("onTimeFullRatePercent")] public string? OnTimeFullRatePercent { get; init; }
    [JsonPropertyName("averageClosedTripDurationMinutes")] public string? AverageClosedTripDurationMinutes { get; init; }
}

public sealed record LogisticsReportingFailureReasonData
{
    [JsonPropertyName("result")] public string Result { get; init; } = string.Empty;
    [JsonPropertyName("reasonCode")] public string ReasonCode { get; init; } = string.Empty;
    [JsonPropertyName("attemptCount")] public string AttemptCount { get; init; } = "0";
}

public sealed record LogisticsReportingTripData
{
    [JsonPropertyName("tripId")] public string TripId { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string TripNumber { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("deliveryRouteId")] public string? DeliveryRouteId { get; init; }
    [JsonPropertyName("routeCode")] public string? RouteCode { get; init; }
    [JsonPropertyName("routeName")] public string? RouteName { get; init; }
    [JsonPropertyName("vehicleCode")] public string? VehicleCode { get; init; }
    [JsonPropertyName("licensePlate")] public string? LicensePlate { get; init; }
    [JsonPropertyName("driverCode")] public string? DriverCode { get; init; }
    [JsonPropertyName("driverName")] public string? DriverName { get; init; }
    [JsonPropertyName("plannedStartAt")] public string PlannedStartAt { get; init; } = string.Empty;
    [JsonPropertyName("dispatchedAt")] public string? DispatchedAt { get; init; }
    [JsonPropertyName("closedAt")] public string? ClosedAt { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("stopCount")] public string StopCount { get; init; } = "0";
    [JsonPropertyName("deliveryOrderCount")] public string DeliveryOrderCount { get; init; } = "0";
    [JsonPropertyName("deliveredFullCount")] public string DeliveredFullCount { get; init; } = "0";
    [JsonPropertyName("deliveredPartialCount")] public string DeliveredPartialCount { get; init; } = "0";
    [JsonPropertyName("failedCount")] public string FailedCount { get; init; } = "0";
    [JsonPropertyName("rescheduledCount")] public string RescheduledCount { get; init; } = "0";
    [JsonPropertyName("onTimeFullRatePercent")] public string? OnTimeFullRatePercent { get; init; }
    [JsonPropertyName("pendingResultCount")] public string PendingResultCount { get; init; } = "0";
    [JsonPropertyName("tripDurationMinutes")] public string? TripDurationMinutes { get; init; }
}

public sealed record LogisticsReportingAttemptData
{
    [JsonPropertyName("attemptId")] public string AttemptId { get; init; } = string.Empty;
    [JsonPropertyName("tripId")] public string TripId { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string TripNumber { get; init; } = string.Empty;
    [JsonPropertyName("tripStopId")] public string TripStopId { get; init; } = string.Empty;
    [JsonPropertyName("stopSequence")] public JsonElement StopSequence { get; init; }
    [JsonPropertyName("plannedArrivalAt")] public string? PlannedArrivalAt { get; init; }
    [JsonPropertyName("deliveryOrderId")] public string DeliveryOrderId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("customerCodeSnapshot")] public string CustomerCodeSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("customerNameSnapshot")] public string CustomerNameSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("driverProfileId")] public string DriverProfileId { get; init; } = string.Empty;
    [JsonPropertyName("driverCode")] public string DriverCode { get; init; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; init; } = string.Empty;
    [JsonPropertyName("result")] public string Result { get; init; } = string.Empty;
    [JsonPropertyName("reasonCode")] public string? ReasonCode { get; init; }
    [JsonPropertyName("attemptedAt")] public string AttemptedAt { get; init; } = string.Empty;
    [JsonPropertyName("rescheduledFor")] public string? RescheduledFor { get; init; }
    [JsonPropertyName("onTime")] public bool? OnTime { get; init; }
}

public sealed record LogisticsReportingExceptionData
{
    [JsonPropertyName("exceptionCode")] public string ExceptionCode { get; init; } = string.Empty;
    [JsonPropertyName("exceptionCount")] public string ExceptionCount { get; init; } = "0";
}

public sealed record LogisticsReportingReconciliationData
{
    [JsonPropertyName("postedReturnReceiptCount")] public string? PostedReturnReceiptCount { get; init; }
    [JsonPropertyName("tripsWithReturnReceiptCount")] public string? TripsWithReturnReceiptCount { get; init; }
}

public sealed record LogisticsReportingDataQualityData
{
    [JsonPropertyName("exceptions")] public LogisticsReportingExceptionData[] Exceptions { get; init; } = [];
}

public sealed record LogisticsReportingDashboardData
{
    [JsonPropertyName("family")] public string Family { get; init; } = string.Empty;
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = string.Empty;
    [JsonPropertyName("filters")] public LogisticsReportingFiltersData Filters { get; init; } = new();
    [JsonPropertyName("summary")] public LogisticsReportingSummaryData Summary { get; init; } = new();
    [JsonPropertyName("warehouses")] public LogisticsReportingWarehouseData[] Warehouses { get; init; } = [];
    [JsonPropertyName("drivers")] public LogisticsReportingActorData[] Drivers { get; init; } = [];
    [JsonPropertyName("vehicles")] public LogisticsReportingActorData[] Vehicles { get; init; } = [];
    [JsonPropertyName("failureReasons")] public LogisticsReportingFailureReasonData[] FailureReasons { get; init; } = [];
    [JsonPropertyName("trips")] public LogisticsReportingTripData[] Trips { get; init; } = [];
    [JsonPropertyName("attempts")] public LogisticsReportingAttemptData[] Attempts { get; init; } = [];
    [JsonPropertyName("reconciliation")] public LogisticsReportingReconciliationData Reconciliation { get; init; } = new();
    [JsonPropertyName("dataQuality")] public LogisticsReportingDataQualityData DataQuality { get; init; } = new();
}
