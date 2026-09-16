using System.Text.RegularExpressions;

namespace CongTy.ApiClient;

public interface ICanonicalIdempotencyKeyProvider
{
    string Create(string scope);
    bool IsValid(string? value);
}

public sealed partial class CanonicalIdempotencyKeyProvider : ICanonicalIdempotencyKeyProvider
{
    public string Create(string scope)
    {
        var normalizedScope = NormalizeScope(scope);
        var key = $"{normalizedScope}_{Guid.NewGuid():N}";
        if (!IsValid(key))
        {
            throw new InvalidOperationException("Không tạo được khóa chống xử lý trùng hợp lệ.");
        }

        return key;
    }

    public bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && CanonicalPattern().IsMatch(value.Trim());

    private static string NormalizeScope(string scope)
    {
        var candidate = string.IsNullOrWhiteSpace(scope) ? "desktop" : scope.Trim().ToLowerInvariant();
        candidate = UnsafeCharacters().Replace(candidate, "-").Trim('-', '_', '.');
        if (candidate.Length == 0)
        {
            candidate = "desktop";
        }

        return candidate.Length <= 80 ? candidate : candidate[..80];
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalPattern();

    [GeneratedRegex("[^A-Za-z0-9._-]+", RegexOptions.CultureInvariant)]
    private static partial Regex UnsafeCharacters();
}
