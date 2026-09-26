using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class AttendanceViolationViewModel : INotifyPropertyChanged
{
    private const string SelfReadPermission = "core.attendance.self.read";
    private const string ReadPermission = "core.attendance.read";
    private const string SelfExplainPermission = "core.attendance-violation.self-explain";
    private const string ResolvePermission = "core.attendance-violation.resolve";

    private readonly IAttendanceViolationService _service;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);

    private AttendanceViolationHandlingResponseData? _data;
    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;

    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _employeeQuery = string.Empty;
    private AttendanceViolationBranchOption? _selectedBranch;

    private AttendanceViolationRowView? _explainRow;
    private string _explanation = string.Empty;

    private AttendanceViolationRowView? _reviewRow;
    private AttendanceViolationOutcomeOption? _selectedOutcome;
    private string _reviewNote = string.Empty;

    public AttendanceViolationViewModel(
        IAttendanceViolationService service,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _service = service;
        _idempotencyKeys = idempotencyKeys;
        _access = access;

        OutcomeOptions =
        [
            new("EXCUSED", "Chấp nhận giải trình"),
            new("CONFIRMED", "Xác nhận vi phạm"),
        ];
        _selectedOutcome = OutcomeOptions[0];

        var today = WorkSchedulePresentation.BusinessToday();
        _fromDate = new DateTime(today.Year, today.Month, 1);
        _toDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            _data = null;
            _mutationKeys.Clear();
            Rows.Clear();
            BranchOptions.Clear();
            ExplainRow = null;
            ReviewRow = null;
            EmployeeQuery = string.Empty;
            SelectedBranch = null;
            SetMessage(string.Empty, false);
            RaiseAll();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AttendanceViolationRowView> Rows { get; } = [];
    public ObservableCollection<AttendanceViolationBranchOption> BranchOptions { get; } = [];
    public IReadOnlyList<AttendanceViolationOutcomeOption> OutcomeOptions { get; }

    public bool HasSelfReadPermission => _access.HasPermission(SelfReadPermission);
    public bool HasReadPermission => _access.HasPermission(ReadPermission);
    public bool HasSelfExplainPermission => _access.HasPermission(SelfExplainPermission);
    public bool HasResolvePermission => _access.HasPermission(ResolvePermission);

    public bool CanView =>
        HasReadPermission
        || HasSelfReadPermission
        || HasSelfExplainPermission
        || HasResolvePermission;

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

    public string EmployeeQuery
    {
        get => _employeeQuery;
        set => SetField(ref _employeeQuery, value ?? string.Empty);
    }

    public AttendanceViolationBranchOption? SelectedBranch
    {
        get => _selectedBranch;
        set => SetField(ref _selectedBranch, value);
    }

    public AttendanceViolationRowView? ExplainRow
    {
        get => _explainRow;
        private set
        {
            if (!SetField(ref _explainRow, value)) return;
            OnPropertyChanged(nameof(HasExplainModal));
            OnPropertyChanged(nameof(ExplainTitle));
            OnPropertyChanged(nameof(ExplainFact));
            OnPropertyChanged(nameof(CanSubmitExplanation));
        }
    }

    public bool HasExplainModal => ExplainRow is not null;
    public string ExplainTitle => ExplainRow is null
        ? string.Empty
        : $"{ExplainRow.ViolationText} · {ExplainRow.DateText}";
    public string ExplainFact => ExplainRow?.FactText ?? string.Empty;

    public string Explanation
    {
        get => _explanation;
        set
        {
            if (!SetField(ref _explanation, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitExplanation));
        }
    }

    public AttendanceViolationRowView? ReviewRow
    {
        get => _reviewRow;
        private set
        {
            if (!SetField(ref _reviewRow, value)) return;
            OnPropertyChanged(nameof(HasReviewModal));
            OnPropertyChanged(nameof(ReviewTitle));
            OnPropertyChanged(nameof(ReviewEmployee));
            OnPropertyChanged(nameof(ReviewExplanation));
            OnPropertyChanged(nameof(ReviewSnapshot));
            OnPropertyChanged(nameof(CanSubmitConclusion));
        }
    }

    public bool HasReviewModal => ReviewRow is not null;
    public string ReviewTitle => ReviewRow is null
        ? string.Empty
        : $"{ReviewRow.ViolationText} · {ReviewRow.DateText}";
    public string ReviewEmployee => ReviewRow?.EmployeeText ?? string.Empty;
    public string ReviewExplanation => ReviewRow?.Source.Case?.Explanation ?? string.Empty;
    public string ReviewSnapshot => ReviewRow?.Source.Case?.ViolationDetailSnapshot ?? string.Empty;

    public AttendanceViolationOutcomeOption? SelectedOutcome
    {
        get => _selectedOutcome;
        set
        {
            if (!SetField(ref _selectedOutcome, value)) return;
            OnPropertyChanged(nameof(CanSubmitConclusion));
        }
    }

    public string ReviewNote
    {
        get => _reviewNote;
        set
        {
            if (!SetField(ref _reviewNote, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitConclusion));
        }
    }

    public bool CanFilterScoped => _data is not null && !_data.Scope.SelfOnly;
    public bool HasBranchFilter => CanFilterScoped && BranchOptions.Count > 1;
    public bool CanRefresh => CanView && !IsBusy;
    public bool CanPagePrevious => !IsBusy && (_data?.Pagination.HasPrevious ?? false);
    public bool CanPageNext => !IsBusy && (_data?.Pagination.HasNext ?? false);
    public bool NoRows => _loaded && Rows.Count == 0;

    public bool CanSubmitExplanation =>
        !IsBusy
        && ExplainRow?.CanExplain == true
        && !string.IsNullOrWhiteSpace(Explanation);

    public bool CanSubmitConclusion =>
        !IsBusy
        && ReviewRow?.CanConclude == true
        && SelectedOutcome is not null
        && !string.IsNullOrWhiteSpace(ReviewNote);

    public string PeriodText => _data is null
        ? "—"
        : AttendanceViolationPresentation.Period(_data.Period);

    public string ScopeText => _data is null
        ? "—"
        : TimesheetPresentation.ScopeText(_data.Scope);

    public string CurrentCountText =>
        (_data?.Entries.Count(entry => entry.Violation is not null) ?? 0)
        .ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    public string WaitingCountText =>
        (_data?.Entries.Count(entry => entry.Case?.Status == "EXPLANATION_SUBMITTED") ?? 0)
        .ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    public string ReviewingCountText =>
        (_data?.Entries.Count(entry => entry.Case?.Status == "UNDER_REVIEW") ?? 0)
        .ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    public string ResolvedCountText =>
        (_data?.Entries.Count(entry => entry.Case?.Status == "RESOLVED") ?? 0)
        .ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    public string PaginationText
    {
        get
        {
            if (_data is null || _data.Pagination.Total <= 0) return "0 / 0 ngày công";
            var start = _data.Pagination.Offset + 1;
            var end = Math.Min(_data.Pagination.Offset + _data.Pagination.Limit, _data.Pagination.Total);
            return $"{start}–{end} / {_data.Pagination.Total} ngày công";
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

    public void OpenExplanation(AttendanceViolationRowView? row)
    {
        if (row?.CanExplain != true) return;
        ExplainRow = row;
        Explanation = string.Empty;
        SetMessage(string.Empty, false);
    }

    public void CloseExplanation()
    {
        ExplainRow = null;
        Explanation = string.Empty;
    }

    public void OpenConclusion(AttendanceViolationRowView? row)
    {
        if (row?.CanConclude != true) return;
        ReviewRow = row;
        SelectedOutcome = OutcomeOptions[0];
        ReviewNote = string.Empty;
        SetMessage(string.Empty, false);
    }

    public void CloseConclusion()
    {
        ReviewRow = null;
        SelectedOutcome = OutcomeOptions[0];
        ReviewNote = string.Empty;
    }

    public async Task SubmitExplanationAsync()
    {
        if (!CanSubmitExplanation || ExplainRow?.Source.Violation is null) return;

        var explanation = Explanation.Trim();
        if (explanation.Length is < 1 or > 2000)
        {
            SetMessage("Giải trình phải từ 1 đến 2.000 ký tự.", true);
            return;
        }

        var payload = new SubmitAttendanceViolationExplanationRequest(
            ExplainRow.Source.WorkDate,
            ExplainRow.Source.Violation.Kind,
            explanation);
        var slot = MutationSlot("attendance-violation-explain", payload);
        var key = MutationKey(slot, "desktop-attendance-violation-explain");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.SubmitExplanationAsync(payload, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            CloseExplanation();
            await ReloadCurrentAsync().ConfigureAwait(true);
            SetMessage("Đã gửi giải trình. Quản lý có thể xem xét và kết luận hồ sơ.", false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không gửi được giải trình."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task StartReviewAsync(AttendanceViolationRowView? row)
    {
        if (IsBusy || row?.CanStartReview != true || row.Source.Case is null) return;

        var payload = new ReviewAttendanceViolationRequest(
            row.Source.Case.Id,
            row.Source.Case.Version,
            "START_REVIEW");
        var slot = MutationSlot("attendance-violation-review", payload);
        var key = MutationKey(slot, "desktop-attendance-violation-review");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.ReviewAsync(payload, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            await ReloadCurrentAsync().ConfigureAwait(true);
            SetMessage("Hồ sơ đã chuyển sang trạng thái đang xem xét.", false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không bắt đầu xem xét được hồ sơ."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SubmitConclusionAsync()
    {
        if (!CanSubmitConclusion || ReviewRow?.Source.Case is null || SelectedOutcome is null) return;

        var note = ReviewNote.Trim();
        if (note.Length is < 1 or > 2000)
        {
            SetMessage("Kết luận xử lý phải từ 1 đến 2.000 ký tự.", true);
            return;
        }

        var payload = new ReviewAttendanceViolationRequest(
            ReviewRow.Source.Case.Id,
            ReviewRow.Source.Case.Version,
            "CONCLUDE",
            SelectedOutcome.Key,
            note);
        var slot = MutationSlot("attendance-violation-review", payload);
        var key = MutationKey(slot, "desktop-attendance-violation-review");
        var outcome = SelectedOutcome.Key;

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.ReviewAsync(payload, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            CloseConclusion();
            await ReloadCurrentAsync().ConfigureAwait(true);
            SetMessage(
                outcome == "CONFIRMED"
                    ? "Đã xác nhận vi phạm."
                    : "Đã chấp nhận giải trình.",
                false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không lưu được kết luận xử lý."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync(int offset)
    {
        if (!CanView || IsBusy) return;
        if (!TryPeriod(out var from, out var to, out var validation))
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
                employeeQuery,
                SelectedBranch?.Id,
                100,
                Math.Max(0, offset)).ConfigureAwait(true);
            ApplyData(data);
            _loaded = true;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tải được hồ sơ xử lý vi phạm."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadCurrentAsync()
    {
        if (!TryPeriod(out var from, out var to, out _)) return;
        var data = await _service.ListAsync(
            from!,
            to!,
            EmployeeQuery.Trim(),
            SelectedBranch?.Id,
            100,
            _data?.Pagination.Offset ?? 0).ConfigureAwait(true);
        ApplyData(data);
        _loaded = true;
    }

    private void ApplyData(AttendanceViolationHandlingResponseData data)
    {
        _data = data;

        if (TryParseDate(data.Period.From, out var from)) FromDate = from;
        if (TryParseDate(data.Period.To, out var to)) ToDate = to;

        var selectedBranchId = SelectedBranch?.Id;
        BranchOptions.Clear();
        BranchOptions.Add(new AttendanceViolationBranchOption(string.Empty, "Tất cả chi nhánh được cấp"));
        foreach (var branch in data.Scope.Branches.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
            BranchOptions.Add(new AttendanceViolationBranchOption(branch.Id, $"{branch.Code} · {branch.Name}"));

        SelectedBranch = data.Scope.SelfOnly
            ? BranchOptions[0]
            : BranchOptions.FirstOrDefault(item => string.Equals(item.Id, selectedBranchId, StringComparison.Ordinal))
              ?? BranchOptions[0];

        Rows.Clear();
        foreach (var entry in data.Entries)
        {
            var canExplain = data.Capabilities.CanExplain && entry.Case is null && entry.Violation is not null;
            var canStartReview = data.Capabilities.CanReview && entry.Case?.Status == "EXPLANATION_SUBMITTED";
            var canConclude = data.Capabilities.CanReview
                              && entry.Case is not null
                              && entry.Case.Status is "EXPLANATION_SUBMITTED" or "UNDER_REVIEW";

            Rows.Add(new AttendanceViolationRowView(
                entry,
                WorkSchedulePresentation.DateText(entry.WorkDate),
                AttendanceViolationPresentation.Employee(entry),
                AttendanceViolationPresentation.Branch(entry),
                AttendanceViolationPresentation.ViolationLabel(entry),
                AttendanceViolationPresentation.ViolationDetail(entry),
                AttendanceViolationPresentation.Metric(entry),
                AttendanceViolationPresentation.CaseStatus(entry.Case),
                AttendanceViolationPresentation.Explanation(entry),
                AttendanceViolationPresentation.Conclusion(entry),
                AttendanceViolationPresentation.Version(entry),
                canExplain,
                canStartReview,
                canConclude,
                !canExplain && !canStartReview && !canConclude));
        }

        if (ExplainRow is not null)
        {
            var current = FindCurrentRow(ExplainRow);
            ExplainRow = current?.CanExplain == true ? current : null;
        }
        if (ReviewRow is not null)
        {
            var current = FindCurrentRow(ReviewRow);
            ReviewRow = current?.CanConclude == true ? current : null;
        }

        OnPropertyChanged(nameof(CanFilterScoped));
        OnPropertyChanged(nameof(HasBranchFilter));
        OnPropertyChanged(nameof(CurrentCountText));
        OnPropertyChanged(nameof(WaitingCountText));
        OnPropertyChanged(nameof(ReviewingCountText));
        OnPropertyChanged(nameof(ResolvedCountText));
        OnPropertyChanged(nameof(PeriodText));
        OnPropertyChanged(nameof(ScopeText));
        OnPropertyChanged(nameof(PaginationText));
        OnPropertyChanged(nameof(CanPagePrevious));
        OnPropertyChanged(nameof(CanPageNext));
        OnPropertyChanged(nameof(NoRows));
        RaiseActions();
    }

    private AttendanceViolationRowView? FindCurrentRow(AttendanceViolationRowView previous) =>
        Rows.FirstOrDefault(item =>
            string.Equals(item.Source.Employee.Id, previous.Source.Employee.Id, StringComparison.Ordinal)
            && string.Equals(item.Source.WorkDate, previous.Source.WorkDate, StringComparison.Ordinal)
            && string.Equals(
                item.Source.Violation?.Kind ?? item.Source.Case?.ViolationKind,
                previous.Source.Violation?.Kind ?? previous.Source.Case?.ViolationKind,
                StringComparison.Ordinal));

    private bool TryPeriod(out string? from, out string? to, out string validation)
    {
        from = null;
        to = null;
        validation = string.Empty;

        if (FromDate is null || ToDate is null)
        {
            validation = "Chọn đầy đủ Từ ngày và Đến ngày.";
            return false;
        }

        var start = FromDate.Value.Date;
        var end = ToDate.Value.Date;
        if (end < start)
        {
            validation = "Khoảng thời gian xử lý vi phạm không hợp lệ.";
            return false;
        }

        if ((end - start).TotalDays + 1 > 93)
        {
            validation = "Mỗi lần chỉ xem tối đa 93 ngày.";
            return false;
        }

        from = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        to = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

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
        OnPropertyChanged(nameof(CanSubmitExplanation));
        OnPropertyChanged(nameof(CanSubmitConclusion));
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(HasSelfReadPermission));
        OnPropertyChanged(nameof(HasReadPermission));
        OnPropertyChanged(nameof(HasSelfExplainPermission));
        OnPropertyChanged(nameof(HasResolvePermission));
        OnPropertyChanged(nameof(CanView));
        OnPropertyChanged(nameof(CanFilterScoped));
        OnPropertyChanged(nameof(HasBranchFilter));
        OnPropertyChanged(nameof(CurrentCountText));
        OnPropertyChanged(nameof(WaitingCountText));
        OnPropertyChanged(nameof(ReviewingCountText));
        OnPropertyChanged(nameof(ResolvedCountText));
        OnPropertyChanged(nameof(PeriodText));
        OnPropertyChanged(nameof(ScopeText));
        OnPropertyChanged(nameof(PaginationText));
        OnPropertyChanged(nameof(NoRows));
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
