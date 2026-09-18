using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Access;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _employeeDirectoryShellWired;

    private void WireEmployeeDirectoryWorkspace()
    {
        if (_employeeDirectoryShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeEmployeeDirectoryShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var apiClient = app.ResolveRequired<CompanyApiClient>();
        var session = app.ResolveRequired<IAuthenticatedSessionAccessor>();
        var readService = new EmployeeDirectoryReadService(apiClient, session);
        var mutationService = new EmployeeDirectoryMutationService(apiClient, session);
        var view = new EmployeeDirectoryView(new EmployeeDirectoryViewModel(
            readService,
            mutationService,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>()));

        while (workspaceTabs.Items.Count <= 52)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[52] = new TabItem { Content = view };

        _employeeDirectoryShellWired = true;
    }

    private async void EmployeeDirectory_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateEmployeeDirectoryAsync();
        ApplyEmployeeDirectoryHeader();
    }

    private void ApplyEmployeeDirectoryHeader()
    {
        if (!_viewModel.IsEmployeeDirectorySelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Danh mục nhân sự");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Quản lý hồ sơ nhân sự, chức danh, thông tin liên hệ và đơn vị công tác.");
    }
}