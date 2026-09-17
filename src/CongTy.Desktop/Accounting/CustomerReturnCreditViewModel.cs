using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed class CustomerReturnCreditViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.customer-return-credit.read";
    private const string AllocatePermission = "core.customer-return-credit.allocate";
    private const string ReversePermission = "core.customer-return-credit.reverse";
    private const string RefundCreatePermission = "core.customer-refund.create";
    private const string RefundReversePermission = "core.customer-refund.reverse";

    private readonly ICustomerReturnCreditService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);
    private CustomerReturnCreditData[] _credits = [];
    private ReceivableAllocationTargetData[] _targets = [];
    private CustomerReturnCreditData? _selected;
    private CustomerReturnCreditRow? _selectedRow;
    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private string _search = string.Empty;
    private string _allocationDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _refundDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _refundAmount = string.Empty;
    private string _refundMethod = "BANK_TRANSFER";
    private string _destinationReference = string.Empty;
    private string _externalReference = string.Empty;
    private string _refundReason = string.Empty;
    private string _reversalReason = string.Empty;

    public CustomerReturnCreditViewModel(ICustomerReturnCreditService service, IAccessStateService access, ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<CustomerReturnCreditRow> Credits { get; } = [];
    public ObservableCollection<CustomerReturnCreditLineRow> Lines { get; } = [];
    public ObservableCollection<CustomerReturnCreditTargetRow> Targets { get; } = [];
    public ObservableCollection<CustomerReturnCreditAllocationRow> Allocations { get; } = [];
    public ObservableCollection<CustomerRefundRow> Refunds { get; } = [];
    public IReadOnlyList<Choice> RefundMethods { get; } = [new("BANK_TRANSFER", "Chuyển khoản"), new("CASH", "Tiền mặt")];

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanAllocate => _access.HasPermission(AllocatePermission);
    public bool CanReverse => _access.HasPermission(ReversePermission);
    public bool CanCreateRefund => _access.HasPermission(RefundCreatePermission);
    public bool CanReverseRefund => _access.HasPermission(RefundReversePermission);
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) RaiseActions(); } }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasSelected => _selected is not null;
    public bool ShowEmpty => _loaded && !IsBusy && Credits.Count == 0;
    public bool CanUseAllocation => CanAllocate && !IsBusy && _selected is not null && _selected.Status != "reversed" && Amount(_selected.RemainingAmount) > 0m;
    public bool CanUseRefund => CanCreateRefund && !IsBusy && _selected is not null && _selected.Status != "reversed" && Amount(_selected.RemainingAmount) > 0m;
    public bool CanUseReverseCredit => CanReverse && !IsBusy && _selected is not null && _selected.Status != "reversed" && !_selected.Refunds.Any(item => item.ReversalId is null);

    public string Message { get => _message; private set { if (SetField(ref _message, value ?? string.Empty)) OnPropertyChanged(nameof(HasMessage)); } }
    public string Search { get => _search; set { if (SetField(ref _search, value ?? string.Empty)) RebuildCredits(); } }
    public CustomerReturnCreditRow? SelectedRow { get => _selectedRow; set => SetField(ref _selectedRow, value); }
    public string AllocationDate { get => _allocationDate; set => SetField(ref _allocationDate, value ?? string.Empty); }
    public string RefundDate { get => _refundDate; set => SetField(ref _refundDate, value ?? string.Empty); }
    public string RefundAmount { get => _refundAmount; set => SetField(ref _refundAmount, value ?? string.Empty); }
    public string RefundMethod { get => _refundMethod; set => SetField(ref _refundMethod, value ?? "BANK_TRANSFER"); }
    public string DestinationReference { get => _destinationReference; set => SetField(ref _destinationReference, value ?? string.Empty); }
    public string ExternalReference { get => _externalReference; set => SetField(ref _externalReference, value ?? string.Empty); }
    public string RefundReason { get => _refundReason; set => SetField(ref _refundReason, value ?? string.Empty); }
    public string ReversalReason { get => _reversalReason; set => SetField(ref _reversalReason, value ?? string.Empty); }

    public string DetailEmptyText => HasSelected ? string.Empty : "Chọn một khoản giảm công nợ để xem chi tiết.";
    public string SelectedDocument => _selected?.DocumentNumber ?? "—";
    public string SelectedCustomer => _selected is null ? "—" : CustomerReturnCreditPresentation.Party(_selected.CustomerCode, _selected.CustomerName);
    public string SelectedSourceReturn => _selected?.SourceReturnNumber ?? "—";
    public string SelectedSalesOrder => _selected?.SourceSalesOrderNumber ?? "—";
    public string SelectedWarehouse => _selected is null ? "—" : CustomerReturnCreditPresentation.Party(_selected.WarehouseCode, _selected.WarehouseName);
    public string SelectedGross => _selected is null ? "—" : CustomerReturnCreditPresentation.Money(_selected.GrossAmount, _selected.CurrencyCode);
    public string SelectedRemaining => _selected is null ? "—" : CustomerReturnCreditPresentation.Money(_selected.RemainingAmount, _selected.CurrencyCode);
    public string SelectedStatus => _selected is null ? "—" : CustomerReturnCreditPresentation.Status(_selected.Status);
    public string TargetEmptyText => Targets.Count == 0 ? "Không còn khoản nợ phù hợp để phân bổ." : string.Empty;
    public string AllocationEmptyText => Allocations.Count == 0 ? "Chưa có lịch sử phân bổ." : string.Empty;
    public string RefundEmptyText => Refunds.Count == 0 ? "Chưa có khoản hoàn tiền." : string.Empty;

    public Task<bool> EnsureLoadedAsync() => _loaded ? Task.FromResult(true) : RefreshAsync();

    public async Task<bool> RefreshAsync(string? selectId = null)
    {
        if (IsBusy) return false;
        if (!CanRead) { Message = "Tài khoản chưa được cấp quyền xem Điều chỉnh công nợ hàng trả."; return false; }
        IsBusy = true; Message = string.Empty;
        try
        {
            var creditsTask = _service.ListCreditsAsync();
            var targetsTask = _service.ListTargetsAsync();
            await Task.WhenAll(creditsTask, targetsTask).ConfigureAwait(true);
            _credits = (await creditsTask.ConfigureAwait(true)).ToArray();
            _targets = (await targetsTask.ConfigureAwait(true)).ToArray();
            _loaded = true;
            RebuildCredits();
            var desired = selectId ?? SelectedRow?.Id ?? _credits.FirstOrDefault()?.Id;
            if (!string.IsNullOrWhiteSpace(desired)) await OpenAsync(desired).ConfigureAwait(true); else ClearDetail();
            Message = "Dữ liệu điều chỉnh công nợ hàng trả đã được cập nhật.";
            OnPropertyChanged(nameof(ShowEmpty));
            return true;
        }
        catch (CanonicalApiException ex) { Message = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(ex), ex.RequestId); return false; }
        catch { Message = "Không tải được khoản giảm công nợ từ hàng khách trả. Hãy cập nhật lại."; return false; }
        finally { IsBusy = false; }
    }

    public async Task OpenAsync(string id)
    {
        if (!CanRead || string.IsNullOrWhiteSpace(id)) return;
        try
        {
            _selected = await _service.GetCreditAsync(id).ConfigureAwait(true);
            SelectedRow = Credits.FirstOrDefault(item => item.Id == id);
            RebuildDetail();
        }
        catch (CanonicalApiException ex) { Message = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(ex), ex.RequestId); }
        catch { Message = "Không tải được chi tiết khoản giảm công nợ đã chọn."; }
    }

    public async Task AllocateAsync()
    {
        if (!CanUseAllocation || _selected is null) return;
        if (!ValidDate(AllocationDate)) { Message = "Ngày phân bổ phải theo định dạng yyyy-MM-dd."; return; }
        var remaining = Amount(_selected.RemainingAmount);
        var drafts = Targets.Select(item => new { Item = item, Value = Amount(item.AmountInput) }).Where(x => x.Value > 0m).ToArray();
        if (drafts.Length == 0) { Message = "Nhập số tiền cần phân bổ cho ít nhất một khoản nợ."; return; }
        if (drafts.Any(x => x.Value > Amount(_targets.First(t => t.Id == x.Item.Id).RemainingAmount)) || drafts.Sum(x => x.Value) > remaining)
        { Message = "Số tiền phân bổ vượt quá số còn phải thu hoặc phần giảm công nợ còn lại."; return; }
        var allocations = drafts.Select(x => new CustomerReturnCreditAllocationDraft(x.Item.Id, DecimalText(x.Value))).ToArray();
        var request = new CustomerReturnCreditAllocateRequest(AllocationDate.Trim(), allocations);
        var slot = $"allocate|{_selected.Id}|{request.AllocationDate}|{string.Join(';', allocations.Select(x => $"{x.ReceivableDocumentId}={x.Amount}"))}";
        var key = MutationKey(slot, "customer-return-credit-allocate");
        IsBusy = true;
        try
        {
            var updated = await _service.AllocateAsync(_selected.Id, request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot); IsBusy = false;
            await RefreshAsync(updated.Id).ConfigureAwait(true);
            Message = "Đã phân bổ khoản giảm công nợ vào các khoản phải thu đã chọn.";
        }
        catch (CanonicalApiException ex) { Message = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(ex), ex.RequestId); }
        catch { Message = "Không phân bổ được khoản giảm công nợ."; }
        finally { IsBusy = false; }
    }

    public async Task CreateRefundAsync()
    {
        if (!CanUseRefund || _selected is null) return;
        var amount = Amount(RefundAmount);
        if (amount <= 0m || amount > Amount(_selected.RemainingAmount)) { Message = "Số tiền hoàn phải lớn hơn 0 và không vượt phần chưa sử dụng."; return; }
        if (!ValidDate(RefundDate) || string.IsNullOrWhiteSpace(DestinationReference) || string.IsNullOrWhiteSpace(RefundReason))
        { Message = "Nhập đủ ngày hoàn, nơi nhận và lý do hoàn tiền."; return; }
        var request = new CustomerRefundCreateRequest(_selected.Id, DecimalText(amount), RefundMethod, DestinationReference.Trim(), NullIfBlank(ExternalReference), RefundReason.Trim(), RefundDate.Trim());
        var slot = $"refund|{request.SourceCreditDocumentId}|{request.Amount}|{request.RefundMethod}|{request.DestinationReference}|{request.ExternalReference}|{request.Reason}|{request.RefundDate}";
        var key = MutationKey(slot, "customer-refund-create");
        IsBusy = true;
        try
        {
            await _service.CreateRefundAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot); RefundAmount = DestinationReference = ExternalReference = RefundReason = string.Empty; var id = _selected.Id; IsBusy = false;
            await RefreshAsync(id).ConfigureAwait(true); Message = "Đã ghi nhận hoàn tiền từ phần chưa sử dụng.";
        }
        catch (CanonicalApiException ex) { Message = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(ex), ex.RequestId); }
        catch { Message = "Không ghi nhận được khoản hoàn tiền."; }
        finally { IsBusy = false; }
    }

    public async Task ReverseRefundAsync(CustomerRefundRow row)
    {
        if (!CanReverseRefund || IsBusy || !row.CanReverse || _selected is null) return;
        var reason = ReversalReason.Trim();
        if (reason.Length == 0) { Message = "Cần nhập lý do trước khi đảo hoàn tiền."; return; }
        var request = new CustomerReturnCreditReverseRequest(reason);
        var slot = $"reverse-refund|{row.Id}|{reason}"; var key = MutationKey(slot, "customer-refund-reverse");
        IsBusy = true;
        try
        {
            await _service.ReverseRefundAsync(row.Id, request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot); var id = _selected.Id; IsBusy = false; await RefreshAsync(id).ConfigureAwait(true); Message = "Đã đảo khoản hoàn tiền.";
        }
        catch (CanonicalApiException ex) { Message = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(ex), ex.RequestId); }
        catch { Message = "Không đảo được khoản hoàn tiền."; }
        finally { IsBusy = false; }
    }

    public async Task ReverseCreditAsync()
    {
        if (!CanUseReverseCredit || _selected is null) return;
        var reason = ReversalReason.Trim();
        if (reason.Length == 0) { Message = "Cần nhập lý do trước khi đảo khoản giảm công nợ hàng trả."; return; }
        var request = new CustomerReturnCreditReverseRequest(reason);
        var slot = $"reverse-credit|{_selected.Id}|{reason}"; var key = MutationKey(slot, "customer-return-credit-reverse");
        IsBusy = true;
        try
        {
            var updated = await _service.ReverseCreditAsync(_selected.Id, request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot); IsBusy = false; await RefreshAsync(updated.Id).ConfigureAwait(true); Message = "Đã đảo khoản giảm công nợ hàng trả.";
        }
        catch (CanonicalApiException ex) { Message = CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(ex), ex.RequestId); }
        catch { Message = "Không đảo được khoản giảm công nợ hàng trả."; }
        finally { IsBusy = false; }
    }

    private void RebuildCredits()
    {
        Credits.Clear(); int i = 0;
        foreach (var item in _credits.Where(Matches)) Credits.Add(new CustomerReturnCreditRow(++i, item.Id, item.DocumentNumber, item.SourceReturnNumber ?? "—", CustomerReturnCreditPresentation.Party(item.CustomerCode, item.CustomerName), CustomerReturnCreditPresentation.Party(item.WarehouseCode, item.WarehouseName), CustomerReturnCreditPresentation.Date(item.PostingDate), CustomerReturnCreditPresentation.Money(item.GrossAmount, item.CurrencyCode), CustomerReturnCreditPresentation.Money(item.AllocatedAmount, item.CurrencyCode), CustomerReturnCreditPresentation.Money(item.RefundedAmount, item.CurrencyCode), CustomerReturnCreditPresentation.Money(item.RemainingAmount, item.CurrencyCode), CustomerReturnCreditPresentation.Status(item.Status)));
        OnPropertyChanged(nameof(ShowEmpty));
    }

    private bool Matches(CustomerReturnCreditData item)
    {
        if (string.IsNullOrWhiteSpace(Search)) return true;
        var q = Search.Trim();
        return new[] { item.DocumentNumber, item.SourceReturnNumber, item.CustomerCode, item.CustomerName, item.SourceSalesOrderNumber }.Any(v => !string.IsNullOrWhiteSpace(v) && v.Contains(q, StringComparison.CurrentCultureIgnoreCase));
    }

    private void RebuildDetail()
    {
        Lines.Clear(); Allocations.Clear(); Refunds.Clear(); Targets.Clear();
        if (_selected is null) { RaiseDetail(); return; }
        foreach (var line in _selected.Lines) Lines.Add(new CustomerReturnCreditLineRow(line.SkuCode, line.ProductName, CustomerReturnCreditPresentation.Quantity(line.QuantityAccepted), CustomerReturnCreditPresentation.Money(line.CreditAmount, _selected.CurrencyCode)));
        foreach (var allocation in _selected.Allocations) Allocations.Add(new CustomerReturnCreditAllocationRow(allocation.TargetDocumentNumber ?? allocation.TargetReceivableDocumentId, CustomerReturnCreditPresentation.Date(allocation.AllocationDate), CustomerReturnCreditPresentation.Money(allocation.Amount, _selected.CurrencyCode), allocation.Reversed ? "Đã đảo" : "Đã phân bổ"));
        foreach (var refund in _selected.Refunds) Refunds.Add(new CustomerRefundRow(refund.Id, refund.RefundNumber, $"{CustomerReturnCreditPresentation.RefundMethod(refund.RefundMethod)} — {refund.DestinationReference}", CustomerReturnCreditPresentation.Date(refund.RefundDate), CustomerReturnCreditPresentation.Money(refund.Amount, refund.CurrencyCode), refund.ReversalId is null ? "Đã hoàn" : "Đã đảo", refund.ReversalId is null));
        foreach (var target in _targets.Where(t => t.CustomerId == _selected.CustomerId && string.Equals(t.CurrencyCode, _selected.CurrencyCode, StringComparison.OrdinalIgnoreCase) && Amount(t.RemainingAmount) > 0m))
            Targets.Add(new CustomerReturnCreditTargetRow { Id = target.Id, CustomerId = target.CustomerId, CurrencyCode = target.CurrencyCode, Reference = string.Join(" / ", new[] { target.SalesOrderNumber, target.DeliveryOrderNumber, target.DocumentNumber }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()), Warehouse = CustomerReturnCreditPresentation.Party(target.WarehouseCode, target.WarehouseName), RemainingAmount = CustomerReturnCreditPresentation.Money(target.RemainingAmount, target.CurrencyCode) });
        RaiseDetail();
    }

    private void ClearDetail() { _selected = null; SelectedRow = null; Lines.Clear(); Targets.Clear(); Allocations.Clear(); Refunds.Clear(); RaiseDetail(); }
    private void RaiseDetail()
    {
        foreach (var name in new[] { nameof(HasSelected), nameof(DetailEmptyText), nameof(SelectedDocument), nameof(SelectedCustomer), nameof(SelectedSourceReturn), nameof(SelectedSalesOrder), nameof(SelectedWarehouse), nameof(SelectedGross), nameof(SelectedRemaining), nameof(SelectedStatus), nameof(TargetEmptyText), nameof(AllocationEmptyText), nameof(RefundEmptyText) }) OnPropertyChanged(name);
        RaiseActions();
    }
    private void RaiseActions() { foreach (var name in new[] { nameof(CanUseAllocation), nameof(CanUseRefund), nameof(CanUseReverseCredit) }) OnPropertyChanged(name); }
    private string MutationKey(string slot, string scope) { if (_mutationKeys.TryGetValue(slot, out var key)) return key; key = _idempotencyKeys.Create(scope); _mutationKeys[slot] = key; return key; }
    private static decimal Amount(string? value) => CustomerReturnCreditPresentation.Amount(value);
    private static string DecimalText(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ValidDate(string value) => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    private void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public sealed record Choice(string Id, string Display);
}
