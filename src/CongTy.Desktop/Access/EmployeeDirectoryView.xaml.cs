using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Access;

public partial class EmployeeDirectoryView : UserControl
{
    public EmployeeDirectoryView(EmployeeDirectoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private EmployeeDirectoryViewModel ViewModel => (EmployeeDirectoryViewModel)DataContext;

    private async void EmployeeDirectoryView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private void Create_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenCreate();

    private void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EmployeeDirectoryRowView row })
            ViewModel.OpenEdit(row.Source);
    }

    private async void Save_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveAsync();

    private void Toggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EmployeeDirectoryRowView row })
            ViewModel.OpenToggle(row.Source);
    }

    private void CancelToggle_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CancelToggle();

    private async void ConfirmToggle_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ConfirmToggleAsync();

    private async void ReloadAfterConflict_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReloadAfterConflictAsync();

    private void CloseEditor_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseEditor();
}