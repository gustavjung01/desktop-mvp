using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Access;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _accessRolesShellWired;

    private void WireAccessRolesWorkspace()
    {
        if (_accessRolesShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeAccessRolesShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var apiClient = app.ResolveRequired<CompanyApiClient>();
        var session = app.ResolveRequired<IAuthenticatedSessionAccessor>();
        var readService = new AccessRoleReadService(apiClient, session);
        var mutationService = new AccessRoleMutationService(apiClient, session);
        var view = new AccessRolesView(new AccessRolesViewModel(
            readService,
            mutationService,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>()));

        while (workspaceTabs.Items.Count <= 51)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[51] = new TabItem { Content = view };

        _accessRolesShellWired = true;
    }

    private async void AccessRoles_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateAccessRolesAsync();
        ApplyAccessRolesHeader();
    }

    private void ApplyAccessRolesHeader()
    {
        if (!_viewModel.IsAccessRolesSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "PHÂN QUYỀN");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Vai trò và phân quyền");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Quản lý vai trò, trạng thái sử dụng và phạm vi quyền theo công việc.");
    }
}
