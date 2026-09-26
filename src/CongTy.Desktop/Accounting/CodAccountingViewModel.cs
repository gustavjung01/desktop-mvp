using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed partial class CodAccountingViewModel : INotifyPropertyChanged
{
    private const int IntentCacheLimit = 256;
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem COD và đối soát.";

    private readonly ICodAccountingService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _intentTimestamps = new(StringComparer.Ordinal);

    private CodReportingDashboardData? _report;
    private CodHandoverData? _selectedHandover;
    private CodHandoverListRow? _selectedHandoverRow;
    private bool _loaded;
    private bool _reconciliationLoaded;
    private Task<bool>? _initialLoadTask;
    private Task<bool>? _reconciliationLoadTask;
    private long _accessGeneration;
    private long _loadGeneration;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _reportError = string.Empty;
    private string _from = string.Empty;
    private string _to = string.Empty;
    private string _selectedWarehouseId = string.Empty;
    private int _activeTabIndex;
    private string _acceptedAmount = string.Empty;
    private string _acceptanceReason = string.Empty;
    private string _acceptanceNote = string.Empty;
    private string _reversalReason = string.Empty;

    public CodAccountingViewModel(
        ICodAccountingService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loaded = false;
            _reconciliationLoaded = false;
            _initialLoadTask = null;
            _reconciliationLoadTask = null;
            Reset();
            RaisePermissions();
            if (CanReadReport && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CodWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<CodCustodyDriverRow> CustodyDrivers { get; } = [];
    public ObservableCollection<CodCollectionActivityRow> CollectionActivity { get; } = [];
    public ObservableCollection<CodHandoverActivityRow> HandoverActivity { get; } = [];
    public ObservableCollection<CodAcceptanceActivityRow> AcceptanceActivity { get; } = [];
    public ObservableCollection<CodPendingHandoverRow> PendingHandovers { get; } = [];
    public ObservableCollection<CodOverduePromiseRow> OverduePromises { get; } = [];
    public ObservableCollection<CodExceptionRow> Exceptions { get; } = [];
    public ObservableCollection<CodHandoverListRow> Handovers { get; } = [];
    public ObservableCollection<CodHandoverLineRow> HandoverLines { get; } = [];

    public bool CanReadReport => _access.HasPermission("core.reporting.cod.read");
    public bool CanReadReconciliation => _access.HasPermission("core.cod-reconciliation.read");
    public bool CanAccept => _access.HasPermission("core.cod-reconciliation.accept");
    public bool CanAdjust => _access.HasPermission("core.cod-adjustment.create");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoadingReport => _busyAction == "report";
    public bool IsLoadingReconciliation => _busyAction == "reconciliation";
    public bool HasReport => _report is not null;
    public bool ShowInitialLoading => IsLoadingReport && !HasReport;
    public bool HasReportError => !string.IsNullOrWhiteSpace(ReportError);
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasCustodyDrivers => CustodyDrivers.Count > 0;
    public bool HasCollectionActivity => CollectionActivity.Count > 0;
    public bool HasHandoverActivity => HandoverActivity.Count > 0;
    public bool HasAcceptanceActivity => AcceptanceActivity.Count > 0;
    public bool HasPendingHandovers => PendingHandovers.Count > 0;
    public bool HasOverduePromises => OverduePromises.Count > 0;
    public bool HasExceptions => Exceptions.Count > 0;
    public bool HasHandovers => Handovers.Count > 0;
    public bool HasSelectedHandover => SelectedHandover is not null;
    public bool ShowReconciliationDenied => ActiveTabIndex == 3 && !CanReadReconciliation;

    public string Message
    {
        get => _message;
        private set
        {
            if (SetField(ref _message, value ?? string.Empty))
                OnPropertyChanged(nameof(HasMessage));
        }
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

    public string From
    {
        get => _from;
        set => SetField(ref _from, value ?? string.Empty);
    }

    public string To
    {
        get => _to;
        set => SetField(ref _to, value ?? string.Empty);
    }

    public DateTime? FromDateValue
    {
        get => DateTime.TryParseExact(_from, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var value) ? value : null;
        set
        {
            var normalized = value?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            if (!SetField(ref _from, normalized, nameof(From))) return;
            OnPropertyChanged();
        }
    }

    public DateTime? ToDateValue
    {
        get => DateTime.TryParseExact(_to, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var value) ? value : null;
        set
        {
            var normalized = value?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            if (!SetField(ref _to, normalized, nameof(To))) return;
            OnPropertyChanged();
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
        set
        {
            var normalized = Math.Clamp(value, 0, 5);
            if (!SetField(ref _activeTabIndex, normalized)) return;
            OnPropertyChanged(nameof(ShowReconciliationDenied));
            if (normalized == 3 && CanReadReconciliation)
                _ = EnsureReconciliationLoadedAsync();
        }
    }

    public string FilterButtonText => IsLoadingReport ? "Đang tải…" : "Lọc báo cáo";
    public string GeneratedAtText => _report is null ? string.Empty : $"Cập nhật: {CodAccountingPresentation.Timestamp(_report.GeneratedAt)}";
    public string PeriodNotice => _report is null
        ? string.Empty
        : "Khoản tiền tài xế đang giữ là số liệu hiện tại và không bị giới hạn bởi kỳ báo cáo. Từ/đến ngày chỉ áp dụng cho hoạt động thu, bàn giao và kế toán tiếp nhận.";

    public string CustodyCount => SumCounts(_report?.CurrentSnapshot.CustodyByCurrency ?? []);
    public string CustodyAmountList => CodAccountingPresentation.CustodyAmountList(_report?.CurrentSnapshot.CustodyByCurrency ?? []);
    public string PendingHandoverCount => CodAccountingPresentation.Count((_report?.CurrentSnapshot.PendingHandovers.Length ?? 0).ToString());
    public string OverduePromiseCount => CodAccountingPresentation.Count((_report?.CurrentSnapshot.OverduePromises.Length ?? 0).ToString());
    public string ExceptionCount => CodAccountingPresentation.Count(((_report?.CurrentSnapshot.Discrepancies.Length ?? 0)
        + (_report?.Exceptions.Lifecycle.Length ?? 0)
        + (_report?.Exceptions.CurrencyLineage.Length ?? 0)).ToString());

    public string CustodyEmptyText => !IsBusy && HasReport && !HasCustodyDrivers ? "Không có tiền mặt COD đang nằm ở tài xế." : string.Empty;
    public string CollectionEmptyText => !IsBusy && HasReport && !HasCollectionActivity ? "Không có hoạt động thu COD trong kỳ." : string.Empty;
    public string HandoverActivityEmptyText => !IsBusy && HasReport && !HasHandoverActivity ? "Không có bàn giao COD trong kỳ." : string.Empty;
    public string AcceptanceActivityEmptyText => !IsBusy && HasReport && !HasAcceptanceActivity ? "Không có kế toán tiếp nhận COD trong kỳ." : string.Empty;
    public string PendingHandoverEmptyText => !IsBusy && HasReport && !HasPendingHandovers ? "Không có bàn giao đang chờ tiếp nhận." : string.Empty;
    public string PromiseEmptyText => !IsBusy && HasReport && !HasOverduePromises ? "Không có lời hẹn thu quá hạn." : string.Empty;
    public string ExceptionEmptyText => !IsBusy && HasReport && !HasExceptions ? "Không có trường hợp COD cần kiểm tra hiện tại." : string.Empty;
    public string HandoverEmptyText => !IsBusy && CanReadReconciliation && _reconciliationLoaded && !HasHandovers ? "Chưa có bàn giao COD." : string.Empty;

    public CodHandoverListRow? SelectedHandoverRow
    {
        get => _selectedHandoverRow;
        set
        {
            if (!SetField(ref _selectedHandoverRow, value)) return;
            if (value is null)
            {
                SelectedHandover = null;
                return;
            }
            _ = LoadHandoverAsync(value.Data.Id);
        }
    }

    public CodHandoverData? SelectedHandover
    {
        get => _selectedHandover;
        private set
        {
            if (!SetField(ref _selectedHandover, value)) return;
            Replace(HandoverLines, value is null ? [] : value.Lines.Select(CodAccountingPresentation.HandoverLine));
            SeedAcceptance();
            RaiseHandoverState();
        }
    }

    public string SelectedHandoverTitle => SelectedHandover is null
        ? "Chọn một bàn giao COD."
        : $"{SelectedHandover.TripNumber ?? "Chuyến giao"} · {SelectedHandover.DriverName ?? "Tài xế"}";

    public string SelectedHandoverClaimed => SelectedHandover is null
        ? string.Empty
        : CodAccountingPresentation.Money(SelectedHandover.HandedOverTotal);

    public string SelectedHandoverExcess => SelectedHandover is null
        ? string.Empty
        : CodAccountingPresentation.Money(SelectedHandover.UnattributedExcessAmount);

    public string SelectedHandoverDifference => SelectedHandover is null
        ? string.Empty
        : CodAccountingPresentation.Money(SelectedHandover.DifferenceAmount);

    public string SelectedHandoverStatus => CodAccountingPresentation.HandoverStatus(SelectedHandover?.Status);

    public string AcceptedAmount
    {
        get => _acceptedAmount;
        set
        {
            if (!SetField(ref _acceptedAmount, value ?? string.Empty)) return;
            RaiseHandoverState();
        }
    }

    public string AcceptanceReason
    {
        get => _acceptanceReason;
        set
        {
            if (!SetField(ref _acceptanceReason, value ?? string.Empty)) return;
            RaiseHandoverState();
        }
    }

    public string AcceptanceNote
    {
        get => _acceptanceNote;
        set => SetField(ref _acceptanceNote, value ?? string.Empty);
    }

    public string ReversalReason
    {
        get => _reversalReason;
        set => SetField(ref _reversalReason, value ?? string.Empty);
    }

    public string AcceptanceDifference
    {
        get
        {
            if (SelectedHandover is null
                || !CodAccountingPresentation.TryScaled(AcceptedAmount, out var accepted)
                || !CodAccountingPresentation.TryScaled(SelectedHandover.HandedOverTotal, out var handed)
                || !CodAccountingPresentation.TryScaled(SelectedHandover.UnattributedExcessAmount, out var excess))
                return "—";
            return CodAccountingPresentation.Money(CodAccountingPresentation.Decimal(accepted - handed - excess));
        }
    }

    public bool IsAcceptanceDifference => AcceptanceDifference != "—" && AcceptanceDifference != "0 VND";
    public bool ShowAcceptForm => SelectedHandover?.Status == "submitted" && SelectedHandover.Acceptance is null;
    public bool CanAcceptAction => CanAccept && ShowAcceptForm && IsNotBusy;
    public bool CanReverseAcceptanceAction => CanAdjust
        && IsNotBusy
        && SelectedHandover?.Acceptance is { ReversalId: null };
    public bool CanReverseHandoverAction => CanAdjust
        && IsNotBusy
        && SelectedHandover is not null
        && SelectedHandover.Status != "reversed"
        && (SelectedHandover.Acceptance is null || SelectedHandover.Acceptance.ReversalId is not null);
    public bool CanReverseCollectionAction => CanAdjust && IsNotBusy && SelectedHandover?.Status == "reversed";
    public string AcceptButtonText => _busyAction == "accept" ? "Đang xác nhận…" : "Xác nhận tiền thực nhận";
    public string ReverseAcceptanceButtonText => _busyAction == "reverse-acceptance" ? "Đang đảo…" : "Đảo xác nhận";
    public string ReverseHandoverButtonText => _busyAction == "reverse-handover" ? "Đang đảo…" : "Đảo bàn giao";

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);
        _initialLoadTask = LoadReportAsync(string.Empty, string.Empty, string.Empty);
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

    public Task<bool> ApplyAsync() => LoadReportAsync(From, To, SelectedWarehouseId);
    public Task<bool> RefreshAsync() => LoadReportAsync(From, To, SelectedWarehouseId);

    public async Task<bool> EnsureReconciliationLoadedAsync()
    {
        if (_reconciliationLoaded) return true;
        if (!CanReadReconciliation) return false;
        if (_reconciliationLoadTask is not null) return await _reconciliationLoadTask.ConfigureAwait(true);
        _reconciliationLoadTask = LoadHandoversAsync(SelectedHandover?.Id ?? SelectedHandoverRow?.Data.Id);
        try
        {
            _reconciliationLoaded = await _reconciliationLoadTask.ConfigureAwait(true);
            return _reconciliationLoaded;
        }
        finally
        {
            _reconciliationLoadTask = null;
        }
    }

    public Task<bool> RefreshReconciliationAsync() =>
        LoadHandoversAsync(SelectedHandover?.Id ?? SelectedHandoverRow?.Data.Id);

    public async Task OpenPendingHandoverAsync(CodPendingHandoverRow? row)
    {
        if (row is null) return;
        await OpenAccountingHandoverAsync(row.HandoverId).ConfigureAwait(true);
    }

    public async Task OpenExceptionAsync(CodExceptionRow? row)
    {
        if (row is null || !row.CanOpenAccounting || string.IsNullOrWhiteSpace(row.HandoverId)) return;
        await OpenAccountingHandoverAsync(row.HandoverId).ConfigureAwait(true);
    }

    private async Task OpenAccountingHandoverAsync(string handoverId)
    {
        ActiveTabIndex = 3;
        if (!CanReadReconciliation)
        {
            SetError("Tài khoản chưa được cấp quyền xem đối soát COD của kế toán.");
            return;
        }

        await EnsureReconciliationLoadedAsync().ConfigureAwait(true);
        var target = Handovers.FirstOrDefault(row => row.Data.Id == handoverId);
        if (target is null)
        {
            SetError("Bàn giao COD không còn trong danh sách hiện tại.");
            return;
        }

        _selectedHandoverRow = target;
        OnPropertyChanged(nameof(SelectedHandoverRow));
        await LoadHandoverAsync(handoverId).ConfigureAwait(true);
    }

    public async Task AcceptAsync()
    {
        var selected = SelectedHandover;
        if (selected is null || !CanAcceptAction) return;
        if (!CodAccountingPresentation.TryScaled(AcceptedAmount, out var accepted) || accepted < 0)
        {
            SetError("Số tiền thực nhận không hợp lệ.");
            return;
        }

        if (!CodAccountingPresentation.TryScaled(selected.HandedOverTotal, out var handed)
            || !CodAccountingPresentation.TryScaled(selected.UnattributedExcessAmount, out var excess))
        {
            SetError("Số tiền bàn giao không hợp lệ.");
            return;
        }

        var reason = AcceptanceReason.Trim();
        var note = AcceptanceNote.Trim();
        if (reason.Length > 2000 || note.Length > 2000)
        {
            SetError("Lý do và ghi chú tối đa 2.000 ký tự.");
            return;
        }
        if (accepted - handed - excess != 0 && reason.Length == 0)
        {
            SetError("Chênh lệch tiền thực nhận cần có lý do.");
            return;
        }

        var normalizedAmount = CodAccountingPresentation.Decimal(accepted);
        var intent = Intent("accept", selected.Id, normalizedAmount, reason, note);
        var acceptedAt = TimestampFor(intent);
        SetBusy("accept");
        ClearMessage();
        try
        {
            var result = await _service.AcceptAsync(
                selected.Id,
                new CodAcceptRequest(normalizedAmount, acceptedAt, EmptyToNull(reason), EmptyToNull(note)),
                KeyFor(intent)).ConfigureAwait(true);
            CompleteIntent(intent);
            await LoadHandoversAsync(selected.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice(result.Replayed
                ? "Xác nhận tiền thực nhận đã được xử lý trước đó; dữ liệu được tải lại."
                : "Đã xác nhận số tiền Công Ty thực nhận.");
        }
        catch (Exception exception) { SetCodError(exception); }
        finally { SetBusy(null); }
    }

    public async Task ReverseAcceptanceAsync()
    {
        var selected = SelectedHandover;
        var acceptance = selected?.Acceptance;
        if (selected is null || acceptance is null || !CanReverseAcceptanceAction) return;
        var reason = RequireReversalReason();
        if (reason is null) return;

        var intent = Intent("reverse-acceptance", acceptance.Id, reason);
        SetBusy("reverse-acceptance");
        ClearMessage();
        try
        {
            var result = await _service.ReverseAcceptanceAsync(
                acceptance.Id,
                new CodReversalRequest(reason),
                KeyFor(intent)).ConfigureAwait(true);
            CompleteIntent(intent);
            await LoadHandoversAsync(selected.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice(result.Replayed
                ? "Việc đảo xác nhận đã được xử lý trước đó; dữ liệu được tải lại."
                : "Đã đảo xác nhận COD bằng bản ghi bù.");
        }
        catch (Exception exception) { SetCodError(exception); }
        finally { SetBusy(null); }
    }

    public async Task ReverseHandoverAsync()
    {
        var selected = SelectedHandover;
        if (selected is null || !CanReverseHandoverAction) return;
        var reason = RequireReversalReason();
        if (reason is null) return;

        var intent = Intent("reverse-handover", selected.Id, reason);
        SetBusy("reverse-handover");
        ClearMessage();
        try
        {
            var result = await _service.ReverseHandoverAsync(
                selected.Id,
                new CodReversalRequest(reason),
                KeyFor(intent)).ConfigureAwait(true);
            CompleteIntent(intent);
            await LoadHandoversAsync(selected.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice(result.Replayed
                ? "Việc đảo bàn giao đã được xử lý trước đó; dữ liệu được tải lại."
                : "Đã đảo bàn giao COD.");
        }
        catch (Exception exception) { SetCodError(exception); }
        finally { SetBusy(null); }
    }

    public async Task ReverseCollectionAsync(CodHandoverLineRow? line)
    {
        var selected = SelectedHandover;
        if (selected is null || line is null || !CanReverseCollectionAction) return;
        var reason = RequireReversalReason();
        if (reason is null) return;

        var intent = Intent("reverse-collection", line.Data.CollectionId, reason);
        SetBusy("reverse-collection");
        ClearMessage();
        try
        {
            var result = await _service.ReverseCollectionAsync(
                line.Data.CollectionId,
                new CodReversalRequest(reason),
                KeyFor(intent)).ConfigureAwait(true);
            CompleteIntent(intent);
            await LoadHandoversAsync(selected.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice(result.Replayed
                ? "Việc đảo khoản thu đã được xử lý trước đó; dữ liệu được tải lại."
                : "Đã đảo khoản thu COD và các bút toán liên quan.");
        }
        catch (Exception exception) { SetCodError(exception); }
        finally { SetBusy(null); }
    }

    private async Task<bool> LoadReportAsync(string? from, string? to, string? warehouseId)
    {
        if (IsBusy) return false;
        if (!CanReadReport)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetError(ReadDenied);
            return false;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        SetBusy("report");
        ReportError = string.Empty;
        try
        {
            var report = await _service.GetReportAsync(from, to, warehouseId).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanReadReport)
                return false;

            ApplyReport(report);
            _loaded = true;
            SetNotice("COD và đối soát đã được cập nhật.");
            return true;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
            {
                SetCodError(exception);
                ReportError = Message;
            }
            return false;
        }
        finally
        {
            if (request == _loadGeneration && _busyAction == "report") SetBusy(null);
        }
    }

    private void ApplyReport(CodReportingDashboardData report)
    {
        _report = report ?? throw new InvalidOperationException("Báo cáo COD không có dữ liệu.");
        _from = report.Filters.From;
        _to = report.Filters.To;
        _selectedWarehouseId = report.Filters.WarehouseId ?? string.Empty;
        OnPropertyChanged(nameof(From));
        OnPropertyChanged(nameof(To));
        OnPropertyChanged(nameof(FromDateValue));
        OnPropertyChanged(nameof(ToDateValue));

        Replace(
            Warehouses,
            new[] { new CodWarehouseOption(string.Empty, "Tất cả kho được cấp quyền") }
                .Concat(report.Warehouses.Select(CodAccountingPresentation.Warehouse)));
        if (!Warehouses.Any(row => row.Id == _selectedWarehouseId))
            _selectedWarehouseId = string.Empty;
        OnPropertyChanged(nameof(SelectedWarehouseId));

        Replace(CustodyDrivers, report.CurrentSnapshot.CustodyByDriver.Select(CodAccountingPresentation.Custody));
        Replace(CollectionActivity, report.Activity.Collections.Select(CodAccountingPresentation.Collection));
        Replace(HandoverActivity, report.Activity.Handovers.Select(CodAccountingPresentation.HandoverActivity));
        Replace(AcceptanceActivity, report.Activity.Acceptances.Select(CodAccountingPresentation.AcceptanceActivity));
        Replace(PendingHandovers, report.CurrentSnapshot.PendingHandovers.Select(CodAccountingPresentation.Pending));
        Replace(OverduePromises, report.CurrentSnapshot.OverduePromises.Select(CodAccountingPresentation.Promise));

        var warehouseLabels = report.Warehouses.ToDictionary(row => row.WarehouseId, row => row.WarehouseCode, StringComparer.Ordinal);
        var exceptionRows = new List<CodExceptionRow>();
        exceptionRows.AddRange(report.CurrentSnapshot.Discrepancies.Select(row => new CodExceptionRow(
            "Chênh lệch bàn giao",
            $"{row.TripNumber} · {row.DriverCode}",
            row.WarehouseCode,
            $"{CodAccountingPresentation.Money(row.VarianceAmount, row.CurrencyCode)} · {CodAccountingPresentation.ReconciliationStatus(row.ProjectionStatus)}",
            row.HandoverId)));
        exceptionRows.AddRange(report.Exceptions.Lifecycle.Select(row => new CodExceptionRow(
            row.AnomalyType switch
            {
                "cod_collection" => "Khoản thu COD",
                "cod_handover" => "Bàn giao COD",
                _ => CodAccountingPresentation.OfficeToken(row.AnomalyType)
            },
            string.IsNullOrWhiteSpace(row.SourceNumber) ? "Nguồn đối soát" : row.SourceNumber,
            warehouseLabels.TryGetValue(row.WarehouseId, out var warehouseCode) ? warehouseCode : "Kho trong phạm vi",
            CodAccountingPresentation.ReconciliationStatus(row.ReconciliationStatus),
            null)));
        exceptionRows.AddRange(report.Exceptions.CurrencyLineage.Select(row => new CodExceptionRow(
            "Không xác định loại tiền",
            $"{row.TripNumber} · {row.DriverCode}",
            row.WarehouseCode,
            $"{CodAccountingPresentation.Count(row.CurrencyCount)} loại tiền trong một bàn giao — không đưa vào tổng tiền",
            row.HandoverId)));
        Replace(Exceptions, exceptionRows);

        ReportError = string.Empty;
        RaiseReportState();
    }

    private async Task<bool> LoadHandoversAsync(string? preferredId, bool preserveMessage = false)
    {
        if (!CanReadReconciliation)
        {
            _reconciliationLoaded = false;
            return false;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        var ownsBusy = _busyAction is null;
        if (ownsBusy) SetBusy("reconciliation");
        if (!preserveMessage) ClearMessage();

        try
        {
            var rows = await _service.ListHandoversAsync().ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanReadReconciliation)
                return false;

            Replace(Handovers, rows.Select(CodAccountingPresentation.Handover));
            var selected = !string.IsNullOrWhiteSpace(preferredId)
                ? Handovers.FirstOrDefault(row => row.Data.Id == preferredId)
                : Handovers.FirstOrDefault();
            _selectedHandoverRow = selected;
            OnPropertyChanged(nameof(SelectedHandoverRow));

            if (selected is null)
                SelectedHandover = null;
            else
                await LoadHandoverCoreAsync(selected.Data.Id, request).ConfigureAwait(true);

            _reconciliationLoaded = true;
            RaiseReconciliationState();
            return true;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
                SetCodError(exception);
            return false;
        }
        finally
        {
            if (ownsBusy && request == _loadGeneration && _busyAction == "reconciliation") SetBusy(null);
        }
    }

    private async Task LoadHandoverAsync(string id)
    {
        if (!CanReadReconciliation || IsBusy) return;
        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        SetBusy("detail");
        ClearMessage();
        try
        {
            await LoadHandoverCoreAsync(id, request).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
                SetCodError(exception);
        }
        finally
        {
            if (request == _loadGeneration && _busyAction == "detail") SetBusy(null);
        }
    }

    private async Task LoadHandoverCoreAsync(string id, long request)
    {
        var detail = await _service.GetHandoverAsync(id).ConfigureAwait(true);
        if (request != _loadGeneration || !CanReadReconciliation) return;
        SelectedHandover = detail;
    }

    private void SeedAcceptance()
    {
        _acceptanceReason = string.Empty;
        _acceptanceNote = string.Empty;
        _reversalReason = string.Empty;

        if (SelectedHandover is not null
            && CodAccountingPresentation.TryScaled(SelectedHandover.HandedOverTotal, out var handed)
            && CodAccountingPresentation.TryScaled(SelectedHandover.UnattributedExcessAmount, out var excess))
            _acceptedAmount = CodAccountingPresentation.Decimal(handed + excess);
        else
            _acceptedAmount = string.Empty;

        OnPropertyChanged(nameof(AcceptedAmount));
        OnPropertyChanged(nameof(AcceptanceReason));
        OnPropertyChanged(nameof(AcceptanceNote));
        OnPropertyChanged(nameof(ReversalReason));
    }

    private string? RequireReversalReason()
    {
        var reason = ReversalReason.Trim();
        if (reason.Length == 0)
        {
            SetError("Cần nhập lý do đảo.");
            return null;
        }
        if (reason.Length > 2000)
        {
            SetError("Lý do đảo tối đa 2.000 ký tự.");
            return null;
        }
        return reason;
    }

    private string Intent(string prefix, params string?[] parts) =>
        $"{prefix}|{string.Join("|", parts.Select(value => value?.Trim() ?? string.Empty))}";

    private string KeyFor(string intent)
    {
        if (_intentKeys.TryGetValue(intent, out var existing)) return existing;
        TrimIntentCache();
        var prefix = intent.Split('|', 2)[0];
        var key = _idempotencyKeys.Create($"cod-accounting-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private string TimestampFor(string intent)
    {
        if (_intentTimestamps.TryGetValue(intent, out var existing)) return existing;
        TrimIntentCache();
        var timestamp = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        _intentTimestamps[intent] = timestamp;
        return timestamp;
    }

    private void CompleteIntent(string intent)
    {
        _intentKeys.Remove(intent);
        _intentTimestamps.Remove(intent);
    }

    private void TrimIntentCache()
    {
        while (_intentKeys.Count >= IntentCacheLimit)
        {
            var oldest = _intentKeys.Keys.FirstOrDefault();
            if (oldest is null) break;
            _intentKeys.Remove(oldest);
            _intentTimestamps.Remove(oldest);
        }
    }

    private void SetBusy(string? action)
    {
        if (string.Equals(_busyAction, action, StringComparison.Ordinal)) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoadingReport));
        OnPropertyChanged(nameof(IsLoadingReconciliation));
        OnPropertyChanged(nameof(ShowInitialLoading));
        OnPropertyChanged(nameof(FilterButtonText));
        OnPropertyChanged(nameof(AcceptButtonText));
        OnPropertyChanged(nameof(ReverseAcceptanceButtonText));
        OnPropertyChanged(nameof(ReverseHandoverButtonText));
        RaiseReportState();
        RaiseHandoverState();
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanReadReport));
        OnPropertyChanged(nameof(CanReadReconciliation));
        OnPropertyChanged(nameof(CanAccept));
        OnPropertyChanged(nameof(CanAdjust));
        OnPropertyChanged(nameof(ShowReconciliationDenied));
        RaiseHandoverState();
    }

    private void RaiseReportState()
    {
        OnPropertyChanged(nameof(HasReport));
        OnPropertyChanged(nameof(GeneratedAtText));
        OnPropertyChanged(nameof(PeriodNotice));
        OnPropertyChanged(nameof(CustodyCount));
        OnPropertyChanged(nameof(CustodyAmountList));
        OnPropertyChanged(nameof(PendingHandoverCount));
        OnPropertyChanged(nameof(OverduePromiseCount));
        OnPropertyChanged(nameof(ExceptionCount));
        OnPropertyChanged(nameof(HasCustodyDrivers));
        OnPropertyChanged(nameof(HasCollectionActivity));
        OnPropertyChanged(nameof(HasHandoverActivity));
        OnPropertyChanged(nameof(HasAcceptanceActivity));
        OnPropertyChanged(nameof(HasPendingHandovers));
        OnPropertyChanged(nameof(HasOverduePromises));
        OnPropertyChanged(nameof(HasExceptions));
        OnPropertyChanged(nameof(CustodyEmptyText));
        OnPropertyChanged(nameof(CollectionEmptyText));
        OnPropertyChanged(nameof(HandoverActivityEmptyText));
        OnPropertyChanged(nameof(AcceptanceActivityEmptyText));
        OnPropertyChanged(nameof(PendingHandoverEmptyText));
        OnPropertyChanged(nameof(PromiseEmptyText));
        OnPropertyChanged(nameof(ExceptionEmptyText));
    }

    private void RaiseReconciliationState()
    {
        OnPropertyChanged(nameof(HasHandovers));
        OnPropertyChanged(nameof(HandoverEmptyText));
        RaiseHandoverState();
    }

    private void RaiseHandoverState()
    {
        OnPropertyChanged(nameof(HasSelectedHandover));
        OnPropertyChanged(nameof(SelectedHandoverTitle));
        OnPropertyChanged(nameof(SelectedHandoverClaimed));
        OnPropertyChanged(nameof(SelectedHandoverExcess));
        OnPropertyChanged(nameof(SelectedHandoverDifference));
        OnPropertyChanged(nameof(SelectedHandoverStatus));
        OnPropertyChanged(nameof(AcceptanceDifference));
        OnPropertyChanged(nameof(IsAcceptanceDifference));
        OnPropertyChanged(nameof(ShowAcceptForm));
        OnPropertyChanged(nameof(CanAcceptAction));
        OnPropertyChanged(nameof(CanReverseAcceptanceAction));
        OnPropertyChanged(nameof(CanReverseHandoverAction));
        OnPropertyChanged(nameof(CanReverseCollectionAction));
    }

    private void ClearMessage()
    {
        Message = string.Empty;
        MessageIsError = false;
    }

    private void SetNotice(string value)
    {
        Message = value;
        MessageIsError = false;
    }

    private void SetError(string value)
    {
        Message = string.IsNullOrWhiteSpace(value) ? "Yêu cầu COD không thành công." : value;
        MessageIsError = true;
    }

    private void SetCodError(Exception exception)
    {
        if (exception is CanonicalApiException api)
        {
            var officeMessage = api.Code switch
            {
                "COD_ACCEPTANCE_DIFFERENCE_REASON_REQUIRED" => "Chênh lệch tiền thực nhận cần có lý do.",
                "COD_HANDOVER_ALREADY_ACCEPTED" => "Bàn giao này đã được kế toán xác nhận.",
                "COD_ACCEPTANCE_REVERSED" => "Xác nhận đã được đảo. Hãy đảo bàn giao và tạo bàn giao mới.",
                "COD_HANDOVER_REVERSED" => "Bàn giao này đã được đảo.",
                "COD_HANDOVER_ACCEPTANCE_EXISTS" => "Cần đảo xác nhận đang còn hiệu lực trước khi đảo bàn giao.",
                "COD_COLLECTION_HANDOVER_EXISTS" => "Cần đảo bàn giao đang còn hiệu lực trước khi đảo khoản thu.",
                "COD_REVERSAL_REASON_REQUIRED" => "Cần nhập lý do đảo.",
                "WAREHOUSE_SCOPE_DENIED" => "Tài khoản chưa có phạm vi kho phù hợp để xem hoặc xử lý COD.",
                _ => CanonicalErrorMessages.ToOfficeMessage(api)
            };
            SetError(CanonicalErrorMessages.WithRequestId(officeMessage, api.RequestId));
            return;
        }
        SetError(exception.Message);
    }

    private void Reset()
    {
        _report = null;
        _selectedHandover = null;
        _selectedHandoverRow = null;
        _from = string.Empty;
        _to = string.Empty;
        _selectedWarehouseId = string.Empty;
        _activeTabIndex = 0;
        _acceptedAmount = string.Empty;
        _acceptanceReason = string.Empty;
        _acceptanceNote = string.Empty;
        _reversalReason = string.Empty;
        _busyAction = null;
        Warehouses.Clear();
        CustodyDrivers.Clear();
        CollectionActivity.Clear();
        HandoverActivity.Clear();
        AcceptanceActivity.Clear();
        PendingHandovers.Clear();
        OverduePromises.Clear();
        Exceptions.Clear();
        Handovers.Clear();
        HandoverLines.Clear();
        ClearMessage();
        ReportError = string.Empty;
        RaiseReportState();
        RaiseReconciliationState();
        OnPropertyChanged(nameof(SelectedHandoverRow));
        OnPropertyChanged(nameof(From));
        OnPropertyChanged(nameof(To));
        OnPropertyChanged(nameof(FromDateValue));
        OnPropertyChanged(nameof(ToDateValue));
        OnPropertyChanged(nameof(SelectedWarehouseId));
        OnPropertyChanged(nameof(ActiveTabIndex));
    }

    private static string SumCounts(IEnumerable<CodCurrencyAmountData> rows)
    {
        var total = BigInteger.Zero;
        foreach (var row in rows)
            if (BigInteger.TryParse(row.CollectionCount, out var count))
                total += count;
        return CodAccountingPresentation.Count(total.ToString());
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
