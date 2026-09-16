using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Logistics;

public partial class CustomerReturnView : UserControl
{
    private readonly CustomerReturnViewModel _viewModel;

    public CustomerReturnView(CustomerReturnViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public event Action? TripReconciliationRequested;

    private async void CustomerReturnView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void CustomerReturnView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private async void Create_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CreateAsync();

    private async void Receive_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ReceiveAsync();

    private async void Cancel_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CancelAsync();

    private void TripReconciliation_OnClick(object sender, RoutedEventArgs e) =>
        TripReconciliationRequested?.Invoke();
}
