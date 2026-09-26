using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed partial class LogisticsReportingViewModel : INotifyPropertyChanged
{
    private readonly ILogisticsReportingService _service;
    private readonly IAccessStateService _access;
    private long _accessGeneration;
    private long _loadGeneration;
    private bool _loaded;
    private Task<bool>? _initialLoadTask;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _selectedWarehouseId = string.Empty;
    private int _activeTabIndex;
    private LogisticsReportingDashboardData? _report;

    public LogisticsReportingViewModel(
        ILogisticsReportingService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;
        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            Reset();
            OnPropertyChanged(nameof(CanRead));
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LogisticsWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<LogisticsActorRow> Drivers { get; } = [];
    public ObservableCollection<LogisticsActorRow> Vehicles { get; } = [];
    public ObservableCollection<LogisticsFailureReasonRow> FailureReasons { get; } = [];
    public ObservableCollection<LogisticsTripRow> Trips { get; } = [];
    public ObservableCollection<LogisticsExceptionCard> ExceptionCards { get; } = [];

    public bool CanRead => _access.HasPermission("core.reporting.logistics.read");
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(IsNotBusy));
            OnPropertyChanged(nameof(ShowInitialLoading));
        }
    }
    public bool IsNotBusy => !IsBusy;
    public bool HasReport => _report is not null;
    public bool ShowInitialLoading => IsBusy && !HasReport;
    public bool HasNoDrivers => HasReport && Drivers.Count == 0;
    public bool HasNoVehicles => HasReport && Vehicles.Count == 0;
    public bool HasNoFailureReasons => HasReport && FailureReasons.Count == 0;
    public bool HasNoTrips => HasReport && Trips.Count == 0;

    public string Message { get => _message; private set => SetField(ref _message, value); }
    public bool MessageIsError { get => _messageIsError; private set => SetField(ref _messageIsError, value); }

    public DateTime? FromDate { get => _fromDate; set => SetField(ref _fromDate, value); }
    public DateTime? ToDate { get => _toDate; set => SetField(ref _toDate, value); }
    public string SelectedWarehouseId { get => _selectedWarehouseId; set => SetField(ref _selectedWarehouseId, value?.Trim() ?? string.Empty); }
    public int ActiveTabIndex { get => _activeTabIndex; set => SetField(ref _activeTabIndex, Math.Clamp(value, 0, 4)); }

    public string TripCount => LogisticsReportingPresentation.Count(_report?.Summary.TripCount);
    public string StopsOrders => $"{LogisticsReportingPresentation.Count(_report?.Summary.StopCount)} / {LogisticsReportingPresentation.Count(_report?.Summary.DeliveryOrderCount)}";
    public string DeliveredFullCount => LogisticsReportingPresentation.Count(_report?.Summary.DeliveredFullCount);
    public string DeliveredFullHint => $"Đúng hạn: {LogisticsReportingPresentation.Percent(_report?.Summary.OnTimeFullRatePercent)} trên {LogisticsReportingPresentation.Count(_report?.Summary.OnTimeEligibleFullCount)} phiếu có giờ dự kiến.";
    public string SlaCoverage => LogisticsReportingPresentation.Percent(_report?.Summary.SlaCoveragePercent);
    public string SlaCoverageHint => $"{LogisticsReportingPresentation.Count(_report?.Summary.FullWithoutPlanCount)} phiếu giao đủ thiếu giờ dự kiến.";
    public string OtherResults => $"{LogisticsReportingPresentation.Count(_report?.Summary.DeliveredPartialCount)} / {LogisticsReportingPresentation.Count(_report?.Summary.FailedCount)} / {LogisticsReportingPresentation.Count(_report?.Summary.RescheduledCount)}";
    public string AverageClosedTrip => LogisticsReportingPresentation.Duration(_report?.Summary.AverageClosedTripDurationMinutes);
    public string SourceNote => _report is null
        ? string.Empty
        : $"Nguồn dữ liệu: chuyến → điểm giao → phiếu giao → kết quả giao → đối soát. Cập nhật lúc {LogisticsReportingPresentation.Timestamp(_report.GeneratedAt)}.";

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);
        _initialLoadTask = LoadAsync(null, null, string.Empty);
        try { return await _initialLoadTask.ConfigureAwait(true); }
        finally { _initialLoadTask = null; }
    }

    public Task<bool> RefreshAsync() => LoadAsync(FromDate, ToDate, SelectedWarehouseId);

    public Task<bool> ApplyFiltersAsync() => LoadAsync(FromDate, ToDate, SelectedWarehouseId);

    public Task<bool> ResetToCurrentMonthAsync() => LoadAsync(null, null, string.Empty);

    private async Task<bool> LoadAsync(DateTime? from, DateTime? to, string warehouseId)
    {
        if (!CanRead)
        {
            SetError("Tài khoản chưa được cấp quyền xem Hiệu suất giao hàng.");
            return false;
        }

        if (from is not null && to is not null && from.Value.Date > to.Value.Date)
        {
            SetError("Ngày bắt đầu không được sau ngày kết thúc.");
            return false;
        }

        var generation = _accessGeneration;
        var load = ++_loadGeneration;
        IsBusy = true;
        ClearMessage();

        try
        {
            var data = await _service.GetAsync(
                FormatDate(from),
                FormatDate(to),
                warehouseId).ConfigureAwait(true);

            if (generation != _accessGeneration || load != _loadGeneration || !CanRead) return false;

            _report = data;
            _fromDate = ParseDate(data.Filters.From);
            _toDate = ParseDate(data.Filters.To);
            _selectedWarehouseId = data.Filters.WarehouseId ?? string.Empty;

            Replace(Warehouses,
            [
                new LogisticsWarehouseOption(string.Empty, "Tất cả kho được cấp quyền"),
                .. data.Warehouses.Select(row => new LogisticsWarehouseOption(
                    row.WarehouseId,
                    $"{row.WarehouseCode} — {row.WarehouseName}"))
            ]);
            Replace(Drivers, data.Drivers.Select(LogisticsReportingPresentation.Driver));
            Replace(Vehicles, data.Vehicles.Select(LogisticsReportingPresentation.Vehicle));
            Replace(FailureReasons, data.FailureReasons.Select(LogisticsReportingPresentation.FailureReason));
            Replace(Trips, data.Trips.Select(LogisticsReportingPresentation.Trip));

            var exceptionCards = new List<LogisticsExceptionCard>
            {
                new(
                    "Phiếu nhận hàng trả đã ghi nhận",
                    LogisticsReportingPresentation.Count(data.Reconciliation.PostedReturnReceiptCount),
                    $"{LogisticsReportingPresentation.Count(data.Reconciliation.TripsWithReturnReceiptCount)} chuyến có nhận hàng chưa giao về kho.")
            };
            exceptionCards.AddRange(data.DataQuality.Exceptions.Select(row =>
                new LogisticsExceptionCard(
                    LogisticsReportingPresentation.ExceptionLabel(row.ExceptionCode),
                    LogisticsReportingPresentation.Count(row.ExceptionCount),
                    "Cần xử lý tại nghiệp vụ nguồn; báo cáo không tự suy diễn.")));
            Replace(ExceptionCards, exceptionCards);

            _loaded = true;
            RaiseReportChanged();
            return true;
        }
        catch (Exception exception)
        {
            if (generation != _accessGeneration || load != _loadGeneration || !CanRead) return false;
            if (exception is CanonicalApiException apiException)
            {
                SetError(CanonicalErrorMessages.WithRequestId(
                    CanonicalErrorMessages.ToOfficeMessage(apiException),
                    apiException.RequestId));
            }
            else
            {
                SetError("Không tải được hiệu suất giao hàng. Vui lòng thử lại.");
            }
            return false;
        }
        finally
        {
            if (load == _loadGeneration) IsBusy = false;
        }
    }

    private void RaiseReportChanged()
    {
        OnPropertyChanged(nameof(HasReport));
        OnPropertyChanged(nameof(ShowInitialLoading));
        OnPropertyChanged(nameof(HasNoDrivers));
        OnPropertyChanged(nameof(HasNoVehicles));
        OnPropertyChanged(nameof(HasNoFailureReasons));
        OnPropertyChanged(nameof(HasNoTrips));
        OnPropertyChanged(nameof(FromDate));
        OnPropertyChanged(nameof(ToDate));
        OnPropertyChanged(nameof(SelectedWarehouseId));
        OnPropertyChanged(nameof(TripCount));
        OnPropertyChanged(nameof(StopsOrders));
        OnPropertyChanged(nameof(DeliveredFullCount));
        OnPropertyChanged(nameof(DeliveredFullHint));
        OnPropertyChanged(nameof(SlaCoverage));
        OnPropertyChanged(nameof(SlaCoverageHint));
        OnPropertyChanged(nameof(OtherResults));
        OnPropertyChanged(nameof(AverageClosedTrip));
        OnPropertyChanged(nameof(SourceNote));
    }

    private void Reset()
    {
        _report = null;
        _fromDate = null;
        _toDate = null;
        _selectedWarehouseId = string.Empty;
        _activeTabIndex = 0;
        Warehouses.Clear();
        Drivers.Clear();
        Vehicles.Clear();
        FailureReasons.Clear();
        Trips.Clear();
        ExceptionCards.Clear();
        ClearMessage();
        RaiseReportChanged();
        OnPropertyChanged(nameof(ActiveTabIndex));
    }

    private void SetError(string message)
    {
        MessageIsError = true;
        Message = message;
    }

    private void ClearMessage()
    {
        MessageIsError = false;
        Message = string.Empty;
    }

    private static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime? ParseDate(string value) =>
        DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;

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
