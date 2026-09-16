using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public static class PurchaseOrderPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static readonly IReadOnlyDictionary<string, string> StatusLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["draft"] = "Nháp",
            ["pending_approval"] = "Chờ duyệt",
            ["approved"] = "Đã duyệt",
            ["partially_received"] = "Đã nhận một phần",
            ["fully_received"] = "Đã nhận đủ",
            ["closed"] = "Đã đóng",
            ["cancelled"] = "Đã hủy",
            ["posted"] = "Đã ghi sổ",
            ["reversed"] = "Đã đảo"
        };

    public static string Status(string? value) =>
        value is not null && StatusLabels.TryGetValue(value, out var label) ? label : value ?? "—";

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        if (DateTime.TryParse(value, Inv, DateTimeStyles.AssumeLocal, out var date))
        {
            return date.ToString("dd/MM/yyyy", Vi);
        }

        return value;
    }

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

    public static bool TryPositiveDecimal(string? value, out decimal number) =>
        decimal.TryParse(value?.Trim(), NumberStyles.Number, Inv, out number) && number > 0;

    public static string ApiDecimal(decimal value) => value.ToString("0.######", Inv);
}

public sealed record PurchaseOrderStatusOption(string Value, string Label);

public sealed record PurchaseOrderLookupOption(string Id, string Display);

public sealed record PurchaseOrderRow(
    PurchaseOrderData Data,
    int Stt,
    string Number,
    string OrderDate,
    string Supplier,
    string Warehouse,
    string LineCount,
    string Total,
    string Status,
    string UpdatedAt,
    bool CanView,
    bool CanPrint,
    bool CanEdit,
    bool CanSubmit,
    bool CanApprove,
    bool CanCancel)
{
    public string SearchText => string.Join(
        " ",
        new[]
        {
            Data.Number,
            Data.SupplierReference,
            Data.SupplierCode,
            Data.SupplierName,
            Data.WarehouseCode,
            Data.WarehouseName
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed record PurchaseOrderSkuResultRow(
    PurchaseOrderSkuSearchOptionData Data,
    string Sku,
    string Name,
    string Unit,
    string Eligibility,
    bool Selectable);

public sealed record PurchaseOrderReceiptRow(
    string Number,
    string Date,
    string Status,
    string SupplierReference,
    string Quantity);

public sealed record PurchaseOrderDetailLineRow(
    string Sku,
    string Name,
    string Quantity,
    string Received,
    string Accepted,
    string Rejected,
    string ShortageClosed,
    string Remaining,
    string Unit,
    string Conversion,
    string UnitPrice,
    string Discount,
    string Tax,
    string Total);

public sealed class PurchaseOrderEditorLine : System.ComponentModel.INotifyPropertyChanged
{
    private string _quantity = "1";
    private string _unitPrice = string.Empty;
    private bool _manualPrice;
    private string _discountMode = "TOTAL_AMOUNT";
    private string _discountValue = "0";
    private string _taxRate = "0";
    private string _overrideReason = string.Empty;
    private string _note = string.Empty;
    private string _priceStatus = "NOT_FOUND";

    public required string VariantId { get; init; }
    public required string Sku { get; init; }
    public required string ItemName { get; init; }
    public required string UnitId { get; init; }
    public required string UnitCode { get; init; }
    public string ConversionToBase { get; init; } = "1";

    public string Quantity { get => _quantity; set { if (Set(ref _quantity, value)) Raise(nameof(LineTotalPreview)); } }
    public string UnitPrice { get => _unitPrice; set { if (Set(ref _unitPrice, value)) Raise(nameof(LineTotalPreview)); } }
    public bool ManualPrice { get => _manualPrice; set => Set(ref _manualPrice, value); }
    public string DiscountMode { get => _discountMode; set { if (Set(ref _discountMode, value)) Raise(nameof(LineTotalPreview)); } }
    public string DiscountValue { get => _discountValue; set { if (Set(ref _discountValue, value)) Raise(nameof(LineTotalPreview)); } }
    public string TaxRate { get => _taxRate; set { if (Set(ref _taxRate, value)) Raise(nameof(LineTotalPreview)); } }
    public string OverrideReason { get => _overrideReason; set => Set(ref _overrideReason, value); }
    public string Note { get => _note; set => Set(ref _note, value); }
    public string PriceStatus { get => _priceStatus; set { if (Set(ref _priceStatus, value)) Raise(nameof(PriceStatusText)); } }

    public string PriceStatusText => PriceStatus == "RESOLVED" ? "Đã có giá mua" : "Chưa có giá mua";

    public string LineTotalPreview
    {
        get
        {
            if (!PurchaseOrderPresentation.TryPositiveDecimal(Quantity, out var quantity)
                || !PurchaseOrderPresentation.TryPositiveDecimal(UnitPrice, out var price))
            {
                return "—";
            }

            var gross = quantity * price;
            decimal discount = 0;
            if (decimal.TryParse(DiscountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var discountValue))
            {
                discount = DiscountMode switch
                {
                    "PERCENT" => gross * discountValue / 100m,
                    "PER_UNIT" => quantity * discountValue,
                    _ => discountValue
                };
            }

            var taxable = Math.Max(0, gross - discount);
            decimal tax = 0;
            if (decimal.TryParse(TaxRate, NumberStyles.Number, CultureInfo.InvariantCulture, out var taxRate))
            {
                tax = taxable * taxRate / 100m;
            }

            return PurchaseOrderPresentation.Money(
                PurchaseOrderPresentation.ApiDecimal(taxable + tax),
                "VND");
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? Changed;

    private bool Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(name);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void Raise(string? name) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
