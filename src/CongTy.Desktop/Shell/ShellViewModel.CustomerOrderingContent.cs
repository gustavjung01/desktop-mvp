using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private static readonly HashSet<string> CustomerOrderingContentOwnerRoles =
        new(["system:security-owner", "system:implementation-owner"], StringComparer.Ordinal);
    private bool _customerOrderingContentSelectionObserverAttached;

    public bool CanViewCustomerOrderingContent =>
        _access.Current.IsAuthenticated
        && (_access.HasPermission("core.config.read")
            || _access.HasPermission("core.organization.write")
            || _access.Current.Roles.Any(CustomerOrderingContentOwnerRoles.Contains));

    public bool IsCustomerOrderingContentSelected => SelectedWorkspaceIndex == 90;

    internal void InitializeCustomerOrderingContentShell() => EnsureCustomerOrderingContentSelectionObserver();

    public Task NavigateCustomerOrderingContentAsync()
    {
        EnsureCustomerOrderingContentSelectionObserver();
        SetSelectedNavigation("settings.customer-ordering-content");
        IsCompanySettingsOpen = true;

        if (!CanViewCustomerOrderingContent)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Nội dung đặt hàng.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 90;
        return Task.CompletedTask;
    }

    private void EnsureCustomerOrderingContentSelectionObserver()
    {
        if (_customerOrderingContentSelectionObserverAttached) return;
        PropertyChanged += CustomerOrderingContentShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewCustomerOrderingContent));
            if (!CanViewCustomerOrderingContent && IsCustomerOrderingContentSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Nội dung đặt hàng.";
        };
        _customerOrderingContentSelectionObserverAttached = true;
    }

    private void CustomerOrderingContentShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsCustomerOrderingContentSelected));
    }
}
