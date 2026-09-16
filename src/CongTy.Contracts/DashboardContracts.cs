using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record DashboardSalesReportData
{
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("filters")] public DashboardSalesFiltersData Filters { get; init; } = new();
    [JsonPropertyName("summary")] public DashboardSalesSummaryData Summary { get; init; } = new();
}

public sealed record DashboardSalesFiltersData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
}

public sealed record DashboardSalesSummaryData
{
    [JsonPropertyName("effectiveOrderCount")] public string? EffectiveOrderCount { get; init; }
    [JsonPropertyName("cancelledOrderCount")] public string? CancelledOrderCount { get; init; }
}

public sealed record DashboardLogisticsData
{
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("filters")] public DashboardLogisticsFiltersData Filters { get; init; } = new();
    [JsonPropertyName("summary")] public DashboardLogisticsSummaryData Summary { get; init; } = new();
}

public sealed record DashboardLogisticsFiltersData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
}

public sealed record DashboardLogisticsSummaryData
{
    [JsonPropertyName("tripCount")] public string? TripCount { get; init; }
    [JsonPropertyName("deliveredFullCount")] public string? DeliveredFullCount { get; init; }
    [JsonPropertyName("onTimeEligibleFullCount")] public string? OnTimeEligibleFullCount { get; init; }
    [JsonPropertyName("onTimeFullRatePercent")] public string? OnTimeFullRatePercent { get; init; }
}

public sealed record DashboardAgingData
{
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("receivable")] public DashboardAgingSectionData Receivable { get; init; } = new();
}

public sealed record DashboardAgingSectionData
{
    [JsonPropertyName("summary")] public DashboardAgingBucketData[] Summary { get; init; } = [];
}

public sealed record DashboardAgingBucketData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("ageBucket")] public string AgeBucket { get; init; } = string.Empty;
    [JsonPropertyName("documentCount")] public string DocumentCount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
}
