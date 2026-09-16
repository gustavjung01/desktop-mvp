using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record InventoryTransferLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sourceLocationId")] public string? SourceLocationId { get; init; }
    [JsonPropertyName("sourceVariantId")] public string SourceVariantId { get; init; } = string.Empty;
    [JsonPropertyName("sourceSku")] public string SourceSku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitId")] public string SourceUnitId { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitCode")] public string SourceUnitCode { get; init; } = string.Empty;
    [JsonPropertyName("sourceQuantity")] public string SourceQuantity { get; init; } = "0";
    [JsonPropertyName("conversionToBase")] public string ConversionToBase { get; init; } = "1";
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = "0";
    [JsonPropertyName("lotId")] public string? LotId { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed record InventoryTransferData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("documentNumber")] public string? DocumentNumber { get; init; }
    [JsonPropertyName("transferDate")] public string TransferDate { get; init; } = string.Empty;
    [JsonPropertyName("sourceWarehouseId")] public string SourceWarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("sourceWarehouseCode")] public string SourceWarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("sourceWarehouseName")] public string SourceWarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("destinationWarehouseId")] public string DestinationWarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("destinationWarehouseCode")] public string DestinationWarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("destinationWarehouseName")] public string DestinationWarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("inventoryMovementId")] public string? InventoryMovementId { get; init; }
    [JsonPropertyName("approvedAt")] public string? ApprovedAt { get; init; }
    [JsonPropertyName("approvedBy")] public string? ApprovedBy { get; init; }
    [JsonPropertyName("dispatchedAt")] public string? DispatchedAt { get; init; }
    [JsonPropertyName("dispatchedBy")] public string? DispatchedBy { get; init; }
    [JsonPropertyName("cancelledAt")] public string? CancelledAt { get; init; }
    [JsonPropertyName("cancelledBy")] public string? CancelledBy { get; init; }
    [JsonPropertyName("cancellationReason")] public string? CancellationReason { get; init; }
    [JsonPropertyName("lineCount")] public int LineCount { get; init; }
    [JsonPropertyName("baseQuantityTotal")] public string BaseQuantityTotal { get; init; } = "0";
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("createdBy")] public string CreatedBy { get; init; } = string.Empty;
    [JsonPropertyName("updatedBy")] public string UpdatedBy { get; init; } = string.Empty;
    [JsonPropertyName("lines")] public InventoryTransferLineData[] Lines { get; init; } = [];
}

public sealed record InventoryTransferInTransitData
{
    [JsonPropertyName("transferId")] public string TransferId { get; init; } = string.Empty;
    [JsonPropertyName("documentNumber")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("transferDate")] public string TransferDate { get; init; } = string.Empty;
    [JsonPropertyName("dispatchedAt")] public string DispatchedAt { get; init; } = string.Empty;
    [JsonPropertyName("sourceWarehouseId")] public string SourceWarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("sourceWarehouseCode")] public string SourceWarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("sourceWarehouseName")] public string SourceWarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("destinationWarehouseId")] public string DestinationWarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("destinationWarehouseCode")] public string DestinationWarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("destinationWarehouseName")] public string DestinationWarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("transferLineId")] public string TransferLineId { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sourceVariantId")] public string SourceVariantId { get; init; } = string.Empty;
    [JsonPropertyName("sourceSku")] public string SourceSku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitCode")] public string SourceUnitCode { get; init; } = string.Empty;
    [JsonPropertyName("sourceQuantity")] public string SourceQuantity { get; init; } = "0";
    [JsonPropertyName("dispatchedSourceQuantity")] public string? DispatchedSourceQuantity { get; init; }
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = "0";
    [JsonPropertyName("dispatchedBaseQuantity")] public string? DispatchedBaseQuantity { get; init; }
    [JsonPropertyName("acceptedBaseQuantity")] public string? AcceptedBaseQuantity { get; init; }
    [JsonPropertyName("damagedBaseQuantity")] public string? DamagedBaseQuantity { get; init; }
    [JsonPropertyName("shortBaseQuantity")] public string? ShortBaseQuantity { get; init; }
    [JsonPropertyName("overBaseQuantity")] public string? OverBaseQuantity { get; init; }
    [JsonPropertyName("lotId")] public string? LotId { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("inventoryMovementId")] public string InventoryMovementId { get; init; } = string.Empty;
}

public sealed record InventoryTransferReceiptLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("transferLineId")] public string TransferLineId { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("destinationLocationId")] public string? DestinationLocationId { get; init; }
    [JsonPropertyName("destinationLocationCode")] public string? DestinationLocationCode { get; init; }
    [JsonPropertyName("destinationLocationName")] public string? DestinationLocationName { get; init; }
    [JsonPropertyName("sourceSku")] public string SourceSku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitCode")] public string SourceUnitCode { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("acceptedQuantity")] public string AcceptedQuantity { get; init; } = "0";
    [JsonPropertyName("damagedQuantity")] public string DamagedQuantity { get; init; } = "0";
    [JsonPropertyName("overQuantity")] public string OverQuantity { get; init; } = "0";
    [JsonPropertyName("acceptedBaseQuantity")] public string AcceptedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("damagedBaseQuantity")] public string DamagedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("overBaseQuantity")] public string OverBaseQuantity { get; init; } = "0";
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed record InventoryTransferDamageApprovalData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("approvedAt")] public string ApprovedAt { get; init; } = string.Empty;
    [JsonPropertyName("approvedBy")] public string ApprovedBy { get; init; } = string.Empty;
}

public sealed record InventoryTransferReceiptReversalData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("inventoryMovementId")] public string? InventoryMovementId { get; init; }
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("reversedAt")] public string ReversedAt { get; init; } = string.Empty;
    [JsonPropertyName("reversedBy")] public string ReversedBy { get; init; } = string.Empty;
}

