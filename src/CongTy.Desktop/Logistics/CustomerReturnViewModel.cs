using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed partial class CustomerReturnViewModel : INotifyPropertyChanged
{
    private const int IntentCacheLimit = 256;
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Hàng khách trả.";

    private readonly ICustomerReturnService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);

    private bool _loaded;
    private bool _tabInitialized;
    private Task<bool>? _initialLoadTask;
    private long _accessGeneration;
    private long _loadGeneration;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private bool _requiresTripReconciliation;
    private int _activeTabIndex;
    private CustomerReturnEligibilityRow? _selectedEligibility;
    private CustomerReturnListRow? _selectedReturnRow;
    private CustomerReturnData? _selectedReturn;
    private string _quantity = string.Empty;
    private string _reasonCode = "DAMAGED_OR_UNWANTED";
    private string _reasonNote = string.Empty;
    private string _returnNote = string.Empty;
    private string _cancelReason = string.Empty;

    public CustomerReturnViewModel(
        ICustomerReturnService service,
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
            _tabInitialized = false;
            _initialLoadTask = null;
            Reset();
            RaisePermissions();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CustomerReturnEligibilityRow> Eligibility { get; } = [];
    public ObservableCollection<CustomerReturnListRow> Returns { get; } = [];
    public ObservableCollection<CustomerReturnLineRow> ReturnLines { get; } = [];
    public IReadOnlyList<CustomerReturnReasonOption> Reasons => CustomerReturnPresentation.Reasons;

    public bool CanRead => _access.HasPermission("core.customer-return.read");
    public bool CanCreate => _access.HasPermission("core.customer-return.create");
    public bool CanReceive => _access.HasPermission("core.customer-return.receive");
    public bool CanCancel => _access.HasPermission("core.customer-return.cancel");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoading => _busyAction == "load";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool RequiresTripReconciliation
    {
        get => _requiresTripReconciliation;
        private set => SetField(ref _requiresTripReconciliation, value);
    }

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

    public string RefreshButtonText => IsLoading ? "Đang tải..." : "Làm mới";

    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set => SetField(ref _activeTabIndex, Math.Clamp(value, 0, 1));
    }

    public int EligibleCount => Eligibility.Count;
    public int DraftCount => Returns.Count(row => row.Data.Status == "draft");
    public int ReceivedCount => Returns.Count(row => row.Data.Status == "received");
    public int CancelledCount => Returns.Count(row => row.Data.Status == "cancelled");
    public bool HasEligibility => Eligibility.Count > 0;
    public bool HasReturns => Returns.Count > 0;
    public bool HasSelectedEligibility => SelectedEligibility is not null;
    public bool HasSelectedReturn => SelectedReturn is not null;
    public bool IsSelectedReturnDraft => SelectedReturn?.Status == "draft";
    public bool ShowMovement => !string.IsNullOrWhiteSpace(SelectedReturn?.InventoryMovementId);
    public bool ShowCancellation => !string.IsNullOrWhiteSpace(SelectedReturn?.CancellationReason);

    public CustomerReturnEligibilityRow? SelectedEligibility
    {
        get => _selectedEligibility;
        set
        {
            if (!SetField(ref _selectedEligibility, value)) return;
            Quantity = value is null ? string.Empty : CustomerReturnPresentation.Quantity(value.Data.AvailableReturnBaseQuantity);
            RaiseCreateState();
        }
    }

    public CustomerReturnListRow? SelectedReturnRow
    {
        get => _selectedReturnRow;
        set
        {
            if (!SetField(ref _selectedReturnRow, value)) return;
            if (value is null)
            {
                SelectedReturn = null;
                return;
            }
            _ = LoadDetailAsync(value.Data.Id);
        }
    }

    public CustomerReturnData? SelectedReturn
    {
        get => _selectedReturn;
        private set
        {
            if (!SetField(ref _selectedReturn, value)) return;
            Replace(ReturnLines, value is null
                ? []
                : value.Lines.Select(line => new CustomerReturnLineRow(line, value.Status == "draft")));
            CancelReason = string.Empty;
            RaiseProcessState();
        }
    }

    public string Quantity
    {
        get => _quantity;
        set
        {
            if (!SetField(ref _quantity, value ?? string.Empty)) return;
            RaiseCreateState();
        }
    }

    public string ReasonCode
    {
        get => _reasonCode;
        set => SetField(ref _reasonCode, value ?? "OTHER");
    }

    public string ReasonNote
    {
        get => _reasonNote;
        set
        {
            if (!SetField(ref _reasonNote, value ?? string.Empty)) return;
            RaiseCreateState();
        }
    }

    public string ReturnNote
    {
        get => _returnNote;
        set => SetField(ref _returnNote, value ?? string.Empty);
    }

    public string CancelReason
    {
        get => _cancelReason;
        set
        {
            if (!SetField(ref _cancelReason, value ?? string.Empty)) return;
            RaiseProcessState();
        }
    }

    public string SelectedSourceTitle => SelectedEligibility?.Item ?? "Chọn dòng hàng đã xuất";
    public string SelectedSourceCustomer => SelectedEligibility?.Customer ?? string.Empty;
    public string SelectedSourceNumber => SelectedEligibility?.Number ?? string.Empty;
    public string SelectedSourceMaximum => SelectedEligibility is null
        ? string.Empty
        : $"Tối đa {CustomerReturnPresentation.Quantity(SelectedEligibility.Data.AvailableReturnBaseQuantity)} {SelectedEligibility.Data.UnitCode}";

    public string SelectedReturnNumber => CustomerReturnPresentation.Number(SelectedReturn?.Number);
    public string SelectedReturnCustomer => SelectedReturn is null ? string.Empty : $"{SelectedReturn.CustomerCode} — {SelectedReturn.CustomerName}";
    public string SelectedReturnWarehouse => SelectedReturn is null ? string.Empty : $"{SelectedReturn.WarehouseCode} — {SelectedReturn.WarehouseName}";
    public string SelectedReturnStatus => CustomerReturnPresentation.Status(SelectedReturn?.Status);
    public string MovementDisplay => SelectedReturn?.InventoryMovementId ?? string.Empty;
    public string CancellationDisplay => SelectedReturn?.CancellationReason ?? string.Empty;

    public bool CanCreateAction =>
        CanCreate && IsNotBusy && SelectedEligibility is not null
        && !string.IsNullOrWhiteSpace(ReasonNote)
        && ValidCreateQuantity();

    public bool CanReceiveAction => CanReceive && IsNotBusy && IsSelectedReturnDraft;
    public bool CanCancelAction =>
        CanCancel && IsNotBusy && IsSelectedReturnDraft && !string.IsNullOrWhiteSpace(CancelReason);

    public string CreateButtonText => _busyAction == "create" ? "Đang tạo..." : "Tạo phiếu trả nháp";
    public string ReceiveButtonText => _busyAction == "receive" ? "Đang nhận kho..." : "Xác nhận thực nhận và nhập kho";
    public string CancelButtonText => _busyAction == "cancel" ? "Đang hủy..." : "Hủy phiếu nháp";

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);
        _initialLoadTask = LoadAllAsync();
        try
        {
            _loaded = await _initialLoadTask.ConfigureAwait(true);
            return _loaded;
        }
        finally { _initialLoadTask = null; }
    }

    public Task<bool> RefreshAsync() => LoadAllAsync(SelectedReturn?.Id ?? SelectedReturnRow?.Data.Id);

    public async Task CreateAsync()
    {
        var source = SelectedEligibility;
        if (source is null || !CanCreate || IsBusy) return;

        if (!CustomerReturnPresentation.TryScaledQuantity(Quantity, out var quantity)
            || quantity <= 0)
        {
            SetErrorMessage("Số lượng trả phải lớn hơn 0.");
            return;
        }
        CustomerReturnPresentation.TryScaledQuantity(source.Data.AvailableReturnBaseQuantity, out var available);
        if (quantity > available)
        {
            SetErrorMessage("Số lượng trả không được vượt phần còn có thể trả.");
            return;
        }

        var reason = ReasonNote.Trim();
        if (reason.Length == 0 || reason.Length > 2000)
        {
            SetErrorMessage("Nhập lý do chi tiết, tối đa 2.000 ký tự.");
            return;
        }
        var note = ReturnNote.Trim();
        if (note.Length > 4000)
        {
            SetErrorMessage("Ghi chú phiếu trả tối đa 4.000 ký tự.");
            return;
        }

        var normalizedQuantity = CustomerReturnPresentation.Quantity(Quantity);
        var intent = Intent("create", source.Data.IssueLineId, normalizedQuantity, ReasonCode, reason, note);
        SetBusy("create");
        ClearMessage();
        try
        {
            var result = await _service.CreateAsync(
                new CustomerReturnCreateRequest(
                    EmptyToNull(note),
                    [new CustomerReturnCreateLineRequest(source.Data.IssueLineId, normalizedQuantity, ReasonCode, reason)]),
                KeyFor(intent)).ConfigureAwait(true);

            _intentKeys.Remove(intent);
            ReasonNote = string.Empty;
            ReturnNote = string.Empty;
            ActiveTabIndex = 1;
            await LoadAllAsync(result.CustomerReturn.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice(result.Replayed
                ? "Phiếu trả đã được tạo trước đó; dữ liệu được tải lại."
                : "Đã tạo phiếu hàng khách trả nháp; tồn kho chưa thay đổi.");
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally { SetBusy(null); }
    }

    public async Task ReceiveAsync()
    {
        var detail = SelectedReturn;
        if (detail is null || !CanReceive || detail.Status != "draft" || IsBusy) return;

        var lines = new List<CustomerReturnReceiveLineRequest>();
        var anyPositive = false;
        foreach (var row in ReturnLines)
        {
            if (!CustomerReturnPresentation.TryScaledQuantity(row.AcceptedQuantity, out var accepted) || accepted < 0)
            {
                SetErrorMessage($"Số lượng thực nhận của {row.Data.Sku} không hợp lệ.");
                return;
            }
            CustomerReturnPresentation.TryScaledQuantity(row.Data.RequestedBaseQuantity, out var requested);
            if (accepted > requested)
            {
                SetErrorMessage($"Số lượng thực nhận của {row.Data.Sku} vượt số lượng yêu cầu.");
                return;
            }
            if (accepted > 0) anyPositive = true;
            lines.Add(new CustomerReturnReceiveLineRequest(row.Data.Id, CustomerReturnPresentation.Quantity(row.AcceptedQuantity)));
        }
        if (!anyPositive)
        {
            SetErrorMessage("Ít nhất một dòng phải có số lượng thực nhận lớn hơn 0.");
            return;
        }

        var canonical = lines.OrderBy(row => row.CustomerReturnLineId, StringComparer.Ordinal).ToArray();
        var fingerprint = string.Join(".", canonical.Select(row => $"{row.CustomerReturnLineId}-{row.AcceptedQuantity}"));
        var documentDate = CustomerReturnPresentation.LocalDate();
        var intent = Intent("receive", detail.Id, detail.Revision, documentDate, fingerprint);

        SetBusy("receive");
        ClearMessage();
        try
        {
            var result = await _service.ReceiveAsync(
                detail.Id,
                new CustomerReturnReceiveRequest(documentDate, detail.Revision, lines.ToArray()),
                KeyFor(intent)).ConfigureAwait(true);
            _intentKeys.Remove(intent);
            await LoadAllAsync(result.CustomerReturn.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice(result.Replayed
                ? "Việc nhận kho đã được xử lý trước đó; dữ liệu được tải lại."
                : "Đã xác nhận thực nhận và nhập hàng vào kho.");
        }
        catch (Exception exception) { SetError(exception); }
        finally { SetBusy(null); }
    }

    public async Task CancelAsync()
    {
        var detail = SelectedReturn;
        if (detail is null || !CanCancel || detail.Status != "draft" || IsBusy) return;
        var reason = CancelReason.Trim();
        if (reason.Length == 0 || reason.Length > 1000)
        {
            SetErrorMessage("Nhập lý do hủy phiếu nháp, tối đa 1.000 ký tự.");
            return;
        }

        var intent = Intent("cancel", detail.Id, detail.Revision, reason);
        SetBusy("cancel");
        ClearMessage();
        try
        {
            var result = await _service.CancelAsync(
                detail.Id,
                new CustomerReturnCancelRequest(reason),
                KeyFor(intent)).ConfigureAwait(true);
            _intentKeys.Remove(intent);
            ActiveTabIndex = 0;
            await LoadAllAsync(result.CustomerReturn.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice(result.Replayed
                ? "Việc hủy phiếu đã được xử lý trước đó; dữ liệu được tải lại."
                : "Đã hủy phiếu nháp; số lượng nguồn được mở lại để lập phiếu khác.");
        }
        catch (Exception exception) { SetError(exception); }
        finally { SetBusy(null); }
    }

    private async Task<bool> LoadAllAsync(string? preferredReturnId = null, bool preserveMessage = false)
    {
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetErrorMessage(ReadDenied);
            return false;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        SetBusy("load");
        if (!preserveMessage) ClearMessage();

        try
        {
            var eligibilityTask = _service.ListEligibilityAsync();
            var returnsTask = _service.ListAsync();
            await Task.WhenAll(eligibilityTask, returnsTask).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead) return false;

            Replace(Eligibility, (await eligibilityTask.ConfigureAwait(true)).Select(CustomerReturnPresentation.Eligibility));
            Replace(Returns, (await returnsTask.ConfigureAwait(true)).Select(CustomerReturnPresentation.ListRow));

            if (!_tabInitialized)
            {
                _tabInitialized = true;
                ActiveTabIndex = Eligibility.Count == 0 && Returns.Count > 0 ? 1 : 0;
            }

            var source = SelectedEligibility is null
                ? Eligibility.FirstOrDefault()
                : Eligibility.FirstOrDefault(row => row.Data.IssueLineId == SelectedEligibility.Data.IssueLineId) ?? Eligibility.FirstOrDefault();
            _selectedEligibility = source;
            OnPropertyChanged(nameof(SelectedEligibility));
            Quantity = source is null ? string.Empty : CustomerReturnPresentation.Quantity(source.Data.AvailableReturnBaseQuantity);

            var target = !string.IsNullOrWhiteSpace(preferredReturnId)
                ? Returns.FirstOrDefault(row => row.Data.Id == preferredReturnId)
                : SelectedReturnRow is null ? Returns.FirstOrDefault() : Returns.FirstOrDefault(row => row.Data.Id == SelectedReturnRow.Data.Id) ?? Returns.FirstOrDefault();
            _selectedReturnRow = target;
            OnPropertyChanged(nameof(SelectedReturnRow));

            if (target is null) SelectedReturn = null;
            else await LoadDetailAsync(target.Data.Id, request).ConfigureAwait(true);

            RaiseCounts();
            _loaded = true;
            return true;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration) SetError(exception);
            return false;
        }
        finally
        {
            if (request == _loadGeneration && _busyAction == "load") SetBusy(null);
        }
    }

    private async Task LoadDetailAsync(string id, long? parentRequest = null)
    {
        var accessGeneration = _accessGeneration;
        var request = parentRequest ?? ++_loadGeneration;
        var ownsBusy = parentRequest is null;
        if (ownsBusy)
        {
            SetBusy($"detail-{id}");
            ClearMessage();
        }

        try
        {
            var detail = await _service.GetAsync(id).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead) return;
            SelectedReturn = detail;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration) SetError(exception);
        }
        finally
        {
            if (ownsBusy && request == _loadGeneration && _busyAction == $"detail-{id}") SetBusy(null);
        }
    }

    private bool ValidCreateQuantity()
    {
        if (SelectedEligibility is null) return false;
        if (!CustomerReturnPresentation.TryScaledQuantity(Quantity, out var quantity) || quantity <= 0) return false;
        CustomerReturnPresentation.TryScaledQuantity(SelectedEligibility.Data.AvailableReturnBaseQuantity, out var available);
        return quantity <= available;
    }

    private string Intent(string prefix, params string?[] parts) =>
        $"{prefix}|{string.Join("|", parts.Select(value => value?.Trim() ?? string.Empty))}";

    private string KeyFor(string intent)
    {
        if (_intentKeys.TryGetValue(intent, out var existing)) return existing;
        if (_intentKeys.Count >= IntentCacheLimit)
        {
            var oldest = _intentKeys.Keys.FirstOrDefault();
            if (oldest is not null) _intentKeys.Remove(oldest);
        }
        var prefix = intent.Split('|', 2)[0];
        var key = _idempotencyKeys.Create($"customer-return-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private void Reset()
    {
        Eligibility.Clear();
        Returns.Clear();
        ReturnLines.Clear();
        _selectedEligibility = null;
        _selectedReturnRow = null;
        _selectedReturn = null;
        _quantity = string.Empty;
        _reasonNote = string.Empty;
        _returnNote = string.Empty;
        _cancelReason = string.Empty;
        ClearMessage();
        OnPropertyChanged(nameof(SelectedEligibility));
        OnPropertyChanged(nameof(SelectedReturnRow));
        RaiseCounts();
        RaiseCreateState();
        RaiseProcessState();
    }

    private void SetBusy(string? action)
    {
        if (string.Equals(_busyAction, action, StringComparison.Ordinal)) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(RefreshButtonText));
        OnPropertyChanged(nameof(CreateButtonText));
        OnPropertyChanged(nameof(ReceiveButtonText));
        OnPropertyChanged(nameof(CancelButtonText));
        RaiseCreateState();
        RaiseProcessState();
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanReceive));
        OnPropertyChanged(nameof(CanCancel));
        RaiseCreateState();
        RaiseProcessState();
    }

    private void RaiseCounts()
    {
        OnPropertyChanged(nameof(EligibleCount));
        OnPropertyChanged(nameof(DraftCount));
        OnPropertyChanged(nameof(ReceivedCount));
        OnPropertyChanged(nameof(CancelledCount));
        OnPropertyChanged(nameof(HasEligibility));
        OnPropertyChanged(nameof(HasReturns));
    }

    private void RaiseCreateState()
    {
        OnPropertyChanged(nameof(HasSelectedEligibility));
        OnPropertyChanged(nameof(SelectedSourceTitle));
        OnPropertyChanged(nameof(SelectedSourceCustomer));
        OnPropertyChanged(nameof(SelectedSourceNumber));
        OnPropertyChanged(nameof(SelectedSourceMaximum));
        OnPropertyChanged(nameof(CanCreateAction));
    }

    private void RaiseProcessState()
    {
        OnPropertyChanged(nameof(HasSelectedReturn));
        OnPropertyChanged(nameof(IsSelectedReturnDraft));
        OnPropertyChanged(nameof(ShowMovement));
        OnPropertyChanged(nameof(ShowCancellation));
        OnPropertyChanged(nameof(SelectedReturnNumber));
        OnPropertyChanged(nameof(SelectedReturnCustomer));
        OnPropertyChanged(nameof(SelectedReturnWarehouse));
        OnPropertyChanged(nameof(SelectedReturnStatus));
        OnPropertyChanged(nameof(MovementDisplay));
        OnPropertyChanged(nameof(CancellationDisplay));
        OnPropertyChanged(nameof(CanReceiveAction));
        OnPropertyChanged(nameof(CanCancelAction));
    }

    private void ClearMessage()
    {
        MessageIsError = false;
        Message = string.Empty;
        RequiresTripReconciliation = false;
    }

    private void SetNotice(string value)
    {
        MessageIsError = false;
        Message = value;
        RequiresTripReconciliation = false;
    }

    private void SetErrorMessage(string value)
    {
        MessageIsError = true;
        Message = value;
    }

    private void SetError(Exception exception)
    {
        if (exception is CanonicalApiException api)
        {
            RequiresTripReconciliation = api.Code == "CUSTOMER_RETURN_RECEIVABLE_NOT_POSTED";
            var message = api.Code switch
            {
                "CUSTOMER_RETURN_RECEIVABLE_NOT_POSTED" => "Phiếu giao chưa phát sinh công nợ. Nếu khách chưa nhận hàng, hãy nhập hàng về tại Đối soát cuối chuyến.",
                "CUSTOMER_RETURN_QUANTITY_CONFLICT" => "Số lượng trả vượt phần còn có thể trả.",
                "CUSTOMER_RETURN_REVISION_CONFLICT" => "Phiếu trả đã thay đổi. Tải lại dữ liệu trước khi nhận kho.",
                "CUSTOMER_RETURN_NOT_DRAFT" => "Chỉ phiếu nháp mới được nhận kho hoặc hủy.",
                _ => CanonicalErrorMessages.ToOfficeMessage(api)
            };
            SetErrorMessage(CanonicalErrorMessages.WithRequestId(message, api.RequestId));
            return;
        }
        SetErrorMessage(exception.Message);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
