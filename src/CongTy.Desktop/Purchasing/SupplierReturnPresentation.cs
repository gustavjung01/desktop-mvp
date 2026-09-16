using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public static class SupplierReturnPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static readonly IReadOnlyDictionary<string, string> StatusLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["draft"] = "Nháp",
            ["pending_approval"] = "Chờ duyệt",
            ["approved"] = "Đã duyệt",
            ["posted"] = "Đã ghi sổ",
            ["reversed"] = "Đã đảo",
            ["cancelled"] = "Đã hủy"
        };

    public static string Status(string? value) =>
        value is not null && StatusLabels.TryGetValue(value, out var label) ? label : value ?? "—";

    public static string Number(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        return decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, Inv, out var number)
            ? number.ToString("#,##0.######", Vi)
            : text;
    }

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        return DateTime.TryParse(value, Inv, DateTimeStyles.AssumeLocal, out var date)
            ? date.ToString("dd/MM/yyyy", Vi)
            : value;
    }

    public static bool TryPositiveDecimal(string? value, out decimal number) =>
        decimal.TryParse((value ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Number, Inv, out number)
        && number > 0;

    public static bool TryNonNegativeDecimal(string? value, out decimal number) =>
        decimal.TryParse((value ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Number, Inv, out number)
        && number >= 0;

    public static string ApiDecimal(decimal value) => value.ToString("0.######", Inv);
}

public sealed record SupplierReturnStatusOption(string Value, string Label);
public sealed record SupplierReturnReceiptOption(string Id, string Display);

public sealed record SupplierReturnRow(
    SupplierReturnData Data,
    int Stt,
    string Number,
    string Supplier,
    string Warehouse,
    string ReturnDate,
    string Status,
    string LineCount,
    string Quantity,
    bool CanView,
    bool CanPrint,
    bool CanEdit,
    bool CanSubmit,
    bool CanApprove,
    bool CanCancel,
    bool CanPost,
    bool CanReverse);

public sealed record SupplierReturnDetailLineRow(
    string SourceReceipt,
    string SourceLine,
    string PurchaseOrder,
    string Sku,
    string Name,
    string Unit,
    string Accepted,
    string PostedReturn,
    string Returnable,
    string Returned,
    string Reason,
    string LocationLot,
    string Note);

public sealed class SupplierReturnEditorLine : System.ComponentModel.INotifyPropertyChanged
{
    private string _returnQuantity = string.Empty;
    private string _reasonCode = string.Empty;
    private string _reasonNote = string.Empty;
    private string _note = string.Empty;

    public required string SourceGoodsReceiptLineId { get; init; }
    public required string SourceSupplierId { get; init; }
    public required string SourceWarehouseId { get; init; }
    public int SourceGoodsReceiptLineNumber { get; init; }
    public required string SourceGoodsReceiptNumber { get; init; }
    public required string SourcePurchaseOrderNumber { get; init; }
    public required string SourceSku { get; init; }
    public required string SourceItemName { get; init; }
    public required string SourceUnitCode { get; init; }
    public string SourceAcceptedQuantity { get; init; } = "0";
    public string PostedReturnQuantity { get; init; } = "0";
    public string ReturnableQuantity { get; init; } = "0";
    public string LocationLot { get; init; } = "—";

    public string ReturnQuantity { get => _returnQuantity; set => Set(ref _returnQuantity, value); }
    public string ReasonCode { get => _reasonCode; set => Set(ref _reasonCode, (value ?? string.Empty).ToUpperInvariant()); }
    public string ReasonNote { get => _reasonNote; set => Set(ref _reasonNote, value ?? string.Empty); }
    public string Note { get => _note; set => Set(ref _note, value ?? string.Empty); }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? Changed;

    private bool Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
