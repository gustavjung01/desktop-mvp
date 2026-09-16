using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed class TransferViewModel : INotifyPropertyChanged
{
    private const string ReadDeniedMessage = "Tài khoản chưa được cấp quyền xem Chuyển kho.";
    private const int IdempotencyIntentCacheLimit = 256;

    private readonly IInventoryTransferService _service;
    private readonly IInventoryService _inventoryService;
    private readonly IInternalOrganizationService _organizationService;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private readonly List<InventoryTransferData> _transfers = [];
    private readonly List<InventoryTransferInTransitData> _inTransit = [];
    private readonly List<InventoryBalanceData> _balances = [];
    private readonly List<WarehouseLocationData> _locations = [];

    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private int _modeIndex;
    private string _search = string.Empty;
    private string _statusFilter = "all";
    private bool _isCreateOpen;
    private InventoryTransferData? _selectedTransfer;
    private InventoryTransferReceiptBundleData? _receiptBundle;
    private bool _isReceiptFormOpen;
    private DateTime _transferDate = DateTime.Today;
    private string _sourceWarehouseId = string.Empty;
    private string _destinationWarehouseId = string.Empty;
    private string _note = string.Empty;
    private string _cancelReason = string.Empty;
    private DateTime _receiptDate = DateTime.Today;
    private string _receiptNote = string.Empty;
    private string _shortReason = string.Empty;

    public TransferViewModel(
        IInventoryTransferService service,
        IInventoryService inventoryService,
        IInternalOrganizationService organizationService,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _inventoryService = inventoryService;
        _organizationService = organizationService;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        StatusOptions.Add(new TransferStatusOption("all", "Tất cả"));
        StatusOptions.Add(new TransferStatusOption("draft", "Nháp"));
        StatusOptions.Add(new TransferStatusOption("approved", "Đã duyệt"));
        StatusOptions.Add(new TransferStatusOption("dispatched", "Đã xuất chuyển"));
        StatusOptions.Add(new TransferStatusOption("cancelled", "Đã hủy"));

        DraftLines.Add(new TransferDraftLineRow());

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loaded = false;
            ResetSessionData();
            RaisePermissions();
            if (CanRead || !_access.Current.IsAuthenticated)
            {
                if (string.Equals(Message, ReadDeniedMessage, StringComparison.Ordinal)
                    || !_access.Current.IsAuthenticated)
                {
                    ClearMessage();
                }
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TransferStatusOption> StatusOptions { get; } = [];
    public ObservableCollection<TransferWarehouseOption> WarehouseOptions { get; } = [];
    public ObservableCollection<TransferWarehouseOption> DestinationWarehouseOptions { get; } = [];
    public ObservableCollection<TransferCardRow> TransferCards { get; } = [];
    public ObservableCollection<TransferTransitCardRow> TransitCards { get; } = [];
    public ObservableCollection<TransferDraftLineRow> DraftLines { get; } = [];
    public ObservableCollection<TransferDetailLineRow> DetailLines { get; } = [];
    public ObservableCollection<TransferResolutionRow> ResolutionRows { get; } = [];
    public ObservableCollection<TransferReceiptDraftRow> ReceiptDraftRows { get; } = [];
    public ObservableCollection<TransferReceiptHistoryRow> ReceiptHistoryRows { get; } = [];

    public bool CanRead => _access.HasPermission("core.inventory-transfer.read");
    public bool CanCreate => _access.HasPermission("core.inventory-transfer.create");
    public bool CanApprove => _access.HasPermission("core.inventory-transfer.approve");
    public bool CanDispatch => _access.HasPermission("core.inventory-transfer.dispatch");
    public bool CanCancel => _access.HasPermission("core.inventory-transfer.cancel");
    public bool CanReceive => _access.HasPermission("core.inventory-transfer.receive");
    public bool CanApproveDamage => _access.HasPermission("core.inventory-transfer.damage-approve");
    public bool CanResolve => _access.HasPermission("core.inventory-transfer.resolve");
    public bool CanReverse => _access.HasPermission("core.inventory-transfer.reverse");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoading => _busyAction == "load";
    public string RefreshText => _busyAction == "load" ? "Đang tải..." : "Làm mới";
    public string CreateButtonText => _busyAction == "create" ? "Đang tạo…" : "Lưu phiếu nháp";
    public string ApproveButtonText => _busyAction == "approve" ? "Đang duyệt…" : "Duyệt phiếu";
    public string DispatchButtonText => _busyAction == "dispatch" ? "Đang xuất kho…" : "Xuất kho chuyển";
    public string ReceiveButtonText => _busyAction == "receive" ? "Đang ghi nhận…" : "Xác nhận lần nhận";
    public string CloseShortButtonText => _busyAction == "close-short" ? "Đang đóng…" : "Đóng phần thiếu";

    public string Message
    {
        get => _message;
        private set
        {
            if (SetField(ref _message, value ?? string.Empty))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public int ModeIndex
    {
        get => _modeIndex;
        set
        {
            var next = Math.Clamp(value, 0, 1);
            if (!SetField(ref _modeIndex, next)) return;
            OnPropertyChanged(nameof(IsTransfersMode));
            OnPropertyChanged(nameof(IsTransitMode));
            OnPropertyChanged(nameof(HasBrowseRows));
            ApplyFilters();
        }
    }

    public bool IsTransfersMode => ModeIndex == 0;
    public bool IsTransitMode => ModeIndex == 1;

    public string Search
    {
        get => _search;
        set
        {
            if (SetField(ref _search, value ?? string.Empty))
                ApplyFilters();
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetField(ref _statusFilter, string.IsNullOrWhiteSpace(value) ? "all" : value))
                ApplyFilters();
        }
    }

    public bool IsCreateOpen
    {
        get => _isCreateOpen;
        private set
        {
            if (!SetField(ref _isCreateOpen, value)) return;
            OnPropertyChanged(nameof(IsBrowse));
            OnPropertyChanged(nameof(HasSelectedTransfer));
        }
    }

    public InventoryTransferData? SelectedTransfer
    {
        get => _selectedTransfer;
        private set
        {
            if (!SetField(ref _selectedTransfer, value)) return;
            OnPropertyChanged(nameof(HasSelectedTransfer));
            OnPropertyChanged(nameof(IsBrowse));
            OnPropertyChanged(nameof(DetailStatus));
            OnPropertyChanged(nameof(DetailNumber));
            OnPropertyChanged(nameof(DetailRouteDate));
            OnPropertyChanged(nameof(DetailSourceWarehouse));
            OnPropertyChanged(nameof(DetailDestinationWarehouse));
            OnPropertyChanged(nameof(DetailLineCount));
            OnPropertyChanged(nameof(DetailBaseQuantity));
            OnPropertyChanged(nameof(DetailMovementState));
            OnPropertyChanged(nameof(DetailUpdatedAt));
            OnPropertyChanged(nameof(IsDraftSelected));
            OnPropertyChanged(nameof(IsApprovedSelected));
            OnPropertyChanged(nameof(IsDispatchedSelected));
            OnPropertyChanged(nameof(CanApproveSelected));
            OnPropertyChanged(nameof(CanDispatchSelected));
            OnPropertyChanged(nameof(CanCancelSelected));
            OnPropertyChanged(nameof(CanReceiveSelected));
            OnPropertyChanged(nameof(CanPrintSelected));
        }
    }

    public bool HasSelectedTransfer => SelectedTransfer is not null && !IsCreateOpen;
    public bool IsBrowse => !IsCreateOpen && SelectedTransfer is null;
    public bool HasBrowseRows => IsTransfersMode ? TransferCards.Count > 0 : TransitCards.Count > 0;

    public string SummaryDraft { get; private set; } = "0";
    public string SummaryApproved { get; private set; } = "0";
    public string SummaryDispatched { get; private set; } = "0";
    public string SummaryInTransitLines { get; private set; } = "0";

    public DateTime TransferDate { get => _transferDate; set => SetField(ref _transferDate, value); }

    public string SourceWarehouseId
    {
        get => _sourceWarehouseId;
        set
        {
            if (!SetField(ref _sourceWarehouseId, value ?? string.Empty)) return;
            if (_destinationWarehouseId == _sourceWarehouseId)
            {
                _destinationWarehouseId = string.Empty;
                OnPropertyChanged(nameof(DestinationWarehouseId));
            }
            RefreshDestinationWarehouseOptions();
            RefreshDraftLineOptions();
            OnPropertyChanged(nameof(CanAddDraftLine));
            OnPropertyChanged(nameof(CanCreateTransfer));
        }
    }

    public string DestinationWarehouseId
    {
        get => _destinationWarehouseId;
        set
        {
            if (SetField(ref _destinationWarehouseId, value ?? string.Empty))
                OnPropertyChanged(nameof(CanCreateTransfer));
        }
    }

    public string Note { get => _note; set => SetField(ref _note, value ?? string.Empty); }
    public string CancelReason { get => _cancelReason; set => SetField(ref _cancelReason, value ?? string.Empty); }
    public DateTime ReceiptDate { get => _receiptDate; set => SetField(ref _receiptDate, value); }
    public string ReceiptNote { get => _receiptNote; set => SetField(ref _receiptNote, value ?? string.Empty); }
    public string ShortReason { get => _shortReason; set => SetField(ref _shortReason, value ?? string.Empty); }

    public bool IsReceiptFormOpen
    {
        get => _isReceiptFormOpen;
        private set => SetField(ref _isReceiptFormOpen, value);
    }

    public bool CanAddDraftLine => !string.IsNullOrWhiteSpace(SourceWarehouseId);
    public bool CanRemoveDraftLine => DraftLines.Count > 1;
    public bool CanCreateTransfer => CanCreate && IsNotBusy && !string.IsNullOrWhiteSpace(SourceWarehouseId) && !string.IsNullOrWhiteSpace(DestinationWarehouseId);

    public string DetailStatus
    {
        get
        {
            if (SelectedTransfer is null) return string.Empty;
            return TransferPresentation.Status(SelectedTransfer.Status, !IsTransferInTransit(SelectedTransfer.Id));
        }
    }

    public string DetailNumber => InventoryPresentation.First(SelectedTransfer?.DocumentNumber, "Phiếu chuyển kho nháp");
    public string DetailRouteDate => SelectedTransfer is null
        ? string.Empty
        : $"{SelectedTransfer.SourceWarehouseCode} → {SelectedTransfer.DestinationWarehouseCode} · {InventoryPresentation.Date(SelectedTransfer.TransferDate)}";
    public string DetailSourceWarehouse => SelectedTransfer?.SourceWarehouseName ?? string.Empty;
    public string DetailDestinationWarehouse => SelectedTransfer?.DestinationWarehouseName ?? string.Empty;
    public string DetailLineCount => (SelectedTransfer?.Lines?.Length ?? SelectedTransfer?.LineCount ?? 0).ToString(CultureInfo.InvariantCulture);
    public string DetailBaseQuantity => InventoryPresentation.Quantity(SelectedTransfer?.BaseQuantityTotal);
    public string DetailMovementState => SelectedTransfer?.InventoryMovementId is null ? "Chưa ghi sổ" : "Đã ghi sổ";
    public string DetailUpdatedAt => InventoryPresentation.DateTimeText(SelectedTransfer?.UpdatedAt);

    public bool IsDraftSelected => SelectedTransfer?.Status == "draft";
    public bool IsApprovedSelected => SelectedTransfer?.Status == "approved";
    public bool IsDispatchedSelected => SelectedTransfer?.Status == "dispatched";
    public bool CanApproveSelected => IsDraftSelected && CanApprove && IsNotBusy;
    public bool CanDispatchSelected => IsApprovedSelected && CanDispatch && IsNotBusy;
    public bool CanCancelSelected => SelectedTransfer is not null && SelectedTransfer.Status is "draft" or "approved" && CanCancel && IsNotBusy;
    public bool CanReceiveSelected => SelectedTransfer?.Status == "dispatched"
        && _receiptBundle is not null
        && _receiptBundle.ShortClosure is null
        && CanReceive
        && IsNotBusy;
    public bool CanPrintSelected => SelectedTransfer is not null;

    public bool HasReceiptWorkspace => SelectedTransfer?.Status == "dispatched" && _receiptBundle is not null;
    public bool HasReceipts => ReceiptHistoryRows.Count > 0;
    public bool HasRemainingTransfer => _receiptBundle is not null
        && _receiptBundle.ShortClosure is null
        && _receiptBundle.Resolution.Any(line => TransferPresentation.IsPositive(line.RemainingBaseQuantity));
    public bool HasShortClosure => _receiptBundle?.ShortClosure is not null;
    public bool CanCloseShortSelected => CanResolve && HasRemainingTransfer && IsNotBusy;
    public string ShortClosureText => _receiptBundle?.ShortClosure is null
        ? string.Empty
        : $"Đã đóng phần thiếu. {_receiptBundle.ShortClosure.Reason} · {InventoryPresentation.DateTimeText(_receiptBundle.ShortClosure.ClosedAt)}";

    public string RemainingTransferText
    {
        get
        {
            if (_receiptBundle is null) return "0";
            decimal total = 0m;
            foreach (var line in _receiptBundle.Resolution)
            {
                if (decimal.TryParse(line.RemainingBaseQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                    total += value;
            }
            return InventoryPresentation.Quantity(total.ToString(CultureInfo.InvariantCulture));
        }
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        if (!CanRead)
        {
            if (_access.Current.IsAuthenticated) SetErrorMessage(ReadDeniedMessage);
            return;
        }

        var loadTask = _initialLoadTask ??= LoadAsync();
        try
        {
            _loaded = await loadTask.ConfigureAwait(true);
        }
        finally
        {
            if (ReferenceEquals(_initialLoadTask, loadTask)) _initialLoadTask = null;
        }
    }

    public async Task RefreshAsync()
    {
        _loaded = await LoadAsync(SelectedTransfer?.Id, preserveMessage: false).ConfigureAwait(true);
    }

    public void ResetFilters()
    {
        _search = string.Empty;
        _statusFilter = "all";
        OnPropertyChanged(nameof(Search));
        OnPropertyChanged(nameof(StatusFilter));
        ApplyFilters();
    }

    public void OpenCreate()
    {
        if (!CanCreate || IsBusy) return;
        SelectedTransfer = null;
        _receiptBundle = null;
        IsReceiptFormOpen = false;
        ResetCreateForm();
        IsCreateOpen = true;
        ClearMessage();
    }

    public void CloseCreate()
    {
        IsCreateOpen = false;
        ResetCreateForm();
    }

    public void AddDraftLine()
    {
        if (!CanAddDraftLine) return;
        var row = new TransferDraftLineRow();
        DraftLines.Add(row);
        RefreshDraftLineOptions();
        OnPropertyChanged(nameof(CanRemoveDraftLine));
    }

    public void RemoveDraftLine(TransferDraftLineRow? row)
    {
        if (row is null || DraftLines.Count <= 1) return;
        DraftLines.Remove(row);
        OnPropertyChanged(nameof(CanRemoveDraftLine));
    }

    public async Task CreateAsync()
    {
        if (!CanCreateTransfer) return;

        if (SourceWarehouseId == DestinationWarehouseId)
        {
            SetErrorMessage("Chọn hai kho khác nhau trước khi tạo phiếu.");
            return;
        }

        var selected = DraftLines.Select(row => (Row: row, Option: row.SelectedOption)).ToArray();
        if (selected.Any(item => item.Option is null || !TransferPresentation.IsPositive(item.Row.Quantity)))
        {
            SetErrorMessage("Mỗi dòng phải chọn hàng tồn và nhập số lượng lớn hơn 0.");
            return;
        }

        foreach (var item in selected)
        {
            if (TransferPresentation.IsGreaterThan(item.Row.Quantity, item.Option!.Data.AvailableQuantity))
            {
                SetErrorMessage($"Số lượng chuyển của {item.Option.Data.BaseSku} vượt số khả dụng {InventoryPresentation.Quantity(item.Option.Data.AvailableQuantity)}.");
                return;
            }
        }

        var request = new InventoryTransferCreateRequest(
            TransferDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            SourceWarehouseId,
            DestinationWarehouseId,
            NormalizeOptional(Note),
            selected.Select(item => new InventoryTransferCreateLineRequest(
                item.Option!.Data.BaseVariantId,
                item.Option.Data.LocationId,
                item.Option.Data.LotId,
                item.Row.Quantity.Trim())).ToArray());

        var fingerprint = new StringBuilder()
            .Append(request.TransferDate).Append('|')
            .Append(request.SourceWarehouseId).Append('|')
            .Append(request.DestinationWarehouseId).Append('|')
            .Append(request.Note).Append('|')
            .Append(string.Join(";", request.Lines.Select(line => $"{line.SourceVariantId}.{line.SourceLocationId}.{line.LotId}.{line.SourceQuantity}")))
            .ToString();

        SetBusy("create");
        ClearMessage();
        try
        {
            var created = await _service.CreateAsync(
                request,
                KeyFor("create", "new", fingerprint)).ConfigureAwait(true);
            IsCreateOpen = false;
            ResetCreateForm();
            var refreshed = await LoadAsync(created.Id, preserveMessage: true).ConfigureAwait(true);
            if (refreshed)
            {
                SetNotice("Đã tạo phiếu chuyển kho nháp.");
            }
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public async Task OpenTransferAsync(string transferId)
    {
        if (!CanRead || IsBusy) return;
        SetBusy($"detail-{transferId}");
        ClearMessage();
        try
        {
            var detail = await _service.GetTransferAsync(transferId).ConfigureAwait(true);
            IsCreateOpen = false;
            SelectedTransfer = detail;
            BuildDetailLines();
            IsReceiptFormOpen = false;
            if (detail.Status == "dispatched")
                await LoadReceiptBundleAsync(detail.Id).ConfigureAwait(true);
            else
                ClearReceiptBundle();
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public void CloseDetail()
    {
        SelectedTransfer = null;
        ClearReceiptBundle();
        IsReceiptFormOpen = false;
        CancelReason = string.Empty;
    }

    public async Task ApproveAsync() =>
        await TransitionAsync("approve").ConfigureAwait(true);

    public async Task DispatchAsync() =>
        await TransitionAsync("dispatch").ConfigureAwait(true);

    public async Task CancelAsync()
    {
        if (SelectedTransfer is null || !CanCancelSelected) return;
        if (string.IsNullOrWhiteSpace(CancelReason))
        {
            SetErrorMessage("Nhập lý do hủy phiếu.");
            return;
        }
        await TransitionAsync("cancel").ConfigureAwait(true);
    }

    public void OpenReceiptForm()
    {
        if (!CanReceiveSelected || _receiptBundle is null || SelectedTransfer is null) return;
        var options = _locations
            .Where(location => location.IsActive && location.WarehouseId == SelectedTransfer.DestinationWarehouseId)
            .OrderBy(location => location.Code, StringComparer.Ordinal)
            .Select(location => new TransferLocationOption(location.Id, $"{location.Code} — {location.Name}"))
            .ToArray();

        ReceiptDate = DateTime.Today;
        ReceiptNote = string.Empty;
        Replace(ReceiptDraftRows, _receiptBundle.Resolution.Select(line => new TransferReceiptDraftRow(line, options)));
        IsReceiptFormOpen = true;
        ClearMessage();
    }

    public void CloseReceiptForm()
    {
        IsReceiptFormOpen = false;
        ReceiptDraftRows.Clear();
    }

    public async Task SubmitReceiptAsync()
    {
        if (SelectedTransfer is null || _receiptBundle is null || !CanReceive || IsBusy) return;

        var active = ReceiptDraftRows.Where(row =>
            TransferPresentation.IsPositive(row.AcceptedQuantity)
            || TransferPresentation.IsPositive(row.DamagedQuantity)
            || TransferPresentation.IsPositive(row.OverQuantity)).ToArray();

        if (active.Length == 0)
        {
            SetErrorMessage("Nhập ít nhất một số lượng nhận đạt, hư hỏng hoặc thừa.");
            return;
        }

        if (active.Any(row => TransferPresentation.IsPositive(row.AcceptedQuantity) && string.IsNullOrWhiteSpace(row.DestinationLocationId)))
        {
            SetErrorMessage("Hàng nhận đạt phải có vị trí nhập tại kho đích.");
            return;
        }

        var request = new InventoryTransferReceiptCreateRequest(
            ReceiptDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            NormalizeOptional(ReceiptNote),
            active.Select(row => new InventoryTransferReceiptLineRequest(
                row.Data.TransferLineId,
                string.IsNullOrWhiteSpace(row.DestinationLocationId) ? null : row.DestinationLocationId,
                NormalizeQuantity(row.AcceptedQuantity),
                NormalizeQuantity(row.DamagedQuantity),
                NormalizeQuantity(row.OverQuantity),
                NormalizeOptional(row.Note))).ToArray());

        var fingerprint = $"{SelectedTransfer.Revision}|{request.ReceiptDate}|{request.Note}|"
            + string.Join(";", request.Lines.Select(line => $"{line.TransferLineId}.{line.DestinationLocationId}.{line.AcceptedQuantity}.{line.DamagedQuantity}.{line.OverQuantity}.{line.Note}"));

        SetBusy("receive");
        ClearMessage();
        try
        {
            await _service.ReceiveAsync(
                SelectedTransfer.Id,
                request,
                KeyFor("receive", SelectedTransfer.Id, fingerprint)).ConfigureAwait(true);
            IsReceiptFormOpen = false;
            ReceiptDraftRows.Clear();
            await ReloadSelectedAsync().ConfigureAwait(true);
            SetNotice("Đã ghi nhận lần nhận chuyển kho. Chỉ hàng đạt được cộng tồn khả dụng.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public async Task ApproveDamageAsync(TransferReceiptHistoryRow? row)
    {
        if (row is null || SelectedTransfer is null || !row.CanApproveDamage || IsBusy) return;
        var fingerprint = $"{SelectedTransfer.Revision}|{row.Data.Id}|{row.DamageNote.Trim()}";
        SetBusy($"damage-{row.Data.Id}");
        ClearMessage();
        try
        {
            await _service.ApproveDamageAsync(
                SelectedTransfer.Id,
                row.Data.Id,
                new InventoryTransferDamageApprovalRequest(NormalizeOptional(row.DamageNote)),
                KeyFor("damage-approve", row.Data.Id, fingerprint)).ConfigureAwait(true);
            await ReloadSelectedAsync().ConfigureAwait(true);
            SetNotice("Quản lý kho đã xác nhận biên bản hư hỏng.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public async Task ReverseReceiptAsync(TransferReceiptHistoryRow? row)
    {
        if (row is null || SelectedTransfer is null || !row.CanReverse || IsBusy) return;
        if (string.IsNullOrWhiteSpace(row.ReverseReason))
        {
            SetErrorMessage("Nhập lý do trước khi đảo lần nhận.");
            return;
        }

        var request = new InventoryTransferReverseReceiptRequest(
            DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            row.ReverseReason.Trim());
        var fingerprint = $"{SelectedTransfer.Revision}|{row.Data.Id}|{request.DocumentDate}|{request.Reason}";
        SetBusy($"reverse-{row.Data.Id}");
        ClearMessage();
        try
        {
            await _service.ReverseReceiptAsync(
                SelectedTransfer.Id,
                row.Data.Id,
                request,
                KeyFor("receipt-reverse", row.Data.Id, fingerprint)).ConfigureAwait(true);
            await ReloadSelectedAsync().ConfigureAwait(true);
            SetNotice("Đã đảo lần nhận và mở lại phần hàng đang đi đường tương ứng.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public async Task CloseShortAsync()
    {
        if (SelectedTransfer is null || !CanResolve || !HasRemainingTransfer || IsBusy) return;
        if (string.IsNullOrWhiteSpace(ShortReason))
        {
            SetErrorMessage("Nhập lý do trước khi đóng phần thiếu.");
            return;
        }

        var reason = ShortReason.Trim();
        var fingerprint = $"{SelectedTransfer.Revision}|{RemainingTransferText}|{reason}";
        SetBusy("close-short");
        ClearMessage();
        try
        {
            await _service.CloseShortAsync(
                SelectedTransfer.Id,
                new InventoryTransferCloseShortRequest(reason),
                KeyFor("close-short", SelectedTransfer.Id, fingerprint)).ConfigureAwait(true);
            ShortReason = string.Empty;
            await ReloadSelectedAsync().ConfigureAwait(true);
            SetNotice("Đã đóng phần thiếu bằng biên bản riêng; số lượng xuất gốc không bị sửa.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    public InventoryTransferData? GetPrintableTransfer() => SelectedTransfer;

    private async Task TransitionAsync(string action)
    {
        if (SelectedTransfer is null || IsBusy) return;
        var transfer = SelectedTransfer;
        if (action == "approve" && !CanApproveSelected) return;
        if (action == "dispatch" && !CanDispatchSelected) return;
        if (action == "cancel" && !CanCancelSelected) return;

        SetBusy(action);
        ClearMessage();
        try
        {
            InventoryTransferData updated;
            if (action == "approve")
            {
                updated = await _service.ApproveAsync(
                    transfer.Id,
                    new InventoryTransferTransitionRequest(transfer.Revision),
                    KeyFor("approve", transfer.Id, transfer.Revision)).ConfigureAwait(true);
            }
            else if (action == "dispatch")
            {
                updated = await _service.DispatchAsync(
                    transfer.Id,
                    new InventoryTransferTransitionRequest(transfer.Revision),
                    KeyFor("dispatch", transfer.Id, transfer.Revision)).ConfigureAwait(true);
            }
            else
            {
                var reason = CancelReason.Trim();
                updated = await _service.CancelAsync(
                    transfer.Id,
                    new InventoryTransferCancelRequest(transfer.Revision, reason),
                    KeyFor("cancel", transfer.Id, $"{transfer.Revision}|{reason}")).ConfigureAwait(true);
            }

            CancelReason = string.Empty;
            var refreshed = await LoadAsync(updated.Id, preserveMessage: true).ConfigureAwait(true);
            if (refreshed)
            {
                SetNotice(action switch
                {
                    "approve" => "Phiếu đã được duyệt.",
                    "dispatch" => "Đã xuất kho nguồn và ghi nhận hàng đang đi đường.",
                    _ => "Phiếu đã được hủy."
                });
            }
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    private async Task<bool> LoadAsync(string? selectId = null, bool preserveMessage = false)
    {
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetErrorMessage(ReadDeniedMessage);
            return false;
        }

        var generation = _accessGeneration;
        SetBusy("load");
        if (!preserveMessage) ClearMessage();

        var failures = new List<string>();
        try
        {
            IReadOnlyList<InventoryTransferData> transfers = [];
            IReadOnlyList<InventoryTransferInTransitData> inTransit = [];
            IReadOnlyList<InventoryBalanceData> balances = [];
            IReadOnlyList<WarehouseLocationData> locations = [];

            var transfersTask = _service.ListTransfersAsync();
            var inTransitTask = _service.ListInTransitAsync();
            var balancesTask = _inventoryService.ListBalancesAsync();
            var locationsTask = _organizationService.ListLocationsAsync();

            try { transfers = await transfersTask.ConfigureAwait(true); }
            catch (Exception exception) { failures.Add($"Danh sách phiếu: {ErrorText(exception)}"); }

            try { inTransit = await inTransitTask.ConfigureAwait(true); }
            catch (Exception exception) { failures.Add($"Hàng đang đi đường: {ErrorText(exception)}"); }

            try { balances = await balancesTask.ConfigureAwait(true); }
            catch (Exception exception) { failures.Add($"Tồn kho khả dụng: {ErrorText(exception)}"); }

            try { locations = await locationsTask.ConfigureAwait(true); }
            catch (Exception exception) { failures.Add($"Vị trí kho: {ErrorText(exception)}"); }

            if (generation != _accessGeneration || !CanRead) return false;

            _transfers.Clear();
            _transfers.AddRange(transfers);
            _inTransit.Clear();
            _inTransit.AddRange(inTransit);
            _balances.Clear();
            _balances.AddRange(balances);
            _locations.Clear();
            _locations.AddRange(locations.Where(location => location.IsActive));

            BuildWarehouseOptions();
            RecalculateSummary();
            ApplyFilters();

            if (!string.IsNullOrWhiteSpace(selectId))
            {
                try
                {
                    var detail = await _service.GetTransferAsync(selectId).ConfigureAwait(true);
                    if (generation != _accessGeneration || !CanRead) return false;
                    SelectedTransfer = detail;
                    IsCreateOpen = false;
                    BuildDetailLines();
                    if (detail.Status == "dispatched")
                        await LoadReceiptBundleAsync(detail.Id).ConfigureAwait(true);
                    else
                        ClearReceiptBundle();
                }
                catch (Exception exception)
                {
                    failures.Add($"Chi tiết phiếu: {ErrorText(exception)}");
                }
            }
            else if (SelectedTransfer is not null)
            {
                var selectedId = SelectedTransfer.Id;
                var summary = _transfers.FirstOrDefault(transfer => transfer.Id == selectedId);
                if (summary is null) CloseDetail();
            }

            if (failures.Count > 0)
            {
                SetErrorMessage($"Một phần dữ liệu chuyển kho chưa tải được. {string.Join(" · ", failures)}");
                return false;
            }

            return true;
        }
        finally
        {
            if (_busyAction == "load") SetBusy(null);
        }
    }

    private async Task ReloadSelectedAsync()
    {
        if (SelectedTransfer is null) return;
        var id = SelectedTransfer.Id;
        var detail = await _service.GetTransferAsync(id).ConfigureAwait(true);
        SelectedTransfer = detail;
        BuildDetailLines();
        if (detail.Status == "dispatched")
            await LoadReceiptBundleAsync(id).ConfigureAwait(true);
        else
            ClearReceiptBundle();

        var index = _transfers.FindIndex(item => item.Id == id);
        if (index >= 0) _transfers[index] = detail;
        RecalculateSummary();
        ApplyFilters();
    }

    private async Task LoadReceiptBundleAsync(string transferId)
    {
        _receiptBundle = await _service.GetReceiptBundleAsync(transferId).ConfigureAwait(true);
        Replace(ResolutionRows, _receiptBundle.Resolution.Select((line, index) => new TransferResolutionRow(
            index + 1,
            $"{line.SourceSku} · {line.ItemName}{(string.IsNullOrWhiteSpace(line.LotCode) ? string.Empty : $" · lô {line.LotCode}")}",
            line.SourceUnitCode,
            InventoryPresentation.Quantity(line.DispatchedQuantity),
            InventoryPresentation.Quantity(line.AcceptedQuantity),
            InventoryPresentation.Quantity(line.DamagedQuantity),
            InventoryPresentation.Quantity(line.ShortQuantity),
            InventoryPresentation.Quantity(line.OverQuantity),
            InventoryPresentation.Quantity(line.RemainingQuantity))));
        Replace(ReceiptHistoryRows, _receiptBundle.Receipts.Select(receipt =>
            new TransferReceiptHistoryRow(receipt, CanApproveDamage, CanReverse, _receiptBundle.ShortClosure is not null)));
        IsReceiptFormOpen = false;
        ReceiptDraftRows.Clear();
        NotifyReceiptState();
    }

    private void ClearReceiptBundle()
    {
        _receiptBundle = null;
        ResolutionRows.Clear();
        ReceiptHistoryRows.Clear();
        ReceiptDraftRows.Clear();
        IsReceiptFormOpen = false;
        NotifyReceiptState();
    }

    private void BuildDetailLines()
    {
        Replace(DetailLines, (SelectedTransfer?.Lines ?? []).Select((line, index) => new TransferDetailLineRow(
            index + 1,
            $"{line.SourceSku}{Environment.NewLine}{line.ItemName}",
            $"{InventoryPresentation.Quantity(line.SourceQuantity)} {line.SourceUnitCode}",
            InventoryPresentation.First(line.LotCode, "Không lô"),
            line.ExpiryDate is null ? "Không có" : InventoryPresentation.Date(line.ExpiryDate))));
    }

    private void BuildWarehouseOptions()
    {
        var values = new Dictionary<string, TransferWarehouseOption>(StringComparer.Ordinal);
        foreach (var balance in _balances)
            values[balance.WarehouseId] = new TransferWarehouseOption(balance.WarehouseId, $"{balance.WarehouseCode} — {balance.WarehouseName}");
        foreach (var transfer in _transfers)
        {
            values[transfer.SourceWarehouseId] = new TransferWarehouseOption(transfer.SourceWarehouseId, $"{transfer.SourceWarehouseCode} — {transfer.SourceWarehouseName}");
            values[transfer.DestinationWarehouseId] = new TransferWarehouseOption(transfer.DestinationWarehouseId, $"{transfer.DestinationWarehouseCode} — {transfer.DestinationWarehouseName}");
        }
        Replace(WarehouseOptions, values.Values.OrderBy(option => option.Label, StringComparer.Ordinal));
        RefreshDestinationWarehouseOptions();
        RefreshDraftLineOptions();
    }

    private void RefreshDestinationWarehouseOptions()
    {
        Replace(
            DestinationWarehouseOptions,
            WarehouseOptions.Where(option => option.Id != SourceWarehouseId));
        if (DestinationWarehouseId == SourceWarehouseId
            || !DestinationWarehouseOptions.Any(option => option.Id == DestinationWarehouseId))
        {
            _destinationWarehouseId = string.Empty;
            OnPropertyChanged(nameof(DestinationWarehouseId));
        }
    }

    private void RefreshDraftLineOptions()
    {
        var options = _balances
            .Where(balance => balance.WarehouseId == SourceWarehouseId && TransferPresentation.IsPositive(balance.AvailableQuantity))
            .OrderBy(balance => balance.BaseSku, StringComparer.Ordinal)
            .ThenBy(balance => balance.LocationCode, StringComparer.Ordinal)
            .ThenBy(balance => balance.LotCode, StringComparer.Ordinal)
            .Select(balance => new TransferBalanceOption(
                TransferPresentation.BalanceKey(balance),
                TransferPresentation.BalanceLabel(balance),
                InventoryPresentation.Quantity(balance.AvailableQuantity),
                balance))
            .ToArray();

        foreach (var row in DraftLines) row.RefreshOptions(options);
    }

    private void RecalculateSummary()
    {
        SummaryDraft = _transfers.Count(item => item.Status == "draft").ToString(CultureInfo.InvariantCulture);
        SummaryApproved = _transfers.Count(item => item.Status == "approved").ToString(CultureInfo.InvariantCulture);
        SummaryDispatched = _transfers.Count(item => item.Status == "dispatched").ToString(CultureInfo.InvariantCulture);
        SummaryInTransitLines = _inTransit.Count.ToString(CultureInfo.InvariantCulture);
        OnPropertyChanged(nameof(SummaryDraft));
        OnPropertyChanged(nameof(SummaryApproved));
        OnPropertyChanged(nameof(SummaryDispatched));
        OnPropertyChanged(nameof(SummaryInTransitLines));
    }

    private void ApplyFilters()
    {
        var term = Search.Trim().ToLower(CultureInfo.GetCultureInfo("vi-VN"));
        var transitIds = _inTransit.Select(line => line.TransferId).ToHashSet(StringComparer.Ordinal);

        var transfers = _transfers.Where(transfer =>
        {
            if (StatusFilter != "all" && transfer.Status != StatusFilter) return false;
            if (term.Length == 0) return true;
            var haystack = string.Join(" ", transfer.DocumentNumber, transfer.SourceWarehouseCode, transfer.SourceWarehouseName,
                transfer.DestinationWarehouseCode, transfer.DestinationWarehouseName, transfer.Status, transfer.Note)
                .ToLower(CultureInfo.GetCultureInfo("vi-VN"));
            return haystack.Contains(term, StringComparison.Ordinal);
        }).Select((transfer, index) => new TransferCardRow(
            index + 1,
            transfer.Id,
            TransferPresentation.Status(transfer.Status, transfer.Status == "dispatched" && !transitIds.Contains(transfer.Id)),
            InventoryPresentation.Date(transfer.TransferDate),
            InventoryPresentation.First(transfer.DocumentNumber, "Phiếu nháp chưa cấp số"),
            $"{transfer.SourceWarehouseCode} → {transfer.DestinationWarehouseCode}",
            transfer.LineCount.ToString(CultureInfo.InvariantCulture),
            InventoryPresentation.Quantity(transfer.BaseQuantityTotal),
            string.Empty,
            transfer)).ToArray();

        var inTransit = _inTransit.Where(line =>
        {
            if (term.Length == 0) return true;
            var haystack = string.Join(" ", line.DocumentNumber, line.SourceWarehouseCode, line.SourceWarehouseName,
                line.DestinationWarehouseCode, line.DestinationWarehouseName, line.SourceSku, line.ItemName, line.LotCode)
                .ToLower(CultureInfo.GetCultureInfo("vi-VN"));
            return haystack.Contains(term, StringComparison.Ordinal);
        }).Select((line, index) => new TransferTransitCardRow(
            index + 1,
            line.TransferId,
            "Đang đi đường",
            InventoryPresentation.DateTimeText(line.DispatchedAt),
            $"{line.SourceSku} · {line.ItemName}",
            $"{line.SourceWarehouseCode} → {line.DestinationWarehouseCode}",
            line.DocumentNumber,
            $"{InventoryPresentation.Quantity(line.SourceQuantity)} {line.SourceUnitCode}",
            InventoryPresentation.First(line.LotCode, "Không lô"),
            string.Empty,
            line)).ToArray();

        Replace(TransferCards, transfers);
        Replace(TransitCards, inTransit);
        OnPropertyChanged(nameof(HasBrowseRows));
    }

    private void ResetCreateForm()
    {
        TransferDate = DateTime.Today;
        SourceWarehouseId = string.Empty;
        DestinationWarehouseId = string.Empty;
        Note = string.Empty;
        DraftLines.Clear();
        DraftLines.Add(new TransferDraftLineRow());
        RefreshDraftLineOptions();
        OnPropertyChanged(nameof(CanRemoveDraftLine));
        OnPropertyChanged(nameof(CanCreateTransfer));
    }

    private void ResetSessionData()
    {
        _transfers.Clear();
        _inTransit.Clear();
        _balances.Clear();
        _locations.Clear();
        _intentKeys.Clear();
        TransferCards.Clear();
        TransitCards.Clear();
        WarehouseOptions.Clear();
        DestinationWarehouseOptions.Clear();
        SelectedTransfer = null;
        IsCreateOpen = false;
        ClearReceiptBundle();
        DetailLines.Clear();
        ResetCreateForm();
        SummaryDraft = "0";
        SummaryApproved = "0";
        SummaryDispatched = "0";
        SummaryInTransitLines = "0";
        OnPropertyChanged(nameof(SummaryDraft));
        OnPropertyChanged(nameof(SummaryApproved));
        OnPropertyChanged(nameof(SummaryDispatched));
        OnPropertyChanged(nameof(SummaryInTransitLines));
    }

    private bool IsTransferInTransit(string transferId) =>
        _inTransit.Any(line => line.TransferId == transferId);

    private string KeyFor(string prefix, string id, string fingerprint)
    {
        var intent = $"{prefix}:{id}:{fingerprint}";
        if (_intentKeys.TryGetValue(intent, out var existing)) return existing;
        if (_intentKeys.Count >= IdempotencyIntentCacheLimit)
        {
            var oldest = _intentKeys.Keys.FirstOrDefault();
            if (oldest is not null) _intentKeys.Remove(oldest);
        }
        var key = _idempotencyKeys.Create($"transfer-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private void NotifyReceiptState()
    {
        OnPropertyChanged(nameof(HasReceiptWorkspace));
        OnPropertyChanged(nameof(HasReceipts));
        OnPropertyChanged(nameof(HasRemainingTransfer));
        OnPropertyChanged(nameof(CanCloseShortSelected));
        OnPropertyChanged(nameof(HasShortClosure));
        OnPropertyChanged(nameof(ShortClosureText));
        OnPropertyChanged(nameof(RemainingTransferText));
        OnPropertyChanged(nameof(CanReceiveSelected));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanDispatch));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CanReceive));
        OnPropertyChanged(nameof(CanApproveDamage));
        OnPropertyChanged(nameof(CanResolve));
        OnPropertyChanged(nameof(CanReverse));
        OnPropertyChanged(nameof(CanCreateTransfer));
        OnPropertyChanged(nameof(CanApproveSelected));
        OnPropertyChanged(nameof(CanDispatchSelected));
        OnPropertyChanged(nameof(CanCancelSelected));
        OnPropertyChanged(nameof(CanReceiveSelected));
        OnPropertyChanged(nameof(CanCloseShortSelected));
    }

    private void SetBusy(string? action)
    {
        if (_busyAction == action) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(RefreshText));
        OnPropertyChanged(nameof(CreateButtonText));
        OnPropertyChanged(nameof(ApproveButtonText));
        OnPropertyChanged(nameof(DispatchButtonText));
        OnPropertyChanged(nameof(ReceiveButtonText));
        OnPropertyChanged(nameof(CloseShortButtonText));
        OnPropertyChanged(nameof(CanCreateTransfer));
        OnPropertyChanged(nameof(CanApproveSelected));
        OnPropertyChanged(nameof(CanDispatchSelected));
        OnPropertyChanged(nameof(CanCancelSelected));
        OnPropertyChanged(nameof(CanReceiveSelected));
        OnPropertyChanged(nameof(CanCloseShortSelected));
    }

    private void ClearMessage()
    {
        Message = string.Empty;
        MessageIsError = false;
    }

    private void SetNotice(string message)
    {
        MessageIsError = false;
        Message = message;
    }

    private void SetErrorMessage(string message)
    {
        MessageIsError = true;
        Message = message;
    }

    private void SetError(Exception exception) => SetErrorMessage(ErrorText(exception));

    private static string ErrorText(Exception exception) =>
        exception is CanonicalApiException apiException
            ? CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(apiException), apiException.RequestId)
            : exception.Message;

    private static string? NormalizeOptional(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeQuantity(string value) =>
        string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
