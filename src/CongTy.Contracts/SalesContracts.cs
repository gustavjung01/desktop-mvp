using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record SalesPriceStepData
{
    [JsonPropertyName("kind")] public string Kind { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("resolutionFingerprint")] public string? ResolutionFingerprint { get; init; }
    [JsonPropertyName("channelId")] public string? ChannelId { get; init; }
    [JsonPropertyName("priceListId")] public string? PriceListId { get; init; }
    [JsonPropertyName("priceListCode")] public string? PriceListCode { get; init; }
    [JsonPropertyName("priceListType")] public string? PriceListType { get; init; }
    [JsonPropertyName("itemId")] public string? ItemId { get; init; }
    [JsonPropertyName("adjustmentType")] public string? AdjustmentType { get; init; }
    [JsonPropertyName("amountMinor")] public string? AmountMinor { get; init; }
    [JsonPropertyName("rateBps")] public int? RateBps { get; init; }
    [JsonPropertyName("beforeUnitPriceMinor")] public string? BeforeUnitPriceMinor { get; init; }
    [JsonPropertyName("afterUnitPriceMinor")] public string? AfterUnitPriceMinor { get; init; }
    [JsonPropertyName("priority")] public int? Priority { get; init; }
    [JsonPropertyName("stackingMode")] public string? StackingMode { get; init; }
    [JsonPropertyName("sourceKind")] public string? SourceKind { get; init; }
    [JsonPropertyName("sourceKey")] public string? SourceKey { get; init; }
    [JsonPropertyName("externalRuleCode")] public string? ExternalRuleCode { get; init; }
    [JsonPropertyName("sourceSalesOrderId")] public string? SourceSalesOrderId { get; init; }
    [JsonPropertyName("sourceSalesOrderNumber")] public string? SourceSalesOrderNumber { get; init; }
    [JsonPropertyName("sourceVersionNumber")] public string? SourceVersionNumber { get; init; }
    [JsonPropertyName("sourceLineId")] public string? SourceLineId { get; init; }
    [JsonPropertyName("sourceConfirmedAt")] public string? SourceConfirmedAt { get; init; }
    [JsonPropertyName("sourceUnitPriceMinor")] public string? SourceUnitPriceMinor { get; init; }
}

public sealed record SalesOrderLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitId")] public string UnitId { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("unitName")] public string? UnitName { get; init; }
    [JsonPropertyName("conversionToBase")] public string ConversionToBase { get; init; } = "1";
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "0";
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = "0";
    [JsonPropertyName("unitWeightKg")] public string? UnitWeightKg { get; init; }
    [JsonPropertyName("lineWeightKg")] public string? LineWeightKg { get; init; }
    [JsonPropertyName("priceListId")] public string? PriceListId { get; init; }
    [JsonPropertyName("priceRuleId")] public string? PriceRuleId { get; init; }
    [JsonPropertyName("priceSource")] public string PriceSource { get; init; } = "PRICE_ENGINE";
    [JsonPropertyName("baseUnitPrice")] public string BaseUnitPrice { get; init; } = "0";
    [JsonPropertyName("systemUnitPrice")] public string SystemUnitPrice { get; init; } = "0";
    [JsonPropertyName("unitPrice")] public string UnitPrice { get; init; } = "0";
    [JsonPropertyName("manualOverrideReason")] public string? ManualOverrideReason { get; init; }
    [JsonPropertyName("pricingTrace")] public SalesPriceStepData[] PricingTrace { get; init; } = [];
    [JsonPropertyName("discountMode")] public string DiscountMode { get; init; } = "TOTAL_AMOUNT";
    [JsonPropertyName("discountValue")] public string DiscountValue { get; init; } = "0";
    [JsonPropertyName("discountAmount")] public string DiscountAmount { get; init; } = "0";
    [JsonPropertyName("taxMode")] public string TaxMode { get; init; } = "EXCLUSIVE";
    [JsonPropertyName("taxRate")] public string TaxRate { get; init; } = "0";
    [JsonPropertyName("taxAmount")] public string TaxAmount { get; init; } = "0";
    [JsonPropertyName("lineSubtotal")] public string LineSubtotal { get; init; } = "0";
    [JsonPropertyName("lineTotal")] public string LineTotal { get; init; } = "0";
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed record SalesOrderAddressData
{
    [JsonPropertyName("label")] public string? Label { get; init; }
    [JsonPropertyName("recipientName")] public string? RecipientName { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("addressLine1")] public string? AddressLine1 { get; init; }
    [JsonPropertyName("addressLine2")] public string? AddressLine2 { get; init; }
    [JsonPropertyName("ward")] public string? Ward { get; init; }
    [JsonPropertyName("district")] public string? District { get; init; }
    [JsonPropertyName("province")] public string? Province { get; init; }
    [JsonPropertyName("postalCode")] public string? PostalCode { get; init; }
    [JsonPropertyName("countryCode")] public string? CountryCode { get; init; }
}

