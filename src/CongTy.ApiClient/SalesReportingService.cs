using System.Globalization;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ISalesReportingService
{
    Task<SalesReportingDashboardData> GetAsync(DateTime? from, DateTime? to, string? warehouseId, string? productGroupId, string? brandId, string? customerGroupId, bool includeZeroProducts, CancellationToken cancellationToken = default);
    Task<ApiDownloadFile> ExportAsync(DateTime? from, DateTime? to, string? warehouseId, string? productGroupId, string? brandId, string? customerGroupId, bool includeZeroProducts, string dimension, string format, IReadOnlyCollection<string> columns, string? quantityDisplay, string? sort, CancellationToken cancellationToken = default);
}

public sealed class SalesReportingService(CompanyApiClient apiClient, IAuthenticatedSessionAccessor sessionAccessor) : ISalesReportingService
{
    private static readonly HashSet<string> Dimensions = new(StringComparer.Ordinal) { "customers", "customerGroups", "channels", "products", "productGroups", "employees" };
    private static readonly HashSet<string> AnalysisDimensions = new(StringComparer.Ordinal) { "products", "customerGroups", "channels", "productGroups" };
    private static readonly HashSet<string> AnalysisMetrics = new(StringComparer.Ordinal) { "revenue", "quantity", "both" };
    private static readonly HashSet<string> QuantityDisplays = new(StringComparer.Ordinal) { "sold", "carton", "base" };
    private static readonly HashSet<string> AnalysisSorts = new(StringComparer.Ordinal) { "name-asc", "revenue-desc", "revenue-asc", "quantity-desc", "quantity-asc" };
    private static readonly HashSet<string> Formats = new(StringComparer.Ordinal) { "xlsx", "csv" };

    public Task<SalesReportingDashboardData> GetAsync(DateTime? from, DateTime? to, string? warehouseId, string? productGroupId, string? brandId, string? customerGroupId, bool includeZeroProducts, CancellationToken cancellationToken = default)
    {
        var path = BuildPath("/api/reporting/sales", from, to, warehouseId, productGroupId, brandId, customerGroupId, includeZeroProducts, null, null, null, null, null);
        return apiClient.GetDataAsync<SalesReportingDashboardData>(path, RequireToken(), cancellationToken);
    }

    public Task<ApiDownloadFile> ExportAsync(DateTime? from, DateTime? to, string? warehouseId, string? productGroupId, string? brandId, string? customerGroupId, bool includeZeroProducts, string dimension, string format, IReadOnlyCollection<string> columns, string? quantityDisplay, string? sort, CancellationToken cancellationToken = default)
    {
        var analysis = IsAnalysisDimension(dimension);
        if (!analysis && !Dimensions.Contains(dimension)) throw new ArgumentException("Chiều phân tích không hợp lệ.", nameof(dimension));
        if (!Formats.Contains(format) || (analysis && format != "xlsx")) throw new ArgumentException("Định dạng xuất file không hợp lệ.", nameof(format));
        if (columns.Count == 0) throw new ArgumentException("Cần chọn ít nhất một cột để xuất.", nameof(columns));
        if (analysis)
        {
            if (!string.IsNullOrWhiteSpace(quantityDisplay) && !QuantityDisplays.Contains(quantityDisplay)) throw new ArgumentException("Cách hiển thị sản lượng không hợp lệ.", nameof(quantityDisplay));
            if (!string.IsNullOrWhiteSpace(sort) && !AnalysisSorts.Contains(sort)) throw new ArgumentException("Cách sắp xếp báo cáo không hợp lệ.", nameof(sort));
        }

        var path = BuildPath("/api/reporting/sales-export", from, to, warehouseId, productGroupId, brandId, customerGroupId, includeZeroProducts, dimension, format, columns, analysis ? quantityDisplay : null, analysis ? sort : null);
        return apiClient.GetFileAsync(path, RequireToken(), cancellationToken);
    }

    private static bool IsAnalysisDimension(string value)
    {
        var parts = value.Split('.', StringSplitOptions.TrimEntries);
        return parts.Length == 4 && parts[0] == "analysis" && AnalysisDimensions.Contains(parts[1]) && AnalysisDimensions.Contains(parts[2]) && parts[1] != parts[2] && AnalysisMetrics.Contains(parts[3]);
    }

    private static string BuildPath(string endpoint, DateTime? from, DateTime? to, string? warehouseId, string? productGroupId, string? brandId, string? customerGroupId, bool includeZeroProducts, string? dimension, string? format, IReadOnlyCollection<string>? columns, string? quantityDisplay, string? sort)
    {
        var query = new List<string>();
        AddDate(query, "from", from);
        AddDate(query, "to", to);
        AddText(query, "warehouseId", warehouseId);
        AddText(query, "productGroupId", productGroupId);
        AddText(query, "brandId", brandId);
        AddText(query, "customerGroupId", customerGroupId);
        if (includeZeroProducts) query.Add("includeZeroProducts=true");
        AddText(query, "dimension", dimension);
        AddText(query, "format", format);
        AddText(query, "quantityDisplay", quantityDisplay);
        AddText(query, "sort", sort);
        if (columns is not null)
            foreach (var column in columns.Where(value => !string.IsNullOrWhiteSpace(value))) query.Add($"column={Uri.EscapeDataString(column.Trim())}");
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

    private string RequireToken() => string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken) ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.") : sessionAccessor.CurrentToken;
}
