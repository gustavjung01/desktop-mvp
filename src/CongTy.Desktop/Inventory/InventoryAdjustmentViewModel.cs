using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record AdjustmentOption(string Id, string Label);
public sealed record AdjustmentWarehouseOption(string Id, string Label);
public sealed record AdjustmentLocationOption(string Id, string Label);
public sealed record AdjustmentReasonOption(string Id, string Label);
public sealed record AdjustmentSourceOption(string Id, string Label, InventoryBalanceData Data);

public sealed record InventoryAdjustmentListRow(InventoryAdjustmentData Data)
{
    public string Number => Data.AdjustmentNumber;
    public string Kind => InventoryAdjustmentPresentation.Kind(Data.DocumentKind);
    public string Warehouse => InventoryAdjustmentPresentation.Warehouse(Data.WarehouseCode, Data.WarehouseName);
    public string Status => InventoryAdjustmentPresentation.Status(Data.Status);
    public string Created => InventoryAdjustmentPresentation.DateTimeText(Data.CreatedAt);
    public string Summary => $"{Warehouse} · {Status} · {Created}";
}

public sealed record InventoryAdjustmentLineRow(
    int LineNumber,
    string Product,
    string Sku,
    string Lot,
    string SourceLocation,
    string DestinationLocation,
    string Quantity,
    string Effect);

public sealed class BulkAdjustmentRowView : INotifyPropertyChanged
{
    private string _locationCode;
    private string _lotCode;
    private readonly Action _changed;

    public BulkAdjustmentRowView(
        BulkInventoryAdjustmentInputRow input,
        BulkInventoryAdjustmentPreviewRow? preview,
        Action changed)
    {
        InputLineNumber = input.LineNumber;
        Sku = input.Sku;
        ActualQuantity = input.ActualQuantity;
        _locationCode = string.IsNullOrWhiteSpace(input.LocationCode)
            ? preview?.LocationCode ?? string.Empty
            : input.LocationCode;
        _lotCode = string.IsNullOrWhiteSpace(input.LotCode)
            ? preview?.LotCode ?? string.Empty
            : input.LotCode;
        _changed = changed;

        ProductName = preview?.ProductName ?? string.Empty;
        CurrentQuantity = preview?.CurrentBaseQuantity is null
            ? "Chưa xác định"
            : $"{InventoryAdjustmentPresentation.Quantity(preview.CurrentBaseQuantity)} {preview.BaseUnitCode}".Trim();
        ActualQuantityDisplay = preview is null
            ? input.ActualQuantity
            : $"{InventoryAdjustmentPresentation.Quantity(preview.EnteredQuantity)} {preview.EnteredUnitCode}".Trim();
        Difference = preview?.DeltaBaseQuantity is null
            ? "Chưa xác định"
            : $"{InventoryAdjustmentPresentation.SignedQuantity(preview.DeltaBaseQuantity)} {preview.BaseUnitCode}".Trim();
        Result = preview is null ? "Chờ kiểm tra" : InventoryAdjustmentPresentation.Direction(preview.Direction);
        Status = preview is null
            ? "Chờ kiểm tra"
            : preview.Status == "READY"
                ? "Sẵn sàng"
                : preview.Errors.FirstOrDefault()?.Message ?? "Cần xử lý";

        var locationChoices = (preview?.ScopeOptions ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.LocationCode))
            .GroupBy(item => item.LocationCode, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new AdjustmentOption(
                    first.LocationCode,
                    string.IsNullOrWhiteSpace(first.LocationName)
                        ? first.LocationCode
                        : $"{first.LocationCode} · {first.LocationName}");
            })
            .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase);
        foreach (var item in locationChoices) LocationChoices.Add(item);

        var lotChoices = (preview?.ScopeOptions ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.LotCode))
            .Select(item => item.LotCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .Select(item => new AdjustmentOption(item, item));
        foreach (var item in lotChoices) LotChoices.Add(item);

        RequiresLocation = preview?.ScopeRequired == true || LocationCode.Length > 0;
        RequiresLot = preview?.LotRequired == true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int InputLineNumber { get; }
    public string Sku { get; }
    public string ActualQuantity { get; }
    public string ProductName { get; }
    public string CurrentQuantity { get; }
    public string ActualQuantityDisplay { get; }
    public string Difference { get; }
    public string Result { get; }
    public string Status { get; }
    public bool RequiresLocation { get; }
    public bool RequiresLot { get; }
    public ObservableCollection<AdjustmentOption> LocationChoices { get; } = [];
    public ObservableCollection<AdjustmentOption> LotChoices { get; } = [];
    public bool HasLocationChoices => LocationChoices.Count > 0;
    public bool HasLotChoices => LotChoices.Count > 0;

    public string LocationCode
    {
        get => _locationCode;
        set
        {
            var next = value?.Trim().ToUpperInvariant() ?? string.Empty;
            if (_locationCode == next) return;
            _locationCode = next;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocationCode)));
            _changed();
        }
    }

    public string LotCode
    {
        get => _lotCode;
        set
        {
            var next = value?.Trim().ToUpperInvariant() ?? string.Empty;
            if (_lotCode == next) return;
            _lotCode = next;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LotCode)));
            _changed();
        }
    }

    public BulkInventoryAdjustmentInputRow ToRequest() =>
        new(InputLineNumber, Sku, ActualQuantity, LocationCode, LotCode);
}

public sealed class InventoryAdjustmentViewModel : INotifyPropertyChanged
{
    private const string ReadDeniedMessage = "Tài khoản chưa được cấp quyền xem Điều chỉnh tồn.";
    private const int IdempotencyIntentCacheLimit = 256;

    private readonly IInventoryAdjustmentService _service;
    private readonly IInventoryService _inventoryService;
    private readonly IInternalOrganizationService _organizationService;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private readonly List<InventoryAdjustmentData> _adjustments = [];
    private readonly List<InventoryAdjustmentReasonData> _reasons = [];
    private readonly List<InventoryBalanceData> _balances = [];
    private readonly List<WarehouseLocationData> _locations = [];

    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _activeTab = "documents";
    private string _statusFilter = string.Empty;
    private string _kindFilter = string.Empty;
    private InventoryAdjustmentListRow? _selectedListRow;
    private InventoryAdjustmentData? _selectedAdjustment;
    private string _actionReason = string.Empty;

    private string _manualKind = "MANUAL_ADJUSTMENT";
    private string _manualDirection = "OUT";
    private string _manualWarehouseId = string.Empty;
    private string _manualSourceKey = string.Empty;
    private string _manualDestinationLocationId = string.Empty;
    private string _manualQuantity = string.Empty;
    private string _manualReasonCode = string.Empty;
    private string _manualReasonNote = string.Empty;

