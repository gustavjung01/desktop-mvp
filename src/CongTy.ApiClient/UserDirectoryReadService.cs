using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IUserDirectoryReadService
{
    Task<IReadOnlyList<AccessUserData>> ListUsersAsync(CancellationToken cancellationToken = default);
    Task<AccessUserData> GetUserAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class UserDirectoryReadService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IUserDirectoryReadService
{
    public async Task<IReadOnlyList<AccessUserData>> ListUsersAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<AccessUserData[]>(
            "/api/access/users?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<AccessUserData> GetUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Người dùng không hợp lệ.", nameof(userId));

        return apiClient.GetDataAsync<AccessUserData>(
            $"/api/access/users/{Uri.EscapeDataString(userId.Trim())}",
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
