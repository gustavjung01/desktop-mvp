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

    private void Create_OnClick(object sender, RoutedEventArgs e)
    {
        EditorPasswordBox.Clear();
        ViewModel.OpenCreate();
    }

    private void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: UserDirectoryRowView row })
        {
            EditorPasswordBox.Clear();
            ViewModel.OpenEdit(row.Source);
        }
    }

    private void Password_OnChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
            ViewModel.DraftPassword = passwordBox.Password;
    }

    private async void Save_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveAsync();
        if (!ViewModel.IsEditorOpen)
            EditorPasswordBox.Clear();
    }

    private void CloseEditor_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CloseEditor();
        EditorPasswordBox.Clear();
    }

    private async void ReloadAfterConflict_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReloadAfterConflictAsync();

    private void Toggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: UserDirectoryRowView row })
            ViewModel.OpenToggle(row.Source);
    }

    private void CancelToggle_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CancelToggle();

    private async void ConfirmToggle_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ConfirmToggleAsync();
}
