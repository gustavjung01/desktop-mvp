using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public static class PurchasingReportingPresentation
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    private static readonly IReadOnlyDictionary<string, string> StatusLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["draft"] = "Nháp",
            ["pending_approval"] = "Chờ duyệt",
            ["approved"] = "Đã duyệt",
            ["partially_received"] = "Nhận một phần",
            ["fully_received"] = "Đã nhận đủ",
            ["cancelled"] = "Đã hủy",
            ["closed"] = "Đã đóng",
            ["posted"] = "Đã ghi sổ",
            ["reversed"] = "Đã đảo"
        };

    public static string Number(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        return decimal.TryParse(normalized, NumberStyles.Number, Invariant, out var number)
            ? number.ToString("#,##0.######", Vietnamese)
            : normalized;
    }

    public static string Money(string? value, string? currencyCode)
    {
        var code = string.IsNullOrWhiteSpace(currencyCode) ? "—" : currencyCode.Trim().ToUpperInvariant();
        var suffix = string.Equals(code, "VND", StringComparison.Ordinal) ? "₫" : code;
        return $"{Number(value)} {suffix}";
    }

    public static string Date(string? value)
    {
        return DateTime.TryParseExact(
            value?.Trim(),
            "yyyy-MM-dd",
            Invariant,
            DateTimeStyles.None,
            out var date)
            ? date.ToString("dd/MM/yyyy", Vietnamese)
            : value ?? "—";
    }

    public static string Status(string? value)
    {
        var state = value?.Trim() ?? string.Empty;
        if (StatusLabels.TryGetValue(state, out var label)) return label;
        return string.IsNullOrWhiteSpace(state) ? "—" : state.Replace('_', ' ');
    }

    public static DateTime? ParseDate(string? value) =>
        DateTime.TryParseExact(
            value?.Trim(),
            "yyyy-MM-dd",
            Invariant,
            DateTimeStyles.None,
            out var date)
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
}

public sealed record PurchasingCurrencyRow(
    int Stt,
    string CurrencyCode,
    string DocumentCount,
    string Value);

public sealed record PurchasingStatusRow(
    string Label,
    string DocumentCount);

public sealed record PurchasingTrendRow(
    int Stt,
    string Date,
    string CurrencyCode,
    string DocumentCount,
    string Value);

public sealed record PurchasingSupplierRow(
    int Stt,
    string Code,
    string Name,
    string CurrencyCode,
    string DocumentCount,
    string Value);

public sealed record PurchasingSkuRow(
    int Stt,
    string Sku,
    string NameAndCurrency,
    string BaseQuantity,
    string Value,
    string SourceDocument);
