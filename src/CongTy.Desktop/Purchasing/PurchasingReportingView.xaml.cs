using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Purchasing;

public sealed class PurchaseOrderSearchRequestedEventArgs(string searchText) : EventArgs
{
    public string SearchText { get; } = searchText;
}

public partial class PurchasingReportingView : UserControl
{
    private readonly PurchasingReportingViewModel _viewModel;

    public event EventHandler<PurchaseOrderSearchRequestedEventArgs>? PurchaseOrdersRequested;

    public PurchasingReportingView(PurchasingReportingViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void Apply_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync().ConfigureAwait(true);

    private async void Reset_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ResetAsync().ConfigureAwait(true);

    private void OpenSupplierOrders_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PurchasingSupplierRow row } || !_viewModel.CanOpenPurchaseOrders) return;
        PurchaseOrdersRequested?.Invoke(this, new PurchaseOrderSearchRequestedEventArgs(row.Code));
    }

    private void OpenSkuOrders_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PurchasingSkuRow row } || !_viewModel.CanOpenPurchaseOrders) return;
        PurchaseOrdersRequested?.Invoke(this, new PurchaseOrderSearchRequestedEventArgs(row.SourceDocument));
    }

    private async void PurchasingReportingView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshAsync().ConfigureAwait(true);
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            e.Handled = true;
            FromDatePicker.Focus();
        }
    }
}
