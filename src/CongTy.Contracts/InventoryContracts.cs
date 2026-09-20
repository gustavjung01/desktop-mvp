using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record InventoryBalanceData
{
    [JsonPropertyName("warehouse_id")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_code")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_name")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("location_id")] public string? LocationId { get; init; }
    [JsonPropertyName("location_code")] public string? LocationCode { get; init; }
    [JsonPropertyName("location_name")] public string? LocationName { get; init; }
    [JsonPropertyName("base_variant_id")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("base_sku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("base_variant_name")] public string? BaseVariantName { get; init; }
    [JsonPropertyName("product_id")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("product_code")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("product_name")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("base_unit_code")] public string? BaseUnitCode { get; init; }
    [JsonPropertyName("base_unit_name")] public string? BaseUnitName { get; init; }
    [JsonPropertyName("base_unit_symbol")] public string? BaseUnitSymbol { get; init; }
    [JsonPropertyName("package_sku")] public string? PackageSku { get; init; }
    [JsonPropertyName("package_variant_name")] public string? PackageVariantName { get; init; }
    [JsonPropertyName("package_unit_code")] public string? PackageUnitCode { get; init; }
    [JsonPropertyName("package_unit_name")] public string? PackageUnitName { get; init; }
    [JsonPropertyName("package_unit_symbol")] public string? PackageUnitSymbol { get; init; }
    [JsonPropertyName("package_conversion_to_base")] public string? PackageConversionToBase { get; init; }
    [JsonPropertyName("lot_id")] public string? LotId { get; init; }
    [JsonPropertyName("lot_code")] public string? LotCode { get; init; }
    [JsonPropertyName("expiry_date")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("on_hand_quantity")] public string OnHandQuantity { get; init; } = "0";
    [JsonPropertyName("reserved_quantity")] public string ReservedQuantity { get; init; } = "0";
    [JsonPropertyName("available_quantity")] public string AvailableQuantity { get; init; } = "0";
    [JsonPropertyName("business_on_hand_quantity")] public string? BusinessOnHandQuantity { get; init; }
    [JsonPropertyName("business_held_quantity")] public string? BusinessHeldQuantity { get; init; }
    [JsonPropertyName("business_available_quantity")] public string? BusinessAvailableQuantity { get; init; }
}

public sealed record InventoryHistoryData
{
    [JsonPropertyName("movement_id")] public string MovementId { get; init; } = string.Empty;
    [JsonPropertyName("movement_type")] public string MovementType { get; init; } = string.Empty;
    [JsonPropertyName("source_domain")] public string SourceDomain { get; init; } = string.Empty;
    [JsonPropertyName("source_document_type")] public string? SourceDocumentType { get; init; }
    [JsonPropertyName("source_document_id")] public string? SourceDocumentId { get; init; }
    [JsonPropertyName("source_document_number")] public string? SourceDocumentNumber { get; init; }
    [JsonPropertyName("document_number")] public string? DocumentNumber { get; init; }
    [JsonPropertyName("document_date")] public string? DocumentDate { get; init; }
    [JsonPropertyName("posted_at")] public string PostedAt { get; init; } = string.Empty;
    [JsonPropertyName("posted_by")] public string PostedBy { get; init; } = string.Empty;
    [JsonPropertyName("posted_by_name")] public string? PostedByName { get; init; }
    [JsonPropertyName("reason_code")] public string? ReasonCode { get; init; }
    [JsonPropertyName("reason_note")] public string? ReasonNote { get; init; }
    [JsonPropertyName("reversal_of_movement_id")] public string? ReversalOfMovementId { get; init; }
    [JsonPropertyName("warehouse_id")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_code")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_name")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("base_variant_id")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("base_sku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("base_quantity_delta")] public string BaseQuantityDelta { get; init; } = "0";
    [JsonPropertyName("stock_after")] public string StockAfter { get; init; } = "0";
    [JsonPropertyName("line_count")] public int LineCount { get; init; }
    [JsonPropertyName("location_summary")] public string? LocationSummary { get; init; }
    [JsonPropertyName("lot_summary")] public string? LotSummary { get; init; }
}

public sealed record InventoryLotData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("base_variant_id")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("lot_code")] public string LotCode { get; init; } = string.Empty;
    [JsonPropertyName("normalized_lot_code")] public string NormalizedLotCode { get; init; } = string.Empty;
    [JsonPropertyName("manufactured_date")] public string? ManufacturedDate { get; init; }
    [JsonPropertyName("expiry_date")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("supplier_lot_reference")] public string? SupplierLotReference { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("base_sku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("base_variant_name")] public string? BaseVariantName { get; init; }
    [JsonPropertyName("product_code")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("product_name")] public string ProductName { get; init; } = string.Empty;
}

public sealed record InventoryReportingDashboardData
{
    [JsonPropertyName("family")] public string Family { get; init; } = string.Empty;
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("currentDate")] public string CurrentDate { get; init; } = string.Empty;
    [JsonPropertyName("filters")] public InventoryReportingFiltersData Filters { get; init; } = new();
    [JsonPropertyName("summary")] public InventoryReportingSummaryData Summary { get; init; } = new();
    [JsonPropertyName("periodFlow")] public InventoryPeriodFlowData[] PeriodFlow { get; init; } = [];
    [JsonPropertyName("movementTypes")] public InventoryMovementTypeData[] MovementTypes { get; init; } = [];
    [JsonPropertyName("warehouseSummary")] public InventoryWarehouseSummaryData[] WarehouseSummary { get; init; } = [];
    [JsonPropertyName("currentPositions")] public InventoryPositionData[] CurrentPositions { get; init; } = [];
    [JsonPropertyName("slowMoving")] public InventorySlowMovingData[] SlowMoving { get; init; } = [];
    [JsonPropertyName("expiryLots")] public InventoryExpiryLotData[] ExpiryLots { get; init; } = [];
    [JsonPropertyName("exceptions")] public InventoryExceptionData[] Exceptions { get; init; } = [];
    [JsonPropertyName("projectionState")] public InventoryProjectionStateData ProjectionState { get; init; } = new();
}

public sealed record InventoryReportingFiltersData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
    [JsonPropertyName("slowDays")] public int SlowDays { get; init; } = 90;
}

