using System.Globalization;

namespace CongTy.Desktop.Accounting;

public static class PayablesPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static decimal Amount(string? value) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) ? amount : 0m;

    public static string Money(string? value, string? currencyCode)
    {
        var code = string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode.Trim().ToUpperInvariant();
        var amount = Amount(value);
        return code == "VND"
            ? $"{amount.ToString("N0", Vi)} ₫"
            : $"{amount.ToString("N2", Vi)} {code}";
    }

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

    public static string Status(string? status) => status switch
    {
        "open" => "Còn mở",
        "partially_allocated" => "Đã phân bổ một phần",
        "settled" => "Đã tất toán",
        "reversed" => "Đã đảo",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string Source(string? sourceType) => sourceType switch
    {
        "GOODS_RECEIPT" => "Phiếu nhận hàng",
        "SUPPLIER_RETURN" => "Phiếu trả nhà cung cấp",
        "SUPPLIER_PAYMENT" => "Thanh toán nhà cung cấp",
        _ => string.IsNullOrWhiteSpace(sourceType) ? "Chứng từ" : sourceType
    };

    public static string PaymentMethod(string? method) => method switch
    {
        "CASH" => "Tiền mặt",
        "BANK_TRANSFER" => "Chuyển khoản",
        "CREDIT" => "Công nợ",
        _ => string.IsNullOrWhiteSpace(method) ? "—" : method
    };

    public static string Date(string? value)
    {
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToString("dd/MM/yyyy", Vi);
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
            return timestamp.ToLocalTime().ToString("dd/MM/yyyy", Vi);
        return string.IsNullOrWhiteSpace(value) ? "—" : value;
    }

    public static string DateTimeText(string? value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
            return timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Vi);
        return string.IsNullOrWhiteSpace(value) ? "—" : value;
    }

    public static string LedgerEntry(string? entryType) => entryType switch
    {
        "POST" => "Ghi nhận",
        "ALLOCATE" => "Phân bổ",
        "REVERSE" => "Đảo bút toán",
        "PAYMENT" => "Thanh toán",
        _ => string.IsNullOrWhiteSpace(entryType) ? "—" : entryType
    };
}
