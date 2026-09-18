using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record DataExchangeProductVariantData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("product_id")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("variant_kind")] public string VariantKind { get; init; } = string.Empty;
    [JsonPropertyName("is_inventory_base")] public bool IsInventoryBase { get; init; }
    [JsonPropertyName("is_sellable")] public bool IsSellable { get; init; }
    [JsonPropertyName("is_catalog_visible")] public bool IsCatalogVisible { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record DataExchangeCustomerGroupData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record DataExchangeCustomerData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("group_id")] public string? GroupId { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record DataExchangeOfficialRowsData
{
    [JsonPropertyName("jobId")] public string? JobId { get; init; }
    [JsonPropertyName("columns")] public string[] Columns { get; init; } = [];
    [JsonPropertyName("rows")] public Dictionary<string, JsonElement>[] Rows { get; init; } = [];
}

public sealed record DataExchangeProductImportSummaryData
{
    [JsonPropertyName("imported")] public int Imported { get; init; }
    [JsonPropertyName("totalItems")] public int? TotalItems { get; init; }
}

public sealed record DataExchangeProductOnboardingSummaryData
{
    [JsonPropertyName("variantsConfigured")] public int VariantsConfigured { get; init; }
    [JsonPropertyName("policiesConfigured")] public int PoliciesConfigured { get; init; }
}

public sealed record DataExchangeProductImportResultData
{
    [JsonPropertyName("jobId")] public string? JobId { get; init; }
    [JsonPropertyName("import")] public DataExchangeProductImportSummaryData? Import { get; init; }
    [JsonPropertyName("onboarding")] public DataExchangeProductOnboardingSummaryData? Onboarding { get; init; }
}

public sealed record DataExchangePricingImportResultData
{
    [JsonPropertyName("itemsCreated")] public int ItemsCreated { get; init; }
    [JsonPropertyName("itemsUpdated")] public int ItemsUpdated { get; init; }
    [JsonPropertyName("totalItems")] public int TotalItems { get; init; }
}

public sealed record DataExchangeStocktakeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("stocktakeNumber")] public string StocktakeNumber { get; init; } = string.Empty;
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
}

public sealed record DataExchangeStocktakeImportResultData
{
    [JsonPropertyName("jobId")] public string? JobId { get; init; }
    [JsonPropertyName("stocktake")] public DataExchangeStocktakeData Stocktake { get; init; } = new();
}

public sealed record DataExchangeQuotationRowData
{
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("skuName")] public string SkuName { get; init; } = string.Empty;
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("unitPriceMinor")] public string UnitPriceMinor { get; init; } = string.Empty;
    [JsonPropertyName("lineTotalMinor")] public string LineTotalMinor { get; init; } = string.Empty;
    [JsonPropertyName("priceListCode")] public string PriceListCode { get; init; } = string.Empty;
}

public sealed record DataExchangeMovementData
{
    [JsonPropertyName("movement_id")] public string MovementId { get; init; } = string.Empty;
    [JsonPropertyName("movement_type")] public string MovementType { get; init; } = string.Empty;
    [JsonPropertyName("source_document_type")] public string? SourceDocumentType { get; init; }
    [JsonPropertyName("source_document_number")] public string? SourceDocumentNumber { get; init; }
    [JsonPropertyName("document_number")] public string? DocumentNumber { get; init; }
    [JsonPropertyName("document_date")] public string? DocumentDate { get; init; }
    [JsonPropertyName("posted_at")] public string PostedAt { get; init; } = string.Empty;
    [JsonPropertyName("direction")] public string Direction { get; init; } = string.Empty;
    [JsonPropertyName("base_quantity_delta")] public string BaseQuantityDelta { get; init; } = "0";
    [JsonPropertyName("lot_code")] public string? LotCode { get; init; }
    [JsonPropertyName("source_line_reference")] public string? SourceLineReference { get; init; }
}

public sealed record DataExchangeFileRowsRequest(
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("rows")] IReadOnlyList<Dictionary<string, string>> Rows);

public sealed record DataExchangeExportRequest(
    [property: JsonPropertyName("format")] string Format);

public sealed record DataExchangeStocktakeExportRequest(
    [property: JsonPropertyName("warehouseId")] string WarehouseId,
    [property: JsonPropertyName("format")] string Format);

public sealed record DataExchangePricingImportItemRequest(
    [property: JsonPropertyName("priceListCode")] string PriceListCode,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("adjustmentType")] string AdjustmentType,
    [property: JsonPropertyName("amountMinor")] string AmountMinor,
    [property: JsonPropertyName("minQuantity")] string MinQuantity,
    [property: JsonPropertyName("maxQuantity")] string? MaxQuantity,
    [property: JsonPropertyName("effectiveFrom")] string? EffectiveFrom,
    [property: JsonPropertyName("effectiveTo")] string? EffectiveTo,
    [property: JsonPropertyName("sourceKind")] string SourceKind,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record DataExchangePricingImportRequest(
    [property: JsonPropertyName("matchBySku")] bool MatchBySku,
    [property: JsonPropertyName("sourceBatchId")] string SourceBatchId,
    [property: JsonPropertyName("items")] IReadOnlyList<DataExchangePricingImportItemRequest> Items);

public sealed record DataExchangeQuotationRequest(
    [property: JsonPropertyName("skus")] IReadOnlyList<string> Skus,
    [property: JsonPropertyName("quantity")] string Quantity,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("channelId")] string? ChannelId,
    [property: JsonPropertyName("customerGroupId")] string? CustomerGroupId,
    [property: JsonPropertyName("customerId")] string? CustomerId,
    [property: JsonPropertyName("format")] string Format);
