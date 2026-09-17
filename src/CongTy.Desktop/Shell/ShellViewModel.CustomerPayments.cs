using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string CustomerPaymentReadPermission = "core.customer-payment.read";
    private bool _customerPaymentSelectionObserverAttached;

    public bool CanViewCustomerPayments => _access.HasPermission(CustomerPaymentReadPermission);
    public bool IsCustomerPaymentsSelected => SelectedWorkspaceIndex == 40;

    internal void InitializeCustomerPaymentShell() => EnsureCustomerPaymentSelectionObserver();

    public Task NavigateCustomerPaymentsAsync()
    {
        EnsureCustomerPaymentSelectionObserver();
        SetSelectedNavigation("accounting.customer-payments");
        IsAccountingOpen = true;

        if (!CanViewCustomerPayments)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Thu tiền khách hàng.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 40;
        return Task.CompletedTask;
    }

    private void EnsureCustomerPaymentSelectionObserver()
    {
        if (_customerPaymentSelectionObserverAttached) return;

        PropertyChanged += CustomerPaymentShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewCustomerPayments));
            if (!CanViewCustomerPayments && IsCustomerPaymentsSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Thu tiền khách hàng.";
        };
        _customerPaymentSelectionObserverAttached = true;
    }

    private void CustomerPaymentShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsCustomerPaymentsSelected));
    }
}
