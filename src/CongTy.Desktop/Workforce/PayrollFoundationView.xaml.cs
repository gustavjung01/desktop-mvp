using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Workforce;

public partial class PayrollFoundationView : UserControl
{
    public PayrollFoundationView(PayrollFoundationViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public PayrollFoundationViewModel ViewModel { get; }

    private async void PayrollFoundationView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void CreatePeriod_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.CreatePeriodAsync();

    private async void OpenPeriod_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PayrollPeriodRowView row })
            await ViewModel.OpenPeriodAsync(row);
    }

    private async void SaveSalary_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveSalaryAsync();

    private async void SaveFixed_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.AssignFixedComponentAsync();

    private async void CreateComponent_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.CreateComponentTypeAsync();

    private async void AddPeriodComponent_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.AddPeriodComponentAsync();
}
