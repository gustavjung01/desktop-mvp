using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record TripReconciliationLineData
{
    [JsonPropertyName("assignmentId")] public string AssignmentId { get; init; } = string.Empty;
    [JsonPropertyName("stopSequence")] public int StopSequence { get; init; }
    [JsonPropertyName("deliveryOrderId")] public string DeliveryOrderId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("attemptId")] public string? AttemptId { get; init; }
    [JsonPropertyName("attemptResult")] public string? AttemptResult { get; init; }
    [JsonPropertyName("inventoryIssueLineId")] public string InventoryIssueLineId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("issuedBaseQuantity")] public string IssuedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("deliveredBaseQuantity")] public string DeliveredBaseQuantity { get; init; } = "0";
    [JsonPropertyName("returnedBaseQuantity")] public string ReturnedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("outstandingBaseQuantity")] public string OutstandingBaseQuantity { get; init; } = "0";
}

public sealed record TripReturnReceiptLineData
{
    [JsonPropertyName("inventoryIssueLineId")] public string InventoryIssueLineId { get; init; } = string.Empty;
    [JsonPropertyName("returnedBaseQuantity")] public string ReturnedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("sku")] public string? Sku { get; init; }
}

public sealed record TripReturnReceiptData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("inventoryMovementId")] public string InventoryMovementId { get; init; } = string.Empty;
    [JsonPropertyName("receivedAt")] public string ReceivedAt { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("lines")] public TripReturnReceiptLineData[] Lines { get; init; } = [];
}

public sealed record TripReconciliationData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string Number { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("licensePlate")] public string? LicensePlate { get; init; }
    [JsonPropertyName("driverName")] public string? DriverName { get; init; }
    [JsonPropertyName("dispatchedAt")] public string? DispatchedAt { get; init; }
    [JsonPropertyName("closedAt")] public string? ClosedAt { get; init; }
    [JsonPropertyName("closeNote")] public string? CloseNote { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = "0";
    [JsonPropertyName("canClose")] public bool CanClose { get; init; }
    [JsonPropertyName("lines")] public TripReconciliationLineData[] Lines { get; init; } = [];
    [JsonPropertyName("receipts")] public TripReturnReceiptData[] Receipts { get; init; } = [];
}

public sealed record TripReturnReceiptInputLine(
    [property: JsonPropertyName("inventoryIssueLineId")] string InventoryIssueLineId,
    [property: JsonPropertyName("returnedBaseQuantity")] string ReturnedBaseQuantity);

public sealed record TripReturnReceiptRequest(
    [property: JsonPropertyName("receivedAt")] string ReceivedAt,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("lines")] TripReturnReceiptInputLine[] Lines);

public sealed record TripCloseRequest(
    [property: JsonPropertyName("closedAt")] string ClosedAt,
    [property: JsonPropertyName("note")] string? Note);

public sealed record TripReconciliationMutationResult
{
    [JsonPropertyName("ok")] public bool Ok { get; init; }
    [JsonPropertyName("replayed")] public bool Replayed { get; init; }
    [JsonPropertyName("receiptId")] public string? ReceiptId { get; init; }
    [JsonPropertyName("trip")] public TripReconciliationData Trip { get; init; } = new();
}
