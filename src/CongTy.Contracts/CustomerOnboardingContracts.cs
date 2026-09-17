using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record CustomerOnboardingAddressData
{
    [JsonPropertyName("addressLine1")] public string AddressLine1 { get; init; } = string.Empty;
    [JsonPropertyName("addressLine2")] public string? AddressLine2 { get; init; }
    [JsonPropertyName("ward")] public string? Ward { get; init; }
    [JsonPropertyName("district")] public string? District { get; init; }
    [JsonPropertyName("province")] public string? Province { get; init; }
    [JsonPropertyName("postalCode")] public string? PostalCode { get; init; }
    [JsonPropertyName("countryCode")] public string CountryCode { get; init; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; init; } = string.Empty;
}

public sealed record CustomerOnboardingProposedCustomerData
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("address")] public CustomerOnboardingAddressData Address { get; init; } = new();
}

public sealed record CustomerOnboardingRequestData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("sourceSystem")] public string SourceSystem { get; init; } = string.Empty;
    [JsonPropertyName("sourceOutletId")] public string SourceOutletId { get; init; } = string.Empty;
    [JsonPropertyName("sourceDemandReference")] public string SourceDemandReference { get; init; } = string.Empty;
    [JsonPropertyName("requestedByEmployeeId")] public string? RequestedByEmployeeId { get; init; }
    [JsonPropertyName("sourceMetadata")] public Dictionary<string, JsonElement>? SourceMetadata { get; init; }
    [JsonPropertyName("proposedCustomer")] public CustomerOnboardingProposedCustomerData ProposedCustomer { get; init; } = new();
    [JsonPropertyName("reviewReason")] public string? ReviewReason { get; init; }
    [JsonPropertyName("approvedCustomerId")] public string? ApprovedCustomerId { get; init; }
    [JsonPropertyName("approvedCustomerAddressId")] public string? ApprovedCustomerAddressId { get; init; }
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("submittedAt")] public string SubmittedAt { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record CustomerOnboardingListData
{
    [JsonPropertyName("customerOnboardingRequests")]
    public CustomerOnboardingRequestData[] CustomerOnboardingRequests { get; init; } = [];
}

public sealed record CustomerOnboardingMutationData
{
    [JsonPropertyName("customerOnboardingRequest")]
    public CustomerOnboardingRequestData CustomerOnboardingRequest { get; init; } = new();
}

public sealed record CustomerOnboardingPortalOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
}

public sealed record CustomerOnboardingPortalOptionsData
{
    [JsonPropertyName("warehouses")] public CustomerOnboardingPortalOptionData[] Warehouses { get; init; } = [];
    [JsonPropertyName("salesChannels")] public CustomerOnboardingPortalOptionData[] SalesChannels { get; init; } = [];
}
