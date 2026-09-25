using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using CongTy.Contracts;
using CongTy.Desktop.Sales;

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

    [TestMethod]
    public void PrintPreview_EightLineA4FixturePaginatesAsOneStablePage()
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                var document = BuildRuntimeFixtureDocument();
                Assert.AreEqual(793.700787d, document.PageWidth, 0.01d);
                Assert.AreEqual(1122.519685d, document.PageHeight, 0.01d);
                Assert.AreEqual(24d, document.PagePadding.Left, 0.01d);
                Assert.AreEqual(18d, document.PagePadding.Top, 0.01d);

                var tables = document.Blocks.OfType<Table>().ToList();
                Assert.IsGreaterThanOrEqualTo(3, tables.Count, "Phiếu phải có meta, bảng hàng và tổng cộng.");

                var meta = tables[0];
                Assert.AreEqual(2, meta.Columns.Count, "Meta phải giữ đúng hai cột 50/50 để không rơi chữ theo chiều dọc.");
                Assert.IsTrue(meta.Columns.All(column => column.Width.GridUnitType == GridUnitType.Star));
                Assert.AreEqual(meta.Columns[0].Width.Value, meta.Columns[1].Width.Value, 0.001d);
                var metaRows = meta.RowGroups.SelectMany(group => group.Rows).ToList();
                Assert.AreEqual(2, metaRows[0].Cells.Count, "Khách hàng và ngày đơn phải nằm trên cùng hàng hai ô.");
                Assert.AreEqual(2, metaRows[1].Cells[0].ColumnSpan, "Khối lượng phải chiếm trọn hai cột.");

                var lines = tables.Single(table =>
                    table.RowGroups.SelectMany(group => group.Rows).Count() == 9
                    && table.Columns.Count == 6);
                Assert.IsGreaterThan(lines.Columns[0].Width.Value * 5d, lines.Columns[1].Width.Value,
                    "Tên sản phẩm phải rộng hơn cột STT đủ lớn để tránh wrap làm tràn trang.");
                Assert.IsGreaterThan(lines.Columns[5].Width.Value * 2d, lines.Columns[1].Width.Value,
                    "Tên sản phẩm phải ưu tiên chiều rộng hơn cột thành tiền.");

                var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                paginator.ComputePageCount();
                Assert.AreEqual(1, paginator.PageCount, "Fixture 8 dòng tương đương ảnh thực tế phải nằm gọn một trang A4.");
                var page = paginator.GetPage(0);
                Assert.IsNotNull(page.Visual);
                Assert.IsGreaterThan(700d, page.Size.Width);
                Assert.IsGreaterThan(1000d, page.Size.Height);
            }
            catch (Exception exception)
            {
                captured = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(15)), "Render test WPF không được treo CI.");
        if (captured is not null) ExceptionDispatchInfo.Capture(captured).Throw();
    }

    private static FlowDocument BuildRuntimeFixtureDocument()
    {
        var type = typeof(OrderManagementPresentation).Assembly.GetType(
            "CongTy.Desktop.Sales.SalesOrderPrintPreview",
            throwOnError: true)!;
        var method = type.GetMethod("BuildDocument", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Không tìm thấy builder phiếu xuất kho.");

        var order = new SalesOrderData
        {
            Id = "order-layout-regression",
            Number = "SO-202609-000791",
            Status = "confirmed"
        };
        var names = new[]
        {
            "PUDING TRỨNG DOUXIAN",
            "PUDING SOCOLA DOUXIAN",
            "PUDING DƯA LƯỚI DOUXIAN",
            "SIRO MAMA DÂU",
            "SIRO MAMA VIỆT QUỐC",
            "ĐƯỜNG NƯỚC 25KG",
            "TRÀ OLONG LỘC PHÁT",
            "THẠCH DỪA DEDU"
        };
        var prices = new[] { "170000", "170000", "170000", "50000", "50000", "570000", "306000", "640000" };
        var units = new[] { "Bịch", "Bịch", "Bịch", "Chai", "Chai", "Thùng", "Bịch", "Thùng" };
        var lines = names.Select((name, index) => new SalesOrderLineData
        {
            Id = $"line-{index + 1}",
            LineNumber = index + 1,
            ItemName = name,
            Sku = $"SKU-{index + 1:00}",
            Quantity = "1",
            UnitName = units[index],
            UnitPrice = prices[index],
            DiscountAmount = "0",
            TaxAmount = "0",
            LineTotal = prices[index]
        }).ToArray();

        var version = new SalesOrderVersionData
        {
            Id = "version-layout-regression",
            CustomerMode = "EXISTING",
            CustomerCode = "KH000791",
            CustomerName = "NHÀ ĐẬU",
            WarehouseCode = "KHO-01",
            WarehouseName = "Kho chính",
            DeliveryMode = "DELIVERY",
            DeliveryExecutionMode = "MANUAL",
            CollectionPolicy = "COLLECT_ON_DELIVERY",
            CreatedAt = "2026-09-24T12:53:00+07:00",
            ConfirmedAt = "2026-09-24T12:53:00+07:00",
            TotalWeightKg = "48.06",
            Subtotal = "2126000",
            DiscountTotal = "0",
            TaxTotal = "0",
            Total = "2126000",
            Lines = lines
        };
        var template = new DocumentPrintTemplateData
        {
            DocumentType = "SALES_ORDER",
            TemplateCode = "layout-regression",
            PageSize = "A4",
            FontSizePercent = 100,
            HeadingVisible = false,
            TitleAlign = "center",
            VisibleFieldKeys =
            [
                "customer",
                "document_date",
                "total_weight",
                "line_no",
                "line_item",
                "line_quantity",
                "line_unit",
                "line_unit_price",
                "line_total",
                "total_total"
            ]
        };

        try
        {
            return (FlowDocument)(method.Invoke(null, [order, version, template])
                ?? throw new InvalidOperationException("Builder không trả về FlowDocument."));
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
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
