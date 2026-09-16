using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ILogisticsReportingService
{
    Task<LogisticsReportingDashboardData> GetAsync(
        string? from = null,
        string? to = null,
        string? warehouseId = null,
        CancellationToken cancellationToken = default);
}

public sealed class LogisticsReportingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : ILogisticsReportingService
{
    public Task<LogisticsReportingDashboardData> GetAsync(
        string? from = null,
        string? to = null,
        string? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(from))
        {
            query.Add($"from={Uri.EscapeDataString(from.Trim())}");
        }
        if (!string.IsNullOrWhiteSpace(to))
        {
            query.Add($"to={Uri.EscapeDataString(to.Trim())}");
        }
        if (!string.IsNullOrWhiteSpace(warehouseId))
        {
            if (!Guid.TryParse(warehouseId, out _))
            {
                throw new ArgumentException("Kho báo cáo không hợp lệ.", nameof(warehouseId));
            }
            query.Add($"warehouseId={Uri.EscapeDataString(warehouseId.Trim())}");
        }

        var path = "/api/reporting/logistics";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        return apiClient.GetDataAsync<LogisticsReportingDashboardData>(
            path,
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
