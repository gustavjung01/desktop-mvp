using CongTy.Desktop.Purchasing;

namespace CongTy.UnitTests;

[TestClass]
public sealed class PurchasePriceParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalPriceEndpointsAndCreateIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "SupplierPurchasePriceService.cs");

        StringAssert.Contains(service, "/api/supplier-purchase-prices?limit=1000&offset=0");
        StringAssert.Contains(service, "PostIdempotentDataAsync<SupplierPurchasePriceRequest, SupplierPurchasePriceData>");
        StringAssert.Contains(service, "PatchDataAsync<SupplierPurchasePriceRequest, SupplierPurchasePriceData>");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(idempotencyKey)");
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("PatchIdempotentDataAsync", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesCanonicalPermissionsRevisionAndDispatcher()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchasePriceViewModel.cs");

        StringAssert.Contains(vm, "core.supplier-purchase-price.read");
        StringAssert.Contains(vm, "core.supplier-purchase-price.manage");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"supplier-purchase-price-create\")");
        StringAssert.Contains(vm, "ExpectedRevision = _editing?.Revision");
        StringAssert.Contains(vm, "Dispatcher.CurrentDispatcher");
        StringAssert.Contains(vm, "_uiDispatcher.BeginInvoke((Action)HandleAccessChanged)");
        Assert.IsFalse(vm.Contains("sales", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Ui_UsesCompactKpisFilterListAndEditor()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchasePriceView.xaml");

        foreach (var marker in new[]
        {
            "Text=\"Tổng dòng giá\"",
            "Text=\"Đang hiệu lực quản trị\"",
            "Text=\"Nhà cung cấp có giá\"",
            "Text=\"Lọc theo nhà cung cấp\"",
            "Text=\"Danh mục mua hàng\"",
            "Text=\"Giá theo nhà cung cấp và SKU\"",
            "Content=\"Thêm giá mua\"",
            "Text=\"Nhà cung cấp\"",
            "Text=\"Tìm SKU mua hàng\"",
            "Text=\"Giá mua\"",
            "Text=\"Tiền tệ\"",
            "Text=\"Số lượng tối thiểu\"",
            "Text=\"Hiệu lực từ\"",
            "Text=\"Hiệu lực đến\"",
            "Text=\"Tham chiếu thỏa thuận\"",
            "Content=\"Đang sử dụng\""
        })
        {
            StringAssert.Contains(view, marker);
        }

        StringAssert.Contains(view, "OfficeSummaryCardStyle");
        StringAssert.Contains(view, "OfficeSummaryLabelStyle");
        StringAssert.Contains(view, "OfficeSummaryValueStyle");
        Assert.IsFalse(view.Contains("<UniformGrid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Shell_WiresPurchasePricesAfterPurchaseOrders()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 22");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 23");
        StringAssert.Contains(shell, "await _tripPlanning.EnsureLoadedAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 24");
        StringAssert.Contains(shell, "\"purchasing.purchase-prices\" => \"Bảng giá mua\"");
        StringAssert.Contains(shell, "CanViewPurchasePrices");
        StringAssert.Contains(shell, "NavigatePurchasePricesAsync");

        StringAssert.Contains(xaml, "Tag=\"{Binding IsPurchasePricesSelected}\"");
        StringAssert.Contains(xaml, "Click=\"PurchasePrices_OnClick\"");
        StringAssert.Contains(xaml, "x:Name=\"PurchasePriceHost\"");
        Assert.IsFalse(xaml.Contains("IsEnabled=\"False\"><TextBlock Text=\"Bảng giá mua\"", StringComparison.Ordinal));

        StringAssert.Contains(code, "PurchasePriceHost.Content = purchasePriceView");
        StringAssert.Contains(code, "NavigatePurchasePricesAsync()");
        StringAssert.Contains(app, "AddSingleton<ISupplierPurchasePriceService, SupplierPurchasePriceService>()");
        StringAssert.Contains(app, "AddSingleton<PurchasePriceViewModel>()");
        StringAssert.Contains(app, "AddSingleton<PurchasePriceView>()");
    }

    [TestMethod]
    public void Audit_LocksPurchasingOwnershipAndNoDatabaseWork()
    {
        var audit = ReadRepoFile("docs", "parity", "UI6_3_PURCHASE_PRICES_AUDIT.md");

        StringAssert.Contains(audit, "core.supplier-purchase-price.read");
        StringAssert.Contains(audit, "core.supplier-purchase-price.manage");
        StringAssert.Contains(audit, "CanonicalIdempotencyKeyProvider");
        StringAssert.Contains(audit, "expectedRevision");
        StringAssert.Contains(audit, "không lấy Sales Pricing làm fallback");
        StringAssert.Contains(audit, "không cần backend, DB hoặc migration mới");
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
