using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record GrossMarginReportingDashboardData
{
    [JsonPropertyName("family")] public string Family { get; init; } = string.Empty;
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = "Asia/Ho_Chi_Minh";
    [JsonPropertyName("filters")] public GrossMarginReportingFiltersData Filters { get; init; } = new();
    [JsonPropertyName("basis")] public GrossMarginReportingBasisData Basis { get; init; } = new();
    [JsonPropertyName("summary")] public GrossMarginReportingSummaryData Summary { get; init; } = new();
    [JsonPropertyName("topCustomers")] public GrossMarginReportingGroupData[] TopCustomers { get; init; } = [];
    [JsonPropertyName("topSkus")] public GrossMarginReportingGroupData[] TopSkus { get; init; } = [];
    [JsonPropertyName("lines")] public GrossMarginReportingLineData[] Lines { get; init; } = [];
    [JsonPropertyName("exceptions")] public GrossMarginReportingLineData[] Exceptions { get; init; } = [];
}

public sealed record GrossMarginReportingFiltersData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
}

public sealed record GrossMarginReportingBasisData
{
    [JsonPropertyName("revenue")] public string Revenue { get; init; } = string.Empty;
    [JsonPropertyName("cogs")] public string Cogs { get; init; } = string.Empty;
    [JsonPropertyName("comparableCurrency")] public string ComparableCurrency { get; init; } = string.Empty;
    [JsonPropertyName("lineage")] public string Lineage { get; init; } = string.Empty;
}

public sealed record GrossMarginReportingSummaryData
{
    [JsonPropertyName("eventLineCount")] public string EventLineCount { get; init; } = "0";
    [JsonPropertyName("comparableLineCount")] public string ComparableLineCount { get; init; } = "0";
    [JsonPropertyName("missingLineageCount")] public string MissingLineageCount { get; init; } = "0";
    [JsonPropertyName("missingCostCount")] public string MissingCostCount { get; init; } = "0";
    [JsonPropertyName("costAnomalyCount")] public string CostAnomalyCount { get; init; } = "0";
    [JsonPropertyName("nonVndCount")] public string NonVndCount { get; init; } = "0";
    [JsonPropertyName("netRevenueVnd")] public string NetRevenueVnd { get; init; } = "0";
    [JsonPropertyName("cogsVnd")] public string CogsVnd { get; init; } = "0";
    [JsonPropertyName("grossMarginVnd")] public string GrossMarginVnd { get; init; } = "0";
    [JsonPropertyName("grossMarginPercent")] public string? GrossMarginPercent { get; init; }
    [JsonPropertyName("costingCompletedAt")] public string? CostingCompletedAt { get; init; }
}

public sealed record GrossMarginReportingGroupData
{
    [JsonPropertyName("customerId")] public string? CustomerId { get; init; }
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("variantId")] public string? VariantId { get; init; }
    [JsonPropertyName("sku")] public string? Sku { get; init; }
    [JsonPropertyName("lineCount")] public string LineCount { get; init; } = "0";
    [JsonPropertyName("netRevenueVnd")] public string NetRevenueVnd { get; init; } = "0";
    [JsonPropertyName("cogsVnd")] public string CogsVnd { get; init; } = "0";
    [JsonPropertyName("grossMarginVnd")] public string GrossMarginVnd { get; init; } = "0";
    [JsonPropertyName("grossMarginPercent")] public string? GrossMarginPercent { get; init; }
}

public sealed record GrossMarginReportingLineData
{
    [JsonPropertyName("eventKind")] public string EventKind { get; init; } = string.Empty;
    [JsonPropertyName("accountingDocumentId")] public string AccountingDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("documentNumber")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("documentDate")] public string DocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("netRevenue")] public string NetRevenue { get; init; } = "0";
    [JsonPropertyName("cogs")] public string? Cogs { get; init; }
    [JsonPropertyName("grossMargin")] public string? GrossMargin { get; init; }
    [JsonPropertyName("exceptionCode")] public string? ExceptionCode { get; init; }
    [JsonPropertyName("rebuildRunId")] public string? RebuildRunId { get; init; }
    [JsonPropertyName("costingCompletedAt")] public string? CostingCompletedAt { get; init; }
    [JsonPropertyName("sourceLineId")] public string SourceLineId { get; init; } = string.Empty;
    [JsonPropertyName("costingMovementLineId")] public string? CostingMovementLineId { get; init; }
}
