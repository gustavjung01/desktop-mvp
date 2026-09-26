using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Operations;
using CongTy.ApiClient;

namespace CongTy.Desktop.Workforce;

public partial class AttendanceView : UserControl
{
    public AttendanceView(AttendanceViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public AttendanceViewModel ViewModel { get; }

    private async void AttendanceView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private void PasteQr_OnClick(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText())
            ViewModel.QrInput = Clipboard.GetText().Trim();
    }

    private async void SubmitQr_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitQrAsync();

    private async void SubmitManual_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SubmitManualAsync();

    private async void ShowWorkplaceQr_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ShowWorkplaceQrAsync();

    private async void RefreshQr_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshQrAsync();

    private void ClearQr_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.ClearQrToken();

    private async void ExportAttendance_OnClick(object sender, RoutedEventArgs e) =>
        await OfficeExportDialog.RunAsync(
            this,
            () => Task.FromResult<ApiDownloadFile?>(
                ((AttendanceViewModel)DataContext).ExportTodayAttendance()));
}
