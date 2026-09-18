using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.Desktop.Sales;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _customerOnboardingShellWired;

    private void WireCustomerOnboardingWorkspace()
    {
        if (_customerOnboardingShellWired) return;
        _viewModel.InitializeCustomerOnboardingShell();
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;
        while (workspaceTabs.Items.Count <= 38) workspaceTabs.Items.Add(new TabItem());
        var view = ((CongTy.Desktop.App)Application.Current).ResolveRequired<CustomerOnboardingView>();
        workspaceTabs.Items[38] = new TabItem { Content = view };
        _customerOnboardingShellWired = true;
    }

    private async void CustomerOnboarding_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateCustomerOnboardingAsync();
}
