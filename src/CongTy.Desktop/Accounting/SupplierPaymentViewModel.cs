using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed class SupplierPaymentViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.supplier-payment.read";
    private const string CreatePermission = "core.supplier-payment.create";
    private const string ReversePermission = "core.supplier-payment.reverse";
    private const string PayableReadPermission = "core.payable.read";
    private const string AllocatePermission = "core.payable-allocation.create";
    private const string ReverseAllocationPermission = "core.payable-allocation.reverse";
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Thanh toán nhà cung cấp.";

    private readonly ISupplierPaymentService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);

    private SupplierPaymentData[] _payments = [];
    private SupplierPaymentAllocationTargetData[] _targets = [];
    private SupplierData[] _suppliers = [];
    private WarehouseData[] _warehouses = [];
    private SupplierPaymentData? _selectedPayment;
    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private long _accessGeneration;
    private long _requestGeneration;

    private SupplierPaymentChoice? _selectedSupplier;
    private SupplierPaymentChoice? _selectedWarehouse;
    private SupplierPaymentRow? _selectedPaymentRow;
    private SupplierPaymentTargetChoice? _selectedTarget;
    private string _paymentDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _paymentMethod = "BANK_TRANSFER";
    private string _amount = string.Empty;
    private string _externalReference = string.Empty;
    private string _note = string.Empty;
    private string _allocationDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _allocationAmount = string.Empty;
    private string _reversalReason = string.Empty;

    public SupplierPaymentViewModel(
        ISupplierPaymentService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _requestGeneration++;
            _loaded = false;
            IsBusy = false;
            _payments = [];
            _targets = [];
            _suppliers = [];
            _warehouses = [];
            _selectedPayment = null;
            Suppliers.Clear();
            Warehouses.Clear();
            Payments.Clear();
            Targets.Clear();
            AllocationHistory.Clear();
            SelectedPaymentRow = null;
            SelectedTarget = null;
            Message = string.Empty;
            RaisePermissions();
            RaiseDetail();
            OnPropertyChanged(nameof(ShowEmptyPayments));
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SupplierPaymentChoice> Suppliers { get; } = [];
    public ObservableCollection<SupplierPaymentChoice> Warehouses { get; } = [];
    public ObservableCollection<SupplierPaymentRow> Payments { get; } = [];
    public ObservableCollection<SupplierPaymentTargetChoice> Targets { get; } = [];
    public ObservableCollection<SupplierPaymentAllocationRow> AllocationHistory { get; } = [];

    public IReadOnlyList<SupplierPaymentChoice> PaymentMethods { get; } =
    [
        new("BANK_TRANSFER", "Chuyển khoản"),
        new("CASH", "Tiền mặt"),
        new("OTHER", "Khác")
    ];

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanCreate => _access.HasPermission(CreatePermission);
    public bool CanReadTargets => _access.HasPermission(PayableReadPermission);
    public bool CanAllocate => _access.HasPermission(AllocatePermission);
    public bool CanReversePayment => _access.HasPermission(ReversePermission);
    public bool CanReverseAllocation => _access.HasPermission(ReverseAllocationPermission);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaisePermissions();
            OnPropertyChanged(nameof(ShowEmptyPayments));
        }
    }

    public bool CanUseCreateForm => CanCreate && _loaded && !IsBusy;
    public bool CanUseAllocationForm => CanReadTargets && CanAllocate && _loaded && !IsBusy
        && _selectedPayment is not null
        && _selectedPayment.Status != "reversed"
        && SupplierPaymentPresentation.Amount(_selectedPayment.RemainingAmount) > 0m;
    public bool CanUseReversePayment => CanReversePayment && _loaded && !IsBusy
        && _selectedPayment is not null
        && _selectedPayment.Status != "reversed"
        && !_selectedPayment.Allocations.Any(item => !item.Reversed)
        && SupplierPaymentPresentation.Amount(_selectedPayment.AllocatedAmount) <= 0m;

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasSelectedPayment => _selectedPayment is not null;
    public SupplierPaymentData? SelectedPayment => _selectedPayment;
    public bool ShowEmptyPayments => _loaded && !IsBusy && Payments.Count == 0;
    public string DetailEmptyText => HasSelectedPayment ? string.Empty : "Chọn một phiếu thanh toán để xem chi tiết.";
    public string AllocationEmptyText => AllocationHistory.Count == 0 ? "Chưa có phân bổ." : string.Empty;
    public string TargetEmptyText => Targets.Count > 0
        ? string.Empty
        : CanReadTargets ? "Không còn chứng từ phải trả phù hợp để phân bổ." : "Tài khoản chưa được cấp quyền xem chứng từ phải trả.";

    public string Message
    {
        get => _message;
        private set
        {
            if (!SetField(ref _message, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasMessage));
        }
    }

    public SupplierPaymentChoice? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetField(ref _selectedSupplier, value);
    }

    public SupplierPaymentChoice? SelectedWarehouse
    {
        get => _selectedWarehouse;
        set => SetField(ref _selectedWarehouse, value);
    }

    public SupplierPaymentRow? SelectedPaymentRow
    {
        get => _selectedPaymentRow;
        set => SetField(ref _selectedPaymentRow, value);
    }

    public SupplierPaymentTargetChoice? SelectedTarget
    {
        get => _selectedTarget;
        set => SetField(ref _selectedTarget, value);
    }

    public string PaymentDate
    {
        get => _paymentDate;
        set => SetField(ref _paymentDate, value ?? string.Empty);
    }

    public string PaymentMethod
    {
        get => _paymentMethod;
        set => SetField(ref _paymentMethod, value ?? "BANK_TRANSFER");
    }

    public string Amount
    {
        get => _amount;
        set => SetField(ref _amount, value ?? string.Empty);
    }

    public string ExternalReference
    {
        get => _externalReference;
        set => SetField(ref _externalReference, value ?? string.Empty);
    }

    public string Note
    {
        get => _note;
        set => SetField(ref _note, value ?? string.Empty);
    }

    public string AllocationDate
    {
        get => _allocationDate;
        set => SetField(ref _allocationDate, value ?? string.Empty);
    }

    public string AllocationAmount
    {
        get => _allocationAmount;
        set => SetField(ref _allocationAmount, value ?? string.Empty);
    }

    public string ReversalReason
    {
        get => _reversalReason;
        set => SetField(ref _reversalReason, value ?? string.Empty);
    }

    public string SelectedPaymentId => _selectedPayment?.Id ?? string.Empty;
    public string SelectedDocumentNumber => _selectedPayment?.DocumentNumber ?? "—";
    public string SelectedSupplierText => _selectedPayment is null
        ? "—"
        : SupplierPaymentPresentation.Party(_selectedPayment.SupplierCode, _selectedPayment.SupplierName);
    public string SelectedRemainingText => _selectedPayment is null
        ? "—"
        : $"{SupplierPaymentPresentation.Money(_selectedPayment.RemainingAmount, _selectedPayment.CurrencyCode)} chưa phân bổ";
    public string SelectedOriginalText => _selectedPayment is null
        ? "—"
        : SupplierPaymentPresentation.Money(_selectedPayment.OriginalAmount, _selectedPayment.CurrencyCode);
    public string SelectedAllocatedText => _selectedPayment is null
        ? "—"
        : SupplierPaymentPresentation.Money(_selectedPayment.AllocatedAmount, _selectedPayment.CurrencyCode);
    public string SelectedStatusText => _selectedPayment is null
        ? "—"
        : SupplierPaymentPresentation.Status(_selectedPayment.Status);
    public string SelectedWarehouseText => _selectedPayment is null
        ? "—"
        : SupplierPaymentPresentation.Party(_selectedPayment.WarehouseCode, _selectedPayment.WarehouseName);
    public string SelectedPaymentMethodText => _selectedPayment is null
        ? "—"
        : SupplierPaymentPresentation.PaymentMethod(_selectedPayment.PaymentMethod);
    public string SelectedPaymentDateText => _selectedPayment is null
        ? "—"
        : SupplierPaymentPresentation.Date(_selectedPayment.PaymentDate);

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        return await RefreshAsync().ConfigureAwait(true);
    }

    public async Task<bool> RefreshAsync(string? selectId = null)
    {
        if (!await LoadListsAsync().ConfigureAwait(true)) return false;

        var requested = selectId ?? SelectedPaymentRow?.Id;
        var desired = !string.IsNullOrWhiteSpace(requested) && _payments.Any(item => item.Id == requested)
            ? requested
            : _payments.FirstOrDefault()?.Id;
        if (!string.IsNullOrWhiteSpace(desired))
            await OpenPaymentAsync(desired).ConfigureAwait(true);
        else
            ClearDetail();

        return true;
    }

    public async Task OpenPaymentAsync(string id)
    {
        if (IsBusy || !CanRead || string.IsNullOrWhiteSpace(id)) return;

        var accessGeneration = _accessGeneration;
        var request = ++_requestGeneration;
        IsBusy = true;
        try
        {
            var detail = await _service.GetPaymentAsync(id).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _requestGeneration || !CanRead) return;

            _selectedPayment = detail;
            SelectedPaymentRow = Payments.FirstOrDefault(item => item.Id == detail.Id);
            AllocationAmount = string.Empty;
            ReversalReason = string.Empty;
            RebuildTargets();
            RebuildAllocationHistory();
            RaiseDetail();
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration)
            {
                ClearDetail();
                Message = CanonicalErrorMessages.WithRequestId(OfficeError(exception), exception.RequestId);
            }
        }
        catch (Exception)
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration)
            {
                ClearDetail();
                Message = "Không tải được chi tiết phiếu thanh toán. Hãy chọn lại phiếu trước khi thao tác.";
            }
        }
        finally
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration) IsBusy = false;
        }
    }

    public async Task SavePaymentAsync()
    {
        if (!CanUseCreateForm) return;
        if (SelectedSupplier is null || SelectedWarehouse is null)
        {
            Message = "Chọn nhà cung cấp và kho trước khi ghi nhận thanh toán.";
            return;
        }
        if (!ValidDate(PaymentDate))
        {
            Message = "Ngày thanh toán phải theo định dạng yyyy-MM-dd.";
            return;
        }

        var amount = SupplierPaymentPresentation.Amount(Amount);
        if (amount <= 0m)
        {
            Message = "Số tiền thanh toán phải lớn hơn 0.";
            return;
        }

        var request = new SupplierPaymentCreateRequest(
            SelectedSupplier.Id,
            SelectedWarehouse.Id,
            PaymentDate.Trim(),
            "VND",
            PaymentMethod,
            DecimalText(amount),
            NullIfBlank(ExternalReference),
            NullIfBlank(Note));

        var slot = string.Join("|",
            "create",
            request.SupplierId,
            request.WarehouseId,
            request.PaymentDate,
            request.PaymentMethod,
            request.Amount,
            request.ExternalReference,
            request.Note);
        var key = MutationKey(slot, "supplier-payment-create");
        var accessGeneration = _accessGeneration;

        IsBusy = true;
        Message = string.Empty;
        try
        {
            var created = await _service.CreatePaymentAsync(request, key).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || !CanCreate) return;

            _mutationKeys.Remove(slot);
            Amount = string.Empty;
            ExternalReference = string.Empty;
            Note = string.Empty;
            IsBusy = false;
            await RefreshAsync(created.Id).ConfigureAwait(true);
            Message = $"Đã ghi nhận phiếu thanh toán {created.DocumentNumber}.";
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration)
                Message = CanonicalErrorMessages.WithRequestId(OfficeError(exception), exception.RequestId);
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration)
                Message = string.IsNullOrWhiteSpace(exception.Message) ? "Không ghi nhận được thanh toán nhà cung cấp." : exception.Message;
        }
        finally
        {
            if (accessGeneration == _accessGeneration) IsBusy = false;
        }
    }

    public async Task AllocateAsync()
    {
        if (!CanUseAllocationForm || _selectedPayment is null) return;
        if (SelectedTarget is null)
        {
            Message = "Chọn chứng từ phải trả cần phân bổ.";
            return;
        }
        if (!ValidDate(AllocationDate))
        {
            Message = "Ngày phân bổ phải theo định dạng yyyy-MM-dd.";
            return;
        }

        var amount = SupplierPaymentPresentation.Amount(AllocationAmount);
        var sourceRemaining = SupplierPaymentPresentation.Amount(_selectedPayment.RemainingAmount);
        var targetRemaining = SupplierPaymentPresentation.Amount(SelectedTarget.RemainingAmount);
        if (amount <= 0m || amount > sourceRemaining || amount > targetRemaining)
        {
            Message = "Số tiền phân bổ phải lớn hơn 0 và không vượt số còn lại của phiếu hoặc chứng từ phải trả.";
            return;
        }

        var request = new SupplierPaymentAllocationRequest(
            _selectedPayment.Id,
            SelectedTarget.Id,
            DecimalText(amount),
            AllocationDate.Trim());
        var slot = string.Join("|",
            "allocate",
            request.SourcePayableDocumentId,
            request.TargetPayableDocumentId,
            request.Amount,
            request.AllocationDate);
        var key = MutationKey(slot, "payable-allocation-create");
        var accessGeneration = _accessGeneration;
        var selectedId = _selectedPayment.Id;

        IsBusy = true;
        Message = string.Empty;
        try
        {
            await _service.AllocateAsync(request, key).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || !CanAllocate) return;

            _mutationKeys.Remove(slot);
            AllocationAmount = string.Empty;
            IsBusy = false;
            await RefreshAsync(selectedId).ConfigureAwait(true);
            Message = "Đã phân bổ thanh toán vào chứng từ phải trả.";
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration)
                Message = CanonicalErrorMessages.WithRequestId(OfficeError(exception), exception.RequestId);
        }
        catch (Exception)
        {
            if (accessGeneration == _accessGeneration)
                Message = "Không phân bổ được thanh toán vào chứng từ phải trả.";
        }
        finally
        {
            if (accessGeneration == _accessGeneration) IsBusy = false;
        }
    }

    public async Task ReverseAllocationAsync(SupplierPaymentAllocationRow? row)
    {
        if (row is null || !row.CanReverse || !CanReverseAllocation || IsBusy || _selectedPayment is null) return;

        var reason = ReversalReason.Trim();
        if (reason.Length == 0)
        {
            Message = "Nhập lý do đảo trước khi thực hiện.";
            return;
        }
        if (reason.Length > 2000)
        {
            Message = "Lý do đảo không được vượt quá 2000 ký tự.";
            return;
        }

        var request = new SupplierPaymentReverseRequest(reason);
        var slot = $"reverse-allocation|{row.Id}|{reason}";
        var key = MutationKey(slot, "payable-allocation-reverse");
        var selectedId = _selectedPayment.Id;
        var accessGeneration = _accessGeneration;

        IsBusy = true;
        Message = string.Empty;
        try
        {
            await _service.ReverseAllocationAsync(row.Id, request, key).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || !CanReverseAllocation) return;

            _mutationKeys.Remove(slot);
            IsBusy = false;
            await RefreshAsync(selectedId).ConfigureAwait(true);
            Message = "Đã đảo phân bổ.";
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration)
                Message = CanonicalErrorMessages.WithRequestId(OfficeError(exception), exception.RequestId);
        }
        catch (Exception)
        {
            if (accessGeneration == _accessGeneration) Message = "Không đảo được phân bổ đã chọn.";
        }
        finally
        {
            if (accessGeneration == _accessGeneration) IsBusy = false;
        }
    }

    public async Task ReversePaymentAsync()
    {
        if (!CanUseReversePayment || _selectedPayment is null) return;

        var reason = ReversalReason.Trim();
        if (reason.Length == 0)
        {
            Message = "Nhập lý do đảo trước khi thực hiện.";
            return;
        }
        if (reason.Length > 2000)
        {
            Message = "Lý do đảo không được vượt quá 2000 ký tự.";
            return;
        }

        var request = new SupplierPaymentReverseRequest(reason);
        var selectedId = _selectedPayment.Id;
        var slot = $"reverse-payment|{selectedId}|{reason}";
        var key = MutationKey(slot, "supplier-payment-reverse");
        var accessGeneration = _accessGeneration;

        IsBusy = true;
        Message = string.Empty;
        try
        {
            var reversed = await _service.ReversePaymentAsync(selectedId, request, key).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || !CanReversePayment) return;

            _mutationKeys.Remove(slot);
            IsBusy = false;
            await RefreshAsync(reversed.Id).ConfigureAwait(true);
            Message = $"Đã đảo phiếu thanh toán {reversed.DocumentNumber}.";
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration)
                Message = CanonicalErrorMessages.WithRequestId(OfficeError(exception), exception.RequestId);
        }
        catch (Exception)
        {
            if (accessGeneration == _accessGeneration) Message = "Không đảo được phiếu thanh toán.";
        }
        finally
        {
            if (accessGeneration == _accessGeneration) IsBusy = false;
        }
    }

    private async Task<bool> LoadListsAsync()
    {
        if (IsBusy) return false;
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) Message = ReadDenied;
            return false;
        }

        _loaded = false;
        RaisePermissions();
        OnPropertyChanged(nameof(ShowEmptyPayments));

        var accessGeneration = _accessGeneration;
        var request = ++_requestGeneration;
        IsBusy = true;
        Message = string.Empty;

        try
        {
            var paymentsTask = _service.ListPaymentsAsync();
            var suppliersTask = _service.ListSuppliersAsync();
            var warehousesTask = _service.ListWarehousesAsync();
            var targetsTask = CanReadTargets
                ? _service.ListTargetsAsync()
                : Task.FromResult<IReadOnlyList<SupplierPaymentAllocationTargetData>>([]);

            await Task.WhenAll(paymentsTask, suppliersTask, warehousesTask, targetsTask).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _requestGeneration || !CanRead) return false;

            _payments = (await paymentsTask.ConfigureAwait(true)).ToArray();
            _suppliers = (await suppliersTask.ConfigureAwait(true)).Where(item => item.IsActive).ToArray();
            _warehouses = (await warehousesTask.ConfigureAwait(true)).Where(item => item.IsActive).ToArray();
            _targets = (await targetsTask.ConfigureAwait(true)).ToArray();

            ApplyLists();
            _loaded = true;
            RaisePermissions();
            OnPropertyChanged(nameof(ShowEmptyPayments));
            return true;
        }
        catch (CanonicalApiException exception)
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration)
                Message = CanonicalErrorMessages.WithRequestId(OfficeError(exception), exception.RequestId);
            return false;
        }
        catch (Exception)
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration)
                Message = "Một phần dữ liệu thanh toán nhà cung cấp chưa tải được. Hãy cập nhật lại trang trước khi thao tác.";
            return false;
        }
        finally
        {
            if (accessGeneration == _accessGeneration && request == _requestGeneration) IsBusy = false;
        }
    }

    private void ApplyLists()
    {
        var supplierId = SelectedSupplier?.Id;
        Suppliers.Clear();
        foreach (var supplier in _suppliers)
            Suppliers.Add(new SupplierPaymentChoice(supplier.Id, SupplierPaymentPresentation.Party(supplier.Code, supplier.Name)));
        SelectedSupplier = Suppliers.FirstOrDefault(item => item.Id == supplierId) ?? Suppliers.FirstOrDefault();

        var warehouseId = SelectedWarehouse?.Id;
        Warehouses.Clear();
        foreach (var warehouse in _warehouses)
            Warehouses.Add(new SupplierPaymentChoice(warehouse.Id, SupplierPaymentPresentation.Party(warehouse.Code, warehouse.Name)));
        SelectedWarehouse = Warehouses.FirstOrDefault(item => item.Id == warehouseId) ?? Warehouses.FirstOrDefault();

        Payments.Clear();
        var index = 0;
        foreach (var payment in _payments)
        {
            index++;
            Payments.Add(new SupplierPaymentRow(
                index,
                payment.Id,
                payment.DocumentNumber,
                SupplierPaymentPresentation.Date(payment.PaymentDate),
                SupplierPaymentPresentation.Party(payment.SupplierCode, payment.SupplierName),
                SupplierPaymentPresentation.Money(payment.OriginalAmount, payment.CurrencyCode),
                SupplierPaymentPresentation.Money(payment.RemainingAmount, payment.CurrencyCode),
                SupplierPaymentPresentation.Status(payment.Status)));
        }

        OnPropertyChanged(nameof(ShowEmptyPayments));
    }

    private void RebuildTargets()
    {
        var selectedId = SelectedTarget?.Id;
        Targets.Clear();

        if (_selectedPayment is not null && CanReadTargets)
        {
            foreach (var target in _targets.Where(item =>
                         item.SupplierId == _selectedPayment.SupplierId
                         && item.WarehouseId == _selectedPayment.WarehouseId
                         && string.Equals(item.CurrencyCode, _selectedPayment.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                         && SupplierPaymentPresentation.Amount(item.RemainingAmount) > 0m))
            {
                Targets.Add(new SupplierPaymentTargetChoice(
                    target.Id,
                    $"{target.DocumentNumber} · Hạn {SupplierPaymentPresentation.Date(target.DueDate)} · Còn {SupplierPaymentPresentation.Money(target.RemainingAmount, target.CurrencyCode)}",
                    target.RemainingAmount,
                    target.CurrencyCode));
            }
        }

        SelectedTarget = Targets.FirstOrDefault(item => item.Id == selectedId) ?? Targets.FirstOrDefault();
        OnPropertyChanged(nameof(TargetEmptyText));
    }

    private void RebuildAllocationHistory()
    {
        AllocationHistory.Clear();
        if (_selectedPayment is null) return;

        foreach (var allocation in _selectedPayment.Allocations)
        {
            AllocationHistory.Add(new SupplierPaymentAllocationRow(
                allocation.Id,
                allocation.TargetDocumentNumber ?? "—",
                SupplierPaymentPresentation.Date(allocation.AllocationDate),
                SupplierPaymentPresentation.Money(allocation.Amount, _selectedPayment.CurrencyCode),
                allocation.Reversed ? "Đã đảo" : "Hiệu lực",
                !allocation.Reversed && _loaded && CanReverseAllocation && !IsBusy));
        }

        OnPropertyChanged(nameof(AllocationEmptyText));
    }

    private string MutationKey(string slot, string scope)
    {
        if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;
        var created = _idempotencyKeys.Create(scope);
        _mutationKeys[slot] = created;
        return created;
    }

    private void ClearDetail()
    {
        _selectedPayment = null;
        SelectedPaymentRow = null;
        SelectedTarget = null;
        Targets.Clear();
        AllocationHistory.Clear();
        RaiseDetail();
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanReadTargets));
        OnPropertyChanged(nameof(CanAllocate));
        OnPropertyChanged(nameof(CanReversePayment));
        OnPropertyChanged(nameof(CanReverseAllocation));
        OnPropertyChanged(nameof(CanUseCreateForm));
        OnPropertyChanged(nameof(CanUseAllocationForm));
        OnPropertyChanged(nameof(CanUseReversePayment));
        RebuildAllocationHistory();
    }

    private void RaiseDetail()
    {
        OnPropertyChanged(nameof(HasSelectedPayment));
        OnPropertyChanged(nameof(DetailEmptyText));
        OnPropertyChanged(nameof(SelectedPaymentId));
        OnPropertyChanged(nameof(SelectedDocumentNumber));
        OnPropertyChanged(nameof(SelectedSupplierText));
        OnPropertyChanged(nameof(SelectedRemainingText));
        OnPropertyChanged(nameof(SelectedOriginalText));
        OnPropertyChanged(nameof(SelectedAllocatedText));
        OnPropertyChanged(nameof(SelectedStatusText));
        OnPropertyChanged(nameof(SelectedWarehouseText));
        OnPropertyChanged(nameof(SelectedPaymentMethodText));
        OnPropertyChanged(nameof(SelectedPaymentDateText));
        OnPropertyChanged(nameof(TargetEmptyText));
        OnPropertyChanged(nameof(AllocationEmptyText));
        OnPropertyChanged(nameof(CanUseAllocationForm));
        OnPropertyChanged(nameof(CanUseReversePayment));
    }

    private static string OfficeError(CanonicalApiException exception) => exception.Code switch
    {
        "INVALID_SUPPLIER_ID" => "Nhà cung cấp không hợp lệ.",
        "INVALID_WAREHOUSE_ID" => "Kho không hợp lệ.",
        "INVALID_PAYMENT_DATE" => "Ngày thanh toán không hợp lệ.",
        "INVALID_CURRENCY_CODE" => "Tiền tệ thanh toán không hợp lệ.",
        "INVALID_PAYMENT_METHOD" => "Phương thức thanh toán không hợp lệ.",
        "INVALID_PAYMENT_AMOUNT" => "Số tiền thanh toán phải lớn hơn 0 và tối đa 6 chữ số thập phân.",
        "EXTERNAL_REFERENCE_TOO_LONG" => "Tham chiếu ngân hàng không được vượt quá 256 ký tự.",
        "NOTE_TOO_LONG" => "Ghi chú không được vượt quá 4000 ký tự.",
        "SUPPLIER_NOT_FOUND" => "Không tìm thấy nhà cung cấp đã chọn.",
        "SUPPLIER_INACTIVE" => "Nhà cung cấp đã ngừng hoạt động.",
        "WAREHOUSE_NOT_FOUND" => "Không tìm thấy kho đã chọn.",
        "WAREHOUSE_INACTIVE" => "Kho đã ngừng hoạt động.",
        "PAYABLE_DOCUMENT_NOT_FOUND" => "Không tìm thấy chứng từ phải trả đã chọn.",
        "INVALID_ALLOCATION_SOURCE" => "Phiếu thanh toán không thể dùng làm nguồn phân bổ.",
        "INVALID_ALLOCATION_TARGET" => "Chứng từ đã chọn không thể nhận phân bổ.",
        "ALLOCATION_SUPPLIER_MISMATCH" => "Phiếu thanh toán và chứng từ phải trả không cùng nhà cung cấp.",
        "ALLOCATION_WAREHOUSE_MISMATCH" => "Phiếu thanh toán và chứng từ phải trả không cùng kho.",
        "ALLOCATION_CURRENCY_MISMATCH" => "Phiếu thanh toán và chứng từ phải trả không cùng tiền tệ.",
        "ALLOCATION_EXCEEDS_SOURCE" => "Số tiền phân bổ vượt số chưa phân bổ của phiếu thanh toán.",
        "ALLOCATION_EXCEEDS_TARGET" => "Số tiền phân bổ vượt số còn phải trả của chứng từ.",
        "PAYABLE_ALLOCATION_NOT_FOUND" => "Không tìm thấy phân bổ đã chọn.",
        "PAYABLE_ALLOCATION_ALREADY_REVERSED" => "Phân bổ này đã được đảo trước đó.",
        "ALLOCATED_DOCUMENT_REVERSED" => "Một chứng từ liên quan đã được đảo; không thể tiếp tục thao tác này.",
        "PAYMENT_ALLOCATION_EXISTS" => "Phiếu thanh toán còn phân bổ đang hiệu lực. Hãy đảo các phân bổ trước khi đảo phiếu.",
        "PAYMENT_REVERSAL_REASON_REQUIRED" => "Phải nhập lý do đảo phiếu thanh toán.",
        "ALLOCATION_REVERSAL_REASON_REQUIRED" => "Phải nhập lý do đảo phân bổ.",
        "SUPPLIER_PAYMENT_CONFLICT" => "Phiếu thanh toán đã thay đổi. Hãy tải lại dữ liệu rồi thực hiện lại.",
        _ => CanonicalErrorMessages.ToOfficeMessage(exception)
    };

    private static bool ValidDate(string? value) =>
        DateOnly.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string DecimalText(decimal value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);

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
