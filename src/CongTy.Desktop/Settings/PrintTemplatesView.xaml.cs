using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Settings;

public partial class PrintTemplatesView : UserControl
{
    public PrintTemplatesView(PrintTemplatesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public event EventHandler? DataBackupRequested;
    public event EventHandler? AppearanceRequested;

    private PrintTemplatesViewModel ViewModel => (PrintTemplatesViewModel)DataContext;

    private async void PrintTemplatesView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private void DataBackup_OnClick(object sender, RoutedEventArgs e) =>
        DataBackupRequested?.Invoke(this, EventArgs.Empty);

    private void Appearance_OnClick(object sender, RoutedEventArgs e) =>
        AppearanceRequested?.Invoke(this, EventArgs.Empty);

    private async void Save_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveAsync();

    private async void Reset_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            Window.GetWindow(this),
            "Khôi phục mẫu in đang chọn về cấu hình mặc định dùng chung?",
            "Khôi phục mẫu in",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (result == MessageBoxResult.Yes)
            await ViewModel.ResetAsync();
    }
}
