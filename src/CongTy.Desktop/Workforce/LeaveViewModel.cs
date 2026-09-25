using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class LeaveViewModel : INotifyPropertyChanged
{
    private const string LeaveSelfReadPermission = "core.leave.self.read";
    private const string LeaveSelfRequestPermission = "core.leave.self.request";
    private const string LeaveReadPermission = "core.leave.read";
    private const string LeaveApprovePermission = "core.leave.approve";
    private const string LeaveTypeManagePermission = "core.leave-type.manage";

    private readonly ILeaveService _service;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);
    private readonly List<LeaveTypeData> _leaveTypes = [];
    private readonly List<LeaveRequestData> _requests = [];
    private readonly List<LeaveBalanceData> _balances = [];

    private LeaveTypeCapabilitiesData _typeCapabilities = new();
    private LeaveRequestCapabilitiesData _requestCapabilities = new();
    private LeaveBalanceCapabilitiesData _balanceCapabilities = new();
    private LeaveSelectedEmployeeData? _selectedEmployee;
    private LeavePaginationData _pagination = new();

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private LeaveOption? _selectedStatus;
    private string _employeeQuery = string.Empty;
    private LeaveOption? _selectedBranch;
    private string _balanceAsOfText = "—";

    private LeaveOption? _selectedRequestType;
    private DateTime? _requestFromDate;
    private DateTime? _requestToDate;
    private LeaveOption? _selectedDayPart;
    private string _requestReason = string.Empty;
    private string _attachmentReference = string.Empty;

    private LeaveRequestRowView? _reviewTarget;
    private string _reviewReason = string.Empty;
    private LeaveRequestRowView? _cancelTarget;
    private string _cancelReason = string.Empty;

    private LeaveOption? _selectedBalanceEmployee;
    private LeaveOption? _selectedBalanceLeaveType;
    private LeaveOption? _selectedBalanceEntryType;
    private DateTime? _balanceEffectiveDate;
    private string _balanceDays = string.Empty;
    private string _balanceReason = string.Empty;
    private string _balanceLedgerTitle = "Chọn một dòng số dư để xem lịch sử phát sinh.";

    private LeaveTypeData? _editingType;
    private bool _isTypeEditorOpen;
    private string _typeCode = string.Empty;
    private string _typeName = string.Empty;
    private bool _typeIsActive = true;
    private bool _typeIsPaid = true;
    private bool _typeCountsAsWorkday = true;
    private bool _typeRequiresApproval = true;
    private bool _typeAllowsFullDay = true;
    private bool _typeAllowsHalfDay = true;
    private bool _typeRequiresAttachment;
    private bool _typeTracksBalance;
    private bool _typeAllowNegativeBalance;

    public LeaveViewModel(
        ILeaveService service,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _service = service;
        _idempotencyKeys = idempotencyKeys;
        _access = access;

        StatusOptions =
        [
            new LeaveOption(string.Empty, "Tất cả"),
            new LeaveOption("SUBMITTED", "Chờ duyệt"),
            new LeaveOption("APPROVED", "Đã duyệt"),
            new LeaveOption("REJECTED", "Từ chối"),
            new LeaveOption("CANCELLED", "Đã hủy")
        ];
        DayPartOptions =
        [
            new LeaveOption("FULL_DAY", "Cả ngày"),
            new LeaveOption("FIRST_HALF", "Nửa ca đầu"),
            new LeaveOption("SECOND_HALF", "Nửa ca sau")
        ];
        BalanceEntryTypeOptions =
        [
            new LeaveOption("OPENING_GRANT", "Cấp đầu kỳ"),
            new LeaveOption("ACCRUAL", "Phát sinh định kỳ"),
            new LeaveOption("ADJUSTMENT", "Điều chỉnh"),
            new LeaveOption("CARRY_OVER", "Chuyển năm"),
            new LeaveOption("EXPIRY", "Hết hạn"),
            new LeaveOption("COMPENSATORY", "Nghỉ bù")
        ];

        BranchOptions.Add(new LeaveOption(string.Empty, "Tất cả chi nhánh được cấp"));
        SelectedStatus = StatusOptions[0];
        SelectedBranch = BranchOptions[0];
        SelectedDayPart = DayPartOptions[0];
        SelectedBalanceEntryType = BalanceEntryTypeOptions[0];

        var today = WorkSchedulePresentation.BusinessToday();
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = FromDate.Value.AddMonths(1).AddDays(-1);
        RequestFromDate = today;
        RequestToDate = today;
        BalanceEffectiveDate = today;
        InitializeManualLeaveDefaults(today);

        _access.Changed += (_, _) => RunOnUiThread(ResetForAccessChange);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LeaveRequestRowView> RequestRows { get; } = [];
    public ObservableCollection<LeaveBalanceRowView> BalanceRows { get; } = [];
    public ObservableCollection<LeaveBalanceEntryRowView> BalanceEntryRows { get; } = [];
    public ObservableCollection<LeaveTypeRowView> LeaveTypeRows { get; } = [];
    public ObservableCollection<LeaveOption> BranchOptions { get; } = [];
    public ObservableCollection<LeaveOption> RequestLeaveTypeOptions { get; } = [];
    public ObservableCollection<LeaveOption> BalanceEmployeeOptions { get; } = [];
    public ObservableCollection<LeaveOption> BalanceLeaveTypeOptions { get; } = [];

    public IReadOnlyList<LeaveOption> StatusOptions { get; }
    public IReadOnlyList<LeaveOption> DayPartOptions { get; }
    public IReadOnlyList<LeaveOption> BalanceEntryTypeOptions { get; }

    public bool CanViewLeave =>
        _access.HasPermission(LeaveSelfReadPermission)
        || _access.HasPermission(LeaveSelfRequestPermission)
        || _access.HasPermission(LeaveReadPermission)
        || _access.HasPermission(LeaveApprovePermission)
        || _access.HasPermission(LeaveTypeManagePermission);

    public bool CanSubmitOwn =>
        _requestCapabilities.CanSubmitOwn
        && _access.HasPermission(LeaveSelfRequestPermission);

    public bool CanApprove =>
        _requestCapabilities.CanApprove
        && _access.HasPermission(LeaveApprovePermission);

    public bool CanSubmitManual =>
        _requestCapabilities.CanSubmitManual
        && _access.HasPermission(LeaveApprovePermission);

    public bool ShowManualPanel => CanSubmitManual;

    public bool CanManageTypes =>
        (_typeCapabilities.CanManage || _requestCapabilities.CanManageTypes || _balanceCapabilities.CanManage)
        && _access.HasPermission(LeaveTypeManagePermission);

    public bool CanManageBalances => CanManageTypes;
    public bool SelfOnly => _requestCapabilities.SelfOnly;
    public bool ShowScopedFilters => !SelfOnly;
    public bool CanRefresh => CanViewLeave && !IsBusy;
    public bool CanPrevious => !IsBusy && _pagination.HasPrevious;
    public bool CanNext => !IsBusy && _pagination.HasNext;
    public bool ShowLoading => IsBusy && RequestRows.Count == 0;
    public bool IsRequestEmpty => !IsBusy && RequestRows.Count == 0;
    public bool IsBalanceEmpty => !IsBusy && BalanceRows.Count == 0;
    public bool IsBalanceHistoryEmpty => !IsBusy && BalanceEntryRows.Count == 0;
    public bool IsTypeEmpty => !IsBusy && LeaveTypeRows.Count == 0;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool IsReviewOpen => ReviewTarget is not null;
    public bool IsCancelOpen => CancelTarget is not null;
    public bool IsTypeEditorOpen
    {
        get => _isTypeEditorOpen;
        private set
        {
            if (!SetField(ref _isTypeEditorOpen, value)) return;
            OnPropertyChanged(nameof(TypeEditorTitle));
            OnPropertyChanged(nameof(CanEditTypeCode));
            OnPropertyChanged(nameof(CanSaveType));
        }
    }

    public bool CanEditTypeCode => _editingType is null;
    public string TypeEditorTitle => _editingType is null ? "Thêm chế độ nghỉ" : "Cập nhật chế độ nghỉ";
    public string RequestCountText => $"{_pagination.Total} đơn trong kỳ";
    public string PendingCountText => $"{_requests.Count(item => item.Status == "SUBMITTED")} chờ duyệt trong trang";
    public string ActiveTypeCountText => $"{_leaveTypes.Count(item => item.IsActive)} chế độ đang áp dụng";
    public string PageText
    {
        get
        {
            if (_pagination.Total <= 0) return "0 đơn";
            var start = _pagination.Offset + 1;
            var end = Math.Min(_pagination.Offset + RequestRows.Count, _pagination.Total);
            return $"{start}–{end} / {_pagination.Total}";
        }
    }

    public string BalanceAsOfText
    {
        get => _balanceAsOfText;
        private set => SetField(ref _balanceAsOfText, value);
    }

    public string BalanceLedgerTitle
    {
        get => _balanceLedgerTitle;
        private set => SetField(ref _balanceLedgerTitle, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaiseAvailability();
            RaiseEmptyStates();
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

    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetField(ref _fromDate, value);
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => SetField(ref _toDate, value);
    }

    public LeaveOption? SelectedStatus
    {
        get => _selectedStatus;
        set => SetField(ref _selectedStatus, value);
    }

    public string EmployeeQuery
    {
        get => _employeeQuery;
        set => SetField(ref _employeeQuery, value ?? string.Empty);
    }

    public LeaveOption? SelectedBranch
    {
        get => _selectedBranch;
        set => SetField(ref _selectedBranch, value);
    }

    public LeaveOption? SelectedRequestType
    {
        get => _selectedRequestType;
        set
        {
            if (!SetField(ref _selectedRequestType, value)) return;
            OnPropertyChanged(nameof(RequestTypeHint));
            OnPropertyChanged(nameof(ShowAttachmentField));
            OnPropertyChanged(nameof(CanSubmitRequest));
        }
    }

    public DateTime? RequestFromDate
    {
        get => _requestFromDate;
        set
        {
            if (!SetField(ref _requestFromDate, value)) return;
            OnPropertyChanged(nameof(CanSubmitRequest));
        }
    }

    public DateTime? RequestToDate
    {
        get => _requestToDate;
        set
        {
            if (!SetField(ref _requestToDate, value)) return;
            OnPropertyChanged(nameof(CanSubmitRequest));
        }
    }

    public LeaveOption? SelectedDayPart
    {
        get => _selectedDayPart;
        set
        {
            if (!SetField(ref _selectedDayPart, value)) return;
            OnPropertyChanged(nameof(CanSubmitRequest));
        }
    }

    public string RequestReason
    {
        get => _requestReason;
        set
        {
            if (!SetField(ref _requestReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitRequest));
        }
    }

    public string AttachmentReference
    {
        get => _attachmentReference;
        set
        {
            if (!SetField(ref _attachmentReference, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitRequest));
        }
    }

    public string RequestTypeHint
    {
        get
        {
            var type = SelectedRequestTypeData();
            if (type is null) return "Chọn chế độ nghỉ để xem quy tắc áp dụng.";
            var approval = type.RequiresApproval ? "Cần duyệt" : "Tự động duyệt";
            var balance = type.TracksBalance ? "có theo dõi số dư" : "không theo dõi số dư";
            return $"{approval} · {balance}" + (type.RequiresAttachment ? " · cần chứng từ" : string.Empty);
        }
    }

    public bool ShowAttachmentField => SelectedRequestTypeData()?.RequiresAttachment == true;

    public bool CanSubmitRequest =>
        CanSubmitOwn
        && !IsBusy
        && SelectedRequestTypeData() is not null
        && RequestFromDate is not null
        && RequestToDate is not null
        && SelectedDayPart is not null
        && !string.IsNullOrWhiteSpace(RequestReason);

    public LeaveRequestRowView? ReviewTarget
    {
        get => _reviewTarget;
        private set
        {
            if (!SetField(ref _reviewTarget, value)) return;
            OnPropertyChanged(nameof(IsReviewOpen));
        }
    }

    public string ReviewReason
    {
        get => _reviewReason;
        set => SetField(ref _reviewReason, value ?? string.Empty);
    }

    public LeaveRequestRowView? CancelTarget
    {
        get => _cancelTarget;
        private set
        {
            if (!SetField(ref _cancelTarget, value)) return;
            OnPropertyChanged(nameof(IsCancelOpen));
        }
    }

    public string CancelReason
    {
        get => _cancelReason;
        set => SetField(ref _cancelReason, value ?? string.Empty);
    }

    public LeaveOption? SelectedBalanceEmployee
    {
        get => _selectedBalanceEmployee;
        set => SetField(ref _selectedBalanceEmployee, value);
    }

    public LeaveOption? SelectedBalanceLeaveType
    {
        get => _selectedBalanceLeaveType;
        set => SetField(ref _selectedBalanceLeaveType, value);
    }

    public LeaveOption? SelectedBalanceEntryType
    {
        get => _selectedBalanceEntryType;
        set => SetField(ref _selectedBalanceEntryType, value);
    }

    public DateTime? BalanceEffectiveDate
    {
        get => _balanceEffectiveDate;
        set => SetField(ref _balanceEffectiveDate, value);
    }

    public string BalanceDays
    {
        get => _balanceDays;
        set => SetField(ref _balanceDays, value ?? string.Empty);
    }

    public string BalanceReason
    {
        get => _balanceReason;
        set => SetField(ref _balanceReason, value ?? string.Empty);
    }

    public string TypeCode
    {
        get => _typeCode;
        set
        {
            if (!SetField(ref _typeCode, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveType));
        }
    }

    public string TypeName
    {
        get => _typeName;
        set
        {
            if (!SetField(ref _typeName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveType));
        }
    }

    public bool TypeIsActive { get => _typeIsActive; set => SetTypeFlag(ref _typeIsActive, value); }
    public bool TypeIsPaid { get => _typeIsPaid; set => SetTypeFlag(ref _typeIsPaid, value); }
    public bool TypeCountsAsWorkday { get => _typeCountsAsWorkday; set => SetTypeFlag(ref _typeCountsAsWorkday, value); }
    public bool TypeRequiresApproval { get => _typeRequiresApproval; set => SetTypeFlag(ref _typeRequiresApproval, value); }
    public bool TypeAllowsFullDay { get => _typeAllowsFullDay; set => SetTypeFlag(ref _typeAllowsFullDay, value); }
    public bool TypeAllowsHalfDay { get => _typeAllowsHalfDay; set => SetTypeFlag(ref _typeAllowsHalfDay, value); }
    public bool TypeRequiresAttachment { get => _typeRequiresAttachment; set => SetTypeFlag(ref _typeRequiresAttachment, value); }
    public bool TypeTracksBalance
    {
        get => _typeTracksBalance;
        set
        {
            if (!SetField(ref _typeTracksBalance, value)) return;
            if (!value && TypeAllowNegativeBalance) TypeAllowNegativeBalance = false;
            OnPropertyChanged(nameof(CanSaveType));
        }
    }
    public bool TypeAllowNegativeBalance { get => _typeAllowNegativeBalance; set => SetTypeFlag(ref _typeAllowNegativeBalance, value); }

    public bool CanSaveType =>
        CanManageTypes
        && IsTypeEditorOpen
        && !IsBusy
        && !string.IsNullOrWhiteSpace(TypeName)
        && (_editingType is not null || !string.IsNullOrWhiteSpace(TypeCode))
        && (TypeAllowsFullDay || TypeAllowsHalfDay)
        && (!TypeAllowNegativeBalance || TypeTracksBalance);

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanViewLeave) return;
        await ReloadAsync(0).ConfigureAwait(true);
    }

    public Task RefreshAsync() => ReloadAsync(_pagination.Offset);

    public Task ApplyFiltersAsync() => ReloadAsync(0);

    public Task PreviousPageAsync() =>
        CanPrevious ? ReloadAsync(Math.Max(0, _pagination.Offset - _pagination.Limit)) : Task.CompletedTask;

    public Task NextPageAsync() =>
        CanNext ? ReloadAsync(_pagination.Offset + _pagination.Limit) : Task.CompletedTask;

    private async Task ReloadAsync(int offset)
    {
        if (!CanViewLeave || IsBusy) return;
        if (!TryFilterRange(out var from, out var to, out var validation))
        {
            SetMessage(validation, true);
            return;
        }

        IsBusy = true;
        SetMessage(string.Empty, false);
        var errors = new List<string>();
        try
        {
            try
            {
                var types = await _service.ListLeaveTypesAsync().ConfigureAwait(true);
                ApplyTypes(types);
            }
            catch (Exception exception)
            {
                errors.Add(PublicError(exception, "Không tải được chế độ nghỉ."));
            }

            try
            {
                var data = await _service.ListLeaveRequestsAsync(
                    CanonicalDate(from),
                    CanonicalDate(to),
                    SelectedStatus?.Value,
                    ShowScopedFilters ? EmployeeQuery : null,
                    ShowScopedFilters ? SelectedBranch?.Value : null,
                    50,
                    Math.Max(0, offset)).ConfigureAwait(true);
                ApplyRequests(data);
            }
            catch (Exception exception)
            {
                errors.Add(PublicError(exception, "Không tải được đơn nghỉ và số dư phép."));
            }

            _loaded = true;
            if (errors.Count > 0)
                SetMessage(string.Join(" ", errors.Distinct()), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyTypes(LeaveTypeListResponseData response)
    {
        _typeCapabilities = response.Capabilities ?? new LeaveTypeCapabilitiesData();
        _leaveTypes.Clear();
        _leaveTypes.AddRange(response.LeaveTypes ?? []);

        var requestTypeId = SelectedRequestType?.Value;
        var manualTypeId = SelectedManualLeaveType?.Value;
        var balanceTypeId = SelectedBalanceLeaveType?.Value;

        LeaveTypeRows.Clear();
        RequestLeaveTypeOptions.Clear();
        BalanceLeaveTypeOptions.Clear();

        foreach (var type in _leaveTypes.OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase))
        {
            LeaveTypeRows.Add(new LeaveTypeRowView(
                type,
                type.Code,
                type.Name,
                LeavePresentation.LeaveTypeBadges(type),
                type.IsActive ? "Đang áp dụng" : "Ngừng áp dụng",
                CanManageTypes));

            if (type.IsActive)
                RequestLeaveTypeOptions.Add(new LeaveOption(type.Id, $"{type.Code} · {type.Name}"));
            if (type.TracksBalance)
                BalanceLeaveTypeOptions.Add(new LeaveOption(type.Id, $"{type.Code} · {type.Name}"));
        }

        SelectedRequestType = RequestLeaveTypeOptions.FirstOrDefault(item => item.Value == requestTypeId)
            ?? RequestLeaveTypeOptions.FirstOrDefault();
        SelectedManualLeaveType = RequestLeaveTypeOptions.FirstOrDefault(item => item.Value == manualTypeId)
            ?? RequestLeaveTypeOptions.FirstOrDefault();
        SelectedBalanceLeaveType = BalanceLeaveTypeOptions.FirstOrDefault(item => item.Value == balanceTypeId)
            ?? BalanceLeaveTypeOptions.FirstOrDefault();

        RaiseAccess();
        RaiseSummary();
        RaiseEmptyStates();
    }

    private void ApplyRequests(LeaveRequestListResponseData response)
    {
        _requestCapabilities = response.Capabilities ?? new LeaveRequestCapabilitiesData();
        _selectedEmployee = response.SelectedEmployee;
        _pagination = response.Pagination ?? new LeavePaginationData();

        _requests.Clear();
        _requests.AddRange(response.Requests ?? []);
        _balances.Clear();
        _balances.AddRange(response.LeaveBalances ?? []);
        ApplyManualEmployees(response.Employees ?? []);

        BalanceAsOfText = LeavePresentation.DateText(response.BalanceAsOfDate ?? CanonicalDate(ToDate ?? WorkSchedulePresentation.BusinessToday()));

        var branchId = SelectedBranch?.Value;
        BranchOptions.Clear();
        BranchOptions.Add(new LeaveOption(string.Empty, "Tất cả chi nhánh được cấp"));
        foreach (var branch in (response.Branches ?? []).OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase))
            BranchOptions.Add(new LeaveOption(branch.Id, $"{branch.Code} · {branch.Name}"));
        SelectedBranch = BranchOptions.FirstOrDefault(item => item.Value == branchId) ?? BranchOptions[0];

        RebuildRequestRows();
        RebuildBalanceRows();

        BalanceEntryRows.Clear();
        foreach (var entry in response.BalanceEntries ?? [])
            BalanceEntryRows.Add(ToBalanceEntryRow(entry));

        RaiseAccess();
        RaiseSummary();
        RaiseAvailability();
        RaiseEmptyStates();
    }

    private void RebuildRequestRows()
    {
        RequestRows.Clear();
        foreach (var item in _requests)
        {
            var employee = !string.IsNullOrWhiteSpace(item.EmployeeCode) || !string.IsNullOrWhiteSpace(item.EmployeeName)
                ? $"{item.EmployeeCode ?? "—"} · {item.EmployeeName ?? "Chưa xác định"}"
                : _selectedEmployee is null
                    ? "Nhân sự hiện tại"
                    : $"{_selectedEmployee.Code} · {_selectedEmployee.Name}";

            var canSelfCancel = _access.HasPermission(LeaveSelfRequestPermission)
                && string.Equals(item.EmployeeId, _access.Current.EmployeeId, StringComparison.Ordinal)
                && string.Equals(item.Status, "SUBMITTED", StringComparison.Ordinal);
            var canManagerCancel = CanApprove
                && (string.Equals(item.Status, "SUBMITTED", StringComparison.Ordinal)
                    || string.Equals(item.Status, "APPROVED", StringComparison.Ordinal));
            var reviewText = string.Equals(item.Status, "CANCELLED", StringComparison.Ordinal)
                ? item.CancelReason ?? "—"
                : item.ReviewReason ?? "—";

            RequestRows.Add(new LeaveRequestRowView(
                item,
                employee,
                $"{item.LeaveTypeCodeSnapshot} · {item.LeaveTypeNameSnapshot}",
                LeavePresentation.RequestPeriod(item),
                LeavePresentation.DayPartLabel(item.DayPart),
                LeavePresentation.StatusLabel(item.Status),
                LeavePresentation.ReasonDetails(item),
                reviewText,
                !string.IsNullOrWhiteSpace(item.AttachmentUrl),
                CanApprove && string.Equals(item.Status, "SUBMITTED", StringComparison.Ordinal),
                canSelfCancel || canManagerCancel));
        }
    }

    private void RebuildBalanceRows()
    {
        var selectedEmployeeId = SelectedBalanceEmployee?.Value;
        var selectedTypeId = SelectedBalanceLeaveType?.Value;

        BalanceRows.Clear();
        foreach (var balance in _balances
                     .OrderBy(item => item.EmployeeCode, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.LeaveTypeCode, StringComparer.OrdinalIgnoreCase))
        {
            BalanceRows.Add(new LeaveBalanceRowView(
                balance,
                $"{balance.EmployeeCode} · {balance.EmployeeName}",
                $"{balance.LeaveTypeCode} · {balance.LeaveTypeName}",
                LeavePresentation.DaysText(balance.BalanceDays),
                LeavePresentation.DateText(balance.LastActivityDate),
                balance.AllowNegativeBalance ? "Cho phép âm" : "Không cho phép âm"));
        }

        BalanceEmployeeOptions.Clear();
        foreach (var employee in _balances
                     .GroupBy(item => item.EmployeeId, StringComparer.Ordinal)
                     .Select(group => group.First())
                     .OrderBy(item => item.EmployeeCode, StringComparer.OrdinalIgnoreCase))
            BalanceEmployeeOptions.Add(new LeaveOption(employee.EmployeeId, $"{employee.EmployeeCode} · {employee.EmployeeName}"));

        if (_selectedEmployee is not null
            && BalanceEmployeeOptions.All(item => item.Value != _selectedEmployee.Id))
            BalanceEmployeeOptions.Add(new LeaveOption(_selectedEmployee.Id, $"{_selectedEmployee.Code} · {_selectedEmployee.Name}"));

        SelectedBalanceEmployee = BalanceEmployeeOptions.FirstOrDefault(item => item.Value == selectedEmployeeId)
            ?? BalanceEmployeeOptions.FirstOrDefault();
        SelectedBalanceLeaveType = BalanceLeaveTypeOptions.FirstOrDefault(item => item.Value == selectedTypeId)
            ?? SelectedBalanceLeaveType
            ?? BalanceLeaveTypeOptions.FirstOrDefault();

        RaiseEmptyStates();
    }

    private static LeaveBalanceEntryRowView ToBalanceEntryRow(LeaveBalanceEntryData entry) =>
        new(
            entry,
            LeavePresentation.DateText(entry.EffectiveDate),
            string.IsNullOrWhiteSpace(entry.EmployeeCode) && string.IsNullOrWhiteSpace(entry.EmployeeName)
                ? "—"
                : $"{entry.EmployeeCode ?? "—"} · {entry.EmployeeName ?? "Chưa xác định"}",
            string.IsNullOrWhiteSpace(entry.LeaveTypeCodeSnapshot)
                ? entry.LeaveTypeNameSnapshot
                : $"{entry.LeaveTypeCodeSnapshot} · {entry.LeaveTypeNameSnapshot}",
            LeavePresentation.EntryTypeLabel(entry.EntryType),
            LeavePresentation.SignedDaysText(entry.QuantityDays),
            entry.Reason);

    private LeaveTypeData? SelectedRequestTypeData() =>
        _leaveTypes.FirstOrDefault(item => item.Id == SelectedRequestType?.Value);

    private bool TryFilterRange(out DateTime from, out DateTime to, out string message)
    {
        from = FromDate?.Date ?? default;
        to = ToDate?.Date ?? default;
        message = string.Empty;
        if (FromDate is null || ToDate is null || to < from)
        {
            message = "Khoảng ngày tra cứu không hợp lệ.";
            return false;
        }
        if ((to - from).TotalDays > 365)
        {
            message = "Mỗi lần chỉ xem tối đa 366 ngày.";
            return false;
        }
        return true;
    }

    private string MutationKey(string slot, string scope)
    {
        if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;
        var key = _idempotencyKeys.Create(scope);
        _mutationKeys[slot] = key;
        return key;
    }

    private static string MutationSlot<T>(string kind, T payload) =>
        $"{kind}|{JsonSerializer.Serialize(payload)}";

    private static string CanonicalDate(DateTime value) =>
        value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? EmptyToNull(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }

    private static string PublicError(Exception exception, string fallback)
    {
        if (exception is CanonicalApiException canonical)
            return CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(canonical),
                canonical.RequestId);
        return exception is InvalidOperationException && !string.IsNullOrWhiteSpace(exception.Message)
            ? exception.Message
            : fallback;
    }

    private void ResetForAccessChange()
    {
        _loaded = false;
        _mutationKeys.Clear();
        _leaveTypes.Clear();
        _requests.Clear();
        _balances.Clear();
        _typeCapabilities = new LeaveTypeCapabilitiesData();
        _requestCapabilities = new LeaveRequestCapabilitiesData();
        _balanceCapabilities = new LeaveBalanceCapabilitiesData();
        _selectedEmployee = null;
        _pagination = new LeavePaginationData();
        RequestRows.Clear();
        BalanceRows.Clear();
        BalanceEntryRows.Clear();
        LeaveTypeRows.Clear();
        RequestLeaveTypeOptions.Clear();
        ManualEmployeeOptions.Clear();
        ResetManualLeaveState();
        BalanceEmployeeOptions.Clear();
        BalanceLeaveTypeOptions.Clear();
        BranchOptions.Clear();
        BranchOptions.Add(new LeaveOption(string.Empty, "Tất cả chi nhánh được cấp"));
        SelectedBranch = BranchOptions[0];
        ReviewTarget = null;
        CancelTarget = null;
        IsTypeEditorOpen = false;
        Message = string.Empty;
        MessageIsError = false;
        BalanceLedgerTitle = "Chọn một dòng số dư để xem lịch sử phát sinh.";
        RaiseAccess();
        RaiseSummary();
        RaiseAvailability();
        RaiseEmptyStates();
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(CanViewLeave));
        OnPropertyChanged(nameof(CanSubmitOwn));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanSubmitManual));
        OnPropertyChanged(nameof(ShowManualPanel));
        OnPropertyChanged(nameof(CanSubmitManualRequest));
        OnPropertyChanged(nameof(CanManageTypes));
        OnPropertyChanged(nameof(CanManageBalances));
        OnPropertyChanged(nameof(SelfOnly));
        OnPropertyChanged(nameof(ShowScopedFilters));
        OnPropertyChanged(nameof(CanSubmitRequest));
        OnPropertyChanged(nameof(CanSubmitManualRequest));
        OnPropertyChanged(nameof(CanSaveType));
        RebuildRequestRowsIfLoaded();
    }

    private void RebuildRequestRowsIfLoaded()
    {
        if (_requests.Count > 0 || RequestRows.Count > 0)
            RebuildRequestRows();
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(RequestCountText));
        OnPropertyChanged(nameof(PendingCountText));
        OnPropertyChanged(nameof(ActiveTypeCountText));
        OnPropertyChanged(nameof(PageText));
    }

    private void RaiseAvailability()
    {
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanPrevious));
        OnPropertyChanged(nameof(CanNext));
        OnPropertyChanged(nameof(CanSubmitRequest));
        OnPropertyChanged(nameof(CanSaveType));
    }

    private void RaiseEmptyStates()
    {
        OnPropertyChanged(nameof(ShowLoading));
        OnPropertyChanged(nameof(IsRequestEmpty));
        OnPropertyChanged(nameof(IsBalanceEmpty));
        OnPropertyChanged(nameof(IsBalanceHistoryEmpty));
        OnPropertyChanged(nameof(IsTypeEmpty));
    }

    private void SetMessage(string message, bool isError)
    {
        MessageIsError = isError;
        Message = message;
    }

    private void SetTypeFlag(ref bool field, bool value)
    {
        if (!SetField(ref field, value)) return;
        OnPropertyChanged(nameof(CanSaveType));
    }

    private static void RunOnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            dispatcher.Invoke(action);
        else
            action();
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
