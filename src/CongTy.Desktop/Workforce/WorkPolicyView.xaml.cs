using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Workforce;

public partial class WorkPolicyView : UserControl
{
    public WorkPolicyView(WorkPolicyViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public WorkPolicyViewModel ViewModel { get; }

    private async void WorkPolicyView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private void NewPolicy_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenCreate();

    private void EditPolicy_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WorkPolicyRowView row })
            ViewModel.OpenEdit(row);
    }

    private void CloseEditor_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseEditor();

    private async void SavePolicy_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveAsync();
}
