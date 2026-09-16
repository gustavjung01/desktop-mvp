using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ISupplierPurchasePriceService
{
    Task<IReadOnlyList<SupplierPurchasePriceData>> ListAsync(CancellationToken cancellationToken = default);
    Task<SupplierPurchasePriceData> CreateAsync(SupplierPurchasePriceRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierPurchasePriceData> UpdateAsync(string id, SupplierPurchasePriceRequest request, CancellationToken cancellationToken = default);
}

public sealed class SupplierPurchasePriceService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ISupplierPurchasePriceService
{
    public async Task<IReadOnlyList<SupplierPurchasePriceData>> ListAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierPurchasePriceData[]>(
            "/api/supplier-purchase-prices?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<SupplierPurchasePriceData> CreateAsync(
        SupplierPurchasePriceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
        {
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));
        }

        return apiClient.PostIdempotentDataAsync<SupplierPurchasePriceRequest, SupplierPurchasePriceData>(
            "/api/supplier-purchase-prices",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    public Task<SupplierPurchasePriceData> UpdateAsync(
        string id,
        SupplierPurchasePriceRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<SupplierPurchasePriceRequest, SupplierPurchasePriceData>(
            $"/api/supplier-purchase-prices/{RequireId(id)}",
            request,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _) ? value.Trim() : throw new ArgumentException("Mã giá mua không hợp lệ.", nameof(value));
}
