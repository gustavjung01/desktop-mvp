using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record CustomerOrderingHomeContentData
{
    [JsonPropertyName("sectionTitle")] public string SectionTitle { get; init; } = "Sự kiện";
    [JsonPropertyName("programContent")] public string ProgramContent { get; init; } = string.Empty;
    [JsonPropertyName("visible")] public bool Visible { get; init; }
    [JsonPropertyName("bannerUrl")] public string? BannerUrl { get; init; }
    [JsonPropertyName("imagePresent")] public bool ImagePresent { get; init; }
    [JsonPropertyName("updatedAt")] public string? UpdatedAt { get; init; }
}

public sealed record CustomerOrderingHomeContentEnvelopeData
{
    [JsonPropertyName("content")] public CustomerOrderingHomeContentData Content { get; init; } = new();
}

public sealed record CustomerOrderingHomeContentUpdateRequest
{
    [JsonPropertyName("sectionTitle")] public string SectionTitle { get; init; } = string.Empty;
    [JsonPropertyName("programContent")] public string ProgramContent { get; init; } = string.Empty;
    [JsonPropertyName("visible")] public bool Visible { get; init; }
}
