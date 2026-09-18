using CongTy.Desktop.Accounting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class CustomerReturnCreditParityTests
{
    [TestMethod]
    public void Presentation_UsesCurrentWebOfficeLanguage()
    {
        Assert.AreEqual("Chưa sử dụng", CustomerReturnCreditPresentation.Status("open"));
        Assert.AreEqual("Đã dùng một phần", CustomerReturnCreditPresentation.Status("partially_allocated"));
        Assert.AreEqual("Đã dùng hết", CustomerReturnCreditPresentation.Status("settled"));
        Assert.AreEqual("Đã đảo", CustomerReturnCreditPresentation.Status("reversed"));
        Assert.AreEqual("Chuyển khoản", CustomerReturnCreditPresentation.RefundMethod("BANK_TRANSFER"));
        Assert.AreEqual("Tiền mặt", CustomerReturnCreditPresentation.RefundMethod("CASH"));
    }

    [TestMethod]
    public void Service_UsesCanonicalReturnCreditRoutesAndIdempotentMutations()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "CustomerReturnCreditService.cs");
        foreach (var endpoint in new[]
        {
            "\"/api/customer-return-credits?limit=1000\"",
            "\"/api/customer-payments/allocation-targets\"",
            "$\"/api/customer-return-credits/{Uri.EscapeDataString(RequireId(creditId, \"Khoản giảm công nợ\"))}/allocations\"",
            "\"/api/customer-refunds\"",
            "$\"/api/customer-refunds/{Uri.EscapeDataString(RequireId(refundId, \"Phiếu hoàn\"))}/reverse\"",
            "$\"/api/customer-return-credits/{Uri.EscapeDataString(RequireId(creditId, \"Khoản giảm công nợ\"))}/reverse\""
        }) StringAssert.Contains(source, endpoint);
        StringAssert.Contains(source, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(source, "idempotencyKeys.IsValid(idempotencyKey)");
        StringAssert.Contains(source, "PostIdempotentDataAsync");
        Assert.IsFalse(source.Contains("PostDataAsync<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesExactPermissionsAndReusesCanonicalKeysForRetry()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CustomerReturnCreditViewModel.cs");
        foreach (var permission in new[]
        {
            "core.customer-return-credit.read",
            "core.customer-return-credit.allocate",
            "core.customer-return-credit.reverse",
            "core.customer-refund.create",
            "core.customer-refund.reverse"
        }) StringAssert.Contains(source, $"\"{permission}\"");
        StringAssert.Contains(source, "_idempotencyKeys.Create(\"customer-return-credit\")");
        StringAssert.Contains(source, "_mutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(source, "_mutationKeys.Remove(slot)");
        StringAssert.Contains(source, "allocate:");
        StringAssert.Contains(source, "refund:");
        StringAssert.Contains(source, "refund-reverse:");
        StringAssert.Contains(source, "credit-reverse:");
    }

    [TestMethod]
    public void View_PreservesWebLayoutFieldsActionsAndOfficeLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CustomerReturnCreditView.xaml");
        foreach (var text in new[]
        {
            "Khoản giảm công nợ từ hàng khách trả", "Phiếu trả", "Khách hàng", "Kho", "Giá trị", "Còn dùng", "Trạng thái",
            "Chi tiết và xử lý", "Dòng hàng đã nhận", "Chứng từ nguồn", "SL nhận", "Giá trị giảm",
            "Phân bổ khoản giảm công nợ còn lại", "Ngày phân bổ", "Khoản nợ / Đơn bán", "Còn nợ", "Số phân bổ", "Phân bổ công nợ",
            "Hoàn tiền từ phần chưa sử dụng", "Số tiền", "Ngày hoàn", "Phương thức", "Nơi nhận / tài khoản nhận", "Tham chiếu giao dịch",
            "Lý do hoàn tiền", "Ghi nhận hoàn tiền", "Lịch sử hoàn tiền", "Đảo hoàn tiền", "Lý do đảo", "Đảo khoản giảm công nợ hàng trả"
        }) StringAssert.Contains(view, text);
        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("API", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Shell_WiresUi75AfterCustomerPaymentsWithoutShiftingExistingWorkspaces()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.CustomerReturnCredits.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.CustomerReturnCredits.cs");
        var hook = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        StringAssert.Contains(shell, "\"accounting.customer-return-credits\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 41");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 41");
        StringAssert.Contains(host, "workspaceTabs.Items[41]");
        StringAssert.Contains(host, "ResolveRequired<ICanonicalIdempotencyKeyProvider>()");
        StringAssert.Contains(host, "\"Điều chỉnh công nợ hàng trả\"");
        StringAssert.Contains(host, "\"KẾ TOÁN BÁN HÀNG\"");
        StringAssert.Contains(hook, "WireCustomerReturnCreditsWorkspace();");
        StringAssert.Contains(xaml, "Click=\"CustomerReturnCredits_OnClick\"");
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
