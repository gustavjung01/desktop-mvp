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

        var sidebarButton = FindVisualChildren<Button>(this)
            .FirstOrDefault(button =>
                FindVisualChildren<TextBlock>(button)
                    .Any(text => text.Text == "Danh mục nhân sự"));
        if (sidebarButton is null) return;

        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeEmployeeDirectoryShell();

        sidebarButton.IsEnabled = true;
        BindingOperations.SetBinding(
            sidebarButton,
            Button.TagProperty,
            new Binding(nameof(ShellViewModel.IsEmployeeDirectorySelected)));
        BindingOperations.SetBinding(
            sidebarButton,
            UIElement.VisibilityProperty,
            new Binding(nameof(ShellViewModel.CanViewEmployeeDirectory))
            {
                Converter = (IValueConverter)FindResource("BooleanToVisibilityConverter")
            });
        sidebarButton.Click += EmployeeDirectory_OnClick;

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new EmployeeDirectoryReadService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new EmployeeDirectoryView(new EmployeeDirectoryViewModel(
            service,
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