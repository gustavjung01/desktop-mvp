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

    private const string TemplateCsv =
        "SKU,Khối lượng,Đơn vị khối lượng\r\n" +
        "SKU-MAU,1.5,KG\r\n";

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
        File.WriteAllText(dialog.FileName, TemplateCsv, new UTF8Encoding(true));
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
