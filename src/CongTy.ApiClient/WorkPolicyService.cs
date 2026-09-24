using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IWorkPolicyService
{
    Task<IReadOnlyList<WorkPolicyDetailData>> ListPoliciesAsync(CancellationToken cancellationToken = default);
    Task<WorkPolicyDetailData> SavePolicyAsync(
        SaveWorkPolicyRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class WorkPolicyService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IWorkPolicyService
{
    public async Task<IReadOnlyList<WorkPolicyDetailData>> ListPoliciesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<WorkPolicyDetailData[]>(
            "/api/workforce/policies",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<WorkPolicyDetailData> SavePolicyAsync(
        SaveWorkPolicyRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<SaveWorkPolicyRequest, WorkPolicyDetailData>(
            "/api/workforce/policies",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
