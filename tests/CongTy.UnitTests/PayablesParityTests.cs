using CongTy.Desktop.Accounting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class PayablesParityTests
{
    [TestMethod]
    public void Presentation_UsesCurrentWebOfficeLanguage()
    {
        Assert.AreEqual("Còn mở", PayablesPresentation.Status("open"));
        Assert.AreEqual("Đã phân bổ một phần", PayablesPresentation.Status("partially_allocated"));
        Assert.AreEqual("Đã tất toán", PayablesPresentation.Status("settled"));
        Assert.AreEqual("Đã đảo", PayablesPresentation.Status("reversed"));
        Assert.AreEqual("Phiếu nhận hàng", PayablesPresentation.Source("GOODS_RECEIPT"));
        Assert.AreEqual("Phiếu trả nhà cung cấp", PayablesPresentation.Source("SUPPLIER_RETURN"));
    }

    [TestMethod]
    public void Service_UsesCanonicalReadOnlyPayableRoutes()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "PayablesService.cs");
        StringAssert.Contains(source, "\"/api/payables?limit=1000\"");
        StringAssert.Contains(source, "\"/api/payables/balances?limit=1000\"");
        StringAssert.Contains(source, "$\"/api/payables/{Uri.EscapeDataString(RequireDocumentId(id))}\"");
        Assert.IsFalse(source.Contains("Post", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Idempotency", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesExactReadPermissionAndWebSummarySemantics()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "PayablesViewModel.cs");
        StringAssert.Contains(source, "\"core.payable.read\"");
        StringAssert.Contains(source, "TotalBalanceText");
        StringAssert.Contains(source, "TotalOverdueText");
        StringAssert.Contains(source, "OpenDocumentCountText");
        StringAssert.Contains(source, "x.Status is \"open\" or \"partially_allocated\"");
    }

    [TestMethod]
    public void View_PreservesCurrentWebSectionsFieldsAndOfficeLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "PayablesView.xaml");
        foreach (var text in new[]
        {
            "Số dư phải trả (VND)", "Đã quá hạn (VND)", "Chứng từ còn mở",
            "Số dư theo nhà cung cấp", "Nhà cung cấp", "Tiền tệ", "Số dư", "Còn mở", "Quá hạn",
            "Chứng từ công nợ", "Chứng từ nguồn", "Kho", "Hạn thanh toán", "Điều khoản", "Trạng thái", "Giá trị",
            "Chi tiết chứng từ công nợ phải trả", "Giá trị gốc", "Còn lại", "Dòng chứng từ", "SKU", "Hàng hóa",
            "Số lượng", "Đơn giá", "Thành tiền", "Sổ chi tiết", "Thời điểm", "Loại bút toán", "Yêu cầu", "Số tiền"
        }) StringAssert.Contains(view, text);
        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("API", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Shell_WiresUi76AfterReturnCreditsWithoutShiftingExistingWorkspaces()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.Payables.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.Payables.cs");
        var hook = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        StringAssert.Contains(shell, "\"accounting.payables\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 42");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 42");
        StringAssert.Contains(host, "workspaceTabs.Items[42]");
        StringAssert.Contains(host, "\"KẾ TOÁN MUA HÀNG\"");
        StringAssert.Contains(hook, "WireCustomerReturnCreditsWorkspace();");
        StringAssert.Contains(hook, "WirePayablesWorkspace();");
        StringAssert.Contains(xaml, "IsEnabled=\"False\"><TextBlock Text=\"Công nợ phải trả\" /></Button>");
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
