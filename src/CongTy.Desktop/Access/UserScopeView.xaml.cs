using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Access;

public partial class UserScopeView : UserControl
{
    public UserScopeView(UserScopeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private UserScopeViewModel ViewModel => (UserScopeViewModel)DataContext;

    private async void UserScopeView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void RefreshScopes_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void SaveScopes_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveScopesAsync();
}
