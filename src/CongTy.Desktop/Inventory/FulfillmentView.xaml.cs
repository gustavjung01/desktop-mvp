using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Inventory;

public partial class FulfillmentView : UserControl
{
    private readonly FulfillmentViewModel _viewModel;
    private bool _loaded;

    public FulfillmentView(FulfillmentViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void FulfillmentView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await _viewModel.EnsureLoadedAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private void ResetFilters_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.ResetFilters();

    private async void OrderList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrderList.SelectedItem is FulfillmentOrderRow row
            && _viewModel.SelectedOrder?.SalesOrderId != row.SalesOrderId)
            await _viewModel.SelectOrderAsync(row);
    }

    private async void ProductGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductGrid.SelectedItem is FulfillmentProductRow row
            && _viewModel.SelectedProduct?.Data.FulfillmentDemandId != row.Data.FulfillmentDemandId)
            await _viewModel.SelectProductAsync(row);
    }

    private async void AutoAllocateOrder_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.AutoAllocateOrderAsync();

    private async void PrintPicking_OnClick(object sender, RoutedEventArgs e)
    {
        var order = _viewModel.SelectedOrder;
        if (order is null || !order.CanPrint) return;
        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "FULFILLMENT_PICKING");
        if (template is null) return;
        ActualDocumentPrintPreview.PrintFulfillmentPicking(Window.GetWindow(this), order.Items, template);
    }

    private async void AllocateQuantity_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FulfillmentProductRow row })
            await _viewModel.AllocateProductAsync(row, full: false);
    }

    private async void AllocateFull_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FulfillmentProductRow row })
            await _viewModel.AllocateProductAsync(row, full: true);
    }

    private async void Pick_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FulfillmentAllocationRow row })
            await _viewModel.UpdateProgressAsync(row, pack: false);
    }

    private async void Pack_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FulfillmentAllocationRow row })
            await _viewModel.UpdateProgressAsync(row, pack: true);
    }

    private async void OpenProductHolds_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FulfillmentProductRow row })
            await _viewModel.OpenProductHoldsAsync(row);
    }

    private async void OpenSelectedHolds_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.OpenSelectedProductHoldsAsync();

    private void CloseHolds_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseHolds();

    private async void FulfillmentView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _viewModel.IsHoldDetailOpen)
        {
            e.Handled = true;
            _viewModel.CloseHolds();
            return;
        }

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

        if (e.Key == Key.Enter && ProductGrid.IsKeyboardFocusWithin && ProductGrid.SelectedItem is FulfillmentProductRow product)
        {
            e.Handled = true;
            await _viewModel.SelectProductAsync(product);
        }
    }
}
