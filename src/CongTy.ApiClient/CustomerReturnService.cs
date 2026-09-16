using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ICustomerReturnService
{
    Task<IReadOnlyList<CustomerReturnEligibilityData>> ListEligibilityAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerReturnData>> ListAsync(CancellationToken cancellationToken = default);
    Task<CustomerReturnData> GetAsync(string customerReturnId, CancellationToken cancellationToken = default);
    Task<CustomerReturnMutationResult> CreateAsync(CustomerReturnCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerReturnMutationResult> ReceiveAsync(string customerReturnId, CustomerReturnReceiveRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerReturnMutationResult> CancelAsync(string customerReturnId, CustomerReturnCancelRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class CustomerReturnService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ICustomerReturnService
{
    private const string BasePath = "/api/delivery-orders/customer-returns";

    public async Task<IReadOnlyList<CustomerReturnEligibilityData>> ListEligibilityAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerReturnEligibilityData[]>(
            $"{BasePath}/eligibility?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<CustomerReturnData>> ListAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerReturnData[]>(
            $"{BasePath}?limit=500",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<CustomerReturnData> GetAsync(string customerReturnId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<CustomerReturnData>(
            $"{BasePath}/{Uri.EscapeDataString(RequireId(customerReturnId))}",
            RequireToken(),
            cancellationToken);

    public Task<CustomerReturnMutationResult> CreateAsync(
        CustomerReturnCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync(BasePath, request, idempotencyKey, cancellationToken);

    public Task<CustomerReturnMutationResult> ReceiveAsync(
        string customerReturnId,
        CustomerReturnReceiveRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync($"{BasePath}/{Uri.EscapeDataString(RequireId(customerReturnId))}/receive", request, idempotencyKey, cancellationToken);

    public Task<CustomerReturnMutationResult> CancelAsync(
        string customerReturnId,
        CustomerReturnCancelRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync($"{BasePath}/{Uri.EscapeDataString(RequireId(customerReturnId))}/cancel", request, idempotencyKey, cancellationToken);

    private Task<CustomerReturnMutationResult> PostAsync<TRequest>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));

        return apiClient.PostIdempotentDataAsync<TRequest, CustomerReturnMutationResult>(
            path, request, idempotencyKey.Trim(), RequireToken(), cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã phiếu hàng khách trả không hợp lệ.", nameof(value));
}
