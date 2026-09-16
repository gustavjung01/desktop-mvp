using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record CodWarehouseOptionData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
}

public sealed record CodReportFiltersData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
}

public sealed record CodCurrencyAmountData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("collectionCount")] public string CollectionCount { get; init; } = "0";
    [JsonPropertyName("custodyRemainingAmount")] public string CustodyRemainingAmount { get; init; } = "0";
}

public sealed record CodCustodyDriverData
{
    [JsonPropertyName("driverProfileId")] public string DriverProfileId { get; init; } = string.Empty;
    [JsonPropertyName("driverCode")] public string DriverCode { get; init; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("collectionCount")] public string CollectionCount { get; init; } = "0";
    [JsonPropertyName("custodyRemainingAmount")] public string CustodyRemainingAmount { get; init; } = "0";
    [JsonPropertyName("oldestCollectedAt")] public string OldestCollectedAt { get; init; } = string.Empty;
    [JsonPropertyName("oldestAgeDays")] public string OldestAgeDays { get; init; } = "0";
}

public sealed record CodOverduePromiseData
{
    [JsonPropertyName("collectionId")] public string CollectionId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("tripId")] public string TripId { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string TripNumber { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderId")] public string DeliveryOrderId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("driverProfileId")] public string DriverProfileId { get; init; } = string.Empty;
    [JsonPropertyName("driverCode")] public string DriverCode { get; init; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("expectedAmount")] public string ExpectedAmount { get; init; } = "0";
    [JsonPropertyName("reasonCode")] public string ReasonCode { get; init; } = string.Empty;
    [JsonPropertyName("promisedBy")] public string PromisedBy { get; init; } = string.Empty;
    [JsonPropertyName("dueAt")] public string DueAt { get; init; } = string.Empty;
    [JsonPropertyName("overdueDays")] public string OverdueDays { get; init; } = "0";
}

public sealed record CodHandoverQueueData
{
    [JsonPropertyName("handoverId")] public string HandoverId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("tripId")] public string TripId { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string TripNumber { get; init; } = string.Empty;
    [JsonPropertyName("driverProfileId")] public string DriverProfileId { get; init; } = string.Empty;
    [JsonPropertyName("driverCode")] public string DriverCode { get; init; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; init; }
    [JsonPropertyName("claimedAmount")] public string ClaimedAmount { get; init; } = "0";
    [JsonPropertyName("pendingAcceptanceAmount")] public string? PendingAcceptanceAmount { get; init; }
    [JsonPropertyName("acceptedAmount")] public string? AcceptedAmount { get; init; }
    [JsonPropertyName("handoverDifferenceAmount")] public string? HandoverDifferenceAmount { get; init; }
    [JsonPropertyName("varianceAmount")] public string? VarianceAmount { get; init; }
    [JsonPropertyName("handedOverAt")] public string HandedOverAt { get; init; } = string.Empty;
    [JsonPropertyName("acceptedAt")] public string? AcceptedAt { get; init; }
    [JsonPropertyName("projectionStatus")] public string ProjectionStatus { get; init; } = string.Empty;
}

public sealed record CodCollectionActivityData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("collectionMethod")] public string CollectionMethod { get; init; } = string.Empty;
    [JsonPropertyName("collectionStatus")] public string CollectionStatus { get; init; } = string.Empty;
    [JsonPropertyName("collectionCount")] public string CollectionCount { get; init; } = "0";
    [JsonPropertyName("expectedAmount")] public string ExpectedAmount { get; init; } = "0";
    [JsonPropertyName("receivedAmount")] public string ReceivedAmount { get; init; } = "0";
}

public sealed record CodHandoverActivityData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("handoverCount")] public string HandoverCount { get; init; } = "0";
    [JsonPropertyName("claimedAmount")] public string ClaimedAmount { get; init; } = "0";
    [JsonPropertyName("handoverDifferenceAmount")] public string HandoverDifferenceAmount { get; init; } = "0";
}

public sealed record CodAcceptanceActivityData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("acceptanceCount")] public string AcceptanceCount { get; init; } = "0";
    [JsonPropertyName("acceptedAmount")] public string AcceptedAmount { get; init; } = "0";
    [JsonPropertyName("varianceAmount")] public string VarianceAmount { get; init; } = "0";
}

