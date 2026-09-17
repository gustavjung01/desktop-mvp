using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record PayableLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "0";
    [JsonPropertyName("unitPrice")] public string UnitPrice { get; init; } = "0";
    [JsonPropertyName("grossAmount")] public string GrossAmount { get; init; } = "0";
    [JsonPropertyName("discountAmount")] public string DiscountAmount { get; init; } = "0";
    [JsonPropertyName("taxAmount")] public string TaxAmount { get; init; } = "0";
    [JsonPropertyName("lineAmount")] public string LineAmount { get; init; } = "0";
}

public sealed record PayableLedgerEntryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("entryType")] public string EntryType { get; init; } = string.Empty;
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0";
    [JsonPropertyName("requestId")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("occurredAt")] public string OccurredAt { get; init; } = string.Empty;
}

public sealed record PayableAllocationHistoryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("targetDocumentNumber")] public string TargetDocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0";
    [JsonPropertyName("allocationDate")] public string AllocationDate { get; init; } = string.Empty;
    [JsonPropertyName("reversed")] public bool Reversed { get; init; }
}

public sealed record PayableDocumentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("supplierCode")] public string? SupplierCode { get; init; }
    [JsonPropertyName("supplierName")] public string? SupplierName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("direction")] public string Direction { get; init; } = string.Empty;
    [JsonPropertyName("documentType")] public string DocumentType { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentType")] public string SourceDocumentType { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentId")] public string SourceDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentNumber")] public string SourceDocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentDate")] public string SourceDocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("paymentMethod")] public string PaymentMethod { get; init; } = string.Empty;
    [JsonPropertyName("paymentTermDays")] public int PaymentTermDays { get; init; }
    [JsonPropertyName("dueDate")] public string DueDate { get; init; } = string.Empty;
    [JsonPropertyName("originalAmount")] public string OriginalAmount { get; init; } = "0";
    [JsonPropertyName("allocatedAmount")] public string AllocatedAmount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("signedOriginalAmount")] public string SignedOriginalAmount { get; init; } = "0";
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("postedAt")] public string PostedAt { get; init; } = string.Empty;
    [JsonPropertyName("postedBy")] public string PostedBy { get; init; } = string.Empty;
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("lines")] public PayableLineData[] Lines { get; init; } = [];
    [JsonPropertyName("ledgerEntries")] public PayableLedgerEntryData[] LedgerEntries { get; init; } = [];
    [JsonPropertyName("allocations")] public PayableAllocationHistoryData[] Allocations { get; init; } = [];
}

public sealed record SupplierPayableBalanceData
{
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("supplierCode")] public string SupplierCode { get; init; } = string.Empty;
    [JsonPropertyName("supplierName")] public string SupplierName { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("balance")] public string Balance { get; init; } = "0";
    [JsonPropertyName("openAmount")] public string OpenAmount { get; init; } = "0";
    [JsonPropertyName("overdueAmount")] public string OverdueAmount { get; init; } = "0";
    [JsonPropertyName("openDocumentCount")] public int OpenDocumentCount { get; init; }
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
}