    private string _bulkWarehouseId = string.Empty;
    private string _bulkFileName = string.Empty;
    private string _bulkIncreaseReasonCode = string.Empty;
    private string _bulkDecreaseReasonCode = string.Empty;
    private string _bulkReasonNote = string.Empty;
    private BulkInventoryAdjustmentPreviewData? _bulkPreview;
    private bool _bulkPreviewStale;

    private string _exportStatusFilter = string.Empty;
    private string _exportKindFilter = string.Empty;
    private string _exportFormat = "xlsx";
    private string _exportError = string.Empty;

    public InventoryAdjustmentViewModel(
        IInventoryAdjustmentService service,
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

        StatusOptions.Add(new AdjustmentOption(string.Empty, "Tất cả"));
        foreach (var status in new[] { "DRAFT", "SUBMITTED", "APPROVED", "POSTED", "CANCELLED", "REVERSED" })
        {
            StatusOptions.Add(new AdjustmentOption(status, InventoryAdjustmentPresentation.Status(status)));
        }

        KindOptions.Add(new AdjustmentOption(string.Empty, "Tất cả"));
        foreach (var kind in new[] { "MANUAL_ADJUSTMENT", "QUARANTINE_TRANSFER", "DAMAGED_TRANSFER", "SCRAP" })
        {
            KindOptions.Add(new AdjustmentOption(kind, InventoryAdjustmentPresentation.Kind(kind)));
        }

        ManualKindOptions.Add(new AdjustmentOption("MANUAL_ADJUSTMENT", "Điều chỉnh thủ công"));
        ManualKindOptions.Add(new AdjustmentOption("QUARANTINE_TRANSFER", "Chuyển cách ly"));
        ManualKindOptions.Add(new AdjustmentOption("DAMAGED_TRANSFER", "Chuyển hư hỏng"));
        ManualKindOptions.Add(new AdjustmentOption("SCRAP", "Tiêu hủy"));
        DirectionOptions.Add(new AdjustmentOption("IN", "Tăng tồn"));
        DirectionOptions.Add(new AdjustmentOption("OUT", "Giảm tồn"));
        ExportFormatOptions.Add(new AdjustmentOption("xlsx", "Excel (.xlsx)"));
        ExportFormatOptions.Add(new AdjustmentOption("csv", "CSV (.csv)"));
        ResetExportColumns();

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            _intentKeys.Clear();
            ResetSessionData();
            RaisePermissions();

            if (CanRead && _access.Current.IsAuthenticated)
            {
                _ = EnsureLoadedAsync();
            }
            else if (!_access.Current.IsAuthenticated)
            {
                ClearMessage();
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AdjustmentOption> StatusOptions { get; } = [];
    public ObservableCollection<AdjustmentOption> KindOptions { get; } = [];
    public ObservableCollection<AdjustmentOption> ManualKindOptions { get; } = [];
    public ObservableCollection<AdjustmentOption> DirectionOptions { get; } = [];
    public ObservableCollection<AdjustmentWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<AdjustmentSourceOption> ManualSourceOptions { get; } = [];
    public ObservableCollection<AdjustmentLocationOption> DestinationLocations { get; } = [];
    public ObservableCollection<AdjustmentReasonOption> ManualReasons { get; } = [];
    public ObservableCollection<AdjustmentReasonOption> BulkIncreaseReasons { get; } = [];
    public ObservableCollection<AdjustmentReasonOption> BulkDecreaseReasons { get; } = [];
    public ObservableCollection<InventoryAdjustmentListRow> FilteredAdjustments { get; } = [];
    public ObservableCollection<InventoryAdjustmentLineRow> DetailLines { get; } = [];
    public ObservableCollection<BulkAdjustmentRowView> BulkRows { get; } = [];
    public ObservableCollection<string> BulkErrors { get; } = [];
    public ObservableCollection<AdjustmentOption> ExportFormatOptions { get; } = [];
    public ObservableCollection<InventoryAdjustmentExportColumnOption> ExportColumns { get; } = [];

    public bool CanRead => _access.HasPermission("core.inventory-adjustment.read");
    public bool CanCreate => _access.HasPermission("core.inventory-adjustment.create");
    public bool CanSubmit => _access.HasPermission("core.inventory-adjustment.submit");
    public bool CanApprove => _access.HasPermission("core.inventory-adjustment.approve");
    public bool CanPost => _access.HasPermission("core.inventory-adjustment.post");
    public bool CanCancel => _access.HasPermission("core.inventory-adjustment.cancel");
    public bool CanReverse => _access.HasPermission("core.inventory-adjustment.reverse");
    public bool CanExportData => CanRead && IsNotBusy && ExportColumns.Any(column => column.IsSelected);

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public string RefreshText => _busyAction == "load" ? "Đang cập nhật..." : "Làm mới";

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public string ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (!SetField(ref _activeTab, value)) return;
            OnPropertyChanged(nameof(IsDocumentsTab));
            OnPropertyChanged(nameof(IsManualTab));
            OnPropertyChanged(nameof(IsBulkTab));
            OnPropertyChanged(nameof(IsExportTab));
        }
    }

