using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Logistics;

public partial class TripReconciliationView : UserControl
{
    private readonly TripReconciliationViewModel _viewModel;

    public TripReconciliationView(TripReconciliationViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public event Action? DeliveryAttemptsRequested;

    private async void TripReconciliationView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void TripReconciliationView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private async void Receive_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ReceiveReturnAsync();

    private async void Close_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CloseTripAsync();

    private void DeliveryAttempts_OnClick(object sender, RoutedEventArgs e) =>
        DeliveryAttemptsRequested?.Invoke();

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Detail is null || !_viewModel.CanPrint) return;

        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "TRIP_RECONCILIATION");
        if (template is null) return;
        try
        {
            TripReconciliationPrinter.Print(_viewModel.Detail, template);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Không in được phiếu đối soát.\n\n{exception.Message}",
                "Không in được phiếu",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
