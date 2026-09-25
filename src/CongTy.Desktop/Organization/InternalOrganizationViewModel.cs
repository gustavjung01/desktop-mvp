using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Windows;

namespace CongTy.Desktop.Organization;

public sealed class InternalOrganizationViewModel : INotifyPropertyChanged
{
    private const string BranchRead = "core.branch.read";
    private const string BranchWrite = "core.branch.write";
    private const string WarehouseRead = "core.warehouse.read";
    private const string WarehouseWrite = "core.warehouse.write";
    private const string LocationRead = "core.warehouse.location.read";
    private const string LocationWrite = "core.warehouse.location.write";
    private const string EmployeeRead = "core.employee.read";
    private const string EmployeeWrite = "core.employee.write";

    private enum EditorKind
    {
        None,
        Branch,
        Warehouse,
        Location,
        Employee
    }

    private readonly IInternalOrganizationService _service;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly IAuthenticationService _authentication;
    private readonly DesktopSettingsState _settingsState;

    private readonly List<BranchData> _branches = [];
    private readonly List<WarehouseData> _warehouses = [];
    private readonly List<WarehouseLocationData> _locations = [];
    private readonly List<EmployeeData> _employees = [];

    private bool _isLoaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _overviewCheckedAt = "Chưa có thời điểm cập nhật";
    private string? _pendingBranchStatusId;
    private bool _isBranchStatusConfirmOpen;
    private EditorKind _pendingWarehouseStatusKind = EditorKind.None;
    private string? _pendingWarehouseStatusId;
    private bool _isWarehouseStatusConfirmOpen;
    private bool _hasLocationModeDetail;
    private string _locationModeDetailTitle = string.Empty;
    private string _locationModeDetailMeta = string.Empty;
    private string _locationModeDetailSummary = string.Empty;

    private string _branchSearch = string.Empty;
    private string _branchStatus = "all";
    private string _warehouseSearch = string.Empty;
    private string _warehouseStatus = "all";
    private string _locationSearch = string.Empty;
    private string _locationStatus = "all";
    private string _employeeSearch = string.Empty;
    private string _employeeStatus = "all";
    private string _employeeBranchFilter = "all";
    private string _selectedWarehouseId = string.Empty;

    private EditorKind _editorKind;
    private bool _isCreateMode;
    private string? _editingId;
    private string? _editorIdempotencyKey;
    private string _draftCode = string.Empty;
    private string _draftName = string.Empty;
    private string _draftAddress = string.Empty;
    private string _draftPhone = string.Empty;
    private string _draftEmail = string.Empty;
    private string _draftBranchId = string.Empty;
    private string _draftWarehouseId = string.Empty;
    private string _draftType = string.Empty;
    private string _draftJobTitle = string.Empty;
    private bool _draftAllowNegativeStock;

    private string _quickWarehouseBranchId = string.Empty;
    private string _quickWarehouseCode = string.Empty;
    private string _quickWarehouseName = string.Empty;
    private string _quickWarehouseType = "main";
    private bool _quickWarehouseAllowNegativeStock;
    private string? _quickWarehouseIdempotencyKey;

    private bool _isLayoutEditorOpen;
    private string _layoutTargetMode = "MANAGED";
    private string _layoutDestinationLocationId = string.Empty;
    private WarehouseLocationModePreviewData? _layoutPreview;
    private string? _layoutIdempotencyKey;
    private string _layoutPreviewText = string.Empty;
    private string _layoutBlockersText = string.Empty;

