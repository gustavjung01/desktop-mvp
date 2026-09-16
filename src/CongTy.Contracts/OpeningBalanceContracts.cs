using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record OpeningBalanceWarehouseData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("locationManagementMode")] public string? LocationManagementMode { get; init; }
    [JsonPropertyName("locationRequired")] public bool LocationRequired { get; init; }
}

public sealed record OpeningBalanceLocationData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("locationType")] public string LocationType { get; init; } = string.Empty;
}

public sealed record OpeningBalanceLocationEnvelopeData
{
    [JsonPropertyName("warehouse")] public OpeningBalanceWarehouseData Warehouse { get; init; } = new();
    [JsonPropertyName("locations")] public OpeningBalanceLocationData[] Locations { get; init; } = [];
}

public sealed record OpeningBalanceDraftRowData
{
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("sourceQuantity")] public string SourceQuantity { get; init; } = string.Empty;
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("manufacturedDate")] public string? ManufacturedDate { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
    [JsonPropertyName("supplierLotReference")] public string? SupplierLotReference { get; init; }
    [JsonPropertyName("sourceLineReference")] public string? SourceLineReference { get; init; }
    [JsonPropertyName("metadata")] public Dictionary<string, object?> Metadata { get; init; } = [];
}

public sealed record OpeningBalanceOperatorMetadataData
{
    [JsonPropertyName("importMethod")] public string ImportMethod { get; init; } = "csv-upload-operator";
    [JsonPropertyName("originalFilename")] public string OriginalFilename { get; init; } = string.Empty;
    [JsonPropertyName("defaultLocationCode")] public string? DefaultLocationCode { get; init; }
}

public sealed record OpeningBalanceOperatorDraftData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("sourceKey")] public string SourceKey { get; init; } = string.Empty;
    [JsonPropertyName("sourceFilename")] public string? SourceFilename { get; init; }
    [JsonPropertyName("documentDate")] public string DocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("metadata")] public OpeningBalanceOperatorMetadataData Metadata { get; init; } = new();
    [JsonPropertyName("rows")] public OpeningBalanceDraftRowData[] Rows { get; init; } = [];
}

public sealed record OpeningBalanceOperatorRequestData
{
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("sourceKey")] public string SourceKey { get; init; } = string.Empty;
    [JsonPropertyName("sourceFilename")] public string? SourceFilename { get; init; }
    [JsonPropertyName("documentDate")] public string DocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("metadata")] public OpeningBalanceOperatorMetadataData Metadata { get; init; } = new();
    [JsonPropertyName("rows")] public OpeningBalanceDraftRowData[] Rows { get; init; } = [];
    [JsonPropertyName("contentChecksum")] public string ContentChecksum { get; init; } = string.Empty;
}

public sealed record OpeningBalanceValidationErrorData
{
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}

public sealed record OpeningBalanceValidationRowData
{
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("warehouseId")] public string? WarehouseId { get; init; }
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("locationManagementMode")] public string? LocationManagementMode { get; init; }
    [JsonPropertyName("locationRequired")] public bool? LocationRequired { get; init; }
    [JsonPropertyName("locationId")] public string? LocationId { get; init; }
    [JsonPropertyName("locationCode")] public string? LocationCode { get; init; }
    [JsonPropertyName("locationName")] public string? LocationName { get; init; }
    [JsonPropertyName("sourceVariantId")] public string? SourceVariantId { get; init; }
    [JsonPropertyName("sourceSku")] public string? SourceSku { get; init; }
    [JsonPropertyName("sourceUnitId")] public string? SourceUnitId { get; init; }
    [JsonPropertyName("sourceUnitCode")] public string? SourceUnitCode { get; init; }
    [JsonPropertyName("sourceQuantity")] public string? SourceQuantity { get; init; }
    [JsonPropertyName("productCode")] public string? ProductCode { get; init; }
    [JsonPropertyName("productName")] public string? ProductName { get; init; }
    [JsonPropertyName("baseVariantId")] public string? BaseVariantId { get; init; }
    [JsonPropertyName("baseSku")] public string? BaseSku { get; init; }
    [JsonPropertyName("baseQuantity")] public string? BaseQuantity { get; init; }
    [JsonPropertyName("lotTrackingMode")] public string? LotTrackingMode { get; init; }
    [JsonPropertyName("expiryTrackingMode")] public string? ExpiryTrackingMode { get; init; }
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("manufacturedDate")] public string? ManufacturedDate { get; init; }
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; init; }
}

public sealed record OpeningBalanceValidationTotalsData
{
    [JsonPropertyName("rowCount")] public int RowCount { get; init; }
    [JsonPropertyName("sourceQuantityTotal")] public string SourceQuantityTotal { get; init; } = "0";
    [JsonPropertyName("baseQuantityTotal")] public string BaseQuantityTotal { get; init; } = "0";
}

public sealed record OpeningBalanceValidationResultData
{
    [JsonPropertyName("rowErrors")] public OpeningBalanceValidationErrorData[] RowErrors { get; init; } = [];
    [JsonPropertyName("rows")] public OpeningBalanceValidationRowData[] Rows { get; init; } = [];
    [JsonPropertyName("totals")] public OpeningBalanceValidationTotalsData Totals { get; init; } = new();
}

public sealed record OpeningBalanceImportData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("source_key")] public string SourceKey { get; init; } = string.Empty;
    [JsonPropertyName("source_filename")] public string? SourceFilename { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("document_date")] public string DocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("row_count")] public int RowCount { get; init; }
    [JsonPropertyName("source_quantity_total")] public string SourceQuantityTotal { get; init; } = "0";
    [JsonPropertyName("base_quantity_total")] public string BaseQuantityTotal { get; init; } = "0";
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("request_id")] public string RequestId { get; init; } = string.Empty;
}

public sealed record OpeningBalancePostResultData
{
    [JsonPropertyName("ok")] public bool Ok { get; init; }
    [JsonPropertyName("replayed")] public bool Replayed { get; init; }
    [JsonPropertyName("import")] public OpeningBalanceImportData? Import { get; init; }
    [JsonPropertyName("movement")] public JsonElement? Movement { get; init; }
    [JsonPropertyName("totals")] public OpeningBalanceValidationTotalsData Totals { get; init; } = new();
}
