using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record AuditHistoryRowData
{
    [JsonPropertyName("auditId")] public string AuditId { get; init; } = string.Empty;
    [JsonPropertyName("actorId")] public string ActorId { get; init; } = string.Empty;
    [JsonPropertyName("employeeId")] public string? EmployeeId { get; init; }
    [JsonPropertyName("sourceApp")] public string SourceApp { get; init; } = string.Empty;
    [JsonPropertyName("requestId")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("action")] public string Action { get; init; } = string.Empty;
    [JsonPropertyName("resourceType")] public string ResourceType { get; init; } = string.Empty;
    [JsonPropertyName("resourceId")] public string? ResourceId { get; init; }
    [JsonPropertyName("occurredAt")] public string OccurredAt { get; init; } = string.Empty;
    [JsonPropertyName("hasBeforeData")] public bool HasBeforeData { get; init; }
    [JsonPropertyName("hasAfterData")] public bool HasAfterData { get; init; }
    [JsonPropertyName("hasMetadata")] public bool HasMetadata { get; init; }
}

public sealed record AuditHistoryPageData
{
    [JsonPropertyName("hasMore")] public bool HasMore { get; init; }
    [JsonPropertyName("nextCursor")] public string? NextCursor { get; init; }
}

public sealed record AuditHistoryData
{
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = string.Empty;
    [JsonPropertyName("rows")] public AuditHistoryRowData[] Rows { get; init; } = [];
    [JsonPropertyName("page")] public AuditHistoryPageData Page { get; init; } = new();
}
