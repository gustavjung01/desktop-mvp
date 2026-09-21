using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record InventoryAdjustmentReasonData
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("documentKind")] public string DocumentKind { get; init; } = string.Empty;
    [JsonPropertyName("adjustmentDirection")] public string? AdjustmentDirection { get; init; }
    [JsonPropertyName("label")] public string Label { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; init; } = string.Empty;
}

public sealed record InventoryAdjustmentLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("sourceLocationId")] public string? SourceLocationId { get; init; }
    [JsonPropertyName("sourceLocationCode")] public string? SourceLocationCode { get; init; }
    [JsonPropertyName("sourceLocationName")] public string? SourceLocationName { get; init; }
    [JsonPropertyName("sourceLocationType")] public string? SourceLocationType { get; init; }
    [JsonPropertyName("destinationLocationId")] public string? DestinationLocationId { get; init; }
    [JsonPropertyName("destinationLocationCode")] public string? DestinationLocationCode { get; init; }
    [JsonPropertyName("destinationLocationName")] public string? DestinationLocationName { get; init; }
    [JsonPropertyName("destinationLocationType")] public string? DestinationLocationType { get; init; }
    [JsonPropertyName("sourceVariantId")] public string SourceVariantId { get; init; } = string.Empty;
    [JsonPropertyName("sourceSku")] public string SourceSku { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitId")] public string SourceUnitId { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitCode")] public string SourceUnitCode { get; init; } = string.Empty;
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "0";
    [JsonPropertyName("conversionToBase")] public string ConversionToBase { get; init; } = "1";
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = "0";
    [JsonPropertyName("lotId")] public string? LotId { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("sourceSnapshotScopeVersion")] public string SourceSnapshotScopeVersion { get; init; } = string.Empty;
    [JsonPropertyName("destinationSnapshotScopeVersion")] public string? DestinationSnapshotScopeVersion { get; init; }
    [JsonPropertyName("productName")] public string? ProductName { get; init; }
    [JsonPropertyName("systemBaseQuantity")] public string? SystemBaseQuantity { get; init; }
    [JsonPropertyName("countedBaseQuantity")] public string? CountedBaseQuantity { get; init; }
}

public sealed record InventoryAdjustmentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("adjustmentNumber")] public string AdjustmentNumber { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("documentKind")] public string DocumentKind { get; init; } = string.Empty;
    [JsonPropertyName("adjustmentDirection")] public string? AdjustmentDirection { get; init; }
    [JsonPropertyName("reasonCode")] public string ReasonCode { get; init; } = string.Empty;
    [JsonPropertyName("reasonLabel")] public string? ReasonLabel { get; init; }
    [JsonPropertyName("reasonNote")] public string ReasonNote { get; init; } = string.Empty;
    [JsonPropertyName("reconciliationBatchCode")] public string? ReconciliationBatchCode { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("correctionOfAdjustmentId")] public string? CorrectionOfAdjustmentId { get; init; }
    [JsonPropertyName("inventoryMovementId")] public string? InventoryMovementId { get; init; }
    [JsonPropertyName("reversalMovementId")] public string? ReversalMovementId { get; init; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("createdBy")] public string CreatedBy { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string? UpdatedAt { get; init; }
    [JsonPropertyName("updatedBy")] public string? UpdatedBy { get; init; }
    [JsonPropertyName("submittedAt")] public string? SubmittedAt { get; init; }
    [JsonPropertyName("submittedBy")] public string? SubmittedBy { get; init; }
    [JsonPropertyName("approvedAt")] public string? ApprovedAt { get; init; }
    [JsonPropertyName("approvedBy")] public string? ApprovedBy { get; init; }
    [JsonPropertyName("postedAt")] public string? PostedAt { get; init; }
    [JsonPropertyName("postedBy")] public string? PostedBy { get; init; }
    [JsonPropertyName("cancelledAt")] public string? CancelledAt { get; init; }
    [JsonPropertyName("cancelledBy")] public string? CancelledBy { get; init; }
    [JsonPropertyName("cancelReason")] public string? CancelReason { get; init; }
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
    [JsonPropertyName("reversedBy")] public string? ReversedBy { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("lineCount")] public int LineCount { get; init; }
    [JsonPropertyName("lines")] public InventoryAdjustmentLineData[] Lines { get; init; } = [];
}

public sealed record InventoryAdjustmentCreateLineRequest(
    [property: JsonPropertyName("sourceLocationId")] string? SourceLocationId,
    [property: JsonPropertyName("sourceVariantId")] string SourceVariantId,
    [property: JsonPropertyName("lotId")] string? LotId,
    [property: JsonPropertyName("quantity")] string Quantity,
    [property: JsonPropertyName("destinationLocationId")] string? DestinationLocationId);

public sealed record InventoryAdjustmentCreateRequest(
    [property: JsonPropertyName("warehouseId")] string WarehouseId,
    [property: JsonPropertyName("documentKind")] string DocumentKind,
    [property: JsonPropertyName("adjustmentDirection")] string? AdjustmentDirection,
    [property: JsonPropertyName("reasonCode")] string ReasonCode,
    [property: JsonPropertyName("reasonNote")] string ReasonNote,
    [property: JsonPropertyName("lines")] InventoryAdjustmentCreateLineRequest[] Lines);

public sealed record InventoryAdjustmentTransitionRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision);

public sealed record InventoryAdjustmentReasonRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("reason")] string Reason);

public sealed record BulkInventoryAdjustmentInputRow(
    [property: JsonPropertyName("lineNumber")] int LineNumber,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("actualQuantity")] string ActualQuantity,
    [property: JsonPropertyName("locationCode")] string LocationCode,
    [property: JsonPropertyName("lotCode")] string LotCode);

public sealed record BulkInventoryAdjustmentPreviewRequest(
    [property: JsonPropertyName("warehouseId")] string WarehouseId,
    [property: JsonPropertyName("rows")] BulkInventoryAdjustmentInputRow[] Rows);

public sealed record BulkInventoryAdjustmentScopeOption
{
    [JsonPropertyName("locationCode")] public string LocationCode { get; init; } = string.Empty;
    [JsonPropertyName("locationName")] public string? LocationName { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
}

public sealed record BulkInventoryAdjustmentPreviewError
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
}

public sealed record BulkInventoryAdjustmentPreviewRow
{
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("productCode")] public string? ProductCode { get; init; }
    [JsonPropertyName("productName")] public string? ProductName { get; init; }
    [JsonPropertyName("enteredQuantity")] public string EnteredQuantity { get; init; } = string.Empty;
    [JsonPropertyName("enteredUnitCode")] public string? EnteredUnitCode { get; init; }
    [JsonPropertyName("actualBaseQuantity")] public string? ActualBaseQuantity { get; init; }
    [JsonPropertyName("baseUnitCode")] public string? BaseUnitCode { get; init; }
    [JsonPropertyName("currentBaseQuantity")] public string? CurrentBaseQuantity { get; init; }
    [JsonPropertyName("deltaBaseQuantity")] public string? DeltaBaseQuantity { get; init; }
    [JsonPropertyName("signedDeltaBaseQuantity")] public string? SignedDeltaBaseQuantity { get; init; }
    [JsonPropertyName("direction")] public string Direction { get; init; } = "NONE";
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("locationName")] public string? LocationName { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("lotTrackingMode")] public string LotTrackingMode { get; init; } = "NONE";
    [JsonPropertyName("lotRequired")] public bool LotRequired { get; init; }
    [JsonPropertyName("scopeRequired")] public bool ScopeRequired { get; init; }
    [JsonPropertyName("requiresLocationSelection")] public bool RequiresLocationSelection { get; init; }
    [JsonPropertyName("requiresLotSelection")] public bool RequiresLotSelection { get; init; }
    [JsonPropertyName("locationAutoFilled")] public bool LocationAutoFilled { get; init; }
    [JsonPropertyName("lotAutoFilled")] public bool LotAutoFilled { get; init; }
    [JsonPropertyName("scopeOptions")] public BulkInventoryAdjustmentScopeOption[] ScopeOptions { get; init; } = [];
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("errors")] public BulkInventoryAdjustmentPreviewError[] Errors { get; init; } = [];
}

public sealed record BulkInventoryAdjustmentPreviewTotals
{
    [JsonPropertyName("inputRowCount")] public int InputRowCount { get; init; }
    [JsonPropertyName("readyRowCount")] public int ReadyRowCount { get; init; }
    [JsonPropertyName("attentionRowCount")] public int AttentionRowCount { get; init; }
    [JsonPropertyName("increaseRowCount")] public int IncreaseRowCount { get; init; }
    [JsonPropertyName("decreaseRowCount")] public int DecreaseRowCount { get; init; }
    [JsonPropertyName("unchangedRowCount")] public int UnchangedRowCount { get; init; }
}

public sealed record BulkInventoryAdjustmentPreviewData
{
    [JsonPropertyName("ready")] public bool Ready { get; init; }
    [JsonPropertyName("stockUnchanged")] public bool StockUnchanged { get; init; }
    [JsonPropertyName("rows")] public BulkInventoryAdjustmentPreviewRow[] Rows { get; init; } = [];
    [JsonPropertyName("rowErrors")] public BulkInventoryAdjustmentPreviewError[] RowErrors { get; init; } = [];
    [JsonPropertyName("totals")] public BulkInventoryAdjustmentPreviewTotals Totals { get; init; } = new();
}

public sealed record BulkInventoryAdjustmentConfirmRequest(
    [property: JsonPropertyName("warehouseId")] string WarehouseId,
    [property: JsonPropertyName("rows")] BulkInventoryAdjustmentInputRow[] Rows,
    [property: JsonPropertyName("increaseReasonCode")] string? IncreaseReasonCode,
    [property: JsonPropertyName("decreaseReasonCode")] string? DecreaseReasonCode,
    [property: JsonPropertyName("reasonNote")] string ReasonNote);

public sealed record BulkInventoryAdjustmentConfirmData
{
    [JsonPropertyName("adjustments")] public InventoryAdjustmentData[] Adjustments { get; init; } = [];
    [JsonPropertyName("reconciliationBatchCode")] public string? ReconciliationBatchCode { get; init; }
    [JsonPropertyName("preview")] public BulkInventoryAdjustmentPreviewData Preview { get; init; } = new();
}
