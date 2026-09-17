using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ISupplierPaymentService
{
    Task<IReadOnlyList<SupplierPaymentData>> ListPaymentsAsync(CancellationToken cancellationToken = default);
    Task<SupplierPaymentData> GetPaymentAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierPaymentAllocationTargetData>> ListTargetsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierData>> ListSuppliersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default);
    Task<SupplierPaymentData> CreatePaymentAsync(SupplierPaymentCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierPaymentAllocationData> AllocateAsync(SupplierPaymentAllocationRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierPaymentAllocationData> ReverseAllocationAsync(string allocationId, SupplierPaymentReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierPaymentData> ReversePaymentAsync(string paymentId, SupplierPaymentReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class SupplierPaymentService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ISupplierPaymentService
{
    public async Task<IReadOnlyList<SupplierPaymentData>> ListPaymentsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierPaymentData[]>("/api/supplier-payments?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<SupplierPaymentData> GetPaymentAsync(string id, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<SupplierPaymentData>(
            $"/api/supplier-payments/{Uri.EscapeDataString(RequireId(id, "Phiếu thanh toán"))}",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<SupplierPaymentAllocationTargetData>> ListTargetsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierPaymentAllocationTargetData[]>("/api/supplier-payments/allocation-targets", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<SupplierData>> ListSuppliersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierData[]>("/api/suppliers?active=true&limit=1000&offset=0", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<WarehouseData[]>("/api/warehouses?limit=1000&offset=0", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<SupplierPaymentData> CreatePaymentAsync(
        SupplierPaymentCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<SupplierPaymentCreateRequest, SupplierPaymentData>(
            "/api/supplier-payments", request, idempotencyKey, cancellationToken);

    public Task<SupplierPaymentAllocationData> AllocateAsync(
        SupplierPaymentAllocationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<SupplierPaymentAllocationRequest, SupplierPaymentAllocationData>(
            "/api/payable-allocations", request, idempotencyKey, cancellationToken);

    public Task<SupplierPaymentAllocationData> ReverseAllocationAsync(
        string allocationId,
        SupplierPaymentReverseRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<SupplierPaymentReverseRequest, SupplierPaymentAllocationData>(
            $"/api/payable-allocations/{Uri.EscapeDataString(RequireId(allocationId, "Phân bổ"))}/reverse",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<SupplierPaymentData> ReversePaymentAsync(
        string paymentId,
        SupplierPaymentReverseRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<SupplierPaymentReverseRequest, SupplierPaymentData>(
            $"/api/supplier-payments/{Uri.EscapeDataString(RequireId(paymentId, "Phiếu thanh toán"))}/reverse",
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
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));

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

    private static string RequireId(string? value, string label) =>
        Guid.TryParse(value?.Trim(), out var id)
            ? id.ToString("D")
            : throw new ArgumentException($"{label} không hợp lệ.", nameof(value));
}
