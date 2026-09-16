using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Dashboard;

public sealed record DashboardKpiRow(string Id, string Label, string Value, string Hint);
public sealed record DashboardActivityRow(string Label, string Value, string Hint);
public sealed record DashboardMeasurementRow(string Id, string Eyebrow, string Title, string PrimaryValue, string SecondaryValue, string Detail);

public static class DashboardPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Count(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "0";
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount.ToString("0", Vi)
            : value.Trim();
    }

    public static string MoneyCompact(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)) return "—";
        var absolute = Math.Abs(amount);
        if (absolute >= 1_000_000_000m) return $"{(amount / 1_000_000_000m).ToString("0.#", Vi)} tỷ ₫";
        if (absolute >= 1_000_000m) return $"{(amount / 1_000_000m).ToString("0.#", Vi)} tr ₫";
        if (absolute >= 1_000m) return $"{(amount / 1_000m).ToString("0.#", Vi)} nghìn ₫";
        return $"{amount.ToString("0", Vi)} ₫";
    }

    public static string Percent(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? $"{amount.ToString("0.#", Vi)}%"
            : "—";

    public static string DateRange(string? from, string? to) =>
        $"{Date(from)} → {Date(to)}";

    public static string Date(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date)
            ? date.ToString("dd/MM/yyyy", Vi)
            : "—";

    public static string Generated(IEnumerable<string?> values)
    {
        var latest = values
            .Select(value => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : (DateTimeOffset?)null)
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .OrderBy(value => value)
            .LastOrDefault();

        return latest == default
            ? "Chưa có thời điểm cập nhật"
            : $"Cập nhật lúc {latest.ToLocalTime():dd/MM/yyyy HH:mm}";
    }

    public static string SumReceivableVnd(DashboardAgingData? aging)
    {
        if (aging is null) return "—";
        var total = aging.Receivable.Summary
            .Where(row => string.Equals(row.CurrencyCode, "VND", StringComparison.OrdinalIgnoreCase))
            .Select(row => decimal.TryParse(row.RemainingAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : 0m)
            .Sum();
        return MoneyCompact(total.ToString(CultureInfo.InvariantCulture));
    }

    public static string SumReceivableDocuments(DashboardAgingData? aging)
    {
        if (aging is null) return "0";
        var total = aging.Receivable.Summary
            .Where(row => string.Equals(row.CurrencyCode, "VND", StringComparison.OrdinalIgnoreCase))
            .Select(row => decimal.TryParse(row.DocumentCount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : 0m)
            .Sum();
        return total.ToString("0", Vi);
    }
}
