using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Workforce;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _payrollFoundationShellWired;

    private void WirePayrollFoundationWorkspace()
    {
        if (_payrollFoundationShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeWorkforceNavigationShell();
        _viewModel.InitializePayrollFoundationShell();

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new PayrollFoundationService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new PayrollFoundationView(new PayrollFoundationViewModel(
            service,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>()));

        while (workspaceTabs.Items.Count <= WorkspaceSlots.PayrollFoundation)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.PayrollFoundation] = new TabItem { Content = view };

        _payrollFoundationShellWired = true;
    }

    private async void PayrollFoundation_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigatePayrollFoundationAsync();
        ApplyPayrollFoundationHeader();
    }

    private void ApplyPayrollFoundationHeader()
    {
        if (!_viewModel.IsPayrollFoundationSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Tính lương");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Thiết lập dữ liệu nền tính lương theo kỳ, theo nhân sự và theo ngày áp dụng.");
    }
}
