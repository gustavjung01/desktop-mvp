using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record ManualInboundWarehouseOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("locationManagementMode")] public string? LocationManagementMode { get; init; }
    [JsonPropertyName("locationRequired")] public bool LocationRequired { get; init; }
}

public sealed record ManualInboundLocationOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("locationType")] public string LocationType { get; init; } = string.Empty;
}

public sealed record ManualInboundLocationResponseData
{
    [JsonPropertyName("warehouse")] public ManualInboundWarehouseOptionData Warehouse { get; init; } = new();
    [JsonPropertyName("locations")] public ManualInboundLocationOptionData[] Locations { get; init; } = [];
}

public sealed record ManualInboundSupplierOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
}

public sealed record ManualInboundProductOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("productId")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("variantName")] public string? VariantName { get; init; }
    [JsonPropertyName("productCode")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("unitName")] public string UnitName { get; init; } = string.Empty;
    [JsonPropertyName("allowsFractional")] public bool AllowsFractional { get; init; }
    [JsonPropertyName("conversionToBase")] public string? ConversionToBase { get; init; }
    [JsonPropertyName("baseVariantId")] public string? BaseVariantId { get; init; }
    [JsonPropertyName("baseSku")] public string? BaseSku { get; init; }
    [JsonPropertyName("lotTrackingMode")] public string? LotTrackingMode { get; init; }
    [JsonPropertyName("expiryTrackingMode")] public string? ExpiryTrackingMode { get; init; }
    [JsonPropertyName("primaryBarcode")] public string? PrimaryBarcode { get; init; }
    [JsonPropertyName("unitCost")] public string? UnitCost { get; init; }
    [JsonPropertyName("locationManagementMode")] public string LocationManagementMode { get; init; } = string.Empty;
    [JsonPropertyName("locationRequired")] public bool LocationRequired { get; init; }
}

public sealed record ManualInboundDraftRowRequest(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("sourceQuantity")] string SourceQuantity,
    [property: JsonPropertyName("unitCost")] string? UnitCost,
    [property: JsonPropertyName("locationCode")] string? LocationCode,
    [property: JsonPropertyName("lotCode")] string? LotCode,
    [property: JsonPropertyName("manufacturedDate")] string? ManufacturedDate,
    [property: JsonPropertyName("expiryDate")] string? ExpiryDate,
    [property: JsonPropertyName("supplierLotReference")] string? SupplierLotReference);

public sealed record ManualInboundOperatorRequest(
    [property: JsonPropertyName("warehouseId")] string WarehouseId,
    [property: JsonPropertyName("supplierId")] string? SupplierId,
    [property: JsonPropertyName("inboundType")] string InboundType,
    [property: JsonPropertyName("documentDate")] string DocumentDate,
    [property: JsonPropertyName("referenceNumber")] string? ReferenceNumber,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("rows")] ManualInboundDraftRowRequest[] Rows);

public sealed record ManualInboundPreviewErrorData
{
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}

public sealed record ManualInboundPreviewRowData
{
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sourceLineNumbers")] public int[] SourceLineNumbers { get; init; } = [];
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("sourceQuantity")] public string SourceQuantity { get; init; } = string.Empty;
    [JsonPropertyName("unitCost")] public string? UnitCost { get; init; }
    [JsonPropertyName("costSource")] public string? CostSource { get; init; }
    [JsonPropertyName("locationId")] public string? LocationId { get; init; }
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("locationName")] public string? LocationName { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("manufacturedDate")] public string? ManufacturedDate { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("supplierLotReference")] public string? SupplierLotReference { get; init; }
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("productName")] public string? ProductName { get; init; }
    [JsonPropertyName("sourceUnitCode")] public string? SourceUnitCode { get; init; }
    [JsonPropertyName("baseSku")] public string? BaseSku { get; init; }
    [JsonPropertyName("baseQuantity")] public string? BaseQuantity { get; init; }
    [JsonPropertyName("baseUnitCode")] public string? BaseUnitCode { get; init; }
    [JsonPropertyName("currentOnHand")] public string? CurrentOnHand { get; init; }
    [JsonPropertyName("afterOnHand")] public string? AfterOnHand { get; init; }
    [JsonPropertyName("lotTrackingMode")] public string? LotTrackingMode { get; init; }
    [JsonPropertyName("expiryTrackingMode")] public string? ExpiryTrackingMode { get; init; }
    [JsonPropertyName("locationRequired")] public bool LocationRequired { get; init; }
    [JsonPropertyName("requiredFields")] public string[] RequiredFields { get; init; } = [];
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
}

public sealed record ManualInboundPreviewTotalsData
{
    [JsonPropertyName("inputRowCount")] public int InputRowCount { get; init; }
    [JsonPropertyName("previewRowCount")] public int PreviewRowCount { get; init; }
    [JsonPropertyName("mergedDuplicateCount")] public int MergedDuplicateCount { get; init; }
    [JsonPropertyName("sourceQuantityTotal")] public string SourceQuantityTotal { get; init; } = "0";
    [JsonPropertyName("readyRowCount")] public int ReadyRowCount { get; init; }
    [JsonPropertyName("attentionRowCount")] public int AttentionRowCount { get; init; }
}

public sealed record ManualInboundPreviewData
{
    [JsonPropertyName("ready")] public bool Ready { get; init; }
    [JsonPropertyName("stockUnchanged")] public bool StockUnchanged { get; init; }
    [JsonPropertyName("rowErrors")] public ManualInboundPreviewErrorData[] RowErrors { get; init; } = [];
    [JsonPropertyName("rows")] public ManualInboundPreviewRowData[] Rows { get; init; } = [];
    [JsonPropertyName("totals")] public ManualInboundPreviewTotalsData Totals { get; init; } = new();
}

public sealed record ManualInboundHistoryDocumentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("inboundType")] public string InboundType { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("documentDate")] public string DocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("referenceNumber")] public string? ReferenceNumber { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("reversalDate")] public string? ReversalDate { get; init; }
    [JsonPropertyName("reversalNote")] public string? ReversalNote { get; init; }
}

public sealed record ManualInboundHistoryMovementLineData
{
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string? ProductName { get; init; }
    [JsonPropertyName("baseUnitCode")] public string? BaseUnitCode { get; init; }
    [JsonPropertyName("quantityBefore")] public string QuantityBefore { get; init; } = "0";
    [JsonPropertyName("quantityDelta")] public string QuantityDelta { get; init; } = "0";
    [JsonPropertyName("quantityAfter")] public string QuantityAfter { get; init; } = "0";
}

public sealed record ManualInboundHistoryMovementData
{
    [JsonPropertyName("documentId")] public string DocumentId { get; init; } = string.Empty;
    [JsonPropertyName("documentDate")] public string DocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("referenceNumber")] public string? ReferenceNumber { get; init; }
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("lines")] public ManualInboundHistoryMovementLineData[] Lines { get; init; } = [];
}

public sealed record ManualInboundReverseRequest(
    [property: JsonPropertyName("documentDate")] string DocumentDate,
    [property: JsonPropertyName("reasonNote")] string ReasonNote);
