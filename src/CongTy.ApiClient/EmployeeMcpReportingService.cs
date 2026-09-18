using System.Globalization;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IEmployeeMcpReportingService
{
    Task<EmployeeMcpDashboardData> GetAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}

public sealed class EmployeeMcpReportingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IEmployeeMcpReportingService
{
    public Task<EmployeeMcpDashboardData> GetAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddDate(query, "from", from);
        AddDate(query, "to", to);
        var path = query.Count == 0
            ? "/api/reporting/employee-mcp"
            : $"/api/reporting/employee-mcp?{string.Join("&", query)}";

        return apiClient.GetDataAsync<EmployeeMcpDashboardData>(
            path,
            RequireToken(),
            cancellationToken);
    }

    private static void AddDate(ICollection<string> query, string key, DateTime? value)
    {
        if (value is null) return;
        query.Add($"{key}={Uri.EscapeDataString(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