public sealed record SalesOrderVersionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("versionNumber")] public string VersionNumber { get; init; } = "1";
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("customerMode")] public string CustomerMode { get; init; } = "EXISTING";
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("walkInDisplayName")] public string? WalkInDisplayName { get; init; }
    [JsonPropertyName("walkInPhone")] public string? WalkInPhone { get; init; }
    [JsonPropertyName("customerAddressId")] public string? CustomerAddressId { get; init; }
    [JsonPropertyName("customerAddress")] public SalesOrderAddressData? CustomerAddress { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("salesChannelId")] public string? SalesChannelId { get; init; }
    [JsonPropertyName("salesChannelCode")] public string? SalesChannelCode { get; init; }
    [JsonPropertyName("salesChannelName")] public string? SalesChannelName { get; init; }
    [JsonPropertyName("priceSelectionMode")] public string PriceSelectionMode { get; init; } = "STANDARD";
    [JsonPropertyName("deliveryMode")] public string DeliveryMode { get; init; } = "DELIVERY";
    [JsonPropertyName("deliveryExecutionMode")] public string? DeliveryExecutionMode { get; init; }
    [JsonPropertyName("collectionPolicy")] public string CollectionPolicy { get; init; } = "COLLECT_ON_DELIVERY";
    [JsonPropertyName("currency")] public string Currency { get; init; } = "VND";
    [JsonPropertyName("requestedDeliveryDate")] public string? RequestedDeliveryDate { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("subtotal")] public string Subtotal { get; init; } = "0";
    [JsonPropertyName("discountTotal")] public string DiscountTotal { get; init; } = "0";
    [JsonPropertyName("taxTotal")] public string TaxTotal { get; init; } = "0";
    [JsonPropertyName("total")] public string Total { get; init; } = "0";
    [JsonPropertyName("totalWeightKg")] public string? TotalWeightKg { get; init; }
    [JsonPropertyName("missingWeightLineCount")] public int MissingWeightLineCount { get; init; }
    [JsonPropertyName("documentDiscountMode")] public string DocumentDiscountMode { get; init; } = "NONE";
    [JsonPropertyName("documentDiscountValue")] public string DocumentDiscountValue { get; init; } = "0";
    [JsonPropertyName("documentDiscountReason")] public string? DocumentDiscountReason { get; init; }
    [JsonPropertyName("amendmentReason")] public string? AmendmentReason { get; init; }
    [JsonPropertyName("basedOnVersionNumber")] public string? BasedOnVersionNumber { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("confirmedAt")] public string? ConfirmedAt { get; init; }
    [JsonPropertyName("lines")] public SalesOrderLineData[] Lines { get; init; } = [];
}

