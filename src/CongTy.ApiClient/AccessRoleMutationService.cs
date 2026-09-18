using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IAccessRoleMutationService
{
    Task<AccessRoleData> CreateAsync(
        AccessRoleCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<AccessRoleData> UpdateAsync(
        string roleId,
        AccessRoleUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<AccessRoleData> ToggleAsync(
        string roleId,
        AccessRoleToggleRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class AccessRoleMutationService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IAccessRoleMutationService
{
    public Task<AccessRoleData> CreateAsync(
        AccessRoleCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<AccessRoleCreateRequest, AccessRoleData>(
            "/api/access/roles",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AccessRoleData> UpdateAsync(
        string roleId,
        AccessRoleUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchIdempotentDataAsync<AccessRoleUpdateRequest, AccessRoleData>(
            BuildRolePath(roleId),
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AccessRoleData> ToggleAsync(
        string roleId,
        AccessRoleToggleRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchIdempotentDataAsync<AccessRoleToggleRequest, AccessRoleData>(
            BuildRolePath(roleId),
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string BuildRolePath(string roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            throw new ArgumentException("Vai trò không hợp lệ.", nameof(roleId));

        return $"/api/access/roles/{Uri.EscapeDataString(roleId.Trim())}";
    }
}
