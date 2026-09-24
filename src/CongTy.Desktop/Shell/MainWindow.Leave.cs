using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Workforce;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _leaveShellWired;

    private void WireLeaveWorkspace()
    {
        if (_leaveShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeWorkforceNavigationShell();
        _viewModel.InitializeLeaveShell();

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new LeaveService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new LeaveView(new LeaveViewModel(
            service,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>()));

        while (workspaceTabs.Items.Count <= WorkspaceSlots.Leave)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.Leave] = new TabItem { Content = view };

        _leaveShellWired = true;
    }

    private async void Leave_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateLeaveAsync();
        ApplyLeaveHeader();
    }

    private void ApplyLeaveHeader()
    {
        if (!_viewModel.IsLeaveSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Nghỉ và đơn nghỉ");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Gửi, duyệt và theo dõi nghỉ phép theo đúng chế độ nghỉ và phạm vi được cấp.");
    }
}
