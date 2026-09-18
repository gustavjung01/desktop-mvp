using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed class EmployeeDirectoryViewModel : INotifyPropertyChanged
{
    private const string EmployeeRead = "core.employee.read";
    private const string EmployeeWrite = "core.employee.write";
    private const string BranchRead = "core.branch.read";

    private readonly IEmployeeDirectoryReadService _readService;
    private readonly IAccessStateService _access;
    private readonly List<EmployeeDirectoryData> _employees = [];
    private readonly List<EmployeeDirectoryBranchData> _branches = [];

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _searchTerm = string.Empty;
    private EmployeeStatusFilterOption _selectedStatusFilter;
    private EmployeeBranchFilterOption _selectedBranchFilter;
    private bool _isEditorOpen;
    private bool _isCreateMode;
    private string _draftCode = string.Empty;
    private string _draftFullName = string.Empty;
    private string _draftJobTitle = string.Empty;
    private string _draftPhone = string.Empty;
    private string _draftEmail = string.Empty;
    private string _draftBranchId = string.Empty;
    private long _accessGeneration;
    private long _loadGeneration;
    private CancellationTokenSource? _loadCts;

    public EmployeeDirectoryViewModel(
        IEmployeeDirectoryReadService readService,
        IAccessStateService access)
    {
        _readService = readService;
        _access = access;
        _selectedStatusFilter = StatusFilters[0];
        _selectedBranchFilter = new EmployeeBranchFilterOption("all", "Tất cả chi nhánh");
        BranchFilters.Add(_selectedBranchFilter);
        BranchFilters.Add(new EmployeeBranchFilterOption("unassigned", "Chưa phân công"));
        DraftBranchOptions.Add(new EmployeeBranchOption(string.Empty, "Chưa phân công"));

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loadCts?.Cancel();
            _loadCts = null;
            _loaded = false;
            _employees.Clear();
            _branches.Clear();
            VisibleEmployees.Clear();
            ResetBranchOptions();
            CloseEditor();
            Message = string.Empty;
            MessageIsError = false;
            IsBusy = false;
            RaiseAccess();
            RaiseSummary();
            if (CanViewEmployees && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<EmployeeDirectoryRowView> VisibleEmployees { get; } = [];
    public ObservableCollection<EmployeeBranchFilterOption> BranchFilters { get; } = [];
    public ObservableCollection<EmployeeBranchOption> DraftBranchOptions { get; } = [];

    public IReadOnlyList<EmployeeStatusFilterOption> StatusFilters { get; } =
    [
        new("all", "Tất cả trạng thái"),
        new("active", "Đang làm việc"),
        new("inactive", "Ngừng làm việc")
    ];

    public bool CanViewEmployees => _access.HasPermission(EmployeeRead);
    public bool CanWriteEmployees => _access.HasPermission(EmployeeWrite);
    public bool CanReadBranches => _access.HasPermission(BranchRead);
    public bool CanOpenEditor => CanWriteEmployees && !IsBusy;

    // Lô đọc chỉ dựng form parity. Mutation được khóa để review riêng.
    public bool CanPersist => false;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanOpenEditor));
            OnPropertyChanged(nameof(RefreshText));
            OnPropertyChanged(nameof(ShowLoading));
        }
    }

    public string RefreshText => IsBusy ? "Đang cập nhật…" : "Cập nhật dữ liệu";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool ShowLoading => IsBusy && !_loaded;
    public bool IsEmpty => _loaded && VisibleEmployees.Count == 0;
    public string EmptyText => IsEmpty ? "Không tìm thấy hồ sơ nhân sự phù hợp." : string.Empty;

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

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (!SetField(ref _searchTerm, value ?? string.Empty)) return;
            ApplyFilter();
        }
    }

    public EmployeeStatusFilterOption SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (value is null || !SetField(ref _selectedStatusFilter, value)) return;
            ApplyFilter();
        }
    }

    public EmployeeBranchFilterOption SelectedBranchFilter
    {
        get => _selectedBranchFilter;
        set
        {
            if (value is null || !SetField(ref _selectedBranchFilter, value)) return;
            ApplyFilter();
        }
    }

    public string TotalEmployeesText => _employees.Count.ToString("N0");
    public string ActiveEmployeesText => _employees.Count(item => item.IsActive).ToString("N0");
    public string InactiveEmployeesHint => $"{_employees.Count(item => !item.IsActive):N0} hồ sơ đã ngừng làm việc";
    public string AssignedEmployeesText => _employees.Count(item => !string.IsNullOrWhiteSpace(item.BranchId)).ToString("N0");
    public string UnassignedEmployeesHint => $"{_employees.Count(item => string.IsNullOrWhiteSpace(item.BranchId)):N0} hồ sơ chưa gắn chi nhánh";
    public string VisibleEmployeeCountText => $"{VisibleEmployees.Count:N0} hồ sơ";

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        private set => SetField(ref _isEditorOpen, value);
    }

    public bool IsCreateMode
    {
        get => _isCreateMode;
        private set
        {
            if (!SetField(ref _isCreateMode, value)) return;
            OnPropertyChanged(nameof(EditorKicker));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(IsCodeEditable));
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }

    public string EditorKicker => IsCreateMode ? "HỒ SƠ MỚI" : "CẬP NHẬT HỒ SƠ";
    public string EditorTitle => IsCreateMode ? "Thêm nhân sự" : "Chỉnh sửa nhân sự";
    public bool IsCodeEditable => IsCreateMode;
    public string SaveButtonText => IsCreateMode ? "Tạo hồ sơ" : "Lưu thay đổi";

    public string DraftCode
    {
        get => _draftCode;
        set => SetField(ref _draftCode, NormalizeCode(value));
    }

    public string DraftFullName
    {
        get => _draftFullName;
        set => SetField(ref _draftFullName, value ?? string.Empty);
    }

    public string DraftJobTitle
    {
        get => _draftJobTitle;
        set => SetField(ref _draftJobTitle, value ?? string.Empty);
    }

    public string DraftPhone
    {
        get => _draftPhone;
        set => SetField(ref _draftPhone, value ?? string.Empty);
    }

    public string DraftEmail
    {
        get => _draftEmail;
        set => SetField(ref _draftEmail, value ?? string.Empty);
    }

    public string DraftBranchId
    {
        get => _draftBranchId;
        set => SetField(ref _draftBranchId, value ?? string.Empty);
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanViewEmployees || !_access.Current.IsAuthenticated) return;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (!CanViewEmployees || !_access.Current.IsAuthenticated) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var accessGeneration = _accessGeneration;
        var loadGeneration = ++_loadGeneration;

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;

        IReadOnlyList<EmployeeDirectoryData>? employees = null;
        IReadOnlyList<EmployeeDirectoryBranchData>? branches = null;
        var errors = new List<string>();

        try
        {
            try
            {
                employees = await _readService.ListEmployeesAsync(token).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                errors.Add(PublicError(exception, "Không tải được danh mục nhân sự."));
            }

            if (CanReadBranches)
            {
                try
                {
                    branches = await _readService.ListBranchesAsync(token).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    errors.Add(PublicError(exception, "Không tải được danh mục chi nhánh."));
                }
            }
            else
            {
                errors.Add("Tài khoản chưa được cấp quyền xem danh mục chi nhánh.");
            }

            if (token.IsCancellationRequested
                || accessGeneration != _accessGeneration
                || loadGeneration != _loadGeneration)
                return;

            RunOnUiThread(() =>
            {
                if (employees is not null)
                {
                    _employees.Clear();
                    _employees.AddRange(employees);
                    _loaded = true;
                }

                if (branches is not null)
                {
                    _branches.Clear();
                    _branches.AddRange(branches);
                }

                RebuildBranchOptions();
                ApplyFilter();
                RaiseSummary();
                MessageIsError = errors.Count > 0;
                Message = string.Join(" · ", errors.Distinct(StringComparer.Ordinal));
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (accessGeneration == _accessGeneration && loadGeneration == _loadGeneration)
                RunOnUiThread(() => IsBusy = false);
        }
    }

    public void OpenCreate()
    {
        if (!CanOpenEditor) return;

        IsCreateMode = true;
        DraftCode = string.Empty;
        DraftFullName = string.Empty;
        DraftJobTitle = string.Empty;
        DraftPhone = string.Empty;
        DraftEmail = string.Empty;
        DraftBranchId = DraftBranchOptions.FirstOrDefault(option => option.Id.Length > 0)?.Id ?? string.Empty;
        IsEditorOpen = true;
    }

    public void OpenEdit(EmployeeDirectoryData employee)
    {
        if (!CanOpenEditor) return;

        IsCreateMode = false;
        DraftCode = employee.Code;
        DraftFullName = employee.FullName;
        DraftJobTitle = employee.JobTitle ?? string.Empty;
        DraftPhone = employee.Phone ?? string.Empty;
        DraftEmail = employee.Email ?? string.Empty;
        DraftBranchId = employee.BranchId ?? string.Empty;
        IsEditorOpen = true;
    }

    public void CloseEditor()
    {
        IsEditorOpen = false;
    }

    private void ApplyFilter()
    {
        var search = EmployeeDirectoryPresentation.NormalizeSearch(SearchTerm);
        var branchMap = _branches.ToDictionary(branch => branch.Id, branch => branch, StringComparer.Ordinal);

        var rows = _employees
            .Where(employee =>
            {
                var matchesStatus = SelectedStatusFilter.Key switch
                {
                    "active" => employee.IsActive,
                    "inactive" => !employee.IsActive,
                    _ => true
                };
                if (!matchesStatus) return false;

                var matchesBranch = SelectedBranchFilter.Key switch
                {
                    "all" => true,
                    "unassigned" => string.IsNullOrWhiteSpace(employee.BranchId),
                    _ => string.Equals(employee.BranchId, SelectedBranchFilter.Key, StringComparison.Ordinal)
                };
                if (!matchesBranch) return false;

                if (string.IsNullOrWhiteSpace(search)) return true;

                branchMap.TryGetValue(employee.BranchId ?? string.Empty, out var branch);
                var haystack = EmployeeDirectoryPresentation.NormalizeSearch(
                    $"{employee.Code} {employee.FullName} {employee.JobTitle} {employee.Phone} {employee.Email} {branch?.Code} {branch?.Name}");
                return haystack.Contains(search, StringComparison.Ordinal);
            })
            .OrderBy(employee => employee.Code, StringComparer.Ordinal)
            .Select(employee =>
            {
                branchMap.TryGetValue(employee.BranchId ?? string.Empty, out var branch);
                var branchText = string.IsNullOrWhiteSpace(employee.BranchId)
                    ? "Chưa phân công chi nhánh"
                    : branch is null
                        ? "Chưa tải được thông tin chi nhánh"
                        : EmployeeDirectoryPresentation.BranchLabel(branch);

                return new EmployeeDirectoryRowView(
                    employee,
                    employee.Code,
                    employee.FullName,
                    string.IsNullOrWhiteSpace(employee.JobTitle) ? "Chưa khai báo chức danh" : employee.JobTitle.Trim(),
                    branchText,
                    string.IsNullOrWhiteSpace(employee.Phone) ? "Chưa có số điện thoại" : employee.Phone.Trim(),
                    string.IsNullOrWhiteSpace(employee.Email) ? "Chưa có email" : employee.Email.Trim(),
                    employee.IsActive ? "Đang làm việc" : "Ngừng làm việc",
                    employee.IsActive,
                    EmployeeDirectoryPresentation.DateTimeText(employee.UpdatedAt));
            })
            .ToArray();

        VisibleEmployees.Clear();
        foreach (var row in rows) VisibleEmployees.Add(row);

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(VisibleEmployeeCountText));
    }

    private void RebuildBranchOptions()
    {
        var selectedKey = SelectedBranchFilter.Key;
        BranchFilters.Clear();
        BranchFilters.Add(new EmployeeBranchFilterOption("all", "Tất cả chi nhánh"));
        BranchFilters.Add(new EmployeeBranchFilterOption("unassigned", "Chưa phân công"));
        foreach (var branch in _branches.OrderBy(branch => branch.Code, StringComparer.Ordinal))
            BranchFilters.Add(new EmployeeBranchFilterOption(branch.Id, EmployeeDirectoryPresentation.BranchLabel(branch)));

        _selectedBranchFilter = BranchFilters.FirstOrDefault(option => option.Key == selectedKey) ?? BranchFilters[0];
        OnPropertyChanged(nameof(SelectedBranchFilter));

        DraftBranchOptions.Clear();
        DraftBranchOptions.Add(new EmployeeBranchOption(string.Empty, "Chưa phân công"));
        foreach (var branch in _branches
                     .Where(branch => branch.IsActive)
                     .OrderBy(branch => branch.Code, StringComparer.Ordinal))
            DraftBranchOptions.Add(new EmployeeBranchOption(branch.Id, EmployeeDirectoryPresentation.BranchLabel(branch)));
    }

    private void ResetBranchOptions()
    {
        BranchFilters.Clear();
        BranchFilters.Add(new EmployeeBranchFilterOption("all", "Tất cả chi nhánh"));
        BranchFilters.Add(new EmployeeBranchFilterOption("unassigned", "Chưa phân công"));
        _selectedBranchFilter = BranchFilters[0];
        OnPropertyChanged(nameof(SelectedBranchFilter));

        DraftBranchOptions.Clear();
        DraftBranchOptions.Add(new EmployeeBranchOption(string.Empty, "Chưa phân công"));
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(CanViewEmployees));
        OnPropertyChanged(nameof(CanWriteEmployees));
        OnPropertyChanged(nameof(CanReadBranches));
        OnPropertyChanged(nameof(CanOpenEditor));
        OnPropertyChanged(nameof(CanPersist));
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(TotalEmployeesText));
        OnPropertyChanged(nameof(ActiveEmployeesText));
        OnPropertyChanged(nameof(InactiveEmployeesHint));
        OnPropertyChanged(nameof(AssignedEmployeesText));
        OnPropertyChanged(nameof(UnassignedEmployeesHint));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
    }

    private static string NormalizeCode(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return new string(value
            .ToUpperInvariant()
            .Where(character => char.IsLetterOrDigit(character) || character is '_' or '-')
            .Take(64)
            .ToArray());
    }

    private static string PublicError(Exception exception, string fallback) =>
        exception is CanonicalApiException canonical
            ? CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(canonical),
                canonical.RequestId)
            : fallback;

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
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