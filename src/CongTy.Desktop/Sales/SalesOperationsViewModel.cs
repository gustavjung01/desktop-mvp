using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed class SalesOperationsViewModel(
    ISalesOrderService salesOrders,
    IInternalOrganizationService organization,
    IAccessStateService access) : INotifyPropertyChanged
{
    private static readonly string[] OnboardingStatuses = ["submitted", "under_review", "need_more_info"];
    private bool _isBusy;
    private string _activeBranches = "—";
    private string _activeWarehouses = "—";
    private string _activeLocations = "—";
    private string _pendingWork = "0";
    private string _organizationError = string.Empty;
    private string _ordersError = string.Empty;
    private string _onboardingError = string.Empty;
    private string _message = string.Empty;
    private bool _messageIsError;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SalesOperationsOrderRow> DraftOrders { get; } = [];
    public ObservableCollection<SalesOperationsOnboardingRow> OnboardingQueue { get; } = [];

    public bool CanReadBranches => access.HasPermission("core.branch.read");
    public bool CanReadWarehouses => access.HasPermission("core.warehouse.read");
    public bool CanReadLocations => access.HasPermission("core.warehouse.location.read");
    public bool CanReadOrders => access.HasPermission("core.sales-order.read");
    public bool CanReadOnboarding => access.HasPermission("core.customer-onboarding.read");
    public bool CanRead => CanReadBranches || CanReadWarehouses || CanReadLocations || CanReadOrders || CanReadOnboarding;
    public bool CanRefresh => CanRead && !IsBusy;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanRefresh));
            OnPropertyChanged(nameof(LoadingText));
            OnPropertyChanged(nameof(OrdersEmptyText));
            OnPropertyChanged(nameof(OnboardingEmptyText));
        }
    }

    public string LoadingText => IsBusy ? "Đang cập nhật dữ liệu điều hành…" : string.Empty;
    public string ActiveBranches { get => _activeBranches; private set => SetField(ref _activeBranches, value); }
    public string ActiveWarehouses { get => _activeWarehouses; private set => SetField(ref _activeWarehouses, value); }
    public string ActiveLocations { get => _activeLocations; private set => SetField(ref _activeLocations, value); }
    public string PendingWork { get => _pendingWork; private set => SetField(ref _pendingWork, value); }

    public string OrganizationError
    {
        get => _organizationError;
        private set
        {
            if (SetField(ref _organizationError, value)) OnPropertyChanged(nameof(HasOrganizationError));
        }
    }

    public string OrdersError
    {
        get => _ordersError;
        private set
        {
            if (!SetField(ref _ordersError, value)) return;
            OnPropertyChanged(nameof(HasOrdersError));
            OnPropertyChanged(nameof(OrdersEmptyText));
        }
    }

    public string OnboardingError
    {
        get => _onboardingError;
        private set
        {
            if (!SetField(ref _onboardingError, value)) return;
            OnPropertyChanged(nameof(HasOnboardingError));
            OnPropertyChanged(nameof(OnboardingEmptyText));
        }
    }

    public bool HasOrganizationError => !string.IsNullOrWhiteSpace(OrganizationError);
    public bool HasOrdersError => !string.IsNullOrWhiteSpace(OrdersError);
    public bool HasOnboardingError => !string.IsNullOrWhiteSpace(OnboardingError);
    public string OrdersEmptyText => !IsBusy && !HasOrdersError && DraftOrders.Count == 0 ? "Không có đơn nháp trong danh sách gần nhất." : string.Empty;
    public string OnboardingEmptyText => !IsBusy && !HasOnboardingError && OnboardingQueue.Count == 0 ? "Không có đề nghị đang chờ trong danh sách gần nhất." : string.Empty;
    public string Message { get => _message; private set => SetField(ref _message, value); }
    public bool MessageIsError { get => _messageIsError; private set => SetField(ref _messageIsError, value); }

    public Task EnsureLoadedAsync() => RefreshAsync();

    public async Task RefreshAsync()
    {
        if (!CanRead)
        {
            ClearData();
            Message = "Tài khoản chưa được cấp quyền xem Điều hành bán hàng.";
            MessageIsError = true;
            return;
        }

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;
        OrganizationError = string.Empty;
        OrdersError = string.Empty;
        OnboardingError = string.Empty;

        try
        {
            var branchesTask = LoadAsync(CanReadBranches, organization.ListBranchesAsync);
            var warehousesTask = LoadAsync(CanReadWarehouses, organization.ListWarehousesAsync);
            var locationsTask = LoadAsync(CanReadLocations, organization.ListLocationsAsync);
            var ordersTask = LoadAsync(CanReadOrders, token => salesOrders.ListOperationsDraftsAsync(20, token));
            var onboardingTasks = OnboardingStatuses
                .Select(status => LoadAsync(CanReadOnboarding, token => salesOrders.ListOperationsCustomerOnboardingAsync(status, 20, token)))
                .ToArray();

            var pending = new List<Task> { branchesTask, warehousesTask, locationsTask, ordersTask };
            pending.AddRange(onboardingTasks);
            await Task.WhenAll(pending).ConfigureAwait(true);

            ApplyOrganization(await branchesTask, await warehousesTask, await locationsTask);
            ApplyOrders(await ordersTask);

            var onboardingResults = new List<LoadResult<CustomerOnboardingRequestData>>();
            foreach (var task in onboardingTasks) onboardingResults.Add(await task);
            ApplyOnboarding(onboardingResults);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyOrganization(LoadResult<BranchData> branches, LoadResult<WarehouseData> warehouses, LoadResult<WarehouseLocationData> locations)
    {
        var unavailable = new List<string>();
        ActiveBranches = Metric(branches, item => item.IsActive, "chi nhánh", unavailable);
        ActiveWarehouses = Metric(warehouses, item => item.IsActive, "kho", unavailable);
        ActiveLocations = Metric(locations, item => item.IsActive, "vị trí kho", unavailable);
        OrganizationError = unavailable.Count == 0 ? string.Empty : $"Chưa tải được số liệu {string.Join(", ", unavailable)}.";
    }

    private void ApplyOrders(LoadResult<SalesOrderData> result)
    {
        DraftOrders.Clear();
        if (!result.Available)
        {
            OrdersError = "Không tải được đơn bán hàng đang chờ xác nhận";
        }
        else
        {
            foreach (var row in result.Items.Take(20).Select(SalesOperationsPresentation.OrderRow)) DraftOrders.Add(row);
        }
        RebuildPendingWork();
        OnPropertyChanged(nameof(OrdersEmptyText));
    }

    private void ApplyOnboarding(IReadOnlyList<LoadResult<CustomerOnboardingRequestData>> results)
    {
        OnboardingQueue.Clear();
        var failed = results.Count(result => !result.Available);
        var requests = results.Where(result => result.Available).SelectMany(result => result.Items)
            .OrderByDescending(request => SalesOperationsPresentation.SortTime(request.UpdatedAt)).Take(20);
        foreach (var row in requests.Select(SalesOperationsPresentation.OnboardingRow)) OnboardingQueue.Add(row);

        OnboardingError = failed switch
        {
            0 => string.Empty,
            var count when count == results.Count => "Không tải được danh sách đề nghị mở mã khách hàng",
            _ => "Một phần danh sách đề nghị mở mã khách hàng chưa tải được"
        };
        RebuildPendingWork();
        OnPropertyChanged(nameof(OnboardingEmptyText));
    }

    private static string Metric<T>(LoadResult<T> result, Func<T, bool> predicate, string label, ICollection<string> unavailable)
    {
        if (result.Available) return result.Items.Count(predicate).ToString("N0");
        unavailable.Add(label);
        return "—";
    }

    private void RebuildPendingWork() => PendingWork = (DraftOrders.Count + OnboardingQueue.Count).ToString("N0");

    private static async Task<LoadResult<T>> LoadAsync<T>(bool allowed, Func<CancellationToken, Task<IReadOnlyList<T>>> loader)
    {
        if (!allowed) return LoadResult<T>.Unavailable;
        try { return new LoadResult<T>(true, await loader(CancellationToken.None).ConfigureAwait(false)); }
        catch { return LoadResult<T>.Unavailable; }
    }

    private void ClearData()
    {
        DraftOrders.Clear();
        OnboardingQueue.Clear();
        ActiveBranches = "—";
        ActiveWarehouses = "—";
        ActiveLocations = "—";
        PendingWork = "0";
        OrganizationError = string.Empty;
        OrdersError = string.Empty;
        OnboardingError = string.Empty;
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

    private sealed record LoadResult<T>(bool Available, IReadOnlyList<T> Items)
    {
        public static LoadResult<T> Unavailable { get; } = new(false, Array.Empty<T>());
    }
}
