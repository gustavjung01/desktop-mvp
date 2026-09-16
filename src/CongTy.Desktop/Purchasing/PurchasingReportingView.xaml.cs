using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Purchasing;

public partial class PurchasingReportingView : UserControl
{
    private readonly PurchasingReportingViewModel _viewModel;

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
