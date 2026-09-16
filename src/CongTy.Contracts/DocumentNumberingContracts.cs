using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record DocumentNumberSeriesData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("document_type")] public string DocumentType { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("prefix")] public string Prefix { get; init; } = string.Empty;
    [JsonPropertyName("number_template")] public string NumberTemplate { get; init; } = string.Empty;
    [JsonPropertyName("reset_policy")] public string ResetPolicy { get; init; } = string.Empty;
    [JsonPropertyName("sequence_width")] public int SequenceWidth { get; init; }
    [JsonPropertyName("start_counter")] public string StartCounter { get; init; } = string.Empty;
    [JsonPropertyName("timezone_name")] public string TimezoneName { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("format_locked")] public bool FormatLocked { get; init; }
    [JsonPropertyName("allocation_count")] public int AllocationCount { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record DocumentNumberCounterData
{
    [JsonPropertyName("period_key")] public string PeriodKey { get; init; } = string.Empty;
    [JsonPropertyName("next_counter")] public string NextCounter { get; init; } = string.Empty;
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record DocumentNumberAllocationData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("series_id")] public string SeriesId { get; init; } = string.Empty;
    [JsonPropertyName("series_code")] public string SeriesCode { get; init; } = string.Empty;
    [JsonPropertyName("document_type")] public string DocumentType { get; init; } = string.Empty;
    [JsonPropertyName("document_date")] public string DocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("period_key")] public string PeriodKey { get; init; } = string.Empty;
    [JsonPropertyName("counter_value")] public string CounterValue { get; init; } = string.Empty;
    [JsonPropertyName("document_number")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("allocated_at")] public string AllocatedAt { get; init; } = string.Empty;
    [JsonPropertyName("metadata")] public Dictionary<string, JsonElement> Metadata { get; init; } = [];
    [JsonPropertyName("replayed")] public bool Replayed { get; init; }
}

public sealed record DocumentNumberHistoryData
{
    [JsonPropertyName("allocations")] public DocumentNumberAllocationData[] Allocations { get; init; } = [];
    [JsonPropertyName("counters")] public DocumentNumberCounterData[] Counters { get; init; } = [];
}

public sealed record DocumentNumberSeriesCreateRequest(
    [property: JsonPropertyName("documentType")] string DocumentType,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("prefix")] string Prefix,
    [property: JsonPropertyName("numberTemplate")] string NumberTemplate,
    [property: JsonPropertyName("resetPolicy")] string ResetPolicy);

public sealed record DocumentNumberSeriesUpdateRequest(
    [property: JsonPropertyName("documentType")] string DocumentType,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("prefix")] string Prefix,
    [property: JsonPropertyName("numberTemplate")] string NumberTemplate,
    [property: JsonPropertyName("resetPolicy")] string ResetPolicy,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record DocumentNumberSeriesStatusRequest(
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record DocumentNumberAllocationRequest(
    [property: JsonPropertyName("documentDate")] string DocumentDate,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string> Metadata);
