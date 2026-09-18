using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Settings;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _dataBackupShellWired;

    private void WireDataBackupWorkspace()
    {
        if (_dataBackupShellWired) return;

        var sidebarButton = FindVisualChildren<Button>(this)
            .FirstOrDefault(button =>
                FindVisualChildren<TextBlock>(button)
                    .Any(text => text.Text == "Thiết lập chung"));
        if (sidebarButton is null) return;

        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeDataBackupShell();

        sidebarButton.IsEnabled = true;
        BindingOperations.SetBinding(
            sidebarButton,
            Button.TagProperty,
            new Binding(nameof(ShellViewModel.IsDataBackupSelected)));
        BindingOperations.SetBinding(
            sidebarButton,
            UIElement.VisibilityProperty,
            new Binding(nameof(ShellViewModel.CanViewDataBackup))
            {
                Converter = (IValueConverter)FindResource("BooleanToVisibilityConverter")
            });
        sidebarButton.Click += DataBackup_OnClick;

        var app = (CongTy.Desktop.App)Application.Current;
        var idempotency = app.ResolveRequired<ICanonicalIdempotencyKeyProvider>();
        var service = new DataBackupService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<ICompanyEndpointProvider>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var viewModel = new DataBackupViewModel(
            service,
            app.ResolveRequired<IAccessStateService>(),
            idempotency);
        var view = new DataBackupView(viewModel);

        while (workspaceTabs.Items.Count <= 47)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[47] = new TabItem { Content = view };

        _dataBackupShellWired = true;
    }

    private async void DataBackup_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateDataBackupAsync();
        ApplyDataBackupHeader();
    }

    private void ApplyDataBackupHeader()
    {
        if (!_viewModel.IsDataBackupSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "CÀI ĐẶT CÔNG TY");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Dữ liệu & sao lưu");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Xuất số liệu doanh nghiệp, sao lưu kỹ thuật, di chuyển và khôi phục dữ liệu quan trọng.");
    }
}
