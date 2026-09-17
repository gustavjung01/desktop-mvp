using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IPayablesService
{
    Task<IReadOnlyList<PayableDocumentData>> ListDocumentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierPayableBalanceData>> ListBalancesAsync(CancellationToken cancellationToken = default);
    Task<PayableDocumentData> GetDocumentAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class PayablesService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IPayablesService
{
    public async Task<IReadOnlyList<PayableDocumentData>> ListDocumentsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<PayableDocumentData[]>("/api/payables?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<SupplierPayableBalanceData>> ListBalancesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierPayableBalanceData[]>("/api/payables/balances?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<PayableDocumentData> GetDocumentAsync(string id, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<PayableDocumentData>(
            $"/api/payables/{Uri.EscapeDataString(RequireDocumentId(id))}",
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireDocumentId(string? value) =>
        Guid.TryParse(value?.Trim(), out var id)
            ? id.ToString("D")
            : throw new ArgumentException("Chứng từ công nợ phải trả không hợp lệ.", nameof(value));
}
