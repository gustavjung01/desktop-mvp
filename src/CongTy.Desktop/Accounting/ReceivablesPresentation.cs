using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public static class ReceivablesPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public static string Money(string? value, string? currencyCode = "VND")
    {
        var currency = string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode.Trim().ToUpperInvariant();
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return $"{value ?? "0"} {currency}";

        if (currency == "VND")
            return amount.ToString("C0", Vietnamese);

        var formatted = amount.ToString("0.######", Vietnamese);
        return $"{formatted} {currency}";
    }

    public static string Status(string? status) => status switch
    {
        "open" => "Còn phải thu",
        "partially_allocated" => "Đã thu một phần",
        "settled" => "Đã thu đủ",
        "reversed" => "Đã hủy",
        _ => string.IsNullOrWhiteSpace(status) ? "Chưa xác định" : status.Trim()
    };

    public static string Source(string? sourceDocumentType) => sourceDocumentType switch
    {
        "PICKUP_HANDOVER" => "Nhận tại quầy",
        "DELIVERY_ATTEMPT" => "Giao hàng",
        "MANUAL_SALES_ORDER" => "Giao thủ công",
        _ => "Giao hàng"
    };

    public static string CollectionPolicy(string? policy) => policy switch
    {
        "PREPAID" => "Thanh toán trước",
        "COLLECT_ON_DELIVERY" => "Thu tiền khi giao",
        "COLLECT_AFTER_DELIVERY" => "Thu sau khi giao",
        "CREDIT_TERMS" => "Bán chịu theo điều khoản",
        _ => "Theo thỏa thuận thanh toán"
    };

    public static string LedgerEntry(string? entryType)
    {
        var value = entryType ?? string.Empty;
        if (value == "SALE_POST") return "Phát sinh công nợ";
        if (value == "SALE_REVERSE") return "Điều chỉnh giảm công nợ";
        if (value == "CUSTOMER_PAYMENT_POST") return "Ghi nhận thu tiền";
        if (value == "CUSTOMER_PAYMENT_REVERSE") return "Hủy phiếu thu";
        if (value.Contains("REFUND", StringComparison.Ordinal)) return "Hoàn tiền khách hàng";
        if (value.Contains("RETURN", StringComparison.Ordinal) || value.Contains("CREDIT", StringComparison.Ordinal))
            return "Điều chỉnh giảm công nợ";
        return "Bút toán công nợ";
    }

    public static string Date(string? value)
    {
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToString("dd/MM/yyyy", Vietnamese);
        return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
    }

    public static string DateTimeText(string? value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
            return TimeZoneInfo.ConvertTime(timestamp, VietnamTimeZone).ToString("dd/MM/yyyy HH:mm", Vietnamese);
        return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
    }

    public static decimal Amount(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : 0m;

    public static string Party(string? code, string? name)
    {
        var left = string.IsNullOrWhiteSpace(code) ? "—" : code.Trim();
        var right = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        return string.IsNullOrWhiteSpace(right) ? left : $"{left} · {right}";
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }

    public static string DocumentReference(ReceivableDocumentData document)
    {
        var sales = string.IsNullOrWhiteSpace(document.SalesOrderNumber) ? "—" : document.SalesOrderNumber.Trim();
        var delivery = !string.IsNullOrWhiteSpace(document.DeliveryOrderNumber)
            ? document.DeliveryOrderNumber.Trim()
            : string.IsNullOrWhiteSpace(document.SourceDocumentNumber) ? "—" : document.SourceDocumentNumber.Trim();
        return $"{sales} / {delivery}";
    }
}

public sealed record ReceivableBalanceRow(
    int Sequence,
    string Customer,
    string CurrencyCode,
    string Balance,
    string OpenAmount,
    int OpenDocumentCount);

public sealed record ReceivableDocumentRow(
    int Sequence,
    string Id,
    string Operation,
    string SourceDate,
    string Customer,
    string Reference,
    string Warehouse,
    string Status,
    string OriginalAmount,
    string RemainingAmount);

public sealed record ReceivableLineRow(
    int Sequence,
    string Sku,
    string ItemName,
    string AcceptedQuantity,
    string GrossAmount,
    string DiscountAmount,
    string TaxAmount,
    string LineAmount);

public sealed record ReceivableLedgerRow(
    int Sequence,
    string OccurredAt,
    string EntryType,
    string Source,
    string Amount);
