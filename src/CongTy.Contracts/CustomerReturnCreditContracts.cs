using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record CustomerReturnCreditAllocationData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("targetReceivableDocumentId")] public string TargetReceivableDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("targetDocumentNumber")] public string? TargetDocumentNumber { get; init; }
    [JsonPropertyName("targetWarehouseId")] public string? TargetWarehouseId { get; init; }
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0";
    [JsonPropertyName("allocationDate")] public string AllocationDate { get; init; } = string.Empty;
    [JsonPropertyName("reversed")] public bool Reversed { get; init; }
    [JsonPropertyName("reversalId")] public string? ReversalId { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
}

public sealed record CustomerRefundData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("refundNumber")] public string RefundNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourceCreditDocumentId")] public string SourceCreditDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("sourceCreditDocumentNumber")] public string? SourceCreditDocumentNumber { get; init; }
    [JsonPropertyName("sourceCustomerReturnId")] public string? SourceCustomerReturnId { get; init; }
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("refundDate")] public string RefundDate { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("refundMethod")] public string RefundMethod { get; init; } = string.Empty;
    [JsonPropertyName("destinationReference")] public string DestinationReference { get; init; } = string.Empty;
    [JsonPropertyName("externalReference")] public string? ExternalReference { get; init; }
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0";
    [JsonPropertyName("postedBy")] public string PostedBy { get; init; } = string.Empty;
    [JsonPropertyName("ledgerEntryId")] public string? LedgerEntryId { get; init; }
    [JsonPropertyName("reversalId")] public string? ReversalId { get; init; }
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
}

public sealed record CustomerReturnCreditLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("sourceReturnLineId")] public string SourceReturnLineId { get; init; } = string.Empty;
    [JsonPropertyName("productId")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("skuId")] public string SkuId { get; init; } = string.Empty;
    [JsonPropertyName("skuCode")] public string SkuCode { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("quantityAccepted")] public string QuantityAccepted { get; init; } = "0";
    [JsonPropertyName("creditAmount")] public string CreditAmount { get; init; } = "0";
}

public sealed record CustomerReturnCreditData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("documentNumber")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourceCustomerReturnId")] public string SourceCustomerReturnId { get; init; } = string.Empty;
    [JsonPropertyName("sourceReturnNumber")] public string? SourceReturnNumber { get; init; }
    [JsonPropertyName("sourceSalesOrderId")] public string? SourceSalesOrderId { get; init; }
    [JsonPropertyName("sourceSalesOrderNumber")] public string? SourceSalesOrderNumber { get; init; }
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("postingDate")] public string PostingDate { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("grossAmount")] public string GrossAmount { get; init; } = "0";
    [JsonPropertyName("allocatedAmount")] public string AllocatedAmount { get; init; } = "0";
    [JsonPropertyName("refundedAmount")] public string RefundedAmount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("postedBy")] public string PostedBy { get; init; } = string.Empty;
    [JsonPropertyName("ledgerEntryId")] public string? LedgerEntryId { get; init; }
    [JsonPropertyName("reversalId")] public string? ReversalId { get; init; }
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("lines")] public CustomerReturnCreditLineData[] Lines { get; init; } = [];
    [JsonPropertyName("allocations")] public CustomerReturnCreditAllocationData[] Allocations { get; init; } = [];
    [JsonPropertyName("refunds")] public CustomerRefundData[] Refunds { get; init; } = [];
}

public sealed record CustomerReturnCreditAllocationDraft(
    [property: JsonPropertyName("receivableDocumentId")] string ReceivableDocumentId,
    [property: JsonPropertyName("amount")] string Amount);

public sealed record CustomerReturnCreditAllocateRequest(
    [property: JsonPropertyName("allocationDate")] string AllocationDate,
    [property: JsonPropertyName("allocations")] CustomerReturnCreditAllocationDraft[] Allocations);

public sealed record CustomerRefundCreateRequest(
    [property: JsonPropertyName("sourceCreditDocumentId")] string SourceCreditDocumentId,
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("refundMethod")] string RefundMethod,
    [property: JsonPropertyName("destinationReference")] string DestinationReference,
    [property: JsonPropertyName("externalReference")] string? ExternalReference,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("refundDate")] string RefundDate);

public sealed record CustomerReturnCreditReverseRequest(
    [property: JsonPropertyName("reason")] string Reason);