public sealed record InventoryReportingSummaryData
{
    [JsonPropertyName("stockPositionCount")] public string? StockPositionCount { get; init; }
    [JsonPropertyName("stockedSkuCount")] public string? StockedSkuCount { get; init; }
    [JsonPropertyName("reservedPositionCount")] public string? ReservedPositionCount { get; init; }
    [JsonPropertyName("lotScopeCount")] public string? LotScopeCount { get; init; }
    [JsonPropertyName("inventoryValueVnd")] public string? InventoryValueVnd { get; init; }
    [JsonPropertyName("costingExceptionCount")] public string? CostingExceptionCount { get; init; }
}

public sealed record InventoryPeriodFlowData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("openingQuantity")] public string OpeningQuantity { get; init; } = "0";
    [JsonPropertyName("inboundQuantity")] public string InboundQuantity { get; init; } = "0";
    [JsonPropertyName("outboundQuantity")] public string OutboundQuantity { get; init; } = "0";
    [JsonPropertyName("closingQuantity")] public string ClosingQuantity { get; init; } = "0";
    [JsonPropertyName("movementLineCount")] public string MovementLineCount { get; init; } = "0";
    [JsonPropertyName("lastPostedAt")] public string? LastPostedAt { get; init; }
}

public sealed record InventoryMovementTypeData
{
    [JsonPropertyName("movementType")] public string MovementType { get; init; } = string.Empty;
    [JsonPropertyName("movementCount")] public string MovementCount { get; init; } = "0";
    [JsonPropertyName("movementLineCount")] public string MovementLineCount { get; init; } = "0";
    [JsonPropertyName("skuCount")] public string SkuCount { get; init; } = "0";
}

public sealed record InventoryWarehouseSummaryData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("stockedSkuCount")] public string StockedSkuCount { get; init; } = "0";
    [JsonPropertyName("reservedSkuCount")] public string ReservedSkuCount { get; init; } = "0";
    [JsonPropertyName("inventoryValueVnd")] public string InventoryValueVnd { get; init; } = "0";
    [JsonPropertyName("costingExceptionCount")] public string CostingExceptionCount { get; init; } = "0";
    [JsonPropertyName("quantityProjectedThrough")] public string? QuantityProjectedThrough { get; init; }
    [JsonPropertyName("costingUpdatedAt")] public string? CostingUpdatedAt { get; init; }
}

