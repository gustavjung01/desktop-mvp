using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Operations;

public partial class AuditHistoryView : UserControl
{
    public AuditHistoryView(AuditHistoryViewModel viewModel)
    {
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

    private async void NextPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadNextAsync();
}
