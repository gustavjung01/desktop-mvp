using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record TripDispatchAssignmentData
{
    [JsonPropertyName("assignmentId")] public string AssignmentId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderId")] public string DeliveryOrderId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
}

public sealed record TripDispatchStopData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("sequence")] public int Sequence { get; init; }
    [JsonPropertyName("assignments")] public TripDispatchAssignmentData[] Assignments { get; init; } = [];
}

public sealed record TripDispatchItemData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderId")] public string DeliveryOrderId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("inventoryIssueId")] public string InventoryIssueId { get; init; } = string.Empty;
    [JsonPropertyName("inventoryMovementId")] public string InventoryMovementId { get; init; } = string.Empty;
    [JsonPropertyName("movementType")] public string? MovementType { get; init; }
    [JsonPropertyName("postedAt")] public string? PostedAt { get; init; }
}

public sealed record TripDispatchData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string Number { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("vehicleCode")] public string? VehicleCode { get; init; }
    [JsonPropertyName("licensePlate")] public string? LicensePlate { get; init; }
    [JsonPropertyName("driverCode")] public string? DriverCode { get; init; }
    [JsonPropertyName("driverName")] public string? DriverName { get; init; }
    [JsonPropertyName("plannedStartAt")] public string? PlannedStartAt { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("revision")] public string Revision { get; init; } = "0";
    [JsonPropertyName("dispatchId")] public string? DispatchId { get; init; }
    [JsonPropertyName("handoverReceiverName")] public string? HandoverReceiverName { get; init; }
    [JsonPropertyName("handoverNote")] public string? HandoverNote { get; init; }
    [JsonPropertyName("dispatchedAt")] public string? DispatchedAt { get; init; }
    [JsonPropertyName("dispatchedBy")] public string? DispatchedBy { get; init; }
    [JsonPropertyName("stops")] public TripDispatchStopData[] Stops { get; init; } = [];
    [JsonPropertyName("dispatchItems")] public TripDispatchItemData[] DispatchItems { get; init; } = [];
}

public sealed record TripDispatchRequest(
    [property: JsonPropertyName("dispatchedAt")] string DispatchedAt,
    [property: JsonPropertyName("handoverReceiverName")] string HandoverReceiverName,
    [property: JsonPropertyName("handoverNote")] string? HandoverNote);

public sealed record TripDispatchResultData
{
    [JsonPropertyName("ok")] public bool Ok { get; init; }
    [JsonPropertyName("replayed")] public bool Replayed { get; init; }
    [JsonPropertyName("trip")] public TripDispatchData Trip { get; init; } = new();
}
