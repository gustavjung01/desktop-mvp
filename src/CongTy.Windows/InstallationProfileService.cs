using CongTy.ApiClient;

namespace CongTy.Windows;

public sealed record InstallationSaveResult(
    bool Success,
    string Message,
    string? NormalizedApiUrl = null,
    string? RequestId = null);

public interface IInstallationProfileService
{
    bool IsConfigured { get; }

    Task<InstallationSaveResult> ValidateAndSaveAsync(
        string installationName,
        string companyDisplayName,
        string apiBaseUrl,
        CancellationToken cancellationToken = default);
}

public sealed class InstallationProfileService(
    ILocalSettingsStore settingsStore,
    DesktopSettingsState settingsState,
    ICompanyEndpointProvider endpoints,
    CompanyApiClient apiClient,
    ISessionTokenStore tokenStore) : IInstallationProfileService
{
    public bool IsConfigured =>
        endpoints.BaseUri is not null
        && !string.IsNullOrWhiteSpace(settingsState.Current.InstallationName)
        && !string.IsNullOrWhiteSpace(settingsState.Current.CompanyDisplayName);

    public async Task<InstallationSaveResult> ValidateAndSaveAsync(
        string installationName,
        string companyDisplayName,
        string apiBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var normalizedInstallationName = installationName?.Trim() ?? string.Empty;
        var normalizedCompanyName = companyDisplayName?.Trim() ?? string.Empty;

        if (normalizedInstallationName.Length is < 1 or > 128)
        {
            return new InstallationSaveResult(false, "Tên cấu hình cài đặt phải có từ 1 đến 128 ký tự.");
        }

        if (normalizedCompanyName.Length is < 1 or > 160)
        {
            return new InstallationSaveResult(false, "Tên Công Ty hiển thị phải có từ 1 đến 160 ký tự.");
        }

        if (!CompanyEndpointProvider.TryNormalizeHttpsBaseUrl(
                apiBaseUrl,
                out var candidateUri,
                out var normalizedUrl,
                out var urlError))
        {
            return new InstallationSaveResult(false, urlError);
        }

        HealthCheckResult health;
        try
        {
            health = await apiClient.ValidateHealthAsync(candidateUri!, cancellationToken).ConfigureAwait(false);
        }
        catch (CanonicalApiException exception)
        {
            return new InstallationSaveResult(
                false,
                CanonicalErrorMessages.ToOfficeMessage(exception),
                RequestId: exception.RequestId);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new InstallationSaveResult(false, "Không kết nối được địa chỉ hệ thống đã nhập.");
        }

        if (!health.Live || !health.Ready)
        {
            return new InstallationSaveResult(false, "Địa chỉ hệ thống chưa vượt qua kiểm tra sẵn sàng.");
        }

        var previousUri = endpoints.BaseUri;
        var changedEndpoint = previousUri is not null
            && Uri.Compare(
                previousUri,
                candidateUri!,
                UriComponents.SchemeAndServer,
                UriFormat.SafeUnescaped,
                StringComparison.OrdinalIgnoreCase) != 0;

        if (changedEndpoint)
        {
            try
            {
                await tokenStore.DeleteAsync(previousUri!, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return new InstallationSaveResult(
                    false,
                    "Không thể đóng phiên đăng nhập của cấu hình cũ trên máy tính này.");
            }
        }

        var updated = settingsState.Current with
        {
            InstallationName = normalizedInstallationName,
            CompanyDisplayName = normalizedCompanyName,
            ApiBaseUrl = normalizedUrl
        };

        try
        {
            await settingsStore.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return new InstallationSaveResult(false, "Không thể lưu cấu hình cài đặt trên máy tính này.");
        }

        settingsState.Update(updated);
        endpoints.SetBaseUri(candidateUri!);

        return new InstallationSaveResult(
            true,
            "Đã xác nhận kết nối an toàn tới hệ thống Công Ty.",
            normalizedUrl);
    }
}
