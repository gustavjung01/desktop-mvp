using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IDeliveryAttemptService
{
    Task<IReadOnlyList<DeliveryTripData>> ListTripsAsync(CancellationToken cancellationToken = default);
    Task<DeliveryAttemptSummaryData> GetSummaryAsync(string tripId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeliveryProofData>> ListProofsAsync(
        string tripId,
        string attemptId,
        CancellationToken cancellationToken = default);
}

public sealed class DeliveryAttemptService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IDeliveryAttemptService
{
    public async Task<IReadOnlyList<DeliveryTripData>> ListTripsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<DeliveryTripData[]>(
            "/api/logistics/trips?status=all",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<DeliveryAttemptSummaryData> GetSummaryAsync(
        string tripId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<DeliveryAttemptSummaryData>(
            $"/api/logistics/trips/{Uri.EscapeDataString(RequireId(tripId))}/attempts",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<DeliveryProofData>> ListProofsAsync(
        string tripId,
        string attemptId,
        CancellationToken cancellationToken = default)
    {
        var data = await apiClient.GetDataAsync<DeliveryProofListData>(
            $"/api/logistics/trips/{Uri.EscapeDataString(RequireId(tripId))}/attempts/{Uri.EscapeDataString(RequireId(attemptId))}/pod",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
        return data.Proofs;
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã dữ liệu giao hàng không hợp lệ.", nameof(value));
}
