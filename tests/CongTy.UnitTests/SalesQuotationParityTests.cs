namespace CongTy.UnitTests;

[TestClass]
public sealed class SalesQuotationParityTests
{
    [TestMethod]
    public void Quotation_IsOwnedBySalesAndRemovedFromDataExchange()
    {
        var dataExchangeView = Read("src", "CongTy.Desktop", "Operations", "DataExchangeView.xaml");
        var dataExchangeVm = Read("src", "CongTy.Desktop", "Operations", "DataExchangeViewModel.cs");
        var dataExchangeShell = Read("src", "CongTy.Desktop", "Shell", "MainWindow.DataExchange.cs");
        var shell = Read("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shellVm = Read("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var quotationShell = Read("src", "CongTy.Desktop", "Shell", "ShellViewModel.SalesQuotation.cs");

        Assert.IsFalse(dataExchangeView.Contains("Header=\"Báo giá\"", StringComparison.Ordinal));
        Assert.IsFalse(dataExchangeVm.Contains("BuildQuotationAsync", StringComparison.Ordinal));
        Assert.IsFalse(dataExchangeVm.Contains("QuotationRows", StringComparison.Ordinal));
        StringAssert.Contains(dataExchangeVm, "Math.Clamp(value, 0, 4)");
        StringAssert.Contains(dataExchangeVm, "public bool IsOfficeFormsTab => SelectedTabIndex == 4;");
        Assert.IsFalse(dataExchangeShell.Contains("Nhập/xuất dữ liệu và báo giá", StringComparison.Ordinal));
        StringAssert.Contains(dataExchangeShell, "\"Nhập/xuất dữ liệu\"");

        StringAssert.Contains(shell, "Click=\"SalesQuotation_OnClick\"");
        StringAssert.Contains(shell, "<TextBlock Text=\"Báo giá\" />");
        StringAssert.Contains(shellVm, "\"sales.quotations\" => \"Báo giá\"");
        StringAssert.Contains(quotationShell, "SetSelectedNavigation(\"sales.quotations\")");
        StringAssert.Contains(quotationShell, "WorkspaceSlots.SalesQuotation");
    }

    [TestMethod]
    public void Quotation_UsesLiveWebReadContractsAndBounds()
    {
        var service = Read("src", "CongTy.ApiClient", "DataExchangeService.cs");

        StringAssert.Contains(service, "QuotationMaxProductOffset = 10_000");
        StringAssert.Contains(service, "/api/products?active=true&limit={QuotationProductPageSize}&offset={offset}");
        StringAssert.Contains(service, "QuotationVariantBatchSize = 500");
        StringAssert.Contains(service, "/api/products/variants/query");
        StringAssert.Contains(service, "/api/sales-orders/sku-search?search=");
        StringAssert.Contains(service, "limit=30&offset=0");
        StringAssert.Contains(service, "/api/file-operations/quotation");
    }

    [TestMethod]
    public void Quotation_DeniesByDefaultForEveryBackendReadItNeeds()
    {
        var shell = Read("src", "CongTy.Desktop", "Shell", "ShellViewModel.SalesQuotation.cs");
        var vm = Read("src", "CongTy.Desktop", "Sales", "SalesQuotationViewModel.cs");

        foreach (var permission in new[] { "core.sales-order.read", "core.product.read", "core.price.read", "core.customer.read" })
        {
            StringAssert.Contains(shell, permission);
            StringAssert.Contains(vm, permission);
        }
    }

    [TestMethod]
    public void Quotation_RetryReusesCanonicalKeyAndInputChangesInvalidateIt()
    {
        var vm = Read("src", "CongTy.Desktop", "Sales", "SalesQuotationViewModel.cs");

        StringAssert.Contains(vm, "_operationKey ??= _idempotencyKeys.Create(\"sales-quotation\")");
        StringAssert.Contains(vm, "BuildQuotationAsync(request, _operationKey)");
        StringAssert.Contains(vm, "private void InvalidateResult()");
        StringAssert.Contains(vm, "_operationKey = null;");
        StringAssert.Contains(vm, "catch (Exception exception)");
        StringAssert.Contains(vm, "SetMessage(string.IsNullOrWhiteSpace(exception.Message) ? \"Không tính được báo giá.\"");
    }

    [TestMethod]
    public void Quotation_UsesCurrentWebCustomerSkuPickerAndLocalManualPriceOverride()
    {
        var view = Read("src", "CongTy.Desktop", "Sales", "SalesQuotationView.xaml");
        var vm = Read("src", "CongTy.Desktop", "Sales", "SalesQuotationViewModel.cs");

        StringAssert.Contains(view, "Tìm khách hàng");
        StringAssert.Contains(view, "Tìm sản phẩm / mã hàng");
        StringAssert.Contains(view, "Giá hệ thống");
        StringAssert.Contains(view, "không thay đổi bảng giá Công Ty");
        StringAssert.Contains(vm, "SelectedSkus");
        StringAssert.Contains(vm, "Eligibility.Selectable");
        StringAssert.Contains(vm, "HasManualPrice");
        StringAssert.Contains(vm, "LineTotalForPrice");
        StringAssert.Contains(vm, "Giá chỉnh trên báo giá");
        Assert.IsFalse(vm.Contains("/api/pricing/import", StringComparison.Ordinal));
        Assert.IsFalse(vm.Contains("CreatePrice", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Quotation_ExportsDisplayedEffectiveRowsToExcelAndCsv()
    {
        var vm = Read("src", "CongTy.Desktop", "Sales", "SalesQuotationViewModel.cs");
        var helper = Read("src", "CongTy.Desktop", "Operations", "DataExchangeFileHelper.cs");

        foreach (var header in new[] { "Mã hàng", "Sản phẩm", "Quy cách", "Số lượng", "Tiền tệ", "Đơn giá", "Thành tiền", "Nguồn giá" })
            StringAssert.Contains(vm, header);
        StringAssert.Contains(vm, "row.EffectiveUnitPrice");
        StringAssert.Contains(vm, "row.EffectiveLineTotal");
        StringAssert.Contains(vm, "row.PriceSource");
        StringAssert.Contains(vm, "DataExchangeFileHelper.Write");
        StringAssert.Contains(vm, "format is not (\"xlsx\" or \"csv\")");
        StringAssert.Contains(helper, "GuardFormula");
    }

    private static string Read(params string[] parts)
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
