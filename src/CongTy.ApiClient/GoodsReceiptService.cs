using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IGoodsReceiptService
{
    Task<IReadOnlyList<GoodsReceiptData>> ListAsync(CancellationToken cancellationToken = default);
    Task<GoodsReceiptData> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoodsReceiptTrackingRequirementData>> GetTrackingRequirementsAsync(string purchaseOrderId, CancellationToken cancellationToken = default);
    Task<GoodsReceiptData> CreateAsync(GoodsReceiptDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<GoodsReceiptData> UpdateAsync(string id, GoodsReceiptDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<GoodsReceiptData> PostAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<GoodsReceiptData> ReverseAsync(string id, string expectedRevision, DateTime documentDate, string reasonNote, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class GoodsReceiptService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IGoodsReceiptService
{
    public async Task<IReadOnlyList<GoodsReceiptData>> ListAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<GoodsReceiptData[]>(
            "/api/goods-receipts?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<GoodsReceiptData> GetAsync(string id, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<GoodsReceiptData>(
            $"/api/goods-receipts/{RequireId(id)}",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<GoodsReceiptTrackingRequirementData>> GetTrackingRequirementsAsync(
        string purchaseOrderId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<GoodsReceiptTrackingRequirementData[]>(
            $"/api/goods-receipts/tracking-requirements?purchaseOrderId={Uri.EscapeDataString(RequireId(purchaseOrderId))}",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<GoodsReceiptData> CreateAsync(
        GoodsReceiptDraftRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync("/api/goods-receipts", request, idempotencyKey, cancellationToken);

    public Task<GoodsReceiptData> UpdateAsync(
        string id,
        GoodsReceiptDraftRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        EnsureKey(idempotencyKey);
        return apiClient.PatchIdempotentDataAsync<GoodsReceiptDraftRequest, GoodsReceiptData>(
            $"/api/goods-receipts/{RequireId(id)}",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    public Task<GoodsReceiptData> PostAsync(
        string id,
        string expectedRevision,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync(
            $"/api/goods-receipts/{RequireId(id)}/post",
            new GoodsReceiptPostRequest(expectedRevision),
            idempotencyKey,
            cancellationToken);

    public Task<GoodsReceiptData> ReverseAsync(
        string id,
        string expectedRevision,
        DateTime documentDate,
        string reasonNote,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync(
            $"/api/goods-receipts/{RequireId(id)}/reverse",
            new GoodsReceiptReverseRequest(
                expectedRevision,
                documentDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                reasonNote.Trim()),
            idempotencyKey,
            cancellationToken);

    private Task<GoodsReceiptData> PostIdempotentAsync<TRequest>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        EnsureKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<TRequest, GoodsReceiptData>(
            path,
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    private void EnsureKey(string key)
    {
        if (!idempotencyKeys.IsValid(key))
        {
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(key));
        }
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _) ? value.Trim() : throw new ArgumentException("Mã chứng từ không hợp lệ.", nameof(value));
}
