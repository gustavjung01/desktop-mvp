using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

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

    private void SelectManualAttachment_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn chứng từ phiếu nghỉ",
            Filter = "Chứng từ nghỉ (*.jpg;*.jpeg;*.png;*.webp;*.pdf)|*.jpg;*.jpeg;*.png;*.webp;*.pdf",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog() == true)
            ViewModel.SelectManualAttachment(dialog.FileName);
    }

    private void ClearManualAttachment_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.ClearManualAttachment();

    private async void SubmitManualLeave_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitManualLeaveAsync();

    private async void SubmitRequest_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitRequestAsync();

    private void OpenAttachment_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LeaveRequestRowView row }) return;
        var raw = row.Source.AttachmentUrl?.Trim();
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return;
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

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
