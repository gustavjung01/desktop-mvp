using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace CongTy.Desktop.Pricing;

public sealed record PricingWorkbookSheet(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<string[]> Rows);

public static class PricingWorkbookWriter
{
    private const string SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string OfficeRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypeNs = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static void Write(string filePath, IReadOnlyList<PricingWorkbookSheet> sheets)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Chưa chọn nơi lưu tệp Excel.", nameof(filePath));
        if (sheets.Count == 0) throw new InvalidOperationException("Không có dữ liệu để tạo tệp Excel.");

        using var stream = File.Create(filePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false);
        WriteContentTypes(archive, sheets.Count);
        WriteRootRelationships(archive);
        WriteWorkbook(archive, sheets);
        WriteWorkbookRelationships(archive, sheets.Count);
        for (var index = 0; index < sheets.Count; index++)
            WriteWorksheet(archive, index + 1, sheets[index]);
    }

    private static void WriteContentTypes(ZipArchive archive, int sheetCount)
    {
        using var writer = CreateXmlWriter(archive, "[Content_Types].xml");
        writer.WriteStartDocument();
        writer.WriteStartElement("Types", ContentTypeNs);
        writer.WriteStartElement("Default", ContentTypeNs);
        writer.WriteAttributeString("Extension", "rels");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-package.relationships+xml");
        writer.WriteEndElement();
        writer.WriteStartElement("Default", ContentTypeNs);
        writer.WriteAttributeString("Extension", "xml");
        writer.WriteAttributeString("ContentType", "application/xml");
        writer.WriteEndElement();
        writer.WriteStartElement("Override", ContentTypeNs);
        writer.WriteAttributeString("PartName", "/xl/workbook.xml");
        writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
        writer.WriteEndElement();
        for (var index = 1; index <= sheetCount; index++)
        {
            writer.WriteStartElement("Override", ContentTypeNs);
            writer.WriteAttributeString("PartName", $"/xl/worksheets/sheet{index}.xml");
            writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteRootRelationships(ZipArchive archive)
    {
        using var writer = CreateXmlWriter(archive, "_rels/.rels");
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", PackageRelNs);
        writer.WriteStartElement("Relationship", PackageRelNs);
        writer.WriteAttributeString("Id", "rId1");
        writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument");
        writer.WriteAttributeString("Target", "xl/workbook.xml");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbook(ZipArchive archive, IReadOnlyList<PricingWorkbookSheet> sheets)
    {
        using var writer = CreateXmlWriter(archive, "xl/workbook.xml");
        writer.WriteStartDocument();
        writer.WriteStartElement("workbook", SpreadsheetNs);
        writer.WriteAttributeString("xmlns", "r", null, OfficeRelNs);
        writer.WriteStartElement("sheets", SpreadsheetNs);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < sheets.Count; index++)
        {
            writer.WriteStartElement("sheet", SpreadsheetNs);
            writer.WriteAttributeString("name", UniqueSheetName(sheets[index].Name, used));
            writer.WriteAttributeString("sheetId", (index + 1).ToString());
            writer.WriteAttributeString("r", "id", OfficeRelNs, $"rId{index + 1}");
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbookRelationships(ZipArchive archive, int sheetCount)
    {
        using var writer = CreateXmlWriter(archive, "xl/_rels/workbook.xml.rels");
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", PackageRelNs);
        for (var index = 1; index <= sheetCount; index++)
        {
            writer.WriteStartElement("Relationship", PackageRelNs);
            writer.WriteAttributeString("Id", $"rId{index}");
            writer.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet");
            writer.WriteAttributeString("Target", $"worksheets/sheet{index}.xml");
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorksheet(ZipArchive archive, int sheetIndex, PricingWorkbookSheet sheet)
    {
        using var writer = CreateXmlWriter(archive, $"xl/worksheets/sheet{sheetIndex}.xml");
        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", SpreadsheetNs);
        writer.WriteStartElement("sheetData", SpreadsheetNs);
        WriteRow(writer, 1, sheet.Headers);
        for (var index = 0; index < sheet.Rows.Count; index++)
            WriteRow(writer, index + 2, sheet.Rows[index]);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteRow(XmlWriter writer, int rowNumber, IReadOnlyList<string> cells)
    {
        writer.WriteStartElement("row", SpreadsheetNs);
        writer.WriteAttributeString("r", rowNumber.ToString());
        for (var index = 0; index < cells.Count; index++)
        {
            writer.WriteStartElement("c", SpreadsheetNs);
            writer.WriteAttributeString("r", $"{ColumnName(index + 1)}{rowNumber}");
            writer.WriteAttributeString("t", "inlineStr");
            writer.WriteStartElement("is", SpreadsheetNs);
            writer.WriteStartElement("t", SpreadsheetNs);
            writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
            writer.WriteString(cells[index] ?? string.Empty);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private static XmlWriter CreateXmlWriter(ZipArchive archive, string path)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        return XmlWriter.Create(entry.Open(), new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            CloseOutput = true
        });
    }

    private static string ColumnName(int value)
    {
        var result = string.Empty;
        while (value > 0)
        {
            value--;
            result = (char)('A' + value % 26) + result;
            value /= 26;
        }
        return result;
    }

    private static string UniqueSheetName(string source, ISet<string> used)
    {
        var invalid = new HashSet<char>(['\\', '/', '?', '*', '[', ']', ':']);
        var clean = new string((source ?? string.Empty).Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
        if (clean.Length == 0) clean = "Dữ liệu";
        if (clean.Length > 31) clean = clean[..31];
        var candidate = clean;
        var suffix = 2;
        while (!used.Add(candidate))
        {
            var tail = $" {suffix++}";
            candidate = clean[..Math.Min(clean.Length, 31 - tail.Length)] + tail;
        }
        return candidate;
    }
}
