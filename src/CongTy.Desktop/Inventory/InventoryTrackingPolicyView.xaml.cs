using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Inventory;

public partial class InventoryTrackingPolicyView : UserControl
{
    private readonly InventoryTrackingPolicyViewModel _viewModel;

    public InventoryTrackingPolicyView(InventoryTrackingPolicyViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public Task<bool> RefreshAsync() => _viewModel.RefreshAsync();

    private async void InventoryTrackingPolicyView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void InventoryTrackingPolicyView_OnPreviewKeyDown(object sender, KeyEventArgs e)
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
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S && _viewModel.CanSave)
        {
            e.Handled = true;
            await _viewModel.SaveAsync();
        }
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private void Choose_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TrackingPolicyCandidateRow row)
        {
            _viewModel.Choose(row);
        }
    }

    private async void Save_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveAsync();
}
