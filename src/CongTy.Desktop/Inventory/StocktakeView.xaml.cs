using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Inventory;

public partial class StocktakeView : UserControl
{
    private readonly StocktakeViewModel _viewModel;

    public StocktakeView(StocktakeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public Task RefreshAsync() => _viewModel.RefreshAsync();

    public void OpenCreate() => _viewModel.OpenCreate();

    private async void StocktakeView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void StocktakeView_OnPreviewKeyDown(object sender, KeyEventArgs e)
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
            StocktakeSearchBox.Focus();
            StocktakeSearchBox.SelectAll();
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsCreateOpen)
        {
            e.Handled = true;
            _viewModel.CloseCreate();
        }
    }

    private async void StocktakeList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StocktakeList.SelectedItem is StocktakeListRow row)
        {
            await _viewModel.SelectStocktakeAsync(row.Data.Id);
        }
    }

    private void CloseCreate_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseCreate();
    private async void Create_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CreateAsync();
    private void ResetFilters_OnClick(object sender, RoutedEventArgs e) => _viewModel.ResetFilters();
    private async void Count_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CompleteCountAsync();
    private async void Submit_OnClick(object sender, RoutedEventArgs e) => await _viewModel.SubmitAsync();
    private async void Recount_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RecountAsync();
    private async void Approve_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ApproveAsync();
    private async void Post_OnClick(object sender, RoutedEventArgs e) => await _viewModel.PostAsync();
    private async void Cancel_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CancelAsync();
    private async void Reverse_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ReverseAsync();

    private void Print_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanPrint) return;

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        dialog.PrintVisual(DetailPrintArea, $"Phiếu kiểm kê {_viewModel.DetailNumber}");
    }
}
