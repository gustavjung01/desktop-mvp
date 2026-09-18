using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record UserScopeBranchData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record UserScopeWarehouseData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string BranchId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record UserScopeSet(
    [property: JsonPropertyName("branchIds")] string[] BranchIds,
    [property: JsonPropertyName("warehouseIds")] string[] WarehouseIds,
    [property: JsonPropertyName("territoryIds")] string[] TerritoryIds);

public sealed record UserScopeReplaceRequest(
    [property: JsonPropertyName("scopes")] UserScopeSet Scopes);

public sealed record UserScopeReplaceResult
{
    [JsonPropertyName("userId")] public string UserId { get; init; } = string.Empty;
    [JsonPropertyName("scopes")] public UserScopeSet Scopes { get; init; } = new([], [], []);
}
