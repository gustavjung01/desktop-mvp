using CongTy.Desktop.Operations;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WebDeltaLot1ParityTests
{
    [TestMethod]
    public void InventoryHistory_ConsumesBusinessContextAndOwnsMultiSkuExport()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "InventoryContracts.cs");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLookupViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLookupView.xaml");

        StringAssert.Contains(contracts, "[JsonPropertyName(\"customer_code\")]");
        StringAssert.Contains(contracts, "[JsonPropertyName(\"customer_name\")]");
        StringAssert.Contains(contracts, "[JsonPropertyName(\"sales_order_number\")]");
        StringAssert.Contains(vm, "MaxHistoryExportSkus = 20");
        StringAssert.Contains(vm, "LoadAllHistoryForExportAsync");
        StringAssert.Contains(vm, "OfficeDataExportFile.Xlsx");
        StringAssert.Contains(vm, "\"Đơn bán hàng\", \"Mã chứng từ\"");
        StringAssert.Contains(view, "Content=\"Xuất lịch sử…\"");
        StringAssert.Contains(view, "Header=\"Đơn / chứng từ\"");
        StringAssert.Contains(view, "Header=\"Khách hàng\"");
        StringAssert.Contains(view, "Text=\"{Binding DetailSalesOrder}\"");
        StringAssert.Contains(view, "Text=\"{Binding DetailCustomer}\"");
    }

    [TestMethod]
    public void SharedOfficeExport_TrimsOnlyRedundantDecimalScale()
    {
        Assert.AreEqual("12", OfficeDataExportFile.FormatOfficeExportValue("12.000000000000"));
        Assert.AreEqual("-240", OfficeDataExportFile.FormatOfficeExportValue("-240.000000000000"));
        Assert.AreEqual("12.34", OfficeDataExportFile.FormatOfficeExportValue("12.340000000000"));
        Assert.AreEqual("-0.5", OfficeDataExportFile.FormatOfficeExportValue("-0.500000000000"));
        Assert.AreEqual("0", OfficeDataExportFile.FormatOfficeExportValue("-0.000000000000"));
        Assert.AreEqual("00123", OfficeDataExportFile.FormatOfficeExportValue("00123"));
        Assert.AreEqual("SO-202609-000948", OfficeDataExportFile.FormatOfficeExportValue("SO-202609-000948"));
    }

    [TestMethod]
    public void SalesOrderPrint_AlreadyTreatsEveryLineFieldAsIndependentVisibility()
    {
        var print = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesOrderPrintPreview.cs");
        var settings = ReadRepoFile("src", "CongTy.Desktop", "Settings", "PrintTemplatesViewModel.cs");

        foreach (var key in new[] { "line_item", "line_sku", "line_quantity", "line_unit", "line_unit_price", "line_total" })
            StringAssert.Contains(print, $"new(\"{key}\"");

        StringAssert.Contains(print, ".Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key))");
        StringAssert.Contains(settings, "Required = field.Required");
        StringAssert.Contains(settings, "public bool CanToggle => !Required");
    }

    [TestMethod]
    public void SalesEntry_UsesBackendPricingAuthorityAndCancelsStaleAddressReads()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesView.xaml");

        StringAssert.Contains(vm, "_service.ResolvePriceAsync");
        StringAssert.Contains(vm, "_addressLoadCts?.Cancel()");
        StringAssert.Contains(vm, "ListCustomerAddressesAsync(customerId,token)");
        StringAssert.Contains(vm, "DraftCustomerId,expectedCustomerId");
        StringAssert.Contains(view, "Text=\"{Binding DraftNote}\"");
        Assert.IsFalse(view.Contains("Text=\"{Binding DraftNote, UpdateSourceTrigger=PropertyChanged}\"", StringComparison.Ordinal));
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
