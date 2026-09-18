namespace CongTy.UnitTests;

[TestClass]
public sealed class PrintTemplateRuntimeParityTests
{
    [TestMethod]
    public void Runtime_UsesCanonicalTemplateServiceAndNativePageSizes()
    {
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");
        var runtime = ReadRepoFile("src", "CongTy.Desktop", "Printing", "DocumentPrintTemplateRuntime.cs");

        StringAssert.Contains(app, "AddSingleton<IDocumentPrintTemplateService, DocumentPrintTemplateService>()");
        StringAssert.Contains(runtime, "IDocumentPrintTemplateService");
        StringAssert.Contains(runtime, "LoadForPrintAsync");
        StringAssert.Contains(runtime, "PageMediaSizeName.ISOA4");
        StringAssert.Contains(runtime, "PageMediaSizeName.ISOA5");
        StringAssert.Contains(runtime, "template.VisibleFieldKeys.Contains");
        StringAssert.Contains(runtime, "template.HeadingVisible");
        StringAssert.Contains(runtime, "template.HeadingAlign");
        StringAssert.Contains(runtime, "template.TitleAlign");
    }

    [TestMethod]
    public void EveryCanonicalDesktopPrintSurface_LoadsItsInstallationTemplate()
    {
        var expected = new (string[] Path, string Marker)[]
        {
            (["src", "CongTy.Desktop", "Sales", "SalesView.xaml.cs"], "\"SALES_ORDER\""),
            (["src", "CongTy.Desktop", "Purchasing", "PurchaseOrderView.xaml.cs"], "\"PURCHASE_ORDER\""),
            (["src", "CongTy.Desktop", "Purchasing", "GoodsReceiptView.xaml.cs"], "\"GOODS_RECEIPT\""),
            (["src", "CongTy.Desktop", "Accounting", "CustomerPaymentView.xaml.cs"], "\"CUSTOMER_PAYMENT\""),
            (["src", "CongTy.Desktop", "Logistics", "DeliveryOrderView.xaml.cs"], "\"DELIVERY_ORDER\""),
            (["src", "CongTy.Desktop", "Inventory", "TransferView.xaml.cs"], "\"INVENTORY_TRANSFER\""),
            (["src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml.cs"], "\"STOCKTAKE\""),
            (["src", "CongTy.Desktop", "Logistics", "TripDispatchView.xaml.cs"], "\"DELIVERY_TRIP\""),
            (["src", "CongTy.Desktop", "Logistics", "TripReconciliationView.xaml.cs"], "\"TRIP_RECONCILIATION\"")
        };

        foreach (var item in expected)
        {
            var source = ReadRepoFile(item.Path);
            StringAssert.Contains(source, "DocumentPrintTemplateRuntime.LoadForPrintAsync");
            StringAssert.Contains(source, item.Marker);
        }

        var delivery = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryOrderView.xaml.cs");
        StringAssert.Contains(delivery, "\"packing-list\"");
        StringAssert.Contains(delivery, "\"standard\"");
    }

    [TestMethod]
    public void CanonicalBuilders_RespectVisibleFieldsAndSharedHeaders()
    {
        var builders = new[]
        {
            ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesOrderPrintPreview.cs"),
            ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchaseOrderPrintPreview.cs"),
            ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptPrintPreview.cs"),
            ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CustomerPaymentPrintPreview.cs"),
            ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryOrderPrintPreview.cs"),
            ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryTransferPrintPreview.cs"),
            ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakePrintPreview.cs"),
            ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripSheetPrinter.cs"),
            ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripReconciliationPrinter.cs")
        };

        foreach (var builder in builders)
        {
            StringAssert.Contains(builder, "DocumentPrintTemplateRuntime.AddHeader");
            StringAssert.Contains(builder, "DocumentPrintTemplateRuntime.Shows");
        }
    }

    [TestMethod]
    public void Printing_MatchesCurrentWebGuardrailsAndDocumentSpecificRules()
    {
        var sales = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesOrderPrintPreview.cs");
        var purchase = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchaseOrderPrintPreview.cs");
        var goods = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptViewModel.cs");
        var transfer = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "TransferViewModel.cs");
        var stocktake = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeViewModel.cs");
        var reconciliation = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripReconciliationPrinter.cs");

        StringAssert.Contains(sales, "ShowDiscount(version)");
        StringAssert.Contains(sales, "ShowTax(version)");
        StringAssert.Contains(purchase, "ShowDiscount(order)");
        StringAssert.Contains(purchase, "ShowTax(order)");

        StringAssert.Contains(goods, "receipt.Status != \"draft\"");
        StringAssert.Contains(goods, "!string.IsNullOrWhiteSpace(receipt.DocumentNumber)");
        StringAssert.Contains(transfer, "SelectedTransfer.Status != \"draft\"");
        StringAssert.Contains(transfer, "!string.IsNullOrWhiteSpace(SelectedTransfer.DocumentNumber)");
        StringAssert.Contains(stocktake, "SelectedStocktake?.Status != \"draft\"");

        StringAssert.Contains(reconciliation, "Đã đối chiếu đủ điều kiện đóng chuyến.");
        StringAssert.Contains(reconciliation, "Còn hàng/chứng từ cần đối chiếu trước khi đóng chuyến.");
        StringAssert.Contains(reconciliation, "\"Điều phối\", \"Thủ kho\", \"Tài xế\"");
    }

    [TestMethod]
    public void LegacyInlinePrintCode_IsRemovedWhereCanonicalTemplatesNowApply()
    {
        var customerPayment = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CustomerPaymentView.xaml.cs");
        var delivery = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryOrderView.xaml.cs");
        var transfer = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "TransferView.xaml.cs");
        var stocktake = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml.cs");

        Assert.IsFalse(customerPayment.Contains("new FlowDocument", StringComparison.Ordinal));
        Assert.IsFalse(delivery.Contains("new FlowDocument", StringComparison.Ordinal));
        Assert.IsFalse(transfer.Contains("BuildPrintDocument", StringComparison.Ordinal));
        Assert.IsFalse(stocktake.Contains("PrintVisual", StringComparison.Ordinal));

        var supplierReturn = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "SupplierReturnPrintPreview.cs");
        Assert.IsFalse(supplierReturn.Contains("DocumentPrintTemplateRuntime", StringComparison.Ordinal));
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
