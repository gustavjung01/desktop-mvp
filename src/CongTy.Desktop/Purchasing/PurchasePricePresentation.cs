using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public static class PurchasePricePresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Number(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        return decimal.TryParse(text, NumberStyles.Number, Inv, out var number)
            ? number.ToString("#,##0.######", Vi)
            : text;
    }

    public static string Money(string? value, string? currency)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        var code = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant();
        return $"{Number(value)} {(code == "VND" ? "₫" : code)}";
    }

    public static string DateRange(string from, string? to) =>
        $"{Date(from)} → {(string.IsNullOrWhiteSpace(to) ? "không giới hạn" : Date(to))}";

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date)
            ? date.ToString("dd/MM/yyyy", Vi)
            : value;
    }

    public static bool TryPositiveDecimal(string? value, out decimal number) =>
        decimal.TryParse(value?.Trim(), NumberStyles.Number, Inv, out number) && number > 0;

    public static bool TryNonNegativeDecimal(string? value, out decimal number) =>
        decimal.TryParse(value?.Trim(), NumberStyles.Number, Inv, out number) && number >= 0;

    public static string ApiDecimal(decimal value) => value.ToString("0.######", Inv);
}

public sealed record PurchasePriceSupplierOption(string Id, string Display);

public sealed record PurchasePriceRow(
    SupplierPurchasePriceData Data,
    int Stt,
    string Supplier,
    string Sku,
    string Unit,
    string Price,
    string MinQuantity,
    string EffectiveRange,
    string SupplierSku,
    string Status,
    bool CanEdit);

public sealed record PurchasePriceSkuRow(
    PurchaseOrderSkuSearchOptionData Data,
    string Sku,
    string Name,
    string Unit,
    string Eligibility,
    bool Selectable);
