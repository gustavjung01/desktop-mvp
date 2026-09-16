using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed record AgingWarehouseOption(string Id, string Label);

public sealed record AgingSummaryRow(
    string CurrencyCode,
    string Bucket,
    string DocumentCount,
    string RemainingAmount);

public sealed record AgingPartyRow(
    string Party,
    string CurrencyCode,
    string DocumentCount,
    string RemainingAmount,
    string ReferenceDate,
    string MaximumDays);

public static class AgingReportingPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string ReceivableBucket(string? value) => value switch
    {
        "AGE_0_30" => "0–30 ngày",
        "AGE_31_60" => "31–60 ngày",
        "AGE_61_90" => "61–90 ngày",
        "AGE_91_PLUS" => "Trên 90 ngày",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string PayableBucket(string? value) => value switch
    {
        "NOT_DUE" => "Chưa đến hạn",
        "OVERDUE_1_30" => "Quá hạn 1–30 ngày",
        "OVERDUE_31_60" => "Quá hạn 31–60 ngày",
        "OVERDUE_61_90" => "Quá hạn 61–90 ngày",
        "OVERDUE_91_PLUS" => "Quá hạn trên 90 ngày",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string Number(string? value, int maxFraction = 0)
    {
        var normalized = (value ?? "0").Trim();
        if (normalized.Length == 0) normalized = "0";

        var sign = string.Empty;
        if (normalized[0] == '-')
        {
            sign = "-";
            normalized = normalized[1..];
        }

        var parts = normalized.Split('.', 2);
        if (parts.Length > 2 || parts[0].Length == 0 || parts[0].Any(ch => ch is < '0' or > '9'))
            return value?.Trim() ?? "0";

        var integer = parts[0].TrimStart('0');
        if (integer.Length == 0) integer = "0";
        var fraction = parts.Length == 2 ? parts[1] : string.Empty;
        if (fraction.Any(ch => ch is < '0' or > '9')) return value?.Trim() ?? "0";

        var precision = Math.Max(0, maxFraction);
        var kept = fraction.Length <= precision ? fraction : fraction[..precision];
        var shouldRound = fraction.Length > precision && fraction[precision] >= '5';

        if (shouldRound)
        {
            if (precision == 0)
            {
                integer = IncrementDigits(integer);
            }
            else
            {
                var combined = IncrementDigits(integer + kept.PadRight(precision, '0'));
                var splitAt = combined.Length - precision;
                integer = splitAt <= 0 ? "0" : combined[..splitAt];
                kept = combined[Math.Max(0, splitAt)..].PadLeft(precision, '0');
            }
        }

        kept = kept.TrimEnd('0');
        var isZero = integer.All(ch => ch == '0') && kept.Length == 0;
        var grouped = GroupThousands(integer);
        return $"{(isZero ? string.Empty : sign)}{grouped}{(kept.Length == 0 ? string.Empty : $",{kept}")}";
    }

    public static string Money(string? value, string? currencyCode)
    {
        var currency = string.IsNullOrWhiteSpace(currencyCode) ? "—" : currencyCode.Trim().ToUpperInvariant();
        return $"{Number(value, currency == "VND" ? 0 : 6)} {currency}";
    }

    public static string Date(string? value) =>
        DateTime.TryParseExact(
            value?.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed.ToString("dd/MM/yyyy", Vietnamese)
            : string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    public static string GeneratedAt(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", Vietnamese)
            : "—";

    public static AgingWarehouseOption Warehouse(AgingWarehouseOptionData row) =>
        new(row.WarehouseId, $"{row.WarehouseCode} — {row.WarehouseName}");

    public static AgingSummaryRow ReceivableSummary(AgingBucketData row) =>
        new(row.CurrencyCode, ReceivableBucket(row.AgeBucket), Number(row.DocumentCount), Money(row.RemainingAmount, row.CurrencyCode));

    public static AgingSummaryRow PayableSummary(AgingBucketData row) =>
        new(row.CurrencyCode, PayableBucket(row.AgeBucket), Number(row.DocumentCount), Money(row.RemainingAmount, row.CurrencyCode));

    public static AgingPartyRow ReceivableParty(AgingPartyData row) =>
        new(
            Party(row.CustomerCode, row.CustomerName),
            row.CurrencyCode,
            Number(row.DocumentCount),
            Money(row.RemainingAmount, row.CurrencyCode),
            Date(row.OldestDocumentDate),
            $"{Number(row.OldestAgeDays)} ngày");

    public static AgingPartyRow PayableParty(AgingPartyData row) =>
        new(
            Party(row.SupplierCode, row.SupplierName),
            row.CurrencyCode,
            Number(row.DocumentCount),
            Money(row.RemainingAmount, row.CurrencyCode),
            Date(row.EarliestDueDate),
            $"{Number(row.MaxOverdueDays)} ngày");

    private static string Party(string? code, string? name)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(code) ? "—" : code.Trim();
        var normalizedName = string.IsNullOrWhiteSpace(name) ? "Chưa có tên" : name.Trim();
        return $"{normalizedCode} — {normalizedName}";
    }

    private static string IncrementDigits(string value)
    {
        var digits = value.ToCharArray();
        for (var index = digits.Length - 1; index >= 0; index--)
        {
            if (digits[index] != '9')
            {
                digits[index]++;
                return new string(digits);
            }
            digits[index] = '0';
        }
        return "1" + new string(digits);
    }

    private static string GroupThousands(string value)
    {
        if (value.Length <= 3) return value;
        var first = value.Length % 3;
        if (first == 0) first = 3;
        var groups = new List<string> { value[..first] };
        for (var index = first; index < value.Length; index += 3)
            groups.Add(value.Substring(index, Math.Min(3, value.Length - index)));
        return string.Join(".", groups);
    }
}
