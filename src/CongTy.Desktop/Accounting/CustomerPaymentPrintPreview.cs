using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Accounting;

internal static class CustomerPaymentPrintPreview
{
    public static void Print(Window? owner, CustomerPaymentData payment, DocumentPrintTemplateData template)
    {
        var document = DocumentPrintTemplateRuntime.CreateDocument(template, 12, new Thickness(34));
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            "PHIẾU THU",
            payment.DocumentNumber,
            "Chứng từ thu tiền khách hàng");

        AddIf(document, template, "status", "Kết quả", CustomerPaymentPresentation.Status(payment.Status));
        AddIf(document, template, "customer", "Khách hàng", CustomerPaymentPresentation.Party(payment.CustomerCode, payment.CustomerName));
        AddIf(document, template, "receiving_unit", "Đơn vị nhận tiền", CustomerPaymentPresentation.Party(payment.WarehouseCode, payment.WarehouseName));
        AddIf(document, template, "payment_date", "Ngày thu", CustomerPaymentPresentation.Date(payment.PaymentDate));
        AddIf(document, template, "payment_method", "Hình thức nhận tiền", CustomerPaymentPresentation.PaymentMethod(payment.PaymentMethod));
        AddIf(document, template, "bank_reference", "Mã giao dịch ngân hàng", payment.ExternalReference ?? "—");
        AddIf(
            document,
            template,
            "remitting_employee",
            "Nhân viên nộp tiền",
            string.IsNullOrWhiteSpace(payment.RemittingEmployeeName)
                ? "—"
                : CustomerPaymentPresentation.Party(payment.RemittingEmployeeCode, payment.RemittingEmployeeName));
        AddIf(document, template, "recorded_by", "Người ghi nhận", payment.PostedBy);

        if (DocumentPrintTemplateRuntime.Shows(template, "total_received"))
        {
            document.Blocks.Add(new Paragraph(new Run(
                $"SỐ TIỀN ĐÃ NHẬN: {CustomerPaymentPresentation.Money(payment.OriginalAmount, payment.CurrencyCode)}"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 15
            });
        }
        AddIf(document, template, "total_allocated", "Đã ghi vào đơn", CustomerPaymentPresentation.Money(payment.AllocatedAmount, payment.CurrencyCode));
        AddIf(document, template, "total_unallocated", "Chưa gắn với đơn", CustomerPaymentPresentation.Money(payment.RemainingAmount, payment.CurrencyCode));

        if (DocumentPrintTemplateRuntime.Shows(template, "note"))
        {
            var note = payment.Status == "reversed"
                ? $"ĐÃ HỦY{(string.IsNullOrWhiteSpace(payment.ReversalReason) ? string.Empty : $" — {payment.ReversalReason}")}"
                : payment.Note;
            if (!string.IsNullOrWhiteSpace(note)) document.Blocks.Add(Line($"Ghi chú: {note}"));
        }

        DocumentPrintTemplateRuntime.AddSignatures(document, template, "Người nộp tiền", "Người lập phiếu", "Thủ quỹ / Kế toán");

        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template, new Thickness(34));
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Phiếu thu {payment.DocumentNumber}");
    }

    private static void AddIf(FlowDocument document, DocumentPrintTemplateData template, string key, string label, string value)
    {
        if (!DocumentPrintTemplateRuntime.Shows(template, key)) return;
        document.Blocks.Add(Line($"{label}: {value}"));
    }

    private static Paragraph Line(string text) =>
        new(new Run(text)) { Margin = new Thickness(0, 4, 0, 4) };
}
