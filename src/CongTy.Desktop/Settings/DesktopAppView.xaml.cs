using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Settings;

public partial class DesktopAppView : UserControl
{
    public DesktopAppView(DesktopAppViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public event EventHandler? DataBackupRequested;
    public event EventHandler? PrintTemplatesRequested;
    public event EventHandler? AppearanceRequested;

    private DesktopAppViewModel ViewModel => (DesktopAppViewModel)DataContext;

    private void DataBackup_OnClick(object sender, RoutedEventArgs e) =>
        DataBackupRequested?.Invoke(this, EventArgs.Empty);

    private void PrintTemplates_OnClick(object sender, RoutedEventArgs e) =>
        PrintTemplatesRequested?.Invoke(this, EventArgs.Empty);

    private void Appearance_OnClick(object sender, RoutedEventArgs e) =>
        AppearanceRequested?.Invoke(this, EventArgs.Empty);

    private async void PrimaryAction_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CanInstall)
        {
            if (await ViewModel.InstallAsync())
                Application.Current.Shutdown();
            return;
        }

        await ViewModel.CheckAsync();
    }
}