public sealed record InventoryPositionData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("onHandQuantity")] public string OnHandQuantity { get; init; } = "0";
    [JsonPropertyName("reservedQuantity")] public string ReservedQuantity { get; init; } = "0";
    [JsonPropertyName("availableQuantity")] public string AvailableQuantity { get; init; } = "0";
    [JsonPropertyName("costingQuantity")] public string? CostingQuantity { get; init; }
    [JsonPropertyName("inventoryValue")] public string? InventoryValue { get; init; }
    [JsonPropertyName("averageUnitCost")] public string? AverageUnitCost { get; init; }
    [JsonPropertyName("costingStatus")] public string CostingStatus { get; init; } = string.Empty;
    [JsonPropertyName("anomalyCount")] public string AnomalyCount { get; init; } = "0";
    [JsonPropertyName("projectedThrough")] public string? ProjectedThrough { get; init; }
}

public sealed record InventorySlowMovingData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("onHandQuantity")] public string OnHandQuantity { get; init; } = "0";
    [JsonPropertyName("availableQuantity")] public string AvailableQuantity { get; init; } = "0";
    [JsonPropertyName("lastOutDate")] public string? LastOutDate { get; init; }
    [JsonPropertyName("daysSinceOutbound")] public string? DaysSinceOutbound { get; init; }
    [JsonPropertyName("neverOutbound")] public bool NeverOutbound { get; init; }
    [JsonPropertyName("inventoryValueVnd")] public string? InventoryValueVnd { get; init; }
}

public sealed record InventoryExpiryLotData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("lotId")] public string LotId { get; init; } = string.Empty;
    [JsonPropertyName("lotCode")] public string LotCode { get; init; } = string.Empty;
    [JsonPropertyName("manufacturedDate")] public string? ManufacturedDate { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("onHandQuantity")] public string OnHandQuantity { get; init; } = "0";
    [JsonPropertyName("availableQuantity")] public string AvailableQuantity { get; init; } = "0";
    [JsonPropertyName("manufacturedAgeDays")] public string? ManufacturedAgeDays { get; init; }
    [JsonPropertyName("daysToExpiry")] public string? DaysToExpiry { get; init; }
    [JsonPropertyName("expiryBucket")] public string ExpiryBucket { get; init; } = string.Empty;
}

public sealed record InventoryExceptionData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("ledgerQuantity")] public string LedgerQuantity { get; init; } = "0";
    [JsonPropertyName("costingQuantity")] public string CostingQuantity { get; init; } = "0";
    [JsonPropertyName("quantityDifference")] public string QuantityDifference { get; init; } = "0";
    [JsonPropertyName("costingStatus")] public string CostingStatus { get; init; } = string.Empty;
    [JsonPropertyName("anomalyCount")] public string AnomalyCount { get; init; } = "0";
    [JsonPropertyName("reconciliationStatus")] public string ReconciliationStatus { get; init; } = string.Empty;
}

public sealed record InventoryProjectionStateData
{
    [JsonPropertyName("ledgerThrough")] public string? LedgerThrough { get; init; }
    [JsonPropertyName("quantityProjectedThrough")] public string? QuantityProjectedThrough { get; init; }
    [JsonPropertyName("costingProjectedThrough")] public string? CostingProjectedThrough { get; init; }
    [JsonPropertyName("quantityProjectionStale")] public bool QuantityProjectionStale { get; init; }
}

public sealed record InventoryHoldBreakdownData
{
    [JsonPropertyName("heldBaseQuantity")] public string HeldBaseQuantity { get; init; } = "0";
    [JsonPropertyName("availableBaseQuantity")] public string AvailableBaseQuantity { get; init; } = "0";
    [JsonPropertyName("orders")] public InventoryHoldOrderData[] Orders { get; init; } = [];
}

public sealed record InventoryHoldOrderData
{
    [JsonPropertyName("salesOrderId")] public string SalesOrderId { get; init; } = string.Empty;
    [JsonPropertyName("orderNumber")] public string OrderNumber { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("salesSku")] public string SalesSku { get; init; } = string.Empty;
    [JsonPropertyName("baseSku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("baseUnitCode")] public string BaseUnitCode { get; init; } = string.Empty;
    [JsonPropertyName("baseUnitName")] public string? BaseUnitName { get; init; }
    [JsonPropertyName("deliveryMode")] public string? DeliveryMode { get; init; }
    [JsonPropertyName("deliveryExecutionMode")] public string? DeliveryExecutionMode { get; init; }
    [JsonPropertyName("fulfillmentStatus")] public string? FulfillmentStatus { get; init; }
    [JsonPropertyName("heldBaseQuantity")] public string HeldBaseQuantity { get; init; } = "0";
}
