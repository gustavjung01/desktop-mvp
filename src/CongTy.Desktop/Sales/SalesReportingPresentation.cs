using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public static partial class SalesReportingPresentation
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static readonly IReadOnlyList<SalesReportingOption> Dimensions =
    [
        new("customers", "Khách hàng"),
        new("customerGroups", "Loại khách"),
        new("channels", "Kênh bán"),
        new("products", "Sản phẩm"),
        new("productGroups", "Nhóm hàng"),
        new("employees", "Nhân viên bán hàng")
    ];

    public static readonly IReadOnlyList<SalesReportingOption> Comparisons =
    [
        new("all", "Tất cả"),
        new("comparable", "Có thể so sánh"),
        new("new", "Mới trong kỳ"),
        new("inactive", "Không phát sinh kỳ này")
    ];

    public static readonly IReadOnlyList<SalesReportingOption> Presets =
    [
        new("", "Chọn kỳ"),
        new("today", "Hôm nay"),
        new("last7", "7 ngày"),
        new("thisMonth", "Tháng này"),
        new("previousMonth", "Tháng trước")
    ];

    public static string Number(string? value, int maxFractionDigits = 6)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Float, Invariant, out var number)) return normalized;
        var decimals = Math.Clamp(maxFractionDigits, 0, 12);
        var format = decimals == 0 ? "#,##0" : $"#,##0.{new string('#', decimals)}";
        return number.ToString(format, Vietnamese);
    }

    public static string Money(string? value, string? currencyCode)
    {
        var code = string.IsNullOrWhiteSpace(currencyCode) ? "—" : currencyCode.Trim().ToUpperInvariant();
        var suffix = string.Equals(code, "VND", StringComparison.Ordinal) ? "₫" : code;
        return $"{Number(value, 2)} {suffix}";
    }

    public static string Percent(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "Chưa có cơ sở so sánh"
            : $"{Number(value, 2)}%";

    public static string Date(string? value) =>
        DateTime.TryParseExact(value?.Trim(), "yyyy-MM-dd", Invariant, DateTimeStyles.None, out var date)
            ? date.ToString("dd/MM/yyyy", Vietnamese)
            : value ?? "—";

    public static DateTime? ParseDate(string? value) =>
        DateTime.TryParseExact(value?.Trim(), "yyyy-MM-dd", Invariant, DateTimeStyles.None, out var date)
            ? date
            : null;

    public static string GeneratedAt(string? value)
    {
        return DateTimeOffset.TryParse(
            value,
            Invariant,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var instant)
            ? instant.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", Vietnamese)
            : "—";
    }

    public static string DimensionLabel(string key) =>
        Dimensions.FirstOrDefault(option => string.Equals(option.Key, key, StringComparison.Ordinal))?.Label
        ?? "Khách hàng";

    public static string MetricLabel(string dimension) => dimension switch
    {
        "products" => "Sản lượng",
        "customers" => "Số đơn",
        "customerGroups" => "Khách · đơn",
        "channels" => "Đơn · khách",
        "productGroups" => "Số sản phẩm",
        _ => "Đơn · khách"
    };

    public static string MetricValue(SalesBreakdownData row, string dimension) => dimension switch
    {
        "products" => $"{Number(row.Quantity)} {UnitLabel(row.Unit)}",
        "customers" => $"{Number(row.DocumentCount)} đơn",
        "customerGroups" => $"{Number(row.CustomerCount)} khách · {Number(row.DocumentCount)} đơn",
        "channels" => $"{Number(row.DocumentCount)} đơn · {Number(row.CustomerCount)} khách",
        "productGroups" => $"{Number(row.ProductCount)} sản phẩm",
        _ => $"{Number(row.DocumentCount)} đơn · {Number(row.CustomerCount)} khách"
    };

    public static string PreviousValue(SalesBreakdownData row, string dimension)
    {
        var revenue = Money(row.PreviousRevenue, row.CurrencyCode);
        return string.Equals(dimension, "products", StringComparison.Ordinal)
            ? $"{revenue} · {Number(row.PreviousQuantity)} {UnitLabel(row.Unit)}"
            : revenue;
    }

    public static string ChangeText(SalesBreakdownData row) => row.ComparisonState switch
    {
        "new" => "Mới trong kỳ",
        "inactive" => "Không phát sinh kỳ này",
        _ => Percent(row.ChangePercent)
    };

    public static SalesAnalysisRow AnalysisRow(SalesBreakdownData row, string dimension, int index) => new(
        index + 1,
        row,
        string.IsNullOrWhiteSpace(row.Name) ? "Chưa xác định" : row.Name.Trim(),
        string.Join(" · ", new[] { row.Code, row.CurrencyCode }.Where(value => !string.IsNullOrWhiteSpace(value))),
        Money(row.Revenue, row.CurrencyCode),
        MetricValue(row, dimension),
        Percent(row.SharePercent),
        PreviousValue(row, dimension),
        ChangeText(row));

    public static IReadOnlyList<SalesTrendSeriesRow> TrendSeries(IEnumerable<SalesReportingTrendData> rows)
    {
        return rows
            .GroupBy(row => row.CurrencyCode, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var values = group.OrderBy(row => row.BusinessDate, StringComparer.Ordinal).ToArray();
                var current = ParseTrendValues(values.Select(row => row.Revenue));
                var previous = ParseTrendValues(values.Select(row => row.PreviousRevenue));
                var scale = current.Concat(previous).ToArray();
                var minimum = scale.Length == 0 ? 0m : scale.Min();
                var maximum = scale.Length == 0 ? 0m : scale.Max();
                return new SalesTrendSeriesRow(
                    group.Key,
                    $"{values.Length} ngày có dữ liệu",
                    values.Length == 0 ? "0" : Money(values[^1].Revenue, group.Key),
                    BuildPoints(current, minimum, maximum),
                    BuildPoints(previous, minimum, maximum));
            })
            .ToArray();
    }

    public static IReadOnlyList<SalesExportColumnDefinition> ExportColumns(string dimension)
    {
        static SalesExportColumnDefinition C(string key, string label, bool selected) => new(key, label, selected);
        return dimension switch
        {
            "customers" =>
            [
                C("code", "Mã", true), C("name", "Tên", true), C("currencyCode", "Tiền tệ", true),
                C("revenue", "Doanh thu", true), C("documentCount", "Số đơn", true),
                C("sharePercent", "Tỷ trọng (%)", true), C("previousRevenue", "Doanh thu kỳ trước", true),
                C("changePercent", "Thay đổi doanh thu (%)", true), C("source", "Nguồn dữ liệu", false)
            ],
            "customerGroups" =>
            [
                C("code", "Mã", true), C("name", "Tên", true), C("currencyCode", "Tiền tệ", true),
                C("revenue", "Doanh thu", true), C("customerCount", "Số khách", true),
                C("documentCount", "Số đơn", true), C("sharePercent", "Tỷ trọng (%)", true),
                C("previousRevenue", "Doanh thu kỳ trước", true), C("changePercent", "Thay đổi doanh thu (%)", true),
                C("source", "Nguồn dữ liệu", false)
            ],
            "channels" or "employees" =>
            [
                C("code", "Mã", true), C("name", "Tên", true), C("currencyCode", "Tiền tệ", true),
                C("revenue", "Doanh thu", true), C("documentCount", "Số đơn", true),
                C("customerCount", "Số khách", true), C("sharePercent", "Tỷ trọng (%)", true),
                C("previousRevenue", "Doanh thu kỳ trước", true), C("changePercent", "Thay đổi doanh thu (%)", true),
                C("source", "Nguồn dữ liệu", false)
            ],
            "products" =>
            [
                C("code", "Mã", true), C("name", "Tên", true), C("currencyCode", "Tiền tệ", true),
                C("unitCode", "Mã ĐVT", false), C("unitName", "ĐVT", true), C("quantity", "Sản lượng", true),
                C("revenue", "Doanh thu", true), C("sharePercent", "Tỷ trọng (%)", true),
                C("previousRevenue", "Doanh thu kỳ trước", true), C("previousQuantity", "Sản lượng kỳ trước", true),
                C("changePercent", "Thay đổi doanh thu (%)", true), C("source", "Nguồn dữ liệu", false)
            ],
            "productGroups" =>
            [
                C("code", "Mã", true), C("name", "Tên", true), C("currencyCode", "Tiền tệ", true),
                C("revenue", "Doanh thu", true), C("productCount", "Số sản phẩm", true),
                C("sharePercent", "Tỷ trọng (%)", true), C("previousRevenue", "Doanh thu kỳ trước", true),
                C("changePercent", "Thay đổi doanh thu (%)", true), C("source", "Nguồn dữ liệu", false)
            ],
            _ => []
        };
    }

    private static string UnitLabel(SalesReportingUnitData unit) =>
        !string.IsNullOrWhiteSpace(unit.Name) ? unit.Name.Trim()
        : !string.IsNullOrWhiteSpace(unit.Code) ? unit.Code.Trim()
        : "ĐVT chưa xác định";

    private static decimal[] ParseTrendValues(IEnumerable<string> source) =>
        source.Select(value => decimal.TryParse(value, NumberStyles.Float, Invariant, out var number) ? number : 0m)
            .ToArray();

    private static PointCollection BuildPoints(IReadOnlyList<decimal> values, decimal minimum, decimal maximum)
    {
        var points = new PointCollection();
        if (values.Count == 0) return points;

        var range = maximum == minimum ? 1m : maximum - minimum;
        for (var index = 0; index < values.Count; index++)
        {
            var x = values.Count == 1 ? 0d : index * 300d / (values.Count - 1);
            var ratio = (double)((maximum - values[index]) / range);
            points.Add(new Point(x, 8d + ratio * 74d));
        }

        return points;
    }
}

public sealed record SalesReportingOption(string Key, string Label);
public sealed record SalesWarehouseOption(string Id, string Label);
public sealed record SalesClassificationOption(string Id, string Label);
public sealed record SalesRevenueRow(string CurrencyCode, string Revenue, string Comparison);
public sealed record SalesTrendRow(
    int Stt,
    string Date,
    string CurrencyCode,
    string Revenue,
    string PreviousRevenue,
    string Change);
public sealed record SalesTrendSeriesRow(
    string CurrencyCode,
    string DayCountText,
    string LatestRevenue,
    PointCollection CurrentPoints,
    PointCollection PreviousPoints);
public sealed record SalesAnalysisRow(
    int Stt,
    SalesBreakdownData Data,
    string Name,
    string CodeCurrency,
    string Revenue,
    string Metric,
    string Share,
    string Previous,
    string Change);
public sealed record SalesExportColumnDefinition(string Key, string Label, bool DefaultSelected);

public sealed class SalesExportColumnOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public SalesExportColumnOption(string key, string label, bool isSelected)
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
