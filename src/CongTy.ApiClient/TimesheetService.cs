using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ITimesheetService
{
    Task<AttendanceTimesheetResponseData> ListAsync(
        string view,
        string fromDate,
        string toDate,
        string? employeeQuery = null,
        string? branchId = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default);
}

public sealed class TimesheetService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : ITimesheetService
{
    public Task<AttendanceTimesheetResponseData> ListAsync(
        string view,
        string fromDate,
        string toDate,
        string? employeeQuery = null,
        string? branchId = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var normalizedView = string.Equals(view, "monthly", StringComparison.OrdinalIgnoreCase)
            ? "monthly"
            : "employee";
        var maxLimit = normalizedView == "monthly" ? 25 : 100;
        var query = new List<string>
        {
            $"view={Uri.EscapeDataString(normalizedView)}",
            $"from={Uri.EscapeDataString(fromDate)}",
            $"to={Uri.EscapeDataString(toDate)}",
            $"limit={Math.Clamp(limit, 1, maxLimit)}",
            $"offset={Math.Max(0, offset)}",
        };
        AddQuery(query, "employeeQuery", employeeQuery);
        AddQuery(query, "branchId", branchId);

        return apiClient.GetDataAsync<AttendanceTimesheetResponseData>(
            "/api/workforce/attendance/timesheet?" + string.Join("&", query),
            RequireToken(),
            cancellationToken);
    }

    private static void AddQuery(List<string> query, string key, string? value)
    {
        var normalized = value?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
            query.Add($"{key}={Uri.EscapeDataString(normalized)}");
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
