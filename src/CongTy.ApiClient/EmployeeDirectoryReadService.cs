using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IEmployeeDirectoryReadService
{
    Task<IReadOnlyList<EmployeeDirectoryData>> ListEmployeesAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDirectoryData> GetEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDirectoryBranchData>> ListBranchesAsync(CancellationToken cancellationToken = default);
}

public sealed class EmployeeDirectoryReadService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IEmployeeDirectoryReadService
{
    public async Task<IReadOnlyList<EmployeeDirectoryData>> ListEmployeesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<EmployeeDirectoryData[]>(
            "/api/employees?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<EmployeeDirectoryData> GetEmployeeAsync(
        string employeeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
            throw new ArgumentException("Nhân sự không hợp lệ.", nameof(employeeId));

        return apiClient.GetDataAsync<EmployeeDirectoryData>(
            $"/api/employees/{Uri.EscapeDataString(employeeId.Trim())}",
            RequireToken(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeDirectoryBranchData>> ListBranchesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<EmployeeDirectoryBranchData[]>(
            "/api/branches?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}