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

    public Task<CustomerOrderingHomeContentData> GetAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<CustomerOrderingHomeContentData>(
            Root,
            RequireToken(),
            cancellationToken);

    public Task<CustomerOrderingHomeContentData> SaveAsync(
        CustomerOrderingHomeContentUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return apiClient.PatchIdempotentDataAsync<CustomerOrderingHomeContentUpdateRequest, CustomerOrderingHomeContentData>(
            Root,
            request,
            Key(idempotencyKey),
            RequireToken(),
            cancellationToken);
    }

    public Task<CustomerOrderingHomeContentData> UploadBannerAsync(
        byte[] webpBytes,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webpBytes);
        if (webpBytes.Length is < 1 or > MaxBannerBytes)
            throw new InvalidOperationException("Ảnh WebP phải có dung lượng từ 1 byte đến 5 MB.");

        return apiClient.PutBytesIdempotentDataAsync<CustomerOrderingHomeContentData>(
            Banner,
            webpBytes,
            "image/webp",
            Key(idempotencyKey),
            RequireToken(),
            cancellationToken);
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
