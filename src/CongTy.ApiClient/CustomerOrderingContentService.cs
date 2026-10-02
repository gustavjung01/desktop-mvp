using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ICustomerOrderingContentService
{
    Task<CustomerOrderingHomeContentData> GetAsync(CancellationToken cancellationToken = default);
    Task<CustomerOrderingHomeContentData> SaveAsync(
        CustomerOrderingHomeContentUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<CustomerOrderingHomeContentData> UploadBannerAsync(
        byte[] webpBytes,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class CustomerOrderingContentService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ICustomerOrderingContentService
{
    private const string Root = "/api/customer-ordering-home-content";
    private const string Banner = "/api/customer-ordering-home-content/banner";
    private const int MaxBannerBytes = 5 * 1024 * 1024;

    public async Task<CustomerOrderingHomeContentData> GetAsync(CancellationToken cancellationToken = default)
    {
        var data = await apiClient.GetDataAsync<CustomerOrderingHomeContentEnvelopeData>(
            Root,
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
        return data.Content;
    }

    public async Task<CustomerOrderingHomeContentData> SaveAsync(
        CustomerOrderingHomeContentUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await apiClient.PatchIdempotentDataAsync<CustomerOrderingHomeContentUpdateRequest, CustomerOrderingHomeContentEnvelopeData>(
            Root,
            request,
            Key(idempotencyKey),
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
        return data.Content;
    }

    public async Task<CustomerOrderingHomeContentData> UploadBannerAsync(
        byte[] webpBytes,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webpBytes);
        if (webpBytes.Length is < 1 or > MaxBannerBytes)
            throw new InvalidOperationException("Ảnh WebP phải có dung lượng từ 1 byte đến 5 MB.");

        var data = await apiClient.PutBytesIdempotentDataAsync<CustomerOrderingHomeContentEnvelopeData>(
            Banner,
            webpBytes,
            "image/webp",
            Key(idempotencyKey),
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
        return data.Content;
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private string Key(string value) =>
        idempotencyKeys.IsValid(value)
            ? value.Trim()
            : throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(value));
}
