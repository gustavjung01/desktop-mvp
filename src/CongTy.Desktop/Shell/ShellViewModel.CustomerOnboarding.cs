using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string CustomerOnboardingReadPermission = "core.customer-onboarding.read";
    private bool _customerOnboardingSelectionObserverAttached;

    public bool CanViewCustomerOnboarding => _access.HasPermission(CustomerOnboardingReadPermission);
    public bool IsCustomerOnboardingSelected => SelectedWorkspaceIndex == 38;

    public Task NavigateCustomerOnboardingAsync()
    {
        EnsureCustomerOnboardingSelectionObserver();
        SetSelectedNavigation("sales.customer-onboarding");
        IsSalesOpen = true;
        if (!CanViewCustomerOnboarding)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem đề nghị mở/liên kết mã khách.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 38;
        return Task.CompletedTask;
    }

    private void EnsureCustomerOnboardingSelectionObserver()
    {
        if (_customerOnboardingSelectionObserverAttached) return;
        PropertyChanged += CustomerOnboardingShellPropertyChanged;
        _customerOnboardingSelectionObserverAttached = true;
    }

    private void CustomerOnboardingShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex)) OnPropertyChanged(nameof(IsCustomerOnboardingSelected));
        if (e.PropertyName is nameof(IsWorkspace) or nameof(CanViewSales)) OnPropertyChanged(nameof(CanViewCustomerOnboarding));
    }
}
