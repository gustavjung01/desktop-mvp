using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ICustomerOnboardingService
{
    Task<IReadOnlyList<CustomerOnboardingRequestData>> ListPendingAsync(CancellationToken cancellationToken = default);
    Task<CustomerOnboardingPortalOptionsData> GetPortalOptionsAsync(CancellationToken cancellationToken = default);
    Task<CustomerOnboardingRequestData> StartReviewAsync(string id, int expectedVersion, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerOnboardingRequestData> RequestMoreInfoAsync(string id, int expectedVersion, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerOnboardingRequestData> ApproveAsync(string id, int expectedVersion, string customerCode, string? portalWarehouseId, string? portalSalesChannelId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerOnboardingRequestData> LinkExistingAsync(string id, int expectedVersion, string customerId, string addressId, string? portalWarehouseId, string? portalSalesChannelId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerOnboardingRequestData> RejectAsync(string id, int expectedVersion, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class CustomerOnboardingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ICustomerOnboardingService
{
    private static readonly string[] PendingStatuses = ["submitted", "under_review", "need_more_info"];

    public async Task<IReadOnlyList<CustomerOnboardingRequestData>> ListPendingAsync(CancellationToken cancellationToken = default)
    {
        var byId = new Dictionary<string, CustomerOnboardingRequestData>(StringComparer.Ordinal);
        foreach (var status in PendingStatuses)
        {
            var offset = 0;
            const int limit = 100;
            while (true)
            {
                var page = await apiClient.GetDataAsync<CustomerOnboardingListData>(
                    $"/api/customer-onboarding-requests?status={status}&limit={limit}&offset={offset}",
                    RequireToken(),
                    cancellationToken).ConfigureAwait(false);
                foreach (var request in page.CustomerOnboardingRequests)
                {
                    if (!string.IsNullOrWhiteSpace(request.Id)) byId[request.Id] = request;
                }
                if (page.CustomerOnboardingRequests.Length < limit) break;
                offset += limit;
            }
        }

        return byId.Values
            .OrderByDescending(item => ParseTimestamp(item.UpdatedAt))
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public Task<CustomerOnboardingPortalOptionsData> GetPortalOptionsAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<CustomerOnboardingPortalOptionsData>(
            "/api/customer-onboarding-portal-options",
            RequireToken(),
            cancellationToken);

    public Task<CustomerOnboardingRequestData> StartReviewAsync(
        string id,
        int expectedVersion,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        MutateAsync(id, "review", new ExpectedVersionPayload(expectedVersion), idempotencyKey, cancellationToken);

    public Task<CustomerOnboardingRequestData> RequestMoreInfoAsync(
        string id,
        int expectedVersion,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        MutateAsync(id, "need-more-info", new ReasonPayload(expectedVersion, RequireReason(reason)), idempotencyKey, cancellationToken);

    public Task<CustomerOnboardingRequestData> ApproveAsync(
        string id,
        int expectedVersion,
        string customerCode,
        string? portalWarehouseId,
        string? portalSalesChannelId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var code = (customerCode ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length is < 1 or > 64 || code.Any(ch => !(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-')))
            throw new ArgumentException("Mã khách chỉ gồm chữ in hoa, số, dấu gạch ngang hoặc gạch dưới.", nameof(customerCode));

        return MutateAsync(
            id,
            "approve",
            new ApprovePayload(expectedVersion, code, NormalizeOptionalId(portalWarehouseId), NormalizeOptionalId(portalSalesChannelId)),
            idempotencyKey,
            cancellationToken);
    }

    public Task<CustomerOnboardingRequestData> LinkExistingAsync(
        string id,
        int expectedVersion,
        string customerId,
        string addressId,
        string? portalWarehouseId,
        string? portalSalesChannelId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            id,
            "link-existing",
            new LinkExistingPayload(
                expectedVersion,
                RequireId(customerId, nameof(customerId)),
                RequireId(addressId, nameof(addressId)),
                NormalizeOptionalId(portalWarehouseId),
                NormalizeOptionalId(portalSalesChannelId)),
            idempotencyKey,
            cancellationToken);

    public Task<CustomerOnboardingRequestData> RejectAsync(
        string id,
        int expectedVersion,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        MutateAsync(id, "reject", new ReasonPayload(expectedVersion, RequireReason(reason)), idempotencyKey, cancellationToken);

    private async Task<CustomerOnboardingRequestData> MutateAsync<TPayload>(
        string id,
        string action,
        TPayload payload,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestId = RequireId(id, nameof(id));
        RequireKey(idempotencyKey);
        var result = await apiClient.PostIdempotentDataAsync<TPayload, CustomerOnboardingMutationData>(
            $"/api/customer-onboarding-requests/{Uri.EscapeDataString(requestId)}/{action}",
            payload,
            idempotencyKey,
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
        return result.CustomerOnboardingRequest;
    }

    private string RequireToken() =>
        sessionAccessor.CurrentToken
        ?? throw new InvalidOperationException("Phiên đăng nhập chưa sẵn sàng.");

    private void RequireKey(string key)
    {
        if (!idempotencyKeys.IsValid(key))
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(key));
    }

    private static string RequireId(string? value, string parameterName)
    {
        var id = value?.Trim() ?? string.Empty;
        if (id.Length is < 1 or > 240 || id.Any(ch => !(char.IsAsciiLetterOrDigit(ch) || ch is '.' or '_' or '-')))
            throw new ArgumentException("Mã dữ liệu không hợp lệ.", parameterName);
        return id;
    }

    private static string? NormalizeOptionalId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return RequireId(value, nameof(value));
    }

    private static string RequireReason(string? value)
    {
        var reason = value?.Trim() ?? string.Empty;
        if (reason.Length is < 1 or > 2000)
            throw new ArgumentException("Lý do phải có từ 1 đến 2.000 ký tự.", nameof(value));
        return reason;
    }

    private static DateTimeOffset ParseTimestamp(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : DateTimeOffset.MinValue;

    private sealed record ExpectedVersionPayload(int ExpectedVersion);
    private sealed record ReasonPayload(int ExpectedVersion, string Reason);
    private sealed record ApprovePayload(int ExpectedVersion, string CustomerCode, string? PortalWarehouseId, string? PortalSalesChannelId);
    private sealed record LinkExistingPayload(int ExpectedVersion, string CustomerId, string AddressId, string? PortalWarehouseId, string? PortalSalesChannelId);
}
