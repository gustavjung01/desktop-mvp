namespace CongTy.ApiClient;

public interface ICompanyEndpointProvider
{
    Uri? BaseUri { get; }
    bool TrySet(string? value, out string normalizedUrl, out string errorMessage);
    void SetBaseUri(Uri baseUri);
}

public sealed class CompanyEndpointProvider : ICompanyEndpointProvider
{
    private readonly object _gate = new();
    private Uri? _baseUri;

    public Uri? BaseUri
    {
        get
        {
            lock (_gate)
            {
                return _baseUri;
            }
        }
    }

    public bool TrySet(string? value, out string normalizedUrl, out string errorMessage)
    {
        if (!TryNormalizeHttpsBaseUrl(value, out var baseUri, out normalizedUrl, out errorMessage))
        {
            return false;
        }

        SetBaseUri(baseUri!);
        return true;
    }

    public void SetBaseUri(Uri baseUri)
    {
        ArgumentNullException.ThrowIfNull(baseUri);

        if (!TryNormalizeHttpsBaseUrl(baseUri.AbsoluteUri, out var normalized, out _, out var error))
        {
            throw new ArgumentException(error, nameof(baseUri));
        }

        lock (_gate)
        {
            _baseUri = normalized;
        }
    }

    public static bool TryNormalizeHttpsBaseUrl(
        string? value,
        out Uri? baseUri,
        out string normalizedUrl,
        out string errorMessage)
    {
        baseUri = null;
        normalizedUrl = string.Empty;
        errorMessage = string.Empty;

        var candidate = value?.Trim() ?? string.Empty;
        if (candidate.Length == 0)
        {
            errorMessage = "Địa chỉ hệ thống là bắt buộc.";
            return false;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var parsed)
            || !string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(parsed.Host))
        {
            errorMessage = "Địa chỉ hệ thống phải là địa chỉ HTTPS hợp lệ.";
            return false;
        }

        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            errorMessage = "Địa chỉ hệ thống không được chứa tên đăng nhập hoặc mật khẩu.";
            return false;
        }

        if (!string.IsNullOrEmpty(parsed.Query) || !string.IsNullOrEmpty(parsed.Fragment))
        {
            errorMessage = "Địa chỉ hệ thống không được chứa tham số hoặc dấu neo.";
            return false;
        }

        if (parsed.AbsolutePath is not ("" or "/"))
        {
            errorMessage = "Chỉ nhập địa chỉ gốc của hệ thống, không kèm đường dẫn.";
            return false;
        }

        normalizedUrl = parsed.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        baseUri = new Uri($"{normalizedUrl}/", UriKind.Absolute);
        return true;
    }
}
