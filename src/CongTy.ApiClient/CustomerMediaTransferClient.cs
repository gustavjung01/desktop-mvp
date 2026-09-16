using System.Net.Http.Headers;

namespace CongTy.ApiClient;

public interface ICustomerMediaTransferClient
{
    Task UploadAsync(
        string putUrl,
        string mimeType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);

    Task<byte[]> DownloadAsync(
        string viewUrl,
        CancellationToken cancellationToken = default);
}

public sealed class CustomerMediaTransferClient(HttpClient httpClient) : ICustomerMediaTransferClient, IDisposable
{
    public async Task UploadAsync(
        string putUrl,
        string mimeType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        var uri = RequireHttpsSignedUrl(putUrl);
        using var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new ByteArrayContent(content.ToArray())
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Kho ảnh từ chối dữ liệu ({(int)response.StatusCode}).",
                null,
                response.StatusCode);
        }
    }

    public async Task<byte[]> DownloadAsync(
        string viewUrl,
        CancellationToken cancellationToken = default)
    {
        var uri = RequireHttpsSignedUrl(viewUrl);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Không tải được ảnh khách hàng ({(int)response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength is > 5_242_880)
        {
            throw new InvalidOperationException("Ảnh khách hàng vượt quá giới hạn 5 MB.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (bytes.Length > 5_242_880)
        {
            throw new InvalidOperationException("Ảnh khách hàng vượt quá giới hạn 5 MB.");
        }

        return bytes;
    }

    public void Dispose() => httpClient.Dispose();

    private static Uri RequireHttpsSignedUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException("Đường dẫn kho ảnh không hợp lệ.");
        }

        return uri;
    }
}
