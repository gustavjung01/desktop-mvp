using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record InventoryTrackingPolicyData
{
    [JsonPropertyName("installation_id")] public string? InstallationId { get; init; }
    [JsonPropertyName("base_variant_id")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("lot_tracking_mode")] public string LotTrackingMode { get; init; } = "NONE";
    [JsonPropertyName("expiry_tracking_mode")] public string ExpiryTrackingMode { get; init; } = "NONE";
    [JsonPropertyName("location_required")] public bool LocationRequired { get; init; }
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("created_at")] public string? CreatedAt { get; init; }
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_at")] public string? UpdatedAt { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
    [JsonPropertyName("base_sku")] public string? BaseSku { get; init; }
    [JsonPropertyName("base_variant_name")] public string? BaseVariantName { get; init; }
    [JsonPropertyName("base_variant_active")] public bool? BaseVariantActive { get; init; }
    [JsonPropertyName("is_inventory_base")] public bool? IsInventoryBase { get; init; }
    [JsonPropertyName("product_code")] public string? ProductCode { get; init; }
    [JsonPropertyName("product_name")] public string? ProductName { get; init; }
}

public sealed record InventoryTrackingPolicyCandidateData
{
    [JsonPropertyName("base_variant_id")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("base_sku")] public string BaseSku { get; init; } = string.Empty;
    [JsonPropertyName("base_variant_name")] public string? BaseVariantName { get; init; }
    [JsonPropertyName("base_variant_active")] public bool BaseVariantActive { get; init; }
    [JsonPropertyName("is_inventory_base")] public bool IsInventoryBase { get; init; }
    [JsonPropertyName("product_code")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("product_name")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("product_active")] public bool ProductActive { get; init; }
    [JsonPropertyName("has_policy")] public bool HasPolicy { get; init; }
    [JsonPropertyName("related_variant_search_text")] public string RelatedVariantSearchText { get; init; } = string.Empty;
}

public sealed record InventoryTrackingPolicySaveRequest
{
    [JsonPropertyName("baseVariantId")] public string BaseVariantId { get; init; } = string.Empty;
    [JsonPropertyName("lotTrackingMode")] public string LotTrackingMode { get; init; } = "NONE";
    [JsonPropertyName("expiryTrackingMode")] public string ExpiryTrackingMode { get; init; } = "NONE";
    [JsonPropertyName("expectedVersion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ExpectedVersion { get; init; }
}
