using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Logistics;

public partial class LogisticsReportingView : UserControl
{
    private readonly LogisticsReportingViewModel _viewModel;

    public event Action<string>? NavigationRequested;

    public LogisticsReportingView(LogisticsReportingViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void LogisticsReportingView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void LogisticsReportingView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void ApplyFilters_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplyFiltersAsync();

    private async void CurrentMonth_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ResetToCurrentMonthAsync();

    private void TripPlanning_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("trips");

    private void DeliveryAttempts_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("delivery-attempts");

    private void DeliveryOrders_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("delivery-orders");

    private void TripReconciliation_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("trip-reconciliation");
}
