using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IAgingReportingService
{
    Task<AgingDashboardData> GetAsync(
        string? warehouseId,
        CancellationToken cancellationToken = default);
}

public sealed class AgingReportingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IAgingReportingService
{
    public Task<AgingDashboardData> GetAsync(
        string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var normalizedWarehouseId = string.IsNullOrWhiteSpace(warehouseId)
            ? null
            : RequireWarehouseId(warehouseId);

        var path = normalizedWarehouseId is null
            ? "/api/reporting/aging"
            : $"/api/reporting/aging?warehouseId={Uri.EscapeDataString(normalizedWarehouseId)}";

        return apiClient.GetDataAsync<AgingDashboardData>(
            path,
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireWarehouseId(string value) =>
        Guid.TryParse(value.Trim(), out _)
            ? value.Trim().ToLowerInvariant()
            : throw new ArgumentException("Kho báo cáo không hợp lệ.", nameof(value));
}
