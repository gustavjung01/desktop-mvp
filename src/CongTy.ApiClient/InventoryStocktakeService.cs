using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IInventoryStocktakeService
{
    Task<IReadOnlyList<InventoryStocktakeData>> ListAsync(CancellationToken cancellationToken = default);
    Task<InventoryStocktakeData> GetAsync(string stocktakeId, CancellationToken cancellationToken = default);
    Task<InventoryStocktakeData> CreateAsync(InventoryStocktakeCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryStocktakeData> CountAsync(string stocktakeId, InventoryStocktakeCountRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryStocktakeData> SubmitAsync(string stocktakeId, InventoryStocktakeTransitionRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryStocktakeData> RecountAsync(string stocktakeId, InventoryStocktakeReasonRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryStocktakeData> ApproveAsync(string stocktakeId, InventoryStocktakeTransitionRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryStocktakeData> PostAsync(string stocktakeId, InventoryStocktakeTransitionRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryStocktakeData> CancelAsync(string stocktakeId, InventoryStocktakeReasonRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InventoryStocktakeData> ReverseAsync(string stocktakeId, InventoryStocktakeReasonRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class InventoryStocktakeService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IInventoryStocktakeService
{
    public async Task<IReadOnlyList<InventoryStocktakeData>> ListAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryStocktakeData[]>(
            "/api/inventory/stocktakes?limit=500&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<InventoryStocktakeData> GetAsync(string stocktakeId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<InventoryStocktakeData>(
            $"/api/inventory/stocktakes/{Uri.EscapeDataString(RequireId(stocktakeId))}",
            RequireToken(),
            cancellationToken);

    public Task<InventoryStocktakeData> CreateAsync(InventoryStocktakeCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync("/api/inventory/stocktakes", request, idempotencyKey, cancellationToken);

    public Task<InventoryStocktakeData> CountAsync(string stocktakeId, InventoryStocktakeCountRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync(ActionPath(stocktakeId, "count"), request, idempotencyKey, cancellationToken);

    public Task<InventoryStocktakeData> SubmitAsync(string stocktakeId, InventoryStocktakeTransitionRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync(ActionPath(stocktakeId, "submit"), request, idempotencyKey, cancellationToken);

    public Task<InventoryStocktakeData> RecountAsync(string stocktakeId, InventoryStocktakeReasonRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync(ActionPath(stocktakeId, "recount"), request, idempotencyKey, cancellationToken);

    public Task<InventoryStocktakeData> ApproveAsync(string stocktakeId, InventoryStocktakeTransitionRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync(ActionPath(stocktakeId, "approve"), request, idempotencyKey, cancellationToken);

    public Task<InventoryStocktakeData> PostAsync(string stocktakeId, InventoryStocktakeTransitionRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync(ActionPath(stocktakeId, "post"), request, idempotencyKey, cancellationToken);

    public Task<InventoryStocktakeData> CancelAsync(string stocktakeId, InventoryStocktakeReasonRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync(ActionPath(stocktakeId, "cancel"), request, idempotencyKey, cancellationToken);

    public Task<InventoryStocktakeData> ReverseAsync(string stocktakeId, InventoryStocktakeReasonRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync(ActionPath(stocktakeId, "reverse"), request, idempotencyKey, cancellationToken);

    private Task<InventoryStocktakeData> PostAsync<TRequest>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
        {
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));
        }

        return apiClient.PostIdempotentDataAsync<TRequest, InventoryStocktakeData>(
            path,
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private static string ActionPath(string stocktakeId, string action) =>
        $"/api/inventory/stocktakes/{Uri.EscapeDataString(RequireId(stocktakeId))}/{action}";

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã kiểm kê không hợp lệ.", nameof(value));
}
