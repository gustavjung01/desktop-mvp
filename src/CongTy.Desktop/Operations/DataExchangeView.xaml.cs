using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Shell;
using Microsoft.Win32;

namespace CongTy.Desktop.Operations;

public partial class DataExchangeView : UserControl
{
    public DataExchangeView(DataExchangeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private DataExchangeViewModel ViewModel => (DataExchangeViewModel)DataContext;

    private async void DataExchangeView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void ProductImport_OnClick(object sender, RoutedEventArgs e) =>
        await PrepareImportAsync("products");

    private async void PricingImport_OnClick(object sender, RoutedEventArgs e) =>
        await PrepareImportAsync("pricing");

    private async void StocktakeImport_OnClick(object sender, RoutedEventArgs e) =>
        await PrepareImportAsync("stocktake");

    private async Task PrepareImportAsync(string kind)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel hoặc CSV (*.xlsx;*.csv)|*.xlsx;*.csv",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            await ViewModel.PrepareImportAsync(kind, dialog.FileName);
    }

    private async void ConfirmImport_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ConfirmPendingImportAsync();

    private void CancelImport_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CancelPendingImport();

    private void ProductTemplateXlsx_OnClick(object sender, RoutedEventArgs e) =>
        WriteTemplate("xlsx", "mau-san-pham-sku.xlsx", ViewModel.WriteProductTemplate);

    private void ProductTemplateCsv_OnClick(object sender, RoutedEventArgs e) =>
        WriteTemplate("csv", "mau-san-pham-sku.csv", ViewModel.WriteProductTemplate);

    private void PricingTemplateXlsx_OnClick(object sender, RoutedEventArgs e) =>
        WriteTemplate("xlsx", "mau-cap-nhat-gia.xlsx", ViewModel.WritePricingTemplate);

    private void PricingTemplateCsv_OnClick(object sender, RoutedEventArgs e) =>
        WriteTemplate("csv", "mau-cap-nhat-gia.csv", ViewModel.WritePricingTemplate);

    private void WriteTemplate(string format, string defaultName, Action<string, string> writer)
    {
        var path = SavePath(format, defaultName);
        if (path is null) return;
        try
        {
            writer(format, path);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                string.IsNullOrWhiteSpace(exception.Message) ? "Không tạo được tệp mẫu." : exception.Message,
                "Nhập/xuất dữ liệu",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async void ProductExportXlsx_OnClick(object sender, RoutedEventArgs e) =>
        await ExportProductAsync("xlsx", "san-pham-sku.xlsx");

    private async void ProductExportCsv_OnClick(object sender, RoutedEventArgs e) =>
        await ExportProductAsync("csv", "san-pham-sku.csv");

    private async Task ExportProductAsync(string format, string defaultName)
    {
        var path = SavePath(format, defaultName);
        if (path is not null) await ViewModel.ExportProductsAsync(format, path);
    }

    private async void PricingExportXlsx_OnClick(object sender, RoutedEventArgs e) =>
        await ExportPricingAsync("xlsx");

    private async void PricingExportCsv_OnClick(object sender, RoutedEventArgs e) =>
        await ExportPricingAsync("csv");

    private async Task ExportPricingAsync(string format)
    {
        var path = SavePath(format, $"cap-nhat-gia.{format}");
        if (path is not null) await ViewModel.ExportPricingAsync(format, path);
    }

    private async void StocktakeExportXlsx_OnClick(object sender, RoutedEventArgs e) =>
        await ExportStocktakeAsync("xlsx");

    private async void StocktakeExportCsv_OnClick(object sender, RoutedEventArgs e) =>
        await ExportStocktakeAsync("csv");

    private async Task ExportStocktakeAsync(string format)
    {
        var path = SavePath(format, $"kiem-ke.{format}");
        if (path is not null) await ViewModel.ExportStocktakeAsync(format, path);
    }

    private async void BuildQuotation_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.BuildQuotationAsync();

    private void QuotationExportXlsx_OnClick(object sender, RoutedEventArgs e) =>
        ExportQuotation("xlsx");

    private void QuotationExportCsv_OnClick(object sender, RoutedEventArgs e) =>
        ExportQuotation("csv");

    private void ExportQuotation(string format)
    {
        var path = SavePath(format, $"bao-gia.{format}");
        if (path is null) return;
        try
        {
            ViewModel.ExportQuotation(format, path);
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "Báo giá", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void LoadMovements_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadMovementsAsync(append: false);

    private async void LoadMoreMovements_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadMovementsAsync(append: true);

    private async void MovementExportXlsx_OnClick(object sender, RoutedEventArgs e) =>
        await ExportMovementAsync("xlsx");

    private async void MovementExportCsv_OnClick(object sender, RoutedEventArgs e) =>
        await ExportMovementAsync("csv");

    private async Task ExportMovementAsync(string format)
    {
        var path = SavePath(format, $"Bien-dong-kho.{format}");
        if (path is not null) await ViewModel.ExportMovementsAsync(format, path);
    }

    private async void OpenStocktake_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this)?.DataContext is ShellViewModel shell)
            await shell.NavigateInventoryStocktakeAsync();
    }

    private void SelectAllPending_OnClick(object sender, RoutedEventArgs e) => ViewModel.SelectPendingRows("all");
    private void SelectBasePending_OnClick(object sender, RoutedEventArgs e) => ViewModel.SelectPendingRows("base");
    private void SelectCartonPending_OnClick(object sender, RoutedEventArgs e) => ViewModel.SelectPendingRows("carton");
    private void ClearPendingSelection_OnClick(object sender, RoutedEventArgs e) => ViewModel.SelectPendingRows("none");
    private void ExpandSameProduct_OnClick(object sender, RoutedEventArgs e) => ViewModel.ExpandSameProductSelection();
    private void ApplyBulk_OnClick(object sender, RoutedEventArgs e) => ViewModel.ApplyBulkValue();

    private string? SavePath(string format, string defaultName)
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultName,
            AddExtension = true,
            DefaultExt = format == "xlsx" ? ".xlsx" : ".csv",
            Filter = format == "xlsx" ? "Excel (*.xlsx)|*.xlsx" : "CSV (*.csv)|*.csv"
        };
        return dialog.ShowDialog(Window.GetWindow(this)) == true ? dialog.FileName : null;
    }
}
