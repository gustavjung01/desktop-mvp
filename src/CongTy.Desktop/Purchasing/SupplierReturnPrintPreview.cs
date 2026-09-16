using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public static class SupplierReturnPrintPreview
{
    public static FlowDocument Create(SupplierReturnData item)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(36),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11,
            ColumnWidth = double.PositiveInfinity
        };

        document.Blocks.Add(new Paragraph(new Run("PHIẾU TRẢ NHÀ CUNG CẤP"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });
        document.Blocks.Add(new Paragraph(new Run(item.DocumentNumber ?? "Phiếu chưa cấp số"))
        {
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        });

        var info = new Table { CellSpacing = 0 };
        info.Columns.Add(new TableColumn { Width = new GridLength(150) });
        info.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        info.RowGroups.Add(group);
        AddInfo(group, "Trạng thái", SupplierReturnPresentation.Status(item.Status));
        AddInfo(group, "Nhà cung cấp", $"{item.SupplierCode} — {item.SupplierName}");
        AddInfo(group, "Kho xuất trả", $"{item.WarehouseCode} — {item.WarehouseName}");
        AddInfo(group, "Ngày trả", SupplierReturnPresentation.Date(item.ReturnDate));
        AddInfo(group, "Tổng số lượng", SupplierReturnPresentation.Number(item.ReturnQuantityTotal));
        AddInfo(group, "Ghi chú", item.Note ?? "Không có");
        document.Blocks.Add(info);

        document.Blocks.Add(new Paragraph(new Run("Dòng trả hàng"))
        {
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 16, 0, 6)
        });

        var table = new Table { CellSpacing = 0 };
        foreach (var width in new[] { 105d, 95d, 185d, 65d, 80d, 155d })
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });

        var rows = new TableRowGroup();
        table.RowGroups.Add(rows);
        AddRow(rows, true, "Phiếu nhận", "SKU", "Tên hàng", "ĐVT", "SL trả", "Lý do");
        foreach (var line in item.Lines)
        {
            AddRow(
                rows,
                false,
                line.SourceGoodsReceiptNumber,
                line.SourceSku,
                line.SourceItemName,
                line.SourceUnitCode,
                SupplierReturnPresentation.Number(line.ReturnQuantity),
                $"{line.ReasonCode} — {line.ReasonNote}");
        }

        document.Blocks.Add(table);

        if (item.Status == "cancelled" && !string.IsNullOrWhiteSpace(item.CancellationReason))
            document.Blocks.Add(new Paragraph(new Run($"Lý do hủy: {item.CancellationReason}")) { Margin = new Thickness(0, 12, 0, 0) });
        if (item.Status == "reversed" && !string.IsNullOrWhiteSpace(item.ReversalReason))
            document.Blocks.Add(new Paragraph(new Run($"Lý do đảo: {item.ReversalReason}")) { Margin = new Thickness(0, 12, 0, 0) });

        return document;
    }

    private static void AddInfo(TableRowGroup group, string label, string value)
    {
        var row = new TableRow();
        row.Cells.Add(Cell(label, true));
        row.Cells.Add(Cell(value, false));
        group.Rows.Add(row);
    }

    private static void AddRow(TableRowGroup group, bool header, params string[] values)
    {
        var row = new TableRow();
        foreach (var value in values) row.Cells.Add(Cell(value, header));
        group.Rows.Add(row);
    }

    private static TableCell Cell(string value, bool bold)
    {
        var paragraph = new Paragraph(new Run(value))
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