    public InternalOrganizationViewModel(
        IInternalOrganizationService service,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access,
        IAuthenticationService authentication,
        DesktopSettingsState settingsState)
    {
        _service = service;
        _idempotencyKeys = idempotencyKeys;
        _access = access;
        _authentication = authentication;
        _settingsState = settingsState;

        _access.Changed += (_, _) => RaisePermissionState();
        _settingsState.Changed += (_, _) => OnPropertyChanged(nameof(CompanyDisplayName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BranchRow> BranchRows { get; } = [];
    public ObservableCollection<WarehouseRow> WarehouseRows { get; } = [];
    public ObservableCollection<LocationRow> LocationRows { get; } = [];
    public ObservableCollection<EmployeeRow> EmployeeRows { get; } = [];
    public ObservableCollection<LookupOption> BranchOptions { get; } = [];
    public ObservableCollection<LookupOption> WarehouseBranchOptions { get; } = [];
    public ObservableCollection<LookupOption> WarehouseOptions { get; } = [];
    public ObservableCollection<LookupOption> ActiveWarehouseOptions { get; } = [];
    public ObservableCollection<LookupOption> StorageLocationOptions { get; } = [];
    public ObservableCollection<LookupOption> EmployeeBranchFilterOptions { get; } = [];
    public ObservableCollection<LocationModeRunRow> LocationModeRuns { get; } = [];
    public ObservableCollection<LocationModeLineRow> LocationModeLines { get; } = [];
    public ObservableCollection<OrganizationOverviewHierarchyRow> OverviewHierarchyRows { get; } = [];
    public ObservableCollection<OrganizationOverviewRecentRow> OverviewRecentRows { get; } = [];

    public IReadOnlyList<LookupOption> StatusOptions => OrganizationPresentation.StatusOptions;
    public IReadOnlyList<LookupOption> WarehouseStatusOptions => OrganizationPresentation.WarehouseStatusOptions;
    public IReadOnlyList<LookupOption> NegativeStockOptions => OrganizationPresentation.NegativeStockOptions;
    public IReadOnlyList<LookupOption> WarehouseTypeOptions => OrganizationPresentation.WarehouseTypeOptions;
    public IReadOnlyList<LookupOption> LocationTypeOptions => OrganizationPresentation.WarehouseLocationTypeOptions;
    public IReadOnlyList<LookupOption> LayoutModeOptions => OrganizationPresentation.LayoutModeOptions;

    public string CompanyDisplayName =>
        string.IsNullOrWhiteSpace(_settingsState.Current.CompanyDisplayName)
            ? "Công Ty"
            : _settingsState.Current.CompanyDisplayName;

    public bool IsLoaded => _isLoaded;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                OnPropertyChanged(nameof(BranchEmptyMessage));
                OnPropertyChanged(nameof(WarehouseEmptyMessage));
                OnPropertyChanged(nameof(WarehouseLayoutEmptyMessage));
                OnPropertyChanged(nameof(LocationModeHistoryEmptyMessage));
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

    public bool CanReadBranches => _access.HasPermission(BranchRead);
    public bool CanWriteBranches => _access.HasPermission(BranchWrite);
    public bool CanReadWarehouses => _access.HasPermission(WarehouseRead);
    public bool CanWriteWarehouses => _access.HasPermission(WarehouseWrite);
    public bool CanReadLocations => _access.HasPermission(LocationRead);
    public bool CanWriteLocations => _access.HasPermission(LocationWrite);
    public bool CanReadEmployees => _access.HasPermission(EmployeeRead);
    public bool CanWriteEmployees => _access.HasPermission(EmployeeWrite);

    public bool CanViewWarehouses => CanReadWarehouses || CanReadLocations;
    public bool CanViewInternalOrganization =>
        _access.HasPermission("core.organization.read")
        || CanReadBranches
        || CanReadWarehouses
        || CanReadLocations;

    public int BranchTotal => _branches.Count;
    public int BranchActive => _branches.Count(item => item.IsActive);
    public int BranchInactive => BranchTotal - BranchActive;
    public string BranchStatusSummary => $"{BranchActive} đang hoạt động · {BranchInactive} ngừng hoạt động";
    public int WarehouseTotal => _warehouses.Count;
    public int WarehouseActive => _warehouses.Count(item => item.IsActive);
    public int WarehouseInactive => WarehouseTotal - WarehouseActive;
    public string WarehouseStatusSummary => $"{WarehouseActive} đang hoạt động · {WarehouseInactive} ngừng hoạt động";
    public int LocationTotal => _locations.Count;
    public int LocationActive => _locations.Count(item => item.IsActive);
    public int LocationInactive => LocationTotal - LocationActive;
    public string LocationStatusSummary => $"{LocationActive} đang hoạt động · {LocationInactive} ngừng hoạt động";
    public int EmployeeTotal => _employees.Count;
    public int EmployeeActive => _employees.Count(item => item.IsActive);
    public int OverviewRecordTotal => BranchTotal + WarehouseTotal + LocationTotal;

    public string OverviewCheckedAt
    {
        get => _overviewCheckedAt;
        private set => SetField(ref _overviewCheckedAt, value);
    }

    public IReadOnlyList<LookupOption> BranchStatusOptions => OrganizationPresentation.BranchStatusOptions;
    public string BranchVisibleSummary => $"{BranchRows.Count} hồ sơ";
    public bool BranchHasNoRows => BranchRows.Count == 0;
    public string BranchEmptyMessage => IsBusy
        ? "Đang tải dữ liệu…"
        : "Không tìm thấy chi nhánh phù hợp.";

    public bool IsBranchStatusConfirmOpen
    {
        get => _isBranchStatusConfirmOpen;
        private set => SetField(ref _isBranchStatusConfirmOpen, value);
    }

    public string BranchStatusConfirmTitle
    {
        get
        {
            var branch = string.IsNullOrWhiteSpace(_pendingBranchStatusId)
                ? null
                : _branches.FirstOrDefault(item => item.Id == _pendingBranchStatusId);
            return branch?.IsActive == true ? "Ngừng sử dụng" : "Đưa vào sử dụng";
        }
    }

    public string BranchStatusConfirmText =>
        $"Bạn muốn {BranchStatusConfirmTitle.ToLowerInvariant()} chi nhánh này?";

    public bool IsSharedEditorOpen => IsEditorOpen && !IsBranchEditor && !IsWarehouseEditor && !IsLocationEditor;

    public string WarehouseVisibleSummary => $"{WarehouseRows.Count} kho";
    public bool WarehouseHasNoRows => WarehouseRows.Count == 0;
    public string WarehouseEmptyMessage => IsBusy
        ? "Đang tải dữ liệu…"
        : "Không tìm thấy kho hàng phù hợp.";

    public bool HasSelectedWarehouse => FindWarehouse(SelectedWarehouseId) is not null;
    public string SelectedWarehouseLocationSummary => $"{LocationRows.Count} khu vực";
    public bool SelectedWarehouseHasNoLocations => HasSelectedWarehouse && LocationRows.Count == 0;
    public string WarehouseLayoutEmptyMessage => IsBusy
        ? "Đang tải dữ liệu…"
        : HasSelectedWarehouse
            ? "Kho này chưa có khu vực trong sơ đồ."
            : "Chọn kho để xem sơ đồ.";

    public bool IsWarehouseStatusConfirmOpen
    {
        get => _isWarehouseStatusConfirmOpen;
        private set => SetField(ref _isWarehouseStatusConfirmOpen, value);
    }

    public string WarehouseStatusConfirmTitle
    {
        get
        {
            var active = PendingWarehouseStatusActive();
            return active == true ? "Ngừng sử dụng" : "Đưa vào sử dụng";
        }
    }

    public string WarehouseStatusConfirmText
    {
        get
        {
            var verb = WarehouseStatusConfirmTitle.ToLowerInvariant();
            return _pendingWarehouseStatusKind == EditorKind.Location
                ? $"Bạn muốn {verb} khu vực trong kho này?"
                : $"Bạn muốn {verb} kho hàng này?";
        }
    }

    public bool LocationModeHistoryHasRuns => LocationModeRuns.Count > 0;
    public string LocationModeHistoryEmptyMessage => IsBusy
        ? "Đang tải lịch sử…"
        : "Kho này chưa có lịch sử thay đổi sơ đồ.";

    public bool HasLocationModeDetail
    {
        get => _hasLocationModeDetail;
        private set => SetField(ref _hasLocationModeDetail, value);
    }

    public string LocationModeDetailTitle
    {
        get => _locationModeDetailTitle;
        private set => SetField(ref _locationModeDetailTitle, value);
    }

    public string LocationModeDetailMeta
    {
        get => _locationModeDetailMeta;
        private set => SetField(ref _locationModeDetailMeta, value);
    }

    public string LocationModeDetailSummary
    {
        get => _locationModeDetailSummary;
        private set => SetField(ref _locationModeDetailSummary, value);
    }

    public string BranchSearch
    {
        get => _branchSearch;
        set
        {
            if (SetField(ref _branchSearch, value))
            {
                RefreshBranches();
            }
        }
    }

    public string BranchStatus
    {
        get => _branchStatus;
        set
        {
            if (SetField(ref _branchStatus, value))
            {
                RefreshBranches();
            }
        }
    }

    public string WarehouseSearch
    {
        get => _warehouseSearch;
        set
        {
            if (SetField(ref _warehouseSearch, value))
            {
                RefreshWarehouses();
            }
        }
    }

    public string WarehouseStatus
    {
        get => _warehouseStatus;
        set
        {
            if (SetField(ref _warehouseStatus, value))
            {
                RefreshWarehouses();
            }
        }
    }

    public string LocationSearch
    {
        get => _locationSearch;
        set
        {
            if (SetField(ref _locationSearch, value))
            {
                RefreshLocations();
            }
        }
    }

    public string LocationStatus
    {
        get => _locationStatus;
        set
        {
            if (SetField(ref _locationStatus, value))
            {
                RefreshLocations();
            }
        }
    }

    public string EmployeeSearch
    {
        get => _employeeSearch;
        set
        {
            if (SetField(ref _employeeSearch, value))
            {
                RefreshEmployees();
            }
        }
    }

    public string EmployeeStatus
    {
        get => _employeeStatus;
        set
        {
            if (SetField(ref _employeeStatus, value))
            {
                RefreshEmployees();
            }
        }
    }

    public string EmployeeBranchFilter
    {
        get => _employeeBranchFilter;
        set
        {
            if (SetField(ref _employeeBranchFilter, value))
            {
                RefreshEmployees();
            }
        }
    }

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            if (SetField(ref _selectedWarehouseId, value))
            {
                RefreshLocations();
                RefreshStorageLocations();
                LocationModeRuns.Clear();
                LocationModeLines.Clear();
                OnPropertyChanged(nameof(SelectedWarehouseLabel));
                OnPropertyChanged(nameof(SelectedWarehouseLayoutModeText));
                OnPropertyChanged(nameof(SelectedWarehouseLocationCount));
                OnPropertyChanged(nameof(SelectedWarehouseBranchText));
                OnPropertyChanged(nameof(CanManageSelectedWarehouseLayout));
                OnPropertyChanged(nameof(CanAddLocationToSelectedWarehouse));
                OnPropertyChanged(nameof(HasSelectedWarehouse));
                OnPropertyChanged(nameof(SelectedWarehouseLocationSummary));
                OnPropertyChanged(nameof(SelectedWarehouseHasNoLocations));
                OnPropertyChanged(nameof(WarehouseLayoutEmptyMessage));
                OnPropertyChanged(nameof(LayoutCurrentDescription));
                ClearLocationModeDetail();
            }
        }
    }

    public string SelectedWarehouseLabel
    {
        get
        {
            var warehouse = FindWarehouse(SelectedWarehouseId);
            return warehouse is null ? "Chưa chọn kho" : $"{warehouse.Code} · {warehouse.Name}";
        }
    }


    public string SelectedWarehouseLayoutModeText
    {
        get
        {
            var warehouse = FindWarehouse(SelectedWarehouseId);
            if (warehouse is null) return "Chưa chọn kho";
            return OrganizationPresentation.LayoutMode(warehouse.LocationManagementMode);
        }
    }

    public int SelectedWarehouseLocationCount =>
        string.IsNullOrWhiteSpace(SelectedWarehouseId)
            ? 0
            : _locations.Count(location => location.WarehouseId == SelectedWarehouseId);

    public string SelectedWarehouseBranchText
    {
        get
        {
            var warehouse = FindWarehouse(SelectedWarehouseId);
            if (warehouse is null) return "—";
            var branch = _branches.FirstOrDefault(item => item.Id == warehouse.BranchId);
            return branch is null ? "Chưa xác định chi nhánh" : $"{branch.Code} · {branch.Name}";
        }
    }

    public bool CanAddLocationToSelectedWarehouse =>
        CanWriteLocations && FindWarehouse(SelectedWarehouseId) is not null;

    public bool CanManageSelectedWarehouseLayout
    {
        get
        {
            if (!CanWriteWarehouses || FindWarehouse(SelectedWarehouseId) is null)
            {
                return false;
            }

            return _access.Current.Roles.Contains("bootstrap", StringComparer.Ordinal)
                || _access.Current.Scopes.WarehouseIds.Contains(SelectedWarehouseId, StringComparer.Ordinal);
        }
    }

    public bool IsEditorOpen => _editorKind != EditorKind.None;
    public bool IsBranchEditor => _editorKind == EditorKind.Branch;
    public bool IsWarehouseEditor => _editorKind == EditorKind.Warehouse;
    public bool IsLocationEditor => _editorKind == EditorKind.Location;
    public bool IsEmployeeEditor => _editorKind == EditorKind.Employee;
    public bool IsCreateMode => _isCreateMode;
    public bool IsEditMode => IsEditorOpen && !IsCreateMode;

    public string EditorTitle => _editorKind switch
    {
        EditorKind.Branch => IsCreateMode ? "Thêm chi nhánh" : "Chỉnh sửa chi nhánh",
        EditorKind.Warehouse => IsCreateMode ? "Thêm kho hàng" : "Chỉnh sửa kho hàng",
        EditorKind.Location => IsCreateMode ? "Thêm khu vực trong kho" : "Chỉnh sửa khu vực",
        EditorKind.Employee => IsCreateMode ? "Thêm nhân sự" : "Chỉnh sửa nhân sự",
        _ => string.Empty
    };

    public string EditorNameLabel => _editorKind switch
    {
        EditorKind.Branch => "Tên chi nhánh",
        EditorKind.Warehouse => "Tên kho",
        EditorKind.Location => "Tên khu vực",
        EditorKind.Employee => "Họ và tên",
        _ => "Tên"
    };

    public string EditorPrimaryActionLabel => _editorKind switch
    {
        EditorKind.Branch when IsCreateMode => "Tạo chi nhánh",
        EditorKind.Warehouse when IsCreateMode => "Thêm kho",
        EditorKind.Location when IsCreateMode => "Thêm khu vực",
        EditorKind.Employee when IsCreateMode => "Thêm nhân sự",
        EditorKind.None => "Lưu",
        _ => "Lưu thay đổi"
    };

    public string DraftCode
    {
        get => _draftCode;
        set => SetField(ref _draftCode, value);
    }

    public string DraftName
    {
        get => _draftName;
        set => SetField(ref _draftName, value);
    }

    public string DraftAddress
    {
        get => _draftAddress;
        set => SetField(ref _draftAddress, value);
    }

    public string DraftPhone
    {
        get => _draftPhone;
        set => SetField(ref _draftPhone, value);
    }

    public string DraftEmail
    {
        get => _draftEmail;
        set => SetField(ref _draftEmail, value);
    }

    public string DraftBranchId
    {
        get => _draftBranchId;
        set => SetField(ref _draftBranchId, value);
    }

    public string DraftWarehouseId
    {
        get => _draftWarehouseId;
        set => SetField(ref _draftWarehouseId, value);
    }

    public string DraftType
    {
        get => _draftType;
        set => SetField(ref _draftType, value);
    }

    public string DraftJobTitle
    {
        get => _draftJobTitle;
        set => SetField(ref _draftJobTitle, value);
    }

    public bool DraftAllowNegativeStock
    {
        get => _draftAllowNegativeStock;
        set
        {
            if (SetField(ref _draftAllowNegativeStock, value))
            {
                OnPropertyChanged(nameof(DraftNegativeStockPolicy));
            }
        }
    }

    public string DraftNegativeStockPolicy
    {
        get => DraftAllowNegativeStock ? "allow" : "deny";
        set => DraftAllowNegativeStock = string.Equals(value, "allow", StringComparison.Ordinal);
    }


    public string QuickWarehouseBranchId
    {
        get => _quickWarehouseBranchId;
        set => SetField(ref _quickWarehouseBranchId, value);
    }

    public string QuickWarehouseCode
    {
        get => _quickWarehouseCode;
        set => SetField(ref _quickWarehouseCode, value);
    }

    public string QuickWarehouseName
    {
        get => _quickWarehouseName;
        set => SetField(ref _quickWarehouseName, value);
    }

    public string QuickWarehouseType
    {
        get => _quickWarehouseType;
        set => SetField(ref _quickWarehouseType, value);
    }

    public bool QuickWarehouseAllowNegativeStock
    {
        get => _quickWarehouseAllowNegativeStock;
        set
        {
            if (SetField(ref _quickWarehouseAllowNegativeStock, value))
            {
                OnPropertyChanged(nameof(QuickWarehouseNegativeStockPolicy));
            }
        }
    }

    public string QuickWarehouseNegativeStockPolicy
    {
        get => QuickWarehouseAllowNegativeStock ? "allow" : "deny";
        set => QuickWarehouseAllowNegativeStock = string.Equals(value, "allow", StringComparison.Ordinal);
    }

    public bool IsLayoutEditorOpen
    {
        get => _isLayoutEditorOpen;
        private set => SetField(ref _isLayoutEditorOpen, value);
    }

    public string LayoutTargetMode
    {
        get => _layoutTargetMode;
        set
        {
            if (SetField(ref _layoutTargetMode, value))
            {
                LayoutDestinationLocationId = string.Empty;
                ClearLayoutPreview();
                OnPropertyChanged(nameof(IsLayoutDestinationRequired));
            }
        }
    }

    public bool IsLayoutDestinationRequired => LayoutTargetMode == "MANAGED";

    public string LayoutDestinationLocationId
    {
        get => _layoutDestinationLocationId;
        set
        {
            if (SetField(ref _layoutDestinationLocationId, value))
            {
                ClearLayoutPreview();
            }
        }
    }

    public string LayoutPreviewText
    {
        get => _layoutPreviewText;
        private set => SetField(ref _layoutPreviewText, value);
    }

    public bool HasLayoutPreview => _layoutPreview is not null;

    public string LayoutPreviewDestinationText =>
        _layoutPreview is null
            ? string.Empty
            : _layoutPreview.TargetMode == "MANAGED"
                ? $"Tồn chung sẽ được chuyển vào {_layoutPreview.DestinationLocation?.Code ?? "khu vực đã chọn"}."
                : "Tồn tại các khu vực sẽ được gộp về tồn chung.";

    public string LayoutCurrentDescription
    {
        get
        {
            var warehouse = FindWarehouse(SelectedWarehouseId);
            return warehouse is null
                ? string.Empty
                : $"Kho {warehouse.Code} · {warehouse.Name} hiện {OrganizationPresentation.LayoutMode(warehouse.LocationManagementMode).ToLowerInvariant()}. Hệ thống chỉ thay đổi sau khi xem trước và xác nhận.";
        }
    }

    public string LayoutBlockersText
    {
        get => _layoutBlockersText;
        private set => SetField(ref _layoutBlockersText, value);
    }

    public bool CanConfirmLayout => _layoutPreview?.CanConvert == true && !string.IsNullOrWhiteSpace(_layoutIdempotencyKey);

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoaded)
        {
            return;
        }

