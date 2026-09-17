using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed record CustomerOnboardingChoice(string Id, string Label);

public sealed class CustomerOnboardingRow : INotifyPropertyChanged
{
    private string _customerCode = string.Empty;
    private string _reason = string.Empty;
    private string _selectedCustomerId = string.Empty;
    private string _selectedAddressId = string.Empty;
    private string _selectedWarehouseId = string.Empty;
    private string _selectedSalesChannelId = string.Empty;
    private bool _isBusy;
    private bool _isAddressLoading;
    private int _addressRequestVersion;

    public required CustomerOnboardingRequestData Request { get; init; }
    public required string StatusLabel { get; init; }
    public required string SourceLabel { get; init; }
    public required string BroughtByLabel { get; init; }
    public required string OutletLabel { get; init; }
    public required string ReasonLabel { get; init; }
    public required string BusinessType { get; init; }
    public required string Address { get; init; }
    public required string UpdatedAt { get; init; }
    public required IReadOnlyList<CustomerOnboardingChoice> CustomerOptions { get; init; }
    public required IReadOnlyList<CustomerOnboardingChoice> WarehouseOptions { get; init; }
    public required IReadOnlyList<CustomerOnboardingChoice> SalesChannelOptions { get; init; }

    public string Id => Request.Id;
    public string CustomerName => Request.ProposedCustomer.Name;
    public string Phone => string.IsNullOrWhiteSpace(Request.ProposedCustomer.Phone) ? "Chưa có số điện thoại" : Request.ProposedCustomer.Phone!;
    public string ReviewReason => Request.ReviewReason ?? string.Empty;
    public string Status => Request.Status;
    public bool IsPortal => string.Equals(Request.SourceSystem, "CUSTOMER_PORTAL", StringComparison.Ordinal);
    public bool IsSubmittedOrNeedMoreInfo => Status is "submitted" or "need_more_info";
    public bool IsUnderReview => Status == "under_review";

    public bool CanReview { get; set; }
    public bool CanApprove { get; set; }
    public bool CanLinkExisting { get; set; }
    public bool CanReject { get; set; }

    public string CustomerCode
    {
        get => _customerCode;
        set => SetField(ref _customerCode, (value ?? string.Empty).ToUpperInvariant());
    }

    public string Reason
    {
        get => _reason;
        set => SetField(ref _reason, value ?? string.Empty);
    }

    public string SelectedCustomerId
    {
        get => _selectedCustomerId;
        set => SetField(ref _selectedCustomerId, value ?? string.Empty);
    }

