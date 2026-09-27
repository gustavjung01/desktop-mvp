using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CongTy.Desktop.Shell;
using Microsoft.Win32;

namespace CongTy.Desktop.Sales;

public partial class SalesQuotationView : UserControl
{
    public SalesQuotationView(SalesQuotationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private SalesQuotationViewModel ViewModel => (SalesQuotationViewModel)DataContext;

    private async void SalesQuotationView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private void SelectCustomer_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SalesQuotationCustomerOption option })
            ViewModel.SelectCustomer(option);
    }

    private void ClearCustomer_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.ClearCustomer();

    private void AddSku_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SalesQuotationSkuOption option })
            ViewModel.AddSku(option);
    }

    private void RemoveSku_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id })
            ViewModel.RemoveSku(id);
    }

    private void SkuSearch_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var selectable = ViewModel.SkuResults.FirstOrDefault(item => item.Selectable);
        if (selectable is null) return;
        ViewModel.AddSku(selectable);
        e.Handled = true;
    }

    private async void BuildQuotation_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.BuildAsync();

    private void UseSystemPrice_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SalesQuotationRowView row })
            ViewModel.UseSystemPrice(row);
    }

    private void ExportXlsx_OnClick(object sender, RoutedEventArgs e) => Export("xlsx");
    private void ExportCsv_OnClick(object sender, RoutedEventArgs e) => Export("csv");

    private void Export(string format)
    {
        var dialog = new SaveFileDialog
        {
            FileName = ViewModel.DefaultExportFileName(format),
            AddExtension = true,
            DefaultExt = format == "xlsx" ? ".xlsx" : ".csv",
            Filter = format == "xlsx" ? "Excel (*.xlsx)|*.xlsx" : "CSV (*.csv)|*.csv"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            ViewModel.Export(format, dialog.FileName);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                string.IsNullOrWhiteSpace(exception.Message) ? "Không xuất được báo giá." : exception.Message,
                "Báo giá",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async void OpenSalesOrders_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this)?.DataContext is ShellViewModel shell)
            await shell.NavigateSalesAsync();
    }
}
