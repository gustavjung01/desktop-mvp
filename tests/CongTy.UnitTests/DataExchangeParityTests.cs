namespace CongTy.UnitTests;

[TestClass]
public sealed class DataExchangeParityTests
{
    [TestMethod]
    public void Service_UsesCurrentCanonicalDataExchangeRoutes()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "DataExchangeService.cs");
        foreach (var route in new[]
        {
            "/api/file-operations/products/export",
            "/api/file-operations/products/import",
            "/api/file-operations/pricing/export",
            "/api/pricing/import",
            "/api/file-operations/stocktake/export",
            "/api/file-operations/stocktake/import",
            "/api/file-operations/quotation",
            "/api/inventory/balances/drill-down"
        }) StringAssert.Contains(source, route);

        StringAssert.Contains(source, "PostIdempotentDataAsync");
        StringAssert.Contains(source, "idempotencyKeys.IsValid");
    }

    [TestMethod]
    public void ViewModel_UsesExactPermissionsAndCanonicalRetryKeys()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Operations", "DataExchangeViewModel.cs");
        foreach (var permission in new[]
        {
            "core.product.read",
            "core.product.write",
            "core.inventory-tracking-policy.read",
            "core.inventory-tracking-policy.manage",
            "core.price.read",
            "core.price.write",
            "core.stocktake.read",
            "core.stocktake.create",
            "core.stocktake.count",
            "core.inventory.read"
        }) StringAssert.Contains(source, $"\"{permission}\"");

        StringAssert.Contains(source, "_importOperationKey ??=");
        StringAssert.Contains(source, "_idempotencyKeys.Create(ImportScope(PendingKind))");
        StringAssert.Contains(source, "_operationKeys.TryGetValue(slot");
        StringAssert.Contains(source, "current.Fingerprint == fingerprint");
        StringAssert.Contains(source, "new DataExchangePricingImportRequest(true, operationKey, items)");
        Assert.IsFalse(source.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesFiveWebWorkspacesAndOfficeLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Operations", "DataExchangeView.xaml");
        foreach (var text in new[]
        {
            "Sản phẩm và SKU",
            "Cập nhật giá bán theo SKU",
            "Nhập số kiểm kê thực tế",
            "Báo giá",
            "Biến động tồn kho theo SKU",
            "Chọn tệp để nhập",
            "Tải mẫu Excel",
            "Tải mẫu CSV",
            "Xuất giá hiện tại Excel",
            "Kho kiểm kê",
            "Kiểm kê không thay đổi tồn ngay.",
            "Tính báo giá",
            "Kênh bán",
            "Nhóm khách",
            "Khách hàng",
            "Tồn hiện tại",
            "Đang giữ",
            "Khả dụng",
            "Xem thêm"
        }) StringAssert.Contains(view, text);

        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains(">API<", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("NPP Core", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(view, "{Binding PendingFileName, Mode=OneWay}");
        StringAssert.Contains(view, "{Binding PendingRows.Count, Mode=OneWay}");
        StringAssert.Contains(view, "{Binding SelectedPendingCount, Mode=OneWay}");
    }

    [TestMethod]
    public void ProductImport_PreservesCurrentWebColumnsAndGuardrails()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Operations", "DataExchangeModels.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Operations", "DataExchangeViewModel.cs");

        foreach (var column in new[]
        {
            "productCode", "productName", "catalogName", "categoryCode", "brandCode",
            "productIsCatalogVisible", "productIsOrderable", "productIsActive",
            "sku", "skuName", "variantKind", "isInventoryBase", "isSellable",
            "isCatalogVisible", "isActive", "unitCode", "conversionToBase",
            "lotTrackingMode", "expiryTrackingMode"
        }) StringAssert.Contains(source, $"\"{column}\"");

        StringAssert.Contains(viewModel, "SKU dùng làm đơn vị tồn chuẩn phải có Hệ số quy đổi = 1.");
        StringAssert.Contains(viewModel, "muốn quản lý hạn sử dụng thì phải bật Quản lý theo lô.");
        StringAssert.Contains(viewModel, "Mỗi lần chỉ nhập tối đa 2.000 dòng giá.");
        StringAssert.Contains(viewModel, "Mỗi đợt kiểm kê tối đa 500 dòng.");
    }

    [TestMethod]
    public void FileHandling_ReusesNativeSpreadsheetReaderWriter()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Operations", "DataExchangeFileHelper.cs");
        StringAssert.Contains(source, "SpreadsheetMatrixReader.ReadAsync");
        StringAssert.Contains(source, "PricingWorkbookWriter.Write");
        StringAssert.Contains(source, "GuardFormula");
    }

    [TestMethod]
    public void Shell_WiresDataExchangeAfterUi77AtWorkspace44()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.DataExchange.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.DataExchange.cs");
        var hook = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "\"operations.data-exchange\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 44");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 44");
        StringAssert.Contains(host, "workspaceTabs.Items[44]");
        StringAssert.Contains(host, "\"DỮ LIỆU VẬN HÀNH\"");
        StringAssert.Contains(host, "\"Nhập/xuất dữ liệu và báo giá\"");
        StringAssert.Contains(hook, "WireSupplierPaymentsWorkspace();");
        StringAssert.Contains(hook, "WireDataExchangeWorkspace();");
        StringAssert.Contains(xaml, "Text=\"Nhập/xuất dữ liệu\"");
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
