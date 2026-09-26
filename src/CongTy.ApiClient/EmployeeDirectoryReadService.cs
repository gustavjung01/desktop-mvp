using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IEmployeeDirectoryReadService
{
    Task<IReadOnlyList<EmployeeDirectoryData>> ListEmployeesAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDirectoryData> GetEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeOrganizationCatalogData> GetOrganizationAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDirectoryBranchData>> ListBranchesAsync(CancellationToken cancellationToken = default);
}

public sealed class EmployeeDirectoryReadService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IEmployeeDirectoryReadService
{
    public Task<IReadOnlyList<EmployeeDirectoryData>> ListEmployeesAsync(
        CancellationToken cancellationToken = default) =>
        ListAllAsync<EmployeeDirectoryData>("/api/employees", cancellationToken);

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

    public Task<EmployeeOrganizationCatalogData> GetOrganizationAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<EmployeeOrganizationCatalogData>(
            "/api/employees/organization",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<EmployeeDirectoryBranchData>> ListBranchesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<EmployeeDirectoryBranchData[]>(
            "/api/branches?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    private async Task<IReadOnlyList<T>> ListAllAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        const int limit = 1000;
        const int maxPages = 200;
        var rows = new List<T>();
        for (var page = 0; page < maxPages; page++)
        {
            var offset = rows.Count;
            var separator = path.Contains('?') ? '&' : '?';
            var batch = await apiClient.GetDataAsync<T[]>(
                $"{path}{separator}limit={limit}&offset={offset}",
                RequireToken(),
                cancellationToken).ConfigureAwait(false);
            rows.AddRange(batch);
            if (batch.Length < limit) return rows;
        }

        throw new InvalidOperationException("Danh sách nhân sự vượt giới hạn xuất an toàn 200.000 dòng.");
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}