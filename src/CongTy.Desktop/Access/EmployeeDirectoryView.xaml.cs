using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Operations;
using CongTy.ApiClient;

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

    private async void Organization_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.OpenOrganizationAsync();

    private void CloseOrganization_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseOrganization();

    private async void SaveDepartment_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveDepartmentAsync();

    private async void SavePosition_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SavePositionAsync();

    private async void ToggleDepartment_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EmployeeDepartmentRowView row })
            await ViewModel.ToggleDepartmentAsync(row);
    }

    private async void TogglePosition_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EmployeePositionRowView row })
            await ViewModel.TogglePositionAsync(row);
    }

    private async void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EmployeeDirectoryRowView row })
            await ViewModel.OpenEditAsync(row.Source);
    }

    private async void Save_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveAsync();

    private async void Toggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EmployeeDirectoryRowView row })
            await ViewModel.OpenToggleAsync(row.Source);
    }

    private void CancelToggle_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CancelToggle();

    private async void ConfirmToggle_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ConfirmToggleAsync();

    private async void ReloadAfterConflict_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReloadAfterConflictAsync();

    private void CloseEditor_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseEditor();

    private async void ExportEmployees_OnClick(object sender, RoutedEventArgs e) =>
        await OfficeExportDialog.RunAsync(
            this,
            () => Task.FromResult<ApiDownloadFile?>(ViewModel.ExportEmployees()));
}