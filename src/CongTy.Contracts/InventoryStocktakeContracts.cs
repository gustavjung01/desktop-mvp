using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record InventoryStocktakeRoundData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("roundNumber")] public int RoundNumber { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("createdBy")] public string CreatedBy { get; init; } = string.Empty;
    [JsonPropertyName("countedAt")] public string? CountedAt { get; init; }
    [JsonPropertyName("countedBy")] public string? CountedBy { get; init; }
    [JsonPropertyName("submittedAt")] public string? SubmittedAt { get; init; }
    [JsonPropertyName("submittedBy")] public string? SubmittedBy { get; init; }
    [JsonPropertyName("approvedAt")] public string? ApprovedAt { get; init; }
    [JsonPropertyName("approvedBy")] public string? ApprovedBy { get; init; }
}

public sealed record InventoryStocktakeLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("roundNumber")] public int RoundNumber { get; init; }
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("locationId")] public string? LocationId { get; init; }
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("locationName")] public string? LocationName { get; init; }
    [JsonPropertyName("sourceVariantId")] public string SourceVariantId { get; init; } = string.Empty;
    [JsonPropertyName("sourceSku")] public string SourceSku { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitId")] public string SourceUnitId { get; init; } = string.Empty;
    [JsonPropertyName("sourceUnitCode")] public string SourceUnitCode { get; init; } = string.Empty;
    [JsonPropertyName("conversionToBase")] public string ConversionToBase { get; init; } = "1";
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("lotId")] public string? LotId { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("expectedBaseQuantity")] public string? ExpectedBaseQuantity { get; init; }
    [JsonPropertyName("countedBaseQuantity")] public string? CountedBaseQuantity { get; init; }
    [JsonPropertyName("countStatus")] public string? CountStatus { get; init; }
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("finalDelta")] public string? FinalDelta { get; init; }
    [JsonPropertyName("snapshotScopeVersion")] public string? SnapshotScopeVersion { get; init; }
    [JsonPropertyName("postedScopeVersion")] public string? PostedScopeVersion { get; init; }
    [JsonPropertyName("countedAt")] public string? CountedAt { get; init; }
    [JsonPropertyName("countedBy")] public string? CountedBy { get; init; }
}

public sealed record InventoryStocktakeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("stocktakeNumber")] public string StocktakeNumber { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("currentRound")] public int CurrentRound { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("inventoryMovementId")] public string? InventoryMovementId { get; init; }
    [JsonPropertyName("reversalMovementId")] public string? ReversalMovementId { get; init; }
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
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("createdBy")] public string CreatedBy { get; init; } = string.Empty;
    [JsonPropertyName("currentCountedAt")] public string? CurrentCountedAt { get; init; }
    [JsonPropertyName("currentCountedBy")] public string? CurrentCountedBy { get; init; }
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updatedBy")] public string UpdatedBy { get; init; } = string.Empty;
    [JsonPropertyName("lineCount")] public int LineCount { get; init; }
    [JsonPropertyName("rounds")] public InventoryStocktakeRoundData[] Rounds { get; init; } = [];
    [JsonPropertyName("lines")] public InventoryStocktakeLineData[] Lines { get; init; } = [];
}

public sealed record InventoryStocktakeLotSelectionRequest(
    [property: JsonPropertyName("baseVariantId")] string BaseVariantId,
    [property: JsonPropertyName("lotId")] string? LotId);

public sealed record InventoryStocktakeCreateRequest(
    [property: JsonPropertyName("warehouseId")] string WarehouseId,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("scopeMode")] string ScopeMode,
    [property: JsonPropertyName("lotSelections")] InventoryStocktakeLotSelectionRequest[]? LotSelections = null,
    [property: JsonPropertyName("locationIds")] string?[]? LocationIds = null);

public sealed record InventoryStocktakeCountLineRequest(
    [property: JsonPropertyName("lineId")] string LineId,
    [property: JsonPropertyName("countedBaseQuantity")] string CountedBaseQuantity,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("note")] string? Note);

public sealed record InventoryStocktakeCountRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("counts")] InventoryStocktakeCountLineRequest[] Counts);

public sealed record InventoryStocktakeAnnotationLineRequest(
    [property: JsonPropertyName("lineId")] string LineId,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("note")] string? Note);

public sealed record InventoryStocktakeAnnotateRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("annotations")] InventoryStocktakeAnnotationLineRequest[] Annotations);

public sealed record InventoryStocktakeCopyRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision);

public sealed record InventoryStocktakeTransitionRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision);

public sealed record InventoryStocktakeReasonRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("reason")] string Reason);
