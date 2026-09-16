using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public static class PurchaseOrderPrintPreview
{
    public static FlowDocument Create(PurchaseOrderData order)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(36),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11,
            ColumnWidth = double.PositiveInfinity
        };

        document.Blocks.Add(new Paragraph(new Run("ĐƠN MUA HÀNG"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });
        document.Blocks.Add(new Paragraph(new Run(order.Number ?? "Đơn chưa cấp số"))
        {
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        });

        var info = new Table { CellSpacing = 0 };
        info.Columns.Add(new TableColumn { Width = new GridLength(130) });
        info.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        info.RowGroups.Add(group);
        AddInfo(group, "Trạng thái", PurchaseOrderPresentation.Status(order.Status));
        AddInfo(group, "Nhà cung cấp", $"{order.SupplierCode ?? "—"} — {order.SupplierName}");
        AddInfo(group, "Kho nhận", $"{order.WarehouseCode ?? "—"} — {order.WarehouseName}");
        AddInfo(group, "Ngày đặt", PurchaseOrderPresentation.Date(order.PlacedAt));
        AddInfo(group, "Dự kiến nhận", PurchaseOrderPresentation.Date(order.ExpectedAt));
        AddInfo(group, "Tham chiếu", order.SupplierReference ?? "Không có");
        document.Blocks.Add(info);

        document.Blocks.Add(new Paragraph(new Run("Hàng mua"))
        {
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 16, 0, 6)
        });

        var table = new Table { CellSpacing = 0 };
        foreach (var width in new[] { 36d, 95d, 220d, 70d, 70d, 95d, 110d })
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        }

        var rows = new TableRowGroup();
        table.RowGroups.Add(rows);
        AddRow(rows, true, "STT", "SKU", "Tên hàng", "SL", "ĐVT", "Đơn giá", "Thành tiền");
        for (var i = 0; i < order.Lines.Length; i++)
        {
            var line = order.Lines[i];
            AddRow(
                rows,
                false,
                (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                line.SkuCode,
                line.ItemName,
                PurchaseOrderPresentation.Number(line.Quantity),
                line.UnitCode,
                PurchaseOrderPresentation.Money(line.UnitPrice, order.Currency),
                PurchaseOrderPresentation.Money(line.LineTotal, order.Currency));
        }

        document.Blocks.Add(table);
        document.Blocks.Add(new Paragraph(new Run(
            $"Tổng cộng: {PurchaseOrderPresentation.Money(order.Total, order.Currency)}"))
        {
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        });

        if (!string.IsNullOrWhiteSpace(order.Note))
        {
            document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {order.Note}"))
            {
                Margin = new Thickness(0, 12, 0, 0)
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
