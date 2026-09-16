using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IFulfillmentService
{
    Task<IReadOnlyList<FulfillmentWorkItemData>> ListWorkAsync(CancellationToken cancellationToken = default);
    Task<FulfillmentSuggestionData> GetSuggestionsAsync(string demandId, CancellationToken cancellationToken = default);
    Task<FulfillmentOrderAllocationResultData> AllocateOrderAsync(string salesOrderId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<FulfillmentMutationResultData> AllocateDemandAsync(string demandId, string mode, string? quantity, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<FulfillmentMutationResultData> PickAsync(string allocationId, string quantity, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<FulfillmentMutationResultData> PackAsync(string allocationId, string quantity, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryHoldBreakdownData> GetHoldBreakdownAsync(string warehouseId, string baseVariantId, string salesOrderId, CancellationToken cancellationToken = default);
}

public sealed class FulfillmentService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IFulfillmentService
{
    public async Task<IReadOnlyList<FulfillmentWorkItemData>> ListWorkAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<FulfillmentWorkItemData[]>(
            "/api/inventory/fulfillment-work?limit=500",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<FulfillmentSuggestionData> GetSuggestionsAsync(
        string demandId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<FulfillmentSuggestionData>(
            $"/api/inventory/fulfillment-demands/{Uri.EscapeDataString(RequireId(demandId, nameof(demandId)))}/suggestions",
            RequireToken(),
            cancellationToken);

    public Task<FulfillmentOrderAllocationResultData> AllocateOrderAsync(
        string salesOrderId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<object, FulfillmentOrderAllocationResultData>(
            $"/api/inventory/fulfillment-orders/{Uri.EscapeDataString(RequireId(salesOrderId, nameof(salesOrderId)))}/allocate",
            new { mode = "AUTO" },
            RequireKey(idempotencyKey),
            RequireToken(),
            cancellationToken);

    public Task<FulfillmentMutationResultData> AllocateDemandAsync(
        string demandId,
        string mode,
        string? quantity,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedMode = string.Equals(mode, "QUANTITY", StringComparison.Ordinal) ? "QUANTITY" : "AUTO";
        object payload = normalizedMode == "QUANTITY"
            ? new { mode = normalizedMode, quantity = quantity?.Trim() }
            : new { mode = normalizedMode };

        return apiClient.PostIdempotentDataAsync<object, FulfillmentMutationResultData>(
            $"/api/inventory/fulfillment-demands/{Uri.EscapeDataString(RequireId(demandId, nameof(demandId)))}/allocate",
            payload,
            RequireKey(idempotencyKey),
            RequireToken(),
            cancellationToken);
    }

    public Task<FulfillmentMutationResultData> PickAsync(
        string allocationId,
        string quantity,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostProgressAsync(allocationId, "pick", quantity, idempotencyKey, cancellationToken);

    public Task<FulfillmentMutationResultData> PackAsync(
        string allocationId,
        string quantity,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostProgressAsync(allocationId, "pack", quantity, idempotencyKey, cancellationToken);

    public Task<InventoryHoldBreakdownData> GetHoldBreakdownAsync(
        string warehouseId,
        string baseVariantId,
        string salesOrderId,
        CancellationToken cancellationToken = default)
    {
        var path = $"/api/inventory/holds?warehouseId={Uri.EscapeDataString(RequireId(warehouseId, nameof(warehouseId)))}"
            + $"&baseVariantId={Uri.EscapeDataString(RequireId(baseVariantId, nameof(baseVariantId)))}"
            + $"&excludeSalesOrderId={Uri.EscapeDataString(RequireId(salesOrderId, nameof(salesOrderId)))}";
        return apiClient.GetDataAsync<InventoryHoldBreakdownData>(path, RequireToken(), cancellationToken);
    }

    private Task<FulfillmentMutationResultData> PostProgressAsync(
        string allocationId,
        string action,
        string quantity,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(quantity))
            throw new ArgumentException("Số lượng thao tác không hợp lệ.", nameof(quantity));

        return apiClient.PostIdempotentDataAsync<object, FulfillmentMutationResultData>(
            $"/api/inventory/fulfillment-allocations/{Uri.EscapeDataString(RequireId(allocationId, nameof(allocationId)))}/{action}",
            new { quantity = quantity.Trim() },
            RequireKey(idempotencyKey),
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private string RequireKey(string value) =>
        idempotencyKeys.IsValid(value)
            ? value.Trim()
            : throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(value));

    private static string RequireId(string value, string name) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã dữ liệu không hợp lệ.", name);
}
