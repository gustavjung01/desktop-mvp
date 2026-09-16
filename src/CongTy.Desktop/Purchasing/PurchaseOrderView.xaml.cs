using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace CongTy.Desktop.Purchasing;

public partial class PurchaseOrderView : UserControl
{
    private readonly PurchaseOrderViewModel _viewModel;

    public PurchaseOrderView(PurchaseOrderViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync().ConfigureAwait(true);

    private void Create_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.BeginCreate();

    private async void View_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PurchaseOrderRow row)
        {
            await _viewModel.ShowDetailAsync(row).ConfigureAwait(true);
        }
    }

    private async void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PurchaseOrderRow row)
        {
            await _viewModel.BeginEditAsync(row).ConfigureAwait(true);
        }
    }

    private async void Submit_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not PurchaseOrderRow row) return;
        if (MessageBox.Show(
                $"Gửi {row.Number} sang trạng thái chờ duyệt? Sau đó nội dung đơn sẽ không còn được sửa trực tiếp.",
                "Gửi duyệt đơn mua hàng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _viewModel.SubmitAsync(row).ConfigureAwait(true);
        }
    }

    private async void Approve_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not PurchaseOrderRow row) return;
        if (MessageBox.Show(
                $"Duyệt {row.Number} và cấp số chứng từ chính thức?",
                "Duyệt đơn mua hàng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _viewModel.ApproveAsync(row).ConfigureAwait(true);
        }
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PurchaseOrderRow row)
        {
            _viewModel.BeginCancel(row);
        }
    }

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not PurchaseOrderRow row) return;
        var order = await _viewModel.GetForPrintAsync(row).ConfigureAwait(true);
        if (order is null) return;

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;
        var document = PurchaseOrderPrintPreview.Create(order);
        dialog.PrintDocument(
            ((System.Windows.Documents.IDocumentPaginatorSource)document).DocumentPaginator,
            $"Đơn mua hàng {order.Number ?? string.Empty}");
    }

    private void CloseEditor_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseEditor();

    private async void Save_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveAsync().ConfigureAwait(true);

    private async void SearchSku_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SearchSkuAsync().ConfigureAwait(true);

    private async void BrowseSku_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.BrowseSkuAsync().ConfigureAwait(true);

    private async void SkuSearch_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await _viewModel.SearchSkuAsync().ConfigureAwait(true);
    }

    private async void AddSku_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PurchaseOrderSkuResultRow row)
        {
            await _viewModel.AddSkuAsync(row).ConfigureAwait(true);
        }
    }

    private async void SkuResults_OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as DataGrid)?.SelectedItem is PurchaseOrderSkuResultRow row)
        {
            await _viewModel.AddSkuAsync(row).ConfigureAwait(true);
        }
    }

    private void RemoveLine_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PurchaseOrderEditorLine line)
        {
            _viewModel.RemoveLine(line);
        }
    }

    private async void RefreshPrices_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshPricesAsync().ConfigureAwait(true);

    private async void Supplier_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel.IsEditorOpen)
        {
            await _viewModel.RefreshPricesAsync().ConfigureAwait(true);
        }
    }

    private async void OrderDate_OnChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel.IsEditorOpen)
        {
            await _viewModel.RefreshPricesAsync().ConfigureAwait(true);
        }
    }

    private async void EditorLines_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        _viewModel.MarkDraftChanged();
        if (e.Row.Item is not PurchaseOrderEditorLine line) return;
        var header = e.Column.Header?.ToString();
        if (!string.Equals(header, "Số lượng", StringComparison.Ordinal)) return;

        await Dispatcher.InvokeAsync(() => { });
        if (!line.ManualPrice)
        {
            await _viewModel.ResolvePriceAsync(line).ConfigureAwait(true);
        }
    }

    private void DownloadBulkTemplate_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = "mau-don-mua-hang.xlsx",
            AddExtension = true
        };
        if (dialog.ShowDialog() != true) return;
        File.WriteAllBytes(dialog.FileName, PurchaseOrderBulkImport.CreateTemplate());
    }

    private async void ChooseBulkFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Dữ liệu nhập (*.xlsx;*.csv;*.tsv)|*.xlsx;*.csv;*.tsv|Excel Workbook (*.xlsx)|*.xlsx|Text (*.csv;*.tsv)|*.csv;*.tsv"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            IReadOnlyList<PurchaseOrderBulkInputRow> rows;
            if (string.Equals(Path.GetExtension(dialog.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                rows = PurchaseOrderBulkImport.ReadXlsx(File.ReadAllBytes(dialog.FileName));
            }
            else
            {
                rows = PurchaseOrderBulkImport.ParseText(File.ReadAllText(dialog.FileName));
            }

            await _viewModel.ImportBulkAsync(rows).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Không đọc được tệp nhập nhiều dòng. {exception.Message}",
                "Đơn mua hàng",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async void ImportBulkText_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ImportBulkTextAsync().ConfigureAwait(true);

    private void CloseDetail_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseDetail();

    private void CloseCancel_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseCancel();

    private async void ConfirmCancel_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ConfirmCancelAsync().ConfigureAwait(true);
}
