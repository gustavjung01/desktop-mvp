using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record CustomerReturnCreditLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sourceReceivableDocumentId")] public string SourceReceivableDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentNumber")] public string? SourceDocumentNumber { get; init; }
    [JsonPropertyName("sku")] public string? Sku { get; init; }
    [JsonPropertyName("itemName")] public string? ItemName { get; init; }
    [JsonPropertyName("unitCode")] public string? UnitCode { get; init; }
    [JsonPropertyName("acceptedBaseQuantity")] public string AcceptedBaseQuantity { get; init; } = "0";
    [JsonPropertyName("adjustmentAmount")] public string AdjustmentAmount { get; init; } = "0";
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
}

public sealed record CustomerReturnCreditAllocationData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("sourceReceivableDocumentId")] public string SourceReceivableDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentNumber")] public string? SourceDocumentNumber { get; init; }
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
    [JsonPropertyName("sourceCreditDocumentId")] public string SourceCreditDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("sourceCreditNumber")] public string? SourceCreditNumber { get; init; }
    [JsonPropertyName("refundNumber")] public string? RefundNumber { get; init; }
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0";
    [JsonPropertyName("refundMethod")] public string RefundMethod { get; init; } = string.Empty;
    [JsonPropertyName("destinationReference")] public string DestinationReference { get; init; } = string.Empty;
    [JsonPropertyName("externalReference")] public string? ExternalReference { get; init; }
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("postedAt")] public string PostedAt { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("reversalId")] public string? ReversalId { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
}

public sealed record CustomerReturnCreditData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("customerReturnId")] public string CustomerReturnId { get; init; } = string.Empty;
    [JsonPropertyName("returnNumber")] public string ReturnNumber { get; init; } = string.Empty;
    [JsonPropertyName("customerReturnReceivedAt")] public string? CustomerReturnReceivedAt { get; init; }
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("documentNumber")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
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
    [JsonPropertyName("lines")] public CustomerReturnCreditLineData[] Lines { get; init; } = [];
    [JsonPropertyName("allocations")] public CustomerReturnCreditAllocationData[] Allocations { get; init; } = [];
    [JsonPropertyName("refunds")] public CustomerRefundData[] Refunds { get; init; } = [];
}

public sealed record CustomerReturnCreditAllocateRequest(
    [property: JsonPropertyName("allocationDate")] string AllocationDate,
    [property: JsonPropertyName("allocations")] CustomerPaymentAllocationDraft[] Allocations);

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