        await RefreshAsync(cancellationToken).ConfigureAwait(true);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || !CanViewInternalOrganization)
        {
            return;
        }

        IsBusy = true;
        SetMessage("Đang cập nhật dữ liệu tổ chức...", isError: false);

        try
        {
            var branchesTask = CanReadBranches
                ? _service.ListBranchesAsync(cancellationToken)
                : Task.FromResult<IReadOnlyList<BranchData>>([]);
            var warehousesTask = CanReadWarehouses
                ? _service.ListWarehousesAsync(cancellationToken)
                : Task.FromResult<IReadOnlyList<WarehouseData>>([]);
            var locationsTask = CanReadLocations
                ? _service.ListLocationsAsync(cancellationToken)
                : Task.FromResult<IReadOnlyList<WarehouseLocationData>>([]);

            await Task.WhenAll(branchesTask, warehousesTask, locationsTask).ConfigureAwait(true);

            Replace(_branches, await branchesTask.ConfigureAwait(true));
            Replace(_warehouses, await warehousesTask.ConfigureAwait(true));
            Replace(_locations, await locationsTask.ConfigureAwait(true));

            _isLoaded = true;
            OnPropertyChanged(nameof(IsLoaded));
            RefreshPresentation();
            SetMessage("Dữ liệu tổ chức đã được cập nhật.", isError: false);
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

    public void OpenCreateBranch()
    {
        if (!CanWriteBranches) return;
        OpenEditor(EditorKind.Branch, create: true, null);
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftAddress = string.Empty;
        DraftPhone = string.Empty;
        DraftEmail = string.Empty;
    }

    public void OpenEditBranch(string id)
    {
        if (!CanWriteBranches) return;
        var item = _branches.FirstOrDefault(branch => branch.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.Branch, create: false, id);
        DraftCode = item.Code;
        DraftName = item.Name;
        DraftAddress = item.Address ?? string.Empty;
        DraftPhone = item.Phone ?? string.Empty;
        DraftEmail = item.Email ?? string.Empty;
    }

    public void EnterWarehouseLayout()
    {
        LocationSearch = string.Empty;
        LocationStatus = "all";

        if (string.IsNullOrWhiteSpace(SelectedWarehouseId) && _warehouses.Count > 0)
        {
            SelectedWarehouseId = _warehouses.FirstOrDefault(item => item.IsActive)?.Id ?? _warehouses[0].Id;
        }

        RefreshLocations();
    }

    public void OpenCreateWarehouse()
    {
        if (!CanWriteWarehouses) return;
        OpenEditor(EditorKind.Warehouse, create: true, null);
        DraftBranchId = _branches.FirstOrDefault(branch => branch.IsActive)?.Id ?? string.Empty;
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftType = "main";
        DraftAllowNegativeStock = false;
    }


    public void ResetQuickWarehouseSetup()
    {
        QuickWarehouseBranchId = _branches.FirstOrDefault(branch => branch.IsActive)?.Id ?? string.Empty;
        QuickWarehouseCode = string.Empty;
        QuickWarehouseName = string.Empty;
        QuickWarehouseType = "main";
        QuickWarehouseAllowNegativeStock = false;
        _quickWarehouseIdempotencyKey = _idempotencyKeys.Create("warehouse-quick-create");
    }

    public async Task SaveQuickWarehouseAsync(CancellationToken cancellationToken = default)
    {
        if (!CanWriteWarehouses || IsBusy) return;
        if (!Guid.TryParse(QuickWarehouseBranchId, out _))
        {
            SetMessage("Vui lòng chọn chi nhánh quản lý.", isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(QuickWarehouseCode) || string.IsNullOrWhiteSpace(QuickWarehouseName))
        {
            SetMessage("Mã kho và tên kho là bắt buộc.", isError: true);
            return;
        }

        IsBusy = true;
        try
        {
            _quickWarehouseIdempotencyKey ??= _idempotencyKeys.Create("warehouse-quick-create");
            if (!_idempotencyKeys.IsValid(_quickWarehouseIdempotencyKey))
            {
                throw new InvalidOperationException("Thao tác tạo kho nhanh chưa có khóa chống xử lý trùng hợp lệ.");
            }

            var saved = await _service.CreateWarehouseAsync(
                new WarehouseCreateRequest(
                    QuickWarehouseBranchId,
                    QuickWarehouseCode.Trim().ToUpperInvariant(),
                    QuickWarehouseName.Trim(),
                    QuickWarehouseType,
                    QuickWarehouseAllowNegativeStock),
                _quickWarehouseIdempotencyKey,
                cancellationToken).ConfigureAwait(true);

            Upsert(_warehouses, saved, item => item.Id);
            SelectedWarehouseId = saved.Id;
            ResetQuickWarehouseSetup();
            RefreshPresentation();
            SetMessage($"Đã tạo kho {saved.Code} · {saved.Name}. Có thể sang Sơ đồ kho để chia khu vực chứa hàng.", isError: false);
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

    public void OpenEditWarehouse(string id)
    {
        if (!CanWriteWarehouses) return;
        var item = FindWarehouse(id);
        if (item is null) return;
        OpenEditor(EditorKind.Warehouse, create: false, id);
        DraftBranchId = item.BranchId;
        DraftCode = item.Code;
        DraftName = item.Name;
        DraftType = item.WarehouseType;
        DraftAllowNegativeStock = item.AllowNegativeStock;
    }

    public void OpenCreateLocation()
    {
        if (!CanAddLocationToSelectedWarehouse) return;
        OpenEditor(EditorKind.Location, create: true, null);
        DraftWarehouseId = SelectedWarehouseId;
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftType = "storage";
    }

    public void OpenEditLocation(string id)
    {
        if (!CanWriteLocations) return;
        var item = _locations.FirstOrDefault(location => location.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.Location, create: false, id);
        DraftWarehouseId = item.WarehouseId;
        DraftCode = item.Code;
        DraftName = item.Name;
        DraftType = item.LocationType;
    }

    public void OpenCreateEmployee()
    {
        if (!CanWriteEmployees) return;
        OpenEditor(EditorKind.Employee, create: true, null);
        DraftCode = string.Empty;
        DraftName = string.Empty;
        DraftJobTitle = string.Empty;
        DraftBranchId = _branches.FirstOrDefault(branch => branch.IsActive)?.Id ?? string.Empty;
        DraftPhone = string.Empty;
        DraftEmail = string.Empty;
    }

    public void OpenEditEmployee(string id)
    {
        if (!CanWriteEmployees) return;
        var item = _employees.FirstOrDefault(employee => employee.Id == id);
        if (item is null) return;
        OpenEditor(EditorKind.Employee, create: false, id);
        DraftCode = item.Code;
        DraftName = item.FullName;
        DraftJobTitle = item.JobTitle ?? string.Empty;
        DraftBranchId = item.BranchId ?? string.Empty;
        DraftPhone = item.Phone ?? string.Empty;
        DraftEmail = item.Email ?? string.Empty;
    }

    public void CancelEditor()
    {
        _editorKind = EditorKind.None;
        _editingId = null;
        _editorIdempotencyKey = null;
        RaiseEditorState();
    }

    public async Task SaveEditorAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEditorOpen || IsBusy)
        {
            return;
        }

        if (!ValidateEditor(out var validationMessage))
        {
            SetMessage(validationMessage, isError: true);
            return;
        }

        IsBusy = true;
        SetMessage("Đang lưu thay đổi...", isError: false);

        try
        {
            switch (_editorKind)
            {
                case EditorKind.Branch:
                    await SaveBranchAsync(cancellationToken).ConfigureAwait(true);
                    break;
                case EditorKind.Warehouse:
                    await SaveWarehouseAsync(cancellationToken).ConfigureAwait(true);
                    break;
                case EditorKind.Location:
                    await SaveLocationAsync(cancellationToken).ConfigureAwait(true);
                    break;
                case EditorKind.Employee:
                    await SaveEmployeeAsync(cancellationToken).ConfigureAwait(true);
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

    public void OpenBranchStatusConfirm(string id)
    {
        if (!CanWriteBranches)
        {
            return;
        }

        var branch = _branches.FirstOrDefault(item => item.Id == id);
        if (branch is null)
        {
            return;
        }

        SetMessage(string.Empty, isError: false);
        _pendingBranchStatusId = id;
        IsBranchStatusConfirmOpen = true;
        OnPropertyChanged(nameof(BranchStatusConfirmTitle));
        OnPropertyChanged(nameof(BranchStatusConfirmText));
    }

    public void CancelBranchStatusConfirm()
    {
        _pendingBranchStatusId = null;
        IsBranchStatusConfirmOpen = false;
        OnPropertyChanged(nameof(BranchStatusConfirmTitle));
        OnPropertyChanged(nameof(BranchStatusConfirmText));
    }

    public async Task ConfirmBranchStatusAsync(CancellationToken cancellationToken = default)
    {
        var id = _pendingBranchStatusId;
        if (string.IsNullOrWhiteSpace(id) || IsBusy)
        {
            return;
        }

        await ToggleBranchAsync(id, cancellationToken).ConfigureAwait(true);
        if (!MessageIsError)
        {
            CancelBranchStatusConfirm();
        }
    }

    public Task ToggleBranchAsync(string id, CancellationToken cancellationToken = default) =>
        ToggleAsync(EditorKind.Branch, id, cancellationToken);

    public void OpenWarehouseStatusConfirm(string id) =>
        OpenWarehouseStatusConfirm(EditorKind.Warehouse, id);

    public void OpenLocationStatusConfirm(string id) =>
        OpenWarehouseStatusConfirm(EditorKind.Location, id);

    public void CancelWarehouseStatusConfirm()
    {
        _pendingWarehouseStatusKind = EditorKind.None;
        _pendingWarehouseStatusId = null;
        IsWarehouseStatusConfirmOpen = false;
        OnPropertyChanged(nameof(WarehouseStatusConfirmTitle));
        OnPropertyChanged(nameof(WarehouseStatusConfirmText));
    }

    public async Task ConfirmWarehouseStatusAsync(CancellationToken cancellationToken = default)
    {
        var kind = _pendingWarehouseStatusKind;
        var id = _pendingWarehouseStatusId;
        if (kind is not (EditorKind.Warehouse or EditorKind.Location) || string.IsNullOrWhiteSpace(id) || IsBusy)
        {
            return;
        }

        await ToggleAsync(kind, id, cancellationToken).ConfigureAwait(true);
        if (!MessageIsError)
        {
            CancelWarehouseStatusConfirm();
        }
    }

    public Task ToggleWarehouseAsync(string id, CancellationToken cancellationToken = default) =>
        ToggleAsync(EditorKind.Warehouse, id, cancellationToken);

    public Task ToggleLocationAsync(string id, CancellationToken cancellationToken = default) =>
        ToggleAsync(EditorKind.Location, id, cancellationToken);

    public Task ToggleEmployeeAsync(string id, CancellationToken cancellationToken = default) =>
        ToggleAsync(EditorKind.Employee, id, cancellationToken);

    public void OpenLayoutEditor()
    {
        var warehouse = FindWarehouse(SelectedWarehouseId);
        if (warehouse is null || !CanManageSelectedWarehouseLayout)
        {
            return;
        }

        LayoutTargetMode = warehouse.LocationManagementMode == "MANAGED" ? "UNMANAGED" : "MANAGED";
        LayoutDestinationLocationId = string.Empty;
        ClearLayoutPreview();
        OnPropertyChanged(nameof(LayoutCurrentDescription));
        IsLayoutEditorOpen = true;
    }

    public void CloseLayoutEditor()
    {
        IsLayoutEditorOpen = false;
        ClearLayoutPreview();
    }

    public async Task PreviewLayoutAsync(CancellationToken cancellationToken = default)
    {
        if (!IsLayoutEditorOpen || !CanManageSelectedWarehouseLayout || IsBusy)
        {
            return;
        }

        if (IsLayoutDestinationRequired && string.IsNullOrWhiteSpace(LayoutDestinationLocationId))
        {
            SetMessage("Vui lòng chọn khu vực lưu trữ nhận hàng ban đầu.", isError: true);
            return;
        }

        IsBusy = true;
        try
        {
            _layoutPreview = await _service.PreviewLocationModeAsync(
                SelectedWarehouseId,
                LayoutTargetMode,
                string.IsNullOrWhiteSpace(LayoutDestinationLocationId) ? null : LayoutDestinationLocationId,
                cancellationToken).ConfigureAwait(true);

            _layoutIdempotencyKey = _idempotencyKeys.Create("warehouse-location-mode-convert");
            LayoutPreviewText =
                $"{_layoutPreview.Summary.AffectedSkuCount} sản phẩm · {_layoutPreview.Summary.AffectedScopeCount} phạm vi tồn · tổng số lượng {OrganizationPresentation.FormatDecimal(_layoutPreview.Summary.TotalBaseQuantity)}.";
            OnPropertyChanged(nameof(LayoutPreviewDestinationText));
            LayoutBlockersText = _layoutPreview.Blockers.Length == 0
                ? string.Empty
                : string.Join(Environment.NewLine, _layoutPreview.Blockers.Select(item => $"• {item.Message ?? item.Code}"));
            OnPropertyChanged(nameof(HasLayoutPreview));
            OnPropertyChanged(nameof(CanConfirmLayout));
            SetMessage("Đã xem trước thay đổi sơ đồ kho.", isError: !_layoutPreview.CanConvert);
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

    public async Task ConfirmLayoutAsync(CancellationToken cancellationToken = default)
    {
        if (!CanConfirmLayout || _layoutPreview is null || string.IsNullOrWhiteSpace(_layoutIdempotencyKey) || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _service.ConvertLocationModeAsync(
                SelectedWarehouseId,
                new WarehouseLocationModeConvertRequest(
                    LayoutTargetMode,
                    string.IsNullOrWhiteSpace(LayoutDestinationLocationId) ? null : LayoutDestinationLocationId,
                    _layoutPreview.PreviewHash),
                _layoutIdempotencyKey,
                cancellationToken).ConfigureAwait(true);

            IsLayoutEditorOpen = false;
            ClearLayoutPreview();
            await ReloadWarehousesAndLocationsAsync(cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật cách quản lý vị trí của kho.", isError: false);
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

    public async Task LoadLocationModeHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || !CanWriteWarehouses)
        {
            return;
        }

        var selected = FindWarehouse(SelectedWarehouseId);
        if (selected?.IsActive != true)
        {
            SelectedWarehouseId = _warehouses.FirstOrDefault(item => item.IsActive)?.Id ?? string.Empty;
        }

        if (!CanManageSelectedWarehouseLayout || string.IsNullOrWhiteSpace(SelectedWarehouseId))
        {
            LocationModeRuns.Clear();
            ClearLocationModeDetail();
            OnPropertyChanged(nameof(LocationModeHistoryHasRuns));
            OnPropertyChanged(nameof(LocationModeHistoryEmptyMessage));
            return;
        }

        IsBusy = true;
        try
        {
            var runs = await _service.ListLocationModeRunsAsync(SelectedWarehouseId, cancellationToken).ConfigureAwait(true);
            ResetCollection(
                LocationModeRuns,
                runs.Select(run => new LocationModeRunRow(
                    run.Id,
                    OrganizationPresentation.FormatDateTime(run.CompletedAt),
                    run.TargetMode == "UNMANAGED" ? "Chuyển về tồn chung" : "Bắt đầu dùng sơ đồ kho",
                    OrganizationPresentation.ModeTransition(run.FromMode, run.TargetMode),
                    run.AffectedSkuCount.ToString(),
                    run.CompletedBy,
                    run)));
            ClearLocationModeDetail();
            OnPropertyChanged(nameof(LocationModeHistoryHasRuns));
            OnPropertyChanged(nameof(LocationModeHistoryEmptyMessage));
            SetMessage("Đã tải lịch sử sơ đồ kho.", isError: false);
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

    public async Task LoadLocationModeRunDetailAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId) || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var run = await _service.GetLocationModeRunAsync(runId, cancellationToken).ConfigureAwait(true);
            var action = run.TargetMode == "UNMANAGED" ? "Chuyển về tồn chung" : "Bắt đầu dùng sơ đồ kho";
            LocationModeDetailTitle = $"{action} · {run.WarehouseCode} — {run.WarehouseName}";
            LocationModeDetailMeta = $"Thời gian: {OrganizationPresentation.FormatDateTime(run.CompletedAt)} · Người thực hiện: {run.CompletedBy}";
            LocationModeDetailSummary = $"{OrganizationPresentation.ModeTransition(run.FromMode, run.TargetMode)} · {run.AffectedSkuCount} SKU · {run.AffectedScopeCount} phạm vi tồn";
            HasLocationModeDetail = true;
            ResetCollection(
                LocationModeLines,
                (run.Lines ?? []).Select(line => new LocationModeLineRow(
                    line.Sku,
                    line.LotCode ?? "Không lô",
                    OrganizationPresentation.LocationLabel(line.SourceLocationId, line.SourceLocationCode, line.SourceLocationName),
                    OrganizationPresentation.LocationLabel(line.DestinationLocationId, line.DestinationLocationCode, line.DestinationLocationName),
                    OrganizationPresentation.FormatDecimal(line.BaseQuantity))));
            SetMessage($"Chi tiết lần thay đổi lúc {OrganizationPresentation.FormatDateTime(run.CompletedAt)}.", isError: false);
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

    private async Task SaveBranchAsync(CancellationToken cancellationToken)
    {
        BranchData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateBranchAsync(
                new BranchCreateRequest(
                    DraftCode.Trim().ToUpperInvariant(),
                    DraftName.Trim(),
                    NullIfBlank(DraftAddress),
                    NullIfBlank(DraftPhone),
                    NullIfBlank(DraftEmail)),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);
            SetMessage($"Đã tạo chi nhánh {saved.Code} · {saved.Name}.", isError: false);
        }
        else
        {
            var current = _branches.First(item => item.Id == _editingId);
            saved = await _service.UpdateBranchAsync(
                current.Id,
                new BranchUpdateRequest(
                    DraftName.Trim(),
                    NullIfBlank(DraftAddress),
                    NullIfBlank(DraftPhone),
                    NullIfBlank(DraftEmail),
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật chi nhánh.", isError: false);
        }

        Upsert(_branches, saved, item => item.Id);
        _editorIdempotencyKey = null;
    }

    private async Task SaveWarehouseAsync(CancellationToken cancellationToken)
    {
        WarehouseData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateWarehouseAsync(
                new WarehouseCreateRequest(
                    DraftBranchId,
                    DraftCode.Trim().ToUpperInvariant(),
                    DraftName.Trim(),
                    DraftType,
                    DraftAllowNegativeStock),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);
            SelectedWarehouseId = saved.Id;
            SetMessage($"Đã tạo kho {saved.Code} · {saved.Name}.", isError: false);
        }
        else
        {
            var current = _warehouses.First(item => item.Id == _editingId);
            saved = await _service.UpdateWarehouseAsync(
                current.Id,
                new WarehouseUpdateRequest(
                    DraftName.Trim(),
                    DraftType,
                    DraftAllowNegativeStock,
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật kho hàng.", isError: false);
        }

        Upsert(_warehouses, saved, item => item.Id);
        _editorIdempotencyKey = null;
    }

    private async Task SaveLocationAsync(CancellationToken cancellationToken)
    {
        WarehouseLocationData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateLocationAsync(
                new WarehouseLocationCreateRequest(
                    DraftWarehouseId,
                    DraftCode.Trim().ToUpperInvariant(),
                    DraftName.Trim(),
                    DraftType),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);
            SelectedWarehouseId = saved.WarehouseId;
            SetMessage($"Đã thêm khu vực {saved.Code} · {saved.Name}.", isError: false);
        }
        else
        {
            var current = _locations.First(item => item.Id == _editingId);
            saved = await _service.UpdateLocationAsync(
                current.Id,
                new WarehouseLocationUpdateRequest(
                    DraftName.Trim(),
                    DraftType,
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật khu vực trong kho.", isError: false);
        }

        Upsert(_locations, saved, item => item.Id);
        _editorIdempotencyKey = null;
    }

    private async Task SaveEmployeeAsync(CancellationToken cancellationToken)
    {
        EmployeeData saved;
        if (IsCreateMode)
        {
            saved = await _service.CreateEmployeeAsync(
                new EmployeeCreateRequest(
                    DraftCode.Trim().ToUpperInvariant(),
                    DraftName.Trim(),
                    NullIfBlank(DraftJobTitle),
                    NullIfBlank(DraftPhone),
                    NullIfBlank(DraftEmail),
                    NullIfBlank(DraftBranchId)),
                RequireEditorKey(),
                cancellationToken).ConfigureAwait(true);
            SetMessage($"Đã tạo hồ sơ nhân sự {saved.Code} · {saved.FullName}.", isError: false);
        }
        else
        {
            var current = _employees.First(item => item.Id == _editingId);
            saved = await _service.UpdateEmployeeAsync(
                current.Id,
                new EmployeeUpdateRequest(
                    DraftName.Trim(),
                    NullIfBlank(DraftJobTitle),
                    NullIfBlank(DraftPhone),
                    NullIfBlank(DraftEmail),
                    NullIfBlank(DraftBranchId),
                    current.UpdatedAt),
                cancellationToken).ConfigureAwait(true);
            SetMessage("Đã cập nhật hồ sơ nhân sự.", isError: false);
        }

        Upsert(_employees, saved, item => item.Id);
        _editorIdempotencyKey = null;
    }

    private async Task ToggleAsync(EditorKind kind, string id, CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            switch (kind)
            {
                case EditorKind.Branch when CanWriteBranches:
                {
                    var current = _branches.First(item => item.Id == id);
                    var saved = await _service.SetBranchActiveAsync(
                        current.Id,
                        !current.IsActive,
                        current.UpdatedAt,
                        cancellationToken).ConfigureAwait(true);
                    Upsert(_branches, saved, item => item.Id);
                    SetMessage(saved.IsActive ? "Chi nhánh đã được đưa vào hoạt động." : "Chi nhánh đã ngừng hoạt động.", false);
                    break;
                }
                case EditorKind.Warehouse when CanWriteWarehouses:
                {
                    var current = _warehouses.First(item => item.Id == id);
                    var saved = await _service.SetWarehouseActiveAsync(
                        current.Id,
                        !current.IsActive,
                        current.UpdatedAt,
                        cancellationToken).ConfigureAwait(true);
                    Upsert(_warehouses, saved, item => item.Id);
                    SetMessage(saved.IsActive ? "Kho đã được đưa vào sử dụng." : "Kho đã ngừng sử dụng.", false);
                    break;
                }
                case EditorKind.Location when CanWriteLocations:
                {
                    var current = _locations.First(item => item.Id == id);
                    var saved = await _service.SetLocationActiveAsync(
                        current.Id,
                        !current.IsActive,
                        current.UpdatedAt,
                        cancellationToken).ConfigureAwait(true);
                    Upsert(_locations, saved, item => item.Id);
                    SetMessage(saved.IsActive ? "Khu vực đã được đưa vào sử dụng." : "Khu vực đã ngừng sử dụng.", false);
                    break;
                }
                case EditorKind.Employee when CanWriteEmployees:
                {
                    var current = _employees.First(item => item.Id == id);
                    var saved = await _service.SetEmployeeActiveAsync(
                        current.Id,
                        !current.IsActive,
                        current.UpdatedAt,
                        cancellationToken).ConfigureAwait(true);
                    Upsert(_employees, saved, item => item.Id);
                    SetMessage(saved.IsActive ? "Nhân sự đã được đưa trở lại làm việc." : "Nhân sự đã ngừng làm việc.", false);
                    break;
                }
            }

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

    private void OpenEditor(EditorKind kind, bool create, string? id)
    {
        _editorKind = kind;
        _isCreateMode = create;
        _editingId = id;
        _editorIdempotencyKey = create ? _idempotencyKeys.Create("organization-create") : null;
        SetMessage(string.Empty, false);
        RaiseEditorState();
    }

    private bool ValidateEditor(out string message)
    {
        if (string.IsNullOrWhiteSpace(DraftName))
        {
            message = IsEmployeeEditor ? "Họ và tên là bắt buộc." : "Tên là bắt buộc.";
            return false;
        }

        if (IsCreateMode && string.IsNullOrWhiteSpace(DraftCode))
        {
            message = "Mã là bắt buộc.";
            return false;
        }

        if (IsWarehouseEditor && string.IsNullOrWhiteSpace(DraftBranchId))
        {
            message = "Vui lòng chọn chi nhánh quản lý.";
            return false;
        }

        if (IsLocationEditor && string.IsNullOrWhiteSpace(DraftWarehouseId))
        {
            message = "Vui lòng chọn kho.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private async Task ReloadWarehousesAndLocationsAsync(CancellationToken cancellationToken)
    {
        if (CanReadWarehouses)
        {
            Replace(_warehouses, await _service.ListWarehousesAsync(cancellationToken).ConfigureAwait(true));
        }

        if (CanReadLocations)
        {
            Replace(_locations, await _service.ListLocationsAsync(cancellationToken).ConfigureAwait(true));
        }

        RefreshPresentation();
    }

    private void RefreshPresentation()
    {
        RefreshLookups();
        RefreshOverview();
        RefreshBranches();
        RefreshWarehouses();
        RefreshLocations();
        RefreshEmployees();
        RaiseCounts();
        OverviewCheckedAt = OrganizationPresentation.FormatDateTime(DateTimeOffset.Now.ToString("O"));
        OnPropertyChanged(nameof(SelectedWarehouseLabel));
        OnPropertyChanged(nameof(SelectedWarehouseLayoutModeText));
        OnPropertyChanged(nameof(SelectedWarehouseLocationCount));
        OnPropertyChanged(nameof(SelectedWarehouseBranchText));
        OnPropertyChanged(nameof(CanManageSelectedWarehouseLayout));
        OnPropertyChanged(nameof(CanAddLocationToSelectedWarehouse));
        OnPropertyChanged(nameof(LayoutCurrentDescription));
        OnPropertyChanged(nameof(HasSelectedWarehouse));
        OnPropertyChanged(nameof(SelectedWarehouseLocationSummary));
        OnPropertyChanged(nameof(SelectedWarehouseHasNoLocations));
        OnPropertyChanged(nameof(WarehouseLayoutEmptyMessage));
    }

    private void RefreshLookups()
    {
        ResetCollection(
            BranchOptions,
            _branches
                .Where(branch => branch.IsActive)
                .OrderBy(branch => branch.Code)
                .Select(branch => new LookupOption(branch.Id, $"{branch.Code} · {branch.Name}")));

        ResetCollection(
            WarehouseBranchOptions,
            _branches
                .OrderBy(branch => branch.Code)
                .Select(branch => new LookupOption(
                    branch.Id,
                    $"{branch.Code} · {branch.Name}{(branch.IsActive ? string.Empty : " · Ngừng hoạt động")}")));

        ResetCollection(
            WarehouseOptions,
            _warehouses
                .OrderBy(warehouse => warehouse.Code)
                .Select(warehouse => new LookupOption(
                    warehouse.Id,
                    $"{warehouse.Code} · {warehouse.Name}{(warehouse.IsActive ? string.Empty : " · Ngừng sử dụng")}")));

        ResetCollection(
            ActiveWarehouseOptions,
            _warehouses
                .Where(warehouse => warehouse.IsActive)
                .OrderBy(warehouse => warehouse.Code)
                .Select(warehouse => new LookupOption(warehouse.Id, $"{warehouse.Code} · {warehouse.Name}")));

        var employeeFilters = new List<LookupOption>
        {
            new("all", "Tất cả chi nhánh"),
            new("unassigned", "Chưa phân công")
        };
        employeeFilters.AddRange(_branches
            .OrderBy(branch => branch.Code)
            .Select(branch => new LookupOption(branch.Id, $"{branch.Code} · {branch.Name}")));
        ResetCollection(EmployeeBranchFilterOptions, employeeFilters);

        if (string.IsNullOrWhiteSpace(SelectedWarehouseId) && _warehouses.Count > 0)
        {
            _selectedWarehouseId = _warehouses[0].Id;
            OnPropertyChanged(nameof(SelectedWarehouseId));
        }

        if (string.IsNullOrWhiteSpace(QuickWarehouseBranchId) && _branches.Count > 0)
        {
            _quickWarehouseBranchId = _branches.FirstOrDefault(branch => branch.IsActive)?.Id ?? _branches[0].Id;
            OnPropertyChanged(nameof(QuickWarehouseBranchId));
        }

        RefreshStorageLocations();
    }

    private void RefreshStorageLocations()
    {
        ResetCollection(
            StorageLocationOptions,
            _locations
                .Where(location =>
                    location.WarehouseId == SelectedWarehouseId
                    && location.IsActive
                    && location.LocationType == "storage")
                .OrderBy(location => location.Code)
                .Select(location => new LookupOption(location.Id, $"{location.Code} · {location.Name}")));
    }

    private void RefreshOverview()
    {
        ResetCollection(
            OverviewHierarchyRows,
            OrganizationPresentation.BuildOverviewHierarchy(_branches, _warehouses, _locations));
        ResetCollection(
            OverviewRecentRows,
            OrganizationPresentation.BuildOverviewRecent(_branches, _warehouses, _locations));
    }

    private void RefreshBranches()
    {
        var search = OrganizationPresentation.NormalizeSearch(BranchSearch);
        ResetCollection(
            BranchRows,
            _branches
                .Where(branch =>
                    OrganizationPresentation.MatchesStatus(branch.IsActive, BranchStatus)
                    && OrganizationPresentation.MatchesSearch(
                        search,
                        branch.Code,
                        branch.Name,
                        branch.Address,
                        branch.Phone,
                        branch.Email))
                .OrderBy(branch => branch.Code)
                .Select(branch => new BranchRow(
                    branch.Id,
                    branch.Code,
                    branch.Name,
                    string.IsNullOrWhiteSpace(branch.Address) ? "Chưa có địa chỉ" : branch.Address!,
                    string.IsNullOrWhiteSpace(branch.Phone) ? "Chưa có số điện thoại" : branch.Phone!,
                    string.IsNullOrWhiteSpace(branch.Email) ? "Chưa có email" : branch.Email!,
                    OrganizationPresentation.Status(branch.IsActive),
                    OrganizationPresentation.BranchStatusAction(branch.IsActive),
                    branch.IsActive,
                    OrganizationPresentation.FormatDateTime(branch.UpdatedAt),
                    branch)));
        OnPropertyChanged(nameof(BranchVisibleSummary));
        OnPropertyChanged(nameof(BranchHasNoRows));
        OnPropertyChanged(nameof(BranchEmptyMessage));
    }

    private void RefreshWarehouses()
    {
        var search = OrganizationPresentation.NormalizeSearch(WarehouseSearch);
        ResetCollection(
            WarehouseRows,
            _warehouses
                .Where(warehouse =>
                {
                    var branch = _branches.FirstOrDefault(item => item.Id == warehouse.BranchId);
                    return OrganizationPresentation.MatchesStatus(warehouse.IsActive, WarehouseStatus)
                        && OrganizationPresentation.MatchesSearch(
                            search,
                            warehouse.Code,
                            warehouse.Name,
                            warehouse.WarehouseType,
                            branch?.Code,
                            branch?.Name);
                })
                .OrderBy(warehouse => warehouse.Code)
                .Select(warehouse =>
                {
                    var branch = _branches.FirstOrDefault(item => item.Id == warehouse.BranchId);
                    return new WarehouseRow(
                        warehouse.Id,
                        warehouse.Code,
                        warehouse.Name,
                        branch is null ? "Chưa xác định chi nhánh" : $"{branch.Code} · {branch.Name}",
                        OrganizationPresentation.WarehouseType(warehouse.WarehouseType),
                        OrganizationPresentation.LayoutMode(warehouse.LocationManagementMode),
                        warehouse.AllowNegativeStock,
                        OrganizationPresentation.WarehouseNegativeStockStatus(warehouse.AllowNegativeStock),
                        OrganizationPresentation.Status(warehouse.IsActive),
                        OrganizationPresentation.StatusAction(warehouse.IsActive),
                        warehouse.IsActive,
                        OrganizationPresentation.FormatDateTime(warehouse.UpdatedAt),
                        warehouse);
                }));
        OnPropertyChanged(nameof(WarehouseVisibleSummary));
        OnPropertyChanged(nameof(WarehouseHasNoRows));
        OnPropertyChanged(nameof(WarehouseEmptyMessage));
    }

    private void RefreshLocations()
    {
        var search = OrganizationPresentation.NormalizeSearch(LocationSearch);
        ResetCollection(
            LocationRows,
            _locations
                .Where(location =>
                {
                    var warehouse = FindWarehouse(location.WarehouseId);
                    var branch = warehouse is null ? null : _branches.FirstOrDefault(item => item.Id == warehouse.BranchId);
                    var warehouseSelected = string.IsNullOrWhiteSpace(SelectedWarehouseId) || location.WarehouseId == SelectedWarehouseId;
                    return warehouseSelected
                        && OrganizationPresentation.MatchesStatus(location.IsActive, LocationStatus)
                        && OrganizationPresentation.MatchesSearch(
                            search,
                            location.Code,
                            location.Name,
                            location.LocationType,
                            warehouse?.Code,
                            warehouse?.Name,
                            branch?.Code,
                            branch?.Name);
                })
                .OrderBy(location => location.Code)
                .Select(location =>
                {
                    var warehouse = FindWarehouse(location.WarehouseId);
                    var branch = warehouse is null ? null : _branches.FirstOrDefault(item => item.Id == warehouse.BranchId);
                    return new LocationRow(
                        location.Id,
                        location.Code,
                        location.Name,
                        warehouse is null ? "Kho không khả dụng" : $"{warehouse.Code} · {warehouse.Name}",
                        branch is null ? "Chưa xác định chi nhánh" : $"{branch.Code} · {branch.Name}",
                        OrganizationPresentation.WarehouseLocationType(location.LocationType),
                        OrganizationPresentation.Status(location.IsActive),
                        OrganizationPresentation.StatusAction(location.IsActive),
                        location.IsActive,
                        OrganizationPresentation.FormatDateTime(location.UpdatedAt),
                        location);
                }));
        OnPropertyChanged(nameof(SelectedWarehouseLocationCount));
        OnPropertyChanged(nameof(SelectedWarehouseLocationSummary));
        OnPropertyChanged(nameof(SelectedWarehouseHasNoLocations));
        OnPropertyChanged(nameof(WarehouseLayoutEmptyMessage));
        OnPropertyChanged(nameof(SelectedWarehouseLayoutModeText));
        OnPropertyChanged(nameof(SelectedWarehouseBranchText));
        OnPropertyChanged(nameof(HasSelectedWarehouse));
    }

    private void RefreshEmployees()
    {
        var search = OrganizationPresentation.NormalizeSearch(EmployeeSearch);
        ResetCollection(
            EmployeeRows,
            _employees
                .Where(employee =>
                {
                    var branch = employee.BranchId is null
                        ? null
                        : _branches.FirstOrDefault(item => item.Id == employee.BranchId);
                    var matchesBranch = EmployeeBranchFilter switch
                    {
                        "all" => true,
                        "unassigned" => string.IsNullOrWhiteSpace(employee.BranchId),
                        _ => employee.BranchId == EmployeeBranchFilter
                    };
                    return matchesBranch
                        && OrganizationPresentation.MatchesStatus(employee.IsActive, EmployeeStatus)
                        && OrganizationPresentation.MatchesSearch(
                            search,
                            employee.Code,
                            employee.FullName,
                            employee.JobTitle,
                            employee.Phone,
                            employee.Email,
                            branch?.Code,
                            branch?.Name);
                })
                .OrderBy(employee => employee.Code)
                .Select(employee =>
                {
                    var branch = employee.BranchId is null
                        ? null
                        : _branches.FirstOrDefault(item => item.Id == employee.BranchId);
                    return new EmployeeRow(
                        employee.Id,
                        employee.Code,
                        employee.FullName,
                        employee.JobTitle ?? "Chưa khai báo chức danh",
                        branch is null ? (employee.BranchId is null ? "Chưa phân công" : "Không có quyền xem chi nhánh") : $"{branch.Code} · {branch.Name}",
                        string.Join(" · ", new[] { employee.Phone, employee.Email }.Where(value => !string.IsNullOrWhiteSpace(value))),
                        employee.IsActive ? "Đang làm việc" : "Ngừng làm việc",
                        OrganizationPresentation.FormatDateTime(employee.UpdatedAt),
                        employee);
                }));
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
                "WAREHOUSE_SCOPE_DENIED" => "Kho nằm ngoài phạm vi được cấp cho tài khoản.",
                "SECURITY_OWNER_PROTECTED" => "Hồ sơ Chủ hệ thống được bảo vệ và không thể thay đổi bằng tài khoản hiện tại.",
                "DUPLICATE_CODE" => "Mã đã tồn tại. Vui lòng dùng mã khác.",
                "STALE_VERSION" => "Dữ liệu đã thay đổi trên hệ thống. Hãy cập nhật dữ liệu rồi thực hiện lại.",
                "BRANCH_INACTIVE" => "Chi nhánh đang ngừng hoạt động. Hãy kích hoạt chi nhánh trước.",
                "WAREHOUSE_INACTIVE" => "Kho đang ngừng hoạt động. Hãy kích hoạt kho trước.",
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

    private void ClearLayoutPreview()
    {
        _layoutPreview = null;
        _layoutIdempotencyKey = null;
        LayoutPreviewText = string.Empty;
        LayoutBlockersText = string.Empty;
        OnPropertyChanged(nameof(LayoutPreviewDestinationText));
        OnPropertyChanged(nameof(HasLayoutPreview));
        OnPropertyChanged(nameof(CanConfirmLayout));
    }

    private void OpenWarehouseStatusConfirm(EditorKind kind, string id)
    {
        if (kind == EditorKind.Warehouse && !CanWriteWarehouses) return;
        if (kind == EditorKind.Location && !CanWriteLocations) return;

        var exists = kind switch
        {
            EditorKind.Warehouse => _warehouses.Any(item => item.Id == id),
            EditorKind.Location => _locations.Any(item => item.Id == id),
            _ => false
        };
        if (!exists) return;

        SetMessage(string.Empty, isError: false);
        _pendingWarehouseStatusKind = kind;
        _pendingWarehouseStatusId = id;
        IsWarehouseStatusConfirmOpen = true;
        OnPropertyChanged(nameof(WarehouseStatusConfirmTitle));
        OnPropertyChanged(nameof(WarehouseStatusConfirmText));
    }

    private bool? PendingWarehouseStatusActive()
    {
        if (string.IsNullOrWhiteSpace(_pendingWarehouseStatusId)) return null;
        return _pendingWarehouseStatusKind switch
        {
            EditorKind.Warehouse => _warehouses.FirstOrDefault(item => item.Id == _pendingWarehouseStatusId)?.IsActive,
            EditorKind.Location => _locations.FirstOrDefault(item => item.Id == _pendingWarehouseStatusId)?.IsActive,
            _ => null
        };
    }

    private void ClearLocationModeDetail()
    {
        HasLocationModeDetail = false;
        LocationModeDetailTitle = string.Empty;
        LocationModeDetailMeta = string.Empty;
        LocationModeDetailSummary = string.Empty;
        LocationModeLines.Clear();
    }

    private string RequireEditorKey() =>
        !string.IsNullOrWhiteSpace(_editorIdempotencyKey) && _idempotencyKeys.IsValid(_editorIdempotencyKey)
            ? _editorIdempotencyKey
            : throw new InvalidOperationException("Thao tác tạo mới chưa có khóa chống xử lý trùng hợp lệ.");

    private WarehouseData? FindWarehouse(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : _warehouses.FirstOrDefault(warehouse => warehouse.Id == id);

    private void RaisePermissionState()
    {
        OnPropertyChanged(nameof(CanReadBranches));
        OnPropertyChanged(nameof(CanWriteBranches));
        OnPropertyChanged(nameof(CanReadWarehouses));
        OnPropertyChanged(nameof(CanWriteWarehouses));
        OnPropertyChanged(nameof(CanReadLocations));
        OnPropertyChanged(nameof(CanWriteLocations));
        OnPropertyChanged(nameof(CanReadEmployees));
        OnPropertyChanged(nameof(CanWriteEmployees));
        OnPropertyChanged(nameof(CanViewWarehouses));
        OnPropertyChanged(nameof(CanViewInternalOrganization));
        OnPropertyChanged(nameof(CanManageSelectedWarehouseLayout));
        OnPropertyChanged(nameof(CanAddLocationToSelectedWarehouse));
        OnPropertyChanged(nameof(LocationModeHistoryEmptyMessage));
    }

    private void RaiseEditorState()
    {
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsBranchEditor));
        OnPropertyChanged(nameof(IsWarehouseEditor));
        OnPropertyChanged(nameof(IsLocationEditor));
        OnPropertyChanged(nameof(IsEmployeeEditor));
        OnPropertyChanged(nameof(IsSharedEditorOpen));
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorNameLabel));
        OnPropertyChanged(nameof(EditorPrimaryActionLabel));
    }

    private void RaiseCounts()
    {
        OnPropertyChanged(nameof(BranchTotal));
        OnPropertyChanged(nameof(BranchActive));
        OnPropertyChanged(nameof(BranchInactive));
        OnPropertyChanged(nameof(BranchStatusSummary));
        OnPropertyChanged(nameof(WarehouseTotal));
        OnPropertyChanged(nameof(WarehouseActive));
        OnPropertyChanged(nameof(WarehouseInactive));
        OnPropertyChanged(nameof(WarehouseStatusSummary));
        OnPropertyChanged(nameof(LocationTotal));
        OnPropertyChanged(nameof(LocationActive));
        OnPropertyChanged(nameof(LocationInactive));
        OnPropertyChanged(nameof(LocationStatusSummary));
        OnPropertyChanged(nameof(EmployeeTotal));
        OnPropertyChanged(nameof(EmployeeActive));
        OnPropertyChanged(nameof(OverviewRecordTotal));
    }

    private void SetMessage(string message, bool isError)
    {
        Message = message;
        MessageIsError = isError;
        OnPropertyChanged(nameof(HasMessage));
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
