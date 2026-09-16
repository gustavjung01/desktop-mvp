using CongTy.Contracts;

namespace CongTy.ApiClient;

public enum ConnectionStatus
{
    NotConfigured,
    Checking,
    Online,
    Offline
}

public sealed record ConnectionSnapshot(ConnectionStatus Status, string Message)
{
    public static ConnectionSnapshot NotConfigured { get; } =
        new(ConnectionStatus.NotConfigured, "Chưa cấu hình địa chỉ hệ thống.");
}

public interface IConnectionStateService
{
    ConnectionSnapshot Current { get; }
    event EventHandler<ConnectionSnapshot>? Changed;
    Task<ConnectionSnapshot> RefreshAsync(CancellationToken cancellationToken = default);
}

public sealed class ConnectionStateService(CompanyApiClient apiClient) : IConnectionStateService
{
    public ConnectionSnapshot Current { get; private set; } = ConnectionSnapshot.NotConfigured;

    public event EventHandler<ConnectionSnapshot>? Changed;

    public async Task<ConnectionSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!apiClient.IsConfigured)
        {
            return Set(ConnectionSnapshot.NotConfigured);
        }

        Set(new ConnectionSnapshot(ConnectionStatus.Checking, "Đang kiểm tra kết nối..."));

        try
        {
            var live = await apiClient.GetDataAsync<HealthStatusData>(
                "/health/live",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!string.Equals(live.Status, "ok", StringComparison.Ordinal))
            {
                return Set(new ConnectionSnapshot(ConnectionStatus.Offline, "Hệ thống chưa sẵn sàng để kết nối."));
            }

            var ready = await apiClient.GetDataAsync<HealthStatusData>(
                "/health/ready",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return string.Equals(ready.Status, "ready", StringComparison.Ordinal)
                ? Set(new ConnectionSnapshot(ConnectionStatus.Online, "Đã kết nối hệ thống Công Ty."))
                : Set(new ConnectionSnapshot(ConnectionStatus.Offline, "Hệ thống đang khởi động và chưa sẵn sàng."));
        }
        catch (CanonicalApiException exception)
        {
            return Set(new ConnectionSnapshot(
                ConnectionStatus.Offline,
                CanonicalErrorMessages.WithRequestId(
                    exception.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                        ? "Hệ thống đang hoạt động nhưng chưa sẵn sàng."
                        : "Không xác nhận được tình trạng hệ thống.",
                    exception.RequestId)));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return Set(new ConnectionSnapshot(ConnectionStatus.Offline, "Không kết nối được hệ thống Công Ty."));
        }
    }

    private ConnectionSnapshot Set(ConnectionSnapshot snapshot)
    {
        Current = snapshot;
        Changed?.Invoke(this, snapshot);
        return snapshot;
    }
}
