using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _salesQuotationSelectionObserverAttached;

    public bool CanViewSalesQuotation =>
        _access.HasPermission("core.sales-order.read")
        && _access.HasPermission("core.product.read")
        && _access.HasPermission("core.price.read")
        && _access.HasPermission("core.customer.read");

    internal void InitializeSalesQuotationShell()
    {
        if (_salesQuotationSelectionObserverAttached) return;

        PropertyChanged += SalesQuotationShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewSalesQuotation));
            if (!CanViewSalesQuotation && IsSalesQuotationSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp đủ quyền xem Báo giá.";
        };
        _salesQuotationSelectionObserverAttached = true;
    }

    public Task NavigateSalesQuotationAsync()
    {
        SetSelectedNavigation("sales.quotations");
        IsSalesOpen = true;

        if (!CanViewSalesQuotation)
        {
            WorkspaceMessage = "Báo giá cần quyền xem đơn bán hàng, sản phẩm, giá bán và khách hàng.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.SalesQuotation;
        return Task.CompletedTask;
    }

    private void SalesQuotationShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsSalesQuotationSelected));
    }
}