    public string SelectedAddressId
    {
        get => _selectedAddressId;
        set => SetField(ref _selectedAddressId, value ?? string.Empty);
    }

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set => SetField(ref _selectedWarehouseId, value ?? string.Empty);
    }

    public string SelectedSalesChannelId
    {
        get => _selectedSalesChannelId;
        set => SetField(ref _selectedSalesChannelId, value ?? string.Empty);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

    public bool IsAddressLoading
    {
        get => _isAddressLoading;
        set => SetField(ref _isAddressLoading, value);
    }

    public ObservableCollection<CustomerOnboardingChoice> AddressOptions { get; } = [];

    public int BeginAddressRequest() => ++_addressRequestVersion;
    public bool IsCurrentAddressRequest(int version) => _addressRequestVersion == version;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanReview));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanLinkExisting));
        OnPropertyChanged(nameof(CanReject));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class CustomerOnboardingViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.customer-onboarding.read";
    private const string ReviewPermission = "core.customer-onboarding.review";
    private const string ApprovePermission = "core.customer-onboarding.approve";
    private const string LinkExistingPermission = "core.customer-onboarding.link-existing";
    private const string RejectPermission = "core.customer-onboarding.reject";

    private readonly ICustomerOnboardingService _service;
    private readonly IPartnerService _partners;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _operationKeys = new(StringComparer.Ordinal);
    private bool _isLoaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;

    public CustomerOnboardingViewModel(
        ICustomerOnboardingService service,
        IPartnerService partners,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _partners = partners;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanRead));
            ApplyPermissionsToRows();
        };
    }

    public ObservableCollection<CustomerOnboardingRow> Rows { get; } = [];
    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) OnPropertyChanged(nameof(IsNotBusy)); } }
    public bool IsNotBusy => !IsBusy;
    public string Message { get => _message; private set { if (SetField(ref _message, value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool MessageIsError { get => _messageIsError; private set => SetField(ref _messageIsError, value); }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasRows => Rows.Count > 0;
    public bool IsEmpty => _isLoaded && !IsBusy && Rows.Count == 0;
    public int PendingCount => Rows.Count;

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;
        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        if (!CanRead)
        {
            SetMessage("Tài khoản chưa được cấp quyền xem đề nghị mở/liên kết mã khách.", true);
            return;
        }

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var requests = await _service.ListPendingAsync().ConfigureAwait(true);
            IReadOnlyList<CustomerData> customers = [];
            IReadOnlyList<EmployeeData> employees = [];
            var supplementalErrors = new List<string>();

            try { customers = await _partners.ListCustomersAsync().ConfigureAwait(true); }
            catch { supplementalErrors.Add("Không tải được danh sách khách hàng có sẵn để liên kết."); }

            try { employees = await _partners.ListEmployeesAsync().ConfigureAwait(true); }
            catch { supplementalErrors.Add("Không tải được tên nhân viên đưa khách về."); }

            var portalOptions = new CustomerOnboardingPortalOptionsData();
            if (requests.Any(item => string.Equals(item.SourceSystem, "CUSTOMER_PORTAL", StringComparison.Ordinal)))
            {
                try { portalOptions = await _service.GetPortalOptionsAsync().ConfigureAwait(true); }
                catch { supplementalErrors.Add("Không tải được danh sách kho/kênh bán để kích hoạt tài khoản khách hàng."); }
            }

            var employeeById = employees.ToDictionary(item => item.Id, StringComparer.Ordinal);
            var customerOptions = customers
                .Where(item => item.IsActive)
                .OrderBy(item => item.Code, StringComparer.CurrentCultureIgnoreCase)
                .Select(item => new CustomerOnboardingChoice(item.Id, CustomerOnboardingPresentation.CustomerLabel(item)))
                .ToArray();
            var warehouseOptions = portalOptions.Warehouses.Select(item => new CustomerOnboardingChoice(item.Id, $"{item.Code} — {item.Name}")).ToArray();
            var salesChannelOptions = portalOptions.SalesChannels.Select(item => new CustomerOnboardingChoice(item.Id, $"{item.Code} — {item.Name}")).ToArray();
            var defaultWarehouseId = warehouseOptions.Length == 1 ? warehouseOptions[0].Id : string.Empty;
            var canonicalChannels = portalOptions.SalesChannels.Where(item => item.Code == "CUSTOMER_PORTAL").ToArray();
            var defaultSalesChannelId = canonicalChannels.Length == 1
                ? canonicalChannels[0].Id
                : salesChannelOptions.Length == 1 ? salesChannelOptions[0].Id : string.Empty;

            Rows.Clear();
            foreach (var request in requests)
            {
                var row = new CustomerOnboardingRow
                {
                    Request = request,
                    StatusLabel = CustomerOnboardingPresentation.StatusLabel(request.Status),
                    SourceLabel = CustomerOnboardingPresentation.SourceLabel(request),
                    BroughtByLabel = CustomerOnboardingPresentation.BroughtByLabel(request, employeeById),
                    OutletLabel = CustomerOnboardingPresentation.OutletLabel(request),
                    ReasonLabel = CustomerOnboardingPresentation.ReasonLabel(request),
                    BusinessType = CustomerOnboardingPresentation.BusinessType(request),
                    Address = CustomerOnboardingPresentation.Address(request.ProposedCustomer.Address),
                    UpdatedAt = CustomerOnboardingPresentation.UpdatedAt(request.UpdatedAt),
                    CustomerOptions = customerOptions,
                    WarehouseOptions = warehouseOptions,
                    SalesChannelOptions = salesChannelOptions,
                    SelectedWarehouseId = defaultWarehouseId,
                    SelectedSalesChannelId = defaultSalesChannelId
                };
                ApplyPermissions(row);
                Rows.Add(row);
            }

            _isLoaded = true;
            _operationKeys.Clear();
            RaiseRowsState();
            if (supplementalErrors.Count > 0) SetMessage(string.Join(" ", supplementalErrors), true);
        }
        catch (CanonicalApiException exception)
        {
            SetMessage(exception.Message, true);
        }
        catch
        {
            SetMessage("Không tải được danh sách đề nghị mở/liên kết mã khách. Vui lòng cập nhật dữ liệu và thử lại.", true);
        }
        finally
        {
            IsBusy = false;
            RaiseRowsState();
        }
    }

    public async Task LoadAddressesAsync(CustomerOnboardingRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var version = row.BeginAddressRequest();
        row.AddressOptions.Clear();
        row.SelectedAddressId = string.Empty;
        if (string.IsNullOrWhiteSpace(row.SelectedCustomerId)) return;

        row.IsAddressLoading = true;
        try
        {
            var addresses = await _partners.ListCustomerAddressesAsync(row.SelectedCustomerId).ConfigureAwait(true);
            if (!row.IsCurrentAddressRequest(version)) return;
            foreach (var address in addresses.Where(item => item.IsActive))
                row.AddressOptions.Add(new CustomerOnboardingChoice(address.Id, CustomerOnboardingPresentation.AddressLabel(address)));
        }
        catch
        {
            if (row.IsCurrentAddressRequest(version))
                SetMessage("Không tải được địa chỉ khách hàng đã chọn.", true);
        }
        finally
        {
            if (row.IsCurrentAddressRequest(version)) row.IsAddressLoading = false;
        }
    }

    public Task StartReviewAsync(CustomerOnboardingRow row) =>
        PerformAsync(row, "review", key => _service.StartReviewAsync(row.Id, row.Request.Version, key), "Đã chuyển đề nghị sang bước xem xét.");

    public Task RequestMoreInfoAsync(CustomerOnboardingRow row)
    {
        var reason = row.Reason.Trim();
        if (reason.Length is < 1 or > 2000) return ValidationError("Cần nhập lý do từ 1 đến 2.000 ký tự trước khi yêu cầu bổ sung.");
        return PerformAsync(row, "need-more-info", key => _service.RequestMoreInfoAsync(row.Id, row.Request.Version, reason, key), "Đã yêu cầu bổ sung thông tin.");
    }

    public Task ApproveAsync(CustomerOnboardingRow row)
    {
        var code = row.CustomerCode.Trim().ToUpperInvariant();
        if (code.Length is < 1 or > 64 || code.Any(ch => !(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-')))
            return ValidationError("Mã khách chỉ gồm chữ in hoa, số, dấu gạch ngang hoặc gạch dưới.");
        if (!TryPortalActivation(row, out var warehouseId, out var channelId)) return Task.CompletedTask;
        return PerformAsync(row, "approve", key => _service.ApproveAsync(row.Id, row.Request.Version, code, warehouseId, channelId, key), "Đã duyệt và tạo mã khách hàng mới.");
    }

    public Task LinkExistingAsync(CustomerOnboardingRow row)
    {
        if (string.IsNullOrWhiteSpace(row.SelectedCustomerId) || string.IsNullOrWhiteSpace(row.SelectedAddressId))
            return ValidationError("Cần chọn khách hàng và địa chỉ cần liên kết.");
        if (!TryPortalActivation(row, out var warehouseId, out var channelId)) return Task.CompletedTask;
        return PerformAsync(row, "link-existing", key => _service.LinkExistingAsync(row.Id, row.Request.Version, row.SelectedCustomerId, row.SelectedAddressId, warehouseId, channelId, key), "Đã liên kết đề nghị với khách hàng có sẵn.");
    }

    public Task RejectAsync(CustomerOnboardingRow row)
    {
        var reason = row.Reason.Trim();
        if (reason.Length is < 1 or > 2000) return ValidationError("Cần nhập lý do từ 1 đến 2.000 ký tự trước khi từ chối.");
        return PerformAsync(row, "reject", key => _service.RejectAsync(row.Id, row.Request.Version, reason, key), "Đã từ chối đề nghị.");
    }

    private async Task PerformAsync(CustomerOnboardingRow row, string action, Func<string, Task<CustomerOnboardingRequestData>> mutation, string success)
    {
        if (row.IsBusy) return;
        if (!Allowed(row, action))
        {
            SetMessage("Tài khoản chưa được cấp quyền thực hiện thao tác này.", true);
            return;
        }

        var cacheKey = $"{row.Id}:{action}:{row.Request.Version}";
        if (!_operationKeys.TryGetValue(cacheKey, out var key))
        {
            key = _idempotencyKeys.Create("customer-onboarding-action");
            _operationKeys[cacheKey] = key;
        }

        row.IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            _ = await mutation(key).ConfigureAwait(true);
            _operationKeys.Remove(cacheKey);
            SetMessage(success, false);
            _isLoaded = false;
            await RefreshAsync().ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(Message)) SetMessage(success, false);
        }
        catch (CanonicalApiException exception)
        {
            if (!exception.Retryable) _operationKeys.Remove(cacheKey);
            SetMessage(exception.Message, true);
        }
        catch (ArgumentException exception)
        {
            _operationKeys.Remove(cacheKey);
            SetMessage(exception.Message, true);
        }
        catch
        {
            SetMessage("Không thực hiện được thao tác. Có thể bấm lại để thử lại với cùng yêu cầu.", true);
        }
        finally
        {
            row.IsBusy = false;
        }
    }

    private bool TryPortalActivation(CustomerOnboardingRow row, out string? warehouseId, out string? channelId)
    {
        warehouseId = null;
        channelId = null;
        if (!row.IsPortal) return true;
        if (string.IsNullOrWhiteSpace(row.SelectedWarehouseId) || string.IsNullOrWhiteSpace(row.SelectedSalesChannelId))
        {
            SetMessage("Cần chọn kho mặc định và kênh bán cho tài khoản khách hàng.", true);
            return false;
        }
        warehouseId = row.SelectedWarehouseId;
        channelId = row.SelectedSalesChannelId;
        return true;
    }

    private bool Allowed(CustomerOnboardingRow row, string action) => action switch
    {
        "review" or "need-more-info" => row.CanReview,
        "approve" => row.CanApprove,
        "link-existing" => row.CanLinkExisting,
        "reject" => row.CanReject,
        _ => false
    };

    private Task ValidationError(string message)
    {
        SetMessage(message, true);
        return Task.CompletedTask;
    }

    private void ApplyPermissionsToRows()
    {
        foreach (var row in Rows) ApplyPermissions(row);
    }

    private void ApplyPermissions(CustomerOnboardingRow row)
    {
        row.CanReview = _access.HasPermission(ReviewPermission);
        row.CanApprove = _access.HasPermission(ApprovePermission);
        row.CanLinkExisting = _access.HasPermission(LinkExistingPermission);
        row.CanReject = _access.HasPermission(RejectPermission);
        row.RaisePermissions();
    }

    private void SetMessage(string value, bool isError)
    {
        MessageIsError = isError;
        Message = value;
    }

    private void RaiseRowsState()
    {
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(PendingCount));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
