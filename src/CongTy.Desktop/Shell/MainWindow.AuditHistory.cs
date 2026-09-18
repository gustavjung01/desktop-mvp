using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _auditHistoryShellWired;

    private void WireAuditHistoryWorkspace()
    {
        if (_auditHistoryShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeAuditHistoryShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var service = new AuditHistoryService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var viewModel = new AuditHistoryViewModel(
            service,
            app.ResolveRequired<IAccessStateService>());
        var view = new AuditHistoryView(
            viewModel,
            NavigateImportExportHistoryFromOtherWorkspaceAsync);

        while (workspaceTabs.Items.Count <= 45)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[45] = new TabItem { Content = view };

        _auditHistoryShellWired = true;
    }

    private async void AuditHistory_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateAuditHistoryAsync();
        ApplyAuditHistoryHeader();
    }

    private void ApplyAuditHistoryHeader()
    {
        if (!_viewModel.IsAuditHistorySelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "LỊCH SỬ VẬN HÀNH");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Lịch sử thay đổi hệ thống");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Theo dõi dữ liệu nào đã thay đổi, thao tác gì được thực hiện và vào thời điểm nào.");
    }
}
