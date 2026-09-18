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
            var viewer = new FlowDocumentPageViewer { Document = document, Margin = new Thickness(8) };
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
                Width = 980,
                Height = 760,
                MinWidth = 760,
                MinHeight = 560,
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
        var viewer = new FlowDocumentPageViewer { Document = document, Margin = new Thickness(8) };
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
            Width = 980,
            Height = 760,
            MinWidth = 760,
            MinHeight = 560,
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
        var document = DocumentPrintTemplateRuntime.CreateDocument(template);
        AppendOrder(document, order, version, template, false);
        return document;
    }

    private static FlowDocument BuildBatchDocument(
        IReadOnlyList<(SalesOrderData Order, SalesOrderVersionData Version)> items,
        DocumentPrintTemplateData template)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template);
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

        var customer = version.CustomerMode == "WALK_IN"
            ? Display(version.WalkInDisplayName, "Khách vãng lai")
            : Display(version.CustomerName);
        var customerCode = version.CustomerMode == "WALK_IN" ? "Khách vãng lai" : Display(version.CustomerCode);
        var phone = version.CustomerMode == "WALK_IN" ? version.WalkInPhone : version.CustomerAddress?.Phone;

        AddInfoIf(document, template, "customer", "Khách hàng", customer);
        AddInfoIf(document, template, "customer_code", "Mã khách", customerCode);
        AddInfoIf(document, template, "phone", "Điện thoại", Display(phone));
        AddInfoIf(document, template, "document_date", "Ngày đơn", Date(version.ConfirmedAt ?? version.CreatedAt));
        AddInfoIf(document, template, "address", "Địa chỉ", Address(version.CustomerAddress));
        AddInfoIf(document, template, "warehouse", "Kho", JoinCodeName(version.WarehouseCode, version.WarehouseName));
        AddInfoIf(
            document,
            template,
            "delivery_method",
            "Hình thức giao nhận",
            version.DeliveryMode == "PICKUP"
                ? "Mua tại quầy"
                : version.DeliveryExecutionMode == "MANUAL" ? "Giao thủ công" : "Giao theo chuyến");
        AddInfoIf(document, template, "collection_policy", "Thanh toán", SalesPresentation.CollectionPolicy(version.CollectionPolicy));
        AddInfoIf(document, template, "requested_delivery_date", "Ngày giao dự kiến", Date(version.RequestedDeliveryDate));
        if (DocumentPrintTemplateRuntime.Shows(template, "total_weight"))
        {
            AddInfo(
                document,
                "Tổng khối lượng",
                string.IsNullOrWhiteSpace(version.TotalWeightKg)
                    ? "Chưa đủ dữ liệu"
                    : $"{SalesPresentation.Quantity(version.TotalWeightKg)} kg");
        }

        var lines = BuildLines(version, template);
        if (lines is not null) document.Blocks.Add(lines);

        var totals = new List<string>();
        if (DocumentPrintTemplateRuntime.Shows(template, "total_subtotal"))
            totals.Add($"Tạm tính: {SalesPresentation.Money(version.Subtotal)}");
        if (ShowDiscount(version) && DocumentPrintTemplateRuntime.Shows(template, "total_discount"))
            totals.Add($"Tổng chiết khấu: {SalesPresentation.Money(version.DiscountTotal)}");
        if (ShowTax(version) && DocumentPrintTemplateRuntime.Shows(template, "total_tax"))
            totals.Add($"Tổng thuế: {SalesPresentation.Money(version.TaxTotal)}");
        if (DocumentPrintTemplateRuntime.Shows(template, "total_total"))
            totals.Add($"TỔNG CỘNG: {SalesPresentation.Money(version.Total)}");
        if (totals.Count > 0)
        {
            document.Blocks.Add(new Paragraph(new Run(string.Join("\n", totals)))
            {
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0),
                FontWeight = FontWeights.Bold,
                FontSize = 13
            });
        }

        if (DocumentPrintTemplateRuntime.Shows(template, "note") && !string.IsNullOrWhiteSpace(version.Note))
            document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {version.Note.Trim()}")) { Margin = new Thickness(0, 10, 0, 0) });

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người lập", "Kho giao hàng", "Khách hàng");
    }

    private static Table? BuildLines(SalesOrderVersionData version, DocumentPrintTemplateData template)
    {
        var columns = new List<(string Key, string Header, Func<SalesOrderLineData, string> Value)>
        {
            ("line_no", "STT", line => line.LineNumber.ToString(CultureInfo.InvariantCulture)),
            ("line_item", "Tên sản phẩm", line => Display(line.ItemName)),
            ("line_sku", "SKU", line => Display(line.Sku)),
            ("line_quantity", "Số lượng", line => SalesPresentation.Quantity(line.Quantity)),
            ("line_unit", "ĐVT", line => Display(line.UnitName ?? line.UnitCode)),
            ("line_unit_price", "Đơn giá", line => SalesPresentation.Money(line.UnitPrice)),
            ("line_discount", "Chiết khấu", line => SalesPresentation.Money(line.DiscountAmount)),
            ("line_tax", "Thuế", line => SalesPresentation.Money(line.TaxAmount)),
            ("line_total", "Thành tiền", line => SalesPresentation.Money(line.LineTotal))
        }
        .Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key))
        .Where(column => column.Key != "line_discount" || ShowDiscount(version))
        .Where(column => column.Key != "line_tax" || ShowTax(version))
        .ToList();

        if (columns.Count == 0) return null;

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 14, 0, 0) };
        foreach (var _ in columns) table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        var header = new TableRow();
        foreach (var column in columns) header.Cells.Add(Cell(column.Header, true));
        group.Rows.Add(header);

        foreach (var line in (version.Lines ?? []).OrderBy(item => item.LineNumber))
        {
            var row = new TableRow();
            foreach (var column in columns) row.Cells.Add(Cell(column.Value(line), column.Key == "line_total"));
            group.Rows.Add(row);
        }

        table.RowGroups.Add(group);
        return table;
    }

    private static TableCell Cell(string value, bool bold = false)
    {
        var run = new Run(Display(value));
        if (bold) run.FontWeight = FontWeights.SemiBold;
        return new TableCell(new Paragraph(run) { Margin = new Thickness(0) })
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(5)
        };
    }

    private static void AddInfoIf(
        FlowDocument document,
        DocumentPrintTemplateData template,
        string key,
        string label,
        string? value)
    {
        if (DocumentPrintTemplateRuntime.Shows(template, key)) AddInfo(document, label, value);
    }

    private static void AddInfo(FlowDocument document, string label, string? value) =>
        document.Blocks.Add(new Paragraph
        {
            Margin = new Thickness(0, 1, 0, 1),
            Inlines = { new Bold(new Run($"{label}: ")), new Run(Display(value)) }
        });

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
            DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template);
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
            DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template);
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
