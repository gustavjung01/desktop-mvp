using CongTy.ApiClient;
using CongTy.Desktop.Sales;

namespace CongTy.UnitTests;

[TestClass]
public sealed class ManagementProposalParityTests
{
    [TestMethod]
    public void Service_UsesCompanyProposalEndpointsAndCanonicalIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "ManagementProposalService.cs");
        StringAssert.Contains(service, "/api/management-proposals?source=company");
        StringAssert.Contains(service, "/api/management-proposals/{Uri.EscapeDataString(id)}/resubmit");
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "ICanonicalIdempotencyKeyProvider");
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_ReusesCanonicalKeyForSameIntent()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Sales", "ManagementProposalViewModel.cs");
        StringAssert.Contains(viewModel, "_keys.Create(\"company-management-proposal\")");
        StringAssert.Contains(viewModel, "_keys.Create(\"company-management-proposal-resubmit\")");
        StringAssert.Contains(viewModel, "_createIntentSignature");
        StringAssert.Contains(viewModel, "_resubmitIntents");
        StringAssert.Contains(viewModel, "Nội dung vừa nhập vẫn được giữ");
        Assert.IsFalse(viewModel.Contains("Guid.NewGuid", StringComparison.Ordinal));
        Assert.IsFalse(viewModel.Contains("Idempotency-Key", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Presentation_MatchesWebLabels()
    {
        Assert.AreEqual("Chờ quyết định", ManagementProposalPresentation.StatusLabel("pending"));
        Assert.AreEqual("Chờ bổ sung", ManagementProposalPresentation.StatusLabel("needs-info"));
        Assert.AreEqual("Đã đồng ý", ManagementProposalPresentation.StatusLabel("approved"));
        Assert.AreEqual("Đã từ chối", ManagementProposalPresentation.StatusLabel("rejected"));
        Assert.AreEqual("Thương mại", ManagementProposalPresentation.DomainLabel("commercial"));
        Assert.AreEqual("Khách hàng & công nợ", ManagementProposalPresentation.DomainLabel("customer-debt"));
        Assert.AreEqual("Vận hành", ManagementProposalPresentation.DomainLabel("operations"));
    }

    [TestMethod]
    public void View_PreservesProposalFormHistoryAndResubmitFlow()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Sales", "ManagementProposalView.xaml");
        foreach (var text in new[]
        {
            "PHIẾU ĐỀ XUẤT",
            "Nội dung cần Admin quyết định",
            "Nguồn: Công Ty",
            "Tiêu đề",
            "Nội dung đề xuất",
            "Thêm thông tin liên quan (không bắt buộc)",
            "PHẢN HỒI TỪ ADMIN",
            "Đề xuất của tôi",
            "Phản hồi Admin:",
            "Nội dung bổ sung"
        })
        {
            StringAssert.Contains(view, text);
        }

        StringAssert.Contains(view, "Content=\"{Binding SubmitButtonText}\"");
        StringAssert.Contains(view, "Content=\"{Binding ResubmitButtonText}\"");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Sales", "ManagementProposalViewModel.cs");
        StringAssert.Contains(viewModel, "Gửi Đề xuất");
        StringAssert.Contains(viewModel, "Đang gửi…");
        StringAssert.Contains(viewModel, "Gửi bổ sung");

        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("canonical", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Phase", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_WiresUi54AfterSalesOperations()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "sales.proposals");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 36");
        StringAssert.Contains(shell, "CanViewManagementProposals");
        StringAssert.Contains(shell, "NavigateManagementProposalsAsync");
        StringAssert.Contains(xaml, "IsManagementProposalsSelected");
        StringAssert.Contains(xaml, "ManagementProposals_OnClick");
        StringAssert.Contains(xaml, "ManagementProposalHost");
        StringAssert.Contains(xaml, "Quay lại Điều hành bán hàng");
        StringAssert.Contains(code, "ManagementProposalHost.Content = managementProposalView");
        StringAssert.Contains(code, "ManagementProposalsBack_OnClick");
    }

    [TestMethod]
    public void CanonicalProvider_ProducesAllowedKeys()
    {
        var provider = new CanonicalIdempotencyKeyProvider();
        var key = provider.Create("company-management-proposal");
        Assert.IsTrue(provider.IsValid(key));
        Assert.IsTrue(key.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '.' or '_' or '-'));
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