    public bool IsDocumentsTab => ActiveTab == "documents";
    public bool IsManualTab => ActiveTab == "manual";
    public bool IsBulkTab => ActiveTab == "bulk";
    public bool IsExportTab => ActiveTab == "export";

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetField(ref _statusFilter, value ?? string.Empty)) ApplyListFilter();
        }
    }

    public string KindFilter
    {
        get => _kindFilter;
        set
        {
            if (SetField(ref _kindFilter, value ?? string.Empty)) ApplyListFilter();
        }
    }

    public string ExportStatusFilter
    {
        get => _exportStatusFilter;
        set => SetField(ref _exportStatusFilter, value ?? string.Empty);
    }

    public string ExportKindFilter
    {
        get => _exportKindFilter;
        set => SetField(ref _exportKindFilter, value ?? string.Empty);
    }

    public string ExportFormat
    {
        get => _exportFormat;
        set
        {
            var normalized = string.Equals(value, "csv", StringComparison.OrdinalIgnoreCase) ? "csv" : "xlsx";
            if (!SetField(ref _exportFormat, normalized)) return;
            OnPropertyChanged(nameof(ExportButtonText));
        }
    }

    public string ExportError
    {
        get => _exportError;
        private set
        {
            if (!SetField(ref _exportError, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasExportError));
        }
    }

    public bool HasExportError => !string.IsNullOrWhiteSpace(ExportError);
    public bool CanSelectAllExportColumns =>
        IsNotBusy && ExportColumns.Count > 0 && ExportColumns.Any(column => !column.IsSelected);
    public bool CanClearExportColumns =>
        IsNotBusy && ExportColumns.Any(column => column.IsSelected);
    public string ExportSelectedCountText => $"Đã chọn {ExportColumns.Count(column => column.IsSelected)}/{ExportColumns.Count} cột.";
    public string ExportButtonText => _busyAction == "export" ? "Đang tạo file…" : "Xuất dữ liệu";

    public InventoryAdjustmentListRow? SelectedListRow
    {
        get => _selectedListRow;
        set => SetField(ref _selectedListRow, value);
    }

    public InventoryAdjustmentData? SelectedAdjustment
    {
        get => _selectedAdjustment;
        private set
        {
            if (SetField(ref _selectedAdjustment, value)) RaiseDetailState();
        }
    }

    public string ActionReason
    {
        get => _actionReason;
        set => SetField(ref _actionReason, value);
    }

    public bool HasDetail => SelectedAdjustment is not null;
    public string DetailNumber => SelectedAdjustment?.AdjustmentNumber ?? "Điều chỉnh tồn";
    public string DetailStatus => InventoryAdjustmentPresentation.Status(SelectedAdjustment?.Status);
    public string DetailKind => InventoryAdjustmentPresentation.Kind(SelectedAdjustment?.DocumentKind);
    public string DetailReason => SelectedAdjustment is null
        ? string.Empty
        : $"{SelectedAdjustment.ReasonLabel ?? SelectedAdjustment.ReasonCode} · {SelectedAdjustment.ReasonNote}";
    public string DetailWarehouse => SelectedAdjustment is null
        ? string.Empty
        : InventoryAdjustmentPresentation.Warehouse(SelectedAdjustment.WarehouseCode, SelectedAdjustment.WarehouseName);
    public string DetailSource => SelectedAdjustment is null
        ? string.Empty
        : InventoryAdjustmentPresentation.Source(SelectedAdjustment);
    public string WorkflowHint => InventoryAdjustmentPresentation.WorkflowHint(SelectedAdjustment?.Status);
    public string CreatedByText => InventoryAdjustmentPresentation.ActorTime(
        SelectedAdjustment?.CreatedBy,
        SelectedAdjustment?.CreatedAt,
        "Người lập");
    public string SubmittedByText => InventoryAdjustmentPresentation.ActorTime(
        SelectedAdjustment?.SubmittedBy,
        SelectedAdjustment?.SubmittedAt,
        "Người gửi");
    public string ApprovedByText => InventoryAdjustmentPresentation.ActorTime(
        SelectedAdjustment?.ApprovedBy,
        SelectedAdjustment?.ApprovedAt,
        "Người duyệt");
    public string PostedText => SelectedAdjustment?.PostedAt is not null
        ? $"Đã hoàn tất · {InventoryAdjustmentPresentation.DateTimeText(SelectedAdjustment.PostedAt)}"
        : SelectedAdjustment?.Status == "APPROVED" ? "Chưa cập nhật" : "Chưa đến bước cập nhật tồn";
    public bool CanSubmitSelected => SelectedAdjustment?.Status == "DRAFT" && CanSubmit && IsNotBusy;
    public bool CanApproveSelected => SelectedAdjustment?.Status == "SUBMITTED" && CanApprove && IsNotBusy;
    public bool CanPostSelected => SelectedAdjustment?.Status == "APPROVED" && CanPost && IsNotBusy;
    public bool CanCancelSelected =>
        (SelectedAdjustment?.Status is "DRAFT" or "SUBMITTED" or "APPROVED") && CanCancel && IsNotBusy;
    public bool CanReverseSelected => SelectedAdjustment?.Status == "POSTED" && CanReverse && IsNotBusy;
    public bool ShowActionReason => SelectedAdjustment?.Status is "DRAFT" or "SUBMITTED" or "APPROVED" or "POSTED";

    public string ManualKind
    {
        get => _manualKind;
        set
        {
            if (!SetField(ref _manualKind, value ?? "MANUAL_ADJUSTMENT")) return;
            ManualReasonCode = string.Empty;
            ManualDestinationLocationId = string.Empty;
            RebuildManualDependencies();
        }
    }

    public string ManualDirection
    {
        get => _manualDirection;
        set
        {
            if (!SetField(ref _manualDirection, value ?? "OUT")) return;
            ManualReasonCode = string.Empty;
            RebuildReasons();
        }
    }

    public bool IsManualAdjustment => ManualKind == "MANUAL_ADJUSTMENT";
    public bool NeedsDestination => ManualKind is "QUARANTINE_TRANSFER" or "DAMAGED_TRANSFER";
    public string DestinationLabel => ManualKind == "QUARANTINE_TRANSFER" ? "Vị trí cách ly" : "Vị trí hư hỏng";

    public string ManualWarehouseId
    {
        get => _manualWarehouseId;
        set
        {
            if (!SetField(ref _manualWarehouseId, value ?? string.Empty)) return;
            ManualSourceKey = string.Empty;
            ManualDestinationLocationId = string.Empty;
            RebuildManualDependencies();
        }
    }

    public string ManualSourceKey
    {
        get => _manualSourceKey;
        set
        {
            if (SetField(ref _manualSourceKey, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(SelectedManualBalance));
                OnPropertyChanged(nameof(ManualSourceSummary));
            }
        }
    }

    public InventoryBalanceData? SelectedManualBalance =>
        ManualSourceOptions.FirstOrDefault(item => item.Id == ManualSourceKey)?.Data;

    public string ManualSourceSummary => SelectedManualBalance is null
        ? string.Empty
        : $"Nguồn: {InventoryAdjustmentPresentation.Kind(ManualKind)} · Tồn hiện tại {InventoryAdjustmentPresentation.Quantity(SelectedManualBalance.OnHandQuantity)} {SelectedManualBalance.BaseUnitCode}";

    public string ManualDestinationLocationId
    {
        get => _manualDestinationLocationId;
        set => SetField(ref _manualDestinationLocationId, value ?? string.Empty);
    }

    public string ManualQuantity
    {
        get => _manualQuantity;
        set => SetField(ref _manualQuantity, value);
    }

    public string ManualReasonCode
    {
        get => _manualReasonCode;
        set => SetField(ref _manualReasonCode, value ?? string.Empty);
    }

    public string ManualReasonNote
    {
        get => _manualReasonNote;
        set => SetField(ref _manualReasonNote, value);
    }

    public string BulkWarehouseId
    {
        get => _bulkWarehouseId;
        set
        {
            if (!SetField(ref _bulkWarehouseId, value ?? string.Empty)) return;
            InvalidateBulkPreview();
        }
    }

    public string BulkFileName
    {
        get => _bulkFileName;
        private set => SetField(ref _bulkFileName, value);
    }

    public string BulkFileSummary => BulkRows.Count == 0 ? "Chưa có dữ liệu" : $"{BulkRows.Count} dòng";
    public bool HasBulkRows => BulkRows.Count > 0;
    public bool HasBulkPreview => _bulkPreview is not null;
    public bool BulkPreviewReady => _bulkPreview?.Ready == true && !_bulkPreviewStale;
    public bool BulkPreviewStale => _bulkPreviewStale;
    public bool BulkHasChanges => (_bulkPreview?.Totals.IncreaseRowCount ?? 0) + (_bulkPreview?.Totals.DecreaseRowCount ?? 0) > 0;
    public bool ShowBulkConfirm => BulkPreviewReady && BulkHasChanges;
    public bool HasBulkErrors => BulkErrors.Count > 0;
    public bool BulkHasIncrease => (_bulkPreview?.Totals.IncreaseRowCount ?? 0) > 0;
    public bool BulkHasDecrease => (_bulkPreview?.Totals.DecreaseRowCount ?? 0) > 0;
    public string BulkReadySummary => _bulkPreview is null
        ? "Bấm Kiểm tra tệp để đối chiếu với tồn hệ thống."
        : $"{_bulkPreview.Totals.ReadyRowCount}/{_bulkPreview.Totals.InputRowCount} dòng sẵn sàng";
    public string BulkTotalsSummary => _bulkPreview is null
        ? string.Empty
        : $"Tăng tồn: {_bulkPreview.Totals.IncreaseRowCount} · Giảm tồn: {_bulkPreview.Totals.DecreaseRowCount} · Không chênh lệch: {_bulkPreview.Totals.UnchangedRowCount} · Cần xử lý: {_bulkPreview.Totals.AttentionRowCount}";
    public bool CanCheckBulk => CanRead && IsNotBusy && HasBulkRows && !string.IsNullOrWhiteSpace(BulkWarehouseId);
    public bool CanConfirmBulk =>
        CanCreate
        && IsNotBusy
        && BulkPreviewReady
        && BulkHasChanges
        && (!BulkHasIncrease || !string.IsNullOrWhiteSpace(BulkIncreaseReasonCode))
        && (!BulkHasDecrease || !string.IsNullOrWhiteSpace(BulkDecreaseReasonCode))
        && !string.IsNullOrWhiteSpace(BulkReasonNote);

    public string BulkIncreaseReasonCode
    {
        get => _bulkIncreaseReasonCode;
        set
        {
            if (SetField(ref _bulkIncreaseReasonCode, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(CanConfirmBulk));
            }
        }
    }

    public string BulkDecreaseReasonCode
    {
        get => _bulkDecreaseReasonCode;
        set
        {
            if (SetField(ref _bulkDecreaseReasonCode, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(CanConfirmBulk));
            }
        }
    }

    public string BulkReasonNote
    {
        get => _bulkReasonNote;
        set
        {
            if (SetField(ref _bulkReasonNote, value))
            {
                OnPropertyChanged(nameof(CanConfirmBulk));
            }
        }
    }

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);

        _initialLoadTask = LoadAsync(null, preserveMessage: false);
        try
        {
            return await _initialLoadTask.ConfigureAwait(true);
        }
        finally
        {
            _initialLoadTask = null;
        }
    }

    public async Task<bool> LoadAsync(string? selectId, bool preserveMessage)
    {
        var generation = _accessGeneration;
        if (!CanRead)
        {
            SetErrorMessage(ReadDeniedMessage);
            return false;
        }

        SetBusy("load");
        if (!preserveMessage) ClearMessage();

        var adjustmentsTask = _service.ListAsync();
        var reasonsTask = _service.ListReasonsAsync();
        var balancesTask = _inventoryService.ListBalancesAsync();
        var organizationTask = _organizationService.LoadAsync();

        IReadOnlyList<InventoryAdjustmentData> adjustments = [];
        IReadOnlyList<InventoryAdjustmentReasonData> reasons = [];
        IReadOnlyList<InventoryBalanceData> balances = [];
        InternalOrganizationSnapshot organization = new([], [], [], []);
        var partialFailure = false;
        var listLoaded = false;

        try { adjustments = await adjustmentsTask.ConfigureAwait(true); listLoaded = true; }
        catch { partialFailure = true; }
        try { reasons = await reasonsTask.ConfigureAwait(true); }
        catch { partialFailure = true; }
        try { balances = await balancesTask.ConfigureAwait(true); }
        catch { partialFailure = true; }
        try { organization = await organizationTask.ConfigureAwait(true); }
        catch { partialFailure = true; }

        if (generation != _accessGeneration || !CanRead)
        {
            SetBusy(null);
            return false;
        }

        _adjustments.Clear();
        _adjustments.AddRange(adjustments.OrderByDescending(item => item.CreatedAt, StringComparer.Ordinal));
        _reasons.Clear();
        _reasons.AddRange(reasons);
        _balances.Clear();
        _balances.AddRange(balances);
        _locations.Clear();
        _locations.AddRange(organization.Locations.Where(item => item.IsActive));

        Replace(
            Warehouses,
            organization.Warehouses
                .Where(item => item.IsActive)
                .Where(item => !string.Equals(item.WarehouseType, "vehicle", StringComparison.OrdinalIgnoreCase))
                .Where(item => !string.Equals(item.WarehouseType, "transit", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                .Select(item => new AdjustmentWarehouseOption(item.Id, $"{item.Code} · {item.Name}")));

        RebuildReasons();
        RebuildManualDependencies();
        ApplyListFilter();
        _loaded = listLoaded;
        SetBusy(null);

        if (partialFailure)
        {
            SetErrorMessage("Một phần dữ liệu Điều chỉnh tồn chưa tải được. Hãy bấm Làm mới trước khi thao tác.");
        }

        if (!string.IsNullOrWhiteSpace(selectId))
        {
            await SelectAdjustmentAsync(selectId).ConfigureAwait(true);
        }

        return listLoaded;
    }

    public Task<bool> RefreshAsync() =>
        LoadAsync(SelectedAdjustment?.Id, preserveMessage: false);

    public void SetTab(string tab)
    {
        if (tab is not ("documents" or "manual" or "bulk" or "export")) return;
        ActiveTab = tab;
        ExportError = string.Empty;
        ClearMessage();
    }

    public void SelectAllExportColumns()
    {
        foreach (var column in ExportColumns) column.IsSelected = true;
        RaiseExportState();
    }

    public void ClearExportColumns()
    {
        foreach (var column in ExportColumns) column.IsSelected = false;
        RaiseExportState();
    }

    public void ResetExportColumns()
    {
        foreach (var existing in ExportColumns)
            existing.PropertyChanged -= ExportColumn_OnPropertyChanged;

        ExportColumns.Clear();
        foreach (var definition in InventoryAdjustmentExportFile.Columns)
        {
            var option = new InventoryAdjustmentExportColumnOption(
                definition.Key,
                definition.Label,
                definition.DefaultSelected);
            option.PropertyChanged += ExportColumn_OnPropertyChanged;
            ExportColumns.Add(option);
        }

        RaiseExportState();
    }

    public async Task<ApiDownloadFile?> ExportAsync()
    {
        var columns = ExportColumns
            .Where(column => column.IsSelected)
            .Select(column => column.Key)
            .ToArray();

        if (!CanRead)
        {
            ExportError = ReadDeniedMessage;
            SetErrorMessage(ExportError);
            return null;
        }

        if (columns.Length == 0)
        {
            ExportError = "Vui lòng chọn ít nhất một cột để xuất.";
            SetErrorMessage(ExportError);
            return null;
        }

        if (IsBusy) return null;

        var generation = _accessGeneration;
        SetBusy("export");
        ExportError = string.Empty;
        ClearMessage();

        try
        {
            var rows = await _service.ListForExportAsync(
                ExportStatusFilter,
                ExportKindFilter).ConfigureAwait(true);

            if (generation != _accessGeneration || !CanRead) return null;

            return InventoryAdjustmentExportFile.Create(
                rows,
                columns,
                ExportFormat);
        }
        catch (Exception exception)
        {
            if (generation == _accessGeneration)
            {
                SetError(exception);
                ExportError = Message;
            }
            return null;
        }
        finally
        {
            if (generation == _accessGeneration && _busyAction == "export")
                SetBusy(null);
        }
    }

    public void NotifyExportSaved(string fileName) =>
        SetNotice($"Đã lưu file {fileName}.");

    public void NotifyExportSaveError(string message)
    {
        ExportError = string.IsNullOrWhiteSpace(message)
            ? "Không lưu được file dữ liệu điều chỉnh tồn."
            : message.Trim();
        SetErrorMessage(ExportError);
    }

    public void ResetFilters()
    {
        StatusFilter = string.Empty;
        KindFilter = string.Empty;
    }

    public async Task SelectAdjustmentAsync(string adjustmentId)
    {
        if (!CanRead || IsBusy || string.IsNullOrWhiteSpace(adjustmentId)) return;

        SetBusy("detail");
        ClearMessage();
        try
        {
            ApplyDetail(await _service.GetAsync(adjustmentId).ConfigureAwait(true));
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

    public async Task CreateManualAsync()
    {
        if (!CanCreate || IsBusy) return;

        var balance = SelectedManualBalance;
        if (balance is null
            || string.IsNullOrWhiteSpace(ManualWarehouseId)
            || string.IsNullOrWhiteSpace(ManualReasonCode)
            || string.IsNullOrWhiteSpace(ManualReasonNote)
            || string.IsNullOrWhiteSpace(ManualQuantity))
        {
            SetErrorMessage("Chọn đủ kho, sản phẩm/lô/vị trí, lý do và số lượng.");
            return;
        }

        if (!decimal.TryParse(
                ManualQuantity,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var quantity)
            || quantity <= 0)
        {
            SetErrorMessage("Số lượng điều chỉnh phải lớn hơn 0.");
            return;
        }

        if (NeedsDestination && string.IsNullOrWhiteSpace(ManualDestinationLocationId))
        {
            SetErrorMessage($"Chọn {DestinationLabel.ToLowerInvariant()} trước khi lập phiếu.");
            return;
        }

        var request = new InventoryAdjustmentCreateRequest(
            ManualWarehouseId,
            ManualKind,
            IsManualAdjustment ? ManualDirection : null,
            ManualReasonCode,
            ManualReasonNote.Trim(),
            [
                new InventoryAdjustmentCreateLineRequest(
                    balance.LocationId,
                    balance.BaseVariantId,
                    balance.LotId,
                    ManualQuantity.Trim(),
                    NeedsDestination ? ManualDestinationLocationId : null)
            ]);

        var signature =
            $"{ManualWarehouseId}:{ManualKind}:{ManualDirection}:{ManualSourceKey}:{ManualDestinationLocationId}:{ManualQuantity.Trim()}:{ManualReasonCode}:{ManualReasonNote.Trim()}";

        var success = await RunMutationAsync(
            "create",
            $"create:{signature}",
            key => _service.CreateAsync(request, key),
            "Đã lập phiếu. Kiểm tra lại phiếu rồi chọn Gửi duyệt.").ConfigureAwait(true);

        if (!success) return;

        ActiveTab = "documents";
        ResetManualDraft();
    }

    public Task SubmitAsync() =>
        RunSelectedTransitionAsync("submit", "Đã gửi phiếu chờ duyệt.");

    public Task ApproveAsync() =>
        RunSelectedTransitionAsync(
            "approve",
            "Đã duyệt phiếu. Tồn kho chưa thay đổi. Chọn Cập nhật tồn kho để hoàn tất.");

    public Task PostAsync() =>
        RunSelectedTransitionAsync("post", "Đã cập nhật tồn kho theo phiếu đã duyệt.");

    public Task CancelAsync() =>
        RunSelectedReasonTransitionAsync("cancel", "Đã hủy phiếu. Tồn kho không thay đổi.");

    public Task ReverseAsync() =>
        RunSelectedReasonTransitionAsync("reverse", "Đã hoàn tác phần cập nhật tồn kho của phiếu.");

    public void LoadBulkFile(string path)
    {
        try
        {
            var parsed = InventoryAdjustmentBulkFile.Read(path);
            Replace(
                BulkRows,
                parsed.Select(item => new BulkAdjustmentRowView(item, null, MarkBulkPreviewStale)));
            BulkFileName = Path.GetFileName(path);
            _bulkPreview = null;
            _bulkPreviewStale = false;
            BulkErrors.Clear();
            BulkIncreaseReasonCode = string.Empty;
            BulkDecreaseReasonCode = string.Empty;
            BulkReasonNote = string.Empty;
            SetNotice(BulkWarehouseId.Length > 0
                ? $"Đã đọc {parsed.Count} dòng. Bấm Kiểm tra tệp để tiếp tục. Tồn kho chưa thay đổi."
                : $"Đã đọc {parsed.Count} dòng. Chọn kho, rồi bấm Kiểm tra tệp. Tồn kho chưa thay đổi.");
            RaiseBulkState();
        }
        catch (Exception exception)
        {
            BulkRows.Clear();
            BulkFileName = string.Empty;
            _bulkPreview = null;
            _bulkPreviewStale = false;
            BulkErrors.Clear();
            SetErrorMessage(exception.Message);
            RaiseBulkState();
        }
    }

    public async Task PreviewBulkAsync()
    {
        if (!CanCheckBulk) return;

        SetBusy("bulk-preview");
        ClearMessage();
        try
        {
            var requestRows = BulkRows.Select(row => row.ToRequest()).ToArray();
            var preview = await _service.PreviewBulkAsync(
                new BulkInventoryAdjustmentPreviewRequest(BulkWarehouseId, requestRows)).ConfigureAwait(true);

            _bulkPreview = preview;
            _bulkPreviewStale = false;
            RebuildBulkRows(requestRows, preview);
            Replace(BulkErrors, preview.RowErrors.Take(20).Select(item => item.Message));

            var changed = preview.Totals.IncreaseRowCount + preview.Totals.DecreaseRowCount;
            if (preview.Ready)
            {
                SetNotice(changed > 0
                    ? $"Đã kiểm tra toàn bộ {preview.Totals.InputRowCount} dòng. Có {changed} dòng chênh lệch; tiếp tục chọn lý do để lập phiếu."
                    : "Tất cả dòng đang khớp tồn hệ thống. Không cần lập phiếu điều chỉnh.");
            }
            else
            {
                SetNotice($"Có {preview.Totals.AttentionRowCount} dòng cần xử lý. Bổ sung Lô/Vị trí rồi kiểm tra lại. Tồn kho chưa thay đổi.");
            }

            RaiseBulkState();
        }
        catch (Exception exception)
        {
            _bulkPreview = null;
            _bulkPreviewStale = false;
            SetError(exception);
            RaiseBulkState();
        }
        finally
        {
            SetBusy(null);
        }
    }

    public async Task ConfirmBulkAsync()
    {
        if (!CanConfirmBulk) return;

        var rows = BulkRows.Select(row => row.ToRequest()).ToArray();
        var request = new BulkInventoryAdjustmentConfirmRequest(
            BulkWarehouseId,
            rows,
            BulkHasIncrease ? BulkIncreaseReasonCode : null,
            BulkHasDecrease ? BulkDecreaseReasonCode : null,
            BulkReasonNote.Trim());

        var signature =
            $"{BulkWarehouseId}:{string.Join("|", rows.Select(item => $"{item.LineNumber}:{item.Sku}:{item.ActualQuantity}:{item.LocationCode}:{item.LotCode}"))}:{request.IncreaseReasonCode}:{request.DecreaseReasonCode}:{request.ReasonNote}";

        SetBusy("bulk-confirm");
        ClearMessage();
        var keyIntent = $"bulk:bulk:{signature}";
        var key = KeyFor("bulk", "bulk", signature);

        try
        {
            var result = await _service.ConfirmBulkAsync(request, key).ConfigureAwait(true);
            ForgetKey(keyIntent);
            _bulkPreview = result.Preview;
            var numbers = string.Join(", ", result.Adjustments.Select(item => item.AdjustmentNumber));

            foreach (var item in result.Adjustments) UpsertSummary(item);

            var first = result.Adjustments.FirstOrDefault();
            if (first is not null)
            {
                ActiveTab = "documents";
                ApplyDetail(first);
                SetNotice($"Đã lập {numbers}. Đang mở phiếu vừa lập để kiểm tra và Gửi duyệt.");
                ResetBulkDraft();
            }
            else
            {
                SetNotice("Không có phiếu mới cần lập vì dữ liệu không còn chênh lệch.");
            }
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
            RaiseBulkState();
        }
    }

    private async Task RunSelectedTransitionAsync(string action, string successMessage)
    {
        if (SelectedAdjustment is null || IsBusy) return;
        var id = SelectedAdjustment.Id;
        var revision = SelectedAdjustment.Revision;

        await RunMutationAsync(
            action,
            $"{action}:{id}:{revision}",
            key => action switch
            {
                "submit" => _service.SubmitAsync(id, new InventoryAdjustmentTransitionRequest(revision), key),
                "approve" => _service.ApproveAsync(id, new InventoryAdjustmentTransitionRequest(revision), key),
                "post" => _service.PostAsync(id, new InventoryAdjustmentTransitionRequest(revision), key),
                _ => throw new InvalidOperationException("Thao tác phiếu điều chỉnh không hợp lệ.")
            },
            successMessage).ConfigureAwait(true);
    }

    private async Task RunSelectedReasonTransitionAsync(string action, string successMessage)
    {
        if (SelectedAdjustment is null || IsBusy) return;

        if (string.IsNullOrWhiteSpace(ActionReason))
        {
            SetErrorMessage(action == "cancel" ? "Nhập lý do hủy phiếu." : "Nhập lý do hoàn tác phiếu.");
            return;
        }

        var id = SelectedAdjustment.Id;
        var revision = SelectedAdjustment.Revision;
        var reason = ActionReason.Trim();

        var success = await RunMutationAsync(
            action,
            $"{action}:{id}:{revision}:{reason}",
            key => action switch
            {
                "cancel" => _service.CancelAsync(
                    id,
                    new InventoryAdjustmentReasonRequest(revision, reason),
                    key),
                "reverse" => _service.ReverseAsync(
                    id,
                    new InventoryAdjustmentReasonRequest(revision, reason),
                    key),
                _ => throw new InvalidOperationException("Thao tác phiếu điều chỉnh không hợp lệ.")
            },
            successMessage).ConfigureAwait(true);

        if (success) ActionReason = string.Empty;
    }

    private async Task<bool> RunMutationAsync(
        string action,
        string intent,
        Func<string, Task<InventoryAdjustmentData>> mutation,
        string successMessage)
    {
        SetBusy(action);
        ClearMessage();
        var key = KeyFor(action, intent, intent);

        try
        {
            var result = await mutation(key).ConfigureAwait(true);
            ForgetKey($"{action}:{intent}:{intent}");
            UpsertSummary(result);
            ApplyDetail(result);
            SetNotice(successMessage);
            return true;
        }
        catch (Exception exception)
        {
            SetError(exception);
            return false;
        }
        finally
        {
            SetBusy(null);
        }
    }

    private void ApplyDetail(InventoryAdjustmentData detail)
    {
        SelectedAdjustment = detail;
        Replace(
            DetailLines,
            detail.Lines
                .OrderBy(line => line.LineNumber)
                .Select(line =>
                {
                    var balance = _balances.FirstOrDefault(item =>
                        item.WarehouseId == line.WarehouseId
                        && item.LocationId == line.SourceLocationId
                        && item.BaseVariantId == line.BaseVariantId
                        && item.LotId == line.LotId);

                    var product = balance is null
                        ? line.BaseSku
                        : string.IsNullOrWhiteSpace(balance.ProductName)
                            ? balance.BaseVariantName ?? balance.BaseSku
                            : balance.ProductName;

                    var source = string.IsNullOrWhiteSpace(line.SourceLocationCode)
                        ? "Không vị trí"
                        : string.IsNullOrWhiteSpace(line.SourceLocationName)
                            ? line.SourceLocationCode!
                            : $"{line.SourceLocationCode} · {line.SourceLocationName}";
                    var destination = string.IsNullOrWhiteSpace(line.DestinationLocationCode)
                        ? "—"
                        : string.IsNullOrWhiteSpace(line.DestinationLocationName)
                            ? line.DestinationLocationCode!
                            : $"{line.DestinationLocationCode} · {line.DestinationLocationName}";

                    var delta = SourceDelta(detail, line);
                    var effect = detail.Status is "DRAFT" or "SUBMITTED" or "APPROVED"
                        ? PreviewEffect(balance, delta)
                        : $"{InventoryAdjustmentPresentation.SignedQuantity(delta)} · {(detail.Status == "POSTED" ? "Tồn kho đã cập nhật" : detail.Status == "REVERSED" ? "Đã hoàn tác cập nhật tồn" : "Không làm thay đổi tồn kho")}";

                    return new InventoryAdjustmentLineRow(
                        line.LineNumber,
                        product,
                        line.SourceSku,
                        string.IsNullOrWhiteSpace(line.LotCode) ? "Không lô" : line.LotCode!,
                        source,
                        destination,
                        $"{InventoryAdjustmentPresentation.Quantity(line.Quantity)} {line.SourceUnitCode}",
                        effect);
                }));

        SelectedListRow = FilteredAdjustments.FirstOrDefault(item => item.Data.Id == detail.Id);
        RaiseDetailState();
    }

    private static string SourceDelta(InventoryAdjustmentData adjustment, InventoryAdjustmentLineData line)
    {
        if (adjustment.DocumentKind == "MANUAL_ADJUSTMENT" && adjustment.AdjustmentDirection == "IN")
        {
            return line.BaseQuantity;
        }

        return decimal.TryParse(line.BaseQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? (-value).ToString(CultureInfo.InvariantCulture)
            : $"-{line.BaseQuantity}";
    }

    private static string PreviewEffect(InventoryBalanceData? balance, string delta)
    {
        if (balance is null
            || !decimal.TryParse(balance.OnHandQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var onHand)
            || !decimal.TryParse(delta, NumberStyles.Number, CultureInfo.InvariantCulture, out var deltaValue))
        {
            return $"Điều chỉnh {InventoryAdjustmentPresentation.SignedQuantity(delta)} · Tồn sau điều chỉnh chưa tải được";
        }

        return $"Tồn hiện tại {InventoryAdjustmentPresentation.Quantity(balance.OnHandQuantity)} → Điều chỉnh {InventoryAdjustmentPresentation.SignedQuantity(delta)} → Tồn sau điều chỉnh {InventoryAdjustmentPresentation.Quantity((onHand + deltaValue).ToString(CultureInfo.InvariantCulture))}";
    }

    private void UpsertSummary(InventoryAdjustmentData item)
    {
        var index = _adjustments.FindIndex(current => current.Id == item.Id);
        if (index >= 0) _adjustments[index] = item;
        else _adjustments.Insert(0, item);

        ApplyListFilter();
    }

    private void ApplyListFilter()
    {
        Replace(
            FilteredAdjustments,
            _adjustments
                .Where(item => string.IsNullOrWhiteSpace(StatusFilter) || item.Status == StatusFilter)
                .Where(item => string.IsNullOrWhiteSpace(KindFilter) || item.DocumentKind == KindFilter)
                .Select(item => new InventoryAdjustmentListRow(item)));
    }

    private void RebuildManualDependencies()
    {
        Replace(
            ManualSourceOptions,
            _balances
                .Where(item => !string.IsNullOrWhiteSpace(item.LocationId))
                .Where(item => string.IsNullOrWhiteSpace(ManualWarehouseId) || item.WarehouseId == ManualWarehouseId)
                .OrderBy(item => item.ProductName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.BaseSku, StringComparer.OrdinalIgnoreCase)
                .Select(item => new AdjustmentSourceOption(
                    InventoryAdjustmentPresentation.BalanceKey(item),
                    $"{DisplayProduct(item)} · {item.BaseSku} · Lô {(string.IsNullOrWhiteSpace(item.LotCode) ? "Không lô" : item.LotCode)} · Vị trí {(string.IsNullOrWhiteSpace(item.LocationCode) ? "Không vị trí" : item.LocationCode)} · Tồn {InventoryAdjustmentPresentation.Quantity(item.OnHandQuantity)} · Có thể xử lý {InventoryAdjustmentPresentation.Quantity(item.AvailableQuantity)}",
                    item)));

        var locationType = ManualKind == "QUARANTINE_TRANSFER"
            ? "quarantine"
            : ManualKind == "DAMAGED_TRANSFER"
                ? "damaged"
                : string.Empty;

        Replace(
            DestinationLocations,
            _locations
                .Where(item => item.WarehouseId == ManualWarehouseId)
                .Where(item => string.IsNullOrWhiteSpace(locationType) || string.Equals(item.LocationType, locationType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                .Select(item => new AdjustmentLocationOption(item.Id, $"{item.Code} · {item.Name}")));

        RebuildReasons();
        OnPropertyChanged(nameof(IsManualAdjustment));
        OnPropertyChanged(nameof(NeedsDestination));
        OnPropertyChanged(nameof(DestinationLabel));
        OnPropertyChanged(nameof(SelectedManualBalance));
        OnPropertyChanged(nameof(ManualSourceSummary));
    }

    private void RebuildReasons()
    {
        Replace(
            ManualReasons,
            _reasons
                .Where(item => item.DocumentKind == ManualKind)
                .Where(item => ManualKind != "MANUAL_ADJUSTMENT" || item.AdjustmentDirection == ManualDirection)
                .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
                .Select(item => new AdjustmentReasonOption(item.Code, item.Label)));

        Replace(
            BulkIncreaseReasons,
            _reasons
                .Where(item => item.DocumentKind == "MANUAL_ADJUSTMENT" && item.AdjustmentDirection == "IN")
                .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
                .Select(item => new AdjustmentReasonOption(item.Code, item.Label)));

        Replace(
            BulkDecreaseReasons,
            _reasons
                .Where(item => item.DocumentKind == "MANUAL_ADJUSTMENT" && item.AdjustmentDirection == "OUT")
                .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
                .Select(item => new AdjustmentReasonOption(item.Code, item.Label)));
    }

    private void RebuildBulkRows(
        IReadOnlyList<BulkInventoryAdjustmentInputRow> inputs,
        BulkInventoryAdjustmentPreviewData preview)
    {
        var byLine = preview.Rows.ToDictionary(item => item.LineNumber);
        Replace(
            BulkRows,
            inputs.Select(input => new BulkAdjustmentRowView(
                input,
                byLine.GetValueOrDefault(input.LineNumber),
                MarkBulkPreviewStale)));
    }

    private void MarkBulkPreviewStale()
    {
        if (_bulkPreview is null) return;
        _bulkPreviewStale = true;
        SetNotice("Đã cập nhật Lô/Vị trí. Bấm Kiểm tra tệp để đối chiếu lại trước khi lập phiếu.");
        RaiseBulkState();
    }

    private void InvalidateBulkPreview()
    {
        _bulkPreview = null;
        _bulkPreviewStale = false;
        BulkErrors.Clear();
        RaiseBulkState();
    }

    private void ResetManualDraft()
    {
        ManualKind = "MANUAL_ADJUSTMENT";
        ManualDirection = "OUT";
        ManualWarehouseId = string.Empty;
        ManualSourceKey = string.Empty;
        ManualDestinationLocationId = string.Empty;
        ManualQuantity = string.Empty;
        ManualReasonCode = string.Empty;
        ManualReasonNote = string.Empty;
    }

    private void ResetBulkDraft()
    {
        BulkRows.Clear();
        BulkErrors.Clear();
        BulkFileName = string.Empty;
        BulkWarehouseId = string.Empty;
        BulkIncreaseReasonCode = string.Empty;
        BulkDecreaseReasonCode = string.Empty;
        BulkReasonNote = string.Empty;
        _bulkPreview = null;
        _bulkPreviewStale = false;
        RaiseBulkState();
    }

    private void ResetSessionData()
    {
        _adjustments.Clear();
        _reasons.Clear();
        _balances.Clear();
        _locations.Clear();
        FilteredAdjustments.Clear();
        Warehouses.Clear();
        ManualSourceOptions.Clear();
        DestinationLocations.Clear();
        ManualReasons.Clear();
        BulkIncreaseReasons.Clear();
        BulkDecreaseReasons.Clear();
        DetailLines.Clear();
        BulkRows.Clear();
        BulkErrors.Clear();
        ExportStatusFilter = string.Empty;
        ExportKindFilter = string.Empty;
        ExportFormat = "xlsx";
        ExportError = string.Empty;
        ResetExportColumns();
        SelectedAdjustment = null;
        SelectedListRow = null;
        ActionReason = string.Empty;
        ResetManualDraft();
        ResetBulkDraft();
    }

    private void RaiseDetailState()
    {
        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(DetailNumber));
        OnPropertyChanged(nameof(DetailStatus));
        OnPropertyChanged(nameof(DetailKind));
        OnPropertyChanged(nameof(DetailReason));
        OnPropertyChanged(nameof(DetailWarehouse));
        OnPropertyChanged(nameof(DetailSource));
        OnPropertyChanged(nameof(WorkflowHint));
        OnPropertyChanged(nameof(CreatedByText));
        OnPropertyChanged(nameof(SubmittedByText));
        OnPropertyChanged(nameof(ApprovedByText));
        OnPropertyChanged(nameof(PostedText));
        OnPropertyChanged(nameof(CanSubmitSelected));
        OnPropertyChanged(nameof(CanApproveSelected));
        OnPropertyChanged(nameof(CanPostSelected));
        OnPropertyChanged(nameof(CanCancelSelected));
        OnPropertyChanged(nameof(CanReverseSelected));
        OnPropertyChanged(nameof(ShowActionReason));
    }

    private void RaiseBulkState()
    {
        OnPropertyChanged(nameof(BulkFileSummary));
        OnPropertyChanged(nameof(HasBulkRows));
        OnPropertyChanged(nameof(HasBulkPreview));
        OnPropertyChanged(nameof(BulkPreviewReady));
        OnPropertyChanged(nameof(BulkPreviewStale));
        OnPropertyChanged(nameof(BulkHasChanges));
        OnPropertyChanged(nameof(ShowBulkConfirm));
        OnPropertyChanged(nameof(HasBulkErrors));
        OnPropertyChanged(nameof(BulkHasIncrease));
        OnPropertyChanged(nameof(BulkHasDecrease));
        OnPropertyChanged(nameof(BulkReadySummary));
        OnPropertyChanged(nameof(BulkTotalsSummary));
        OnPropertyChanged(nameof(CanCheckBulk));
        OnPropertyChanged(nameof(CanConfirmBulk));
    }

    private void ExportColumn_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InventoryAdjustmentExportColumnOption.IsSelected))
            RaiseExportState();
    }

    private void RaiseExportState()
    {
        OnPropertyChanged(nameof(CanExportData));
        OnPropertyChanged(nameof(CanSelectAllExportColumns));
        OnPropertyChanged(nameof(CanClearExportColumns));
        OnPropertyChanged(nameof(ExportSelectedCountText));
        OnPropertyChanged(nameof(ExportButtonText));
        OnPropertyChanged(nameof(HasExportError));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanPost));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CanReverse));
        RaiseDetailState();
        RaiseBulkState();
        RaiseExportState();
    }

    private void SetBusy(string? action)
    {
        if (_busyAction == action) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(RefreshText));
        RaiseDetailState();
        RaiseBulkState();
        RaiseExportState();
    }

    private string KeyFor(string prefix, string id, string fingerprint)
    {
        var intent = $"{prefix}:{id}:{fingerprint}";
        if (_intentKeys.TryGetValue(intent, out var existing)) return existing;

        if (_intentKeys.Count >= IdempotencyIntentCacheLimit)
        {
            var oldest = _intentKeys.Keys.FirstOrDefault();
            if (oldest is not null) _intentKeys.Remove(oldest);
        }

        var key = _idempotencyKeys.Create($"inventory-adjustment-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private void ForgetKey(string intent)
    {
        _intentKeys.Remove(intent);
    }

    private static string DisplayProduct(InventoryBalanceData item) =>
        string.IsNullOrWhiteSpace(item.ProductName)
            ? item.BaseVariantName ?? item.BaseSku
            : item.ProductName;

    private void ClearMessage()
    {
        MessageIsError = false;
        Message = string.Empty;
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

    private void SetError(Exception exception)
    {
        if (exception is CanonicalApiException apiException)
        {
            var office = apiException.Code switch
            {
                "INVENTORY_ADJUSTMENT_SELF_APPROVAL_DENIED" => "Bạn không thể tự duyệt phiếu mình đã gửi.",
                "INVENTORY_ADJUSTMENT_REVERSAL_DOWNSTREAM_CONFLICT" => "Tồn kho đã phát sinh giao dịch sau phiếu này. Hãy lập phiếu điều chỉnh mới thay vì hoàn tác.",
                "INVENTORY_ADJUSTMENT_SCOPE_CHANGED" or "STOCK_SCOPE_CHANGED" => "Tồn kho đã thay đổi. Hãy kiểm tra lại dữ liệu trước khi tiếp tục.",
                _ => CanonicalErrorMessages.ToOfficeMessage(apiException)
            };
            SetErrorMessage(CanonicalErrorMessages.WithRequestId(office, apiException.RequestId));
            return;
        }

        SetErrorMessage(exception.Message);
    }

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

    private bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
