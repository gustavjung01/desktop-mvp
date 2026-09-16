using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IOpeningBalanceService
{
    Task<IReadOnlyList<OpeningBalanceWarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default);
    Task<OpeningBalanceLocationEnvelopeData> ListLocationsAsync(string warehouseId, CancellationToken cancellationToken = default);
    Task<OpeningBalanceValidationResultData> ValidateAsync(OpeningBalanceOperatorRequestData request, CancellationToken cancellationToken = default);
    Task<OpeningBalancePostResultData> PostAsync(OpeningBalanceOperatorRequestData request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpeningBalanceImportData>> ListImportsAsync(CancellationToken cancellationToken = default);
}

public sealed class OpeningBalanceService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IOpeningBalanceService
{
    public async Task<IReadOnlyList<OpeningBalanceWarehouseData>> ListWarehousesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<OpeningBalanceWarehouseData[]>(
            "/api/inventory/opening-balances/operator/warehouses",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<OpeningBalanceLocationEnvelopeData> ListLocationsAsync(
        string warehouseId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(warehouseId, out _))
        {
            throw new ArgumentException("Kho đã chọn không hợp lệ.", nameof(warehouseId));
        }

        return apiClient.GetDataAsync<OpeningBalanceLocationEnvelopeData>(
            $"/api/inventory/opening-balances/operator/locations?warehouseId={Uri.EscapeDataString(warehouseId.Trim())}",
            RequireToken(),
            cancellationToken);
    }

    public Task<OpeningBalanceValidationResultData> ValidateAsync(
        OpeningBalanceOperatorRequestData request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return apiClient.PostDataAsync<OpeningBalanceOperatorRequestData, OpeningBalanceValidationResultData>(
            "/api/inventory/opening-balances/operator/validate",
            request,
            RequireToken(),
            cancellationToken);
    }

    public Task<OpeningBalancePostResultData> PostAsync(
        OpeningBalanceOperatorRequestData request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!idempotencyKeys.IsValid(idempotencyKey))
        {
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));
        }

        return apiClient.PostIdempotentDataAsync<OpeningBalanceOperatorRequestData, OpeningBalancePostResultData>(
            "/api/inventory/opening-balances/operator/post",
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<OpeningBalanceImportData>> ListImportsAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<OpeningBalanceImportData[]>(
            "/api/inventory/opening-balances?limit=200",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
