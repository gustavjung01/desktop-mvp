using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record SalesSettlementFiltersData
{
    [JsonPropertyName("from")] public string? From { get; init; }
    [JsonPropertyName("to")] public string? To { get; init; }
    [JsonPropertyName("search")] public string? Search { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "all";
    [JsonPropertyName("limit")] public int Limit { get; init; } = 100;
}

public sealed record SalesSettlementSummaryData
{
    [JsonPropertyName("customerGroupCount")] public string CustomerGroupCount { get; init; } = "0";
    [JsonPropertyName("debitOutstandingAmount")] public string DebitOutstandingAmount { get; init; } = "0";
    [JsonPropertyName("unappliedCreditAmount")] public string UnappliedCreditAmount { get; init; } = "0";
    [JsonPropertyName("ledgerBalance")] public string LedgerBalance { get; init; } = "0";
    [JsonPropertyName("documentMismatchCount")] public string DocumentMismatchCount { get; init; } = "0";
    [JsonPropertyName("anomalyCount")] public string AnomalyCount { get; init; } = "0";
    [JsonPropertyName("codCustodyAmount")] public string CodCustodyAmount { get; init; } = "0";
    [JsonPropertyName("collectionMismatchCount")] public string CollectionMismatchCount { get; init; } = "0";
    [JsonPropertyName("codPendingAcceptanceAmount")] public string CodPendingAcceptanceAmount { get; init; } = "0";
    [JsonPropertyName("codAcceptedAmount")] public string CodAcceptedAmount { get; init; } = "0";
    [JsonPropertyName("codVarianceAmount")] public string CodVarianceAmount { get; init; } = "0";
    [JsonPropertyName("handoverMismatchCount")] public string HandoverMismatchCount { get; init; } = "0";
}

public sealed record SalesSettlementCustomerData
{
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("debitPostedAmount")] public string DebitPostedAmount { get; init; } = "0";
    [JsonPropertyName("creditPostedAmount")] public string CreditPostedAmount { get; init; } = "0";
    [JsonPropertyName("debitOutstandingAmount")] public string DebitOutstandingAmount { get; init; } = "0";
    [JsonPropertyName("unappliedCreditAmount")] public string UnappliedCreditAmount { get; init; } = "0";
    [JsonPropertyName("calculatedOpenBalance")] public string CalculatedOpenBalance { get; init; } = "0";
    [JsonPropertyName("ledgerBalance")] public string LedgerBalance { get; init; } = "0";
    [JsonPropertyName("documentMismatchCount")] public string DocumentMismatchCount { get; init; } = "0";
    [JsonPropertyName("latestDocumentDate")] public string? LatestDocumentDate { get; init; }
    [JsonPropertyName("reconciliationStatus")] public string ReconciliationStatus { get; init; } = string.Empty;
}

public sealed record SalesSettlementDocumentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("salesOrderId")] public string? SalesOrderId { get; init; }
    [JsonPropertyName("deliveryOrderId")] public string? DeliveryOrderId { get; init; }
    [JsonPropertyName("documentType")] public string DocumentType { get; init; } = string.Empty;
    [JsonPropertyName("direction")] public string Direction { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentType")] public string SourceDocumentType { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentId")] public string SourceDocumentId { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentNumber")] public string SourceDocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentDate")] public string SourceDocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("customerCodeSnapshot")] public string CustomerCodeSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("customerNameSnapshot")] public string CustomerNameSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCodeSnapshot")] public string WarehouseCodeSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("originalAmount")] public string OriginalAmount { get; init; } = "0";
    [JsonPropertyName("projectedAllocatedAmount")] public string ProjectedAllocatedAmount { get; init; } = "0";
    [JsonPropertyName("projectedRemainingAmount")] public string ProjectedRemainingAmount { get; init; } = "0";
    [JsonPropertyName("documentStatus")] public string DocumentStatus { get; init; } = string.Empty;
    [JsonPropertyName("ledgerAmount")] public string LedgerAmount { get; init; } = "0";
    [JsonPropertyName("expectedLedgerAmount")] public string ExpectedLedgerAmount { get; init; } = "0";
    [JsonPropertyName("ledgerMatches")] public bool LedgerMatches { get; init; }
    [JsonPropertyName("allocationProjectionMatches")] public bool AllocationProjectionMatches { get; init; }
    [JsonPropertyName("reconciliationStatus")] public string ReconciliationStatus { get; init; } = string.Empty;
}

