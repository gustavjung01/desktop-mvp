using CongTy.Desktop.Logistics;

namespace CongTy.UnitTests;

[TestClass]
public sealed class CustomerReturnParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeStatusesReasonsAndExactQuantity()
    {
        Assert.AreEqual("Nháp chờ nhận", CustomerReturnPresentation.Status("draft"));
        Assert.AreEqual("Đã nhận vào kho", CustomerReturnPresentation.Status("received"));
        Assert.AreEqual("Đã hủy", CustomerReturnPresentation.Status("cancelled"));
        Assert.AreEqual("Hư hỏng / không nhận", CustomerReturnPresentation.Reason("DAMAGED_OR_UNWANTED"));
        Assert.AreEqual("Sai hàng", CustomerReturnPresentation.Reason("WRONG_ITEM"));
        Assert.AreEqual("Khiếu nại chất lượng", CustomerReturnPresentation.Reason("QUALITY_COMPLAINT"));
        Assert.AreEqual("12.34", CustomerReturnPresentation.Quantity("12.340000000000"));
    }

    [TestMethod]
    public void Service_UsesDirectBackendCustomerReturnRoutesAndIdempotentWrites()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "CustomerReturnService.cs");

        StringAssert.Contains(service, "BasePath = \"/api/delivery-orders/customer-returns\"");
        StringAssert.Contains(service, "/eligibility?limit=1000");
        StringAssert.Contains(service, "?limit=500");
        StringAssert.Contains(service, "/receive");
        StringAssert.Contains(service, "/cancel");
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid");
        Assert.IsFalse(service.Contains("/api/customer-returns", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_ReusesCanonicalKeysAndPreservesRevisionOnReceive()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "CustomerReturnViewModel.cs");

        StringAssert.Contains(vm, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(vm, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(vm, "_idempotencyKeys.Create($\"customer-return-{prefix}\")");
        StringAssert.Contains(vm, "_intentKeys.Remove(intent)");
        StringAssert.Contains(vm, "detail.Revision");
        StringAssert.Contains(vm, "new CustomerReturnReceiveRequest(documentDate, detail.Revision");
        StringAssert.Contains(vm, "OrderBy(row => row.CustomerReturnLineId, StringComparer.Ordinal)");
        StringAssert.Contains(vm, "CUSTOMER_RETURN_RECEIVABLE_NOT_POSTED");
        Assert.IsFalse(vm.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesWebStatsTabsQueuesAndActions()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "CustomerReturnView.xaml");

        foreach (var text in new[]
        {
            "NGUỒN TRẢ BẤT BIẾN",
            "Nhận hàng khách trả có đối chiếu",
            "Yêu cầu trả hoặc giao thất bại không tự hoàn kho. Kho phải xác nhận số lượng thực nhận.",
            "Dòng còn có thể trả",
            "Phiếu nháp",
            "Đã nhận kho",
            "Đã hủy",
            "Header=\"Lập phiếu trả\"",
            "Header=\"Nhận &amp; xử lý\"",
            "Dòng hàng đã xuất",
            "Số lượng khách trả",
            "Mã lý do",
            "Lý do chi tiết",
            "Ghi chú phiếu",
            "Phiếu hàng khách trả",
            "Phiếu nháp chưa làm tăng tồn kho.",
            "Thực nhận",
            "Lý do hủy nháp",
            "Mã nhập kho",
            "Lý do hủy",
            "Mở Đối soát cuối chuyến"
        }) StringAssert.Contains(view, text);

        var create = view.IndexOf("Header=\"Lập phiếu trả\"", StringComparison.Ordinal);
        var process = view.IndexOf("Header=\"Nhận &amp; xử lý\"", StringComparison.Ordinal);
        Assert.IsGreaterThan(create, process);
    }

    [TestMethod]
    public void MutationButtonLabels_AreBoundFromViewModelAndUseOfficeLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "CustomerReturnView.xaml");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "CustomerReturnViewModel.cs");

        StringAssert.Contains(view, "Content=\"{Binding CreateButtonText}\"");
        StringAssert.Contains(view, "Content=\"{Binding ReceiveButtonText}\"");
        StringAssert.Contains(view, "Content=\"{Binding CancelButtonText}\"");
        StringAssert.Contains(vm, "\"Tạo phiếu trả nháp\"");
        StringAssert.Contains(vm, "\"Xác nhận thực nhận và nhập kho\"");
        StringAssert.Contains(vm, "\"Hủy phiếu nháp\"");

        Assert.IsFalse(view.Contains("Delivery Order", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Inventory IN", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("movement line", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_ActivatesSeventhLogisticsScreenAndKeepsStackedIndexes()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "\"logistics.customer-returns\" => \"Hàng khách trả\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 30");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 29");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 28");
        StringAssert.Contains(shell, "CanViewCustomerReturns");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsCustomerReturnsSelected}\"");
        StringAssert.Contains(xaml, "Click=\"CustomerReturns_OnClick\"");
        StringAssert.Contains(xaml, "Content=\"Bàn giao giao nhận\"");
        StringAssert.Contains(xaml, "x:Name=\"CustomerReturnHost\"");
        StringAssert.Contains(code, "CustomerReturnHost.Content = customerReturnView");
        StringAssert.Contains(code, "CustomerReturnView_OnTripReconciliationRequested");
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
