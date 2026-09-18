using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Logistics;

public partial class TripDispatchView : UserControl
{
    private readonly TripDispatchViewModel _viewModel;

    public TripDispatchView(TripDispatchViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void TripDispatchView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void TripDispatchView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private void PrimaryReceiver_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.UsePrimaryReceiver();

    private void OtherReceiver_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.UseOtherReceiver();

    private async void Dispatch_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.DispatchAsync();

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTrip is null || !_viewModel.CanPrintSelected) return;

        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "DELIVERY_TRIP");
        if (template is null) return;
        try
        {
            TripSheetPrinter.Print(_viewModel.SelectedTrip, template);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Không in được phiếu chuyến giao hàng.\n\n{exception.Message}",
                "Không in được phiếu",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
