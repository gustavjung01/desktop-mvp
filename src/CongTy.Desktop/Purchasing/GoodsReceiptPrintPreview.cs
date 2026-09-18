using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Purchasing;

public static class GoodsReceiptPrintPreview
{
    public static FlowDocument Create(GoodsReceiptData receipt, DocumentPrintTemplateData template)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template);
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            "PHIẾU NHẬN HÀNG",
            receipt.DocumentNumber ?? "Phiếu chưa cấp số",
            "Chứng từ nhập hàng");

        var info = new Table { CellSpacing = 0 };
        info.Columns.Add(new TableColumn { Width = new GridLength(145) });
        info.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        info.RowGroups.Add(group);
        AddInfoIf(group, template, "status", "Trạng thái", GoodsReceiptPresentation.Status(receipt.Status));
        AddInfoIf(group, template, "supplier", "Nhà cung cấp", $"{receipt.SupplierCode ?? "—"} — {receipt.SupplierName}");
        AddInfoIf(group, template, "purchase_order", "Đơn mua hàng", receipt.PurchaseOrderNumber ?? "Chưa cấp số");
        AddInfoIf(group, template, "warehouse", "Kho nhận", $"{receipt.WarehouseCode ?? "—"} — {receipt.WarehouseName}");
        AddInfoIf(group, template, "receipt_date", "Ngày nhận", GoodsReceiptPresentation.Date(receipt.ReceiptDate));
        AddInfoIf(group, template, "delivery_reference", "Tham chiếu giao", receipt.SupplierDeliveryReference ?? "Không có");
        AddInfoIf(group, template, "line_count", "Số dòng", receipt.LineCount.ToString());
        if (group.Rows.Count > 0) document.Blocks.Add(info);

        var columns = new List<(string Key, string Header, Func<GoodsReceiptLineData, string> Value)>
        {
            ("line_no", "STT", line => line.LineNumber.ToString()),
            ("line_item", "Hàng hóa / SKU", line => $"{line.ItemName}\n{line.SkuCode}"),
            ("line_received", "Thực nhận", line => GoodsReceiptPresentation.Number(line.ReceivedQuantity)),
            ("line_accepted", "Chấp nhận", line => GoodsReceiptPresentation.Number(line.AcceptedQuantity)),
            ("line_rejected", "Loại", line => GoodsReceiptPresentation.Number(line.RejectedQuantity)),
            ("line_unit", "ĐVT", line => line.UnitCode),
            ("line_lot", "Lô / hạn sử dụng", line => string.Join(" · ", new[] { line.LotCode, GoodsReceiptPresentation.Date(line.ExpiryDate) }.Where(value => !string.IsNullOrWhiteSpace(value) && value != "—")))
        }.Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key)).ToList();

        if (columns.Count > 0)
        {
            document.Blocks.Add(new Paragraph(new Run("Dòng nhận hàng"))
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
            foreach (var line in receipt.Lines)
                AddRow(rows, false, columns.Select(column => column.Value(line)));
            document.Blocks.Add(table);
        }

        var totals = new List<string>();
        if (DocumentPrintTemplateRuntime.Shows(template, "total_received")) totals.Add($"Tổng thực nhận: {GoodsReceiptPresentation.Number(receipt.ReceivedQuantityTotal)}");
        if (DocumentPrintTemplateRuntime.Shows(template, "total_accepted")) totals.Add($"Tổng chấp nhận: {GoodsReceiptPresentation.Number(receipt.AcceptedQuantityTotal)}");
        if (DocumentPrintTemplateRuntime.Shows(template, "total_rejected")) totals.Add($"Tổng loại: {GoodsReceiptPresentation.Number(receipt.RejectedQuantityTotal)}");
        if (DocumentPrintTemplateRuntime.Shows(template, "total_shortage")) totals.Add($"Tổng chốt thiếu: {GoodsReceiptPresentation.Number(receipt.ShortageClosedQuantityTotal)}");
        if (totals.Count > 0)
            document.Blocks.Add(new Paragraph(new Run(string.Join("   |   ", totals))) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 12, 0, 0) });

        if (DocumentPrintTemplateRuntime.Shows(template, "note") && !string.IsNullOrWhiteSpace(receipt.Note))
            document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {receipt.Note}")) { Margin = new Thickness(0, 12, 0, 0) });

        if (receipt.Status == "reversed" && !string.IsNullOrWhiteSpace(receipt.ReversalReason))
            document.Blocks.Add(new Paragraph(new Run($"Lý do đảo: {receipt.ReversalReason}")) { Margin = new Thickness(0, 8, 0, 0) });

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người giao", "Thủ kho", "Người kiểm nhận");
        return document;
    }

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
        return new TableCell(paragraph) { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.4) };
    }
}
