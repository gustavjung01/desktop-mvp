using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record DeliveryAttemptTripSummaryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string Number { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
}

public sealed record DeliveryAttemptData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("tripId")] public string TripId { get; init; } = string.Empty;
    [JsonPropertyName("stopId")] public string StopId { get; init; } = string.Empty;
    [JsonPropertyName("stopSequence")] public int StopSequence { get; init; }
    [JsonPropertyName("assignmentId")] public string AssignmentId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderId")] public string DeliveryOrderId { get; init; } = string.Empty;
    [JsonPropertyName("deliveryOrderNumber")] public string? DeliveryOrderNumber { get; init; }
    [JsonPropertyName("customerCode")] public string? CustomerCode { get; init; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; init; }
    [JsonPropertyName("driverProfileId")] public string DriverProfileId { get; init; } = string.Empty;
    [JsonPropertyName("result")] public string Result { get; init; } = string.Empty;
    [JsonPropertyName("attemptedAt")] public string AttemptedAt { get; init; } = string.Empty;
    [JsonPropertyName("reasonCode")] public string? ReasonCode { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("rescheduledFor")] public string? RescheduledFor { get; init; }
}

public sealed record DeliveryAttemptSummaryData
{
    [JsonPropertyName("trip")] public DeliveryAttemptTripSummaryData Trip { get; init; } = new();
    [JsonPropertyName("attempts")] public DeliveryAttemptData[] Attempts { get; init; } = [];
}

public sealed record DeliveryProofFileData
{
    [JsonPropertyName("fileName")] public string FileName { get; init; } = string.Empty;
    [JsonPropertyName("contentType")] public string? ContentType { get; init; }
    [JsonPropertyName("byteSize")] public long? ByteSize { get; init; }
    [JsonPropertyName("checksumSha256")] public string? ChecksumSha256 { get; init; }
    [JsonPropertyName("downloadUrl")] public string? DownloadUrl { get; init; }
    [JsonPropertyName("downloadExpiresIn")] public int? DownloadExpiresIn { get; init; }
}

public sealed record DeliveryProofData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("podType")] public string PodType { get; init; } = string.Empty;
    [JsonPropertyName("receiverName")] public string? ReceiverName { get; init; }
    [JsonPropertyName("confirmationReference")] public string? ConfirmationReference { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("capturedAt")] public string CapturedAt { get; init; } = string.Empty;
    [JsonPropertyName("file")] public DeliveryProofFileData? File { get; init; }
}

public sealed record DeliveryProofListData
{
    [JsonPropertyName("proofs")] public DeliveryProofData[] Proofs { get; init; } = [];
}
