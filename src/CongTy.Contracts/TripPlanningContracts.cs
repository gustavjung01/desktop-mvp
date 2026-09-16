using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record TripWarehouseData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
}

public sealed record LogisticsRouteData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("defaultWarehouseId")] public string? DefaultWarehouseId { get; init; }
    [JsonPropertyName("defaultWarehouseCode")] public string? DefaultWarehouseCode { get; init; }
    [JsonPropertyName("defaultWarehouseName")] public string? DefaultWarehouseName { get; init; }
    [JsonPropertyName("isActive")] public bool IsActive { get; init; }
}

public sealed record LogisticsVehicleData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("licensePlate")] public string LicensePlate { get; init; } = string.Empty;
    [JsonPropertyName("vehicleType")] public string VehicleType { get; init; } = string.Empty;
    [JsonPropertyName("operationalStatus")] public string OperationalStatus { get; init; } = string.Empty;
    [JsonPropertyName("isActive")] public bool IsActive { get; init; }
}

public sealed record LogisticsDriverData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("employeeId")] public string? EmployeeId { get; init; }
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("licenseReference")] public string? LicenseReference { get; init; }
    [JsonPropertyName("isActive")] public bool IsActive { get; init; }
}

public sealed record LogisticsDriverEmployeeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("fullName")] public string FullName { get; init; } = string.Empty;
    [JsonPropertyName("jobTitle")] public string? JobTitle { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
    [JsonPropertyName("isActive")] public bool IsActive { get; init; }
}

public sealed record TripEligibleDeliveryOrderData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string? Number { get; init; }
    [JsonPropertyName("salesOrderId")] public string SalesOrderId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerAddressId")] public string CustomerAddressId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("destination")] public Dictionary<string, JsonElement> Destination { get; init; } = [];
    [JsonPropertyName("requestedDeliveryDate")] public string? RequestedDeliveryDate { get; init; }
    [JsonPropertyName("collectionPolicy")] public string CollectionPolicy { get; init; } = string.Empty;
    [JsonPropertyName("lineCount")] public int LineCount { get; init; }
    [JsonPropertyName("totalBaseQuantity")] public string TotalBaseQuantity { get; init; } = "0";
}

public sealed record TripAssignmentData
{
    [JsonPropertyName("assignmentId")] public string AssignmentId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderId")] public string DeliveryOrderId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("requestedDeliveryDate")] public string? RequestedDeliveryDate { get; init; }
    [JsonPropertyName("collectionPolicy")] public string CollectionPolicy { get; init; } = string.Empty;
}

public sealed record TripStopData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("sequence")] public int Sequence { get; init; }
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerAddressId")] public string CustomerAddressId { get; init; } = string.Empty;
    [JsonPropertyName("address")] public Dictionary<string, JsonElement> Address { get; init; } = [];
    [JsonPropertyName("plannedArrivalAt")] public string? PlannedArrivalAt { get; init; }
    [JsonPropertyName("assignments")] public TripAssignmentData[] Assignments { get; init; } = [];
}

public sealed record DeliveryTripData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string Number { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("deliveryRouteId")] public string? DeliveryRouteId { get; init; }
    [JsonPropertyName("routeCode")] public string? RouteCode { get; init; }
    [JsonPropertyName("routeName")] public string? RouteName { get; init; }
    [JsonPropertyName("vehicleId")] public string? VehicleId { get; init; }
    [JsonPropertyName("vehicleCode")] public string? VehicleCode { get; init; }
    [JsonPropertyName("licensePlate")] public string? LicensePlate { get; init; }
    [JsonPropertyName("primaryDriverId")] public string? PrimaryDriverId { get; init; }
    [JsonPropertyName("driverCode")] public string? DriverCode { get; init; }
    [JsonPropertyName("driverName")] public string? DriverName { get; init; }
    [JsonPropertyName("plannedStartAt")] public string? PlannedStartAt { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "draft";
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = "0";
    [JsonPropertyName("stopCount")] public int? StopCount { get; init; }
    [JsonPropertyName("assignmentCount")] public int? AssignmentCount { get; init; }
    [JsonPropertyName("stops")] public TripStopData[] Stops { get; init; } = [];
}

public sealed record TripMutationData
{
    [JsonPropertyName("ok")] public bool Ok { get; init; }
    [JsonPropertyName("replayed")] public bool Replayed { get; init; }
    [JsonPropertyName("assignmentCount")] public int? AssignmentCount { get; init; }
    [JsonPropertyName("trip")] public DeliveryTripData Trip { get; init; } = new();
}

public sealed record LogisticsRouteCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("defaultWarehouseId")] string DefaultWarehouseId);

public sealed record LogisticsVehicleCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("licensePlate")] string LicensePlate,
    [property: JsonPropertyName("vehicleType")] string VehicleType);

public sealed record LogisticsDriverCreateRequest(
    [property: JsonPropertyName("employeeId")] string EmployeeId,
    [property: JsonPropertyName("licenseReference")] string? LicenseReference);

public sealed record DeliveryTripCreateRequest
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryRouteId")] public string? DeliveryRouteId { get; init; }
    [JsonPropertyName("vehicleId")] public string? VehicleId { get; init; }
    [JsonPropertyName("primaryDriverId")] public string? PrimaryDriverId { get; init; }
    [JsonPropertyName("plannedStartAt")] public string? PlannedStartAt { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed record DeliveryTripUpdateRequest
{
    [JsonPropertyName("deliveryRouteId")] public string? DeliveryRouteId { get; init; }
    [JsonPropertyName("vehicleId")] public string? VehicleId { get; init; }
    [JsonPropertyName("primaryDriverId")] public string? PrimaryDriverId { get; init; }
    [JsonPropertyName("plannedStartAt")] public string? PlannedStartAt { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed record DeliveryTripAssignRequest(
    [property: JsonPropertyName("deliveryOrderIds")] string[] DeliveryOrderIds);

public sealed record DeliveryTripUnassignRequest(
    [property: JsonPropertyName("deliveryOrderId")] string DeliveryOrderId,
    [property: JsonPropertyName("reason")] string Reason);

public sealed record DeliveryTripReorderRequest(
    [property: JsonPropertyName("stopIds")] string[] StopIds);

public sealed record DeliveryTripReopenRequest(
    [property: JsonPropertyName("reason")] string Reason);

public sealed record DeliveryTripEmptyRequest;
