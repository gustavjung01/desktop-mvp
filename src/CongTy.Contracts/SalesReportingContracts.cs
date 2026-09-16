using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record SalesReportingDashboardData
{
    [JsonPropertyName("family")] public string Family { get; init; } = string.Empty;
    [JsonPropertyName("contractVersion")] public string ContractVersion { get; init; } = string.Empty;
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = "Asia/Ho_Chi_Minh";
    [JsonPropertyName("filters")] public SalesReportingFiltersData Filters { get; init; } = new();
    [JsonPropertyName("scopeWarehouses")] public SalesScopeWarehouseData[] ScopeWarehouses { get; init; } = [];
    [JsonPropertyName("basis")] public SalesReportingBasisData Basis { get; init; } = new();
    [JsonPropertyName("comparison")] public SalesReportingComparisonData Comparison { get; init; } = new();
    [JsonPropertyName("summary")] public SalesReportingSummaryData Summary { get; init; } = new();
    [JsonPropertyName("breakdowns")] public SalesReportingBreakdownsData Breakdowns { get; init; } = new();
    [JsonPropertyName("breakdownTotals")] public SalesReportingBreakdownsData BreakdownTotals { get; init; } = new();
    [JsonPropertyName("reconciliation")] public SalesReportingReconciliationData Reconciliation { get; init; } = new();
    [JsonPropertyName("dataQuality")] public SalesReportingDataQualityData DataQuality { get; init; } = new();
    [JsonPropertyName("classification")] public SalesReportingClassificationData Classification { get; init; } = new();
    [JsonPropertyName("dailyTrend")] public SalesReportingTrendData[] DailyTrend { get; init; } = [];
}

public sealed record SalesReportingFiltersData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
    [JsonPropertyName("productGroupId")] public string? ProductGroupId { get; init; }
    [JsonPropertyName("customerGroupId")] public string? CustomerGroupId { get; init; }
    [JsonPropertyName("includeZeroProducts")] public bool IncludeZeroProducts { get; init; }
}

public sealed record SalesScopeWarehouseData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
}

public sealed record SalesReportingUnitData
{
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
}

public sealed record SalesReportingBasisData
{
    [JsonPropertyName("date")] public string Date { get; init; } = string.Empty;
    [JsonPropertyName("revenue")] public string Revenue { get; init; } = string.Empty;
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = string.Empty;
    [JsonPropertyName("classification")] public string Classification { get; init; } = string.Empty;
    [JsonPropertyName("employee")] public string Employee { get; init; } = string.Empty;
    [JsonPropertyName("historicalDimensions")] public string HistoricalDimensions { get; init; } = string.Empty;
    [JsonPropertyName("effectiveStates")] public string[] EffectiveStates { get; init; } = [];
}

public sealed record SalesReportingPeriodData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("dayCount")] public int DayCount { get; init; }
}

public sealed record SalesReportingComparisonData
{
    [JsonPropertyName("current")] public SalesReportingPeriodData Current { get; init; } = new();
    [JsonPropertyName("previous")] public SalesReportingPeriodData Previous { get; init; } = new();
}

public sealed record SalesRevenueSummaryData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("revenue")] public string Revenue { get; init; } = "0";
    [JsonPropertyName("previousRevenue")] public string PreviousRevenue { get; init; } = "0";
    [JsonPropertyName("changePercent")] public string? ChangePercent { get; init; }
    [JsonPropertyName("documentCount")] public string DocumentCount { get; init; } = "0";
}

public sealed record SalesQuantitySummaryData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("unit")] public SalesReportingUnitData Unit { get; init; } = new();
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "0";
    [JsonPropertyName("previousQuantity")] public string PreviousQuantity { get; init; } = "0";
    [JsonPropertyName("changePercent")] public string? ChangePercent { get; init; }
}

