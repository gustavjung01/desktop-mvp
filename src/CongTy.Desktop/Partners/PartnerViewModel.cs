using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Partners;

public sealed partial class PartnerViewModel : INotifyPropertyChanged
{
    private const string CustomerRead = "core.customer.read";
    private const string CustomerWrite = "core.customer.write";
    private const string SupplierRead = "core.supplier.read";
    private const string SupplierWrite = "core.supplier.write";
    private const string EmployeeRead = "core.employee.read";

    private enum EditorKind
    {
        None,
        Customer,
        CustomerGroup,
        CustomerAddress,
        Supplier,
        SupplierContact,
        SupplierAddress,
        SupplierPaymentTerm
    }

    private sealed record CustomerMediaUploadAttempt(
        string ClientUploadId,
        string PrepareKey,
        string FinalizeKey);

    private readonly IPartnerService _service;
    private readonly ICustomerMediaTransferClient _mediaTransfer;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly IAuthenticationService _authentication;

    private readonly List<CustomerData> _customers = [];
    private readonly List<CustomerGroupData> _groups = [];
    private readonly List<EmployeeData> _employees = [];
    private readonly List<CustomerAddressData> _customerAddresses = [];
    private readonly Dictionary<string, CustomerMediaUploadAttempt> _customerMediaUploadAttempts = [];
    private int _customerMediaMaxPhotos = 3;
    private CustomerProfileOverviewData? _customerOverview;
    private string _customerOverviewPeriod = "90d";
    private string _customerPurchasedSearch = string.Empty;
    private string _customerOrderSearch = string.Empty;
    private int _customerPurchasedOffset;
    private int _customerOrderOffset;
    private int _customerReceivableOffset;
    private int _customerPaymentOffset;
    private int _customerDeliveryOffset;
    private int _customerReturnOffset;
    private bool _customerPurchasedHasNext;
    private long _customerPurchasedTotal;
    private bool _customerOrderHasNext;
    private bool _customerReceivableHasNext;
    private bool _customerPaymentHasNext;
    private string _customerPaymentAccess = "unknown";
    private CustomerDeliveryReturnsData? _customerDeliveryReturns;
    private readonly List<SupplierData> _suppliers = [];
    private readonly List<SupplierContactData> _supplierContacts = [];
    private readonly List<SupplierAddressData> _supplierAddresses = [];
    private readonly List<SupplierPaymentTermData> _supplierPaymentTerms = [];

    private bool _isLoaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;

    private string _customerSearch = string.Empty;
    private string _customerStatus = "all";
    private string _customerGroupFilter = "all";
    private string _customerEmployeeFilter = "all";
    private string _groupSearch = string.Empty;
    private string _groupStatus = "all";
    private string _supplierSearch = string.Empty;
    private string _supplierStatus = "all";
    private string _selectedCustomerId = string.Empty;
    private string _selectedSupplierId = string.Empty;
    private int _customerWorkspaceTabIndex;
    private bool _isCustomerAddressManagerOpen;
    private CustomerData? _pendingCreatedCustomer;

    private EditorKind _editorKind;
    private bool _isCreateMode;
    private string? _editingId;
    private string? _editorIdempotencyKey;
    private string? _editorAddressIdempotencyKey;
    private bool _bulkIdentificationReady;
    private string _bulkSourceSummary = string.Empty;

    private string _draftCode = string.Empty;
    private string _draftName = string.Empty;
    private string _draftDescription = string.Empty;
    private string _draftGroupId = string.Empty;
    private string _draftEmployeeId = string.Empty;
    private string _draftPhone = string.Empty;
    private string _draftEmail = string.Empty;
    private string _draftTaxId = string.Empty;
    private string _draftPaymentTermsDays = "0";
    private string _draftCreditLimit = "0";
    private string _draftNotes = string.Empty;
    private string _draftBankAccount = string.Empty;
    private string _draftBankName = string.Empty;
    private string _draftAvgDeliveryDays = string.Empty;

    private string _draftLabel = string.Empty;
    private string _draftRecipient = string.Empty;
    private string _draftLocationUrl = string.Empty;
    private string _draftAddressPhone = string.Empty;
    private string _draftAddressLine1 = string.Empty;
    private string _draftAddressLine2 = string.Empty;
    private string _draftWard = string.Empty;
    private string _draftDistrict = string.Empty;
    private string _draftProvince = string.Empty;
    private string _draftPostalCode = string.Empty;
    private string _draftCountry = "VN";
    private bool _draftDefaultOrPrimary;
    private bool _draftActive = true;
    private string _draftTitle = string.Empty;
    private string _draftAddressType = "business";
    private string _draftPaymentMethod = string.Empty;
    private string _draftTermDays = string.Empty;

    private string _bulkMode = "import";
    private string _bulkFileName = string.Empty;
    private IReadOnlyList<string[]> _bulkMatrix = [];
    private bool _bulkHasHeader = true;
    private string? _bulkOperationKey;
    private bool _bulkApplied;
    private string _bulkSummary = string.Empty;

