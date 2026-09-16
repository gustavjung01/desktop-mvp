using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed class InventoryViewModel : INotifyPropertyChanged
{
    private const int HistoryPageSize = 50;
    private readonly IInventoryService _service;
    private readonly IAccessStateService _access;
    private readonly List<InventoryBalanceData> _balanceSource = [];
    private readonly List<InventoryLotData> _lotSource = [];
    private InventoryReportingDashboardData? _report;
    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private string _balanceSearch = string.Empty;
    private string _lotSearch = string.Empty;
    private int _mainTabIndex;
    private int _lookupTabIndex;
    private int _reportTabIndex;
    private InventoryBalanceRow? _selectedBalance;
    private InventoryHistoryRow? _selectedHistory;
    private bool _isHistoryDetailOpen;
    private int _historyPage;
    private bool _historyHasNext;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _selectedWarehouseId = string.Empty;
    private int _slowDays = 90;
    private bool _hasReport;
    private string _reportError = string.Empty;
    private bool _isHoldDetailOpen;
    private bool _isHoldBusy;
    private string _holdSummary = string.Empty;
    private string _holdStatus = string.Empty;

    public InventoryViewModel(IInventoryService service, IAccessStateService access)
    {
        _service = service;
        _access = access;
        _access.Changed += (_, _) => RunOnUiThread(RaisePermissions);
        WarehouseOptions.Add(new InventoryWarehouseOption(string.Empty, "Tất cả kho được cấp quyền"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<InventoryBalanceRow> Balances { get; } = [];
    public ObservableCollection<InventoryHistoryRow> HistoryRows { get; } = [];
    public ObservableCollection<InventoryLotRow> Lots { get; } = [];
    public ObservableCollection<InventoryWarehouseOption> WarehouseOptions { get; } = [];
    public ObservableCollection<InventoryWarehouseSummaryRow> WarehouseSummaryRows { get; } = [];
    public ObservableCollection<InventoryPositionRow> PositionRows { get; } = [];
    public ObservableCollection<InventoryFlowRow> FlowRows { get; } = [];
    public ObservableCollection<InventoryMovementTypeRow> MovementTypeRows { get; } = [];
    public ObservableCollection<InventorySlowRow> SlowRows { get; } = [];
    public ObservableCollection<InventoryExpiryRow> ExpiryRows { get; } = [];
    public ObservableCollection<InventoryExceptionRow> ExceptionRows { get; } = [];
    public ObservableCollection<InventoryHoldOrderRow> HoldOrders { get; } = [];

    public IReadOnlyList<InventorySlowDayOption> SlowDayOptions { get; } =
    [
        new(30, "30 ngày"), new(60, "60 ngày"), new(90, "90 ngày"),
        new(120, "120 ngày"), new(180, "180 ngày"), new(365, "365 ngày")
    ];

    public bool CanReadBalances => _access.HasPermission("core.inventory.read");
    public bool CanReadLots => _access.HasPermission("core.inventory.lot.read");
    public bool CanReadReporting => _access.HasPermission("core.reporting.inventory.read");
    public bool CanExportReport => CanReadReporting && _access.HasPermission("core.reporting.export") && _report is not null && IsNotBusy;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                OnPropertyChanged(nameof(IsReportLoading));
                OnPropertyChanged(nameof(CanExportReport));
            }
        }
    }

    public bool IsNotBusy => !IsBusy;
    public bool HasReport
    {
        get => _hasReport;
        private set
        {
            if (SetField(ref _hasReport, value))
            {
                OnPropertyChanged(nameof(IsReportLoading));
                OnPropertyChanged(nameof(CanExportReport));
            }
        }
    }
    public bool IsReportLoading => IsBusy && !HasReport;
    public string ReportError
    {
        get => _reportError;
        private set
        {
            if (SetField(ref _reportError, value ?? string.Empty))
                OnPropertyChanged(nameof(HasReportError));
        }
    }
    public bool HasReportError => !string.IsNullOrWhiteSpace(ReportError);
    public bool IsHoldDetailOpen { get => _isHoldDetailOpen; private set => SetField(ref _isHoldDetailOpen, value); }
    public bool IsHoldBusy { get => _isHoldBusy; private set => SetField(ref _isHoldBusy, value); }
    public string HoldSummary { get => _holdSummary; private set => SetField(ref _holdSummary, value ?? string.Empty); }
    public string HoldStatus { get => _holdStatus; private set => SetField(ref _holdStatus, value ?? string.Empty); }

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public int MainTabIndex
    {
        get => _mainTabIndex;
        set => SetField(ref _mainTabIndex, Math.Clamp(value, 0, 2));
    }

    public int LookupTabIndex
    {
        get => _lookupTabIndex;
        set => SetField(ref _lookupTabIndex, Math.Clamp(value, 0, 1));
    }

    public int ReportTabIndex
    {
        get => _reportTabIndex;
        set
        {
            if (SetField(ref _reportTabIndex, Math.Clamp(value, 0, 5)))
                OnPropertyChanged(nameof(ExportDescription));
        }
    }

    public string BalanceSearch
    {
        get => _balanceSearch;
        set
        {
            if (SetField(ref _balanceSearch, value ?? string.Empty)) ApplyBalanceFilter();
        }
    }

    public string LotSearch
    {
        get => _lotSearch;
        set
        {
            if (SetField(ref _lotSearch, value ?? string.Empty)) ApplyLotFilter();
        }
    }

    public InventoryBalanceRow? SelectedBalance
    {
        get => _selectedBalance;
        set
        {
            if (SetField(ref _selectedBalance, value))
            {
                OnPropertyChanged(nameof(HistoryContext));
                OnPropertyChanged(nameof(CanOpenHistory));
            }
        }
    }

    public InventoryHistoryRow? SelectedHistory
    {
        get => _selectedHistory;
        set => SetField(ref _selectedHistory, value);
    }

    public bool IsHistoryDetailOpen
    {
        get => _isHistoryDetailOpen;
        private set => SetField(ref _isHistoryDetailOpen, value);
    }

    public bool CanOpenHistory => SelectedBalance is not null && CanReadBalances && IsNotBusy;
    public bool HistoryHasPrevious => _historyPage > 0 && SelectedBalance is not null;
    public bool HistoryHasNext => _historyHasNext && SelectedBalance is not null;
    public string HistoryPageText => $"Trang {_historyPage + 1}";
    public string HistoryContext => SelectedBalance is null
        ? "Chọn “Xem lịch sử” tại tab Tồn kho để tra cứu biến động của một SKU theo kho."
        : $"Đang xem SKU {SelectedBalance.Data.BaseSku} tại kho {SelectedBalance.Data.WarehouseCode} · {SelectedBalance.Data.WarehouseName}.";

    public DateTime? FromDate
    {
        get => _fromDate;
        set { if (SetField(ref _fromDate, value)) OnPropertyChanged(nameof(PeriodFlowDescription)); }
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set { if (SetField(ref _toDate, value)) OnPropertyChanged(nameof(PeriodFlowDescription)); }
    }

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set => SetField(ref _selectedWarehouseId, value ?? string.Empty);
    }

    public int SlowDays
    {
        get => _slowDays;
        set
        {
            if (SetField(ref _slowDays, Math.Clamp(value, 30, 365)))
                OnPropertyChanged(nameof(SlowMovingDescription));
        }
    }

    public string PeriodFlowDescription =>
        $"{FromDate:dd/MM/yyyy} → {ToDate:dd/MM/yyyy}. Số lượng chỉ so sánh trong cùng mã hàng; nguồn là sổ nhập xuất kho không sửa ngược.";
    public string SlowMovingDescription =>
        $"Hàng còn tồn và không phát sinh xuất kho trong {SlowDays} ngày gần nhất; mặt hàng chưa từng xuất được ghi riêng.";

    public string StockedSkuCount { get; private set; } = "0";
    public string StockPositionCount { get; private set; } = "0";
    public string ReservedPositionCount { get; private set; } = "0";
    public string LotScopeCount { get; private set; } = "0";
    public string InventoryValue { get; private set; } = "—";
    public string CostingExceptionCount { get; private set; } = "0";
    public string ReportStatus { get; private set; } = "Chưa tải báo cáo.";
    public string ExportDescription => ReportTabIndex switch
    {
        0 => "Tổng quan",
        1 => "Tồn hiện tại",
        2 => "Luân chuyển",
        3 => "Chậm luân chuyển",
        4 => "Lô & hạn dùng",
        _ => "Cần kiểm tra"
    };

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await RefreshAllAsync().ConfigureAwait(true);
        _loaded = true;
    }

    public Task RefreshAllAsync() => RunBusyAsync(async () =>
    {
        if (CanReadBalances) await LoadBalancesAsync().ConfigureAwait(true);
        if (CanReadReporting) await LoadReportAsync().ConfigureAwait(true);
        if (CanReadLots) await LoadLotsAsync().ConfigureAwait(true);
        Message = "Dữ liệu Kho/Tồn kho đã được làm mới.";
    });

    public Task RefreshBalancesAsync() => RunBusyAsync(async () =>
    {
        await LoadBalancesAsync().ConfigureAwait(true);
        Message = $"Đã làm mới {Balances.Count} dòng tồn kho.";
    });

    public Task RefreshLotsAsync() => RunBusyAsync(async () =>
    {
        await LoadLotsAsync().ConfigureAwait(true);
        Message = $"Đã làm mới {Lots.Count} lô hàng.";
    });

    public Task RefreshReportAsync() => RunBusyAsync(async () =>
    {
        await LoadReportAsync().ConfigureAwait(true);
        Message = "Báo cáo tồn kho đã được làm mới.";
    });

    public async Task ResetReportAsync()
    {
        FromDate = null;
        ToDate = null;
        SelectedWarehouseId = string.Empty;
        SlowDays = 90;
        await RefreshReportAsync().ConfigureAwait(true);
    }

    public Task OpenHistoryAsync(InventoryBalanceRow? row = null) => RunBusyAsync(async () =>
    {
        if (row is not null) SelectedBalance = row;
        if (SelectedBalance is null) return;
        _historyPage = 0;
        await LoadHistoryPageAsync().ConfigureAwait(true);
        LookupTabIndex = 1;
        RaiseHistoryPaging();
    });

    public Task RefreshHistoryAsync() => RunBusyAsync(async () =>
    {
        if (SelectedBalance is null) return;
        await LoadHistoryPageAsync().ConfigureAwait(true);
        RaiseHistoryPaging();
    });

    public Task HistoryPreviousAsync() => RunBusyAsync(async () =>
    {
        if (SelectedBalance is null || _historyPage <= 0) return;
        _historyPage--;
        await LoadHistoryPageAsync().ConfigureAwait(true);
        RaiseHistoryPaging();
    });

    public Task HistoryNextAsync() => RunBusyAsync(async () =>
    {
        if (SelectedBalance is null || !_historyHasNext) return;
        _historyPage++;
        await LoadHistoryPageAsync().ConfigureAwait(true);
        RaiseHistoryPaging();
    });

    public void OpenHistoryDetail(InventoryHistoryRow row)
    {
        SelectedHistory = row;
        IsHistoryDetailOpen = true;
    }

    public void CloseHistoryDetail() => IsHistoryDetailOpen = false;

    public async Task OpenReportHoldsAsync(InventoryPositionRow? row)
    {
        if (row is null || IsHoldBusy) return;
        IsHoldDetailOpen = true;
        IsHoldBusy = true;
        HoldOrders.Clear();
        HoldSummary = $"Tổng đang giữ: {row.Reserved}{(string.IsNullOrWhiteSpace(row.BaseUnit) ? string.Empty : $" {row.BaseUnit}")}";
        HoldStatus = string.Empty;
        try
        {
            var data = await _service.GetHoldBreakdownAsync(row.WarehouseId, row.VariantId).ConfigureAwait(true);
            var fallbackUnit = row.BaseUnit;
            HoldSummary = $"Tổng đang giữ: {InventoryPresentation.Quantity(data.HeldBaseQuantity)}{(string.IsNullOrWhiteSpace(fallbackUnit) ? string.Empty : $" {fallbackUnit}")}";
            Replace(HoldOrders, data.Orders.Select((order, index) =>
            {
                var unit = InventoryPresentation.First(order.BaseUnitName, order.BaseUnitCode, fallbackUnit);
                var sku = order.BaseSku == order.SalesSku || string.IsNullOrWhiteSpace(order.BaseSku) ? order.SalesSku : $"{order.SalesSku} → {order.BaseSku}";
                return new InventoryHoldOrderRow(
                    index + 1,
                    InventoryPresentation.First(order.OrderNumber, "—"),
                    InventoryPresentation.First(order.CustomerName, "Khách hàng"),
                    sku,
                    InventoryPresentation.First(order.WarehouseCode, order.WarehouseName, "—"),
                    InventoryPresentation.HoldFlow(order.DeliveryMode, order.DeliveryExecutionMode),
                    $"{InventoryPresentation.Quantity(order.HeldBaseQuantity)}{(string.IsNullOrWhiteSpace(unit) ? string.Empty : $" {unit}")}");
            }));
            HoldStatus = HoldOrders.Count == 0 ? "Không có đơn khác đang giữ hàng." : string.Empty;
        }
        catch (CanonicalApiException exception)
        {
            HoldStatus = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(exception), exception.RequestId);
        }
        catch (Exception exception)
        {
            HoldStatus = exception.Message;
        }
        finally { IsHoldBusy = false; }
    }

    public void CloseReportHolds()
    {
        IsHoldDetailOpen = false;
        HoldStatus = string.Empty;
        HoldOrders.Clear();
    }

    public string BuildReportExportCsv()
    {
        if (_report is null) throw new InvalidOperationException("Chưa có báo cáo tồn kho để xuất.");
        if (!CanExportReport) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xuất báo cáo.");

        var lines = new List<string>();
        void Header(params string[] cells) => lines.Add(string.Join(';', cells.Select(Csv)));
        void Row(params string[] cells) => lines.Add(string.Join(';', cells.Select(Csv)));

        switch (ReportTabIndex)
        {
            case 0:
                Header("STT", "Kho", "Mã hàng có tồn", "Mã hàng có giữ", "Giá trị", "Cần kiểm tra", "Cập nhật tồn");
                foreach (var x in WarehouseSummaryRows) Row(x.Stt.ToString(), x.Warehouse, x.StockedSku, x.ReservedSku, x.Value, x.Exceptions, x.UpdatedAt);
                break;
            case 1:
                Header("STT", "Kho", "Sản phẩm / mã hàng", "Tồn kho", "Quy đổi", "Đã giữ", "Có thể xuất", "Giá trị", "Giá bình quân", "Tình trạng giá vốn");
                foreach (var x in PositionRows) Row(x.Stt.ToString(), x.Warehouse, x.ProductSku, x.OnHand, x.PackageBreakdown, x.Reserved, x.Available, x.Value, x.AverageCost, x.CostingStatus);
                break;
            case 2:
                Header("STT", "Kho", "Sản phẩm / mã hàng", "Đầu kỳ", "Nhập", "Xuất", "Cuối kỳ", "Dòng nghiệp vụ");
                foreach (var x in FlowRows) Row(x.Stt.ToString(), x.Warehouse, x.ProductSku, x.Opening, x.Inbound, x.Outbound, x.Closing, x.Lines);
                lines.Add(string.Empty);
                Header("STT", "Loại nghiệp vụ", "Chứng từ", "Dòng", "Mã hàng");
                foreach (var x in MovementTypeRows) Row(x.Stt.ToString(), x.Movement, x.Documents, x.Lines, x.Skus);
                break;
            case 3:
                Header("STT", "Kho", "Sản phẩm / mã hàng", "Tồn kho", "Có thể xuất", "Lần xuất cuối", "Số ngày", "Giá trị");
                foreach (var x in SlowRows) Row(x.Stt.ToString(), x.Warehouse, x.ProductSku, x.OnHand, x.Available, x.LastOut, x.Days, x.Value);
                break;
            case 4:
                Header("STT", "Kho", "Sản phẩm / mã hàng", "Lô", "Ngày sản xuất", "Hạn dùng", "Tồn kho", "Có thể xuất", "Trạng thái");
                foreach (var x in ExpiryRows) Row(x.Stt.ToString(), x.Warehouse, x.ProductSku, x.Lot, x.Manufactured, x.Expiry, x.OnHand, x.Available, x.Status);
                break;
            default:
                Header("STT", "Kho", "Sản phẩm / mã hàng", "Số lượng sổ kho", "Số lượng tính giá", "Chênh lệch", "Trạng thái", "Số cảnh báo");
                foreach (var x in ExceptionRows) Row(x.Stt.ToString(), x.Warehouse, x.ProductSku, x.Ledger, x.Costing, x.Difference, x.Status, x.Alerts);
                break;
        }

        return string.Join(Environment.NewLine, lines);
    }

    private async Task LoadBalancesAsync()
    {
        if (!CanReadBalances) return;
        var rows = await _service.ListBalancesAsync().ConfigureAwait(true);
        _balanceSource.Clear();
        _balanceSource.AddRange(rows.Where(HasDisplayableBalance));
        ApplyBalanceFilter();
        if (SelectedBalance is not null)
            SelectedBalance = Balances.FirstOrDefault(x => SameBalance(x.Data, SelectedBalance.Data));
    }

    private async Task LoadLotsAsync()
    {
        if (!CanReadLots) return;
        var rows = await _service.ListLotsAsync().ConfigureAwait(true);
        _lotSource.Clear();
        _lotSource.AddRange(rows);
        ApplyLotFilter();
    }

    private async Task LoadHistoryPageAsync()
    {
        if (!CanReadBalances || SelectedBalance is null) return;
        var rows = await _service.ListHistoryAsync(
            SelectedBalance.Data.WarehouseId,
            SelectedBalance.Data.BaseVariantId,
            _historyPage).ConfigureAwait(true);
        _historyHasNext = rows.Count > HistoryPageSize;
        Replace(HistoryRows, rows.Take(HistoryPageSize).Select((x, index) => new InventoryHistoryRow(
            index + 1 + _historyPage * HistoryPageSize,
            InventoryPresentation.DateTimeText(x.PostedAt),
            InventoryPresentation.ReportMovement(x.MovementType),
            InventoryPresentation.First(x.SourceDocumentNumber, x.DocumentNumber, "—"),
            InventoryPresentation.QuantityWithUnit(x.BaseQuantityDelta, SelectedBalance.Data),
            InventoryPresentation.QuantityWithUnit(x.StockAfter, SelectedBalance.Data),
            InventoryPresentation.First(x.LocationSummary, x.LotSummary, "—"),
            InventoryPresentation.First(x.PostedByName, x.PostedBy, "—"),
            InventoryPresentation.First(x.ReasonNote, "—"),
            x)));
        Message = $"Đã tải {HistoryRows.Count} lần biến động của {SelectedBalance.Data.BaseSku}.";
    }

    private async Task LoadReportAsync()
    {
        if (!CanReadReporting) return;
        ReportError = string.Empty;
        ReportStatus = "Đang tải báo cáo tồn kho…";
        OnPropertyChanged(nameof(ReportStatus));
        InventoryReportingDashboardData report;
        try
        {
            report = await _service.GetReportAsync(FromDate, ToDate, SelectedWarehouseId, SlowDays).ConfigureAwait(true);
        }
        catch (CanonicalApiException exception)
        {
            ReportError = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(exception), exception.RequestId);
            ReportStatus = ReportError;
            OnPropertyChanged(nameof(ReportStatus));
            throw;
        }
        catch (Exception exception)
        {
            ReportError = exception.Message;
            ReportStatus = ReportError;
            OnPropertyChanged(nameof(ReportStatus));
            throw;
        }
        _report = report;
        HasReport = true;

        if (DateTime.TryParse(report.Filters.From, out var from)) FromDate = from;
        if (DateTime.TryParse(report.Filters.To, out var to)) ToDate = to;
        SlowDays = report.Filters.SlowDays;

        var selectedWarehouse = SelectedWarehouseId;
        if (WarehouseOptions.Count <= 1 || string.IsNullOrWhiteSpace(selectedWarehouse))
        {
            Replace(WarehouseOptions,
            [
                new InventoryWarehouseOption(string.Empty, "Tất cả kho được cấp quyền"),
                .. report.WarehouseSummary.GroupBy(x => x.WarehouseId).Select(group => group.First())
                    .Select(x => new InventoryWarehouseOption(x.WarehouseId, $"{x.WarehouseCode} — {x.WarehouseName}"))
            ]);
        }
        SelectedWarehouseId = WarehouseOptions.Any(x => x.Id == selectedWarehouse) ? selectedWarehouse : string.Empty;

        StockedSkuCount = InventoryPresentation.Quantity(report.Summary.StockedSkuCount);
        StockPositionCount = InventoryPresentation.Quantity(report.Summary.StockPositionCount);
        ReservedPositionCount = InventoryPresentation.Quantity(report.Summary.ReservedPositionCount);
        LotScopeCount = InventoryPresentation.Quantity(report.Summary.LotScopeCount);
        InventoryValue = InventoryPresentation.Money(report.Summary.InventoryValueVnd);
        CostingExceptionCount = InventoryPresentation.Quantity(report.Summary.CostingExceptionCount);
        RaiseReportSummary();

        Replace(WarehouseSummaryRows, report.WarehouseSummary.Select((x, i) => new InventoryWarehouseSummaryRow(
            i + 1, $"{x.WarehouseCode}{Environment.NewLine}{x.WarehouseName}",
            InventoryPresentation.Quantity(x.StockedSkuCount), InventoryPresentation.Quantity(x.ReservedSkuCount),
            InventoryPresentation.Money(x.InventoryValueVnd), InventoryPresentation.Quantity(x.CostingExceptionCount),
            InventoryPresentation.DateTimeText(x.QuantityProjectedThrough))));

        Replace(PositionRows, report.CurrentPositions.Select((x, i) =>
        {
            var metadata = ProductMetadata(x.VariantId);
            return new InventoryPositionRow(
                i + 1, x.WarehouseCode, ProductLabel(x.VariantId, x.Sku),
                InventoryPresentation.Quantity(x.OnHandQuantity),
                InventoryPresentation.PackageBreakdown(x.OnHandQuantity, metadata),
                InventoryPresentation.Quantity(x.ReservedQuantity),
                InventoryPresentation.Quantity(x.AvailableQuantity), InventoryPresentation.Money(x.InventoryValue),
                InventoryPresentation.Money(x.AverageUnitCost), InventoryPresentation.CostingStatus(x.CostingStatus),
                x.WarehouseId, x.VariantId, InventoryPresentation.Unit(metadata));
        }));

        Replace(FlowRows, report.PeriodFlow.Select((x, i) => new InventoryFlowRow(
            i + 1, x.WarehouseCode, ProductLabel(x.VariantId, x.Sku),
            InventoryPresentation.Quantity(x.OpeningQuantity), InventoryPresentation.Quantity(x.InboundQuantity),
            InventoryPresentation.Quantity(x.OutboundQuantity), InventoryPresentation.Quantity(x.ClosingQuantity),
            InventoryPresentation.Quantity(x.MovementLineCount))));

        Replace(MovementTypeRows, report.MovementTypes.Select((x, i) => new InventoryMovementTypeRow(
            i + 1, InventoryPresentation.Movement(x.MovementType), InventoryPresentation.Quantity(x.MovementCount),
            InventoryPresentation.Quantity(x.MovementLineCount), InventoryPresentation.Quantity(x.SkuCount))));

        Replace(SlowRows, report.SlowMoving.Select((x, i) => new InventorySlowRow(
            i + 1, x.WarehouseCode, ProductLabel(x.VariantId, x.Sku),
            InventoryPresentation.Quantity(x.OnHandQuantity), InventoryPresentation.Quantity(x.AvailableQuantity),
            x.NeverOutbound ? "Chưa từng xuất" : InventoryPresentation.Date(x.LastOutDate),
            InventoryPresentation.First(x.DaysSinceOutbound, "—"), InventoryPresentation.Money(x.InventoryValueVnd))));

        Replace(ExpiryRows, report.ExpiryLots.Select((x, i) => new InventoryExpiryRow(
            i + 1, x.WarehouseCode, ProductLabel(x.VariantId, x.Sku), x.LotCode,
            InventoryPresentation.DateWithAge(x.ManufacturedDate, x.ManufacturedAgeDays), InventoryPresentation.Date(x.ExpiryDate),
            InventoryPresentation.Quantity(x.OnHandQuantity), InventoryPresentation.Quantity(x.AvailableQuantity),
            InventoryPresentation.ExpiryStatus(x.ExpiryBucket))));

        Replace(ExceptionRows, report.Exceptions.Select((x, i) => new InventoryExceptionRow(
            i + 1, x.WarehouseCode, ProductLabel(x.VariantId, x.Sku),
            InventoryPresentation.Quantity(x.LedgerQuantity), InventoryPresentation.Quantity(x.CostingQuantity),
            InventoryPresentation.Quantity(x.QuantityDifference), InventoryPresentation.ReconciliationStatus(x.ReconciliationStatus),
            InventoryPresentation.Quantity(x.AnomalyCount))));

        ReportStatus =
            $"Cập nhật dữ liệu: sổ kho {InventoryPresentation.DateTimeText(report.ProjectionState.LedgerThrough)} · " +
            $"Tồn kho {InventoryPresentation.DateTimeText(report.ProjectionState.QuantityProjectedThrough)} · " +
            $"Giá vốn {InventoryPresentation.DateTimeText(report.ProjectionState.CostingProjectedThrough)} · " +
            (report.ProjectionState.QuantityProjectionStale
                ? "Tồn kho đang chậm cập nhật so với sổ kho."
                : "Tồn kho đã cập nhật theo sổ kho.");
        OnPropertyChanged(nameof(ReportStatus));
        OnPropertyChanged(nameof(CanExportReport));
    }

    private void ApplyBalanceFilter()
    {
        var term = BalanceSearch.Trim();
        var rows = _balanceSource.Where(x => string.IsNullOrWhiteSpace(term) || BalanceSearchText(x).Contains(term, StringComparison.CurrentCultureIgnoreCase))
            .Select((x, index) => ToBalanceRow(x, index + 1));
        Replace(Balances, rows);
    }

    private void ApplyLotFilter()
    {
        var term = LotSearch.Trim();
        var rows = _lotSource.Where(x => string.IsNullOrWhiteSpace(term) || LotSearchText(x).Contains(term, StringComparison.CurrentCultureIgnoreCase))
            .Select((x, index) => new InventoryLotRow(
                index + 1,
                InventoryPresentation.ProductSku(x.ProductName, x.BaseSku),
                x.LotCode,
                InventoryPresentation.Date(x.ManufacturedDate),
                InventoryPresentation.Date(x.ExpiryDate),
                InventoryPresentation.First(x.SupplierLotReference, "—"),
                InventoryPresentation.DateTimeText(x.CreatedAt),
                LotSearchText(x)));
        Replace(Lots, rows);
    }

    private InventoryBalanceRow ToBalanceRow(InventoryBalanceData x, int index)
    {
        var warehouseLocation = string.IsNullOrWhiteSpace(x.LocationCode)
            ? $"{x.WarehouseCode} · {x.WarehouseName}"
            : $"{x.WarehouseCode} · {x.WarehouseName}{Environment.NewLine}{x.LocationCode} · {InventoryPresentation.First(x.LocationName, "Vị trí")}";
        return new InventoryBalanceRow(
            index,
            warehouseLocation,
            InventoryPresentation.ProductSku(x.ProductName, x.BaseSku),
            InventoryPresentation.First(x.LotCode, "—"),
            InventoryPresentation.Date(x.ExpiryDate),
            InventoryPresentation.QuantityWithUnit(x.OnHandQuantity, x),
            InventoryPresentation.QuantityWithUnit(x.ReservedQuantity, x),
            InventoryPresentation.QuantityWithUnit(x.AvailableQuantity, x),
            InventoryPresentation.PackageBreakdown(x),
            BalanceSearchText(x),
            x);
    }

    private InventoryBalanceData? ProductMetadata(string variantId) =>
        _balanceSource.FirstOrDefault(x => x.BaseVariantId == variantId);

    private string ProductLabel(string variantId, string sku) =>
        InventoryPresentation.ProductSkuWithUnit(ProductMetadata(variantId), sku);

    private static bool HasDisplayableBalance(InventoryBalanceData row) =>
        !string.Equals(InventoryPresentation.Quantity(row.OnHandQuantity), "0", StringComparison.Ordinal)
        || !string.Equals(InventoryPresentation.Quantity(row.ReservedQuantity), "0", StringComparison.Ordinal);

    private static bool SameBalance(InventoryBalanceData a, InventoryBalanceData b) =>
        a.WarehouseId == b.WarehouseId
        && a.LocationId == b.LocationId
        && a.BaseVariantId == b.BaseVariantId
        && a.LotId == b.LotId;

    private static string BalanceSearchText(InventoryBalanceData x) =>
        string.Join(' ', new[] { x.ProductName, x.ProductCode, x.BaseSku, x.PackageSku, x.WarehouseCode, x.WarehouseName, x.LocationCode, x.LocationName, x.LotCode, x.ExpiryDate }.Where(v => !string.IsNullOrWhiteSpace(v)));

    private static string LotSearchText(InventoryLotData x) =>
        string.Join(' ', new[] { x.ProductName, x.ProductCode, x.BaseSku, x.LotCode, x.NormalizedLotCode, x.ExpiryDate, x.SupplierLotReference }.Where(v => !string.IsNullOrWhiteSpace(v)));

    private async Task RunBusyAsync(Func<Task> work)
    {
        if (IsBusy) return;
        IsBusy = true;
        Message = string.Empty;
        try
        {
            await work().ConfigureAwait(true);
        }
        catch (CanonicalApiException exception)
        {
            Message = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(exception), exception.RequestId);
        }
        catch (Exception exception)
        {
            Message = exception.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanOpenHistory));
        }
    }

    private void RaiseHistoryPaging()
    {
        OnPropertyChanged(nameof(HistoryHasPrevious));
        OnPropertyChanged(nameof(HistoryHasNext));
        OnPropertyChanged(nameof(HistoryPageText));
        OnPropertyChanged(nameof(HistoryContext));
    }

    private void RaiseReportSummary()
    {
        OnPropertyChanged(nameof(StockedSkuCount));
        OnPropertyChanged(nameof(StockPositionCount));
        OnPropertyChanged(nameof(ReservedPositionCount));
        OnPropertyChanged(nameof(LotScopeCount));
        OnPropertyChanged(nameof(InventoryValue));
        OnPropertyChanged(nameof(CostingExceptionCount));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanReadBalances));
        OnPropertyChanged(nameof(CanReadLots));
        OnPropertyChanged(nameof(CanReadReporting));
        OnPropertyChanged(nameof(CanExportReport));
        OnPropertyChanged(nameof(CanOpenHistory));
    }

    private static string Csv(string value)
    {
        var normalized = (value ?? string.Empty).Replace(Environment.NewLine, " ", StringComparison.Ordinal);
        var quote = '"';
        return string.Concat(quote, normalized.Replace(quote.ToString(), new string(quote, 2), StringComparison.Ordinal), quote);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> rows)
    {
        target.Clear();
        foreach (var row in rows) target.Add(row);
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
