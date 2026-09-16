using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record BranchData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("address")] public string? Address { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record WarehouseData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string BranchId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_type")] public string WarehouseType { get; init; } = string.Empty;
    [JsonPropertyName("location_management_mode")] public string? LocationManagementMode { get; init; }
    [JsonPropertyName("allow_negative_stock")] public bool AllowNegativeStock { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record WarehouseLocationData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("warehouse_id")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("location_type")] public string LocationType { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record EmployeeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("full_name")] public string FullName { get; init; } = string.Empty;
    [JsonPropertyName("job_title")] public string? JobTitle { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record InternalOrganizationSnapshot(
    IReadOnlyList<BranchData> Branches,
    IReadOnlyList<WarehouseData> Warehouses,
    IReadOnlyList<WarehouseLocationData> Locations,
    IReadOnlyList<EmployeeData> Employees);

public sealed record BranchCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email);

public sealed record BranchUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record WarehouseCreateRequest(
    [property: JsonPropertyName("branchId")] string BranchId,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("warehouseType")] string WarehouseType,
    [property: JsonPropertyName("allowNegativeStock")] bool AllowNegativeStock);

public sealed record WarehouseUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("warehouseType")] string WarehouseType,
    [property: JsonPropertyName("allowNegativeStock")] bool AllowNegativeStock,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record WarehouseLocationCreateRequest(
    [property: JsonPropertyName("warehouseId")] string WarehouseId,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("locationType")] string LocationType);

public sealed record WarehouseLocationUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("locationType")] string LocationType,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record EmployeeCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("jobTitle")] string? JobTitle,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("branchId")] string? BranchId);

public sealed record EmployeeUpdateRequest(
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("jobTitle")] string? JobTitle,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("branchId")] string? BranchId,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record ActiveStatusUpdateRequest(
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record WarehouseLocationModeSummary
{
    [JsonPropertyName("affectedSkuCount")] public int AffectedSkuCount { get; init; }
    [JsonPropertyName("affectedScopeCount")] public int AffectedScopeCount { get; init; }
    [JsonPropertyName("totalBaseQuantity")] public string TotalBaseQuantity { get; init; } = "0";
    [JsonPropertyName("relocatedReservationCount")] public int RelocatedReservationCount { get; init; }
}

public sealed record WarehouseLocationModeDestination
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
}

public sealed record WarehouseLocationModeBlocker
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string? Message { get; init; }
}

public sealed record WarehouseLocationModePreviewData
{
    [JsonPropertyName("targetMode")] public string TargetMode { get; init; } = string.Empty;
    [JsonPropertyName("destinationLocation")] public WarehouseLocationModeDestination? DestinationLocation { get; init; }
    [JsonPropertyName("summary")] public WarehouseLocationModeSummary Summary { get; init; } = new();
    [JsonPropertyName("blockers")] public WarehouseLocationModeBlocker[] Blockers { get; init; } = [];
    [JsonPropertyName("canConvert")] public bool CanConvert { get; init; }
    [JsonPropertyName("previewHash")] public string PreviewHash { get; init; } = string.Empty;
}

public sealed record WarehouseLocationModeConvertRequest(
    [property: JsonPropertyName("targetMode")] string TargetMode,
    [property: JsonPropertyName("destinationLocationId")] string? DestinationLocationId,
    [property: JsonPropertyName("previewHash")] string PreviewHash);

public sealed record WarehouseLocationModeRunLine
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("lotCode")] public string? LotCode { get; init; }
    [JsonPropertyName("sourceLocationId")] public string? SourceLocationId { get; init; }
    [JsonPropertyName("sourceLocationCode")] public string? SourceLocationCode { get; init; }
    [JsonPropertyName("sourceLocationName")] public string? SourceLocationName { get; init; }
    [JsonPropertyName("destinationLocationId")] public string? DestinationLocationId { get; init; }
    [JsonPropertyName("destinationLocationCode")] public string? DestinationLocationCode { get; init; }
    [JsonPropertyName("destinationLocationName")] public string? DestinationLocationName { get; init; }
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = "0";
}

public sealed record WarehouseLocationModeRun
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string WarehouseCode { get; init; } = string.Empty;
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("fromMode")] public string? FromMode { get; init; }
    [JsonPropertyName("targetMode")] public string TargetMode { get; init; } = string.Empty;
    [JsonPropertyName("destinationLocationCode")] public string? DestinationLocationCode { get; init; }
    [JsonPropertyName("destinationLocationName")] public string? DestinationLocationName { get; init; }
    [JsonPropertyName("affectedSkuCount")] public int AffectedSkuCount { get; init; }
    [JsonPropertyName("affectedScopeCount")] public int AffectedScopeCount { get; init; }
    [JsonPropertyName("totalBaseQuantity")] public string TotalBaseQuantity { get; init; } = "0";
    [JsonPropertyName("completedAt")] public string CompletedAt { get; init; } = string.Empty;
    [JsonPropertyName("completedBy")] public string CompletedBy { get; init; } = string.Empty;
    [JsonPropertyName("lines")] public WarehouseLocationModeRunLine[]? Lines { get; init; }
}
