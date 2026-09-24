using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Sales;

internal static class SalesOrderPrintPreview
{
    private static readonly Thickness PrintPadding = new(24, 18, 24, 24);
    public static void Show(
        Window? owner,
        SalesOrderData order,
        SalesOrderVersionData version,
        DocumentPrintTemplateData template)
    {
        try
        {
            ShowCore(owner, order, version, template);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            ShowWarning(owner, "Không mở được bản xem trước của đơn bán hàng. Vui lòng thử lại.");
        }
    }

    public static void ShowBatch(
        Window? owner,
        IReadOnlyList<(SalesOrderData Order, SalesOrderVersionData Version)> items,
        DocumentPrintTemplateData template)
    {
        if (items.Count == 0) return;
        try
        {
            var document = BuildBatchDocument(items, template);
            var viewer = CreateViewer(document);
            var printButton = new Button { Content = "In…", MinWidth = 90, Margin = new Thickness(4) };
            var closeButton = new Button { Content = "Đóng", MinWidth = 90, Margin = new Thickness(4) };
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(8, 6, 8, 2)
            };
            actions.Children.Add(printButton);
            actions.Children.Add(closeButton);
            var layout = new DockPanel();
            DockPanel.SetDock(actions, Dock.Top);
            layout.Children.Add(actions);
            layout.Children.Add(viewer);
            var window = new Window
            {
                Title = $"Xem trước in · {items.Count:N0} đơn",
                Width = 1120,
                Height = 840,
                MinWidth = 860,
                MinHeight = 640,
                Content = layout,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (owner is not null) window.Owner = owner;
            printButton.Click += (_, _) => TryPrintBatch(window, document, items.Count, template);
            closeButton.Click += (_, _) => window.Close();
            window.ShowDialog();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            ShowWarning(owner, "Không mở được bản xem trước các đơn đã chọn. Vui lòng thử lại.");
        }
    }

