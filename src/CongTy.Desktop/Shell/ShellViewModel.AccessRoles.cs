using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string AccessRoleReadPermission = "core.role.read";
    private bool _accessRolesSelectionObserverAttached;

    public bool CanViewAccessRoles => _access.HasPermission(AccessRoleReadPermission);
    public bool IsAccessRolesSelected => SelectedWorkspaceIndex == 51;

    internal void InitializeAccessRolesShell() => EnsureAccessRolesSelectionObserver();

    public Task NavigateAccessRolesAsync()
    {
        EnsureAccessRolesSelectionObserver();
        SetSelectedNavigation("access.roles");
        IsAccessOpen = true;

        if (!CanViewAccessRoles)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Vai trò và phân quyền.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 51;
        return Task.CompletedTask;
    }

    private void EnsureAccessRolesSelectionObserver()
    {
        if (_accessRolesSelectionObserverAttached) return;

        PropertyChanged += AccessRolesShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewAccessRoles));
            if (!CanViewAccessRoles && IsAccessRolesSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Vai trò và phân quyền.";
        };
        _accessRolesSelectionObserverAttached = true;
    }

    private void AccessRolesShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsAccessRolesSelected));
    }
}
