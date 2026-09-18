using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string EmployeeDirectoryReadPermission = "core.employee.read";
    private bool _employeeDirectorySelectionObserverAttached;

    public bool CanViewEmployeeDirectory => _access.HasPermission(EmployeeDirectoryReadPermission);
    public bool IsEmployeeDirectorySelected => SelectedWorkspaceIndex == 52;

    internal void InitializeEmployeeDirectoryShell() => EnsureEmployeeDirectorySelectionObserver();

    public Task NavigateEmployeeDirectoryAsync()
    {
        EnsureEmployeeDirectorySelectionObserver();
        SetSelectedNavigation("access.employees");
        IsAccessOpen = true;

        if (!CanViewEmployeeDirectory)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Danh mục nhân sự.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 52;
        return Task.CompletedTask;
    }

    private void EnsureEmployeeDirectorySelectionObserver()
    {
        if (_employeeDirectorySelectionObserverAttached) return;

        PropertyChanged += EmployeeDirectoryShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewEmployeeDirectory));
            if (!CanViewEmployeeDirectory && IsEmployeeDirectorySelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Danh mục nhân sự.";
        };
        _employeeDirectorySelectionObserverAttached = true;
    }

    private void EmployeeDirectoryShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsEmployeeDirectorySelected));
    }
}