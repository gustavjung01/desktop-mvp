namespace CongTy.UnitTests;

[TestClass]
public sealed class Issue85Lot3ActualDocumentPrintParityTests
{
    [TestMethod]
    public void Lot3_WiresSevenActualDocumentsToCanonicalTemplateRuntime()
    {
        var surfaces = new (string[] Path, string Type)[]
        {
            (["src", "CongTy.Desktop", "Inventory", "ManualInboundView.xaml.cs"], "MANUAL_INBOUND"),
            (["src", "CongTy.Desktop", "Purchasing", "SupplierReturnView.xaml.cs"], "SUPPLIER_RETURN"),
            (["src", "CongTy.Desktop", "Logistics", "CustomerReturnView.xaml.cs"], "CUSTOMER_RETURN"),
            (["src", "CongTy.Desktop", "Inventory", "FulfillmentView.xaml.cs"], "FULFILLMENT_PICKING"),
            (["src", "CongTy.Desktop", "Accounting", "SupplierPaymentView.xaml.cs"], "SUPPLIER_PAYMENT"),
            (["src", "CongTy.Desktop", "Accounting", "CustomerReturnCreditView.xaml.cs"], "CUSTOMER_REFUND"),
            (["src", "CongTy.Desktop", "Accounting", "CodAccountingView.xaml.cs"], "COD_RECONCILIATION")
        };

        foreach (var (path, type) in surfaces)
        {
            var source = Read(path);
            StringAssert.Contains(source, "DocumentPrintTemplateRuntime.LoadForPrintAsync");
            StringAssert.Contains(source, string.Concat("\"", type, "\""));
        }
    }

    [TestMethod]
    public void Lot3_BuilderLocksWebDocumentTitlesFieldsAndSignatures()
    {
        var source = Read("src", "CongTy.Desktop", "Printing", "ActualDocumentPrintPreview.cs");

        foreach (var title in new[]
        {
            "PHIẾU NHẬP KHO",
            "PHIẾU TRẢ HÀNG NHÀ CUNG CẤP",
            "PHIẾU NHẬN HÀNG KHÁCH TRẢ",
            "PHIẾU SOẠN HÀNG / CẤP HÀNG",
            "PHIẾU CHI / THANH TOÁN NHÀ CUNG CẤP",
            "PHIẾU HOÀN TIỀN KHÁCH HÀNG",
            "BIÊN BẢN ĐỐI SOÁT COD"
        })
            StringAssert.Contains(source, title);

        foreach (var field in new[]
        {
            "\"inbound_type\"", "\"source_receipt\"", "\"source_document\"",
            "\"line_allocated\"", "\"total_paid\"", "\"total_refund\"", "\"accepted_total\""
        })
            StringAssert.Contains(source, field);

        foreach (var signature in new[]
        {
            "Người lập phiếu", "Thủ kho", "Người kiểm nhận", "Người soạn hàng",
            "Kế toán / Thủ quỹ", "Khách hàng / Người nhận", "Tài xế / Người bàn giao"
        })
            StringAssert.Contains(source, signature);
    }

    [TestMethod]
    public void Lot3_LocksWebPrintGuardrailsAndA5Fallback()
    {
        var supplierReturnVm = Read("src", "CongTy.Desktop", "Purchasing", "SupplierReturnViewModel.cs");
        var customerReturnCode = Read("src", "CongTy.Desktop", "Logistics", "CustomerReturnView.xaml.cs");
        var fulfillment = Read("src", "CongTy.Desktop", "Inventory", "FulfillmentPresentation.cs");
        var runtime = Read("src", "CongTy.Desktop", "Printing", "DocumentPrintTemplateRuntime.cs");

        StringAssert.Contains(supplierReturnVm, "item.Status != \"draft\"");
        StringAssert.Contains(supplierReturnVm, "!string.IsNullOrWhiteSpace(item.DocumentNumber)");
        StringAssert.Contains(customerReturnCode, "\"received\"");
        StringAssert.Contains(customerReturnCode, "string.IsNullOrWhiteSpace(item.Number)");
        StringAssert.Contains(fulfillment, "AllocatedBaseQuantity");
        StringAssert.Contains(fulfillment, "PickedBaseQuantity");
        StringAssert.Contains(fulfillment, "PackedBaseQuantity");
        StringAssert.Contains(runtime, "\"SUPPLIER_PAYMENT\"");
        StringAssert.Contains(runtime, "\"CUSTOMER_REFUND\"");
    }

    [TestMethod]
    public void Lot3_PrintBuilderIsReadOnlyAndTemplateDriven()
    {
        var source = Read("src", "CongTy.Desktop", "Printing", "ActualDocumentPrintPreview.cs");

        StringAssert.Contains(source, "DocumentPrintTemplateRuntime.AddHeader");
        StringAssert.Contains(source, "DocumentPrintTemplateRuntime.Shows");
        StringAssert.Contains(source, "DocumentPrintTemplateRuntime.AddSignatures");
        Assert.IsFalse(source.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(source.Contains("PostAsync", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(source.Contains("PatchAsync", StringComparison.OrdinalIgnoreCase));
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

        Assert.Fail("Không tìm thấy tệp trong repo: " + string.Join("/", parts));
        return string.Empty;
    }
}
