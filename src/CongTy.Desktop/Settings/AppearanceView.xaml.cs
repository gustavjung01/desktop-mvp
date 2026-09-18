using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Settings;

public partial class AppearanceView : UserControl
{
    private bool _initialized;

    public AppearanceView(AppearanceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) =>
        {
            ScaleSlider.Value = viewModel.Scale;
            _initialized = true;
        };
    }

    public event EventHandler? DataBackupRequested;
    public event EventHandler? PrintTemplatesRequested;
    public event EventHandler? DesktopAppRequested;

    private AppearanceViewModel ViewModel => (AppearanceViewModel)DataContext;

    private void DataBackup_OnClick(object sender, RoutedEventArgs e) =>
        DataBackupRequested?.Invoke(this, EventArgs.Empty);

    private void PrintTemplates_OnClick(object sender, RoutedEventArgs e) =>
        PrintTemplatesRequested?.Invoke(this, EventArgs.Empty);

    private void DesktopApp_OnClick(object sender, RoutedEventArgs e) =>
        DesktopAppRequested?.Invoke(this, EventArgs.Empty);

    private async void Theme_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string key })
            await ViewModel.ChooseThemeAsync(key);
    }

    private void ScaleSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initialized) ViewModel.PreviewScale((int)Math.Round(e.NewValue));
    }

    private async void ScaleSlider_OnCommit(object sender, MouseButtonEventArgs e)
    {
        if (_initialized) await ViewModel.CommitScaleAsync();
    }

    private async void ScaleSlider_OnKeyCommit(object sender, KeyEventArgs e)
    {
        if (_initialized && e.Key is Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown)
            await ViewModel.CommitScaleAsync();
    }

    private async void ResetScale_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ResetScaleAsync();
        ScaleSlider.Value = ViewModel.Scale;
    }
}
