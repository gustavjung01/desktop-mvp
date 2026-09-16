using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ITripReconciliationService
{
    Task<IReadOnlyList<DeliveryTripData>> ListTripsAsync(CancellationToken cancellationToken = default);
    Task<TripReconciliationData> GetAsync(string tripId, CancellationToken cancellationToken = default);
    Task<TripReconciliationMutationResult> ReceiveReturnAsync(
        string tripId,
        TripReturnReceiptRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<TripReconciliationMutationResult> CloseAsync(
        string tripId,
        TripCloseRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class TripReconciliationService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ITripReconciliationService
{
    public async Task<IReadOnlyList<DeliveryTripData>> ListTripsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<DeliveryTripData[]>(
            "/api/logistics/trips?status=all",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<TripReconciliationData> GetAsync(string tripId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<TripReconciliationData>(
            $"{TripPath(tripId)}/reconciliation",
            RequireToken(),
            cancellationToken);

    public Task<TripReconciliationMutationResult> ReceiveReturnAsync(
        string tripId,
        TripReturnReceiptRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync($"{TripPath(tripId)}/return-receipts", request, idempotencyKey, cancellationToken);

    public Task<TripReconciliationMutationResult> CloseAsync(
        string tripId,
        TripCloseRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync($"{TripPath(tripId)}/close", request, idempotencyKey, cancellationToken);

    private Task<TripReconciliationMutationResult> PostAsync<TRequest>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));

        return apiClient.PostIdempotentDataAsync<TRequest, TripReconciliationMutationResult>(
            path,
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private static string TripPath(string tripId) =>
        $"/api/logistics/trips/{Uri.EscapeDataString(RequireId(tripId))}";

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã chuyến giao không hợp lệ.", nameof(value));
}
