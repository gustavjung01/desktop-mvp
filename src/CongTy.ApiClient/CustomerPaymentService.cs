using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ICustomerPaymentService
{
    Task<IReadOnlyList<CustomerPaymentData>> ListPaymentsAsync(CancellationToken cancellationToken = default);
    Task<CustomerPaymentData> GetPaymentAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceivableAllocationTargetData>> ListTargetsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RemittingEmployeeOptionData>> ListRemittingEmployeesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default);
    Task<CustomerPaymentData> CreatePaymentAsync(CustomerPaymentCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerPaymentData> AllocateAsync(string paymentId, CustomerPaymentAllocateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerPaymentData> ReversePaymentAsync(string paymentId, CustomerPaymentReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerPaymentAllocationData> ReverseAllocationAsync(string allocationId, CustomerPaymentReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class CustomerPaymentService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ICustomerPaymentService
{
    public async Task<IReadOnlyList<CustomerPaymentData>> ListPaymentsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerPaymentData[]>("/api/customer-payments?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<CustomerPaymentData> GetPaymentAsync(string id, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<CustomerPaymentData>(
            $"/api/customer-payments/{Uri.EscapeDataString(RequireId(id, "Phiếu thu"))}",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<ReceivableAllocationTargetData>> ListTargetsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ReceivableAllocationTargetData[]>("/api/customer-payments/allocation-targets", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<RemittingEmployeeOptionData>> ListRemittingEmployeesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<RemittingEmployeeOptionData[]>("/api/customer-payments/remitting-employees", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<CustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerData[]>("/api/customers?active=true&limit=1000&offset=0", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<WarehouseData[]>("/api/warehouses?limit=1000&offset=0", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<CustomerPaymentData> CreatePaymentAsync(
        CustomerPaymentCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CustomerPaymentCreateRequest, CustomerPaymentData>(
            "/api/customer-payments", request, idempotencyKey, cancellationToken);

    public Task<CustomerPaymentData> AllocateAsync(
        string paymentId,
        CustomerPaymentAllocateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CustomerPaymentAllocateRequest, CustomerPaymentData>(
            $"/api/customer-payments/{Uri.EscapeDataString(RequireId(paymentId, "Phiếu thu"))}/allocations",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerPaymentData> ReversePaymentAsync(
        string paymentId,
        CustomerPaymentReverseRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CustomerPaymentReverseRequest, CustomerPaymentData>(
            $"/api/customer-payments/{Uri.EscapeDataString(RequireId(paymentId, "Phiếu thu"))}/reverse",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerPaymentAllocationData> ReverseAllocationAsync(
        string allocationId,
        CustomerPaymentReverseRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CustomerPaymentReverseRequest, CustomerPaymentAllocationData>(
            $"/api/receivable-allocations/{Uri.EscapeDataString(RequireId(allocationId, "Khoản ghi nhận"))}/reverse",
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
