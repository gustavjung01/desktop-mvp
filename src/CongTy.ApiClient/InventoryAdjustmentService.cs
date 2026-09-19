using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IInventoryAdjustmentService
{
    Task<IReadOnlyList<InventoryAdjustmentData>> ListAsync(
        string? status = null,
        string? documentKind = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryAdjustmentData>> ListForExportAsync(
        string? status = null,
        string? documentKind = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryAdjustmentReasonData>> ListReasonsAsync(
        CancellationToken cancellationToken = default);

    Task<InventoryAdjustmentData> GetAsync(
        string adjustmentId,
        CancellationToken cancellationToken = default);

    Task<InventoryAdjustmentData> CreateAsync(
        InventoryAdjustmentCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<InventoryAdjustmentData> SubmitAsync(
        string adjustmentId,
        InventoryAdjustmentTransitionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<InventoryAdjustmentData> ApproveAsync(
        string adjustmentId,
        InventoryAdjustmentTransitionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<InventoryAdjustmentData> PostAsync(
        string adjustmentId,
        InventoryAdjustmentTransitionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<InventoryAdjustmentData> CancelAsync(
        string adjustmentId,
        InventoryAdjustmentReasonRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<InventoryAdjustmentData> ReverseAsync(
        string adjustmentId,
        InventoryAdjustmentReasonRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<BulkInventoryAdjustmentPreviewData> PreviewBulkAsync(
        BulkInventoryAdjustmentPreviewRequest request,
        CancellationToken cancellationToken = default);

    Task<BulkInventoryAdjustmentConfirmData> ConfirmBulkAsync(
        BulkInventoryAdjustmentConfirmRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class InventoryAdjustmentService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IInventoryAdjustmentService
{
    private const int PageSize = 500;
    private const int MaxExportRows = 2000;
    private const int MaxExportScanRows = 5000;

    public Task<IReadOnlyList<InventoryAdjustmentData>> ListAsync(
        string? status = null,
        string? documentKind = null,
        CancellationToken cancellationToken = default) =>
        ListPageAsync(status, documentKind, PageSize, 0, cancellationToken);

    public async Task<IReadOnlyList<InventoryAdjustmentData>> ListForExportAsync(
        string? status = null,
        string? documentKind = null,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<InventoryAdjustmentData>();
        var offset = 0;
        var scanned = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = await ListPageAsync(
                status,
                documentKind,
                PageSize,
                offset,
                cancellationToken).ConfigureAwait(false);

            scanned += batch.Count;
            rows.AddRange(batch);

            if (rows.Count > MaxExportRows)
            {
                throw new InvalidOperationException(
                    "Có hơn 2.000 phiếu phù hợp. Hãy thu hẹp Trạng thái hoặc Loại phiếu trước khi xuất.");
            }

            if (batch.Count < PageSize) break;

            offset += batch.Count;
            if (scanned >= MaxExportScanRows)
            {
                throw new InvalidOperationException(
                    "Phạm vi dữ liệu quá lớn để xuất an toàn. Hãy thu hẹp bộ lọc trước khi xuất.");
            }
        }

        return rows;
    }

    private async Task<IReadOnlyList<InventoryAdjustmentData>> ListPageAsync(
        string? status,
        string? documentKind,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
        {
            parameters.Add($"status={Uri.EscapeDataString(status.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(documentKind))
        {
            parameters.Add($"documentKind={Uri.EscapeDataString(documentKind.Trim())}");
        }

        parameters.Add($"limit={limit}");
        parameters.Add($"offset={offset}");

        return await apiClient.GetDataAsync<InventoryAdjustmentData[]>(
            $"/api/inventory/adjustments?{string.Join("&", parameters)}",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InventoryAdjustmentReasonData>> ListReasonsAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryAdjustmentReasonData[]>(
            "/api/inventory/adjustments/reasons",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<InventoryAdjustmentData> GetAsync(
        string adjustmentId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<InventoryAdjustmentData>(
            $"/api/inventory/adjustments/{Uri.EscapeDataString(RequireId(adjustmentId))}",
            RequireToken(),
            cancellationToken);

    public Task<InventoryAdjustmentData> CreateAsync(
        InventoryAdjustmentCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync(
            "/api/inventory/adjustments",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryAdjustmentData> SubmitAsync(
        string adjustmentId,
        InventoryAdjustmentTransitionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync(
            ActionPath(adjustmentId, "submit"),
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryAdjustmentData> ApproveAsync(
        string adjustmentId,
        InventoryAdjustmentTransitionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync(
            ActionPath(adjustmentId, "approve"),
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryAdjustmentData> PostAsync(
        string adjustmentId,
        InventoryAdjustmentTransitionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync(
            ActionPath(adjustmentId, "post"),
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryAdjustmentData> CancelAsync(
        string adjustmentId,
        InventoryAdjustmentReasonRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync(
            ActionPath(adjustmentId, "cancel"),
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryAdjustmentData> ReverseAsync(
        string adjustmentId,
        InventoryAdjustmentReasonRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync(
            ActionPath(adjustmentId, "reverse"),
            request,
            idempotencyKey,
            cancellationToken);

    public Task<BulkInventoryAdjustmentPreviewData> PreviewBulkAsync(
        BulkInventoryAdjustmentPreviewRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostDataAsync<BulkInventoryAdjustmentPreviewRequest, BulkInventoryAdjustmentPreviewData>(
            "/api/inventory/adjustments/bulk-preview",
            request,
            RequireToken(),
            cancellationToken);

    public Task<BulkInventoryAdjustmentConfirmData> ConfirmBulkAsync(
        BulkInventoryAdjustmentConfirmRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotentAsync<BulkInventoryAdjustmentConfirmRequest, BulkInventoryAdjustmentConfirmData>(
            "/api/inventory/adjustments/bulk-confirm",
            request,
            idempotencyKey,
            cancellationToken);

    private Task<InventoryAdjustmentData> PostIdempotentAsync<TRequest>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        PostIdempotentAsync<TRequest, InventoryAdjustmentData>(
            path,
            request,
            idempotencyKey,
            cancellationToken);

    private Task<TResponse> PostIdempotentAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
        {
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));
        }

        return apiClient.PostIdempotentDataAsync<TRequest, TResponse>(
            path,
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private static string ActionPath(string adjustmentId, string action) =>
        $"/api/inventory/adjustments/{Uri.EscapeDataString(RequireId(adjustmentId))}/{action}";

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã phiếu điều chỉnh không hợp lệ.", nameof(value));
}
