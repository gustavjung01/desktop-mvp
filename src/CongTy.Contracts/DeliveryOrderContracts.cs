using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record DeliveryOrderEligibilityData
{
    [JsonPropertyName("fulfillmentAllocationId")] public string FulfillmentAllocationId { get; init; } = string.Empty;
    [JsonPropertyName("fulfillmentDemandId")] public string FulfillmentDemandId { get; init; } = string.Empty;
    [JsonPropertyName("salesOrderId")] public string SalesOrderId { get; init; } = string.Empty;
    [JsonPropertyName("salesOrderNumber")] public string? SalesOrderNumber { get; init; }
    [JsonPropertyName("salesOrderVersionId")] public string SalesOrderVersionId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("handoverMode")] public string HandoverMode { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("requestedDeliveryDate")] public string? RequestedDeliveryDate { get; init; }
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("packedBaseQuantity")] public string PackedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("claimedBaseQuantity")] public string ClaimedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("availableForDeliveryOrderBaseQuantity")] public string AvailableForDeliveryOrderBaseQuantity { get; init; } = "0";
    [JsonPropertyName("backorderedBaseQuantity")] public string BackorderedBaseQuantity { get; init; } = "0";
}

public sealed record DeliveryOrderLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("fulfillmentAllocationId")] public string FulfillmentAllocationId { get; init; } = string.Empty;
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("deliveryBaseQuantity")] public string DeliveryBaseQuantity { get; init; } = "0";
}

public sealed record DeliveryOrderData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string? Number { get; init; }
    [JsonPropertyName("salesOrderId")] public string SalesOrderId { get; init; } = string.Empty;
    [JsonPropertyName("salesOrderNumber")] public string? SalesOrderNumber { get; init; }
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("handoverMode")] public string HandoverMode { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("revision")] public string Revision { get; init; } = "0";
    [JsonPropertyName("lineCount")] public int? LineCount { get; init; }
    [JsonPropertyName("totalBaseQuantity")] public string? TotalBaseQuantity { get; init; }
    [JsonPropertyName("requestedDeliveryDate")] public string? RequestedDeliveryDate { get; init; }
    [JsonPropertyName("collectionPolicy")] public string? CollectionPolicy { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("cancellationReason")] public string? CancellationReason { get; init; }
    [JsonPropertyName("lines")] public DeliveryOrderLineData[] Lines { get; init; } = [];
}

public sealed record DeliveryOrderCreateLineRequest(
    [property: JsonPropertyName("fulfillmentAllocationId")] string FulfillmentAllocationId,
    [property: JsonPropertyName("quantity")] string Quantity);

public sealed record DeliveryOrderCreateRequest
{
    [JsonPropertyName("lines")] public DeliveryOrderCreateLineRequest[] Lines { get; init; } = [];
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed record DeliveryOrderCreateResultData
{
    [JsonPropertyName("replayed")] public bool Replayed { get; init; }
    [JsonPropertyName("deliveryOrder")] public DeliveryOrderData DeliveryOrder { get; init; } = new();
}

public sealed record DeliveryOrderCancelRequest(
    [property: JsonPropertyName("reason")] string Reason);

public sealed record DeliveryOrderHandoverRequest(
    [property: JsonPropertyName("receiverName")] string ReceiverName,
    [property: JsonPropertyName("receiverNote")] string? ReceiverNote,
    [property: JsonPropertyName("handedOverAt")] string HandedOverAt);

public sealed record DeliveryOrderReverseIssueRequest(
    [property: JsonPropertyName("documentDate")] string DocumentDate,
    [property: JsonPropertyName("reasonCode")] string ReasonCode,
    [property: JsonPropertyName("reasonNote")] string ReasonNote);
