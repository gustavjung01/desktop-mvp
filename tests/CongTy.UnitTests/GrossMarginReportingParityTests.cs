using CongTy.Desktop.Sales;

namespace CongTy.UnitTests;

[TestClass]
public sealed class GrossMarginReportingParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalReadAndExportEndpoints()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "GrossMarginReportingService.cs");

        StringAssert.Contains(service, ""/api/reporting/gross-margin"");
        StringAssert.Contains(service, ""/api/reporting/gross-margin-export"");
        StringAssert.Contains(service, ""customers", "skus", "lines", "exceptions"");
        StringAssert.Contains(service, "warehouseId");
        StringAssert.Contains(service, "column=");
        StringAssert.Contains(service, "apiClient.GetFileAsync");
        Assert.IsFalse(service.Contains("PostDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("PostIdempotentDataAsync", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesDenyByDefaultPermissionsAndInvalidatesInflightWork()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Sales", "GrossMarginReportingViewModel.cs");

        StringAssert.Contains(vm, ""core.reporting.gross-margin.read"");
        StringAssert.Contains(vm, ""core.reporting.export"");
        StringAssert.Contains(vm, "CanExport => CanRead && _access.HasPermission(ExportPermission)");
        StringAssert.Contains(vm, "_accessGeneration++");
        StringAssert.Contains(vm, "_loadCts?.Cancel()");
        StringAssert.Contains(vm, "_exportCts?.Cancel()");
        StringAssert.Contains(vm, "accessGeneration != _accessGeneration");
        StringAssert.Contains(vm, "request != _exportGeneration");
        StringAssert.Contains(vm, "Kỳ báo cáo tối đa 366 ngày.");
    }

    [TestMethod]
    public void Presentation_KeepsCanonicalTabsExceptionLabelsAndExportDefaults()
    {
        CollectionAssert.AreEqual(
            new[] { "customers", "skus", "lines", "exceptions" },
            GrossMarginReportingPresentation.ExportDimensions.Select(option => option.Key).ToArray());

        Assert.AreEqual("Doanh thu không phải VND", GrossMarginReportingPresentation.ExceptionLabel("NON_VND_REVENUE"));
        Assert.AreEqual("Thiếu liên kết xuất/nhập kho", GrossMarginReportingPresentation.ExceptionLabel("MISSING_INVENTORY_LINEAGE"));
        Assert.AreEqual("Chưa có dữ liệu giá vốn", GrossMarginReportingPresentation.ExceptionLabel("MISSING_COST_FACT"));
        Assert.AreEqual("Dữ liệu giá vốn có bất thường", GrossMarginReportingPresentation.ExceptionLabel("COST_ANOMALY"));

        CollectionAssert.AreEqual(
            new[] { "customerCode", "customerName", "netRevenue", "cogs", "grossMargin", "grossMarginPercent" },
            GrossMarginReportingPresentation.ExportColumns("customers").Where(column => column.DefaultSelected).Select(column => column.Key).ToArray());

        CollectionAssert.AreEqual(
            new[] { "documentDate", "documentNumber", "eventKind", "customerCode", "customerName", "warehouseCode", "sku", "productName", "netRevenue", "cogs", "grossMargin" },
            GrossMarginReportingPresentation.ExportColumns("lines").Where(column => column.DefaultSelected).Select(column => column.Key).ToArray());
    }

    [TestMethod]
    public void View_PreservesWebFiltersKpisTabsAndExportFlow()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Sales", "GrossMarginReportingView.xaml");

        foreach (var text in new[]
        {
            "Từ ngày", "Đến ngày", "Kho", "Áp dụng", "Đặt lại",
            "Doanh thu thuần so sánh được", "Giá vốn", "Lãi gộp", "Biên lãi gộp",
            "Đối soát:", "Theo khách hàng", "Theo SKU", "Ngoại lệ",
            "Dòng chưa đủ điều kiện tính lãi gộp", "Mở giá vốn",
            "Xuất báo cáo lãi gộp", "Excel (.xlsx)", "CSV (.csv)",
            "Chọn tất cả", "Bỏ chọn", "Mặc định"
        })
        {
            StringAssert.Contains(view, text);
        }

        Assert.IsFalse(view.Contains("Phase 7", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("cost fact", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("canonical", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_WiresGrossMarginAsRealUi52Workspace()
    {
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(app, "IGrossMarginReportingService, GrossMarginReportingService");
        StringAssert.Contains(app, "GrossMarginReportingViewModel");
        StringAssert.Contains(app, "GrossMarginReportingView");
        StringAssert.Contains(shell, ""sales.gross-margin" => "Lãi gộp"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 33");
        StringAssert.Contains(shell, "CanViewGrossMargin");
        StringAssert.Contains(shell, "CanExportGrossMargin");
        StringAssert.Contains(shell, "NavigateGrossMarginAsync");
        StringAssert.Contains(xaml, "Tag="{Binding IsGrossMarginSelected}"");
        StringAssert.Contains(xaml, "Click="GrossMargin_OnClick"");
        StringAssert.Contains(xaml, "x:Name="GrossMarginReportingHost"");
        StringAssert.Contains(xaml, "Click="SalesGrossMargin_OnClick"");
        StringAssert.Contains(code, "GrossMarginReportingHost.Content = grossMarginReportingView");
        StringAssert.Contains(code, "_grossMarginReportingView.OpenExportDialog()");
        StringAssert.Contains(code, "NavigateInventoryCostingAsync()");
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
