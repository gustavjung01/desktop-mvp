using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Operations;

public sealed record ImportExportFilterOption(string Value, string Label)
{
    public override string ToString() => Label;
}

public static class ImportExportHistoryPresentation
{
    public static string Direction(string value) =>
        value switch
        {
            "IMPORT" => "Nhập dữ liệu",
            "EXPORT" => "Xuất dữ liệu",
            _ => "Xử lý dữ liệu"
        };

    public static string Status(string value) =>
        (value ?? string.Empty).ToLowerInvariant() switch
        {
            "queued" => "Đang chờ",
            "running" => "Đang xử lý",
            "completed" => "Hoàn tất",
            "failed" => "Thất bại",
            "cancelled" => "Đã hủy",
            _ => "Trạng thái khác"
        };

    public static string Definition(string value)
    {
        var key = (value ?? string.Empty).ToLowerInvariant().Replace('_', '-');
        return key switch
        {
            "products" or "product" => "Sản phẩm",
            "customers" or "customer" => "Khách hàng",
            "suppliers" or "supplier" => "Nhà cung cấp",
            "product-categories" => "Loại sản phẩm",
            "product-brands" => "Nhãn hàng",
            "inventory" => "Tồn kho",
            "warehouses" => "Kho hàng",
            "sales-orders" => "Đơn bán hàng",
            "purchase-orders" => "Đơn mua hàng",
            "pricing" => "Giá bán",
            "receivables" => "Công nợ khách hàng",
            _ => "Dữ liệu nghiệp vụ"
        };
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

    public static string Time(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return value;
        return parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }
}

public sealed record ImportExportHistoryRow(
    string JobId,
    string RequestedAt,
    string Format,
    string Direction,
    string Definition,
    string Status,
    string CompletedAt,
    string Actor,
    string Source,
    string RowCount,
    string ResultState,
    string TechnicalRequestId,
    string TechnicalDefinitionKey,
    string TechnicalDefinitionVersion,
    string TechnicalActorId,
    string TechnicalSourceApp,
    string TechnicalFailureCode,
    bool HasFailureCode);

public sealed class ImportExportHistoryViewModel : INotifyPropertyChanged
{
    private const string Permission = "core.reporting.audit-history.read";

    private readonly IImportExportHistoryService _service;
    private readonly IAccessStateService _access;
    private bool _loaded;
    private bool _isBusy;
    private bool _messageIsError;
    private string _message = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private DateTime? _activeFrom;
    private DateTime? _activeTo;
    private ImportExportFilterOption _selectedDirection;
    private ImportExportFilterOption _selectedStatus;
    private string? _activeDirection;
    private string? _activeStatus;
    private string? _nextCursor;
    private bool _hasMore;
    private int _pageNumber = 1;
    private bool _filtersDirty;

    public ImportExportHistoryViewModel(
        IImportExportHistoryService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;
        _selectedDirection = DirectionOptions[0];
        _selectedStatus = StatusOptions[0];

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

    public ObservableCollection<ImportExportHistoryRow> Rows { get; } = [];

    public IReadOnlyList<ImportExportFilterOption> DirectionOptions { get; } =
    [
        new(string.Empty, "Tất cả"),
        new("IMPORT", "Nhập dữ liệu"),
        new("EXPORT", "Xuất dữ liệu")
    ];

    public IReadOnlyList<ImportExportFilterOption> StatusOptions { get; } =
    [
        new(string.Empty, "Tất cả"),
        new("queued", "Đang chờ"),
        new("running", "Đang xử lý"),
        new("completed", "Hoàn tất"),
        new("failed", "Thất bại"),
        new("cancelled", "Đã hủy")
    ];

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

    public ImportExportFilterOption SelectedDirection
    {
        get => _selectedDirection;
        set
        {
            if (!SetField(ref _selectedDirection, value ?? DirectionOptions[0])) return;
            FiltersDirty = true;
        }
    }

    public ImportExportFilterOption SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (!SetField(ref _selectedStatus, value ?? StatusOptions[0])) return;
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
        _activeDirection = EmptyToNull(SelectedDirection.Value);
        _activeStatus = EmptyToNull(SelectedStatus.Value);
        await LoadPageAsync(cursor: null, pageNumber: 1);
    }

    public async Task ClearFilterAsync()
    {
        if (!CanUseFilter) return;
        FromDate = null;
        ToDate = null;
        SelectedDirection = DirectionOptions[0];
        SelectedStatus = StatusOptions[0];
        _activeFrom = null;
        _activeTo = null;
        _activeDirection = null;
        _activeStatus = null;
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
        _activeDirection = null;
        _activeStatus = null;
        await LoadPageAsync(cursor: null, pageNumber: 1);
    }

    private async Task LoadPageAsync(string? cursor, int pageNumber)
    {
        if (!CanRead || IsBusy) return;
        Begin();
        try
        {
            var data = await _service.GetAsync(
                _activeFrom,
                _activeTo,
                _activeDirection,
                _activeStatus,
                definitionKey: null,
                cursor: cursor);

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
                    ? "Không tải được lịch sử nhập/xuất dữ liệu."
                    : exception.Message,
                isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static ImportExportHistoryRow Map(ImportExportHistoryRowData item)
    {
        var failure = !string.IsNullOrWhiteSpace(item.FailureCode);
        return new(
            item.JobId,
            ImportExportHistoryPresentation.Time(item.RequestedAt),
            string.IsNullOrWhiteSpace(item.Format) ? "—" : item.Format.ToUpperInvariant(),
            ImportExportHistoryPresentation.Direction(item.Direction),
            ImportExportHistoryPresentation.Definition(item.DefinitionKey),
            ImportExportHistoryPresentation.Status(item.Status),
            $"Hoàn tất: {ImportExportHistoryPresentation.Time(item.CompletedAt)}",
            item.EmployeeId is null ? "Tài khoản hệ thống" : "Nhân viên nội bộ",
            ImportExportHistoryPresentation.Source(item.SourceApp),
            item.RowCount is null ? "Chưa có số liệu" : $"{item.RowCount} dòng",
            failure ? "Có lỗi cần xử lý" : item.HasResult ? "Có kết quả" : "Chưa có kết quả",
            item.RequestId,
            item.DefinitionKey,
            item.DefinitionVersion,
            item.ActorId,
            item.SourceApp,
            item.FailureCode ?? string.Empty,
            failure);
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

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
