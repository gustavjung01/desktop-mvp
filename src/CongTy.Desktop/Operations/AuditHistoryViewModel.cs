using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Operations;

public static class AuditHistoryPresentation
{
    public static string Action(string value)
    {
        var action = (value ?? string.Empty).ToLowerInvariant();
        if (action.Contains("create", StringComparison.Ordinal)) return "Tạo mới";
        if (action.Contains("confirm", StringComparison.Ordinal)) return "Xác nhận";
        if (action.Contains("cancel", StringComparison.Ordinal)) return "Hủy";
        if (action.Contains("delete", StringComparison.Ordinal) || action.Contains("remove", StringComparison.Ordinal)) return "Xóa";
        if (action.Contains("deactivate", StringComparison.Ordinal) || action.Contains("disable", StringComparison.Ordinal)) return "Ngừng sử dụng";
        if (action.Contains("activate", StringComparison.Ordinal)) return "Đưa vào sử dụng";
        if (action.Contains("import", StringComparison.Ordinal)) return "Nhập dữ liệu";
        if (action.Contains("export", StringComparison.Ordinal)) return "Xuất dữ liệu";
        if (action.Contains("update", StringComparison.Ordinal)
            || action.Contains("change", StringComparison.Ordinal)
            || action.Contains("edit", StringComparison.Ordinal)) return "Cập nhật";
        return "Thao tác hệ thống";
    }

    public static string Resource(string value)
    {
        var resource = (value ?? string.Empty).ToLowerInvariant();
        if (resource.Contains("customer", StringComparison.Ordinal)) return "Khách hàng";
        if (resource.Contains("supplier", StringComparison.Ordinal)) return "Nhà cung cấp";
        if (resource.Contains("product", StringComparison.Ordinal) || resource.Contains("sku", StringComparison.Ordinal)) return "Sản phẩm";
        if (resource.Contains("sales", StringComparison.Ordinal) && resource.Contains("order", StringComparison.Ordinal)) return "Đơn bán hàng";
        if (resource.Contains("purchase", StringComparison.Ordinal) && resource.Contains("order", StringComparison.Ordinal)) return "Đơn mua hàng";
        if (resource.Contains("warehouse", StringComparison.Ordinal)) return "Kho hàng";
        if (resource.Contains("inventory", StringComparison.Ordinal)) return "Tồn kho";
        if (resource.Contains("payment", StringComparison.Ordinal) || resource.Contains("receivable", StringComparison.Ordinal)) return "Công nợ và thanh toán";
        if (resource.Contains("role", StringComparison.Ordinal) || resource.Contains("permission", StringComparison.Ordinal)) return "Vai trò và phân quyền";
        if (resource.Contains("user", StringComparison.Ordinal) || resource.Contains("employee", StringComparison.Ordinal)) return "Người dùng và nhân sự";
        return "Dữ liệu nghiệp vụ";
    }

    public static string Source(string value)
    {
        var source = (value ?? string.Empty).ToLowerInvariant();
        if (source.Contains("mcp", StringComparison.Ordinal)) return "Ứng dụng nhân viên thị trường";
        if (source.Contains("delivery", StringComparison.Ordinal)) return "Ứng dụng giao hàng";
        if (source.Contains("admin", StringComparison.Ordinal)) return "Ứng dụng quản trị";
        if (source.Contains("web", StringComparison.Ordinal)
            || source.Contains("npp", StringComparison.Ordinal)
            || source.Contains("core", StringComparison.Ordinal)) return "Hệ thống điều hành";
        return "Hệ thống nội bộ";
    }

    public static string Time(string value)
    {
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return value;
        return parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }
}

public sealed record AuditHistoryRow(
    string AuditId,
    string OccurredAt,
    string Action,
    string ResourceType,
    string Actor,
    string Source,
    string ChangeSummary,
    string TechnicalAction,
    string TechnicalResourceType,
    string TechnicalResourceId,
    string TechnicalActorId,
    string TechnicalSourceApp,
    string TechnicalRequestId);

public sealed class AuditHistoryViewModel : INotifyPropertyChanged
{
    private const string Permission = "core.reporting.audit-history.read";

    private readonly IAuditHistoryService _service;
    private readonly IAccessStateService _access;
    private bool _loaded;
    private bool _isBusy;
    private bool _messageIsError;
    private string _message = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private DateTime? _activeFrom;
    private DateTime? _activeTo;
    private string? _nextCursor;
    private bool _hasMore;
    private int _pageNumber = 1;
    private bool _filtersDirty;

