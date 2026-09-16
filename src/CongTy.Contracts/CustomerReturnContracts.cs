using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record CustomerReturnEligibilityData
{
    [JsonPropertyName("issueLineId")] public string IssueLineId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderId")] public string DeliveryOrderId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("issuedBaseQuantity")] public string IssuedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("claimedReturnBaseQuantity")] public string ClaimedReturnBaseQuantity { get; init; } = "0";
    [JsonPropertyName("availableReturnBaseQuantity")] public string AvailableReturnBaseQuantity { get; init; } = "0";
}

public sealed record CustomerReturnLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("requestedBaseQuantity")] public string RequestedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("acceptedBaseQuantity")] public string AcceptedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("reasonCode")] public string ReasonCode { get; init; } = string.Empty;
    [JsonPropertyName("reasonNote")] public string ReasonNote { get; init; } = string.Empty;
}

public sealed record CustomerReturnData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string? Number { get; init; }
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = "draft";
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = "0";
    [JsonPropertyName("lineCount")] public int? LineCount { get; init; }
    [JsonPropertyName("requestedBaseQuantity")] public string? RequestedBaseQuantity { get; init; }
    [JsonPropertyName("acceptedBaseQuantity")] public string? AcceptedBaseQuantity { get; init; }
    [JsonPropertyName("inventoryMovementId")] public string? InventoryMovementId { get; init; }
    [JsonPropertyName("cancellationReason")] public string? CancellationReason { get; init; }
    [JsonPropertyName("lines")] public CustomerReturnLineData[] Lines { get; init; } = [];
}

public sealed record CustomerReturnCreateLineRequest(
    [property: JsonPropertyName("issueLineId")] string IssueLineId,
    [property: JsonPropertyName("quantity")] string Quantity,
    [property: JsonPropertyName("reasonCode")] string ReasonCode,
    [property: JsonPropertyName("reasonNote")] string ReasonNote);

public sealed record CustomerReturnCreateRequest(
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("lines")] CustomerReturnCreateLineRequest[] Lines);

public sealed record CustomerReturnReceiveLineRequest(
    [property: JsonPropertyName("customerReturnLineId")] string CustomerReturnLineId,
    [property: JsonPropertyName("acceptedQuantity")] string AcceptedQuantity);

public sealed record CustomerReturnReceiveRequest(
    [property: JsonPropertyName("documentDate")] string DocumentDate,
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("lines")] CustomerReturnReceiveLineRequest[] Lines);

public sealed record CustomerReturnCancelRequest(
    [property: JsonPropertyName("reason")] string Reason);

public sealed record CustomerReturnMutationResult
{
    [JsonPropertyName("ok")] public bool Ok { get; init; }
    [JsonPropertyName("replayed")] public bool Replayed { get; init; }
    [JsonPropertyName("customerReturn")] public CustomerReturnData CustomerReturn { get; init; } = new();
}
