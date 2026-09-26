using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Printing;

internal static class ActualDocumentPrintPreview
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static void PrintManualInbound(Window? owner, ManualInboundHistoryDocumentData source, ManualInboundHistoryMovementData detail, DocumentPrintTemplateData template)
    {
        var document = Begin(template, "PHIẾU NHẬP KHO", source.ReferenceNumber, "Chứng từ nhập kho thủ công", ManualInboundStatus(source.Status));
        AddFields(document, template,
        [
            new("inbound_type", "Loại nhập", ManualInboundType(source.InboundType)),
            new("warehouse", "Kho nhập", Party(source.WarehouseCode, source.WarehouseName)),
            new("document_date", "Ngày chứng từ", Date(source.DocumentDate))
        ]);
        AddTable(document, template, detail.Lines.OrderBy(line => line.Sku).ToArray(),
        [
            new PrintColumn<ManualInboundHistoryMovementLineData>("line_no", "STT", (_, index) => (index + 1).ToString(Vi), true),
            new("line_item", "Sản phẩm / SKU", (line, _) => Product(line.ProductName, line.Sku)),
            new("line_quantity", "Số lượng nhập", (line, _) => Number(line.QuantityDelta), true),
            new("line_unit", "ĐVT", (line, _) => Dash(line.BaseUnitCode))
        ]);
        var note = source.Status.Equals("reversed", StringComparison.OrdinalIgnoreCase)
            ? JoinNote(source.Note, string.IsNullOrWhiteSpace(source.ReversalNote) ? "ĐÃ ĐẢO" : $"ĐÃ ĐẢO — {source.ReversalNote}")
            : source.Note;
        AddNote(document, template, note);
        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người lập phiếu", "Thủ kho", "Người giao hàng");
        Print(owner, document, template, "Phiếu nhập kho", source.ReferenceNumber);
    }

    public static void PrintSupplierReturn(Window? owner, SupplierReturnData item, DocumentPrintTemplateData template)
    {
        var document = Begin(template, "PHIẾU TRẢ HÀNG NHÀ CUNG CẤP", item.DocumentNumber, "Chứng từ xuất trả hàng", SupplierReturnStatus(item.Status));
        AddFields(document, template,
        [
            new("supplier", "Nhà cung cấp", Party(item.SupplierCode, item.SupplierName)),
            new("warehouse", "Kho xuất trả", Party(item.WarehouseCode, item.WarehouseName)),
            new("return_date", "Ngày trả", Date(item.ReturnDate)),
            new("source_receipt", "Phiếu nhận nguồn", JoinDistinct(item.Lines.Select(line => line.SourceGoodsReceiptNumber)))
        ]);
        AddTable(document, template, item.Lines.OrderBy(line => line.LineNumber).ToArray(),
        [
            new PrintColumn<SupplierReturnLineData>("line_no", "STT", (line, _) => line.LineNumber.ToString(Vi), true),
            new("line_item", "Sản phẩm / SKU", (line, _) => Product(line.SourceItemName, line.SourceSku)),
            new("line_reason", "Lý do", (line, _) => Dash(string.IsNullOrWhiteSpace(line.ReasonNote) ? line.ReasonCode : line.ReasonNote)),
            new("line_quantity", "SL trả", (line, _) => Number(line.ReturnQuantity), true),
            new("line_unit", "ĐVT", (line, _) => Dash(line.SourceUnitCode)),
            new("line_lot", "Lô", (line, _) => Dash(line.LotCode))
        ]);
        AddTotals(document, template, [new("total_quantity", "Tổng số lượng trả", Number(item.ReturnQuantityTotal))]);
        var note = item.Status.ToLowerInvariant() switch
        {
            "reversed" => JoinNote(item.Note, string.IsNullOrWhiteSpace(item.ReversalReason) ? "ĐÃ ĐẢO" : $"ĐÃ ĐẢO — {item.ReversalReason}"),
            "cancelled" => JoinNote(item.Note, string.IsNullOrWhiteSpace(item.CancellationReason) ? "ĐÃ HỦY" : $"ĐÃ HỦY — {item.CancellationReason}"),
            _ => item.Note
        };
        AddNote(document, template, note);
        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người lập phiếu", "Thủ kho", "Nhà cung cấp / Người nhận");
        Print(owner, document, template, "Phiếu trả nhà cung cấp", item.DocumentNumber);
    }

    public static void PrintCustomerReturn(Window? owner, CustomerReturnData item, DocumentPrintTemplateData template)
    {
        var document = Begin(template, "PHIẾU NHẬN HÀNG KHÁCH TRẢ", item.Number, "Chứng từ thực nhận hàng trả", "Đã nhận vào kho");
        AddFields(document, template,
        [
            new("customer", "Khách hàng", Party(item.CustomerCode, item.CustomerName)),
            new("warehouse", "Kho nhận", Party(item.WarehouseCode, item.WarehouseName)),
            new("source_document", "Chứng từ nguồn", JoinDistinct(item.Lines.Select(line => line.DeliveryOrderNumber)))
        ]);
        AddTable(document, template, item.Lines.OrderBy(line => line.LineNumber).ToArray(),
        [
            new PrintColumn<CustomerReturnLineData>("line_no", "STT", (line, _) => line.LineNumber.ToString(Vi), true),
            new("line_item", "Sản phẩm / SKU", (line, _) => Product(line.ItemName, line.Sku)),
            new("line_reason", "Lý do", (line, _) => CustomerReturnReason(line.ReasonCode, line.ReasonNote)),
            new("line_requested", "Yêu cầu", (line, _) => Number(line.RequestedBaseQuantity), true),
            new("line_accepted", "Thực nhận", (line, _) => Number(line.AcceptedBaseQuantity), true),
            new("line_unit", "ĐVT", (line, _) => Dash(line.UnitCode)),
            new("line_lot", "Lô", (line, _) => Dash(line.LotCode))
        ]);
        AddTotals(document, template,
        [
            new("total_requested", "Tổng yêu cầu", Sum(item.Lines.Select(line => line.RequestedBaseQuantity))),
            new("total_accepted", "Tổng thực nhận", Sum(item.Lines.Select(line => line.AcceptedBaseQuantity)))
        ]);
        AddNote(document, template, item.Note);
        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Khách hàng / Người giao", "Thủ kho", "Người kiểm nhận");
        Print(owner, document, template, "Phiếu nhận hàng khách trả", item.Number);
    }

    public static void PrintFulfillmentPicking(Window? owner, IReadOnlyList<FulfillmentWorkItemData> items, DocumentPrintTemplateData template)
    {
        if (items.Count == 0) return;
        var first = items[0];
        var document = Begin(template, "PHIẾU SOẠN HÀNG / CẤP HÀNG", first.OrderNumber, "Tình trạng phân bổ, soạn và đóng gói", FulfillmentStatus(first.FulfillmentStatus));
        AddFields(document, template,
        [
            new("customer", "Khách hàng", Party(first.CustomerCode, first.CustomerName)),
            new("warehouse", "Kho xử lý", Party(first.WarehouseCode, first.WarehouseName)),
            new("sales_channel", "Kênh bán", Party(first.SalesChannelCode, first.SalesChannelName)),
            new("delivery_date", "Ngày giao yêu cầu", Date(first.RequestedDeliveryDate))
        ]);
        AddTable(document, template, items.OrderBy(item => item.LineNumber).ToArray(),
        [
            new PrintColumn<FulfillmentWorkItemData>("line_no", "STT", (item, _) => item.LineNumber.ToString(Vi), true),
            new("line_item", "Sản phẩm / SKU", (item, _) => Product(item.ItemName, item.Sku)),
            new("line_ordered", "SL đặt", (item, _) => Number(item.OrderedBaseQuantity), true),
            new("line_allocated", "Đã phân bổ", (item, _) => Number(item.AllocatedBaseQuantity), true),
            new("line_picked", "Đã soạn", (item, _) => Number(item.PickedBaseQuantity), true),
            new("line_packed", "Đã đóng gói", (item, _) => Number(item.PackedBaseQuantity), true),
            new("line_unit", "ĐVT", (item, _) => Dash(item.BaseUnitCode))
        ]);
        AddNote(document, template, "Phiếu phản ánh trạng thái phân bổ, soạn và đóng gói tại thời điểm in.");
        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người soạn hàng", "Thủ kho", "Người nhận bàn giao");
        Print(owner, document, template, "Phiếu soạn hàng", first.OrderNumber);
    }

    public static void PrintSupplierPayment(Window? owner, SupplierPaymentData payment, DocumentPrintTemplateData template)
    {
        var document = Begin(template, "PHIẾU CHI / THANH TOÁN NHÀ CUNG CẤP", payment.DocumentNumber, "Chứng từ thanh toán Nhà cung cấp", SupplierPaymentStatus(payment.Status));
        AddFields(document, template,
        [
            new("supplier", "Nhà cung cấp", Party(payment.SupplierCode, payment.SupplierName)),
            new("paying_unit", "Đơn vị chi", Party(payment.WarehouseCode, payment.WarehouseName)),
            new("payment_date", "Ngày thanh toán", Date(payment.PaymentDate)),
            new("payment_method", "Phương thức", PaymentMethod(payment.PaymentMethod)),
            new("bank_reference", "Tham chiếu ngân hàng", Dash(payment.ExternalReference)),
            new("recorded_by", "Người ghi nhận", Dash(payment.PostedBy))
        ]);
        AddTotals(document, template,
        [
            new("total_paid", "Tổng tiền chi", Money(payment.OriginalAmount, payment.CurrencyCode), true),
            new("total_allocated", "Đã phân bổ", Money(payment.AllocatedAmount, payment.CurrencyCode)),
            new("total_unallocated", "Chưa phân bổ", Money(payment.RemainingAmount, payment.CurrencyCode))
        ]);
        var note = payment.Status.Equals("reversed", StringComparison.OrdinalIgnoreCase)
            ? JoinNote(payment.Note, string.IsNullOrWhiteSpace(payment.ReversalReason) ? "ĐÃ ĐẢO" : $"ĐÃ ĐẢO — {payment.ReversalReason}")
            : payment.Note;
        AddNote(document, template, note);
        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người lập phiếu", "Kế toán / Thủ quỹ", "Nhà cung cấp / Người nhận");
        Print(owner, document, template, "Phiếu chi nhà cung cấp", payment.DocumentNumber);
    }

    public static void PrintCustomerRefund(Window? owner, CustomerRefundData refund, DocumentPrintTemplateData template)
    {
        var document = Begin(template, "PHIẾU HOÀN TIỀN KHÁCH HÀNG", refund.RefundNumber, "Chứng từ hoàn tiền từ khoản giảm công nợ", string.IsNullOrWhiteSpace(refund.ReversalId) ? "Đã hoàn" : "Đã đảo");
        AddFields(document, template,
        [
            new("customer", "Khách hàng", Party(refund.CustomerCode, refund.CustomerName)),
            new("warehouse", "Đơn vị hoàn", Party(refund.WarehouseCode, refund.WarehouseName)),
            new("source_credit", "Khoản giảm nguồn", Dash(refund.SourceCreditNumber)),
            new("refund_date", "Ngày hoàn", DateTimeText(refund.PostedAt)),
            new("refund_method", "Phương thức", PaymentMethod(refund.RefundMethod)),
            new("destination", "Nơi nhận", Dash(refund.DestinationReference)),
            new("transaction_reference", "Tham chiếu giao dịch", Dash(refund.ExternalReference))
        ]);
        AddTotals(document, template, [new("total_refund", "Số tiền hoàn", Money(refund.Amount, refund.CurrencyCode), true)]);
        var note = string.IsNullOrWhiteSpace(refund.ReversalId)
            ? refund.Reason
            : JoinNote(refund.Reason, string.IsNullOrWhiteSpace(refund.ReversalReason) ? "ĐÃ ĐẢO" : $"ĐÃ ĐẢO — {refund.ReversalReason}");
        AddNote(document, template, note);
        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người lập phiếu", "Kế toán / Thủ quỹ", "Khách hàng / Người nhận");
        Print(owner, document, template, "Phiếu hoàn tiền khách hàng", refund.RefundNumber);
    }

    public static void PrintCodReconciliation(Window? owner, CodHandoverData handover, DocumentPrintTemplateData template)
    {
        var document = Begin(template, "BIÊN BẢN ĐỐI SOÁT COD", null, "Đối chiếu tiền thu hộ bàn giao về Công Ty", CodStatus(handover.Status));
        AddFields(document, template,
        [
            new("trip", "Chuyến giao", Dash(handover.TripNumber)),
            new("warehouse", "Kho", Party(handover.WarehouseCode, handover.WarehouseName)),
            new("driver", "Tài xế", Party(handover.DriverCode, handover.DriverName)),
            new("handover_at", "Bàn giao lúc", DateTimeText(handover.HandedOverAt))
        ]);
        AddTable(document, template, handover.Lines,
        [
            new PrintColumn<CodHandoverLineData>("line_delivery_order", "Phiếu giao", (line, _) => Dash(line.DeliveryOrderNumber)),
            new("line_customer", "Khách hàng", (line, _) => Party(line.CustomerCode, line.CustomerName)),
            new("line_expected", "Đang giữ", (line, _) => Money(line.ExpectedAmount, "VND"), true),
            new("line_handed_over", "Bàn giao", (line, _) => Money(line.HandedOverAmount, "VND"), true)
        ]);
        var accepted = handover.Acceptance is { } acceptance && string.IsNullOrWhiteSpace(acceptance.ReversalId)
            ? Money(acceptance.AcceptedAmount, "VND")
            : "Chưa xác nhận";
        AddTotals(document, template,
        [
            new("expected_total", "Tổng đang giữ", Money(handover.ExpectedTotal, "VND")),
            new("handed_over_total", "Tổng bàn giao", Money(handover.HandedOverTotal, "VND"), true),
            new("accepted_total", "Công Ty thực nhận", accepted),
            new("difference_total", "Chênh lệch", Money(handover.DifferenceAmount, "VND"))
        ]);
        var note = JoinNote(handover.Reason, handover.Note);
        if (!string.IsNullOrWhiteSpace(handover.ReversalId))
            note = JoinNote(note, string.IsNullOrWhiteSpace(handover.ReversalReason) ? "ĐÃ ĐẢO" : $"ĐÃ ĐẢO — {handover.ReversalReason}");
        AddNote(document, template, note);
        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Tài xế / Người bàn giao", "Người nhận tiền", "Kế toán");
        Print(owner, document, template, "Biên bản đối soát COD", handover.TripNumber);
    }

    private static FlowDocument Begin(DocumentPrintTemplateData template, string title, string? number, string subtitle, string status)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template, 10.5, new Thickness(32));
        DocumentPrintTemplateRuntime.AddHeader(document, template, title, number, subtitle);
        AddFields(document, template, [new("status", "Trạng thái", status)]);
        return document;
    }

    private static void AddFields(FlowDocument document, DocumentPrintTemplateData template, IReadOnlyList<PrintField> fields)
    {
        var visible = fields.Where(field => DocumentPrintTemplateRuntime.Shows(template, field.Key)).ToArray();
        if (visible.Length == 0) return;
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(150) });
        table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        foreach (var field in visible)
        {
            var row = new TableRow();
            row.Cells.Add(Cell(field.Label, true));
            row.Cells.Add(Cell(Dash(field.Value), false));
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);
        document.Blocks.Add(table);
    }

    private static void AddTable<T>(FlowDocument document, DocumentPrintTemplateData template, IReadOnlyList<T> rows, IReadOnlyList<PrintColumn<T>> columns)
    {
        var visible = columns.Where(column => DocumentPrintTemplateRuntime.Shows(template, column.Key)).ToArray();
        if (visible.Length == 0) return;
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 8) };
        foreach (var _ in visible) table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        var header = new TableRow();
        foreach (var column in visible) header.Cells.Add(Cell(column.Header, true, column.RightAligned ? TextAlignment.Right : TextAlignment.Left, true));
        group.Rows.Add(header);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = new TableRow();
            foreach (var column in visible) row.Cells.Add(Cell(Dash(column.Value(rows[index], index)), false, column.RightAligned ? TextAlignment.Right : TextAlignment.Left));
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);
        document.Blocks.Add(table);
    }

    private static void AddTotals(FlowDocument document, DocumentPrintTemplateData template, IReadOnlyList<PrintTotal> totals)
    {
        foreach (var total in totals)
        {
            if (!DocumentPrintTemplateRuntime.Shows(template, total.Key)) continue;
            var paragraph = new Paragraph { Margin = new Thickness(0, 2, 0, 2), TextAlignment = TextAlignment.Right };
            paragraph.Inlines.Add(new Run($"{total.Label}: ") { FontWeight = FontWeights.SemiBold });
            paragraph.Inlines.Add(new Run(Dash(total.Value)) { FontWeight = total.Emphasis ? FontWeights.Bold : FontWeights.Normal });
            document.Blocks.Add(paragraph);
        }
    }

    private static void AddNote(FlowDocument document, DocumentPrintTemplateData template, string? note)
    {
        if (!DocumentPrintTemplateRuntime.Shows(template, "note") || string.IsNullOrWhiteSpace(note)) return;
        var paragraph = new Paragraph { Margin = new Thickness(0, 12, 0, 0) };
        paragraph.Inlines.Add(new Run("Ghi chú: ") { FontWeight = FontWeights.SemiBold });
        paragraph.Inlines.Add(new Run(note.Trim()));
        document.Blocks.Add(paragraph);
    }

    private static TableCell Cell(string value, bool bold, TextAlignment alignment = TextAlignment.Left, bool header = false)
    {
        var paragraph = new Paragraph(new Run(value)) { Margin = new Thickness(0), TextAlignment = alignment, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal };
        return new TableCell(paragraph)
        {
            Padding = new Thickness(5, 4, 5, 4),
            BorderBrush = Brushes.LightGray,
            BorderThickness = header ? new Thickness(0, 0, 0, 1) : new Thickness(0, 0, 0, 0.5)
        };
    }

    private static void Print(Window? owner, FlowDocument document, DocumentPrintTemplateData template, string jobName, string? number)
    {
        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template, new Thickness(32));
        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        var suffix = string.IsNullOrWhiteSpace(number) ? string.Empty : $" {number.Trim()}";
        dialog.PrintDocument(paginator, $"{jobName}{suffix}");
    }

    private static string ManualInboundStatus(string? value) => value?.Equals("reversed", StringComparison.OrdinalIgnoreCase) == true ? "Đã đảo" : "Đã nhập kho";
    private static string ManualInboundType(string? value) => value?.ToUpperInvariant() switch
    {
        "MANUAL_RECEIPT" => "Nhập kho thủ công", "OFF_DOCUMENT_CUSTOMER_RETURN" => "Hàng khách trả ngoài chứng từ",
        "RECOVERY" => "Nhập khôi phục", "OTHER" => "Nhập khác", _ => Dash(value)
    };
    private static string SupplierReturnStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "draft" => "Nháp", "pending_approval" => "Chờ duyệt", "approved" => "Đã duyệt",
        "posted" => "Đã ghi sổ", "reversed" => "Đã đảo", "cancelled" => "Đã hủy", _ => Dash(value)
    };
    private static string CustomerReturnReason(string? code, string? note)
    {
        var label = code?.ToUpperInvariant() switch
        {
            "DAMAGED_OR_UNWANTED" => "Hư hỏng / không nhận", "WRONG_ITEM" => "Sai hàng",
            "QUALITY_COMPLAINT" => "Khiếu nại chất lượng", "OTHER" => "Khác", _ => Dash(code)
        };
        return string.IsNullOrWhiteSpace(note) ? label : $"{label} — {note.Trim()}";
    }
    private static string FulfillmentStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "backordered" => "Chờ thêm hàng", "partially_reserved" => "Có hàng một phần", "reserved" => "Chờ phân bổ",
        "partially_allocated" => "Phân bổ một phần", "allocated" => "Đã phân bổ", "partially_picked" => "Đang soạn",
        "picked" => "Đã soạn", "partially_packed" => "Đang đóng gói", "packed" => "Đã đóng gói", _ => "Đang xử lý"
    };
    private static string SupplierPaymentStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "open" => "Chưa phân bổ", "partially_allocated" => "Đã phân bổ một phần", "settled" => "Đã thanh toán", "reversed" => "Đã đảo", _ => Dash(value)
    };
    private static string CodStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "submitted" => "Chờ xác nhận", "reconciled" => "Đã khớp", "discrepancy" => "Có chênh lệch",
        "reversed" => "Đã đảo bàn giao", "acceptance_reversed" => "Đã đảo xác nhận", _ => Dash(value)
    };
    private static string PaymentMethod(string? value) => value?.ToUpperInvariant() switch
    {
        "CASH" => "Tiền mặt", "BANK_TRANSFER" => "Chuyển khoản", "OTHER" => "Khác", _ => Dash(value)
    };
    private static string Product(string? name, string? sku)
    {
        var item = Dash(name); var code = Dash(sku);
        return item == code ? item : $"{item} · {code}";
    }
    private static string Party(string? code, string? name)
    {
        var parts = new[] { code?.Trim(), name?.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return parts.Length == 0 ? "—" : string.Join(" — ", parts);
    }
    private static string JoinDistinct(IEnumerable<string?> values)
    {
        var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return items.Length == 0 ? "—" : string.Join(", ", items);
    }
    private static string JoinNote(string? first, string? second)
    {
        var parts = new[] { first?.Trim(), second?.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return string.Join(Environment.NewLine, parts);
    }
    private static string Sum(IEnumerable<string?> values) => values.Sum(Decimal).ToString("#,0.######", Vi);
    private static string Number(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number.ToString("#,0.######", Vi) : Dash(value);
    private static string Money(string? value, string? currency)
    {
        var amount = Number(value);
        return string.IsNullOrWhiteSpace(currency) ? amount : $"{amount} {currency.Trim()}";
    }
    private static decimal Decimal(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : 0m;
    private static string Date(string? value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTime)) return dateTime.ToString("dd/MM/yyyy", Vi);
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateOnly)) return dateOnly.ToString("dd/MM/yyyy", Vi);
        return Dash(value);
    }
    private static string DateTimeText(string? value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTime)) return dateTime.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Vi);
        return Date(value);
    }
    private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private sealed record PrintField(string Key, string Label, string Value);
    private sealed record PrintTotal(string Key, string Label, string Value, bool Emphasis = false);
    private sealed record PrintColumn<T>(string Key, string Header, Func<T, int, string> Value, bool RightAligned = false);
}
