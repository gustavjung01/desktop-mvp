using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Purchasing;

public partial class PurchasePriceView : UserControl
{
    private readonly PurchasePriceViewModel _viewModel;

    public PurchasePriceView(PurchasePriceViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync().ConfigureAwait(true);

    private void Create_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.BeginCreate();

    private void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PurchasePriceRow row)
        {
            _viewModel.BeginEdit(row);
        }
    }

    private void CloseEditor_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseEditor();

    private async void SearchSku_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SearchSkuAsync().ConfigureAwait(true);

    private async void SkuSearch_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await _viewModel.SearchSkuAsync().ConfigureAwait(true);
    }

    private void SelectSku_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PurchasePriceSkuRow row)
        {
            _viewModel.SelectSku(row);
        }
    }

    private void SkuResults_OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as DataGrid)?.SelectedItem is PurchasePriceSkuRow row)
        {
            _viewModel.SelectSku(row);
        }
    }

    private async void Save_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveAsync().ConfigureAwait(true);
}
