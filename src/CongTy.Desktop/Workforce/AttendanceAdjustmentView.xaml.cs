using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Workforce;

public partial class AttendanceAdjustmentView : UserControl
{
    public AttendanceAdjustmentView(AttendanceAdjustmentViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public AttendanceAdjustmentViewModel ViewModel { get; }

    public event EventHandler? OpenTimesheetRequested;

    private async void AttendanceAdjustmentView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void ApplyFilters_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ApplyFiltersAsync();

    private async void PreviousPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.PreviousPageAsync();

    private async void NextPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.NextPageAsync();

    private void OpenReview_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenReview((sender as FrameworkElement)?.Tag as AttendanceAdjustmentRowView);

    private void CloseReview_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseReview();

    private async void ApproveReview_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReviewAsync("APPROVE");

    private async void RejectReview_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReviewAsync("REJECT");

    private async void SubmitOwn_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitOwnAsync();

    private async void SubmitDirect_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitDirectAsync();

    private async void LockPeriod_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.LockPeriodAsync();

    private void OpenTimesheet_OnClick(object sender, RoutedEventArgs e) =>
        OpenTimesheetRequested?.Invoke(this, EventArgs.Empty);
}
