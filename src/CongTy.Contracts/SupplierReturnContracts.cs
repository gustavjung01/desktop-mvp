using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record SupplierReturnLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sourceGoodsReceiptId")] public string SourceGoodsReceiptId { get; init; } = string.Empty;
    [JsonPropertyName("sourceGoodsReceiptNumber")] public string SourceGoodsReceiptNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourceGoodsReceiptStatus")] public string SourceGoodsReceiptStatus { get; init; } = string.Empty;
    [JsonPropertyName("sourceGoodsReceiptLineId")] public string SourceGoodsReceiptLineId { get; init; } = string.Empty;
    [JsonPropertyName("sourceGoodsReceiptLineNumber")] public int SourceGoodsReceiptLineNumber { get; init; }
    [JsonPropertyName("sourcePurchaseOrderId")] public string SourcePurchaseOrderId { get; init; } = string.Empty;
    [JsonPropertyName("sourcePurchaseOrderNumber")] public string SourcePurchaseOrderNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourcePurchaseOrderLineId")] public string SourcePurchaseOrderLineId { get; init; } = string.Empty;
    [JsonPropertyName("sourcePurchaseOrderLineNumber")] public int SourcePurchaseOrderLineNumber { get; init; }
    [JsonPropertyName("sourceSupplierId")] public string SourceSupplierId { get; init; } = string.Empty;
    [JsonPropertyName("sourceSupplierCode")] public string SourceSupplierCode { get; init; } = string.Empty;
    [JsonPropertyName("sourceSupplierName")] public string SourceSupplierName { get; init; } = string.Empty;
    [JsonPropertyName("sourceWarehouseId")] public string SourceWarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("sourceWarehouseCode")] public string SourceWarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("sourceWarehouseName")] public string SourceWarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("sourceVariantId")] public string SourceVariantId { get; init; } = string.Empty;
    [JsonPropertyName("sourceSku")] public string SourceSku { get; init; } = string.Empty;
    [JsonPropertyName("sourceItemName")] public string SourceItemName { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitId")] public string SourceUnitId { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitCode")] public string SourceUnitCode { get; init; } = string.Empty;
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("conversionToBase")] public string ConversionToBase { get; init; } = "1";
    [JsonPropertyName("sourceAcceptedQuantity")] public string SourceAcceptedQuantity { get; init; } = "0";
    [JsonPropertyName("returnQuantity")] public string ReturnQuantity { get; init; } = "0";
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = "0";
    [JsonPropertyName("reasonCode")] public string ReasonCode { get; init; } = string.Empty;
    [JsonPropertyName("reasonNote")] public string ReasonNote { get; init; } = string.Empty;
    [JsonPropertyName("locationId")] public string? LocationId { get; init; }
    [JsonPropertyName("lotId")] public string? LotId { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("manufacturedDate")] public string? ManufacturedDate { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("supplierLotReference")] public string? SupplierLotReference { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("postedReturnQuantity")] public string? PostedReturnQuantity { get; init; }
    [JsonPropertyName("returnableQuantity")] public string? ReturnableQuantity { get; init; }
}

public sealed record SupplierReturnData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("supplierCode")] public string SupplierCode { get; init; } = string.Empty;
    [JsonPropertyName("supplierName")] public string SupplierName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = "draft";
    [JsonPropertyName("documentNumber")] public string? DocumentNumber { get; init; }
    [JsonPropertyName("returnDate")] public string ReturnDate { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("submittedAt")] public string? SubmittedAt { get; init; }
    [JsonPropertyName("submittedBy")] public string? SubmittedBy { get; init; }
    [JsonPropertyName("approvedAt")] public string? ApprovedAt { get; init; }
    [JsonPropertyName("approvedBy")] public string? ApprovedBy { get; init; }
    [JsonPropertyName("cancelledAt")] public string? CancelledAt { get; init; }
    [JsonPropertyName("cancelledBy")] public string? CancelledBy { get; init; }
    [JsonPropertyName("cancellationReason")] public string? CancellationReason { get; init; }
    [JsonPropertyName("postedAt")] public string? PostedAt { get; init; }
    [JsonPropertyName("postedBy")] public string? PostedBy { get; init; }
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
    [JsonPropertyName("reversedBy")] public string? ReversedBy { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("inventoryMovementId")] public string? InventoryMovementId { get; init; }
    [JsonPropertyName("inventoryReversalMovementId")] public string? InventoryReversalMovementId { get; init; }
    [JsonPropertyName("lineCount")] public int LineCount { get; init; }
    [JsonPropertyName("returnQuantityTotal")] public string ReturnQuantityTotal { get; init; } = "0";
    [JsonPropertyName("baseQuantityTotal")] public string BaseQuantityTotal { get; init; } = "0";
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("createdBy")] public string CreatedBy { get; init; } = string.Empty;
    [JsonPropertyName("updatedBy")] public string UpdatedBy { get; init; } = string.Empty;
    [JsonPropertyName("lines")] public SupplierReturnLineData[] Lines { get; init; } = [];
}

public sealed record SupplierReturnDraftLineRequest
{
    [JsonPropertyName("sourceGoodsReceiptLineId")] public string SourceGoodsReceiptLineId { get; init; } = string.Empty;
    [JsonPropertyName("returnQuantity")] public string ReturnQuantity { get; init; } = string.Empty;
    [JsonPropertyName("reasonCode")] public string ReasonCode { get; init; } = string.Empty;
    [JsonPropertyName("reasonNote")] public string ReasonNote { get; init; } = string.Empty;
    [JsonPropertyName("note"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Note { get; init; }
}

public sealed record SupplierReturnDraftRequest
{
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("returnDate")] public string ReturnDate { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string Note { get; init; } = string.Empty;
    [JsonPropertyName("lines")] public SupplierReturnDraftLineRequest[] Lines { get; init; } = [];
    [JsonPropertyName("expectedRevision"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExpectedRevision { get; init; }
}

public sealed record SupplierReturnExpectedRevisionRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision);

public sealed record SupplierReturnCancelRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("reason")] string Reason);

public sealed record SupplierReturnPostRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("documentDate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DocumentDate = null,
    [property: JsonPropertyName("reasonNote"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ReasonNote = null);

public sealed record SupplierReturnReverseRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("documentDate")] string DocumentDate,
    [property: JsonPropertyName("reasonNote")] string ReasonNote);
