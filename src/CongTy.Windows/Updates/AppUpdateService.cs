using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace CongTy.Windows.Updates;

public sealed record AppUpdateArtifactValidation(bool IsValid, string? Error);

public static class AppUpdateArtifactVerifier
{
    public static async Task<AppUpdateArtifactValidation> VerifyAsync(
        string filePath,
        AppUpdateManifest manifest,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return new AppUpdateArtifactValidation(false, "Không tìm thấy file cập nhật.");

        var info = new FileInfo(filePath);
        if (info.Length != manifest.Size)
            return new AppUpdateArtifactValidation(
                false,
                $"Dung lượng file không khớp manifest ({info.Length} / {manifest.Size} byte).");

        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        var actual = Convert.ToHexString(hash);
        if (!string.Equals(actual, manifest.Sha256, StringComparison.OrdinalIgnoreCase))
            return new AppUpdateArtifactValidation(false, "SHA-256 của file cập nhật không khớp manifest.");

        return new AppUpdateArtifactValidation(true, null);
    }
}

public sealed class AppUpdateService : IAppUpdateService, IDisposable
{
    public const string UpdateFeedUrl =
        "https://pub-381648426a2447a7a5edd970ca02d14e.r2.dev/core/windows/stable/latest.json";

    private const string RegistryPath = @"Software\CongTy\Desktop";
    private static readonly Uri FeedRoot =
        new("https://pub-381648426a2447a7a5edd970ca02d14e.r2.dev/core/windows/stable/");
    private static readonly Uri ManifestUri = new(UpdateFeedUrl);
    private static readonly JsonSerializerOptions ManifestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _currentVersion;
    private AppUpdateManifest? _readyManifest;
    private string? _readyInstallerPath;
    private AppUpdateSnapshot _current;

    public AppUpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _currentVersion = ReadCurrentVersion();

        if (!IsInstalledBuild())
        {
            _current = NewSnapshot(
                AppUpdatePhase.Unsupported,
                "Cập nhật chỉ hoạt động trên bản CONGTY đã cài.");
            return;
        }

        var marker = AppUpdatePendingMarkerStore.Load();
        var postRestart = AppUpdatePendingMarkerStore.Resolve(_currentVersion, marker);
        if (postRestart.Updated && postRestart.PreviousVersion is not null)
        {
            AppUpdatePendingMarkerStore.Clear();
            _current = new AppUpdateSnapshot(
                AppUpdatePhase.Updated,
                _currentVersion,
                null,
                postRestart.PreviousVersion,
                string.Empty,
                null,
                $"Đã cập nhật thành công từ v{postRestart.PreviousVersion} lên v{_currentVersion}.",
                null);
            return;
        }

