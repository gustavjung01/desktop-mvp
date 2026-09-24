using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record SalesChannelData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record PriceListData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("list_type")] public string ListType { get; init; } = "BASE";
    [JsonPropertyName("currency_code")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("channel_id")] public string? ChannelId { get; init; }
    [JsonPropertyName("customer_group_id")] public string? CustomerGroupId { get; init; }
    [JsonPropertyName("customer_id")] public string? CustomerId { get; init; }
    [JsonPropertyName("priority")] public int Priority { get; init; }
    [JsonPropertyName("stacking_mode")] public string StackingMode { get; init; } = "EXCLUSIVE";
    [JsonPropertyName("stop_processing")] public bool StopProcessing { get; init; }
    [JsonPropertyName("effective_from")] public string? EffectiveFrom { get; init; }
    [JsonPropertyName("effective_to")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("channel_code")] public string? ChannelCode { get; init; }
    [JsonPropertyName("channel_name")] public string? ChannelName { get; init; }
    [JsonPropertyName("customer_group_code")] public string? CustomerGroupCode { get; init; }
    [JsonPropertyName("customer_group_name")] public string? CustomerGroupName { get; init; }
    [JsonPropertyName("customer_code")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customer_name")] public string? CustomerName { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record PriceListItemData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("price_list_id")] public string PriceListId { get; init; } = string.Empty;
    [JsonPropertyName("variant_id")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("product_id")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("adjustment_type")] public string AdjustmentType { get; init; } = "FIXED_PRICE";
    [JsonPropertyName("amount_minor")] public string? AmountMinor { get; init; }
    [JsonPropertyName("rate_bps")] public int? RateBps { get; init; }
    [JsonPropertyName("min_quantity")] public string MinQuantity { get; init; } = "0";
    [JsonPropertyName("max_quantity")] public string? MaxQuantity { get; init; }
    [JsonPropertyName("effective_from")] public string? EffectiveFrom { get; init; }
    [JsonPropertyName("effective_to")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("source_kind")] public string SourceKind { get; init; } = "ADMIN";
    [JsonPropertyName("source_key")] public string? SourceKey { get; init; }
    [JsonPropertyName("external_rule_code")] public string? ExternalRuleCode { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("variant_name")] public string VariantName { get; init; } = string.Empty;
    [JsonPropertyName("product_code")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("product_name")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record PricingResolutionStepData
{
    [JsonPropertyName("kind")] public string Kind { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string? Reason { get; init; }
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
}

public sealed record PricingResolutionData
{
    [JsonPropertyName("variant")] public ProductVariantData Variant { get; init; } = new();
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "1";
    [JsonPropertyName("priceAt")] public string PriceAt { get; init; } = string.Empty;
    [JsonPropertyName("channelId")] public string? ChannelId { get; init; }
    [JsonPropertyName("customerGroupId")] public string? CustomerGroupId { get; init; }
    [JsonPropertyName("customerId")] public string? CustomerId { get; init; }
    [JsonPropertyName("baseUnitPriceMinor")] public string BaseUnitPriceMinor { get; init; } = "0";
    [JsonPropertyName("finalUnitPriceMinor")] public string FinalUnitPriceMinor { get; init; } = "0";
    [JsonPropertyName("lineTotalMinor")] public string LineTotalMinor { get; init; } = "0";
    [JsonPropertyName("steps")] public PricingResolutionStepData[] Steps { get; init; } = [];
}

public sealed record SalesChannelCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record SalesChannelUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record PriceListCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("listType")] string ListType,
    [property: JsonPropertyName("priority")] int Priority,
    [property: JsonPropertyName("stackingMode")] string StackingMode,
    [property: JsonPropertyName("stopProcessing")] bool StopProcessing,
    [property: JsonPropertyName("channelId")] string? ChannelId,
    [property: JsonPropertyName("customerGroupId")] string? CustomerGroupId,
    [property: JsonPropertyName("customerId")] string? CustomerId,
    [property: JsonPropertyName("effectiveFrom")] string? EffectiveFrom,
    [property: JsonPropertyName("effectiveTo")] string? EffectiveTo,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record PriceListUpdateRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("priority")] int? Priority,
    [property: JsonPropertyName("stackingMode")] string? StackingMode,
    [property: JsonPropertyName("stopProcessing")] bool? StopProcessing,
    [property: JsonPropertyName("channelId")] string? ChannelId,
    [property: JsonPropertyName("customerGroupId")] string? CustomerGroupId,
    [property: JsonPropertyName("customerId")] string? CustomerId,
    [property: JsonPropertyName("effectiveFrom")] string? EffectiveFrom,
    [property: JsonPropertyName("effectiveTo")] string? EffectiveTo,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isActive")] bool? IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record PriceListStatusUpdateRequest(
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record PriceListItemCreateRequest(
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

public sealed record PriceListItemUpdateRequest(
    [property: JsonPropertyName("amountMinor")] string? AmountMinor,
    [property: JsonPropertyName("rateBps")] int? RateBps,
    [property: JsonPropertyName("minQuantity")] string? MinQuantity,
    [property: JsonPropertyName("maxQuantity")] string? MaxQuantity,
    [property: JsonPropertyName("effectiveFrom")] string? EffectiveFrom,
    [property: JsonPropertyName("effectiveTo")] string? EffectiveTo,
    [property: JsonPropertyName("externalRuleCode")] string? ExternalRuleCode,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("isActive")] bool? IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record PriceListItemStatusUpdateRequest(
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record PricingResolveRequest(
    [property: JsonPropertyName("variantId")] string VariantId,
    [property: JsonPropertyName("quantity")] string Quantity,
    [property: JsonPropertyName("channelId")] string? ChannelId,
    [property: JsonPropertyName("customerGroupId")] string? CustomerGroupId,
    [property: JsonPropertyName("customerId")] string? CustomerId,
    [property: JsonPropertyName("manualUnitPriceMinor")] string? ManualUnitPriceMinor,
    [property: JsonPropertyName("manualReason")] string? ManualReason);


public sealed record PricingAdjustmentItemRequest(
    [property: JsonPropertyName("priceListCode")] string PriceListCode,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("adjustmentType")] string AdjustmentType,
    [property: JsonPropertyName("amountMinor")] string? AmountMinor,
    [property: JsonPropertyName("rateBps")] int? RateBps,
    [property: JsonPropertyName("minQuantity")] string MinQuantity,
    [property: JsonPropertyName("maxQuantity")] string? MaxQuantity,
    [property: JsonPropertyName("sourceKind")] string SourceKind,
    [property: JsonPropertyName("externalRuleCode")] string ExternalRuleCode,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record PricingAdjustmentRequest(
    [property: JsonPropertyName("matchBySku")] bool MatchBySku,
    [property: JsonPropertyName("replaceFrom")] bool ReplaceFrom,
    [property: JsonPropertyName("applyAt")] string? ApplyAt,
    [property: JsonPropertyName("sourceBatchId")] string SourceBatchId,
    [property: JsonPropertyName("items")] IReadOnlyList<PricingAdjustmentItemRequest> Items);

public sealed record PricingAdjustmentResultData
{
    [JsonPropertyName("itemsCreated")] public int ItemsCreated { get; init; }
    [JsonPropertyName("itemsUpdated")] public int ItemsUpdated { get; init; }
    [JsonPropertyName("itemsReplaced")] public int ItemsReplaced { get; init; }
    [JsonPropertyName("totalItems")] public int TotalItems { get; init; }
}

public sealed record PricingExportRequest(
    [property: JsonPropertyName("format")] string Format);

public sealed record PricingOfficialRowsData
{
    [JsonPropertyName("jobId")] public string? JobId { get; init; }
    [JsonPropertyName("columns")] public string[] Columns { get; init; } = [];
    [JsonPropertyName("rows")] public Dictionary<string, JsonElement>[] Rows { get; init; } = [];
}
