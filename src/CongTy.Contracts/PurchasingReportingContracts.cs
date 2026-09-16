using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record PurchasingReportingDashboardData
{
    [JsonPropertyName("family")] public string Family { get; init; } = string.Empty;
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = "Asia/Ho_Chi_Minh";
    [JsonPropertyName("filters")] public PurchasingReportingFiltersData Filters { get; init; } = new();
    [JsonPropertyName("basis")] public PurchasingReportingBasisData Basis { get; init; } = new();
    [JsonPropertyName("summary")] public PurchasingReportingSummaryData Summary { get; init; } = new();
    [JsonPropertyName("currencyTotals")] public PurchasingReportingCurrencyTotalData[] CurrencyTotals { get; init; } = [];
    [JsonPropertyName("statusBreakdown")] public PurchasingReportingStatusData[] StatusBreakdown { get; init; } = [];
    [JsonPropertyName("dailyTrend")] public PurchasingReportingTrendData[] DailyTrend { get; init; } = [];
    [JsonPropertyName("topEntities")] public PurchasingReportingEntityData[] TopEntities { get; init; } = [];
    [JsonPropertyName("topSkus")] public PurchasingReportingSkuData[] TopSkus { get; init; } = [];
}

public sealed record PurchasingReportingFiltersData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
}

public sealed record PurchasingReportingBasisData
{
    [JsonPropertyName("date")] public string Date { get; init; } = string.Empty;
    [JsonPropertyName("value")] public string Value { get; init; } = string.Empty;
    [JsonPropertyName("effectiveStates")] public string[] EffectiveStates { get; init; } = [];
}

public sealed record PurchasingReportingSummaryData
{
    [JsonPropertyName("allOrderCount")] public string AllOrderCount { get; init; } = "0";
    [JsonPropertyName("effectiveOrderCount")] public string EffectiveOrderCount { get; init; } = "0";
    [JsonPropertyName("cancelledOrderCount")] public string CancelledOrderCount { get; init; } = "0";
    [JsonPropertyName("pendingApprovalCount")] public string PendingApprovalCount { get; init; } = "0";
    [JsonPropertyName("postedReceiptCount")] public string PostedReceiptCount { get; init; } = "0";
    [JsonPropertyName("reversedReceiptCount")] public string ReversedReceiptCount { get; init; } = "0";
}

public sealed record PurchasingReportingCurrencyTotalData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("documentCount")] public string DocumentCount { get; init; } = "0";
    [JsonPropertyName("totalValue")] public string TotalValue { get; init; } = "0";
}

public sealed record PurchasingReportingStatusData
{
    [JsonPropertyName("dimension")] public string Dimension { get; init; } = string.Empty;
    [JsonPropertyName("state")] public string State { get; init; } = string.Empty;
    [JsonPropertyName("documentCount")] public string DocumentCount { get; init; } = "0";
}

public sealed record PurchasingReportingTrendData
{
    [JsonPropertyName("businessDate")] public string BusinessDate { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("documentCount")] public string DocumentCount { get; init; } = "0";
    [JsonPropertyName("totalValue")] public string TotalValue { get; init; } = "0";
}

public sealed record PurchasingReportingEntityData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("entityId")] public string EntityId { get; init; } = string.Empty;
    [JsonPropertyName("entityCode")] public string EntityCode { get; init; } = string.Empty;
    [JsonPropertyName("entityName")] public string EntityName { get; init; } = string.Empty;
    [JsonPropertyName("documentCount")] public string DocumentCount { get; init; } = "0";
    [JsonPropertyName("totalValue")] public string TotalValue { get; init; } = "0";
}

public sealed record PurchasingReportingSkuData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = "0";
    [JsonPropertyName("totalValue")] public string TotalValue { get; init; } = "0";
    [JsonPropertyName("sampleDocumentNumber")] public string SampleDocumentNumber { get; init; } = string.Empty;
}
