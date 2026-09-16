using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record ProductCategoryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("parent_category_id")] public string? ParentCategoryId { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("sort_order")] public int SortOrder { get; init; }
    [JsonPropertyName("is_catalog_visible")] public bool IsCatalogVisible { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record ProductBrandData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("is_catalog_visible")] public bool IsCatalogVisible { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record ProductData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("catalog_name")] public string? CatalogName { get; init; }
    [JsonPropertyName("category_id")] public string? CategoryId { get; init; }
    [JsonPropertyName("brand_id")] public string? BrandId { get; init; }
    [JsonPropertyName("category_code")] public string? CategoryCode { get; init; }
    [JsonPropertyName("category_name")] public string? CategoryName { get; init; }
    [JsonPropertyName("brand_code")] public string? BrandCode { get; init; }
    [JsonPropertyName("brand_name")] public string? BrandName { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("notes")] public string? Notes { get; init; }
    [JsonPropertyName("is_catalog_visible")] public bool IsCatalogVisible { get; init; }
    [JsonPropertyName("is_orderable")] public bool IsOrderable { get; init; }
    [JsonPropertyName("is_inventory_managed")] public bool IsInventoryManaged { get; init; } = true;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record ProductUnitData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("symbol")] public string? Symbol { get; init; }
    [JsonPropertyName("unit_kind")] public string UnitKind { get; init; } = "COUNT";
    [JsonPropertyName("allows_fractional")] public bool AllowsFractional { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record ProductBarcodeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("variant_id")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("barcode")] public string Barcode { get; init; } = string.Empty;
    [JsonPropertyName("normalized_barcode")] public string NormalizedBarcode { get; init; } = string.Empty;
    [JsonPropertyName("barcode_type")] public string BarcodeType { get; init; } = "INTERNAL";
    [JsonPropertyName("is_primary")] public bool IsPrimary { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("source_reference")] public string? SourceReference { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record ProductQuantityNormalizationData
{
    [JsonPropertyName("productId")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("enteredQuantity")] public string EnteredQuantity { get; init; } = string.Empty;
    [JsonPropertyName("conversionToBase")] public string ConversionToBase { get; init; } = string.Empty;
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = string.Empty;
    [JsonPropertyName("inventoryBase")] public bool InventoryBase { get; init; }
}

public sealed record ProductImageIndexData
{
    [JsonPropertyName("baseUrl")] public string BaseUrl { get; init; } = string.Empty;
    [JsonPropertyName("codes")] public string[] Codes { get; init; } = [];
}

public sealed record ProductImageMutationData
{
    [JsonPropertyName("productId")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("productCode")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("imageUrl")] public string ImageUrl { get; init; } = string.Empty;
    [JsonPropertyName("byteSize")] public long? ByteSize { get; init; }
    [JsonPropertyName("deleted")] public bool? Deleted { get; init; }
}

public sealed record ProductTrackingPolicyData
{
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("base_variant_id")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("lot_tracking_mode")] public string LotTrackingMode { get; init; } = "NONE";
    [JsonPropertyName("expiry_tracking_mode")] public string ExpiryTrackingMode { get; init; } = "NONE";
    [JsonPropertyName("location_required")] public bool LocationRequired { get; init; }
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("base_sku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("product_code")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("product_name")] public string ProductName { get; init; } = string.Empty;
}

public sealed record ProductPriceListData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("list_type")] public string ListType { get; init; } = string.Empty;
    [JsonPropertyName("currency_code")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("priority")] public int Priority { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record ProductPriceItemData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("price_list_id")] public string PriceListId { get; init; } = string.Empty;
    [JsonPropertyName("variant_id")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("product_id")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("adjustment_type")] public string AdjustmentType { get; init; } = string.Empty;
    [JsonPropertyName("amount_minor")] public string? AmountMinor { get; init; }
    [JsonPropertyName("rate_bps")] public int? RateBps { get; init; }
    [JsonPropertyName("min_quantity")] public string MinQuantity { get; init; } = "0";
    [JsonPropertyName("max_quantity")] public string? MaxQuantity { get; init; }
    [JsonPropertyName("source_kind")] public string SourceKind { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record ProductCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("catalogName")] string? CatalogName,
    [property: JsonPropertyName("categoryId")] string? CategoryId,
    [property: JsonPropertyName("brandId")] string? BrandId,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("isCatalogVisible")] bool IsCatalogVisible,
    [property: JsonPropertyName("isOrderable")] bool IsOrderable,
    [property: JsonPropertyName("isInventoryManaged")] bool IsInventoryManaged,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record ProductUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("catalogName")] string? CatalogName,
    [property: JsonPropertyName("categoryId")] string? CategoryId,
    [property: JsonPropertyName("brandId")] string? BrandId,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("isCatalogVisible")] bool IsCatalogVisible,
    [property: JsonPropertyName("isOrderable")] bool IsOrderable,
    [property: JsonPropertyName("isInventoryManaged")] bool IsInventoryManaged,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record ProductCategoryCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("parentCategoryId")] string? ParentCategoryId,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("sortOrder")] int SortOrder,
    [property: JsonPropertyName("isCatalogVisible")] bool IsCatalogVisible,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record ProductCategoryUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("parentCategoryId")] string? ParentCategoryId,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("sortOrder")] int SortOrder,
    [property: JsonPropertyName("isCatalogVisible")] bool IsCatalogVisible,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record ProductBrandCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isCatalogVisible")] bool IsCatalogVisible,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record ProductBrandUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isCatalogVisible")] bool IsCatalogVisible,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record ProductVariantCreateRequest(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("variantKind")] string VariantKind,
    [property: JsonPropertyName("isInventoryBase")] bool IsInventoryBase,
    [property: JsonPropertyName("isSellable")] bool IsSellable,
    [property: JsonPropertyName("isCatalogVisible")] bool IsCatalogVisible,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("weightValue")] string? WeightValue,
    [property: JsonPropertyName("weightUomCode")] string? WeightUomCode);

