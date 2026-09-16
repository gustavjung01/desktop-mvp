using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IInventoryTransferService
{
    Task<IReadOnlyList<InventoryTransferData>> ListTransfersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryTransferInTransitData>> ListInTransitAsync(CancellationToken cancellationToken = default);
    Task<InventoryTransferData> GetTransferAsync(string transferId, CancellationToken cancellationToken = default);
    Task<InventoryTransferReceiptBundleData> GetReceiptBundleAsync(string transferId, CancellationToken cancellationToken = default);

    Task<InventoryTransferData> CreateAsync(InventoryTransferCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryTransferData> ApproveAsync(string transferId, InventoryTransferTransitionRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryTransferData> DispatchAsync(string transferId, InventoryTransferTransitionRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryTransferData> CancelAsync(string transferId, InventoryTransferCancelRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryTransferReceiptMutationResultData> ReceiveAsync(string transferId, InventoryTransferReceiptCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryTransferReceiptMutationResultData> ApproveDamageAsync(string transferId, string receiptId, InventoryTransferDamageApprovalRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryTransferResolutionMutationResultData> CloseShortAsync(string transferId, InventoryTransferCloseShortRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryTransferReceiptMutationResultData> ReverseReceiptAsync(string transferId, string receiptId, InventoryTransferReverseReceiptRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class InventoryTransferService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IInventoryTransferService
{
    public async Task<IReadOnlyList<InventoryTransferData>> ListTransfersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryTransferData[]>(
            "/api/inventory/transfers?limit=500",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<InventoryTransferInTransitData>> ListInTransitAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryTransferInTransitData[]>(
            "/api/inventory/transfers/in-transit?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<InventoryTransferData> GetTransferAsync(string transferId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<InventoryTransferData>(
            $"/api/inventory/transfers/{Uri.EscapeDataString(RequireId(transferId, nameof(transferId)))}",
            RequireToken(),
            cancellationToken);

    public Task<InventoryTransferReceiptBundleData> GetReceiptBundleAsync(string transferId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<InventoryTransferReceiptBundleData>(
            $"/api/inventory/transfers/{Uri.EscapeDataString(RequireId(transferId, nameof(transferId)))}/receipts",
            RequireToken(),
            cancellationToken);

    public Task<InventoryTransferData> CreateAsync(
        InventoryTransferCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<InventoryTransferCreateRequest, InventoryTransferData>(
            "/api/inventory/transfers",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryTransferData> ApproveAsync(
        string transferId,
        InventoryTransferTransitionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<InventoryTransferTransitionRequest, InventoryTransferData>(
            $"/api/inventory/transfers/{Uri.EscapeDataString(RequireId(transferId, nameof(transferId)))}/approve",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryTransferData> DispatchAsync(
        string transferId,
        InventoryTransferTransitionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<InventoryTransferTransitionRequest, InventoryTransferData>(
            $"/api/inventory/transfers/{Uri.EscapeDataString(RequireId(transferId, nameof(transferId)))}/dispatch",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryTransferData> CancelAsync(
        string transferId,
        InventoryTransferCancelRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<InventoryTransferCancelRequest, InventoryTransferData>(
            $"/api/inventory/transfers/{Uri.EscapeDataString(RequireId(transferId, nameof(transferId)))}/cancel",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryTransferReceiptMutationResultData> ReceiveAsync(
        string transferId,
        InventoryTransferReceiptCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<InventoryTransferReceiptCreateRequest, InventoryTransferReceiptMutationResultData>(
            $"/api/inventory/transfers/{Uri.EscapeDataString(RequireId(transferId, nameof(transferId)))}/receipts",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryTransferReceiptMutationResultData> ApproveDamageAsync(
        string transferId,
        string receiptId,
        InventoryTransferDamageApprovalRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<InventoryTransferDamageApprovalRequest, InventoryTransferReceiptMutationResultData>(
            $"/api/inventory/transfers/{Uri.EscapeDataString(RequireId(transferId, nameof(transferId)))}/receipts/{Uri.EscapeDataString(RequireId(receiptId, nameof(receiptId)))}/approve-damage",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryTransferResolutionMutationResultData> CloseShortAsync(
        string transferId,
        InventoryTransferCloseShortRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<InventoryTransferCloseShortRequest, InventoryTransferResolutionMutationResultData>(
            $"/api/inventory/transfers/{Uri.EscapeDataString(RequireId(transferId, nameof(transferId)))}/close-short",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<InventoryTransferReceiptMutationResultData> ReverseReceiptAsync(
        string transferId,
        string receiptId,
        InventoryTransferReverseReceiptRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<InventoryTransferReverseReceiptRequest, InventoryTransferReceiptMutationResultData>(
            $"/api/inventory/transfers/{Uri.EscapeDataString(RequireId(transferId, nameof(transferId)))}/receipts/{Uri.EscapeDataString(RequireId(receiptId, nameof(receiptId)))}/reverse",
            request,
            idempotencyKey,
            cancellationToken);

    private Task<TResponse> PostAsync<TRequest, TResponse>(
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

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value, string name) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã dữ liệu không hợp lệ.", name);
}