public sealed record SalesOrderFulfillmentTotalsData
{
    [JsonPropertyName("orderedBaseQuantity")] public string OrderedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("reservedBaseQuantity")] public string ReservedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("backorderedBaseQuantity")] public string BackorderedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("allocatedBaseQuantity")] public string AllocatedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("pickedBaseQuantity")] public string PickedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("packedBaseQuantity")] public string PackedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("issuedBaseQuantity")] public string IssuedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("cancelledBaseQuantity")] public string CancelledBaseQuantity { get; init; } = "0";
}

public sealed record SalesOrderFulfillmentLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("salesOrderVersionId")] public string SalesOrderVersionId { get; init; } = string.Empty;
    [JsonPropertyName("salesOrderLineId")] public string SalesOrderLineId { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("salesVariantId")] public string SalesVariantId { get; init; } = string.Empty;
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("baseUnitCode")] public string BaseUnitCode { get; init; } = string.Empty;
    [JsonPropertyName("baseUnitName")] public string? BaseUnitName { get; init; }
    [JsonPropertyName("orderedBaseQuantity")] public string OrderedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("reservedBaseQuantity")] public string ReservedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("backorderedBaseQuantity")] public string BackorderedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("allocatedBaseQuantity")] public string AllocatedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("pickedBaseQuantity")] public string PickedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("packedBaseQuantity")] public string PackedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("issuedBaseQuantity")] public string IssuedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("cancelledBaseQuantity")] public string CancelledBaseQuantity { get; init; } = "0";
    [JsonPropertyName("warehouseOnHandBaseQuantity")] public string? WarehouseOnHandBaseQuantity { get; init; }
    [JsonPropertyName("warehouseHeldByOthersBaseQuantity")] public string? WarehouseHeldByOthersBaseQuantity { get; init; }
    [JsonPropertyName("warehouseAvailableBaseQuantity")] public string? WarehouseAvailableBaseQuantity { get; init; }
    [JsonPropertyName("state")] public string State { get; init; } = string.Empty;
}

public sealed record SalesOrderFulfillmentData
{
    [JsonPropertyName("status")] public string Status { get; init; } = "unallocated";
    [JsonPropertyName("allowBackorder")] public bool AllowBackorder { get; init; }
    [JsonPropertyName("totals")] public SalesOrderFulfillmentTotalsData? Totals { get; init; }
    [JsonPropertyName("lines")] public SalesOrderFulfillmentLineData[] Lines { get; init; } = [];
}

public sealed record SalesOrderData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string? Number { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("currentVersionNumber")] public string CurrentVersionNumber { get; init; } = "1";
    [JsonPropertyName("sourceType")] public string SourceType { get; init; } = "MANUAL";
    [JsonPropertyName("sourceId")] public string? SourceId { get; init; }
    [JsonPropertyName("customerMode")] public string CustomerMode { get; init; } = "EXISTING";
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("walkInDisplayName")] public string? WalkInDisplayName { get; init; }
    [JsonPropertyName("walkInPhone")] public string? WalkInPhone { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("salesChannelId")] public string? SalesChannelId { get; init; }
    [JsonPropertyName("salesChannelCode")] public string? SalesChannelCode { get; init; }
    [JsonPropertyName("salesChannelName")] public string? SalesChannelName { get; init; }
    [JsonPropertyName("priceSelectionMode")] public string PriceSelectionMode { get; init; } = "STANDARD";
    [JsonPropertyName("deliveryMode")] public string DeliveryMode { get; init; } = "DELIVERY";
    [JsonPropertyName("deliveryExecutionMode")] public string? DeliveryExecutionMode { get; init; }
    [JsonPropertyName("collectionPolicy")] public string CollectionPolicy { get; init; } = "COLLECT_ON_DELIVERY";
    [JsonPropertyName("fulfillmentStatus")] public string FulfillmentStatus { get; init; } = "unallocated";
    [JsonPropertyName("fulfillment")] public SalesOrderFulfillmentData? Fulfillment { get; init; }
    [JsonPropertyName("deliveryStatus")] public string DeliveryStatus { get; init; } = "pending";
    [JsonPropertyName("settlementStatus")] public string SettlementStatus { get; init; } = "not_due";
    [JsonPropertyName("receivableRemainingAmount")] public string ReceivableRemainingAmount { get; init; } = "0";
    [JsonPropertyName("total")] public string? Total { get; init; }
    [JsonPropertyName("currency")] public string Currency { get; init; } = "VND";
    [JsonPropertyName("requestedDeliveryDate")] public string? RequestedDeliveryDate { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("confirmedAt")] public string? ConfirmedAt { get; init; }
    [JsonPropertyName("cancelledAt")] public string? CancelledAt { get; init; }
    [JsonPropertyName("cancellationReason")] public string? CancellationReason { get; init; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("versions")] public SalesOrderVersionData[] Versions { get; init; } = [];
}

