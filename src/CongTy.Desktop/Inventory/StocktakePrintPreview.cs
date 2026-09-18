using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Inventory;

internal static class StocktakePrintPreview
{
    public static void Print(
        InventoryStocktakeData stocktake,
        IReadOnlyList<StocktakeLineRow> lines,
        DocumentPrintTemplateData template)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template);
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            "PHIẾU KIỂM KÊ",
            stocktake.StocktakeNumber,
            "Chứng từ kiểm kê kho");

        AddInfoIf(document, template, "status", "Trạng thái", StocktakePresentation.Status(stocktake.Status));
        AddInfoIf(document, template, "warehouse", "Kho", $"{stocktake.WarehouseCode} — {stocktake.WarehouseName}");
        AddInfoIf(document, template, "round", "Vòng đếm", stocktake.CurrentRound.ToString());
        AddInfoIf(document, template, "line_count", "Số dòng", stocktake.LineCount.ToString());
        AddInfoIf(document, template, "created_date", "Ngày tạo", StocktakePresentation.DateTimeText(stocktake.CreatedAt));
        AddInfoIf(document, template, "approved_date", "Ngày duyệt", StocktakePresentation.DateTimeText(stocktake.ApprovedAt));
        AddInfoIf(document, template, "posted_date", "Ngày ghi sổ", StocktakePresentation.DateTimeText(stocktake.PostedAt));

        var columns = new List<(string Key, string Header, Func<StocktakeLineRow, string> Value)>
        {
            ("line_no", "STT", line => line.LineNumber.ToString()),
            ("line_sku", "SKU", line => line.Sku),
            ("line_location", "Vị trí / lô", line => $"{line.Location} / {line.Lot}"),
            ("line_expected", "Theo sổ", line => line.Expected),
            ("line_counted", "Thực đếm", line => line.CountedDisplay),
            ("line_delta", "Chênh lệch", line => line.Difference),
            ("line_unit", "ĐVT", line => line.Unit)
        }
        .Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key))
        .ToList();

        if (columns.Count > 0)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 14, 0, 0) };
            foreach (var _ in columns) table.Columns.Add(new TableColumn());
            var group = new TableRowGroup();
            table.RowGroups.Add(group);
            group.Rows.Add(Row(columns.Select(column => column.Header), true));
            foreach (var line in lines)
                group.Rows.Add(Row(columns.Select(column => column.Value(line)), false));
            document.Blocks.Add(table);
        }

        if (DocumentPrintTemplateRuntime.Shows(template, "note"))
        {
            var note = stocktake.Status == "reversed"
                ? $"ĐÃ ĐẢO{(string.IsNullOrWhiteSpace(stocktake.ReversalReason) ? string.Empty : $" — {stocktake.ReversalReason}")}"
                : stocktake.Status == "cancelled"
                    ? $"ĐÃ HỦY{(string.IsNullOrWhiteSpace(stocktake.CancelReason) ? string.Empty : $" — {stocktake.CancelReason}")}"
                    : stocktake.Note;
            if (!string.IsNullOrWhiteSpace(note))
                document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {note}")) { Margin = new Thickness(0, 10, 0, 0) });
        }

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người kiểm kê", "Thủ kho", "Người duyệt");

        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Phiếu kiểm kê {stocktake.StocktakeNumber}");
    }

    private static void AddInfoIf(FlowDocument document, DocumentPrintTemplateData template, string key, string label, string value)
    {
        if (!DocumentPrintTemplateRuntime.Shows(template, key)) return;
        document.Blocks.Add(new Paragraph(new Run($"{label}: {value}")) { Margin = new Thickness(0, 2, 0, 2) });
    }

    private static TableRow Row(IEnumerable<string> values, bool header)
    {
        var row = new TableRow();
        foreach (var value in values)
        {
            var paragraph = new Paragraph(new Run(string.IsNullOrWhiteSpace(value) ? "—" : value)) { Margin = new Thickness(4) };
            if (header) paragraph.FontWeight = FontWeights.SemiBold;
            row.Cells.Add(new TableCell(paragraph) { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
        }
        return row;
    }
}
