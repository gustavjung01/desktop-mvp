using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CongTy.Desktop.Sales;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _orderManagementShellWired;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        WireOrderManagementWorkspace();
        WireCustomerOnboardingWorkspace();
        WireReceivablesWorkspace();
        WireCustomerPaymentsWorkspace();
        WireCustomerReturnCreditsWorkspace();
    }

    private void WireOrderManagementWorkspace()
    {
        if(_orderManagementShellWired)return;

        var sidebarButton=FindVisualChildren<Button>(this)
            .FirstOrDefault(button=>FindVisualChildren<TextBlock>(button).Any(text=>text.Text=="Quản lý đơn hàng"));
        if(sidebarButton is null)return;

        sidebarButton.IsEnabled=true;
        BindingOperations.SetBinding(sidebarButton,Button.TagProperty,new Binding(nameof(ShellViewModel.IsOrderManagementSelected)));
        BindingOperations.SetBinding(sidebarButton,UIElement.VisibilityProperty,new Binding(nameof(ShellViewModel.CanViewOrderManagement))
        {
            Converter=(IValueConverter)FindResource("BooleanToVisibilityConverter")
        });
        sidebarButton.Click+=OrderManagement_OnClick;

        var workspaceTabs=FindLogicalParent<TabControl>(HomeHost);
        if(workspaceTabs is null)return;
        while(workspaceTabs.Items.Count<=37)workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[37]=new TabItem{Content=new OrderManagementView()};
        _orderManagementShellWired=true;
    }

    private async void OrderManagement_OnClick(object sender,RoutedEventArgs e)=>
        await _viewModel.NavigateOrderManagementAsync();

    private static T? FindLogicalParent<T>(DependencyObject start) where T:DependencyObject
    {
        DependencyObject? current=start;
        while(current is not null)
        {
            if(current is T typed)return typed;
            current=LogicalTreeHelper.GetParent(current);
        }
        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T:DependencyObject
    {
        for(var index=0;index<VisualTreeHelper.GetChildrenCount(root);index++)
        {
            var child=VisualTreeHelper.GetChild(root,index);
            if(child is T typed)yield return typed;
            foreach(var nested in FindVisualChildren<T>(child))yield return nested;
        }
    }
}
