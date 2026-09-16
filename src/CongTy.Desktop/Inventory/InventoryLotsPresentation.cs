using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record InventoryLotsRow(int Sequence, InventoryLotData Data)
{
    public string Sku => Data.BaseSku;
    public string Product => string.IsNullOrWhiteSpace(Data.ProductCode)
        ? Data.ProductName
        : $"{Data.ProductCode} · {Data.ProductName}";
    public string LotCode => Data.LotCode;
    public string NormalizedLotCode =>
        string.Equals(Data.NormalizedLotCode, Data.LotCode, StringComparison.Ordinal)
            ? string.Empty
            : Data.NormalizedLotCode;
    public string ExpiryDate => InventoryLotsPresentation.Date(Data.ExpiryDate);
    public string ManufacturedDate => InventoryLotsPresentation.Date(Data.ManufacturedDate);
    public string SupplierReference => string.IsNullOrWhiteSpace(Data.SupplierLotReference)
        ? "—"
        : Data.SupplierLotReference;
    public string CreatedAt => InventoryLotsPresentation.DateTimeText(Data.CreatedAt);
}

public static class InventoryLotsPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string SearchText(InventoryLotData row) =>
        string.Join(' ', new[]
        {
            row.LotCode,
            row.NormalizedLotCode,
            row.BaseSku,
            row.BaseVariantName,
            row.ProductCode,
            row.ProductName,
            row.ExpiryDate,
            row.SupplierLotReference
        }.Where(value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Không có";
        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var date)
            ? date.ToString("dd/MM/yyyy", Vi)
            : value;
    }

    public static string DateTimeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Không có";
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var date))
        {
            return value;
        }

        var local = TimeZoneInfo.ConvertTime(
            date,
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        return local.ToString("dd/MM/yyyy HH:mm", Vi);
    }
}
