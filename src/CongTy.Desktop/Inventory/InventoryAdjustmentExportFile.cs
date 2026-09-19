using System.IO;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ClosedXML.Excel;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record InventoryAdjustmentExportColumnDefinition(
    string Key,
    string Label,
    bool DefaultSelected);

public sealed class InventoryAdjustmentExportColumnOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public InventoryAdjustmentExportColumnOption(
        string key,
        string label,
        bool isSelected)
    {
        Key = key;
        Label = label;
        _isSelected = isSelected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Key { get; }
    public string Label { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}

public static class InventoryAdjustmentExportFile
{
    public const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static IReadOnlyList<InventoryAdjustmentExportColumnDefinition> Columns { get; } =
    [
        new("adjustmentNumber", "Số phiếu", true),
        new("createdAt", "Ngày lập", true),
        new("warehouseCode", "Mã kho", true),
        new("warehouseName", "Kho", true),
        new("documentKind", "Loại phiếu", true),
        new("adjustmentDirection", "Điều chỉnh", true),
        new("status", "Trạng thái", true),
        new("reasonLabel", "Lý do", true),
        new("reasonNote", "Diễn giải", false),
        new("lineCount", "Số dòng", true),
        new("submittedAt", "Gửi duyệt lúc", false),
        new("approvedAt", "Duyệt lúc", false),
        new("postedAt", "Cập nhật tồn lúc", true),
        new("cancelledAt", "Hủy lúc", false),
        new("reversedAt", "Hoàn tác lúc", false)
    ];

    private static readonly IReadOnlyDictionary<string, InventoryAdjustmentExportColumnDefinition> ColumnByKey =
        Columns.ToDictionary(item => item.Key, StringComparer.Ordinal);

    public static ApiDownloadFile Create(
        IReadOnlyList<InventoryAdjustmentData> documents,
        IReadOnlyList<string> requestedColumns,
        string format,
        DateTimeOffset? generatedAt = null)
    {
        var columns = NormalizeColumns(requestedColumns);
        var normalizedFormat = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
            ? "csv"
            : string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase)
                ? "xlsx"
                : throw new InvalidOperationException("Định dạng file không hợp lệ.");

        var headers = columns.Select(item => item.Label).ToArray();
        var rows = documents
            .Select(document => columns.Select(column => ValueFor(document, column.Key)).ToArray())
            .ToArray();
        var date = (generatedAt ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var fileName = $"Dieu-chinh-ton-{date}.{normalizedFormat}";

        if (normalizedFormat == "csv")
        {
            return new ApiDownloadFile(
                CreateCsv(headers, rows),
                "text/csv; charset=utf-8",
                fileName);
        }

        return new ApiDownloadFile(
            CreateXlsx(headers, rows),
            XlsxContentType,
            fileName);
    }

    private static InventoryAdjustmentExportColumnDefinition[] NormalizeColumns(
        IReadOnlyList<string> requestedColumns)
    {
        if (requestedColumns.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một cột để xuất.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<InventoryAdjustmentExportColumnDefinition>();
        foreach (var key in requestedColumns)
        {
            if (string.IsNullOrWhiteSpace(key)
                || !ColumnByKey.TryGetValue(key.Trim(), out var definition))
            {
                throw new InvalidOperationException("Có cột xuất dữ liệu không hợp lệ.");
            }

            if (seen.Add(definition.Key)) result.Add(definition);
        }

        if (result.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một cột để xuất.");

        return result.ToArray();
    }

    private static string ValueFor(InventoryAdjustmentData document, string key) => key switch
    {
        "adjustmentNumber" => document.AdjustmentNumber,
        "createdAt" => DateTimeText(document.CreatedAt),
        "warehouseCode" => document.WarehouseCode ?? string.Empty,
        "warehouseName" => document.WarehouseName ?? string.Empty,
        "documentKind" => InventoryAdjustmentPresentation.Kind(document.DocumentKind),
        "adjustmentDirection" => document.AdjustmentDirection switch
        {
            "IN" => "Tăng tồn",
            "OUT" => "Giảm tồn",
            _ => string.Empty
        },
        "status" => InventoryAdjustmentPresentation.Status(document.Status),
        "reasonLabel" => document.ReasonLabel ?? document.ReasonCode,
        "reasonNote" => document.ReasonNote ?? string.Empty,
        "lineCount" => document.LineCount.ToString(CultureInfo.InvariantCulture),
        "submittedAt" => DateTimeText(document.SubmittedAt),
        "approvedAt" => DateTimeText(document.ApprovedAt),
        "postedAt" => DateTimeText(document.PostedAt),
        "cancelledAt" => DateTimeText(document.CancelledAt),
        "reversedAt" => DateTimeText(document.ReversedAt),
        _ => string.Empty
    };

    private static string DateTimeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return string.Empty;
        }

        return parsed
            .ToOffset(TimeSpan.FromHours(7))
            .ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
    }

    private static byte[] CreateCsv(
        IReadOnlyList<string> headers,
        IReadOnlyList<string[]> rows)
    {
        var lines = new List<string>(rows.Count + 1)
        {
            string.Join(",", headers.Select(CsvCell))
        };
        lines.AddRange(rows.Select(row => string.Join(",", row.Select(CsvCell))));
        var text = string.Join("\r\n", lines);
        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(text))
            .ToArray();
    }

    private static string CsvCell(string? value)
    {
        var text = GuardSpreadsheetFormula(value ?? string.Empty);
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private static string GuardSpreadsheetFormula(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') || trimmed.StartsWith('@')
            ? $"'{value}"
            : value;
    }

    private static byte[] CreateXlsx(
        IReadOnlyList<string> headers,
        IReadOnlyList<string[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Điều chỉnh tồn");

        for (var column = 0; column < headers.Count; column++)
        {
            sheet.Cell(1, column + 1).SetValue(headers[column]);
        }

        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < headers.Count; column++)
            {
                sheet.Cell(row + 2, column + 1).SetValue(rows[row][column] ?? string.Empty);
            }
        }

        var header = sheet.Range(1, 1, 1, headers.Count);
        header.Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(2, rows.Count + 1), headers.Count).SetAutoFilter();

        for (var column = 0; column < headers.Count; column++)
        {
            var maxLength = rows
                .Select(row => row[column]?.Length ?? 0)
                .Append(headers[column].Length)
                .DefaultIfEmpty(12)
                .Max();
            sheet.Column(column + 1).Width = Math.Clamp(maxLength + 2, 12, 48);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
