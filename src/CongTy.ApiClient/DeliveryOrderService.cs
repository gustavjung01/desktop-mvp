using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IDeliveryOrderService
{
    Task<IReadOnlyList<DeliveryOrderEligibilityData>> ListEligibilityAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeliveryOrderData>> ListAsync(CancellationToken cancellationToken = default);
    Task<DeliveryOrderData> GetAsync(string deliveryOrderId, CancellationToken cancellationToken = default);
    Task<DeliveryOrderCreateResultData> CreateAsync(DeliveryOrderCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task ConfirmAsync(string deliveryOrderId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task CancelAsync(string deliveryOrderId, DeliveryOrderCancelRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task PickupHandoverAsync(string deliveryOrderId, DeliveryOrderHandoverRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task ManualHandoverAsync(string deliveryOrderId, DeliveryOrderHandoverRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task ReverseInventoryIssueAsync(string deliveryOrderId, DeliveryOrderReverseIssueRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class DeliveryOrderService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IDeliveryOrderService
{
    public async Task<IReadOnlyList<DeliveryOrderEligibilityData>> ListEligibilityAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<DeliveryOrderEligibilityData[]>(
            "/api/delivery-orders/eligibility?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<DeliveryOrderData>> ListAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<DeliveryOrderData[]>(
            "/api/delivery-orders?limit=500&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<DeliveryOrderData> GetAsync(string deliveryOrderId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<DeliveryOrderData>(
            $"/api/delivery-orders/{Uri.EscapeDataString(RequireId(deliveryOrderId))}",
            RequireToken(),
            cancellationToken);

    public Task<DeliveryOrderCreateResultData> CreateAsync(
        DeliveryOrderCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<DeliveryOrderCreateRequest, DeliveryOrderCreateResultData>(
            "/api/delivery-orders", request, idempotencyKey, cancellationToken);

    public async Task ConfirmAsync(string deliveryOrderId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _ = await PostAsync<object, JsonElement>(
            ActionPath(deliveryOrderId, "confirm"), new { }, idempotencyKey, cancellationToken).ConfigureAwait(false);

    public async Task CancelAsync(string deliveryOrderId, DeliveryOrderCancelRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _ = await PostAsync<DeliveryOrderCancelRequest, JsonElement>(
            ActionPath(deliveryOrderId, "cancel"), request, idempotencyKey, cancellationToken).ConfigureAwait(false);

    public async Task PickupHandoverAsync(string deliveryOrderId, DeliveryOrderHandoverRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _ = await PostAsync<DeliveryOrderHandoverRequest, JsonElement>(
            ActionPath(deliveryOrderId, "pickup-handover"), request, idempotencyKey, cancellationToken).ConfigureAwait(false);

    public async Task ManualHandoverAsync(string deliveryOrderId, DeliveryOrderHandoverRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _ = await PostAsync<DeliveryOrderHandoverRequest, JsonElement>(
            ActionPath(deliveryOrderId, "manual-handover"), request, idempotencyKey, cancellationToken).ConfigureAwait(false);

    public async Task ReverseInventoryIssueAsync(string deliveryOrderId, DeliveryOrderReverseIssueRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _ = await PostAsync<DeliveryOrderReverseIssueRequest, JsonElement>(
            ActionPath(deliveryOrderId, "reverse-inventory-issue"), request, idempotencyKey, cancellationToken).ConfigureAwait(false);

    private Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));

        return apiClient.PostIdempotentDataAsync<TRequest, TResponse>(
            path,
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private static string ActionPath(string deliveryOrderId, string action) =>
        $"/api/delivery-orders/{Uri.EscapeDataString(RequireId(deliveryOrderId))}/{action}";

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã phiếu giao hàng không hợp lệ.", nameof(value));
}
