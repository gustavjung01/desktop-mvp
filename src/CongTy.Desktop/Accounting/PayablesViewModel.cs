using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed class PayablesViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.payable.read";
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Công nợ phải trả.";

    private readonly IPayablesService _service;
    private readonly IAccessStateService _access;
    private PayableDocumentData[] _documents = [];
    private SupplierPayableBalanceData[] _balances = [];
    private PayableDocumentData? _detail;
    private bool _loaded;
    private bool _isBusy;
    private string _pageError = string.Empty;
    private string _detailError = string.Empty;
    private long _accessGeneration;
    private long _requestGeneration;

    public PayablesViewModel(IPayablesService service, IAccessStateService access)
    {
        _service = service;
        _access = access;
        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _requestGeneration++;
            _loaded = false;
            IsBusy = false;
            _documents = [];
            _balances = [];
            _detail = null;
            Balances.Clear();
            Documents.Clear();
            DetailLines.Clear();
            DetailLedger.Clear();
            PageError = string.Empty;
            DetailError = string.Empty;
            RaiseAll();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PayableBalanceRow> Balances { get; } = [];
    public ObservableCollection<PayableDocumentRow> Documents { get; } = [];
    public ObservableCollection<PayableLineRow> DetailLines { get; } = [];
    public ObservableCollection<PayableLedgerRow> DetailLedger { get; } = [];

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanRefresh => CanRead && !IsBusy;
    public bool HasBalances => Balances.Count > 0;
    public bool HasDocuments => Documents.Count > 0;
    public bool HasDetail => _detail is not null;
    public bool HasPageError => !string.IsNullOrWhiteSpace(PageError);
    public bool HasDetailError => !string.IsNullOrWhiteSpace(DetailError);
    public bool ShowInitialLoading => IsBusy && !_loaded;

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

    public string TotalBalanceText => PayablesPresentation.Money(
        _balances.Where(x => x.CurrencyCode.Equals("VND", StringComparison.OrdinalIgnoreCase))
            .Sum(x => PayablesPresentation.Amount(x.Balance)).ToString(CultureInfo.InvariantCulture), "VND");

    public string TotalOverdueText => PayablesPresentation.Money(
        _balances.Where(x => x.CurrencyCode.Equals("VND", StringComparison.OrdinalIgnoreCase))
            .Sum(x => PayablesPresentation.Amount(x.OverdueAmount)).ToString(CultureInfo.InvariantCulture), "VND");

    public string OpenDocumentCountText =>
        _documents.Count(x => x.Status is "open" or "partially_allocated").ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    public string BalanceEmptyText => !IsBusy && _loaded && !HasBalances ? "Chưa có số dư công nợ." : string.Empty;
    public string DocumentsEmptyText => !IsBusy && _loaded && !HasDocuments ? "Chưa phát sinh chứng từ công nợ." : string.Empty;

    public string DetailTitle => _detail?.SourceDocumentNumber ?? string.Empty;
    public string DetailSupplier => _detail is null ? "—" : PayablesPresentation.Party(_detail.SupplierCode, _detail.SupplierName);
    public string DetailWarehouse => _detail is null ? "—" : PayablesPresentation.Party(_detail.WarehouseCode, _detail.WarehouseName);
    public string DetailOriginalAmount => _detail is null ? "—" : PayablesPresentation.Money(_detail.SignedOriginalAmount, _detail.CurrencyCode);
    public string DetailRemainingAmount => _detail is null ? "—" : PayablesPresentation.Money(_detail.RemainingAmount, _detail.CurrencyCode);
    public string DetailStatus => _detail is null ? "—" : PayablesPresentation.Status(_detail.Status);
    public string DetailDue => _detail is null ? "—" : $"{PayablesPresentation.Date(_detail.DueDate)} · {PayablesPresentation.PaymentMethod(_detail.PaymentMethod)} · {_detail.PaymentTermDays} ngày";

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        return await LoadAsync();
    }

    public Task<bool> RefreshAsync() => LoadAsync();

    public async Task OpenDetailAsync(string id)
    {
        if (IsBusy) return;
        if (!CanRead)
        {
            if (_access.Current.IsAuthenticated) PageError = ReadDenied;
            return;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_requestGeneration;
        IsBusy = true;
        DetailError = string.Empty;
        try
        {
            var detail = await _service.GetDocumentAsync(id);
            if (accessGeneration != _accessGeneration || request != _requestGeneration || !CanRead) return;
            ApplyDetail(detail);
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration)
                DetailError = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(exception), exception.RequestId);
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration)
                DetailError = string.IsNullOrWhiteSpace(exception.Message) ? "Không tải được chi tiết chứng từ công nợ đã chọn." : exception.Message;
        }
        finally
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration) IsBusy = false;
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
            if (_access.Current.IsAuthenticated) PageError = ReadDenied;
            return false;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_requestGeneration;
        IsBusy = true;
        PageError = string.Empty;
        try
        {
            var documentsTask = _service.ListDocumentsAsync();
            var balancesTask = _service.ListBalancesAsync();
            await Task.WhenAll(documentsTask, balancesTask);
            if (accessGeneration != _accessGeneration || request != _requestGeneration || !CanRead) return false;
            ApplyLists(await documentsTask, await balancesTask);
            _loaded = true;
            return true;
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration)
                PageError = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(exception), exception.RequestId);
            return false;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration)
                PageError = string.IsNullOrWhiteSpace(exception.Message) ? "Không tải được đầy đủ dữ liệu công nợ phải trả. Hãy thử tải lại." : exception.Message;
            return false;
        }
        finally
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration) IsBusy = false;
        }
    }

    private void ApplyLists(IReadOnlyList<PayableDocumentData> documents, IReadOnlyList<SupplierPayableBalanceData> balances)
    {
        _documents = documents.ToArray();
        _balances = balances.ToArray();

        Balances.Clear();
        var sequence = 0;
        foreach (var item in balances)
        {
            sequence++;
            Balances.Add(new PayableBalanceRow(sequence, PayablesPresentation.Party(item.SupplierCode, item.SupplierName), item.CurrencyCode,
                PayablesPresentation.Money(item.Balance, item.CurrencyCode), PayablesPresentation.Money(item.OpenAmount, item.CurrencyCode),
                PayablesPresentation.Money(item.OverdueAmount, item.CurrencyCode)));
        }

        Documents.Clear();
        sequence = 0;
        foreach (var item in documents)
        {
            sequence++;
            Documents.Add(new PayableDocumentRow(sequence, item.Id, item.SourceDocumentNumber, PayablesPresentation.Date(item.SourceDocumentDate),
                PayablesPresentation.Party(item.SupplierCode, item.SupplierName), PayablesPresentation.Party(item.WarehouseCode, item.WarehouseName),
                PayablesPresentation.Date(item.DueDate), $"{PayablesPresentation.PaymentMethod(item.PaymentMethod)} · {item.PaymentTermDays} ngày",
                PayablesPresentation.Status(item.Status), PayablesPresentation.Money(item.SignedOriginalAmount, item.CurrencyCode)));
        }

        RaiseLists();
    }

    private void ApplyDetail(PayableDocumentData detail)
    {
        _detail = detail;
        DetailLines.Clear();
        var sequence = 0;
        foreach (var line in detail.Lines)
        {
            sequence++;
            DetailLines.Add(new PayableLineRow(sequence, line.Sku, line.ItemName,
                $"{OfficeNumberFormatting.Compact(line.Quantity)} {line.UnitCode}".Trim(),
                PayablesPresentation.Money(line.UnitPrice, detail.CurrencyCode),
                PayablesPresentation.Money(line.LineAmount, detail.CurrencyCode)));
        }

        DetailLedger.Clear();
        sequence = 0;
        foreach (var entry in detail.LedgerEntries)
        {
            sequence++;
            DetailLedger.Add(new PayableLedgerRow(sequence, PayablesPresentation.DateTimeText(entry.OccurredAt),
                PayablesPresentation.LedgerEntry(entry.EntryType), entry.RequestId,
                PayablesPresentation.Money(entry.Amount, detail.CurrencyCode)));
        }
        RaiseDetail();
    }

    private void RaiseLists()
    {
        OnPropertyChanged(nameof(TotalBalanceText));
        OnPropertyChanged(nameof(TotalOverdueText));
        OnPropertyChanged(nameof(OpenDocumentCountText));
        OnPropertyChanged(nameof(HasBalances));
        OnPropertyChanged(nameof(HasDocuments));
        OnPropertyChanged(nameof(BalanceEmptyText));
        OnPropertyChanged(nameof(DocumentsEmptyText));
    }

    private void RaiseDetail()
    {
        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(DetailTitle));
        OnPropertyChanged(nameof(DetailSupplier));
        OnPropertyChanged(nameof(DetailWarehouse));
        OnPropertyChanged(nameof(DetailOriginalAmount));
        OnPropertyChanged(nameof(DetailRemainingAmount));
        OnPropertyChanged(nameof(DetailStatus));
        OnPropertyChanged(nameof(DetailDue));
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(ShowInitialLoading));
        OnPropertyChanged(nameof(HasPageError));
        OnPropertyChanged(nameof(HasDetailError));
        RaiseLists();
        RaiseDetail();
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

public sealed record PayableBalanceRow(int Sequence, string Supplier, string CurrencyCode, string Balance, string OpenAmount, string OverdueAmount);
public sealed record PayableDocumentRow(int Sequence, string Id, string SourceNumber, string SourceDate, string Supplier, string Warehouse, string DueDate, string PaymentTerms, string Status, string SignedAmount);
public sealed record PayableLineRow(int Sequence, string Sku, string ItemName, string Quantity, string UnitPrice, string LineAmount);
public sealed record PayableLedgerRow(int Sequence, string OccurredAt, string EntryType, string RequestId, string Amount);