public sealed record ProductVariantUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("variantKind")] string VariantKind,
    [property: JsonPropertyName("isInventoryBase")] bool IsInventoryBase,
    [property: JsonPropertyName("isSellable")] bool IsSellable,
    [property: JsonPropertyName("isCatalogVisible")] bool IsCatalogVisible,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("weightValue")] string? WeightValue,
    [property: JsonPropertyName("weightUomCode")] string? WeightUomCode,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record ProductUnitCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("symbol")] string? Symbol,
    [property: JsonPropertyName("unitKind")] string UnitKind,
    [property: JsonPropertyName("allowsFractional")] bool AllowsFractional,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record ProductUnitUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("symbol")] string? Symbol,
    [property: JsonPropertyName("unitKind")] string UnitKind,
    [property: JsonPropertyName("allowsFractional")] bool AllowsFractional,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record ProductVariantUnitUpdateRequest(
    [property: JsonPropertyName("unitId")] string UnitId,
    [property: JsonPropertyName("conversionToBase")] string ConversionToBase,
    [property: JsonPropertyName("isPurchasable")] bool IsPurchasable,
    [property: JsonPropertyName("netContent")] ProductNetContentRequest? NetContent,
    [property: JsonPropertyName("sourceUnitLabel")] string? SourceUnitLabel,
    [property: JsonPropertyName("sourcePackageDescription")] string? SourcePackageDescription,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record ProductNetContentRequest(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("unitCode")] string UnitCode);

public sealed record ProductBarcodeCreateRequest(
    [property: JsonPropertyName("barcode")] string Barcode,
    [property: JsonPropertyName("barcodeType")] string BarcodeType,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary);

