using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Products;

public partial class ProductBulkUpdateView : UserControl
{
    public ProductBulkUpdateView() => InitializeComponent();

    private ProductViewModel ViewModel =>
        DataContext as ProductViewModel
        ?? throw new InvalidOperationException("Màn Cập nhật SP chưa có dữ liệu làm việc.");

    private const string BulkTemplateCsv =
        "SKU,Tên sản phẩm,Tên hiển thị bán hàng,Loại sản phẩm,Nhãn hàng,Mô tả,Ghi chú,Hiển thị sản phẩm khi bán hàng,Cho phép đặt hàng,Quản lý tồn kho,Sản phẩm đang sử dụng,Tên SKU / quy cách,Loại SKU,SKU dùng làm đơn vị tồn chuẩn,Cho phép bán SKU,Hiển thị SKU khi bán hàng,SKU đang sử dụng,Đơn vị tính,Hệ số quy đổi về đơn vị tồn chuẩn,Cho phép mua SKU,Định lượng quy cách,Đơn vị định lượng,Tên đơn vị nguồn,Mô tả quy cách nguồn,Khối lượng,Đơn vị khối lượng\r\n" +
        "SKU-MAU,,,,,,,,,,,,,,,,,,,,,,,,1.5,KG\r\n";

    private void DownloadTemplate_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu file mẫu cập nhật sản phẩm",
            Filter = "CSV (*.csv)|*.csv",
            FileName = "mau-cap-nhat-san-pham-theo-sku.csv",
            AddExtension = true,
            DefaultExt = ".csv"
        };
        if (dialog.ShowDialog() != true) return;
        File.WriteAllText(dialog.FileName, BulkTemplateCsv, new UTF8Encoding(true));
    }

    private async void ChooseFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn tệp cập nhật sản phẩm",
            Filter = "Tệp Excel hoặc CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Tất cả tệp (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
            await ViewModel.LoadBulkFileAsync(dialog.FileName);
    }

    private async void ChooseImportFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn tệp nhập sản phẩm",
            Filter = "Tệp Excel hoặc CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Tất cả tệp (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
            await ViewModel.PrepareProductImportAsync(dialog.FileName);
    }

    private void DownloadImportXlsx_OnClick(object sender, RoutedEventArgs e) =>
        SaveProductTemplate("xlsx");

    private void DownloadImportCsv_OnClick(object sender, RoutedEventArgs e) =>
        SaveProductTemplate("csv");

    private void SaveProductTemplate(string format)
    {
        var isCsv = format == "csv";
        var dialog = new SaveFileDialog
        {
            Title = "Lưu file mẫu nhập sản phẩm",
            Filter = isCsv ? "CSV (*.csv)|*.csv" : "Excel (*.xlsx)|*.xlsx",
            FileName = isCsv ? "mau-nhap-san-pham.csv" : "mau-nhap-san-pham.xlsx",
            AddExtension = true,
            DefaultExt = isCsv ? ".csv" : ".xlsx"
        };
        if (dialog.ShowDialog() == true)
            ViewModel.WriteProductImportTemplate(format, dialog.FileName);
    }

    private async void ExportProductsXlsx_OnClick(object sender, RoutedEventArgs e) =>
        await SaveProductExportAsync("xlsx");

    private async void ExportProductsCsv_OnClick(object sender, RoutedEventArgs e) =>
        await SaveProductExportAsync("csv");

    private async Task SaveProductExportAsync(string format)
    {
        var isCsv = format == "csv";
        var dialog = new SaveFileDialog
        {
            Title = "Xuất sản phẩm và SKU",
            Filter = isCsv ? "CSV (*.csv)|*.csv" : "Excel (*.xlsx)|*.xlsx",
            FileName = isCsv ? "san-pham-sku.csv" : "san-pham-sku.xlsx",
            AddExtension = true,
            DefaultExt = isCsv ? ".csv" : ".xlsx",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() == true)
            await ViewModel.ExportProductFileAsync(format, dialog.FileName);
    }

    private void SelectAllProductColumns_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.SelectAllProductExportColumns();

    private void ClearProductColumns_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.ClearProductExportColumns();

    private async void ConfirmImport_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ConfirmProductImportAsync();

    private void CancelImport_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CancelProductImport();

    private void Reset_OnClick(object sender, RoutedEventArgs e) => ViewModel.ResetBulkFile();

    private async void Header_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox box)
            await ViewModel.SetBulkHeaderAsync(box.IsChecked == true);
    }

    private void Mapping_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { DataContext: ProductBulkColumnRow row })
        {
            if (row.Locked)
            {
                row.Mapping = "SKU";
                return;
            }
            ViewModel.BulkMappingChanged();
        }
    }

    private async void Preview_OnClick(object sender, RoutedEventArgs e) => await ViewModel.PreviewBulkAsync();
    private async void Apply_OnClick(object sender, RoutedEventArgs e) => await ViewModel.ApplyBulkAsync();
}
