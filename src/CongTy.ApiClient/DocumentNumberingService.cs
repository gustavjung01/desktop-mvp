using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IDocumentNumberingService
{
    Task<IReadOnlyList<DocumentNumberSeriesData>> ListSeriesAsync(CancellationToken cancellationToken = default);
    Task<DocumentNumberSeriesData> CreateSeriesAsync(DocumentNumberSeriesCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DocumentNumberSeriesData> UpdateSeriesAsync(string seriesId, DocumentNumberSeriesUpdateRequest request, CancellationToken cancellationToken = default);
    Task<DocumentNumberSeriesData> UpdateSeriesStatusAsync(string seriesId, DocumentNumberSeriesStatusRequest request, CancellationToken cancellationToken = default);
    Task<DocumentNumberHistoryData> GetHistoryAsync(string seriesId, CancellationToken cancellationToken = default);
    Task<DocumentNumberAllocationData> AllocateReferenceAsync(string seriesId, DocumentNumberAllocationRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class DocumentNumberingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IDocumentNumberingService
{
    public async Task<IReadOnlyList<DocumentNumberSeriesData>> ListSeriesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<DocumentNumberSeriesData[]>("/api/document-number-series?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<DocumentNumberSeriesData> CreateSeriesAsync(DocumentNumberSeriesCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<DocumentNumberSeriesCreateRequest, DocumentNumberSeriesData>(
            "/api/document-number-series", request, Key(idempotencyKey), RequireToken(), cancellationToken);

    public Task<DocumentNumberSeriesData> UpdateSeriesAsync(string seriesId, DocumentNumberSeriesUpdateRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<DocumentNumberSeriesUpdateRequest, DocumentNumberSeriesData>(
            $"/api/document-number-series/{Id(seriesId)}", request, RequireToken(), cancellationToken);

    public Task<DocumentNumberSeriesData> UpdateSeriesStatusAsync(string seriesId, DocumentNumberSeriesStatusRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<DocumentNumberSeriesStatusRequest, DocumentNumberSeriesData>(
            $"/api/document-number-series/{Id(seriesId)}", request, RequireToken(), cancellationToken);

    public Task<DocumentNumberHistoryData> GetHistoryAsync(string seriesId, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<DocumentNumberHistoryData>(
            $"/api/document-number-series/{Id(seriesId)}/allocations?limit=200", RequireToken(), cancellationToken);

    public Task<DocumentNumberAllocationData> AllocateReferenceAsync(string seriesId, DocumentNumberAllocationRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<DocumentNumberAllocationRequest, DocumentNumberAllocationData>(
            $"/api/document-number-series/{Id(seriesId)}/allocate", request, Key(idempotencyKey), RequireToken(), cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private string Key(string value) =>
        idempotencyKeys.IsValid(value)
            ? value.Trim()
            : throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(value));

    private static string Id(string value) =>
        Guid.TryParse(value, out _)
            ? Uri.EscapeDataString(value.Trim())
            : throw new ArgumentException("Mã quy tắc đánh số không hợp lệ.", nameof(value));
}