    public PartnerViewModel(
        IPartnerService service,
        ICustomerMediaTransferClient mediaTransfer,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access,
        IAuthenticationService authentication)
    {
        _service = service;
        _mediaTransfer = mediaTransfer;
        _idempotencyKeys = idempotencyKeys;
        _access = access;
        _authentication = authentication;

        _access.Changed += (_, _) => RaisePermissionState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CustomerRow> CustomerRows { get; } = [];
    public ObservableCollection<CustomerGroupRow> CustomerGroupRows { get; } = [];
    public ObservableCollection<CustomerAddressRow> CustomerAddressRows { get; } = [];
    public ObservableCollection<CustomerMediaRow> CustomerMediaRows { get; } = [];
    public ObservableCollection<CustomerPurchasedItemRow> CustomerPurchasedItemRows { get; } = [];
    public ObservableCollection<CustomerOrderRow> CustomerOrderRows { get; } = [];
    public ObservableCollection<CustomerReceivableRow> CustomerReceivableRows { get; } = [];
    public ObservableCollection<CustomerPaymentRow> CustomerPaymentRows { get; } = [];
    public ObservableCollection<CustomerDeliveryRow> CustomerDeliveryRows { get; } = [];
    public ObservableCollection<CustomerReturnRow> CustomerReturnRows { get; } = [];
    public ObservableCollection<SupplierRow> SupplierRows { get; } = [];
    public ObservableCollection<SupplierContactRow> SupplierContactRows { get; } = [];
    public ObservableCollection<SupplierAddressRow> SupplierAddressRows { get; } = [];
    public ObservableCollection<SupplierPaymentTermRow> SupplierPaymentTermRows { get; } = [];
    public ObservableCollection<PartnerLookupOption> CustomerGroupOptions { get; } = [];
    public ObservableCollection<PartnerLookupOption> CustomerGroupFilterOptions { get; } = [];
    public ObservableCollection<PartnerLookupOption> EmployeeOptions { get; } = [];
    public ObservableCollection<PartnerLookupOption> CustomerEmployeeFilterOptions { get; } = [];
    public ObservableCollection<PartnerLookupOption> SupplierProvinceOptions { get; } = [];
    public ObservableCollection<PartnerLookupOption> SupplierWardOptions { get; } = [];
    public ObservableCollection<BulkColumnMappingRow> BulkMappings { get; } = [];
    public ObservableCollection<BulkSourcePreviewRow> BulkSourceRows { get; } = [];
    public ObservableCollection<BulkPreviewRow> BulkPreviewRows { get; } = [];

    public IReadOnlyList<PartnerLookupOption> StatusOptions => PartnerPresentation.StatusOptions;
    public IReadOnlyList<PartnerLookupOption> SupplierStatusOptions => PartnerPresentation.SupplierStatusOptions;
    public IReadOnlyList<PartnerLookupOption> MappingOptions => PartnerPresentation.MappingOptions;

    public string BulkSourceSummary
    {
        get => _bulkSourceSummary;
        private set => SetField(ref _bulkSourceSummary, value);
    }

    public bool CanReadCustomers => _access.HasPermission(CustomerRead);
    public bool CanWriteCustomers => _access.HasPermission(CustomerWrite);
    public bool CanReadSuppliers => _access.HasPermission(SupplierRead);
    public bool CanWriteSuppliers => _access.HasPermission(SupplierWrite);
    public bool CanReadEmployees => _access.HasPermission(EmployeeRead);
    public bool CanViewPartners => CanReadCustomers || CanReadSuppliers;

    public bool IsLoaded => _isLoaded;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                OnPropertyChanged(nameof(CanApplyBulk));
                OnPropertyChanged(nameof(CanAddCustomerMedia));
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public int CustomerTotal => _customers.Count;
    public int CustomerActive => _customers.Count(item => item.IsActive);
    public int CustomerInactive => _customers.Count(item => !item.IsActive);
    public int CustomerWorkspaceTabIndex
    {
        get => _customerWorkspaceTabIndex;
        private set
        {
            var normalized = Math.Clamp(value, 0, 5);
            if (!SetField(ref _customerWorkspaceTabIndex, normalized)) return;
            OnPropertyChanged(nameof(IsCustomerProfileOpen));
            OnPropertyChanged(nameof(IsCustomerListSection));
            OnPropertyChanged(nameof(IsCustomerGroupSection));
            OnPropertyChanged(nameof(CanShowCustomerTopbarCreate));
            OnPropertyChanged(nameof(CustomerTopbarCreateText));
        }
    }

    public bool IsCustomerAddressManagerOpen
    {
        get => _isCustomerAddressManagerOpen;
        private set => SetField(ref _isCustomerAddressManagerOpen, value);
    }

    public bool CanAddSelectedCustomerAddress =>
        CanWriteCustomers && FindCustomer(SelectedCustomerId)?.IsActive == true;

    public bool IsCustomerProfileOpen => CustomerWorkspaceTabIndex == 5;
    public bool IsCustomerListSection => CustomerWorkspaceTabIndex == 0;
    public bool IsCustomerGroupSection => CustomerWorkspaceTabIndex == 4;
    public bool CanShowCustomerTopbarCreate => CustomerWorkspaceTabIndex is 0 or 4;
    public string CustomerTopbarCreateText => IsCustomerGroupSection ? "Thêm nhóm" : "Thêm khách hàng";
    public string CustomerVisibleSummary => $"{CustomerRows.Count} khách hàng";
    public bool CustomerHasNoRows => CustomerRows.Count == 0;
    public string CustomerGroupSummary => $"{CustomerGroupRows.Count} nhóm";
    public bool CustomerGroupHasNoRows => CustomerGroupRows.Count == 0;

    public int SupplierTotal => _suppliers.Count;
    public int SupplierActive => _suppliers.Count(item => item.IsActive);
    public int SupplierInactive => _suppliers.Count(item => !item.IsActive);

    public string CustomerSearch
    {
        get => _customerSearch;
        set
        {
            if (SetField(ref _customerSearch, value))
            {
                RefreshCustomers();
            }
        }
    }

    public string CustomerStatus
    {
        get => _customerStatus;
        set
        {
            if (SetField(ref _customerStatus, value))
            {
                RefreshCustomers();
            }
        }
    }

    public string CustomerGroupFilter
    {
        get => _customerGroupFilter;
        set
        {
            if (SetField(ref _customerGroupFilter, value))
            {
                RefreshCustomers();
            }
        }
    }

    public string CustomerEmployeeFilter
    {
        get => _customerEmployeeFilter;
        set
        {
            if (SetField(ref _customerEmployeeFilter, value))
            {
                RefreshCustomers();
            }
        }
    }

    public string GroupSearch
    {
        get => _groupSearch;
        set
        {
            if (SetField(ref _groupSearch, value))
            {
                RefreshGroups();
            }
        }
    }

    public string GroupStatus
    {
        get => _groupStatus;
        set
        {
            if (SetField(ref _groupStatus, value))
            {
                RefreshGroups();
            }
        }
    }

    public string SupplierSearch
    {
        get => _supplierSearch;
        set
        {
            if (SetField(ref _supplierSearch, value))
            {
                RefreshSuppliers();
            }
        }
    }

    public string SupplierStatus
    {
        get => _supplierStatus;
        set
        {
            if (SetField(ref _supplierStatus, value))
            {
                RefreshSuppliers();
            }
        }
    }

    public string SelectedCustomerId
    {
        get => _selectedCustomerId;
        private set
        {
            if (SetField(ref _selectedCustomerId, value))
            {
                OnPropertyChanged(nameof(SelectedCustomerLabel));
                OnPropertyChanged(nameof(SelectedCustomerToggleAction));
                OnPropertyChanged(nameof(CanAddCustomerMedia));
                OnPropertyChanged(nameof(CanAddSelectedCustomerAddress));
            }
        }
    }

    public string SelectedCustomerLabel
    {
        get
        {
            var item = FindCustomer(SelectedCustomerId);
            return item is null ? "Chưa chọn khách hàng" : $"{item.Code} · {item.Name}";
        }
    }

    public string SelectedCustomerToggleAction =>
        FindCustomer(SelectedCustomerId)?.IsActive == true ? "Ngừng sử dụng" : "Đưa vào sử dụng";

    public string CustomerOverviewCode => _customerOverview?.Customer.Code ?? FindCustomer(SelectedCustomerId)?.Code ?? "—";
    public string CustomerOverviewName => _customerOverview?.Customer.Name ?? FindCustomer(SelectedCustomerId)?.Name ?? "Chưa chọn khách hàng";
    public string CustomerOverviewPhone => _customerOverview?.Customer.Phone ?? "Chưa có số điện thoại";
    public string CustomerOverviewEmail => _customerOverview?.Customer.Email ?? "Chưa có email";
    public string CustomerOverviewTaxCode => _customerOverview?.Customer.TaxCode ?? "Chưa có mã số thuế";
    public string CustomerOverviewPaymentTerms => _customerOverview is null ? "—" : $"{_customerOverview.Customer.PaymentTermsDays} ngày";
    public string CustomerOverviewCreditLimit => _customerOverview is null ? "—" : PartnerPresentation.FormatMoney(_customerOverview.Customer.CreditLimit);
    public string CustomerOverviewAddressLabel
    {
        get
        {
            var address = _customerAddresses.FirstOrDefault(item => item.IsDefault && item.IsActive)
                ?? _customerAddresses.FirstOrDefault(item => item.IsActive)
                ?? _customerAddresses.FirstOrDefault();
            return address?.Label ?? "Chưa có địa chỉ";
        }
    }

    public string CustomerOverviewAddressText
    {
        get
        {
            var address = _customerAddresses.FirstOrDefault(item => item.IsDefault && item.IsActive)
                ?? _customerAddresses.FirstOrDefault(item => item.IsActive)
                ?? _customerAddresses.FirstOrDefault();
            return address is null ? "Chưa có địa chỉ giao dịch" : PartnerPresentation.CustomerAddress(address);
        }
    }

    public string CustomerMediaSummary =>
        $"{CustomerMediaRows.Count}/{_customerMediaMaxPhotos} ảnh";

    public string CustomerOverviewDefaultAddress
    {
        get
        {
            var address = _customerAddresses.FirstOrDefault(item => item.IsDefault && item.IsActive)
                ?? _customerAddresses.FirstOrDefault(item => item.IsActive)
                ?? _customerAddresses.FirstOrDefault();
            return address is null
                ? "Chưa có địa chỉ giao dịch"
                : $"{address.Label} · {PartnerPresentation.CustomerAddress(address)}";
        }
    }

    public bool HasCustomerOverview => _customerOverview is not null;

    public string CustomerOverviewPeriod
    {
        get => _customerOverviewPeriod;
        set
        {
            var normalized = value is "30d" or "90d" or "365d" or "all" ? value : "90d";
            SetField(ref _customerOverviewPeriod, normalized);
        }
    }

    public string CustomerOverviewStatus =>
        _customerOverview is null ? "Chưa tải" : PartnerPresentation.Status(_customerOverview.Customer.IsActive);

    public string CustomerOverviewGroup =>
        _customerOverview?.Customer.GroupName ?? "Chưa phân nhóm";

    public string CustomerOverviewEmployee =>
        _customerOverview?.Customer.ResponsibleEmployeeName ?? "Chưa giao phụ trách";

    public string CustomerOverviewContact =>
        _customerOverview is null
            ? "—"
            : string.Join(" · ", new[]
            {
                _customerOverview.Customer.Phone,
                _customerOverview.Customer.Email,
                _customerOverview.Customer.TaxCode
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public string CustomerOverviewRevenue =>
        _customerOverview?.Permissions.Sales == true && _customerOverview.Sales is not null
            ? PartnerPresentation.FormatMoney(_customerOverview.Sales.Revenue)
            : "Không có quyền xem";

    public string CustomerOverviewOrderCount =>
        _customerOverview?.Permissions.Sales == true && _customerOverview.Sales is not null
            ? _customerOverview.Sales.OrderCount
            : "—";

    public string CustomerOverviewLastPurchase =>
        _customerOverview?.Permissions.Sales == true && _customerOverview.Sales is not null
            ? PartnerPresentation.FormatDateTime(_customerOverview.Sales.LastPurchaseAt)
            : "—";

    public string CustomerOverviewReceivable =>
        _customerOverview?.Permissions.Receivable == true && _customerOverview.Receivable is not null
            ? PartnerPresentation.FormatMoney(_customerOverview.Receivable.Balance)
            : "Không có quyền xem";

    public string CustomerOverviewOpenAmount =>
        _customerOverview?.Permissions.Receivable == true && _customerOverview.Receivable is not null
            ? PartnerPresentation.FormatMoney(_customerOverview.Receivable.OpenAmount)
            : "Không có quyền xem";

    public string CustomerOverviewOpenDocuments =>
        _customerOverview?.Permissions.Receivable == true && _customerOverview.Receivable is not null
            ? _customerOverview.Receivable.OpenDocumentCount
            : "—";

    public string CustomerOverviewCredit =>
        _customerOverview is null
            ? "—"
            : $"{PartnerPresentation.FormatMoney(_customerOverview.Customer.CreditLimit)} · {_customerOverview.Customer.PaymentTermsDays} ngày";

    public string CustomerOverviewNotes =>
        string.IsNullOrWhiteSpace(_customerOverview?.Customer.Notes)
            ? "Chưa có ghi chú."
            : _customerOverview.Customer.Notes!;

    public bool CanAddCustomerMedia =>
        CanWriteCustomers
        && FindCustomer(SelectedCustomerId)?.IsActive == true
        && CustomerMediaRows.Count < _customerMediaMaxPhotos;


    public bool CustomerSalesAllowed => _customerOverview?.Permissions.Sales == true;
    public bool CustomerReceivableAllowed => _customerOverview?.Permissions.Receivable == true;
    public bool CustomerPaymentAllowed => _customerPaymentAccess == "allowed";
    public string CustomerPaymentAccessText => _customerPaymentAccess switch
    {
        "forbidden" => "Bạn không có quyền xem lịch sử thu tiền của khách hàng này.",
        "error" => "Chưa tải được lịch sử thu tiền.",
        _ => string.Empty
    };

    public string CustomerPurchasedSearch
    {
        get => _customerPurchasedSearch;
        set => SetField(ref _customerPurchasedSearch, value);
    }

    public string CustomerOrderSearch
    {
        get => _customerOrderSearch;
        set => SetField(ref _customerOrderSearch, value);
    }

    public bool CustomerPurchasedHasPrevious => _customerPurchasedOffset > 0;
    public bool CustomerPurchasedHasNext => _customerPurchasedHasNext;
    public bool CustomerOrderHasPrevious => _customerOrderOffset > 0;
    public bool CustomerOrderHasNext => _customerOrderHasNext;
    public bool CustomerReceivableHasPrevious => _customerReceivableOffset > 0;
    public bool CustomerReceivableHasNext => _customerReceivableHasNext;
    public bool CustomerPaymentHasPrevious => _customerPaymentOffset > 0;
    public bool CustomerPaymentHasNext => _customerPaymentHasNext;
    public bool CustomerDeliveryHasPrevious => _customerDeliveryReturns?.Deliveries.HasPrevious == true;
    public bool CustomerDeliveryHasNext => _customerDeliveryReturns?.Deliveries.HasNext == true;
    public bool CustomerReturnHasPrevious => _customerDeliveryReturns?.Returns.HasPrevious == true;
    public bool CustomerReturnHasNext => _customerDeliveryReturns?.Returns.HasNext == true;

    public string CustomerPurchasedSummary => $"{_customerPurchasedTotal} mặt hàng";
    public string CustomerPurchasedEmptyText => CustomerSalesAllowed
        ? "Khách hàng chưa có mặt hàng phù hợp."
        : "Bạn không có quyền xem lịch sử mua hàng của khách hàng này.";
    public string CustomerOrderEmptyText => CustomerSalesAllowed
        ? "Khách hàng chưa có đơn hàng phù hợp."
        : "Bạn không có quyền xem đơn hàng của khách hàng này.";
    public string CustomerReceivableEmptyText => CustomerReceivableAllowed
        ? "Khách hàng chưa có chứng từ công nợ."
        : "Bạn không có quyền xem công nợ của khách hàng này.";
    public string CustomerPaymentEmptyText => _customerPaymentAccess switch
    {
        "forbidden" => "Bạn không có quyền xem lịch sử thu tiền của khách hàng này.",
        "error" => "Chưa tải được lịch sử thu tiền.",
        _ => "Khách hàng chưa có phiếu thu."
    };
    public string CustomerDeliveryEmptyText => _customerDeliveryReturns?.Permissions.DeliveryOrders == false
        ? "Bạn không có quyền xem lịch sử giao hàng của khách hàng này."
        : "Khách hàng chưa có phiếu giao hàng.";
    public string CustomerReturnEmptyText => _customerDeliveryReturns?.Permissions.Returns == false
        ? "Bạn không có quyền xem lịch sử trả hàng của khách hàng này."
        : "Khách hàng chưa có phiếu trả hàng.";

    public string CustomerPurchasedPageText =>
        CustomerPurchasedItemRows.Count == 0 ? "Không có dữ liệu" : $"Đang xem {_customerPurchasedOffset + 1}–{_customerPurchasedOffset + CustomerPurchasedItemRows.Count}";

    public string CustomerOrderPageText =>
        CustomerOrderRows.Count == 0 ? "Không có dữ liệu" : $"Đang xem {_customerOrderOffset + 1}–{_customerOrderOffset + CustomerOrderRows.Count}";

    public string CustomerReceivablePageText =>
        CustomerReceivableRows.Count == 0 ? "Không có dữ liệu" : $"Đang xem {_customerReceivableOffset + 1}–{_customerReceivableOffset + CustomerReceivableRows.Count}";

    public string CustomerPaymentPageText =>
        CustomerPaymentRows.Count == 0 ? "Không có dữ liệu" : $"Đang xem {_customerPaymentOffset + 1}–{_customerPaymentOffset + CustomerPaymentRows.Count}";

    public string SelectedSupplierId
    {
        get => _selectedSupplierId;
        private set
        {
            if (SetField(ref _selectedSupplierId, value))
            {
                OnPropertyChanged(nameof(SelectedSupplierLabel));
                OnPropertyChanged(nameof(CanManageSelectedSupplierDetails));
            }
        }
    }

    public string SelectedSupplierLabel
    {
        get
        {
            var item = FindSupplier(SelectedSupplierId);
            return item is null ? "Chưa chọn nhà cung cấp" : $"{item.Code} · {item.Name}";
        }
    }

    public bool CanManageSelectedSupplierDetails =>
        CanWriteSuppliers && FindSupplier(SelectedSupplierId) is not null;

    public bool IsEditorOpen => _editorKind != EditorKind.None;
    public bool IsCreateMode => _isCreateMode;
    public bool IsCustomerEditor => _editorKind == EditorKind.Customer;
    public bool IsCustomerGroupEditor => _editorKind == EditorKind.CustomerGroup;
    public bool IsCustomerAddressEditor => _editorKind == EditorKind.CustomerAddress;
    public bool IsSupplierEditor => _editorKind == EditorKind.Supplier;
    public bool IsSupplierContactEditor => _editorKind == EditorKind.SupplierContact;
    public bool IsSupplierAddressEditor => _editorKind == EditorKind.SupplierAddress;
    public bool IsSupplierPaymentTermEditor => _editorKind == EditorKind.SupplierPaymentTerm;

    public string EditorTitle => _editorKind switch
    {
        EditorKind.Customer => IsCreateMode ? "Thêm khách hàng và địa chỉ" : "Sửa khách hàng",
        EditorKind.CustomerGroup => IsCreateMode ? "Thêm nhóm khách hàng" : "Chỉnh sửa nhóm khách hàng",
        EditorKind.CustomerAddress => IsCreateMode ? "Thêm địa chỉ khách hàng" : "Chỉnh sửa địa chỉ khách hàng",
        EditorKind.Supplier => IsCreateMode ? "Thêm nhà cung cấp" : "Chỉnh sửa nhà cung cấp",
        EditorKind.SupplierContact => IsCreateMode ? "Thêm người liên hệ" : "Chỉnh sửa người liên hệ",
        EditorKind.SupplierAddress => IsCreateMode ? "Thêm địa chỉ nhà cung cấp" : "Chỉnh sửa địa chỉ nhà cung cấp",
        EditorKind.SupplierPaymentTerm => IsCreateMode ? "Thêm điều khoản thanh toán" : "Chỉnh sửa điều khoản thanh toán",
        _ => string.Empty
    };

    public bool IsCustomerMasterDraftEnabled => _pendingCreatedCustomer is null;
    public bool IsCustomerCodeEditable => IsCustomerEditor && IsCreateMode && _pendingCreatedCustomer is null;
    public bool HasPendingCreatedCustomer => _pendingCreatedCustomer is not null;
    public string PendingCreatedCustomerText => _pendingCreatedCustomer is null
        ? string.Empty
        : $"Đã tạo {_pendingCreatedCustomer.Code}. Hãy lưu lại địa chỉ mặc định.";

    public string PartnerEditorPrimaryActionLabel => _editorKind switch
    {
        EditorKind.Supplier when IsCreateMode => "Thêm nhà cung cấp",
        EditorKind.Supplier => "Lưu thay đổi",
        EditorKind.SupplierContact when IsCreateMode => "Thêm liên hệ",
        EditorKind.SupplierContact => "Lưu thay đổi",
        EditorKind.SupplierAddress when IsCreateMode => "Thêm địa chỉ",
        EditorKind.SupplierAddress => "Lưu thay đổi",
        EditorKind.SupplierPaymentTerm when IsCreateMode => "Thêm điều khoản",
        EditorKind.SupplierPaymentTerm => "Lưu thay đổi",
        EditorKind.Customer when IsCreateMode => _pendingCreatedCustomer is null ? "Lưu khách hàng và địa chỉ" : "Lưu lại địa chỉ",
        EditorKind.Customer => "Lưu khách hàng",
        EditorKind.CustomerGroup => "Lưu nhóm",
        EditorKind.CustomerAddress => "Lưu địa chỉ",
        _ => "Lưu"
    };

    public string DraftCode { get => _draftCode; set => SetField(ref _draftCode, value); }
    public string DraftName { get => _draftName; set => SetField(ref _draftName, value); }
    public string DraftDescription { get => _draftDescription; set => SetField(ref _draftDescription, value); }
    public string DraftGroupId { get => _draftGroupId; set => SetField(ref _draftGroupId, value); }
    public string DraftEmployeeId { get => _draftEmployeeId; set => SetField(ref _draftEmployeeId, value); }
    public string DraftPhone { get => _draftPhone; set => SetField(ref _draftPhone, value); }
    public string DraftEmail { get => _draftEmail; set => SetField(ref _draftEmail, value); }
    public string DraftTaxId { get => _draftTaxId; set => SetField(ref _draftTaxId, value); }
    public string DraftPaymentTermsDays { get => _draftPaymentTermsDays; set => SetField(ref _draftPaymentTermsDays, value); }
    public string DraftCreditLimit { get => _draftCreditLimit; set => SetField(ref _draftCreditLimit, value); }
    public string DraftNotes { get => _draftNotes; set => SetField(ref _draftNotes, value); }
    public string DraftBankAccount { get => _draftBankAccount; set => SetField(ref _draftBankAccount, value); }
    public string DraftBankName { get => _draftBankName; set => SetField(ref _draftBankName, value); }
    public string DraftAvgDeliveryDays { get => _draftAvgDeliveryDays; set => SetField(ref _draftAvgDeliveryDays, value); }
    public string DraftLabel { get => _draftLabel; set => SetField(ref _draftLabel, value); }
    public string DraftRecipient { get => _draftRecipient; set => SetField(ref _draftRecipient, value); }
    public string DraftLocationUrl { get => _draftLocationUrl; set => SetField(ref _draftLocationUrl, value); }
    public string DraftAddressPhone { get => _draftAddressPhone; set => SetField(ref _draftAddressPhone, value); }
    public string DraftAddressLine1 { get => _draftAddressLine1; set => SetField(ref _draftAddressLine1, value); }
    public string DraftAddressLine2 { get => _draftAddressLine2; set => SetField(ref _draftAddressLine2, value); }
    public string DraftWard { get => _draftWard; set => SetField(ref _draftWard, value); }
    public string DraftDistrict { get => _draftDistrict; set => SetField(ref _draftDistrict, value); }
    public string DraftProvince { get => _draftProvince; set => SetField(ref _draftProvince, value); }
    public string DraftPostalCode { get => _draftPostalCode; set => SetField(ref _draftPostalCode, value); }
    public string DraftCountry { get => _draftCountry; set => SetField(ref _draftCountry, value); }
    public bool DraftDefaultOrPrimary { get => _draftDefaultOrPrimary; set => SetField(ref _draftDefaultOrPrimary, value); }
    public bool DraftActive { get => _draftActive; set => SetField(ref _draftActive, value); }
    public string DraftTitle { get => _draftTitle; set => SetField(ref _draftTitle, value); }
    public string DraftAddressType { get => _draftAddressType; set => SetField(ref _draftAddressType, value); }
    public string DraftPaymentMethod { get => _draftPaymentMethod; set => SetField(ref _draftPaymentMethod, value); }
    public string DraftTermDays { get => _draftTermDays; set => SetField(ref _draftTermDays, value); }

    public string BulkMode
    {
        get => _bulkMode;
        set
        {
            if (SetField(ref _bulkMode, value is "update" ? "update" : "import"))
            {
                ResetBulkPreview();
                _bulkIdentificationReady = false;
                if (_bulkMatrix.Count > 0)
                {
                    BuildBulkMappings();
                    BuildBulkSourcePreview();
                }
                else
                {
                    BulkSourceRows.Clear();
                    BulkSourceSummary = string.Empty;
                }
                OnPropertyChanged(nameof(IsBulkImport));
                OnPropertyChanged(nameof(BulkModeTitle));
            }
        }
    }

    public bool IsBulkImport => BulkMode == "import";
    public string BulkModeTitle => IsBulkImport ? "Nhập khách hàng" : "Cập nhật khách hàng";

    public string BulkFileName
    {
        get => _bulkFileName;
        private set
        {
            if (SetField(ref _bulkFileName, value))
            {
                OnPropertyChanged(nameof(HasBulkFile));
            }
        }
    }

    public bool HasBulkFile => _bulkMatrix.Count > 0;

    public bool BulkHasHeader
    {
        get => _bulkHasHeader;
        set
        {
            if (SetField(ref _bulkHasHeader, value) && _bulkMatrix.Count > 0)
            {
                BuildBulkMappings();
                BuildBulkSourcePreview();
                _bulkIdentificationReady = false;
                ResetBulkPreview();
            }
        }
    }

    public string BulkSummary
    {
        get => _bulkSummary;
        private set => SetField(ref _bulkSummary, value);
    }

    public bool CanApplyBulk =>
        !IsBusy
        && !_bulkApplied
        && !string.IsNullOrWhiteSpace(_bulkOperationKey)
        && BulkPreviewRows.Count > 0;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (!IsLoaded)
        {
            await RefreshAsync(cancellationToken).ConfigureAwait(true);
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || !CanViewPartners)
        {
            return;
        }

        IsBusy = true;
        SetMessage("Đang cập nhật dữ liệu đối tác...", false);

        try
        {
            var customersTask = CanReadCustomers
                ? _service.ListCustomersAsync(cancellationToken)
                : Task.FromResult<IReadOnlyList<CustomerData>>([]);
            var groupsTask = CanReadCustomers
                ? _service.ListCustomerGroupsAsync(cancellationToken)
                : Task.FromResult<IReadOnlyList<CustomerGroupData>>([]);
            var suppliersTask = CanReadSuppliers
                ? _service.ListSuppliersAsync(cancellationToken)
                : Task.FromResult<IReadOnlyList<SupplierData>>([]);
            var employeesTask = CanReadEmployees
                ? _service.ListEmployeesAsync(cancellationToken)
                : Task.FromResult<IReadOnlyList<EmployeeData>>([]);

            await Task.WhenAll(customersTask, groupsTask, suppliersTask, employeesTask).ConfigureAwait(true);

            Replace(_customers, await customersTask.ConfigureAwait(true));
            Replace(_groups, await groupsTask.ConfigureAwait(true));
            Replace(_suppliers, await suppliersTask.ConfigureAwait(true));
            Replace(_employees, await employeesTask.ConfigureAwait(true));

            _isLoaded = true;
            OnPropertyChanged(nameof(IsLoaded));
            RefreshPresentation();
            SetMessage("Dữ liệu đối tác đã được cập nhật.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SetCustomerWorkspaceTab(int index) =>
        CustomerWorkspaceTabIndex = index;

    public void CloseCustomerProfile() =>
        CustomerWorkspaceTabIndex = 0;

    public void OpenCreateCustomer()
    {
        if (!CanWriteCustomers) return;
        _pendingCreatedCustomer = null;
        RaisePendingCustomerState();
        OpenEditor(EditorKind.Customer, true, null, "customer-create");
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftGroupId = string.Empty;
        DraftEmployeeId = string.Empty;
        DraftPhone = string.Empty;
        DraftEmail = string.Empty;
        DraftTaxId = string.Empty;
        DraftPaymentTermsDays = "0";
        DraftCreditLimit = "0";
        DraftNotes = string.Empty;
        DraftLabel = "Địa chỉ giao dịch";
        DraftRecipient = string.Empty;
        DraftAddressPhone = string.Empty;
        DraftLocationUrl = string.Empty;
        DraftAddressLine1 = string.Empty;
        DraftAddressLine2 = string.Empty;
        DraftWard = string.Empty;
        DraftDistrict = string.Empty;
        DraftProvince = string.Empty;
        DraftPostalCode = string.Empty;
        DraftCountry = "VN";
        DraftDefaultOrPrimary = true;
        _editorAddressIdempotencyKey = _idempotencyKeys.Create("customer-address-create");
    }

    public void OpenEditCustomer(string id)
    {
        if (!CanWriteCustomers) return;
        var item = FindCustomer(id);
        if (item is null) return;
        OpenEditor(EditorKind.Customer, false, id, null);
        DraftCode = item.Code;
        DraftName = item.Name;
        DraftGroupId = item.GroupId ?? string.Empty;
        DraftEmployeeId = item.ResponsibleEmployeeId ?? string.Empty;
        DraftPhone = item.Phone ?? string.Empty;
        DraftEmail = item.Email ?? string.Empty;
        DraftTaxId = item.TaxCode ?? string.Empty;
        DraftPaymentTermsDays = item.PaymentTermsDays.ToString(CultureInfo.InvariantCulture);
        DraftCreditLimit = item.CreditLimit;
        DraftNotes = item.Notes ?? string.Empty;
    }

    public void OpenCreateGroup()
    {
        if (!CanWriteCustomers) return;
        OpenEditor(EditorKind.CustomerGroup, true, null, "customer-group-create");
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftDescription = string.Empty;
    }

    public void OpenEditGroup(string id)
    {
        if (!CanWriteCustomers) return;
        var item = _groups.FirstOrDefault(group => group.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.CustomerGroup, false, id, null);
        DraftCode = item.Code;
        DraftName = item.Name;
        DraftDescription = item.Description ?? string.Empty;
    }

    public async Task OpenCustomerAddressesAsync(string customerId, CancellationToken cancellationToken = default)
    {
        if (!CanReadCustomers || IsBusy) return;
        SelectedCustomerId = customerId;
        IsCustomerAddressManagerOpen = true;
        IsBusy = true;
        try
        {
            Replace(_customerAddresses, await _service.ListCustomerAddressesAsync(customerId, cancellationToken).ConfigureAwait(true));
            RefreshCustomerAddresses();
            SetMessage($"Đã tải địa chỉ của {SelectedCustomerLabel}.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CloseCustomerAddressManager()
    {
        if (IsBusy) return;
        IsCustomerAddressManagerOpen = false;
    }

    public async Task OpenCustomerOverviewAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadCustomers || IsBusy) return;

        SelectedCustomerId = customerId;
        await ReloadCustomerOverviewAsync(cancellationToken).ConfigureAwait(true);
    }

    public async Task ReloadCustomerOverviewAsync(CancellationToken cancellationToken = default)
    {
        if (!CanReadCustomers || string.IsNullOrWhiteSpace(SelectedCustomerId) || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var overviewTask = _service.GetCustomerOverviewAsync(
                SelectedCustomerId,
                CustomerOverviewPeriod,
                cancellationToken);
            var addressesTask = _service.ListCustomerAddressesAsync(
                SelectedCustomerId,
                cancellationToken);

            await Task.WhenAll(overviewTask, addressesTask).ConfigureAwait(true);
            _customerOverview = await overviewTask.ConfigureAwait(true);
            Replace(_customerAddresses, await addressesTask.ConfigureAwait(true));
            RefreshCustomerAddresses();
            RaiseCustomerOverviewState();
            SetMessage($"Đã tải tổng quan của {SelectedCustomerLabel}.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadCustomerProfileSectionAsync(
        string section,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadCustomers || string.IsNullOrWhiteSpace(SelectedCustomerId) || IsBusy) return;

        switch (section)
        {
            case "overview":
                await ReloadCustomerOverviewAsync(cancellationToken).ConfigureAwait(true);
                break;
            case "purchased-items":
                await LoadCustomerPurchasedItemsAsync(resetOffset: true, cancellationToken).ConfigureAwait(true);
                break;
            case "orders":
                await LoadCustomerOrdersAsync(resetOffset: true, cancellationToken).ConfigureAwait(true);
                break;
            case "finance":
                await LoadCustomerFinanceAsync(resetOffset: true, cancellationToken).ConfigureAwait(true);
                break;
            case "delivery-returns":
                await LoadCustomerDeliveryReturnsAsync(resetOffset: true, cancellationToken).ConfigureAwait(true);
                break;
            case "info":
                await LoadCustomerInfoAsync(cancellationToken).ConfigureAwait(true);
                break;
        }
    }

    public Task SearchCustomerPurchasedItemsAsync(CancellationToken cancellationToken = default) =>
        LoadCustomerPurchasedItemsAsync(resetOffset: true, cancellationToken);

    public Task SearchCustomerOrdersAsync(CancellationToken cancellationToken = default) =>
        LoadCustomerOrdersAsync(resetOffset: true, cancellationToken);

    public Task PreviousCustomerPurchasedItemsAsync(CancellationToken cancellationToken = default)
    {
        _customerPurchasedOffset = Math.Max(0, _customerPurchasedOffset - 50);
        return LoadCustomerPurchasedItemsAsync(resetOffset: false, cancellationToken);
    }

    public Task NextCustomerPurchasedItemsAsync(CancellationToken cancellationToken = default)
    {
        _customerPurchasedOffset += 50;
        return LoadCustomerPurchasedItemsAsync(resetOffset: false, cancellationToken);
    }

    public Task PreviousCustomerOrdersAsync(CancellationToken cancellationToken = default)
    {
        _customerOrderOffset = Math.Max(0, _customerOrderOffset - 20);
        return LoadCustomerOrdersAsync(resetOffset: false, cancellationToken);
    }

    public Task NextCustomerOrdersAsync(CancellationToken cancellationToken = default)
    {
        _customerOrderOffset += 20;
        return LoadCustomerOrdersAsync(resetOffset: false, cancellationToken);
    }

    public Task PreviousCustomerReceivablesAsync(CancellationToken cancellationToken = default)
    {
        _customerReceivableOffset = Math.Max(0, _customerReceivableOffset - 20);
        return LoadCustomerFinanceAsync(resetOffset: false, cancellationToken);
    }

    public Task NextCustomerReceivablesAsync(CancellationToken cancellationToken = default)
    {
        _customerReceivableOffset += 20;
        return LoadCustomerFinanceAsync(resetOffset: false, cancellationToken);
    }

    public Task PreviousCustomerPaymentsAsync(CancellationToken cancellationToken = default)
    {
        _customerPaymentOffset = Math.Max(0, _customerPaymentOffset - 20);
        return LoadCustomerFinanceAsync(resetOffset: false, cancellationToken);
    }

    public Task NextCustomerPaymentsAsync(CancellationToken cancellationToken = default)
    {
        _customerPaymentOffset += 20;
        return LoadCustomerFinanceAsync(resetOffset: false, cancellationToken);
    }

    public Task PreviousCustomerDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        _customerDeliveryOffset = Math.Max(0, _customerDeliveryOffset - 20);
        return LoadCustomerDeliveryReturnsAsync(resetOffset: false, cancellationToken);
    }

    public Task NextCustomerDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        _customerDeliveryOffset += 20;
        return LoadCustomerDeliveryReturnsAsync(resetOffset: false, cancellationToken);
    }

    public Task PreviousCustomerReturnsAsync(CancellationToken cancellationToken = default)
    {
        _customerReturnOffset = Math.Max(0, _customerReturnOffset - 20);
        return LoadCustomerDeliveryReturnsAsync(resetOffset: false, cancellationToken);
    }

    public Task NextCustomerReturnsAsync(CancellationToken cancellationToken = default)
    {
        _customerReturnOffset += 20;
        return LoadCustomerDeliveryReturnsAsync(resetOffset: false, cancellationToken);
    }

    public async Task OpenCustomerMediaAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadCustomers || IsBusy) return;

        SelectedCustomerId = customerId;
        IsBusy = true;
        try
        {
            await ReloadCustomerMediaAsync(cancellationToken).ConfigureAwait(true);
            SetMessage($"Đã tải ảnh của {SelectedCustomerLabel}.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task UploadCustomerMediaAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var customer = FindCustomer(SelectedCustomerId);
        if (!CanAddCustomerMedia || customer is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var image = await ImageMetadataReader.ReadAsync(filePath, cancellationToken).ConfigureAwait(true);
            var fullPath = Path.GetFullPath(filePath);
            var fingerprint = $"{customer.Id}|{fullPath}|{image.Bytes.LongLength}|{File.GetLastWriteTimeUtc(fullPath).Ticks}";
            if (!_customerMediaUploadAttempts.TryGetValue(fingerprint, out var attempt))
            {
                attempt = new CustomerMediaUploadAttempt(
                    Guid.NewGuid().ToString("N"),
                    _idempotencyKeys.Create("desktop-customer-media-prepare"),
                    _idempotencyKeys.Create("desktop-customer-media-finalize"));
                _customerMediaUploadAttempts[fingerprint] = attempt;
            }

            var prepared = await _service.PrepareCustomerMediaAsync(
                customer.Id,
                new CustomerMediaPrepareRequest(
                    "prepare",
                    attempt.ClientUploadId,
                    image.MimeType,
                    image.Bytes.LongLength),
                attempt.PrepareKey,
                cancellationToken).ConfigureAwait(true);

            await _mediaTransfer.UploadAsync(
                prepared.PutUrl,
                prepared.MimeType,
                image.Bytes,
                cancellationToken).ConfigureAwait(true);

            await _service.FinalizeCustomerMediaAsync(
                customer.Id,
                new CustomerMediaFinalizeRequest(
                    "finalize",
                    prepared.MediaId,
                    image.Width,
                    image.Height),
                attempt.FinalizeKey,
                cancellationToken).ConfigureAwait(true);

            _customerMediaUploadAttempts.Remove(fingerprint);
            await ReloadCustomerMediaAsync(cancellationToken).ConfigureAwait(true);
            SetMessage("Đã thêm ảnh khách hàng.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OpenCreateCustomerAddress()
    {
        if (!CanAddSelectedCustomerAddress) return;
        OpenEditor(EditorKind.CustomerAddress, true, null, "customer-address-create");
        DraftLabel = "Địa chỉ giao dịch";
        DraftRecipient = FindCustomer(SelectedCustomerId)?.Name ?? string.Empty;
        DraftAddressPhone = FindCustomer(SelectedCustomerId)?.Phone ?? string.Empty;
        DraftLocationUrl = string.Empty;
        DraftAddressLine1 = string.Empty;
        DraftAddressLine2 = string.Empty;
        DraftWard = string.Empty;
        DraftDistrict = string.Empty;
        DraftProvince = string.Empty;
        DraftPostalCode = string.Empty;
        DraftCountry = "VN";
        DraftDefaultOrPrimary = _customerAddresses.Count == 0;
        DraftActive = true;
    }

    public void OpenEditCustomerAddress(string id)
    {
        if (!CanWriteCustomers) return;
        var item = _customerAddresses.FirstOrDefault(address => address.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.CustomerAddress, false, id, null);
        DraftLabel = item.Label;
        DraftRecipient = item.RecipientName ?? string.Empty;
        DraftAddressPhone = item.Phone ?? string.Empty;
        DraftLocationUrl = item.LocationUrl ?? string.Empty;
        DraftAddressLine1 = item.AddressLine1;
        DraftAddressLine2 = item.AddressLine2 ?? string.Empty;
        DraftWard = item.Ward ?? string.Empty;
        DraftDistrict = item.District ?? string.Empty;
        DraftProvince = item.Province ?? string.Empty;
        DraftPostalCode = item.PostalCode ?? string.Empty;
        DraftCountry = item.CountryCode;
        DraftDefaultOrPrimary = item.IsDefault;
        DraftActive = item.IsActive;
    }

    public void OpenCreateSupplier()
    {
        if (!CanWriteSuppliers) return;
        OpenEditor(EditorKind.Supplier, true, null, "supplier-create");
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftTaxId = string.Empty;
        DraftBankAccount = string.Empty;
        DraftBankName = string.Empty;
        DraftAvgDeliveryDays = string.Empty;
        DraftEmployeeId = string.Empty;
        DraftAddressType = "business";
        DraftAddressLine1 = string.Empty;
        DraftWard = string.Empty;
        DraftDistrict = string.Empty;
        DraftProvince = string.Empty;
        DraftPostalCode = string.Empty;
        DraftCountry = "Việt Nam";
        DraftDefaultOrPrimary = true;
        DraftActive = true;
        _editorAddressIdempotencyKey = _idempotencyKeys.Create("supplier-address-create");
    }

    public async Task OpenCreateSupplierAsync(CancellationToken cancellationToken = default)
    {
        OpenCreateSupplier();
        if (!IsSupplierEditor || !IsCreateMode) return;

        IsBusy = true;
        try
        {
            var provinces = await _service.ListVietnamProvincesAsync(cancellationToken).ConfigureAwait(true);
            ResetCollection(
                SupplierProvinceOptions,
                provinces
                    .OrderBy(item => item.Name, StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), false))
                    .Select(item => new PartnerLookupOption(item.Code, item.Name)));
            SupplierWardOptions.Clear();
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadSupplierWardsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupplierEditor || !IsCreateMode)
        {
            return;
        }

        DraftWard = string.Empty;
        SupplierWardOptions.Clear();
        var province = SupplierProvinceOptions.FirstOrDefault(item =>
            string.Equals(item.Label, DraftProvince, StringComparison.Ordinal));
        if (province is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var wards = await _service.ListVietnamWardsAsync(province.Id, cancellationToken).ConfigureAwait(true);
            ResetCollection(
                SupplierWardOptions,
                wards
                    .OrderBy(item => item.Name, StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), false))
                    .Select(item => new PartnerLookupOption(item.Code, item.Name)));
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OpenEditSupplier(string id)
    {
        if (!CanWriteSuppliers) return;
        var item = FindSupplier(id);
        if (item is null) return;
        OpenEditor(EditorKind.Supplier, false, id, null);
        DraftCode = item.Code;
        DraftName = item.Name;
        DraftTaxId = item.TaxId ?? string.Empty;
        DraftBankAccount = item.BankAccount ?? string.Empty;
        DraftBankName = item.BankName ?? string.Empty;
        DraftAvgDeliveryDays = item.AvgDeliveryDays?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        DraftEmployeeId = item.PurchaseOwnerEmployeeId ?? string.Empty;
    }

    public async Task OpenSupplierDetailsAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        if (!CanReadSuppliers || IsBusy || FindSupplier(supplierId) is null) return;
        ClearSupplierChildren();
        SelectedSupplierId = supplierId;
        IsBusy = true;
        try
        {
            var contactsTask = _service.ListSupplierContactsAsync(supplierId, cancellationToken);
            var addressesTask = _service.ListSupplierAddressesAsync(supplierId, cancellationToken);
            var termsTask = _service.ListSupplierPaymentTermsAsync(supplierId, cancellationToken);
            await Task.WhenAll(contactsTask, addressesTask, termsTask).ConfigureAwait(true);
            Replace(_supplierContacts, await contactsTask.ConfigureAwait(true));
            Replace(_supplierAddresses, await addressesTask.ConfigureAwait(true));
            Replace(_supplierPaymentTerms, await termsTask.ConfigureAwait(true));
            RefreshSupplierChildren();
            SetMessage($"Đã tải hồ sơ nhà cung cấp {SelectedSupplierLabel}.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OpenCreateSupplierContact()
    {
        if (!CanManageSelectedSupplierDetails) return;
        OpenEditor(EditorKind.SupplierContact, true, null, "supplier-contact-create");
        DraftName = string.Empty;
        DraftTitle = string.Empty;
        DraftPhone = string.Empty;
        DraftEmail = string.Empty;
        DraftDefaultOrPrimary = _supplierContacts.Count == 0;
        DraftActive = true;
    }

    public void OpenEditSupplierContact(string id)
    {
        if (!CanWriteSuppliers) return;
        var item = _supplierContacts.FirstOrDefault(contact => contact.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.SupplierContact, false, id, null);
        DraftName = item.ContactName;
        DraftTitle = item.ContactTitle ?? string.Empty;
        DraftPhone = item.Phone ?? string.Empty;
        DraftEmail = item.Email ?? string.Empty;
        DraftDefaultOrPrimary = item.IsPrimary;
        DraftActive = item.IsActive;
    }

    public void OpenCreateSupplierAddress()
    {
        if (!CanManageSelectedSupplierDetails) return;
        OpenEditor(EditorKind.SupplierAddress, true, null, "supplier-address-create");
        DraftAddressType = "business";
        DraftAddressLine1 = string.Empty;
        DraftDistrict = string.Empty;
        DraftProvince = string.Empty;
        DraftPostalCode = string.Empty;
        DraftCountry = "Việt Nam";
        DraftDefaultOrPrimary = _supplierAddresses.Count == 0;
        DraftActive = true;
    }

    public void OpenEditSupplierAddress(string id)
    {
        if (!CanWriteSuppliers) return;
        var item = _supplierAddresses.FirstOrDefault(address => address.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.SupplierAddress, false, id, null);
        DraftAddressType = item.AddressType;
        DraftAddressLine1 = item.Street;
        DraftDistrict = item.City ?? string.Empty;
        DraftProvince = item.Province ?? string.Empty;
        DraftPostalCode = item.PostalCode ?? string.Empty;
        DraftCountry = item.Country;
        DraftDefaultOrPrimary = item.IsPrimary;
        DraftActive = item.IsActive;
    }

    public void OpenCreateSupplierPaymentTerm()
    {
        if (!CanManageSelectedSupplierDetails) return;
        OpenEditor(EditorKind.SupplierPaymentTerm, true, null, "supplier-payment-term-create");
        DraftPaymentMethod = string.Empty;
        DraftTermDays = string.Empty;
        DraftDescription = string.Empty;
        DraftDefaultOrPrimary = _supplierPaymentTerms.Count == 0;
        DraftActive = true;
    }

    public void OpenEditSupplierPaymentTerm(string id)
    {
        if (!CanWriteSuppliers) return;
        var item = _supplierPaymentTerms.FirstOrDefault(term => term.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.SupplierPaymentTerm, false, id, null);
        DraftPaymentMethod = item.PaymentMethod;
        DraftTermDays = item.TermDays?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        DraftDescription = item.Description ?? string.Empty;
        DraftDefaultOrPrimary = item.IsPrimary;
        DraftActive = item.IsActive;
    }

    public void CancelEditor()
    {
        if (IsCustomerEditor && _pendingCreatedCustomer is not null)
        {
            SetMessage(
                $"Khách hàng {_pendingCreatedCustomer.Code} đã được tạo nhưng địa chỉ mặc định chưa được lưu.",
                false);
        }

        _pendingCreatedCustomer = null;
        RaisePendingCustomerState();
        _editorKind = EditorKind.None;
        _editingId = null;
        _editorIdempotencyKey = null;
        _editorAddressIdempotencyKey = null;
        RaiseEditorState();
    }

    public async Task SaveEditorAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEditorOpen || IsBusy) return;
        if (!ValidateEditor(out var validationMessage))
        {
            SetMessage(validationMessage, true);
            return;
        }

        IsBusy = true;
        try
        {
            switch (_editorKind)
            {
                case EditorKind.Customer:
                    await SaveCustomerAsync(cancellationToken).ConfigureAwait(true);
                    break;
                case EditorKind.CustomerGroup:
                    await SaveGroupAsync(cancellationToken).ConfigureAwait(true);
                    break;
                case EditorKind.CustomerAddress:
                    await SaveCustomerAddressAsync(cancellationToken).ConfigureAwait(true);
                    break;
                case EditorKind.Supplier:
                    await SaveSupplierAsync(cancellationToken).ConfigureAwait(true);
                    break;
                case EditorKind.SupplierContact:
                    await SaveSupplierContactAsync(cancellationToken).ConfigureAwait(true);
                    break;
                case EditorKind.SupplierAddress:
                    await SaveSupplierAddressAsync(cancellationToken).ConfigureAwait(true);
                    break;
                case EditorKind.SupplierPaymentTerm:
                    await SaveSupplierPaymentTermAsync(cancellationToken).ConfigureAwait(true);
                    break;
            }

            CancelEditor();
            RefreshPresentation();
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public Task ToggleCustomerAsync(string id, CancellationToken cancellationToken = default) =>
        ToggleCustomerCoreAsync(id, cancellationToken);

    public Task ToggleGroupAsync(string id, CancellationToken cancellationToken = default) =>
        ToggleGroupCoreAsync(id, cancellationToken);

    public Task ToggleSupplierAsync(string id, CancellationToken cancellationToken = default) =>
        ToggleSupplierCoreAsync(id, cancellationToken);

    public async Task SetCustomerAddressDefaultAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = _customerAddresses.FirstOrDefault(address => address.Id == id);
        if (!CanWriteCustomers || IsBusy || item is null || item.IsDefault || !item.IsActive || string.IsNullOrWhiteSpace(SelectedCustomerId))
        {
            return;
        }

        await UpdateCustomerAddressStateAsync(
            item,
            isDefault: true,
            isActive: item.IsActive,
            "Đã đặt địa chỉ mặc định.",
            cancellationToken).ConfigureAwait(true);
    }

    public async Task ToggleCustomerAddressAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = _customerAddresses.FirstOrDefault(address => address.Id == id);
        if (!CanWriteCustomers || IsBusy || item is null || string.IsNullOrWhiteSpace(SelectedCustomerId))
        {
            return;
        }

        await UpdateCustomerAddressStateAsync(
            item,
            isDefault: item.IsDefault,
            isActive: !item.IsActive,
            item.IsActive ? "Địa chỉ đã ngừng sử dụng." : "Địa chỉ đã được đưa vào sử dụng.",
            cancellationToken).ConfigureAwait(true);
    }

    public async Task LoadBulkFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _bulkMatrix = await SpreadsheetMatrixReader.ReadAsync(filePath, cancellationToken).ConfigureAwait(true);
            BulkFileName = Path.GetFileName(filePath);
            BulkHasHeader = true;
            BuildBulkMappings();
            BuildBulkSourcePreview();
            _bulkIdentificationReady = false;
            ResetBulkPreview();
            SetMessage($"Đã đọc {Math.Max(0, _bulkMatrix.Count - 1)} dòng từ {BulkFileName}.", false);
            if (!IsBulkImport)
            {
                await IdentifyBulkCustomersCoreAsync(cancellationToken).ConfigureAwait(true);
            }
        }
        catch (Exception exception)
        {
            _bulkMatrix = [];
            BulkFileName = string.Empty;
            BulkMappings.Clear();
            ResetBulkPreview();
            SetMessage(exception.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ResetBulkFile()
    {
        _bulkMatrix = [];
        BulkFileName = string.Empty;
        BulkMappings.Clear();
        BulkSourceRows.Clear();
        BulkSourceSummary = string.Empty;
        _bulkIdentificationReady = false;
        ResetBulkPreview();
        BulkSummary = string.Empty;
    }

    public async Task IdentifyBulkCustomersAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || IsBulkImport || _bulkMatrix.Count == 0)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await IdentifyBulkCustomersCoreAsync(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task PreviewBulkAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || _bulkMatrix.Count == 0) return;

        if (!IsBulkImport && !_bulkIdentificationReady)
        {
            IsBusy = true;
            try
            {
                await IdentifyBulkCustomersCoreAsync(cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                IsBusy = false;
            }

            if (!_bulkIdentificationReady)
            {
                SetMessage("Cần nhận diện khách hàng trong tệp trước khi xem trước cập nhật.", true);
                return;
            }
        }

        var request = BuildBulkRequest(dryRun: true, includeExpectedVersions: false);
        if (!ValidateBulkMappings(request.Mappings, out var validationMessage))
        {
            SetMessage(validationMessage, true);
            return;
        }

        IsBusy = true;
        try
        {
            var result = IsBulkImport
                ? await _service.PreviewCustomerImportAsync(request, cancellationToken).ConfigureAwait(true)
                : await _service.PreviewCustomerBulkUpdateAsync(request, cancellationToken).ConfigureAwait(true);

            _bulkOperationKey = result.OperationKey;
            _bulkApplied = false;
            ShowBulkResult(result, applied: false);
            BulkSummary = $"Đã đối chiếu {result.Rows.Length} dòng · {result.Skipped} dòng lỗi sẽ được bỏ qua.";
            OnPropertyChanged(nameof(CanApplyBulk));
            SetMessage("Đã tạo bản xem trước. Kiểm tra kết quả trước khi thực hiện.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ApplyBulkAsync(CancellationToken cancellationToken = default)
    {
        if (!CanApplyBulk || string.IsNullOrWhiteSpace(_bulkOperationKey)) return;

        var request = BuildBulkRequest(dryRun: false, includeExpectedVersions: !IsBulkImport);

        IsBusy = true;
        try
        {
            var result = IsBulkImport
                ? await _service.ApplyCustomerImportAsync(request, _bulkOperationKey, cancellationToken).ConfigureAwait(true)
                : await _service.ApplyCustomerBulkUpdateAsync(request, _bulkOperationKey, cancellationToken).ConfigureAwait(true);

            _bulkApplied = true;
            ShowBulkResult(result, applied: true);
            BulkSummary = IsBulkImport
                ? $"Đã nhập {result.Created ?? 0} khách hàng · bỏ qua {result.Skipped} dòng lỗi."
                : $"Đã cập nhật {result.Updated ?? 0} khách hàng · bỏ qua {result.Skipped} dòng lỗi.";
            OnPropertyChanged(nameof(CanApplyBulk));
            await ReloadCustomerMasterAsync(cancellationToken).ConfigureAwait(true);
            SetMessage(BulkSummary, false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveCustomerAsync(CancellationToken cancellationToken)
    {
        var paymentTerms = ParseRequiredInt(DraftPaymentTermsDays, 0, 3650, "Thời hạn thanh toán");
        var creditLimit = ParseNonNegativeDecimalString(DraftCreditLimit, "Hạn mức tín dụng");

        CustomerData saved;
        if (IsCreateMode)
        {
            ValidateHttpsLocationUrl(DraftLocationUrl);
            saved = _pendingCreatedCustomer ?? await _service.CreateCustomerAsync(
                new CustomerCreateRequest(
                    DraftCode.Trim().ToUpperInvariant(),
                    DraftName.Trim(),
                    NullIfBlank(DraftGroupId),
                    NullIfBlank(DraftEmployeeId),
                    NullIfBlank(DraftPhone),
                    NullIfBlank(DraftEmail),
                    NullIfBlank(DraftTaxId),
                    paymentTerms,
                    creditLimit,
                    NullIfBlank(DraftNotes)),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);

            if (_pendingCreatedCustomer is null)
            {
                _pendingCreatedCustomer = saved;
                RaisePendingCustomerState();
                Upsert(_customers, saved, item => item.Id);
                RefreshCustomers();
                OnPropertyChanged(nameof(CustomerTotal));
                OnPropertyChanged(nameof(CustomerActive));
                OnPropertyChanged(nameof(CustomerInactive));
            }

            var addressKey = RequireEditorAddressKey();
            await _service.CreateCustomerAddressAsync(
                saved.Id,
                new CustomerAddressCreateRequest(
                    DraftLabel.Trim(),
                    NullIfBlank(DraftRecipient) ?? saved.Name,
                    NullIfBlank(DraftAddressPhone) ?? NullIfBlank(DraftPhone),
                    NullIfBlank(DraftLocationUrl),
                    DraftAddressLine1.Trim(),
                    NullIfBlank(DraftAddressLine2),
                    NullIfBlank(DraftWard),
                    NullIfBlank(DraftDistrict),
                    NullIfBlank(DraftProvince),
                    NullIfBlank(DraftPostalCode),
                    NormalizeCountryCode(DraftCountry),
                    true),
                addressKey,
                cancellationToken).ConfigureAwait(true);
            _pendingCreatedCustomer = null;
            RaisePendingCustomerState();
            SetMessage($"Đã tạo khách hàng {saved.Code} · {saved.Name} và địa chỉ mặc định.", false);
        }
        else
        {
            var current = _customers.First(item => item.Id == _editingId);
            saved = await _service.UpdateCustomerAsync(
                current.Id,
                new CustomerUpdateRequest(
                    DraftName.Trim(),
                    NullIfBlank(DraftGroupId),
                    NullIfBlank(DraftEmployeeId),
                    NullIfBlank(DraftPhone),
                    NullIfBlank(DraftEmail),
                    NullIfBlank(DraftTaxId),
                    paymentTerms,
                    creditLimit,
                    NullIfBlank(DraftNotes),
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật khách hàng.", false);
        }

        Upsert(_customers, saved, item => item.Id);
        _editorIdempotencyKey = null;
        _editorAddressIdempotencyKey = null;
    }

    private async Task SaveGroupAsync(CancellationToken cancellationToken)
    {
        CustomerGroupData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateCustomerGroupAsync(
                new CustomerGroupCreateRequest(
                    DraftCode.Trim().ToUpperInvariant(),
                    DraftName.Trim(),
                    NullIfBlank(DraftDescription)),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);
            SetMessage($"Đã tạo nhóm khách hàng {saved.Code} · {saved.Name}.", false);
        }
        else
        {
            var current = _groups.First(item => item.Id == _editingId);
            saved = await _service.UpdateCustomerGroupAsync(
                current.Id,
                new CustomerGroupUpdateRequest(
                    DraftName.Trim(),
                    NullIfBlank(DraftDescription),
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật nhóm khách hàng.", false);
        }

        Upsert(_groups, saved, item => item.Id);
        _editorIdempotencyKey = null;
    }

    private async Task UpdateCustomerAddressStateAsync(
        CustomerAddressData item,
        bool isDefault,
        bool isActive,
        string successMessage,
        CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            await _service.UpdateCustomerAddressAsync(
                SelectedCustomerId,
                item.Id,
                new CustomerAddressUpdateRequest(
                    item.Label,
                    item.RecipientName,
                    item.Phone,
                    item.LocationUrl,
                    item.AddressLine1,
                    item.AddressLine2,
                    item.Ward,
                    item.District,
                    item.Province,
                    item.PostalCode,
                    item.CountryCode,
                    isDefault,
                    isActive,
                    item.UpdatedAt),
                cancellationToken).ConfigureAwait(true);

            Replace(
                _customerAddresses,
                await _service.ListCustomerAddressesAsync(SelectedCustomerId, cancellationToken).ConfigureAwait(true));
            RefreshCustomerAddresses();
            SetMessage(successMessage, false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveCustomerAddressAsync(CancellationToken cancellationToken)
    {
        ValidateHttpsLocationUrl(DraftLocationUrl);

        CustomerAddressData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateCustomerAddressAsync(
                SelectedCustomerId,
                new CustomerAddressCreateRequest(
                    DraftLabel.Trim(),
                    NullIfBlank(DraftRecipient),
                    NullIfBlank(DraftAddressPhone),
                    NullIfBlank(DraftLocationUrl),
                    DraftAddressLine1.Trim(),
                    NullIfBlank(DraftAddressLine2),
                    NullIfBlank(DraftWard),
                    NullIfBlank(DraftDistrict),
                    NullIfBlank(DraftProvince),
                    NullIfBlank(DraftPostalCode),
                    NormalizeCountryCode(DraftCountry),
                    DraftDefaultOrPrimary),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã thêm địa chỉ khách hàng.", false);
        }
        else
        {
            var current = _customerAddresses.First(item => item.Id == _editingId);
            saved = await _service.UpdateCustomerAddressAsync(
                SelectedCustomerId,
                current.Id,
                new CustomerAddressUpdateRequest(
                    DraftLabel.Trim(),
                    NullIfBlank(DraftRecipient),
                    NullIfBlank(DraftAddressPhone),
                    NullIfBlank(DraftLocationUrl),
                    DraftAddressLine1.Trim(),
                    NullIfBlank(DraftAddressLine2),
                    NullIfBlank(DraftWard),
                    NullIfBlank(DraftDistrict),
                    NullIfBlank(DraftProvince),
                    NullIfBlank(DraftPostalCode),
                    NormalizeCountryCode(DraftCountry),
                    DraftDefaultOrPrimary,
                    DraftActive,
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật địa chỉ khách hàng.", false);
        }

        Upsert(_customerAddresses, saved, item => item.Id);
        _editorIdempotencyKey = null;
        RefreshCustomerAddresses();
    }

    private async Task SaveSupplierAsync(CancellationToken cancellationToken)
    {
        var avgDeliveryDays = ParseOptionalInt(DraftAvgDeliveryDays, 0, 3650, "Thời gian giao hàng");

        SupplierData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateSupplierAsync(
                new SupplierCreateRequest(
                    DraftCode.Trim().ToUpperInvariant(),
                    DraftName.Trim(),
                    NullIfBlank(DraftTaxId),
                    NullIfBlank(DraftBankAccount),
                    NullIfBlank(DraftBankName),
                    avgDeliveryDays,
                    NullIfBlank(DraftEmployeeId)),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);

            var hasSupplierAddress = !string.IsNullOrWhiteSpace(DraftAddressLine1)
                && !string.IsNullOrWhiteSpace(DraftProvince)
                && !string.IsNullOrWhiteSpace(DraftWard);
            if (hasSupplierAddress)
            {
                await _service.CreateSupplierAddressAsync(
                    saved.Id,
                    new SupplierAddressCreateRequest(
                        string.IsNullOrWhiteSpace(DraftAddressType) ? "business" : DraftAddressType.Trim(),
                        DraftAddressLine1.Trim(),
                        DraftWard.Trim(),
                        DraftProvince.Trim(),
                        null,
                        "Việt Nam",
                        true,
                        true),
                    RequireEditorAddressKey(),
                    cancellationToken).ConfigureAwait(true);
            }

            SetMessage(
                hasSupplierAddress
                    ? $"Đã tạo nhà cung cấp {saved.Code} · {saved.Name} và địa chỉ mặc định."
                    : $"Đã tạo nhà cung cấp {saved.Code} · {saved.Name}.",
                false);
        }
        else
        {
            var current = _suppliers.First(item => item.Id == _editingId);
            saved = await _service.UpdateSupplierAsync(
                current.Id,
                new SupplierUpdateRequest(
                    DraftName.Trim(),
                    NullIfBlank(DraftTaxId),
                    NullIfBlank(DraftBankAccount),
                    NullIfBlank(DraftBankName),
                    avgDeliveryDays,
                    NullIfBlank(DraftEmployeeId),
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật nhà cung cấp.", false);
        }

        Upsert(_suppliers, saved, item => item.Id);
        _editorIdempotencyKey = null;
        _editorAddressIdempotencyKey = null;
    }

    private async Task SaveSupplierContactAsync(CancellationToken cancellationToken)
    {
        SupplierContactData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateSupplierContactAsync(
                SelectedSupplierId,
                new SupplierContactCreateRequest(
                    DraftName.Trim(),
                    NullIfBlank(DraftTitle),
                    NullIfBlank(DraftPhone),
                    NullIfBlank(DraftEmail),
                    DraftDefaultOrPrimary,
                    DraftActive),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã thêm người liên hệ.", false);
        }
        else
        {
            var current = _supplierContacts.First(item => item.Id == _editingId);
            saved = await _service.UpdateSupplierContactAsync(
                SelectedSupplierId,
                current.Id,
                new SupplierContactUpdateRequest(
                    DraftName.Trim(),
                    NullIfBlank(DraftTitle),
                    NullIfBlank(DraftPhone),
                    NullIfBlank(DraftEmail),
                    DraftDefaultOrPrimary,
                    DraftActive,
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật người liên hệ.", false);
        }

        Upsert(_supplierContacts, saved, item => item.Id);
        _editorIdempotencyKey = null;
        RefreshSupplierChildren();
    }

    private async Task SaveSupplierAddressAsync(CancellationToken cancellationToken)
    {
        SupplierAddressData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateSupplierAddressAsync(
                SelectedSupplierId,
                new SupplierAddressCreateRequest(
                    string.IsNullOrWhiteSpace(DraftAddressType) ? "business" : DraftAddressType.Trim(),
                    DraftAddressLine1.Trim(),
                    NullIfBlank(DraftDistrict),
                    NullIfBlank(DraftProvince),
                    NullIfBlank(DraftPostalCode),
                    string.IsNullOrWhiteSpace(DraftCountry) ? "Việt Nam" : DraftCountry.Trim(),
                    DraftDefaultOrPrimary,
                    DraftActive),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã thêm địa chỉ nhà cung cấp.", false);
        }
        else
        {
            var current = _supplierAddresses.First(item => item.Id == _editingId);
            saved = await _service.UpdateSupplierAddressAsync(
                SelectedSupplierId,
                current.Id,
                new SupplierAddressUpdateRequest(
                    string.IsNullOrWhiteSpace(DraftAddressType) ? "business" : DraftAddressType.Trim(),
                    DraftAddressLine1.Trim(),
                    NullIfBlank(DraftDistrict),
                    NullIfBlank(DraftProvince),
                    NullIfBlank(DraftPostalCode),
                    string.IsNullOrWhiteSpace(DraftCountry) ? "Việt Nam" : DraftCountry.Trim(),
                    DraftDefaultOrPrimary,
                    DraftActive,
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật địa chỉ nhà cung cấp.", false);
        }

        Upsert(_supplierAddresses, saved, item => item.Id);
        _editorIdempotencyKey = null;
        RefreshSupplierChildren();
    }

    private async Task SaveSupplierPaymentTermAsync(CancellationToken cancellationToken)
    {
        var termDays = ParseOptionalInt(DraftTermDays, 0, 3650, "Số ngày thanh toán");

        SupplierPaymentTermData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateSupplierPaymentTermAsync(
                SelectedSupplierId,
                new SupplierPaymentTermCreateRequest(
                    DraftPaymentMethod.Trim(),
                    termDays,
                    NullIfBlank(DraftDescription),
                    DraftDefaultOrPrimary,
                    DraftActive),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã thêm điều khoản thanh toán.", false);
        }
        else
        {
            var current = _supplierPaymentTerms.First(item => item.Id == _editingId);
            saved = await _service.UpdateSupplierPaymentTermAsync(
                SelectedSupplierId,
                current.Id,
                new SupplierPaymentTermUpdateRequest(
                    DraftPaymentMethod.Trim(),
                    termDays,
                    NullIfBlank(DraftDescription),
                    DraftDefaultOrPrimary,
                    DraftActive,
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật điều khoản thanh toán.", false);
        }

        Upsert(_supplierPaymentTerms, saved, item => item.Id);
        _editorIdempotencyKey = null;
        RefreshSupplierChildren();
    }

    private async Task ToggleCustomerCoreAsync(string id, CancellationToken cancellationToken)
    {
        if (!CanWriteCustomers || IsBusy) return;
        IsBusy = true;
        try
        {
            var current = _customers.First(item => item.Id == id);
            var saved = await _service.SetCustomerActiveAsync(
                current.Id,
                !current.IsActive,
                current.UpdatedAt,
                cancellationToken).ConfigureAwait(true);
            Upsert(_customers, saved, item => item.Id);
            RefreshCustomers();
            RaiseCounts();
            OnPropertyChanged(nameof(SelectedCustomerToggleAction));
            RaiseCustomerMediaState();
            SetMessage(saved.IsActive ? "Khách hàng đã được kích hoạt." : "Khách hàng đã ngừng hoạt động.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleGroupCoreAsync(string id, CancellationToken cancellationToken)
    {
        if (!CanWriteCustomers || IsBusy) return;
        IsBusy = true;
        try
        {
            var current = _groups.First(item => item.Id == id);
            var saved = await _service.SetCustomerGroupActiveAsync(
                current.Id,
                !current.IsActive,
                current.UpdatedAt,
                cancellationToken).ConfigureAwait(true);
            Upsert(_groups, saved, item => item.Id);
            RefreshGroups();
            RefreshCustomerLookups();
            SetMessage(saved.IsActive ? "Nhóm khách hàng đã được kích hoạt." : "Nhóm khách hàng đã ngừng hoạt động.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleSupplierCoreAsync(string id, CancellationToken cancellationToken)
    {
        if (!CanWriteSuppliers || IsBusy) return;
        IsBusy = true;
        try
        {
            var current = _suppliers.First(item => item.Id == id);
            var saved = await _service.SetSupplierActiveAsync(
                current.Id,
                !current.IsActive,
                current.UpdatedAt,
                cancellationToken).ConfigureAwait(true);
            Upsert(_suppliers, saved, item => item.Id);
            RefreshSuppliers();
            RaiseCounts();
            SetMessage(saved.IsActive ? "Nhà cung cấp đã được đưa vào sử dụng." : "Nhà cung cấp đã ngừng sử dụng.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadCustomerMasterAsync(CancellationToken cancellationToken)
    {
        if (!CanReadCustomers) return;
        Replace(_customers, await _service.ListCustomersAsync(cancellationToken).ConfigureAwait(true));
        Replace(_groups, await _service.ListCustomerGroupsAsync(cancellationToken).ConfigureAwait(true));
        RefreshPresentation();
    }

    private void OpenEditor(EditorKind kind, bool create, string? id, string? idempotencyScope)
    {
        _editorKind = kind;
        _isCreateMode = create;
        _editingId = id;
        _editorIdempotencyKey = create
            ? _idempotencyKeys.Create(idempotencyScope ?? "partner-create")
            : null;
        SetMessage(string.Empty, false);
        RaiseEditorState();
    }

    private bool ValidateEditor(out string message)
    {
        if ((IsCustomerEditor || IsCustomerGroupEditor || IsSupplierEditor || IsSupplierContactEditor)
            && string.IsNullOrWhiteSpace(DraftName))
        {
            message = IsSupplierContactEditor ? "Tên người liên hệ là bắt buộc." : "Tên là bắt buộc.";
            return false;
        }

        if (IsCreateMode
            && (IsCustomerEditor || IsCustomerGroupEditor || IsSupplierEditor)
            && string.IsNullOrWhiteSpace(DraftCode))
        {
            message = "Mã là bắt buộc.";
            return false;
        }

        if (IsCreateMode && IsCustomerEditor
            && (string.IsNullOrWhiteSpace(DraftLabel)
                || string.IsNullOrWhiteSpace(DraftAddressLine1)
                || string.IsNullOrWhiteSpace(DraftProvince)
                || string.IsNullOrWhiteSpace(DraftWard)))
        {
            message = "Địa chỉ mặc định cần nhãn, số nhà/tên đường, tỉnh/thành và phường/xã.";
            return false;
        }

        if (IsCustomerAddressEditor
            && (string.IsNullOrWhiteSpace(DraftLabel) || string.IsNullOrWhiteSpace(DraftAddressLine1)))
        {
            message = "Nhãn địa chỉ và số nhà/tên đường là bắt buộc.";
            return false;
        }

        if (IsCreateMode && IsSupplierEditor
            && (string.IsNullOrWhiteSpace(DraftAddressLine1)
                || string.IsNullOrWhiteSpace(DraftProvince)
                || string.IsNullOrWhiteSpace(DraftWard)))
        {
            message = "Địa chỉ mặc định cần địa chỉ chi tiết, tỉnh/thành phố và xã/phường/đặc khu.";
            return false;
        }

        if (IsSupplierAddressEditor && string.IsNullOrWhiteSpace(DraftAddressLine1))
        {
            message = "Địa chỉ chi tiết là bắt buộc.";
            return false;
        }

        if (IsSupplierPaymentTermEditor && string.IsNullOrWhiteSpace(DraftPaymentMethod))
        {
            message = "Phương thức thanh toán là bắt buộc.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void BuildBulkMappings()
    {
        BulkMappings.Clear();
        if (_bulkMatrix.Count == 0) return;

        var columnCount = _bulkMatrix.Max(row => row.Length);
        var headers = BulkHasHeader ? _bulkMatrix[0] : [];
        var defaults = IsBulkImport
            ? new[] { "NAME", "PHONE", "GROUP_CODE", "RESPONSIBLE_EMPLOYEE_CODE", "EMAIL", "TAX_CODE", "PAYMENT_TERMS_DAYS", "CREDIT_LIMIT", "NOTES" }
            : new[] { "CUSTOMER_CODE", "NAME", "PHONE", "GROUP_CODE", "RESPONSIBLE_EMPLOYEE_CODE", "EMAIL", "TAX_CODE", "PAYMENT_TERMS_DAYS", "CREDIT_LIMIT", "NOTES" };

        var used = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < columnCount; index++)
        {
            string mapping;
            if (!IsBulkImport && index == 0)
            {
                mapping = "CUSTOMER_CODE";
            }
            else
            {
                var header = index < headers.Length ? headers[index] : string.Empty;
                var fromHeader = PartnerPresentation.MappingFromHeader(header);
                var candidate = fromHeader != "IGNORE"
                    ? fromHeader
                    : index < defaults.Length
                        ? defaults[index]
                        : "IGNORE";

                mapping = candidate != "IGNORE" && used.Contains(candidate) ? "IGNORE" : candidate;
            }

            if (mapping != "IGNORE") used.Add(mapping);

            var firstDataRow = BulkHasHeader ? _bulkMatrix.Skip(1).FirstOrDefault() : _bulkMatrix.FirstOrDefault();
            var row = new BulkColumnMappingRow
            {
                Index = index,
                SourceHeader = index < headers.Length ? headers[index] : $"Cột {index + 1}",
                PreviewValue = firstDataRow is not null && index < firstDataRow.Length ? firstDataRow[index] : string.Empty,
                Mapping = mapping
            };
            row.MappingChanged += (_, _) =>
            {
                if (IsBulkImport)
                {
                    BuildBulkSourcePreview();
                }
                ResetBulkPreview();
            };
            BulkMappings.Add(row);
        }
    }

    private CustomerBulkSourceRow[] BuildBulkSourceRows(bool includeExpectedVersions)
    {
        return (BulkHasHeader ? _bulkMatrix.Skip(1) : _bulkMatrix)
            .Select((cells, index) => new CustomerBulkSourceRow
            {
                RowNumber = index + (BulkHasHeader ? 2 : 1),
                Cells = cells,
                ExpectedUpdatedAt = includeExpectedVersions
                    ? BulkPreviewRows.FirstOrDefault(row => row.RowNumber == index + (BulkHasHeader ? 2 : 1))?.Source.ExpectedUpdatedAt
                    : null
            })
            .Where(row => row.Cells.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .ToArray();
    }

    private void BuildBulkSourcePreview(CustomerIdentifyResult? identification = null)
    {
        if (_bulkMatrix.Count == 0)
        {
            BulkSourceRows.Clear();
            BulkSourceSummary = string.Empty;
            return;
        }

        var rows = BuildBulkSourceRows(includeExpectedVersions: false);
        var identifiedByRow = identification?.Rows.ToDictionary(row => row.RowNumber)
            ?? new Dictionary<int, CustomerBulkResultRow>();
        var nameIndex = BulkMappings.FirstOrDefault(item => item.Mapping == "NAME")?.Index ?? -1;
        var codeIndex = BulkMappings.FirstOrDefault(item => item.Mapping == "CUSTOMER_CODE")?.Index ?? -1;

        ResetCollection(
            BulkSourceRows,
            rows.Select(row =>
            {
                identifiedByRow.TryGetValue(row.RowNumber, out var identified);
                var sourceCode = codeIndex >= 0 && codeIndex < row.Cells.Length ? row.Cells[codeIndex] : string.Empty;
                var sourceName = nameIndex >= 0 && nameIndex < row.Cells.Length ? row.Cells[nameIndex] : string.Empty;
                var result = IsBulkImport
                    ? "Chờ xem trước"
                    : identified is null
                        ? "Chưa nhận diện"
                        : identified.Errors.FirstOrDefault()?.Message ?? "Đã nhận diện";
                return new BulkSourcePreviewRow(
                    row.RowNumber,
                    string.Join(" · ", row.Cells.Select(value => string.IsNullOrWhiteSpace(value) ? "Trống" : value)),
                    string.IsNullOrWhiteSpace(identified?.CustomerCode) ? (string.IsNullOrWhiteSpace(sourceCode) ? "—" : sourceCode) : identified.CustomerCode,
                    string.IsNullOrWhiteSpace(identified?.CustomerName) ? (string.IsNullOrWhiteSpace(sourceName) ? "—" : sourceName) : identified.CustomerName,
                    result);
            }));

        var columnCount = _bulkMatrix.Max(row => row.Length);
        BulkSourceSummary = $"{rows.Length} dòng · {columnCount} cột";
        if (identification is not null)
        {
            BulkSourceSummary += $" · {identification.Identified} khách đã nhận diện · {identification.Skipped} dòng mã có lỗi";
        }
    }

    private async Task IdentifyBulkCustomersCoreAsync(CancellationToken cancellationToken)
    {
        if (IsBulkImport || _bulkMatrix.Count == 0)
        {
            _bulkIdentificationReady = IsBulkImport;
            BuildBulkSourcePreview();
            return;
        }

        try
        {
            var result = await _service.IdentifyCustomersAsync(
                BuildBulkSourceRows(includeExpectedVersions: false),
                cancellationToken).ConfigureAwait(true);
            _bulkIdentificationReady = true;
            BuildBulkSourcePreview(result);
            SetMessage($"Đã nhận diện {result.Identified} khách hàng; {result.Skipped} dòng mã có lỗi.", false);
        }
        catch (Exception exception)
        {
            _bulkIdentificationReady = false;
            BuildBulkSourcePreview();
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
    }

    private CustomerBulkRequest BuildBulkRequest(bool dryRun, bool includeExpectedVersions)
    {
        return new CustomerBulkRequest
        {
            DryRun = dryRun,
            Mappings = BulkMappings.OrderBy(item => item.Index).Select(item => item.Mapping).ToArray(),
            Rows = BuildBulkSourceRows(includeExpectedVersions)
        };
    }

    private bool ValidateBulkMappings(IReadOnlyList<string> mappings, out string message)
    {
        var nonIgnore = mappings.Where(mapping => mapping != "IGNORE").ToArray();
        if (nonIgnore.Length != nonIgnore.Distinct(StringComparer.Ordinal).Count())
        {
            message = "Một trường chỉ được chọn cho một cột.";
            return false;
        }

        if (IsBulkImport && !mappings.Contains("NAME", StringComparer.Ordinal))
        {
            message = "Cần chọn cột Tên khách hàng.";
            return false;
        }

        if (!IsBulkImport)
        {
            if (mappings.Count == 0 || mappings[0] != "CUSTOMER_CODE")
            {
                message = "Cột đầu tiên phải là Mã khách hàng.";
                return false;
            }

            if (!mappings.Skip(1).Any(mapping => mapping != "IGNORE"))
            {
                message = "Chọn ít nhất một trường cần cập nhật.";
                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    private void ShowBulkResult(CustomerBulkResult result, bool applied)
    {
        ResetCollection(
            BulkPreviewRows,
            result.Rows.Select(row => new BulkPreviewRow(
                row.RowNumber,
                string.IsNullOrWhiteSpace(row.CustomerCode) ? "—" : row.CustomerCode,
                string.IsNullOrWhiteSpace(row.CustomerName) ? "—" : row.CustomerName,
                row.Changes.Length == 0
                    ? "—"
                    : string.Join("; ", row.Changes.Select(change => $"{change.Label}: {change.OldValue} → {change.NewValue}")),
                PartnerPresentation.BulkResultText(row, applied, IsBulkImport),
                row)));
    }

    private void ResetBulkPreview()
    {
        BulkPreviewRows.Clear();
        _bulkOperationKey = null;
        _bulkApplied = false;
        BulkSummary = string.Empty;
        OnPropertyChanged(nameof(CanApplyBulk));
    }

    private void RefreshPresentation()
    {
        RefreshCustomerLookups();
        RefreshCustomers();
        RefreshGroups();
        RefreshSuppliers();
        RefreshCustomerAddresses();
        RefreshSupplierChildren();
        RaiseCounts();
    }

    private void RefreshCustomerLookups()
    {
        ResetCollection(
            CustomerGroupOptions,
            _groups
                .Where(group => group.IsActive)
                .OrderBy(group => group.Code)
                .Select(group => new PartnerLookupOption(group.Id, $"{group.Code} · {group.Name}")));

        var groupFilters = new List<PartnerLookupOption>
        {
            new("all", "Tất cả nhóm"),
            new("unassigned", "Chưa phân nhóm")
        };
        groupFilters.AddRange(_groups.OrderBy(group => group.Code).Select(group => new PartnerLookupOption(group.Id, $"{group.Code} · {group.Name}")));
        ResetCollection(CustomerGroupFilterOptions, groupFilters);

        ResetCollection(
            EmployeeOptions,
            _employees
                .Where(employee => employee.IsActive)
                .OrderBy(employee => employee.Code)
                .Select(employee => new PartnerLookupOption(employee.Id, $"{employee.Code} · {employee.FullName}")));

        var employeeFilters = new List<PartnerLookupOption>
        {
            new("all", "Tất cả phụ trách"),
            new("unassigned", "Chưa giao phụ trách")
        };
        employeeFilters.AddRange(_employees.OrderBy(employee => employee.Code).Select(employee => new PartnerLookupOption(employee.Id, $"{employee.Code} · {employee.FullName}")));
        ResetCollection(CustomerEmployeeFilterOptions, employeeFilters);
    }

    private void RefreshCustomers()
    {
        var search = PartnerPresentation.NormalizeSearch(CustomerSearch);
        ResetCollection(
            CustomerRows,
            _customers
                .Where(customer =>
                {
                    var statusMatch = PartnerPresentation.MatchesStatus(customer.IsActive, CustomerStatus);
                    var groupMatch = CustomerGroupFilter switch
                    {
                        "all" => true,
                        "unassigned" => string.IsNullOrWhiteSpace(customer.GroupId),
                        _ => customer.GroupId == CustomerGroupFilter
                    };
                    var employeeMatch = CustomerEmployeeFilter switch
                    {
                        "all" => true,
                        "unassigned" => string.IsNullOrWhiteSpace(customer.ResponsibleEmployeeId),
                        _ => customer.ResponsibleEmployeeId == CustomerEmployeeFilter
                    };
                    return statusMatch
                        && groupMatch
                        && employeeMatch
                        && PartnerPresentation.MatchesSearch(
                            search,
                            customer.Code,
                            customer.Name,
                            customer.Phone,
                            customer.Email,
                            customer.TaxCode,
                            customer.GroupName,
                            customer.ResponsibleEmployeeName);
                })
                .OrderBy(customer => customer.Code)
                .Select((customer, index) => new CustomerRow(
                    customer.Id,
                    index + 1,
                    customer.Code,
                    customer.Name,
                    customer.GroupName ?? "Chưa phân nhóm",
                    customer.ResponsibleEmployeeName ?? "Chưa giao phụ trách",
                    string.Join(" · ", new[] { customer.Phone, customer.Email, customer.TaxCode }.Where(value => !string.IsNullOrWhiteSpace(value))),
                    $"{customer.PaymentTermsDays} ngày · {PartnerPresentation.FormatMoney(customer.CreditLimit)}",
                    PartnerPresentation.Status(customer.IsActive),
                    PartnerPresentation.FormatDateTime(customer.UpdatedAt),
                    customer)));
        OnPropertyChanged(nameof(CustomerVisibleSummary));
        OnPropertyChanged(nameof(CustomerHasNoRows));
    }

    private void RefreshGroups()
    {
        var search = PartnerPresentation.NormalizeSearch(GroupSearch);
        ResetCollection(
            CustomerGroupRows,
            _groups
                .Where(group =>
                    PartnerPresentation.MatchesStatus(group.IsActive, GroupStatus)
                    && PartnerPresentation.MatchesSearch(search, group.Code, group.Name, group.Description))
                .OrderBy(group => group.Code)
                .Select((group, index) => new CustomerGroupRow(
                    group.Id,
                    index + 1,
                    group.Code,
                    group.Name,
                    group.Description ?? "—",
                    PartnerPresentation.Status(group.IsActive),
                    PartnerPresentation.FormatDateTime(group.UpdatedAt),
                    group)));
        OnPropertyChanged(nameof(CustomerGroupSummary));
        OnPropertyChanged(nameof(CustomerGroupHasNoRows));
    }

    private void RefreshCustomerAddresses()
    {
        ResetCollection(
            CustomerAddressRows,
            _customerAddresses
                .OrderByDescending(address => address.IsDefault)
                .ThenBy(address => address.Label)
                .Select(address => new CustomerAddressRow(
                    address.Id,
                    address.Label,
                    address.RecipientName ?? "—",
                    PartnerPresentation.CustomerAddress(address),
                    address.Phone ?? "—",
                    address.LocationUrl ?? "—",
                    address.IsDefault ? "Mặc định" : "—",
                    PartnerPresentation.Status(address.IsActive),
                    address)));
        OnPropertyChanged(nameof(CustomerOverviewDefaultAddress));
        OnPropertyChanged(nameof(CustomerOverviewAddressLabel));
        OnPropertyChanged(nameof(CustomerOverviewAddressText));
    }

    private void RaiseCustomerOverviewState()
    {
        OnPropertyChanged(nameof(HasCustomerOverview));
        OnPropertyChanged(nameof(CustomerOverviewStatus));
        OnPropertyChanged(nameof(CustomerOverviewCode));
        OnPropertyChanged(nameof(CustomerOverviewName));
        OnPropertyChanged(nameof(CustomerOverviewPhone));
        OnPropertyChanged(nameof(CustomerOverviewEmail));
        OnPropertyChanged(nameof(CustomerOverviewTaxCode));
        OnPropertyChanged(nameof(CustomerOverviewPaymentTerms));
        OnPropertyChanged(nameof(CustomerOverviewCreditLimit));
        OnPropertyChanged(nameof(CustomerOverviewAddressLabel));
        OnPropertyChanged(nameof(CustomerOverviewAddressText));
        OnPropertyChanged(nameof(CustomerOverviewGroup));
        OnPropertyChanged(nameof(CustomerOverviewEmployee));
        OnPropertyChanged(nameof(CustomerOverviewContact));
        OnPropertyChanged(nameof(CustomerOverviewRevenue));
        OnPropertyChanged(nameof(CustomerOverviewOrderCount));
        OnPropertyChanged(nameof(CustomerOverviewLastPurchase));
        OnPropertyChanged(nameof(CustomerOverviewReceivable));
        OnPropertyChanged(nameof(CustomerOverviewOpenAmount));
        OnPropertyChanged(nameof(CustomerOverviewOpenDocuments));
        OnPropertyChanged(nameof(CustomerOverviewCredit));
        OnPropertyChanged(nameof(CustomerOverviewNotes));
        OnPropertyChanged(nameof(CustomerSalesAllowed));
        OnPropertyChanged(nameof(CustomerReceivableAllowed));
        RaiseCustomerHistoryState();
    }

    private async Task LoadCustomerPurchasedItemsAsync(bool resetOffset, CancellationToken cancellationToken)
    {
        if (!CustomerSalesAllowed)
        {
            CustomerPurchasedItemRows.Clear();
            _customerPurchasedTotal = 0;
            RaiseCustomerHistoryState();
            SetMessage("Tài khoản không có quyền xem lịch sử mua hàng của khách hàng này.", false);
            return;
        }

        if (resetOffset) _customerPurchasedOffset = 0;
        IsBusy = true;
        try
        {
            var page = await _service.GetCustomerPurchasedItemsAsync(
                SelectedCustomerId,
                CustomerOverviewPeriod,
                CustomerPurchasedSearch,
                50,
                _customerPurchasedOffset,
                cancellationToken).ConfigureAwait(true);

            _customerPurchasedTotal = long.TryParse(page.Total, out var purchasedTotal)
                ? purchasedTotal
                : page.Items.Length;
            ResetCollection(
                CustomerPurchasedItemRows,
                page.Items.Select(item => new CustomerPurchasedItemRow(
                    item.Sku,
                    item.ProductName,
                    item.UnitCode,
                    PartnerPresentation.FormatDecimal(item.TotalQuantity),
                    PartnerPresentation.FormatMoney(item.Revenue),
                    item.PurchaseCount,
                    PartnerPresentation.FormatMoney(item.LastUnitPrice),
                    PartnerPresentation.FormatDateTime(item.LastPurchaseAt))));
            _customerPurchasedHasNext = page.Items.Length == page.Limit && long.TryParse(page.Total, out var total) && _customerPurchasedOffset + page.Items.Length < total;
            RaiseCustomerHistoryState();
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCustomerOrdersAsync(bool resetOffset, CancellationToken cancellationToken)
    {
        if (!CustomerSalesAllowed)
        {
            CustomerOrderRows.Clear();
            SetMessage("Tài khoản không có quyền xem đơn hàng của khách hàng này.", false);
            return;
        }

        if (resetOffset) _customerOrderOffset = 0;
        IsBusy = true;
        try
        {
            var rows = await _service.ListCustomerOrdersAsync(
                SelectedCustomerId,
                CustomerOrderSearch,
                21,
                _customerOrderOffset,
                cancellationToken).ConfigureAwait(true);
            _customerOrderHasNext = rows.Count > 20;
            ResetCollection(
                CustomerOrderRows,
                rows.Take(20).Select(item => new CustomerOrderRow(
                    item.Id,
                    item.Number ?? "Chưa cấp số",
                    PartnerPresentation.FormatDateTime(item.ConfirmedAt ?? item.CreatedAt),
                    PartnerPresentation.FormatMoney(item.Total),
                    PartnerPresentation.OrderStatus(item.Status),
                    PartnerPresentation.SettlementStatus(item.SettlementStatus),
                    PartnerPresentation.DeliveryStatus(item.DeliveryStatus),
                    PartnerPresentation.FormatMoney(item.ReceivableRemainingAmount))));
            RaiseCustomerHistoryState();
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCustomerFinanceAsync(bool resetOffset, CancellationToken cancellationToken)
    {
        if (resetOffset)
        {
            _customerReceivableOffset = 0;
            _customerPaymentOffset = 0;
        }

        IsBusy = true;
        try
        {
            if (CustomerReceivableAllowed)
            {
                var receivables = await _service.ListCustomerReceivablesAsync(
                    SelectedCustomerId, 21, _customerReceivableOffset, cancellationToken).ConfigureAwait(true);
                _customerReceivableHasNext = receivables.Count > 20;
                ResetCollection(
                    CustomerReceivableRows,
                    receivables.Take(20).Select(item => new CustomerReceivableRow(
                        item.Id,
                        item.SourceDocumentNumber,
                        PartnerPresentation.FormatDateTime(item.SourceDocumentDate),
                        PartnerPresentation.FormatMoney(item.OriginalAmount),
                        PartnerPresentation.FormatMoney(item.AllocatedAmount),
                        PartnerPresentation.FormatMoney(item.RemainingAmount),
                        PartnerPresentation.ReceivableStatus(item.Status))));
            }
            else
            {
                CustomerReceivableRows.Clear();
                _customerReceivableHasNext = false;
            }

            try
            {
                var payments = await _service.ListCustomerPaymentsAsync(
                    SelectedCustomerId, 21, _customerPaymentOffset, cancellationToken).ConfigureAwait(true);
                _customerPaymentAccess = "allowed";
                _customerPaymentHasNext = payments.Count > 20;
                ResetCollection(
                    CustomerPaymentRows,
                    payments.Take(20).Select(item => new CustomerPaymentRow(
                        item.Id,
                        item.DocumentNumber,
                        PartnerPresentation.FormatDateTime(item.PaymentDate),
                        PartnerPresentation.PaymentMethod(item.PaymentMethod),
                        PartnerPresentation.FormatMoney(item.OriginalAmount),
                        PartnerPresentation.FormatMoney(item.AllocatedAmount),
                        PartnerPresentation.FormatMoney(item.RemainingAmount),
                        PartnerPresentation.PaymentStatus(item.Status))));
            }
            catch (CanonicalApiException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
            {
                _customerPaymentAccess = "forbidden";
                _customerPaymentHasNext = false;
                CustomerPaymentRows.Clear();
            }

            RaiseCustomerHistoryState();
        }
        catch (Exception exception)
        {
            _customerPaymentAccess = "error";
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
            RaiseCustomerHistoryState();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCustomerDeliveryReturnsAsync(bool resetOffset, CancellationToken cancellationToken)
    {
        if (resetOffset)
        {
            _customerDeliveryOffset = 0;
            _customerReturnOffset = 0;
        }

        IsBusy = true;
        try
        {
            _customerDeliveryReturns = await _service.GetCustomerDeliveryReturnsAsync(
                SelectedCustomerId,
                20,
                _customerDeliveryOffset,
                20,
                _customerReturnOffset,
                cancellationToken).ConfigureAwait(true);

            ResetCollection(
                CustomerDeliveryRows,
                _customerDeliveryReturns.Deliveries.Items.Select(item => new CustomerDeliveryRow(
                    item.Id,
                    item.Number ?? "Chưa cấp số",
                    item.SalesOrderNumber ?? "Mở đơn bán",
                    PartnerPresentation.FormatDateTime(item.CreatedAt),
                    PartnerPresentation.HandoverMode(item.HandoverMode),
                    PartnerPresentation.DeliveryStatus(item.Status),
                    _customerDeliveryReturns.Permissions.DeliveryAttempts ? PartnerPresentation.AttemptSummary(item.Attempts) : "Không có quyền xem",
                    item.LineCount.ToString(CultureInfo.InvariantCulture))));

            ResetCollection(
                CustomerReturnRows,
                _customerDeliveryReturns.Returns.Items.Select(item => new CustomerReturnRow(
                    item.Id,
                    item.Number ?? "Chưa cấp số",
                    PartnerPresentation.FormatDateTime(item.ReceivedAt ?? item.CreatedAt),
                    item.WarehouseName ?? item.WarehouseCode ?? "—",
                    PartnerPresentation.ReturnStatus(item.Status),
                    item.LineCount.ToString(CultureInfo.InvariantCulture),
                    item.AcceptedLineCount.ToString(CultureInfo.InvariantCulture),
                    item.CancellationReason ?? item.Note ?? "—")));

            RaiseCustomerHistoryState();
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCustomerInfoAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var addressesTask = _service.ListCustomerAddressesAsync(SelectedCustomerId, cancellationToken);
            var mediaTask = _service.ListCustomerMediaAsync(SelectedCustomerId, cancellationToken);
            await Task.WhenAll(addressesTask, mediaTask).ConfigureAwait(true);
            Replace(_customerAddresses, await addressesTask.ConfigureAwait(true));
            RefreshCustomerAddresses();
            await ReloadCustomerMediaFromResultAsync(await mediaTask.ConfigureAwait(true), cancellationToken).ConfigureAwait(true);
            SetMessage($"Đã tải thông tin và địa chỉ của {SelectedCustomerLabel}.", false);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(exception, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseCustomerHistoryState()
    {
        OnPropertyChanged(nameof(CustomerSalesAllowed));
        OnPropertyChanged(nameof(CustomerReceivableAllowed));
        OnPropertyChanged(nameof(CustomerPaymentAllowed));
        OnPropertyChanged(nameof(CustomerPaymentAccessText));
        OnPropertyChanged(nameof(CustomerPurchasedHasPrevious));
        OnPropertyChanged(nameof(CustomerPurchasedHasNext));
        OnPropertyChanged(nameof(CustomerOrderHasPrevious));
        OnPropertyChanged(nameof(CustomerOrderHasNext));
        OnPropertyChanged(nameof(CustomerReceivableHasPrevious));
        OnPropertyChanged(nameof(CustomerReceivableHasNext));
        OnPropertyChanged(nameof(CustomerPaymentHasPrevious));
        OnPropertyChanged(nameof(CustomerPaymentHasNext));
        OnPropertyChanged(nameof(CustomerDeliveryHasPrevious));
        OnPropertyChanged(nameof(CustomerDeliveryHasNext));
        OnPropertyChanged(nameof(CustomerReturnHasPrevious));
        OnPropertyChanged(nameof(CustomerReturnHasNext));
        OnPropertyChanged(nameof(CustomerPurchasedSummary));
        OnPropertyChanged(nameof(CustomerPurchasedEmptyText));
        OnPropertyChanged(nameof(CustomerOrderEmptyText));
        OnPropertyChanged(nameof(CustomerReceivableEmptyText));
        OnPropertyChanged(nameof(CustomerPaymentEmptyText));
        OnPropertyChanged(nameof(CustomerDeliveryEmptyText));
        OnPropertyChanged(nameof(CustomerReturnEmptyText));
        OnPropertyChanged(nameof(CustomerPurchasedPageText));
        OnPropertyChanged(nameof(CustomerOrderPageText));
        OnPropertyChanged(nameof(CustomerReceivablePageText));
        OnPropertyChanged(nameof(CustomerPaymentPageText));
    }

    private async Task ReloadCustomerMediaAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SelectedCustomerId))
        {
            CustomerMediaRows.Clear();
            _customerMediaMaxPhotos = 3;
            RaiseCustomerMediaState();
            return;
        }

        var result = await _service.ListCustomerMediaAsync(
            SelectedCustomerId,
            cancellationToken).ConfigureAwait(true);
        await ReloadCustomerMediaFromResultAsync(result, cancellationToken).ConfigureAwait(true);
    }

    private async Task ReloadCustomerMediaFromResultAsync(
        CustomerMediaListData result,
        CancellationToken cancellationToken)
    {
        _customerMediaMaxPhotos = result.MaxPhotos > 0 ? result.MaxPhotos : 3;
        var rows = new List<CustomerMediaRow>();

        foreach (var item in result.Media.Take(_customerMediaMaxPhotos))
        {
            BitmapImage? image = null;
            if (!string.IsNullOrWhiteSpace(item.ViewUrl))
            {
                try
                {
                    var bytes = await _mediaTransfer.DownloadAsync(
                        item.ViewUrl,
                        cancellationToken).ConfigureAwait(true);
                    image = CreateBitmap(bytes);
                }
                catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
                {
                    image = null;
                }
            }

            var size = item.ActualByteSize is null
                ? "Chưa rõ dung lượng"
                : $"{item.ActualByteSize.Value / 1024d:N0} KB";
            var dimensions = item.Width is null || item.Height is null
                ? "Chưa rõ kích thước"
                : $"{item.Width}×{item.Height}px";

            rows.Add(new CustomerMediaRow(
                item.Id,
                image,
                item.SourceApp == "MCP" ? "MCP Thị trường" : "Công Ty",
                $"{dimensions} · {size} · {PartnerPresentation.FormatDateTime(item.CapturedAt)}",
                item));
        }

        ResetCollection(CustomerMediaRows, rows);
        RaiseCustomerMediaState();
    }

    private static BitmapImage CreateBitmap(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void RaiseCustomerMediaState()
    {
        OnPropertyChanged(nameof(CustomerMediaSummary));
        OnPropertyChanged(nameof(CanAddCustomerMedia));
    }

    private void RefreshSuppliers()
    {
        var search = PartnerPresentation.NormalizeSearch(SupplierSearch);
        ResetCollection(
            SupplierRows,
            _suppliers
                .Where(supplier =>
                    PartnerPresentation.MatchesStatus(supplier.IsActive, SupplierStatus)
                    && PartnerPresentation.MatchesSearch(
                        search,
                        supplier.Code,
                        supplier.Name,
                        supplier.TaxId,
                        supplier.BankAccount))
                .OrderBy(supplier => supplier.Code)
                .Select((supplier, index) => new SupplierRow(
                    index + 1,
                    supplier.Id,
                    supplier.Code,
                    supplier.Name,
                    supplier.TaxId ?? "—",
                    string.IsNullOrWhiteSpace(supplier.BankName)
                        ? supplier.BankAccount ?? "—"
                        : supplier.BankName,
                    supplier.AvgDeliveryDays is null ? "—" : $"{supplier.AvgDeliveryDays} ngày",
                    PartnerPresentation.SupplierStatus(supplier.IsActive),
                    supplier.IsActive ? "Ngừng sử dụng" : "Đưa vào sử dụng",
                    supplier)));
    }

    private void ClearSupplierChildren()
    {
        _supplierContacts.Clear();
        _supplierAddresses.Clear();
        _supplierPaymentTerms.Clear();
        RefreshSupplierChildren();
    }

    private void RefreshSupplierChildren()
    {
        ResetCollection(
            SupplierContactRows,
            _supplierContacts
                .OrderByDescending(contact => contact.IsPrimary)
                .ThenBy(contact => contact.ContactName)
                .Select(contact => new SupplierContactRow(
                    contact.Id,
                    contact.ContactName,
                    contact.ContactTitle ?? "—",
                    string.Join(" · ", new[] { contact.Phone, contact.Email }.Where(value => !string.IsNullOrWhiteSpace(value))),
                    contact.IsPrimary ? "Chính" : "—",
                    PartnerPresentation.Status(contact.IsActive),
                    contact)));

        ResetCollection(
            SupplierAddressRows,
            _supplierAddresses
                .OrderByDescending(address => address.IsPrimary)
                .ThenBy(address => address.AddressType)
                .Select(address => new SupplierAddressRow(
                    address.Id,
                    address.AddressType,
                    PartnerPresentation.SupplierAddress(address),
                    address.IsPrimary ? "Chính" : "—",
                    PartnerPresentation.Status(address.IsActive),
                    address)));

        ResetCollection(
            SupplierPaymentTermRows,
            _supplierPaymentTerms
                .OrderByDescending(term => term.IsPrimary)
                .ThenBy(term => term.PaymentMethod)
                .Select(term => new SupplierPaymentTermRow(
                    term.Id,
                    term.PaymentMethod,
                    term.TermDays is null ? "—" : $"{term.TermDays} ngày",
                    term.Description ?? "—",
                    term.IsPrimary ? "Chính" : "—",
                    PartnerPresentation.Status(term.IsActive),
                    term)));
    }

    private async Task HandleFailureAsync(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is CanonicalApiException apiException)
        {
            if (apiException.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _authentication.LogoutAsync(cancellationToken).ConfigureAwait(true);
                SetMessage(
                    CanonicalErrorMessages.WithRequestId(
                        "Phiên đăng nhập không còn hiệu lực. Vui lòng đăng nhập lại.",
                        apiException.RequestId),
                    true);
                return;
            }

            var message = apiException.Code switch
            {
                "FORBIDDEN" => "Tài khoản chưa được cấp quyền cho thao tác này.",
                "DUPLICATE_CODE" => "Mã đã tồn tại. Vui lòng dùng mã khác.",
                "CONFLICT" => "Dữ liệu đã thay đổi trên hệ thống. Hãy cập nhật danh sách rồi thực hiện lại.",
                "GROUP_INACTIVE" => "Nhóm khách hàng đang ngừng hoạt động.",
                "EMPLOYEE_INACTIVE" => "Nhân sự phụ trách đang ngừng hoạt động.",
                "CUSTOMER_INACTIVE" => "Khách hàng đang ngừng hoạt động.",
                "SUPPLIER_INACTIVE" => "Nhà cung cấp đang ngừng sử dụng.",
                "INVALID_EXPECTED_UPDATED_AT" => "Mốc đối chiếu dữ liệu không còn hợp lệ. Hãy xem trước lại tệp.",
                "CUSTOMER_BULK_STORAGE_UNAVAILABLE" => "Dữ liệu khách hàng tạm thời chưa sẵn sàng. Vui lòng thử lại sau.",
                "IDEMPOTENCY_STORAGE_ERROR" => "Hệ thống chưa thể bảo đảm chống xử lý trùng. Vui lòng thử lại sau.",
                _ => CanonicalErrorMessages.ToOfficeMessage(apiException)
            };

            SetMessage(CanonicalErrorMessages.WithRequestId(message, apiException.RequestId), true);
            return;
        }

        SetMessage(
            exception is HttpRequestException or TaskCanceledException
                ? "Không kết nối được hệ thống Công Ty. Vui lòng kiểm tra kết nối rồi thử lại."
                : exception.Message,
            true);
    }

    private void RaisePermissionState()
    {
        OnPropertyChanged(nameof(CanReadCustomers));
        OnPropertyChanged(nameof(CanWriteCustomers));
        OnPropertyChanged(nameof(CanReadSuppliers));
        OnPropertyChanged(nameof(CanWriteSuppliers));
        OnPropertyChanged(nameof(CanReadEmployees));
        OnPropertyChanged(nameof(CanViewPartners));
        OnPropertyChanged(nameof(CanAddCustomerMedia));
        OnPropertyChanged(nameof(CanAddSelectedCustomerAddress));
        OnPropertyChanged(nameof(CanManageSelectedSupplierDetails));
    }

    private void RaisePendingCustomerState()
    {
        OnPropertyChanged(nameof(IsCustomerMasterDraftEnabled));
        OnPropertyChanged(nameof(IsCustomerCodeEditable));
        OnPropertyChanged(nameof(HasPendingCreatedCustomer));
        OnPropertyChanged(nameof(PendingCreatedCustomerText));
        OnPropertyChanged(nameof(PartnerEditorPrimaryActionLabel));
    }

    private void RaiseEditorState()
    {
        OnPropertyChanged(nameof(IsCustomerMasterDraftEnabled));
        OnPropertyChanged(nameof(IsCustomerCodeEditable));
        OnPropertyChanged(nameof(HasPendingCreatedCustomer));
        OnPropertyChanged(nameof(PendingCreatedCustomerText));
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsCustomerEditor));
        OnPropertyChanged(nameof(IsCustomerGroupEditor));
        OnPropertyChanged(nameof(IsCustomerAddressEditor));
        OnPropertyChanged(nameof(IsSupplierEditor));
        OnPropertyChanged(nameof(IsSupplierContactEditor));
        OnPropertyChanged(nameof(IsSupplierAddressEditor));
        OnPropertyChanged(nameof(IsSupplierPaymentTermEditor));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(PartnerEditorPrimaryActionLabel));
    }

    private void RaiseCounts()
    {
        OnPropertyChanged(nameof(CustomerTotal));
        OnPropertyChanged(nameof(CustomerActive));
        OnPropertyChanged(nameof(CustomerInactive));
        OnPropertyChanged(nameof(SupplierTotal));
        OnPropertyChanged(nameof(SupplierActive));
        OnPropertyChanged(nameof(SupplierInactive));
    }

    private void SetMessage(string message, bool isError)
    {
        Message = message;
        MessageIsError = isError;
        OnPropertyChanged(nameof(HasMessage));
    }

    private string RequireEditorKey() =>
        !string.IsNullOrWhiteSpace(_editorIdempotencyKey) && _idempotencyKeys.IsValid(_editorIdempotencyKey)
            ? _editorIdempotencyKey
            : throw new InvalidOperationException("Thao tác tạo mới chưa có khóa chống xử lý trùng hợp lệ.");


    private string RequireEditorAddressKey() =>
        !string.IsNullOrWhiteSpace(_editorAddressIdempotencyKey) && _idempotencyKeys.IsValid(_editorAddressIdempotencyKey)
            ? _editorAddressIdempotencyKey
            : throw new InvalidOperationException("Thao tác tạo địa chỉ mặc định chưa có khóa chống xử lý trùng hợp lệ.");

    private CustomerData? FindCustomer(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : _customers.FirstOrDefault(customer => customer.Id == id);

    private SupplierData? FindSupplier(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : _suppliers.FirstOrDefault(supplier => supplier.Id == id);

    private static int ParseRequiredInt(string value, int min, int max, string label)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            || parsed < min
            || parsed > max)
        {
            throw new InvalidOperationException($"{label} phải là số nguyên từ {min} đến {max}.");
        }

        return parsed;
    }

    private static int? ParseOptionalInt(string value, int min, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseRequiredInt(value, min, max, label);
    }

    private static string ParseNonNegativeDecimalString(string value, string label)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 0)
        {
            throw new InvalidOperationException($"{label} phải là số không âm.");
        }

        return parsed.ToString(CultureInfo.InvariantCulture);
    }

    private static void ValidateHttpsLocationUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > 2048
            || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException("Link định vị phải là URL HTTPS hợp lệ.");
        }
    }

    private static string NormalizeCountryCode(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "VN" : value.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(character => !char.IsAsciiLetter(character)))
        {
            throw new InvalidOperationException("Mã quốc gia phải gồm 2 chữ cái.");
        }

        return normalized;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Replace<T>(List<T> target, IEnumerable<T> source)
    {
        target.Clear();
        target.AddRange(source);
    }

    private static void Upsert<T>(List<T> target, T item, Func<T, string> idSelector)
    {
        var id = idSelector(item);
        var index = target.FindIndex(existing => idSelector(existing) == id);
        if (index < 0)
        {
            target.Add(item);
        }
        else
        {
            target[index] = item;
        }
    }

    private static void ResetCollection<T>(ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values)
        {
            collection.Add(value);
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}


public sealed record BulkSourcePreviewRow(
    int RowNumber,
    string RawValues,
    string CustomerCode,
    string CustomerName,
    string Result);
