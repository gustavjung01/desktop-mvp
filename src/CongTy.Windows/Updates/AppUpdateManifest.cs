using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CongTy.Windows.Updates;

public sealed class AppSemanticVersion : IComparable<AppSemanticVersion>
{
    private static readonly Regex Pattern = new(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private AppSemanticVersion(int major, int minor, int patch, string[] prerelease, string original)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        Original = original;
    }

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public IReadOnlyList<string> Prerelease { get; }
    public string Original { get; }

    public static bool TryParse(string? value, out AppSemanticVersion version)
    {
        version = null!;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var match = Pattern.Match(value.Trim());
        if (!match.Success) return false;

        var prerelease = match.Groups[4].Success
            ? match.Groups[4].Value.Split('.', StringSplitOptions.RemoveEmptyEntries)
            : [];

        if (prerelease.Any(identifier =>
                identifier.Length > 1
                && identifier[0] == '0'
                && identifier.All(char.IsDigit)))
            return false;

        version = new AppSemanticVersion(
            int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture),
            prerelease,
            value.Trim());
        return true;
    }

    public int CompareTo(AppSemanticVersion? other)
    {
        if (other is null) return 1;

        var result = Major.CompareTo(other.Major);
        if (result != 0) return result;
        result = Minor.CompareTo(other.Minor);
        if (result != 0) return result;
        result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;

        if (Prerelease.Count == 0 && other.Prerelease.Count == 0) return 0;
        if (Prerelease.Count == 0) return 1;
        if (other.Prerelease.Count == 0) return -1;

        var count = Math.Max(Prerelease.Count, other.Prerelease.Count);
        for (var index = 0; index < count; index++)
        {
            if (index >= Prerelease.Count) return -1;
            if (index >= other.Prerelease.Count) return 1;

            result = CompareIdentifier(Prerelease[index], other.Prerelease[index]);
            if (result != 0) return result;
        }

        return 0;
    }

    private static int CompareIdentifier(string left, string right)
    {
        var leftNumeric = left.All(char.IsDigit);
        var rightNumeric = right.All(char.IsDigit);
        if (leftNumeric && rightNumeric)
        {
            var leftNumber = left.TrimStart('0');
            var rightNumber = right.TrimStart('0');
            if (leftNumber.Length != rightNumber.Length)
                return leftNumber.Length.CompareTo(rightNumber.Length);
            return string.CompareOrdinal(leftNumber, rightNumber);
        }

        if (leftNumeric != rightNumeric) return leftNumeric ? -1 : 1;
        return string.CompareOrdinal(left, right);
    }
}

public sealed record AppUpdateManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; init; } = string.Empty;

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; init; } = string.Empty;

    [JsonPropertyName("downloadPath")]
    public string DownloadPath { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset? PublishedAt { get; init; }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (SchemaVersion != 1) errors.Add("schemaVersion phải bằng 1.");
        if (!AppSemanticVersion.TryParse(LatestVersion, out _))
            errors.Add("latestVersion không phải SemVer hợp lệ.");

        if (string.IsNullOrWhiteSpace(DownloadPath)
            || Uri.TryCreate(DownloadPath, UriKind.Absolute, out _)
            || DownloadPath.IndexOfAny(new[] { '/', '\\' }) >= 0
            || DownloadPath.Contains("..", StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(DownloadPath), DownloadPath, StringComparison.Ordinal)
            || !string.Equals(Path.GetExtension(DownloadPath), ".exe", StringComparison.OrdinalIgnoreCase))
            errors.Add("downloadPath phải là tên file .exe tương đối an toàn.");

        if (!Regex.IsMatch(Sha256 ?? string.Empty, "^[0-9a-fA-F]{64}$", RegexOptions.CultureInvariant))
            errors.Add("sha256 phải gồm đúng 64 ký tự hex.");
        if (Size <= 0) errors.Add("size phải lớn hơn 0.");

        return errors;
    }
}
