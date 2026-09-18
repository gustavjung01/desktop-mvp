using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Access;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _userDirectoryShellWired;

    private void WireUserDirectoryWorkspace()
    {
        if (_userDirectoryShellWired) return;

        var sidebarButton = FindVisualChildren<Button>(this)
            .FirstOrDefault(button =>
                FindVisualChildren<TextBlock>(button)
                    .Any(text => text.Text == "Người dùng"));
        if (sidebarButton is null) return;

        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeUserDirectoryShell();

        sidebarButton.IsEnabled = true;
        BindingOperations.SetBinding(
            sidebarButton,
            Button.TagProperty,
            new Binding(nameof(ShellViewModel.IsUserDirectorySelected)));
        BindingOperations.SetBinding(
            sidebarButton,
            UIElement.VisibilityProperty,
            new Binding(nameof(ShellViewModel.CanViewUserDirectory))
            {
                Converter = (IValueConverter)FindResource("BooleanToVisibilityConverter")
            });
        sidebarButton.Click += UserDirectory_OnClick;

        var app = (CongTy.Desktop.App)Application.Current;
        var apiClient = app.ResolveRequired<CompanyApiClient>();
        var session = app.ResolveRequired<IAuthenticatedSessionAccessor>();
        var idempotencyKeys = app.ResolveRequired<ICanonicalIdempotencyKeyProvider>();
        var access = app.ResolveRequired<IAccessStateService>();
        var userReadService = new UserDirectoryReadService(apiClient, session);
        var view = new UserDirectoryView(
            new UserDirectoryViewModel(
                userReadService,
                new UserDirectoryMutationService(apiClient, session),
                new EmployeeDirectoryReadService(apiClient, session),
                new AccessRoleReadService(apiClient, session),
                idempotencyKeys,
                access),
            new UserScopeViewModel(
                userReadService,
                new UserScopeService(apiClient, session),
                idempotencyKeys,
                access));

        while (workspaceTabs.Items.Count <= 53)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[53] = new TabItem { Content = view };

        _userDirectoryShellWired = true;
    }

    private async void UserDirectory_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateUserDirectoryAsync();
        ApplyUserDirectoryHeader();
    }

    private void ApplyUserDirectoryHeader()
    {
        if (!_viewModel.IsUserDirectorySelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ VÀ PHÂN QUYỀN");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Người dùng");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Quản lý tài khoản sử dụng hệ thống, liên kết nhân sự và phân quyền theo công việc.");
    }
}
