using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Workforce;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _overtimeCloseoutShellWired;

    private void WireOvertimeCloseoutWorkspace()
    {
        if (_overtimeCloseoutShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeWorkforceNavigationShell();
        _viewModel.InitializeOvertimeCloseoutShell();

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new OvertimeCloseoutService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new OvertimeCloseoutView(new OvertimeCloseoutViewModel(
            service,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>()));

        while (workspaceTabs.Items.Count <= 55)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[55] = new TabItem { Content = view };

        _overtimeCloseoutShellWired = true;
    }

    private async void OvertimeCloseout_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateOvertimeCloseoutAsync();
        ApplyOvertimeCloseoutHeader();
    }

    private void ApplyOvertimeCloseoutHeader()
    {
        if (!_viewModel.IsOvertimeCloseoutSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Tăng ca & chốt công");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Quản lý tăng ca theo phê duyệt và chốt kỳ công thành đầu vào sạch cho tính lương.");
    }
}
