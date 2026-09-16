using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public sealed class PurchasingReportingViewModel : INotifyPropertyChanged
{
    private const string ReportingRead = "core.reporting.purchasing.read";
    private const string PurchaseOrderRead = "core.purchase-order.read";
    private const string GoodsReceiptRead = "core.goods-receipt.read";

    private readonly IPurchasingReportingService _service;
    private readonly IAccessStateService _access;
    private PurchasingReportingDashboardData? _report;
    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _reportError = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;

    public PurchasingReportingViewModel(
        IPurchasingReportingService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;
        _access.Changed += (_, _) =>
        {
            _loaded = false;
            OnPropertyChanged(nameof(CanRead));
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(CanOpenPurchaseOrders));
            OnPropertyChanged(nameof(CanOpenGoodsReceipts));
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PurchasingCurrencyRow> CurrencyRows { get; } = [];
    public ObservableCollection<PurchasingStatusRow> PurchaseOrderStatuses { get; } = [];
    public ObservableCollection<PurchasingStatusRow> GoodsReceiptStatuses { get; } = [];
    public ObservableCollection<PurchasingTrendRow> TrendRows { get; } = [];
    public ObservableCollection<PurchasingSupplierRow> SupplierRows { get; } = [];
    public ObservableCollection<PurchasingSkuRow> SkuRows { get; } = [];

    public bool CanRead => _access.HasPermission(ReportingRead);
    public bool CanApply => CanRead && !IsBusy;
    public bool CanOpenPurchaseOrders => _access.HasPermission(PurchaseOrderRead);
    public bool CanOpenGoodsReceipts => _access.HasPermission(GoodsReceiptRead);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(ApplyText));
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

    public bool HasReportError => !string.IsNullOrWhiteSpace(ReportError);

    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            if (!SetField(ref _fromDate, value)) return;
            OnPropertyChanged(nameof(PeriodText));
        }
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            if (!SetField(ref _toDate, value)) return;
            OnPropertyChanged(nameof(PeriodText));
        }
    }

    public string PeriodText =>
        FromDate is not null && ToDate is not null
            ? $"{FromDate:dd/MM/yyyy} → {ToDate:dd/MM/yyyy}"
            : "Mặc định tháng hiện tại theo giờ Việt Nam";

    public string AllOrderCount => _report?.Summary.AllOrderCount ?? "0";
    public string EffectiveOrderCount => _report?.Summary.EffectiveOrderCount ?? "0";
    public string CancelledOrderCount => _report?.Summary.CancelledOrderCount ?? "0";
    public string PendingApprovalCount => _report?.Summary.PendingApprovalCount ?? "0";
    public string PostedReceiptCount => _report?.Summary.PostedReceiptCount ?? "0";
    public string ReversedReceiptCount => _report?.Summary.ReversedReceiptCount ?? "0";

    public string EffectiveStatesText =>
        _report?.Basis.EffectiveStates is { Length: > 0 } states
            ? string.Join(" · ", states.Select(PurchasingReportingPresentation.Status))
            : "Theo trạng thái có hiệu lực của Công Ty";

    public string CurrencyCountText => $"{CurrencyRows.Count} tiền tệ";
    public string GeneratedAtText => $"Cập nhật: {PurchasingReportingPresentation.GeneratedAt(_report?.GeneratedAt)}";
    public string LineageText =>
        "Nguồn số liệu: ngày đặt hàng; phiếu nhận dùng ngày nhận hàng. Giá trị lấy từ tổng giá trị đơn mua. Các loại tiền tệ được giữ riêng, không cộng gộp.";

    public string CurrencyEmptyText => CurrencyRows.Count == 0 && !IsBusy ? "Không có giá trị hiệu lực trong kỳ." : string.Empty;
    public string TrendEmptyText => TrendRows.Count == 0 && !IsBusy ? "Không có dữ liệu xu hướng trong kỳ." : string.Empty;
    public string SupplierEmptyText => SupplierRows.Count == 0 && !IsBusy ? "Chưa có dữ liệu xếp hạng." : string.Empty;
    public string SkuEmptyText => SkuRows.Count == 0 && !IsBusy ? "Chưa có dữ liệu SKU." : string.Empty;
    public string PurchaseStatusEmptyText => PurchaseOrderStatuses.Count == 0 && !IsBusy ? "Không có trạng thái đơn mua trong kỳ." : string.Empty;
    public string ReceiptStatusEmptyText => GoodsReceiptStatuses.Count == 0 && !IsBusy ? "Không có trạng thái phiếu nhận trong kỳ." : string.Empty;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy) return;

        if (!CanRead)
        {
            SetError("Tài khoản chưa được cấp quyền xem Báo cáo mua hàng.");
            return;
        }

        if (FromDate is not null && ToDate is not null && FromDate.Value.Date > ToDate.Value.Date)
        {
            SetError("Ngày bắt đầu không được sau ngày kết thúc.");
            return;
        }

        IsBusy = true;
        ReportError = string.Empty;
        try
        {
            var report = await _service.GetAsync(FromDate, ToDate).ConfigureAwait(true);
            Apply(report);
            _loaded = true;
            Message = "Báo cáo mua hàng đã được cập nhật.";
            MessageIsError = false;
        }
        catch (CanonicalApiException exception)
        {
            SetError(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId));
        }
        catch (Exception exception)
        {
            SetError(exception.Message);
        }
        finally
        {
            IsBusy = false;
            RaiseEmptyStates();
        }
    }

    public async Task ResetAsync()
    {
        if (IsBusy) return;
        FromDate = null;
        ToDate = null;
        await RefreshAsync().ConfigureAwait(true);
    }

    private void Apply(PurchasingReportingDashboardData report)
    {
        _report = report ?? throw new InvalidOperationException("Báo cáo mua hàng không có dữ liệu.");

        FromDate = PurchasingReportingPresentation.ParseDate(report.Filters.From);
        ToDate = PurchasingReportingPresentation.ParseDate(report.Filters.To);

        Replace(
            CurrencyRows,
            report.CurrencyTotals.Select((row, index) => new PurchasingCurrencyRow(
                index + 1,
                row.CurrencyCode,
                PurchasingReportingPresentation.Number(row.DocumentCount),
                PurchasingReportingPresentation.Money(row.TotalValue, row.CurrencyCode))));

        Replace(
            PurchaseOrderStatuses,
            report.StatusBreakdown
                .Where(row => string.Equals(row.Dimension, "purchase_order", StringComparison.Ordinal))
                .Select(row => new PurchasingStatusRow(
                    PurchasingReportingPresentation.Status(row.State),
                    PurchasingReportingPresentation.Number(row.DocumentCount))));

        Replace(
            GoodsReceiptStatuses,
            report.StatusBreakdown
                .Where(row => string.Equals(row.Dimension, "goods_receipt", StringComparison.Ordinal))
                .Select(row => new PurchasingStatusRow(
                    PurchasingReportingPresentation.Status(row.State),
                    PurchasingReportingPresentation.Number(row.DocumentCount))));

        Replace(
            TrendRows,
            report.DailyTrend.Select((row, index) => new PurchasingTrendRow(
                index + 1,
                PurchasingReportingPresentation.Date(row.BusinessDate),
                row.CurrencyCode,
                PurchasingReportingPresentation.Number(row.DocumentCount),
                PurchasingReportingPresentation.Money(row.TotalValue, row.CurrencyCode))));

        Replace(
            SupplierRows,
            report.TopEntities.Select((row, index) => new PurchasingSupplierRow(
                index + 1,
                row.EntityCode,
                row.EntityName,
                row.CurrencyCode,
                PurchasingReportingPresentation.Number(row.DocumentCount),
                PurchasingReportingPresentation.Money(row.TotalValue, row.CurrencyCode))));

        Replace(
            SkuRows,
            report.TopSkus.Select((row, index) => new PurchasingSkuRow(
                index + 1,
                row.Sku,
                string.IsNullOrWhiteSpace(row.ItemName)
                    ? row.CurrencyCode
                    : $"{row.ItemName} · {row.CurrencyCode}",
                PurchasingReportingPresentation.Number(row.BaseQuantity),
                PurchasingReportingPresentation.Money(row.TotalValue, row.CurrencyCode),
                row.SampleDocumentNumber)));

        foreach (var property in new[]
        {
            nameof(AllOrderCount),
            nameof(EffectiveOrderCount),
            nameof(CancelledOrderCount),
            nameof(PendingApprovalCount),
            nameof(PostedReceiptCount),
            nameof(ReversedReceiptCount),
            nameof(EffectiveStatesText),
            nameof(CurrencyCountText),
            nameof(GeneratedAtText),
            nameof(LineageText)
        })
        {
            OnPropertyChanged(property);
        }

        RaiseEmptyStates();
    }

    private void SetError(string message)
    {
        ReportError = string.IsNullOrWhiteSpace(message) ? "Không tải được Báo cáo mua hàng." : message;
        Message = ReportError;
        MessageIsError = true;
    }

    private void RaiseEmptyStates()
    {
        OnPropertyChanged(nameof(CurrencyCountText));
        OnPropertyChanged(nameof(CurrencyEmptyText));
        OnPropertyChanged(nameof(TrendEmptyText));
        OnPropertyChanged(nameof(SupplierEmptyText));
        OnPropertyChanged(nameof(SkuEmptyText));
        OnPropertyChanged(nameof(PurchaseStatusEmptyText));
        OnPropertyChanged(nameof(ReceiptStatusEmptyText));
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
