using System.Globalization;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IAuditHistoryService
{
    Task<AuditHistoryData> GetAsync(
        DateTime? from,
        DateTime? to,
        string? cursor = null,
        CancellationToken cancellationToken = default);
}

public sealed class AuditHistoryService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IAuditHistoryService
{
    public Task<AuditHistoryData> GetAsync(
        DateTime? from,
        DateTime? to,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            throw new ArgumentException("Từ ngày không được sau Đến ngày.");

        var query = new List<string>();
        if (from.HasValue)
            query.Add($"from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        if (to.HasValue)
            query.Add($"to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        if (!string.IsNullOrWhiteSpace(cursor))
            query.Add($"cursor={Uri.EscapeDataString(cursor.Trim())}");

        var path = "/api/reporting/audit-history";
        if (query.Count > 0) path += "?" + string.Join("&", query);

        return apiClient.GetDataAsync<AuditHistoryData>(
            path,
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
