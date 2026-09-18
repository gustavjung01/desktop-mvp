using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IAccessRoleReadService
{
    Task<IReadOnlyList<AccessPermissionData>> ListPermissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRoleData>> ListRolesAsync(CancellationToken cancellationToken = default);
    Task<AccessRoleData> GetRoleAsync(string roleId, CancellationToken cancellationToken = default);
}

public sealed class AccessRoleReadService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IAccessRoleReadService
{
    public async Task<IReadOnlyList<AccessPermissionData>> ListPermissionsAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<AccessPermissionData[]>(
            "/api/access/permissions",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<AccessRoleData>> ListRolesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<AccessRoleData[]>(
            "/api/access/roles?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<AccessRoleData> GetRoleAsync(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            throw new ArgumentException("Vai trò không hợp lệ.", nameof(roleId));

        return apiClient.GetDataAsync<AccessRoleData>(
            $"/api/access/roles/{Uri.EscapeDataString(roleId.Trim())}",
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
