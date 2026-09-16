using System.Net;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ISessionTokenStore
{
    Task<string?> ReadAsync(Uri baseUri, CancellationToken cancellationToken = default);
    Task WriteAsync(Uri baseUri, string token, CancellationToken cancellationToken = default);
    Task DeleteAsync(Uri baseUri, CancellationToken cancellationToken = default);
}

public interface IAuthenticatedSessionAccessor
{
    string? CurrentToken { get; }
}

public enum LoginAttemptKind
{
    Succeeded,
    ChallengeRequired,
    Failed
}

public sealed record LoginAttemptResult(
    LoginAttemptKind Kind,
    string Message,
    string? RequestId = null);

public enum SessionRestoreKind
{
    Succeeded,
    NoSession,
    ExpiredOrInvalid,
    Unavailable,
    Failed
}

public sealed record SessionRestoreResult(
    SessionRestoreKind Kind,
    string Message,
    string? RequestId = null);

public sealed record LogoutResult(string Message, string? RequestId = null);

public interface IAuthenticationService
{
    Task<LoginAttemptResult> LoginAsync(
        string loginName,
        string password,
        string? ownerCode = null,
        CancellationToken cancellationToken = default);

    Task<SessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default);
    Task<LogoutResult> LogoutAsync(CancellationToken cancellationToken = default);
}

