using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string SupplierPaymentReadPermission = "core.supplier-payment.read";
    private bool _supplierPaymentSelectionObserverAttached;

    public bool CanViewSupplierPayments => _access.HasPermission(SupplierPaymentReadPermission);
    public bool IsSupplierPaymentsSelected => SelectedWorkspaceIndex == 43;

    internal void InitializeSupplierPaymentShell() => EnsureSupplierPaymentSelectionObserver();

    public Task NavigateSupplierPaymentsAsync()
    {
        EnsureSupplierPaymentSelectionObserver();
        SetSelectedNavigation("accounting.supplier-payments");
        IsAccountingOpen = true;

        if (!CanViewSupplierPayments)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Thanh toán nhà cung cấp.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 43;
        return Task.CompletedTask;
    }

    private void EnsureSupplierPaymentSelectionObserver()
    {
        if (_supplierPaymentSelectionObserverAttached) return;

        PropertyChanged += SupplierPaymentShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewSupplierPayments));
            if (!CanViewSupplierPayments && IsSupplierPaymentsSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Thanh toán nhà cung cấp.";
        };
        _supplierPaymentSelectionObserverAttached = true;
    }

    private void SupplierPaymentShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsSupplierPaymentsSelected));
    }
}
