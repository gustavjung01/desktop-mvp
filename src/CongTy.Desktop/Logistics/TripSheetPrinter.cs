using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Logistics;

public static class TripSheetPrinter
{
    public static void Print(TripDispatchData trip, DocumentPrintTemplateData template)
    {
        ArgumentNullException.ThrowIfNull(trip);
        if (trip.Status is not ("locked" or "dispatched")) return;

        var document = DocumentPrintTemplateRuntime.CreateDocument(template);
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            "PHIẾU CHUYẾN GIAO HÀNG",
            trip.Number,
            "Danh sách bàn giao chuyến");

        AddMetaIf(document, template, "status", "Trạng thái", TripDispatchPresentation.Status(trip.Status));
        AddMetaIf(document, template, "warehouse", "Kho xuất phát", $"{trip.WarehouseCode ?? "—"} — {trip.WarehouseName ?? "—"}");
        AddMetaIf(document, template, "vehicle", "Xe", trip.LicensePlate ?? trip.VehicleCode ?? "—");
        AddMetaIf(document, template, "driver", "Tài xế", $"{trip.DriverCode ?? "—"} — {trip.DriverName ?? "—"}");
        AddMetaIf(document, template, "planned_start", "Dự kiến", TripDispatchPresentation.LocalDateTime(trip.PlannedStartAt));
        AddMetaIf(document, template, "dispatched_at", "Xuất phát", TripDispatchPresentation.LocalDateTime(trip.DispatchedAt));
        AddMetaIf(document, template, "handover_receiver", "Người nhận bàn giao", trip.HandoverReceiverName ?? trip.DriverName ?? "—");

        var columns = new List<(string Key, string Header, Func<TripDispatchStopData, TripDispatchAssignmentData, string> Value)>
        {
            ("line_stop", "Điểm giao", (stop, _) => stop.Sequence.ToString()),
            ("line_delivery_order", "Phiếu giao", (_, assignment) => TripDispatchPresentation.Number(assignment.DeliveryOrderNumber)),
            ("line_customer", "Khách hàng", (_, assignment) => $"{assignment.CustomerName ?? "—"}\n{assignment.CustomerCode ?? "—"}")
        }.Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key)).ToList();

        if (columns.Count > 0)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 14, 0, 10) };
            foreach (var _ in columns) table.Columns.Add(new TableColumn());
            var rows = new TableRowGroup();
            table.RowGroups.Add(rows);
            rows.Rows.Add(Row(columns.Select(column => column.Header), true));
            foreach (var stop in trip.Stops.OrderBy(row => row.Sequence))
            {
                foreach (var assignment in stop.Assignments)
                    rows.Rows.Add(Row(columns.Select(column => column.Value(stop, assignment)), false));
            }
            document.Blocks.Add(table);
        }

        var totals = new List<string>();
        if (DocumentPrintTemplateRuntime.Shows(template, "total_stops")) totals.Add($"Số điểm giao: {trip.Stops.Length}");
        if (DocumentPrintTemplateRuntime.Shows(template, "total_delivery_orders")) totals.Add($"Số phiếu bàn giao: {trip.Stops.Sum(stop => stop.Assignments.Length)}");
        if (totals.Count > 0)
            document.Blocks.Add(new Paragraph(new Run(string.Join("     ", totals))) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 8) });

        if (DocumentPrintTemplateRuntime.Shows(template, "note") && !string.IsNullOrWhiteSpace(trip.HandoverNote))
            document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {trip.HandoverNote}")));

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Điều phối", "Thủ kho", "Tài xế / Người nhận");

        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Phiếu chuyến giao hàng {trip.Number}");
    }

    private static void AddMetaIf(FlowDocument document, DocumentPrintTemplateData template, string key, string label, string value)
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
                Margin = new Thickness(5),
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
