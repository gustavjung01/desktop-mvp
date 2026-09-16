using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IInventoryTrackingPolicyService
{
    Task<IReadOnlyList<InventoryTrackingPolicyData>> ListPoliciesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryTrackingPolicyCandidateData>> ListCandidatesAsync(CancellationToken cancellationToken = default);
    Task<InventoryTrackingPolicyData> SaveAsync(
        InventoryTrackingPolicySaveRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class InventoryTrackingPolicyService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IInventoryTrackingPolicyService
{
    public async Task<IReadOnlyList<InventoryTrackingPolicyData>> ListPoliciesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryTrackingPolicyData[]>(
            "/api/inventory/tracking-policies?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<InventoryTrackingPolicyCandidateData>> ListCandidatesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryTrackingPolicyCandidateData[]>(
            "/api/inventory/tracking-policies/candidates?limit=2000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<InventoryTrackingPolicyData> SaveAsync(
        InventoryTrackingPolicySaveRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.BaseVariantId, out _))
        {
            throw new ArgumentException("SKU tồn chuẩn không hợp lệ.", nameof(request));
        }

        if (!idempotencyKeys.IsValid(idempotencyKey))
        {
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));
        }

        return apiClient.PutIdempotentDataAsync<InventoryTrackingPolicySaveRequest, InventoryTrackingPolicyData>(
            $"/api/inventory/tracking-policies/{Uri.EscapeDataString(request.BaseVariantId.Trim())}",
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
