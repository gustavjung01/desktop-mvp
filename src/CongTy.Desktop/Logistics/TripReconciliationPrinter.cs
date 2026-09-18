using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Logistics;

public static class TripReconciliationPrinter
{
    public static void Print(TripReconciliationData detail, DocumentPrintTemplateData template)
    {
        ArgumentNullException.ThrowIfNull(detail);

        var document = DocumentPrintTemplateRuntime.CreateDocument(template, 10);
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            "BIÊN BẢN ĐỐI SOÁT CHUYẾN",
            detail.Number,
            "Đối chiếu giao hàng và hàng quay về");

        AddInfoIf(document, template, "status", "Trạng thái", detail.Status == "closed" ? "Đã đóng" : "Đang đối soát");
        AddInfoIf(document, template, "warehouse", "Kho", $"{detail.WarehouseCode ?? "—"} — {detail.WarehouseName ?? "—"}");
        AddInfoIf(document, template, "vehicle", "Xe", detail.LicensePlate ?? "—");
        AddInfoIf(document, template, "driver", "Tài xế", detail.DriverName ?? "—");
        AddInfoIf(document, template, "receipt_count", "Số lần nhập hàng về", detail.Receipts.Length.ToString());
        AddInfoIf(document, template, "closed_at", "Thời điểm đóng", TripReconciliationPresentation.LocalDateTime(detail.ClosedAt));
        AddInfoIf(document, template, "can_close", "Có thể đóng", detail.CanClose ? "Có" : "Chưa");

        var columns = new List<(string Key, string Header, Func<TripReconciliationLineData, string> Value)>
        {
            ("line_stop", "Điểm giao", line => line.StopSequence.ToString()),
            ("line_delivery_order", "Phiếu giao", line => TripReconciliationPresentation.Number(line.DeliveryOrderNumber)),
            ("line_customer", "Khách hàng", line => $"{line.CustomerName ?? "—"}\n{line.CustomerCode ?? "—"}"),
            ("line_item", "SKU / hàng hóa", line => $"{line.ItemName}\n{line.Sku}{(string.IsNullOrWhiteSpace(line.LotCode) ? string.Empty : $" · Lô {line.LotCode}")}"),
            ("line_result", "Kết quả", line => TripReconciliationPresentation.Result(line.AttemptResult)),
            ("line_issued", "Xuất", line => $"{TripReconciliationPresentation.Quantity(line.IssuedBaseQuantity)} {line.UnitCode}"),
            ("line_delivered", "Đã giao", line => TripReconciliationPresentation.Quantity(line.DeliveredBaseQuantity)),
            ("line_returned", "Đã về", line => TripReconciliationPresentation.Quantity(line.ReturnedBaseQuantity)),
            ("line_outstanding", "Còn xe", line => TripReconciliationPresentation.Quantity(line.OutstandingBaseQuantity))
        }.Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key)).ToList();

        if (columns.Count > 0)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 12, 0, 0) };
            foreach (var _ in columns) table.Columns.Add(new TableColumn());
            var group = new TableRowGroup();
            table.RowGroups.Add(group);
            group.Rows.Add(Row(columns.Select(column => column.Header), true));
            foreach (var line in detail.Lines)
                group.Rows.Add(Row(columns.Select(column => column.Value(line)), false));
            document.Blocks.Add(table);
        }

        if (DocumentPrintTemplateRuntime.Shows(template, "note"))
        {
            var note = detail.CanClose
                ? "Đã đối chiếu đủ điều kiện đóng chuyến."
                : "Còn hàng/chứng từ cần đối chiếu trước khi đóng chuyến.";
            document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {note}")) { Margin = new Thickness(0, 10, 0, 0) });
        }

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Điều phối", "Thủ kho", "Tài xế");

        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Đối soát {detail.Number}");
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
            row.Cells.Add(new TableCell(new Paragraph(new Run(value))
            {
                Margin = new Thickness(4),
                FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal
            })
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(2)
            });
        }
        return row;
    }
}
