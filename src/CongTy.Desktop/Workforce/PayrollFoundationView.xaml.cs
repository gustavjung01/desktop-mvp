using System.IO;
using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using Microsoft.Win32;

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

    private async void AggregatePayroll_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.AggregatePayrollAsync();

    private async void ReconcilePayroll_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReconcilePayrollAsync();

    private async void ClosePayroll_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ClosePayrollAsync();

    private async void RecordPayrollAdjustment_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RecordPayrollAdjustmentAsync();

    private async void OpenCloseHistory_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PayrollCloseHistoryRowView row }) return;
        await ViewModel.OpenCloseHistoryAsync(row);
        PayrollTabs.SelectedIndex = 0;
    }

    private void ExportPayrollExcel_OnClick(object sender, RoutedEventArgs e) =>
        SaveExport(ViewModel.CreatePayrollWorkbook(), "Excel (*.xlsx)|*.xlsx");

    private void ExportPayslipPdf_OnClick(object sender, RoutedEventArgs e) =>
        SaveExport(ViewModel.CreateSelectedPayslipPdf(), "PDF (*.pdf)|*.pdf");

    private static void SaveExport(ApiDownloadFile? file, string filter)
    {
        if (file is null) return;
        var dialog = new SaveFileDialog
        {
            FileName = file.FileName,
            Filter = filter,
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            File.WriteAllBytes(dialog.FileName, file.Content);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Không lưu được tệp. {exception.Message}",
                "Xuất tệp",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
