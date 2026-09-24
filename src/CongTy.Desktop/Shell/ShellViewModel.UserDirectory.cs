using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string UserDirectoryReadPermission = "core.user.read";
    private bool _userDirectorySelectionObserverAttached;

    public bool CanViewUserDirectory => _access.HasPermission(UserDirectoryReadPermission);
    public bool IsUserDirectorySelected => SelectedWorkspaceIndex == WorkspaceSlots.UserDirectory;

    internal void InitializeUserDirectoryShell() => EnsureUserDirectorySelectionObserver();

    public Task NavigateUserDirectoryAsync()
    {
        EnsureUserDirectorySelectionObserver();
        SetSelectedNavigation("access.users");
        IsAccessOpen = true;

        if (!CanViewUserDirectory)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Người dùng.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.UserDirectory;
        return Task.CompletedTask;
    }

    private void EnsureUserDirectorySelectionObserver()
    {
        if (_userDirectorySelectionObserverAttached) return;

        PropertyChanged += UserDirectoryShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewUserDirectory));
            if (!CanViewUserDirectory && IsUserDirectorySelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Người dùng.";
        };
        _userDirectorySelectionObserverAttached = true;
    }

    private void UserDirectoryShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsUserDirectorySelected));
    }
}
