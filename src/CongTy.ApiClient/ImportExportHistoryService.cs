using System.Globalization;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IImportExportHistoryService
{
    Task<ImportExportHistoryData> GetAsync(
        DateTime? from,
        DateTime? to,
        string? direction,
        string? status,
        string? definitionKey = null,
        string? cursor = null,
        CancellationToken cancellationToken = default);
}

public sealed class ImportExportHistoryService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IImportExportHistoryService
{
    private static readonly HashSet<string> Directions =
        new(StringComparer.Ordinal) { "IMPORT", "EXPORT" };

    private static readonly HashSet<string> Statuses =
        new(StringComparer.Ordinal) { "queued", "running", "completed", "failed", "cancelled" };

    public Task<ImportExportHistoryData> GetAsync(
        DateTime? from,
        DateTime? to,
        string? direction,
        string? status,
        string? definitionKey = null,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            throw new ArgumentException("Từ ngày không được sau Đến ngày.");

        var normalizedDirection = Normalize(direction);
        var normalizedStatus = Normalize(status);
        var normalizedDefinitionKey = Normalize(definitionKey);

        if (normalizedDirection is not null && !Directions.Contains(normalizedDirection))
            throw new ArgumentException("Loại thao tác không hợp lệ.");
        if (normalizedStatus is not null && !Statuses.Contains(normalizedStatus))
            throw new ArgumentException("Trạng thái không hợp lệ.");
        if (normalizedDefinitionKey is { Length: > 160 })
            throw new ArgumentException("Nhóm dữ liệu không hợp lệ.");

        var query = new List<string>();
        if (from.HasValue)
            query.Add($"from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        if (to.HasValue)
            query.Add($"to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        if (normalizedDirection is not null)
            query.Add($"direction={Uri.EscapeDataString(normalizedDirection)}");
        if (normalizedStatus is not null)
            query.Add($"status={Uri.EscapeDataString(normalizedStatus)}");
        if (normalizedDefinitionKey is not null)
            query.Add($"definitionKey={Uri.EscapeDataString(normalizedDefinitionKey)}");
        if (!string.IsNullOrWhiteSpace(cursor))
            query.Add($"cursor={Uri.EscapeDataString(cursor.Trim())}");

        var path = "/api/reporting/import-export-history";
        if (query.Count > 0) path += "?" + string.Join("&", query);

        return apiClient.GetDataAsync<ImportExportHistoryData>(
            path,
            RequireToken(),
            cancellationToken);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
