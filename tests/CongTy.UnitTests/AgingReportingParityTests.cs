using CongTy.Desktop.Accounting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class AgingReportingParityTests
{
    [TestMethod]
    public void Presentation_UsesWebBucketsAndOfficeFormatting()
    {
        Assert.AreEqual("0–30 ngày", AgingReportingPresentation.ReceivableBucket("AGE_0_30"));
        Assert.AreEqual("31–60 ngày", AgingReportingPresentation.ReceivableBucket("AGE_31_60"));
        Assert.AreEqual("61–90 ngày", AgingReportingPresentation.ReceivableBucket("AGE_61_90"));
        Assert.AreEqual("Trên 90 ngày", AgingReportingPresentation.ReceivableBucket("AGE_91_PLUS"));

        Assert.AreEqual("Chưa đến hạn", AgingReportingPresentation.PayableBucket("NOT_DUE"));
        Assert.AreEqual("Quá hạn 1–30 ngày", AgingReportingPresentation.PayableBucket("OVERDUE_1_30"));
        Assert.AreEqual("Quá hạn 31–60 ngày", AgingReportingPresentation.PayableBucket("OVERDUE_31_60"));
        Assert.AreEqual("Quá hạn 61–90 ngày", AgingReportingPresentation.PayableBucket("OVERDUE_61_90"));
        Assert.AreEqual("Quá hạn trên 90 ngày", AgingReportingPresentation.PayableBucket("OVERDUE_91_PLUS"));

        Assert.AreEqual("1.234.568", AgingReportingPresentation.Number("1234567.5", 0));
        Assert.AreEqual("12,345679 USD", AgingReportingPresentation.Money("12.3456789", "USD"));
        Assert.AreEqual("1.234.568 VND", AgingReportingPresentation.Money("1234567.5", "VND"));
        Assert.AreEqual("16/09/2026", AgingReportingPresentation.Date("2026-09-16"));
    }

    [TestMethod]
    public void Service_UsesOnlyCanonicalAgingReadEndpointAndWarehouseFilter()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "AgingReportingService.cs");

        StringAssert.Contains(service, "\"/api/reporting/aging\"");
        StringAssert.Contains(service, "warehouseId=");
        StringAssert.Contains(service, "GetDataAsync<AgingDashboardData>");
        Assert.IsFalse(service.Contains("from=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(service.Contains("to=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(service.Contains("PostIdempotentDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("PutIdempotentDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("ICanonicalIdempotencyKeyProvider", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesDedicatedPermissionAndWebFilterSemantics()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "AgingReportingViewModel.cs");

        StringAssert.Contains(vm, "core.reporting.aging.read");
        StringAssert.Contains(vm, "Tất cả kho được cấp quyền");
        StringAssert.Contains(vm, "LoadAsync(SelectedWarehouseId)");
        StringAssert.Contains(vm, "LoadAsync(string.Empty)");
        StringAssert.Contains(vm, "Phải thu được phân tuổi từ ngày chứng từ");
        StringAssert.Contains(vm, "phải trả dùng ngày đến hạn trên chứng từ");
        var invalidation = vm.IndexOf("_loadGeneration++;", StringComparison.Ordinal);
        var releaseBusy = vm.IndexOf("IsBusy = false;", invalidation, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, invalidation);
        Assert.IsGreaterThan(invalidation, releaseBusy);
        Assert.IsFalse(vm.Contains("FromDate", StringComparison.Ordinal));
        Assert.IsFalse(vm.Contains("ToDate", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesFilterNoticeTabsSectionsAndEmptyStates()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "AgingReportingView.xaml");

        var receivableTab = view.IndexOf("Header=\"Phải thu\"", StringComparison.Ordinal);
        var payableTab = view.IndexOf("Header=\"Phải trả\"", StringComparison.Ordinal);
        Assert.IsGreaterThan(receivableTab, payableTab);

        foreach (var text in new[]
        {
            "Kho",
            "Đặt lại",
            "Đang tải tuổi nợ…",
            "Phải thu khách hàng",
            "Tuổi khoản phải thu tính từ ngày chứng từ nguồn trên số dư hiện còn phải thu.",
            "Mở công nợ phải thu",
            "Tiền tệ",
            "Tuổi khoản phải thu",
            "Chứng từ",
            "Còn phải thu",
            "Khách hàng còn công nợ",
            "Chứng từ cũ nhất",
            "Tuổi lớn nhất",
            "Phải trả nhà cung cấp",
            "Quá hạn tính từ ngày đến hạn đã ghi trên chứng từ phải trả.",
            "Mở công nợ phải trả",
            "Trạng thái hạn",
            "Còn phải trả",
            "Nhà cung cấp còn công nợ",
            "Hạn sớm nhất",
            "Quá hạn lớn nhất"
        }) StringAssert.Contains(view, text);

        StringAssert.Contains(view, "Click=\"Receivables_OnClick\"");
        StringAssert.Contains(view, "Click=\"Payables_OnClick\"");
        StringAssert.Contains(view, "DataContext.CanViewReceivables");
        StringAssert.Contains(view, "DataContext.CanViewPayables");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "AgingReportingViewModel.cs");
        StringAssert.Contains(vm, "Không có khoản phải thu đang mở.");
        StringAssert.Contains(vm, "Không có khoản phải trả đang mở.");
        StringAssert.Contains(vm, "Không có dữ liệu.");
    }

    [TestMethod]
    public void Shell_ActivatesAccountingAgingWithoutChangingExistingIndexes()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "\"accounting.aging\" => \"Tuổi nợ phải thu / phải trả\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 31");
        StringAssert.Contains(shell, "30 => _customerReturns.Message");
        StringAssert.Contains(shell, "CanViewAging");
        StringAssert.Contains(shell, "IsAccountingOpen = true");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsAgingSelected}\"");
        StringAssert.Contains(xaml, "Click=\"AccountingAging_OnClick\"");
        StringAssert.Contains(xaml, "x:Name=\"AgingReportingHost\"");
        StringAssert.Contains(xaml, "Content=\"Công nợ phải thu\"");
        StringAssert.Contains(xaml, "Content=\"Công nợ phải trả\"");
        StringAssert.Contains(code, "AgingReportingHost.Content = agingReportingView");
        StringAssert.Contains(code, "AccountingAging_OnClick");
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
