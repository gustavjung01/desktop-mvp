using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public static class TripSheetPrinter
{
    public static void Print(TripDispatchData trip)
    {
        ArgumentNullException.ThrowIfNull(trip);
        if (trip.Status is not ("locked" or "dispatched")) return;

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        var document = Build(trip);
        document.PageWidth = dialog.PrintableAreaWidth;
        document.PageHeight = dialog.PrintableAreaHeight;
        document.PagePadding = new Thickness(36);
        document.ColumnGap = 0;
        document.ColumnWidth = dialog.PrintableAreaWidth;

        dialog.PrintDocument(
            ((IDocumentPaginatorSource)document).DocumentPaginator,
            $"Phiếu chuyến giao hàng {trip.Number}");
    }

    private static FlowDocument Build(TripDispatchData trip)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11
        };

        document.Blocks.Add(new Paragraph(new Run("PHIẾU CHUYẾN GIAO HÀNG"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        });
        document.Blocks.Add(new Paragraph(new Run("Danh sách bàn giao chuyến"))
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 14)
        });

        var meta = new Table { CellSpacing = 0 };
        meta.Columns.Add(new TableColumn { Width = new GridLength(130) });
        meta.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        meta.RowGroups.Add(group);
        AddMeta(group, "Số chuyến", trip.Number);
        AddMeta(group, "Trạng thái", TripDispatchPresentation.Status(trip.Status));
        AddMeta(group, "Kho xuất phát", $"{trip.WarehouseCode ?? "—"} — {trip.WarehouseName ?? "—"}");
        AddMeta(group, "Xe", trip.LicensePlate ?? trip.VehicleCode ?? "—");
        AddMeta(group, "Tài xế", $"{trip.DriverCode ?? "—"} — {trip.DriverName ?? "—"}");
        AddMeta(group, "Dự kiến", TripDispatchPresentation.LocalDateTime(trip.PlannedStartAt));
        AddMeta(group, "Xuất phát", TripDispatchPresentation.LocalDateTime(trip.DispatchedAt));
        AddMeta(group, "Người nhận bàn giao", trip.HandoverReceiverName ?? trip.DriverName ?? "—");
        document.Blocks.Add(meta);

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 14, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(65) });
        table.Columns.Add(new TableColumn { Width = new GridLength(160) });
        table.Columns.Add(new TableColumn());
        var rows = new TableRowGroup();
        table.RowGroups.Add(rows);
        var header = new TableRow();
        header.Cells.Add(Cell("Điểm", true));
        header.Cells.Add(Cell("Phiếu giao", true));
        header.Cells.Add(Cell("Khách hàng", true));
        rows.Rows.Add(header);

        foreach (var stop in trip.Stops.OrderBy(row => row.Sequence))
        {
            foreach (var assignment in stop.Assignments)
            {
                var row = new TableRow();
                row.Cells.Add(Cell(stop.Sequence.ToString()));
                row.Cells.Add(Cell(TripDispatchPresentation.Number(assignment.DeliveryOrderNumber)));
                row.Cells.Add(Cell($"{assignment.CustomerName ?? "—"}\n{assignment.CustomerCode ?? "—"}"));
                rows.Rows.Add(row);
            }
        }
        document.Blocks.Add(table);

        document.Blocks.Add(new Paragraph(new Run(
            $"Số điểm giao: {trip.Stops.Length}     Số phiếu bàn giao: {trip.Stops.Sum(stop => stop.Assignments.Length)}"))
        {
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 0, 8)
        });

        if (!string.IsNullOrWhiteSpace(trip.HandoverNote))
            document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {trip.HandoverNote}")));

        var signatures = new Table { CellSpacing = 0, Margin = new Thickness(0, 24, 0, 0) };
        signatures.Columns.Add(new TableColumn());
        signatures.Columns.Add(new TableColumn());
        signatures.Columns.Add(new TableColumn());
        var signatureGroup = new TableRowGroup();
        signatures.RowGroups.Add(signatureGroup);
        var signatureRow = new TableRow();
        foreach (var label in new[] { "Điều phối", "Thủ kho", "Tài xế / Người nhận" })
            signatureRow.Cells.Add(new TableCell(new Paragraph(new Run(label)) { TextAlignment = TextAlignment.Center, FontWeight = FontWeights.SemiBold }));
        signatureGroup.Rows.Add(signatureRow);
        document.Blocks.Add(signatures);

        return document;
    }

    private static void AddMeta(TableRowGroup group, string label, string value)
    {
        var row = new TableRow();
        row.Cells.Add(Cell(label, true));
        row.Cells.Add(Cell(value));
        group.Rows.Add(row);
    }

    private static TableCell Cell(string value, bool bold = false) =>
        new(new Paragraph(new Run(value))
        {
            Margin = new Thickness(5),
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal
        })
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(2)
        };
}