    public AuditHistoryViewModel(
        IAuditHistoryService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;
        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            Rows.Clear();
            _nextCursor = null;
            HasMore = false;
            PageNumber = 1;
            Message = string.Empty;
            OnPropertyChanged(nameof(CanRead));
            RaiseActions();
            if (_access.Current.IsAuthenticated && CanRead) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AuditHistoryRow> Rows { get; } = [];

    public bool CanRead => _access.HasPermission(Permission);
    public bool CanUseFilter => CanRead && !IsBusy;
    public bool CanLoadNext => CanRead && !IsBusy && HasMore && !FiltersDirty && !string.IsNullOrWhiteSpace(_nextCursor);
    public bool HasRows => Rows.Count > 0;
    public bool ShowEmpty => !IsBusy && !HasRows && !MessageIsError;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaiseActions();
            OnPropertyChanged(nameof(ShowEmpty));
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public string Message
    {
        get => _message;
        private set
        {
            if (!SetField(ref _message, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasMessage));
            OnPropertyChanged(nameof(ShowEmpty));
        }
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            if (!SetField(ref _fromDate, value?.Date)) return;
            FiltersDirty = true;
        }
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            if (!SetField(ref _toDate, value?.Date)) return;
            FiltersDirty = true;
        }
    }

    public bool HasMore
    {
        get => _hasMore;
        private set
        {
            if (!SetField(ref _hasMore, value)) return;
            RaiseActions();
        }
    }

    public int PageNumber
    {
        get => _pageNumber;
        private set
        {
            if (!SetField(ref _pageNumber, value)) return;
            OnPropertyChanged(nameof(PageText));
        }
    }

    public string PageText => $"Trang {PageNumber}";

    public bool FiltersDirty
    {
        get => _filtersDirty;
        private set
        {
            if (!SetField(ref _filtersDirty, value)) return;
            RaiseActions();
        }
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || IsBusy || !CanRead) return;
        await LoadFirstPageAsync();
    }

    public async Task ApplyFilterAsync()
    {
        if (!CanUseFilter) return;
        if (FromDate.HasValue && ToDate.HasValue && FromDate.Value.Date > ToDate.Value.Date)
        {
            SetMessage("Từ ngày không được sau Đến ngày.", isError: true);
            return;
        }

        _activeFrom = FromDate;
        _activeTo = ToDate;
        await LoadPageAsync(cursor: null, pageNumber: 1);
    }

    public async Task ClearFilterAsync()
    {
        if (!CanUseFilter) return;
        FromDate = null;
        ToDate = null;
        _activeFrom = null;
        _activeTo = null;
        await LoadPageAsync(cursor: null, pageNumber: 1);
    }

    public async Task LoadNextAsync()
    {
        if (!CanLoadNext || string.IsNullOrWhiteSpace(_nextCursor)) return;
        await LoadPageAsync(_nextCursor, PageNumber + 1);
    }

    public async Task RefreshAsync()
    {
        if (!CanRead || IsBusy) return;
        if (FiltersDirty)
        {
            await ApplyFilterAsync();
            return;
        }
        await LoadPageAsync(cursor: null, pageNumber: 1);
    }

    private async Task LoadFirstPageAsync()
    {
        _activeFrom = null;
        _activeTo = null;
        await LoadPageAsync(cursor: null, pageNumber: 1);
    }

    private async Task LoadPageAsync(string? cursor, int pageNumber)
    {
        if (!CanRead || IsBusy) return;
        Begin();
        try
        {
            var data = await _service.GetAsync(_activeFrom, _activeTo, cursor);
            Rows.Clear();
            foreach (var item in data.Rows)
                Rows.Add(Map(item));

            _nextCursor = data.Page.NextCursor;
            HasMore = data.Page.HasMore && !string.IsNullOrWhiteSpace(_nextCursor);
            PageNumber = pageNumber;
            FiltersDirty = false;
            _loaded = true;
            OnPropertyChanged(nameof(HasRows));
            OnPropertyChanged(nameof(ShowEmpty));
            SetMessage(string.Empty);
        }
        catch (Exception exception)
        {
            _nextCursor = null;
            HasMore = false;
            SetMessage(
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Không tải được lịch sử thay đổi."
                    : exception.Message,
                isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static AuditHistoryRow Map(AuditHistoryRowData item) =>
        new(
            item.AuditId,
            AuditHistoryPresentation.Time(item.OccurredAt),
            AuditHistoryPresentation.Action(item.Action),
            AuditHistoryPresentation.Resource(item.ResourceType),
            item.EmployeeId is null ? "Tài khoản hệ thống" : "Nhân viên nội bộ",
            AuditHistoryPresentation.Source(item.SourceApp),
            item.HasBeforeData || item.HasAfterData ? "Có nội dung thay đổi" : "Ghi nhận thao tác",
            item.Action,
            item.ResourceType,
            string.IsNullOrWhiteSpace(item.ResourceId) ? "—" : item.ResourceId,
            item.ActorId,
            item.SourceApp,
            item.RequestId);

    private void Begin()
    {
        IsBusy = true;
        SetMessage(string.Empty);
    }

    private void SetMessage(string value, bool isError = false)
    {
        MessageIsError = isError;
        Message = value;
    }

    private void RaiseActions()
    {
        OnPropertyChanged(nameof(CanUseFilter));
        OnPropertyChanged(nameof(CanLoadNext));
    }

    private void RunOnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher is null || Application.Current.Dispatcher.CheckAccess())
            action();
        else
            Application.Current.Dispatcher.Invoke(action);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
