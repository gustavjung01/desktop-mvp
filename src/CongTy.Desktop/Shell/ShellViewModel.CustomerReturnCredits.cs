using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string CustomerReturnCreditReadPermission = "core.customer-return-credit.read";
    private bool _customerReturnCreditSelectionObserverAttached;

    public bool CanViewCustomerReturnCredits => _access.HasPermission(CustomerReturnCreditReadPermission);
    public bool IsCustomerReturnCreditsSelected => SelectedWorkspaceIndex == 41;

    internal void InitializeCustomerReturnCreditShell() => EnsureCustomerReturnCreditSelectionObserver();

    public Task NavigateCustomerReturnCreditsAsync()
    {
        EnsureCustomerReturnCreditSelectionObserver();
        SetSelectedNavigation("accounting.customer-return-credits");
        IsAccountingOpen = true;

        if (!CanViewCustomerReturnCredits)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Điều chỉnh công nợ hàng trả.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 41;
        return Task.CompletedTask;
    }

    private void EnsureCustomerReturnCreditSelectionObserver()
    {
        if (_customerReturnCreditSelectionObserverAttached) return;
        PropertyChanged += CustomerReturnCreditShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewCustomerReturnCredits));
            if (!CanViewCustomerReturnCredits && IsCustomerReturnCreditsSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Điều chỉnh công nợ hàng trả.";
        };
        _customerReturnCreditSelectionObserverAttached = true;
    }

    private void CustomerReturnCreditShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsCustomerReturnCreditsSelected));
    }
}
