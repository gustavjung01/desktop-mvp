using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record InventoryCostBalanceRow(int Sequence, InventoryCostBalanceData Data)
{
    public string Warehouse => Data.WarehouseCode ?? Data.WarehouseName ?? "—";
    public string Sku => Data.BaseSku ?? "—";
    public string Quantity => InventoryCostingPresentation.DecimalText(Data.Quantity);
    public string InventoryValue => InventoryCostingPresentation.MoneyVnd(Data.InventoryValue);
    public string AverageUnitCost => InventoryCostingPresentation.MoneyVnd(Data.AverageUnitCost);
    public string Status => InventoryCostingPresentation.Status(Data.Status);
}

public sealed record InventoryCostPeriodRow(int Sequence, InventoryCostingPeriodData Data)
{
    public string Period => $"{InventoryCostingPresentation.DateText(Data.PeriodStart)} → {InventoryCostingPresentation.DateText(Data.PeriodEnd)}";
    public string Status => InventoryCostingPresentation.Status(Data.Status);
    public string Snapshot => Data.Status == "CLOSED" ? $"{Data.SnapshotPoolCount} nhóm dữ liệu" : "—";
    public string OpenedBy => string.IsNullOrWhiteSpace(Data.OpenedBy) ? "—" : Data.OpenedBy;
    public string ClosedBy => string.IsNullOrWhiteSpace(Data.ClosedBy) ? "—" : Data.ClosedBy;
}

public sealed record InventoryCostReconciliationRow(int Sequence, InventoryCostReconciliationData Data)
{
    public string Warehouse => Data.WarehouseCode ?? "—";
    public string Sku => Data.BaseSku ?? "—";
    public string LedgerQuantity => InventoryCostingPresentation.DecimalText(Data.LedgerQuantity);
    public string CostingQuantity => InventoryCostingPresentation.DecimalText(Data.CostingQuantity);
    public string Difference => InventoryCostingPresentation.DecimalText(Data.QuantityDifference);
    public string Status => InventoryCostingPresentation.Status(Data.ReconciliationStatus);
}

public sealed record InventoryCostAdjustmentRow(int Sequence, InventoryCostAdjustmentEventData Data)
{
    public string PostingDate => InventoryCostingPresentation.DateText(Data.PostingDate);
    public string WarehouseSku => $"{Data.WarehouseCode ?? "—"} · {Data.BaseSku ?? "—"}";
    public string Type => InventoryCostingPresentation.AdjustmentType(Data.EventType);
    public string Quantity => InventoryCostingPresentation.DecimalText(Data.QuantityDelta);
    public string Value => InventoryCostingPresentation.MoneyVnd(Data.ValueDelta);
    public string Source => InventoryCostingPresentation.DocumentSource(Data.SourceDocumentType, Data.SourceLineReference);
}

public sealed record InventoryCostFactRow(int Sequence, InventoryCostFactData Data)
{
    public string PostedAt => InventoryCostingPresentation.DateTimeText(Data.MovementPostedAt);
    public string WarehouseSku => $"{Data.WarehouseCode ?? "—"} · {Data.BaseSku ?? "—"}";
    public string Type => InventoryCostingPresentation.EventType(Data.EventType);
    public string Quantity => InventoryCostingPresentation.DecimalText(Data.QuantityDelta);
    public string UnitCost => InventoryCostingPresentation.MoneyVnd(Data.UnitCost);
    public string Value => InventoryCostingPresentation.MoneyVnd(Data.ValueDelta);
    public string Source => $"{InventoryCostingPresentation.CostSource(Data.SourceCostType)} · {Data.SourceDocumentNumber ?? Data.SourceLineReference ?? "Không có số tham chiếu"}";
}

public sealed record InventoryCostIssueRow(
    int Sequence,
    string Title,
    string Message,
    string Meta,
    string Status);

public sealed class InventoryCostingViewModel : INotifyPropertyChanged
{
    private readonly IInventoryCostingService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;

    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _activeTab = "balances";
    private InventoryCostingRunData? _run;
    private string? _pendingRebuildKey;
    private string? _pendingPeriodFingerprint;
    private string? _pendingPeriodKey;

