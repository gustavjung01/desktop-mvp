using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Inventory;

internal static class InventoryAdjustmentPrintPreview
{
    public static void Print(InventoryAdjustmentData adjustment, DocumentPrintTemplateData template)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template);
        var direction = adjustment.AdjustmentDirection switch
        {
            "IN" => "TĂNG",
            "OUT" => "GIẢM",
            _ => string.Empty
        };
        var title = string.IsNullOrWhiteSpace(direction)
            ? "PHIẾU ĐIỀU CHỈNH TỒN"
            : $"PHIẾU ĐIỀU CHỈNH {direction} TỒN";

        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            title,
            adjustment.AdjustmentNumber,
            "Chứng từ đối chiếu tồn kho");

        AddInfoIf(document, template, "status", "Trạng thái", InventoryAdjustmentPresentation.Status(adjustment.Status));
        AddInfoIf(document, template, "batch", "Mã đợt đối soát", adjustment.ReconciliationBatchCode ?? "—");
        AddInfoIf(document, template, "warehouse", "Kho", InventoryAdjustmentPresentation.Warehouse(adjustment.WarehouseCode, adjustment.WarehouseName));
        AddInfoIf(document, template, "created_at", "Ngày lập", InventoryAdjustmentPresentation.DateTimeText(adjustment.CreatedAt));
        AddInfoIf(document, template, "created_by", "Người lập", string.IsNullOrWhiteSpace(adjustment.CreatedBy) ? "Người lập" : adjustment.CreatedBy);
        AddInfoIf(document, template, "reason", "Lý do", adjustment.ReasonLabel ?? adjustment.ReasonCode);
        AddInfoIf(document, template, "line_count", "Số dòng", adjustment.LineCount.ToString());

        var columns = new List<(string Key, string Header, Func<InventoryAdjustmentLineData, string> Value)>
        {
            ("line_no", "STT", line => line.LineNumber.ToString()),
            ("line_item", "Sản phẩm / SKU", line => $"{(string.IsNullOrWhiteSpace(line.ProductName) ? line.SourceSku : line.ProductName)}\n{line.SourceSku}"),
            ("line_lot", "Lô", line => string.IsNullOrWhiteSpace(line.LotCode) ? "—" : line.LotCode!),
            ("line_location", "Vị trí", line => Location(line)),
            ("line_system", "Tồn hệ thống", line => line.SystemBaseQuantity is null ? "—" : InventoryAdjustmentPresentation.Quantity(line.SystemBaseQuantity)),
            ("line_counted", "Tồn thực tế", line => line.CountedBaseQuantity is null ? "—" : InventoryAdjustmentPresentation.Quantity(line.CountedBaseQuantity)),
            ("line_delta", "Chênh lệch", line => InventoryAdjustmentPresentation.SignedQuantity(SourceDelta(adjustment, line))),
            ("line_unit", "ĐVT", line => line.SourceUnitCode)
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
            foreach (var line in adjustment.Lines.OrderBy(item => item.LineNumber))
                group.Rows.Add(Row(columns.Select(column => column.Value(line)), false));
            document.Blocks.Add(table);
        }

        if (DocumentPrintTemplateRuntime.Shows(template, "note"))
        {
            var notPosted = adjustment.Status is not ("POSTED" or "REVERSED");
            var note = string.Join(
                " — ",
                new[]
                {
                    notPosted ? "CHƯA CẬP NHẬT TỒN KHO" : string.Empty,
                    adjustment.ReasonNote
                }.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (!string.IsNullOrWhiteSpace(note))
                document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {note}")) { Margin = new Thickness(0, 10, 0, 0) });
        }

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người đối chiếu", "Thủ kho", "Người duyệt");

        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template);
        dialog.PrintDocument(
            ((IDocumentPaginatorSource)document).DocumentPaginator,
            $"Phiếu điều chỉnh tồn {adjustment.AdjustmentNumber}");
    }

    private static string Location(InventoryAdjustmentLineData line)
    {
        var values = new[] { line.SourceLocationCode, line.SourceLocationName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        return values.Length == 0 ? "Không vị trí" : string.Join(" · ", values);
    }

    private static string SourceDelta(InventoryAdjustmentData adjustment, InventoryAdjustmentLineData line)
    {
        if (adjustment.DocumentKind == "MANUAL_ADJUSTMENT" && adjustment.AdjustmentDirection == "IN")
            return line.BaseQuantity;

        return decimal.TryParse(
            line.BaseQuantity,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var value)
            ? (-value).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : $"-{line.BaseQuantity}";
    }

    private static void AddInfoIf(
        FlowDocument document,
        DocumentPrintTemplateData template,
        string key,
        string label,
        string value)
    {
        if (!DocumentPrintTemplateRuntime.Shows(template, key)) return;
        document.Blocks.Add(new Paragraph(new Run($"{label}: {value}")) { Margin = new Thickness(0, 2, 0, 2) });
    }

    private static TableRow Row(IEnumerable<string> values, bool header)
    {
        var row = new TableRow();
        foreach (var value in values)
        {
            var paragraph = new Paragraph(new Run(string.IsNullOrWhiteSpace(value) ? "—" : value))
            {
                Margin = new Thickness(4)
            };
            if (header) paragraph.FontWeight = FontWeights.SemiBold;
            row.Cells.Add(new TableCell(paragraph)
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            });
        }
        return row;
    }
}
