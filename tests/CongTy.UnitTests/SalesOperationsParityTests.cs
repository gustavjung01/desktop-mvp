using CongTy.Contracts;
using CongTy.Desktop.Sales;

namespace CongTy.UnitTests;

[TestClass]
public sealed class SalesOperationsParityTests
{
    [TestMethod]
    public void Service_UsesReadOnlyManagementQueueEndpoints()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "SalesOrderService.cs");
        StringAssert.Contains(service, "/api/sales-orders?status=draft&limit=");
        StringAssert.Contains(service, "/api/customer-onboarding-requests?status=");
        StringAssert.Contains(service, "ListOperationsDraftsAsync");
        StringAssert.Contains(service, "ListOperationsCustomerOnboardingAsync");
        foreach (var status in new[] { "submitted", "under_review", "need_more_info" })
            StringAssert.Contains(service, status);
    }

    [TestMethod]
    public void ViewModel_UsesDenyByDefaultPermissionsAndPartialFailureStates()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesOperationsViewModel.cs");
        foreach (var permission in new[]
        {
            "core.branch.read",
            "core.warehouse.read",
            "core.warehouse.location.read",
            "core.sales-order.read",
            "core.customer-onboarding.read"
        })
        {
            StringAssert.Contains(viewModel, permission);
        }

        StringAssert.Contains(viewModel, "Task.WhenAll");
        StringAssert.Contains(viewModel, "Một phần danh sách đề nghị mở mã khách hàng chưa tải được");
        StringAssert.Contains(viewModel, "Chưa tải được số liệu");
        StringAssert.Contains(viewModel, "EnsureLoadedAsync() => RefreshAsync()");
    }

    [TestMethod]
    public void Presentation_MatchesWebOfficeLanguage()
    {
        Assert.AreEqual("Nhân viên thị trường", SalesOperationsPresentation.SalesOrderSourceLabel("MCP", "x"));
        Assert.AreEqual("Khách hàng", SalesOperationsPresentation.SalesOrderSourceLabel("API", "CUSTOMER_PORTAL:123"));
        Assert.AreEqual("Công Ty", SalesOperationsPresentation.SalesOrderSourceLabel("MANUAL", null));
        Assert.AreEqual("Mới gửi", SalesOperationsPresentation.OnboardingStatusLabel("submitted"));
        Assert.AreEqual("Đang xem xét", SalesOperationsPresentation.OnboardingStatusLabel("under_review"));
        Assert.AreEqual("Cần bổ sung", SalesOperationsPresentation.OnboardingStatusLabel("need_more_info"));

        var address = new CustomerOnboardingAddressData
        {
            AddressLine1 = "12 Nguyễn Huệ",
            Ward = "Bến Nghé",
            Province = "TP.HCM"
        };
        Assert.AreEqual("12 Nguyễn Huệ, Bến Nghé, TP.HCM", SalesOperationsPresentation.Address(address));
    }

    [TestMethod]
    public void View_PreservesManagementOverviewLayoutAndStates()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesOperationsView.xaml");
        foreach (var text in new[]
        {
            "trung tâm điều hành bán hàng chung",
            "Chi nhánh đang hoạt động",
            "Kho đang hoạt động",
            "Vị trí kho đang hoạt động",
            "Việc bán hàng đang chờ",
            "Đơn chờ xác nhận",
            "Mở màn xác nhận",
            "Đề nghị mở hoặc liên kết mã khách",
            "Mở màn xử lý",
            "F5 · Cập nhật dữ liệu"
        })
        {
            StringAssert.Contains(view, text);
        }

        Assert.IsFalse(view.Contains("canonical", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Phase", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_WiresUi53WithoutImplementingLaterWorkspaces()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "SalesOperationsViewModel");
        StringAssert.Contains(shell, "sales.operations");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 35");
        StringAssert.Contains(shell, "CanViewSalesOperations");
        StringAssert.Contains(shell, "NavigateSalesOperationsAsync");
        StringAssert.Contains(xaml, "IsSalesOperationsSelected");
        StringAssert.Contains(xaml, "SalesOperations_OnClick");
        StringAssert.Contains(xaml, "SalesOperationsHost");
        StringAssert.Contains(xaml, "Gửi Đề xuất");
        StringAssert.Contains(xaml, "Xem đơn bán hàng");
        StringAssert.Contains(code, "new SalesOperationsView(viewModel.SalesOperations)");
        StringAssert.Contains(code, "SalesOperationsHost.Content = _salesOperationsView");
        StringAssert.Contains(code, "SalesOperationsView_OnSalesOrdersRequested");
        StringAssert.Contains(shell, "sales.proposals");
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
