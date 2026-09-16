using System.Globalization;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryBalanceData>> ListBalancesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryHistoryData>> ListHistoryAsync(string warehouseId, string baseVariantId, int page, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryLotData>> ListLotsAsync(CancellationToken cancellationToken = default);
    Task<InventoryReportingDashboardData> GetReportAsync(DateTime? from, DateTime? to, string? warehouseId, int slowDays, CancellationToken cancellationToken = default);
    Task<ApiDownloadFile> ExportReportAsync(
        DateTime? from,
        DateTime? to,
        string? warehouseId,
        int slowDays,
        string dimension,
        string format,
        IReadOnlyCollection<string> columns,
        CancellationToken cancellationToken = default);
    Task<InventoryHoldBreakdownData> GetHoldBreakdownAsync(string warehouseId, string baseVariantId, CancellationToken cancellationToken = default);
}

public sealed class InventoryService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IInventoryService
{
    private const int PageSize = 1000;
    private const int HistoryPageSize = 50;
    private static readonly HashSet<string> ReportExportDimensions = new(StringComparer.Ordinal)
    {
        "overview", "positions", "movement", "slow-moving", "lots", "exceptions"
    };
    private static readonly HashSet<string> ReportExportFormats = new(StringComparer.Ordinal)
    {
        "xlsx", "csv"
    };

    public Task<IReadOnlyList<InventoryBalanceData>> ListBalancesAsync(CancellationToken cancellationToken = default) =>
        ReadAllAsync<InventoryBalanceData>("/api/inventory/balances", cancellationToken);

    public async Task<IReadOnlyList<InventoryHistoryData>> ListHistoryAsync(
        string warehouseId,
        string baseVariantId,
        int page,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(0, page);
        var offset = checked(safePage * HistoryPageSize);
        var path = $"/api/inventory/balances/history?warehouseId={Uri.EscapeDataString(RequireId(warehouseId))}&baseVariantId={Uri.EscapeDataString(RequireId(baseVariantId))}&scope=warehouse&limit={HistoryPageSize + 1}&offset={offset}";
        return await apiClient.GetDataAsync<InventoryHistoryData[]>(path, RequireToken(), cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<InventoryLotData>> ListLotsAsync(CancellationToken cancellationToken = default) =>
        ReadAllAsync<InventoryLotData>("/api/inventory/lots", cancellationToken);

    public Task<InventoryReportingDashboardData> GetReportAsync(
        DateTime? from,
        DateTime? to,
        string? warehouseId,
        int slowDays,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (from is not null) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        if (to is not null) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        if (!string.IsNullOrWhiteSpace(warehouseId)) query.Add($"warehouseId={Uri.EscapeDataString(RequireId(warehouseId))}");
        query.Add($"slowDays={Math.Clamp(slowDays, 30, 365)}");
        return apiClient.GetDataAsync<InventoryReportingDashboardData>(
            $"/api/reporting/inventory?{string.Join("&", query)}",
            RequireToken(),
            cancellationToken);
    }


    public Task<ApiDownloadFile> ExportReportAsync(
        DateTime? from,
        DateTime? to,
        string? warehouseId,
        int slowDays,
        string dimension,
        string format,
        IReadOnlyCollection<string> columns,
        CancellationToken cancellationToken = default)
    {
        if (!ReportExportDimensions.Contains(dimension))
            throw new ArgumentException("Nội dung xuất báo cáo không hợp lệ.", nameof(dimension));
        if (!ReportExportFormats.Contains(format))
            throw new ArgumentException("Định dạng xuất file không hợp lệ.", nameof(format));
        if (columns.Count == 0)
            throw new ArgumentException("Cần chọn ít nhất một cột để xuất.", nameof(columns));
        if (slowDays is < 30 or > 365)
            throw new ArgumentOutOfRangeException(nameof(slowDays), "Ngưỡng hàng chậm luân chuyển phải từ 30 đến 365 ngày.");

        var query = new List<string>();
        if (from is not null) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        if (to is not null) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        if (!string.IsNullOrWhiteSpace(warehouseId)) query.Add($"warehouseId={Uri.EscapeDataString(RequireId(warehouseId))}");
        query.Add($"slowDays={slowDays}");
        query.Add($"dimension={Uri.EscapeDataString(dimension)}");
        query.Add($"format={Uri.EscapeDataString(format)}");
        foreach (var column in columns.Where(value => !string.IsNullOrWhiteSpace(value)))
            query.Add($"column={Uri.EscapeDataString(column.Trim())}");

        return apiClient.GetFileAsync(
            $"/api/inventory/reporting-export?{string.Join("&", query)}",
            RequireToken(),
            cancellationToken);
    }

    public Task<InventoryHoldBreakdownData> GetHoldBreakdownAsync(
        string warehouseId,
        string baseVariantId,
        CancellationToken cancellationToken = default)
    {
        var path = $"/api/inventory/holds?warehouseId={Uri.EscapeDataString(RequireId(warehouseId))}&baseVariantId={Uri.EscapeDataString(RequireId(baseVariantId))}";
        return apiClient.GetDataAsync<InventoryHoldBreakdownData>(path, RequireToken(), cancellationToken);
    }

    private async Task<IReadOnlyList<T>> ReadAllAsync<T>(string path, CancellationToken cancellationToken)
    {
        var rows = new List<T>();
        var offset = 0;
        while (true)
        {
            var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            var page = await apiClient.GetDataAsync<T[]>(
                $"{path}{separator}limit={PageSize}&offset={offset}",
                RequireToken(),
                cancellationToken).ConfigureAwait(false);
            rows.AddRange(page);
            if (page.Length < PageSize) return rows;
            offset = checked(offset + PageSize);
        }
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã dữ liệu không hợp lệ.", nameof(value));
}
