using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record InventoryCostingRunData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("methodVersion")] public string MethodVersion { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("warehouseIds")] public string[] WarehouseIds { get; init; } = [];
    [JsonPropertyName("ledgerLineCount")] public int LedgerLineCount { get; init; }
    [JsonPropertyName("factCount")] public int FactCount { get; init; }
    [JsonPropertyName("anomalyCount")] public int AnomalyCount { get; init; }
    [JsonPropertyName("startedAt")] public string StartedAt { get; init; } = string.Empty;
    [JsonPropertyName("completedAt")] public string CompletedAt { get; init; } = string.Empty;
}

public sealed record InventoryCostBalanceData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string? BaseSku { get; init; }
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "0";
    [JsonPropertyName("inventoryValue")] public string? InventoryValue { get; init; }
    [JsonPropertyName("averageUnitCost")] public string? AverageUnitCost { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("anomalyCount")] public int AnomalyCount { get; init; }
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record InventoryCostFactData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("eventType")] public string EventType { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string? BaseSku { get; init; }
    [JsonPropertyName("direction")] public string Direction { get; init; } = string.Empty;
    [JsonPropertyName("quantityDelta")] public string QuantityDelta { get; init; } = "0";
    [JsonPropertyName("unitCost")] public string? UnitCost { get; init; }
    [JsonPropertyName("valueDelta")] public string? ValueDelta { get; init; }
    [JsonPropertyName("sourceCostType")] public string SourceCostType { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentType")] public string? SourceDocumentType { get; init; }
    [JsonPropertyName("sourceDocumentNumber")] public string? SourceDocumentNumber { get; init; }
    [JsonPropertyName("sourceLineReference")] public string? SourceLineReference { get; init; }
    [JsonPropertyName("movementPostedAt")] public string MovementPostedAt { get; init; } = string.Empty;
}

public sealed record InventoryCostAnomalyData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string? BaseSku { get; init; }
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
}

public sealed record InventoryCostReconciliationData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string? BaseSku { get; init; }
    [JsonPropertyName("ledgerQuantity")] public string LedgerQuantity { get; init; } = "0";
    [JsonPropertyName("costingQuantity")] public string CostingQuantity { get; init; } = "0";
    [JsonPropertyName("quantityDifference")] public string QuantityDifference { get; init; } = "0";
    [JsonPropertyName("reconciliationStatus")] public string ReconciliationStatus { get; init; } = string.Empty;
}

public sealed record InventoryCostingPeriodData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("periodStart")] public string PeriodStart { get; init; } = string.Empty;
    [JsonPropertyName("periodEnd")] public string PeriodEnd { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("openedAt")] public string OpenedAt { get; init; } = string.Empty;
    [JsonPropertyName("openedBy")] public string OpenedBy { get; init; } = string.Empty;
    [JsonPropertyName("closedAt")] public string? ClosedAt { get; init; }
    [JsonPropertyName("closedBy")] public string? ClosedBy { get; init; }
    [JsonPropertyName("snapshotPoolCount")] public int SnapshotPoolCount { get; init; }
    [JsonPropertyName("snapshotAnomalyPoolCount")] public int SnapshotAnomalyPoolCount { get; init; }
}

public sealed record InventoryCostAdjustmentEventData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("eventType")] public string EventType { get; init; } = string.Empty;
    [JsonPropertyName("effectiveDate")] public string EffectiveDate { get; init; } = string.Empty;
    [JsonPropertyName("postingDate")] public string PostingDate { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string? BaseSku { get; init; }
    [JsonPropertyName("quantityDelta")] public string QuantityDelta { get; init; } = "0";
    [JsonPropertyName("valueDelta")] public string ValueDelta { get; init; } = "0";
    [JsonPropertyName("sourceDocumentType")] public string SourceDocumentType { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentId")] public string SourceDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("sourceLineReference")] public string? SourceLineReference { get; init; }
}

public sealed record InventoryCostDiscrepancyData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string? BaseSku { get; init; }
    [JsonPropertyName("inventoryMovementLineId")] public string? InventoryMovementLineId { get; init; }
    [JsonPropertyName("costAdjustmentEventId")] public string? CostAdjustmentEventId { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
    [JsonPropertyName("firstSeenAt")] public string FirstSeenAt { get; init; } = string.Empty;
    [JsonPropertyName("lastSeenAt")] public string LastSeenAt { get; init; } = string.Empty;
    [JsonPropertyName("resolvedAt")] public string? ResolvedAt { get; init; }
}

public sealed record InventoryCostRebuildResultData
{
    [JsonPropertyName("run")] public InventoryCostingRunData Run { get; init; } = new();
    [JsonPropertyName("anomalyCount")] public int AnomalyCount { get; init; }
    [JsonPropertyName("reconciliationMismatchCount")] public int? ReconciliationMismatchCount { get; init; }
    [JsonPropertyName("discrepancyCount")] public int? DiscrepancyCount { get; init; }
    [JsonPropertyName("replayed")] public bool Replayed { get; init; }
}

public sealed record InventoryCostPeriodMutationRequest(
    [property: JsonPropertyName("periodStart")] string PeriodStart);
