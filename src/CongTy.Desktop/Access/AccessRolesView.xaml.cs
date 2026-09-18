using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Access;

public partial class AccessRolesView : UserControl
{
    public AccessRolesView(AccessRolesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private AccessRolesViewModel ViewModel => (AccessRolesViewModel)DataContext;

    private async void AccessRolesView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private void Create_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenCreate();

    private void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AccessRoleRowView row })
            ViewModel.OpenEdit(row.Source);
    }

    private void CloseEditor_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseEditor();
}
