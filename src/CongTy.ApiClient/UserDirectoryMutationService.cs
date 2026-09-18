using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IUserDirectoryMutationService
{
    Task<AccessUserData> CreateAsync(
        AccessUserCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<AccessUserData> ReplaceRolesAsync(
        string userId,
        AccessUserRolesRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<AccessUserData> UpdateStatusAsync(
        string userId,
        AccessUserStatusRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<AccessUserCredentialResult> SetCredentialAsync(
        string userId,
        AccessUserCredentialRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class UserDirectoryMutationService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IUserDirectoryMutationService
{
    public Task<AccessUserData> CreateAsync(
        AccessUserCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<AccessUserCreateRequest, AccessUserData>(
            "/api/access/users",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AccessUserData> ReplaceRolesAsync(
        string userId,
        AccessUserRolesRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchIdempotentDataAsync<AccessUserRolesRequest, AccessUserData>(
            $"/api/access/users/{EscapedUserId(userId)}/roles",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AccessUserData> UpdateStatusAsync(
        string userId,
        AccessUserStatusRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchIdempotentDataAsync<AccessUserStatusRequest, AccessUserData>(
            $"/api/access/users/{EscapedUserId(userId)}",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AccessUserCredentialResult> SetCredentialAsync(
        string userId,
        AccessUserCredentialRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PutDataAsync<AccessUserCredentialRequest, AccessUserCredentialResult>(
            $"/api/internal-auth/users/{EscapedUserId(userId)}/credential",
            request,
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
