using System.IO;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace CongTy.Desktop.Products;

internal static class SpreadsheetMatrixReader
{
    private const long MaxFileBytes = 20L * 1024 * 1024;

    public static Task<IReadOnlyList<string[]>> ReadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Chưa chọn tệp dữ liệu.", nameof(filePath));

        return Task.Run<IReadOnlyList<string[]>>(() => Read(filePath));
    }

    private static IReadOnlyList<string[]> Read(string filePath)
    {
        var file = new FileInfo(filePath);
        if (!file.Exists)
            throw new FileNotFoundException("Không tìm thấy tệp dữ liệu.", filePath);
        if (file.Length > MaxFileBytes)
            throw new InvalidOperationException("Tệp dữ liệu vượt quá giới hạn 20 MB.");

        return file.Extension.ToLowerInvariant() switch
        {
            ".csv" => ReadCsv(file.FullName),
            ".xlsx" => ReadXlsx(file.FullName),
            _ => throw new InvalidOperationException("Chỉ hỗ trợ tệp .xlsx hoặc .csv.")
        };
    }

    private static IReadOnlyList<string[]> ReadCsv(string filePath)
    {
        var text = File.ReadAllText(filePath, DetectEncoding(filePath));
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (ch == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(ch);
                }
                continue;
            }

            if (ch == '"')
            {
                quoted = true;
            }
            else if (ch == ',')
            {
                row.Add(field.ToString().Trim());
                field.Clear();
            }
            else if (ch == '\r' || ch == '\n')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                row.Add(field.ToString().Trim());
                field.Clear();
                AddRow(rows, row);
                row = [];
            }
            else
            {
                field.Append(ch);
            }
        }

        if (quoted)
            throw new InvalidOperationException("Tệp CSV có dấu ngoặc kép chưa đóng.");

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString().Trim());
            AddRow(rows, row);
        }

        return rows;
    }

    private static Encoding DetectEncoding(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        Span<byte> bom = stackalloc byte[3];
        var count = stream.Read(bom);
        if (count >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            return new UTF8Encoding(true);
        return new UTF8Encoding(false, true);
    }

    private static IReadOnlyList<string[]> ReadXlsx(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var sharedStrings = ReadSharedStrings(archive);
        var worksheetPath = ResolveFirstWorksheetPath(archive);
        var worksheet = GetEntry(archive, worksheetPath);

        using var stream = worksheet.Open();
        var document = XDocument.Load(stream, LoadOptions.None);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var rows = new List<string[]>();
        foreach (var rowElement in document.Descendants(ns + "row"))
        {
            var values = new SortedDictionary<int, string>();
            var inferredIndex = 0;

            foreach (var cell in rowElement.Elements(ns + "c"))
            {
                var reference = (string?)cell.Attribute("r");
                var index = string.IsNullOrWhiteSpace(reference)
                    ? inferredIndex
                    : ColumnIndex(reference);

                values[index] = CellText(cell, sharedStrings, ns).Trim();
                inferredIndex = index + 1;
            }

            if (values.Count == 0)
                continue;

            var width = values.Keys.Max() + 1;
            var row = new string[width];
            foreach (var pair in values)
                row[pair.Key] = pair.Value;
            AddRow(rows, row);
        }

        return rows;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
            return [];

        using var stream = entry.Open();
        var document = XDocument.Load(stream, LoadOptions.None);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return document
            .Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static string ResolveFirstWorksheetPath(ZipArchive archive)
    {
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRel = "http://schemas.openxmlformats.org/package/2006/relationships";

        using var workbookStream = GetEntry(archive, "xl/workbook.xml").Open();
        var workbook = XDocument.Load(workbookStream, LoadOptions.None);
        var firstSheet = workbook.Descendants(main + "sheet").FirstOrDefault()
            ?? throw new InvalidOperationException("Tệp Excel không có trang tính.");
        var relationId = (string?)firstSheet.Attribute(rel + "id")
            ?? throw new InvalidOperationException("Tệp Excel thiếu liên kết trang tính.");

        using var relationsStream = GetEntry(archive, "xl/_rels/workbook.xml.rels").Open();
        var relations = XDocument.Load(relationsStream, LoadOptions.None);
        var target = relations
            .Descendants(packageRel + "Relationship")
            .Where(item => string.Equals((string?)item.Attribute("Id"), relationId, StringComparison.Ordinal))
            .Select(item => (string?)item.Attribute("Target"))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("Không xác định được trang tính đầu tiên.");

        var normalized = target.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            return normalized;

        while (normalized.StartsWith("../", StringComparison.Ordinal))
            normalized = normalized[3..];

        return $"xl/{normalized}";
    }

    private static ZipArchiveEntry GetEntry(ZipArchive archive, string path) =>
        archive.GetEntry(path)
        ?? throw new InvalidOperationException($"Tệp Excel thiếu thành phần bắt buộc: {path}.");

    private static string CellText(XElement cell, IReadOnlyList<string> sharedStrings, XNamespace ns)
    {
        var type = (string?)cell.Attribute("t");
        if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
            return string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));

        var value = cell.Element(ns + "v")?.Value ?? string.Empty;
        if (string.Equals(type, "s", StringComparison.Ordinal)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            && index >= 0
            && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        if (string.Equals(type, "b", StringComparison.Ordinal))
            return value == "1" ? "TRUE" : "FALSE";

        return value;
    }

    private static int ColumnIndex(string reference)
    {
        var result = 0;
        var found = false;
        foreach (var ch in reference)
        {
            if (!char.IsLetter(ch))
                break;

            found = true;
            result = checked(result * 26 + (char.ToUpperInvariant(ch) - 'A' + 1));
        }

        if (!found)
            return 0;
        return result - 1;
    }

    private static void AddRow(List<string[]> rows, IReadOnlyList<string> source)
    {
        var last = source.Count - 1;
        while (last >= 0 && string.IsNullOrWhiteSpace(source[last]))
            last--;

        if (last < 0)
            return;

        var trimmed = new string[last + 1];
        for (var i = 0; i <= last; i++)
            trimmed[i] = source[i] ?? string.Empty;
        rows.Add(trimmed);
    }
}
