namespace CongTy.UnitTests;

[TestClass]
public sealed class SalesOrderPrintParityTests
{
    [TestMethod]
    public void PrintPreview_PreservesWarehouseSlipDocumentContract()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesOrderPrintPreview.cs");

        StringAssert.Contains(source, "PHIẾU XUẤT KHO");
        StringAssert.Contains(source, "Phiếu xuất kho {SalesPresentation.Number(number)}");
        StringAssert.Contains(source, "FlowDocumentPageViewer");
        StringAssert.Contains(source, "PrintDialog");
        StringAssert.Contains(source, "DocumentPrintTemplateRuntime.CreateDocument(template, baseFontSize: 10.5, padding: PrintPadding)");
        StringAssert.Contains(source, "DocumentPrintTemplateRuntime.AddHeader");
        Assert.IsFalse(source.Contains("new DocumentViewer", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PrintPreview_GuardsBothPreviewAndPrinterFailures()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesOrderPrintPreview.cs");

        StringAssert.Contains(source, "ShowCore(owner, order, version, template)");
        StringAssert.Contains(source, "TryPrint(window, document, order.Number, template)");
        StringAssert.Contains(source, "Không mở được bản xem trước của đơn bán hàng");
        StringAssert.Contains(source, "Không in được đơn bán hàng");
        Assert.IsGreaterThanOrEqualTo(2, Count(source, "catch (Exception exception)"));
        Assert.IsFalse(source.Contains("exception.Message", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PrintPreview_UsesCanonicalTemplateAndCurrentSalesPrintRules()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesOrderPrintPreview.cs");

        StringAssert.Contains(source, "Display(version.WalkInDisplayName, \"Khách vãng lai\")");
        StringAssert.Contains(source, "JoinCodeName(version.WarehouseCode, version.WarehouseName)");
        StringAssert.Contains(source, "DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template, PrintPadding)");
        StringAssert.Contains(source, "fallbackHeading: \"Hưng Phát\"");
        StringAssert.Contains(source, "ShowDiscount(version)");
        StringAssert.Contains(source, "ShowTax(version)");
        StringAssert.Contains(source, "DocumentPrintTemplateRuntime.Shows(template, column.Key)");
        StringAssert.Contains(source, "BuildMetaTable");
        StringAssert.Contains(source, "MetaPairCell");
        StringAssert.Contains(source, "BuildTotals");
        StringAssert.Contains(source, "document.Background = Brushes.White");
        StringAssert.Contains(source, "table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) })");
        Assert.IsFalse(source.Contains("new TableColumn { Width = new GridLength(88) }", StringComparison.Ordinal));
        StringAssert.Contains(source, "GridUnitType.Star");
        StringAssert.Contains(source, "MoneyNumber(line.UnitPrice)");
        StringAssert.Contains(source, "MoneyNumber(line.LineTotal)");
        StringAssert.Contains(source, "Zoom = 95");
        StringAssert.Contains(source, "PrintPadding");
        StringAssert.Contains(source, "\"Khối lượng\"");
        Assert.IsFalse(source.Contains("\"Tổng khối lượng\"", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("DefaultColumnWidth", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("double.PositiveInfinity", StringComparison.Ordinal));
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
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