public sealed record SalesReportingSummaryData
{
    [JsonPropertyName("allOrderCount")] public string AllOrderCount { get; init; } = "0";
    [JsonPropertyName("effectiveOrderCount")] public string EffectiveOrderCount { get; init; } = "0";
    [JsonPropertyName("cancelledOrderCount")] public string CancelledOrderCount { get; init; } = "0";
    [JsonPropertyName("buyerCount")] public string BuyerCount { get; init; } = "0";
    [JsonPropertyName("soldProductCount")] public string SoldProductCount { get; init; } = "0";
    [JsonPropertyName("revenues")] public SalesRevenueSummaryData[] Revenues { get; init; } = [];
    [JsonPropertyName("quantities")] public SalesQuantitySummaryData[] Quantities { get; init; } = [];
}

public sealed record SalesBreakdownData
{
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("source")] public string Source { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("unit")] public SalesReportingUnitData Unit { get; init; } = new();
    [JsonPropertyName("revenue")] public string Revenue { get; init; } = "0";
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "0";
    [JsonPropertyName("documentCount")] public string DocumentCount { get; init; } = "0";
    [JsonPropertyName("customerCount")] public string CustomerCount { get; init; } = "0";
    [JsonPropertyName("productCount")] public string ProductCount { get; init; } = "0";
    [JsonPropertyName("sharePercent")] public string SharePercent { get; init; } = "0";
    [JsonPropertyName("previousRevenue")] public string PreviousRevenue { get; init; } = "0";
    [JsonPropertyName("previousQuantity")] public string PreviousQuantity { get; init; } = "0";
    [JsonPropertyName("changePercent")] public string? ChangePercent { get; init; }
    [JsonPropertyName("comparisonState")] public string ComparisonState { get; init; } = "comparable";
}

public sealed record SalesReportingBreakdownsData
{
    [JsonPropertyName("customers")] public SalesBreakdownData[] Customers { get; init; } = [];
    [JsonPropertyName("customerGroups")] public SalesBreakdownData[] CustomerGroups { get; init; } = [];
    [JsonPropertyName("channels")] public SalesBreakdownData[] Channels { get; init; } = [];
    [JsonPropertyName("products")] public SalesBreakdownData[] Products { get; init; } = [];
    [JsonPropertyName("productGroups")] public SalesBreakdownData[] ProductGroups { get; init; } = [];
    [JsonPropertyName("employees")] public SalesBreakdownData[] Employees { get; init; } = [];
}

public sealed record SalesReportingTrendData
{
    [JsonPropertyName("businessDate")] public string BusinessDate { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("revenue")] public string Revenue { get; init; } = "0";
    [JsonPropertyName("totalValue")] public string TotalValue { get; init; } = "0";
    [JsonPropertyName("previousRevenue")] public string PreviousRevenue { get; init; } = "0";
    [JsonPropertyName("changePercent")] public string? ChangePercent { get; init; }
}

public sealed record SalesReportingReconciliationData
{
    [JsonPropertyName("ok")] public bool Ok { get; init; }
    [JsonPropertyName("checkedOrderCount")] public string CheckedOrderCount { get; init; } = "0";
    [JsonPropertyName("mismatchCount")] public string MismatchCount { get; init; } = "0";
}

public sealed record SalesReportingDataQualityData
{
    [JsonPropertyName("customerGroupLegacyFallbackCount")] public string CustomerGroupLegacyFallbackCount { get; init; } = "0";
    [JsonPropertyName("productGroupLegacyFallbackCount")] public string ProductGroupLegacyFallbackCount { get; init; } = "0";
    [JsonPropertyName("unitNameLegacyFallbackCount")] public string UnitNameLegacyFallbackCount { get; init; } = "0";
    [JsonPropertyName("unattributedEmployeeCount")] public string UnattributedEmployeeCount { get; init; } = "0";
    [JsonPropertyName("warnings")] public string[] Warnings { get; init; } = [];
}

public sealed record SalesClassificationOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
}

public sealed record SalesProductGroupOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("parentCategoryId")] public string? ParentCategoryId { get; init; }
}

public sealed record SalesReportingClassificationOptionsData
{
    [JsonPropertyName("productGroups")] public SalesProductGroupOptionData[] ProductGroups { get; init; } = [];
    [JsonPropertyName("customerGroups")] public SalesClassificationOptionData[] CustomerGroups { get; init; } = [];
}

public sealed record SalesReportingClassificationData
{
    [JsonPropertyName("options")] public SalesReportingClassificationOptionsData Options { get; init; } = new();
}
