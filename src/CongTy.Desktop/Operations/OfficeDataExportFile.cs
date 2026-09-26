using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;
using CongTy.ApiClient;
using Microsoft.Win32;

namespace CongTy.Desktop.Operations;

public sealed record OfficeExportSheet(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);

public static class OfficeDataExportFile
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static IReadOnlyList<string> Row(params string?[] values) =>
        values.Select(value => value ?? string.Empty).ToArray();

    public static ApiDownloadFile Xlsx(string fileName, params OfficeExportSheet[] sheets)
    {
        using var workbook = new XLWorkbook();
        foreach (var sheet in sheets)
        {
            var worksheet = workbook.Worksheets.Add(SafeSheetName(sheet.Name));
            for (var column = 0; column < sheet.Headers.Count; column++)
                worksheet.Cell(1, column + 1).Value = GuardSpreadsheetText(sheet.Headers[column]);

            for (var row = 0; row < sheet.Rows.Count; row++)
            {
                var values = sheet.Rows[row];
                for (var column = 0; column < sheet.Headers.Count; column++)
                {
                    var value = column < values.Count ? values[column] : string.Empty;
                    worksheet.Cell(row + 2, column + 1).Value = GuardSpreadsheetText(value);
                }
            }

            if (sheet.Headers.Count > 0)
            {
                worksheet.Range(1, 1, 1, sheet.Headers.Count).Style.Font.Bold = true;
                worksheet.SheetView.FreezeRows(1);
                if (sheet.Rows.Count > 0)
                    worksheet.Range(1, 1, sheet.Rows.Count + 1, sheet.Headers.Count).SetAutoFilter();

                foreach (var column in worksheet.ColumnsUsed())
                {
                    column.AdjustToContents();
                    column.Width = Math.Clamp(column.Width + 1d, 10d, 42d);
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ApiDownloadFile(stream.ToArray(), XlsxContentType, fileName);
    }

    public static ApiDownloadFile TemplateXlsx(string fileName, string sheetName, params string[] headers) =>
        Xlsx(fileName, new OfficeExportSheet(sheetName, headers, []));

    public static ApiDownloadFile TemplateCsv(string fileName, params string[] headers)
    {
        var line = string.Join(",", headers.Select(CsvCell));
        return new ApiDownloadFile(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(line + "\r\n"),
            "text/csv; charset=utf-8",
            fileName);
    }

    public static ApiDownloadFile Csv(
        string fileName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(CsvCell)));
        foreach (var row in rows)
        {
            var values = Enumerable.Range(0, headers.Count)
                .Select(index => CsvCell(index < row.Count ? row[index] : string.Empty));
            builder.AppendLine(string.Join(",", values));
        }

        return new ApiDownloadFile(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(builder.ToString()),
            "text/csv; charset=utf-8",
            fileName);
    }

    public static string DateLabel(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        var datePart = text.Length > 10 ? text[..10] : text;
        if (DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
        return text.Length == 0 ? "—" : text;
    }

    public static string FilterValue(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length == 0 || string.Equals(text, "all", StringComparison.OrdinalIgnoreCase) ? string.Empty : text;
    }

    public static string EmploymentType(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "PROBATION" => "Thử việc",
        "PERMANENT" => "Chính thức",
        "FIXED_TERM" => "Hợp đồng có thời hạn",
        "PART_TIME" => "Bán thời gian",
        "TEMPORARY" => "Thời vụ",
        "OTHER" => "Khác",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string AttendanceAction(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "CHECK_IN" => "Vào làm",
        "TEMP_EXIT" => "Ra ngoài",
        "RETURN" => "Quay lại nơi làm việc",
        "CHECK_OUT" => "Kết thúc làm việc",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string MovementReason(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "WORK_BUSINESS" => "Ra ngoài làm việc",
        "PERSONAL" => "Việc cá nhân",
        "BREAK" => "Nghỉ giữa ca",
        "OTHER" => "Lý do khác",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string AttendanceSource(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "QR" => "Mã QR",
        "FACE" => "Nhận diện khuôn mặt",
        "MANUAL" => "Chấm công trực tiếp",
        "ADJUSTMENT" => "Điều chỉnh công",
        "SYSTEM" => "Hệ thống ghi nhận",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string Validation(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "VALID" => "Hợp lệ",
        "PENDING" => "Chờ xác minh",
        "INVALID" => "Không hợp lệ",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string LeaveStatus(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "SUBMITTED" => "Chờ duyệt",
        "APPROVED" => "Đã duyệt",
        "REJECTED" => "Từ chối",
        "CANCELLED" => "Đã hủy",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string LeavePart(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "FULL_DAY" => "Cả ngày",
        "FIRST_HALF" => "Nửa ca đầu",
        "SECOND_HALF" => "Nửa ca sau",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string OvertimeStatus(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "SUBMITTED" => "Chờ duyệt",
        "APPROVED" => "Đã duyệt",
        "REJECTED" => "Từ chối",
        "ACTUAL_RECORDED" => "Đã ghi nhận thực tế",
        "CONFIRMED" => "Đã xác nhận giờ tính",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string ViolationStatus(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "EXPLANATION_SUBMITTED" => "Đã gửi giải trình",
        "UNDER_REVIEW" => "Đang xem xét",
        "RESOLVED" => "Đã kết luận",
        _ => string.IsNullOrWhiteSpace(value) ? "Chưa giải trình" : value.Trim(),
    };

    public static string ViolationOutcome(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "CONFIRMED" => "Xác nhận vi phạm",
        "EXCUSED" => "Chấp nhận giải trình",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string ScheduleKind(string? value) =>
        string.Equals(value, "WORK", StringComparison.OrdinalIgnoreCase) ? "Ngày làm việc" : "Ngày nghỉ";

    public static string ScheduleSource(string? value) => (value ?? string.Empty).ToUpperInvariant() switch
    {
        "OVERRIDE" => "Điều chỉnh riêng",
        "POLICY" => "Theo chính sách",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
    };

    public static string Weekday(int value) => value switch
    {
        0 => "Chủ nhật",
        1 => "Thứ 2",
        2 => "Thứ 3",
        3 => "Thứ 4",
        4 => "Thứ 5",
        5 => "Thứ 6",
        6 => "Thứ 7",
        _ => value.ToString(CultureInfo.InvariantCulture),
    };

    public static string ClockText(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length >= 5 ? text[..5] : text.Length == 0 ? "—" : text;
    }

    private static string GuardSpreadsheetText(string? value)
    {
        var text = value ?? string.Empty;
        var trimmed = text.TrimStart();
        return trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') || trimmed.StartsWith('@')
            ? "'" + text
            : text;
    }

    private static string CsvCell(string? value)
    {
        var safe = GuardSpreadsheetText(value);
        return safe.Contains(',') || safe.Contains('"') || safe.Contains('\n') || safe.Contains('\r')
            ? "\"" + safe.Replace("\"", "\"\"") + "\""
            : safe;
    }

    private static string SafeSheetName(string value)
    {
        var text = string.Concat((value ?? "Dữ liệu").Where(character => !"[]:*?/\\".Contains(character)));
        if (string.IsNullOrWhiteSpace(text)) text = "Dữ liệu";
        return text.Length <= 31 ? text : text[..31];
    }
}

public static class OfficeExportDialog
{
    public static async Task RunAsync(FrameworkElement owner, Func<Task<ApiDownloadFile?>> factory)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var file = await factory().ConfigureAwait(true);
            if (file is null) return;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var dialog = new SaveFileDialog
            {
                Title = "Lưu tệp xuất dữ liệu",
                FileName = file.FileName,
                AddExtension = true,
                DefaultExt = extension,
                Filter = extension == ".csv" ? "CSV (*.csv)|*.csv" : "Excel (*.xlsx)|*.xlsx",
            };
            if (dialog.ShowDialog(Window.GetWindow(owner)) != true) return;
            await File.WriteAllBytesAsync(dialog.FileName, file.Content).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Công Ty Desktop",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }
}
