using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed class UserDirectoryViewModel : INotifyPropertyChanged
{
    private const string UserRead = "core.user.read";
    private const string UserWrite = "core.user.write";
    private const string UserRoleWrite = "core.user-role.write";
    private const string EmployeeRead = "core.employee.read";
    private const string RoleRead = "core.role.read";

    private readonly IUserDirectoryReadService _userReadService;
    private readonly IEmployeeDirectoryReadService _employeeReadService;
    private readonly IAccessRoleReadService _roleReadService;
    private readonly IAccessStateService _access;
    private readonly List<AccessUserData> _users = [];
    private readonly List<EmployeeDirectoryData> _employees = [];
    private readonly List<AccessRoleData> _roles = [];

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _searchTerm = string.Empty;
    private UserStatusFilterOption _selectedStatusFilter;
    private long _accessGeneration;
    private long _loadGeneration;
    private CancellationTokenSource? _loadCts;

    public UserDirectoryViewModel(
        IUserDirectoryReadService userReadService,
        IEmployeeDirectoryReadService employeeReadService,
        IAccessRoleReadService roleReadService,
        IAccessStateService access)
    {
        _userReadService = userReadService;
        _employeeReadService = employeeReadService;
        _roleReadService = roleReadService;
        _access = access;
        _selectedStatusFilter = StatusFilters[0];

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loadCts?.Cancel();
            _loadCts = null;
            _loaded = false;
            _users.Clear();
            _employees.Clear();
            _roles.Clear();
            VisibleUsers.Clear();
            Message = string.Empty;
            MessageIsError = false;
            IsBusy = false;
            RaiseAccess();
            RaiseSummary();
            if (CanViewUsers && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<UserDirectoryRowView> VisibleUsers { get; } = [];

    public IReadOnlyList<UserStatusFilterOption> StatusFilters { get; } =
    [
        new("all", "Tất cả"),
        new("active", "Đang hoạt động"),
        new("inactive", "Ngừng sử dụng")
    ];

    public bool CanViewUsers => _access.HasPermission(UserRead);
    public bool CanWriteUsers => _access.HasPermission(UserWrite);
    public bool CanManageUserRoles => _access.HasPermission(UserRoleWrite);
    public bool CanReadEmployees => _access.HasPermission(EmployeeRead);
    public bool CanReadRoles => _access.HasPermission(RoleRead);

    // Lô 1 chỉ đọc. Các thao tác thay đổi được bật sau khi hoàn tất contract mutation.
    public bool CanMutateUsers => false;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(RefreshText));
            OnPropertyChanged(nameof(ShowLoading));
        }
    }

    public string RefreshText => IsBusy ? "Đang tải…" : "Tải lại";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool ShowLoading => IsBusy && !_loaded;
    public bool IsEmpty => _loaded && VisibleUsers.Count == 0;
    public string EmptyText => IsEmpty ? "Không có người dùng phù hợp." : string.Empty;

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

    public UserStatusFilterOption SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (value is null || !SetField(ref _selectedStatusFilter, value)) return;
            ApplyFilter();
        }
    }

    public string TotalUsersText => _users.Count.ToString("N0");
    public string ActiveUsersText => _users.Count(user => user.IsActive).ToString("N0");
    public string InactiveUsersText => _users.Count(user => !user.IsActive).ToString("N0");
    public string VisibleUserCountText => $"{VisibleUsers.Count:N0} kết quả";

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanViewUsers || !_access.Current.IsAuthenticated) return;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (!CanViewUsers || !_access.Current.IsAuthenticated) return;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var accessGeneration = _accessGeneration;
        var loadGeneration = ++_loadGeneration;

        IsBusy = true;
        Message = string.Empty;
        MessageIsError = false;

        IReadOnlyList<AccessUserData>? users = null;
        IReadOnlyList<EmployeeDirectoryData>? employees = null;
        IReadOnlyList<AccessRoleData>? roles = null;
        var errors = new List<string>();

        try
        {
            var usersTask = TryLoadAsync(
                () => _userReadService.ListUsersAsync(token),
                "Không tải được danh sách người dùng.",
                errors);

            Task<IReadOnlyList<EmployeeDirectoryData>?> employeesTask = CanReadEmployees
                ? TryLoadAsync(
                    () => _employeeReadService.ListEmployeesAsync(token),
                    "Không tải được danh mục nhân sự.",
                    errors)
                : Task.FromResult<IReadOnlyList<EmployeeDirectoryData>?>(null);

            Task<IReadOnlyList<AccessRoleData>?> rolesTask = CanReadRoles
                ? TryLoadAsync(
                    () => _roleReadService.ListRolesAsync(token),
                    "Không tải được danh sách vai trò.",
                    errors)
                : Task.FromResult<IReadOnlyList<AccessRoleData>?>(null);

            if (!CanReadEmployees)
                errors.Add("Tài khoản chưa được cấp quyền xem danh mục nhân sự.");
            if (!CanReadRoles)
                errors.Add("Tài khoản chưa được cấp quyền xem danh sách vai trò.");

            users = await usersTask.ConfigureAwait(false);
            employees = await employeesTask.ConfigureAwait(false);
            roles = await rolesTask.ConfigureAwait(false);

            if (token.IsCancellationRequested
                || accessGeneration != _accessGeneration
                || loadGeneration != _loadGeneration)
                return;

            RunOnUiThread(() =>
            {
                if (users is not null)
                {
                    _users.Clear();
                    _users.AddRange(users);
                    _loaded = true;
                }

                if (employees is not null)
                {
                    _employees.Clear();
                    _employees.AddRange(employees);
                }

                if (roles is not null)
                {
                    _roles.Clear();
                    _roles.AddRange(roles);
                }

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

    private async Task<IReadOnlyList<T>?> TryLoadAsync<T>(
        Func<Task<IReadOnlyList<T>>> loader,
        string fallback,
        List<string> errors)
    {
        try
        {
            return await loader().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            lock (errors)
            {
                errors.Add(PublicError(exception, fallback));
            }
            return null;
        }
    }

    private void ApplyFilter()
    {
        var search = EmployeeDirectoryPresentation.NormalizeSearch(SearchTerm);
        var employeeMap = _employees.ToDictionary(employee => employee.Id, employee => employee, StringComparer.Ordinal);
        var roleMap = _roles.ToDictionary(role => role.Id, role => role, StringComparer.Ordinal);

        var rows = _users
            .Where(user =>
            {
                var matchesStatus = SelectedStatusFilter.Key switch
                {
                    "active" => user.IsActive,
                    "inactive" => !user.IsActive,
                    _ => true
                };
                if (!matchesStatus) return false;

                if (string.IsNullOrWhiteSpace(search)) return true;

                employeeMap.TryGetValue(user.EmployeeId ?? string.Empty, out var employee);
                var roleText = UserDirectoryPresentation.RoleText(user, roleMap);
                var haystack = EmployeeDirectoryPresentation.NormalizeSearch(
                    $"{user.LoginName} {employee?.FullName} {employee?.Code} {user.EmployeeFullName} {user.EmployeeCode} {roleText}");
                return haystack.Contains(search, StringComparison.Ordinal);
            })
            .OrderBy(user => user.LoginName, StringComparer.OrdinalIgnoreCase)
            .Select(user =>
            {
                employeeMap.TryGetValue(user.EmployeeId ?? string.Empty, out var employee);
                var employeeName = employee?.FullName?.Trim();
                if (string.IsNullOrWhiteSpace(employeeName)) employeeName = user.EmployeeFullName?.Trim();
                if (string.IsNullOrWhiteSpace(employeeName)) employeeName = "Không xác định";

                var employeeCode = employee?.Code?.Trim();
                if (string.IsNullOrWhiteSpace(employeeCode)) employeeCode = user.EmployeeCode?.Trim();

                return new UserDirectoryRowView(
                    user,
                    user.LoginName,
                    employeeName,
                    string.IsNullOrWhiteSpace(employeeCode) ? "Chưa có mã nhân sự" : employeeCode,
                    UserDirectoryPresentation.RoleText(user, roleMap),
                    user.IsActive ? "Đang hoạt động" : "Ngừng sử dụng",
                    user.IsActive,
                    EmployeeDirectoryPresentation.DateTimeText(user.UpdatedAt),
                    user.IsActive ? "Ngừng sử dụng" : "Đưa vào sử dụng");
            })
            .ToArray();

        VisibleUsers.Clear();
        foreach (var row in rows) VisibleUsers.Add(row);

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(VisibleUserCountText));
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(CanViewUsers));
        OnPropertyChanged(nameof(CanWriteUsers));
        OnPropertyChanged(nameof(CanManageUserRoles));
        OnPropertyChanged(nameof(CanReadEmployees));
        OnPropertyChanged(nameof(CanReadRoles));
        OnPropertyChanged(nameof(CanMutateUsers));
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(TotalUsersText));
        OnPropertyChanged(nameof(ActiveUsersText));
        OnPropertyChanged(nameof(InactiveUsersText));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyText));
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
