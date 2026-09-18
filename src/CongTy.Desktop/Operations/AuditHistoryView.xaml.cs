using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Operations;

public partial class AuditHistoryView : UserControl
{
    private readonly Func<Task>? _openImportExportHistory;

    public AuditHistoryView(
        AuditHistoryViewModel viewModel,
        Func<Task>? openImportExportHistory = null)
    {
        _openImportExportHistory = openImportExportHistory;
        InitializeComponent();
        DataContext = viewModel;
    }

    private AuditHistoryViewModel ViewModel => (AuditHistoryViewModel)DataContext;

    private async void AuditHistoryView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void ApplyFilter_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ApplyFilterAsync();

    private async void ClearFilter_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ClearFilterAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void OpenImportExportHistory_OnClick(object sender, RoutedEventArgs e)
    {
        if (_openImportExportHistory is not null)
            await _openImportExportHistory();
    }

    private async void NextPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadNextAsync();
}
