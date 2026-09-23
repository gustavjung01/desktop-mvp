using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed partial class SalesReportingViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.reporting.sales.read";
    private const string ExportPermission = "core.reporting.export";

    private readonly ISalesReportingService _service;
    private readonly IAccessStateService _access;
    private readonly ISalesReportingViewStateStore _viewStateStore;
    private SalesReportingDashboardData? _report;
    private bool _loaded;
    private long _accessGeneration;
    private long _loadGeneration;
    private long _exportGeneration;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _exportCts;
    private bool _isBusy;
    private bool _isExporting;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _reportError = string.Empty;
    private string _exportError = string.Empty;
    private string _savedNotice = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private DateTime? _appliedFrom;
    private DateTime? _appliedTo;
    private string _selectedWarehouseId = string.Empty;
    private string _selectedProductGroupId = string.Empty;
    private string _selectedCustomerGroupId = string.Empty;
    private string _selectedDimensionKey = "customers";
    private string _selectedComparison = "all";
    private string _analysisSearch = string.Empty;
    private bool _includeZeroProducts;
    private string _appliedWarehouseId = string.Empty;
    private string _appliedProductGroupId = string.Empty;
    private string _appliedCustomerGroupId = string.Empty;
    private bool _appliedIncludeZeroProducts;
    private SalesAnalysisRow? _selectedAnalysisRow;
    private bool _isExportOpen;
    private string _exportFormat = "xlsx";

    public SalesReportingViewModel(
        ISalesReportingService service,
        IAccessStateService access,
        ISalesReportingViewStateStore viewStateStore)
    {
        _service = service;
        _access = access;
        _viewStateStore = viewStateStore;
        InitializeAnalysisExport();

        var saved = viewStateStore.Load();
        if (saved is not null)
        {
            _selectedDimensionKey = saved.Dimension;
            _analysisSearch = saved.Search;
            _selectedComparison = saved.Comparison;
        }

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _exportGeneration++;

            _loadCts?.Cancel();
            _loadCts = null;
            _exportCts?.Cancel();
            _exportCts = null;

            IsBusy = false;
            IsExporting = false;
            _loaded = false;
            _report = null;
            _appliedFrom = null;
            _appliedTo = null;
            _appliedWarehouseId = string.Empty;
            _appliedProductGroupId = string.Empty;
            _appliedBrandId = string.Empty;
            _appliedCustomerGroupId = string.Empty;
            _appliedIncludeZeroProducts = false;
            IsExportOpen = false;
            SelectedAnalysisRow = null;

            Warehouses.Clear();
            ProductGroups.Clear();
            Brands.Clear();
            CustomerGroups.Clear();
            RevenueRows.Clear();
            AnalysisRows.Clear();
            TotalRows.Clear();
            TrendRows.Clear();
            TrendSeries.Clear();
            Warnings.Clear();
            ReportError = string.Empty;
            ExportError = string.Empty;
            Message = string.Empty;
            MessageIsError = false;

            OnPropertyChanged(nameof(CanRead));
            OnPropertyChanged(nameof(CanExport));
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(CanOpenExport));
            OnPropertyChanged(nameof(CanSubmitExport));
            RaiseReportState();

            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<SalesReportingOption> Dimensions => SalesReportingPresentation.Dimensions;
    public IReadOnlyList<SalesReportingOption> Comparisons => SalesReportingPresentation.Comparisons;
    public IReadOnlyList<SalesReportingOption> Presets => SalesReportingPresentation.Presets;

    public ObservableCollection<SalesWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<SalesClassificationOption> ProductGroups { get; } = [];
    public ObservableCollection<SalesClassificationOption> Brands { get; } = [];
    public ObservableCollection<SalesClassificationOption> CustomerGroups { get; } = [];
    public ObservableCollection<SalesRevenueRow> RevenueRows { get; } = [];
    public ObservableCollection<SalesAnalysisRow> AnalysisRows { get; } = [];
    public ObservableCollection<SalesAnalysisRow> TotalRows { get; } = [];
    public ObservableCollection<SalesTrendRow> TrendRows { get; } = [];
    public ObservableCollection<SalesTrendSeriesRow> TrendSeries { get; } = [];
    public ObservableCollection<string> Warnings { get; } = [];
    public ObservableCollection<SalesExportColumnOption> ExportColumns { get; } = [];

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanExport => _access.HasPermission(ExportPermission) && CanRead;
    public bool CanApply => CanRead && !IsBusy;
    public bool CanOpenExport => CanExport && _report is not null && !IsBusy && !IsExporting;
    public bool CanSubmitExport => CanOpenExport && (IsAnalysisExportMode
        ? AnalysisReady
        : ExportColumns.Any(column => column.IsSelected));
    public bool CanCloseExport => !IsExporting;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(CanOpenExport));
            OnPropertyChanged(nameof(CanSubmitExport));
            OnPropertyChanged(nameof(ApplyText));
        }
    }

    public bool IsExporting
    {
        get => _isExporting;
        private set
        {
            if (!SetField(ref _isExporting, value)) return;
            OnPropertyChanged(nameof(CanOpenExport));
            OnPropertyChanged(nameof(CanSubmitExport));
            OnPropertyChanged(nameof(CanCloseExport));
            OnPropertyChanged(nameof(ExportButtonText));
        }
    }

    public string ApplyText => IsBusy ? "Đang cập nhật…" : "Áp dụng";
    public string ExportButtonText => IsExporting ? "Đang tạo file…"
        : IsAnalysisExportMode ? "Xuất Excel"
        : ExportFormat == "csv" ? "Xuất CSV" : "Xuất Excel";

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value ?? string.Empty);
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public string ReportError
    {
        get => _reportError;
        private set
        {
            if (!SetField(ref _reportError, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasReportError));
        }
    }

    public bool HasReportError => !string.IsNullOrWhiteSpace(ReportError);

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

    public string SavedNotice
    {
        get => _savedNotice;
        private set
        {
            if (!SetField(ref _savedNotice, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasSavedNotice));
        }
    }

    public bool HasSavedNotice => !string.IsNullOrWhiteSpace(SavedNotice);

    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetField(ref _fromDate, value);
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => SetField(ref _toDate, value);
    }

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set => SetField(ref _selectedWarehouseId, value ?? string.Empty);
    }

    public string SelectedProductGroupId
    {
        get => _selectedProductGroupId;
        set => SetField(ref _selectedProductGroupId, value ?? string.Empty);
    }

    public string SelectedBrandId
    {
        get => _selectedBrandId;
        set => SetField(ref _selectedBrandId, value ?? string.Empty);
    }

    public string SelectedCustomerGroupId
    {
        get => _selectedCustomerGroupId;
        set => SetField(ref _selectedCustomerGroupId, value ?? string.Empty);
    }

    public bool IncludeZeroProducts
    {
        get => _includeZeroProducts;
        set => SetField(ref _includeZeroProducts, value);
    }

    public string SelectedDimensionKey
    {
        get => _selectedDimensionKey;
        set
        {
            var normalized = Dimensions.Any(option => option.Key == value) ? value : "customers";
            if (!SetField(ref _selectedDimensionKey, normalized)) return;
            SelectedComparison = "all";
            SelectedAnalysisRow = null;
            OnPropertyChanged(nameof(SelectedDimensionLabel));
            OnPropertyChanged(nameof(MetricHeader));
            OnPropertyChanged(nameof(IsCustomersDimension));
            OnPropertyChanged(nameof(IsProductsDimension));
            OnPropertyChanged(nameof(AnalysisTitle));
            RebuildAnalysis();
        }
    }

    public string SelectedComparison
    {
        get => _selectedComparison;
        set
        {
            var normalized = Comparisons.Any(option => option.Key == value) ? value : "all";
            if (!SetField(ref _selectedComparison, normalized)) return;
            SelectedAnalysisRow = null;
            RebuildAnalysis();
        }
    }

    public string AnalysisSearch
    {
        get => _analysisSearch;
        set
        {
            var normalized = value ?? string.Empty;
            if (normalized.Length > 80) normalized = normalized[..80];
            if (!SetField(ref _analysisSearch, normalized)) return;
            SelectedAnalysisRow = null;
            RebuildAnalysis();
        }
    }

    public SalesAnalysisRow? SelectedAnalysisRow
    {
        get => _selectedAnalysisRow;
        set
        {
            if (!SetField(ref _selectedAnalysisRow, value)) return;
            foreach (var property in new[]
            {
                nameof(HasSelectedAnalysisRow), nameof(SelectedDetailOrderCount),
                nameof(SelectedDetailCustomerCount), nameof(SelectedDetailProductCount),
                nameof(SelectedDetailQuantity)
            }) OnPropertyChanged(property);
        }
    }

    public bool HasSelectedAnalysisRow => SelectedAnalysisRow is not null;

    public bool IsExportOpen
    {
        get => _isExportOpen;
        private set => SetField(ref _isExportOpen, value);
    }

    public string ExportFormat
    {
        get => _exportFormat;
        set
        {
            var normalized = value == "csv" ? "csv" : "xlsx";
            if (!SetField(ref _exportFormat, normalized)) return;
            OnPropertyChanged(nameof(ExportButtonText));
        }
    }

    public string SelectedDimensionLabel => SalesReportingPresentation.DimensionLabel(SelectedDimensionKey);
    public string MetricHeader => SalesReportingPresentation.MetricLabel(SelectedDimensionKey);
    public string AnalysisTitle => $"Chi tiết {SelectedDimensionLabel.ToLowerInvariant()}";
    public bool IsCustomersDimension => SelectedDimensionKey == "customers";
    public bool IsProductsDimension => SelectedDimensionKey == "products";

    public string PeriodText => _appliedFrom is not null && _appliedTo is not null
        ? $"{_appliedFrom:dd/MM/yyyy} → {_appliedTo:dd/MM/yyyy}"
        : "Tháng hiện tại · giờ Việt Nam";

    public string EffectiveOrderCount => SalesReportingPresentation.Number(_report?.Summary.EffectiveOrderCount);
    public string BuyerCount => SalesReportingPresentation.Number(_report?.Summary.BuyerCount);
    public string SoldProductCount => SalesReportingPresentation.Number(_report?.Summary.SoldProductCount);
    public string GeneratedAtText => $"Cập nhật: {SalesReportingPresentation.GeneratedAt(_report?.GeneratedAt)}";
    public string AnalysisCountText => $"{AnalysisRows.Count} dòng đang hiển thị";
    public string AnalysisEmptyText => !IsBusy && AnalysisRows.Count == 0
        ? GetDimensionRows().Length == 0
            ? "Không có dữ liệu cho chiều phân tích này trong kỳ."
            : "Không có dòng nào khớp bộ lọc phân tích."
        : string.Empty;
    public string TotalEmptyText => !IsBusy && TotalRows.Count == 0 ? "Không có tổng theo tiền tệ cho bộ lọc hiện tại." : string.Empty;
    public string TrendEmptyText => !IsBusy && TrendRows.Count == 0 ? "Không có doanh thu theo ngày trong kỳ." : string.Empty;

    public string ReconciliationText => _report is null
        ? "Đối soát: —"
        : _report.Reconciliation.Ok ? "Đối soát: Đã khớp" : "Đối soát: Cần kiểm tra";

    public string ReconciliationDetailText => _report is null
        ? string.Empty
        : $"Đã kiểm {SalesReportingPresentation.Number(_report.Reconciliation.CheckedOrderCount)} đơn · {SalesReportingPresentation.Number(_report.Reconciliation.MismatchCount)} chênh lệch";

    public string DataQualityText => Warnings.Count == 0 ? "Dữ liệu ổn" : $"{Warnings.Count} cảnh báo";

    public string PreviousPeriodText => _report is null
        ? "Giữ riêng từng loại tiền"
        : $"Kỳ trước {SalesReportingPresentation.Date(_report.Comparison.Previous.From)} → {SalesReportingPresentation.Date(_report.Comparison.Previous.To)}";

    public string LineageText =>
        "Số liệu lấy từ đơn bán hàng hiệu lực và ảnh chụp nghiệp vụ đã lưu khi xác nhận. Báo cáo chỉ dùng phạm vi kho tài khoản hiện tại được cấp.";

    public string SelectedDetailOrderCount => SalesReportingPresentation.Number(SelectedAnalysisRow?.Data.DocumentCount);
    public string SelectedDetailCustomerCount => SalesReportingPresentation.Number(SelectedAnalysisRow?.Data.CustomerCount);
    public string SelectedDetailProductCount => SalesReportingPresentation.Number(SelectedAnalysisRow?.Data.ProductCount);
    public string SelectedDetailQuantity
    {
        get
        {
            var row = SelectedAnalysisRow?.Data;
            if (row is null) return "0";
            var unit = !string.IsNullOrWhiteSpace(row.Unit.Name) ? row.Unit.Name : row.Unit.Code;
            return $"{SalesReportingPresentation.Number(row.Quantity)} {unit}".Trim();
        }
    }

    public string SelectedExportCountText
    {
        get
        {
            var columns = IsAnalysisExportMode ? AnalysisExportColumns : ExportColumns;
            return $"{columns.Count(column => column.IsSelected)}/{columns.Count} cột";
        }
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await RefreshAsync(initializeDraft: true).ConfigureAwait(true);
    }

    public Task RefreshAsync() => RefreshAsync(initializeDraft: false);

    private async Task RefreshAsync(bool initializeDraft)
    {
        if (IsBusy) return;
        if (!CanRead)
        {
            SetError("Tài khoản chưa được cấp quyền xem Báo cáo bán hàng.");
            return;
        }

        if (FromDate is not null && ToDate is not null)
        {
            if (FromDate.Value.Date > ToDate.Value.Date)
            {
                SetError("Ngày bắt đầu không được sau ngày kết thúc.");
                return;
            }

            if ((ToDate.Value.Date - FromDate.Value.Date).TotalDays > 365)
            {
                SetError("Kỳ báo cáo tối đa 366 ngày.");
                return;
            }
        }

        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        var cts = new CancellationTokenSource();
        _loadCts = cts;
        IsBusy = true;
        ReportError = string.Empty;
        try
        {
            var report = await _service.GetAsync(
                FromDate,
                ToDate,
                SelectedWarehouseId,
                SelectedProductGroupId,
                SelectedBrandId,
                SelectedCustomerGroupId,
                IncludeZeroProducts,
                cts.Token).ConfigureAwait(true);

            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead)
                return;

            Apply(report, initializeDraft);
            _loaded = true;
            Message = "Báo cáo bán hàng đã được cập nhật.";
            MessageIsError = false;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
            {
                SetError(CanonicalErrorMessages.WithRequestId(
                    CanonicalErrorMessages.ToOfficeMessage(exception),
                    exception.RequestId));
            }
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
                SetError(exception.Message);
        }
        finally
        {
            cts.Dispose();
            if (ReferenceEquals(_loadCts, cts)) _loadCts = null;
            if (request == _loadGeneration) IsBusy = false;
            RaiseReportState();
        }
    }

    public async Task ResetAsync()
    {
        if (IsBusy) return;
        FromDate = null;
        ToDate = null;
        SelectedWarehouseId = string.Empty;
        SelectedProductGroupId = string.Empty;
        SelectedBrandId = string.Empty;
        SelectedCustomerGroupId = string.Empty;
        IncludeZeroProducts = false;
        AnalysisSearch = string.Empty;
        SelectedComparison = "all";
        SelectedAnalysisRow = null;
        await RefreshAsync(initializeDraft: true).ConfigureAwait(true);
    }

    public async Task ApplyPresetAsync(string key)
    {
        if (IsBusy) return;
        var today = VietnamToday();
        switch (key)
        {
            case "today":
                FromDate = today;
                ToDate = today;
                break;
            case "last7":
                FromDate = today.AddDays(-6);
                ToDate = today;
                break;
            case "thisMonth":
                FromDate = new DateTime(today.Year, today.Month, 1);
                ToDate = today;
                break;
            case "previousMonth":
                var lastPrevious = new DateTime(today.Year, today.Month, 1).AddDays(-1);
                FromDate = new DateTime(lastPrevious.Year, lastPrevious.Month, 1);
                ToDate = lastPrevious;
                break;
            default:
                return;
        }

        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task SaveViewAsync()
    {
        try
        {
            var saved = new SalesReportingSavedView(
                SelectedDimensionKey,
                AnalysisSearch,
                SelectedComparison);
            await _viewStateStore.SaveAsync(saved).ConfigureAwait(true);
            SavedNotice = "Đã lưu chế độ xem trên thiết bị này";
            Message = SavedNotice;
            MessageIsError = false;
            await Task.Delay(2200).ConfigureAwait(true);
            if (SavedNotice == "Đã lưu chế độ xem trên thiết bị này") SavedNotice = string.Empty;
            if (Message == "Đã lưu chế độ xem trên thiết bị này") Message = string.Empty;
        }
        catch
        {
            SavedNotice = string.Empty;
            Message = "Không lưu được chế độ xem trên thiết bị này.";
            MessageIsError = true;
        }
    }

    public void OpenExport()
    {
        if (!CanOpenExport) return;
        ExportFormat = "xlsx";
        ExportMode = "list";
        ExportError = string.Empty;
        ReplaceExportColumns();
        ResetAnalysisExportState();
        IsExportOpen = true;
    }

    public void CloseExport()
    {
        if (IsExporting) return;
        IsExportOpen = false;
        ExportError = string.Empty;
    }

    public void SelectAllExportColumns()
    {
        foreach (var column in IsAnalysisExportMode ? AnalysisExportColumns : ExportColumns) column.IsSelected = true;
        RaiseExportSelection();
    }

    public void ClearExportColumns()
    {
        foreach (var column in IsAnalysisExportMode ? AnalysisExportColumns : ExportColumns) column.IsSelected = false;
        RaiseExportSelection();
    }

    public void ResetExportColumns()
    {
        if (IsAnalysisExportMode) RefreshAnalysisExportColumns();
        else ReplaceExportColumns();
    }

    public async Task<ApiDownloadFile?> ExportAsync()
    {
        if (!CanSubmitExport) return null;
        var analysis = IsAnalysisExportMode;
        var columns = (analysis ? AnalysisExportColumns : ExportColumns)
            .Where(column => column.IsSelected)
            .Select(column => column.Key)
            .ToArray();
        var dimension = analysis ? AnalysisExportDimension : SelectedDimensionKey;
        var format = analysis ? "xlsx" : ExportFormat;
        var productGroupId = analysis || SelectedDimensionKey == "products" ? _appliedProductGroupId : string.Empty;
        var brandId = analysis || SelectedDimensionKey == "products" ? _appliedBrandId : string.Empty;
        var customerGroupId = analysis || SelectedDimensionKey == "customers" ? _appliedCustomerGroupId : string.Empty;
        var includeZeroProducts = !analysis && SelectedDimensionKey == "products" && _appliedIncludeZeroProducts;
        var accessGeneration = _accessGeneration;
        var request = ++_exportGeneration;
        var cts = new CancellationTokenSource();
        _exportCts = cts;
        IsExporting = true;
        ExportError = string.Empty;
        try
        {
            var file = await _service.ExportAsync(
                _appliedFrom,
                _appliedTo,
                _appliedWarehouseId,
                productGroupId,
                brandId,
                customerGroupId,
                includeZeroProducts,
                dimension,
                format,
                columns,
                analysis && AnalysisQuantitySelected ? AnalysisQuantityDisplay : null,
                analysis ? AnalysisSort : null,
                cts.Token).ConfigureAwait(true);

            if (accessGeneration != _accessGeneration || request != _exportGeneration || !CanExport)
                return null;

            IsExportOpen = false;
            Message = $"Đã tạo file {file.FileName}.";
            MessageIsError = false;
            return file;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return null;
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration && request == _exportGeneration)
            {
                ExportError = CanonicalErrorMessages.WithRequestId(
                    CanonicalErrorMessages.ToOfficeMessage(exception),
                    exception.RequestId);
                Message = ExportError;
                MessageIsError = true;
            }
            return null;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _exportGeneration)
            {
                ExportError = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Không xuất được Báo cáo bán hàng."
                    : exception.Message;
                Message = ExportError;
                MessageIsError = true;
            }
            return null;
        }
        finally
        {
            cts.Dispose();
            if (ReferenceEquals(_exportCts, cts)) _exportCts = null;
            if (request == _exportGeneration) IsExporting = false;
        }
    }

    public void SelectAnalysisRow(SalesAnalysisRow row) => SelectedAnalysisRow = row;
    public void CloseAnalysisDetail() => SelectedAnalysisRow = null;

    private void Apply(SalesReportingDashboardData report, bool initializeDraft)
    {
        _report = report ?? throw new InvalidOperationException("Báo cáo bán hàng không có dữ liệu.");
        _appliedFrom = SalesReportingPresentation.ParseDate(report.Filters.From);
        _appliedTo = SalesReportingPresentation.ParseDate(report.Filters.To);
        _appliedWarehouseId = report.Filters.WarehouseId ?? string.Empty;
        _appliedProductGroupId = report.Filters.ProductGroupId ?? string.Empty;
        _appliedBrandId = report.Filters.BrandId ?? string.Empty;
        _appliedCustomerGroupId = report.Filters.CustomerGroupId ?? string.Empty;
        _appliedIncludeZeroProducts = report.Filters.IncludeZeroProducts;

        if (initializeDraft)
        {
            FromDate = _appliedFrom;
            ToDate = _appliedTo;
            SelectedWarehouseId = _appliedWarehouseId;
            SelectedProductGroupId = _appliedProductGroupId;
            SelectedBrandId = _appliedBrandId;
            SelectedCustomerGroupId = _appliedCustomerGroupId;
            IncludeZeroProducts = _appliedIncludeZeroProducts;
        }

        Replace(Warehouses, new[] { new SalesWarehouseOption(string.Empty, "Tất cả kho") }
            .Concat(report.ScopeWarehouses.Select(row => new SalesWarehouseOption(
                row.WarehouseId,
                $"{row.WarehouseCode} — {row.WarehouseName}"))));

        Replace(ProductGroups, new[] { new SalesClassificationOption(string.Empty, "Tất cả nhóm sản phẩm") }
            .Concat(report.Classification.Options.ProductGroups.Select(row => new SalesClassificationOption(
                row.Id,
                string.Join(" — ", new[] { row.Code, row.Name }.Where(value => !string.IsNullOrWhiteSpace(value)))))));

        Replace(Brands, new[] { new SalesClassificationOption(string.Empty, "Tất cả nhãn hàng") }
            .Concat(report.Classification.Options.Brands.Select(row => new SalesClassificationOption(
                row.Id,
                string.Join(" — ", new[] { row.Code, row.Name }.Where(value => !string.IsNullOrWhiteSpace(value)))))));

        Replace(CustomerGroups, new[] { new SalesClassificationOption(string.Empty, "Tất cả nhóm khách hàng") }
            .Concat(report.Classification.Options.CustomerGroups.Select(row => new SalesClassificationOption(
                row.Id,
                string.Join(" — ", new[] { row.Code, row.Name }.Where(value => !string.IsNullOrWhiteSpace(value)))))));

        Replace(RevenueRows, report.Summary.Revenues.Select(row => new SalesRevenueRow(
            row.CurrencyCode,
            SalesReportingPresentation.Money(row.Revenue, row.CurrencyCode),
            $"Kỳ trước {SalesReportingPresentation.Money(row.PreviousRevenue, row.CurrencyCode)} · {SalesReportingPresentation.Percent(row.ChangePercent)}")));

        Replace(TrendRows, report.DailyTrend.Select((row, index) => new SalesTrendRow(
            index + 1,
            SalesReportingPresentation.Date(row.BusinessDate),
            row.CurrencyCode,
            SalesReportingPresentation.Money(row.Revenue, row.CurrencyCode),
            SalesReportingPresentation.Money(row.PreviousRevenue, row.CurrencyCode),
            SalesReportingPresentation.Percent(row.ChangePercent))));

        Replace(TrendSeries, SalesReportingPresentation.TrendSeries(report.DailyTrend));
        Replace(Warnings, report.DataQuality.Warnings);
        SelectedAnalysisRow = null;
        RebuildAnalysis();
        RaiseReportState();
    }

    private void RebuildAnalysis()
    {
        if (_report is null)
        {
            AnalysisRows.Clear();
            TotalRows.Clear();
            RaiseReportState();
            return;
        }

        var source = GetDimensionRows();
        var comparison = SelectedComparison;
        var needle = AnalysisSearch.Trim();

        var filtered = source.Where(row =>
        {
            if (!string.Equals(comparison, "all", StringComparison.Ordinal)
                && !string.Equals(row.ComparisonState, comparison, StringComparison.Ordinal)) return false;
            if (needle.Length == 0) return true;
            return (row.Code?.Contains(needle, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || row.Name.Contains(needle, StringComparison.CurrentCultureIgnoreCase);
        }).ToArray();

        Replace(AnalysisRows, filtered.Select((row, index) => SalesReportingPresentation.AnalysisRow(row, SelectedDimensionKey, index)));
        Replace(TotalRows, GetDimensionTotals().Select((row, index) => SalesReportingPresentation.AnalysisRow(row, SelectedDimensionKey, index)));
        RefreshAnalysisExportColumns();
        RaiseReportState();
    }

    private SalesBreakdownData[] GetDimensionRows() => _report is null
        ? []
        : SelectedDimensionKey switch
        {
            "customers" => _report.Breakdowns.Customers,
            "customerGroups" => _report.Breakdowns.CustomerGroups,
            "channels" => _report.Breakdowns.Channels,
            "products" => _report.Breakdowns.Products,
            "productGroups" => _report.Breakdowns.ProductGroups,
            "employees" => _report.Breakdowns.Employees,
            _ => []
        };

    private SalesBreakdownData[] GetDimensionTotals() => _report is null
        ? []
        : SelectedDimensionKey switch
        {
            "customers" => _report.BreakdownTotals.Customers,
            "customerGroups" => _report.BreakdownTotals.CustomerGroups,
            "channels" => _report.BreakdownTotals.Channels,
            "products" => _report.BreakdownTotals.Products,
            "productGroups" => _report.BreakdownTotals.ProductGroups,
            "employees" => _report.BreakdownTotals.Employees,
            _ => []
        };

    private void ReplaceExportColumns()
    {
        foreach (var existing in ExportColumns) existing.PropertyChanged -= ExportColumn_OnPropertyChanged;
        ExportColumns.Clear();
        foreach (var definition in SalesReportingPresentation.ExportColumns(SelectedDimensionKey))
        {
            var option = new SalesExportColumnOption(definition.Key, definition.Label, definition.DefaultSelected);
            option.PropertyChanged += ExportColumn_OnPropertyChanged;
            ExportColumns.Add(option);
        }
        RaiseExportSelection();
    }

    private void ExportColumn_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SalesExportColumnOption.IsSelected)) RaiseExportSelection();
    }

    private void RaiseExportSelection()
    {
        OnPropertyChanged(nameof(SelectedExportCountText));
        OnPropertyChanged(nameof(CanSubmitExport));
    }

    private void RaiseReportState()
    {
        foreach (var property in new[]
        {
            nameof(PeriodText), nameof(EffectiveOrderCount), nameof(BuyerCount), nameof(SoldProductCount),
            nameof(GeneratedAtText), nameof(AnalysisCountText), nameof(AnalysisEmptyText), nameof(TotalEmptyText),
            nameof(TrendEmptyText), nameof(ReconciliationText), nameof(ReconciliationDetailText),
            nameof(DataQualityText), nameof(PreviousPeriodText), nameof(LineageText), nameof(CanOpenExport),
            nameof(CanSubmitExport)
        }) OnPropertyChanged(property);
    }

    private void SetError(string message)
    {
        ReportError = string.IsNullOrWhiteSpace(message) ? "Không tải được Báo cáo bán hàng." : message;
        Message = ReportError;
        MessageIsError = true;
    }

    private static DateTime VietnamToday()
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date;
        }
        catch
        {
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date;
            }
            catch
            {
                return DateTime.Today;
            }
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
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
