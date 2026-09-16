using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Inventory;

public partial class InventoryCostingView : UserControl
{
    private readonly InventoryCostingViewModel _viewModel;

    public InventoryCostingView(InventoryCostingViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public Task<bool> RefreshAsync() => _viewModel.RefreshAsync();

    public Task RebuildAsync() => _viewModel.RebuildAsync();

    private async void InventoryCostingView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void InventoryCostingView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshAsync();
            return;
        }

        if (Keyboard.Modifiers != ModifierKeys.Control) return;

        var tab = e.Key switch
        {
            Key.D1 => "balances",
            Key.D2 => "periods",
            Key.D3 => "reconciliation",
            Key.D4 => "discrepancies",
            Key.D5 => "adjustments",
            Key.D6 => "anomalies",
            Key.D7 => "facts",
            _ => null
        };

        if (tab is null) return;
        e.Handled = true;
        _viewModel.SetTab(tab);
    }

    private void BalancesTab_OnClick(object sender, RoutedEventArgs e) => _viewModel.SetTab("balances");
    private void PeriodsTab_OnClick(object sender, RoutedEventArgs e) => _viewModel.SetTab("periods");
    private void ReconciliationTab_OnClick(object sender, RoutedEventArgs e) => _viewModel.SetTab("reconciliation");
    private void DiscrepanciesTab_OnClick(object sender, RoutedEventArgs e) => _viewModel.SetTab("discrepancies");
    private void AdjustmentsTab_OnClick(object sender, RoutedEventArgs e) => _viewModel.SetTab("adjustments");
    private void AnomaliesTab_OnClick(object sender, RoutedEventArgs e) => _viewModel.SetTab("anomalies");
    private void FactsTab_OnClick(object sender, RoutedEventArgs e) => _viewModel.SetTab("facts");

    private async void PeriodAction_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.MutatePeriodAsync();
}
