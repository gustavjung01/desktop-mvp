using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed class ManagementProposalData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("source")] public string Source { get; init; } = string.Empty;
    [JsonPropertyName("domain")] public string Domain { get; init; } = string.Empty;
    [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
    [JsonPropertyName("content")] public string Content { get; init; } = string.Empty;
    [JsonPropertyName("entityType")] public string EntityType { get; init; } = string.Empty;
    [JsonPropertyName("entityId")] public string EntityId { get; init; } = string.Empty;
    [JsonPropertyName("entityLabel")] public string EntityLabel { get; init; } = string.Empty;
    [JsonPropertyName("impact")] public string Impact { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("rule")] public string Rule { get; init; } = string.Empty;
    [JsonPropertyName("evidence")] public string[] Evidence { get; init; } = [];
    [JsonPropertyName("priority")] public string Priority { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("requesterName")] public string RequesterName { get; init; } = string.Empty;
    [JsonPropertyName("decisionNote")] public string? DecisionNote { get; init; }
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("decidedAt")] public string? DecidedAt { get; init; }
}

public sealed class ManagementProposalListData
{
    [JsonPropertyName("proposals")]
    public ManagementProposalData[] Proposals { get; init; } = [];
}

public sealed record ManagementProposalCreateRequest(
    string Domain,
    string Title,
    string Content,
    string EntityType,
    string EntityId,
    string EntityLabel,
    string Impact,
    string Reason,
    string Rule,
    IReadOnlyList<string> Evidence,
    string Priority);

public sealed record ManagementProposalResubmitRequest(
    string Content,
    string Reason,
    IReadOnlyList<string> Evidence);