public sealed record InventoryTransferReceiptData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("transferId")] public string TransferId { get; init; } = string.Empty;
    [JsonPropertyName("receiptSequence")] public int ReceiptSequence { get; init; }
    [JsonPropertyName("receiptDate")] public string ReceiptDate { get; init; } = string.Empty;
    [JsonPropertyName("inventoryMovementId")] public string? InventoryMovementId { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("createdBy")] public string CreatedBy { get; init; } = string.Empty;
    [JsonPropertyName("damageApproval")] public InventoryTransferDamageApprovalData? DamageApproval { get; init; }
    [JsonPropertyName("reversal")] public InventoryTransferReceiptReversalData? Reversal { get; init; }
    [JsonPropertyName("lines")] public InventoryTransferReceiptLineData[] Lines { get; init; } = [];
}

public sealed record InventoryTransferResolutionLineData
{
    [JsonPropertyName("transferLineId")] public string TransferLineId { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sourceSku")] public string SourceSku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitCode")] public string SourceUnitCode { get; init; } = string.Empty;
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("dispatchedQuantity")] public string DispatchedQuantity { get; init; } = "0";
    [JsonPropertyName("acceptedQuantity")] public string AcceptedQuantity { get; init; } = "0";
    [JsonPropertyName("damagedQuantity")] public string DamagedQuantity { get; init; } = "0";
    [JsonPropertyName("overQuantity")] public string OverQuantity { get; init; } = "0";
    [JsonPropertyName("shortQuantity")] public string ShortQuantity { get; init; } = "0";
    [JsonPropertyName("remainingQuantity")] public string RemainingQuantity { get; init; } = "0";
    [JsonPropertyName("dispatchedBaseQuantity")] public string DispatchedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("acceptedBaseQuantity")] public string AcceptedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("damagedBaseQuantity")] public string DamagedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("overBaseQuantity")] public string OverBaseQuantity { get; init; } = "0";
    [JsonPropertyName("shortBaseQuantity")] public string ShortBaseQuantity { get; init; } = "0";
    [JsonPropertyName("remainingBaseQuantity")] public string RemainingBaseQuantity { get; init; } = "0";
}

public sealed record InventoryTransferShortClosureData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("closedAt")] public string ClosedAt { get; init; } = string.Empty;
    [JsonPropertyName("closedBy")] public string ClosedBy { get; init; } = string.Empty;
}

public sealed record InventoryTransferReceiptBundleData
{
    [JsonPropertyName("transfer")] public InventoryTransferData Transfer { get; init; } = new();
    [JsonPropertyName("receipts")] public InventoryTransferReceiptData[] Receipts { get; init; } = [];
    [JsonPropertyName("resolution")] public InventoryTransferResolutionLineData[] Resolution { get; init; } = [];
    [JsonPropertyName("shortClosure")] public InventoryTransferShortClosureData? ShortClosure { get; init; }
}

public sealed record InventoryTransferCreateLineRequest(
    [property: JsonPropertyName("sourceVariantId")] string SourceVariantId,
    [property: JsonPropertyName("sourceLocationId")] string? SourceLocationId,
    [property: JsonPropertyName("lotId")] string? LotId,
    [property: JsonPropertyName("sourceQuantity")] string SourceQuantity);

public sealed record InventoryTransferCreateRequest(
    [property: JsonPropertyName("transferDate")] string TransferDate,
    [property: JsonPropertyName("sourceWarehouseId")] string SourceWarehouseId,
    [property: JsonPropertyName("destinationWarehouseId")] string DestinationWarehouseId,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("lines")] InventoryTransferCreateLineRequest[] Lines);

public sealed record InventoryTransferTransitionRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision);

public sealed record InventoryTransferCancelRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("reason")] string Reason);

public sealed record InventoryTransferReceiptLineRequest(
    [property: JsonPropertyName("transferLineId")] string TransferLineId,
    [property: JsonPropertyName("destinationLocationId")] string? DestinationLocationId,
    [property: JsonPropertyName("acceptedQuantity")] string AcceptedQuantity,
    [property: JsonPropertyName("damagedQuantity")] string DamagedQuantity,
    [property: JsonPropertyName("overQuantity")] string OverQuantity,
    [property: JsonPropertyName("note")] string? Note);

public sealed record InventoryTransferReceiptCreateRequest(
    [property: JsonPropertyName("receiptDate")] string ReceiptDate,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("lines")] InventoryTransferReceiptLineRequest[] Lines);

public sealed record InventoryTransferDamageApprovalRequest(
    [property: JsonPropertyName("note")] string? Note);

public sealed record InventoryTransferCloseShortRequest(
    [property: JsonPropertyName("reason")] string Reason);

public sealed record InventoryTransferReverseReceiptRequest(
    [property: JsonPropertyName("documentDate")] string DocumentDate,
    [property: JsonPropertyName("reason")] string Reason);

public sealed record InventoryTransferReceiptMutationResultData
{
    [JsonPropertyName("transfer")] public InventoryTransferData Transfer { get; init; } = new();
    [JsonPropertyName("receipt")] public InventoryTransferReceiptData Receipt { get; init; } = new();
}

public sealed record InventoryTransferResolutionMutationResultData
{
    [JsonPropertyName("transfer")] public InventoryTransferData Transfer { get; init; } = new();
    [JsonPropertyName("shortClosure")] public InventoryTransferShortClosureData? ShortClosure { get; init; }
    [JsonPropertyName("resolution")] public InventoryTransferResolutionLineData[] Resolution { get; init; } = [];
}
