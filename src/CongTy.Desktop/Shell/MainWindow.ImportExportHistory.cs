using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _importExportHistoryShellWired;

    private void WireImportExportHistoryWorkspace()
    {
        if (_importExportHistoryShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeImportExportHistoryShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var service = new ImportExportHistoryService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var viewModel = new ImportExportHistoryViewModel(
            service,
            app.ResolveRequired<IAccessStateService>());
        var view = new ImportExportHistoryView(
            viewModel,
            NavigateDataExchangeFromHistoryAsync,
            NavigateAuditHistoryFromImportExportAsync);

        while (workspaceTabs.Items.Count <= 46)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[46] = new TabItem { Content = view };

        _importExportHistoryShellWired = true;
    }

    private async void ImportExportHistory_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateImportExportHistoryAsync();
        ApplyImportExportHistoryHeader();
    }

    private async Task NavigateDataExchangeFromHistoryAsync()
    {
        await _viewModel.NavigateDataExchangeAsync();
        ApplyDataExchangeHeader();
    }

    private async Task NavigateAuditHistoryFromImportExportAsync()
    {
        await _viewModel.NavigateAuditHistoryAsync();
        ApplyAuditHistoryHeader();
    }

    private async Task NavigateImportExportHistoryFromOtherWorkspaceAsync()
    {
        await _viewModel.NavigateImportExportHistoryAsync();
        ApplyImportExportHistoryHeader();
    }

    private void ApplyImportExportHistoryHeader()
    {
        if (!_viewModel.IsImportExportHistorySelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "LỊCH SỬ VẬN HÀNH");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Lịch sử nhập/xuất dữ liệu");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Theo dõi các lần nhập và xuất dữ liệu, người thực hiện, trạng thái và số dòng đã xử lý.");
    }
}
