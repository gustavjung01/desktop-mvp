using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Dashboard;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private const string BranchRead = "core.branch.read";
    private const string WarehouseRead = "core.warehouse.read";
    private const string LocationRead = "core.warehouse.location.read";
    private const string SalesReportingRead = "core.reporting.sales.read";
    private const string InventoryReportingRead = "core.reporting.inventory.read";
    private const string LogisticsReportingRead = "core.reporting.logistics.read";
    private const string AgingReportingRead = "core.reporting.aging.read";

    private readonly IDashboardService _service;
    private readonly IAccessStateService _access;
    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _generatedText = "Chưa có thời điểm cập nhật";
    private string _structureActiveText = "0 điểm đang hoạt động";

    public DashboardViewModel(IDashboardService service, IAccessStateService access)
    {
        _service = service;
        _access = access;
        _access.Changed += (_, _) =>
        {
            _loaded = false;
            RaiseNavigationPermissions();
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DashboardKpiRow> Kpis { get; } = [];
    public ObservableCollection<DashboardMeasurementRow> Measurements { get; } = [];
    public ObservableCollection<DashboardActivityRow> Activities { get; } = [];

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    public string Message
    {
        get => _message;
        private set
        {
            if (SetField(ref _message, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public string GeneratedText
    {
        get => _generatedText;
        private set => SetField(ref _generatedText, value);
    }

    public string StructureActiveText
    {
        get => _structureActiveText;
        private set => SetField(ref _structureActiveText, value);
    }

    public bool CanOpenCustomers => _access.CanNavigate("partners");
    public bool CanOpenSalesOrders => _access.CanNavigate("sales");
    public bool CanOpenInventory => _access.CanNavigate("inventory");

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || IsBusy || !_access.Current.IsAuthenticated) return;
        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;

        try
        {
            var branchesTask = LoadAsync(
                _access.HasPermission(BranchRead),
                "cơ cấu chi nhánh",
                _service.ListBranchesAsync);
            var warehousesTask = LoadAsync(
                _access.HasPermission(WarehouseRead),
                "cơ cấu kho",
                _service.ListWarehousesAsync);
            var locationsTask = LoadAsync(
                _access.HasPermission(LocationRead),
                "vị trí kho",
                _service.ListLocationsAsync);
            var salesTask = LoadAsync(
                _access.HasPermission(SalesReportingRead),
                "bán hàng",
                _service.GetSalesAsync);
            var inventoryTask = LoadAsync(
                _access.HasPermission(InventoryReportingRead),
                "tồn kho",
                _service.GetInventoryAsync);
            var logisticsTask = LoadAsync(
                _access.HasPermission(LogisticsReportingRead),
                "giao hàng",
                _service.GetLogisticsAsync);
            var agingTask = LoadAsync(
                _access.HasPermission(AgingReportingRead),
                "công nợ",
                _service.GetAgingAsync);

            await Task.WhenAll(branchesTask, warehousesTask, locationsTask, salesTask, inventoryTask, logisticsTask, agingTask)
                .ConfigureAwait(true);

            var branches = await branchesTask.ConfigureAwait(true);
            var warehouses = await warehousesTask.ConfigureAwait(true);
            var locations = await locationsTask.ConfigureAwait(true);
            var sales = await salesTask.ConfigureAwait(true);
            var inventory = await inventoryTask.ConfigureAwait(true);
            var logistics = await logisticsTask.ConfigureAwait(true);
            var aging = await agingTask.ConfigureAwait(true);

            Populate(branches, warehouses, locations, sales, inventory, logistics, aging);

            var errors = new[]
            {
                branches.ErrorLabel,
                warehouses.ErrorLabel,
                locations.ErrorLabel,
                sales.ErrorLabel,
                inventory.ErrorLabel,
                logistics.ErrorLabel,
                aging.ErrorLabel
            }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();

            if (errors.Length > 0)
            {
                Message = $"Chưa hiển thị được nhóm: {string.Join(", ", errors)}. Các nhóm còn lại vẫn hiển thị theo quyền hiện tại.";
                MessageIsError = true;
            }

            _loaded = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Populate(
        LoadResult<IReadOnlyList<BranchData>> branches,
        LoadResult<IReadOnlyList<WarehouseData>> warehouses,
        LoadResult<IReadOnlyList<WarehouseLocationData>> locations,
        LoadResult<DashboardSalesReportData> sales,
        LoadResult<InventoryReportingDashboardData> inventory,
        LoadResult<DashboardLogisticsData> logistics,
        LoadResult<DashboardAgingData> aging)
    {
        var unavailable = "Không khả dụng trong phạm vi hiện tại";
        var branchActive = branches.Data?.Count(item => item.IsActive) ?? 0;
        var warehouseActive = warehouses.Data?.Count(item => item.IsActive) ?? 0;
        var locationActive = locations.Data?.Count(item => item.IsActive) ?? 0;

        Kpis.Clear();
        Kpis.Add(new DashboardKpiRow(
            "branches",
            "Chi nhánh",
            branches.IsAllowed ? branchActive.ToString() : "—",
            branches.IsAllowed ? $"{branches.Data?.Count ?? 0} tổng cộng" : unavailable));
        Kpis.Add(new DashboardKpiRow(
            "warehouses",
            "Kho hàng",
            warehouses.IsAllowed ? warehouseActive.ToString() : "—",
            warehouses.IsAllowed ? $"{warehouses.Data?.Count ?? 0} tổng cộng" : unavailable));
        Kpis.Add(new DashboardKpiRow(
            "locations",
            "Vị trí kho",
            locations.IsAllowed ? locationActive.ToString() : "—",
            locations.IsAllowed ? $"{locations.Data?.Count ?? 0} tổng cộng" : unavailable));
        Kpis.Add(new DashboardKpiRow(
            "orders",
            "Đơn bán hiệu lực",
            sales.Data is null ? "—" : DashboardPresentation.Count(sales.Data.Summary.EffectiveOrderCount),
            sales.Data is null ? unavailable : DashboardPresentation.DateRange(sales.Data.Filters.From, sales.Data.Filters.To)));
        Kpis.Add(new DashboardKpiRow(
            "inventory",
            "Giá trị tồn kho",
            inventory.Data is null ? "—" : DashboardPresentation.MoneyCompact(inventory.Data.Summary.InventoryValueVnd),
            inventory.Data is null ? unavailable : $"{DashboardPresentation.Count(inventory.Data.Summary.StockedSkuCount)} SKU có tồn"));
        Kpis.Add(new DashboardKpiRow(
            "logistics",
            "Giao đủ đúng hạn",
            logistics.Data is null ? "—" : DashboardPresentation.Percent(logistics.Data.Summary.OnTimeFullRatePercent),
            logistics.Data is null ? unavailable : $"{DashboardPresentation.Count(logistics.Data.Summary.OnTimeEligibleFullCount)} phiếu đủ điều kiện SLA"));
        Kpis.Add(new DashboardKpiRow(
            "receivable",
            "Công nợ phải thu",
            aging.Data is null ? "—" : DashboardPresentation.SumReceivableVnd(aging.Data),
            aging.Data is null ? unavailable : "Số dư VND hiện còn phải thu"));

        Measurements.Clear();
        Measurements.Add(new DashboardMeasurementRow(
            "sales",
            "Bán hàng",
            "Đơn bán hiệu lực",
            sales.Data is null ? "—" : DashboardPresentation.Count(sales.Data.Summary.EffectiveOrderCount),
            sales.Data is null ? unavailable : $"{DashboardPresentation.Count(sales.Data.Summary.CancelledOrderCount)} đơn đã hủy",
            sales.Data is null ? unavailable : DashboardPresentation.DateRange(sales.Data.Filters.From, sales.Data.Filters.To)));
        Measurements.Add(new DashboardMeasurementRow(
            "inventory",
            "Tồn kho",
            "Giá trị tồn kho",
            inventory.Data is null ? "—" : DashboardPresentation.MoneyCompact(inventory.Data.Summary.InventoryValueVnd),
            inventory.Data is null ? unavailable : $"{DashboardPresentation.Count(inventory.Data.Summary.StockedSkuCount)} SKU có tồn",
            inventory.Data is null ? unavailable : $"{DashboardPresentation.Count(inventory.Data.Summary.CostingExceptionCount)} điểm cần kiểm tra giá vốn"));
        Measurements.Add(new DashboardMeasurementRow(
            "logistics",
            "Giao hàng",
            "Giao đủ đúng hạn",
            logistics.Data is null ? "—" : DashboardPresentation.Percent(logistics.Data.Summary.OnTimeFullRatePercent),
            logistics.Data is null ? unavailable : $"{DashboardPresentation.Count(logistics.Data.Summary.DeliveredFullCount)} phiếu giao đủ",
            logistics.Data is null ? unavailable : $"{DashboardPresentation.Count(logistics.Data.Summary.TripCount)} chuyến trong kỳ"));
        Measurements.Add(new DashboardMeasurementRow(
            "aging",
            "Công nợ",
            "Phải thu hiện tại",
            aging.Data is null ? "—" : DashboardPresentation.SumReceivableVnd(aging.Data),
            aging.Data is null ? unavailable : $"{DashboardPresentation.SumReceivableDocuments(aging.Data)} chứng từ còn dư",
            aging.Data is null ? unavailable : "Tổng hợp VND theo tuổi nợ hiện tại"));

        Activities.Clear();
        Activities.Add(new DashboardActivityRow("Đơn bán hiệu lực", DashboardPresentation.Count(sales.Data?.Summary.EffectiveOrderCount), "Trong kỳ"));
        Activities.Add(new DashboardActivityRow("Đơn bán đã hủy", DashboardPresentation.Count(sales.Data?.Summary.CancelledOrderCount), "Sau xác nhận / trong kỳ"));
        Activities.Add(new DashboardActivityRow("SKU có tồn", DashboardPresentation.Count(inventory.Data?.Summary.StockedSkuCount), "Tồn hiện tại"));
        Activities.Add(new DashboardActivityRow("Ngoại lệ giá vốn", DashboardPresentation.Count(inventory.Data?.Summary.CostingExceptionCount), "Cần đối soát"));
        Activities.Add(new DashboardActivityRow("Chuyến giao", DashboardPresentation.Count(logistics.Data?.Summary.TripCount), "Trong kỳ"));
        Activities.Add(new DashboardActivityRow("Phiếu giao đủ", DashboardPresentation.Count(logistics.Data?.Summary.DeliveredFullCount), "Kết quả hệ thống"));

        GeneratedText = DashboardPresentation.Generated([
            sales.Data?.GeneratedAt,
            inventory.Data?.GeneratedAt,
            logistics.Data?.GeneratedAt,
            aging.Data?.GeneratedAt
        ]);
        StructureActiveText = $"{branchActive + warehouseActive + locationActive} điểm đang hoạt động";
    }

    private static async Task<LoadResult<T>> LoadAsync<T>(
        bool allowed,
        string errorLabel,
        Func<CancellationToken, Task<T>> loader)
        where T : class
    {
        if (!allowed) return new LoadResult<T>(false, null, null);
        try
        {
            return new LoadResult<T>(true, await loader(CancellationToken.None).ConfigureAwait(false), null);
        }
        catch
        {
            return new LoadResult<T>(true, null, errorLabel);
        }
    }

    private void RaiseNavigationPermissions()
    {
        OnPropertyChanged(nameof(CanOpenCustomers));
        OnPropertyChanged(nameof(CanOpenSalesOrders));
        OnPropertyChanged(nameof(CanOpenInventory));
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

    private sealed record LoadResult<T>(bool IsAllowed, T? Data, string? ErrorLabel) where T : class;
}
