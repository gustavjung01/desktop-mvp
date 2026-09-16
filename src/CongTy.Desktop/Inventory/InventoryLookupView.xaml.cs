using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Inventory;

public partial class InventoryLookupView : UserControl
{
    private readonly InventoryLookupViewModel _viewModel;

    public InventoryLookupView(InventoryLookupViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public Task<bool> RefreshAsync() => _viewModel.RefreshCurrentAsync();

    private async void InventoryLookupView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void InventoryLookupView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _viewModel.IsHistoryDetailOpen)
        {
            e.Handled = true;
            _viewModel.CloseHistoryDetail();
            return;
        }

        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshCurrentAsync();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            e.Handled = true;
            _viewModel.SetTab("balances");
            SearchBox.Focus();
            SearchBox.SelectAll();
        }
    }

    private void BalancesTab_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SetTab("balances");

    private void HistoryTab_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SetTab("history");

    private void PreviousPage_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.PreviousPage();

    private void NextPage_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.NextPage();

    private async void RefreshBalances_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshBalancesAsync();

    private async void OpenHistory_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is InventoryLookupBalanceRow row)
        {
            await _viewModel.OpenHistoryAsync(row);
        }
    }

    private async void RefreshHistory_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshHistoryAsync();

    private async void HistoryPrevious_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.HistoryPreviousAsync();

    private async void HistoryNext_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.HistoryNextAsync();

    private void OpenHistoryDetail_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is InventoryLookupHistoryRow row)
        {
            _viewModel.OpenHistoryDetail(row);
        }
    }

    private void CloseHistoryDetail_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseHistoryDetail();
}
