using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public static partial class InventoryAdjustmentBulkFile
{
    public const int MaxRows = 200;
    public const string TemplateCsv = "\uFEFFSKU,Tồn thực tế\r\n";

    public static IReadOnlyList<BulkInventoryAdjustmentInputRow> Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new InvalidOperationException("Không tìm thấy tệp đã chọn.");
        }

        var extension = Path.GetExtension(path);
        var sheet = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? ReadCsv(path)
            : extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? ReadXlsx(path)
                : throw new InvalidOperationException("Chỉ hỗ trợ tệp Excel .xlsx hoặc CSV .csv.");

        return ParseSheet(sheet);
    }

    public static IReadOnlyList<BulkInventoryAdjustmentInputRow> ParseSheet(
        IReadOnlyList<IReadOnlyList<string>> sheet)
    {
        if (sheet.Count < 2)
        {
            throw new InvalidOperationException("Tệp cần có dòng tiêu đề và ít nhất một dòng dữ liệu.");
        }

        var headers = sheet[0].Select(HeaderField).ToArray();
        if (!headers.Contains("sku", StringComparer.Ordinal)
            || !headers.Contains("actualQuantity", StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Tệp cần có hai cột bắt buộc: SKU và Tồn thực tế.");
        }

        var rows = new List<BulkInventoryAdjustmentInputRow>();
        for (var index = 1; index < sheet.Count; index++)
        {
            var cells = sheet[index];
            if (cells.All(cell => string.IsNullOrWhiteSpace(cell))) continue;

            var sku = string.Empty;
            var actualQuantity = string.Empty;
            var locationCode = string.Empty;
            var lotCode = string.Empty;

            for (var column = 0; column < headers.Length; column++)
            {
                var value = column < cells.Count ? cells[column].Trim() : string.Empty;
                switch (headers[column])
                {
                    case "sku": sku = value; break;
                    case "actualQuantity": actualQuantity = value; break;
                    case "locationCode": locationCode = value.ToUpperInvariant(); break;
                    case "lotCode": lotCode = value.ToUpperInvariant(); break;
                }
            }

            rows.Add(new BulkInventoryAdjustmentInputRow(
                index + 1,
                sku,
                actualQuantity,
                locationCode,
                lotCode));
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Tệp chưa có dòng dữ liệu.");
        }

        if (rows.Count > MaxRows)
        {
            throw new InvalidOperationException($"Mỗi lần kiểm tra tối đa {MaxRows} dòng.");
        }

        return rows;
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadCsv(string path)
    {
        var text = File.ReadAllText(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        var rows = new List<IReadOnlyList<string>>();
        var currentRow = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        void CompleteField()
        {
            currentRow.Add(field.ToString());
            field.Clear();
        }

        void CompleteRow()
        {
            CompleteField();
            rows.Add(currentRow.ToArray());
            currentRow = [];
        }

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted)
            {
                if (character == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            switch (character)
            {
                case '"':
                    quoted = true;
                    break;
                case ',':
                    CompleteField();
                    break;
                case '\r':
                    if (index + 1 < text.Length && text[index + 1] == '\n') index++;
                    CompleteRow();
                    break;
                case '\n':
                    CompleteRow();
                    break;
                default:
                    field.Append(character);
                    break;
            }
        }

        if (quoted)
        {
            throw new InvalidOperationException("Tệp CSV có dấu ngoặc kép chưa đóng.");
        }

        if (field.Length > 0 || currentRow.Count > 0)
        {
            CompleteRow();
        }

        if (rows.Count > 0 && rows[0].Count > 0)
        {
            var first = rows[0].ToArray();
            first[0] = first[0].TrimStart('\uFEFF');
            rows[0] = first;
        }

        return rows;
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadXlsx(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidOperationException("Tệp Excel không có trang tính dữ liệu.");

        using var stream = sheetEntry.Open();
        var document = XDocument.Load(stream);
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var rows = new List<IReadOnlyList<string>>();
        foreach (var row in document.Descendants(main + "row"))
        {
            var values = new SortedDictionary<int, string>();
            foreach (var cell in row.Elements(main + "c"))
            {
                var reference = cell.Attribute("r")?.Value ?? string.Empty;
                var column = ColumnIndex(reference);
                var type = cell.Attribute("t")?.Value;
                var value = string.Empty;

                if (type == "inlineStr")
                {
                    value = string.Concat(cell.Descendants(main + "t").Select(item => item.Value));
                }
                else
                {
                    var raw = cell.Element(main + "v")?.Value ?? string.Empty;
                    if (type == "s"
                        && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex)
                        && sharedIndex >= 0
                        && sharedIndex < sharedStrings.Count)
                    {
                        value = sharedStrings[sharedIndex];
                    }
                    else
                    {
                        value = raw;
                    }
                }

                values[column] = value;
            }

            if (values.Count == 0)
            {
                rows.Add([]);
                continue;
            }

            var lastColumn = values.Keys.Max();
            var rowValues = Enumerable.Repeat(string.Empty, lastColumn + 1).ToArray();
            foreach (var pair in values) rowValues[pair.Key] = pair.Value;
            rows.Add(rowValues);
        }

        return rows;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return [];

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return document
            .Descendants(main + "si")
            .Select(item => string.Concat(item.Descendants(main + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static int ColumnIndex(string reference)
    {
        var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
        if (letters.Length == 0) return 0;

        var result = 0;
        foreach (var character in letters.ToUpperInvariant())
        {
            result = (result * 26) + character - 'A' + 1;
        }

        return Math.Max(0, result - 1);
    }

    private static string? HeaderField(string value)
    {
        var header = RemoveDiacritics(value)
            .Trim()
            .ToLowerInvariant();
        header = WhitespaceRegex().Replace(header, " ");

        return header switch
        {
            "sku" => "sku",
            "ton thuc te" or "so luong ton thuc te" => "actualQuantity",
            "vi tri" or "ma vi tri" => "locationCode",
            "lo" or "ma lo" => "lotCode",
            _ => null
        };
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(character == 'đ' ? 'd' : character == 'Đ' ? 'D' : character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
