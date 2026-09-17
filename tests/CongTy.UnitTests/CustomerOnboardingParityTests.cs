using CongTy.Desktop.Sales;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class CustomerOnboardingParityTests
{
    [TestMethod]
    public void Presentation_UsesCurrentWebOfficeLanguage()
    {
        Assert.AreEqual("Mới gửi", CustomerOnboardingPresentation.StatusLabel("submitted"));
        Assert.AreEqual("Đang xem xét", CustomerOnboardingPresentation.StatusLabel("under_review"));
        Assert.AreEqual("Chờ bổ sung", CustomerOnboardingPresentation.StatusLabel("need_more_info"));
        Assert.AreEqual("Đã tạo khách mới", CustomerOnboardingPresentation.StatusLabel("approved"));
        Assert.AreEqual("Đã liên kết khách có sẵn", CustomerOnboardingPresentation.StatusLabel("linked_existing"));

        var portal = new CustomerOnboardingRequestData { SourceSystem = "CUSTOMER_PORTAL" };
        Assert.AreEqual("Ordering · Khách trực tiếp", CustomerOnboardingPresentation.SourceLabel(portal));
        Assert.AreEqual("Đăng ký tài khoản đặt hàng", CustomerOnboardingPresentation.ReasonLabel(portal));

        var field = new CustomerOnboardingRequestData { SourceSystem = "MCP", SourceDemandReference = "FIELD_PROFILE_VERIFICATION" };
        Assert.AreEqual("MCP Field", CustomerOnboardingPresentation.SourceLabel(field));
        Assert.AreEqual("Đề nghị mở / liên kết mã khách hàng", CustomerOnboardingPresentation.ReasonLabel(field));
    }

    [TestMethod]
    public void View_PreservesWebFieldsAndActions()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Sales", "CustomerOnboardingView.xaml");
        foreach (var text in new[]
        {
            "Mở/liên kết mã khách", "Địa chỉ", "Nguồn", "Người đưa về", "Điểm bán", "Lý do gửi", "Cập nhật",
            "Bắt đầu xem xét", "Kích hoạt quyền đặt hàng", "Kho mặc định", "Kênh bán",
            "Tạo khách mới từ đăng ký", "Mã khách hàng", "Tạo khách mới &amp; kích hoạt",
            "Liên kết khách đã có", "Khách hàng", "Yêu cầu bổ sung", "Từ chối"
        }) StringAssert.Contains(view, text);
        StringAssert.Contains(view, "Hiện không có đề nghị nào đang chờ xử lý.");
        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("API", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Service_UsesExistingBackendContractWithoutBackendChanges()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "CustomerOnboardingService.cs");
        StringAssert.Contains(source, "/api/customer-onboarding-requests?status={status}&limit={limit}&offset={offset}");
        StringAssert.Contains(source, "/api/customer-onboarding-portal-options");
        StringAssert.Contains(source, "review");
        StringAssert.Contains(source, "need-more-info");
        StringAssert.Contains(source, "approve");
        StringAssert.Contains(source, "link-existing");
        StringAssert.Contains(source, "reject");
        StringAssert.Contains(source, "PostIdempotentDataAsync");
        Assert.IsFalse(source.Contains("DateTime.Now", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_EnforcesPermissionsStableIdempotencyAndStaleAddressProtection()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Sales", "CustomerOnboardingViewModel.cs");
        foreach (var permission in new[]
        {
            "core.customer-onboarding.read", "core.customer-onboarding.review", "core.customer-onboarding.approve",
            "core.customer-onboarding.link-existing", "core.customer-onboarding.reject"
        }) StringAssert.Contains(source, permission);
        StringAssert.Contains(source, "_operationKeys");
        StringAssert.Contains(source, "{row.Id}:{action}:{row.Request.Version}");
        StringAssert.Contains(source, "_idempotencyKeys.Create(\"customer-onboarding-action\")");
        StringAssert.Contains(source, "BeginAddressRequest()");
        StringAssert.Contains(source, "IsCurrentAddressRequest(version)");
        StringAssert.Contains(source, "row.IsPortal");
        StringAssert.Contains(source, "Cần chọn kho mặc định và kênh bán");
    }

    [TestMethod]
    public void Shell_WiresUi57WithoutRewritingMainShellXaml()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.CustomerOnboarding.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.CustomerOnboarding.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        StringAssert.Contains(shell, "sales.customer-onboarding");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 38");
        StringAssert.Contains(shell, "NavigateCustomerOnboardingAsync");
        StringAssert.Contains(shell, "IsCustomerOnboardingSelected");
        StringAssert.Contains(host, "text.Text == \"Mở/liên kết mã khách\"");
        StringAssert.Contains(host, "sidebarButton.IsEnabled = true");
        StringAssert.Contains(host, "ResolveRequired<CustomerOnboardingView>()");
        StringAssert.Contains(host, "workspaceTabs.Items[38]");
        StringAssert.Contains(xaml, "IsEnabled=\"False\"><TextBlock Text=\"Mở/liên kết mã khách\" /></Button>");
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
