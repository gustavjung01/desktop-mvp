using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Dashboard;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _viewModel;

    public DashboardView(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += DashboardView_OnLoaded;
    }

    public event Action<string>? NavigationRequested;

    private async void DashboardView_OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.EnsureLoadedAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshAsync();
    }

    private void Shortcut_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string key })
        {
            NavigationRequested?.Invoke(key);
        }
    }
}