public sealed record SalesOrderChannelData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
}

public sealed record SalesOrderEntrySettingsPermissionsData
{
    [JsonPropertyName("canPriceOverride")] public bool CanPriceOverride { get; init; }
    [JsonPropertyName("canDiscountOverride")] public bool CanDiscountOverride { get; init; }
    [JsonPropertyName("canConfirm")] public bool CanConfirm { get; init; }
    [JsonPropertyName("canNegativeStockIssue")] public bool CanNegativeStockIssue { get; init; }
}

public sealed record SalesOrderEntrySettingsData
{
    [JsonPropertyName("walkInConfigured")] public bool WalkInConfigured { get; init; }
    [JsonPropertyName("walkInBootstrapSupported")] public bool WalkInBootstrapSupported { get; init; }
    [JsonPropertyName("defaultTaxMode")] public string DefaultTaxMode { get; init; } = "EXCLUSIVE";
    [JsonPropertyName("defaultTaxRate")] public string DefaultTaxRate { get; init; } = "0";
    [JsonPropertyName("salesChannels")] public SalesOrderChannelData[] SalesChannels { get; init; } = [];
    [JsonPropertyName("defaultSalesChannelId")] public string? DefaultSalesChannelId { get; init; }
    [JsonPropertyName("defaultWarehouseId")] public string? DefaultWarehouseId { get; init; }
    [JsonPropertyName("defaultDeliveryChoice")] public string DefaultDeliveryChoice { get; init; } = "TRIP";
    [JsonPropertyName("savedWarehouseId")] public string? SavedWarehouseId { get; init; }
    [JsonPropertyName("savedDeliveryChoice")] public string? SavedDeliveryChoice { get; init; }
    [JsonPropertyName("permissions")] public SalesOrderEntrySettingsPermissionsData Permissions { get; init; } = new();
}

public sealed record SalesOrderEntrySettingsUpdateRequest(
    [property: JsonPropertyName("warehouseId")] string? WarehouseId,
    [property: JsonPropertyName("deliveryChoice")] string? DeliveryChoice);

public sealed record SalesOrderSkuCatalogRowData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("productId")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("productCode")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("variantName")] public string VariantName { get; init; } = string.Empty;
    [JsonPropertyName("barcode")] public string? Barcode { get; init; }
    [JsonPropertyName("barcodes")] public string[] Barcodes { get; init; } = [];
    [JsonPropertyName("unitId")] public string? UnitId { get; init; }
    [JsonPropertyName("unitCode")] public string? UnitCode { get; init; }
    [JsonPropertyName("unitName")] public string? UnitName { get; init; }
    [JsonPropertyName("conversionToBase")] public string? ConversionToBase { get; init; }
    [JsonPropertyName("allowsFractional")] public bool? AllowsFractional { get; init; }
}

public sealed record SalesOrderSkuCatalogData
{
    [JsonPropertyName("cursor")] public string Cursor { get; init; } = string.Empty;
    [JsonPropertyName("full")] public bool Full { get; init; }
    [JsonPropertyName("upserts")] public SalesOrderSkuCatalogRowData[] Upserts { get; init; } = [];
    [JsonPropertyName("removeIds")] public string[] RemoveIds { get; init; } = [];
}