public sealed record SalesSettlementOrderData
{
    [JsonPropertyName("salesOrderId")] public string SalesOrderId { get; init; } = string.Empty;
    [JsonPropertyName("orderNumber")] public string? OrderNumber { get; init; }
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("orderStatus")] public string OrderStatus { get; init; } = string.Empty;
    [JsonPropertyName("fulfillmentStatus")] public string FulfillmentStatus { get; init; } = string.Empty;
    [JsonPropertyName("deliveryStatus")] public string DeliveryStatus { get; init; } = string.Empty;
    [JsonPropertyName("settlementStatus")] public string SettlementStatus { get; init; } = string.Empty;
    [JsonPropertyName("calculatedSettlementStatus")] public string CalculatedSettlementStatus { get; init; } = string.Empty;
    [JsonPropertyName("receivablePostedAmount")] public string ReceivablePostedAmount { get; init; } = "0";
    [JsonPropertyName("receivableAllocatedAmount")] public string ReceivableAllocatedAmount { get; init; } = "0";
    [JsonPropertyName("receivableRemainingAmount")] public string ReceivableRemainingAmount { get; init; } = "0";
    [JsonPropertyName("codCollectedAmount")] public string CodCollectedAmount { get; init; } = "0";
    [JsonPropertyName("codCustodyAmount")] public string CodCustodyAmount { get; init; } = "0";
    [JsonPropertyName("documentMismatchCount")] public string DocumentMismatchCount { get; init; } = "0";
    [JsonPropertyName("settlementProjectionMatches")] public bool SettlementProjectionMatches { get; init; }
    [JsonPropertyName("reconciliationStatus")] public string ReconciliationStatus { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record SalesSettlementCodCollectionData
{
    [JsonPropertyName("collectionId")] public string CollectionId { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("tripId")] public string TripId { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string TripNumber { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderId")] public string DeliveryOrderId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("driverProfileId")] public string DriverProfileId { get; init; } = string.Empty;
    [JsonPropertyName("driverCode")] public string DriverCode { get; init; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; init; } = string.Empty;
    [JsonPropertyName("collectionMethod")] public string CollectionMethod { get; init; } = string.Empty;
    [JsonPropertyName("collectionStatus")] public string CollectionStatus { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("expectedAmount")] public string ExpectedAmount { get; init; } = "0";
    [JsonPropertyName("receivedAmount")] public string ReceivedAmount { get; init; } = "0";
    [JsonPropertyName("handedOverAmount")] public string HandedOverAmount { get; init; } = "0";
    [JsonPropertyName("custodyRemainingAmount")] public string CustodyRemainingAmount { get; init; } = "0";
    [JsonPropertyName("reversed")] public bool Reversed { get; init; }
    [JsonPropertyName("collectedAt")] public string CollectedAt { get; init; } = string.Empty;
    [JsonPropertyName("lifecycleAccountedAmount")] public string LifecycleAccountedAmount { get; init; } = "0";
    [JsonPropertyName("lifecycleMatches")] public bool LifecycleMatches { get; init; }
    [JsonPropertyName("lifecycleStatus")] public string LifecycleStatus { get; init; } = string.Empty;
}

public sealed record SalesSettlementCodHandoverData
{
    [JsonPropertyName("handoverId")] public string HandoverId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("tripId")] public string TripId { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string TripNumber { get; init; } = string.Empty;
    [JsonPropertyName("driverProfileId")] public string DriverProfileId { get; init; } = string.Empty;
    [JsonPropertyName("driverCode")] public string DriverCode { get; init; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; init; } = string.Empty;
    [JsonPropertyName("expectedTotal")] public string ExpectedTotal { get; init; } = "0";
    [JsonPropertyName("handedOverTotal")] public string HandedOverTotal { get; init; } = "0";
    [JsonPropertyName("unattributedExcessAmount")] public string UnattributedExcessAmount { get; init; } = "0";
    [JsonPropertyName("claimedAmount")] public string ClaimedAmount { get; init; } = "0";
    [JsonPropertyName("handoverDifferenceAmount")] public string HandoverDifferenceAmount { get; init; } = "0";
    [JsonPropertyName("pendingAcceptanceAmount")] public string PendingAcceptanceAmount { get; init; } = "0";
    [JsonPropertyName("acceptedAmount")] public string AcceptedAmount { get; init; } = "0";
    [JsonPropertyName("varianceAmount")] public string VarianceAmount { get; init; } = "0";
    [JsonPropertyName("projectionStatus")] public string ProjectionStatus { get; init; } = string.Empty;
    [JsonPropertyName("handedOverAt")] public string HandedOverAt { get; init; } = string.Empty;
    [JsonPropertyName("acceptedAt")] public string? AcceptedAt { get; init; }
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
    [JsonPropertyName("lifecycleMatches")] public bool LifecycleMatches { get; init; }
}

public sealed record SalesSettlementAnomalyData
{
    [JsonPropertyName("anomalyType")] public string AnomalyType { get; init; } = string.Empty;
    [JsonPropertyName("sourceId")] public string SourceId { get; init; } = string.Empty;
    [JsonPropertyName("sourceNumber")] public string SourceNumber { get; init; } = string.Empty;
    [JsonPropertyName("reconciliationStatus")] public string ReconciliationStatus { get; init; } = string.Empty;
    [JsonPropertyName("details")] public IReadOnlyDictionary<string, JsonElement> Details { get; init; } = new Dictionary<string, JsonElement>();
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
}

public sealed record SalesSettlementReconciliationData
{
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("filters")] public SalesSettlementFiltersData Filters { get; init; } = new();
    [JsonPropertyName("summary")] public SalesSettlementSummaryData Summary { get; init; } = new();
    [JsonPropertyName("customers")] public SalesSettlementCustomerData[] Customers { get; init; } = [];
    [JsonPropertyName("documents")] public SalesSettlementDocumentData[] Documents { get; init; } = [];
    [JsonPropertyName("orders")] public SalesSettlementOrderData[] Orders { get; init; } = [];
    [JsonPropertyName("codCollections")] public SalesSettlementCodCollectionData[] CodCollections { get; init; } = [];
    [JsonPropertyName("codHandovers")] public SalesSettlementCodHandoverData[] CodHandovers { get; init; } = [];
    [JsonPropertyName("anomalies")] public SalesSettlementAnomalyData[] Anomalies { get; init; } = [];
}
