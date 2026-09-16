using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IInventoryCostingService
{
    Task<IReadOnlyList<InventoryCostBalanceData>> ListBalancesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryCostFactData>> ListFactsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryCostAnomalyData>> ListAnomaliesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryCostReconciliationData>> ListReconciliationAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryCostingPeriodData>> ListPeriodsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryCostAdjustmentEventData>> ListAdjustmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryCostDiscrepancyData>> ListDiscrepanciesAsync(CancellationToken cancellationToken = default);
    Task<InventoryCostingRunData?> GetLatestRunAsync(CancellationToken cancellationToken = default);
    Task<InventoryCostRebuildResultData> RebuildAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<JsonElement> OpenPeriodAsync(string periodStart, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<JsonElement> ClosePeriodAsync(string periodStart, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class InventoryCostingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IInventoryCostingService
{
    public async Task<IReadOnlyList<InventoryCostBalanceData>> ListBalancesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryCostBalanceData[]>(
            "/api/inventory/costing/balances",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<InventoryCostFactData>> ListFactsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryCostFactData[]>(
            "/api/inventory/costing/facts?limit=100",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<InventoryCostAnomalyData>> ListAnomaliesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryCostAnomalyData[]>(
            "/api/inventory/costing/anomalies?limit=100",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<InventoryCostReconciliationData>> ListReconciliationAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryCostReconciliationData[]>(
            "/api/inventory/costing/reconciliation?limit=500",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<InventoryCostingPeriodData>> ListPeriodsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryCostingPeriodData[]>(
            "/api/inventory/costing/periods",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<InventoryCostAdjustmentEventData>> ListAdjustmentsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryCostAdjustmentEventData[]>(
            "/api/inventory/costing/adjustments",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<InventoryCostDiscrepancyData>> ListDiscrepanciesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryCostDiscrepancyData[]>(
            "/api/inventory/costing/discrepancies",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<InventoryCostingRunData?> GetLatestRunAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<InventoryCostingRunData?>(
            "/api/inventory/costing/run",
            RequireToken(),
            cancellationToken);

    public Task<InventoryCostRebuildResultData> RebuildAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<object, InventoryCostRebuildResultData>(
            "/api/inventory/costing/rebuild",
            new { },
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    public Task<JsonElement> OpenPeriodAsync(
        string periodStart,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        MutatePeriodAsync("open", periodStart, idempotencyKey, cancellationToken);

    public Task<JsonElement> ClosePeriodAsync(
        string periodStart,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        MutatePeriodAsync("close", periodStart, idempotencyKey, cancellationToken);

    private Task<JsonElement> MutatePeriodAsync(
        string action,
        string periodStart,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ValidateKey(idempotencyKey);
        if (!DateOnly.TryParseExact(periodStart, "yyyy-MM-dd", out _))
        {
            throw new ArgumentException("Kỳ giá vốn không hợp lệ.", nameof(periodStart));
        }

        return apiClient.PostIdempotentDataAsync<InventoryCostPeriodMutationRequest, JsonElement>(
            $"/api/inventory/costing/periods/{action}",
            new InventoryCostPeriodMutationRequest(periodStart),
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private void ValidateKey(string value)
    {
        if (!idempotencyKeys.IsValid(value))
        {
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(value));
        }
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