public sealed record ProductVariantData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("product_id")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("variant_kind")] public string VariantKind { get; init; } = "BASE";
    [JsonPropertyName("is_inventory_base")] public bool IsInventoryBase { get; init; }
    [JsonPropertyName("is_sellable")] public bool IsSellable { get; init; }
    [JsonPropertyName("is_catalog_visible")] public bool IsCatalogVisible { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("unit_id")] public string? UnitId { get; init; }
    [JsonPropertyName("conversion_to_base")] public string? ConversionToBase { get; init; }
    [JsonPropertyName("is_purchasable")] public bool IsPurchasable { get; init; }
    [JsonPropertyName("net_content_value")] public string? NetContentValue { get; init; }
    [JsonPropertyName("net_content_uom_code")] public string? NetContentUomCode { get; init; }
    [JsonPropertyName("weight_value")] public string? WeightValue { get; init; }
    [JsonPropertyName("weight_uom_code")] public string? WeightUomCode { get; init; }
    [JsonPropertyName("source_unit_label")] public string? SourceUnitLabel { get; init; }
    [JsonPropertyName("source_package_description")] public string? SourcePackageDescription { get; init; }
    [JsonPropertyName("unit_code")] public string? UnitCode { get; init; }
    [JsonPropertyName("unit_name")] public string? UnitName { get; init; }
    [JsonPropertyName("unit_symbol")] public string? UnitSymbol { get; init; }
    [JsonPropertyName("unit_kind")] public string? UnitKind { get; init; }
    [JsonPropertyName("allows_fractional")] public bool? AllowsFractional { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record SalesOrderSkuEligibilityData
{
    [JsonPropertyName("selectable")] public bool Selectable { get; init; }
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}

public sealed record SalesOrderSkuPricePreviewData
{
    [JsonPropertyName("status")] public string Status { get; init; } = "PENDING";
    [JsonPropertyName("unitPriceMinor")] public string? UnitPriceMinor { get; init; }
    [JsonPropertyName("message")] public string? Message { get; init; }
}

public sealed record SalesOrderSkuInventoryPreviewData
{
    [JsonPropertyName("status")] public string Status { get; init; } = "PENDING";
    [JsonPropertyName("onHandQuantity")] public string? OnHandQuantity { get; init; }
    [JsonPropertyName("availableQuantity")] public string? AvailableQuantity { get; init; }
    [JsonPropertyName("heldQuantity")] public string? HeldQuantity { get; init; }
    [JsonPropertyName("unitCode")] public string? UnitCode { get; init; }
    [JsonPropertyName("unitName")] public string? UnitName { get; init; }
}

public sealed record SalesOrderSkuSearchOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("productId")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("productCode")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("variantName")] public string VariantName { get; init; } = string.Empty;
    [JsonPropertyName("barcode")] public string? Barcode { get; init; }
    [JsonPropertyName("barcodes")] public string[] Barcodes { get; init; } = [];
    [JsonPropertyName("unitId")] public string? UnitId { get; init; }
    [JsonPropertyName("unitCode")] public string? UnitCode { get; init; }
    [JsonPropertyName("conversionToBase")] public string? ConversionToBase { get; init; }
    [JsonPropertyName("allowsFractional")] public bool? AllowsFractional { get; init; }
    [JsonPropertyName("unitName")] public string? UnitName { get; init; }
    [JsonPropertyName("defaultTaxMode")] public string DefaultTaxMode { get; init; } = "EXCLUSIVE";
    [JsonPropertyName("defaultTaxRate")] public string DefaultTaxRate { get; init; } = "0";
    [JsonPropertyName("eligibility")] public SalesOrderSkuEligibilityData Eligibility { get; init; } = new();
}

