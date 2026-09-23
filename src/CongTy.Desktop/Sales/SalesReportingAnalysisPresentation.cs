using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public static class SalesReportingAnalysisPresentation
{
    public static readonly IReadOnlyList<SalesReportingOption> Dimensions =
    [
        new("products", "Sản phẩm"),
        new("customerGroups", "Loại khách"),
        new("channels", "Kênh bán"),
        new("productGroups", "Nhóm hàng")
    ];

    public static readonly IReadOnlyList<SalesReportingOption> QuantityDisplays =
    [
        new("sold", "Theo ĐVT bán"),
        new("carton", "Ưu tiên Thùng"),
        new("base", "Ưu tiên ĐVT lẻ")
    ];

    public static IReadOnlyList<string> DefaultDimensions(string dimension) => dimension switch
    {
        "products" => ["products", "customerGroups"],
        "customerGroups" or "channels" or "productGroups" => ["products", dimension],
        _ => ["products", "customerGroups"]
    };

    public static IReadOnlyList<SalesReportingOption> SortOptions(bool revenue, bool quantity)
    {
        var result = new List<SalesReportingOption> { new("name-asc", "Tên A → Z") };
        if (revenue) { result.Add(new("revenue-desc", "Doanh thu cao → thấp")); result.Add(new("revenue-asc", "Doanh thu thấp → cao")); }
        if (quantity) { result.Add(new("quantity-desc", "Sản lượng cao → thấp")); result.Add(new("quantity-asc", "Sản lượng thấp → cao")); }
        return result;
    }

    public static IReadOnlyList<SalesExportColumnDefinition> BuildColumns(SalesReportingDashboardData? report, string rowDimension, string columnDimension, bool revenue, bool quantity)
    {
        if (report is null || rowDimension == columnDimension || !IsDimension(rowDimension) || !IsDimension(columnDimension)) return [];
        var result = new List<SalesExportColumnDefinition>
        {
            new("meta:code", rowDimension == "products" ? "Mã sản phẩm" : "Mã", true),
            new("meta:name", Label(rowDimension), true)
        };
        if (rowDimension == "products" || quantity) result.Add(new("meta:unit", "ĐVT", true));

        var categories = Rows(report, columnDimension)
            .Select(row => new { Key = CategoryKey(columnDimension, row), Label = CategoryLabel(columnDimension, row) })
            .GroupBy(item => item.Key, StringComparer.Ordinal).Select(group => group.First())
            .OrderBy(item => item.Label, StringComparer.CurrentCulture).ToArray();
        var currencies = report.Summary.Revenues.Select(item => item.CurrencyCode)
            .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();
        if (revenue && currencies.Count == 0) currencies.Add("VND");

        foreach (var category in categories)
        {
            if (revenue)
            {
                foreach (var currency in currencies)
                {
                    var suffix = currencies.Count > 1 ? $" ({currency})" : string.Empty;
                    result.Add(new($"cat:{category.Key}|revenue|{Encode(currency)}", $"{category.Label} - Doanh thu{suffix}", true));
                }
            }
            if (quantity) result.Add(new($"cat:{category.Key}|quantity", $"{category.Label} - Sản lượng", true));
        }
        if (revenue)
        {
            foreach (var currency in currencies)
            {
                var suffix = currencies.Count > 1 ? $" ({currency})" : string.Empty;
                result.Add(new($"total:revenue:{Encode(currency)}", $"Tổng doanh thu{suffix}", true));
            }
        }
        if (quantity) result.Add(new("total:quantity", "Tổng sản lượng", true));
        return result;
    }

    public static string ExportDimension(string row, string column, bool revenue, bool quantity)
    {
        var metric = revenue && quantity ? "both" : quantity ? "quantity" : "revenue";
        return $"analysis.{row}.{column}.{metric}";
    }

    public static string Summary(string row, string column, bool revenue, bool quantity, string quantityDisplay, string sort)
    {
        if (!IsDimension(row) || !IsDimension(column) || row == column) return "Chọn đúng 2 tiêu chí để tạo báo cáo phân tích.";
        var metrics = revenue && quantity ? "Doanh thu và Sản lượng" : revenue ? "Doanh thu" : "Sản lượng";
        var quantityText = quantity ? $" · {QuantityDisplays.First(item => item.Key == quantityDisplay).Label}" : string.Empty;
        var sortText = SortOptions(revenue, quantity).FirstOrDefault(item => item.Key == sort)?.Label ?? "Tên A → Z";
        return $"Sẽ xuất Báo cáo {Label(row)} theo {Label(column)}, gồm {metrics}{quantityText} · {sortText}.";
    }

    private static bool IsDimension(string value) => Dimensions.Any(item => item.Key == value);
    private static string Label(string value) => Dimensions.First(item => item.Key == value).Label;
    private static SalesBreakdownData[] Rows(SalesReportingDashboardData report, string dimension) => dimension switch
    {
        "products" => report.Breakdowns.Products,
        "customerGroups" => report.Breakdowns.CustomerGroups,
        "channels" => report.Breakdowns.Channels,
        "productGroups" => report.Breakdowns.ProductGroups,
        _ => []
    };

    private static string CategoryKey(string dimension, SalesBreakdownData row)
    {
        var main = Encode(First(row.Id, row.Code, row.Name, "__none__"));
        if (dimension != "products") return $"{dimension}:{main}";
        var unit = Encode(First(row.Unit.Id, row.Unit.Code, row.Unit.Name, "__none__"));
        return $"{dimension}:{main}:{unit}";
    }

    private static string CategoryLabel(string dimension, SalesBreakdownData row)
    {
        var name = string.IsNullOrWhiteSpace(row.Name) ? "Chưa xác định" : row.Name.Trim();
        if (dimension != "products") return name;
        var unit = First(row.Unit.Name, row.Unit.Code, string.Empty);
        return string.IsNullOrWhiteSpace(unit) ? name : $"{name} · {unit}";
    }

    private static string First(params string?[] values) => values.Select(value => value?.Trim()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    private static string Encode(string value) => Uri.EscapeDataString(value)
        .Replace("%21", "!", StringComparison.OrdinalIgnoreCase).Replace("%27", "'", StringComparison.OrdinalIgnoreCase)
        .Replace("%28", "(", StringComparison.OrdinalIgnoreCase).Replace("%29", ")", StringComparison.OrdinalIgnoreCase)
        .Replace("%2A", "*", StringComparison.OrdinalIgnoreCase);
}
