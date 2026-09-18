using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record StocktakeStatusOption(string Id, string Label);
public sealed record StocktakeScopeModeOption(string Id, string Label);
public sealed record StocktakeWarehouseOption(string Id, string Label);

public sealed record StocktakeListRow(InventoryStocktakeData Data)
{
    public string Number => Data.StocktakeNumber;
    public string Warehouse => $"{Data.WarehouseCode} · {Data.WarehouseName}";
    public string Status => StocktakePresentation.Status(Data.Status);
    public string ScopeText => $"Lần đếm {Data.CurrentRound} · {Data.LineCount} dòng";
    public string Updated => $"Tạo: {StocktakePresentation.DateTimeText(Data.CreatedAt)}";
    public string Counted =>
        string.IsNullOrWhiteSpace(Data.CurrentCountedAt) && string.IsNullOrWhiteSpace(Data.CurrentCountedBy)
            ? string.Empty
            : $"Kiểm: {(string.IsNullOrWhiteSpace(Data.CurrentCountedBy) ? "Người thực hiện" : Data.CurrentCountedBy)} · {StocktakePresentation.DateTimeText(Data.CurrentCountedAt)}";
}

public sealed class StocktakeScopeGroupRow : INotifyPropertyChanged
{
    private bool _isSelected;

    public StocktakeScopeGroupRow(
        string key,
        string label,
        string detail,
        string? baseVariantId,
        string? lotId,
        string? locationId)
    {
        Key = key;
        Label = label;
        Detail = detail;
        BaseVariantId = baseVariantId;
        LotId = lotId;
        LocationId = locationId;
    }

