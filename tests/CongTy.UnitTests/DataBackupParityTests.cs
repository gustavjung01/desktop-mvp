using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Settings;

namespace CongTy.UnitTests;

[TestClass]
public sealed class DataBackupParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalBackupAndDeletionRoutes()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "DataBackupService.cs");

        foreach (var route in new[]
        {
            "/api/reporting/business-export",
            "/api/backups/technical-access",
            "/api/backups/technical-access/challenges",
            "/api/backups?limit=20",
            "/api/backups",
            "/api/data-deletions"
        }) StringAssert.Contains(source, route);

        StringAssert.Contains(source, "/download");
        StringAssert.Contains(source, "/verify");
        StringAssert.Contains(source, "/execute");
        StringAssert.Contains(source, "\"x-technical-backup-unlock\"");
        StringAssert.Contains(source, "\"Idempotency-Key\"");
    }

    [TestMethod]
    public void ViewModel_UsesOwnerBoundaryAndExactPermissions()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Settings", "DataBackupViewModel.cs");

        foreach (var value in new[]
        {
            "system:security-owner",
            "system:implementation-owner",
            "core.reporting.export",
            "core.backup.read",
            "core.backup.create",
            "core.backup.download",
            "core.data-deletion.authorize"
        }) StringAssert.Contains(source, value);

        StringAssert.Contains(source, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(source, "_idempotency.Create(");
        StringAssert.Contains(source, "PendingKey");
        StringAssert.Contains(source, "Fingerprint");
        Assert.IsFalse(source.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void TechnicalUnlock_IsMemoryOnlyAndRequiredForBackupOperations()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Settings", "DataBackupViewModel.cs");
        var service = ReadRepoFile("src", "CongTy.ApiClient", "DataBackupService.cs");

        StringAssert.Contains(source, "private string? _technicalUnlockToken;");
        StringAssert.Contains(source, "ClearTechnicalUnlockOnly");
        StringAssert.Contains(source, "RequireUnlock()");
        StringAssert.Contains(service, "RequiredUnlock(unlockToken)");
        Assert.IsFalse(source.Contains("SettingsStore", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(source.Contains("CredentialStore", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(source.Contains("@gmail.com", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void View_PreservesCurrentWebSectionsAndDangerGuardrails()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Settings", "DataBackupView.xaml");

        foreach (var text in new[]
        {
            "Dữ liệu &amp; sao lưu",
            "Mẫu in",
            "Ứng dụng máy tính",
            "Giao diện",
            "SỐ LIỆU DOANH NGHIỆP",
            "Excel nghiệp vụ",
            "XUẤT SỐ LIỆU",
            "SAO LƯU HỆ THỐNG",
            "MỞ KHU VỰC KỸ THUẬT",
            "TẠO BẢN SAO LƯU",
            "DI CHUYỂN &amp; KHÔI PHỤC",
            "Gói chuyển hệ thống",
            "LỊCH SỬ SAO LƯU HỆ THỐNG",
            "TẢI .DUMP",
            "TẢI TỆP THÔNG TIN",
            "VÙNG NGUY HIỂM",
            "Xóa dữ liệu",
            "GỬI MÃ XÁC NHẬN",
            "XÁC NHẬN MÃ",
            "XÓA DỮ LIỆU NGAY"
        }) StringAssert.Contains(view, text);

        Assert.IsFalse(view.Contains("DATABASE_URL", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("NPP Core", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("@gmail.com", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(view, "Value=\\\"{Binding ActiveProgress, Mode=OneWay}\\\"");
        StringAssert.Contains(view, "Binding=\\\"{Binding RequestedAt, Mode=OneWay}\\\"");
        StringAssert.Contains(view, "Binding=\\\"{Binding Status, Mode=OneWay}\\\"");
        StringAssert.Contains(view, "Binding=\\\"{Binding SnapshotAt, Mode=OneWay}\\\"");
        StringAssert.Contains(view, "Binding=\\\"{Binding DumpSize, Mode=OneWay}\\\"");
    }

    [TestMethod]
    public void DeleteFlow_RequiresVerifiedBackupCodeAndFinalConfirmation()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Settings", "DataBackupViewModel.cs");
        var codeBehind = ReadRepoFile("src", "CongTy.Desktop", "Settings", "DataBackupView.xaml.cs");

        StringAssert.Contains(source, "_latestVerified is not null");
        StringAssert.Contains(source, "DeleteCode.Length == 6");
        StringAssert.Contains(source, "DeleteAuthorized");
        StringAssert.Contains(source, "CreateDeletionIntentAsync");
        StringAssert.Contains(source, "VerifyDeletionIntentAsync");
        StringAssert.Contains(source, "ExecuteDeletionIntentAsync");
        StringAssert.Contains(codeBehind, "MessageBoxButton.YesNo");
        StringAssert.Contains(codeBehind, "không thể hoàn tác");
    }

    [TestMethod]
    public void Shell_WiresSettingsGeneralToWorkspace47()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.DataBackup.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.DataBackup.cs");
        var hook = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "\"settings.data-backup\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 47");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 47");
        StringAssert.Contains(host, "workspaceTabs.Items[47]");
        StringAssert.Contains(host, "\"CÀI ĐẶT CÔNG TY\"");
        StringAssert.Contains(host, "\"Dữ liệu & sao lưu\"");
        StringAssert.Contains(hook, "WireImportExportHistoryWorkspace();");
        StringAssert.Contains(hook, "WireDataBackupWorkspace();");
        StringAssert.Contains(xaml, "Text=\"Thiết lập chung\"");
    }

    [TestMethod]
    public void Contracts_DeserializeBackupAndDeletionShapes()
    {
        const string backupJson = """
        {
          "id":"11111111-1111-4111-8111-111111111111",
          "status":"VERIFIED",
          "requestedAt":"2026-09-18T01:00:00.000Z",
          "snapshotAt":"2026-09-18T00:59:00.000Z",
          "verifiedAt":"2026-09-18T01:02:00.000Z",
          "schemaVersion":"084",
          "includeXlsx":false,
          "datasetCount":4,
          "totalRowCount":123,
          "artifacts":{
            "databaseDump":{"size":1024,"sha256":"abc"},
            "manifest":{"size":128,"sha256":"def"}
          }
        }
        """;
        const string deletionJson = """
        {
          "id":"22222222-2222-4222-8222-222222222222",
          "status":"PURGED",
          "backupJobId":"11111111-1111-4111-8111-111111111111",
          "targetCode":"OPERATIONS_ONLY",
          "purgeExecuted":true,
          "purgeSummary":{"deletedRows":12,"affectedTableCount":3}
        }
        """;

        var backup = JsonSerializer.Deserialize<DataBackupJobData>(backupJson)
            ?? throw new InvalidOperationException("Không đọc được hợp đồng sao lưu.");
        var deletion = JsonSerializer.Deserialize<DataDeletionIntentData>(deletionJson)
            ?? throw new InvalidOperationException("Không đọc được hợp đồng xóa dữ liệu.");

        Assert.AreEqual("VERIFIED", backup.Status);
        Assert.AreEqual(1024L, backup.Artifacts.DatabaseDump?.Size ?? 0L);
        Assert.AreEqual("PURGED", deletion.Status);
        Assert.AreEqual(12L, deletion.PurgeSummary?.DeletedRows ?? 0L);
        Assert.AreEqual(3, deletion.PurgeSummary?.AffectedTableCount ?? 0);
    }

    [TestMethod]
    public void Presentation_UsesOfficeLabelsForBackupProgress()
    {
        Assert.AreEqual("Xếp hàng", DataBackupPresentation.Status("QUEUED"));
        Assert.AreEqual("Tạo file .dump", DataBackupPresentation.Status("DUMPING_DATABASE"));
        Assert.AreEqual("Đã xác minh", DataBackupPresentation.Status("VERIFIED"));
        Assert.AreEqual(100, DataBackupPresentation.Progress("VERIFIED"));
        Assert.AreEqual("1.0 KB", DataBackupPresentation.Bytes(1024));
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
