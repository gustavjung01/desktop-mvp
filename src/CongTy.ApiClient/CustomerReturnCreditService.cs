using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ICustomerReturnCreditService
{
    Task<IReadOnlyList<CustomerReturnCreditData>> ListCreditsAsync(CancellationToken cancellationToken = default);
    Task<CustomerReturnCreditData> GetCreditAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceivableAllocationTargetData>> ListTargetsAsync(CancellationToken cancellationToken = default);
    Task<CustomerReturnCreditData> AllocateAsync(string creditId, CustomerReturnCreditAllocateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerRefundData> CreateRefundAsync(CustomerRefundCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerRefundData> ReverseRefundAsync(string refundId, CustomerReturnCreditReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerReturnCreditData> ReverseCreditAsync(string creditId, CustomerReturnCreditReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class CustomerReturnCreditService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ICustomerReturnCreditService
{
    public async Task<IReadOnlyList<CustomerReturnCreditData>> ListCreditsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerReturnCreditData[]>("/api/customer-return-credits?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<CustomerReturnCreditData> GetCreditAsync(string id, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<CustomerReturnCreditData>(
            $"/api/customer-return-credits/{Uri.EscapeDataString(RequireId(id, "Khoản giảm công nợ"))}",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<ReceivableAllocationTargetData>> ListTargetsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ReceivableAllocationTargetData[]>("/api/customer-payments/allocation-targets", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<CustomerReturnCreditData> AllocateAsync(
        string creditId,
        CustomerReturnCreditAllocateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CustomerReturnCreditAllocateRequest, CustomerReturnCreditData>(
            $"/api/customer-return-credits/{Uri.EscapeDataString(RequireId(creditId, "Khoản giảm công nợ"))}/allocations",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerRefundData> CreateRefundAsync(
        CustomerRefundCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CustomerRefundCreateRequest, CustomerRefundData>(
            "/api/customer-refunds",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerRefundData> ReverseRefundAsync(
        string refundId,
        CustomerReturnCreditReverseRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CustomerReturnCreditReverseRequest, CustomerRefundData>(
            $"/api/customer-refunds/{Uri.EscapeDataString(RequireId(refundId, "Phiếu hoàn"))}/reverse",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerReturnCreditData> ReverseCreditAsync(
        string creditId,
        CustomerReturnCreditReverseRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CustomerReturnCreditReverseRequest, CustomerReturnCreditData>(
            $"/api/customer-return-credits/{Uri.EscapeDataString(RequireId(creditId, "Khoản giảm công nợ"))}/reverse",
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
