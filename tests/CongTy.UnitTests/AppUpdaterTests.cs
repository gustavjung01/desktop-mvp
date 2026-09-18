using System.Security.Cryptography;
using System.Text;
using CongTy.Windows.Updates;

namespace CongTy.UnitTests;

[TestClass]
public sealed class AppUpdaterTests
{
    [TestMethod]
    public void SemanticVersion_OrdersNewerEqualOlderAndPrerelease()
    {
        Assert.IsTrue(AppSemanticVersion.TryParse("1.2.3", out var stable));
        Assert.IsTrue(AppSemanticVersion.TryParse("1.2.4", out var newer));
        Assert.IsTrue(AppSemanticVersion.TryParse("1.2.3-beta.2", out var prerelease));
        Assert.IsTrue(AppSemanticVersion.TryParse("1.2.3", out var equal));

        Assert.IsGreaterThan(0, newer.CompareTo(stable));
        Assert.AreEqual(0, stable.CompareTo(equal));
        Assert.IsLessThan(0, prerelease.CompareTo(stable));
    }

    [TestMethod]
    public void SemanticVersion_RejectsInvalidValues()
    {
        Assert.IsFalse(AppSemanticVersion.TryParse("1.2", out _));
        Assert.IsFalse(AppSemanticVersion.TryParse("01.2.3", out _));
        Assert.IsFalse(AppSemanticVersion.TryParse("1.2.3-01", out _));
        Assert.IsFalse(AppSemanticVersion.TryParse("v1.2.3", out _));
    }

    [TestMethod]
    public void Manifest_ValidatesSchemaSecurityHashAndSize()
    {
        var valid = CreateManifest();
        Assert.IsEmpty(valid.Validate());

        Assert.IsNotEmpty((valid with { SchemaVersion = 2 }).Validate());
        Assert.IsNotEmpty((valid with { DownloadPath = "../evil.exe" }).Validate());
        Assert.IsNotEmpty((valid with { DownloadPath = "nested/update.exe" }).Validate());
        Assert.IsNotEmpty((valid with { DownloadPath = "https://evil.invalid/a.exe" }).Validate());
        Assert.IsNotEmpty((valid with { Sha256 = "abc" }).Validate());
        Assert.IsNotEmpty((valid with { Size = 0 }).Validate());
    }

    [TestMethod]
    public async Task ArtifactVerifier_RejectsHashAndSizeMismatch()
    {
        var path = Path.GetTempFileName();
        try
        {
            var bytes = Encoding.UTF8.GetBytes("congty-update");
            await File.WriteAllBytesAsync(path, bytes);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));

            var valid = CreateManifest() with
            {
                Size = bytes.Length,
                Sha256 = hash
            };
            Assert.IsTrue((await AppUpdateArtifactVerifier.VerifyAsync(path, valid)).IsValid);

            var badHash = await AppUpdateArtifactVerifier.VerifyAsync(
                path,
                valid with { Sha256 = new string('0', 64) });
            Assert.IsFalse(badHash.IsValid);
            StringAssert.Contains(badHash.Error, "SHA-256");

            var badSize = await AppUpdateArtifactVerifier.VerifyAsync(
                path,
                valid with { Size = bytes.Length + 1 });
            Assert.IsFalse(badSize.IsValid);
            StringAssert.Contains(badSize.Error, "Dung lượng");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void PendingMarker_OnlyReportsSuccessAfterVersionReallyChanged()
    {
        var marker = new AppUpdatePendingMarker(
            "1.0.0",
            "1.0.1",
            DateTimeOffset.UtcNow);

        var success = AppUpdatePendingMarkerStore.Resolve("1.0.1", marker);
        Assert.IsTrue(success.Updated);
        Assert.AreEqual("1.0.0", success.PreviousVersion);
        Assert.IsFalse(success.PendingStillUnresolved);

        var unchanged = AppUpdatePendingMarkerStore.Resolve("1.0.0", marker);
        Assert.IsFalse(unchanged.Updated);
        Assert.IsTrue(unchanged.PendingStillUnresolved);
    }

    [TestMethod]
    public void PhaseRules_CoverCanonicalUpdaterStateMachine()
    {
        Assert.IsTrue(AppUpdatePhaseRules.IsAllowed(AppUpdatePhase.Idle, AppUpdatePhase.Checking));
        Assert.IsTrue(AppUpdatePhaseRules.IsAllowed(AppUpdatePhase.Checking, AppUpdatePhase.Available));
        Assert.IsTrue(AppUpdatePhaseRules.IsAllowed(AppUpdatePhase.Available, AppUpdatePhase.Downloading));
        Assert.IsTrue(AppUpdatePhaseRules.IsAllowed(AppUpdatePhase.Downloading, AppUpdatePhase.Ready));
        Assert.IsTrue(AppUpdatePhaseRules.IsAllowed(AppUpdatePhase.Ready, AppUpdatePhase.Installing));
        Assert.IsTrue(AppUpdatePhaseRules.IsAllowed(AppUpdatePhase.Checking, AppUpdatePhase.UpToDate));
        Assert.IsFalse(AppUpdatePhaseRules.IsAllowed(AppUpdatePhase.Idle, AppUpdatePhase.Installing));
        Assert.IsFalse(AppUpdatePhaseRules.IsAllowed(AppUpdatePhase.Unsupported, AppUpdatePhase.Checking));
    }

    [TestMethod]
    public void InstalledBuildDetector_AcceptsInstalledBaseDirectoryAndRejectsDevelopmentOutput()
    {
        const string installed = @"F:\1_A_Disk_D\Hung-Phat\app";
        const string developmentBase =
            @"C:\src\desktop-mvp\src\CongTy.Desktop\bin\Release\net10.0-windows";
        var installedBase = installed + Path.DirectorySeparatorChar;

        Assert.IsTrue(
            AppUpdateService.IsApplicationBaseDirectoryInInstallDirectory(
                installed,
                installedBase));
        Assert.IsFalse(
            AppUpdateService.IsApplicationBaseDirectoryInInstallDirectory(
                installed,
                developmentBase));
        Assert.IsFalse(
            AppUpdateService.IsApplicationBaseDirectoryInInstallDirectory(
                null,
                installedBase));
    }

    private static AppUpdateManifest CreateManifest() => new()
    {
        SchemaVersion = 1,
        LatestVersion = "1.0.1",
        ReleaseNotes = "Bản kiểm thử",
        DownloadPath = "CONGTY-Setup-1.0.1.exe",
        Sha256 = new string('a', 64),
        Size = 123
    };
}
