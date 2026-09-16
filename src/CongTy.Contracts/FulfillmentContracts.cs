namespace CongTy.Contracts;

public sealed class FulfillmentWorkItemData
{
    public string FulfillmentDemandId { get; init; } = string.Empty;
    public string SalesOrderId { get; init; } = string.Empty;
    public string SalesOrderVersionId { get; init; } = string.Empty;
    public string SalesOrderLineId { get; init; } = string.Empty;
    public string? OrderNumber { get; init; }
    public string? OrderSubtotal { get; init; }
    public string? OrderDiscountTotal { get; init; }
    public string? OrderTaxTotal { get; init; }
    public string? OrderTotal { get; init; }
    public string? SalesChannelCode { get; init; }
    public string? SalesChannelName { get; init; }
    public string FulfillmentStatus { get; init; } = string.Empty;
    public string? RequestedDeliveryDate { get; init; }
    public string SourceType { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string WarehouseId { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string WarehouseName { get; init; } = string.Empty;
    public int LineNumber { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string UnitCode { get; init; } = string.Empty;
    public string OrderedQuantity { get; init; } = "0";
    public string OrderedUnitCode { get; init; } = string.Empty;
    public string BaseUnitCode { get; init; } = string.Empty;
    public string BaseVariantId { get; init; } = string.Empty;
    public string OrderedBaseQuantity { get; init; } = "0";
    public string ReservedBaseQuantity { get; init; } = "0";
    public string BackorderedBaseQuantity { get; init; } = "0";
    public string AllocatedBaseQuantity { get; init; } = "0";
    public string UnallocatedBaseQuantity { get; init; } = "0";
    public string WarehouseOnHandBaseQuantity { get; init; } = "0";
    public string WarehouseHeldByOthersBaseQuantity { get; init; } = "0";
    public string WarehouseAvailableBaseQuantity { get; init; } = "0";
    public string PickedBaseQuantity { get; init; } = "0";
    public string PackedBaseQuantity { get; init; } = "0";
    public int AllocationCount { get; init; }
}

public sealed class FulfillmentSuggestionData
{
    public string RemainingBaseQuantity { get; init; } = "0";
    public string HeldRemainingBaseQuantity { get; init; } = "0";
    public string WarehouseOnHandBaseQuantity { get; init; } = "0";
    public string WarehouseHeldByOthersBaseQuantity { get; init; } = "0";
    public string WarehouseAvailableBaseQuantity { get; init; } = "0";
    public FulfillmentCandidateData[] Candidates { get; init; } = [];
    public FulfillmentSuggestedPlanData[] SuggestedPlan { get; init; } = [];
    public FulfillmentAllocationData[] Allocations { get; init; } = [];
}

public sealed class FulfillmentCandidateData
{
    public int Rank { get; init; }
    public string? LocationId { get; init; }
    public string? LocationCode { get; init; }
    public string? LocationName { get; init; }
    public string? LotId { get; init; }
    public string? LotCode { get; init; }
    public string? ExpiryDate { get; init; }
    public string? FirstReceivedAt { get; init; }
    public string AvailableBaseQuantity { get; init; } = "0";
    public string AllocationPolicy { get; init; } = string.Empty;
}

public sealed class FulfillmentSuggestedPlanData
{
    public string? LocationId { get; init; }
    public string? LotId { get; init; }
    public string AllocationPolicy { get; init; } = string.Empty;
    public int PolicyRank { get; init; }
    public string Quantity { get; init; } = "0";
}

public sealed class FulfillmentAllocationData
{
    public string Id { get; init; } = string.Empty;
    public string? LocationId { get; init; }
    public string? LocationCode { get; init; }
    public string? LocationName { get; init; }
    public string? LotId { get; init; }
    public string? LotCode { get; init; }
    public string? ExpiryDate { get; init; }
    public string AllocationPolicy { get; init; } = string.Empty;
    public string? ManualOverrideReason { get; init; }
    public string AllocatedBaseQuantity { get; init; } = "0";
    public string PickedBaseQuantity { get; init; } = "0";
    public string PackedBaseQuantity { get; init; } = "0";
    public string State { get; init; } = string.Empty;
}

public sealed class FulfillmentMutationResultData
{
    public bool Ok { get; init; }
    public bool Replayed { get; init; }
}

public sealed class FulfillmentOrderAllocationResultData
{
    public bool Ok { get; init; }
    public bool Replayed { get; init; }
    public string SalesOrderId { get; init; } = string.Empty;
    public FulfillmentOrderAllocationSummaryData Summary { get; init; } = new();
    public FulfillmentOrderAllocationLineData[] Lines { get; init; } = [];
}

public sealed class FulfillmentOrderAllocationSummaryData
{
    public int TotalLines { get; init; }
    public int ReadyLines { get; init; }
    public int ShortageLines { get; init; }
    public int NeedsAttentionLines { get; init; }
}

public sealed class FulfillmentOrderAllocationLineData
{
    public string FulfillmentDemandId { get; init; } = string.Empty;
    public string SalesOrderLineId { get; init; } = string.Empty;
    public int LineNumber { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string UnitCode { get; init; } = string.Empty;
    public string OrderedBaseQuantity { get; init; } = "0";
    public string ReservedBaseQuantity { get; init; } = "0";
    public string AllocatedBaseQuantity { get; init; } = "0";
    public string RemainingToAllocateBaseQuantity { get; init; } = "0";
    public string ShortageBaseQuantity { get; init; } = "0";
    public string Outcome { get; init; } = string.Empty;
    public string? ReasonCode { get; init; }
    public string Message { get; init; } = string.Empty;
}