    public InventoryCostingViewModel(
        IInventoryCostingService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            _pendingRebuildKey = null;
            _pendingPeriodFingerprint = null;
            _pendingPeriodKey = null;
            ClearData();
            RaisePermissions();
            if (CanOpen && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<InventoryCostBalanceRow> Balances { get; } = [];
    public ObservableCollection<InventoryCostPeriodRow> Periods { get; } = [];
    public ObservableCollection<InventoryCostReconciliationRow> Reconciliation { get; } = [];
    public ObservableCollection<InventoryCostIssueRow> Discrepancies { get; } = [];
    public ObservableCollection<InventoryCostAdjustmentRow> Adjustments { get; } = [];
    public ObservableCollection<InventoryCostIssueRow> Anomalies { get; } = [];
    public ObservableCollection<InventoryCostFactRow> Facts { get; } = [];

    public bool CanRead => _access.HasPermission("core.inventory-cost.read");
    public bool CanReconcile => _access.HasPermission("core.inventory-cost.reconcile");
    public bool CanRebuild => _access.HasPermission("core.inventory-cost.rebuild");
    public bool CanOpen => CanRead || CanReconcile;
    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool CanRunRebuild => CanRebuild && CanRead && IsNotBusy;
    public bool CanManagePeriod => CanRebuild && CanRead && IsNotBusy;

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
            RaiseTabs();
        }
    }

    public bool IsBalancesTab => ActiveTab == "balances";
    public bool IsPeriodsTab => ActiveTab == "periods";
    public bool IsReconciliationTab => ActiveTab == "reconciliation";
    public bool IsDiscrepanciesTab => ActiveTab == "discrepancies";
    public bool IsAdjustmentsTab => ActiveTab == "adjustments";
    public bool IsAnomaliesTab => ActiveTab == "anomalies";
    public bool IsFactsTab => ActiveTab == "facts";

    public int PoolCount => Balances.Count;
    public int CostedCount => Balances.Count(item => item.Data.Status == "COSTED");
    public int AnomalyCount => Anomalies.Count;
    public int OpenDiscrepancyCount => Discrepancies.Count(item => item.Status == "Đang mở");

    public InventoryCostingPeriodData? OpenPeriod =>
        Periods.Select(item => item.Data).FirstOrDefault(item => item.Status == "OPEN");

    public string PeriodHeadline => OpenPeriod is not null
        ? $"Kỳ {InventoryCostingPresentation.DateText(OpenPeriod.PeriodStart)} đang mở"
        : $"Kỳ kế tiếp: {InventoryCostingPresentation.DateText(SuggestedPeriodStart)}";

    public string PeriodActionText => OpenPeriod is not null
        ? "Khóa kỳ sau đối soát"
        : $"Mở kỳ {InventoryCostingPresentation.DateText(SuggestedPeriodStart)}";

    public string OpenPeriodSummary => OpenPeriod is not null
        ? $"Kỳ mở {InventoryCostingPresentation.DateText(OpenPeriod.PeriodStart)} → {InventoryCostingPresentation.DateText(OpenPeriod.PeriodEnd)}"
        : "Chưa có kỳ giá vốn đang mở";

    public string LatestRunSummary => _run is null
        ? "Chưa có dữ liệu giá vốn tổng hợp"
        : $"Tổng hợp gần nhất · {InventoryCostingPresentation.DateTimeText(_run.CompletedAt)} · {_run.FactCount} dòng giá vốn";

    public string MethodSummary =>
        "Bình quân gia quyền di động theo từng kho và SKU tồn kho cơ sở · tiền tệ VND.";

