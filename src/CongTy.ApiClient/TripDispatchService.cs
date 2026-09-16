using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ITripDispatchService
{
    Task<IReadOnlyList<DeliveryTripData>> ListTripsAsync(CancellationToken cancellationToken = default);
    Task<TripDispatchData> GetAsync(string tripId, CancellationToken cancellationToken = default);
    Task<TripDispatchResultData> DispatchAsync(
        string tripId,
        TripDispatchRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class TripDispatchService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ITripDispatchService
{
    public async Task<IReadOnlyList<DeliveryTripData>> ListTripsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<DeliveryTripData[]>(
            "/api/logistics/trips?status=all",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<TripDispatchData> GetAsync(string tripId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<TripDispatchData>(
            DispatchPath(tripId),
            RequireToken(),
            cancellationToken);

    public Task<TripDispatchResultData> DispatchAsync(
        string tripId,
        TripDispatchRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));

        return apiClient.PostIdempotentDataAsync<TripDispatchRequest, TripDispatchResultData>(
            DispatchPath(tripId),
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private static string DispatchPath(string tripId) =>
        $"/api/logistics/trips/{Uri.EscapeDataString(RequireId(tripId))}/dispatch";

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã chuyến giao không hợp lệ.", nameof(value));
}
