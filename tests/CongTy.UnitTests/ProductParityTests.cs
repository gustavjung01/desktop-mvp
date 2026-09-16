using CongTy.Contracts;
using CongTy.Desktop.Products;

namespace CongTy.UnitTests;

[TestClass]
public sealed class ProductParityTests
{
    [TestMethod]
    public void ProductPresentation_UsesOfficeLabelsAndWebRowShape()
    {
        var row = new ProductRow(
            1,
            "product-id",
            "SP001",
            "Có",
            "Bột mì",
            "Thực phẩm",
            "Nhãn A",
            "Có",
            "Có",
            "Đang sử dụng",
            "Ngừng sử dụng",
            new ProductData());

        Assert.AreEqual(1, row.Stt);
        Assert.AreEqual("SP001", row.Code);
        Assert.AreEqual("Ngừng sử dụng", row.ToggleAction);
        Assert.AreEqual("Có", ProductPresentation.YesNo(true));
        Assert.AreEqual("Ngừng sử dụng", ProductPresentation.Toggle(true));
    }

    [TestMethod]
    public void Source_ProductWorkspaceMatchesCurrentWebStructureAndKeepsInventoryShell()
    {
        var productView = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductView.xaml");
        var catalogView = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductCatalogView.xaml");
        var quickView = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductQuickSetupView.xaml");
        var bulkView = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductBulkUpdateView.xaml");
        var categoryView = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductCategoryView.xaml");
        var brandView = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductBrandView.xaml");
        var unitView = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductUnitWorkspaceView.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductViewModel.cs");
        var service = ReadRepoFile("src", "CongTy.ApiClient", "ProductService.cs");
        var apiClient = ReadRepoFile("src", "CongTy.ApiClient", "CompanyApiClient.cs");
        var reader = ReadRepoFile("src", "CongTy.Desktop", "Products", "SpreadsheetMatrixReader.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var window = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        var expectedTabs = new[]
        {
            "Header=\"Sản phẩm\"",
            "Header=\"Thiết lập nhanh\"",
            "Header=\"Cập nhật SP\"",
            "Header=\"Loại sản phẩm\"",
            "Header=\"Nhãn hàng\"",
            "Header=\"Đơn vị và quy đổi\""
        };
        var previous = -1;
        foreach (var tab in expectedTabs)
        {
            var index = productView.IndexOf(tab, StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, index);
            previous = index;
        }

        StringAssert.Contains(catalogView, "Header=\"Mã\"");
        StringAssert.Contains(catalogView, "Header=\"Ảnh\"");
        StringAssert.Contains(catalogView, "Header=\"Tên\"");
        StringAssert.Contains(catalogView, "Header=\"Loại\"");
        StringAssert.Contains(catalogView, "Header=\"Nhãn hàng\"");
        StringAssert.Contains(catalogView, "Content=\"Thêm sản phẩm\"");
        StringAssert.Contains(catalogView, "Content=\"SKU\"");

        StringAssert.Contains(quickView, "Thiết lập nhanh sản phẩm &amp; SKU");
        StringAssert.Contains(quickView, "SKU &amp; quy cách");
        StringAssert.Contains(quickView, "Đơn vị &amp; quy đổi");
        StringAssert.Contains(quickView, "Giá bán");
        StringAssert.Contains(quickView, "Tồn kho");
        StringAssert.Contains(quickView, "Quản lý lô");
        StringAssert.Contains(quickView, "Hạn sử dụng");
        StringAssert.Contains(quickView, "x:Name=\"ProductSearchPopup\"");
        StringAssert.Contains(quickView, "x:Name=\"ProductSearchResults\"");
        Assert.IsFalse(quickView.Contains("x:Name=\"ProductCombo\"", StringComparison.Ordinal));
        StringAssert.Contains(quickView, "PreviewMouseWheel=\"PageScrollViewer_OnPreviewMouseWheel\"");
        StringAssert.Contains(quickView, "IsEnabled=\"{Binding CanEnableQuickOrderable}\"");
        StringAssert.Contains(catalogView, "IsEnabled=\"{Binding CanEnableDraftOrderable}\"");
        StringAssert.Contains(viewModel, "OpenProductEditAsync");
        StringAssert.Contains(viewModel, "CanEnableQuickOrderable");
        StringAssert.Contains(viewModel, "CanEnableDraftOrderable");

        StringAssert.Contains(bulkView, "Cập nhật sản phẩm theo SKU");
        StringAssert.Contains(bulkView, "Xem trước thay đổi");
        StringAssert.Contains(bulkView, "BulkMappingOptions");
        StringAssert.Contains(bulkView, "Content=\"Tải file mẫu\"");
        var bulkCode = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductBulkUpdateView.xaml.cs");
        StringAssert.Contains(bulkCode, "mau-cap-nhat-san-pham-theo-sku.csv");
        StringAssert.Contains(bulkCode, "SKU,Khối lượng,Đơn vị khối lượng");
        StringAssert.Contains(categoryView, "Loại sản phẩm");
        StringAssert.Contains(brandView, "Nhãn hàng");
        StringAssert.Contains(unitView, "Header=\"Danh mục đơn vị\"");
        StringAssert.Contains(unitView, "Header=\"Thiết lập theo SKU\"");
        StringAssert.Contains(unitView, "Mã vạch");
        StringAssert.Contains(unitView, "PreviewMouseWheel=\"PageScrollViewer_OnPreviewMouseWheel\"");
        var scrollSupport = ReadRepoFile("src", "CongTy.Desktop", "Products", "ProductScrollWheel.cs");
        StringAssert.Contains(scrollSupport, "CanScroll");

        StringAssert.Contains(viewModel, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(viewModel, "KeyFor(");
        StringAssert.Contains(viewModel, "SpreadsheetMatrixReader.ReadAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid");
        StringAssert.Contains(service, "DeleteIdempotentDataAsync");
        StringAssert.Contains(service, "/image/upload");
        Assert.IsFalse(service.Contains("/image/prepare", StringComparison.Ordinal));
        StringAssert.Contains(apiClient, "PutBytesIdempotentDataAsync");
        StringAssert.Contains(apiClient, "HttpMethod.Delete");
        StringAssert.Contains(reader, "\".xlsx\"");
        StringAssert.Contains(reader, "\".csv\"");

        StringAssert.Contains(shell, "\"catalog.products\" => \"Danh mục sản phẩm\"");
        StringAssert.Contains(shell, "NavigateProductsAsync");
        StringAssert.Contains(window, "x:Name=\"ProductHost\"");
        StringAssert.Contains(window, "Click=\"CatalogProducts_OnClick\"");
        StringAssert.Contains(app, "AddSingleton<IProductService, ProductService>()");

        StringAssert.Contains(shell, "NavigateInventoryStocktakeAsync");
        StringAssert.Contains(shell, "\"inventory.stocktake\"");
        StringAssert.Contains(window, "x:Name=\"StocktakeHost\"");
        StringAssert.Contains(window, "Click=\"InventoryStocktake_OnClick\"");
        StringAssert.Contains(window, "x:Name=\"TransferHost\"");
        StringAssert.Contains(window, "x:Name=\"AdjustmentHost\"");
        StringAssert.Contains(window, "x:Name=\"ManualInboundHost\"");
        StringAssert.Contains(shell, "\"inventory.manual-inbound\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 10");
        StringAssert.Contains(shell, "\"inventory.adjustments\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 11");
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