public sealed class AuthenticationService(
    CompanyApiClient apiClient,
    ICompanyEndpointProvider endpoints,
    ISessionTokenStore tokenStore,
    IAccessStateService accessState) :
    IAuthenticationService,
    IAuthenticatedSessionAccessor
{
    private const string SourceApp = "congty-desktop";
    private const string SessionPrefix = "nppusr.";

    private string? _currentToken;

    public string? CurrentToken => _currentToken;

    public async Task<LoginAttemptResult> LoginAsync(
        string loginName,
        string password,
        string? ownerCode = null,
        CancellationToken cancellationToken = default)
    {
        var baseUri = endpoints.BaseUri;
        if (baseUri is null)
        {
            return new LoginAttemptResult(LoginAttemptKind.Failed, "Chưa cấu hình kết nối hệ thống Công Ty.");
        }

        var normalizedLogin = loginName?.Trim() ?? string.Empty;
        if (normalizedLogin.Length == 0 || string.IsNullOrEmpty(password))
        {
            return new LoginAttemptResult(LoginAttemptKind.Failed, "Vui lòng nhập tên đăng nhập và mật khẩu.");
        }

        InternalLoginData login;
        try
        {
            login = await apiClient.PostDataAsync<InternalLoginRequest, InternalLoginData>(
                "/api/internal-auth/login",
                new InternalLoginRequest
                {
                    LoginName = normalizedLogin,
                    Password = password,
                    SourceApp = SourceApp,
                    OwnerCode = string.IsNullOrWhiteSpace(ownerCode) ? null : ownerCode.Trim()
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (CanonicalApiException exception)
        {
            if (exception.Code is "INTERNAL_AUTH_OWNER_CHALLENGE_REQUIRED")
            {
                return new LoginAttemptResult(
                    LoginAttemptKind.ChallengeRequired,
                    "Vui lòng nhập mã xác minh đã được gửi cho tài khoản quản trị.",
                    exception.RequestId);
            }

            if (exception.Code is "INTERNAL_AUTH_OWNER_CODE_INVALID")
            {
                return new LoginAttemptResult(
                    LoginAttemptKind.ChallengeRequired,
                    "Mã xác minh không đúng hoặc đã hết hiệu lực.",
                    exception.RequestId);
            }

            return new LoginAttemptResult(
                LoginAttemptKind.Failed,
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return new LoginAttemptResult(LoginAttemptKind.Failed, "Không kết nối được hệ thống Công Ty.");
        }

        var token = login.Token?.Trim() ?? string.Empty;
        if (!IsPlausibleSessionToken(token))
        {
            return new LoginAttemptResult(LoginAttemptKind.Failed, "Hệ thống trả về phiên đăng nhập không hợp lệ.");
        }

        InternalMeData me;
        try
        {
            me = await apiClient.GetDataAsync<InternalMeData>(
                "/api/internal-auth/me",
                token,
                cancellationToken).ConfigureAwait(false);
        }
        catch (CanonicalApiException exception)
        {
            await BestEffortLogoutAsync(token, cancellationToken).ConfigureAwait(false);
            accessState.Clear();
            return new LoginAttemptResult(
                LoginAttemptKind.Failed,
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            await BestEffortLogoutAsync(token, cancellationToken).ConfigureAwait(false);
            accessState.Clear();
            return new LoginAttemptResult(LoginAttemptKind.Failed, "Không xác nhận được phiên đăng nhập với hệ thống Công Ty.");
        }

        if (!accessState.TryApply(me, out var accessError)
            || accessState.Current.IsExpired(DateTimeOffset.UtcNow))
        {
            await BestEffortLogoutAsync(token, cancellationToken).ConfigureAwait(false);
            accessState.Clear();
            return new LoginAttemptResult(
                LoginAttemptKind.Failed,
                string.IsNullOrWhiteSpace(accessError)
                    ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                    : accessError);
        }

        try
        {
            await tokenStore.WriteAsync(baseUri, token, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await BestEffortLogoutAsync(token, cancellationToken).ConfigureAwait(false);
            accessState.Clear();
            return new LoginAttemptResult(
                LoginAttemptKind.Failed,
                "Không thể lưu phiên đăng nhập an toàn trên máy tính này.");
        }

        _currentToken = token;
        return new LoginAttemptResult(LoginAttemptKind.Succeeded, "Đăng nhập thành công.");
    }

    public async Task<SessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var baseUri = endpoints.BaseUri;
        if (baseUri is null)
        {
            accessState.Clear();
            _currentToken = null;
            return new SessionRestoreResult(SessionRestoreKind.NoSession, "Chưa cấu hình kết nối hệ thống Công Ty.");
        }

        string? token;
        try
        {
            token = await tokenStore.ReadAsync(baseUri, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            accessState.Clear();
            _currentToken = null;
            return new SessionRestoreResult(
                SessionRestoreKind.Failed,
                "Không thể đọc phiên đăng nhập an toàn trên máy tính này.");
        }

        if (!IsPlausibleSessionToken(token))
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                await SafeDeleteTokenAsync(baseUri, cancellationToken).ConfigureAwait(false);
            }

            accessState.Clear();
            _currentToken = null;
            return new SessionRestoreResult(SessionRestoreKind.NoSession, "Vui lòng đăng nhập để tiếp tục.");
        }

        _currentToken = token;

        try
        {
            var me = await apiClient.GetDataAsync<InternalMeData>(
                "/api/internal-auth/me",
                token,
                cancellationToken).ConfigureAwait(false);

            if (!accessState.TryApply(me, out var accessError))
            {
                await SafeDeleteTokenAsync(baseUri, cancellationToken).ConfigureAwait(false);
                _currentToken = null;
                return new SessionRestoreResult(SessionRestoreKind.Failed, accessError);
            }

            if (accessState.Current.IsExpired(DateTimeOffset.UtcNow))
            {
                await SafeDeleteTokenAsync(baseUri, cancellationToken).ConfigureAwait(false);
                accessState.Clear();
                _currentToken = null;
                return new SessionRestoreResult(
                    SessionRestoreKind.ExpiredOrInvalid,
                    "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
            }

            return new SessionRestoreResult(SessionRestoreKind.Succeeded, "Phiên đăng nhập đang có hiệu lực.");
        }
        catch (CanonicalApiException exception) when (
            exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            await SafeDeleteTokenAsync(baseUri, cancellationToken).ConfigureAwait(false);
            accessState.Clear();
            _currentToken = null;
            return new SessionRestoreResult(
                SessionRestoreKind.ExpiredOrInvalid,
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId);
        }
        catch (CanonicalApiException exception) when (exception.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            accessState.Clear();
            return new SessionRestoreResult(
                SessionRestoreKind.Unavailable,
                "Chưa thể kiểm tra phiên đăng nhập vì hệ thống Công Ty tạm thời chưa sẵn sàng.",
                exception.RequestId);
        }
        catch (CanonicalApiException exception)
        {
            accessState.Clear();
            return new SessionRestoreResult(
                SessionRestoreKind.Failed,
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            accessState.Clear();
            return new SessionRestoreResult(
                SessionRestoreKind.Unavailable,
                "Không kết nối được hệ thống Công Ty để kiểm tra phiên đăng nhập.");
        }
    }

    public async Task<LogoutResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var baseUri = endpoints.BaseUri;
        var token = _currentToken;

        if (baseUri is not null && string.IsNullOrWhiteSpace(token))
        {
            try
            {
                token = await tokenStore.ReadAsync(baseUri, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                token = null;
            }
        }

        var message = "Đã đăng xuất khỏi máy tính này.";
        string? requestId = null;

        if (IsPlausibleSessionToken(token))
        {
            try
            {
                await apiClient.PostDataAsync<InternalLogoutData>(
                    "/api/internal-auth/logout",
                    token,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (CanonicalApiException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
            {
                requestId = exception.RequestId;
            }
            catch (CanonicalApiException exception)
            {
                message = "Đã đăng xuất khỏi máy tính này nhưng chưa xác nhận được việc thu hồi phiên trên hệ thống.";
                requestId = exception.RequestId;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                message = "Đã đăng xuất khỏi máy tính này nhưng chưa kết nối được hệ thống để thu hồi phiên.";
            }
        }

        if (baseUri is not null)
        {
            await SafeDeleteTokenAsync(baseUri, cancellationToken).ConfigureAwait(false);
        }

        _currentToken = null;
        accessState.Clear();
        return new LogoutResult(message, requestId);
    }

    private async Task BestEffortLogoutAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.PostDataAsync<InternalLogoutData>(
                "/api/internal-auth/logout",
                token,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private async Task SafeDeleteTokenAsync(Uri baseUri, CancellationToken cancellationToken)
    {
        try
        {
            await tokenStore.DeleteAsync(baseUri, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private static bool IsPlausibleSessionToken(string? token) =>
        !string.IsNullOrWhiteSpace(token)
        && token.StartsWith(SessionPrefix, StringComparison.Ordinal)
        && token.Length <= 512;
}
