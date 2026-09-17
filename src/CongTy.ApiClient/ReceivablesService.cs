using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IReceivablesService
{
    Task<IReadOnlyList<ReceivableDocumentData>> ListDocumentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerReceivableBalanceData>> ListBalancesAsync(CancellationToken cancellationToken = default);
    Task<ReceivableDocumentData> GetDocumentAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class ReceivablesService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IReceivablesService
{
    public async Task<IReadOnlyList<ReceivableDocumentData>> ListDocumentsAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ReceivableDocumentData[]>(
            "/api/receivables?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<CustomerReceivableBalanceData>> ListBalancesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerReceivableBalanceData[]>(
            "/api/receivables/balances?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<ReceivableDocumentData> GetDocumentAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var documentId = RequireDocumentId(id);
        return apiClient.GetDataAsync<ReceivableDocumentData>(
            $"/api/receivables/{Uri.EscapeDataString(documentId)}",
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireDocumentId(string? value)
    {
        var candidate = value?.Trim() ?? string.Empty;
        if (!Guid.TryParse(candidate, out var id))
            throw new ArgumentException("Chứng từ công nợ không hợp lệ.", nameof(value));
        return id.ToString("D");
    }
}
