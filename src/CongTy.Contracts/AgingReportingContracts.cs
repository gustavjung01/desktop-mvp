using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record AgingWarehouseOptionData
{
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
}

public sealed record AgingFilterData
{
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
}

public sealed record AgingBasisData
{
    [JsonPropertyName("receivable")] public string Receivable { get; init; } = string.Empty;
    [JsonPropertyName("payable")] public string Payable { get; init; } = string.Empty;
    [JsonPropertyName("currency")] public string Currency { get; init; } = string.Empty;
}

public sealed record AgingBucketData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("ageBucket")] public string AgeBucket { get; init; } = string.Empty;
    [JsonPropertyName("documentCount")] public string DocumentCount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
}

public sealed record AgingPartyData
{
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("supplierCode")] public string? SupplierCode { get; init; }
    [JsonPropertyName("supplierName")] public string? SupplierName { get; init; }
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("documentCount")] public string DocumentCount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("oldestDocumentDate")] public string? OldestDocumentDate { get; init; }
    [JsonPropertyName("oldestAgeDays")] public string? OldestAgeDays { get; init; }
    [JsonPropertyName("earliestDueDate")] public string? EarliestDueDate { get; init; }
    [JsonPropertyName("maxOverdueDays")] public string? MaxOverdueDays { get; init; }
}

public sealed record AgingDocumentData
{
    [JsonPropertyName("customerId")] public string? CustomerId { get; init; }
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("supplierId")] public string? SupplierId { get; init; }
    [JsonPropertyName("supplierCode")] public string? SupplierCode { get; init; }
    [JsonPropertyName("supplierName")] public string? SupplierName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentNumber")] public string SourceDocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentDate")] public string SourceDocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("dueDate")] public string? DueDate { get; init; }
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("originalAmount")] public string OriginalAmount { get; init; } = "0";
    [JsonPropertyName("allocatedAmount")] public string AllocatedAmount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("ageDays")] public string? AgeDays { get; init; }
    [JsonPropertyName("overdueDays")] public string? OverdueDays { get; init; }
    [JsonPropertyName("ageBucket")] public string AgeBucket { get; init; } = string.Empty;
}

public sealed record AgingReceivableData
{
    [JsonPropertyName("summary")] public AgingBucketData[] Summary { get; init; } = [];
    [JsonPropertyName("customers")] public AgingPartyData[] Customers { get; init; } = [];
    [JsonPropertyName("documents")] public AgingDocumentData[] Documents { get; init; } = [];
}

public sealed record AgingPayableData
{
    [JsonPropertyName("summary")] public AgingBucketData[] Summary { get; init; } = [];
    [JsonPropertyName("suppliers")] public AgingPartyData[] Suppliers { get; init; } = [];
    [JsonPropertyName("documents")] public AgingDocumentData[] Documents { get; init; } = [];
}

public sealed record AgingDashboardData
{
    [JsonPropertyName("family")] public string Family { get; init; } = string.Empty;
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = string.Empty;
    [JsonPropertyName("currentDate")] public string CurrentDate { get; init; } = string.Empty;
    [JsonPropertyName("filters")] public AgingFilterData Filters { get; init; } = new();
    [JsonPropertyName("scopeWarehouses")] public AgingWarehouseOptionData[] ScopeWarehouses { get; init; } = [];
    [JsonPropertyName("basis")] public AgingBasisData Basis { get; init; } = new();
    [JsonPropertyName("receivable")] public AgingReceivableData Receivable { get; init; } = new();
    [JsonPropertyName("payable")] public AgingPayableData Payable { get; init; } = new();
}
