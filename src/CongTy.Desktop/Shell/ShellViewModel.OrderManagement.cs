using System.ComponentModel;
using System.Windows.Input;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _orderManagementSelectionObserverAttached;

    public bool CanViewOrderManagement=>CanViewSales;
    public bool IsOrderManagementSelected=>SelectedWorkspaceIndex==37;
    public ICommand NavigateOrderManagementCommand=>new ShellAsyncCommand(NavigateOrderManagementAsync,()=>CanViewOrderManagement);

    public Task NavigateOrderManagementAsync()
    {
        EnsureOrderManagementSelectionObserver();
        SetSelectedNavigation("sales.order-management");
        IsSalesOpen=true;
        if(!CanViewOrderManagement)
        {
            WorkspaceMessage="Tài khoản chưa được cấp quyền xem Quản lý đơn hàng.";
            return Task.CompletedTask;
        }

        WorkspaceMessage=string.Empty;
        SelectedWorkspaceIndex=37;
        return Task.CompletedTask;
    }

    private void EnsureOrderManagementSelectionObserver()
    {
        if(_orderManagementSelectionObserverAttached)return;
        PropertyChanged+=OrderManagementShellPropertyChanged;
        _orderManagementSelectionObserverAttached=true;
    }

    private void OrderManagementShellPropertyChanged(object? sender,PropertyChangedEventArgs e)
    {
        if(e.PropertyName==nameof(SelectedWorkspaceIndex))OnPropertyChanged(nameof(IsOrderManagementSelected));
        if(e.PropertyName==nameof(CanViewSales))OnPropertyChanged(nameof(CanViewOrderManagement));
    }
}
