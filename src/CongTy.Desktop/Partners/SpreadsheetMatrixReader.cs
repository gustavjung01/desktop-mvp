using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace CongTy.Desktop.Partners;

public static class SpreadsheetMatrixReader
{
    private const long MaxSourceFileBytes = 20L * 1024 * 1024;
    private const long MaxXmlEntryBytes = 50L * 1024 * 1024;
    public static async Task<IReadOnlyList<string[]>> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Chưa chọn tệp dữ liệu.", nameof(filePath));
        }

        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            throw new FileNotFoundException("Không tìm thấy tệp dữ liệu.", filePath);
        }

        if (info.Length is < 1 or > MaxSourceFileBytes)
        {
            throw new InvalidOperationException("Tệp phải có dung lượng từ 1 byte đến 20 MB.");
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".csv" => await ReadCsvAsync(filePath, cancellationToken).ConfigureAwait(false),
            ".xlsx" => await ReadXlsxAsync(filePath, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException("Chỉ hỗ trợ tệp .xlsx hoặc .csv.")
        };
    }

    private static async Task<IReadOnlyList<string[]>> ReadCsvAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        if (text.Length > 0 && text[0] == '﻿')
        {
            text = text[1..];
        }

        var rows = ParseCsv(text)
            .Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))
            .Select(row => row.ToArray())
            .ToArray();

        ValidateMatrix(rows);
        return rows;
    }

    private static Task<IReadOnlyList<string[]>> ReadXlsxAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var archive = ZipFile.OpenRead(filePath);
        XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

        var sharedStrings = ReadSharedStrings(archive, spreadsheet);

        var workbookEntry = archive.GetEntry("xl/workbook.xml")
            ?? throw new InvalidOperationException("Tệp Excel không có workbook hợp lệ.");
        EnsureEntrySize(workbookEntry, 2L * 1024 * 1024);
        var workbook = LoadXml(workbookEntry);

        var firstSheet = workbook
            .Descendants(spreadsheet + "sheet")
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Tệp Excel không có sheet dữ liệu.");

        var relationshipId = firstSheet.Attribute(relationships + "id")?.Value;
        if (string.IsNullOrWhiteSpace(relationshipId))
        {
            throw new InvalidOperationException("Không xác định được sheet dữ liệu trong tệp Excel.");
        }

        var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels")
            ?? throw new InvalidOperationException("Tệp Excel thiếu quan hệ workbook.");
        EnsureEntrySize(relsEntry, 2L * 1024 * 1024);
        var rels = LoadXml(relsEntry);
        var target = rels
            .Descendants(packageRelationships + "Relationship")
            .FirstOrDefault(element => string.Equals(
                element.Attribute("Id")?.Value,
                relationshipId,
                StringComparison.Ordinal))
            ?.Attribute("Target")
            ?.Value;

        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException("Không tìm thấy nội dung sheet đầu tiên.");
        }

        var sheetPath = ResolveWorksheetPath(target);
        var sheetEntry = archive.GetEntry(sheetPath)
            ?? throw new InvalidOperationException("Không đọc được sheet đầu tiên.");
        EnsureEntrySize(sheetEntry, MaxXmlEntryBytes);

        var sheet = LoadXml(sheetEntry);
        var rows = new List<string[]>();

        foreach (var rowElement in sheet.Descendants(spreadsheet + "row"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cells = new SortedDictionary<int, string>();
            foreach (var cell in rowElement.Elements(spreadsheet + "c"))
            {
                var reference = cell.Attribute("r")?.Value;
                var columnIndex = ColumnIndex(reference);
                var type = cell.Attribute("t")?.Value;
                var value = CellText(cell, type, sharedStrings, spreadsheet);
                cells[columnIndex] = value;
            }

            if (cells.Count == 0)
            {
                continue;
            }

            var lastColumn = cells.Keys.Max();
            var row = Enumerable.Range(0, lastColumn + 1)
                .Select(index => cells.TryGetValue(index, out var value) ? value : string.Empty)
                .ToArray();

            if (row.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                rows.Add(row);
            }
        }

        ValidateMatrix(rows);
        return Task.FromResult<IReadOnlyList<string[]>>(rows);
    }

    private static IReadOnlyList<string> ReadSharedStrings(
        ZipArchive archive,
        XNamespace spreadsheet)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        EnsureEntrySize(entry, MaxXmlEntryBytes);
        var document = LoadXml(entry);
        return document
            .Descendants(spreadsheet + "si")
            .Select(item => string.Concat(item.Descendants(spreadsheet + "t").Select(node => node.Value)))
            .ToArray();
    }

    private static string CellText(
        XElement cell,
        string? type,
        IReadOnlyList<string> sharedStrings,
        XNamespace spreadsheet)
    {
        if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
        {
            return string.Concat(cell.Descendants(spreadsheet + "t").Select(node => node.Value));
        }

        var raw = cell.Element(spreadsheet + "v")?.Value ?? string.Empty;
        if (string.Equals(type, "s", StringComparison.Ordinal)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            && index >= 0
            && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        if (string.Equals(type, "b", StringComparison.Ordinal))
        {
            return raw == "1" ? "TRUE" : "FALSE";
        }

        return raw;
    }

    private static int ColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return 0;
        }

        var value = 0;
        foreach (var character in cellReference)
        {
            if (!char.IsAsciiLetter(character))
            {
                break;
            }

            value = (value * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
        }

        return Math.Max(0, value - 1);
    }

    private static string ResolveWorksheetPath(string target)
    {
        var normalized = target.Replace('\\', '/').TrimStart('/');
        while (normalized.StartsWith("../", StringComparison.Ordinal))
        {
            normalized = normalized[3..];
        }

        if (normalized.StartsWith("xl/", StringComparison.Ordinal))
        {
            return normalized;
        }

        return $"xl/{normalized}";
    }

    private static void EnsureEntrySize(ZipArchiveEntry entry, long maxBytes)
    {
        if (entry.Length is < 0 || entry.Length > maxBytes)
        {
            throw new InvalidOperationException("Tệp Excel có thành phần vượt quá giới hạn cho phép.");
        }
    }

    private static XDocument LoadXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.None);
    }

    private static IReadOnlyList<IReadOnlyList<string>> ParseCsv(string text)
    {
        var rows = new List<IReadOnlyList<string>>();
        var currentRow = new List<string>();
        var currentCell = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];

            if (inQuotes)
            {
                if (character == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        currentCell.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentCell.Append(character);
                }

                continue;
            }

            if (character == '"')
            {
                inQuotes = true;
                continue;
            }

            if (character == ',')
            {
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                continue;
            }

            if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                rows.Add(currentRow.ToArray());
                currentRow = [];
                continue;
            }

            currentCell.Append(character);
        }

        if (inQuotes)
        {
            throw new InvalidOperationException("Tệp CSV có dấu ngoặc kép chưa đóng.");
        }

        if (currentCell.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentCell.ToString());
            rows.Add(currentRow.ToArray());
        }

        return rows;
    }

    private static void ValidateMatrix(IReadOnlyCollection<string[]> rows)
    {
        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Tệp không có dữ liệu.");
        }

        if (rows.Count > 10_001)
        {
            throw new InvalidOperationException("Tệp vượt quá 10.000 dòng dữ liệu.");
        }

        var maxColumns = rows.Max(row => row.Length);
        if (maxColumns < 1)
        {
            throw new InvalidOperationException("Tệp không có cột dữ liệu.");
        }

        if (maxColumns > 100)
        {
            throw new InvalidOperationException("Tệp vượt quá 100 cột.");
        }
    }
}
