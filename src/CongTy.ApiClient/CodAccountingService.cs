using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ICodAccountingService
{
    Task<CodReportingDashboardData> GetReportAsync(
        string? from,
        string? to,
        string? warehouseId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodHandoverData>> ListHandoversAsync(CancellationToken cancellationToken = default);
    Task<CodHandoverData> GetHandoverAsync(string handoverId, CancellationToken cancellationToken = default);
    Task<CodMutationResultData> AcceptAsync(string handoverId, CodAcceptRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CodMutationResultData> ReverseCollectionAsync(string collectionId, CodReversalRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CodMutationResultData> ReverseHandoverAsync(string handoverId, CodReversalRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CodMutationResultData> ReverseAcceptanceAsync(string acceptanceId, CodReversalRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class CodAccountingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ICodAccountingService
{
    public Task<CodReportingDashboardData> GetReportAsync(
        string? from,
        string? to,
        string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddDate(query, "from", from);
        AddDate(query, "to", to);
        if (!string.IsNullOrWhiteSpace(warehouseId))
            query.Add($"warehouseId={Uri.EscapeDataString(RequireId(warehouseId, "Kho báo cáo"))}");

        var path = "/api/reporting/cod" + (query.Count == 0 ? string.Empty : "?" + string.Join("&", query));
        return apiClient.GetDataAsync<CodReportingDashboardData>(path, RequireToken(), cancellationToken);
    }

    public async Task<IReadOnlyList<CodHandoverData>> ListHandoversAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CodHandoverData[]>(
            "/api/cod-reconciliation?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<CodHandoverData> GetHandoverAsync(string handoverId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<CodHandoverData>(
            $"/api/cod-reconciliation/{Uri.EscapeDataString(RequireId(handoverId, "Bàn giao COD"))}",
            RequireToken(),
            cancellationToken);

    public Task<CodMutationResultData> AcceptAsync(
        string handoverId,
        CodAcceptRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync(
            $"/api/cod-reconciliation/{Uri.EscapeDataString(RequireId(handoverId, "Bàn giao COD"))}/accept",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CodMutationResultData> ReverseCollectionAsync(
        string collectionId,
        CodReversalRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync(
            $"/api/cod-reconciliation/collections/{Uri.EscapeDataString(RequireId(collectionId, "Khoản thu COD"))}/reverse",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CodMutationResultData> ReverseHandoverAsync(
        string handoverId,
        CodReversalRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync(
            $"/api/cod-reconciliation/handovers/{Uri.EscapeDataString(RequireId(handoverId, "Bàn giao COD"))}/reverse",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CodMutationResultData> ReverseAcceptanceAsync(
        string acceptanceId,
        CodReversalRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync(
            $"/api/cod-reconciliation/acceptances/{Uri.EscapeDataString(RequireId(acceptanceId, "Xác nhận COD"))}/reverse",
            request,
            idempotencyKey,
            cancellationToken);

    private Task<CodMutationResultData> PostAsync<TRequest>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));

        return apiClient.PostIdempotentDataAsync<TRequest, CodMutationResultData>(
            path,
            request,
            idempotencyKey.Trim(),
            RequireToken(),
            cancellationToken);
    }

    private static void AddDate(List<string> query, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var normalized = value.Trim();
        if (!DateOnly.TryParseExact(normalized, "yyyy-MM-dd", out _))
            throw new ArgumentException($"{name} phải theo định dạng yyyy-MM-dd.", name);
        query.Add($"{name}={Uri.EscapeDataString(normalized)}");
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value, string label) =>
        Guid.TryParse(value?.Trim(), out _)
            ? value.Trim().ToLowerInvariant()
            : throw new ArgumentException($"{label} không hợp lệ.", nameof(value));
}
