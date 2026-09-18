using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _dataExchangeShellWired;

    private void WireDataExchangeWorkspace()
    {
        if (_dataExchangeShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeDataExchangeShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var idempotency = app.ResolveRequired<ICanonicalIdempotencyKeyProvider>();
        var service = new DataExchangeService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>(),
            idempotency);
        var viewModel = new DataExchangeViewModel(
            service,
            app.ResolveRequired<IAccessStateService>(),
            idempotency);
        var view = new DataExchangeView(
            viewModel,
            NavigateImportExportHistoryFromOtherWorkspaceAsync);

        while (workspaceTabs.Items.Count <= 44)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[44] = new TabItem { Content = view };

        _dataExchangeShellWired = true;
    }

    private async void DataExchange_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateDataExchangeAsync();
        ApplyDataExchangeHeader();
    }

    private void ApplyDataExchangeHeader()
    {
        if (!_viewModel.IsDataExchangeSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "DỮ LIỆU VẬN HÀNH");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Nhập/xuất dữ liệu và báo giá");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Nhập, kiểm tra và xuất dữ liệu theo từng nghiệp vụ.");
    }
}