    private static void ShowCore(
        Window? owner,
        SalesOrderData order,
        SalesOrderVersionData version,
        DocumentPrintTemplateData template)
    {
        var document = BuildDocument(order, version, template);
        var viewer = CreateViewer(document);
        var printButton = new Button { Content = "In…", MinWidth = 90, Margin = new Thickness(4) };
        var closeButton = new Button { Content = "Đóng", MinWidth = 90, Margin = new Thickness(4) };
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8, 6, 8, 2)
        };
        actions.Children.Add(printButton);
        actions.Children.Add(closeButton);
        var layout = new DockPanel();
        DockPanel.SetDock(actions, Dock.Top);
        layout.Children.Add(actions);
        layout.Children.Add(viewer);
        var window = new Window
        {
            Title = $"Xem trước in · {SalesPresentation.Number(order.Number)}",
            Width = 1120,
            Height = 840,
            MinWidth = 860,
            MinHeight = 640,
            Content = layout,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        if (owner is not null) window.Owner = owner;
        printButton.Click += (_, _) => TryPrint(window, document, order.Number, template);
        closeButton.Click += (_, _) => window.Close();
        window.ShowDialog();
    }

    private static FlowDocument BuildDocument(
        SalesOrderData order,
        SalesOrderVersionData version,
        DocumentPrintTemplateData template)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template, baseFontSize: 10.5, padding: PrintPadding);
        AppendOrder(document, order, version, template, false);
        return document;
    }

    private static FlowDocument BuildBatchDocument(
        IReadOnlyList<(SalesOrderData Order, SalesOrderVersionData Version)> items,
        DocumentPrintTemplateData template)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template, baseFontSize: 10.5, padding: PrintPadding);
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            AppendOrder(document, item.Order, item.Version, template, index > 0);
        }
        return document;
    }

    private static void AppendOrder(
        FlowDocument document,
        SalesOrderData order,
        SalesOrderVersionData version,
        DocumentPrintTemplateData template,
        bool pageBreak)
    {
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            "PHIẾU XUẤT KHO",
            null,
            fallbackHeading: "Hưng Phát",
            breakPageBefore: pageBreak);

        document.Blocks.Add(new Paragraph
        {
            FontSize = 1,
            Margin = new Thickness(0, 0, 0, 8),
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0, 0, 0, 2)
        });

        var customer = version.CustomerMode == "WALK_IN"
            ? Display(version.WalkInDisplayName, "Khách vãng lai")
            : Display(version.CustomerName);
        var customerCode = version.CustomerMode == "WALK_IN" ? "Khách vãng lai" : Display(version.CustomerCode);
        var phone = version.CustomerMode == "WALK_IN" ? version.WalkInPhone : version.CustomerAddress?.Phone;
        var documentDate = Date(version.ConfirmedAt ?? version.CreatedAt);
        var weight = string.IsNullOrWhiteSpace(version.TotalWeightKg)
            ? "Chưa đủ dữ liệu"
            : $"{SalesPresentation.Quantity(version.TotalWeightKg)} kg";

        var meta = BuildMetaTable(
            template,
            [
                new("customer", "Khách hàng", customer, false),
                new("document_date", "Ngày đơn", documentDate, false),
                new("total_weight", "Khối lượng", weight, true),
                new("customer_code", "Mã khách", customerCode, false),
                new("phone", "Điện thoại", Display(phone), false),
                new("address", "Địa chỉ", Address(version.CustomerAddress), true),
                new("warehouse", "Kho", JoinCodeName(version.WarehouseCode, version.WarehouseName), false),
                new("delivery_method", "Hình thức giao nhận", version.DeliveryMode == "PICKUP"
                    ? "Mua tại quầy"
                    : version.DeliveryExecutionMode == "MANUAL" ? "Giao thủ công" : "Giao theo chuyến", false),
                new("collection_policy", "Thanh toán", SalesPresentation.CollectionPolicy(version.CollectionPolicy), false),
                new("requested_delivery_date", "Ngày giao dự kiến", Date(version.RequestedDeliveryDate), false)
            ]);
        if (meta is not null) document.Blocks.Add(meta);

        var lines = BuildLines(version, template);
        if (lines is not null) document.Blocks.Add(lines);

        var totals = BuildTotals(version, template);
        if (totals is not null) document.Blocks.Add(totals);

        if (DocumentPrintTemplateRuntime.Shows(template, "note") && !string.IsNullOrWhiteSpace(version.Note))
        {
            document.Blocks.Add(new Paragraph
            {
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Inlines = { new Bold(new Run("Ghi chú: ")), new Run(version.Note.Trim()) }
            });
        }

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người lập", "Kho giao hàng", "Khách hàng");
        var footerMargin = Math.Clamp(120 - (version.Lines?.Length ?? 0) * 5, 12, 90);
        document.Blocks.Add(new Paragraph(new Run($"{customer.ToUpperInvariant()} - {documentDate}"))
        {
            Margin = new Thickness(0, footerMargin, 0, 0),
            TextAlignment = TextAlignment.Center,
            FontSize = 8,
            Foreground = Brushes.DimGray
        });
    }

    private static Table? BuildLines(SalesOrderVersionData version, DocumentPrintTemplateData template)
    {
        var columns = new List<PrintColumn>
        {
            new("line_no", "STT", 0.65, TextAlignment.Center, line => line.LineNumber.ToString(CultureInfo.InvariantCulture)),
            new("line_item", "Tên sản phẩm", 4.2, TextAlignment.Left, line => Display(line.ItemName), true),
            new("line_sku", "SKU", 1.8, TextAlignment.Left, line => Display(line.Sku)),
            new("line_quantity", "SL", 0.9, TextAlignment.Right, line => SalesPresentation.Quantity(line.Quantity)),
            new("line_unit", "ĐVT", 1.05, TextAlignment.Center, line => Display(line.UnitName ?? line.UnitCode)),
            new("line_unit_price", "Đơn giá", 1.55, TextAlignment.Right, line => MoneyNumber(line.UnitPrice)),
            new("line_discount", "CK", 1.35, TextAlignment.Right, line => MoneyNumber(line.DiscountAmount)),
            new("line_tax", "Thuế", 1.25, TextAlignment.Right, line => MoneyNumber(line.TaxAmount)),
            new("line_total", "Thành tiền", 1.65, TextAlignment.Right, line => MoneyNumber(line.LineTotal), true)
        }
        .Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key))
        .Where(column => column.Key != "line_discount" || ShowDiscount(version))
        .Where(column => column.Key != "line_tax" || ShowTax(version))
        .ToList();

        if (columns.Count == 0) return null;

        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 2, 0, 8)
        };
        foreach (var column in columns)
            table.Columns.Add(new TableColumn { Width = new GridLength(column.Weight, GridUnitType.Star) });

        var group = new TableRowGroup();
        var header = new TableRow();
        foreach (var column in columns)
            header.Cells.Add(LineCell(column.Header, column.Alignment, bold: true, isHeader: true));
        group.Rows.Add(header);

        foreach (var line in (version.Lines ?? []).OrderBy(item => item.LineNumber))
        {
            var row = new TableRow();
            foreach (var column in columns)
                row.Cells.Add(LineCell(column.Value(line), column.Alignment, column.Bold));
            group.Rows.Add(row);
        }

        table.RowGroups.Add(group);
        return table;
    }

    private static TableCell LineCell(string value, TextAlignment alignment, bool bold = false, bool isHeader = false)
    {
        var run = new Run(Display(value));
        if (bold) run.FontWeight = FontWeights.SemiBold;
        return new TableCell(new Paragraph(run)
        {
            Margin = new Thickness(0),
            TextAlignment = alignment,
            FontSize = isHeader ? 10 : 10.5
        })
        {
            Background = isHeader ? Brushes.Gainsboro : Brushes.Transparent,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0.65),
            Padding = new Thickness(5, 5, 5, 5)
        };
    }

    private static Table? BuildMetaTable(
        DocumentPrintTemplateData template,
        IReadOnlyList<PrintMeta> source)
    {
        var items = source.Where(item => DocumentPrintTemplateRuntime.Shows(template, item.Key)).ToList();
        if (items.Count == 0) return null;

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(88) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(88) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var group = new TableRowGroup();
        for (var index = 0; index < items.Count;)
        {
            var first = items[index++];
            var row = new TableRow();
            if (first.Full)
            {
                row.Cells.Add(MetaCell(first.Label, false));
                var valueCell = MetaCell(first.Value, true);
                valueCell.ColumnSpan = 3;
                row.Cells.Add(valueCell);
            }
            else
            {
                row.Cells.Add(MetaCell(first.Label, false));
                row.Cells.Add(MetaCell(first.Value, true));
                if (index < items.Count && !items[index].Full)
                {
                    var second = items[index++];
                    row.Cells.Add(MetaCell(second.Label, false));
                    row.Cells.Add(MetaCell(second.Value, true));
                }
                else
                {
                    row.Cells.Add(MetaCell(string.Empty, false));
                    row.Cells.Add(MetaCell(string.Empty, true));
                }
            }
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);
        return table;
    }

    private static TableCell MetaCell(string value, bool bold)
    {
        var run = new Run(Display(value, string.Empty));
        if (bold) run.FontWeight = FontWeights.SemiBold;
        else run.Foreground = Brushes.DimGray;
        return new TableCell(new Paragraph(run) { Margin = new Thickness(0), FontSize = 10.5 })
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 0, 0.65),
            Padding = new Thickness(0, 3, 8, 4)
        };
    }

    private static Table? BuildTotals(SalesOrderVersionData version, DocumentPrintTemplateData template)
    {
        var rows = new List<PrintTotal>();
        if (DocumentPrintTemplateRuntime.Shows(template, "total_subtotal"))
            rows.Add(new("Tạm tính", $"{MoneyNumber(version.Subtotal)} ₫", false));
        if (ShowDiscount(version) && DocumentPrintTemplateRuntime.Shows(template, "total_discount"))
            rows.Add(new("Chiết khấu", $"{MoneyNumber(version.DiscountTotal)} ₫", false));
        if (ShowTax(version) && DocumentPrintTemplateRuntime.Shows(template, "total_tax"))
            rows.Add(new("Thuế", $"{MoneyNumber(version.TaxTotal)} ₫", false));
        if (DocumentPrintTemplateRuntime.Shows(template, "total_total"))
            rows.Add(new("TỔNG CỘNG", $"{MoneyNumber(version.Total)} ₫", true));
        if (rows.Count == 0) return null;

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 2, 0, 0) };
        table.Columns.Add(new TableColumn { Width = new GridLength(4.2, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.25, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.55, GridUnitType.Star) });
        var group = new TableRowGroup();

        foreach (var item in rows)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph { Margin = new Thickness(0) }));
            row.Cells.Add(TotalCell(item.Label, TextAlignment.Left, item.Emphasis));
            row.Cells.Add(TotalCell(item.Value, TextAlignment.Right, true, item.Emphasis));
            group.Rows.Add(row);
        }

        table.RowGroups.Add(group);
        return table;
    }

    private static TableCell TotalCell(
        string value,
        TextAlignment alignment,
        bool bold,
        bool topRule = false)
    {
        var run = new Run(value);
        if (bold) run.FontWeight = FontWeights.Bold;
        return new TableCell(new Paragraph(run)
        {
            Margin = new Thickness(0),
            TextAlignment = alignment,
            FontSize = topRule ? 12.5 : 10.5
        })
        {
            BorderBrush = Brushes.Black,
            BorderThickness = topRule ? new Thickness(0, 1.5, 0, 0) : new Thickness(0),
            Padding = new Thickness(3, topRule ? 7 : 3, 3, 3)
        };
    }

    private static FlowDocumentPageViewer CreateViewer(FlowDocument document) =>
        new()
        {
            Document = document,
            Margin = new Thickness(8),
            Zoom = 95,
            MinZoom = 50,
            MaxZoom = 180
        };

    private static string MoneyNumber(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        return amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    }

    private sealed record PrintColumn(
        string Key,
        string Header,
        double Weight,
        TextAlignment Alignment,
        Func<SalesOrderLineData, string> Value,
        bool Bold = false);

    private sealed record PrintMeta(string Key, string Label, string Value, bool Full);
    private sealed record PrintTotal(string Label, string Value, bool Emphasis);

    private static string Address(SalesOrderAddressData? address)
    {
        if (address is null) return "—";
        var parts = new[] { address.AddressLine1, address.AddressLine2, address.Ward, address.District, address.Province }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
        return Display(string.Join(", ", parts));
    }

    private static string JoinCodeName(string? code, string? name)
    {
        var parts = new[] { code, name }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim());
        return Display(string.Join(" · ", parts));
    }

    private static bool ShowDiscount(SalesOrderVersionData version) =>
        !IsZeroAmount(version.DiscountTotal)
        || (version.Lines ?? []).Any(line => !IsZeroAmount(line.DiscountAmount));

    private static bool ShowTax(SalesOrderVersionData version) =>
        !IsZeroAmount(version.TaxTotal)
        || (version.Lines ?? []).Any(line => !IsZeroAmount(line.TaxAmount));

    private static bool IsZeroAmount(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) && amount == 0m;

    private static string Display(string? value, string fallback = "—") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"))
            : value.Trim();
    }

    private static void TryPrint(
        Window owner,
        FlowDocument document,
        string? number,
        DocumentPrintTemplateData template)
    {
        try
        {
            var dialog = new PrintDialog();
            DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
            if (dialog.ShowDialog() != true) return;
            DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template, PrintPadding);
            dialog.PrintDocument(
                ((IDocumentPaginatorSource)document).DocumentPaginator,
                $"Phiếu xuất kho {SalesPresentation.Number(number)}");
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            ShowWarning(owner, "Không in được đơn bán hàng. Vui lòng kiểm tra máy in rồi thử lại.");
        }
    }

    private static void TryPrintBatch(
        Window owner,
        FlowDocument document,
        int count,
        DocumentPrintTemplateData template)
    {
        try
        {
            var dialog = new PrintDialog();
            DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
            if (dialog.ShowDialog() != true) return;
            DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template, PrintPadding);
            dialog.PrintDocument(
                ((IDocumentPaginatorSource)document).DocumentPaginator,
                $"Phiếu xuất kho · {count:N0} đơn");
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            ShowWarning(owner, "Không in được các đơn đã chọn. Vui lòng kiểm tra máy in rồi thử lại.");
        }
    }

    private static void ShowWarning(Window? owner, string message)
    {
        if (owner is not null)
        {
            MessageBox.Show(owner, message, "In đơn bán hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show(message, "In đơn bán hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
