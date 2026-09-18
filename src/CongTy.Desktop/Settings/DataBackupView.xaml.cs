using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CongTy.Desktop.Settings;

public partial class DataBackupView : UserControl
{
    private readonly DispatcherTimer _pollTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };

    public DataBackupView(DataBackupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _pollTimer.Tick += PollTimer_OnTick;
    }

    public event EventHandler? PrintTemplatesRequested;
    public event EventHandler? AppearanceRequested;

    private DataBackupViewModel ViewModel => (DataBackupViewModel)DataContext;

    private async void DataBackupView_OnLoaded(object sender, RoutedEventArgs e)
    {
        _pollTimer.Start();
        await ViewModel.EnsureLoadedAsync();
    }

    private void DataBackupView_OnUnloaded(object sender, RoutedEventArgs e) =>
        _pollTimer.Stop();

    private async void PollTimer_OnTick(object? sender, EventArgs e)
    {
        if (ViewModel.ShouldPoll)
            await ViewModel.RefreshAsync();
    }

    private void PrintTemplates_OnClick(object sender, RoutedEventArgs e) =>
        PrintTemplatesRequested?.Invoke(this, EventArgs.Empty);

    private void Appearance_OnClick(object sender, RoutedEventArgs e) =>
        AppearanceRequested?.Invoke(this, EventArgs.Empty);

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void ExportBusiness_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = "so-lieu-doanh-nghiep.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
            Filter = "Excel (*.xlsx)|*.xlsx"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            await ViewModel.ExportBusinessAsync(dialog.FileName);
    }

    private async void RequestTechnicalUnlock_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RequestTechnicalUnlockAsync();

    private async void VerifyTechnicalUnlock_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.VerifyTechnicalUnlockAsync();

    private async void StartBackup_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            Window.GetWindow(this),
            "Tạo một bản sao lưu hệ thống mới gồm file .dump, tệp thông tin khôi phục và mã kiểm tra SHA-256?",
            "Xác nhận sao lưu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
            await ViewModel.StartBackupAsync();
    }

    private async void DownloadBackup_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: DataBackupJobRow row } button
            || button.Tag is not string artifactType)
            return;

        var url = await ViewModel.CreateDownloadUrlAsync(row.Id, artifactType);
        if (string.IsNullOrWhiteSpace(url)) return;

        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private async void RequestDeleteChallenge_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RequestDeleteChallengeAsync();

    private async void VerifyDeleteChallenge_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.VerifyDeleteChallengeAsync();

    private async void ExecuteDelete_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            Window.GetWindow(this),
            $"Xóa thật phạm vi “{ViewModel.SelectedDeleteTarget.Label}”? Thao tác không thể hoàn tác trong cơ sở dữ liệu hiện tại.",
            "Xác nhận xóa dữ liệu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result == MessageBoxResult.Yes)
            await ViewModel.ExecuteDeleteAsync();
    }
}
