using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Workforce;

public partial class OvertimeCloseoutView : UserControl
{
    public OvertimeCloseoutView(OvertimeCloseoutViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public OvertimeCloseoutViewModel ViewModel { get; }

    private async void OvertimeCloseoutView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void SubmitOvertime_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitOvertimeAsync();

    private void OpenOvertime_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: OvertimeRowView row })
            ViewModel.OpenOvertime(row);
    }

    private void CloseOvertime_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseOvertime();

    private async void ApproveOvertime_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReviewOvertimeAsync("APPROVE");

    private async void RejectOvertime_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReviewOvertimeAsync("REJECT");

    private async void RecordActual_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RecordActualAsync();

    private async void ConfirmHours_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ConfirmHoursAsync();

    private async void RefreshRange_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshSelectedRangeAsync();

    private void OpenPeriod_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AttendancePeriodRowView row })
            ViewModel.OpenPeriod(row);
    }

    private void ClosePeriod_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.ClosePeriod();

    private async void RefreshPeriod_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshSelectedPeriodAsync();

    private async void ReconcilePeriod_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReconcileSelectedPeriodAsync();

    private async void CloseAttendancePeriod_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.CloseSelectedPeriodAsync();

    private async void ViewPayrollSelected_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ViewPayrollAsync();

    private async void ViewPayrollRow_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AttendancePeriodRowView row })
            await ViewModel.ViewPayrollAsync(row);
    }
}
