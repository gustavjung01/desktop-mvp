using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ISupplierReturnService
{
    Task<IReadOnlyList<SupplierReturnData>> ListAsync(CancellationToken cancellationToken = default);
    Task<SupplierReturnData> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierReturnLineData>> ListSourceLinesAsync(string goodsReceiptId, CancellationToken cancellationToken = default);
    Task<SupplierReturnData> CreateAsync(SupplierReturnDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierReturnData> UpdateAsync(string id, SupplierReturnDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierReturnData> SubmitAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierReturnData> ApproveAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierReturnData> CancelAsync(string id, string expectedRevision, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierReturnData> PostAsync(string id, string expectedRevision, DateTime documentDate, string? reasonNote, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierReturnData> ReverseAsync(string id, string expectedRevision, DateTime documentDate, string reasonNote, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class SupplierReturnService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ISupplierReturnService
{
    public async Task<IReadOnlyList<SupplierReturnData>> ListAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierReturnData[]>(
            "/api/supplier-returns?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<SupplierReturnData> GetAsync(string id, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<SupplierReturnData>(
            $"/api/supplier-returns/{RequireId(id)}",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<SupplierReturnLineData>> ListSourceLinesAsync(
        string goodsReceiptId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierReturnLineData[]>(
            $"/api/supplier-returns/source-lines?goodsReceiptId={Uri.EscapeDataString(RequireId(goodsReceiptId))}",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<SupplierReturnData> CreateAsync(
        SupplierReturnDraftRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync("/api/supplier-returns", request, idempotencyKey, cancellationToken);

    public Task<SupplierReturnData> UpdateAsync(
        string id,
        SupplierReturnDraftRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        EnsureKey(idempotencyKey);
        return apiClient.PatchIdempotentDataAsync<SupplierReturnDraftRequest, SupplierReturnData>(
            $"/api/supplier-returns/{RequireId(id)}",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    public Task<SupplierReturnData> SubmitAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default) =>
        ActionAsync(id, "submit", new SupplierReturnExpectedRevisionRequest(expectedRevision), idempotencyKey, cancellationToken);

    public Task<SupplierReturnData> ApproveAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default) =>
        ActionAsync(id, "approve", new SupplierReturnExpectedRevisionRequest(expectedRevision), idempotencyKey, cancellationToken);

    public Task<SupplierReturnData> CancelAsync(
        string id,
        string expectedRevision,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        ActionAsync(id, "cancel", new SupplierReturnCancelRequest(expectedRevision, reason.Trim()), idempotencyKey, cancellationToken);

    public Task<SupplierReturnData> PostAsync(
        string id,
        string expectedRevision,
        DateTime documentDate,
        string? reasonNote,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        ActionAsync(
            id,
            "post",
            new SupplierReturnPostRequest(
                expectedRevision,
                documentDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                string.IsNullOrWhiteSpace(reasonNote) ? null : reasonNote.Trim()),
            idempotencyKey,
            cancellationToken);

    public Task<SupplierReturnData> ReverseAsync(
        string id,
        string expectedRevision,
        DateTime documentDate,
        string reasonNote,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        ActionAsync(
            id,
            "reverse",
            new SupplierReturnReverseRequest(
                expectedRevision,
                documentDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                reasonNote.Trim()),
            idempotencyKey,
            cancellationToken);

    private Task<SupplierReturnData> ActionAsync<TRequest>(
        string id,
        string action,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        PostIdempotentAsync(
            $"/api/supplier-returns/{RequireId(id)}/{action}",
            request,
            idempotencyKey,
            cancellationToken);

    private Task<SupplierReturnData> PostIdempotentAsync<TRequest>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        EnsureKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<TRequest, SupplierReturnData>(
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
