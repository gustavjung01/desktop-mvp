using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Accounting;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _payablesShellWired;

    private void WirePayablesWorkspace()
    {
        if (_payablesShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializePayablesShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var service = new PayablesService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var viewModel = new PayablesViewModel(service, app.ResolveRequired<IAccessStateService>());
        var view = new PayablesView(viewModel);

        while (workspaceTabs.Items.Count <= 42)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[42] = new TabItem { Content = view };
        _payablesShellWired = true;
    }

    private async void Payables_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigatePayablesAsync();
        ApplyPayablesHeader();
    }

    private void ApplyPayablesHeader()
    {
        if (!_viewModel.IsPayablesSelected) return;
        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "KẾ TOÁN MUA HÀNG");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Công nợ phải trả");
        SetShellHeaderText(nameof(ShellViewModel.PageSubtitle),
            "Đối chiếu công nợ phát sinh tự động từ phiếu nhận hàng và phiếu trả nhà cung cấp.");
    }
}
