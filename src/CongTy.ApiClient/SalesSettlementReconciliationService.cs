using System.Globalization;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ISalesSettlementReconciliationService
{
    Task<SalesSettlementReconciliationData> GetAsync(
        DateTime? from,
        DateTime? to,
        string? search,
        string? status,
        CancellationToken cancellationToken = default);
}

public sealed class SalesSettlementReconciliationService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : ISalesSettlementReconciliationService
{
    private static readonly HashSet<string> AllowedStatuses =
        new(["all", "matched", "mismatch"], StringComparer.Ordinal);

    public Task<SalesSettlementReconciliationData> GetAsync(
        DateTime? from,
        DateTime? to,
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (from is not null && to is not null && from.Value.Date > to.Value.Date)
            throw new ArgumentException("Ngày bắt đầu không được sau ngày kết thúc.", nameof(from));

        var normalizedSearch = (search ?? string.Empty).Trim();
        if (normalizedSearch.Length > 160)
            throw new ArgumentException("Từ khóa đối soát không được vượt quá 160 ký tự.", nameof(search));

        var normalizedStatus = string.IsNullOrWhiteSpace(status)
            ? "all"
            : status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
            throw new ArgumentException("Trạng thái đối soát không hợp lệ.", nameof(status));

        var query = new List<string> { "limit=100" };
        AddDate(query, "from", from);
        AddDate(query, "to", to);
        if (normalizedSearch.Length > 0)
            query.Add($"search={Uri.EscapeDataString(normalizedSearch)}");
        if (normalizedStatus != "all")
            query.Add($"status={Uri.EscapeDataString(normalizedStatus)}");

        return apiClient.GetDataAsync<SalesSettlementReconciliationData>(
            $"/api/accounting/reconciliation?{string.Join("&", query)}",
            RequireToken(),
            cancellationToken);
    }

    private static void AddDate(List<string> query, string name, DateTime? value)
    {
        if (value is null) return;
        query.Add($"{name}={Uri.EscapeDataString(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