public sealed record CodRecentCollectionData
{
    [JsonPropertyName("collectionId")] public string CollectionId { get; init; } = string.Empty;
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string TripNumber { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("driverCode")] public string DriverCode { get; init; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; init; } = string.Empty;
    [JsonPropertyName("collectionMethod")] public string CollectionMethod { get; init; } = string.Empty;
    [JsonPropertyName("collectionStatus")] public string CollectionStatus { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("expectedAmount")] public string ExpectedAmount { get; init; } = "0";
    [JsonPropertyName("receivedAmount")] public string ReceivedAmount { get; init; } = "0";
    [JsonPropertyName("handedOverAmount")] public string HandedOverAmount { get; init; } = "0";
    [JsonPropertyName("custodyRemainingAmount")] public string CustodyRemainingAmount { get; init; } = "0";
    [JsonPropertyName("collectedAt")] public string CollectedAt { get; init; } = string.Empty;
    [JsonPropertyName("lifecycleStatus")] public string LifecycleStatus { get; init; } = string.Empty;
    [JsonPropertyName("lifecycleMatches")] public bool LifecycleMatches { get; init; }
}

public sealed record CodRecentHandoverData
{
    [JsonPropertyName("handoverId")] public string HandoverId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string TripNumber { get; init; } = string.Empty;
    [JsonPropertyName("driverCode")] public string DriverCode { get; init; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; init; }
    [JsonPropertyName("currencyCount")] public string CurrencyCount { get; init; } = "0";
    [JsonPropertyName("expectedTotal")] public string ExpectedTotal { get; init; } = "0";
    [JsonPropertyName("claimedAmount")] public string ClaimedAmount { get; init; } = "0";
    [JsonPropertyName("pendingAcceptanceAmount")] public string PendingAcceptanceAmount { get; init; } = "0";
    [JsonPropertyName("acceptedAmount")] public string AcceptedAmount { get; init; } = "0";
    [JsonPropertyName("handoverDifferenceAmount")] public string HandoverDifferenceAmount { get; init; } = "0";
    [JsonPropertyName("varianceAmount")] public string VarianceAmount { get; init; } = "0";
    [JsonPropertyName("projectionStatus")] public string ProjectionStatus { get; init; } = string.Empty;
    [JsonPropertyName("handedOverAt")] public string HandedOverAt { get; init; } = string.Empty;
    [JsonPropertyName("acceptedAt")] public string? AcceptedAt { get; init; }
    [JsonPropertyName("lifecycleMatches")] public bool LifecycleMatches { get; init; }
}

public sealed record CodLifecycleExceptionData
{
    [JsonPropertyName("anomalyType")] public string AnomalyType { get; init; } = string.Empty;
    [JsonPropertyName("sourceId")] public string SourceId { get; init; } = string.Empty;
    [JsonPropertyName("sourceNumber")] public string SourceNumber { get; init; } = string.Empty;
    [JsonPropertyName("reconciliationStatus")] public string ReconciliationStatus { get; init; } = string.Empty;
    [JsonPropertyName("details")] public IReadOnlyDictionary<string, JsonElement> Details { get; init; } = new Dictionary<string, JsonElement>();
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
}

public sealed record CodCurrencyLineageExceptionData
{
    [JsonPropertyName("handoverId")] public string HandoverId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string TripNumber { get; init; } = string.Empty;
    [JsonPropertyName("driverCode")] public string DriverCode { get; init; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; init; } = string.Empty;
    [JsonPropertyName("currencyCount")] public string CurrencyCount { get; init; } = "0";
    [JsonPropertyName("handedOverAt")] public string HandedOverAt { get; init; } = string.Empty;
    [JsonPropertyName("projectionStatus")] public string ProjectionStatus { get; init; } = string.Empty;
}

public sealed record CodCurrentSnapshotData
{
    [JsonPropertyName("custodyByCurrency")] public CodCurrencyAmountData[] CustodyByCurrency { get; init; } = [];
    [JsonPropertyName("custodyByDriver")] public CodCustodyDriverData[] CustodyByDriver { get; init; } = [];
    [JsonPropertyName("overduePromises")] public CodOverduePromiseData[] OverduePromises { get; init; } = [];
    [JsonPropertyName("pendingHandovers")] public CodHandoverQueueData[] PendingHandovers { get; init; } = [];
    [JsonPropertyName("discrepancies")] public CodHandoverQueueData[] Discrepancies { get; init; } = [];
}

