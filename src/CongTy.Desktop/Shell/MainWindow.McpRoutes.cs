using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Settings;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _mcpRoutesShellWired;

    private void WireMcpRoutesWorkspace()
    {
        if (_mcpRoutesShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeMcpRoutesShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var service = new EmployeeMcpReportingService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var viewModel = new EmployeeMcpReportingViewModel(
            service,
            app.ResolveRequired<IAccessStateService>());
        var view = new EmployeeMcpReportingView(viewModel);
        view.CustomerOnboardingRequested += McpRoutesCustomerOnboardingRequested;

        while (workspaceTabs.Items.Count <= 50)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[50] = new TabItem { Content = view };

        _mcpRoutesShellWired = true;
    }

    private async void McpRoutes_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateMcpRoutesAsync();
        ApplyMcpRoutesHeader();
    }

    private async void McpRoutesCustomerOnboardingRequested(object? sender, EventArgs e) =>
        await _viewModel.NavigateCustomerOnboardingAsync();

    private void ApplyMcpRoutesHeader()
    {
        if (!_viewModel.IsMcpRoutesSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ THỊ TRƯỜNG");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Hiệu suất nhân viên thị trường");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Theo dõi tuyến, phiên đi thị trường, lượt ghé, ghi nhận có mặt, nhu cầu mua, đề nghị mở mã khách và đơn Công Ty trên cùng nguồn số liệu.");
    }
}
