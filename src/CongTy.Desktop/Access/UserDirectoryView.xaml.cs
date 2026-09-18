using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Access;

public partial class UserDirectoryView : UserControl
{
    public UserDirectoryView(UserDirectoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private UserDirectoryViewModel ViewModel => (UserDirectoryViewModel)DataContext;

    private async void UserDirectoryView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();
}
