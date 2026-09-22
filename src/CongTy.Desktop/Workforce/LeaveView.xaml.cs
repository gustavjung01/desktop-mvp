using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Workforce;

public partial class LeaveView : UserControl
{
    public LeaveView(LeaveViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public LeaveViewModel ViewModel { get; }

    private async void LeaveView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void ApplyFilters_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ApplyFiltersAsync();

    private async void PreviousPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.PreviousPageAsync();

    private async void NextPage_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.NextPageAsync();

    private async void SubmitRequest_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitRequestAsync();

    private void ReviewRequest_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LeaveRequestRowView row })
            ViewModel.OpenReview(row);
    }

    private void CloseReview_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseReview();

    private async void Approve_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReviewAsync("APPROVE");

    private async void Reject_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReviewAsync("REJECT");

    private void OpenCancel_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LeaveRequestRowView row })
            ViewModel.OpenCancel(row);
    }

    private void CloseCancel_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseCancel();

    private async void ConfirmCancel_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.CancelRequestAsync();

    private async void ViewBalance_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LeaveBalanceRowView row })
            await ViewModel.ViewBalanceAsync(row);
    }

    private async void SaveBalanceEntry_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveBalanceEntryAsync();

    private void NewType_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.NewType();

    private void EditType_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LeaveTypeRowView row })
            ViewModel.EditType(row);
    }

    private void CloseTypeEditor_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseTypeEditor();

    private async void SaveType_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveTypeAsync();
}
