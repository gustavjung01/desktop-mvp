using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public static partial class StocktakeFileCodec
{
    public static readonly string[] CountHeaders =
    [
        "Phiếu kiểm kê", "SKU", "Tên sản phẩm", "ĐVT", "Mã lô", "Mã vị trí",
        "Số đếm thực tế", "Lý do", "Ghi chú"
    ];

    public static readonly string[] ResultHeaders =
    [
        "SKU", "Tên sản phẩm", "ĐVT", "Mã lô", "Mã vị trí",
        "Tồn hệ thống", "Thực đếm", "Chênh lệch", "Lý do", "Ghi chú"
    ];

    private sealed record ImportPatch(StocktakeLineRow Line, string Count, string Reason, string Note);

    public static void ExportCountXlsx(string path, InventoryStocktakeData stocktake, IReadOnlyList<StocktakeLineRow> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(stocktake);
        ArgumentNullException.ThrowIfNull(lines);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SafeSheetName($"Kiểm kê {stocktake.StocktakeNumber}"));
        WriteHeaders(sheet, CountHeaders);
        var rowNumber = 2;
        foreach (var line in lines)
        {
            WriteRow(sheet, rowNumber++,
            [
                stocktake.StocktakeNumber, line.Sku, line.Product, line.Unit,
                line.Data.LotCode ?? string.Empty, line.Data.LocationCode ?? string.Empty,
                line.CountedQuantity, line.Reason, line.Note
            ]);
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
    }

    public static int ImportCountFile(string path, InventoryStocktakeData stocktake, IReadOnlyList<StocktakeLineRow> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(stocktake);
        ArgumentNullException.ThrowIfNull(lines);

        var rows = ReadRows(path);
        var usedLineIds = new HashSet<string>(StringComparer.Ordinal);
        var patches = new List<ImportPatch>();
        var errors = new List<string>();

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var actualCount = Value(row, "Số đếm thực tế", "actualCount").Trim();
            if (actualCount.Length == 0) continue;

            var displayRow = index + 2;
            var fileStocktake = Value(row, "Phiếu kiểm kê", "stocktake").Trim();
            if (fileStocktake.Length > 0
                && !string.Equals(Normalize(fileStocktake), Normalize(stocktake.StocktakeNumber), StringComparison.Ordinal))
            {
                errors.Add($"Dòng {displayRow}: file thuộc phiếu {fileStocktake}, không phải {stocktake.StocktakeNumber}.");
                continue;
            }

            var skuRaw = Value(row, "SKU", "sku").Trim();
            var sku = Normalize(skuRaw);
            if (sku.Length == 0)
            {
                errors.Add($"Dòng {displayRow}: thiếu SKU.");
                continue;
            }

            var candidates = lines.Where(line =>
                    string.Equals(Normalize(line.Data.BaseSku), sku, StringComparison.Ordinal)
                    || string.Equals(Normalize(line.Data.SourceSku), sku, StringComparison.Ordinal))
                .ToList();

            var lotCode = Normalize(Value(row, "Mã lô", "lotCode"));
            var locationCode = Normalize(Value(row, "Mã vị trí", "locationCode"));
            if (lotCode.Length > 0)
                candidates = candidates.Where(line => string.Equals(Normalize(line.Data.LotCode), lotCode, StringComparison.Ordinal)).ToList();
            if (locationCode.Length > 0)
                candidates = candidates.Where(line => string.Equals(Normalize(line.Data.LocationCode), locationCode, StringComparison.Ordinal)).ToList();

            if (lotCode.Length == 0
                && candidates.Select(line => Normalize(line.Data.LotCode)).Distinct(StringComparer.Ordinal).Count() > 1)
            {
                errors.Add($"Dòng {displayRow}: SKU {skuRaw} có nhiều lô trong phiếu, cần ghi Mã lô.");
                continue;
            }
            if (locationCode.Length == 0
                && candidates.Select(line => Normalize(line.Data.LocationCode)).Distinct(StringComparer.Ordinal).Count() > 1)
            {
                errors.Add($"Dòng {displayRow}: SKU {skuRaw} có nhiều vị trí trong phiếu, cần ghi Mã vị trí.");
                continue;
            }
            if (candidates.Count != 1)
            {
                errors.Add($"Dòng {displayRow}: không tìm được đúng một dòng của SKU {skuRaw} trong phiếu.");
                continue;
            }

            var line = candidates[0];
            if (usedLineIds.Contains(line.Data.Id))
            {
                errors.Add($"Dòng {displayRow}: SKU {skuRaw} bị trùng phạm vi trong file.");
                continue;
            }

            string normalizedCount;
            try
            {
                normalizedCount = NormalizeQuantity(actualCount, $"Dòng {displayRow} - Số đếm thực tế");
            }
            catch (FormatException exception)
            {
                errors.Add(exception.Message);
                continue;
            }

            var reason = Value(row, "Lý do", "reason").Trim();
            var note = Value(row, "Ghi chú", "note").Trim();
            if (reason.Length > 500)
            {
                errors.Add($"Dòng {displayRow}: Lý do vượt quá 500 ký tự.");
                continue;
            }
            if (note.Length > 2000)
            {
                errors.Add($"Dòng {displayRow}: Ghi chú vượt quá 2000 ký tự.");
                continue;
            }

            usedLineIds.Add(line.Data.Id);
            patches.Add(new ImportPatch(line, normalizedCount, reason, note));
        }

        if (errors.Count > 0)
        {
            var preview = string.Join(" ", errors.Take(6));
            var remaining = errors.Count > 6 ? $" Còn {errors.Count - 6} lỗi khác." : string.Empty;
            throw new InvalidDataException(preview + remaining);
        }
        if (patches.Count == 0)
            throw new InvalidDataException("File chưa có dòng nào được nhập Số đếm thực tế.");

        foreach (var patch in patches)
        {
            patch.Line.CountedQuantity = patch.Count;
            if (patch.Reason.Length > 0) patch.Line.Reason = patch.Reason;
            if (patch.Note.Length > 0) patch.Line.Note = patch.Note;
        }

        return patches.Count;
    }

    public static void ExportResultXlsx(string path, InventoryStocktakeData stocktake, IReadOnlyList<StocktakeLineRow> lines)
    {
        EnsureRevealed(stocktake);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SafeSheetName($"Kết quả {stocktake.StocktakeNumber}"));
        WriteHeaders(sheet, ResultHeaders);
        var rowNumber = 2;
        foreach (var line in lines) WriteRow(sheet, rowNumber++, ResultRow(line));
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
    }

    public static void ExportResultCsv(string path, InventoryStocktakeData stocktake, IReadOnlyList<StocktakeLineRow> lines)
    {
        EnsureRevealed(stocktake);
        var rows = new List<IReadOnlyList<string>> { ResultHeaders };
        rows.AddRange(lines.Select(ResultRow));
        var csv = string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(CsvCell))));
        File.WriteAllText(path, "\uFEFF" + csv, new UTF8Encoding(false));
    }

    public static string CsvCell(string? value)
    {
        var raw = value ?? string.Empty;
        var guarded = FormulaLikeCell().IsMatch(raw) || SuspiciousNegativeCell().IsMatch(raw) ? "'" + raw : raw;
        return "\"" + guarded.Replace("\"", "\"\"") + "\"";
    }

    private static IReadOnlyList<string> ResultRow(StocktakeLineRow line) =>
    [
        line.Sku, line.Product, line.Unit, line.Data.LotCode ?? string.Empty,
        JoinLocation(line.Data.LocationCode, line.Data.LocationName),
        line.Data.ExpectedBaseQuantity ?? string.Empty,
        line.Data.CountedBaseQuantity ?? string.Empty,
        RawDifference(line.Data),
        line.Data.Reason ?? string.Empty,
        line.Data.Note ?? string.Empty
    ];

    private static string RawDifference(InventoryStocktakeLineData line)
    {
        if (!string.IsNullOrWhiteSpace(line.FinalDelta)) return line.FinalDelta!;
        if (!decimal.TryParse(line.CountedBaseQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var counted)
            || !decimal.TryParse(line.ExpectedBaseQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var expected))
            return string.Empty;
        return (counted - expected).ToString("0.############", CultureInfo.InvariantCulture);
    }

    private static void EnsureRevealed(InventoryStocktakeData stocktake)
    {
        if (stocktake.Status is "draft" or "recount_required")
            throw new InvalidOperationException("Chỉ xuất kết quả sau khi số hệ thống được mở để đối chiếu.");
    }

    private static List<Dictionary<string, string>> ReadRows(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)) return ReadXlsx(path);
        if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)) return ReadCsv(path);
        throw new InvalidDataException("Chỉ hỗ trợ file .xlsx hoặc .csv.");
    }

    private static List<Dictionary<string, string>> ReadXlsx(string path)
    {
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet(1);
        var firstRow = sheet.FirstRowUsed() ?? throw new InvalidDataException("File Excel không có dữ liệu.");
        var firstCell = firstRow.FirstCellUsed() ?? throw new InvalidDataException("File Excel thiếu tiêu đề cột.");
        var lastCell = firstRow.LastCellUsed() ?? throw new InvalidDataException("File Excel thiếu tiêu đề cột.");
        var firstColumn = firstCell.Address.ColumnNumber;
        var lastColumn = lastCell.Address.ColumnNumber;
        var headers = Enumerable.Range(firstColumn, lastColumn - firstColumn + 1)
            .Select(column => sheet.Cell(firstRow.RowNumber(), column).GetFormattedString().Trim())
            .ToArray();

        RequireImportHeaders(headers);
        var rows = new List<Dictionary<string, string>>();
        var lastRowNumber = sheet.LastRowUsed()?.RowNumber() ?? firstRow.RowNumber();
        for (var rowNumber = firstRow.RowNumber() + 1; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var offset = 0; offset < headers.Length; offset++)
            {
                var header = headers[offset];
                if (header.Length == 0) continue;
                row[header] = sheet.Cell(rowNumber, firstColumn + offset).GetFormattedString();
            }
            rows.Add(row);
        }
        return rows;
    }

    private static List<Dictionary<string, string>> ReadCsv(string path)
    {
        var records = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
        if (records.Count == 0) throw new InvalidDataException("File CSV không có dữ liệu.");
        var headers = records[0].Select(value => value.Trim().TrimStart('\uFEFF')).ToArray();
        RequireImportHeaders(headers);
        var rows = new List<Dictionary<string, string>>();
        foreach (var record in records.Skip(1))
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
            {
                if (headers[index].Length == 0) continue;
                row[headers[index]] = index < record.Count ? record[index] : string.Empty;
            }
            rows.Add(row);
        }
        return rows;
    }

    private static List<List<string>> ParseCsv(string content)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < content.Length; index++)
        {
            var current = content[index];
            if (quoted)
            {
                if (current == '\"' && index + 1 < content.Length && content[index + 1] == '\"')
                {
                    field.Append('\"');
                    index++;
                }
                else if (current == '\"') quoted = false;
                else field.Append(current);
                continue;
            }

            if (current == '\"') quoted = true;
            else if (current == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (current == '\r' || current == '\n')
            {
                if (current == '\r' && index + 1 < content.Length && content[index + 1] == '\n') index++;
                row.Add(field.ToString());
                field.Clear();
                rows.Add(row);
                row = [];
            }
            else field.Append(current);
        }

        if (quoted) throw new InvalidDataException("File CSV có dấu ngoặc kép chưa đóng.");
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }
        return rows;
    }

    private static void RequireImportHeaders(IEnumerable<string> headers)
    {
        var normalized = new HashSet<string>(headers.Select(NormalizeHeader), StringComparer.Ordinal);
        if (!normalized.Contains(NormalizeHeader("SKU")) || !normalized.Contains(NormalizeHeader("Số đếm thực tế")))
            throw new InvalidDataException("File phải có cột SKU và Số đếm thực tế.");
    }

    private static string Value(IReadOnlyDictionary<string, string> row, string canonical, string alias)
    {
        foreach (var pair in row)
        {
            var normalized = NormalizeHeader(pair.Key);
            if (normalized == NormalizeHeader(canonical) || normalized == NormalizeHeader(alias))
                return pair.Value ?? string.Empty;
        }
        return string.Empty;
    }

    private static string NormalizeHeader(string? value) =>
        Normalize(value).Replace(" ", string.Empty, StringComparison.Ordinal);

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string NormalizeQuantity(string text, string label)
    {
        var parsed = decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)
            ? invariant
            : decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var vietnamese)
                ? vietnamese
                : (decimal?)null;
        if (parsed is null || parsed < 0) throw new FormatException($"{label}: số đếm không hợp lệ.");

        var bits = decimal.GetBits(parsed.Value);
        var scale = (bits[3] >> 16) & 0x7F;
        if (scale > 12) throw new FormatException($"{label}: chỉ hỗ trợ tối đa 12 chữ số thập phân.");
        return parsed.Value.ToString("0.############", CultureInfo.InvariantCulture);
    }

    private static void WriteHeaders(IXLWorksheet sheet, IReadOnlyList<string> headers)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            sheet.Cell(1, index + 1).Value = headers[index];
            sheet.Cell(1, index + 1).Style.Font.Bold = true;
        }
    }

    private static void WriteRow(IXLWorksheet sheet, int rowNumber, IReadOnlyList<string> values)
    {
        for (var index = 0; index < values.Count; index++)
            sheet.Cell(rowNumber, index + 1).Value = values[index] ?? string.Empty;
    }

    private static string SafeSheetName(string value)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return safe.Length <= 31 ? safe : safe[..31];
    }

    private static string JoinLocation(string? code, string? name) =>
        string.Join(" · ", new[] { code, name }.Where(value => !string.IsNullOrWhiteSpace(value)));

    [GeneratedRegex(@"^[=+@]")]
    private static partial Regex FormulaLikeCell();

    [GeneratedRegex(@"^-(?!\d+(?:[.,]\d+)?$)")]
    private static partial Regex SuspiciousNegativeCell();
}
