using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Logistics;

internal static class DeliveryOrderPrintPreview
{
    public static void Print(
        DeliveryOrderData order,
        DocumentPrintTemplateData template,
        bool packing)
    {
        var title = packing ? "PHIẾU ĐÓNG GÓI" : "PHIẾU GIAO HÀNG";
        var document = DocumentPrintTemplateRuntime.CreateDocument(template);
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            title,
            order.Number ?? "Phiếu chưa cấp số",
            packing ? "Danh sách đóng gói" : "Chứng từ giao nhận");

        AddInfoIf(document, template, "status", "Trạng thái", DeliveryOrderPresentation.Status(order.Status));
        AddInfoIf(document, template, "sales_order", "Đơn bán hàng", order.SalesOrderNumber ?? "—");
        AddInfoIf(document, template, "customer", "Khách hàng", $"{order.CustomerCode} — {order.CustomerName}");
        AddInfoIf(document, template, "warehouse", "Kho xuất", $"{order.WarehouseCode} — {order.WarehouseName}");
        AddInfoIf(document, template, "handover_mode", "Hình thức", DeliveryOrderPresentation.HandoverMode(order.HandoverMode));
        AddInfoIf(document, template, "requested_delivery_date", "Ngày giao dự kiến", DeliveryOrderPresentation.Date(order.RequestedDeliveryDate));
        AddInfoIf(document, template, "collection_policy", "Chính sách thu", string.IsNullOrWhiteSpace(order.CollectionPolicy) ? "—" : order.CollectionPolicy);
        AddInfoIf(document, template, "destination", "Địa chỉ giao", Destination(order.Destination));

        var columns = new List<(string Key, string Header, Func<DeliveryOrderLineData, string> Value)>
        {
            ("line_no", "STT", line => line.LineNumber.ToString()),
            ("line_item", "Sản phẩm / SKU", line => $"{line.ItemName}\n{line.Sku}"),
            ("line_quantity", "Số lượng", line => DeliveryOrderPresentation.Quantity(line.DeliveryBaseQuantity)),
            ("line_unit", "ĐVT", line => line.UnitCode),
            ("line_lot", "Lô / HSD", line => string.Join(" · ", new[] { line.LotCode, line.ExpiryDate }.Where(value => !string.IsNullOrWhiteSpace(value)))),
            ("line_location", "Vị trí", line => line.LocationCode ?? "—")
        }.Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key)).ToList();

        if (columns.Count > 0)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 14, 0, 0) };
            foreach (var _ in columns) table.Columns.Add(new TableColumn());
            var group = new TableRowGroup();
            table.RowGroups.Add(group);
            group.Rows.Add(Row(columns.Select(column => column.Header), true));
            foreach (var line in order.Lines.OrderBy(line => line.LineNumber))
                group.Rows.Add(Row(columns.Select(column => column.Value(line)), false));
            document.Blocks.Add(table);
        }

        if (DocumentPrintTemplateRuntime.Shows(template, "total_quantity"))
        {
            document.Blocks.Add(new Paragraph(new Run(
                $"{(packing ? "Tổng SL đóng gói" : "Tổng SL giao")}: {DeliveryOrderPresentation.Quantity(order.TotalBaseQuantity)}"))
            {
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            });
        }

        if (DocumentPrintTemplateRuntime.Shows(template, "note"))
        {
            var note = order.Status == "cancelled" ? "ĐÃ HỦY" : order.Note;
            if (!string.IsNullOrWhiteSpace(note))
                document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {note}")) { Margin = new Thickness(0, 10, 0, 0) });
        }

        DocumentPrintTemplateRuntime.AddSignatures(
            document,
            template,
            packing ? "Người đóng gói" : "Người giao",
            packing ? "Thủ kho" : "Khách hàng",
            packing ? "Người kiểm" : "Thủ kho");

        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template, new Thickness(42));
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"{title} {order.Number}");
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
        var row = new TableRow { FontWeight = header ? FontWeights.Bold : FontWeights.Normal };
        foreach (var value in values)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(string.IsNullOrWhiteSpace(value) ? "—" : value)) { Margin = new Thickness(4) })
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(3)
            });
        }
        return row;
    }

    private static string Destination(DeliveryOrderDestinationData? destination)
    {
        if (destination is null) return "—";
        var parts = new[]
        {
            destination.AddressLine1,
            destination.AddressLine2,
            destination.Ward,
            destination.District,
            destination.Province
        }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim());
        var text = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(text) ? "—" : text;
    }
}