public sealed record SalesOrderSkuSearchPreviewData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("pricePreview")] public SalesOrderSkuPricePreviewData PricePreview { get; init; } = new();
    [JsonPropertyName("inventoryPreview")] public SalesOrderSkuInventoryPreviewData InventoryPreview { get; init; } = new();
    [JsonPropertyName("eligibilityMessage")] public string EligibilityMessage { get; init; } = string.Empty;
}

public sealed record SalesPriceResolutionData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "1";
    [JsonPropertyName("priceAt")] public string PriceAt { get; init; } = string.Empty;
    [JsonPropertyName("channelId")] public string? ChannelId { get; init; }
    [JsonPropertyName("customerId")] public string? CustomerId { get; init; }
    [JsonPropertyName("customerGroupId")] public string? CustomerGroupId { get; init; }
    [JsonPropertyName("baseUnitPriceMinor")] public string BaseUnitPriceMinor { get; init; } = "0";
    [JsonPropertyName("systemUnitPriceMinor")] public string SystemUnitPriceMinor { get; init; } = "0";
    [JsonPropertyName("finalUnitPriceMinor")] public string FinalUnitPriceMinor { get; init; } = "0";
    [JsonPropertyName("lineTotalMinor")] public string LineTotalMinor { get; init; } = "0";
    [JsonPropertyName("resolutionFingerprint")] public string ResolutionFingerprint { get; init; } = string.Empty;
    [JsonPropertyName("priceSource")] public string PriceSource { get; init; } = "PRICE_ENGINE";
    [JsonPropertyName("steps")] public SalesPriceStepData[] Steps { get; init; } = [];
}