public sealed record ProductBarcodeUpdateRequest(
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record ProductNormalizeQuantityRequest(
    [property: JsonPropertyName("quantity")] string Quantity);

public sealed record ProductTrackingPolicyUpdateRequest(
    [property: JsonPropertyName("baseVariantId")] string BaseVariantId,
    [property: JsonPropertyName("lotTrackingMode")] string LotTrackingMode,
    [property: JsonPropertyName("expiryTrackingMode")] string ExpiryTrackingMode,
    [property: JsonPropertyName("expectedVersion")] int ExpectedVersion);

public sealed record ProductPriceItemCreateRequest(
    [property: JsonPropertyName("variantId")] string VariantId,
    [property: JsonPropertyName("adjustmentType")] string AdjustmentType,
    [property: JsonPropertyName("amountMinor")] string? AmountMinor,
    [property: JsonPropertyName("rateBps")] int? RateBps,
    [property: JsonPropertyName("minQuantity")] string MinQuantity,
    [property: JsonPropertyName("maxQuantity")] string? MaxQuantity,
    [property: JsonPropertyName("effectiveFrom")] string? EffectiveFrom,
    [property: JsonPropertyName("effectiveTo")] string? EffectiveTo,
    [property: JsonPropertyName("externalRuleCode")] string? ExternalRuleCode,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("sourceKind")] string SourceKind,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record ProductPriceItemUpdateRequest(
    [property: JsonPropertyName("amountMinor")] string AmountMinor,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record ProductBulkSourceRow(
    [property: JsonPropertyName("rowNumber")] int RowNumber,
    [property: JsonPropertyName("cells")] string[] Cells);

public sealed record ProductBulkIdentifyRequest(
    [property: JsonPropertyName("rows")] ProductBulkSourceRow[] Rows);

public sealed record ProductBulkErrorData
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}

public sealed record ProductBulkIdentificationRowData
{
    [JsonPropertyName("rowNumber")] public int RowNumber { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("variantName")] public string VariantName { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("errors")] public ProductBulkErrorData[] Errors { get; init; } = [];
    [JsonPropertyName("cells")] public object?[] Cells { get; init; } = [];
}

public sealed record ProductBulkIdentificationData
{
    [JsonPropertyName("identified")] public int Identified { get; init; }
    [JsonPropertyName("skipped")] public int Skipped { get; init; }
    [JsonPropertyName("rows")] public ProductBulkIdentificationRowData[] Rows { get; init; } = [];
}

public sealed record ProductBulkUpdateRequest(
    [property: JsonPropertyName("dryRun")] bool DryRun,
    [property: JsonPropertyName("mappings")] string[] Mappings,
    [property: JsonPropertyName("rows")] ProductBulkSourceRow[] Rows);

public sealed record ProductBulkChangeData
{
    [JsonPropertyName("field")] public string Field { get; init; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; init; } = string.Empty;
    [JsonPropertyName("oldValue")] public string OldValue { get; init; } = string.Empty;
    [JsonPropertyName("newValue")] public string NewValue { get; init; } = string.Empty;
}

public sealed record ProductBulkPreviewRowData
{
    [JsonPropertyName("rowNumber")] public int RowNumber { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("errors")] public ProductBulkErrorData[] Errors { get; init; } = [];
    [JsonPropertyName("changes")] public ProductBulkChangeData[] Changes { get; init; } = [];
    [JsonPropertyName("cells")] public object?[] Cells { get; init; } = [];
}

public sealed record ProductBulkPreviewData
{
    [JsonPropertyName("updated")] public int Updated { get; init; }
    [JsonPropertyName("skipped")] public int Skipped { get; init; }
    [JsonPropertyName("ready")] public int? Ready { get; init; }
    [JsonPropertyName("unchanged")] public int? Unchanged { get; init; }
    [JsonPropertyName("rows")] public ProductBulkPreviewRowData[] Rows { get; init; } = [];
    [JsonPropertyName("operationKey")] public string? OperationKey { get; init; }
}
