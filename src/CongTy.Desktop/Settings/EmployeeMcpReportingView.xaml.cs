using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Settings;

public partial class EmployeeMcpReportingView : UserControl
{
    public EmployeeMcpReportingView(EmployeeMcpReportingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public event EventHandler? CustomerOnboardingRequested;
    public event EventHandler? EmployeeDirectoryRequested;

    private EmployeeMcpReportingViewModel ViewModel => (EmployeeMcpReportingViewModel)DataContext;

    private async void EmployeeMcpReportingView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Apply_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ApplyAsync();

    private async void Reset_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ResetCurrentMonthAsync();

    private void CustomerOnboarding_OnClick(object sender, RoutedEventArgs e) =>
        CustomerOnboardingRequested?.Invoke(this, EventArgs.Empty);

    private void EmployeeDirectory_OnClick(object sender, RoutedEventArgs e) =>
        EmployeeDirectoryRequested?.Invoke(this, EventArgs.Empty);

    private async void EmployeeMcpReportingView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5 || !ViewModel.CanApply) return;
        e.Handled = true;
        await ViewModel.RefreshAsync();
    }
}
