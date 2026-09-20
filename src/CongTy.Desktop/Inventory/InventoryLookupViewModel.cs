using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed class InventoryLookupViewModel : INotifyPropertyChanged
{
    private const int PageSize = 100;
    private const int HistoryPageSize = 50;

    private readonly IInventoryService _service;
    private readonly IAccessStateService _access;
    private readonly List<InventoryBalanceData> _allBalances = [];

    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _activeTab = "balances";
    private string _search = string.Empty;
    private int _page;
    private int _filteredCount;
    private int _pageCount = 1;
    private InventoryLookupBalanceRow? _selectedBalance;
    private int _historyPage;
    private bool _historyHasNext;
    private InventoryLookupHistoryRow? _selectedHistory;
    private bool _isHistoryDetailOpen;

    public InventoryLookupViewModel(IInventoryService service, IAccessStateService access)
    {
        _service = service;
        _access = access;
        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            ClearData();
            RaisePermissions();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<InventoryLookupBalanceRow> Balances { get; } = [];
    public ObservableCollection<InventoryLookupHistoryRow> HistoryRows { get; } = [];

    public bool CanRead => _access.HasPermission("core.inventory.read");
    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsBalancesTab => ActiveTab == "balances";
    public bool IsHistoryTab => ActiveTab == "history";
    public bool CanRefreshHistory => SelectedBalance is not null && IsNotBusy;
    public bool HistoryHasPrevious => SelectedBalance is not null && _historyPage > 0 && IsNotBusy;
    public bool HistoryHasNext => SelectedBalance is not null && _historyHasNext && IsNotBusy;
    public bool HasHistoryRows => HistoryRows.Count > 0;
    public bool HasNoHistoryRows => SelectedBalance is not null && !IsBusy && HistoryRows.Count == 0;

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
            OnPropertyChanged(nameof(IsBalancesTab));
            OnPropertyChanged(nameof(IsHistoryTab));
        }
    }

    public string Search
    {
        get => _search;
        set
        {
            if (!SetField(ref _search, value ?? string.Empty)) return;
            _page = 0;
            ApplyFilterAndPage();
        }
    }

    public int FilteredCount => _filteredCount;
    public int PageCount => _pageCount;
    public int PageNumber => Math.Min(_page, PageCount - 1) + 1;
    public string PageSummary => $"{FilteredCount} dòng · Trang {PageNumber}/{PageCount}";
    public bool HasPreviousPage => _page > 0 && IsNotBusy;
    public bool HasNextPage => _page < PageCount - 1 && IsNotBusy;

    public InventoryLookupBalanceRow? SelectedBalance
    {
        get => _selectedBalance;
        private set
        {
            if (!SetField(ref _selectedBalance, value)) return;
            RaiseHistoryContext();
        }
    }

    public string HistoryContext => SelectedBalance is null
        ? "Chọn “Xem lịch sử” tại tab Tồn kho để tra cứu biến động của một SKU theo kho."
        : $"Đang xem SKU {SelectedBalance.Data.BaseSku} tại kho {SelectedBalance.Data.WarehouseCode} · {SelectedBalance.Data.WarehouseName}.";

    public string HistoryProduct => SelectedBalance?.Data.ProductName ?? "Lịch sử kho";
    public string HistorySku => SelectedBalance?.Data.BaseSku ?? string.Empty;
    public string HistoryWarehouse => SelectedBalance is null
        ? string.Empty
        : $"{SelectedBalance.Data.WarehouseCode} · {SelectedBalance.Data.WarehouseName}";
    public string HistoryWarehouseTotal
    {
        get
        {
            if (SelectedBalance is null) return "—";
            var value = InventoryLookupPresentation.SumWarehouseOnHand(_allBalances, SelectedBalance.Data);
            return InventoryLookupPresentation.QuantityWithUnit(
                value.ToString(CultureInfo.InvariantCulture),
                SelectedBalance.Data);
        }
    }

    public string HistoryPageText => $"Trang {_historyPage + 1}";

    public InventoryLookupHistoryRow? SelectedHistory
    {
        get => _selectedHistory;
        private set => SetField(ref _selectedHistory, value);
    }

    public bool IsHistoryDetailOpen
    {
        get => _isHistoryDetailOpen;
        private set => SetField(ref _isHistoryDetailOpen, value);
    }

    public string DetailDocumentType => InventoryLookupPresentation.DocumentType(SelectedHistory?.Data.SourceDocumentType);
    public string DetailDocumentNumber => SelectedHistory?.DocumentNumber ?? "Chi tiết chứng từ kho";
    public string DetailPostedAt => SelectedHistory?.PostedAt ?? "—";
    public string DetailEmployee => SelectedHistory?.Employee ?? "—";
    public string DetailMovement => SelectedHistory?.Movement ?? "—";
    public string DetailQuantity => SelectedHistory?.Quantity ?? "—";
    public string DetailStockAfter => SelectedHistory?.StockAfter ?? "—";
    public string DetailWarehouse => SelectedHistory?.Warehouse ?? "—";
    public string DetailLocation => SelectedHistory?.Data.LocationSummary ?? "Không vị trí";
    public string DetailLot => SelectedHistory?.Data.LotSummary ?? "Không lô";
    public string DetailReason => SelectedHistory?.Data.ReasonNote ?? string.Empty;
    public bool HasDetailReason => !string.IsNullOrWhiteSpace(DetailReason);

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);

        _initialLoadTask = LoadBalancesAsync(preserveSelection: true);
        try { return await _initialLoadTask.ConfigureAwait(true); }
        finally { _initialLoadTask = null; }
    }

    public Task<bool> RefreshBalancesAsync() => LoadBalancesAsync(preserveSelection: true);

    public async Task<bool> RefreshCurrentAsync()
    {
        if (IsHistoryTab && SelectedBalance is not null)
        {
            await LoadHistoryPageAsync(_historyPage).ConfigureAwait(true);
            return true;
        }

        return await RefreshBalancesAsync().ConfigureAwait(true);
    }

    public void SetTab(string tab)
    {
        if (tab is not ("balances" or "history")) return;
        ActiveTab = tab;
        ClearMessage();
    }

    public void PreviousPage()
    {
        if (!HasPreviousPage) return;
        _page--;
        ApplyFilterAndPage();
    }

    public void NextPage()
    {
        if (!HasNextPage) return;
        _page++;
        ApplyFilterAndPage();
    }

    public async Task OpenHistoryAsync(InventoryLookupBalanceRow row)
    {
        if (!CanRead || IsBusy) return;
        SelectedBalance = row;
        _historyPage = 0;
        ActiveTab = "history";
        await LoadHistoryPageAsync(0).ConfigureAwait(true);
    }

    public Task RefreshHistoryAsync() =>
        SelectedBalance is null ? Task.CompletedTask : LoadHistoryPageAsync(_historyPage);

    public async Task HistoryPreviousAsync()
    {
        if (!HistoryHasPrevious) return;
        await LoadHistoryPageAsync(_historyPage - 1).ConfigureAwait(true);
    }

    public async Task HistoryNextAsync()
    {
        if (!HistoryHasNext) return;
        await LoadHistoryPageAsync(_historyPage + 1).ConfigureAwait(true);
    }

    public void OpenHistoryDetail(InventoryLookupHistoryRow row)
    {
        if (!row.HasDocument) return;
        SelectedHistory = row;
        IsHistoryDetailOpen = true;
        RaiseDetail();
    }

    public void CloseHistoryDetail()
    {
        IsHistoryDetailOpen = false;
        SelectedHistory = null;
        RaiseDetail();
    }

    private async Task<bool> LoadBalancesAsync(bool preserveSelection)
    {
        if (!CanRead)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền xem Tra cứu tồn kho.");
            return false;
        }

        var generation = _accessGeneration;
        var selectedKey = preserveSelection && SelectedBalance is not null
            ? BalanceKey(SelectedBalance.Data)
            : null;

        SetBusy("balances");
        ClearMessage();
        try
        {
            var rows = await _service.ListBalancesAsync().ConfigureAwait(true);
            if (generation != _accessGeneration || !CanRead) return false;

            _allBalances.Clear();
            _allBalances.AddRange(rows);
            ApplyFilterAndPage();

            if (selectedKey is not null)
            {
                var selected = _allBalances.FirstOrDefault(row => BalanceKey(row) == selectedKey);
                if (selected is not null)
                {
                    SelectedBalance = new InventoryLookupBalanceRow(0, selected);
                }
            }

            _loaded = true;
            SetNotice($"Đã làm mới {_allBalances.Count(InventoryLookupPresentation.HasDisplayableBalance)} dòng tồn kho.");
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

    private async Task LoadHistoryPageAsync(int page)
    {
        if (SelectedBalance is null || !CanRead) return;

        var generation = _accessGeneration;
        var selected = SelectedBalance;
        SetBusy("history");
        ClearMessage();
        try
        {
            var rows = await _service.ListHistoryAsync(
                selected.Data.WarehouseId,
                selected.Data.BaseVariantId,
                Math.Max(0, page)).ConfigureAwait(true);
            if (generation != _accessGeneration || !CanRead) return;

            _historyPage = Math.Max(0, page);
            _historyHasNext = rows.Count > HistoryPageSize;
            Replace(
                HistoryRows,
                rows.Take(HistoryPageSize).Select(item => new InventoryLookupHistoryRow(item)
                {
                    Quantity = InventoryLookupPresentation.QuantityWithUnit(item.BaseQuantityDelta, selected.Data),
                    StockAfter = InventoryLookupPresentation.QuantityWithUnit(item.StockAfter, selected.Data)
                }));
            SelectedHistory = null;
            IsHistoryDetailOpen = false;
            RaiseHistoryContext();
            RaiseHistoryPaging();
            OnPropertyChanged(nameof(HasHistoryRows));
            OnPropertyChanged(nameof(HasNoHistoryRows));
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
            RaiseHistoryPaging();
            OnPropertyChanged(nameof(HasNoHistoryRows));
        }
    }

    private void ApplyFilterAndPage()
    {
        var search = Search.Trim().ToLowerInvariant();
        var filtered = _allBalances
            .Where(InventoryLookupPresentation.HasDisplayableBalance)
            .Where(row => search.Length == 0 || InventoryLookupPresentation.SearchText(row).Contains(search, StringComparison.Ordinal))
            .ToArray();

        var pages = InventoryLookupPresentation.PaginateBalanceGroups(filtered, PageSize);
        _filteredCount = filtered.Length;
        _pageCount = Math.Max(1, pages.Count);
        _page = Math.Min(_page, _pageCount - 1);

        var pageRows = pages.Count == 0
            ? Array.Empty<InventoryBalanceData>()
            : pages[_page];
        var pageStart = pages.Take(_page).Sum(page => page.Count);
        var displayRows = new List<InventoryLookupBalanceRow>(pageRows.Count);
        var sequence = pageStart + 1;

        foreach (var group in InventoryLookupPresentation.GroupBalancesByWarehouseSku(pageRows))
        {
            var held = InventoryLookupPresentation.BusinessHeldQuantity(group);
            var available = InventoryLookupPresentation.BusinessAvailableQuantity(group);
            for (var index = 0; index < group.Count; index++)
            {
                displayRows.Add(new InventoryLookupBalanceRow(
                    sequence++,
                    group[index],
                    held,
                    available,
                    index == 0));
            }
        }

        Replace(Balances, displayRows);
        RaisePage();
    }

    private void RaisePage()
    {
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(PageNumber));
        OnPropertyChanged(nameof(PageSummary));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
    }

    private void RaiseHistoryContext()
    {
        OnPropertyChanged(nameof(HistoryContext));
        OnPropertyChanged(nameof(HistoryProduct));
        OnPropertyChanged(nameof(HistorySku));
        OnPropertyChanged(nameof(HistoryWarehouse));
        OnPropertyChanged(nameof(HistoryWarehouseTotal));
        OnPropertyChanged(nameof(CanRefreshHistory));
        RaiseHistoryPaging();
    }

    private void RaiseHistoryPaging()
    {
        OnPropertyChanged(nameof(HistoryHasPrevious));
        OnPropertyChanged(nameof(HistoryHasNext));
        OnPropertyChanged(nameof(HistoryPageText));
    }

    private void RaiseDetail()
    {
        OnPropertyChanged(nameof(DetailDocumentType));
        OnPropertyChanged(nameof(DetailDocumentNumber));
        OnPropertyChanged(nameof(DetailPostedAt));
        OnPropertyChanged(nameof(DetailEmployee));
        OnPropertyChanged(nameof(DetailMovement));
        OnPropertyChanged(nameof(DetailQuantity));
        OnPropertyChanged(nameof(DetailStockAfter));
        OnPropertyChanged(nameof(DetailWarehouse));
        OnPropertyChanged(nameof(DetailLocation));
        OnPropertyChanged(nameof(DetailLot));
        OnPropertyChanged(nameof(DetailReason));
        OnPropertyChanged(nameof(HasDetailReason));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanRefreshHistory));
    }

    private void SetBusy(string? action)
    {
        if (_busyAction == action) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        RaisePage();
        RaiseHistoryPaging();
        OnPropertyChanged(nameof(CanRefreshHistory));
        OnPropertyChanged(nameof(HasNoHistoryRows));
    }

    private void ClearData()
    {
        _allBalances.Clear();
        Balances.Clear();
        HistoryRows.Clear();
        _loaded = false;
        _page = 0;
        _filteredCount = 0;
        _historyPage = 0;
        _historyHasNext = false;
        SelectedBalance = null;
        SelectedHistory = null;
        IsHistoryDetailOpen = false;
        Message = string.Empty;
        MessageIsError = false;
        ApplyFilterAndPage();
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
            SetErrorMessage(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(apiException),
                apiException.RequestId));
            return;
        }

        SetErrorMessage(exception.Message);
    }

    private static string BalanceKey(InventoryBalanceData row) =>
        $"{row.WarehouseId}:{row.LocationId ?? "<null>"}:{row.BaseVariantId}:{row.LotId ?? "<null>"}";

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
