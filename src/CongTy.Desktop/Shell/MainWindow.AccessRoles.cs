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

        var sidebarButton = FindVisualChildren<Button>(this)
            .FirstOrDefault(button =>
                FindVisualChildren<TextBlock>(button)
                    .Any(text => text.Text == "Vai trò và phân quyền"));
        if (sidebarButton is null) return;

        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeAccessRolesShell();

        sidebarButton.IsEnabled = true;
        BindingOperations.SetBinding(
            sidebarButton,
            Button.TagProperty,
            new Binding(nameof(ShellViewModel.IsAccessRolesSelected)));
        BindingOperations.SetBinding(
            sidebarButton,
            UIElement.VisibilityProperty,
            new Binding(nameof(ShellViewModel.CanViewAccessRoles))
            {
                Converter = (IValueConverter)FindResource("BooleanToVisibilityConverter")
            });
        sidebarButton.Click += AccessRoles_OnClick;

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new AccessRoleReadService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new AccessRolesView(new AccessRolesViewModel(
            service,
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
