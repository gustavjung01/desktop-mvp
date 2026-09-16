using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Logistics;

public partial class TripPlanningView : UserControl
{
    private readonly TripPlanningViewModel _viewModel;

    public TripPlanningView(TripPlanningViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void TripPlanningView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void TripPlanningView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void DriverEmployeeCombo_OnDropDownOpened(object sender, EventArgs e) =>
        await _viewModel.EnsureDriverEmployeesAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private async void CreateRoute_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CreateRouteAsync();

    private async void CreateVehicle_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CreateVehicleAsync();

    private async void CreateDriver_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CreateDriverAsync();

    private async void CreateTrip_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CreateTripAsync();

    private async void UpdateTrip_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.UpdateTripAsync();

    private async void PlanTrip_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PlanAsync();

    private async void ReopenTrip_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ReopenAsync();

    private async void LockTrip_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.LockAsync();

    private async void AssignSelected_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.AssignSelectedAsync();

    private async void Unassign_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: TripAssignmentRow row })
            await _viewModel.UnassignAsync(row);
    }

    private async void MoveStopUp_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: TripStopRow row })
            await _viewModel.MoveStopAsync(row, -1);
    }

    private async void MoveStopDown_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: TripStopRow row })
            await _viewModel.MoveStopAsync(row, 1);
    }

    private void EligibleOrder_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.NotifyEligibleSelectionChanged();
}