    public string SuggestedPeriodStart
    {
        get
        {
            if (OpenPeriod is not null) return OpenPeriod.PeriodStart;

            var latestClosed = Periods.Select(item => item.Data).FirstOrDefault(item => item.Status == "CLOSED");
            if (latestClosed is not null
                && DateOnly.TryParseExact(latestClosed.PeriodStart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var closedStart))
            {
                var next = closedStart.AddMonths(1);
                return new DateOnly(next.Year, next.Month, 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            var now = DateTime.UtcNow;
            return new DateOnly(now.Year, now.Month, 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);

        _initialLoadTask = LoadAsync();
        try { return await _initialLoadTask.ConfigureAwait(true); }
        finally { _initialLoadTask = null; }
    }

    public async Task<bool> RefreshAsync() => await LoadAsync().ConfigureAwait(true);

    public void SetTab(string tab)
    {
        if (tab is not ("balances" or "periods" or "reconciliation" or "discrepancies" or "adjustments" or "anomalies" or "facts"))
        {
            return;
        }

        if (!CanReconcile && (tab == "reconciliation" || tab == "discrepancies")) return;
        if (!CanRead && tab != "reconciliation" && tab != "discrepancies") return;
        ActiveTab = tab;
    }

    public async Task<bool> LoadAsync()
    {
        var generation = _accessGeneration;
        if (!CanOpen)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền xem Giá vốn tồn kho.");
            return false;
        }

        SetBusy("load");
        ClearMessage();
        var succeeded = true;

        if (CanRead)
        {
            try
            {
                var balancesTask = _service.ListBalancesAsync();
                var factsTask = _service.ListFactsAsync();
                var anomaliesTask = _service.ListAnomaliesAsync();
                var periodsTask = _service.ListPeriodsAsync();
                var adjustmentsTask = _service.ListAdjustmentsAsync();
                var runTask = _service.GetLatestRunAsync();

                await Task.WhenAll(balancesTask, factsTask, anomaliesTask, periodsTask, adjustmentsTask, runTask).ConfigureAwait(true);
                if (generation != _accessGeneration)
                {
                    SetBusy(null);
                    return false;
                }

                Replace(Balances, balancesTask.Result.Select((item, index) => new InventoryCostBalanceRow(index + 1, item)));
                Replace(Facts, factsTask.Result.Select((item, index) => new InventoryCostFactRow(index + 1, item)));
                Replace(Anomalies, anomaliesTask.Result.Select((item, index) => new InventoryCostIssueRow(
                    index + 1,
                    InventoryCostingPresentation.IssueCode(item.Code),
                    item.Message,
                    $"{item.WarehouseCode ?? "—"} · {item.BaseSku ?? "—"}",
                    "Cần xử lý")));
                Replace(Periods, periodsTask.Result.Select((item, index) => new InventoryCostPeriodRow(index + 1, item)));
                Replace(Adjustments, adjustmentsTask.Result.Select((item, index) => new InventoryCostAdjustmentRow(index + 1, item)));
                _run = runTask.Result;
            }
            catch (Exception exception)
            {
                succeeded = false;
                SetError(exception);
            }
        }
        else
        {
            Balances.Clear();
            Facts.Clear();
            Anomalies.Clear();
            Periods.Clear();
            Adjustments.Clear();
            _run = null;
        }

        if (CanReconcile)
        {
            try
            {
                var reconciliationTask = _service.ListReconciliationAsync();
                var discrepanciesTask = _service.ListDiscrepanciesAsync();
                await Task.WhenAll(reconciliationTask, discrepanciesTask).ConfigureAwait(true);
                if (generation != _accessGeneration)
                {
                    SetBusy(null);
                    return false;
                }

                Replace(Reconciliation, reconciliationTask.Result.Select((item, index) => new InventoryCostReconciliationRow(index + 1, item)));
                Replace(Discrepancies, discrepanciesTask.Result.Select((item, index) => new InventoryCostIssueRow(
                    index + 1,
                    InventoryCostingPresentation.IssueCode(item.Code),
                    item.Message,
                    $"{item.WarehouseCode ?? "—"} · {item.BaseSku ?? "—"} · {InventoryCostingPresentation.DateTimeText(item.LastSeenAt)}",
                    InventoryCostingPresentation.Status(item.Status))));
            }
            catch (Exception exception)
            {
                succeeded = false;
                SetError(exception);
            }
        }
        else
        {
            Reconciliation.Clear();
            Discrepancies.Clear();
        }

        if (generation != _accessGeneration)
        {
            SetBusy(null);
            return false;
        }

        if (!CanRead && CanReconcile && ActiveTab != "reconciliation" && ActiveTab != "discrepancies")
        {
            ActiveTab = "reconciliation";
        }

        _loaded = succeeded;
        SetBusy(null);
        RaiseSummary();
        return succeeded;
    }

    public async Task RebuildAsync()
    {
        if (!CanRunRebuild) return;

        _pendingRebuildKey ??= _idempotencyKeys.Create("inventory-costing-rebuild");

        SetBusy("rebuild");
        ClearMessage();
        try
        {
            var result = await _service.RebuildAsync(_pendingRebuildKey).ConfigureAwait(true);
            _pendingRebuildKey = null;
            await LoadAsync().ConfigureAwait(true);
            SetNotice($"Đã tổng hợp {result.Run.FactCount} dòng giá vốn; {result.AnomalyCount} dòng cần xử lý.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
            RaisePermissions();
        }
    }

    public async Task MutatePeriodAsync()
    {
        if (!CanManagePeriod) return;

        var open = OpenPeriod;
        var action = open is null ? "open" : "close";
        var periodStart = open?.PeriodStart ?? SuggestedPeriodStart;
        var fingerprint = $"{action}:{periodStart}";

        if (!string.Equals(_pendingPeriodFingerprint, fingerprint, StringComparison.Ordinal))
        {
            _pendingPeriodFingerprint = fingerprint;
            _pendingPeriodKey = _idempotencyKeys.Create($"inventory-costing-period-{action}");
        }

        SetBusy($"period-{action}");
        ClearMessage();
        try
        {
            if (action == "open")
            {
                await _service.OpenPeriodAsync(periodStart, _pendingPeriodKey!).ConfigureAwait(true);
            }
            else
            {
                await _service.ClosePeriodAsync(periodStart, _pendingPeriodKey!).ConfigureAwait(true);
            }

            _pendingPeriodFingerprint = null;
            _pendingPeriodKey = null;
            await LoadAsync().ConfigureAwait(true);
            SetNotice(action == "open"
                ? $"Đã mở kỳ giá vốn {InventoryCostingPresentation.DateText(periodStart)}."
                : $"Đã khóa kỳ giá vốn {InventoryCostingPresentation.DateText(periodStart)} sau đối soát.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
            RaisePermissions();
        }
    }

    private void ClearData()
    {
        Balances.Clear();
        Periods.Clear();
        Reconciliation.Clear();
        Discrepancies.Clear();
        Adjustments.Clear();
        Anomalies.Clear();
        Facts.Clear();
        _run = null;
        Message = string.Empty;
        MessageIsError = false;
        RaiseSummary();
    }

    private void RaiseTabs()
    {
        OnPropertyChanged(nameof(IsBalancesTab));
        OnPropertyChanged(nameof(IsPeriodsTab));
        OnPropertyChanged(nameof(IsReconciliationTab));
        OnPropertyChanged(nameof(IsDiscrepanciesTab));
        OnPropertyChanged(nameof(IsAdjustmentsTab));
        OnPropertyChanged(nameof(IsAnomaliesTab));
        OnPropertyChanged(nameof(IsFactsTab));
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(PoolCount));
        OnPropertyChanged(nameof(CostedCount));
        OnPropertyChanged(nameof(AnomalyCount));
        OnPropertyChanged(nameof(OpenDiscrepancyCount));
        OnPropertyChanged(nameof(OpenPeriod));
        OnPropertyChanged(nameof(PeriodHeadline));
        OnPropertyChanged(nameof(PeriodActionText));
        OnPropertyChanged(nameof(OpenPeriodSummary));
        OnPropertyChanged(nameof(LatestRunSummary));
        OnPropertyChanged(nameof(SuggestedPeriodStart));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanReconcile));
        OnPropertyChanged(nameof(CanRebuild));
        OnPropertyChanged(nameof(CanOpen));
        OnPropertyChanged(nameof(CanRunRebuild));
        OnPropertyChanged(nameof(CanManagePeriod));
    }

    private void SetBusy(string? action)
    {
        if (_busyAction == action) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        RaisePermissions();
    }

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
                "WAREHOUSE_SCOPE_DENIED" => "Tài khoản chưa được cấp phạm vi kho để xem giá vốn.",
                "INVENTORY_COSTING_REBUILD_FAILED" => "Chưa thể dựng lại giá vốn. Hãy thử lại sau.",
                "INVENTORY_COSTING_MUTATION_FAILED" => "Chưa thể cập nhật kỳ giá vốn. Hãy thử lại sau.",
                "COSTING_PERIOD_DISCREPANCY_BLOCKED" => "Kỳ giá vốn còn chênh lệch cần xử lý nên chưa thể khóa.",
                "COSTING_PERIOD_ANOMALY_BLOCKED" => "Kỳ giá vốn còn bất thường nguồn giá nên chưa thể khóa.",
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
