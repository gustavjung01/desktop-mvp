namespace CongTy.UnitTests;

[TestClass]
public sealed class PurchasingReportingParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalPurchasingReportingEndpoint()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "PurchasingReportingService.cs");

        StringAssert.Contains(service, "/api/reporting/purchasing");
        StringAssert.Contains(service, "from=");
        StringAssert.Contains(service, "to=");
        Assert.IsFalse(service.Contains("warehouseId=", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Post", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesCanonicalReadPermissionAndWebSections()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchasingReportingViewModel.cs");

        StringAssert.Contains(vm, "_access.HasPermission(\"core.reporting.purchasing.read\")");
        StringAssert.Contains(vm, "purchase_order");
        StringAssert.Contains(vm, "goods_receipt");
        StringAssert.Contains(vm, "CurrencyRows");
        StringAssert.Contains(vm, "TrendRows");
        StringAssert.Contains(vm, "SupplierRows");
        StringAssert.Contains(vm, "SkuRows");
        Assert.IsFalse(vm.Contains("idempotency", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Ui_PreservesWebOrderWithoutInventedTabsOrExport()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchasingReportingView.xaml");

        var expected = new[]
        {
            "Text=\"Kỳ báo cáo\"",
            "Text=\"Ngày nghiệp vụ\"",
            "Text=\"Tổng đơn mua trong kỳ\"",
            "Text=\"Đơn mua có hiệu lực\"",
            "Text=\"Đã hủy\"",
            "Text=\"Chờ duyệt\"",
            "Text=\"Phiếu nhận đã ghi sổ\"",
            "Text=\"Phiếu nhận đã đảo\"",
            "Text=\"Giá trị theo tiền tệ\"",
            "Text=\"Trạng thái đơn mua\"",
            "Text=\"Phiếu nhận hàng\"",
            "Text=\"Xu hướng theo ngày\"",
            "Text=\"Nhà cung cấp nổi bật\"",
            "Text=\"SKU nổi bật\""
        };

        var cursor = -1;
        foreach (var marker in expected)
        {
            var next = view.IndexOf(marker, cursor + 1, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự khối: {marker}");
            cursor = next;
        }

        foreach (var header in new[]
        {
            "Header=\"Tiền tệ\"",
            "Header=\"Chứng từ hiệu lực\"",
            "Header=\"Ngày\"",
            "Header=\"Số chứng từ\"",
            "Header=\"Nhà cung cấp\"",
            "Header=\"SKU\"",
            "Header=\"SL cơ sở\"",
            "Header=\"Nguồn\""
        })
        {
            StringAssert.Contains(view, header);
        }

        Assert.IsFalse(view.Contains("<TabControl", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Xuất Excel", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Xuất CSV", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("purchasing.purchase_orders", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("receipt_date", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Ui_UsesSharedWhiteFormAndCompactSummaryCards()
    {
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchasingReportingView.xaml");

        StringAssert.Contains(
            controls,
            "<Style x:Key=\"OfficeCardHeaderStyle\" TargetType=\"Border\" BasedOn=\"{StaticResource OfficeCardStyle}\">");
        StringAssert.Contains(
            controls,
            "<Style x:Key=\"OfficeSummaryCardStyle\" TargetType=\"Border\" BasedOn=\"{StaticResource OfficeCardStyle}\">");
        StringAssert.Contains(controls, "<Setter Property=\"Padding\" Value=\"10,6\" />");

        StringAssert.Contains(view, "<Border Grid.Row=\"0\" Style=\"{StaticResource OfficeCardStyle}\">");
        StringAssert.Contains(view, "<WrapPanel Grid.Row=\"2\" Margin=\"0,8,0,0\">");
        StringAssert.Contains(view, "Style=\"{StaticResource OfficeSummaryLabelStyle}\"");
        StringAssert.Contains(view, "Style=\"{StaticResource OfficeSummaryValueStyle}\"");
        StringAssert.Contains(view, "ToolTip=\"Theo ngày đặt hàng; gồm mọi trạng thái của đơn mua trong kỳ.\"");
        StringAssert.Contains(view, "ToolTip=\"{Binding EffectiveStatesText}\"");

        Assert.IsFalse(view.Contains("<UniformGrid Grid.Row=\"2\" Columns=\"3\"", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("ReportSummaryCaptionStyle", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("ReportSummaryValueStyle", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Shell_PreservesPurchasingReportingAndUsesDedicatedWorkspace()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "\"purchasing.reporting\" => \"Báo cáo mua hàng\"");
        StringAssert.Contains(shell, "public async Task NavigatePurchasingReportingAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 20");
        StringAssert.Contains(shell, "CanViewPurchasingReporting");

        StringAssert.Contains(main, "Tag=\"{Binding IsPurchasingReportingSelected}\"");
        StringAssert.Contains(main, "Click=\"PurchasingReporting_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"LogisticsReportingHost\"");
        StringAssert.Contains(main, "x:Name=\"PurchasingReportingHost\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Báo cáo mua hàng\"", StringComparison.Ordinal));
        StringAssert.Contains(main, "Tag=\"{Binding IsPurchaseOrdersSelected}\"");
        StringAssert.Contains(main, "Click=\"PurchaseOrders_OnClick\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Đơn mua hàng\"", StringComparison.Ordinal));
        StringAssert.Contains(main, "Tag=\"{Binding IsPurchasePricesSelected}\"");
        StringAssert.Contains(main, "Click=\"PurchasePrices_OnClick\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Bảng giá mua\"", StringComparison.Ordinal));
        StringAssert.Contains(main, "Tag=\"{Binding IsGoodsReceiptsSelected}\"");
        StringAssert.Contains(main, "Click=\"GoodsReceipts_OnClick\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Phiếu nhận hàng\"", StringComparison.Ordinal));
        StringAssert.Contains(main, "Tag=\"{Binding IsSupplierReturnsSelected}\"");
        StringAssert.Contains(main, "Click=\"SupplierReturns_OnClick\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Phiếu trả nhà cung cấp\"", StringComparison.Ordinal));

        StringAssert.Contains(mainCode, "PurchasingReportingHost.Content = purchasingReportingView");
        StringAssert.Contains(mainCode, "await _viewModel.NavigatePurchasingReportingAsync()");

        StringAssert.Contains(app, "AddSingleton<IPurchasingReportingService, PurchasingReportingService>()");
        StringAssert.Contains(app, "AddSingleton<PurchasingReportingViewModel>()");
        StringAssert.Contains(app, "AddSingleton<PurchasingReportingView>()");
    }

    [TestMethod]
    public void Presentation_UsesOfficeLabelsForCanonicalStates()
    {
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchasingReportingPresentation.cs");

        StringAssert.Contains(presentation, "[\"pending_approval\"] = \"Chờ duyệt\"");
        StringAssert.Contains(presentation, "[\"approved\"] = \"Đã duyệt\"");
        StringAssert.Contains(presentation, "[\"partially_received\"] = \"Nhận một phần\"");
        StringAssert.Contains(presentation, "[\"fully_received\"] = \"Đã nhận đủ\"");
        StringAssert.Contains(presentation, "[\"posted\"] = \"Đã ghi sổ\"");
        StringAssert.Contains(presentation, "[\"reversed\"] = \"Đã đảo\"");
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
