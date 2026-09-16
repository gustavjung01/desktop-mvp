using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public static class GoodsReceiptPrintPreview
{
    public static FlowDocument Create(GoodsReceiptData receipt)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(36),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11,
            ColumnWidth = double.PositiveInfinity
        };

        document.Blocks.Add(new Paragraph(new Run("PHIẾU NHẬN HÀNG"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });
        document.Blocks.Add(new Paragraph(new Run(receipt.DocumentNumber ?? "Phiếu chưa cấp số"))
        {
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        });

        var info = new Table { CellSpacing = 0 };
        info.Columns.Add(new TableColumn { Width = new GridLength(145) });
        info.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        info.RowGroups.Add(group);
        AddInfo(group, "Trạng thái", GoodsReceiptPresentation.Status(receipt.Status));
        AddInfo(group, "Đơn mua hàng", receipt.PurchaseOrderNumber ?? "Chưa cấp số");
        AddInfo(group, "Nhà cung cấp", $"{receipt.SupplierCode ?? "—"} — {receipt.SupplierName}");
        AddInfo(group, "Kho nhận", $"{receipt.WarehouseCode ?? "—"} — {receipt.WarehouseName}");
        AddInfo(group, "Ngày nhận", GoodsReceiptPresentation.Date(receipt.ReceiptDate));
        AddInfo(group, "Tham chiếu giao hàng", receipt.SupplierDeliveryReference ?? "Không có");
        document.Blocks.Add(info);

        document.Blocks.Add(new Paragraph(new Run("Dòng nhận hàng"))
        {
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 16, 0, 6)
        });

        var table = new Table { CellSpacing = 0 };
        foreach (var width in new[] { 90d, 190d, 65d, 65d, 65d, 65d, 90d })
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        }

        var rows = new TableRowGroup();
        table.RowGroups.Add(rows);
        AddRow(rows, true, "SKU", "Tên hàng", "Thực nhận", "Chấp nhận", "Loại", "Chốt thiếu", "Lô");
        foreach (var line in receipt.Lines)
        {
            AddRow(
                rows,
                false,
                line.SkuCode,
                line.ItemName,
                GoodsReceiptPresentation.Number(line.ReceivedQuantity),
                GoodsReceiptPresentation.Number(line.AcceptedQuantity),
                GoodsReceiptPresentation.Number(line.RejectedQuantity),
                GoodsReceiptPresentation.Number(line.ShortageClosedQuantity),
                line.LotCode ?? "—");
        }

        document.Blocks.Add(table);
        document.Blocks.Add(new Paragraph(new Run(
            $"Tổng thực nhận: {GoodsReceiptPresentation.Number(receipt.ReceivedQuantityTotal)}   |   " +
            $"Chấp nhận: {GoodsReceiptPresentation.Number(receipt.AcceptedQuantityTotal)}   |   " +
            $"Loại: {GoodsReceiptPresentation.Number(receipt.RejectedQuantityTotal)}   |   " +
            $"Chốt thiếu: {GoodsReceiptPresentation.Number(receipt.ShortageClosedQuantityTotal)}"))
        {
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        });

        if (!string.IsNullOrWhiteSpace(receipt.Note))
        {
            document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {receipt.Note}"))
            {
                Margin = new Thickness(0, 12, 0, 0)
            });
        }

        if (receipt.Status == "reversed" && !string.IsNullOrWhiteSpace(receipt.ReversalReason))
        {
            document.Blocks.Add(new Paragraph(new Run($"Lý do đảo: {receipt.ReversalReason}"))
            {
                Margin = new Thickness(0, 8, 0, 0)
            });
        }

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
