using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class CustomerOrderingContentParityTests
{
    [TestMethod]
    public void Contracts_DeserializeCurrentHomeContentShape()
    {
        const string json = """
        {
          "sectionTitle":"Sự kiện tháng 10",
          "programContent":"Ưu đãi nhiều dòng\nÁp dụng đến cuối tháng.",
          "visible":true,
          "bannerUrl":"https://cdn.example/banner.webp?v=2",
          "imagePresent":true,
          "updatedAt":"2026-10-01T02:03:04.000Z"
        }
        """;
        var content = JsonSerializer.Deserialize<CustomerOrderingHomeContentData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được nội dung đặt hàng.");

        Assert.AreEqual("Sự kiện tháng 10", content.SectionTitle);
        StringAssert.Contains(content.ProgramContent, "Ưu đãi nhiều dòng");
        Assert.IsTrue(content.Visible);
        Assert.IsTrue(content.ImagePresent);
    }

    [TestMethod]
    public void Service_UsesCanonicalCoreEndpointsAndIdempotentMutations()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "CustomerOrderingContentService.cs");

        StringAssert.Contains(service, "\"/api/customer-ordering-home-content\"");
        StringAssert.Contains(service, "\"/api/customer-ordering-home-content/banner\"");
        StringAssert.Contains(service, "PatchIdempotentDataAsync");
        StringAssert.Contains(service, "PutBytesIdempotentDataAsync");
        StringAssert.Contains(service, "CustomerOrderingHomeContentEnvelopeData");
        StringAssert.Contains(service, "return data.Content;");
        StringAssert.Contains(service, "\"image/webp\"");
        StringAssert.Contains(service, "idempotencyKeys.IsValid");
        Assert.IsFalse(service.Contains("MCP_API_SERVER_TOKEN", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_ReusesCanonicalKeysForSameLogicalRetry()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Settings", "CustomerOrderingContentViewModel.cs");

        StringAssert.Contains(vm, "ReuseSaveKey(fingerprint)");
        StringAssert.Contains(vm, "ReuseUploadKey(fingerprint)");
        StringAssert.Contains(vm, "SHA256.HashData(webpBytes)");
        StringAssert.Contains(vm, "_saveFingerprint");
        StringAssert.Contains(vm, "_uploadFingerprint");
        StringAssert.Contains(vm, "customer-ordering-home-content-update");
        StringAssert.Contains(vm, "customer-ordering-home-banner-upload");
        StringAssert.Contains(vm, "core.config.read");
        StringAssert.Contains(vm, "core.organization.write");
        StringAssert.Contains(vm, "system:security-owner");
        StringAssert.Contains(vm, "program.Length > 4000");
        StringAssert.Contains(vm, "title.Length is < 1 or > 80");
    }

    [TestMethod]
    public void View_PreservesCurrentWebFieldsAndUsesExistingWebpProcessor()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Settings", "CustomerOrderingContentView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Settings", "CustomerOrderingContentView.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        foreach (var label in new[]
        {
            "Nội dung chương trình",
            "Tiêu đề mục",
            "Ảnh banner",
            "Hiển thị",
            "Lưu thiết lập",
            "JPG, PNG hoặc WebP"
        })
            StringAssert.Contains(view, label);

        StringAssert.Contains(view, "MaxLength=\"80\"");
        StringAssert.Contains(view, "MaxLength=\"4000\"");
        StringAssert.Contains(code, "ProductImageProcessor.ToWebpAsync");
        StringAssert.Contains(shell, "Nội dung đặt hàng");
        StringAssert.Contains(shell, "Click=\"CustomerOrderingContent_OnClick\"");
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
