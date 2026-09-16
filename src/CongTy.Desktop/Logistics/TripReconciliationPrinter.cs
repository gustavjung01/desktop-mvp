using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public static class TripReconciliationPrinter
{
    public static void Print(TripReconciliationData detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        var document = new FlowDocument
        {
            PageWidth = dialog.PrintableAreaWidth,
            PageHeight = dialog.PrintableAreaHeight,
            PagePadding = new Thickness(36),
            ColumnGap = 0,
            ColumnWidth = dialog.PrintableAreaWidth,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 10
        };

        document.Blocks.Add(new Paragraph(new Run("PHIẾU ĐỐI SOÁT CUỐI CHUYẾN"))
        {
            FontSize = 18, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center
        });
        document.Blocks.Add(new Paragraph(new Run($"{detail.Number} · {(detail.Status == "closed" ? "Đã đóng" : "Đang đối soát")}"))
        {
            TextAlignment = TextAlignment.Center
        });

        var summary = new Paragraph();
        summary.Inlines.Add(new Run($"Kho: {detail.WarehouseCode ?? detail.WarehouseName ?? "—"}   "));
        summary.Inlines.Add(new Run($"Tài xế: {detail.DriverName ?? "—"}   "));
        summary.Inlines.Add(new Run($"Xe: {detail.LicensePlate ?? "—"}"));
        document.Blocks.Add(summary);

        var table = new Table { CellSpacing = 0 };
        foreach (var width in new[] { 150d, 80d, 65d, 65d, 65d, 65d })
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        var header = new TableRow();
        foreach (var title in new[] { "Phiếu / hàng", "Kết quả", "Xuất", "Đã giao", "Đã về", "Còn xe" })
            header.Cells.Add(Cell(title, true));
        group.Rows.Add(header);

        foreach (var line in detail.Lines)
        {
            var row = new TableRow();
            row.Cells.Add(Cell($"{TripReconciliationPresentation.Number(line.DeliveryOrderNumber)}\n{line.Sku} — {line.ItemName}"));
            row.Cells.Add(Cell(TripReconciliationPresentation.Result(line.AttemptResult)));
            row.Cells.Add(Cell(TripReconciliationPresentation.Quantity(line.IssuedBaseQuantity)));
            row.Cells.Add(Cell(TripReconciliationPresentation.Quantity(line.DeliveredBaseQuantity)));
            row.Cells.Add(Cell(TripReconciliationPresentation.Quantity(line.ReturnedBaseQuantity)));
            row.Cells.Add(Cell(TripReconciliationPresentation.Quantity(line.OutstandingBaseQuantity)));
            group.Rows.Add(row);
        }
        document.Blocks.Add(table);

        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Đối soát {detail.Number}");
    }

    private static TableCell Cell(string text, bool bold = false) =>
        new(new Paragraph(new Run(text)) { Margin = new Thickness(4), FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal })
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(2)
        };
}