        _current = NewSnapshot(
            AppUpdatePhase.Idle,
            postRestart.PendingStillUnresolved
                ? "Lần cập nhật trước chưa hoàn tất. Có thể kiểm tra lại bản cập nhật."
                : "Sẵn sàng kiểm tra bản cập nhật.");
    }

    public AppUpdateSnapshot Current => _current;

    public event EventHandler<AppUpdateSnapshot>? StateChanged;

    public async Task<AppUpdateSnapshot> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (_current.Phase is AppUpdatePhase.Unsupported
            or AppUpdatePhase.Checking
            or AppUpdatePhase.Available
            or AppUpdatePhase.Downloading
            or AppUpdatePhase.Ready
            or AppUpdatePhase.Installing)
            return _current;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_current.Phase is AppUpdatePhase.Checking
                or AppUpdatePhase.Available
                or AppUpdatePhase.Downloading
                or AppUpdatePhase.Ready
                or AppUpdatePhase.Installing)
                return _current;

            Publish(NewSnapshot(AppUpdatePhase.Checking, "Đang kiểm tra bản cập nhật…"));

            using var request = new HttpRequestMessage(HttpMethod.Get, ManifestUri);
            request.Headers.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };
            request.Headers.Pragma.ParseAdd("no-cache");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var manifestStream =
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var manifest = await JsonSerializer.DeserializeAsync<AppUpdateManifest>(
                manifestStream,
                ManifestJsonOptions,
                cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidDataException("latest.json rỗng.");

            var manifestErrors = manifest.Validate();
            if (manifestErrors.Count > 0)
                throw new InvalidDataException(string.Join(" ", manifestErrors));

            if (!AppSemanticVersion.TryParse(_currentVersion, out var currentVersion))
                throw new InvalidDataException("Phiên bản ứng dụng hiện tại không hợp lệ.");
            if (!AppSemanticVersion.TryParse(manifest.LatestVersion, out var latestVersion))
                throw new InvalidDataException("Phiên bản trên máy chủ không hợp lệ.");

            if (latestVersion.CompareTo(currentVersion) <= 0)
            {
                Publish(new AppUpdateSnapshot(
                    AppUpdatePhase.UpToDate,
                    _currentVersion,
                    null,
                    null,
                    manifest.ReleaseNotes,
                    null,
                    $"CONGTY v{_currentVersion} đang là bản mới nhất.",
                    null));
                return _current;
            }

            Publish(new AppUpdateSnapshot(
                AppUpdatePhase.Available,
                _currentVersion,
                manifest.LatestVersion,
                null,
                manifest.ReleaseNotes,
                null,
                $"Có bản mới v{manifest.LatestVersion}. Đang chuẩn bị tải…",
                null));

            return await DownloadAsync(manifest, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Fail("Đã hủy kiểm tra hoặc tải bản cập nhật.");
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or TaskCanceledException
            or IOException
            or JsonException
            or InvalidDataException
            or UnauthorizedAccessException)
        {
            return Fail(exception.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AppUpdateSnapshot> InstallAsync(CancellationToken cancellationToken = default)
    {
        if (_current.Phase != AppUpdatePhase.Ready
            || _readyManifest is null
            || string.IsNullOrWhiteSpace(_readyInstallerPath))
            return _current;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var verification = await AppUpdateArtifactVerifier.VerifyAsync(
                _readyInstallerPath,
                _readyManifest,
                cancellationToken).ConfigureAwait(false);
            if (!verification.IsValid)
                return Fail(verification.Error ?? "File cập nhật không còn hợp lệ.");

            AppUpdatePendingMarkerStore.Save(new AppUpdatePendingMarker(
                _currentVersion,
                _readyManifest.LatestVersion,
                DateTimeOffset.UtcNow));

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = _readyInstallerPath,
                Arguments = $"/S /UPDATE=1 /WAITPID={Environment.ProcessId}",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(_readyInstallerPath)
                    ?? AppContext.BaseDirectory
            });
            if (process is null)
            {
                AppUpdatePendingMarkerStore.Clear();
                return Fail("Không khởi động được bộ cài cập nhật.");
            }

            Publish(new AppUpdateSnapshot(
                AppUpdatePhase.Installing,
                _currentVersion,
                _readyManifest.LatestVersion,
                null,
                _readyManifest.ReleaseNotes,
                _current.Progress,
                $"Đang khởi động lại để cài v{_readyManifest.LatestVersion}…",
                null));
            return _current;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AppUpdatePendingMarkerStore.Clear();
            return Fail("Đã hủy cài đặt bản cập nhật.");
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception)
        {
            AppUpdatePendingMarkerStore.Clear();
            return Fail(exception.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    public static bool IsExecutableInInstallDirectory(string? installDirectory, string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(installDirectory)
            || string.IsNullOrWhiteSpace(executablePath))
            return false;

        try
        {
            var install = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installDirectory));
            var executable = Path.GetFullPath(executablePath);
            var executableDirectory = Path.TrimEndingDirectorySeparator(
                Path.GetDirectoryName(executable) ?? string.Empty);

            return string.Equals(
                       install,
                       executableDirectory,
                       StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    Path.GetFileName(executable),
                    "CongTy.Desktop.exe",
                    StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            return false;
        }
    }

    private async Task<AppUpdateSnapshot> DownloadAsync(
        AppUpdateManifest manifest,
        CancellationToken cancellationToken)
    {
        if (!TryResolveDownloadUri(manifest, out var downloadUri, out var uriError))
            return Fail(uriError ?? "Đường dẫn tải cập nhật không hợp lệ.");

        var updateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CongTy",
            "Desktop",
            "Updates",
            manifest.LatestVersion);
        Directory.CreateDirectory(updateDirectory);

        var finalPath = Path.Combine(updateDirectory, manifest.DownloadPath);
        var partialPath = $"{finalPath}.partial";
        File.Delete(partialPath);
        File.Delete(finalPath);

        Publish(new AppUpdateSnapshot(
            AppUpdatePhase.Downloading,
            _currentVersion,
            manifest.LatestVersion,
            null,
            manifest.ReleaseNotes,
            new AppUpdateProgress(0, 0, manifest.Size, 0),
            $"Đang tải bản cập nhật v{manifest.LatestVersion}…",
            null));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUri);
            request.Headers.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength is long contentLength
                && contentLength != manifest.Size)
                throw new InvalidDataException(
                    $"Dung lượng tải xuống không khớp manifest ({contentLength} / {manifest.Size} byte).");

            await using var input =
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var output = new FileStream(
                partialPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new byte[81920];
            long transferred = 0;
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                var read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0) break;

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                    .ConfigureAwait(false);
                transferred += read;
                if (transferred > manifest.Size)
                    throw new InvalidDataException("File tải xuống lớn hơn size trong manifest.");

                var elapsed = Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
                var percent = Math.Min(100d, transferred * 100d / manifest.Size);
                Publish(new AppUpdateSnapshot(
                    AppUpdatePhase.Downloading,
                    _currentVersion,
                    manifest.LatestVersion,
                    null,
                    manifest.ReleaseNotes,
                    new AppUpdateProgress(
                        percent,
                        transferred,
                        manifest.Size,
                        transferred / elapsed),
                    $"Đang tải bản cập nhật v{manifest.LatestVersion}…",
                    null));
            }

            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (transferred != manifest.Size)
                throw new InvalidDataException(
                    $"Dung lượng file tải xuống không đủ ({transferred} / {manifest.Size} byte).");

            var verification = await AppUpdateArtifactVerifier.VerifyAsync(
                partialPath,
                manifest,
                cancellationToken).ConfigureAwait(false);
            if (!verification.IsValid)
                throw new InvalidDataException(
                    verification.Error ?? "File cập nhật không qua được kiểm tra.");

            File.Move(partialPath, finalPath, overwrite: true);
            _readyManifest = manifest;
            _readyInstallerPath = finalPath;

            Publish(new AppUpdateSnapshot(
                AppUpdatePhase.Ready,
                _currentVersion,
                manifest.LatestVersion,
                null,
                manifest.ReleaseNotes,
                new AppUpdateProgress(100, manifest.Size, manifest.Size, 0),
                $"Bản v{manifest.LatestVersion} đã tải xong. Sẵn sàng khởi động lại và cập nhật.",
                null));
            return _current;
        }
        catch
        {
            File.Delete(partialPath);
            File.Delete(finalPath);
            throw;
        }
    }

    private static bool TryResolveDownloadUri(
        AppUpdateManifest manifest,
        out Uri downloadUri,
        out string? error)
    {
        downloadUri = null!;
        error = null;

        if (manifest.Validate().Count > 0)
        {
            error = "Manifest cập nhật không hợp lệ.";
            return false;
        }

        var candidate = new Uri(FeedRoot, manifest.DownloadPath);
        if (!string.Equals(candidate.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(candidate.Host, FeedRoot.Host, StringComparison.OrdinalIgnoreCase)
            || !candidate.AbsolutePath.StartsWith(FeedRoot.AbsolutePath, StringComparison.Ordinal))
        {
            error = "Manifest trỏ ra ngoài máy chủ cập nhật chính hãng.";
            return false;
        }

        downloadUri = candidate;
        return true;
    }

    private static bool IsInstalledBuild()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false);
        var installDirectory = key?.GetValue("InstallDir") as string;
        return IsExecutableInInstallDirectory(installDirectory, Environment.ProcessPath);
    }

    private static string ReadCurrentVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
            return informational.Split('+', 2)[0];

        var version = assembly.GetName().Version;
        return version is null
            ? "0.0.0"
            : $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
    }

    private AppUpdateSnapshot NewSnapshot(AppUpdatePhase phase, string message) =>
        new(
            phase,
            _currentVersion,
            null,
            null,
            string.Empty,
            null,
            message,
            null);

    private AppUpdateSnapshot Fail(string error)
    {
        Publish(new AppUpdateSnapshot(
            AppUpdatePhase.Error,
            _currentVersion,
            _current.AvailableVersion,
            _current.PreviousVersion,
            _current.ReleaseNotes,
            null,
            "Không thể cập nhật CONGTY.",
            error));
        return _current;
    }

    private void Publish(AppUpdateSnapshot next)
    {
        if (!AppUpdatePhaseRules.IsAllowed(_current.Phase, next.Phase))
            throw new InvalidOperationException(
                $"Chuyển trạng thái updater không hợp lệ: {_current.Phase} -> {next.Phase}.");

        _current = next;
        StateChanged?.Invoke(this, next);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _gate.Dispose();
    }
}
