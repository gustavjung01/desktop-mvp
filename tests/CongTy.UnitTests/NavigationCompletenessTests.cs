namespace CongTy.UnitTests;

[TestClass]
public sealed class NavigationCompletenessTests
{
    [TestMethod]
    public void Dashboard_ShortcutsUseExistingShellPermissionsAndNavigate()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Dashboard", "DashboardView.xaml");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        var shortcuts = new (string Tag, string Permission, string Navigate)[]
        {
            ("sales.operations", "CanViewSalesOperations", "NavigateSalesOperationsAsync"),
            ("sales.customer-onboarding", "CanViewCustomerOnboarding", "NavigateCustomerOnboardingAsync"),
            ("purchasing.orders", "CanViewPurchaseOrders", "NavigatePurchaseOrdersAsync"),
            ("purchasing.receipts", "CanViewGoodsReceipts", "NavigateGoodsReceiptsAsync"),
            ("inventory.fulfillment", "CanViewFulfillment", "NavigateFulfillmentAsync"),
            ("logistics.delivery-orders", "CanViewDeliveryOrders", "NavigateDeliveryOrdersAsync"),
            ("logistics.trips", "CanViewTripPlanning", "NavigateTripPlanningAsync"),
            ("accounting.receivables", "CanViewReceivables", "NavigateReceivablesAsync"),
            ("accounting.customer-payments", "CanViewCustomerPayments", "NavigateCustomerPaymentsAsync"),
            ("sales.reporting", "CanViewSalesReporting", "NavigateSalesReportingAsync"),
            ("purchasing.reporting", "CanViewPurchasingReporting", "NavigatePurchasingReportingAsync"),
            ("logistics.reporting", "CanViewLogisticsReporting", "NavigateLogisticsReportingAsync"),
            ("accounting.aging", "CanViewAging", "NavigateAgingAsync")
        };

        foreach (var shortcut in shortcuts)
        {
            var line = view.Split('\n').Single(item => item.Contains($"Tag=\"{shortcut.Tag}\"", StringComparison.Ordinal));
            StringAssert.Contains(line, $"DataContext.{shortcut.Permission}");
            StringAssert.Contains(line, "Click=\"Shortcut_OnClick\"");
            Assert.IsFalse(line.Contains("IsEnabled=\"False\"", StringComparison.Ordinal));

            StringAssert.Contains(main, $"case \"{shortcut.Tag}\":");
            StringAssert.Contains(main, shortcut.Navigate);
        }
    }

    [TestMethod]
    public void CrossWorkspaceActions_UseExistingShellPermissionsAndRealDestinations()
    {
        var mcp = ReadRepoFile("src", "CongTy.Desktop", "Settings", "EmployeeMcpReportingView.xaml");
        var mcpCode = ReadRepoFile("src", "CongTy.Desktop", "Settings", "EmployeeMcpReportingView.xaml.cs");
        var mcpShell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.McpRoutes.cs");
        StringAssert.Contains(mcp, "DataContext.CanViewEmployeeDirectory");
        StringAssert.Contains(mcp, "Click=\"EmployeeDirectory_OnClick\"");
        StringAssert.Contains(mcpCode, "EmployeeDirectoryRequested");
        StringAssert.Contains(mcpShell, "McpRoutesEmployeeDirectoryRequested");
        StringAssert.Contains(mcpShell, "NavigateEmployeeDirectoryAsync");
        StringAssert.Contains(mcpShell, "ApplyEmployeeDirectoryHeader");
        Assert.IsFalse(mcp.Contains("sẽ được nối khi phần Quản trị hệ thống được triển khai", StringComparison.Ordinal));

        var aging = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "AgingReportingView.xaml");
        StringAssert.Contains(aging, "DataContext.CanViewReceivables");
        StringAssert.Contains(aging, "DataContext.CanViewPayables");
        StringAssert.Contains(aging, "Click=\"Receivables_OnClick\"");
        StringAssert.Contains(aging, "Click=\"Payables_OnClick\"");
        Assert.IsFalse(aging.Contains("UI-7.3 hoàn thiện", StringComparison.Ordinal));
        Assert.IsFalse(aging.Contains("UI-7.6 hoàn thiện", StringComparison.Ordinal));

        var logistics = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "LogisticsReportingView.xaml");
        foreach (var permission in new[]
        {
            "CanViewTripPlanning",
            "CanViewDeliveryAttempts",
            "CanViewDeliveryOrders",
            "CanViewTripReconciliation"
        })
        {
            StringAssert.Contains(logistics, $"DataContext.{permission}");
        }
        StringAssert.Contains(logistics, "Click=\"TripPlanning_OnClick\"");
        StringAssert.Contains(logistics, "Click=\"DeliveryAttempts_OnClick\"");
        StringAssert.Contains(logistics, "Click=\"DeliveryOrders_OnClick\"");
        StringAssert.Contains(logistics, "Click=\"TripReconciliation_OnClick\"");

        var delivery = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryOrderView.xaml");
        StringAssert.Contains(delivery, "DataContext.CanViewCustomerReturns");
        StringAssert.Contains(delivery, "Click=\"CustomerReturns_OnClick\"");

        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        StringAssert.Contains(main, "logisticsReportingView.NavigationRequested += LogisticsReportingView_OnNavigationRequested");
        StringAssert.Contains(main, "deliveryOrderView.CustomerReturnsRequested += DeliveryOrderView_OnCustomerReturnsRequested");
        StringAssert.Contains(main, "agingReportingView.ReceivablesRequested += AgingReportingView_OnReceivablesRequested");
        StringAssert.Contains(main, "agingReportingView.PayablesRequested += AgingReportingView_OnPayablesRequested");
        StringAssert.Contains(main, "ApplyReceivablesHeader");
        StringAssert.Contains(main, "ApplyPayablesHeader");
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
