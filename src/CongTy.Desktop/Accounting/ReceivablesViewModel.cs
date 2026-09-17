using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed class ReceivablesViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.receivable.read";
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Công nợ phải thu.";

    private readonly IReceivablesService _service;
    private readonly IAccessStateService _access;

    private ReceivableDocumentData[] _documents = [];
    private CustomerReceivableBalanceData[] _balances = [];
    private ReceivableDocumentData? _detail;
    private bool _loaded;
    private Task<bool>? _initialLoadTask;
    private long _accessGeneration;
    private long _loadGeneration;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _pageError = string.Empty;
    private string _detailError = string.Empty;

    public ReceivablesViewModel(
        IReceivablesService service,
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
            _documents = [];
            _balances = [];
            _detail = null;
            Balances.Clear();
            Documents.Clear();
            DetailLines.Clear();
            DetailLedger.Clear();
            ClearMessage();
            PageError = string.Empty;
            DetailError = string.Empty;
            RaiseAll();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ReceivableBalanceRow> Balances { get; } = [];
    public ObservableCollection<ReceivableDocumentRow> Documents { get; } = [];
    public ObservableCollection<ReceivableLineRow> DetailLines { get; } = [];
    public ObservableCollection<ReceivableLedgerRow> DetailLedger { get; } = [];

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanRefresh => CanRead && !IsBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanRefresh));
            OnPropertyChanged(nameof(RefreshText));
            OnPropertyChanged(nameof(ShowInitialLoading));
        }
    }

    public string RefreshText => IsBusy ? "Đang tải…" : "Tải lại";
    public bool ShowInitialLoading => IsBusy && !_loaded;
    public bool HasBalances => Balances.Count > 0;
    public bool HasDocuments => Documents.Count > 0;
    public bool HasDetail => _detail is not null;
    public bool HasPageError => !string.IsNullOrWhiteSpace(PageError);
    public bool HasDetailError => !string.IsNullOrWhiteSpace(DetailError);

    public string PageError
    {
        get => _pageError;
        private set
        {
            if (!SetField(ref _pageError, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasPageError));
        }
    }

    public string DetailError
    {
        get => _detailError;
        private set
        {
            if (!SetField(ref _detailError, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasDetailError));
        }
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

    public string TotalBalanceText
    {
        get
        {
            var total = _balances
                .Where(item => string.Equals(item.CurrencyCode, "VND", StringComparison.OrdinalIgnoreCase))
                .Sum(item => ReceivablesPresentation.Amount(item.Balance));
            return ReceivablesPresentation.Money(
                total.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "VND");
        }
    }

    public string TotalOpenText
    {
        get
        {
            var total = _balances
                .Where(item => string.Equals(item.CurrencyCode, "VND", StringComparison.OrdinalIgnoreCase))
                .Sum(item => ReceivablesPresentation.Amount(item.OpenAmount));
            return ReceivablesPresentation.Money(
                total.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "VND");
        }
    }

    public string OpenDocumentCountText =>
        _documents.Count(item => item.Status is "open" or "partially_allocated")
            .ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));

    public string BalanceEmptyText =>
        !IsBusy && _loaded && !HasBalances ? "Chưa có số dư công nợ khách hàng." : string.Empty;

    public string DocumentsEmptyText =>
        !IsBusy && _loaded && !HasDocuments ? "Chưa phát sinh chứng từ công nợ khách hàng." : string.Empty;

    public string DetailTitle
    {
        get
        {
            if (_detail is null) return string.Empty;
            var number = !string.IsNullOrWhiteSpace(_detail.DeliveryOrderNumber)
                ? _detail.DeliveryOrderNumber
                : _detail.SourceDocumentNumber;
            return $"{ReceivablesPresentation.Source(_detail.SourceDocumentType)} · {number}";
        }
    }

    public string DetailCustomer => _detail is null
        ? "—"
        : ReceivablesPresentation.Party(_detail.CustomerCode, _detail.CustomerName);

    public string DetailSalesOrder => _detail is null || string.IsNullOrWhiteSpace(_detail.SalesOrderNumber)
        ? "Chưa có số chứng từ"
        : _detail.SalesOrderNumber;

    public string DetailOriginalAmount => _detail is null
        ? "—"
        : ReceivablesPresentation.Money(_detail.OriginalAmount, _detail.CurrencyCode);

    public string DetailRemainingAmount => _detail is null
        ? "—"
        : ReceivablesPresentation.Money(_detail.RemainingAmount, _detail.CurrencyCode);

    public string DetailCollectionPolicy => _detail is null
        ? "—"
        : ReceivablesPresentation.CollectionPolicy(_detail.CollectionPolicy);

    public string DetailStatus => _detail is null
        ? "—"
        : ReceivablesPresentation.Status(_detail.Status);

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);

        _initialLoadTask = LoadAsync();
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

    public Task<bool> RefreshAsync() => LoadAsync();

    public async Task OpenDetailAsync(string id)
    {
        if (IsBusy) return;
        if (!CanRead)
        {
            if (_access.Current.IsAuthenticated) SetError(ReadDenied);
            return;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        IsBusy = true;
        DetailError = string.Empty;

        try
        {
            var detail = await _service.GetDocumentAsync(id).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead) return;

            ApplyDetail(detail);
            Message = "Đã tải chi tiết công nợ.";
            MessageIsError = false;
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
            {
                DetailError = CanonicalErrorMessages.WithRequestId(
                    CanonicalErrorMessages.ToOfficeMessage(exception),
                    exception.RequestId);
                Message = DetailError;
                MessageIsError = true;
            }
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
            {
                DetailError = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Không tải được chi tiết chứng từ công nợ đã chọn."
                    : exception.Message;
                Message = DetailError;
                MessageIsError = true;
            }
        }
        finally
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
                IsBusy = false;
        }
    }

    public void CloseDetail()
    {
        _detail = null;
        DetailLines.Clear();
        DetailLedger.Clear();
        DetailError = string.Empty;
        RaiseDetail();
    }

    private async Task<bool> LoadAsync()
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
        PageError = string.Empty;

        try
        {
            var documentsTask = _service.ListDocumentsAsync();
            var balancesTask = _service.ListBalancesAsync();
            await Task.WhenAll(documentsTask, balancesTask).ConfigureAwait(true);

            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead)
                return false;

            ApplyLists(
                await documentsTask.ConfigureAwait(true),
                await balancesTask.ConfigureAwait(true));
            _loaded = true;
            Message = "Công nợ phải thu đã được cập nhật.";
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
            {
                SetError(string.IsNullOrWhiteSpace(exception.Message)
                    ? "Không tải được đầy đủ dữ liệu công nợ khách hàng. Hãy thử tải lại."
                    : exception.Message);
            }
            return false;
        }
        finally
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
                IsBusy = false;
        }
    }

    private void ApplyLists(
        IReadOnlyList<ReceivableDocumentData> documents,
        IReadOnlyList<CustomerReceivableBalanceData> balances)
    {
        _documents = documents.ToArray();
        _balances = balances.ToArray();

        Balances.Clear();
        var balanceIndex = 0;
        foreach (var item in balances)
        {
            balanceIndex++;
            Balances.Add(new ReceivableBalanceRow(
                balanceIndex,
                ReceivablesPresentation.Party(item.CustomerCode, item.CustomerName),
                item.CurrencyCode,
                ReceivablesPresentation.Money(item.Balance, item.CurrencyCode),
                ReceivablesPresentation.Money(item.OpenAmount, item.CurrencyCode),
                item.OpenDocumentCount));
        }

        Documents.Clear();
        var documentIndex = 0;
        foreach (var item in documents)
        {
            documentIndex++;
            var warehouse = ReceivablesPresentation.Party(item.WarehouseCode, item.WarehouseName);
            Documents.Add(new ReceivableDocumentRow(
                documentIndex,
                item.Id,
                ReceivablesPresentation.Source(item.SourceDocumentType),
                ReceivablesPresentation.Date(item.SourceDocumentDate),
                ReceivablesPresentation.Party(item.CustomerCode, item.CustomerName),
                ReceivablesPresentation.DocumentReference(item),
                warehouse,
                ReceivablesPresentation.Status(item.Status),
                ReceivablesPresentation.Money(item.OriginalAmount, item.CurrencyCode),
                ReceivablesPresentation.Money(item.RemainingAmount, item.CurrencyCode)));
        }

        OnPropertyChanged(nameof(TotalBalanceText));
        OnPropertyChanged(nameof(TotalOpenText));
        OnPropertyChanged(nameof(OpenDocumentCountText));
        RaiseEmptyStates();
    }

    private void ApplyDetail(ReceivableDocumentData detail)
    {
        _detail = detail;

        DetailLines.Clear();
        var lineIndex = 0;
        foreach (var line in detail.Lines)
        {
            lineIndex++;
            var quantity = $"{OfficeNumberFormatting.Compact(line.AcceptedBaseQuantity)} {line.UnitCode}".Trim();
            DetailLines.Add(new ReceivableLineRow(
                lineIndex,
                line.Sku,
                line.ItemName,
                quantity,
                ReceivablesPresentation.Money(line.GrossAmount, detail.CurrencyCode),
                ReceivablesPresentation.Money(line.DiscountAmount, detail.CurrencyCode),
                ReceivablesPresentation.Money(line.TaxAmount, detail.CurrencyCode),
                ReceivablesPresentation.Money(line.LineAmount, detail.CurrencyCode)));
        }

        DetailLedger.Clear();
        var entryIndex = 0;
        foreach (var entry in detail.LedgerEntries)
        {
            entryIndex++;
            DetailLedger.Add(new ReceivableLedgerRow(
                entryIndex,
                ReceivablesPresentation.DateTimeText(entry.OccurredAt),
                ReceivablesPresentation.LedgerEntry(entry.EntryType),
                ReceivablesPresentation.Source(detail.SourceDocumentType),
                ReceivablesPresentation.Money(entry.Amount, detail.CurrencyCode)));
        }

        RaiseDetail();
    }

    private void SetError(string message)
    {
        PageError = message;
        Message = message;
        MessageIsError = true;
    }

    private void ClearMessage()
    {
        Message = string.Empty;
        MessageIsError = false;
    }

    private void RaiseEmptyStates()
    {
        OnPropertyChanged(nameof(HasBalances));
        OnPropertyChanged(nameof(HasDocuments));
        OnPropertyChanged(nameof(BalanceEmptyText));
        OnPropertyChanged(nameof(DocumentsEmptyText));
    }

    private void RaiseDetail()
    {
        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(DetailTitle));
        OnPropertyChanged(nameof(DetailCustomer));
        OnPropertyChanged(nameof(DetailSalesOrder));
        OnPropertyChanged(nameof(DetailOriginalAmount));
        OnPropertyChanged(nameof(DetailRemainingAmount));
        OnPropertyChanged(nameof(DetailCollectionPolicy));
        OnPropertyChanged(nameof(DetailStatus));
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(TotalBalanceText));
        OnPropertyChanged(nameof(TotalOpenText));
        OnPropertyChanged(nameof(OpenDocumentCountText));
        OnPropertyChanged(nameof(ShowInitialLoading));
        OnPropertyChanged(nameof(HasPageError));
        OnPropertyChanged(nameof(HasDetailError));
        RaiseEmptyStates();
        RaiseDetail();
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
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
