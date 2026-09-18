using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Settings;
using CongTy.Windows;

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
        var access = app.ResolveRequired<IAccessStateService>();
        var apiClient = app.ResolveRequired<CompanyApiClient>();
        var session = app.ResolveRequired<IAuthenticatedSessionAccessor>();

        var dataBackupService = new DataBackupService(
            apiClient,
            app.ResolveRequired<ICompanyEndpointProvider>(),
            session);
        var dataBackupView = new DataBackupView(new DataBackupViewModel(dataBackupService, access, idempotency));
        dataBackupView.PrintTemplatesRequested += SettingsPrintTemplatesRequested;
        dataBackupView.AppearanceRequested += SettingsAppearanceRequested;

        var printService = new DocumentPrintTemplateService(apiClient, session);
        var printView = new PrintTemplatesView(new PrintTemplatesViewModel(printService, access, idempotency));
        printView.DataBackupRequested += SettingsDataBackupRequested;
        printView.AppearanceRequested += SettingsAppearanceRequested;

        var appearanceView = new AppearanceView(new AppearanceViewModel(
            app.ResolveRequired<ILocalSettingsStore>(),
            app.ResolveRequired<DesktopSettingsState>()));
        appearanceView.DataBackupRequested += SettingsDataBackupRequested;
        appearanceView.PrintTemplatesRequested += SettingsPrintTemplatesRequested;

        while (workspaceTabs.Items.Count <= 49)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[47] = new TabItem { Content = dataBackupView };
        workspaceTabs.Items[48] = new TabItem { Content = printView };
        workspaceTabs.Items[49] = new TabItem { Content = appearanceView };

        _dataBackupShellWired = true;
    }

    private async void DataBackup_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateDataBackupAsync();
        ApplySettingsHeader();
    }

    private async void SettingsDataBackupRequested(object? sender, EventArgs e)
    {
        await _viewModel.NavigateDataBackupAsync();
        ApplySettingsHeader();
    }

    private async void SettingsPrintTemplatesRequested(object? sender, EventArgs e)
    {
        await _viewModel.NavigatePrintTemplatesAsync();
        ApplySettingsHeader();
    }

    private async void SettingsAppearanceRequested(object? sender, EventArgs e)
    {
        await _viewModel.NavigateAppearanceAsync();
        ApplySettingsHeader();
    }

    private void ApplySettingsHeader()
    {
        if (!_viewModel.IsDataBackupSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "CÀI ĐẶT CÔNG TY");
        if (_viewModel.IsPrintTemplatesSelected)
        {
            SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Mẫu in");
            SetShellHeaderText(nameof(ShellViewModel.PageSubtitle), "Thiết lập mẫu in dùng chung cho các chứng từ của Công Ty.");
            return;
        }

        if (_viewModel.IsAppearanceSelected)
        {
            SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Giao diện");
            SetShellHeaderText(nameof(ShellViewModel.PageSubtitle), "Chọn màu sắc và kích thước hiển thị phù hợp trên máy tính này.");
            return;
        }

        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Dữ liệu & sao lưu");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Xuất số liệu doanh nghiệp, sao lưu kỹ thuật, di chuyển và khôi phục dữ liệu quan trọng.");
    }
}
