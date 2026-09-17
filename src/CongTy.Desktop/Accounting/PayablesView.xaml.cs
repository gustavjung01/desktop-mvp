using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Accounting;

public partial class PayablesView : UserControl
{
    public PayablesView(PayablesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private PayablesViewModel ViewModel => (PayablesViewModel)DataContext;

    private async void PayablesView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void OpenDetail_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PayableDocumentRow row })
            await ViewModel.OpenDetailAsync(row.Id);
    }

    private void CloseDetail_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseDetail();
}
