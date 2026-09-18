using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Operations;

public partial class ImportExportHistoryView : UserControl
{
    private readonly Func<Task> _openDataExchange;
    private readonly Func<Task> _openAuditHistory;

    public ImportExportHistoryView(
        ImportExportHistoryViewModel viewModel,
        Func<Task> openDataExchange,
        Func<Task> openAuditHistory)
    {
        _openDataExchange = openDataExchange;
        _openAuditHistory = openAuditHistory;
        InitializeComponent();
        DataContext = viewModel;
    }

    private ImportExportHistoryViewModel ViewModel => (ImportExportHistoryViewModel)DataContext;

    private async void ImportExportHistoryView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void ApplyFilter_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ApplyFilterAsync();

    private async void ClearFilter_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ClearFilterAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void NextPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadNextAsync();

    private async void OpenDataExchange_OnClick(object sender, RoutedEventArgs e) =>
        await _openDataExchange();

    private async void OpenAuditHistory_OnClick(object sender, RoutedEventArgs e) =>
        await _openAuditHistory();
}
