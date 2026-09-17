using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
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
    private CustomerReturnCreditData? _selectedCredit;
    private CustomerReturnCreditRow? _selectedCreditRow;
    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _allocationDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _refundAmount = string.Empty;
    private string _refundDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _refundMethod = "BANK_TRANSFER";
    private string _destinationReference = string.Empty;
    private string _externalReference = string.Empty;
    private string _refundReason = string.Empty;
    private string _reversalReason = string.Empty;

    public CustomerReturnCreditViewModel(
        ICustomerReturnCreditService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            _selectedCredit = null;
            Credits.Clear();
            Lines.Clear();
            AllocationTargets.Clear();
            Refunds.Clear();
            RaiseAll();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CustomerReturnCreditRow> Credits { get; } = [];
    public ObservableCollection<CustomerReturnCreditLineRow> Lines { get; } = [];
    public ObservableCollection<CustomerReturnCreditTargetRow> AllocationTargets { get; } = [];
    public ObservableCollection<CustomerReturnCreditRefundRow> Refunds { get; } = [];

    public IReadOnlyList<CustomerPaymentChoice> RefundMethods { get; } =
    [
        new("BANK_TRANSFER", "Chuyển khoản"),
        new("CASH", "Tiền mặt")
    ];

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanAllocate => _access.HasPermission(AllocatePermission);
    public bool CanReverse => _access.HasPermission(ReversePermission);
    public bool CanCreateRefund => _access.HasPermission(RefundCreatePermission);
    public bool CanReverseRefund => _access.HasPermission(RefundReversePermission);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaiseAll();
        }
    }

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
    public bool HasCredits => Credits.Count > 0;
    public bool ShowEmptyCredits => _loaded && !IsBusy && !HasCredits;
    public bool HasSelectedCredit => _selectedCredit is not null;
    public string DetailEmptyText => HasSelectedCredit ? string.Empty : "Chọn một khoản giảm công nợ để xem chi tiết.";

    public CustomerReturnCreditRow? SelectedCreditRow
    {
        get => _selectedCreditRow;
        set => SetField(ref _selectedCreditRow, value);
    }

    public CustomerReturnCreditData? SelectedCredit => _selectedCredit;
    public string SelectedTitle => _selectedCredit is null ? "—" : $"{_selectedCredit.ReturnNumber} · {CustomerReturnCreditPresentation.Party(_selectedCredit.CustomerCode, _selectedCredit.CustomerName)}";
    public string SelectedOriginal => _selectedCredit is null ? "—" : CustomerReturnCreditPresentation.Money(_selectedCredit.OriginalAmount, _selectedCredit.CurrencyCode);
    public string SelectedAllocated => _selectedCredit is null ? "—" : CustomerReturnCreditPresentation.Money(_selectedCredit.AllocatedAmount, _selectedCredit.CurrencyCode);
    public string SelectedRemaining => _selectedCredit is null ? "—" : CustomerReturnCreditPresentation.Money(_selectedCredit.RemainingAmount, _selectedCredit.CurrencyCode);
    public string SelectedWarehouse => _selectedCredit is null ? "—" : CustomerReturnCreditPresentation.Party(_selectedCredit.WarehouseCode, _selectedCredit.WarehouseName);
    public string SelectedStatus => _selectedCredit is null ? "—" : CustomerReturnCreditPresentation.Status(_selectedCredit.Status);

    public string AllocationDate { get => _allocationDate; set => SetField(ref _allocationDate, value ?? string.Empty); }
    public string RefundAmount { get => _refundAmount; set { if (SetField(ref _refundAmount, value ?? string.Empty)) RaiseAll(); } }
    public string RefundDate { get => _refundDate; set => SetField(ref _refundDate, value ?? string.Empty); }
    public string RefundMethod { get => _refundMethod; set => SetField(ref _refundMethod, value ?? "BANK_TRANSFER"); }
    public string DestinationReference { get => _destinationReference; set { if (SetField(ref _destinationReference, value ?? string.Empty)) RaiseAll(); } }
    public string ExternalReference { get => _externalReference; set => SetField(ref _externalReference, value ?? string.Empty); }
    public string RefundReason { get => _refundReason; set { if (SetField(ref _refundReason, value ?? string.Empty)) RaiseAll(); } }
    public string ReversalReason { get => _reversalReason; set { if (SetField(ref _reversalReason, value ?? string.Empty)) RaiseAll(); } }

    public bool HasRemaining => _selectedCredit is not null && _selectedCredit.Status != "reversed" && CustomerReturnCreditPresentation.Amount(_selectedCredit.RemainingAmount) > 0m;
    public bool CanUseAllocationForm => CanAllocate && !IsBusy && HasRemaining;
    public bool CanUseRefundForm => CanCreateRefund && !IsBusy && HasRemaining;
    public bool CanSubmitAllocation => CanUseAllocationForm && AllocationTotal() > 0m;
    public bool CanSubmitRefund => CanUseRefundForm
        && CustomerReturnCreditPresentation.Amount(RefundAmount) > 0m
        && CustomerReturnCreditPresentation.Amount(RefundAmount) <= CustomerReturnCreditPresentation.Amount(_selectedCredit?.RemainingAmount)
        && !string.IsNullOrWhiteSpace(DestinationReference)
        && !string.IsNullOrWhiteSpace(RefundReason)
        && ValidDate(RefundDate);
    public bool CanUseReverseCredit => CanReverse && !IsBusy && _selectedCredit is not null
        && _selectedCredit.Status != "reversed"
        && !_selectedCredit.Refunds.Any(refund => string.IsNullOrWhiteSpace(refund.ReversalId))
        && !string.IsNullOrWhiteSpace(ReversalReason);
    public string AllocationSummary => _selectedCredit is null ? string.Empty : $"Tổng phân bổ: {CustomerReturnCreditPresentation.Money(AllocationTotal().ToString(CultureInfo.InvariantCulture), _selectedCredit.CurrencyCode)}";
    public string TargetEmptyText => AllocationTargets.Count == 0 && HasRemaining ? "Không còn khoản nợ phù hợp để phân bổ." : string.Empty;
    public string RefundEmptyText => Refunds.Count == 0 ? "Chưa có hoàn tiền từ khoản giảm công nợ này." : string.Empty;

    public async Task<bool> EnsureLoadedAsync() => _loaded || await RefreshAsync().ConfigureAwait(true);

    public async Task<bool> RefreshAsync(string? selectId = null)
    {
        if (IsBusy) return false;
        if (!CanRead)
        {
            SetMessage("Tài khoản chưa được cấp quyền xem Điều chỉnh công nợ hàng trả.", true);
            return false;
        }
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var creditsTask = _service.ListCreditsAsync();
            var targetsTask = _service.ListTargetsAsync();
            await Task.WhenAll(creditsTask, targetsTask).ConfigureAwait(true);
            _credits = (await creditsTask.ConfigureAwait(true)).ToArray();
            _targets = (await targetsTask.ConfigureAwait(true)).ToArray();
            RebuildCredits();
            _loaded = true;
            var desired = selectId ?? SelectedCreditRow?.Id ?? _credits.FirstOrDefault()?.Id;
            if (!string.IsNullOrWhiteSpace(desired)) await OpenCreditAsync(desired).ConfigureAwait(true);
            else ClearDetail();
            SetMessage("Dữ liệu điều chỉnh công nợ hàng trả đã được cập nhật.", false);
            RaiseAll();
            return true;
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(exception), exception.RequestId), true);
            return false;
        }
        catch
        {
            SetMessage("Một phần dữ liệu điều chỉnh công nợ hàng trả chưa tải được. Hãy cập nhật lại trước khi thao tác.", true);
            return false;
        }
        finally { IsBusy = false; }
    }

    public async Task OpenCreditAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || !CanRead) return;
        try
        {
            var detail = await _service.GetCreditAsync(id).ConfigureAwait(true);
            _selectedCredit = detail;
            SelectedCreditRow = Credits.FirstOrDefault(item => item.Id == detail.Id);
            RebuildDetail();
            ResetFormsForSelection();
            RaiseAll();
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(exception), exception.RequestId), true);
        }
        catch { SetMessage("Không tải được chi tiết khoản giảm công nợ. Hãy chọn lại chứng từ.", true); }
    }

    public async Task AllocateAsync()
    {
        if (!CanUseAllocationForm || _selectedCredit is null) return;
        if (!ValidDate(AllocationDate)) { SetMessage("Ngày phân bổ phải theo định dạng yyyy-MM-dd.", true); return; }
        var drafts = new List<CustomerPaymentAllocationDraft>();
        var total = 0m;
        foreach (var row in AllocationTargets)
        {
            var amount = CustomerReturnCreditPresentation.Amount(row.AmountInput);
            if (amount <= 0m) continue;
            if (amount > CustomerReturnCreditPresentation.Amount(row.RemainingAmount)) { SetMessage($"Số phân bổ cho {row.DocumentNumber} vượt số còn nợ.", true); return; }
            total += amount;
            drafts.Add(new CustomerPaymentAllocationDraft(row.Id, DecimalText(amount)));
        }
        if (drafts.Count == 0 || total > CustomerReturnCreditPresentation.Amount(_selectedCredit.RemainingAmount)) { SetMessage("Kiểm tra số tiền phân bổ trước khi ghi nhận.", true); return; }
        var request = new CustomerReturnCreditAllocateRequest(AllocationDate.Trim(), drafts.ToArray());
        var slot = $"allocate:{_selectedCredit.Id}:{AllocationDate.Trim()}:{string.Join("|", drafts.Select(item => $"{item.ReceivableDocumentId}:{item.Amount}"))}";
        await ExecuteMutationAsync(slot, () => _service.AllocateAsync(_selectedCredit.Id, request, KeyFor(slot)), "Đã phân bổ khoản giảm công nợ.", _selectedCredit.Id).ConfigureAwait(true);
    }

    public async Task CreateRefundAsync()
    {
        if (!CanSubmitRefund || _selectedCredit is null) return;
        var request = new CustomerRefundCreateRequest(_selectedCredit.Id, DecimalText(CustomerReturnCreditPresentation.Amount(RefundAmount)), RefundMethod, DestinationReference.Trim(), NullIfBlank(ExternalReference), RefundReason.Trim(), RefundDate.Trim());
        var slot = $"refund:{_selectedCredit.Id}:{request.Amount}:{request.RefundMethod}:{request.DestinationReference}:{request.ExternalReference}:{request.Reason}:{request.RefundDate}";
        var success = await ExecuteMutationAsync(slot, () => _service.CreateRefundAsync(request, KeyFor(slot)), "Đã ghi nhận hoàn tiền từ phần chưa sử dụng.", _selectedCredit.Id).ConfigureAwait(true);
        if (success) { RefundAmount = string.Empty; DestinationReference = string.Empty; ExternalReference = string.Empty; RefundReason = string.Empty; }
    }

    public async Task ReverseRefundAsync(CustomerReturnCreditRefundRow row)
    {
        if (!row.CanReverse || !CanReverseRefund) return;
        if (string.IsNullOrWhiteSpace(ReversalReason)) { SetMessage("Cần nhập lý do trước khi đảo hoàn tiền.", true); return; }
        var request = new CustomerReturnCreditReverseRequest(ReversalReason.Trim());
        var slot = $"refund-reverse:{row.Id}:{request.Reason}";
        var selectedId = _selectedCredit?.Id;
        var success = await ExecuteMutationAsync(slot, () => _service.ReverseRefundAsync(row.Id, request, KeyFor(slot)), "Đã đảo hoàn tiền bằng bút toán bù.", selectedId).ConfigureAwait(true);
        if (success) ReversalReason = string.Empty;
    }

    public async Task ReverseCreditAsync()
    {
        if (!CanUseReverseCredit || _selectedCredit is null) return;
        var request = new CustomerReturnCreditReverseRequest(ReversalReason.Trim());
        var slot = $"credit-reverse:{_selectedCredit.Id}:{request.Reason}";
        var selectedId = _selectedCredit.Id;
        var success = await ExecuteMutationAsync(slot, () => _service.ReverseCreditAsync(selectedId, request, KeyFor(slot)), "Đã đảo khoản giảm công nợ hàng trả bằng bút toán bù.", selectedId).ConfigureAwait(true);
        if (success) ReversalReason = string.Empty;
    }

    private async Task<bool> ExecuteMutationAsync<T>(string slot, Func<Task<T>> mutation, string successMessage, string? selectId)
    {
        if (IsBusy) return false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await mutation().ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            await ReloadAfterMutationAsync(selectId).ConfigureAwait(true);
            SetMessage(successMessage, false);
            return true;
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(exception), exception.RequestId), true);
            return false;
        }
        catch
        {
            SetMessage("Không hoàn tất được thao tác. Có thể thử lại; ứng dụng sẽ dùng lại đúng khóa chống xử lý trùng.", true);
            return false;
        }
        finally { IsBusy = false; }
    }

    private async Task ReloadAfterMutationAsync(string? selectId)
    {
        _credits = (await _service.ListCreditsAsync().ConfigureAwait(true)).ToArray();
        _targets = (await _service.ListTargetsAsync().ConfigureAwait(true)).ToArray();
        RebuildCredits();
        if (!string.IsNullOrWhiteSpace(selectId))
        {
            _selectedCredit = await _service.GetCreditAsync(selectId).ConfigureAwait(true);
            SelectedCreditRow = Credits.FirstOrDefault(item => item.Id == selectId);
            RebuildDetail();
        }
        else ClearDetail();
        RaiseAll();
    }

    private string KeyFor(string slot)
    {
        if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;
        var key = _idempotencyKeys.Create("customer-return-credit");
        _mutationKeys[slot] = key;
        return key;
    }

    private void RebuildCredits()
    {
        Credits.Clear();
        var sequence = 1;
        foreach (var credit in _credits)
            Credits.Add(new CustomerReturnCreditRow(sequence++, credit.Id, credit.ReturnNumber, CustomerReturnCreditPresentation.Party(credit.CustomerCode, credit.CustomerName), CustomerReturnCreditPresentation.Party(credit.WarehouseCode, credit.WarehouseName), CustomerReturnCreditPresentation.Money(credit.OriginalAmount, credit.CurrencyCode), CustomerReturnCreditPresentation.Money(credit.RemainingAmount, credit.CurrencyCode), CustomerReturnCreditPresentation.Status(credit.Status)));
        OnPropertyChanged(nameof(HasCredits));
        OnPropertyChanged(nameof(ShowEmptyCredits));
    }

    private void RebuildDetail()
    {
        Lines.Clear(); AllocationTargets.Clear(); Refunds.Clear();
        if (_selectedCredit is null) return;
        foreach (var line in _selectedCredit.Lines.OrderBy(line => line.LineNumber))
            Lines.Add(new CustomerReturnCreditLineRow($"{line.Sku ?? "—"} · {line.ItemName ?? "—"}", line.SourceDocumentNumber ?? "—", $"{line.AcceptedBaseQuantity} {line.UnitCode}".Trim(), CustomerReturnCreditPresentation.Money(line.AdjustmentAmount, line.CurrencyCode)));
        foreach (var target in _targets.Where(target => target.CustomerId == _selectedCredit.CustomerId && string.Equals(target.CurrencyCode, _selectedCredit.CurrencyCode, StringComparison.OrdinalIgnoreCase) && CustomerReturnCreditPresentation.Amount(target.RemainingAmount) > 0m))
        {
            var row = new CustomerReturnCreditTargetRow(target.Id, target.DocumentNumber, target.SalesOrderNumber ?? string.Empty, CustomerReturnCreditPresentation.Party(target.WarehouseCode, target.WarehouseName), CustomerReturnCreditPresentation.Money(target.RemainingAmount, target.CurrencyCode), target.CurrencyCode);
            row.PropertyChanged += (_, _) => { OnPropertyChanged(nameof(AllocationSummary)); OnPropertyChanged(nameof(CanSubmitAllocation)); };
            AllocationTargets.Add(row);
        }
        foreach (var refund in _selectedCredit.Refunds)
            Refunds.Add(new CustomerReturnCreditRefundRow(refund.Id, refund.RefundNumber ?? "—", $"{CustomerReturnCreditPresentation.RefundMethod(refund.RefundMethod)} · {refund.DestinationReference}", refund.ExternalReference ?? "—", CustomerReturnCreditPresentation.Money(refund.Amount, refund.CurrencyCode), string.IsNullOrWhiteSpace(refund.ReversalId) ? "Đã hoàn" : "Đã đảo", CanReverseRefund && string.IsNullOrWhiteSpace(refund.ReversalId)));
    }

    private void ResetFormsForSelection()
    {
        AllocationDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        RefundAmount = string.Empty; RefundDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); RefundMethod = "BANK_TRANSFER";
        DestinationReference = string.Empty; ExternalReference = string.Empty; RefundReason = string.Empty; ReversalReason = string.Empty;
    }

    private void ClearDetail()
    {
        _selectedCredit = null; SelectedCreditRow = null; Lines.Clear(); AllocationTargets.Clear(); Refunds.Clear(); RaiseAll();
    }

    private decimal AllocationTotal() => AllocationTargets.Sum(row => CustomerReturnCreditPresentation.Amount(row.AmountInput));
    private static bool ValidDate(string value) => DateOnly.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    private static string DecimalText(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void RaiseAll()
    {
        foreach (var name in new[] { nameof(CanRead), nameof(CanAllocate), nameof(CanReverse), nameof(CanCreateRefund), nameof(CanReverseRefund), nameof(HasCredits), nameof(ShowEmptyCredits), nameof(HasSelectedCredit), nameof(DetailEmptyText), nameof(SelectedTitle), nameof(SelectedOriginal), nameof(SelectedAllocated), nameof(SelectedRemaining), nameof(SelectedWarehouse), nameof(SelectedStatus), nameof(HasRemaining), nameof(CanUseAllocationForm), nameof(CanUseRefundForm), nameof(CanSubmitAllocation), nameof(CanSubmitRefund), nameof(CanUseReverseCredit), nameof(AllocationSummary), nameof(TargetEmptyText), nameof(RefundEmptyText) }) OnPropertyChanged(name);
    }

    private void SetMessage(string message, bool isError) { MessageIsError = isError; Message = message; }
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; OnPropertyChanged(name); return true;
    }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action(); else dispatcher.Invoke(action);
    }
}
