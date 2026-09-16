using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

    private void Print_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTrip is null || !_viewModel.CanPrintSelected) return;

        try
        {
            TripSheetPrinter.Print(_viewModel.SelectedTrip);
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