    public string Key { get; }
    public string Label { get; }
    public string Detail { get; }
    public string? BaseVariantId { get; }
    public string? LotId { get; }
    public string? LocationId { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class StocktakeLineRow : INotifyPropertyChanged
{
    private string _countedQuantity;
    private string _reason;
    private string _note;

    public StocktakeLineRow(InventoryStocktakeLineData data, string productName, bool canEditAnnotations)
    {
        Data = data;
        Product = string.IsNullOrWhiteSpace(productName) ? data.BaseSku : productName;
        _countedQuantity = data.CountedBaseQuantity ?? string.Empty;
        _reason = data.Reason ?? string.Empty;
        _note = data.Note ?? string.Empty;
        CanEditAnnotations = canEditAnnotations;
    }

    public InventoryStocktakeLineData Data { get; }
    public int LineNumber => Data.LineNumber;
    public string Product { get; }
    public string Sku => Data.BaseSku;
    public string Lot => string.IsNullOrWhiteSpace(Data.LotCode) ? "Không lô" : Data.LotCode;
    public string Location => string.IsNullOrWhiteSpace(Data.LocationCode) ? "Không vị trí" : Data.LocationCode;
    public string Unit => Data.SourceUnitCode;
    public string Expected => Data.ExpectedBaseQuantity is null ? "—" : StocktakePresentation.Quantity(Data.ExpectedBaseQuantity);
    public string Difference => StocktakePresentation.SignedDifference(CountedQuantity, Data.ExpectedBaseQuantity, Data.FinalDelta);
    public string CountStatus => StocktakePresentation.CountStatus(Data.CountStatus);
    public string CountedDisplay => string.IsNullOrWhiteSpace(CountedQuantity) ? "—" : StocktakePresentation.Quantity(CountedQuantity);
    public bool CanEditAnnotations { get; }

    public string CountedQuantity
    {
        get => _countedQuantity;
        set
        {
            if (_countedQuantity == value) return;
            _countedQuantity = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CountedQuantity)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CountedDisplay)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Difference)));
        }
    }

    public string Reason
    {
        get => _reason;
        set
        {
            if (_reason == value) return;
            _reason = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Reason)));
        }
    }

    public string Note
    {
        get => _note;
        set
        {
            if (_note == value) return;
            _note = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Note)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed record StocktakeRoundRow(InventoryStocktakeRoundData Data)
{
    public string Title => $"Lần đếm {Data.RoundNumber} · {StocktakePresentation.Status(Data.Status)}";
    public string ActorAndTime =>
        $"{(string.IsNullOrWhiteSpace(Data.CreatedBy) ? "Người thực hiện" : Data.CreatedBy)} · {StocktakePresentation.DateTimeText(Data.CreatedAt)}";
    public string Reason => string.IsNullOrWhiteSpace(Data.Reason) ? string.Empty : $"Lý do: {Data.Reason}";
}

public sealed class StocktakeViewModel : INotifyPropertyChanged
{
    private const string ReadDeniedMessage = "Tài khoản chưa được cấp quyền xem Kiểm kê kho.";
    private const int IdempotencyIntentCacheLimit = 256;
    private const int ScopePickerResultLimit = 60;
    public const int LinePageSize = 100;

    private readonly IInventoryStocktakeService _service;
    private readonly IInventoryService _inventoryService;
    private readonly IInternalOrganizationService _organizationService;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private readonly List<InventoryStocktakeData> _stocktakes = [];
    private readonly List<InventoryBalanceData> _balances = [];
    private readonly List<StocktakeScopeGroupRow> _scopeGroupsAll = [];
    private readonly Dictionary<string, string> _productNames = new(StringComparer.Ordinal);

    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _search = string.Empty;
    private string _statusFilter = "all";
    private bool _isCreateOpen;
    private string _selectedWarehouseId = string.Empty;
    private string _scopeMode = "all";
    private string _scopeSearch = string.Empty;
    private string _lineFilter = "all";
    private int _linePage = 1;
    private string _note = string.Empty;
    private string _reason = string.Empty;
    private StocktakeListRow? _selectedListRow;
    private InventoryStocktakeData? _selectedStocktake;

    public StocktakeViewModel(
        IInventoryStocktakeService service,
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

        StatusOptions.Add(new StocktakeStatusOption("all", "Tất cả"));
        StatusOptions.Add(new StocktakeStatusOption("submitted", "Kiểm kê cần duyệt"));
        StatusOptions.Add(new StocktakeStatusOption("draft", "Đang đếm"));
        StatusOptions.Add(new StocktakeStatusOption("counted", "Chờ gửi duyệt"));
        StatusOptions.Add(new StocktakeStatusOption("recount_required", "Yêu cầu đếm lại"));
        StatusOptions.Add(new StocktakeStatusOption("approved", "Chờ cập nhật tồn"));
        StatusOptions.Add(new StocktakeStatusOption("posted", "Hoàn tất"));
        StatusOptions.Add(new StocktakeStatusOption("cancelled", "Đã hủy"));
        StatusOptions.Add(new StocktakeStatusOption("reversed", "Đã hoàn tác"));

        ScopeModeOptions.Add(new StocktakeScopeModeOption("all", "Toàn bộ sản phẩm trong kho"));
        ScopeModeOptions.Add(new StocktakeScopeModeOption("lot", "Theo lô"));
        ScopeModeOptions.Add(new StocktakeScopeModeOption("location", "Theo vị trí"));

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

    public ObservableCollection<StocktakeStatusOption> StatusOptions { get; } = [];
    public ObservableCollection<StocktakeScopeModeOption> ScopeModeOptions { get; } = [];
    public ObservableCollection<StocktakeWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<StocktakeListRow> FilteredStocktakes { get; } = [];
    public ObservableCollection<StocktakeScopeGroupRow> ScopeGroups { get; } = [];
    public ObservableCollection<StocktakeScopeGroupRow> SelectedScopeGroups { get; } = [];
    public ObservableCollection<StocktakeLineRow> Lines { get; } = [];
    public ObservableCollection<StocktakeLineRow> PagedLines { get; } = [];
    public ObservableCollection<StocktakeRoundRow> Rounds { get; } = [];

    public bool CanRead => _access.HasPermission("core.stocktake.read");
    public bool CanCreate => _access.HasPermission("core.stocktake.create");
    public bool CanCount => _access.HasPermission("core.stocktake.count");
    public bool CanSubmit => _access.HasPermission("core.stocktake.submit");
    public bool CanApprove => _access.HasPermission("core.stocktake.approve");
    public bool CanPost => _access.HasPermission("core.stocktake.post");
    public bool CanCancel => _access.HasPermission("core.stocktake.cancel");
    public bool CanReverse => _access.HasPermission("core.stocktake.reverse");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoading => _busyAction == "load";
    public string RefreshText => IsLoading ? "Đang cập nhật..." : "Làm mới";

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

    public string Search
    {
        get => _search;
        set
        {
            if (SetField(ref _search, value))
            {
                ApplyFilter();
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetField(ref _statusFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool IsCreateOpen
    {
        get => _isCreateOpen;
        private set => SetField(ref _isCreateOpen, value);
    }

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            if (SetField(ref _selectedWarehouseId, value ?? string.Empty))
            {
                RebuildScopeGroups();
            }
        }
    }

    public string ScopeMode
    {
        get => _scopeMode;
        set
        {
            var normalized = value is "lot" or "location" ? value : "all";
            if (SetField(ref _scopeMode, normalized))
            {
                OnPropertyChanged(nameof(IsAllScopeMode));
                OnPropertyChanged(nameof(IsLotScopeMode));
                OnPropertyChanged(nameof(IsLocationScopeMode));
                ScopeSearch = string.Empty;
                RebuildScopeGroups();
            }
        }
    }

    public bool IsAllScopeMode
    {
        get => ScopeMode == "all";
        set { if (value) ScopeMode = "all"; }
    }

    public bool IsLotScopeMode
    {
        get => ScopeMode == "lot";
        set { if (value) ScopeMode = "lot"; }
    }

    public bool IsLocationScopeMode
    {
        get => ScopeMode == "location";
        set { if (value) ScopeMode = "location"; }
    }

    public string ScopeSearch
    {
        get => _scopeSearch;
        set
        {
            if (SetField(ref _scopeSearch, value ?? string.Empty))
            {
                ApplyScopeGroupFilter();
            }
        }
    }

    public string LineFilter
    {
        get => _lineFilter;
        private set => SetField(ref _lineFilter, value);
    }

    public string Note
    {
        get => _note;
        set => SetField(ref _note, value);
    }

    public string Reason
    {
        get => _reason;
        set => SetField(ref _reason, value);
    }

    public StocktakeListRow? SelectedListRow
    {
        get => _selectedListRow;
        set => SetField(ref _selectedListRow, value);
    }

    public InventoryStocktakeData? SelectedStocktake
    {
        get => _selectedStocktake;
        private set
        {
            if (SetField(ref _selectedStocktake, value))
            {
                RaiseDetailState();
            }
        }
    }

    public bool HasDetail => SelectedStocktake is not null;
    public string DetailNumber => SelectedStocktake?.StocktakeNumber ?? "Kiểm kê kho";
    public string DetailWarehouse =>
        SelectedStocktake is null
            ? string.Empty
            : $"{SelectedStocktake.WarehouseCode} · {SelectedStocktake.WarehouseName} · Lần đếm {SelectedStocktake.CurrentRound}";
    public string DetailNote => string.IsNullOrWhiteSpace(SelectedStocktake?.Note) ? "Không có ghi chú" : SelectedStocktake.Note!;
    public string DetailStatus => StocktakePresentation.Status(SelectedStocktake?.Status);
    public string WorkflowHint => StocktakePresentation.WorkflowHint(SelectedStocktake?.Status);
    public bool IsBlindCount => SelectedStocktake?.Status is "draft" or "recount_required";
    public bool IsReviewTable => HasDetail && !IsBlindCount;
    public bool CanPrint => HasDetail && SelectedStocktake?.Status != "draft" && IsNotBusy;
    public bool CanCountSelected => IsBlindCount && CanCount && IsNotBusy;
    public bool CanAnnotateSelected => (SelectedStocktake?.Status is "submitted" or "approved") && CanCount && IsNotBusy;
    public bool CanCopySelected => HasDetail && CanCreate && IsNotBusy;
    public bool CanExportCountFile => HasDetail && Lines.Count > 0 && IsNotBusy;
    public bool CanImportCountFile => IsBlindCount && CanCount && IsNotBusy;
    public bool CanExportResults => IsReviewTable && Lines.Count > 0 && IsNotBusy;
    public bool CanSubmitSelected => SelectedStocktake?.Status == "counted" && CanSubmit && IsNotBusy;
    public bool CanRecountSelected =>
        (SelectedStocktake?.Status is "counted" or "submitted" or "approved") && CanApprove && IsNotBusy;
    public bool CanApproveSelected => SelectedStocktake?.Status == "submitted" && CanApprove && IsNotBusy;
    public bool CanPostSelected => SelectedStocktake?.Status == "approved" && CanPost && IsNotBusy;
    public bool CanCancelSelected =>
        (SelectedStocktake?.Status is "draft" or "counted" or "recount_required") && CanCancel && IsNotBusy;
    public bool CanReverseSelected => SelectedStocktake?.Status == "posted" && CanReverse && IsNotBusy;
    public bool ShowReasonField =>
        SelectedStocktake?.Status is "draft" or "counted" or "submitted" or "approved" or "posted" or "recount_required";

    public string SubmittedText => ActorTime(SelectedStocktake?.SubmittedBy, SelectedStocktake?.SubmittedAt, "Người gửi");
    public string ApprovedText => ActorTime(SelectedStocktake?.ApprovedBy, SelectedStocktake?.ApprovedAt, "Người duyệt");
    public string PostedText =>
        SelectedStocktake?.PostedAt is not null
            ? $"Đã hoàn tất · {StocktakePresentation.DateTimeText(SelectedStocktake.PostedAt)}"
            : SelectedStocktake?.Status == "approved" ? "Chưa cập nhật" : "Chưa đến bước cập nhật tồn";
    public string UpdatedText => StocktakePresentation.DateTimeText(SelectedStocktake?.UpdatedAt);

    public int SelectedScopeCount => _scopeGroupsAll.Count(group => group.IsSelected);
    public string ScopeSelectionText =>
        string.IsNullOrWhiteSpace(SelectedWarehouseId)
            ? "Chọn kho để xác định phạm vi kiểm kê."
            : ScopeMode == "all"
                ? "Sẽ kiểm toàn bộ sản phẩm có phạm vi tồn hợp lệ trong kho; hệ thống chốt tồn tại lúc tạo phiếu."
                : $"Đã chọn {SelectedScopeCount} {(ScopeMode == "lot" ? "lô" : "vị trí")}.";
    public string ScopeResultsText =>
        ScopeMode == "all"
            ? string.Empty
            : $"Hiển thị tối đa {ScopePickerResultLimit} kết quả · {ScopeGroups.Count} đang hiển thị";
    public bool ShowScopePicker => ScopeMode != "all";
    public bool HasScopeGroups => ShowScopePicker && ScopeGroups.Count > 0;
    public bool HasNoScopeGroups => ShowScopePicker && ScopeGroups.Count == 0;

    public int AllLineCount => Lines.Count;
    public int UncountedLineCount => Lines.Count(IsLineUncounted);
    public int MatchedLineCount => Lines.Count(line => line.Data.CountStatus == "matched");
    public int MismatchLineCount => Lines.Count(line => line.Data.CountStatus == "mismatch");
    public string AllLineFilterText => $"Tất cả ({AllLineCount})";
    public string UncountedLineFilterText => $"Chưa kiểm ({UncountedLineCount})";
    public string MatchedLineFilterText => $"Khớp ({MatchedLineCount})";
    public string MismatchLineFilterText => $"Lệch ({MismatchLineCount})";
    public bool CanUseRevealFilters => IsReviewTable;
    public int TotalLinePages => Math.Max(1, (FilteredLineCount() + LinePageSize - 1) / LinePageSize);
    public string LinePageText => $"{Math.Min(_linePage, TotalLinePages)}/{TotalLinePages}";
    public bool CanPreviousLinePage => _linePage > 1;
    public bool CanNextLinePage => _linePage < TotalLinePages;

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

        var stocktakesTask = _service.ListAsync();
        var balancesTask = _inventoryService.ListBalancesAsync();
        var warehousesTask = _organizationService.ListWarehousesAsync();

        IReadOnlyList<InventoryStocktakeData> stocktakes = [];
        IReadOnlyList<InventoryBalanceData> balances = [];
        IReadOnlyList<WarehouseData> warehouses = [];
        var partialFailure = false;
        var stocktakesLoaded = false;

        try
        {
            stocktakes = await stocktakesTask.ConfigureAwait(true);
            stocktakesLoaded = true;
        }
        catch
        {
            partialFailure = true;
        }

        try { balances = await balancesTask.ConfigureAwait(true); }
        catch { partialFailure = true; }

        try { warehouses = await warehousesTask.ConfigureAwait(true); }
        catch { partialFailure = true; }

        if (generation != _accessGeneration || !CanRead)
        {
            SetBusy(null);
            return false;
        }

        _stocktakes.Clear();
        _stocktakes.AddRange(stocktakes.OrderByDescending(item => item.UpdatedAt, StringComparer.Ordinal));

        _balances.Clear();
        _balances.AddRange(balances);

        _productNames.Clear();
        foreach (var balance in balances)
        {
            _productNames[balance.BaseVariantId] =
                string.IsNullOrWhiteSpace(balance.ProductName)
                    ? balance.BaseVariantName ?? balance.BaseSku
                    : balance.ProductName;
        }

        Replace(
            Warehouses,
            warehouses
                .Where(item => item.IsActive)
                .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                .Select(item => new StocktakeWarehouseOption(item.Id, $"{item.Code} · {item.Name}")));

        ApplyFilter();
        RebuildScopeGroups();
        _loaded = stocktakesLoaded;
        SetBusy(null);

        if (partialFailure)
        {
            SetErrorMessage("Một phần dữ liệu kiểm kê chưa tải được. Hãy cập nhật trước khi thao tác.");
        }

        if (!string.IsNullOrWhiteSpace(selectId))
        {
            await SelectStocktakeAsync(selectId).ConfigureAwait(true);
        }

        return stocktakesLoaded;
    }

    public Task<bool> RefreshAsync() =>
        LoadAsync(SelectedStocktake?.Id, preserveMessage: false);

    public void OpenCreate()
    {
        if (!CanCreate)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền tạo đợt kiểm kê.");
            return;
        }

        IsCreateOpen = true;
        ClearMessage();
        ScopeMode = "all";
        ScopeSearch = string.Empty;

        if (string.IsNullOrWhiteSpace(SelectedWarehouseId) && Warehouses.Count > 0)
        {
            SelectedWarehouseId = Warehouses[0].Id;
        }
    }

    public void CloseCreate()
    {
        IsCreateOpen = false;
        Note = string.Empty;
        ScopeSearch = string.Empty;
        foreach (var group in _scopeGroupsAll) group.IsSelected = false;
        RaiseScopeSelection();
    }

    public void ResetFilters()
    {
        Search = string.Empty;
        StatusFilter = "all";
    }

    public async Task SelectStocktakeAsync(string stocktakeId)
    {
        if (!CanRead || string.IsNullOrWhiteSpace(stocktakeId) || IsBusy) return;

        SetBusy("detail");
        ClearMessage();
        try
        {
            ApplyDetail(await _service.GetAsync(stocktakeId).ConfigureAwait(true));
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

    public async Task CreateAsync()
    {
        if (!CanCreate || IsBusy) return;

        if (string.IsNullOrWhiteSpace(SelectedWarehouseId))
        {
            SetErrorMessage("Chọn kho cần kiểm kê.");
            return;
        }

        var selectedGroups = _scopeGroupsAll.Where(group => group.IsSelected).ToArray();
        if (ScopeMode != "all" && selectedGroups.Length == 0)
        {
            SetErrorMessage(ScopeMode == "lot" ? "Chọn ít nhất một lô cần kiểm kê." : "Chọn ít nhất một vị trí cần kiểm kê.");
            return;
        }

        var lotSelections = ScopeMode == "lot"
            ? selectedGroups
                .Where(group => !string.IsNullOrWhiteSpace(group.BaseVariantId))
                .Select(group => new InventoryStocktakeLotSelectionRequest(group.BaseVariantId!, group.LotId))
                .ToArray()
            : null;
        var locationIds = ScopeMode == "location"
            ? selectedGroups.Select(group => group.LocationId).Distinct(StringComparer.Ordinal).ToArray()
            : null;
        var selectionFingerprint = ScopeMode == "all"
            ? "all"
            : string.Join("|", selectedGroups.Select(group => group.Key).OrderBy(value => value, StringComparer.Ordinal));
        var fingerprint = $"{SelectedWarehouseId}:{ScopeMode}:{selectionFingerprint}:{Note.Trim()}";

        await RunMutationAsync(
            "create",
            () => _service.CreateAsync(
                new InventoryStocktakeCreateRequest(
                    SelectedWarehouseId,
                    NormalizeOptional(Note),
                    ScopeMode,
                    lotSelections,
                    locationIds),
                KeyFor("create", SelectedWarehouseId, fingerprint)),
            "Đã tạo đợt kiểm kê. Số hệ thống được ẩn trong lúc đếm.").ConfigureAwait(true);

        if (!MessageIsError)
        {
            IsCreateOpen = false;
            Note = string.Empty;
            ScopeSearch = string.Empty;
        }
    }

    public async Task CompleteCountAsync()
    {
        if (!CanCountSelected || SelectedStocktake is null) return;

        if (Lines.Any(line => string.IsNullOrWhiteSpace(line.CountedQuantity)))
        {
            SetErrorMessage("Phải nhập số thực đếm cho toàn bộ phạm vi hiện tại.");
            return;
        }

        foreach (var line in Lines)
        {
            if (!decimal.TryParse(
                    line.CountedQuantity,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var parsed)
                || parsed < 0)
            {
                SetErrorMessage($"Số thực đếm dòng {line.LineNumber} không hợp lệ.");
                return;
            }
            if (line.Reason.Trim().Length > 500)
            {
                SetErrorMessage($"Lý do dòng {line.LineNumber} vượt quá 500 ký tự.");
                return;
            }
            if (line.Note.Trim().Length > 2000)
            {
                SetErrorMessage($"Ghi chú dòng {line.LineNumber} vượt quá 2000 ký tự.");
                return;
            }
        }

        var counts = Lines
            .Select(line => new InventoryStocktakeCountLineRequest(
                line.Data.Id,
                line.CountedQuantity.Trim(),
                NormalizeOptional(line.Reason),
                NormalizeOptional(line.Note)))
            .ToArray();

        var fingerprint = string.Join(
            "|",
            counts.Select(item => $"{item.LineId}:{item.CountedBaseQuantity}:{item.Reason}:{item.Note}"));

        await RunMutationAsync(
            "count",
            () => _service.CountAsync(
                SelectedStocktake.Id,
                new InventoryStocktakeCountRequest(SelectedStocktake.Revision, counts),
                KeyFor("count", SelectedStocktake.Id, $"{SelectedStocktake.Revision}:{fingerprint}")),
            "Đã ghi nhận số đếm thực tế. Chọn Gửi duyệt để chuyển phiếu sang người duyệt.").ConfigureAwait(true);
    }

    public async Task SaveAnnotationsAsync()
    {
        if (!CanAnnotateSelected || SelectedStocktake is null) return;

        foreach (var line in Lines)
        {
            if (line.Reason.Trim().Length > 500)
            {
                SetErrorMessage($"Lý do dòng {line.LineNumber} vượt quá 500 ký tự.");
                return;
            }
            if (line.Note.Trim().Length > 2000)
            {
                SetErrorMessage($"Ghi chú dòng {line.LineNumber} vượt quá 2000 ký tự.");
                return;
            }
        }

        var annotations = Lines
            .Select(line => new InventoryStocktakeAnnotationLineRequest(
                line.Data.Id,
                NormalizeOptional(line.Reason),
                NormalizeOptional(line.Note)))
            .ToArray();
        var fingerprint = string.Join("|", annotations.Select(item => $"{item.LineId}:{item.Reason}:{item.Note}"));
        var revision = SelectedStocktake.Revision;
        var id = SelectedStocktake.Id;

        await RunMutationAsync(
            "annotate",
            () => _service.AnnotateAsync(
                id,
                new InventoryStocktakeAnnotateRequest(revision, annotations),
                KeyFor("annotate", id, $"{revision}:{fingerprint}")),
            "Đã lưu Lý do và Ghi chú cho các dòng kiểm kê.").ConfigureAwait(true);
    }

    public async Task CopyAsync()
    {
        if (!CanCopySelected || SelectedStocktake is null) return;

        var id = SelectedStocktake.Id;
        var revision = SelectedStocktake.Revision;
        await RunMutationAsync(
            "copy",
            () => _service.CopyAsync(
                id,
                new InventoryStocktakeCopyRequest(revision),
                KeyFor("copy", id, revision)),
            "Đã sao chép phiếu và snapshot tồn hiện tại vào phiếu mới.").ConfigureAwait(true);
    }

    public Task SubmitAsync() =>
        RunTransitionAsync("submit", "Đã gửi kiểm kê chờ duyệt. Phiếu đang chờ người có quyền duyệt.");

    public Task ApproveAsync() =>
        RunTransitionAsync("approve", "Đã duyệt kết quả kiểm kê. Tồn kho chưa thay đổi. Chọn Cập nhật tồn kho để hoàn tất.");

    public Task PostAsync() =>
        RunTransitionAsync("post", "Đã cập nhật tồn kho theo kết quả kiểm kê.");

    public Task RecountAsync() =>
        RunReasonTransitionAsync("recount", "Đã yêu cầu đếm lại. Lần đếm trước vẫn được lưu trong lịch sử.");

    public Task CancelAsync() =>
        RunReasonTransitionAsync("cancel", "Đã hủy đợt kiểm kê. Tồn kho không thay đổi.");

    public Task ReverseAsync() =>
        RunReasonTransitionAsync("reverse", "Đã hoàn tác phần cập nhật tồn kho của phiếu kiểm kê.");

    private async Task RunTransitionAsync(string action, string successMessage)
    {
        if (SelectedStocktake is null || IsBusy) return;

        var id = SelectedStocktake.Id;
        var revision = SelectedStocktake.Revision;
        Func<Task<InventoryStocktakeData>> mutation = action switch
        {
            "submit" => () => _service.SubmitAsync(id, new InventoryStocktakeTransitionRequest(revision), KeyFor(action, id, revision)),
            "approve" => () => _service.ApproveAsync(id, new InventoryStocktakeTransitionRequest(revision), KeyFor(action, id, revision)),
            "post" => () => _service.PostAsync(id, new InventoryStocktakeTransitionRequest(revision), KeyFor(action, id, revision)),
            _ => throw new InvalidOperationException("Thao tác kiểm kê không hợp lệ.")
        };

        await RunMutationAsync(action, mutation, successMessage).ConfigureAwait(true);
    }

    private async Task RunReasonTransitionAsync(string action, string successMessage)
    {
        if (SelectedStocktake is null || IsBusy) return;

        if (string.IsNullOrWhiteSpace(Reason))
        {
            SetErrorMessage("Nhập lý do trước khi thực hiện thao tác này.");
            return;
        }

        var id = SelectedStocktake.Id;
        var revision = SelectedStocktake.Revision;
        var reason = Reason.Trim();
        Func<Task<InventoryStocktakeData>> mutation = action switch
        {
            "recount" => () => _service.RecountAsync(id, new InventoryStocktakeReasonRequest(revision, reason), KeyFor(action, id, $"{revision}:{reason}")),
            "cancel" => () => _service.CancelAsync(id, new InventoryStocktakeReasonRequest(revision, reason), KeyFor(action, id, $"{revision}:{reason}")),
            "reverse" => () => _service.ReverseAsync(id, new InventoryStocktakeReasonRequest(revision, reason), KeyFor(action, id, $"{revision}:{reason}")),
            _ => throw new InvalidOperationException("Thao tác kiểm kê không hợp lệ.")
        };

        await RunMutationAsync(action, mutation, successMessage).ConfigureAwait(true);
        if (!MessageIsError) Reason = string.Empty;
    }

    private async Task RunMutationAsync(
        string action,
        Func<Task<InventoryStocktakeData>> mutation,
        string successMessage)
    {
        SetBusy(action);
        ClearMessage();
        try
        {
            var result = await mutation().ConfigureAwait(true);
            UpsertSummary(result);
            ApplyDetail(result);
            SetNotice(successMessage);
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

    private void ApplyDetail(InventoryStocktakeData detail)
    {
        SelectedStocktake = detail;

        Replace(
            Lines,
            detail.Lines
                .OrderBy(line => line.LineNumber)
                .Select(line => new StocktakeLineRow(
                    line,
                    _productNames.TryGetValue(line.BaseVariantId, out var name) ? name : line.BaseSku,
                    (detail.Status is "submitted" or "approved") && CanCount)));

        Replace(
            Rounds,
            detail.Rounds
                .OrderByDescending(round => round.RoundNumber)
                .Select(round => new StocktakeRoundRow(round)));

        var row = FilteredStocktakes.FirstOrDefault(item => item.Data.Id == detail.Id);
        if (row is not null) SelectedListRow = row;

        if (IsBlindCount && (LineFilter is "matched" or "mismatch")) LineFilter = "all";
        _linePage = 1;
        ApplyLinePaging();
        RaiseDetailState();
    }

    private void UpsertSummary(InventoryStocktakeData detail)
    {
        var index = _stocktakes.FindIndex(item => item.Id == detail.Id);
        if (index >= 0) _stocktakes[index] = detail;
        else _stocktakes.Insert(0, detail);

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = Search.Trim();
        Replace(
            FilteredStocktakes,
            _stocktakes
                .Where(item =>
                    (StatusFilter == "all" || string.Equals(item.Status, StatusFilter, StringComparison.Ordinal))
                    && (search.Length == 0
                        || item.StocktakeNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || item.WarehouseCode.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || item.WarehouseName.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || StocktakePresentation.Status(item.Status).Contains(search, StringComparison.OrdinalIgnoreCase)))
                .Select(item => new StocktakeListRow(item)));
    }

    private void RebuildScopeGroups()
    {
        foreach (var group in _scopeGroupsAll)
        {
            group.PropertyChanged -= ScopeGroup_PropertyChanged;
        }

        _scopeGroupsAll.Clear();
        var balances = PickerBalancesForSelectedWarehouse();

        if (ScopeMode == "lot")
        {
            foreach (var group in balances
                         .GroupBy(
                             item => $"lot:{item.BaseVariantId}:{item.LotId ?? "<null>"}",
                             StringComparer.Ordinal)
                         .OrderBy(group => DisplayProduct(group.First()), StringComparer.OrdinalIgnoreCase))
            {
                var first = group.First();
                var lot = string.IsNullOrWhiteSpace(first.LotCode) ? "Không lô" : first.LotCode;
                var expiry = string.IsNullOrWhiteSpace(first.ExpiryDate) ? "Không HSD" : $"HSD {first.ExpiryDate}";
                AddScopeGroup(new StocktakeScopeGroupRow(
                    group.Key,
                    $"{DisplayProduct(first)} · {first.BaseSku}",
                    $"Lô {lot} · {expiry}",
                    first.BaseVariantId,
                    first.LotId,
                    null));
            }
        }
        else if (ScopeMode == "location")
        {
            foreach (var group in balances
                         .GroupBy(
                             item => $"location:{item.LocationId ?? "<null>"}",
                             StringComparer.Ordinal)
                         .OrderBy(group => group.First().LocationCode, StringComparer.OrdinalIgnoreCase))
            {
                var first = group.First();
                var code = string.IsNullOrWhiteSpace(first.LocationCode) ? "Không vị trí" : first.LocationCode;
                var name = string.IsNullOrWhiteSpace(first.LocationName) ? string.Empty : $" · {first.LocationName}";
                AddScopeGroup(new StocktakeScopeGroupRow(
                    group.Key,
                    $"{code}{name}",
                    $"{group.Count()} sản phẩm/lô đang có tồn",
                    null,
                    null,
                    first.LocationId));
            }
        }

        ApplyScopeGroupFilter();
        RaiseScopeSelection();
    }

    private List<InventoryBalanceData> PickerBalancesForSelectedWarehouse() =>
        _balances
            .Where(balance => string.Equals(balance.WarehouseId, SelectedWarehouseId, StringComparison.Ordinal))
            .GroupBy(StocktakePresentation.ScopeKey, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

    private void AddScopeGroup(StocktakeScopeGroupRow group)
    {
        group.PropertyChanged += ScopeGroup_PropertyChanged;
        _scopeGroupsAll.Add(group);
    }

    private void ApplyScopeGroupFilter()
    {
        if (ScopeMode == "all")
        {
            ScopeGroups.Clear();
            RaiseScopeSelection();
            return;
        }

        var search = ScopeSearch.Trim();
        Replace(
            ScopeGroups,
            _scopeGroupsAll
                .Where(group =>
                    search.Length == 0
                    || group.Label.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || group.Detail.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Take(ScopePickerResultLimit));
        RaiseScopeSelection();
    }

    private void ScopeGroup_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StocktakeScopeGroupRow.IsSelected))
        {
            RaiseScopeSelection();
        }
    }

    public void SelectAllScopeResults()
    {
        foreach (var group in ScopeGroups) group.IsSelected = true;
        RaiseScopeSelection();
    }

    public void ClearScopeResults()
    {
        foreach (var group in ScopeGroups) group.IsSelected = false;
        RaiseScopeSelection();
    }

    private void RaiseScopeSelection()
    {
        Replace(SelectedScopeGroups, _scopeGroupsAll.Where(group => group.IsSelected).Take(12));
        OnPropertyChanged(nameof(SelectedScopeCount));
        OnPropertyChanged(nameof(ScopeSelectionText));
        OnPropertyChanged(nameof(ShowScopePicker));
        OnPropertyChanged(nameof(ScopeResultsText));
        OnPropertyChanged(nameof(HasScopeGroups));
        OnPropertyChanged(nameof(HasNoScopeGroups));
    }

    public void SetLineFilter(string filter)
    {
        if (filter is not ("all" or "uncounted" or "matched" or "mismatch")) return;
        if (IsBlindCount && (filter is "matched" or "mismatch")) return;
        LineFilter = filter;
        _linePage = 1;
        ApplyLinePaging();
    }

    public void PreviousLinePage()
    {
        if (_linePage <= 1) return;
        _linePage--;
        ApplyLinePaging();
    }

    public void NextLinePage()
    {
        if (_linePage >= TotalLinePages) return;
        _linePage++;
        ApplyLinePaging();
    }

    public void RefreshLinePaging()
    {
        _linePage = Math.Min(_linePage, TotalLinePages);
        ApplyLinePaging();
    }

    private bool IsLineUncounted(StocktakeLineRow line) =>
        IsBlindCount
            ? string.IsNullOrWhiteSpace(line.CountedQuantity)
            : line.Data.CountedBaseQuantity is null;

    private IEnumerable<StocktakeLineRow> FilteredLines() =>
        LineFilter switch
        {
            "uncounted" => Lines.Where(IsLineUncounted),
            "matched" => Lines.Where(line => line.Data.CountStatus == "matched"),
            "mismatch" => Lines.Where(line => line.Data.CountStatus == "mismatch"),
            _ => Lines
        };

    private int FilteredLineCount() => FilteredLines().Count();

    private void ApplyLinePaging()
    {
        var totalPages = TotalLinePages;
        _linePage = Math.Clamp(_linePage, 1, totalPages);
        Replace(PagedLines, FilteredLines().Skip((_linePage - 1) * LinePageSize).Take(LinePageSize));
        OnPropertyChanged(nameof(AllLineCount));
        OnPropertyChanged(nameof(UncountedLineCount));
        OnPropertyChanged(nameof(MatchedLineCount));
        OnPropertyChanged(nameof(MismatchLineCount));
        OnPropertyChanged(nameof(AllLineFilterText));
        OnPropertyChanged(nameof(UncountedLineFilterText));
        OnPropertyChanged(nameof(MatchedLineFilterText));
        OnPropertyChanged(nameof(MismatchLineFilterText));
        OnPropertyChanged(nameof(CanUseRevealFilters));
        OnPropertyChanged(nameof(TotalLinePages));
        OnPropertyChanged(nameof(LinePageText));
        OnPropertyChanged(nameof(CanPreviousLinePage));
        OnPropertyChanged(nameof(CanNextLinePage));
    }

    private void RaiseDetailState()
    {
        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(DetailNumber));
        OnPropertyChanged(nameof(DetailWarehouse));
        OnPropertyChanged(nameof(DetailNote));
        OnPropertyChanged(nameof(DetailStatus));
        OnPropertyChanged(nameof(WorkflowHint));
        OnPropertyChanged(nameof(IsBlindCount));
        OnPropertyChanged(nameof(IsReviewTable));
        OnPropertyChanged(nameof(CanPrint));
        OnPropertyChanged(nameof(CanCountSelected));
        OnPropertyChanged(nameof(CanAnnotateSelected));
        OnPropertyChanged(nameof(CanCopySelected));
        OnPropertyChanged(nameof(CanExportCountFile));
        OnPropertyChanged(nameof(CanImportCountFile));
        OnPropertyChanged(nameof(CanExportResults));
        OnPropertyChanged(nameof(CanSubmitSelected));
        OnPropertyChanged(nameof(CanRecountSelected));
        OnPropertyChanged(nameof(CanApproveSelected));
        OnPropertyChanged(nameof(CanPostSelected));
        OnPropertyChanged(nameof(CanCancelSelected));
        OnPropertyChanged(nameof(CanReverseSelected));
        OnPropertyChanged(nameof(ShowReasonField));
        OnPropertyChanged(nameof(SubmittedText));
        OnPropertyChanged(nameof(ApprovedText));
        OnPropertyChanged(nameof(PostedText));
        OnPropertyChanged(nameof(UpdatedText));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanCount));
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanPost));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CanReverse));
        RaiseDetailState();
    }

    private void SetBusy(string? action)
    {
        if (string.Equals(_busyAction, action, StringComparison.Ordinal)) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(RefreshText));
        RaiseDetailState();
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

        var key = _idempotencyKeys.Create($"stocktake-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private void ResetSessionData()
    {
        _stocktakes.Clear();
        _balances.Clear();
        _productNames.Clear();
        foreach (var group in _scopeGroupsAll) group.PropertyChanged -= ScopeGroup_PropertyChanged;
        _scopeGroupsAll.Clear();
        FilteredStocktakes.Clear();
        Warehouses.Clear();
        Lines.Clear();
        PagedLines.Clear();
        Rounds.Clear();
        ScopeGroups.Clear();
        SelectedScopeGroups.Clear();
        SelectedStocktake = null;
        SelectedListRow = null;
        IsCreateOpen = false;
        SelectedWarehouseId = string.Empty;
        ScopeSearch = string.Empty;
        LineFilter = "all";
        _linePage = 1;
        Note = string.Empty;
        Reason = string.Empty;
    }

    private static string DisplayProduct(InventoryBalanceData balance) =>
        string.IsNullOrWhiteSpace(balance.ProductName)
            ? balance.BaseVariantName ?? balance.BaseSku
            : balance.ProductName;

    private static string ActorTime(string? actor, string? time, string fallback) =>
        $"{(string.IsNullOrWhiteSpace(actor) ? fallback : actor)}{(string.IsNullOrWhiteSpace(time) ? string.Empty : $" · {StocktakePresentation.DateTimeText(time)}")}";

    private static string? NormalizeOptional(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void ReportFileNotice(string message) => SetNotice(message);

    public void ReportFileError(string message) => SetErrorMessage(message);

    public void ReportFileError(Exception exception) => SetError(exception);

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
        if (exception is CanonicalApiException apiException
            && string.Equals(apiException.Code, "WAREHOUSE_LOCATION_MODE_REQUIRED", StringComparison.Ordinal))
        {
            SetErrorMessage(CanonicalErrorMessages.WithRequestId(
                "Kho chưa thiết lập chế độ quản lý vị trí. Hãy cấu hình chế độ quản lý vị trí trên hệ thống trước khi tạo kiểm kê; Desktop không tự chọn thay.",
                apiException.RequestId));
            return;
        }

        SetErrorMessage(
            exception is CanonicalApiException canonical
                ? CanonicalErrorMessages.WithRequestId(
                    CanonicalErrorMessages.ToOfficeMessage(canonical),
                    canonical.RequestId)
                : exception.Message);
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
