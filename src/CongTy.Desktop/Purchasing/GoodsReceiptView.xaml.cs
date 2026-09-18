using System.IO;
using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Purchasing;

public partial class GoodsReceiptView : UserControl
{
    private readonly GoodsReceiptViewModel _viewModel;

    public GoodsReceiptView(GoodsReceiptViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public event Action<string>? SupplierReturnRequested;

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync().ConfigureAwait(true);

    private void Create_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.BeginCreate();

    private async void PurchaseOrder_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        await _viewModel.LoadSelectedPurchaseOrderAsync().ConfigureAwait(true);

    private async void View_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is GoodsReceiptRow row)
        {
            await _viewModel.ShowDetailAsync(row).ConfigureAwait(true);
        }
    }

    private async void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is GoodsReceiptRow row)
        {
            await _viewModel.BeginEditAsync(row).ConfigureAwait(true);
        }
    }

    private async void Post_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not GoodsReceiptRow row) return;
        if (MessageBox.Show(
                $"Ghi sổ {row.Number}? Phần Chấp nhận sẽ được cập nhật vào tồn kho.",
                "Ghi sổ phiếu nhận hàng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _viewModel.PreparePostAsync(row).ConfigureAwait(true);
        }
    }

    private void Reverse_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is GoodsReceiptRow row)
        {
            _viewModel.BeginReverse(row);
        }
    }

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not GoodsReceiptRow row) return;
        var receipt = await _viewModel.GetForPrintAsync(row).ConfigureAwait(true);
        if (receipt is null) return;

        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "GOODS_RECEIPT");
        if (template is null) return;

        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        var document = GoodsReceiptPrintPreview.Create(receipt, template);
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template);
        dialog.PrintDocument(
            ((System.Windows.Documents.IDocumentPaginatorSource)document).DocumentPaginator,
            $"Phiếu nhận hàng {receipt.DocumentNumber ?? string.Empty}");
    }

    private void CloseEditor_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseEditor();

    private async void Save_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveAsync().ConfigureAwait(true);

    private void SupplierReturn_OnClick(object sender, RoutedEventArgs e)
    {
        var receiptId = _viewModel.GetSupplierReturnSourceId();
        if (string.IsNullOrWhiteSpace(receiptId)) return;
        _viewModel.CloseDetail();
        SupplierReturnRequested?.Invoke(receiptId);
    }

    private void CloseDetail_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseDetail();

    private void CloseReverse_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseReverse();

    private async void ConfirmReverse_OnClick(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
                "Xác nhận đảo phiếu? Tồn kho đã ghi sổ sẽ được phát hành chứng từ bù.",
                "Đảo phiếu nhận hàng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await _viewModel.ConfirmReverseAsync().ConfigureAwait(true);
        }
    }
}
