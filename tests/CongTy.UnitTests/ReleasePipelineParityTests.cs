using System.Text.Json;

namespace CongTy.UnitTests;

[TestClass]
public sealed class ReleasePipelineParityTests
{
    [TestMethod]
    public void ReleaseSourceOfTruthAndScriptsMatchKeyManagerContract()
    {
        using var release = JsonDocument.Parse(ReadRepoFile("release.json"));
        Assert.AreEqual("1.0.0", release.RootElement.GetProperty("version").GetString());

        var script = ReadRepoFile("scripts", "release.ps1");
        StringAssert.Contains(script, "KM_RELEASE_VERSION");
        StringAssert.Contains(script, "KM_RELEASE_NOTES");
        StringAssert.Contains(script, "dotnet restore $projectFile --runtime win-x64");
        StringAssert.Contains(script, "CONGTY-Setup-$version.exe");
        StringAssert.Contains(script, "latest.json");
        StringAssert.Contains(script, "schemaVersion = 1");
        StringAssert.Contains(script, "latestVersion = $version");
        StringAssert.Contains(script, "downloadPath = $installerName");
        StringAssert.Contains(script, "Get-FileHash");
        StringAssert.Contains(script, "SHA256");
    }

    [TestMethod]
    public void InstallerIsPerUserSilentUpdateAndPreservesExternalSettings()
    {
        var installer = ReadRepoFile("installer", "congty.nsi");

        StringAssert.Contains(installer, "RequestExecutionLevel user");
        StringAssert.Contains(installer, "SetRegView 64");
        StringAssert.Contains(installer, "$LOCALAPPDATA\\Programs\\CONGTY");
        StringAssert.Contains(installer, "/WAITPID=");
        StringAssert.Contains(installer, "Wait-Process");
        StringAssert.Contains(installer, "CreateShortCut \"$DESKTOP\\CONGTY.lnk\"");
        StringAssert.Contains(installer, "Software\\CongTy\\Desktop");
        StringAssert.Contains(installer, "DisplayIcon");
        StringAssert.Contains(installer, "MUI_UNPAGE_CONFIRM");
        StringAssert.Contains(installer, "Exec '\"$INSTDIR\\${PRODUCT_EXE}\"'");
        Assert.IsFalse(installer.Contains(
            "$LOCALAPPDATA\\CongTy\\Desktop\\settings.json",
            StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrandingUsesTrackedLogoLocallyAndGeneratesWindowsIcon()
    {
        var project = ReadRepoFile("src", "CongTy.Desktop", "CongTy.Desktop.csproj");
        var mainWindow = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var iconScript = ReadRepoFile("scripts", "create-brand-icon.ps1");

        StringAssert.Contains(project, "<ApplicationIcon>Assets\\Brand\\logo.ico</ApplicationIcon>");
        StringAssert.Contains(project, "<Link>Assets\\Brand\\logo.jpg</Link>");
        StringAssert.Contains(mainWindow, "Source=\"/Assets/Brand/logo.jpg\"");
        Assert.IsFalse(mainWindow.Contains(
            "https://retail.nguyenlieuhungphat.com/logo-transparent.png",
            StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(iconScript, "@(16, 24, 32, 48, 64, 128, 256)");
    }

    [TestMethod]
    public void UpdaterFeedIsOfficialR2AndNoWriteCredentialsAreEmbedded()
    {
        var updater = ReadRepoFile("src", "CongTy.Windows", "Updates", "AppUpdateService.cs");
        StringAssert.Contains(
            updater,
            "https://pub-381648426a2447a7a5edd970ca02d14e.r2.dev/core/windows/stable/latest.json");
        StringAssert.Contains(updater, "SHA256.HashDataAsync");
        StringAssert.Contains(updater, "Manifest trỏ ra ngoài máy chủ cập nhật chính hãng");
        Assert.IsFalse(updater.Contains("R2_SECRET", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(updater.Contains("ACCESS_KEY", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void DesktopSettingsUpdateScreenHasCanonicalRouteAndActions()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.DataBackup.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.DataBackup.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Settings", "DesktopAppView.xaml");

        StringAssert.Contains(shell, "settings.desktop-app");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 54");
        StringAssert.Contains(host, "workspaceTabs.Items[54]");
        StringAssert.Contains(view, "Phiên bản hiện tại");
        StringAssert.Contains(view, "Phiên bản mới");
        StringAssert.Contains(view, "Tiến trình tải");
        StringAssert.Contains(view, "PrimaryAction_OnClick");
        StringAssert.Contains(view, "Value=\"{Binding ProgressPercent, Mode=OneWay}\"");
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            directory = directory.Parent;
        }

        Assert.Fail($"Không tìm thấy tệp trong repo: {string.Join("/", parts)}");
        return string.Empty;
    }
}
