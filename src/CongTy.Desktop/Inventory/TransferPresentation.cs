using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record TransferStatusOption(string Id, string Label);
public sealed record TransferWarehouseOption(string Id, string Label);
public sealed record TransferBalanceOption(string Key, string Label, string Available, InventoryBalanceData Data);
public sealed record TransferLocationOption(string Id, string Label);

public sealed class TransferDraftLineRow : INotifyPropertyChanged
{
    private string _selectedKey = string.Empty;
    private string _quantity = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<TransferBalanceOption> Options { get; set; } = [];

    public string SelectedKey
    {
        get => _selectedKey;
        set
        {
            if (_selectedKey == (value ?? string.Empty)) return;
            _selectedKey = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedOption));
            OnPropertyChanged(nameof(Available));
        }
    }

    public string Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity == (value ?? string.Empty)) return;
            _quantity = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public TransferBalanceOption? SelectedOption => Options.FirstOrDefault(option => option.Key == SelectedKey);
    public string Available => SelectedOption?.Available ?? "—";

    public void RefreshOptions(IReadOnlyList<TransferBalanceOption> options)
    {
        Options = options;
        if (!options.Any(option => option.Key == SelectedKey)) _selectedKey = string.Empty;
        OnPropertyChanged(nameof(Options));
        OnPropertyChanged(nameof(SelectedKey));
        OnPropertyChanged(nameof(SelectedOption));
        OnPropertyChanged(nameof(Available));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class TransferReceiptDraftRow : INotifyPropertyChanged
{
    private string _destinationLocationId = string.Empty;
    private string _acceptedQuantity = string.Empty;
    private string _damagedQuantity = string.Empty;
    private string _overQuantity = string.Empty;
    private string _note = string.Empty;

    public TransferReceiptDraftRow(InventoryTransferResolutionLineData data, IReadOnlyList<TransferLocationOption> locations)
    {
        Data = data;
        Locations = locations;
        _destinationLocationId = locations.FirstOrDefault()?.Id ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public InventoryTransferResolutionLineData Data { get; }
    public IReadOnlyList<TransferLocationOption> Locations { get; }
    public string Product => $"{Data.LineNumber}. {Data.SourceSku} · {Data.ItemName}";
    public string Remaining => $"{InventoryPresentation.Quantity(Data.RemainingQuantity)} {Data.SourceUnitCode}";

    public string DestinationLocationId { get => _destinationLocationId; set => Set(ref _destinationLocationId, value ?? string.Empty); }
    public string AcceptedQuantity { get => _acceptedQuantity; set => Set(ref _acceptedQuantity, value ?? string.Empty); }
    public string DamagedQuantity { get => _damagedQuantity; set => Set(ref _damagedQuantity, value ?? string.Empty); }
    public string OverQuantity { get => _overQuantity; set => Set(ref _overQuantity, value ?? string.Empty); }
    public string Note { get => _note; set => Set(ref _note, value ?? string.Empty); }

    private void Set(ref string field, string value, [CallerMemberName] string? name = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public sealed record TransferCardRow(
    int Stt,
    string TransferId,
    string Status,
    string Date,
    string Number,
    string Route,
    string LineCount,
    string Quantity,
    string SearchText,
    InventoryTransferData Data);

public sealed record TransferTransitCardRow(
    int Stt,
    string TransferId,
    string Status,
    string DispatchedAt,
    string Product,
    string Route,
    string Number,
    string Remaining,
    string Lot,
    string SearchText,
    InventoryTransferInTransitData Data);

public sealed record TransferDetailLineRow(
    int Stt,
    string Product,
    string Quantity,
    string Lot,
    string Expiry);

public sealed record TransferResolutionRow(
    int Stt,
    string Product,
    string Unit,
    string Dispatched,
    string Accepted,
    string Damaged,
    string Short,
    string Over,
    string Remaining);

public sealed class TransferReceiptHistoryRow : INotifyPropertyChanged
{
    private string _damageNote = string.Empty;
    private string _reverseReason = string.Empty;

    public TransferReceiptHistoryRow(InventoryTransferReceiptData data, bool canDamageApprove, bool canReverse, bool shortClosed)
    {
        Data = data;
        CanApproveDamage = canDamageApprove
            && data.DamageApproval is null
            && data.Reversal is null
            && data.Lines.Any(line => TransferPresentation.IsPositive(line.DamagedQuantity));
        CanReverse = canReverse && data.Reversal is null && !shortClosed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public InventoryTransferReceiptData Data { get; }
    public string Header => $"Lần nhận {Data.ReceiptSequence}";
    public string DateStatus => $"{InventoryPresentation.Date(Data.ReceiptDate)} · {(Data.InventoryMovementId is null ? "Không có hàng đạt" : "Đã ghi tồn hàng đạt")}";
    public string State => Data.Reversal is null ? "Có hiệu lực" : "Đã đảo";
    public string LineSummary => string.Join(Environment.NewLine, Data.Lines.Select(line =>
        $"{line.LineNumber}. {line.SourceSku} · Đạt {InventoryPresentation.Quantity(line.AcceptedQuantity)} · Hư {InventoryPresentation.Quantity(line.DamagedQuantity)} · Thừa {InventoryPresentation.Quantity(line.OverQuantity)} · {(string.IsNullOrWhiteSpace(line.DestinationLocationCode) ? "Không nhập tồn" : $"Vị trí {line.DestinationLocationCode}")}{(string.IsNullOrWhiteSpace(line.Note) ? string.Empty : $" · {line.Note}")}"));
    public string ApprovalText => Data.DamageApproval is null
        ? string.Empty
        : $"Hư hỏng đã được duyệt lúc {InventoryPresentation.DateTimeText(Data.DamageApproval.ApprovedAt)}.";
    public string ReversalText => Data.Reversal is null ? string.Empty : $"Đã đảo: {Data.Reversal.Reason}";
    public bool HasApproval => Data.DamageApproval is not null;
    public bool IsReversed => Data.Reversal is not null;
    public bool CanApproveDamage { get; }
    public bool CanReverse { get; }

    public string DamageNote { get => _damageNote; set => Set(ref _damageNote, value ?? string.Empty); }
    public string ReverseReason { get => _reverseReason; set => Set(ref _reverseReason, value ?? string.Empty); }

    private void Set(ref string field, string value, [CallerMemberName] string? name = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public static class TransferPresentation
{
    public static string BalanceKey(InventoryBalanceData row) =>
        string.Join("|", row.WarehouseId, row.LocationId ?? "-", row.BaseVariantId, row.LotId ?? "-");

    public static string BalanceLabel(InventoryBalanceData row) =>
        $"{row.BaseSku} · {InventoryPresentation.First(row.LocationCode, "Không vị trí")} · {InventoryPresentation.First(row.LotCode, "Không lô")} · khả dụng {InventoryPresentation.Quantity(row.AvailableQuantity)}";

    public static string Status(string value, bool resolved) => value switch
    {
        "draft" => "Nháp",
        "approved" => "Đã duyệt",
        "dispatched" when resolved => "Đã xử lý nhận",
        "dispatched" => "Đang đi đường",
        "cancelled" => "Đã hủy",
        _ => "Trạng thái khác"
    };

    public static bool IsPositive(string? value) =>
        FulfillmentPresentation.IsValidPositiveQuantity(value);

    public static bool IsGreaterThan(string? left, string? right) =>
        FulfillmentPresentation.IsGreaterThan(left, right);
}
