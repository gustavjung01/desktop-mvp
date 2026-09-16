using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Logistics;

public partial class DeliveryAttemptView : UserControl
{
    private readonly DeliveryAttemptViewModel _viewModel;

    public DeliveryAttemptView(DeliveryAttemptViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void DeliveryAttemptView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void DeliveryAttemptView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private async void ToggleProofs_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: DeliveryAttemptRow row })
            await _viewModel.ToggleProofsAsync(row);
    }

    private void OpenProofFile_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: DeliveryProofRow row }) return;
        var url = row.Data.File?.DownloadUrl;
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Không mở được ảnh giao hàng.\n\n{exception.Message}",
                "Không mở được ảnh",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
