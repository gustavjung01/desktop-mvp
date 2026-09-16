using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace CongTy.Desktop.Purchasing;

public sealed record PurchaseOrderBulkInputRow(
    int RowNumber,
    string Sku,
    string Quantity,
    string UnitPrice,
    string DiscountMode,
    string DiscountValue,
    string TaxRate,
    string Note);

public static class PurchaseOrderBulkImport
{
    private static readonly string[] Headers =
    [
        "SKU",
        "Số lượng",
        "Đơn giá thủ công",
        "Kiểu chiết khấu",
        "Giá trị chiết khấu",
        "Thuế suất %",
        "Ghi chú"
    ];

    public static IReadOnlyList<PurchaseOrderBulkInputRow> ParseText(string? text)
    {
        var lines = (text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0) return [];

        var delimiter = lines.Any(x => x.Contains('\t')) ? '\t'
            : lines.Any(x => x.Contains(';')) ? ';'
            : ',';
        var start = LooksLikeHeader(Split(lines[0], delimiter)) ? 1 : 0;
        var rows = new List<PurchaseOrderBulkInputRow>();
        for (var index = start; index < lines.Length; index++)
        {
            var cells = Split(lines[index], delimiter);
            if (cells.Count == 0 || string.IsNullOrWhiteSpace(cells[0])) continue;
            rows.Add(ToRow(index + 1, cells));
        }

        return rows;
    }

    public static IReadOnlyList<PurchaseOrderBulkInputRow> ReadXlsx(byte[] bytes)
    {
        using var memory = new MemoryStream(bytes, writable: false);
        using var zip = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: false);
        var sheet = zip.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidDataException("Tệp XLSX không có trang dữ liệu đầu tiên.");
        var shared = LoadSharedStrings(zip);
        XDocument document;
        using (var stream = sheet.Open())
        {
            document = XDocument.Load(stream);
        }

        var ns = document.Root?.Name.Namespace ?? XNamespace.None;
        var matrix = new List<List<string>>();
        foreach (var row in document.Descendants(ns + "row"))
        {
            var values = new SortedDictionary<int, string>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? string.Empty;
                var column = ColumnIndex(reference);
                var type = (string?)cell.Attribute("t");
                string value;
                if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
                {
                    value = string.Concat(cell.Descendants(ns + "t").Select(x => x.Value));
                }
                else
                {
                    var raw = cell.Element(ns + "v")?.Value ?? string.Empty;
                    value = string.Equals(type, "s", StringComparison.Ordinal)
                        && int.TryParse(raw, out var sharedIndex)
                        && sharedIndex >= 0
                        && sharedIndex < shared.Count
                            ? shared[sharedIndex]
                            : raw;
                }

                values[column] = value.Trim();
            }

            if (values.Count == 0) continue;
            var max = Math.Max(0, values.Keys.Max());
            var cells = Enumerable.Range(0, max + 1)
                .Select(index => values.TryGetValue(index, out var value) ? value : string.Empty)
                .ToList();
            matrix.Add(cells);
        }

        if (matrix.Count == 0) return [];
        var start = LooksLikeHeader(matrix[0]) ? 1 : 0;
        var output = new List<PurchaseOrderBulkInputRow>();
        for (var index = start; index < matrix.Count; index++)
        {
            if (matrix[index].Count == 0 || string.IsNullOrWhiteSpace(matrix[index][0])) continue;
            output.Add(ToRow(index + 1, matrix[index]));
        }

        return output;
    }

    public static byte[] CreateTemplate()
    {
        using var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(zip, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "</Types>");
            Write(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");
            Write(zip, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Đơn mua hàng\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Write(zip, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "</Relationships>");

            var headerCells = string.Join(
                string.Empty,
                Headers.Select((header, index) =>
                    $"<c r=\"{ColumnName(index)}1\" t=\"inlineStr\"><is><t>{Escape(header)}</t></is></c>"));
            var sample = new[] { "SKU-MAU", "1", "", "TOTAL_AMOUNT", "0", "0", "Ghi chú mẫu" };
            var sampleCells = string.Join(
                string.Empty,
                sample.Select((value, index) =>
                    $"<c r=\"{ColumnName(index)}2\" t=\"inlineStr\"><is><t>{Escape(value)}</t></is></c>"));
            Write(zip, "xl/worksheets/sheet1.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                $"<row r=\"1\">{headerCells}</row><row r=\"2\">{sampleCells}</row>" +
                "</sheetData></worksheet>");
        }

        return memory.ToArray();
    }

    private static PurchaseOrderBulkInputRow ToRow(int rowNumber, IReadOnlyList<string> cells) =>
        new(
            rowNumber,
            Cell(cells, 0),
            Cell(cells, 1, "1"),
            Cell(cells, 2),
            NormalizeDiscountMode(Cell(cells, 3, "TOTAL_AMOUNT")),
            Cell(cells, 4, "0"),
            Cell(cells, 5, "0"),
            Cell(cells, 6));

    private static string NormalizeDiscountMode(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized is "PERCENT" or "PER_UNIT" or "TOTAL_AMOUNT" ? normalized : "TOTAL_AMOUNT";
    }

    private static string Cell(IReadOnlyList<string> cells, int index, string fallback = "") =>
        index < cells.Count && !string.IsNullOrWhiteSpace(cells[index]) ? cells[index].Trim() : fallback;

    private static List<string> Split(string line, char delimiter) =>
        line.Split(delimiter).Select(value => value.Trim().Trim('"')).ToList();

    private static bool LooksLikeHeader(IReadOnlyList<string> cells) =>
        cells.Count > 0 && (cells[0].Contains("sku", StringComparison.OrdinalIgnoreCase)
            || cells[0].Contains("mã hàng", StringComparison.OrdinalIgnoreCase));

    private static List<string> LoadSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return [];
        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        return doc.Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(x => x.Value)))
            .ToList();
    }

    private static int ColumnIndex(string cellReference)
    {
        var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
        if (letters.Length == 0) return 0;
        var value = 0;
        foreach (var letter in letters) value = value * 26 + (letter - 'A' + 1);
        return Math.Max(0, value - 1);
    }

    private static string ColumnName(int index)
    {
        var value = index + 1;
        var builder = new StringBuilder();
        while (value > 0)
        {
            value--;
            builder.Insert(0, (char)('A' + value % 26));
            value /= 26;
        }

        return builder.ToString();
    }

    private static string Escape(string value) =>
        System.Security.SecurityElement.Escape(value) ?? string.Empty;

    private static void Write(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }
}
