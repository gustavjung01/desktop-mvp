using CongTy.Contracts;
using CongTy.Desktop.Sales;

namespace CongTy.UnitTests;

[TestClass]
public sealed class SalesReportingParityTests
{
    [TestMethod]
    public void Presentation_KeepsCanonicalDimensionsAndOfficeLabels()
    {
        CollectionAssert.AreEqual(
            new[] { "customers", "customerGroups", "channels", "products", "productGroups", "employees" },
            SalesReportingPresentation.Dimensions.Select(option => option.Key).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Khách hàng", "Loại khách", "Kênh bán", "Sản phẩm", "Nhóm hàng", "Nhân viên bán hàng" },
            SalesReportingPresentation.Dimensions.Select(option => option.Label).ToArray());

        Assert.AreEqual("Sản lượng", SalesReportingPresentation.MetricLabel("products"));
        Assert.AreEqual("Số đơn", SalesReportingPresentation.MetricLabel("customers"));
        Assert.AreEqual("Mới trong kỳ", SalesReportingPresentation.Comparisons.Single(option => option.Key == "new").Label);
        Assert.AreEqual("Không phát sinh kỳ này", SalesReportingPresentation.Comparisons.Single(option => option.Key == "inactive").Label);
    }

    [TestMethod]
    public void Service_UsesCanonicalReadAndOfficialExportEndpoints()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "SalesReportingService.cs");
        var client = ReadRepoFile("src", "CongTy.ApiClient", "CompanyApiClient.cs");

        StringAssert.Contains(service, "\"/api/reporting/sales\"");
        StringAssert.Contains(service, "\"/api/reporting/sales-export\"");
        StringAssert.Contains(service, "productGroupId");
        StringAssert.Contains(service, "customerGroupId");
        StringAssert.Contains(service, "includeZeroProducts");
        StringAssert.Contains(service, "column=");
        StringAssert.Contains(service, "apiClient.GetFileAsync");
        var exportCall = service.IndexOf("\"/api/reporting/sales-export\"", StringComparison.Ordinal);
        var warehouseFilter = service.IndexOf("warehouseId,", exportCall, StringComparison.Ordinal);
        var productFilter = service.IndexOf("productGroupId,", warehouseFilter, StringComparison.Ordinal);
        var customerFilter = service.IndexOf("customerGroupId,", productFilter, StringComparison.Ordinal);
        var zeroFilter = service.IndexOf("includeZeroProducts,", customerFilter, StringComparison.Ordinal);
        var dimensionFilter = service.IndexOf("dimension,", zeroFilter, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, exportCall);
        Assert.IsGreaterThan(exportCall, warehouseFilter);
        Assert.IsGreaterThan(warehouseFilter, productFilter);
        Assert.IsGreaterThan(productFilter, customerFilter);
        Assert.IsGreaterThan(customerFilter, zeroFilter);
        Assert.IsGreaterThan(zeroFilter, dimensionFilter);
        StringAssert.Contains(client, "public async Task<ApiDownloadFile> GetFileAsync(");
        StringAssert.Contains(client, "ContentDisposition");
        Assert.IsFalse(service.Contains("PostDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("PostIdempotentDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Patch", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Delete", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesDenyByDefaultReportingAndExportPermissions()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesReportingViewModel.cs");

        StringAssert.Contains(viewModel, "\"core.reporting.sales.read\"");
        StringAssert.Contains(viewModel, "\"core.reporting.export\"");
        StringAssert.Contains(viewModel, "CanExport => _access.HasPermission(ExportPermission) && CanRead");
        StringAssert.Contains(viewModel, "Kỳ báo cáo tối đa 366 ngày.");
        StringAssert.Contains(viewModel, "SalesReportingSavedView");
        StringAssert.Contains(viewModel, "_appliedWarehouseId");
        StringAssert.Contains(viewModel, "_appliedProductGroupId");
        StringAssert.Contains(viewModel, "_appliedCustomerGroupId");
        StringAssert.Contains(viewModel, "_appliedIncludeZeroProducts");
        StringAssert.Contains(viewModel, "_accessGeneration++");
        StringAssert.Contains(viewModel, "_loadCts?.Cancel()");
        StringAssert.Contains(viewModel, "_exportCts?.Cancel()");
        StringAssert.Contains(viewModel, "accessGeneration != _accessGeneration");
        StringAssert.Contains(viewModel, "request != _exportGeneration");
    }

    [TestMethod]
    public void TrendSeries_UsesSharedScaleForCurrentAndPreviousRevenue()
    {
        var series = SalesReportingPresentation.TrendSeries(
        [
            new SalesReportingTrendData { BusinessDate = "2026-09-15", CurrencyCode = "VND", Revenue = "100", PreviousRevenue = "10" },
            new SalesReportingTrendData { BusinessDate = "2026-09-16", CurrencyCode = "VND", Revenue = "200", PreviousRevenue = "20" }
        ]).Single();

        Assert.AreEqual(2, series.CurrentPoints.Count);
        Assert.AreEqual(2, series.PreviousPoints.Count);
        Assert.AreNotEqual(series.CurrentPoints[0].Y, series.PreviousPoints[0].Y);
        Assert.IsLessThan(series.PreviousPoints[0].Y, series.CurrentPoints[0].Y);
    }

    [TestMethod]
    public void View_PreservesWebFilterAnalysisTrendDetailAndExportFlow()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesReportingView.xaml");

        StringAssert.Contains(view, "<BooleanToVisibilityConverter x:Key=\"BooleanToVisibilityConverter\" />");

        foreach (var text in new[]
        {
            "Kỳ nhanh",
            "Từ ngày",
            "Đến ngày",
            "Kho",
            "Chiều phân tích",
            "Nhóm khách",
            "Nhóm sản phẩm",
            "Hiện mã không phát sinh",
            "Tiền tệ",
            "So với kỳ trước",
            "Tìm trong danh sách",
            "Xóa lọc",
            "Lưu chế độ xem",
            "Doanh thu",
            "Đơn đã chốt",
            "Khách mua",
            "Mặt hàng đã bán",
            "Tỷ trọng",
            "Kỳ trước",
            "Thay đổi",
            "Chi tiết",
            "Tổng theo tiền tệ",
            "Doanh thu theo ngày",
            "Xuất Báo cáo bán hàng",
            "Excel (.xlsx)",
            "CSV (.csv)",
            "Cột cần xuất",
            "Chọn tất cả",
            "Bỏ chọn",
            "Mặc định"
        })
        {
            StringAssert.Contains(view, text);
        }

        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesReportingViewModel.cs");
        StringAssert.Contains(viewModel, "ApplyText => IsBusy ? \"Đang cập nhật…\" : \"Áp dụng\"");

        Assert.IsFalse(view.Contains("Core", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("NPP", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExportColumns_MatchWebDimensionDefaults()
    {
        var products = SalesReportingPresentation.ExportColumns("products");
        CollectionAssert.AreEqual(
            new[]
            {
                "code", "name", "currencyCode", "unitCode", "unitName", "quantity", "revenue",
                "sharePercent", "previousRevenue", "previousQuantity", "changePercent", "source"
            },
            products.Select(column => column.Key).ToArray());
        Assert.IsFalse(products.Single(column => column.Key == "unitCode").DefaultSelected);
        Assert.IsFalse(products.Single(column => column.Key == "source").DefaultSelected);

        var customers = SalesReportingPresentation.ExportColumns("customers");
        Assert.IsTrue(customers.Single(column => column.Key == "documentCount").DefaultSelected);
        Assert.IsFalse(customers.Single(column => column.Key == "source").DefaultSelected);
    }

    [TestMethod]
    public void SavedView_StaysLocalAndOnlyStoresAnalysisPreferences()
    {
        var store = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesReportingViewStateStore.cs");

        StringAssert.Contains(store, "sales-reporting-view.json");
        StringAssert.Contains(store, "Dimension");
        StringAssert.Contains(store, "Search");
        StringAssert.Contains(store, "Currency");
        StringAssert.Contains(store, "Comparison");
        Assert.IsFalse(store.Contains("WarehouseId", StringComparison.Ordinal));
        Assert.IsFalse(store.Contains("ProductGroupId", StringComparison.Ordinal));
        Assert.IsFalse(store.Contains("CustomerGroupId", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Shell_PreservesSalesReportingHeaderActionsWithoutFakeGrossMarginNavigation()
    {
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(app, "ISalesReportingService, SalesReportingService");
        StringAssert.Contains(app, "ISalesReportingViewStateStore, SalesReportingViewStateStore");
        StringAssert.Contains(app, "SalesReportingViewModel");
        StringAssert.Contains(app, "SalesReportingView");

        StringAssert.Contains(shell, "\"sales.reporting\" => \"Báo cáo bán hàng\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 32");
        StringAssert.Contains(shell, "CanViewSalesReporting");
        StringAssert.Contains(shell, "CanExportSalesReporting");
        StringAssert.Contains(shell, "NavigateSalesReportingAsync");
        StringAssert.Contains(shell, "32 => _salesReporting.Message");

        StringAssert.Contains(xaml, "Tag=\"{Binding IsSalesReportingSelected}\"");
        StringAssert.Contains(xaml, "Click=\"SalesReporting_OnClick\"");
        StringAssert.Contains(xaml, "Click=\"SalesReportingExport_OnClick\"");
        StringAssert.Contains(xaml, "Click=\"SalesReportingOrders_OnClick\"");
        StringAssert.Contains(xaml, "Content=\"Xem báo cáo lãi gộp\"");
        StringAssert.Contains(xaml, "ToolTip=\"Màn Lãi gộp sẽ được nối khi UI-5.2 hoàn thiện.\"");
        StringAssert.Contains(xaml, "x:Name=\"SalesReportingHost\"");
        StringAssert.Contains(code, "SalesReportingHost.Content = salesReportingView");
        StringAssert.Contains(code, "_salesReportingView.OpenExportDialog()");
        Assert.IsFalse(code.Contains("SalesGrossMargin_OnClick", StringComparison.Ordinal));
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
