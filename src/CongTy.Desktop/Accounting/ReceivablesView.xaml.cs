using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Accounting;

public partial class ReceivablesView : UserControl
{
    public ReceivablesView(ReceivablesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private ReceivablesViewModel ViewModel => (ReceivablesViewModel)DataContext;

    private async void ReceivablesView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void OpenDetail_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ReceivableDocumentRow row })
            await ViewModel.OpenDetailAsync(row.Id);
    }

    private void CloseDetail_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseDetail();
}
