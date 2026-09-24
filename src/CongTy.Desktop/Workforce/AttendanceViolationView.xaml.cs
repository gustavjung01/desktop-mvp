using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Workforce;

public partial class AttendanceViolationView : UserControl
{
    public AttendanceViolationView(AttendanceViolationViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public AttendanceViolationViewModel ViewModel { get; }

    public event EventHandler? OpenTimesheetRequested;

    private async void AttendanceViolationView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void ApplyFilters_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ApplyFiltersAsync();

    private async void PreviousPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.PreviousPageAsync();

    private async void NextPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.NextPageAsync();

    private void OpenExplanation_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenExplanation((sender as FrameworkElement)?.Tag as AttendanceViolationRowView);

    private void CloseExplanation_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseExplanation();

    private async void SubmitExplanation_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitExplanationAsync();

    private async void StartReview_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.StartReviewAsync((sender as FrameworkElement)?.Tag as AttendanceViolationRowView);

    private void OpenConclusion_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenConclusion((sender as FrameworkElement)?.Tag as AttendanceViolationRowView);

    private void CloseConclusion_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseConclusion();

    private async void SubmitConclusion_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitConclusionAsync();
}
