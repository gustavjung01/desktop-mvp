using System.Text;
using CongTy.Desktop.Pricing;
using CongTy.Desktop.Products;

namespace CongTy.Desktop.Operations;

internal static class DataExchangeFileHelper
{
    public static async Task<IReadOnlyList<Dictionary<string, string>>> ReadAsync(
        string filePath,
        IReadOnlyCollection<string> requiredColumns)
    {
        var matrix = await SpreadsheetMatrixReader.ReadAsync(filePath).ConfigureAwait(false);
        if (matrix.Count < 2) throw new InvalidOperationException("Tệp chưa có dòng dữ liệu.");

        var headers = matrix[0].Select(DataExchangePresentation.NormalizeHeader).ToArray();
        if (headers.Any(string.IsNullOrWhiteSpace)
            || headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Length)
            throw new InvalidOperationException("Tên cột đang trống hoặc bị trùng.");

        var missing = requiredColumns.Where(column => !headers.Contains(column, StringComparer.Ordinal)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Tệp đang thiếu cột: {string.Join(", ", missing.Select(DataExchangePresentation.Label))}.");

        var rows = new List<Dictionary<string, string>>();
        foreach (var source in matrix.Skip(1))
        {
            if (!source.Any(value => !string.IsNullOrWhiteSpace(value))) continue;
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < headers.Length; index++)
                row[headers[index]] = index < source.Length ? source[index].Trim() : string.Empty;
            rows.Add(row);
        }
        if (rows.Count == 0) throw new InvalidOperationException("Tệp chưa có dữ liệu.");
        return rows;
    }

    public static void Write(
        string filePath,
        string sheetName,
        IReadOnlyList<string> columns,
        IReadOnlyList<string[]> rows,
        string format)
    {
        var headers = columns.Select(DataExchangePresentation.Label).ToArray();
        if (format == "xlsx")
        {
            PricingWorkbookWriter.Write(
                filePath,
                [new PricingWorkbookSheet(sheetName, headers, rows)]);
            return;
        }
        if (format != "csv") throw new ArgumentException("Định dạng tệp không hợp lệ.", nameof(format));

        var builder = new StringBuilder("﻿");
        WriteCsvRow(builder, headers);
        foreach (var row in rows) WriteCsvRow(builder, row);
        File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void WriteCsvRow(StringBuilder builder, IReadOnlyList<string> values)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0) builder.Append(',');
            var value = GuardFormula(values[index] ?? string.Empty);
            if (value.IndexOfAny([',', '"', '\r', '\n']) >= 0)
            {
                builder.Append('"');
                builder.Append(value.Replace(""", """", StringComparison.Ordinal));
                builder.Append('"');
            }
            else
            {
                builder.Append(value);
            }
        }
        builder.Append("\r\n");
    }

    private static string GuardFormula(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@'
            ? "'" + value
            : value;
    }
}
