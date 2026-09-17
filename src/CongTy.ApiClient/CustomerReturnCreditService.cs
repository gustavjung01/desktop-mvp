using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ICustomerReturnCreditService
{
    Task<IReadOnlyList<CustomerReturnCreditData>> ListCreditsAsync(CancellationToken cancellationToken = default);
    Task<CustomerReturnCreditData> GetCreditAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceivableAllocationTargetData>> ListTargetsAsync(CancellationToken cancellationToken = default);
    Task<CustomerReturnCreditData> AllocateAsync(string id, CustomerReturnCreditAllocateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerRefundData> CreateRefundAsync(CustomerRefundCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerRefundData> ReverseRefundAsync(string id, CustomerReturnCreditReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerReturnCreditData> ReverseCreditAsync(string id, CustomerReturnCreditReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class CustomerReturnCreditService(
    CompanyApiClient api,
    IAuthenticatedSessionAccessor session,
    ICanonicalIdempotencyKeyProvider idempotency) : ICustomerReturnCreditService
{
    public async Task<IReadOnlyList<CustomerReturnCreditData>> ListCreditsAsync(CancellationToken cancellationToken = default)
    {
        var data = await api.GetDataAsync<CustomerReturnCreditData[]>(
            "/api/customer-return-credits?limit=500&offset=0",
            session.CurrentToken,
            cancellationToken).ConfigureAwait(false);
        return data;
    }

    public Task<CustomerReturnCreditData> GetCreditAsync(string id, CancellationToken cancellationToken = default) =>
        api.GetDataAsync<CustomerReturnCreditData>($"/api/customer-return-credits/{RequireId(id)}", session.CurrentToken, cancellationToken);

    public async Task<IReadOnlyList<ReceivableAllocationTargetData>> ListTargetsAsync(CancellationToken cancellationToken = default)
    {
        var data = await api.GetDataAsync<ReceivableAllocationTargetData[]>(
            "/api/customer-payments/allocation-targets",
            session.CurrentToken,
            cancellationToken).ConfigureAwait(false);
        return data;
    }

    public Task<CustomerReturnCreditData> AllocateAsync(string id, CustomerReturnCreditAllocateRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync<CustomerReturnCreditAllocateRequest, CustomerReturnCreditData>(
            $"/api/customer-return-credits/{RequireId(id)}/allocations", request, idempotencyKey, cancellationToken);

    public Task<CustomerRefundData> CreateRefundAsync(CustomerRefundCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync<CustomerRefundCreateRequest, CustomerRefundData>("/api/customer-refunds", request, idempotencyKey, cancellationToken);

    public Task<CustomerRefundData> ReverseRefundAsync(string id, CustomerReturnCreditReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync<CustomerReturnCreditReverseRequest, CustomerRefundData>(
            $"/api/customer-refunds/{RequireId(id)}/reverse", request, idempotencyKey, cancellationToken);

    public Task<CustomerReturnCreditData> ReverseCreditAsync(string id, CustomerReturnCreditReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync<CustomerReturnCreditReverseRequest, CustomerReturnCreditData>(
            $"/api/customer-return-credits/{RequireId(id)}/reverse", request, idempotencyKey, cancellationToken);

    private Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, string key, CancellationToken cancellationToken)
    {
        if (!idempotency.IsValid(key))
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(key));
        return api.PostIdempotentDataAsync<TRequest, TResponse>(path, request, key, session.CurrentToken, cancellationToken);
    }

    private static string RequireId(string value)
    {
        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            throw new ArgumentException("Mã chứng từ không hợp lệ.", nameof(value));
        return id.ToString("D");
    }
}
