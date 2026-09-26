using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed partial class AgingReportingViewModel : INotifyPropertyChanged
{
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Tuổi nợ.";

    private readonly IAgingReportingService _service;
    private readonly IAccessStateService _access;

    private AgingDashboardData? _report;
    private bool _loaded;
    private Task<bool>? _initialLoadTask;
    private long _accessGeneration;
    private long _loadGeneration;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _reportError = string.Empty;
    private string _selectedWarehouseId = string.Empty;
    private int _activeTabIndex;

    public AgingReportingViewModel(
        IAgingReportingService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            IsBusy = false;
            _loaded = false;
            _initialLoadTask = null;
            _report = null;
            Warehouses.Clear();
            ReceivableSummary.Clear();
            ReceivableCustomers.Clear();
            PayableSummary.Clear();
            PayableSuppliers.Clear();
            _selectedWarehouseId = string.Empty;
            ClearMessage();
            ReportError = string.Empty;
            RaiseAll();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AgingWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<AgingSummaryRow> ReceivableSummary { get; } = [];
    public ObservableCollection<AgingPartyRow> ReceivableCustomers { get; } = [];
    public ObservableCollection<AgingSummaryRow> PayableSummary { get; } = [];
    public ObservableCollection<AgingPartyRow> PayableSuppliers { get; } = [];

    public bool CanRead => _access.HasPermission("core.reporting.aging.read");
    public bool CanApply => CanRead && !IsBusy;
    public bool HasReport => _report is not null;
    public bool ShowInitialLoading => IsBusy && !HasReport;
    public bool HasReportError => !string.IsNullOrWhiteSpace(ReportError);
    public bool HasReceivableSummary => ReceivableSummary.Count > 0;
    public bool HasReceivableCustomers => ReceivableCustomers.Count > 0;
    public bool HasPayableSummary => PayableSummary.Count > 0;
    public bool HasPayableSuppliers => PayableSuppliers.Count > 0;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanApply));
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

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set => SetField(ref _selectedWarehouseId, value ?? string.Empty);
    }

    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set => SetField(ref _activeTabIndex, Math.Clamp(value, 0, 1));
    }

    public string CurrentDateText => _report is null
        ? "—"
        : AgingReportingPresentation.Date(_report.CurrentDate);

    public string GeneratedAtText => _report is null
        ? string.Empty
        : $"Cập nhật: {AgingReportingPresentation.GeneratedAt(_report.GeneratedAt)}";

    public string BasisNotice =>
        _report is null
            ? string.Empty
            : $"Ngày chốt hiện tại: {CurrentDateText}. Phải thu được phân tuổi từ ngày chứng từ; phải trả dùng ngày đến hạn trên chứng từ. Mỗi loại tiền tệ được giữ riêng, không cộng gộp.";

    public string ReceivableSummaryEmptyText =>
        !IsBusy && HasReport && !HasReceivableSummary ? "Không có khoản phải thu đang mở." : string.Empty;

    public string ReceivableCustomersEmptyText =>
        !IsBusy && HasReport && !HasReceivableCustomers ? "Không có dữ liệu." : string.Empty;

    public string PayableSummaryEmptyText =>
        !IsBusy && HasReport && !HasPayableSummary ? "Không có khoản phải trả đang mở." : string.Empty;

    public string PayableSuppliersEmptyText =>
        !IsBusy && HasReport && !HasPayableSuppliers ? "Không có dữ liệu." : string.Empty;

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);

        _initialLoadTask = LoadAsync(string.Empty);
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

    public Task<bool> ApplyAsync() =>
        LoadAsync(SelectedWarehouseId);

    public Task<bool> ResetAsync()
    {
        SelectedWarehouseId = string.Empty;
        return LoadAsync(string.Empty);
    }

    public Task<bool> RefreshAsync() =>
        LoadAsync(SelectedWarehouseId);

    private async Task<bool> LoadAsync(string? warehouseId)
    {
        if (IsBusy) return false;

        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetError(ReadDenied);
            return false;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        IsBusy = true;
        ReportError = string.Empty;

        try
        {
            var report = await _service.GetAsync(warehouseId).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead)
                return false;

            ApplyReport(report);
            _loaded = true;
            Message = "Tuổi nợ đã được cập nhật.";
            MessageIsError = false;
            return true;
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
                SetError(string.IsNullOrWhiteSpace(exception.Message) ? "Không tải được báo cáo tuổi nợ." : exception.Message);
            return false;
        }
        finally
        {
            if (request == _loadGeneration) IsBusy = false;
        }
    }

    private void ApplyReport(AgingDashboardData report)
    {
        _report = report ?? throw new InvalidOperationException("Báo cáo tuổi nợ không có dữ liệu.");

        var requestedWarehouse = report.Filters.WarehouseId ?? string.Empty;
        Replace(
            Warehouses,
            new[] { new AgingWarehouseOption(string.Empty, "Tất cả kho được cấp quyền") }
                .Concat(report.ScopeWarehouses.Select(AgingReportingPresentation.Warehouse)));
        SelectedWarehouseId = Warehouses.Any(row => row.Id == requestedWarehouse)
            ? requestedWarehouse
            : string.Empty;

        Replace(ReceivableSummary, report.Receivable.Summary.Select(AgingReportingPresentation.ReceivableSummary));
        Replace(ReceivableCustomers, report.Receivable.Customers.Select(AgingReportingPresentation.ReceivableParty));
        Replace(PayableSummary, report.Payable.Summary.Select(AgingReportingPresentation.PayableSummary));
        Replace(PayableSuppliers, report.Payable.Suppliers.Select(AgingReportingPresentation.PayableParty));

        RaiseAll();
    }

    private void SetError(string message)
    {
        ReportError = string.IsNullOrWhiteSpace(message) ? "Không tải được báo cáo tuổi nợ." : message;
        Message = ReportError;
        MessageIsError = true;
    }

    private void ClearMessage()
    {
        Message = string.Empty;
        MessageIsError = false;
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(HasReport));
        OnPropertyChanged(nameof(ShowInitialLoading));
        OnPropertyChanged(nameof(CurrentDateText));
        OnPropertyChanged(nameof(GeneratedAtText));
        OnPropertyChanged(nameof(BasisNotice));
        RaiseEmptyStates();
    }

    private void RaiseEmptyStates()
    {
        OnPropertyChanged(nameof(HasReceivableSummary));
        OnPropertyChanged(nameof(HasReceivableCustomers));
        OnPropertyChanged(nameof(HasPayableSummary));
        OnPropertyChanged(nameof(HasPayableSuppliers));
        OnPropertyChanged(nameof(ReceivableSummaryEmptyText));
        OnPropertyChanged(nameof(ReceivableCustomersEmptyText));
        OnPropertyChanged(nameof(PayableSummaryEmptyText));
        OnPropertyChanged(nameof(PayableSuppliersEmptyText));
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
