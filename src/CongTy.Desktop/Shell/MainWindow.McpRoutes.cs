using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Settings;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _employeeMcpReportingShellWired;
    private bool _mcpRouteSettingsShellWired;
    private bool _customerOrderingContentShellWired;

    private void WireMcpRoutesWorkspace()
    {
        WireEmployeeMcpReportingWorkspace();
        WireMcpRouteSettingsWorkspace();
        WireCustomerOrderingContentWorkspace();
    }

    private void WireEmployeeMcpReportingWorkspace()
    {
        if (_employeeMcpReportingShellWired) return;
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
        view.CustomerOnboardingRequested += EmployeeMcpReportingCustomerOnboardingRequested;
        view.EmployeeDirectoryRequested += EmployeeMcpReportingEmployeeDirectoryRequested;

        while (workspaceTabs.Items.Count <= 50)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[50] = new TabItem { Content = view };

        _employeeMcpReportingShellWired = true;
    }

    private void WireMcpRouteSettingsWorkspace()
    {
        if (_mcpRouteSettingsShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeMcpRoutesShell();
        while (workspaceTabs.Items.Count <= 91)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[91] = new TabItem { Content = new McpRouteSettingsBoundaryView() };
        _mcpRouteSettingsShellWired = true;
    }

    private void WireCustomerOrderingContentWorkspace()
    {
        if (_customerOrderingContentShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeCustomerOrderingContentShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var service = new CustomerOrderingContentService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>(),
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>());
        var viewModel = new CustomerOrderingContentViewModel(
            service,
            app.ResolveRequired<IAccessStateService>(),
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>());

        while (workspaceTabs.Items.Count <= 90)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[90] = new TabItem { Content = new CustomerOrderingContentView(viewModel) };
        _customerOrderingContentShellWired = true;
    }

    private async void EmployeeMcpReporting_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateEmployeeMcpReportingAsync();
        ApplyEmployeeMcpReportingHeader();
    }

    private async void McpRouteSettings_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateMcpRouteSettingsAsync();
        ApplyMcpRouteSettingsHeader();
    }

    private async void CustomerOrderingContent_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateCustomerOrderingContentAsync();
        ApplyCustomerOrderingContentHeader();
    }

    private async void EmployeeMcpReportingCustomerOnboardingRequested(object? sender, EventArgs e) =>
        await _viewModel.NavigateCustomerOnboardingAsync();

    private async void EmployeeMcpReportingEmployeeDirectoryRequested(object? sender, EventArgs e)
    {
        await _viewModel.NavigateEmployeeDirectoryAsync();
        ApplyEmployeeDirectoryHeader();
    }

    private void ApplyEmployeeMcpReportingHeader()
    {
        if (!_viewModel.IsEmployeeMcpReportingSelected) return;
        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Hiệu suất nhân viên thị trường");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Theo dõi tuyến, phiên đi thị trường, lượt ghé, nhu cầu mua, đề nghị mở mã khách và đơn Công Ty trên số liệu canonical.");
    }

    private void ApplyMcpRouteSettingsHeader()
    {
        if (!_viewModel.IsMcpRouteSettingsSelected) return;
        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "CÀI ĐẶT CÔNG TY");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "MCP và tuyến");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Thiết lập tuyến dùng chung cho MCP PWA và Mobile; Desktop giữ fail-closed khi server chưa có contract workforce-safe.");
    }

    private void ApplyCustomerOrderingContentHeader()
    {
        if (!_viewModel.IsCustomerOrderingContentSelected) return;
        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "CÀI ĐẶT CÔNG TY");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Nội dung đặt hàng");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Thiết lập tiêu đề, nội dung chương trình, trạng thái hiển thị và banner Trang chủ dùng chung cho các kênh đặt hàng.");
    }
}
