using CongTy.Desktop.Pricing;

namespace CongTy.UnitTests;

[TestClass]
public sealed class PricingParityTests
{
    [TestMethod]
    public void Presentation_PreservesWebPricingSemantics()
    {
        CollectionAssert.AreEqual(
            new[] { "BASE", "CHANNEL", "CUSTOMER_GROUP", "CUSTOMER", "PROMOTION", "CUSTOM" },
            PricingPresentation.ListTypes.Select(row => row.Value).ToArray());
        Assert.AreEqual(100, PricingPresentation.DefaultPriority("BASE"));
        Assert.AreEqual(400, PricingPresentation.DefaultPriority("PROMOTION"));
        Assert.AreEqual(500, PricingPresentation.DefaultPriority("CUSTOMER"));
        Assert.AreEqual("Đặt giá trực tiếp", PricingPresentation.AdjustmentLabel("FIXED_PRICE"));
        Assert.AreEqual("Có thể kết hợp · Không xét tiếp", PricingPresentation.ApplyMode(new CongTy.Contracts.PriceListData { StackingMode = "STACKABLE", StopProcessing = true }));
    }

    [TestMethod]
    public void Source_PricingWorkspaceMatchesWebHierarchyAndContracts()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Pricing", "PricingView.xaml");
        var service = ReadRepoFile("src", "CongTy.ApiClient", "PricingService.cs");
        var state = ReadRepoFile("src", "CongTy.Desktop", "Pricing", "PricingViewModel.State.cs");
        var loading = ReadRepoFile("src", "CongTy.Desktop", "Pricing", "PricingViewModel.LoadingChannelsLists.cs");
        var items = ReadRepoFile("src", "CongTy.Desktop", "Pricing", "PricingViewModel.ItemsResolver.cs");
        var adjustment = ReadRepoFile("src", "CongTy.Desktop", "Pricing", "PricingViewModel.Adjustment.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "PricingContracts.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var window = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        var tabs = new[] { "Header=\"Kênh bán\"", "Header=\"Danh mục giá\"", "Header=\"Giá sản phẩm\"", "Header=\"Điều chỉnh giá\"", "Header=\"Kiểm tra giá áp dụng\"" };
        var previous = -1;
        foreach (var tab in tabs)
        {
            var index = view.IndexOf(tab, StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, index);
            previous = index;
        }

        foreach (var label in new[] { "Thứ tự ưu tiên", "Cách áp dụng", "Không xét các mức sau khi áp dụng", "SKU / Quy cách", "Số lượng từ", "Số lượng đến", "Hiệu lực từ", "Hiệu lực đến", "Mã tham chiếu", "Giá điều chỉnh thủ công" })
            StringAssert.Contains(view, label);

        StringAssert.Contains(state, "\"core.price.read\"");
        StringAssert.Contains(state, "\"core.price.write\"");
        StringAssert.Contains(state, "\"core.sales-order.price-override\"");
        StringAssert.Contains(loading, "KeyFor(intent)");
        StringAssert.Contains(items, "KeyFor(intent)");
        StringAssert.Contains(service, "idempotencyKeys.IsValid");
        StringAssert.Contains(service, "/api/sales-channels");
        StringAssert.Contains(service, "/api/price-lists");
        StringAssert.Contains(service, "/api/pricing/resolve");
        StringAssert.Contains(service, "/api/file-operations/pricing/export");
        StringAssert.Contains(service, "/api/pricing/import");
        StringAssert.Contains(contracts, "JsonPropertyName(\"replaceFrom\")");
        StringAssert.Contains(contracts, "JsonPropertyName(\"applyAt\")");
        StringAssert.Contains(adjustment, "ReplaceFrom: true");
        StringAssert.Contains(adjustment, "SourceBatchId: operationKey");
        StringAssert.Contains(adjustment, "KeyFor(intent)");
        StringAssert.Contains(adjustment, "CompleteIntent(intent)");
        StringAssert.Contains(adjustment, "PRICE_FILE_ADJUSTMENT");

        StringAssert.Contains(shell, "\"catalog.pricing\" => \"Giá bán và khuyến mãi\"");
        StringAssert.Contains(shell, "NavigatePricingAsync");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 15");
        StringAssert.Contains(window, "x:Name=\"PricingHost\"");
        StringAssert.Contains(window, "Click=\"CatalogPricing_OnClick\"");
        StringAssert.Contains(app, "AddSingleton<IPricingService, PricingService>()");

        Assert.IsFalse(view.Contains("Background=\"#", StringComparison.Ordinal), "UI-2.8 không được hard-code màu riêng.");
        StringAssert.Contains(view, "OfficeCardHeaderStyle");
        StringAssert.Contains(view, "OfficeCardBodyStyle");
        StringAssert.Contains(view, "OfficePrimaryButtonStyle");
        StringAssert.Contains(view, "Content=\"Tạo mới\"");
        StringAssert.Contains(view, "Content=\"Xem giá áp dụng\"");
        Assert.IsLessThan(view.IndexOf("Tìm sản phẩm hoặc SKU", StringComparison.Ordinal), view.IndexOf("SKU đang bán", StringComparison.Ordinal));
        Assert.IsLessThan(view.IndexOf("x:Name=\"OverviewGrid\"", StringComparison.Ordinal), view.IndexOf("Tìm sản phẩm hoặc SKU", StringComparison.Ordinal));

        StringAssert.Contains(view, "Content=\"Điều chỉnh trực tiếp\"");
        StringAssert.Contains(view, "Content=\"Tải file mẫu\"");
        StringAssert.Contains(view, "Content=\"Nhập từ file\"");
        Assert.IsFalse(view.Contains("Cập nhật giá từ Excel", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("PricingDataExchange_OnClick", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("PricingHistory_OnClick", StringComparison.Ordinal));
        Assert.IsFalse(navigator.Contains("ShowCrossRouteNotice", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WorkbookWriter_CreatesRealXlsxPackage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pricing-{Guid.NewGuid():N}.xlsx");
        try
        {
            PricingWorkbookWriter.Write(path,
            [
                new PricingWorkbookSheet("Bảng giá tổng hợp", ["SKU", "Giá"], [new[] { "SKU001", "120000" }]),
                new PricingWorkbookSheet("Điều kiện áp dụng", ["Mã bảng giá"], [new[] { "BASE" }])
            ]);
            using var archive = System.IO.Compression.ZipFile.OpenRead(path);
            Assert.IsNotNull(archive.GetEntry("[Content_Types].xml"));
            Assert.IsNotNull(archive.GetEntry("xl/workbook.xml"));
            Assert.IsNotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));
            Assert.IsNotNull(archive.GetEntry("xl/worksheets/sheet2.xml"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
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
