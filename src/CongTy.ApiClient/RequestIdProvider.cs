using System.Text.RegularExpressions;

namespace CongTy.ApiClient;

public interface IRequestIdProvider
{
    string Create();
    bool IsValid(string? value);
}

public sealed partial class RequestIdProvider : IRequestIdProvider
{
    private const string Prefix = "desktop";

    public string Create() => $"{Prefix}_{Guid.NewGuid():N}";

    public bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return RequestIdPattern().IsMatch(value.Trim());
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex RequestIdPattern();
}
