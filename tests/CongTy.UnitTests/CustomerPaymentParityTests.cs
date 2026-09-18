using CongTy.Desktop.Accounting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class CustomerPaymentParityTests
{
    [TestMethod]
    public void Presentation_UsesCurrentWebOfficeLanguage()
    {
        Assert.AreEqual("Chưa gắn với đơn", CustomerPaymentPresentation.Status("open"));
        Assert.AreEqual("Đã ghi một phần", CustomerPaymentPresentation.Status("partially_allocated"));
        Assert.AreEqual("Đã ghi nhận", CustomerPaymentPresentation.Status("settled"));
        Assert.AreEqual("Đã hủy", CustomerPaymentPresentation.Status("reversed"));
        Assert.AreEqual("Chuyển khoản", CustomerPaymentPresentation.PaymentMethod("BANK_TRANSFER"));
        Assert.AreEqual("Tiền mặt", CustomerPaymentPresentation.PaymentMethod("CASH"));
        Assert.AreEqual("Khác", CustomerPaymentPresentation.PaymentMethod("OTHER"));
    }

    [TestMethod]
    public void Service_UsesCanonicalCustomerPaymentRoutesAndIdempotentMutations()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "CustomerPaymentService.cs");

        foreach (var endpoint in new[]
        {
            "\"/api/customer-payments?limit=1000\"",
            "\"/api/customer-payments/allocation-targets\"",
            "\"/api/customer-payments/remitting-employees\"",
            "$\"/api/customer-payments/{Uri.EscapeDataString(RequireId(paymentId, \"Phiếu thu\"))}/allocations\"",
            "$\"/api/customer-payments/{Uri.EscapeDataString(RequireId(paymentId, \"Phiếu thu\"))}/reverse\"",
            "$\"/api/receivable-allocations/{Uri.EscapeDataString(RequireId(allocationId, \"Khoản ghi nhận\"))}/reverse\""
        }) StringAssert.Contains(source, endpoint);

        StringAssert.Contains(source, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(source, "idempotencyKeys.IsValid(idempotencyKey)");
        StringAssert.Contains(source, "PostIdempotentDataAsync");
        Assert.IsFalse(source.Contains("PostDataAsync<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesExactPermissionsAndReusesCanonicalKeysForRetry()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CustomerPaymentViewModel.cs");

        foreach (var permission in new[]
        {
            "core.customer-payment.read",
            "core.customer-payment.create",
            "core.customer-payment.reverse",
            "core.receivable.read",
            "core.receivable-allocation.create",
            "core.receivable-allocation.reverse"
        }) StringAssert.Contains(source, $"\"{permission}\"");

        StringAssert.Contains(source, "_idempotencyKeys.Create(scope)");
        StringAssert.Contains(source, "_mutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(source, "_mutationKeys.Remove(slot)");
        StringAssert.Contains(source, "\"customer-payment-create\"");
        StringAssert.Contains(source, "\"customer-payment-allocation\"");
        StringAssert.Contains(source, "\"receivable-allocation-reverse\"");
        StringAssert.Contains(source, "\"customer-payment-reverse\"");
    }

    [TestMethod]
    public void View_PreservesWebFieldsActionsHistoryAndOfficeLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CustomerPaymentView.xaml");

        foreach (var text in new[]
        {
            "Lập phiếu thu khách hàng",
            "Tìm khách hàng",
            "Khách hàng",
            "Nhân viên nộp tiền (không bắt buộc)",
            "Đơn vị nhận tiền",
            "Ngày thu",
            "Số tiền thực nhận",
            "Hình thức nhận tiền",
            "Mã giao dịch ngân hàng",
            "Ghi chú",
            "Chọn đơn cần ghi nhận thanh toán",
            "Tìm đơn hàng",
            "Còn phải thu",
            "Thu lần này",
            "Lưu phiếu thu",
            "Lịch sử thu tiền",
            "Số phiếu",
            "Đơn hàng",
            "Số tiền thu",
            "Kết quả",
            "Chi tiết phiếu thu",
            "Ghi tiền vào đơn",
            "Ngày ghi nhận",
            "Ghi các khoản đã nhập",
            "Lý do hủy",
            "Hủy phiếu thu",
            "Lịch sử ghi tiền vào đơn",
            "Hủy ghi nhận",
            "In"
        }) StringAssert.Contains(view, text);

        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("API", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Print_PreservesA5ReceiptSemantics()
    {
        var action = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CustomerPaymentView.xaml.cs");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CustomerPaymentPrintPreview.cs");
        var runtime = ReadRepoFile("src", "CongTy.Desktop", "Printing", "DocumentPrintTemplateRuntime.cs");

        StringAssert.Contains(action, "\"CUSTOMER_PAYMENT\"");
        StringAssert.Contains(code, "PHIẾU THU");
        StringAssert.Contains(code, "Chứng từ thu tiền khách hàng");
        StringAssert.Contains(runtime, "PageMediaSizeName.ISOA5");
        StringAssert.Contains(runtime, "documentType, \"CUSTOMER_PAYMENT\"");
        StringAssert.Contains(code, "SỐ TIỀN ĐÃ NHẬN");
        StringAssert.Contains(code, "Người nộp tiền");
        StringAssert.Contains(code, "Người lập phiếu");
        StringAssert.Contains(code, "Thủ quỹ / Kế toán");
    }

    [TestMethod]
    public void Shell_WiresUi74AfterReceivablesWithoutShiftingExistingWorkspaces()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.CustomerPayments.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.CustomerPayments.cs");
        var hook = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "\"accounting.customer-payments\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 40");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 40");
        StringAssert.Contains(host, "workspaceTabs.Items[40]");
        StringAssert.Contains(host, "ResolveRequired<ICanonicalIdempotencyKeyProvider>()");
        StringAssert.Contains(host, "\"Thu tiền khách hàng\"");
        StringAssert.Contains(host, "\"KẾ TOÁN BÁN HÀNG\"");
        StringAssert.Contains(hook, "WireReceivablesWorkspace();");
        StringAssert.Contains(hook, "WireCustomerPaymentsWorkspace();");
        StringAssert.Contains(xaml, "IsEnabled=\"False\"><TextBlock Text=\"Thu tiền khách hàng\" /></Button>");
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
