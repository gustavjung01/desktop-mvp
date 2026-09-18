using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record AccessPermissionData
{
    [JsonPropertyName("permission_key")] public string PermissionKey { get; init; } = string.Empty;
    [JsonPropertyName("module")] public string Module { get; init; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; init; } = string.Empty;
    [JsonPropertyName("is_system")] public bool IsSystem { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
}

public sealed record AccessRoleData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("web_login_challenge_required")] public bool WebLoginChallengeRequired { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
    [JsonPropertyName("permission_keys")] public string[] PermissionKeys { get; init; } = [];
}

public sealed record AccessRoleCreateRequest(
    string Code,
    string Name,
    string Description,
    bool IsActive,
    bool WebLoginChallengeRequired,
    string[] PermissionKeys);

public sealed record AccessRoleUpdateRequest(
    string Name,
    string Description,
    bool IsActive,
    bool WebLoginChallengeRequired,
    string[] PermissionKeys,
    string ExpectedUpdatedAt);

public sealed record AccessRoleToggleRequest(
    bool IsActive,
    string ExpectedUpdatedAt);
