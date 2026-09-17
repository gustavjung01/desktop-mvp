using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed class CustomerPaymentViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.customer-payment.read";
    private const string CreatePermission = "core.customer-payment.create";
    private const string ReversePermission = "core.customer-payment.reverse";
    private const string ReceivableReadPermission = "core.receivable.read";
    private const string AllocatePermission = "core.receivable-allocation.create";
    private const string ReverseAllocationPermission = "core.receivable-allocation.reverse";

    private readonly ICustomerPaymentService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _createAmounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _existingAmounts = new(StringComparer.Ordinal);

    private CustomerPaymentData[] _payments = [];
    private ReceivableAllocationTargetData[] _targets = [];
    private CustomerData[] _customers = [];
    private WarehouseData[] _warehouses = [];
    private CustomerPaymentData? _selectedPayment;
    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _customerSearch = string.Empty;
    private string _orderSearch = string.Empty;
    private string _existingOrderSearch = string.Empty;
    private CustomerPaymentChoice? _selectedCustomer;
    private CustomerPaymentChoice? _selectedWarehouse;
    private CustomerPaymentChoice? _selectedEmployee;
    private CustomerPaymentRow? _selectedPaymentRow;
    private string _paymentDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _paymentMethod = "BANK_TRANSFER";
    private string _amount = string.Empty;
    private string _externalReference = string.Empty;
    private string _note = string.Empty;
    private string _allocationDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _reversalReason = string.Empty;

    public CustomerPaymentViewModel(
        ICustomerPaymentService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            _selectedPayment = null;
            Payments.Clear();
            Customers.Clear();
            Warehouses.Clear();
            Employees.Clear();
            CreateTargets.Clear();
            ExistingTargets.Clear();
            AllocationHistory.Clear();
            RaisePermissions();
            RaiseDetail();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CustomerPaymentChoice> Customers { get; } = [];
    public ObservableCollection<CustomerPaymentChoice> Warehouses { get; } = [];
    public ObservableCollection<CustomerPaymentChoice> Employees { get; } = [];
    public ObservableCollection<CustomerPaymentRow> Payments { get; } = [];
    public ObservableCollection<CustomerPaymentTargetRow> CreateTargets { get; } = [];
    public ObservableCollection<CustomerPaymentTargetRow> ExistingTargets { get; } = [];
    public ObservableCollection<CustomerPaymentAllocationRow> AllocationHistory { get; } = [];

    public IReadOnlyList<CustomerPaymentChoice> PaymentMethods { get; } =
    [
        new("BANK_TRANSFER", "Chuyển khoản"),
        new("CASH", "Tiền mặt"),
        new("OTHER", "Khác")
    ];

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanCreate => _access.HasPermission(CreatePermission);
    public bool CanReadTargets => _access.HasPermission(ReceivableReadPermission);
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
        }
    }

    public bool CanUseCreateForm => CanCreate && !IsBusy;
    public bool CanUseAllocationForm => CanAllocate && !IsBusy && _selectedPayment is not null
        && _selectedPayment.Status != "reversed"
        && CustomerPaymentPresentation.Amount(_selectedPayment.RemainingAmount) > 0m;
    public bool CanUseReversePayment => CanReversePayment && !IsBusy && _selectedPayment is not null
        && _selectedPayment.Status != "reversed"
        && !_selectedPayment.Allocations.Any(item => !item.Reversed)
        && CustomerPaymentPresentation.Amount(_selectedPayment.AllocatedAmount) <= 0m;
    public bool HasSelectedPayment => _selectedPayment is not null;
    public string DetailEmptyText => HasSelectedPayment ? string.Empty : "Chọn một phiếu thu để xem chi tiết.";
    public string AllocationEmptyText => AllocationHistory.Count == 0 ? "Chưa ghi tiền vào đơn nào." : string.Empty;
    public bool HasPayments => Payments.Count > 0;
    public bool ShowEmptyPayments => _loaded && !IsBusy && !HasPayments;
    public string CustomerMatchText => $"{Customers.Count}/{_customers.Length} khách hàng phù hợp";
    public string CreateTargetCountText => $"{CreateTargets.Count}/{_targets.Count(item => SelectedCustomer is not null && item.CustomerId == SelectedCustomer.Id && string.Equals(item.CurrencyCode, "VND", StringComparison.OrdinalIgnoreCase) && CustomerPaymentPresentation.Amount(item.RemainingAmount) > 0m)} đơn";
    public string CreateTargetEmptyText => CreateTargets.Count > 0
        ? string.Empty
        : string.IsNullOrWhiteSpace(OrderSearch) ? "Khách hàng chưa có đơn còn phải thu." : "Không tìm thấy đơn phù hợp.";
    public string ExistingTargetEmptyText => ExistingTargets.Count > 0
        ? string.Empty
        : string.IsNullOrWhiteSpace(ExistingOrderSearch) ? "Không còn đơn phù hợp để ghi nhận tiền." : "Không tìm thấy đơn phù hợp.";

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
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public string CustomerSearch
    {
        get => _customerSearch;
        set
        {
            if (!SetField(ref _customerSearch, value ?? string.Empty)) return;
            ApplyCustomerFilter();
        }
    }

    public string OrderSearch
    {
        get => _orderSearch;
        set
        {
            if (!SetField(ref _orderSearch, value ?? string.Empty)) return;
            ApplyCreateTargets();
        }
    }

    public string ExistingOrderSearch
    {
        get => _existingOrderSearch;
        set
        {
            if (!SetField(ref _existingOrderSearch, value ?? string.Empty)) return;
            ApplyExistingTargets();
        }
    }

    public CustomerPaymentChoice? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (!SetField(ref _selectedCustomer, value)) return;
            _createAmounts.Clear();
            _orderSearch = string.Empty;
            OnPropertyChanged(nameof(OrderSearch));
            ApplyCreateTargets();
        }
    }

    public CustomerPaymentChoice? SelectedWarehouse
    {
        get => _selectedWarehouse;
        set => SetField(ref _selectedWarehouse, value);
    }

    public CustomerPaymentChoice? SelectedEmployee
    {
        get => _selectedEmployee;
        set => SetField(ref _selectedEmployee, value);
    }

    public CustomerPaymentRow? SelectedPaymentRow
    {
        get => _selectedPaymentRow;
        set => SetField(ref _selectedPaymentRow, value);
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
        set
        {
            if (!SetField(ref _amount, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CreateAllocationSummary));
        }
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

    public string ReversalReason
    {
        get => _reversalReason;
        set => SetField(ref _reversalReason, value ?? string.Empty);
    }

    public string CreateAllocationSummary =>
        $"Tổng tiền ghi cho đơn: {CustomerPaymentPresentation.Money(CreateAllocationTotal().ToString(CultureInfo.InvariantCulture), "VND")}. Phần còn lại là tiền chưa gắn với đơn.";

    public string ExistingAllocationSummary =>
        $"Tổng tiền ghi thêm: {CustomerPaymentPresentation.Money(ExistingAllocationTotal().ToString(CultureInfo.InvariantCulture), SelectedCurrency)}";

    public string SelectedDocumentNumber => _selectedPayment?.DocumentNumber ?? "—";
    public string SelectedCustomerText => _selectedPayment is null
        ? "—"
        : CustomerPaymentPresentation.Party(_selectedPayment.CustomerCode, _selectedPayment.CustomerName);
    public string SelectedEmployeeText => _selectedPayment is null || string.IsNullOrWhiteSpace(_selectedPayment.RemittingEmployeeName)
        ? "Không ghi nhận"
        : CustomerPaymentPresentation.Party(_selectedPayment.RemittingEmployeeCode, _selectedPayment.RemittingEmployeeName);
    public string SelectedRemainingText => _selectedPayment is null
        ? "—"
        : CustomerPaymentPresentation.Money(_selectedPayment.RemainingAmount, _selectedPayment.CurrencyCode);
    public string SelectedOriginalText => _selectedPayment is null
        ? "—"
        : CustomerPaymentPresentation.Money(_selectedPayment.OriginalAmount, _selectedPayment.CurrencyCode);
    public string SelectedAllocatedText => _selectedPayment is null
        ? "—"
        : CustomerPaymentPresentation.Money(_selectedPayment.AllocatedAmount, _selectedPayment.CurrencyCode);
    public string SelectedStatusText => _selectedPayment is null
        ? "—"
        : CustomerPaymentPresentation.Status(_selectedPayment.Status);
    public string SelectedPaymentMethodText => _selectedPayment is null
        ? "—"
        : CustomerPaymentPresentation.PaymentMethod(_selectedPayment.PaymentMethod);
    public string SelectedDateText => _selectedPayment is null
        ? "—"
        : CustomerPaymentPresentation.Date(_selectedPayment.PaymentDate);
    public string SelectedWarehouseText => _selectedPayment is null
        ? "—"
        : CustomerPaymentPresentation.Party(_selectedPayment.WarehouseCode, _selectedPayment.WarehouseName);
    public string SelectedCurrency => _selectedPayment?.CurrencyCode ?? "VND";
    public CustomerPaymentData? SelectedPayment => _selectedPayment;

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        return await RefreshAsync().ConfigureAwait(true);
    }

    public async Task<bool> RefreshAsync(string? selectId = null)
    {
        if (IsBusy) return false;
        if (!CanRead)
        {
            SetMessage("Tài khoản chưa được cấp quyền xem Thu tiền khách hàng.", true);
            return false;
        }

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var paymentsTask = _service.ListPaymentsAsync();
            var customersTask = _service.ListCustomersAsync();
            var warehousesTask = _service.ListWarehousesAsync();
            var targetsTask = CanReadTargets
                ? _service.ListTargetsAsync()
                : Task.FromResult<IReadOnlyList<ReceivableAllocationTargetData>>([]);
            var employeesTask = CanCreate
                ? _service.ListRemittingEmployeesAsync()
                : Task.FromResult<IReadOnlyList<RemittingEmployeeOptionData>>([]);

            await Task.WhenAll(paymentsTask, customersTask, warehousesTask, targetsTask, employeesTask).ConfigureAwait(true);

            _payments = (await paymentsTask.ConfigureAwait(true)).ToArray();
            _customers = (await customersTask.ConfigureAwait(true)).Where(item => item.IsActive).ToArray();
            _warehouses = (await warehousesTask.ConfigureAwait(true)).Where(item => item.IsActive).ToArray();
            _targets = (await targetsTask.ConfigureAwait(true)).ToArray();
            var employees = (await employeesTask.ConfigureAwait(true)).ToArray();

            RebuildPayments();
            RebuildChoices(employees);
            ApplyCustomerFilter();
            ApplyCreateTargets();

            var desired = selectId ?? SelectedPaymentRow?.Id ?? _payments.FirstOrDefault()?.Id;
            _loaded = true;
            if (!string.IsNullOrWhiteSpace(desired))
                await OpenPaymentAsync(desired).ConfigureAwait(true);
            else
                ClearDetail();

            SetMessage("Dữ liệu thu tiền khách hàng đã được cập nhật.", false);
            OnPropertyChanged(nameof(ShowEmptyPayments));
            return true;
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId), true);
            return false;
        }
        catch (Exception exception)
        {
            SetMessage(string.IsNullOrWhiteSpace(exception.Message)
                ? "Một phần dữ liệu thu tiền khách hàng chưa tải được. Hãy cập nhật lại trước khi thao tác."
                : exception.Message, true);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task OpenPaymentAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || !CanRead) return;
        try
        {
            var detail = await _service.GetPaymentAsync(id).ConfigureAwait(true);
            _selectedPayment = detail;
            SelectedPaymentRow = Payments.FirstOrDefault(item => item.Id == detail.Id);
            _existingAmounts.Clear();
            ExistingOrderSearch = string.Empty;
            ApplyExistingTargets();
            RebuildAllocationHistory();
            RaiseDetail();
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId), true);
        }
        catch (Exception)
        {
            SetMessage("Không tải được chi tiết phiếu thu. Hãy chọn lại phiếu.", true);
        }
    }

    public async Task SavePaymentAsync()
    {
        if (!CanUseCreateForm) return;
        if (SelectedCustomer is null || SelectedWarehouse is null)
        {
            SetMessage("Chọn khách hàng và đơn vị nhận tiền trước khi lưu.", true);
            return;
        }
        if (!ValidDate(PaymentDate))
        {
            SetMessage("Ngày thu phải theo định dạng yyyy-MM-dd.", true);
            return;
        }

        var amount = CustomerPaymentPresentation.Amount(Amount);
        if (amount <= 0m)
        {
            SetMessage("Kiểm tra số tiền thu và tổng tiền ghi cho đơn trước khi lưu.", true);
            return;
        }

        var allocations = BuildAllocations(_createAmounts, SelectedCustomer.Id, amount, out var allocationError);
        if (allocationError is not null)
        {
            SetMessage(allocationError, true);
            return;
        }

        var request = new CustomerPaymentCreateRequest(
            SelectedCustomer.Id,
            SelectedWarehouse.Id,
            PaymentDate.Trim(),
            "VND",
            PaymentMethod,
            DecimalText(amount),
            SelectedEmployee?.Id,
            NullIfBlank(ExternalReference),
            NullIfBlank(Note),
            allocations.Length == 0 ? null : allocations);

        var slot = string.Join("|",
            "create",
            request.CustomerId,
            request.WarehouseId,
            request.PaymentDate,
            request.PaymentMethod,
            request.Amount,
            request.RemittingEmployeeId,
            request.ExternalReference,
            request.Note,
            string.Join(";", allocations.Select(item => $"{item.ReceivableDocumentId}={item.Amount}")));
        var key = MutationKey(slot, "customer-payment-create");

        IsBusy = true;
        try
        {
            var created = await _service.CreatePaymentAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            _createAmounts.Clear();
            Amount = string.Empty;
            ExternalReference = string.Empty;
            Note = string.Empty;
            IsBusy = false;
            await RefreshAsync(created.Id).ConfigureAwait(true);
            SetMessage($"Đã ghi nhận phiếu thu {created.DocumentNumber}.", false);
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId), true);
        }
        catch (Exception exception)
        {
            SetMessage(string.IsNullOrWhiteSpace(exception.Message)
                ? "Không ghi nhận được tiền khách trả."
                : exception.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task AllocateSelectedAsync()
    {
        if (!CanUseAllocationForm || _selectedPayment is null) return;
        if (!ValidDate(AllocationDate))
        {
            SetMessage("Ngày ghi nhận phải theo định dạng yyyy-MM-dd.", true);
            return;
        }

        var remaining = CustomerPaymentPresentation.Amount(_selectedPayment.RemainingAmount);
        var allocations = BuildAllocations(_existingAmounts, _selectedPayment.CustomerId, remaining, out var allocationError);
        if (allocationError is not null || allocations.Length == 0)
        {
            SetMessage(allocationError ?? "Chọn ít nhất một đơn còn nợ và kiểm tra tổng tiền đã nhập.", true);
            return;
        }

        var request = new CustomerPaymentAllocateRequest(AllocationDate.Trim(), allocations);
        var slot = string.Join("|",
            "allocate",
            _selectedPayment.Id,
            request.AllocationDate,
            string.Join(";", allocations.Select(item => $"{item.ReceivableDocumentId}={item.Amount}")));
        var key = MutationKey(slot, "customer-payment-allocation");

        IsBusy = true;
        try
        {
            var updated = await _service.AllocateAsync(_selectedPayment.Id, request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            _existingAmounts.Clear();
            IsBusy = false;
            await RefreshAsync(updated.Id).ConfigureAwait(true);
            SetMessage($"Đã ghi {CustomerPaymentPresentation.Money(allocations.Sum(item => CustomerPaymentPresentation.Amount(item.Amount)).ToString(CultureInfo.InvariantCulture), updated.CurrencyCode)} vào {allocations.Length} khoản nợ.", false);
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId), true);
        }
        catch (Exception)
        {
            SetMessage("Không ghi được tiền thu vào đơn.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ReverseAllocationAsync(CustomerPaymentAllocationRow? row)
    {
        if (row is null || !row.CanReverse || !CanReverseAllocation || IsBusy) return;
        var reason = ReversalReason.Trim();
        if (reason.Length == 0)
        {
            SetMessage("Nhập lý do hủy trước khi thực hiện.", true);
            return;
        }

        var request = new CustomerPaymentReverseRequest(reason);
        var slot = $"reverse-allocation|{row.Id}|{reason}";
        var key = MutationKey(slot, "receivable-allocation-reverse");

        IsBusy = true;
        try
        {
            await _service.ReverseAllocationAsync(row.Id, request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            var selectedId = _selectedPayment?.Id;
            IsBusy = false;
            await RefreshAsync(selectedId).ConfigureAwait(true);
            SetMessage("Đã hủy phần tiền ghi vào đơn.", false);
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId), true);
        }
        catch (Exception)
        {
            SetMessage("Không hủy được phần tiền đã ghi vào đơn.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ReversePaymentAsync()
    {
        if (!CanUseReversePayment || _selectedPayment is null) return;
        var reason = ReversalReason.Trim();
        if (reason.Length == 0)
        {
            SetMessage("Chọn phiếu và nhập lý do hủy trước khi thực hiện.", true);
            return;
        }

        var request = new CustomerPaymentReverseRequest(reason);
        var selectedId = _selectedPayment.Id;
        var slot = $"reverse-payment|{selectedId}|{reason}";
        var key = MutationKey(slot, "customer-payment-reverse");

        IsBusy = true;
        try
        {
            var reversed = await _service.ReversePaymentAsync(selectedId, request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            IsBusy = false;
            await RefreshAsync(reversed.Id).ConfigureAwait(true);
            SetMessage($"Đã hủy phiếu thu {reversed.DocumentNumber}.", false);
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId), true);
        }
        catch (Exception)
        {
            SetMessage("Không hủy được phiếu thu.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RebuildChoices(RemittingEmployeeOptionData[] employees)
    {
        Customers.Clear();
        foreach (var customer in _customers)
            Customers.Add(new CustomerPaymentChoice(customer.Id, CustomerPaymentPresentation.Party(customer.Code, customer.Name)));
        if (SelectedCustomer is null || Customers.All(item => item.Id != SelectedCustomer.Id))
            SelectedCustomer = Customers.FirstOrDefault();

        Warehouses.Clear();
        foreach (var warehouse in _warehouses)
            Warehouses.Add(new CustomerPaymentChoice(warehouse.Id, CustomerPaymentPresentation.Party(warehouse.Code, warehouse.Name)));
        if (SelectedWarehouse is null || Warehouses.All(item => item.Id != SelectedWarehouse.Id))
            SelectedWarehouse = Warehouses.FirstOrDefault();

        Employees.Clear();
        Employees.Add(new CustomerPaymentChoice(string.Empty, "Không chọn nhân viên"));
        foreach (var employee in employees)
            Employees.Add(new CustomerPaymentChoice(employee.Id, CustomerPaymentPresentation.Party(employee.Code, employee.FullName)));
        if (SelectedEmployee is null || Employees.All(item => item.Id != SelectedEmployee.Id))
            SelectedEmployee = Employees.FirstOrDefault();
    }

    private void ApplyCustomerFilter()
    {
        if (_customers.Length == 0) return;
        var selectedId = SelectedCustomer?.Id;
        var filtered = _customers
            .Where(item => Includes(CustomerSearch, item.Code, item.Name))
            .Select(item => new CustomerPaymentChoice(item.Id, CustomerPaymentPresentation.Party(item.Code, item.Name)))
            .ToList();
        var selected = _customers.FirstOrDefault(item => item.Id == selectedId);
        if (selected is not null && filtered.All(item => item.Id != selected.Id))
            filtered.Insert(0, new CustomerPaymentChoice(selected.Id, CustomerPaymentPresentation.Party(selected.Code, selected.Name)));

        Customers.Clear();
        foreach (var item in filtered) Customers.Add(item);
        if (selectedId is not null)
            _selectedCustomer = Customers.FirstOrDefault(item => item.Id == selectedId);
        OnPropertyChanged(nameof(SelectedCustomer));
        OnPropertyChanged(nameof(CustomerMatchText));
    }

    private void RebuildPayments()
    {
        Payments.Clear();
        var index = 0;
        foreach (var payment in _payments)
        {
            index++;
            var employee = string.IsNullOrWhiteSpace(payment.RemittingEmployeeName)
                ? "Không ghi nhận"
                : CustomerPaymentPresentation.Party(payment.RemittingEmployeeCode, payment.RemittingEmployeeName);
            var orders = payment.RelatedSalesOrderNumbers.Length == 0
                ? "Chưa gắn với đơn"
                : string.Join(", ", payment.RelatedSalesOrderNumbers);
            var relatedRemaining = payment.RelatedReceivableCount > 0
                ? CustomerPaymentPresentation.Money(payment.RelatedRemainingAmount, payment.CurrencyCode)
                : "—";
            Payments.Add(new CustomerPaymentRow(
                index,
                payment.Id,
                payment.DocumentNumber,
                CustomerPaymentPresentation.Date(payment.PaymentDate),
                employee,
                CustomerPaymentPresentation.Party(payment.CustomerCode, payment.CustomerName),
                orders,
                CustomerPaymentPresentation.Money(payment.OriginalAmount, payment.CurrencyCode),
                relatedRemaining,
                CustomerPaymentPresentation.Status(payment.Status)));
        }
        OnPropertyChanged(nameof(HasPayments));
        OnPropertyChanged(nameof(ShowEmptyPayments));
    }

    private void ApplyCreateTargets()
    {
        PreserveTargetAmounts(CreateTargets, _createAmounts);
        CreateTargets.Clear();
        if (SelectedCustomer is null) return;

        foreach (var target in _targets.Where(item =>
                     item.CustomerId == SelectedCustomer.Id
                     && string.Equals(item.CurrencyCode, "VND", StringComparison.OrdinalIgnoreCase)
                     && CustomerPaymentPresentation.Amount(item.RemainingAmount) > 0m
                     && Includes(OrderSearch, item.SalesOrderNumber, item.DeliveryOrderNumber, item.DocumentNumber, item.WarehouseCode, item.WarehouseName)))
        {
            var row = CreateTarget(target, _createAmounts);
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(CustomerPaymentTargetRow.AmountInput)) return;
                _createAmounts[row.Id] = row.AmountInput;
                OnPropertyChanged(nameof(CreateAllocationSummary));
            };
            CreateTargets.Add(row);
        }
        OnPropertyChanged(nameof(CreateAllocationSummary));
        OnPropertyChanged(nameof(CreateTargetCountText));
        OnPropertyChanged(nameof(CreateTargetEmptyText));
    }

    private void ApplyExistingTargets()
    {
        PreserveTargetAmounts(ExistingTargets, _existingAmounts);
        ExistingTargets.Clear();
        if (_selectedPayment is null) return;

        foreach (var target in _targets.Where(item =>
                     item.CustomerId == _selectedPayment.CustomerId
                     && string.Equals(item.CurrencyCode, _selectedPayment.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                     && CustomerPaymentPresentation.Amount(item.RemainingAmount) > 0m
                     && Includes(ExistingOrderSearch, item.SalesOrderNumber, item.DeliveryOrderNumber, item.DocumentNumber, item.WarehouseCode, item.WarehouseName)))
        {
            var row = CreateTarget(target, _existingAmounts);
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(CustomerPaymentTargetRow.AmountInput)) return;
                _existingAmounts[row.Id] = row.AmountInput;
                OnPropertyChanged(nameof(ExistingAllocationSummary));
            };
            ExistingTargets.Add(row);
        }
        OnPropertyChanged(nameof(ExistingAllocationSummary));
        OnPropertyChanged(nameof(ExistingTargetEmptyText));
    }

    private void RebuildAllocationHistory()
    {
        AllocationHistory.Clear();
        if (_selectedPayment is null) return;
        foreach (var allocation in _selectedPayment.Allocations)
        {
            var warehouse = _warehouses.FirstOrDefault(item => item.Id == allocation.TargetWarehouseId);
            AllocationHistory.Add(new CustomerPaymentAllocationRow(
                allocation.Id,
                allocation.TargetDocumentNumber ?? "—",
                warehouse?.Code ?? allocation.TargetWarehouseId ?? "—",
                CustomerPaymentPresentation.Date(allocation.AllocationDate),
                CustomerPaymentPresentation.Money(allocation.Amount, _selectedPayment.CurrencyCode),
                allocation.Reversed ? "Đã hủy" : "Đã ghi nhận",
                !allocation.Reversed && CanReverseAllocation));
        }
    }

    private CustomerPaymentAllocationDraft[] BuildAllocations(
        IReadOnlyDictionary<string, string> amounts,
        string customerId,
        decimal sourceLimit,
        out string? error)
    {
        error = null;
        var rows = new List<CustomerPaymentAllocationDraft>();
        decimal total = 0m;
        foreach (var pair in amounts.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            var amount = CustomerPaymentPresentation.Amount(pair.Value);
            if (amount <= 0m) continue;
            var target = _targets.FirstOrDefault(item => item.Id == pair.Key && item.CustomerId == customerId);
            if (target is null) continue;
            var remaining = CustomerPaymentPresentation.Amount(target.RemainingAmount);
            if (amount > remaining)
            {
                error = $"Số tiền ghi cho {target.DocumentNumber} vượt số còn phải thu.";
                return [];
            }
            total += amount;
            rows.Add(new CustomerPaymentAllocationDraft(target.Id, DecimalText(amount)));
        }

        if (total > sourceLimit)
        {
            error = "Tổng tiền ghi cho đơn vượt số tiền có thể sử dụng.";
            return [];
        }
        return rows.ToArray();
    }

    private static CustomerPaymentTargetRow CreateTarget(
        ReceivableAllocationTargetData target,
        IReadOnlyDictionary<string, string> amounts)
    {
        var reference = string.IsNullOrWhiteSpace(target.SalesOrderNumber)
            ? target.DocumentNumber
            : target.SalesOrderNumber;
        var row = new CustomerPaymentTargetRow(
            target.Id,
            reference,
            target.DocumentNumber,
            CustomerPaymentPresentation.Date(target.SourceDocumentDate),
            CustomerPaymentPresentation.Party(target.WarehouseCode, target.WarehouseName),
            CustomerPaymentPresentation.Money(target.RemainingAmount, target.CurrencyCode),
            target.CurrencyCode);
        if (amounts.TryGetValue(target.Id, out var input)) row.AmountInput = input;
        return row;
    }

    private static void PreserveTargetAmounts(
        IEnumerable<CustomerPaymentTargetRow> rows,
        IDictionary<string, string> destination)
    {
        foreach (var row in rows) destination[row.Id] = row.AmountInput;
    }

    private decimal CreateAllocationTotal() =>
        _createAmounts.Values.Sum(CustomerPaymentPresentation.Amount);

    private decimal ExistingAllocationTotal() =>
        _existingAmounts.Values.Sum(CustomerPaymentPresentation.Amount);

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
        ExistingTargets.Clear();
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
    }

    private void RaiseDetail()
    {
        OnPropertyChanged(nameof(HasSelectedPayment));
        OnPropertyChanged(nameof(DetailEmptyText));
        OnPropertyChanged(nameof(AllocationEmptyText));
        OnPropertyChanged(nameof(SelectedPayment));
        OnPropertyChanged(nameof(SelectedDocumentNumber));
        OnPropertyChanged(nameof(SelectedCustomerText));
        OnPropertyChanged(nameof(SelectedEmployeeText));
        OnPropertyChanged(nameof(SelectedRemainingText));
        OnPropertyChanged(nameof(SelectedOriginalText));
        OnPropertyChanged(nameof(SelectedAllocatedText));
        OnPropertyChanged(nameof(SelectedStatusText));
        OnPropertyChanged(nameof(SelectedPaymentMethodText));
        OnPropertyChanged(nameof(SelectedDateText));
        OnPropertyChanged(nameof(SelectedWarehouseText));
        OnPropertyChanged(nameof(SelectedCurrency));
        OnPropertyChanged(nameof(ExistingAllocationSummary));
        RaisePermissions();
    }

    private void SetMessage(string text, bool isError)
    {
        Message = text;
        MessageIsError = isError;
    }

    private static bool ValidDate(string value) =>
        DateOnly.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string DecimalText(decimal value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);

    private static bool Includes(string query, params string?[] values)
    {
        var normalized = Normalize(query);
        if (normalized.Length == 0) return true;
        return values.Any(value => Normalize(value).Contains(normalized, StringComparison.Ordinal));
    }

    private static string Normalize(string? value)
    {
        var text = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var chars = text.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
        return new string(chars).Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant().Trim();
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
