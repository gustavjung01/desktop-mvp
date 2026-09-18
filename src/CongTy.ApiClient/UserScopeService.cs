using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IUserScopeService
{
    Task<IReadOnlyList<UserScopeBranchData>> ListBranchesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserScopeWarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default);
    Task<UserScopeReplaceResult> ReplaceScopesAsync(
        string userId,
        UserScopeReplaceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class UserScopeService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IUserScopeService
{
    public async Task<IReadOnlyList<UserScopeBranchData>> ListBranchesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<UserScopeBranchData[]>(
            "/api/branches?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<UserScopeWarehouseData>> ListWarehousesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<UserScopeWarehouseData[]>(
            "/api/warehouses?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<UserScopeReplaceResult> ReplaceScopesAsync(
        string userId,
        UserScopeReplaceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PutIdempotentDataAsync<UserScopeReplaceRequest, UserScopeReplaceResult>(
            $"/api/internal-auth/users/{EscapedUserId(userId)}/scopes",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string EscapedUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Người dùng không hợp lệ.", nameof(userId));

        return Uri.EscapeDataString(userId.Trim());
    }
}
