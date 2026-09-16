using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrderData>> ListAsync(CancellationToken cancellationToken = default);
    Task<PurchaseOrderData> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrderSkuSearchOptionData>> SearchSkuAsync(string search, int limit = 50, int offset = 0, CancellationToken cancellationToken = default);
    Task<SupplierPurchasePriceResolutionData> ResolvePriceAsync(SupplierPurchasePriceResolveRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderData> CreateDraftAsync(PurchaseOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PurchaseOrderData> UpdateDraftAsync(string id, PurchaseOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PurchaseOrderData> SubmitAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PurchaseOrderData> ApproveAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PurchaseOrderData> CancelAsync(string id, string expectedRevision, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoodsReceiptPurchaseOrderSummaryData>> ListReceiptsAsync(string purchaseOrderId, CancellationToken cancellationToken = default);
}

public sealed class PurchaseOrderService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IPurchaseOrderService
{
    public async Task<IReadOnlyList<PurchaseOrderData>> ListAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<PurchaseOrderData[]>(
            "/api/purchase-orders?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<PurchaseOrderData> GetAsync(string id, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<PurchaseOrderData>(
            $"/api/purchase-orders/{RequireId(id)}",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<PurchaseOrderSkuSearchOptionData>> SearchSkuAsync(
        string search,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var term = (search ?? string.Empty).Trim();
        var query = $"limit={Math.Clamp(limit, 1, 50)}&offset={Math.Max(0, offset)}";
        if (term.Length > 0)
        {
            query += $"&search={Uri.EscapeDataString(term[..Math.Min(256, term.Length)])}";
        }

        return await apiClient.GetDataAsync<PurchaseOrderSkuSearchOptionData[]>(
            $"/api/purchase-orders/sku-search?{query}",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
    }

    public Task<SupplierPurchasePriceResolutionData> ResolvePriceAsync(
        SupplierPurchasePriceResolveRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostDataAsync<SupplierPurchasePriceResolveRequest, SupplierPurchasePriceResolutionData>(
            "/api/supplier-purchase-prices/resolve",
            request,
            RequireToken(),
            cancellationToken);

    public Task<PurchaseOrderData> CreateDraftAsync(
        PurchaseOrderDraftRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        RequireKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<PurchaseOrderDraftRequest, PurchaseOrderData>(
            "/api/purchase-orders",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    public Task<PurchaseOrderData> UpdateDraftAsync(
        string id,
        PurchaseOrderDraftRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        RequireKey(idempotencyKey);
        return apiClient.PatchIdempotentDataAsync<PurchaseOrderDraftRequest, PurchaseOrderData>(
            $"/api/purchase-orders/{RequireId(id)}",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    public Task<PurchaseOrderData> SubmitAsync(
        string id,
        string expectedRevision,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        RunActionAsync(id, "submit", new PurchaseOrderActionRequest(RequireRevision(expectedRevision)), idempotencyKey, cancellationToken);

    public Task<PurchaseOrderData> ApproveAsync(
        string id,
        string expectedRevision,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        RunActionAsync(id, "approve", new PurchaseOrderActionRequest(RequireRevision(expectedRevision)), idempotencyKey, cancellationToken);

    public Task<PurchaseOrderData> CancelAsync(
        string id,
        string expectedRevision,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var normalized = (reason ?? string.Empty).Trim();
        if (normalized.Length is < 1 or > 1000)
        {
            throw new ArgumentException("Lý do hủy phải có từ 1 đến 1000 ký tự.", nameof(reason));
        }

        return RunActionAsync(
            id,
            "cancel",
            new PurchaseOrderActionRequest(RequireRevision(expectedRevision), normalized),
            idempotencyKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<GoodsReceiptPurchaseOrderSummaryData>> ListReceiptsAsync(
        string purchaseOrderId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<GoodsReceiptPurchaseOrderSummaryData[]>(
            $"/api/goods-receipts?purchaseOrderId={Uri.EscapeDataString(RequireId(purchaseOrderId))}&limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    private Task<PurchaseOrderData> RunActionAsync(
        string id,
        string action,
        PurchaseOrderActionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<PurchaseOrderActionRequest, PurchaseOrderData>(
            $"/api/purchase-orders/{RequireId(id)}/{action}",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    private void RequireKey(string key)
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
        Guid.TryParse(value, out _) ? value.Trim() : throw new ArgumentException("Mã dữ liệu không hợp lệ.", nameof(value));

    private static string RequireRevision(string value) =>
        long.TryParse(value, out var revision) && revision > 0
            ? revision.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : throw new ArgumentException("Phiên bản đơn mua hàng không hợp lệ.", nameof(value));
}