public sealed record SalesOrderLineDraftRequest
{
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "1";
    [JsonPropertyName("taxMode")] public string TaxMode { get; init; } = "EXCLUSIVE";
    [JsonPropertyName("taxRate")] public string TaxRate { get; init; } = "0";
    [JsonPropertyName("manualUnitPriceMinor"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ManualUnitPriceMinor { get; init; }
    [JsonPropertyName("manualReason"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ManualReason { get; init; }
    [JsonPropertyName("discountMode")] public string DiscountMode { get; init; } = "TOTAL_AMOUNT";
    [JsonPropertyName("discountValue")] public string DiscountValue { get; init; } = "0";
    [JsonPropertyName("expectedSystemUnitPriceMinor"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExpectedSystemUnitPriceMinor { get; init; }
    [JsonPropertyName("expectedPricingFingerprint"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExpectedPricingFingerprint { get; init; }
}

public sealed record SalesOrderDraftRequest
{
    [JsonPropertyName("sourceType")] public string SourceType { get; init; } = "MANUAL";
    [JsonPropertyName("customerMode")] public string CustomerMode { get; init; } = "EXISTING";
    [JsonPropertyName("customerId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CustomerId { get; init; }
    [JsonPropertyName("walkInDisplayName"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? WalkInDisplayName { get; init; }
    [JsonPropertyName("walkInPhone"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? WalkInPhone { get; init; }
    [JsonPropertyName("customerAddressId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CustomerAddressId { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("salesChannelId")] public string SalesChannelId { get; init; } = string.Empty;
    [JsonPropertyName("priceSelectionMode")] public string PriceSelectionMode { get; init; } = "STANDARD";
    [JsonPropertyName("pricingAt")] public string PricingAt { get; init; } = string.Empty;
    [JsonPropertyName("deliveryMode")] public string DeliveryMode { get; init; } = "DELIVERY";
    [JsonPropertyName("deliveryExecutionMode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? DeliveryExecutionMode { get; init; }
    [JsonPropertyName("collectionPolicy")] public string CollectionPolicy { get; init; } = "COLLECT_ON_DELIVERY";
    [JsonPropertyName("currency")] public string Currency { get; init; } = "VND";
    [JsonPropertyName("requestedDeliveryDate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? RequestedDeliveryDate { get; init; }
    [JsonPropertyName("note"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Note { get; init; }
    [JsonPropertyName("expectedRevision"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExpectedRevision { get; init; }
    [JsonPropertyName("creditOverrideReason"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CreditOverrideReason { get; init; }
    [JsonPropertyName("documentDiscountMode")] public string DocumentDiscountMode { get; init; } = "NONE";
    [JsonPropertyName("documentDiscountValue")] public string DocumentDiscountValue { get; init; } = "0";
    [JsonPropertyName("documentDiscountReason"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? DocumentDiscountReason { get; init; }
    [JsonPropertyName("lines")] public SalesOrderLineDraftRequest[] Lines { get; init; } = [];
}

public sealed record SalesPricePreviewRequest(
    [property: JsonPropertyName("variantId")] string VariantId,
    [property: JsonPropertyName("quantity")] string Quantity,
    [property: JsonPropertyName("salesChannelId")] string SalesChannelId,
    [property: JsonPropertyName("priceSelectionMode")] string PriceSelectionMode,
    [property: JsonPropertyName("pricingAt")] string PricingAt,
    [property: JsonPropertyName("customerId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CustomerId);

public sealed record SalesReasonRequest([property: JsonPropertyName("reason")] string Reason);

public sealed record SalesExpectedRevisionRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("mode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Mode = null);

public sealed record SalesDirectSettlementRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("paidAmount")] string PaidAmount,
    [property: JsonPropertyName("paymentMethod"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PaymentMethod,
    [property: JsonPropertyName("remittingEmployeeId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RemittingEmployeeId = null);


public sealed record InventoryBalanceLookupData
{
    [JsonPropertyName("warehouse_id")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_code")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_name")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("base_variant_id")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("base_sku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("base_unit_code")] public string? BaseUnitCode { get; init; }
    [JsonPropertyName("base_unit_name")] public string? BaseUnitName { get; init; }
    [JsonPropertyName("base_unit_symbol")] public string? BaseUnitSymbol { get; init; }
    [JsonPropertyName("package_variant_id")] public string? PackageVariantId { get; init; }
    [JsonPropertyName("package_sku")] public string? PackageSku { get; init; }
    [JsonPropertyName("package_unit_code")] public string? PackageUnitCode { get; init; }
    [JsonPropertyName("package_unit_name")] public string? PackageUnitName { get; init; }
    [JsonPropertyName("package_unit_symbol")] public string? PackageUnitSymbol { get; init; }
    [JsonPropertyName("on_hand_quantity")] public string OnHandQuantity { get; init; } = "0";
    [JsonPropertyName("reserved_quantity")] public string ReservedQuantity { get; init; } = "0";
    [JsonPropertyName("available_quantity")] public string AvailableQuantity { get; init; } = "0";
}

public sealed record InventoryMovementHistoryData
{
    [JsonPropertyName("movement_id")] public string MovementId { get; init; } = string.Empty;
    [JsonPropertyName("movement_type")] public string MovementType { get; init; } = string.Empty;
    [JsonPropertyName("source_document_number")] public string? SourceDocumentNumber { get; init; }
    [JsonPropertyName("document_number")] public string? DocumentNumber { get; init; }
    [JsonPropertyName("document_date")] public string? DocumentDate { get; init; }
    [JsonPropertyName("posted_at")] public string PostedAt { get; init; } = string.Empty;
    [JsonPropertyName("posted_by_name")] public string? PostedByName { get; init; }
    [JsonPropertyName("posted_by")] public string PostedBy { get; init; } = string.Empty;
    [JsonPropertyName("reason_note")] public string? ReasonNote { get; init; }
    [JsonPropertyName("warehouse_id")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_code")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_name")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("base_variant_id")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("base_sku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("base_quantity_delta")] public string BaseQuantityDelta { get; init; } = "0";
    [JsonPropertyName("stock_after")] public string StockAfter { get; init; } = "0";
    [JsonPropertyName("location_summary")] public string? LocationSummary { get; init; }
    [JsonPropertyName("lot_summary")] public string? LotSummary { get; init; }
}
