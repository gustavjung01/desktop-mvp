using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record ImportExportHistoryRowData
{
    [JsonPropertyName("jobId")] public string JobId { get; init; } = string.Empty;
    [JsonPropertyName("direction")] public string Direction { get; init; } = string.Empty;
    [JsonPropertyName("definitionKey")] public string DefinitionKey { get; init; } = string.Empty;
    [JsonPropertyName("definitionVersion")] public string DefinitionVersion { get; init; } = string.Empty;
    [JsonPropertyName("format")] public string Format { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("actorId")] public string ActorId { get; init; } = string.Empty;
    [JsonPropertyName("employeeId")] public string? EmployeeId { get; init; }
    [JsonPropertyName("sourceApp")] public string SourceApp { get; init; } = string.Empty;
    [JsonPropertyName("requestId")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("rowCount")] public string? RowCount { get; init; }
    [JsonPropertyName("hasResult")] public bool HasResult { get; init; }
    [JsonPropertyName("failureCode")] public string? FailureCode { get; init; }
    [JsonPropertyName("requestedAt")] public string RequestedAt { get; init; } = string.Empty;
    [JsonPropertyName("startedAt")] public string? StartedAt { get; init; }
    [JsonPropertyName("completedAt")] public string? CompletedAt { get; init; }
}

public sealed record ImportExportHistoryPageData
{
    [JsonPropertyName("hasMore")] public bool HasMore { get; init; }
    [JsonPropertyName("nextCursor")] public string? NextCursor { get; init; }
}

public sealed record ImportExportHistoryData
{
    [JsonPropertyName("generatedAt")] public string GeneratedAt { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = string.Empty;
    [JsonPropertyName("rows")] public ImportExportHistoryRowData[] Rows { get; init; } = [];
    [JsonPropertyName("page")] public ImportExportHistoryPageData Page { get; init; } = new();
}
