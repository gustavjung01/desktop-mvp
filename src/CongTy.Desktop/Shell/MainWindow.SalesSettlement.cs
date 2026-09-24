using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Accounting;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _salesSettlementShellWired;
    private SalesSettlementReconciliationViewModel? _salesSettlementViewModel;

    private void WireSalesSettlementReconciliationWorkspace()
    {
        if (_salesSettlementShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new SalesSettlementReconciliationService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        _salesSettlementViewModel = new SalesSettlementReconciliationViewModel(
            service,
            app.ResolveRequired<IAccessStateService>());
        var view = new SalesSettlementReconciliationView(_salesSettlementViewModel);
        view.NavigationRequested += SalesSettlementNavigationRequested;

        while (workspaceTabs.Items.Count <= WorkspaceSlots.SalesSettlement)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.SalesSettlement] = new TabItem { Content = view };

        _salesSettlementShellWired = true;
    }

    private async void AccountingSalesSettlement_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateSalesSettlementAsync();
        if (_viewModel.CanViewSalesSettlement && _salesSettlementViewModel is not null)
            await _salesSettlementViewModel.EnsureLoadedAsync();
    }

    private async void SalesSettlementNavigationRequested(string destination)
    {
        switch (destination)
        {
            case "receivables":
                await _viewModel.NavigateReceivablesAsync();
                ApplyReceivablesHeader();
                break;
            case "cod":
                await _viewModel.NavigateCodAccountingAsync();
                break;
            case "sales-orders":
                await _viewModel.NavigateSalesAsync();
                break;
            case "trip-reconciliation":
                await _viewModel.NavigateTripReconciliationAsync();
                break;
        }
    }
}
