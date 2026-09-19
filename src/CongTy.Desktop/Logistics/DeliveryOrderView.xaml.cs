using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Logistics;

public partial class DeliveryOrderView : UserControl
{
    private readonly DeliveryOrderViewModel _viewModel;

    public event Action? CustomerReturnsRequested;

    public DeliveryOrderView(DeliveryOrderViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void DeliveryOrderView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void DeliveryOrderView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshAsync();
    private async void Create_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CreateAsync();
    private async void Confirm_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ConfirmAsync();
    private async void Cancel_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CancelAsync();
    private async void Pickup_OnClick(object sender, RoutedEventArgs e) => await _viewModel.PickupHandoverAsync();
    private async void Manual_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ManualHandoverAsync();
    private async void Reverse_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ReverseInventoryIssueAsync();

    private void CustomerReturns_OnClick(object sender, RoutedEventArgs e) =>
        CustomerReturnsRequested?.Invoke();

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        var order = _viewModel.SelectedOrderDetail;
        if (order is null || !_viewModel.CanPrintSelected) return;

        var packing = string.Equals(_viewModel.SelectedPrintVariant, "Phiếu đóng gói", StringComparison.Ordinal);
        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(
            Window.GetWindow(this),
            "DELIVERY_ORDER",
            packing ? "packing-list" : "standard");
        if (template is null) return;
        DeliveryOrderPrintPreview.Print(order, template, packing);
    }
}
