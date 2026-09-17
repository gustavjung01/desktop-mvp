using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IManagementProposalService
{
    Task<IReadOnlyList<ManagementProposalData>> ListOwnAsync(CancellationToken cancellationToken = default);
    Task<ManagementProposalData> CreateAsync(
        ManagementProposalCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<ManagementProposalData> ResubmitAsync(
        string proposalId,
        ManagementProposalResubmitRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class ManagementProposalService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IManagementProposalService
{
    public async Task<IReadOnlyList<ManagementProposalData>> ListOwnAsync(CancellationToken cancellationToken = default)
    {
        var data = await apiClient.GetDataAsync<ManagementProposalListData>(
            "/api/management-proposals?source=company",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
        return data.Proposals;
    }

    public Task<ManagementProposalData> CreateAsync(
        ManagementProposalCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireKey(idempotencyKey);
        var payload = new CreatePayload(
            request.Domain,
            request.Title,
            request.Content,
            request.EntityType,
            request.EntityId,
            request.EntityLabel,
            request.Impact,
            request.Reason,
            request.Rule,
            request.Evidence,
            request.Priority,
            "company");
        return apiClient.PostIdempotentDataAsync<CreatePayload, ManagementProposalData>(
            "/api/management-proposals",
            payload,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    public Task<ManagementProposalData> ResubmitAsync(
        string proposalId,
        ManagementProposalResubmitRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireKey(idempotencyKey);
        var id = RequireId(proposalId);
        return apiClient.PostIdempotentDataAsync<ManagementProposalResubmitRequest, ManagementProposalData>(
            $"/api/management-proposals/{Uri.EscapeDataString(id)}/resubmit",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    private string RequireToken() =>
        sessionAccessor.CurrentToken
        ?? throw new InvalidOperationException("Phiên đăng nhập chưa sẵn sàng.");

    private void RequireKey(string key)
    {
        if (!idempotencyKeys.IsValid(key))
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(key));
    }

    private static string RequireId(string value)
    {
        var id = value?.Trim() ?? string.Empty;
        if (id.Length is < 1 or > 240 || id.Any(ch => !(char.IsAsciiLetterOrDigit(ch) || ch is '.' or '_' or '-')))
            throw new ArgumentException("Mã Đề xuất không hợp lệ.", nameof(value));
        return id;
    }

    private sealed record CreatePayload(
        string Domain,
        string Title,
        string Content,
        string EntityType,
        string EntityId,
        string EntityLabel,
        string Impact,
        string Reason,
        string Rule,
        IReadOnlyList<string> Evidence,
        string Priority,
        string Source);
}
