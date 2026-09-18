using CongTy.Desktop.Accounting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class ReceivablesParityTests
{
    [TestMethod]
    public void Presentation_UsesCurrentWebOfficeLanguage()
    {
        Assert.AreEqual("Còn phải thu", ReceivablesPresentation.Status("open"));
        Assert.AreEqual("Đã thu một phần", ReceivablesPresentation.Status("partially_allocated"));
        Assert.AreEqual("Đã thu đủ", ReceivablesPresentation.Status("settled"));
        Assert.AreEqual("Đã hủy", ReceivablesPresentation.Status("reversed"));
        Assert.AreEqual("Nhận tại quầy", ReceivablesPresentation.Source("PICKUP_HANDOVER"));
        Assert.AreEqual("Giao hàng", ReceivablesPresentation.Source("DELIVERY_ATTEMPT"));
        Assert.AreEqual("Thanh toán trước", ReceivablesPresentation.CollectionPolicy("PREPAID"));
        Assert.AreEqual("Thu tiền khi giao", ReceivablesPresentation.CollectionPolicy("COLLECT_ON_DELIVERY"));
        Assert.AreEqual("Thu sau khi giao", ReceivablesPresentation.CollectionPolicy("COLLECT_AFTER_DELIVERY"));
        Assert.AreEqual("Bán chịu theo điều khoản", ReceivablesPresentation.CollectionPolicy("CREDIT_TERMS"));
        Assert.AreEqual("Phát sinh công nợ", ReceivablesPresentation.LedgerEntry("SALE_POST"));
        Assert.AreEqual("Ghi nhận thu tiền", ReceivablesPresentation.LedgerEntry("CUSTOMER_PAYMENT_POST"));
    }

    [TestMethod]
    public void Service_UsesOnlyCanonicalReceivableReadEndpoints()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "ReceivablesService.cs");

        StringAssert.Contains(source, "\"/api/receivables?limit=1000\"");
        StringAssert.Contains(source, "\"/api/receivables/balances?limit=1000\"");
        StringAssert.Contains(source, "$\"/api/receivables/{Uri.EscapeDataString(documentId)}\"");
        StringAssert.Contains(source, "GetDataAsync<ReceivableDocumentData[]>");
        StringAssert.Contains(source, "GetDataAsync<CustomerReceivableBalanceData[]>");
        Assert.IsFalse(source.Contains("PostDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("PostIdempotentDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("PutIdempotentDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("ICanonicalIdempotencyKeyProvider", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesDedicatedReadPermissionAndWebSummarySemantics()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "ReceivablesViewModel.cs");

        StringAssert.Contains(source, "\"core.receivable.read\"");
        StringAssert.Contains(source, "CurrencyCode, \"VND\"");
        StringAssert.Contains(source, "item.Status is \"open\" or \"partially_allocated\"");
        StringAssert.Contains(source, "Chưa có số dư công nợ khách hàng.");
        StringAssert.Contains(source, "Chưa phát sinh chứng từ công nợ khách hàng.");
        StringAssert.Contains(source, "Không tải được đầy đủ dữ liệu công nợ khách hàng. Hãy thử tải lại.");
        StringAssert.Contains(source, "Không tải được chi tiết chứng từ công nợ đã chọn.");
    }

    [TestMethod]
    public void View_PreservesWebSummaryTablesDetailAndOfficeLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "ReceivablesView.xaml");

        foreach (var text in new[]
        {
            "Số dư phải thu (VND)",
            "Còn mở (VND)",
            "Đơn còn phải thu",
            "Số dư theo khách hàng",
            "Khách hàng",
            "Tiền tệ",
            "Số dư",
            "Còn mở",
            "Số chứng từ",
            "Đơn hàng cần thu",
            "Nghiệp vụ",
            "Đơn bán / Phiếu giao",
            "Kho",
            "Trạng thái",
            "Tổng tiền",
            "Còn phải thu",
            "Đóng chi tiết",
            "Giá trị phát sinh",
            "Chính sách thu tiền",
            "Hàng khách đã nhận",
            "Số lượng nhận",
            "Tiền hàng",
            "Giảm giá",
            "Thuế",
            "Sổ chi tiết",
            "Thời điểm",
            "Loại bút toán",
            "Nguồn",
            "Số tiền",
            "Đang tải công nợ khách hàng…"
        }) StringAssert.Contains(view, text);

        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("API", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Shell_WiresUi73WithoutRewritingMainShellXaml()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.Receivables.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.Receivables.cs");
        var shellHook = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "\"accounting.receivables\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 39");
        StringAssert.Contains(shell, "NavigateReceivablesAsync");
        StringAssert.Contains(shell, "IsReceivablesSelected");
        Assert.IsFalse(host.Contains("sidebarButton", StringComparison.Ordinal));
        StringAssert.Contains(host, "workspaceTabs.Items[39]");
        StringAssert.Contains(host, "ResolveRequired<CompanyApiClient>()");
        StringAssert.Contains(host, "ResolveRequired<IAuthenticatedSessionAccessor>()");
        StringAssert.Contains(host, "ResolveRequired<IAccessStateService>()");
        StringAssert.Contains(host, "\"Công nợ phải thu\"");
        StringAssert.Contains(host, "\"KẾ TOÁN & CÔNG NỢ\"");
        StringAssert.Contains(shellHook, "WireReceivablesWorkspace();");
        StringAssert.Contains(xaml, "Click=\"Receivables_OnClick\"");
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
