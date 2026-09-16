using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record ApiSuccessEnvelope<T>(
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("receivedAt")] string ReceivedAt);

public sealed record ApiErrorBody(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("details")] JsonElement Details,
    [property: JsonPropertyName("retryable")] bool Retryable);

public sealed record ApiErrorEnvelope(
    [property: JsonPropertyName("error")] ApiErrorBody Error,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("receivedAt")] string ReceivedAt);
