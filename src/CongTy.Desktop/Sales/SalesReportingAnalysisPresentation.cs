using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public static partial class SalesReportingPresentation
{
    public static readonly IReadOnlyList<SalesReportingOption> AnalysisDimensions =
    [
        new("products", "Sản phẩm"),
        new("customerGroups", "Loại khách"),
        new("channels", "Kênh bán"),
        new("productGroups", "Nhóm hàng")
    ];

    public static readonly IReadOnlyList<SalesReportingOption> AnalysisMetrics =
    [
        new("revenue", "Doanh thu"),
        new("quantity", "Sản lượng")
    ];

    public static readonly IReadOnlyList<SalesReportingOption> QuantityDisplays =
    [
        new("sold", "Theo ĐVT bán"),
        new("carton", "Ưu tiên Thùng"),
        new("base", "Ưu tiên ĐVT lẻ")
    ];

    public static IReadOnlyList<SalesReportingOption> AnalysisSorts(bool revenue, bool quantity)
    {
        var result = new List<SalesReportingOption>
        {
            new("name-asc", "Tên A → Z")
        };
        if (revenue)
        {
            result.Add(new("revenue-desc", "Doanh thu cao → thấp"));
            result.Add(new("revenue-asc", "Doanh thu thấp → cao"));
        }
        if (quantity)
        {
            result.Add(new("quantity-desc", "Sản lượng cao → thấp"));
            result.Add(new("quantity-asc", "Sản lượng thấp → cao"));
        }
        return result;
    }

    public static (string Row, string Column) DefaultAnalysisDimensions(string currentDimension)
    {
        if (string.Equals(currentDimension, "products", StringComparison.Ordinal))
            return ("products", "customerGroups");

        if (AnalysisDimensions.Any(option => string.Equals(option.Key, currentDimension, StringComparison.Ordinal)))
            return ("products", currentDimension);

        return ("products", "customerGroups");
    }

    public static IReadOnlyList<SalesExportColumnDefinition> AnalysisExportColumns(
        SalesReportingDashboardData report,
        string rowDimension,
        string columnDimension,
        bool revenue,
        bool quantity)
    {
        if (!AnalysisDimensions.Any(option => option.Key == rowDimension)
            || !AnalysisDimensions.Any(option => option.Key == columnDimension)
            || string.Equals(rowDimension, columnDimension, StringComparison.Ordinal))
            return [];

        var result = new List<SalesExportColumnDefinition>
        {
            new("meta:code", rowDimension == "products" ? "Mã sản phẩm" : "Mã", true),
            new("meta:name", DimensionLabel(rowDimension), true)
        };

        if (rowDimension == "products" || quantity)
            result.Add(new("meta:unit", "ĐVT", true));

        var categories = DimensionRows(report.Breakdowns, columnDimension)
            .Select(row => new
            {
                Key = AnalysisCategoryKey(columnDimension, row),
                Label = AnalysisCategoryLabel(columnDimension, row)
            })
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(item => item.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        var currencies = report.Summary.Revenues
            .Select(item => item.CurrencyCode?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (currencies.Length == 0) currencies = ["VND"];

        foreach (var category in categories)
        {
            if (revenue)
            {
                foreach (var currency in currencies)
                {
                    var suffix = currencies.Length > 1 ? $" ({currency})" : string.Empty;
                    result.Add(new(
                        $"cat:{category.Key}|revenue|{Uri.EscapeDataString(currency)}",
                        $"{category.Label} - Doanh thu{suffix}",
                        true));
                }
            }

            if (quantity)
                result.Add(new($"cat:{category.Key}|quantity", $"{category.Label} - Sản lượng", true));
        }

        if (revenue)
        {
            foreach (var currency in currencies)
            {
                var suffix = currencies.Length > 1 ? $" ({currency})" : string.Empty;
                result.Add(new(
                    $"total:revenue:{Uri.EscapeDataString(currency)}",
                    $"Tổng doanh thu{suffix}",
                    true));
            }
        }

        if (quantity)
            result.Add(new("total:quantity", "Tổng sản lượng", true));

        return result;
    }

    private static IEnumerable<SalesBreakdownData> DimensionRows(
        SalesReportingBreakdownsData breakdowns,
        string dimension) =>
        dimension switch
        {
            "products" => breakdowns.Products,
            "customerGroups" => breakdowns.CustomerGroups,
            "channels" => breakdowns.Channels,
            "productGroups" => breakdowns.ProductGroups,
            _ => []
        };

    private static string AnalysisCategoryKey(string dimension, SalesBreakdownData row)
    {
        var main = Uri.EscapeDataString(FirstIdentity(row.Id, row.Code, row.Name, "__none__"));
        if (!string.Equals(dimension, "products", StringComparison.Ordinal))
            return $"{dimension}:{main}";

        var unit = Uri.EscapeDataString(FirstIdentity(row.Unit.Id, row.Unit.Code, row.Unit.Name, "__none__"));
        return $"{dimension}:{main}:{unit}";
    }

    private static string AnalysisCategoryLabel(string dimension, SalesBreakdownData row)
    {
        var name = string.IsNullOrWhiteSpace(row.Name) ? "Chưa xác định" : row.Name.Trim();
        if (!string.Equals(dimension, "products", StringComparison.Ordinal)) return name;

        var unit = !string.IsNullOrWhiteSpace(row.Unit.Name)
            ? row.Unit.Name.Trim()
            : !string.IsNullOrWhiteSpace(row.Unit.Code)
                ? row.Unit.Code.Trim()
                : string.Empty;
        return unit.Length > 0 ? $"{name} · {unit}" : name;
    }

    private static string FirstIdentity(string? first, string? second, string? third, string fallback) =>
        !string.IsNullOrWhiteSpace(first) ? first.Trim()
        : !string.IsNullOrWhiteSpace(second) ? second.Trim()
        : !string.IsNullOrWhiteSpace(third) ? third.Trim()
        : fallback;
}
