using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public sealed record HealthCheckResult(bool Live, bool Ready);
public sealed record ApiDownloadFile(byte[] Content, string ContentType, string FileName);

public sealed class CompanyApiClient(
    HttpClient httpClient,
    ICompanyEndpointProvider endpointProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HttpClient HttpClient { get; } = httpClient;

    public bool IsConfigured => endpointProvider.BaseUri is not null;

    public Task<T> GetDataAsync<T>(
        string relativePath,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendDataAsync<T>(HttpMethod.Get, relativePath, bearerToken, null, null, null, cancellationToken);

    public async Task<ApiDownloadFile> GetFileAsync(
        string relativePath,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, relativePath, bearerToken, null, null);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        using var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _ = await ReadCanonicalResponseAsync<object>(response, request, cancellationToken).ConfigureAwait(false);
            throw CreateProtocolException(
                response.StatusCode,
                request,
                "INVALID_DOWNLOAD_RESPONSE",
                "Không tải được tệp từ hệ thống.");
        }

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var rawFileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? "download.bin";
        var fileName = Path.GetFileName(rawFileName.Trim().Trim('"'));
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "download.bin";

        return new ApiDownloadFile(content, contentType, fileName);
    }

    public Task<TResponse> PostDataAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendDataAsync<TResponse>(HttpMethod.Post, relativePath, bearerToken, payload, null, null, cancellationToken);

    public Task<TResponse> PostDataAsync<TResponse>(
        string relativePath,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendDataAsync<TResponse>(HttpMethod.Post, relativePath, bearerToken, null, null, null, cancellationToken);

    public Task<TResponse> PostIdempotentDataAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        string idempotencyKey,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendDataAsync<TResponse>(
            HttpMethod.Post,
            relativePath,
            bearerToken,
            payload,
            idempotencyKey,
            null,
            cancellationToken);

    public Task<TResponse> PutDataAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendDataAsync<TResponse>(
            HttpMethod.Put,
            relativePath,
            bearerToken,
            payload,
            null,
            null,
            cancellationToken);

    public Task<TResponse> PutIdempotentDataAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        string idempotencyKey,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendDataAsync<TResponse>(
            HttpMethod.Put,
            relativePath,
            bearerToken,
            payload,
            idempotencyKey,
            null,
            cancellationToken);

    public Task<TResponse> PatchDataAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendDataAsync<TResponse>(
            HttpMethod.Patch,
            relativePath,
            bearerToken,
            payload,
            null,
            null,
            cancellationToken);

    public Task<TResponse> PatchIdempotentDataAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        string idempotencyKey,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendDataAsync<TResponse>(
            HttpMethod.Patch,
            relativePath,
            bearerToken,
            payload,
            idempotencyKey,
            null,
            cancellationToken);

    public Task<TResponse> PutBytesIdempotentDataAsync<TResponse>(
        string relativePath,
        byte[] payload,
        string contentType,
        string idempotencyKey,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendBytesDataAsync<TResponse>(
            HttpMethod.Put,
            relativePath,
            bearerToken,
            payload,
            contentType,
            idempotencyKey,
            cancellationToken);

    public Task<TResponse> DeleteIdempotentDataAsync<TResponse>(
        string relativePath,
        string idempotencyKey,
        string? bearerToken = null,
        CancellationToken cancellationToken = default) =>
        SendDataAsync<TResponse>(
            HttpMethod.Delete,
            relativePath,
            bearerToken,
            null,
            idempotencyKey,
            null,
            cancellationToken);

    public async Task<HttpStatusCode> GetStatusAsync(
        string relativePath,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, relativePath, bearerToken, null, null);
        using var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        return response.StatusCode;
    }

    public async Task<HealthCheckResult> ValidateHealthAsync(
        Uri baseUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseUri);

        var live = await SendDataAsync<HealthStatusData>(
            HttpMethod.Get,
            "/health/live",
            null,
            null,
            null,
            baseUri,
            cancellationToken).ConfigureAwait(false);

        var ready = await SendDataAsync<HealthStatusData>(
            HttpMethod.Get,
            "/health/ready",
            null,
            null,
            null,
            baseUri,
            cancellationToken).ConfigureAwait(false);

        return new HealthCheckResult(
            string.Equals(live.Status, "ok", StringComparison.Ordinal),
            string.Equals(ready.Status, "ready", StringComparison.Ordinal));
    }

    private async Task<T> SendDataAsync<T>(
        HttpMethod method,
        string relativePath,
        string? bearerToken,
        object? payload,
        string? idempotencyKey,
        Uri? baseUriOverride,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, relativePath, bearerToken, idempotencyKey, baseUriOverride);

        if (payload is not null)
        {
            request.Content = JsonContent.Create(
                payload,
                payload.GetType(),
                mediaType: null,
                options: JsonOptions);
        }

        using var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        return await ReadCanonicalResponseAsync<T>(response, request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> SendBytesDataAsync<T>(
        HttpMethod method,
        string relativePath,
        string? bearerToken,
        byte[] payload,
        string contentType,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length == 0)
        {
            throw new ArgumentException("Dữ liệu gửi lên không được rỗng.", nameof(payload));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Loại nội dung không hợp lệ.", nameof(contentType));
        }

        using var request = CreateRequest(method, relativePath, bearerToken, idempotencyKey, null);
        request.Content = new ByteArrayContent(payload);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType.Trim());

        using var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        return await ReadCanonicalResponseAsync<T>(response, request, cancellationToken).ConfigureAwait(false);
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string relativePath,
        string? bearerToken,
        string? idempotencyKey,
        Uri? baseUriOverride)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith("/", StringComparison.Ordinal))
        {
            throw new ArgumentException("Đường dẫn API phải bắt đầu bằng '/'.", nameof(relativePath));
        }

        var baseUri = baseUriOverride ?? endpointProvider.BaseUri;
        if (baseUri is null)
        {
            throw new InvalidOperationException("Kết nối Công Ty chưa được cấu hình.");
        }

        var request = new HttpRequestMessage(method, new Uri(baseUri, relativePath));
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey.Trim());
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static async Task<T> ReadCanonicalResponseAsync<T>(
        HttpResponseMessage response,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var envelope = await response.Content
                    .ReadFromJsonAsync<ApiSuccessEnvelope<T>>(JsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                if (envelope is null)
                {
                    throw CreateProtocolException(
                        response.StatusCode,
                        request,
                        "INVALID_SUCCESS_ENVELOPE",
                        "Phản hồi từ hệ thống không đúng định dạng.");
                }

                return envelope.Data;
            }
            catch (JsonException exception)
            {
                throw CreateProtocolException(
                    response.StatusCode,
                    request,
                    "INVALID_SUCCESS_ENVELOPE",
                    "Phản hồi từ hệ thống không đúng định dạng.",
                    exception);
            }
        }

        ApiErrorEnvelope? errorEnvelope = null;
        try
        {
            errorEnvelope = await response.Content
                .ReadFromJsonAsync<ApiErrorEnvelope>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
        }

        if (errorEnvelope is null)
        {
            throw CreateProtocolException(
                response.StatusCode,
                request,
                "INVALID_ERROR_ENVELOPE",
                "Hệ thống trả về lỗi nhưng nội dung phản hồi không đúng định dạng.");
        }

        throw new CanonicalApiException(
            response.StatusCode,
            errorEnvelope.Error.Code,
            errorEnvelope.Error.Message,
            errorEnvelope.RequestId,
            errorEnvelope.Error.Retryable,
            errorEnvelope.Error.Details);
    }

    private static CanonicalApiException CreateProtocolException(
        HttpStatusCode statusCode,
        HttpRequestMessage request,
        string code,
        string message,
        Exception? innerException = null)
    {
        request.Options.TryGetValue(RequestIdHandler.RequestIdOption, out var requestId);
        return new CanonicalApiException(
            statusCode,
            code,
            message,
            requestId ?? string.Empty,
            retryable: false,
            default,
            innerException);
    }
}
