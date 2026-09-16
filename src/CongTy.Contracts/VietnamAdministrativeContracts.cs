using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record VietnamProvinceData
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("shortName")] public string ShortName { get; init; } = string.Empty;
    [JsonPropertyName("placeType")] public string PlaceType { get; init; } = string.Empty;
}

public sealed record VietnamWardData
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
}

public sealed record VietnamAdministrativeReferenceData
{
    [JsonPropertyName("provinces")] public VietnamProvinceData[] Provinces { get; init; } = [];
    [JsonPropertyName("wards")] public VietnamWardData[] Wards { get; init; } = [];
}
