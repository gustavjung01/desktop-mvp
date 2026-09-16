using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed class GrossMarginReportingViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.reporting.gross-margin.read";
    private const string ExportPermission = "core.reporting.export";

    private readonly IGrossMarginReportingService _service;
    private readonly IAccessStateService _access;
    private GrossMarginReportingDashboardData? _report;
    private bool _loaded;
    private long _accessGeneration;
    private long _loadGeneration;
    private long _exportGeneration;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _exportCts;
    private bool _isBusy;
    private bool _isExporting;
    private bool _isExportOpen;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _reportError = string.Empty;
    private string _exportError = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private DateTime? _appliedFrom;
    private DateTime? _appliedTo;
    private string _selectedWarehouseId = string.Empty;
    private string _appliedWarehouseId = string.Empty;
    private int _activeTabIndex;
    private string _selectedExportDimension = "customers";
    private string _exportFormat = "xlsx";

    public GrossMarginReportingViewModel(
        IGrossMarginReportingService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;

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
            IsExportOpen = false;
            _loaded = false;
            _report = null;
            _appliedFrom = null;
            _appliedTo = null;
            _appliedWarehouseId = string.Empty;
            Warehouses.Clear();
            CustomerRows.Clear();
            SkuRows.Clear();
            ExceptionRows.Clear();
            ExportColumns.Clear();
            ReportError = string.Empty;
            ExportError = string.Empty;
            Message = string.Empty;
            MessageIsError = false;

            OnPropertyChanged(nameof(CanRead));
            OnPropertyChanged(nameof(CanExport));
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(CanOpenExport));
            RaiseReportState();

            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<GrossMarginWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<GrossMarginGroupRow> CustomerRows { get; } = [];
    public ObservableCollection<GrossMarginGroupRow> SkuRows { get; } = [];
    public ObservableCollection<GrossMarginExceptionRow> ExceptionRows { get; } = [];
    public ObservableCollection<GrossMarginExportColumnOption> ExportColumns { get; } = [];
    public IReadOnlyList<GrossMarginOption> ExportDimensions => GrossMarginReportingPresentation.ExportDimensions;

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanExport => CanRead && _access.HasPermission(ExportPermission);
    public bool CanApply => CanRead && !IsBusy;
    public bool CanOpenExport => CanExport && _report is not null && !IsBusy && !IsExporting;
    public bool CanSubmitExport => CanOpenExport && ExportColumns.Any(column => column.IsSelected);
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
            OnPropertyChanged(nameof(ShowInitialLoading));
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

    public bool IsExportOpen
    {
        get => _isExportOpen;
        private set => SetField(ref _isExportOpen, value);
    }

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

    public string ExportError
    {
        get => _exportError;
        private set => SetField(ref _exportError, value ?? string.Empty);
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetField(ref _fromDate, value?.Date);
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => SetField(ref _toDate, value?.Date);
    }

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set => SetField(ref _selectedWarehouseId, value ?? string.Empty);
    }

    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set => SetField(ref _activeTabIndex, Math.Clamp(value, 0, 2));
    }

    public string SelectedExportDimension
    {
        get => _selectedExportDimension;
        set
        {
            var normalized = GrossMarginReportingPresentation.ExportDimensions.Any(option => option.Key == value)
                ? value
                : "customers";
            if (!SetField(ref _selectedExportDimension, normalized)) return;
            ReplaceExportColumns();
        }
    }

    public string ExportFormat
    {
        get => _exportFormat;
        set
        {
            var normalized = string.Equals(value, "csv", StringComparison.Ordinal) ? "csv" : "xlsx";
            if (!SetField(ref _exportFormat, normalized)) return;
            OnPropertyChanged(nameof(ExportButtonText));
        }
    }

    public string ApplyText => IsBusy ? "Đang cập nhật…" : "Áp dụng";
    public string ExportButtonText => IsExporting ? "Đang tạo file…" : ExportFormat == "csv" ? "Xuất CSV" : "Xuất Excel";
    public bool HasReport => _report is not null;
    public bool ShowInitialLoading => IsBusy && !HasReport;
    public bool HasReportError => !string.IsNullOrWhiteSpace(ReportError);
    public bool HasCustomers => CustomerRows.Count > 0;
    public bool HasSkus => SkuRows.Count > 0;
    public bool HasExceptions => ExceptionRows.Count > 0;
    public string CustomerEmptyText => HasReport && !HasCustomers ? "Chưa có dòng lãi gộp so sánh được." : string.Empty;
    public string SkuEmptyText => HasReport && !HasSkus ? "Chưa có dữ liệu." : string.Empty;
    public string ExceptionEmptyText => HasReport && !HasExceptions ? "Không có ngoại lệ trong kỳ." : string.Empty;

    public string NetRevenueText => GrossMarginReportingPresentation.Money(_report?.Summary.NetRevenueVnd);
    public string CogsText => GrossMarginReportingPresentation.Money(_report?.Summary.CogsVnd);
    public string GrossMarginText => GrossMarginReportingPresentation.Money(_report?.Summary.GrossMarginVnd);
    public string GrossMarginPercentText => GrossMarginReportingPresentation.Percent(_report?.Summary.GrossMarginPercent);
    public string ReconciliationText => _report is null
        ? string.Empty
        : $"Đối soát: {_report.Summary.ComparableLineCount}/{_report.Summary.EventLineCount} dòng so sánh được · thiếu liên kết chứng từ {_report.Summary.MissingLineageCount} · thiếu giá vốn {_report.Summary.MissingCostCount} · bất thường giá vốn {_report.Summary.CostAnomalyCount} · chưa quy đổi VND {_report.Summary.NonVndCount}.";
    public string SelectedExportCountText => $"{ExportColumns.Count(column => column.IsSelected)}/{ExportColumns.Count} cột";

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
            SetError("Tài khoản chưa được cấp quyền xem Lãi gộp.");
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
                cts.Token).ConfigureAwait(true);

            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead)
                return;

            Apply(report, initializeDraft);
            _loaded = true;
            Message = "Lãi gộp đã được cập nhật.";
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
        await RefreshAsync(initializeDraft: true).ConfigureAwait(true);
    }

    public void OpenExport()
    {
        if (!CanOpenExport) return;
        SelectedExportDimension = ActiveTabIndex switch
        {
            1 => "skus",
            2 => "exceptions",
            _ => "customers"
        };
        ExportFormat = "xlsx";
        ExportError = string.Empty;
        ReplaceExportColumns();
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
        foreach (var column in ExportColumns) column.IsSelected = true;
        RaiseExportSelection();
    }

    public void ClearExportColumns()
    {
        foreach (var column in ExportColumns) column.IsSelected = false;
        RaiseExportSelection();
    }

    public void ResetExportColumns() => ReplaceExportColumns();

    public async Task<ApiDownloadFile?> ExportAsync()
    {
        if (!CanSubmitExport) return null;

        var columns = ExportColumns
            .Where(column => column.IsSelected)
            .Select(column => column.Key)
            .ToArray();

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
                SelectedExportDimension,
                ExportFormat,
                columns,
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
                    ? "Không xuất được báo cáo lãi gộp."
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

    private void Apply(GrossMarginReportingDashboardData report, bool initializeDraft)
    {
        _report = report ?? throw new InvalidOperationException("Báo cáo lãi gộp không có dữ liệu.");
        _appliedFrom = GrossMarginReportingPresentation.ParseDate(report.Filters.From);
        _appliedTo = GrossMarginReportingPresentation.ParseDate(report.Filters.To);
        _appliedWarehouseId = report.Filters.WarehouseId ?? string.Empty;

        if (initializeDraft)
        {
            FromDate = _appliedFrom;
            ToDate = _appliedTo;
            SelectedWarehouseId = _appliedWarehouseId;
        }

        var warehouseOptions = report.Lines
            .Concat(report.Exceptions)
            .Where(row => !string.IsNullOrWhiteSpace(row.WarehouseId) && !string.IsNullOrWhiteSpace(row.WarehouseCode))
            .GroupBy(row => row.WarehouseId, StringComparer.Ordinal)
            .Select(group => new GrossMarginWarehouseOption(group.Key, group.First().WarehouseCode))
            .OrderBy(row => row.Label, StringComparer.CurrentCultureIgnoreCase);

        Replace(Warehouses, new[] { new GrossMarginWarehouseOption(string.Empty, "Tất cả kho được cấp quyền") }.Concat(warehouseOptions));
        Replace(CustomerRows, report.TopCustomers.Select(GrossMarginReportingPresentation.CustomerRow));
        Replace(SkuRows, report.TopSkus.Select(GrossMarginReportingPresentation.SkuRow));
        Replace(ExceptionRows, report.Exceptions.Select(GrossMarginReportingPresentation.ExceptionRow));
        RaiseReportState();
    }

    private void ReplaceExportColumns()
    {
        foreach (var existing in ExportColumns) existing.PropertyChanged -= ExportColumn_OnPropertyChanged;
        ExportColumns.Clear();

        foreach (var definition in GrossMarginReportingPresentation.ExportColumns(SelectedExportDimension))
        {
            var option = new GrossMarginExportColumnOption(
                definition.Key,
                definition.Label,
                definition.DefaultSelected);
            option.PropertyChanged += ExportColumn_OnPropertyChanged;
            ExportColumns.Add(option);
        }

        RaiseExportSelection();
    }

    private void ExportColumn_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GrossMarginExportColumnOption.IsSelected))
            RaiseExportSelection();
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
            nameof(HasReport), nameof(ShowInitialLoading), nameof(NetRevenueText), nameof(CogsText),
            nameof(GrossMarginText), nameof(GrossMarginPercentText), nameof(ReconciliationText),
            nameof(HasCustomers), nameof(HasSkus), nameof(HasExceptions), nameof(CustomerEmptyText),
            nameof(SkuEmptyText), nameof(ExceptionEmptyText), nameof(CanOpenExport), nameof(CanSubmitExport)
        }) OnPropertyChanged(property);
    }

    private void SetError(string message)
    {
        ReportError = string.IsNullOrWhiteSpace(message) ? "Không tải được báo cáo lãi gộp." : message;
        Message = ReportError;
        MessageIsError = true;
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
