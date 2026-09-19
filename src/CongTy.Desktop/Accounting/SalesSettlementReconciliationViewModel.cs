using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed class SalesSettlementReconciliationViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.receivable.read";
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Đối soát bán hàng & COD.";

    private readonly ISalesSettlementReconciliationService _service;
    private readonly IAccessStateService _access;

    private SalesSettlementReconciliationData? _report;
    private bool _loaded;
    private Task<bool>? _initialLoadTask;
    private CancellationTokenSource? _loadCts;
    private long _accessGeneration;
    private long _loadGeneration;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _reportError = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _searchText = string.Empty;
    private string _selectedStatus = "all";
    private DateTime? _appliedFrom;
    private DateTime? _appliedTo;
    private string _appliedSearch = string.Empty;
    private string _appliedStatus = "all";
    private int _activeTabIndex;

    public SalesSettlementReconciliationViewModel(
        ISalesSettlementReconciliationService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loadCts?.Cancel();
            _loaded = false;
            _initialLoadTask = null;
            _report = null;
            ClearRows();
            _fromDate = null;
            _toDate = null;
            _searchText = string.Empty;
            _selectedStatus = "all";
            _appliedFrom = null;
            _appliedTo = null;
            _appliedSearch = string.Empty;
            _appliedStatus = "all";
            IsBusy = false;
            ReportError = string.Empty;
            ClearMessage();
            RaiseAll();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SalesSettlementCustomerRow> Customers { get; } = [];
    public ObservableCollection<SalesSettlementOrderRow> Orders { get; } = [];
    public ObservableCollection<SalesSettlementDocumentRow> Documents { get; } = [];
    public ObservableCollection<SalesSettlementCollectionRow> CodCollections { get; } = [];
    public ObservableCollection<SalesSettlementHandoverRow> CodHandovers { get; } = [];
    public ObservableCollection<SalesSettlementAnomalyRow> Anomalies { get; } = [];

    public IReadOnlyList<SalesSettlementStatusOption> StatusOptions =>
        SalesSettlementReconciliationPresentation.StatusOptions;

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool IsNotBusy => !IsBusy;
    public bool CanApply => CanRead && !IsBusy;
    public bool CanExport => CanRead && HasReport && !IsBusy;
    public bool HasReport => _report is not null;
    public bool ShowInitialLoading => IsBusy && !HasReport;
    public bool HasReportError => !string.IsNullOrWhiteSpace(ReportError);
    public bool HasCustomers => Customers.Count > 0;
    public bool HasOrders => Orders.Count > 0;
    public bool HasDocuments => Documents.Count > 0;
    public bool HasCodCollections => CodCollections.Count > 0;
    public bool HasCodHandovers => CodHandovers.Count > 0;
    public bool HasAnomalies => Anomalies.Count > 0;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(IsNotBusy));
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(CanExport));
            OnPropertyChanged(nameof(ApplyText));
            OnPropertyChanged(nameof(ShowInitialLoading));
            RaiseEmptyStates();
        }
    }

    public string ApplyText => IsBusy ? "Đang tải…" : "Áp dụng";

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

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value ?? string.Empty;
            if (normalized.Length > 160) normalized = normalized[..160];
            SetField(ref _searchText, normalized);
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            var normalized = (value ?? "all").Trim().ToLowerInvariant();
            if (!StatusOptions.Any(option => option.Key == normalized)) normalized = "all";
            SetField(ref _selectedStatus, normalized);
        }
    }

    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set => SetField(ref _activeTabIndex, Math.Clamp(value, 0, 4));
    }

    public string ActiveFilterText =>
        SalesSettlementReconciliationPresentation.ActiveFilterText(
            _appliedFrom,
            _appliedTo,
            _appliedSearch,
            _appliedStatus);

    public string GeneratedAtText => _report is null
        ? string.Empty
        : $"Cập nhật: {SalesSettlementReconciliationPresentation.DateTimeText(_report.GeneratedAt)}";

    public string DebitOutstandingText =>
        SalesSettlementReconciliationPresentation.Money(_report?.Summary.DebitOutstandingAmount);

    public string CustomerGroupText =>
        $"{SalesSettlementReconciliationPresentation.Number(_report?.Summary.CustomerGroupCount)} nhóm khách/kho";

    public string UnappliedCreditText =>
        SalesSettlementReconciliationPresentation.Money(_report?.Summary.UnappliedCreditAmount);

    public string CodCustodyText =>
        SalesSettlementReconciliationPresentation.Money(_report?.Summary.CodCustodyAmount);

    public string CodPendingAcceptanceText =>
        SalesSettlementReconciliationPresentation.Money(_report?.Summary.CodPendingAcceptanceAmount);

    public string CodAcceptedText =>
        SalesSettlementReconciliationPresentation.Money(_report?.Summary.CodAcceptedAmount);

    public string CodVarianceText =>
        $"Chênh lệch: {SalesSettlementReconciliationPresentation.Money(_report?.Summary.CodVarianceAmount)}";

    public string AnomalyCountText =>
        SalesSettlementReconciliationPresentation.Number(_report?.Summary.AnomalyCount);

    public string CustomerCountText => $"{Customers.Count} dòng";
    public string OrderCountText => $"{Orders.Count} đơn";
    public string DocumentCountText => $"{Documents.Count} chứng từ";
    public string CodCountText => $"{CodCollections.Count} khoản thu · {CodHandovers.Count} bàn giao";
    public string AnomalyStateText => HasAnomalies ? $"{Anomalies.Count} lỗi" : "Không có lỗi";

    public string CustomersEmptyText =>
        !IsBusy && HasReport && !HasCustomers ? "Không có số dư trong phạm vi lọc." : string.Empty;

    public string OrdersEmptyText =>
        !IsBusy && HasReport && !HasOrders ? "Không có đơn bán hàng trong phạm vi lọc." : string.Empty;

    public string DocumentsEmptyText =>
        !IsBusy && HasReport && !HasDocuments ? "Không có chứng từ trong phạm vi lọc." : string.Empty;

    public string CodCollectionsEmptyText =>
        !IsBusy && HasReport && !HasCodCollections ? "Không có khoản thu COD trong phạm vi lọc." : string.Empty;

    public string CodHandoversEmptyText =>
        !IsBusy && HasReport && !HasCodHandovers ? "Không có bàn giao COD trong phạm vi lọc." : string.Empty;

    public string AnomaliesEmptyText =>
        !IsBusy && HasReport && !HasAnomalies
            ? "Các projection, ledger, allocation và vòng đời COD trong phạm vi kho hiện đang khớp."
            : string.Empty;

    public string ExportFileName => $"doi-soat-ban-hang-cod-{DateTime.Today:yyyy-MM-dd}.csv";

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);

        _initialLoadTask = LoadAsync(initializeDraft: true);
        try
        {
            _loaded = await _initialLoadTask.ConfigureAwait(true);
            return _loaded;
        }
        finally
        {
            _initialLoadTask = null;
        }
    }

    public Task<bool> ApplyAsync() => LoadAsync(initializeDraft: false);

    public async Task<bool> ResetAsync()
    {
        if (IsBusy) return false;
        FromDate = null;
        ToDate = null;
        SearchText = string.Empty;
        SelectedStatus = "all";
        return await LoadAsync(initializeDraft: true).ConfigureAwait(true);
    }

    public Task<bool> RefreshAsync() => LoadAsync(initializeDraft: false);

    public string BuildCsv()
    {
        if (_report is null) throw new InvalidOperationException("Chưa có dữ liệu đối soát để xuất.");

        var rows = new List<string[]>
        {
            ["Nhóm", "Mã nguồn", "Khách/Tài xế", "Kho/Chuyến", "Số tiền/Trạng thái", "Kết quả"]
        };

        rows.AddRange(_report.Customers.Select(row => new[]
        {
            "Khách hàng", row.CustomerCode, row.CustomerName, row.WarehouseCode,
            row.CalculatedOpenBalance, row.ReconciliationStatus
        }));
        rows.AddRange(_report.Documents.Select(row => new[]
        {
            "Chứng từ", row.SourceDocumentNumber, row.CustomerNameSnapshot, row.WarehouseCodeSnapshot,
            row.ProjectedRemainingAmount, row.ReconciliationStatus
        }));
        rows.AddRange(_report.Orders.Select(row => new[]
        {
            "Đơn bán hàng", SalesSettlementReconciliationPresentation.DisplayOrderNumber(row), row.CustomerName, row.WarehouseCode,
            row.SettlementStatus, row.ReconciliationStatus
        }));
        rows.AddRange(_report.CodCollections.Select(row => new[]
        {
            "Thu COD", SalesSettlementReconciliationPresentation.DisplayDeliveryOrderNumber(row), row.DriverName, row.TripNumber,
            row.CustodyRemainingAmount, row.LifecycleMatches ? "matched" : "mismatch"
        }));
        rows.AddRange(_report.CodHandovers.Select(row => new[]
        {
            "Bàn giao COD", row.HandoverId, row.DriverName, row.TripNumber,
            row.ClaimedAmount, row.LifecycleMatches ? "matched" : "mismatch"
        }));
        rows.AddRange(_report.Anomalies.Select(row => new[]
        {
            "Bất thường", row.SourceNumber, row.AnomalyType, row.WarehouseId,
            JsonSerializer.Serialize(row.Details), row.ReconciliationStatus
        }));

        return "\uFEFF" + string.Join(
            "\r\n",
            rows.Select(row => string.Join(",", row.Select(CsvCell))));
    }

    public void NotifyExportSaved(string fileName)
    {
        Message = $"Đã lưu file {fileName}.";
        MessageIsError = false;
    }

    public void NotifyExportError(string? message)
    {
        Message = string.IsNullOrWhiteSpace(message)
            ? "Không xuất được dữ liệu đối soát."
            : message.Trim();
        MessageIsError = true;
    }

    private async Task<bool> LoadAsync(bool initializeDraft)
    {
        if (IsBusy) return false;

        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetError(ReadDenied);
            return false;
        }

        if (FromDate is not null && ToDate is not null && FromDate.Value.Date > ToDate.Value.Date)
        {
            SetError("Ngày bắt đầu không được sau ngày kết thúc.");
            return false;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        var cts = new CancellationTokenSource();
        _loadCts = cts;
        IsBusy = true;
        ReportError = string.Empty;

        try
        {
            var report = await _service.GetAsync(
                FromDate,
                ToDate,
                SearchText,
                SelectedStatus,
                cts.Token).ConfigureAwait(true);

            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead)
                return false;

            ApplyReport(report, initializeDraft);
            _loaded = true;
            Message = "Đối soát bán hàng & COD đã được cập nhật.";
            MessageIsError = false;
            return true;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return false;
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
            {
                SetError(CanonicalErrorMessages.WithRequestId(
                    CanonicalErrorMessages.ToOfficeMessage(exception),
                    exception.RequestId));
            }
            return false;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
            {
                SetError(string.IsNullOrWhiteSpace(exception.Message)
                    ? "Không tải được đối soát bán hàng và COD."
                    : exception.Message);
            }
            return false;
        }
        finally
        {
            if (ReferenceEquals(_loadCts, cts)) _loadCts = null;
            cts.Dispose();
            if (request == _loadGeneration) IsBusy = false;
        }
    }

    private void ApplyReport(SalesSettlementReconciliationData report, bool initializeDraft)
    {
        _report = report ?? throw new InvalidOperationException("Đối soát bán hàng và COD không có dữ liệu.");
        _appliedFrom = SalesSettlementReconciliationPresentation.ParseDate(report.Filters.From);
        _appliedTo = SalesSettlementReconciliationPresentation.ParseDate(report.Filters.To);
        _appliedSearch = report.Filters.Search?.Trim() ?? string.Empty;
        _appliedStatus = string.IsNullOrWhiteSpace(report.Filters.Status) ? "all" : report.Filters.Status.Trim();

        if (initializeDraft)
        {
            FromDate = _appliedFrom;
            ToDate = _appliedTo;
            SearchText = _appliedSearch;
            SelectedStatus = _appliedStatus;
        }

        Replace(Customers, report.Customers.Select(SalesSettlementReconciliationPresentation.Customer));
        Replace(Orders, report.Orders.Select(SalesSettlementReconciliationPresentation.Order));
        Replace(Documents, report.Documents.Select(SalesSettlementReconciliationPresentation.Document));
        Replace(CodCollections, report.CodCollections.Select(SalesSettlementReconciliationPresentation.Collection));
        Replace(CodHandovers, report.CodHandovers.Select(SalesSettlementReconciliationPresentation.Handover));
        Replace(Anomalies, report.Anomalies.Select(SalesSettlementReconciliationPresentation.Anomaly));
        RaiseAll();
    }

    private void SetError(string message)
    {
        ReportError = string.IsNullOrWhiteSpace(message)
            ? "Không tải được đối soát bán hàng và COD."
            : message;
        Message = ReportError;
        MessageIsError = true;
    }

    private void ClearRows()
    {
        Customers.Clear();
        Orders.Clear();
        Documents.Clear();
        CodCollections.Clear();
        CodHandovers.Clear();
        Anomalies.Clear();
    }

    private void ClearMessage()
    {
        Message = string.Empty;
        MessageIsError = false;
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(HasReport));
        OnPropertyChanged(nameof(ShowInitialLoading));
        OnPropertyChanged(nameof(ActiveFilterText));
        OnPropertyChanged(nameof(GeneratedAtText));
        OnPropertyChanged(nameof(DebitOutstandingText));
        OnPropertyChanged(nameof(CustomerGroupText));
        OnPropertyChanged(nameof(UnappliedCreditText));
        OnPropertyChanged(nameof(CodCustodyText));
        OnPropertyChanged(nameof(CodPendingAcceptanceText));
        OnPropertyChanged(nameof(CodAcceptedText));
        OnPropertyChanged(nameof(CodVarianceText));
        OnPropertyChanged(nameof(AnomalyCountText));
        OnPropertyChanged(nameof(CustomerCountText));
        OnPropertyChanged(nameof(OrderCountText));
        OnPropertyChanged(nameof(DocumentCountText));
        OnPropertyChanged(nameof(CodCountText));
        OnPropertyChanged(nameof(AnomalyStateText));
        RaiseEmptyStates();
    }

    private void RaiseEmptyStates()
    {
        OnPropertyChanged(nameof(HasCustomers));
        OnPropertyChanged(nameof(HasOrders));
        OnPropertyChanged(nameof(HasDocuments));
        OnPropertyChanged(nameof(HasCodCollections));
        OnPropertyChanged(nameof(HasCodHandovers));
        OnPropertyChanged(nameof(HasAnomalies));
        OnPropertyChanged(nameof(CustomersEmptyText));
        OnPropertyChanged(nameof(OrdersEmptyText));
        OnPropertyChanged(nameof(DocumentsEmptyText));
        OnPropertyChanged(nameof(CodCollectionsEmptyText));
        OnPropertyChanged(nameof(CodHandoversEmptyText));
        OnPropertyChanged(nameof(AnomaliesEmptyText));
    }

    private static string CsvCell(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
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
