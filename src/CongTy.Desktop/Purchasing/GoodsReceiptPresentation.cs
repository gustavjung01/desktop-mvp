using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public static class GoodsReceiptPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static readonly IReadOnlyDictionary<string, string> StatusLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["draft"] = "Nháp",
            ["posted"] = "Đã ghi sổ",
            ["reversed"] = "Đã đảo"
        };

    public static string Status(string? value) =>
        value is not null && StatusLabels.TryGetValue(value, out var label) ? label : value ?? "—";

    public static string Number(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        return decimal.TryParse(text, NumberStyles.Number, Inv, out var number)
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

    public static bool TryDecimal(string? value, bool allowZero, out decimal number)
    {
        var normalized = (value ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, Inv, out number)
            && (allowZero ? number >= 0 : number > 0);
    }

    public static string ApiDecimal(decimal value) => value.ToString("0.######", Inv);
}

public sealed record GoodsReceiptStatusOption(string Value, string Label);
public sealed record GoodsReceiptPurchaseOrderOption(string Id, string Display);
public sealed record GoodsReceiptLocationOption(string Id, string Display);

public sealed record GoodsReceiptRow(
    GoodsReceiptData Data,
    int Stt,
    string Number,
    string PurchaseOrder,
    string Supplier,
    string Warehouse,
    string ReceiptDate,
    string Status,
    string LineCount,
    string Quantity,
    bool CanView,
    bool CanPrint,
    bool CanEdit,
    bool CanPost,
    bool CanReverse)
{
    public string SearchText => string.Join(" ", new[]
    {
        Data.DocumentNumber,
        Data.PurchaseOrderNumber,
        Data.SupplierCode,
        Data.SupplierName,
        Data.WarehouseCode,
        Data.WarehouseName,
        Data.SupplierDeliveryReference
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed record GoodsReceiptDetailLineRow(
    string Sku,
    string Name,
    string Ordered,
    string Before,
    string Received,
    string Accepted,
    string Rejected,
    string Shortage,
    string Remaining,
    string Location,
    string Lot,
    string Manufactured,
    string Expiry,
    string Variance,
    string Note);

public sealed class GoodsReceiptEditorLine : System.ComponentModel.INotifyPropertyChanged
{
    private string _receivedQuantity = "0";
    private string _acceptedQuantity = "0";
    private string _rejectedQuantity = "0";
    private bool _finalizeLine;
    private string _qualityReasonCode = string.Empty;
    private string _qualityNote = string.Empty;
    private string _locationId = string.Empty;
    private string _lotCode = string.Empty;
    private DateTime? _manufacturedDate;
    private DateTime? _expiryDate;
    private string _supplierLotReference = string.Empty;
    private string _note = string.Empty;

    public required string PurchaseOrderLineId { get; init; }
    public int LineNumber { get; init; }
    public required string Sku { get; init; }
    public required string ItemName { get; init; }
    public required string UnitCode { get; init; }
    public string OrderedQuantity { get; init; } = "0";
    public string ReceivedBefore { get; init; } = "0";
    public string RemainingBefore { get; init; } = "0";
    public GoodsReceiptTrackingPolicyData? TrackingPolicy { get; init; }
    public IReadOnlyList<GoodsReceiptLocationOption> Locations { get; init; } = [];

    public string ReceivedQuantity { get => _receivedQuantity; set => Set(ref _receivedQuantity, value); }
    public string AcceptedQuantity { get => _acceptedQuantity; set { if (Set(ref _acceptedQuantity, value)) Raise(nameof(ReceivedVarianceTotal)); } }
    public string RejectedQuantity { get => _rejectedQuantity; set { if (Set(ref _rejectedQuantity, value)) { Raise(nameof(ReceivedVarianceTotal)); Raise(nameof(VarianceReasonRequired)); } } }
    public bool FinalizeLine { get => _finalizeLine; set { if (Set(ref _finalizeLine, value)) Raise(nameof(VarianceReasonRequired)); } }
    public string QualityReasonCode { get => _qualityReasonCode; set => Set(ref _qualityReasonCode, value); }
    public string QualityNote { get => _qualityNote; set => Set(ref _qualityNote, value); }
    public string LocationId { get => _locationId; set => Set(ref _locationId, value); }
    public string LotCode { get => _lotCode; set => Set(ref _lotCode, value); }
    public DateTime? ManufacturedDate { get => _manufacturedDate; set => Set(ref _manufacturedDate, value); }
    public DateTime? ExpiryDate { get => _expiryDate; set => Set(ref _expiryDate, value); }
    public string SupplierLotReference { get => _supplierLotReference; set => Set(ref _supplierLotReference, value); }
    public string Note { get => _note; set => Set(ref _note, value); }

    public bool LocationRequired => TrackingPolicy?.LocationRequired == true;
    public bool LotRequired => string.Equals(TrackingPolicy?.LotTrackingMode, "REQUIRED", StringComparison.Ordinal);
    public bool LotDisabled => string.Equals(TrackingPolicy?.LotTrackingMode, "NONE", StringComparison.Ordinal);
    public bool ExpiryRequired => string.Equals(TrackingPolicy?.ExpiryTrackingMode, "REQUIRED", StringComparison.Ordinal);
    public bool ExpiryDisabled => string.Equals(TrackingPolicy?.ExpiryTrackingMode, "NONE", StringComparison.Ordinal);

    public bool VarianceReasonRequired =>
        FinalizeLine || (GoodsReceiptPresentation.TryDecimal(RejectedQuantity, true, out var rejected) && rejected > 0);

    public string ReceivedVarianceTotal
    {
        get
        {
            if (!GoodsReceiptPresentation.TryDecimal(AcceptedQuantity, true, out var accepted)
                || !GoodsReceiptPresentation.TryDecimal(RejectedQuantity, true, out var rejected))
            {
                return "—";
            }

            return GoodsReceiptPresentation.Number(GoodsReceiptPresentation.ApiDecimal(accepted + rejected));
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? Changed;

    private bool Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(propertyName);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void Raise(string? propertyName) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
}
