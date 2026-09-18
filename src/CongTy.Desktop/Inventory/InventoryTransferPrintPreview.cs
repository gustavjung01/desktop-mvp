using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Inventory;

internal static class InventoryTransferPrintPreview
{
    public static void Print(InventoryTransferData transfer, DocumentPrintTemplateData template)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template, 11, new Thickness(40));
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            "PHIẾU CHUYỂN KHO",
            transfer.DocumentNumber ?? "Phiếu nháp chưa cấp số",
            "Chứng từ điều chuyển tồn kho");

        AddInfoIf(document, template, "status", "Trạng thái", PrintStatus(transfer.Status));
        AddInfoIf(document, template, "source_warehouse", "Kho xuất", $"{transfer.SourceWarehouseCode} — {transfer.SourceWarehouseName}");
        AddInfoIf(document, template, "destination_warehouse", "Kho nhận", $"{transfer.DestinationWarehouseCode} — {transfer.DestinationWarehouseName}");
        AddInfoIf(document, template, "transfer_date", "Ngày chuyển", InventoryPresentation.Date(transfer.TransferDate));
        AddInfoIf(document, template, "approved_date", "Ngày duyệt", InventoryPresentation.Date(transfer.ApprovedAt));
        AddInfoIf(document, template, "dispatched_date", "Ngày xuất", InventoryPresentation.Date(transfer.DispatchedAt));
        AddInfoIf(document, template, "line_count", "Số dòng", transfer.LineCount.ToString());

        var columns = new List<(string Key, string Header, Func<InventoryTransferLineData, string> Value)>
        {
            ("line_no", "STT", line => line.LineNumber.ToString()),
            ("line_item", "Hàng hóa / SKU", line => $"{line.ItemName}\n{line.SourceSku}"),
            ("line_location", "Vị trí / lô", line => string.Join(" / ", new[] { line.SourceLocationId, line.LotCode }.Where(value => !string.IsNullOrWhiteSpace(value)))),
            ("line_quantity", "Số lượng", line => InventoryPresentation.Quantity(line.SourceQuantity)),
            ("line_unit", "ĐVT", line => line.SourceUnitCode),
            ("line_base_quantity", "SL cơ sở", line => InventoryPresentation.Quantity(line.BaseQuantity))
        }.Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key)).ToList();

        if (columns.Count > 0)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 14, 0, 0) };
            foreach (var _ in columns) table.Columns.Add(new TableColumn());
            var group = new TableRowGroup();
            table.RowGroups.Add(group);
            group.Rows.Add(Row(columns.Select(column => column.Header), true));
            foreach (var line in transfer.Lines)
                group.Rows.Add(Row(columns.Select(column => column.Value(line)), false));
            document.Blocks.Add(table);
        }

        if (DocumentPrintTemplateRuntime.Shows(template, "total_base_quantity"))
        {
            document.Blocks.Add(new Paragraph(new Run($"Tổng số lượng cơ sở: {InventoryPresentation.Quantity(transfer.BaseQuantityTotal)}"))
            {
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            });
        }

        if (DocumentPrintTemplateRuntime.Shows(template, "note"))
        {
            var note = transfer.Status == "cancelled"
                ? $"ĐÃ HỦY{(string.IsNullOrWhiteSpace(transfer.CancellationReason) ? string.Empty : $" — {transfer.CancellationReason}")}"
                : transfer.Note;
            if (!string.IsNullOrWhiteSpace(note))
                document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {note}")) { Margin = new Thickness(0, 10, 0, 0) });
        }

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người lập", "Thủ kho xuất", "Thủ kho nhận");

        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template, new Thickness(40));
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, transfer.DocumentNumber ?? "Phiếu chuyển kho");
    }

    private static string PrintStatus(string status) => status switch
    {
        "draft" => "Nháp",
        "approved" => "Đã duyệt",
        "dispatched" => "Đã xuất kho nguồn",
        "cancelled" => "Đã hủy",
        _ => status
    };

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
