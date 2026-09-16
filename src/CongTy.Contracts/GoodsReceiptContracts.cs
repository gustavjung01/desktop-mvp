using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record GoodsReceiptTrackingPolicyData
{
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("lotTrackingMode")] public string LotTrackingMode { get; init; } = "NONE";
    [JsonPropertyName("expiryTrackingMode")] public string ExpiryTrackingMode { get; init; } = "NONE";
    [JsonPropertyName("locationRequired")] public bool LocationRequired { get; init; }
}

public sealed record GoodsReceiptTrackingRequirementData
{
    [JsonPropertyName("purchaseOrderLineId")] public string PurchaseOrderLineId { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sourceVariantId")] public string SourceVariantId { get; init; } = string.Empty;
    [JsonPropertyName("skuCode")] public string SkuCode { get; init; } = string.Empty;
    [JsonPropertyName("trackingPolicy")] public GoodsReceiptTrackingPolicyData? TrackingPolicy { get; init; }
}

public sealed record GoodsReceiptLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("purchaseOrderLineId")] public string PurchaseOrderLineId { get; init; } = string.Empty;
    [JsonPropertyName("purchaseOrderLineNumber")] public int PurchaseOrderLineNumber { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("skuCode")] public string SkuCode { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitId")] public string UnitId { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("conversionToBase")] public string ConversionToBase { get; init; } = "1";
    [JsonPropertyName("orderedQuantity")] public string OrderedQuantity { get; init; } = "0";
    [JsonPropertyName("receivedQuantityBefore")] public string ReceivedQuantityBefore { get; init; } = "0";
    [JsonPropertyName("remainingQuantityBefore")] public string RemainingQuantityBefore { get; init; } = "0";
    [JsonPropertyName("receivedQuantity")] public string ReceivedQuantity { get; init; } = "0";
    [JsonPropertyName("acceptedQuantity")] public string AcceptedQuantity { get; init; } = "0";
    [JsonPropertyName("rejectedQuantity")] public string RejectedQuantity { get; init; } = "0";
    [JsonPropertyName("shortageClosedQuantity")] public string ShortageClosedQuantity { get; init; } = "0";
    [JsonPropertyName("finalizeLine")] public bool FinalizeLine { get; init; }
    [JsonPropertyName("qualityReasonCode")] public string? QualityReasonCode { get; init; }
    [JsonPropertyName("qualityNote")] public string? QualityNote { get; init; }
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = "0";
    [JsonPropertyName("remainingQuantityAfter")] public string RemainingQuantityAfter { get; init; } = "0";
    [JsonPropertyName("locationId")] public string? LocationId { get; init; }
    [JsonPropertyName("lotId")] public string? LotId { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("manufacturedDate")] public string? ManufacturedDate { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("supplierLotReference")] public string? SupplierLotReference { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("trackingPolicy")] public GoodsReceiptTrackingPolicyData? TrackingPolicy { get; init; }
}

public sealed record GoodsReceiptData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("purchaseOrderId")] public string PurchaseOrderId { get; init; } = string.Empty;
    [JsonPropertyName("purchaseOrderNumber")] public string? PurchaseOrderNumber { get; init; }
    [JsonPropertyName("purchaseOrderStatus")] public string PurchaseOrderStatus { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = "draft";
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("supplierCode")] public string? SupplierCode { get; init; }
    [JsonPropertyName("supplierName")] public string SupplierName { get; init; } = string.Empty;
    [JsonPropertyName("documentNumber")] public string? DocumentNumber { get; init; }
    [JsonPropertyName("receiptDate")] public string ReceiptDate { get; init; } = string.Empty;
    [JsonPropertyName("supplierDeliveryReference")] public string? SupplierDeliveryReference { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("postedAt")] public string? PostedAt { get; init; }
    [JsonPropertyName("postedBy")] public string? PostedBy { get; init; }
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
    [JsonPropertyName("reversedBy")] public string? ReversedBy { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("inventoryMovementId")] public string? InventoryMovementId { get; init; }
    [JsonPropertyName("inventoryReversalMovementId")] public string? InventoryReversalMovementId { get; init; }
    [JsonPropertyName("lineCount")] public int LineCount { get; init; }
    [JsonPropertyName("receivedQuantityTotal")] public string ReceivedQuantityTotal { get; init; } = "0";
    [JsonPropertyName("acceptedQuantityTotal")] public string AcceptedQuantityTotal { get; init; } = "0";
    [JsonPropertyName("rejectedQuantityTotal")] public string RejectedQuantityTotal { get; init; } = "0";
    [JsonPropertyName("shortageClosedQuantityTotal")] public string ShortageClosedQuantityTotal { get; init; } = "0";
    [JsonPropertyName("baseQuantityTotal")] public string BaseQuantityTotal { get; init; } = "0";
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("createdBy")] public string CreatedBy { get; init; } = string.Empty;
    [JsonPropertyName("updatedBy")] public string UpdatedBy { get; init; } = string.Empty;
    [JsonPropertyName("lines")] public GoodsReceiptLineData[] Lines { get; init; } = [];
}

public sealed record GoodsReceiptDraftLineRequest
{
    [JsonPropertyName("purchaseOrderLineId")] public string PurchaseOrderLineId { get; init; } = string.Empty;
    [JsonPropertyName("receivedQuantity")] public string ReceivedQuantity { get; init; } = "0";
    [JsonPropertyName("acceptedQuantity")] public string AcceptedQuantity { get; init; } = "0";
    [JsonPropertyName("rejectedQuantity")] public string RejectedQuantity { get; init; } = "0";
    [JsonPropertyName("finalizeLine")] public bool FinalizeLine { get; init; }
    [JsonPropertyName("qualityReasonCode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? QualityReasonCode { get; init; }
    [JsonPropertyName("qualityNote"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? QualityNote { get; init; }
    [JsonPropertyName("locationId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? LocationId { get; init; }
    [JsonPropertyName("lotId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? LotId { get; init; }
    [JsonPropertyName("lotCode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? LotCode { get; init; }
    [JsonPropertyName("manufacturedDate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ManufacturedDate { get; init; }
    [JsonPropertyName("expiryDate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExpiryDate { get; init; }
    [JsonPropertyName("supplierLotReference"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? SupplierLotReference { get; init; }
    [JsonPropertyName("note"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Note { get; init; }
}

public sealed record GoodsReceiptDraftRequest
{
    [JsonPropertyName("purchaseOrderId")] public string PurchaseOrderId { get; init; } = string.Empty;
    [JsonPropertyName("receiptDate")] public string ReceiptDate { get; init; } = string.Empty;
    [JsonPropertyName("supplierDeliveryReference"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? SupplierDeliveryReference { get; init; }
    [JsonPropertyName("note"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Note { get; init; }
    [JsonPropertyName("lines")] public GoodsReceiptDraftLineRequest[] Lines { get; init; } = [];
    [JsonPropertyName("expectedRevision"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExpectedRevision { get; init; }
}

public sealed record GoodsReceiptPostRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision);

public sealed record GoodsReceiptReverseRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("documentDate")] string DocumentDate,
    [property: JsonPropertyName("reasonNote")] string ReasonNote,
    [property: JsonPropertyName("reasonCode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ReasonCode = null);
