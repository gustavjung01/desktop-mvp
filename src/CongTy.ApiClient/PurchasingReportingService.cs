using System.Globalization;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IPurchasingReportingService
{
    Task<PurchasingReportingDashboardData> GetAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}

public sealed class PurchasingReportingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IPurchasingReportingService
{
    public Task<PurchasingReportingDashboardData> GetAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (from is not null)
        {
            query.Add($"from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        }

        if (to is not null)
        {
            query.Add($"to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        }

        var suffix = query.Count == 0 ? string.Empty : $"?{string.Join("&", query)}";
        return apiClient.GetDataAsync<PurchasingReportingDashboardData>(
            $"/api/reporting/purchasing{suffix}",
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
