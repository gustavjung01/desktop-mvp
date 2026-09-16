using System.Globalization;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IGrossMarginReportingService
{
    Task<GrossMarginReportingDashboardData> GetAsync(
        DateTime? from,
        DateTime? to,
        string? warehouseId,
        CancellationToken cancellationToken = default);

    Task<ApiDownloadFile> ExportAsync(
        DateTime? from,
        DateTime? to,
        string? warehouseId,
        string dimension,
        string format,
        IReadOnlyCollection<string> columns,
        CancellationToken cancellationToken = default);
}

public sealed class GrossMarginReportingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IGrossMarginReportingService
{
    private static readonly HashSet<string> Dimensions = new(StringComparer.Ordinal)
    {
        "customers", "skus", "lines", "exceptions"
    };

    private static readonly HashSet<string> Formats = new(StringComparer.Ordinal)
    {
        "xlsx", "csv"
    };

    public Task<GrossMarginReportingDashboardData> GetAsync(
        DateTime? from,
        DateTime? to,
        string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var path = BuildPath(
            "/api/reporting/gross-margin",
            from,
            to,
            warehouseId,
            dimension: null,
            format: null,
            columns: null);

        return apiClient.GetDataAsync<GrossMarginReportingDashboardData>(
            path,
            RequireToken(),
            cancellationToken);
    }

    public Task<ApiDownloadFile> ExportAsync(
        DateTime? from,
        DateTime? to,
        string? warehouseId,
        string dimension,
        string format,
        IReadOnlyCollection<string> columns,
        CancellationToken cancellationToken = default)
    {
        if (!Dimensions.Contains(dimension))
            throw new ArgumentException("Nội dung xuất không hợp lệ.", nameof(dimension));
        if (!Formats.Contains(format))
            throw new ArgumentException("Định dạng xuất file không hợp lệ.", nameof(format));
        if (columns.Count == 0)
            throw new ArgumentException("Cần chọn ít nhất một cột để xuất.", nameof(columns));

        var path = BuildPath(
            "/api/reporting/gross-margin-export",
            from,
            to,
            warehouseId,
            dimension,
            format,
            columns);

        return apiClient.GetFileAsync(path, RequireToken(), cancellationToken);
    }

    private static string BuildPath(
        string endpoint,
        DateTime? from,
        DateTime? to,
        string? warehouseId,
        string? dimension,
        string? format,
        IReadOnlyCollection<string>? columns)
    {
        var query = new List<string>();
        AddDate(query, "from", from);
        AddDate(query, "to", to);
        AddText(query, "warehouseId", warehouseId);
        AddText(query, "dimension", dimension);
        AddText(query, "format", format);

        if (columns is not null)
        {
            foreach (var column in columns.Where(value => !string.IsNullOrWhiteSpace(value)))
                query.Add($"column={Uri.EscapeDataString(column.Trim())}");
        }

        return query.Count == 0 ? endpoint : $"{endpoint}?{string.Join("&", query)}";
    }

    private static void AddDate(ICollection<string> query, string key, DateTime? value)
    {
        if (value is null) return;
        query.Add($"{key}={Uri.EscapeDataString(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
    }

    private static void AddText(ICollection<string> query, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        query.Add($"{key}={Uri.EscapeDataString(value.Trim())}");
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
