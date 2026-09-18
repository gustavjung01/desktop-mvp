using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Purchasing;

public static class PurchaseOrderPrintPreview
{
    public static FlowDocument Create(PurchaseOrderData order, DocumentPrintTemplateData template)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template);
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            "ĐƠN MUA HÀNG",
            order.Number ?? "Đơn chưa cấp số",
            "Chứng từ mua hàng");

        var info = new Table { CellSpacing = 0 };
        info.Columns.Add(new TableColumn { Width = new GridLength(130) });
        info.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        info.RowGroups.Add(group);

        AddInfoIf(group, template, "status", "Trạng thái", PurchaseOrderPresentation.Status(order.Status));
        AddInfoIf(group, template, "supplier", "Nhà cung cấp", $"{order.SupplierCode ?? "—"} — {order.SupplierName}");
        AddInfoIf(group, template, "warehouse", "Kho nhận", $"{order.WarehouseCode ?? "—"} — {order.WarehouseName}");
        AddInfoIf(group, template, "ordered_date", "Ngày đặt", PurchaseOrderPresentation.Date(order.PlacedAt));
        AddInfoIf(group, template, "expected_date", "Dự kiến nhận", PurchaseOrderPresentation.Date(order.ExpectedAt));
        AddInfoIf(group, template, "supplier_reference", "Tham chiếu", order.SupplierReference ?? "Không có");
        AddInfoIf(group, template, "currency", "Tiền tệ", order.Currency);
        if (group.Rows.Count > 0) document.Blocks.Add(info);

        var columns = new List<(string Key, string Header, Func<PurchaseOrderLineData, int, string> Value)>
        {
            ("line_no", "STT", (line, index) => line.LineNumber > 0 ? line.LineNumber.ToString() : (index + 1).ToString()),
            ("line_item", "Hàng hóa / SKU", (line, _) => $"{line.ItemName}\n{line.SkuCode}"),
            ("line_quantity", "Số lượng", (line, _) => PurchaseOrderPresentation.Number(line.Quantity)),
            ("line_unit", "ĐVT", (line, _) => line.UnitCode),
            ("line_unit_price", "Đơn giá", (line, _) => PurchaseOrderPresentation.Money(line.UnitPrice, order.Currency)),
            ("line_discount", "Chiết khấu", (line, _) => PurchaseOrderPresentation.Money(line.DiscountAmount, order.Currency)),
            ("line_tax", "Thuế", (line, _) => PurchaseOrderPresentation.Money(line.TaxAmount, order.Currency)),
            ("line_total", "Thành tiền", (line, _) => PurchaseOrderPresentation.Money(line.LineTotal, order.Currency))
        }
        .Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key))
        .Where(column => column.Key != "line_discount" || ShowDiscount(order))
        .Where(column => column.Key != "line_tax" || ShowTax(order))
        .ToList();

        if (columns.Count > 0)
        {
            document.Blocks.Add(new Paragraph(new Run("Hàng mua"))
            {
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 16, 0, 6)
            });

            var table = new Table { CellSpacing = 0 };
            foreach (var _ in columns) table.Columns.Add(new TableColumn());
            var rows = new TableRowGroup();
            table.RowGroups.Add(rows);
            AddRow(rows, true, columns.Select(column => column.Header));
            for (var index = 0; index < order.Lines.Length; index++)
            {
                var line = order.Lines[index];
                AddRow(rows, false, columns.Select(column => column.Value(line, index)));
            }
            document.Blocks.Add(table);
        }

        var totals = new List<string>();
        if (DocumentPrintTemplateRuntime.Shows(template, "total_subtotal"))
            totals.Add($"Tiền hàng: {PurchaseOrderPresentation.Money(order.Subtotal, order.Currency)}");
        if (ShowDiscount(order) && DocumentPrintTemplateRuntime.Shows(template, "total_discount"))
            totals.Add($"Chiết khấu: {PurchaseOrderPresentation.Money(order.DiscountTotal, order.Currency)}");
        if (ShowTax(order) && DocumentPrintTemplateRuntime.Shows(template, "total_tax"))
            totals.Add($"Thuế: {PurchaseOrderPresentation.Money(order.TaxTotal, order.Currency)}");
        if (DocumentPrintTemplateRuntime.Shows(template, "total_total"))
            totals.Add($"TỔNG CỘNG: {PurchaseOrderPresentation.Money(order.Total, order.Currency)}");
        if (totals.Count > 0)
        {
            document.Blocks.Add(new Paragraph(new Run(string.Join("\n", totals)))
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            });
        }

        if (DocumentPrintTemplateRuntime.Shows(template, "note") && !string.IsNullOrWhiteSpace(order.Note))
            document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {order.Note}")) { Margin = new Thickness(0, 12, 0, 0) });

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người lập", "Người duyệt", "Nhà cung cấp");
        return document;
    }

    private static bool ShowDiscount(PurchaseOrderData order) =>
        !IsZeroAmount(order.DiscountTotal)
        || order.Lines.Any(line => !IsZeroAmount(line.DiscountAmount));

    private static bool ShowTax(PurchaseOrderData order) =>
        !IsZeroAmount(order.TaxTotal)
        || order.Lines.Any(line => !IsZeroAmount(line.TaxAmount));

    private static bool IsZeroAmount(string? value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var amount)
        && amount == 0m;

    private static void AddInfoIf(TableRowGroup group, DocumentPrintTemplateData template, string key, string label, string value)
    {
        if (!DocumentPrintTemplateRuntime.Shows(template, key)) return;
        var row = new TableRow();
        row.Cells.Add(Cell(label, true));
        row.Cells.Add(Cell(value, false));
        group.Rows.Add(row);
    }

    private static void AddRow(TableRowGroup group, bool header, IEnumerable<string> values)
    {
        var row = new TableRow();
        foreach (var value in values) row.Cells.Add(Cell(value, header));
        group.Rows.Add(row);
    }

    private static TableCell Cell(string? value, bool bold)
    {
        var paragraph = new Paragraph(new Run(string.IsNullOrWhiteSpace(value) ? "—" : value))
        {
            Margin = new Thickness(4, 3, 4, 3),
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal
        };
        return new TableCell(paragraph)
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.4)
        };
    }
}
