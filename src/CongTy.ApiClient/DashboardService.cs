using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IDashboardService
{
    Task<IReadOnlyList<BranchData>> ListBranchesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseLocationData>> ListLocationsAsync(CancellationToken cancellationToken = default);
    Task<DashboardSalesReportData> GetSalesAsync(CancellationToken cancellationToken = default);
    Task<InventoryReportingDashboardData> GetInventoryAsync(CancellationToken cancellationToken = default);
    Task<DashboardLogisticsData> GetLogisticsAsync(CancellationToken cancellationToken = default);
    Task<DashboardAgingData> GetAgingAsync(CancellationToken cancellationToken = default);
}

public sealed class DashboardService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    IInternalOrganizationService organization) : IDashboardService
{
    public Task<IReadOnlyList<BranchData>> ListBranchesAsync(CancellationToken cancellationToken = default) =>
        organization.ListBranchesAsync(cancellationToken);

    public Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default) =>
        organization.ListWarehousesAsync(cancellationToken);

    public Task<IReadOnlyList<WarehouseLocationData>> ListLocationsAsync(CancellationToken cancellationToken = default) =>
        organization.ListLocationsAsync(cancellationToken);

    public Task<DashboardSalesReportData> GetSalesAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<DashboardSalesReportData>(
            "/api/reporting/sales",
            RequireToken(),
            cancellationToken);

    public Task<InventoryReportingDashboardData> GetInventoryAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<InventoryReportingDashboardData>(
            "/api/reporting/inventory",
            RequireToken(),
            cancellationToken);

    public Task<DashboardLogisticsData> GetLogisticsAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<DashboardLogisticsData>(
            "/api/reporting/logistics",
            RequireToken(),
            cancellationToken);

    public Task<DashboardAgingData> GetAgingAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<DashboardAgingData>(
            "/api/reporting/aging",
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
