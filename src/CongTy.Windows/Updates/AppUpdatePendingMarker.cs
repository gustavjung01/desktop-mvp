using System.Text.Json;

namespace CongTy.Windows.Updates;

public sealed record AppUpdatePendingMarker(
    string FromVersion,
    string ToVersion,
    DateTimeOffset RequestedAt);

public sealed record AppUpdatePostRestartResult(
    bool Updated,
    string? PreviousVersion,
    bool PendingStillUnresolved);

public static class AppUpdatePendingMarkerStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CongTy",
        "Desktop",
        "update-pending.json");

    public static AppUpdatePostRestartResult Resolve(
        string currentVersion,
        AppUpdatePendingMarker? marker)
    {
        if (marker is null)
            return new AppUpdatePostRestartResult(false, null, false);

        var updated = string.Equals(marker.ToVersion, currentVersion, StringComparison.Ordinal)
            && !string.Equals(marker.FromVersion, currentVersion, StringComparison.Ordinal);

        return new AppUpdatePostRestartResult(
            updated,
            updated ? marker.FromVersion : null,
            !updated);
    }

    public static AppUpdatePendingMarker? Load(string? path = null)
    {
        var filePath = path ?? DefaultPath;
        if (!File.Exists(filePath)) return null;

        try
        {
            return JsonSerializer.Deserialize<AppUpdatePendingMarker>(
                File.ReadAllText(filePath),
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Save(AppUpdatePendingMarker marker, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(marker);

        var filePath = path ?? DefaultPath;
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = $"{filePath}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(marker, JsonOptions));
        File.Move(temporaryPath, filePath, overwrite: true);
    }

    public static void Clear(string? path = null)
    {
        var filePath = path ?? DefaultPath;
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
