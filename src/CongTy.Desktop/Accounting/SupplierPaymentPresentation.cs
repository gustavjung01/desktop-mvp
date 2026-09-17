using System.Globalization;

namespace CongTy.Desktop.Accounting;

public static class SupplierPaymentPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static decimal Amount(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : 0m;

    public static string Money(string? value, string? currencyCode)
    {
        var currency = string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode.Trim().ToUpperInvariant();
        var amount = Amount(value);
        if (currency == "VND") return $"{amount.ToString("N0", Vi)} ₫";
        var formatted = amount.ToString("N6", Vi).TrimEnd('0').TrimEnd(Vi.NumberFormat.NumberDecimalSeparator.ToCharArray());
        return $"{formatted} {currency}";
    }

    public static string Status(string? status) => status switch
    {
        "open" => "Chưa phân bổ",
        "partially_allocated" => "Đã phân bổ một phần",
        "settled" => "Đã phân bổ hết",
        "reversed" => "Đã đảo",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string PaymentMethod(string? method) => method switch
    {
        "BANK_TRANSFER" => "Chuyển khoản",
        "CASH" => "Tiền mặt",
        "OTHER" => "Khác",
        _ => string.IsNullOrWhiteSpace(method) ? "—" : method
    };

    public static string Party(string? code, string? name)
    {
        var c = string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim();
        var n = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        return (c, n) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => $"{c} · {n}",
            ({ Length: > 0 }, _) => c,
            (_, { Length: > 0 }) => n,
            _ => "—"
        };
    }

    public static string Date(string? value)
    {
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToString("dd/MM/yyyy", Vi);
        return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
    }
}

public sealed record SupplierPaymentChoice(string Id, string Display);

public sealed record SupplierPaymentRow(
    int Sequence,
    string Id,
    string DocumentNumber,
    string PaymentDate,
    string Supplier,
    string OriginalAmount,
    string RemainingAmount,
    string Status);

public sealed record SupplierPaymentTargetChoice(
    string Id,
    string Display,
    string RemainingAmount,
    string CurrencyCode);

public sealed record SupplierPaymentAllocationRow(
    string Id,
    string TargetDocumentNumber,
    string AllocationDate,
    string Amount,
    string Status,
    bool CanReverse);
