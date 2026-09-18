using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record AccessUserData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string? EmployeeId { get; init; }
    [JsonPropertyName("employee_code")] public string? EmployeeCode { get; init; }
    [JsonPropertyName("employee_full_name")] public string? EmployeeFullName { get; init; }
    [JsonPropertyName("login_name")] public string LoginName { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
    [JsonPropertyName("role_ids")] public string[] RoleIds { get; init; } = [];
    [JsonPropertyName("branch_ids")] public string[] BranchIds { get; init; } = [];
    [JsonPropertyName("warehouse_ids")] public string[] WarehouseIds { get; init; } = [];
    [JsonPropertyName("owner_kind")] public string? OwnerKind { get; init; }
}
