using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Settings;

public sealed class EmployeeMcpReportingViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.reporting.employee-mcp.read";

    private readonly IEmployeeMcpReportingService _service;
    private readonly IAccessStateService _access;
    private EmployeeMcpDashboardData? _report;
    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private int _activeTabIndex;
    private long _accessGeneration;
    private long _loadGeneration;
    private CancellationTokenSource? _loadCts;

    public EmployeeMcpReportingViewModel(
        IEmployeeMcpReportingService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loadCts?.Cancel();
            _loadCts = null;
            _loaded = false;
            _report = null;
            IsBusy = false;
            Message = string.Empty;
            MessageIsError = false;
            FromDate = null;
            ToDate = null;
            ActiveTabIndex = 0;
            ActorRows.Clear();
            RouteRows.Clear();
            SessionRows.Clear();
            QualityRows.Clear();
            OnPropertyChanged(nameof(CanRead));
            OnPropertyChanged(nameof(CanApply));
            RaiseReportState();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<EmployeeMcpActorRowView> ActorRows { get; } = [];
    public ObservableCollection<EmployeeMcpRouteRowView> RouteRows { get; } = [];
    public ObservableCollection<EmployeeMcpSessionRowView> SessionRows { get; } = [];
    public ObservableCollection<EmployeeMcpQualityRowView> QualityRows { get; } = [];

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanApply => CanRead && !IsBusy;
    public bool HasReport => _report is not null;
    public bool ShowInitialLoading => IsBusy && !HasReport;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasRoutes => RouteRows.Count > 0;
    public bool HasSessions => SessionRows.Count > 0;
    public bool HasActors => ActorRows.Count > 0;
    public bool HasQualityIssues => QualityRows.Count > 0;
    public string RouteEmptyText => HasReport && !HasRoutes ? "Không có tuyến phát sinh hoạt động." : string.Empty;
    public string SessionEmptyText => HasReport && !HasSessions ? "Không có phiên trong kỳ." : string.Empty;
    public string ActorVisitEmptyText => HasReport && !HasActors ? "Không có hoạt động điểm bán trong kỳ." : string.Empty;
    public string ActorOrderEmptyText => HasReport && !HasActors ? "Không có nhu cầu hoặc đơn trong kỳ." : string.Empty;
    public string ActorEffectivenessEmptyText => HasReport && !HasActors ? "Không có hoạt động thị trường trong kỳ." : string.Empty;
    public string QualityEmptyText => HasReport && !HasQualityIssues ? "Không có dữ liệu cần đối soát trong kỳ." : string.Empty;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(ApplyText));
            OnPropertyChanged(nameof(ShowInitialLoading));
        }
    }

    public string ApplyText => IsBusy ? "Đang cập nhật…" : "Áp dụng";

    public string Message
    {
        get => _message;
        private set
        {
            if (!SetField(ref _message, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasMessage));
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
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

    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set => SetField(ref _activeTabIndex, Math.Clamp(value, 0, 4));
    }

    public string SessionRouteText => _report is null
        ? "0 / 0"
        : $"{EmployeeMcpReportingPresentation.Count(_report.Summary.SessionCount)} / {EmployeeMcpReportingPresentation.Count(_report.Summary.RouteCount)}";

    public string PlannedVisitedText => _report is null
        ? "0 / 0"
        : $"{EmployeeMcpReportingPresentation.Count(_report.Summary.PlannedOutletCount)} / {EmployeeMcpReportingPresentation.Count(_report.Summary.VisitedOutletCount)}";

    public string PlannedVisitedHint => $"Hoàn thành điểm kế hoạch: {EmployeeMcpReportingPresentation.Percent(_report?.Summary.PlannedVisitRatePercent)}.";

    public string CheckedInVisitText => _report is null
        ? "0 / 0"
        : $"{EmployeeMcpReportingPresentation.Count(_report.Summary.CheckedInOutletCount)} / {EmployeeMcpReportingPresentation.Count(_report.Summary.VisitCount)}";

    public string OrderIntentText => EmployeeMcpReportingPresentation.Count(_report?.Summary.OrderIntentCount);
    public string OrderIntentHint => $"Tỷ lệ từ điểm đã ghé sang nhu cầu mua: {EmployeeMcpReportingPresentation.Percent(_report?.Summary.OrderIntentConversionPercent)}.";

    public string OnboardingText => _report is null
        ? "0 / 0"
        : $"{EmployeeMcpReportingPresentation.Count(_report.Summary.OnboardingConvertedCount)} / {EmployeeMcpReportingPresentation.Count(_report.Summary.OnboardingSubmittedCount)}";

    public string OnboardingHint => $"Đã duyệt hoặc liên kết trên tổng đề nghị đã gửi: {EmployeeMcpReportingPresentation.Percent(_report?.Summary.OnboardingConversionPercent)}.";

    public string CoreOrderText => EmployeeMcpReportingPresentation.Count(_report?.Summary.CoreSalesOrderCount);
    public string CoreOrderHint => $"Nhu cầu mua chuyển thành đơn Công Ty: {EmployeeMcpReportingPresentation.Percent(_report?.Summary.CoreOrderConversionPercent)}.";

    public string ScopeText => _report is null
        ? string.Empty
        : _report.Scope.Basis == "EMPLOYEE_CODE"
            ? $"Phạm vi: nhân viên {EmployeeMcpReportingPresentation.Display(_report.Scope.EmployeeCode)}."
            : "Phạm vi: toàn đơn vị theo quyền hiện hành.";

    public string GeneratedText => _report is null
        ? string.Empty
        : EmployeeMcpReportingPresentation.GeneratedAt(_report.GeneratedAt, _report.Timezone);

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanRead || !_access.Current.IsAuthenticated) return;
        await LoadAsync(null, null);
    }

    public Task ApplyAsync() => LoadAsync(FromDate, ToDate);

    public Task ResetCurrentMonthAsync()
    {
        FromDate = null;
        ToDate = null;
        return LoadAsync(null, null);
    }

    public Task RefreshAsync() => LoadAsync(FromDate, ToDate);

    private async Task LoadAsync(DateTime? from, DateTime? to)
    {
        if (!CanRead || !_access.Current.IsAuthenticated) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var cancellationToken = _loadCts.Token;
        var accessGeneration = _accessGeneration;
        var loadGeneration = ++_loadGeneration;

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;

        try
        {
            var report = await _service.GetAsync(from, to, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested
                || accessGeneration != _accessGeneration
                || loadGeneration != _loadGeneration)
                return;

            RunOnUiThread(() => ApplyReport(report));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (accessGeneration != _accessGeneration || loadGeneration != _loadGeneration) return;
            RunOnUiThread(() =>
            {
                MessageIsError = true;
                Message = PublicError(exception);
            });
        }
        finally
        {
            if (accessGeneration == _accessGeneration && loadGeneration == _loadGeneration)
                RunOnUiThread(() => IsBusy = false);
        }
    }

    private void ApplyReport(EmployeeMcpDashboardData report)
    {
        _report = report;
        _loaded = true;
        FromDate = EmployeeMcpReportingPresentation.ParseDate(report.Filters.From);
        ToDate = EmployeeMcpReportingPresentation.ParseDate(report.Filters.To);

        Replace(ActorRows, report.FieldActors.Select(EmployeeMcpActorRowView.From));
        Replace(RouteRows, report.Routes.Select(EmployeeMcpRouteRowView.From));
        Replace(SessionRows, report.Sessions.Select(EmployeeMcpSessionRowView.From));

        QualityRows.Clear();
        foreach (var row in report.DataQuality.UnmappedActors)
            QualityRows.Add(EmployeeMcpQualityRowView.From(row));
        foreach (var row in report.DataQuality.CounterMismatches)
            QualityRows.Add(EmployeeMcpQualityRowView.From(row));

        RaiseReportState();
    }

    private void RaiseReportState()
    {
        foreach (var property in new[]
        {
            nameof(HasReport),
            nameof(HasRoutes),
            nameof(HasSessions),
            nameof(HasActors),
            nameof(HasQualityIssues),
            nameof(RouteEmptyText),
            nameof(SessionEmptyText),
            nameof(ActorVisitEmptyText),
            nameof(ActorOrderEmptyText),
            nameof(ActorEffectivenessEmptyText),
            nameof(QualityEmptyText),
            nameof(SessionRouteText),
            nameof(PlannedVisitedText),
            nameof(PlannedVisitedHint),
            nameof(CheckedInVisitText),
            nameof(OrderIntentText),
            nameof(OrderIntentHint),
            nameof(OnboardingText),
            nameof(OnboardingHint),
            nameof(CoreOrderText),
            nameof(CoreOrderHint),
            nameof(ScopeText),
            nameof(GeneratedText)
        })
            OnPropertyChanged(property);
    }

    private static string PublicError(Exception exception) =>
        exception is CanonicalApiException canonical
            ? canonical.Message
            : "Không tải được báo cáo nhân viên thị trường.";

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> rows)
    {
        target.Clear();
        foreach (var row in rows) target.Add(row);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
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
