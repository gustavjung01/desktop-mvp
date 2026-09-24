using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed class AttendanceAdjustmentViewModel : INotifyPropertyChanged
{
    private const string SelfAdjustPermission = "core.attendance.self-adjust-request";
    private const string ManagePermission = "core.attendance.adjust";
    private const string LockPermission = "core.attendance.lock";
    private const string TimeZone = "Asia/Ho_Chi_Minh";

    private readonly IAttendanceAdjustmentService _service;
    private readonly IEmployeeDirectoryReadService _employees;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);

    private AttendanceAdjustmentListResponseData? _data;
    private AttendancePeriodLockListResponseData? _locks;
    private bool _loaded;
    private bool _employeeCatalogLoaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;

    private DateTime? _fromDate;
    private DateTime? _toDate;
    private AttendanceAdjustmentStatusOption? _selectedStatus;
    private string _employeeQuery = string.Empty;
    private AttendanceAdjustmentBranchOption? _selectedBranch;

    private DateTime? _requestDate;
    private string _requestCheckIn = string.Empty;
    private string _requestCheckOut = string.Empty;
    private bool _requestCheckInNextDay;
    private bool _requestCheckOutNextDay;
    private string _requestReason = string.Empty;

    private AttendanceAdjustmentEmployeeOption? _selectedDirectEmployee;
    private DateTime? _directDate;
    private string _directCheckIn = string.Empty;
    private string _directCheckOut = string.Empty;
    private bool _directCheckInNextDay;
    private bool _directCheckOutNextDay;
    private string _directReason = string.Empty;

    private AttendanceAdjustmentRowView? _selectedReview;
    private string _reviewReason = string.Empty;

    private DateTime? _lockFrom;
    private DateTime? _lockTo;
    private AttendanceAdjustmentBranchOption? _selectedLockBranch;
    private string _lockReason = string.Empty;

    private string? _prefillEmployeeId;

    public AttendanceAdjustmentViewModel(
        IAttendanceAdjustmentService service,
        IEmployeeDirectoryReadService employees,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _service = service;
        _employees = employees;
        _idempotencyKeys = idempotencyKeys;
        _access = access;

        StatusOptions =
        [
            new(string.Empty, "Tất cả"),
            new("SUBMITTED", "Chờ duyệt"),
            new("APPROVED", "Đã duyệt"),
            new("REJECTED", "Từ chối"),
        ];
        _selectedStatus = StatusOptions[0];

        var today = WorkSchedulePresentation.BusinessToday();
        _fromDate = new DateTime(today.Year, today.Month, 1);
        _toDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        _requestDate = today;
        _directDate = today;
        _lockFrom = _fromDate;
        _lockTo = _toDate;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            _employeeCatalogLoaded = false;
            _data = null;
            _locks = null;
            _mutationKeys.Clear();
            Rows.Clear();
            LockRows.Clear();
            BranchOptions.Clear();
            LockBranchOptions.Clear();
            DirectEmployees.Clear();
            SelectedReview = null;
            SelectedDirectEmployee = null;
            SelectedBranch = null;
            SelectedLockBranch = null;
            SetMessage(string.Empty, false);
            RaiseAll();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AttendanceAdjustmentRowView> Rows { get; } = [];
    public ObservableCollection<AttendancePeriodLockRowView> LockRows { get; } = [];
    public ObservableCollection<AttendanceAdjustmentBranchOption> BranchOptions { get; } = [];
    public ObservableCollection<AttendanceAdjustmentBranchOption> LockBranchOptions { get; } = [];
    public ObservableCollection<AttendanceAdjustmentEmployeeOption> DirectEmployees { get; } = [];
    public IReadOnlyList<AttendanceAdjustmentStatusOption> StatusOptions { get; }

    public bool HasSelfPermission => _access.HasPermission(SelfAdjustPermission);
    public bool HasManagePermission => _access.HasPermission(ManagePermission);
    public bool HasLockPermission => _access.HasPermission(LockPermission);
    public bool CanView => HasSelfPermission || HasManagePermission;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaiseActions();
            OnPropertyChanged(nameof(ShowLoading));
        }
    }

    public bool ShowLoading => IsBusy && !_loaded;

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

    public AttendanceAdjustmentStatusOption? SelectedStatus
    {
        get => _selectedStatus;
        set => SetField(ref _selectedStatus, value);
    }

    public string EmployeeQuery
    {
        get => _employeeQuery;
        set => SetField(ref _employeeQuery, value ?? string.Empty);
    }

    public AttendanceAdjustmentBranchOption? SelectedBranch
    {
        get => _selectedBranch;
        set => SetField(ref _selectedBranch, value);
    }

    public DateTime? RequestDate
    {
        get => _requestDate;
        set => SetField(ref _requestDate, value);
    }

    public string RequestCheckIn
    {
        get => _requestCheckIn;
        set
        {
            if (!SetField(ref _requestCheckIn, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitOwn));
        }
    }

    public string RequestCheckOut
    {
        get => _requestCheckOut;
        set
        {
            if (!SetField(ref _requestCheckOut, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitOwn));
        }
    }

    public bool RequestCheckInNextDay
    {
        get => _requestCheckInNextDay;
        set => SetField(ref _requestCheckInNextDay, value);
    }

    public bool RequestCheckOutNextDay
    {
        get => _requestCheckOutNextDay;
        set => SetField(ref _requestCheckOutNextDay, value);
    }

    public string RequestReason
    {
        get => _requestReason;
        set
        {
            if (!SetField(ref _requestReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitOwn));
        }
    }

    public AttendanceAdjustmentEmployeeOption? SelectedDirectEmployee
    {
        get => _selectedDirectEmployee;
        set
        {
            if (!SetField(ref _selectedDirectEmployee, value)) return;
            OnPropertyChanged(nameof(CanSubmitDirect));
        }
    }

    public DateTime? DirectDate
    {
        get => _directDate;
        set => SetField(ref _directDate, value);
    }

    public string DirectCheckIn
    {
        get => _directCheckIn;
        set
        {
            if (!SetField(ref _directCheckIn, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitDirect));
        }
    }

    public string DirectCheckOut
    {
        get => _directCheckOut;
        set
        {
            if (!SetField(ref _directCheckOut, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitDirect));
        }
    }

    public bool DirectCheckInNextDay
    {
        get => _directCheckInNextDay;
        set => SetField(ref _directCheckInNextDay, value);
    }

    public bool DirectCheckOutNextDay
    {
        get => _directCheckOutNextDay;
        set => SetField(ref _directCheckOutNextDay, value);
    }

    public string DirectReason
    {
        get => _directReason;
        set
        {
            if (!SetField(ref _directReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitDirect));
        }
    }

    public AttendanceAdjustmentRowView? SelectedReview
    {
        get => _selectedReview;
        private set
        {
            if (!SetField(ref _selectedReview, value)) return;
            OnPropertyChanged(nameof(HasSelectedReview));
            OnPropertyChanged(nameof(ReviewTitle));
            OnPropertyChanged(nameof(ReviewSummary));
            RaiseActions();
        }
    }

    public string ReviewReason
    {
        get => _reviewReason;
        set
        {
            if (!SetField(ref _reviewReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanReject));
        }
    }

    public bool HasSelectedReview => SelectedReview is not null;
    public string ReviewTitle => SelectedReview is null
        ? string.Empty
        : $"Xử lý yêu cầu ngày {SelectedReview.DateText}";
    public string ReviewSummary => SelectedReview is null
        ? string.Empty
        : $"{SelectedReview.EmployeeText} — {SelectedReview.RequestedTimesText}";

    public DateTime? LockFrom
    {
        get => _lockFrom;
        set => SetField(ref _lockFrom, value);
    }

    public DateTime? LockTo
    {
        get => _lockTo;
        set => SetField(ref _lockTo, value);
    }

    public AttendanceAdjustmentBranchOption? SelectedLockBranch
    {
        get => _selectedLockBranch;
        set
        {
            if (!SetField(ref _selectedLockBranch, value)) return;
            OnPropertyChanged(nameof(CanLockPeriod));
        }
    }

    public string LockReason
    {
        get => _lockReason;
        set
        {
            if (!SetField(ref _lockReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanLockPeriod));
        }
    }

    public bool CanFilterScoped => _data is not null && !_data.Capabilities.SelfOnly;
    public bool HasBranchFilter => CanFilterScoped && BranchOptions.Count > 1;
    public bool ShowSelfPanel => _data?.Capabilities.CanSubmitOwn ?? HasSelfPermission;
    public bool ShowDirectPanel => _data?.Capabilities.CanManage ?? HasManagePermission;
    public bool ShowLockPanel => _data?.Capabilities.CanLock ?? HasLockPermission;
    public bool ShowLockHistory => (_data?.Capabilities.CanManage ?? HasManagePermission) || ShowLockPanel;
    public bool IsCompanyLockScope => _locks?.CompanyScope ?? false;

    public bool CanRefresh => CanView && !IsBusy;
    public bool CanPagePrevious => !IsBusy && (_data?.Pagination.HasPrevious ?? false);
    public bool CanPageNext => !IsBusy && (_data?.Pagination.HasNext ?? false);
    public bool NoRows => _loaded && Rows.Count == 0;
    public bool NoLocks => ShowLockHistory && _locks is not null && LockRows.Count == 0;

    public bool CanSubmitOwn =>
        ShowSelfPanel
        && !IsBusy
        && !string.IsNullOrWhiteSpace(RequestReason)
        && (!string.IsNullOrWhiteSpace(RequestCheckIn) || !string.IsNullOrWhiteSpace(RequestCheckOut));

    public bool CanSubmitDirect =>
        ShowDirectPanel
        && !IsBusy
        && SelectedDirectEmployee is not null
        && !string.IsNullOrWhiteSpace(DirectReason)
        && (!string.IsNullOrWhiteSpace(DirectCheckIn) || !string.IsNullOrWhiteSpace(DirectCheckOut));

    public bool CanApprove =>
        ShowDirectPanel
        && !IsBusy
        && SelectedReview?.Source.Status == "SUBMITTED";

    public bool CanReject =>
        CanApprove
        && !string.IsNullOrWhiteSpace(ReviewReason);

    public bool CanLockPeriod =>
        ShowLockPanel
        && !IsBusy
        && !string.IsNullOrWhiteSpace(LockReason)
        && (IsCompanyLockScope || SelectedLockBranch is not null);

    public string TotalText => AttendanceAdjustmentPresentation.Count(_data?.Pagination.Total ?? 0);
    public string PendingText => AttendanceAdjustmentPresentation.Count(
        _data?.Requests.Count(item => item.Status == "SUBMITTED") ?? 0);
    public string LockedText => AttendanceAdjustmentPresentation.Count(_locks?.Locks.Length ?? 0);
    public string PeriodText => _data is null
        ? "—"
        : AttendanceAdjustmentPresentation.Period(_data.Period.From, _data.Period.To);
    public string PaginationText
    {
        get
        {
            if (_data is null || _data.Pagination.Total <= 0) return "0 / 0";
            var start = _data.Pagination.Offset + 1;
            var end = Math.Min(_data.Pagination.Offset + Rows.Count, _data.Pagination.Total);
            return $"{start}–{end} / {_data.Pagination.Total}";
        }
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanView) return;
        await LoadAsync(0).ConfigureAwait(true);
    }

    public Task RefreshAsync() =>
        LoadAsync(_data?.Pagination.Offset ?? 0);

    public Task ApplyFiltersAsync() =>
        LoadAsync(0);

    public Task PreviousPageAsync()
    {
        if (_data is null || !CanPagePrevious) return Task.CompletedTask;
        return LoadAsync(Math.Max(0, _data.Pagination.Offset - _data.Pagination.Limit));
    }

    public Task NextPageAsync()
    {
        if (_data is null || !CanPageNext) return Task.CompletedTask;
        return LoadAsync(_data.Pagination.Offset + _data.Pagination.Limit);
    }

    public async Task PrepareAsync(string? employeeId, string? workDate)
    {
        _prefillEmployeeId = string.IsNullOrWhiteSpace(employeeId) ? null : employeeId.Trim();
        if (TryParseDate(workDate, out var date))
        {
            RequestDate = date;
            DirectDate = date;
        }

        if (_employeeCatalogLoaded)
        {
            SelectedDirectEmployee = string.IsNullOrWhiteSpace(_prefillEmployeeId)
                ? null
                : DirectEmployees.FirstOrDefault(item =>
                    string.Equals(item.Id, _prefillEmployeeId, StringComparison.Ordinal));
        }

        if (_loaded && CanView && !IsBusy)
            await LoadAsync(0).ConfigureAwait(true);
    }

    public void OpenReview(AttendanceAdjustmentRowView? row)
    {
        if (row is null || !row.CanReview) return;
        SelectedReview = row;
        ReviewReason = string.Empty;
    }

    public void CloseReview()
    {
        SelectedReview = null;
        ReviewReason = string.Empty;
    }

    public async Task SubmitOwnAsync()
    {
        if (!CanSubmitOwn) return;
        if (!TryBuildAdjustment(
                RequestDate,
                RequestCheckIn,
                RequestCheckInNextDay,
                RequestCheckOut,
                RequestCheckOutNextDay,
                RequestReason,
                out var workDate,
                out var checkIn,
                out var checkOut,
                out var reason,
                out var validation))
        {
            SetMessage(validation, true);
            return;
        }

        var payload = new SubmitAttendanceAdjustmentRequest(workDate!, checkIn, checkOut, reason!);
        var slot = MutationSlot("attendance-adjustment-submit", payload);
        var key = MutationKey(slot, "desktop-attendance-adjustment-submit");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.SubmitOwnAsync(payload, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            RequestCheckIn = string.Empty;
            RequestCheckOut = string.Empty;
            RequestCheckInNextDay = false;
            RequestCheckOutNextDay = false;
            RequestReason = string.Empty;
            await ReloadAfterMutationAsync().ConfigureAwait(true);
            SetMessage("Yêu cầu điều chỉnh công đã được gửi.", false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không gửi được yêu cầu điều chỉnh công."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ReviewAsync(string action)
    {
        if (SelectedReview is null || !CanApprove) return;
        var normalizedAction = action?.Trim().ToUpperInvariant();
        if (normalizedAction is not ("APPROVE" or "REJECT")) return;

        var reviewReason = ReviewReason.Trim();
        if (reviewReason.Length > 1000)
        {
            SetMessage("Ý kiến xử lý tối đa 1.000 ký tự.", true);
            return;
        }
        if (normalizedAction == "REJECT" && string.IsNullOrWhiteSpace(reviewReason))
        {
            SetMessage("Cần nhập ý kiến khi từ chối yêu cầu.", true);
            return;
        }

        var payload = new ReviewAttendanceAdjustmentRequest(
            SelectedReview.Source.Id,
            normalizedAction,
            SelectedReview.Source.Version,
            string.IsNullOrWhiteSpace(reviewReason) ? null : reviewReason);
        var slot = MutationSlot("attendance-adjustment-review", payload);
        var key = MutationKey(slot, "desktop-attendance-adjustment-review");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.ReviewAsync(payload, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            CloseReview();
            await ReloadAfterMutationAsync().ConfigureAwait(true);
            SetMessage(
                normalizedAction == "APPROVE"
                    ? "Yêu cầu đã được duyệt và ghi nhận vào bảng công."
                    : "Yêu cầu đã được từ chối.",
                false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không xử lý được yêu cầu điều chỉnh."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SubmitDirectAsync()
    {
        if (!CanSubmitDirect || SelectedDirectEmployee is null) return;
        if (!TryBuildAdjustment(
                DirectDate,
                DirectCheckIn,
                DirectCheckInNextDay,
                DirectCheckOut,
                DirectCheckOutNextDay,
                DirectReason,
                out var workDate,
                out var checkIn,
                out var checkOut,
                out var reason,
                out var validation))
        {
            SetMessage(validation, true);
            return;
        }

        var payload = new DirectAttendanceAdjustmentRequest(
            SelectedDirectEmployee.Id,
            workDate!,
            checkIn,
            checkOut,
            reason!);
        var slot = MutationSlot("attendance-adjustment-direct", payload);
        var key = MutationKey(slot, "desktop-attendance-adjustment-direct");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.DirectAsync(payload, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            DirectCheckIn = string.Empty;
            DirectCheckOut = string.Empty;
            DirectCheckInNextDay = false;
            DirectCheckOutNextDay = false;
            DirectReason = string.Empty;
            await ReloadAfterMutationAsync().ConfigureAwait(true);
            SetMessage("Điều chỉnh trực tiếp đã được ghi nhận vào bảng công.", false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không ghi nhận được điều chỉnh trực tiếp."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LockPeriodAsync()
    {
        if (!CanLockPeriod) return;
        if (!TryPeriod(LockFrom, LockTo, out var from, out var to, out var validation))
        {
            SetMessage(validation, true);
            return;
        }

        var reason = LockReason.Trim();
        if (reason.Length is < 1 or > 1000)
        {
            SetMessage("Lý do khóa kỳ là bắt buộc và tối đa 1.000 ký tự.", true);
            return;
        }

        var branchId = SelectedLockBranch?.Id;
        if (!IsCompanyLockScope && string.IsNullOrWhiteSpace(branchId))
        {
            SetMessage("Chọn chi nhánh cần khóa kỳ.", true);
            return;
        }

        var payload = new CreateAttendancePeriodLockRequest(
            from!,
            to!,
            string.IsNullOrWhiteSpace(branchId) ? null : branchId,
            reason);
        var slot = MutationSlot("attendance-period-lock", payload);
        var key = MutationKey(slot, "desktop-attendance-period-lock");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.LockPeriodAsync(payload, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            LockReason = string.Empty;
            await ReloadLocksAsync().ConfigureAwait(true);
            SetMessage("Kỳ công đã được khóa.", false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không khóa được kỳ công."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync(int offset)
    {
        if (!CanView || IsBusy) return;
        if (!TryPeriod(FromDate, ToDate, out var from, out var to, out var validation))
        {
            SetMessage(validation, true);
            return;
        }

        var employeeQuery = EmployeeQuery.Trim();
        if (employeeQuery.Length > 80)
        {
            SetMessage("Từ khóa nhân sự tối đa 80 ký tự.", true);
            return;
        }

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var data = await _service.ListAsync(
                from!,
                to!,
                SelectedStatus?.Key,
                _prefillEmployeeId,
                employeeQuery,
                SelectedBranch?.Id,
                50,
                Math.Max(0, offset)).ConfigureAwait(true);

            ApplyData(data);
            var secondaryErrors = new List<string>();

            if (data.Capabilities.CanManage)
            {
                try
                {
                    await EnsureEmployeeCatalogAsync().ConfigureAwait(true);
                }
                catch (Exception exception)
                {
                    secondaryErrors.Add(PublicError(exception, "Không tải được danh sách nhân sự."));
                }
            }

            if (data.Capabilities.CanManage || data.Capabilities.CanLock)
            {
                try
                {
                    await ReloadLocksAsync().ConfigureAwait(true);
                }
                catch (Exception exception)
                {
                    ApplyLocks(null);
                    secondaryErrors.Add(PublicError(exception, "Không tải được kỳ công đã khóa."));
                }
            }
            else
            {
                ApplyLocks(null);
            }

            _loaded = true;
            if (secondaryErrors.Count > 0)
                SetMessage(string.Join(" ", secondaryErrors.Distinct()), true);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tải được điều chỉnh công."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadAfterMutationAsync()
    {
        if (!TryPeriod(FromDate, ToDate, out var from, out var to, out _)) return;
        var currentOffset = _data?.Pagination.Offset ?? 0;
        var data = await _service.ListAsync(
            from!,
            to!,
            SelectedStatus?.Key,
            _prefillEmployeeId,
            EmployeeQuery.Trim(),
            SelectedBranch?.Id,
            50,
            currentOffset).ConfigureAwait(true);
        ApplyData(data);
        if (data.Capabilities.CanManage || data.Capabilities.CanLock)
            await ReloadLocksAsync().ConfigureAwait(true);
    }

    private async Task ReloadLocksAsync()
    {
        if (_data is null || (!_data.Capabilities.CanManage && !_data.Capabilities.CanLock))
        {
            ApplyLocks(null);
            return;
        }

        var locks = await _service.ListLocksAsync(
            ApiDate(FromDate),
            ApiDate(ToDate),
            SelectedBranch?.Id).ConfigureAwait(true);
        ApplyLocks(locks);
    }

    private async Task EnsureEmployeeCatalogAsync()
    {
        if (_employeeCatalogLoaded) return;
        var employees = await _employees.ListEmployeesAsync().ConfigureAwait(true);
        DirectEmployees.Clear();
        foreach (var employee in employees
                     .Where(item => item.IsActive)
                     .OrderBy(item => item.Code, StringComparer.CurrentCultureIgnoreCase))
        {
            DirectEmployees.Add(new AttendanceAdjustmentEmployeeOption(
                employee.Id,
                $"{employee.Code} · {employee.FullName}"));
        }

        _employeeCatalogLoaded = true;
        if (!string.IsNullOrWhiteSpace(_prefillEmployeeId))
        {
            SelectedDirectEmployee = DirectEmployees.FirstOrDefault(item =>
                string.Equals(item.Id, _prefillEmployeeId, StringComparison.Ordinal));
        }
    }

    private void ApplyData(AttendanceAdjustmentListResponseData data)
    {
        _data = data;

        if (TryParseDate(data.Period.From, out var from)) FromDate = from;
        if (TryParseDate(data.Period.To, out var to)) ToDate = to;

        var selectedBranchId = SelectedBranch?.Id;
        BranchOptions.Clear();
        BranchOptions.Add(new AttendanceAdjustmentBranchOption(string.Empty, "Tất cả chi nhánh được cấp"));
        foreach (var branch in data.Branches.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
            BranchOptions.Add(new AttendanceAdjustmentBranchOption(branch.Id, $"{branch.Code} · {branch.Name}"));

        SelectedBranch = data.Capabilities.SelfOnly
            ? BranchOptions[0]
            : BranchOptions.FirstOrDefault(item => string.Equals(item.Id, selectedBranchId, StringComparison.Ordinal))
              ?? BranchOptions[0];

        Rows.Clear();
        foreach (var request in data.Requests)
        {
            Rows.Add(new AttendanceAdjustmentRowView(
                request,
                WorkSchedulePresentation.DateText(request.WorkDate),
                AttendanceAdjustmentPresentation.EmployeeText(request, data.SelectedEmployee),
                AttendanceAdjustmentPresentation.BranchText(request),
                AttendanceAdjustmentPresentation.RequestedTimes(request),
                AttendanceAdjustmentPresentation.SourceLabel(request.RequestSource),
                AttendanceAdjustmentPresentation.StatusLabel(request.Status),
                request.Reason,
                AttendanceAdjustmentPresentation.ReviewText(request),
                data.Capabilities.CanManage && request.Status == "SUBMITTED"));
        }

        if (SelectedReview is not null)
        {
            var updated = Rows.FirstOrDefault(item =>
                string.Equals(item.Source.Id, SelectedReview.Source.Id, StringComparison.Ordinal));
            SelectedReview = updated?.CanReview == true ? updated : null;
        }

        OnPropertyChanged(nameof(CanFilterScoped));
        OnPropertyChanged(nameof(HasBranchFilter));
        OnPropertyChanged(nameof(ShowSelfPanel));
        OnPropertyChanged(nameof(ShowDirectPanel));
        OnPropertyChanged(nameof(ShowLockPanel));
        OnPropertyChanged(nameof(ShowLockHistory));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(PendingText));
        OnPropertyChanged(nameof(PeriodText));
        OnPropertyChanged(nameof(PaginationText));
        OnPropertyChanged(nameof(CanPagePrevious));
        OnPropertyChanged(nameof(CanPageNext));
        OnPropertyChanged(nameof(NoRows));
        RaiseActions();
    }

    private void ApplyLocks(AttendancePeriodLockListResponseData? data)
    {
        _locks = data;
        LockRows.Clear();
        LockBranchOptions.Clear();

        if (data is not null)
        {
            if (data.CompanyScope)
                LockBranchOptions.Add(new AttendanceAdjustmentBranchOption(string.Empty, "Toàn Công Ty"));

            foreach (var branch in data.Branches.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
                LockBranchOptions.Add(new AttendanceAdjustmentBranchOption(branch.Id, $"{branch.Code} · {branch.Name}"));

            var selectedId = SelectedLockBranch?.Id;
            SelectedLockBranch = LockBranchOptions.FirstOrDefault(item =>
                                     string.Equals(item.Id, selectedId, StringComparison.Ordinal))
                                 ?? LockBranchOptions.FirstOrDefault();

            foreach (var item in data.Locks)
            {
                LockRows.Add(new AttendancePeriodLockRowView(
                    item,
                    AttendanceAdjustmentPresentation.Period(item.PeriodStart, item.PeriodEnd),
                    AttendanceAdjustmentPresentation.LockScope(item),
                    item.Reason,
                    WorkSchedulePresentation.DateTimeText(item.LockedAt, TimeZone)));
            }
        }
        else
        {
            SelectedLockBranch = null;
        }

        OnPropertyChanged(nameof(IsCompanyLockScope));
        OnPropertyChanged(nameof(LockedText));
        OnPropertyChanged(nameof(NoLocks));
        RaiseActions();
    }

    private static bool TryBuildAdjustment(
        DateTime? workDate,
        string checkInClock,
        bool checkInNextDay,
        string checkOutClock,
        bool checkOutNextDay,
        string rawReason,
        out string? workDateText,
        out string? checkIn,
        out string? checkOut,
        out string? reason,
        out string validation)
    {
        workDateText = null;
        checkIn = null;
        checkOut = null;
        reason = null;
        validation = string.Empty;

        if (workDate is null)
        {
            validation = "Chọn ngày công cần điều chỉnh.";
            return false;
        }

        var today = WorkSchedulePresentation.BusinessToday();
        var date = workDate.Value.Date;
        if (date > today)
        {
            validation = "Ngày công cần điều chỉnh không được ở tương lai.";
            return false;
        }

        var inClock = checkInClock.Trim();
        var outClock = checkOutClock.Trim();
        if (string.IsNullOrWhiteSpace(inClock) && string.IsNullOrWhiteSpace(outClock))
        {
            validation = "Cần nhập ít nhất giờ vào hoặc giờ ra cần điều chỉnh.";
            return false;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(inClock))
                checkIn = WorkSchedulePresentation.ToIso(date, NormalizeClock(inClock), TimeZone, checkInNextDay);
            if (!string.IsNullOrWhiteSpace(outClock))
                checkOut = WorkSchedulePresentation.ToIso(date, NormalizeClock(outClock), TimeZone, checkOutNextDay);
        }
        catch (InvalidOperationException)
        {
            validation = "Giờ điều chỉnh phải theo định dạng HH:mm.";
            return false;
        }

        if (checkIn is not null
            && checkOut is not null
            && DateTimeOffset.Parse(checkOut, CultureInfo.InvariantCulture) < DateTimeOffset.Parse(checkIn, CultureInfo.InvariantCulture))
        {
            validation = "Giờ ra không được sớm hơn giờ vào.";
            return false;
        }

        var normalizedReason = rawReason.Trim();
        if (normalizedReason.Length is < 1 or > 1000)
        {
            validation = "Lý do điều chỉnh là bắt buộc và tối đa 1.000 ký tự.";
            return false;
        }

        workDateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        reason = normalizedReason;
        return true;
    }

    private static string NormalizeClock(string value)
    {
        var candidate = value.Trim();
        if (TimeOnly.TryParseExact(candidate, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var shortTime))
            return shortTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        if (TimeOnly.TryParseExact(candidate, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return time.ToString("HH:mm", CultureInfo.InvariantCulture);
        throw new InvalidOperationException("Giờ không hợp lệ.");
    }

    private static bool TryPeriod(
        DateTime? fromDate,
        DateTime? toDate,
        out string? from,
        out string? to,
        out string validation)
    {
        from = null;
        to = null;
        validation = string.Empty;
        if (fromDate is null || toDate is null)
        {
            validation = "Chọn đầy đủ Từ ngày và Đến ngày.";
            return false;
        }

        var start = fromDate.Value.Date;
        var end = toDate.Value.Date;
        if (end < start)
        {
            validation = "Khoảng thời gian điều chỉnh công không hợp lệ.";
            return false;
        }

        if ((end - start).TotalDays + 1 > 93)
        {
            validation = "Mỗi lần chỉ xử lý tối đa 93 ngày công.";
            return false;
        }

        from = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        to = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    private static string? ApiDate(DateTime? value) =>
        value?.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static bool TryParseDate(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var candidate = value.Trim();
        if (candidate.Length >= 10) candidate = candidate[..10];
        return DateTime.TryParseExact(
            candidate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
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

    private void SetMessage(string message, bool isError)
    {
        MessageIsError = isError;
        Message = message;
    }

    private void RaiseActions()
    {
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanPagePrevious));
        OnPropertyChanged(nameof(CanPageNext));
        OnPropertyChanged(nameof(CanSubmitOwn));
        OnPropertyChanged(nameof(CanSubmitDirect));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanReject));
        OnPropertyChanged(nameof(CanLockPeriod));
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(HasSelfPermission));
        OnPropertyChanged(nameof(HasManagePermission));
        OnPropertyChanged(nameof(HasLockPermission));
        OnPropertyChanged(nameof(CanView));
        OnPropertyChanged(nameof(CanFilterScoped));
        OnPropertyChanged(nameof(HasBranchFilter));
        OnPropertyChanged(nameof(ShowSelfPanel));
        OnPropertyChanged(nameof(ShowDirectPanel));
        OnPropertyChanged(nameof(ShowLockPanel));
        OnPropertyChanged(nameof(ShowLockHistory));
        OnPropertyChanged(nameof(IsCompanyLockScope));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(PendingText));
        OnPropertyChanged(nameof(LockedText));
        OnPropertyChanged(nameof(PeriodText));
        OnPropertyChanged(nameof(PaginationText));
        OnPropertyChanged(nameof(NoRows));
        OnPropertyChanged(nameof(NoLocks));
        OnPropertyChanged(nameof(ShowLoading));
        RaiseActions();
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
