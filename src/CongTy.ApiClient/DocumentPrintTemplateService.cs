using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IDocumentPrintTemplateService
{
    Task<IReadOnlyList<DocumentPrintTemplateData>> ListAsync(CancellationToken cancellationToken = default);
    Task<DocumentPrintTemplateData> SaveAsync(
        string documentType,
        string templateCode,
        DocumentPrintTemplateUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<DocumentPrintTemplateData> ResetAsync(
        string documentType,
        string templateCode,
        DocumentPrintTemplateResetRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class DocumentPrintTemplateService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IDocumentPrintTemplateService
{
    public async Task<IReadOnlyList<DocumentPrintTemplateData>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<DocumentPrintTemplateData[]>(
            "/api/document-print-templates",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<DocumentPrintTemplateData> SaveAsync(
        string documentType,
        string templateCode,
        DocumentPrintTemplateUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchIdempotentDataAsync<DocumentPrintTemplateUpdateRequest, DocumentPrintTemplateData>(
            BuildPath(documentType, templateCode),
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<DocumentPrintTemplateData> ResetAsync(
        string documentType,
        string templateCode,
        DocumentPrintTemplateResetRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchIdempotentDataAsync<DocumentPrintTemplateResetRequest, DocumentPrintTemplateData>(
            BuildPath(documentType, templateCode),
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string BuildPath(string documentType, string templateCode)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(templateCode))
            throw new ArgumentException("Mẫu in không hợp lệ.");

        return $"/api/document-print-templates/{Uri.EscapeDataString(documentType.Trim())}/{Uri.EscapeDataString(templateCode.Trim())}";
    }
}
