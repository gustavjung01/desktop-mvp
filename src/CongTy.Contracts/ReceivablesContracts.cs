using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record ReceivableLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("acceptedBaseQuantity")] public string AcceptedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("grossAmount")] public string GrossAmount { get; init; } = "0";
    [JsonPropertyName("discountAmount")] public string DiscountAmount { get; init; } = "0";
    [JsonPropertyName("taxAmount")] public string TaxAmount { get; init; } = "0";
    [JsonPropertyName("lineAmount")] public string LineAmount { get; init; } = "0";
}

public sealed record ReceivableLedgerEntryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("entryType")] public string EntryType { get; init; } = string.Empty;
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0";
    [JsonPropertyName("sourceDocumentType")] public string SourceDocumentType { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentNumber")] public string SourceDocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("occurredAt")] public string OccurredAt { get; init; } = string.Empty;
}

public sealed record ReceivableDocumentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("salesOrderId")] public string? SalesOrderId { get; init; }
    [JsonPropertyName("salesOrderNumber")] public string? SalesOrderNumber { get; init; }
    [JsonPropertyName("deliveryOrderId")] public string? DeliveryOrderId { get; init; }
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("sourceDocumentType")] public string SourceDocumentType { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentNumber")] public string SourceDocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentDate")] public string SourceDocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("collectionPolicy")] public string CollectionPolicy { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("originalAmount")] public string OriginalAmount { get; init; } = "0";
    [JsonPropertyName("allocatedAmount")] public string AllocatedAmount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("lines")] public ReceivableLineData[] Lines { get; init; } = [];
    [JsonPropertyName("ledgerEntries")] public ReceivableLedgerEntryData[] LedgerEntries { get; init; } = [];
}

public sealed record CustomerReceivableBalanceData
{
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("balance")] public string Balance { get; init; } = "0";
    [JsonPropertyName("openAmount")] public string OpenAmount { get; init; } = "0";
    [JsonPropertyName("openDocumentCount")] public int OpenDocumentCount { get; init; }
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
}
