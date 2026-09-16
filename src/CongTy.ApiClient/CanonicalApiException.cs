using System.Net;
using System.Text.Json;

namespace CongTy.ApiClient;

public sealed class CanonicalApiException : Exception
{
    public CanonicalApiException(
        HttpStatusCode statusCode,
        string code,
        string message,
        string requestId,
        bool retryable,
        JsonElement details,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Code = code;
        RequestId = requestId;
        Retryable = retryable;
        Details = details;
    }

    public HttpStatusCode StatusCode { get; }
    public string Code { get; }
    public string RequestId { get; }
    public bool Retryable { get; }
    public JsonElement Details { get; }
}
