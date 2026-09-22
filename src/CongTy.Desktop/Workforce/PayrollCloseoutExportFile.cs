using System.IO;
using System.Globalization;
using ClosedXML.Excel;
using CongTy.ApiClient;
using CongTy.Contracts;
using SkiaSharp;

namespace CongTy.Desktop.Workforce;

public static class PayrollCloseoutExportFile
{
    public const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string PdfContentType = "application/pdf";

    public static ApiDownloadFile CreatePayrollWorkbook(PayrollCloseoutData closeout)
    {
        ArgumentNullException.ThrowIfNull(closeout);
        var closeSnapshot = closeout.CloseSnapshot
            ?? throw new InvalidOperationException("Kỳ lương chưa có hồ sơ chốt.");

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Bảng lương đã chốt");
        var headers = new[]
        {
            "Mã nhân sự", "Họ tên", "Chi nhánh", "Công được tính", "Công chuẩn",
            "Giờ tăng ca xác nhận", "Lương theo công", "Thu nhập thêm", "Hoàn chi phí",
            "Khấu trừ", "Tổng thu nhập", "Thực nhận", "Phiên bản phiếu"
        };

        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(1, column + 1).SetValue(headers[column]);

        for (var row = 0; row < closeout.Payslips.Length; row++)
        {
            var item = closeout.Payslips[row];
            var pay = item.Snapshot.Pay;
            var values = new[]
            {
                item.Snapshot.Employee.Code,
                item.Snapshot.Employee.Name,
                item.Snapshot.Employee.BranchName ?? "Toàn Công Ty",
                pay.PayableWorkDays.ToString(CultureInfo.InvariantCulture),
                pay.StandardWorkDays.ToString(CultureInfo.InvariantCulture),
                (pay.ConfirmedOvertimeMinutes / 60m).ToString("0.##", CultureInfo.InvariantCulture),
                pay.SalaryAmount,
                pay.IncomeTotal,
                pay.ReimbursementTotal,
                pay.DeductionTotal,
                pay.GrossIncome,
                pay.NetPay,
                item.Revision.ToString(CultureInfo.InvariantCulture)
            };

            for (var column = 0; column < values.Length; column++)
                sheet.Cell(row + 2, column + 1).SetValue(GuardSpreadsheetFormula(values[column]));
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(2, closeout.Payslips.Length + 1), headers.Length).SetAutoFilter();
        for (var column = 1; column <= headers.Length; column++)
            sheet.Column(column).AdjustToContents(12, 42);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var period = closeSnapshot.Snapshot.Period;
        return new ApiDownloadFile(
            stream.ToArray(),
            XlsxContentType,
            $"Bang-luong-{SafeDate(period.From)}-den-{SafeDate(period.To)}.xlsx");
    }

    public static ApiDownloadFile CreatePayslipPdf(PayrollPayslipSnapshotData payslip)
    {
        ArgumentNullException.ThrowIfNull(payslip);
        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream)
            ?? throw new InvalidOperationException("Không tạo được tài liệu PDF.");
        using var typeface = SKTypeface.FromFamilyName("Segoe UI");
        using var titleFont = new SKFont(typeface, 20) { Embolden = true };
        using var headingFont = new SKFont(typeface, 12) { Embolden = true };
        using var textFont = new SKFont(typeface, 11);
        using var paint = new SKPaint { IsAntialias = true };

        var canvas = document.BeginPage(595, 842);
        var y = 58f;
        canvas.DrawText("PHIẾU LƯƠNG", 48, y, SKTextAlign.Left, titleFont, paint);
        y += 32;
        var employee = payslip.Snapshot.Employee;
        var pay = payslip.Snapshot.Pay;
        DrawLine(canvas, textFont, paint, ref y, $"{employee.Code} · {employee.Name}");
        DrawLine(canvas, textFont, paint, ref y, $"Kỳ: {PayrollCloseoutPresentation.PeriodText(payslip.Snapshot.Period)}");
        DrawLine(canvas, textFont, paint, ref y, $"Chi nhánh: {PayrollFoundationPresentation.ScopeText(employee.BranchName)}");
        DrawLine(canvas, textFont, paint, ref y, $"Phiên bản: Lần {payslip.Revision}");
        y += 10;

        canvas.DrawText("Công & thu nhập", 48, y, SKTextAlign.Left, headingFont, paint);
        y += 24;
        DrawLine(canvas, textFont, paint, ref y, $"Công được tính: {pay.PayableWorkDays}/{pay.StandardWorkDays} ngày");
        DrawLine(canvas, textFont, paint, ref y, $"Tăng ca đã xác nhận: {PayrollAggregationPresentation.OvertimeText(pay.ConfirmedOvertimeMinutes)}");
        DrawLine(canvas, textFont, paint, ref y, $"Lương theo công: {PayrollFoundationPresentation.MoneyText(pay.SalaryAmount)}");
        DrawLine(canvas, textFont, paint, ref y, $"Thu nhập thêm: {PayrollFoundationPresentation.MoneyText(pay.IncomeTotal)}");
        DrawLine(canvas, textFont, paint, ref y, $"Tổng thu nhập: {PayrollFoundationPresentation.MoneyText(pay.GrossIncome)}");
        DrawLine(canvas, textFont, paint, ref y, $"Hoàn chi phí: {PayrollFoundationPresentation.MoneyText(pay.ReimbursementTotal)}");
        DrawLine(canvas, textFont, paint, ref y, $"Khấu trừ: {PayrollFoundationPresentation.MoneyText(pay.DeductionTotal)}");
        DrawLine(canvas, headingFont, paint, ref y, $"Thực nhận: {PayrollFoundationPresentation.MoneyText(pay.NetPay)}");

        if (payslip.Snapshot.Adjustments.Length > 0)
        {
            y += 10;
            canvas.DrawText("Điều chỉnh sau chốt", 48, y, SKTextAlign.Left, headingFont, paint);
            y += 24;
            foreach (var adjustment in payslip.Snapshot.Adjustments)
            {
                DrawLine(
                    canvas,
                    textFont,
                    paint,
                    ref y,
                    $"{adjustment.Name} · {(adjustment.Direction == "REVERSE" ? "Ghi giảm" : "Ghi thêm")} · {PayrollFoundationPresentation.MoneyText(adjustment.Amount)}");
                DrawLine(canvas, textFont, paint, ref y, $"Lý do: {adjustment.Reason}");
            }
        }

        document.EndPage();
        document.Close();

        var code = SafeFilePart(employee.Code, "nhan-su");
        return new ApiDownloadFile(stream.ToArray(), PdfContentType, $"Phieu-luong-{code}.pdf");
    }

    private static void DrawLine(SKCanvas canvas, SKFont font, SKPaint paint, ref float y, string value)
    {
        canvas.DrawText(value ?? string.Empty, 48, y, SKTextAlign.Left, font, paint);
        y += 20;
    }

    private static string GuardSpreadsheetFormula(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') || trimmed.StartsWith('@')
            ? $"'{value}"
            : value;
    }

    private static string SafeDate(string value)
    {
        var raw = (value ?? string.Empty).Trim();
        return raw.Length >= 10 ? raw[..10].Replace("/", "-", StringComparison.Ordinal) : "ky-luong";
    }

    private static string SafeFilePart(string? value, string fallback)
    {
        var raw = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var chars = raw.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-' ? ch : '-').ToArray();
        var safe = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }
}
