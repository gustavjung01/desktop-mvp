using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ITripPlanningService
{
    Task<IReadOnlyList<TripWarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogisticsRouteData>> ListRoutesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogisticsVehicleData>> ListVehiclesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogisticsDriverData>> ListDriversAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogisticsDriverEmployeeData>> ListDriverEmployeesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TripEligibleDeliveryOrderData>> ListEligibleDeliveryOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeliveryTripData>> ListTripsAsync(CancellationToken cancellationToken = default);
    Task<DeliveryTripData> GetTripAsync(string tripId, CancellationToken cancellationToken = default);

    Task<LogisticsRouteData> CreateRouteAsync(LogisticsRouteCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<LogisticsVehicleData> CreateVehicleAsync(LogisticsVehicleCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<LogisticsDriverData> CreateDriverAsync(LogisticsDriverCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TripMutationData> CreateTripAsync(DeliveryTripCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TripMutationData> UpdateTripAsync(string tripId, DeliveryTripUpdateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TripMutationData> AssignAsync(string tripId, DeliveryTripAssignRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TripMutationData> UnassignAsync(string tripId, DeliveryTripUnassignRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TripMutationData> ReorderAsync(string tripId, DeliveryTripReorderRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TripMutationData> PlanAsync(string tripId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TripMutationData> ReopenAsync(string tripId, DeliveryTripReopenRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TripMutationData> LockAsync(string tripId, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class TripPlanningService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ITripPlanningService
{
    public async Task<IReadOnlyList<TripWarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<TripWarehouseData[]>(
            "/api/logistics/warehouses",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<LogisticsRouteData>> ListRoutesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<LogisticsRouteData[]>(
            "/api/logistics/routes?active=true",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<LogisticsVehicleData>> ListVehiclesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<LogisticsVehicleData[]>(
            "/api/logistics/vehicles?active=true",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<LogisticsDriverData>> ListDriversAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<LogisticsDriverData[]>(
            "/api/logistics/drivers?active=true",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<LogisticsDriverEmployeeData>> ListDriverEmployeesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<LogisticsDriverEmployeeData[]>(
            "/api/logistics/driver-employees?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<TripEligibleDeliveryOrderData>> ListEligibleDeliveryOrdersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<TripEligibleDeliveryOrderData[]>(
            "/api/logistics/eligible-delivery-orders",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<DeliveryTripData>> ListTripsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<DeliveryTripData[]>(
            "/api/logistics/trips",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<DeliveryTripData> GetTripAsync(string tripId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<DeliveryTripData>(
            $"/api/logistics/trips/{Uri.EscapeDataString(RequireId(tripId))}",
            RequireToken(),
            cancellationToken);

    public Task<LogisticsRouteData> CreateRouteAsync(
        LogisticsRouteCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostMasterAsync("/api/logistics/routes", request, idempotencyKey, cancellationToken);

    public Task<LogisticsVehicleData> CreateVehicleAsync(
        LogisticsVehicleCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostMasterAsync("/api/logistics/vehicles", request, idempotencyKey, cancellationToken);

    public Task<LogisticsDriverData> CreateDriverAsync(
        LogisticsDriverCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostMasterAsync("/api/logistics/drivers", request, idempotencyKey, cancellationToken);

    public Task<TripMutationData> CreateTripAsync(
        DeliveryTripCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostBusinessAsync("/api/logistics/trips", request, idempotencyKey, cancellationToken);

    public Task<TripMutationData> UpdateTripAsync(
        string tripId,
        DeliveryTripUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(idempotencyKey);
        return apiClient.PutIdempotentDataAsync<DeliveryTripUpdateRequest, TripMutationData>(
            TripPath(tripId),
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    public Task<TripMutationData> AssignAsync(
        string tripId,
        DeliveryTripAssignRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostBusinessAsync(ActionPath(tripId, "assign"), request, idempotencyKey, cancellationToken);

    public Task<TripMutationData> UnassignAsync(
        string tripId,
        DeliveryTripUnassignRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostBusinessAsync(ActionPath(tripId, "unassign"), request, idempotencyKey, cancellationToken);

    public Task<TripMutationData> ReorderAsync(
        string tripId,
        DeliveryTripReorderRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostBusinessAsync(ActionPath(tripId, "reorder"), request, idempotencyKey, cancellationToken);

    public Task<TripMutationData> PlanAsync(
        string tripId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostBusinessAsync(ActionPath(tripId, "plan"), new DeliveryTripEmptyRequest(), idempotencyKey, cancellationToken);

    public Task<TripMutationData> ReopenAsync(
        string tripId,
        DeliveryTripReopenRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostBusinessAsync(ActionPath(tripId, "reopen"), request, idempotencyKey, cancellationToken);

    public Task<TripMutationData> LockAsync(
        string tripId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostBusinessAsync(ActionPath(tripId, "lock"), new DeliveryTripEmptyRequest(), idempotencyKey, cancellationToken);

    private Task<TResponse> PostMasterAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ValidateKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<TRequest, TResponse>(
            path,
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private Task<LogisticsRouteData> PostMasterAsync(
        string path,
        LogisticsRouteCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        PostMasterAsync<LogisticsRouteCreateRequest, LogisticsRouteData>(path, request, idempotencyKey, cancellationToken);

    private Task<LogisticsVehicleData> PostMasterAsync(
        string path,
        LogisticsVehicleCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        PostMasterAsync<LogisticsVehicleCreateRequest, LogisticsVehicleData>(path, request, idempotencyKey, cancellationToken);

    private Task<LogisticsDriverData> PostMasterAsync(
        string path,
        LogisticsDriverCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        PostMasterAsync<LogisticsDriverCreateRequest, LogisticsDriverData>(path, request, idempotencyKey, cancellationToken);

    private Task<TripMutationData> PostBusinessAsync<TRequest>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ValidateKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<TRequest, TripMutationData>(
            path,
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private void ValidateKey(string idempotencyKey)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));
    }

    private static string TripPath(string tripId) =>
        $"/api/logistics/trips/{Uri.EscapeDataString(RequireId(tripId))}";

    private static string ActionPath(string tripId, string action) =>
        $"{TripPath(tripId)}/{action}";

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã chuyến giao không hợp lệ.", nameof(value));
}
