using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IEmployeeDirectoryMutationService
{
    Task<EmployeeDirectoryData> CreateAsync(
        EmployeeDirectoryCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<EmployeeDirectoryData> UpdateAsync(
        string employeeId,
        EmployeeDirectoryUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<EmployeeDirectoryData> ToggleAsync(
        string employeeId,
        EmployeeDirectoryToggleRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<EmployeeOrganizationMutationResult> SaveOrganizationAsync(
        EmployeeOrganizationMutationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class EmployeeDirectoryMutationService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IEmployeeDirectoryMutationService
{
    public Task<EmployeeDirectoryData> CreateAsync(
        EmployeeDirectoryCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<EmployeeDirectoryCreateRequest, EmployeeDirectoryData>(
            "/api/employees",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<EmployeeDirectoryData> UpdateAsync(
        string employeeId,
        EmployeeDirectoryUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchIdempotentDataAsync<EmployeeDirectoryUpdateRequest, EmployeeDirectoryData>(
            BuildEmployeePath(employeeId),
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<EmployeeDirectoryData> ToggleAsync(
        string employeeId,
        EmployeeDirectoryToggleRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchIdempotentDataAsync<EmployeeDirectoryToggleRequest, EmployeeDirectoryData>(
            BuildEmployeePath(employeeId),
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<EmployeeOrganizationMutationResult> SaveOrganizationAsync(
        EmployeeOrganizationMutationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<EmployeeOrganizationMutationRequest, EmployeeOrganizationMutationResult>(
            "/api/employees/organization",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string BuildEmployeePath(string employeeId)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
            throw new ArgumentException("Hồ sơ nhân sự không hợp lệ.", nameof(employeeId));

        return $"/api/employees/{Uri.EscapeDataString(employeeId.Trim())}";
    }
}
