using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public static class GrossMarginReportingPresentation
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static readonly IReadOnlyList<GrossMarginOption> ExportDimensions =
    [
        new("customers", "Theo khách hàng"),
        new("skus", "Theo SKU"),
        new("lines", "Chi tiết dòng"),
        new("exceptions", "Ngoại lệ")
    ];

    private static readonly IReadOnlyDictionary<string, GrossMarginExportColumnDefinition[]> ExportDefinitions =
        new Dictionary<string, GrossMarginExportColumnDefinition[]>(StringComparer.Ordinal)
        {
            ["customers"] =
            [
                new("customerCode", "Mã khách hàng", true),
                new("customerName", "Khách hàng", true),
                new("lineCount", "Số dòng", false),
                new("netRevenue", "Doanh thu", true),
                new("cogs", "Giá vốn", true),
                new("grossMargin", "Lãi gộp", true),
                new("grossMarginPercent", "Tỷ lệ lãi gộp (%)", true)
            ],
            ["skus"] =
            [
                new("sku", "SKU", true),
                new("productName", "Tên sản phẩm", true),
                new("lineCount", "Số dòng", false),
                new("netRevenue", "Doanh thu", true),
                new("cogs", "Giá vốn", true),
                new("grossMargin", "Lãi gộp", true),
                new("grossMarginPercent", "Tỷ lệ lãi gộp (%)", true)
            ],
            ["lines"] =
            [
                new("documentDate", "Ngày", true),
                new("documentNumber", "Chứng từ", true),
                new("eventKind", "Loại phát sinh", true),
                new("customerCode", "Mã khách hàng", true),
                new("customerName", "Khách hàng", true),
                new("warehouseCode", "Kho", true),
                new("sku", "SKU", true),
                new("productName", "Tên sản phẩm", true),
                new("currencyCode", "Tiền tệ", false),
                new("netRevenue", "Doanh thu", true),
                new("cogs", "Giá vốn", true),
                new("grossMargin", "Lãi gộp", true),
                new("grossMarginPercent", "Tỷ lệ lãi gộp (%)", false)
            ],
            ["exceptions"] =
            [
                new("documentDate", "Ngày", true),
                new("documentNumber", "Chứng từ", true),
                new("eventKind", "Loại phát sinh", true),
                new("customerCode", "Mã khách hàng", true),
                new("customerName", "Khách hàng", true),
                new("warehouseCode", "Kho", true),
                new("sku", "SKU", true),
                new("productName", "Tên sản phẩm", true),
                new("currencyCode", "Tiền tệ", false),
                new("netRevenue", "Doanh thu", true),
                new("exceptionReason", "Nguyên nhân", true)
            ]
        };

    public static IReadOnlyList<GrossMarginExportColumnDefinition> ExportColumns(string dimension) =>
        ExportDefinitions.TryGetValue(dimension, out var columns)
            ? columns
            : ExportDefinitions["customers"];

    public static string Number(string? value, int maxFractionDigits = 2)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Float, Invariant, out var number)) return normalized;

        var decimals = Math.Clamp(maxFractionDigits, 0, 12);
        var factor = 1m;
        for (var index = 0; index < decimals; index++) factor *= 10m;
        number = decimals == 0
            ? decimal.Truncate(number)
            : decimal.Truncate(number * factor) / factor;

        var format = decimals == 0 ? "#,##0" : $"#,##0.{new string('#', decimals)}";
        return number.ToString(format, Vietnamese);
    }

    public static string Money(string? value) => $"{Number(value, 0)} ₫";

    public static string Percent(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : $"{Number(value, 2)}%";

    public static string Date(string? value) =>
        DateTime.TryParseExact(value?.Trim(), "yyyy-MM-dd", Invariant, DateTimeStyles.None, out var date)
            ? date.ToString("dd/MM/yyyy", Vietnamese)
            : value ?? "—";

    public static DateTime? ParseDate(string? value) =>
        DateTime.TryParseExact(value?.Trim(), "yyyy-MM-dd", Invariant, DateTimeStyles.None, out var date)
            ? date
            : null;

    public static string ExceptionLabel(string? code) => code switch
    {
        "NON_VND_REVENUE" => "Doanh thu không phải VND",
        "MISSING_INVENTORY_LINEAGE" => "Thiếu liên kết xuất/nhập kho",
        "MISSING_COST_FACT" => "Chưa có dữ liệu giá vốn",
        "COST_ANOMALY" => "Dữ liệu giá vốn có bất thường",
        _ => string.IsNullOrWhiteSpace(code) ? "—" : code
    };

    public static GrossMarginGroupRow CustomerRow(GrossMarginReportingGroupData row, int index) =>
        new(
            index + 1,
            row.CustomerCode ?? "—",
            row.CustomerName ?? string.Empty,
            Money(row.NetRevenueVnd),
            Money(row.CogsVnd),
            Money(row.GrossMarginVnd),
            Percent(row.GrossMarginPercent));

    public static GrossMarginGroupRow SkuRow(GrossMarginReportingGroupData row, int index) =>
        new(
            index + 1,
            row.Sku ?? "—",
            string.Empty,
            Money(row.NetRevenueVnd),
            Money(row.CogsVnd),
            Money(row.GrossMarginVnd),
            Percent(row.GrossMarginPercent));

    public static GrossMarginExceptionRow ExceptionRow(GrossMarginReportingLineData row, int index) =>
        new(
            index + 1,
            Date(row.DocumentDate),
            string.Equals(row.EventKind, "RETURN", StringComparison.Ordinal) ? $"Trả hàng · {row.DocumentNumber}" : row.DocumentNumber,
            row.WarehouseCode,
            row.Sku,
            row.CustomerCode,
            ExceptionLabel(row.ExceptionCode));
}

public sealed record GrossMarginOption(string Key, string Label);
public sealed record GrossMarginWarehouseOption(string Id, string Label);
public sealed record GrossMarginGroupRow(
    int Number,
    string Primary,
    string Secondary,
    string Revenue,
    string Cogs,
    string GrossMargin,
    string MarginPercent);
public sealed record GrossMarginExceptionRow(
    int Number,
    string Date,
    string Document,
    string Warehouse,
    string Sku,
    string Customer,
    string Reason);
public sealed record GrossMarginExportColumnDefinition(string Key, string Label, bool DefaultSelected);

public sealed class GrossMarginExportColumnOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public GrossMarginExportColumnOption(string key, string label, bool isSelected)
    {
        Key = key;
        Label = label;
        _isSelected = isSelected;
    }

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

    public event PropertyChangedEventHandler? PropertyChanged;
}
