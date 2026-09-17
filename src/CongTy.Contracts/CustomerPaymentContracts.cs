using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record CustomerPaymentAllocationData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("targetReceivableDocumentId")] public string TargetReceivableDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("targetDocumentNumber")] public string? TargetDocumentNumber { get; init; }
    [JsonPropertyName("targetWarehouseId")] public string? TargetWarehouseId { get; init; }
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0";
    [JsonPropertyName("allocationDate")] public string AllocationDate { get; init; } = string.Empty;
    [JsonPropertyName("reversed")] public bool Reversed { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
}

public sealed record CustomerPaymentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("documentNumber")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("paymentDate")] public string PaymentDate { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("paymentMethod")] public string PaymentMethod { get; init; } = string.Empty;
    [JsonPropertyName("externalReference")] public string? ExternalReference { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("remittingEmployeeId")] public string? RemittingEmployeeId { get; init; }
    [JsonPropertyName("remittingEmployeeCode")] public string? RemittingEmployeeCode { get; init; }
    [JsonPropertyName("remittingEmployeeName")] public string? RemittingEmployeeName { get; init; }
    [JsonPropertyName("originalAmount")] public string OriginalAmount { get; init; } = "0";
    [JsonPropertyName("allocatedAmount")] public string AllocatedAmount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("relatedSalesOrderNumbers")] public string[] RelatedSalesOrderNumbers { get; init; } = [];
    [JsonPropertyName("relatedReceivableCount")] public int RelatedReceivableCount { get; init; }
    [JsonPropertyName("relatedRemainingAmount")] public string RelatedRemainingAmount { get; init; } = "0";
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("postedBy")] public string PostedBy { get; init; } = string.Empty;
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("allocations")] public CustomerPaymentAllocationData[] Allocations { get; init; } = [];
}

public sealed record ReceivableAllocationTargetData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("documentNumber")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentDate")] public string SourceDocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("salesOrderNumber")] public string? SalesOrderNumber { get; init; }
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
}

public sealed record RemittingEmployeeOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("fullName")] public string FullName { get; init; } = string.Empty;
}

public sealed record CustomerPaymentAllocationDraft(
    [property: JsonPropertyName("receivableDocumentId")] string ReceivableDocumentId,
    [property: JsonPropertyName("amount")] string Amount);

public sealed record CustomerPaymentCreateRequest(
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("warehouseId")] string WarehouseId,
    [property: JsonPropertyName("paymentDate")] string PaymentDate,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("paymentMethod")] string PaymentMethod,
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("remittingEmployeeId")] string? RemittingEmployeeId,
    [property: JsonPropertyName("externalReference")] string? ExternalReference,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("allocations")] CustomerPaymentAllocationDraft[]? Allocations);

public sealed record CustomerPaymentAllocateRequest(
    [property: JsonPropertyName("allocationDate")] string AllocationDate,
    [property: JsonPropertyName("allocations")] CustomerPaymentAllocationDraft[] Allocations);

public sealed record CustomerPaymentReverseRequest(
    [property: JsonPropertyName("reason")] string Reason);
