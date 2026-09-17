namespace CongTy.UnitTests;

[TestClass]
public sealed class SalesOrderPrintParityTests
{
    [TestMethod]
    public void PrintPreview_PreservesWarehouseSlipDocumentContract()
    {
        var source=ReadRepoFile("src","CongTy.Desktop","Sales","SalesOrderPrintPreview.cs");

        StringAssert.Contains(source,"PHIẾU XUẤT KHO");
        StringAssert.Contains(source,"Phiếu xuất kho {SalesPresentation.Number(number)}");
        StringAssert.Contains(source,"DocumentViewer");
        StringAssert.Contains(source,"PrintDialog");
    }

    [TestMethod]
    public void PrintPreview_GuardsBothPreviewAndPrinterFailures()
    {
        var source=ReadRepoFile("src","CongTy.Desktop","Sales","SalesOrderPrintPreview.cs");

        StringAssert.Contains(source,"ShowCore(owner,order,version)");
        StringAssert.Contains(source,"printButton.Click+=(_,_)=>TryPrint(window,document,order.Number)");
        StringAssert.Contains(source,"Không mở được bản xem trước của đơn bán hàng");
        StringAssert.Contains(source,"Không in được đơn bán hàng");
        Assert.IsGreaterThanOrEqualTo(2,Count(source,"catch(Exception exception)"));
        Assert.IsFalse(source.Contains("exception.Message",StringComparison.Ordinal));
    }

    [TestMethod]
    public void PrintPreview_NormalizesMissingBusinessDataAndPrinterPageSize()
    {
        var source=ReadRepoFile("src","CongTy.Desktop","Sales","SalesOrderPrintPreview.cs");

        StringAssert.Contains(source,"Display(version.WalkInDisplayName,\"Khách vãng lai\")");
        StringAssert.Contains(source,"JoinCodeName(version.WarehouseCode,version.WarehouseName)");
        StringAssert.Contains(source,"IsUsablePageSize(dialog.PrintableAreaWidth)");
        StringAssert.Contains(source,"IsUsablePageSize(dialog.PrintableAreaHeight)");
        StringAssert.Contains(source,"DefaultColumnWidth=720");
        StringAssert.Contains(source,"document.ColumnWidth=Math.Max(1,dialog.PrintableAreaWidth-document.PagePadding.Left-document.PagePadding.Right)");
        Assert.IsFalse(source.Contains("double.PositiveInfinity",StringComparison.Ordinal));
    }

    private static int Count(string source,string value)
    {
        var count=0;
        var index=0;
        while((index=source.IndexOf(value,index,StringComparison.Ordinal))>=0)
        {
            count++;
            index+=value.Length;
        }
        return count;
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory is not null)
        {
            var candidate=Path.Combine(new[]{directory.FullName}.Concat(parts).ToArray());
            if(File.Exists(candidate))return File.ReadAllText(candidate);
            directory=directory.Parent;
        }

        Assert.Fail($"Không tìm thấy tệp trong repo: {string.Join("/",parts)}");
        return string.Empty;
    }
}
