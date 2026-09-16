using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IManualInboundService
{
    Task<IReadOnlyList<ManualInboundWarehouseOptionData>> ListWarehousesAsync(CancellationToken cancellationToken = default);
    Task<ManualInboundLocationResponseData> ListLocationsAsync(string warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManualInboundSupplierOptionData>> ListSuppliersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManualInboundProductOptionData>> SearchProductsAsync(string warehouseId, string search, CancellationToken cancellationToken = default);
    Task<ManualInboundPreviewData> PreviewAsync(ManualInboundOperatorRequest request, CancellationToken cancellationToken = default);
    Task<JsonElement> ConfirmAsync(ManualInboundOperatorRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManualInboundHistoryDocumentData>> SearchHistoryAsync(string? inboundType, string? referenceNumber, CancellationToken cancellationToken = default);
    Task<ManualInboundHistoryMovementData> ReadHistoryDetailAsync(string documentId, CancellationToken cancellationToken = default);
    Task<JsonElement> ReverseAsync(string documentId, ManualInboundReverseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class ManualInboundService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IManualInboundService
{
    public async Task<IReadOnlyList<ManualInboundWarehouseOptionData>> ListWarehousesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ManualInboundWarehouseOptionData[]>(
            "/api/inventory/manual-inbounds/operator/warehouses",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<ManualInboundLocationResponseData> ListLocationsAsync(string warehouseId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<ManualInboundLocationResponseData>(
            $"/api/inventory/manual-inbounds/operator/locations?warehouseId={Uri.EscapeDataString(RequireId(warehouseId, "Mã kho"))}",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<ManualInboundSupplierOptionData>> ListSuppliersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ManualInboundSupplierOptionData[]>(
            "/api/inventory/manual-inbounds/operator/suppliers",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ManualInboundProductOptionData>> SearchProductsAsync(
        string warehouseId,
        string search,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim();
        if (normalizedSearch.Length == 0) return [];
        return await apiClient.GetDataAsync<ManualInboundProductOptionData[]>(
            $"/api/inventory/manual-inbounds/operator/products?warehouseId={Uri.EscapeDataString(RequireId(warehouseId, "Mã kho"))}&search={Uri.EscapeDataString(normalizedSearch)}&limit=30",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
    }

    public Task<ManualInboundPreviewData> PreviewAsync(
        ManualInboundOperatorRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostDataAsync<ManualInboundOperatorRequest, ManualInboundPreviewData>(
            "/api/inventory/manual-inbounds/operator/preview",
            request,
            RequireToken(),
            cancellationToken);

    public Task<JsonElement> ConfirmAsync(
        ManualInboundOperatorRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<ManualInboundOperatorRequest, JsonElement>(
            "/api/inventory/manual-inbounds/operator/confirm",
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<ManualInboundHistoryDocumentData>> SearchHistoryAsync(
        string? inboundType,
        string? referenceNumber,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(inboundType)) query.Add($"inboundType={Uri.EscapeDataString(inboundType.Trim())}");
        if (!string.IsNullOrWhiteSpace(referenceNumber)) query.Add($"referenceNumber={Uri.EscapeDataString(referenceNumber.Trim())}");
        query.Add("limit=100");
        query.Add("offset=0");

        return await apiClient.GetDataAsync<ManualInboundHistoryDocumentData[]>(
            $"/api/inventory/manual-inbounds/operator/history?{string.Join("&", query)}",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
    }

    public Task<ManualInboundHistoryMovementData> ReadHistoryDetailAsync(
        string documentId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<ManualInboundHistoryMovementData>(
            $"/api/inventory/manual-inbounds/operator/history-detail?documentId={Uri.EscapeDataString(RequireId(documentId, "Mã chứng từ"))}",
            RequireToken(),
            cancellationToken);

    public Task<JsonElement> ReverseAsync(
        string documentId,
        ManualInboundReverseRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<ManualInboundReverseRequest, JsonElement>(
            $"/api/inventory/manual-inbounds/{Uri.EscapeDataString(RequireId(documentId, "Mã chứng từ"))}/reverse",
            request,
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

    private static string RequireId(string value, string label) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException($"{label} không hợp lệ.", nameof(value));
}
