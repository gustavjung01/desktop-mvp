using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record SupplierPaymentLedgerEntryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("entryType")] public string EntryType { get; init; } = string.Empty;
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0";
    [JsonPropertyName("requestId")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("occurredAt")] public string OccurredAt { get; init; } = string.Empty;
}

public sealed record SupplierPaymentAllocationData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("sourcePayableDocumentId")] public string SourcePayableDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentNumber")] public string? SourceDocumentNumber { get; init; }
    [JsonPropertyName("sourceDocumentType")] public string? SourceDocumentType { get; init; }
    [JsonPropertyName("targetPayableDocumentId")] public string TargetPayableDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("targetDocumentNumber")] public string? TargetDocumentNumber { get; init; }
    [JsonPropertyName("targetDocumentType")] public string? TargetDocumentType { get; init; }
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0";
    [JsonPropertyName("allocationDate")] public string AllocationDate { get; init; } = string.Empty;
    [JsonPropertyName("reversed")] public bool Reversed { get; init; }
    [JsonPropertyName("reversalId")] public string? ReversalId { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
}

public sealed record SupplierPaymentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("supplierCode")] public string? SupplierCode { get; init; }
    [JsonPropertyName("supplierName")] public string? SupplierName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("direction")] public string Direction { get; init; } = "CREDIT";
    [JsonPropertyName("documentType")] public string DocumentType { get; init; } = "SUPPLIER_PAYMENT";
    [JsonPropertyName("documentNumber")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("paymentDate")] public string PaymentDate { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("paymentMethod")] public string PaymentMethod { get; init; } = string.Empty;
    [JsonPropertyName("externalReference")] public string? ExternalReference { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("originalAmount")] public string OriginalAmount { get; init; } = "0";
    [JsonPropertyName("allocatedAmount")] public string AllocatedAmount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("postedAt")] public string PostedAt { get; init; } = string.Empty;
    [JsonPropertyName("postedBy")] public string PostedBy { get; init; } = string.Empty;
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
    [JsonPropertyName("reversedBy")] public string? ReversedBy { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("ledgerEntries")] public SupplierPaymentLedgerEntryData[] LedgerEntries { get; init; } = [];
    [JsonPropertyName("allocations")] public SupplierPaymentAllocationData[] Allocations { get; init; } = [];
}

public sealed record SupplierPaymentAllocationTargetData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("documentNumber")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("supplierCode")] public string? SupplierCode { get; init; }
    [JsonPropertyName("supplierName")] public string? SupplierName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("dueDate")] public string DueDate { get; init; } = string.Empty;
    [JsonPropertyName("originalAmount")] public string OriginalAmount { get; init; } = "0";
    [JsonPropertyName("allocatedAmount")] public string AllocatedAmount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
}

public sealed record SupplierPaymentCreateRequest(
    [property: JsonPropertyName("supplierId")] string SupplierId,
    [property: JsonPropertyName("warehouseId")] string WarehouseId,
    [property: JsonPropertyName("paymentDate")] string PaymentDate,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("paymentMethod")] string PaymentMethod,
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("externalReference")] string? ExternalReference,
    [property: JsonPropertyName("note")] string? Note);

public sealed record SupplierPaymentAllocationRequest(
    [property: JsonPropertyName("sourcePayableDocumentId")] string SourcePayableDocumentId,
    [property: JsonPropertyName("targetPayableDocumentId")] string TargetPayableDocumentId,
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("allocationDate")] string AllocationDate);

public sealed record SupplierPaymentReverseRequest(
    [property: JsonPropertyName("reason")] string Reason);
