using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class SettingsPrintAppearanceParityTests
{
    [TestMethod]
    public void PrintTemplates_UsesCurrentContractPermissionAndCanonicalIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "DocumentPrintTemplateService.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Settings", "PrintTemplatesViewModel.cs");

        StringAssert.Contains(service, "\"/api/document-print-templates\"");
        StringAssert.Contains(service, "PatchIdempotentDataAsync");
        StringAssert.Contains(viewModel, "core.print-template.manage");
        StringAssert.Contains(viewModel, "system:security-owner");
        StringAssert.Contains(viewModel, "system:implementation-owner");
        StringAssert.Contains(viewModel, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(viewModel, "_pendingFingerprint");
        StringAssert.Contains(viewModel, "ReuseKey(fingerprint");
        Assert.IsFalse(viewModel.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PrintTemplates_ViewPreservesCurrentWebSectionsAndActions()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Settings", "PrintTemplatesView.xaml");

        foreach (var text in new[]
        {
            "Cấu hình mẫu in dùng chung",
            "Loại chứng từ / mẫu in",
            "Phần đầu phiếu",
            "Tên Công Ty",
            "Loại đơn",
            "Thông tin được in",
            "Luôn in",
            "Xem trước",
            "Khổ giấy",
            "LƯU CẤU HÌNH",
            "KHÔI PHỤC MẶC ĐỊNH"
        }) StringAssert.Contains(view, text);

        StringAssert.Contains(view, "Content=\"Ứng dụng máy tính\"");
        StringAssert.Contains(view, "Click=\"DesktopApp_OnClick\" Content=\"Ứng dụng máy tính\"");
        Assert.IsFalse(view.Contains("NPP Core", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void PrintTemplateContracts_DeserializeCurrentWireShape()
    {
        const string json = """
        {
          "documentType":"SALES_ORDER",
          "templateCode":"standard",
          "name":"PHIẾU XUẤT KHO",
          "pageSize":"A4",
          "fontSizePercent":100,
          "visibleFieldKeys":["line_item","line_quantity"],
          "fields":[
            {"key":"line_item","label":"Tên sản phẩm","defaultSelected":true,"required":true},
            {"key":"line_quantity","label":"Số lượng","defaultSelected":true,"required":true}
          ],
          "heading":"Hưng Phát",
          "title":"PHIẾU XUẤT KHO",
          "subtitle":null,
          "headingVisible":true,
          "headingAlign":"left",
          "titleAlign":"right",
          "isCustomized":true,
          "updatedAt":"2026-09-18T00:00:00.000Z"
        }
        """;

        var template = JsonSerializer.Deserialize<DocumentPrintTemplateData>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được hợp đồng mẫu in.");

        Assert.AreEqual("SALES_ORDER", template.DocumentType);
        Assert.AreEqual("A4", template.PageSize);
        Assert.HasCount(2, template.VisibleFieldKeys);
        Assert.AreEqual("line_item", template.Fields[0].Key);
        Assert.IsTrue(template.Fields[0].Required);
        Assert.AreEqual("right", template.TitleAlign);
    }

    [TestMethod]
    public void Appearance_UsesExistingLocalSettingsAndThreeWebChoices()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Settings", "AppearanceView.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Settings", "AppearanceViewModel.cs");
        var settings = ReadRepoFile("src", "CongTy.Windows", "DesktopSettings.cs");
        var themeManager = ReadRepoFile("src", "CongTy.Desktop", "Themes", "ThemeManager.cs");
        var green = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Theme.Green.xaml");

        foreach (var text in new[]
        {
            "MÀU SẮC",
            "Chọn giao diện",
            "Mặc định",
            "Xanh lá",
            "Tối",
            "KÍCH THƯỚC HIỂN THỊ",
            "Tăng hoặc giảm kích thước",
            "VỀ MẶC ĐỊNH"
        }) StringAssert.Contains(view, text);

        StringAssert.Contains(viewModel, "ILocalSettingsStore");
        StringAssert.Contains(viewModel, "DesktopSettingsState");
        Assert.IsFalse(viewModel.Contains("CompanyApiClient", StringComparison.Ordinal));
        StringAssert.Contains(settings, "DisplayScale");
        StringAssert.Contains(themeManager, "\"Green\"");
        StringAssert.Contains(themeManager, "NormalizeScale");
        StringAssert.Contains(themeManager, "0.04d");
        StringAssert.Contains(green, "#2F7D52");
        StringAssert.Contains(green, "#285F43");
    }

    [TestMethod]
    public void SettingsShell_WiresBackupPrintDesktopAppAndAppearance()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.DataBackup.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.DataBackup.cs");
        var backupView = ReadRepoFile("src", "CongTy.Desktop", "Settings", "DataBackupView.xaml");

        StringAssert.Contains(shell, "settings.data-backup");
        StringAssert.Contains(shell, "settings.print-templates");
        StringAssert.Contains(shell, "settings.appearance");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex is 47 or 48 or 49 or 54");
        StringAssert.Contains(host, "workspaceTabs.Items[47]");
        StringAssert.Contains(host, "workspaceTabs.Items[48]");
        StringAssert.Contains(host, "workspaceTabs.Items[49]");
        StringAssert.Contains(shell, "settings.desktop-app");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 54");
        StringAssert.Contains(host, "DesktopAppView");
        StringAssert.Contains(host, "workspaceTabs.Items[54]");
        StringAssert.Contains(backupView, "Click=\"PrintTemplates_OnClick\"");
        StringAssert.Contains(backupView, "Click=\"DesktopApp_OnClick\"");
        StringAssert.Contains(backupView, "Click=\"Appearance_OnClick\"");
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
