using System.Windows;
using System.Windows.Controls;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public partial class TimesheetView : UserControl
{
    public TimesheetView(TimesheetViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public TimesheetViewModel ViewModel { get; }

    public event EventHandler<TimesheetAdjustmentRequestedEventArgs>? AdjustmentRequested;
    public event EventHandler? ViolationRequested;

    private async void TimesheetView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void DailyView_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SwitchDailyAsync();

    private async void MonthlyView_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SwitchMonthlyAsync();

    private async void ApplyFilters_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ApplyFiltersAsync();

    private async void PreviousPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.PreviousPageAsync();

    private async void NextPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.NextPageAsync();

    private void EmployeeDetail_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenEmployee((sender as FrameworkElement)?.Tag as TimesheetEmployeeRowView);

    private void CloseEmployeeDetail_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseEmployee();

    private void EmployeeDayDetail_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenDay((sender as FrameworkElement)?.Tag as AttendanceTimesheetDayData);

    private void MonthDay_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenDay((sender as FrameworkElement)?.Tag as AttendanceTimesheetDayData);

    private async void QuickCheckIn_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitQuickAttendanceAsync("CHECK_IN");

    private void ToggleQuickExit_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.ToggleQuickExit();

    private async void QuickEndWork_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitQuickAttendanceAsync("CHECK_OUT");

    private async void QuickReturn_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitQuickAttendanceAsync("RETURN");

    private async void QuickEndExternalWork_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitQuickAttendanceAsync("END_EXTERNAL_WORK");

    private async void SubmitQuickExit_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitQuickExitAsync();

    private void ToggleDayAdjustment_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.ToggleDayAdjustment();

    private async void SaveDayAdjustment_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveDayAdjustmentAsync();

    private void OpenAdjustment_OnClick(object sender, RoutedEventArgs e)
    {
        var workDate = ViewModel.AdjustmentTargetWorkDate;
        if (string.IsNullOrWhiteSpace(workDate)) return;
        AdjustmentRequested?.Invoke(
            this,
            new TimesheetAdjustmentRequestedEventArgs(
                ViewModel.AdjustmentTargetEmployeeId,
                workDate));
    }

    private void OpenViolationHandling_OnClick(object sender, RoutedEventArgs e) =>
        ViolationRequested?.Invoke(this, EventArgs.Empty);

    private void CloseDayDetail_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseDay();
}

public sealed class TimesheetAdjustmentRequestedEventArgs(
    string? employeeId,
    string workDate) : EventArgs
{
    public string? EmployeeId { get; } = employeeId;
    public string WorkDate { get; } = workDate;
}
