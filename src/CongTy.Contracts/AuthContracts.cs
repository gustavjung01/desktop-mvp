using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record HealthStatusData
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}

public sealed record AccessScopesData
{
    [JsonPropertyName("branchIds")]
    public string[] BranchIds { get; init; } = [];

    [JsonPropertyName("warehouseIds")]
    public string[] WarehouseIds { get; init; } = [];

    [JsonPropertyName("territoryIds")]
    public string[] TerritoryIds { get; init; } = [];
}

public sealed record InternalAuthSessionData
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("userId")]
    public string? UserId { get; init; }

    [JsonPropertyName("loginName")]
    public string? LoginName { get; init; }

    [JsonPropertyName("employeeFullName")]
    public string? EmployeeFullName { get; init; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("expiresAt")]
    public string ExpiresAt { get; init; } = string.Empty;

    [JsonPropertyName("sourceApp")]
    public string? SourceApp { get; init; }

    [JsonPropertyName("accessChannel")]
    public string? AccessChannel { get; init; }

    [JsonPropertyName("ownerKind")]
    public string? OwnerKind { get; init; }
}

public sealed record InternalAuthUserData
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("loginName")]
    public string LoginName { get; init; } = string.Empty;

    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; init; }

    [JsonPropertyName("employeeFullName")]
    public string? EmployeeFullName { get; init; }

    [JsonPropertyName("roles")]
    public string[] Roles { get; init; } = [];

    [JsonPropertyName("permissions")]
    public string[] Permissions { get; init; } = [];

    [JsonPropertyName("scopes")]
    public AccessScopesData Scopes { get; init; } = new();

    [JsonPropertyName("ownerKind")]
    public string? OwnerKind { get; init; }
}

public sealed record InternalLoginData
{
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;

    [JsonPropertyName("session")]
    public InternalAuthSessionData Session { get; init; } = new();

    [JsonPropertyName("user")]
    public InternalAuthUserData User { get; init; } = new();
}

public sealed record InternalMeData
{
    [JsonPropertyName("actorId")]
    public string ActorId { get; init; } = string.Empty;

    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; init; }

    [JsonPropertyName("roles")]
    public string[] Roles { get; init; } = [];

    [JsonPropertyName("permissions")]
    public string[] Permissions { get; init; } = [];

    [JsonPropertyName("scopes")]
    public AccessScopesData Scopes { get; init; } = new();

    [JsonPropertyName("sourceApp")]
    public string? SourceApp { get; init; }

    [JsonPropertyName("session")]
    public InternalAuthSessionData Session { get; init; } = new();
}

public sealed record InternalLogoutData
{
    [JsonPropertyName("loggedOut")]
    public bool LoggedOut { get; init; }

    [JsonPropertyName("revoked")]
    public bool Revoked { get; init; }
}

public sealed record InternalLoginRequest
{
    [JsonPropertyName("loginName")]
    public string LoginName { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("sourceApp")]
    public string SourceApp { get; init; } = string.Empty;

    [JsonPropertyName("ownerCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OwnerCode { get; init; }
}
