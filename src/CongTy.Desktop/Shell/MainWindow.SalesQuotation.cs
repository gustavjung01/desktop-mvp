using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Sales;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _salesQuotationShellWired;

    private void WireSalesQuotationWorkspace()
    {
        if (_salesQuotationShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeSalesQuotationShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var idempotency = app.ResolveRequired<ICanonicalIdempotencyKeyProvider>();
        var service = new DataExchangeService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>(),
            idempotency);
        var viewModel = new SalesQuotationViewModel(
            service,
            app.ResolveRequired<IAccessStateService>(),
            idempotency);

        while (workspaceTabs.Items.Count <= WorkspaceSlots.SalesQuotation)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.SalesQuotation] = new TabItem
        {
            Content = new SalesQuotationView(viewModel)
        };

        _salesQuotationShellWired = true;
    }

    private async void SalesQuotation_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateSalesQuotationAsync();
}
