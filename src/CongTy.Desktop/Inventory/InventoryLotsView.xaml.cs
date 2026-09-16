using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Inventory;

public partial class InventoryLotsView : UserControl
{
    private readonly InventoryLotsViewModel _viewModel;

    public InventoryLotsView(InventoryLotsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public Task<bool> RefreshAsync() => _viewModel.RefreshAsync();

    private async void InventoryLotsView_OnLoaded(
        object sender,
        RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void InventoryLotsView_OnPreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshAsync();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            e.Handled = true;
            SearchBox.Focus();
            SearchBox.SelectAll();
        }
    }

    private async void Refresh_OnClick(
        object sender,
        RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();
}
