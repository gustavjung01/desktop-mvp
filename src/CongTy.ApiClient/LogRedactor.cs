using System.Text.RegularExpressions;

namespace CongTy.ApiClient;

public interface ILogRedactor
{
    string Redact(string? value);
}

public sealed partial class LogRedactor : ILogRedactor
{
    private const string Mask = "[REDACTED]";

    public string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var result = BearerPattern().Replace(value, $"Bearer {Mask}");
        result = AssignmentPattern().Replace(result, match => $"{match.Groups["key"].Value}={Mask}");
        result = UrlCredentialPattern().Replace(result, match => $"{match.Groups["scheme"].Value}://{Mask}@");
        return result;
    }

    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.CultureInvariant)]
    private static partial Regex BearerPattern();

    [GeneratedRegex(@"(?i)(?<key>password|passwd|token|secret|authorization|api[_-]?key|database_url|service[_-]?role[_-]?key)\s*[:=]\s*(?<value>[^\s,;]+)", RegexOptions.CultureInvariant)]
    private static partial Regex AssignmentPattern();

    [GeneratedRegex(@"(?i)(?<scheme>postgres(?:ql)?|https?)://[^@\s/]+@", RegexOptions.CultureInvariant)]
    private static partial Regex UrlCredentialPattern();
}