public sealed record CodActivityData
{
    [JsonPropertyName("collections")] public CodCollectionActivityData[] Collections { get; init; } = [];
    [JsonPropertyName("handovers")] public CodHandoverActivityData[] Handovers { get; init; } = [];
    [JsonPropertyName("acceptances")] public CodAcceptanceActivityData[] Acceptances { get; init; } = [];
    [JsonPropertyName("recentCollections")] public CodRecentCollectionData[] RecentCollections { get; init; } = [];
    [JsonPropertyName("recentHandovers")] public CodRecentHandoverData[] RecentHandovers { get; init; } = [];
}

public sealed record CodExceptionData
{
    [JsonPropertyName("lifecycle")] public CodLifecycleExceptionData[] Lifecycle { get; init; } = [];
    [JsonPropertyName("currencyLineage")] public CodCurrencyLineageExceptionData[] CurrencyLineage { get; init; } = [];
}

public sealed record CodReportingDashboardData
{
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("businessTimezone")] public string BusinessTimezone { get; init; } = string.Empty;
    [JsonPropertyName("filters")] public CodReportFiltersData Filters { get; init; } = new();
    [JsonPropertyName("warehouses")] public CodWarehouseOptionData[] Warehouses { get; init; } = [];
    [JsonPropertyName("currentSnapshot")] public CodCurrentSnapshotData CurrentSnapshot { get; init; } = new();
    [JsonPropertyName("activity")] public CodActivityData Activity { get; init; } = new();
    [JsonPropertyName("exceptions")] public CodExceptionData Exceptions { get; init; } = new();
}

public sealed record CodHandoverLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("collectionId")] public string CollectionId { get; init; } = string.Empty;
    [JsonPropertyName("expectedAmount")] public string ExpectedAmount { get; init; } = "0";
    [JsonPropertyName("handedOverAmount")] public string HandedOverAmount { get; init; } = "0";
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("paymentDocumentId")] public string? PaymentDocumentId { get; init; }
}

public sealed record CodAcceptanceData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("acceptedAmount")] public string AcceptedAmount { get; init; } = "0";
    [JsonPropertyName("differenceAmount")] public string DifferenceAmount { get; init; } = "0";
    [JsonPropertyName("reconciliationStatus")] public string ReconciliationStatus { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("acceptedAt")] public string AcceptedAt { get; init; } = string.Empty;
    [JsonPropertyName("reversalId")] public string? ReversalId { get; init; }
}

public sealed record CodHandoverData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("tripId")] public string TripId { get; init; } = string.Empty;
    [JsonPropertyName("tripNumber")] public string? TripNumber { get; init; }
    [JsonPropertyName("driverProfileId")] public string DriverProfileId { get; init; } = string.Empty;
    [JsonPropertyName("driverCode")] public string? DriverCode { get; init; }
    [JsonPropertyName("driverName")] public string? DriverName { get; init; }
    [JsonPropertyName("expectedTotal")] public string ExpectedTotal { get; init; } = "0";
    [JsonPropertyName("handedOverTotal")] public string HandedOverTotal { get; init; } = "0";
    [JsonPropertyName("unattributedExcessAmount")] public string UnattributedExcessAmount { get; init; } = "0";
    [JsonPropertyName("differenceAmount")] public string DifferenceAmount { get; init; } = "0";
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("handedOverAt")] public string HandedOverAt { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("reversalId")] public string? ReversalId { get; init; }
    [JsonPropertyName("reversalReason")] public string? ReversalReason { get; init; }
    [JsonPropertyName("reversedAt")] public string? ReversedAt { get; init; }
    [JsonPropertyName("acceptance")] public CodAcceptanceData? Acceptance { get; init; }
    [JsonPropertyName("lines")] public CodHandoverLineData[] Lines { get; init; } = [];
}

public sealed record CodAcceptRequest(
    [property: JsonPropertyName("acceptedAmount")] string AcceptedAmount,
    [property: JsonPropertyName("acceptedAt")] string AcceptedAt,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("note")] string? Note);

public sealed record CodReversalRequest(
    [property: JsonPropertyName("reason")] string Reason);

public sealed record CodMutationResultData
{
    [JsonPropertyName("ok")] public bool Ok { get; init; }
    [JsonPropertyName("replayed")] public bool Replayed { get; init; }
    [JsonPropertyName("handover")] public CodHandoverData? Handover { get; init; }
    [JsonPropertyName("acceptance")] public JsonElement? Acceptance { get; init; }
    [JsonPropertyName("collection")] public JsonElement? Collection { get; init; }
}
