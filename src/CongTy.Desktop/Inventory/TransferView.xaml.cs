using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public partial class TransferView : UserControl
{
    private readonly TransferViewModel _viewModel;
    public TransferView(TransferViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public Task RefreshAsync() => _viewModel.RefreshAsync();
    public void OpenCreate() => _viewModel.OpenCreate();

    private async void TransferView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private void TransfersMode_OnClick(object sender, RoutedEventArgs e) => _viewModel.ModeIndex = 0;
    private void TransitMode_OnClick(object sender, RoutedEventArgs e) => _viewModel.ModeIndex = 1;
    private void ResetFilters_OnClick(object sender, RoutedEventArgs e) => _viewModel.ResetFilters();
    private void CloseCreate_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseCreate();
    private void CancelCreate_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseCreate();
    private void AddDraftLine_OnClick(object sender, RoutedEventArgs e) => _viewModel.AddDraftLine();

    private void RemoveDraftLine_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TransferDraftLineRow row }) _viewModel.RemoveDraftLine(row);
    }

    private async void SaveCreate_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CreateAsync();

    private async void OpenTransfer_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) await _viewModel.OpenTransferAsync(id);
    }

    private void CloseDetail_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseDetail();
    private async void ApproveTransfer_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ApproveAsync();
    private async void DispatchTransfer_OnClick(object sender, RoutedEventArgs e) => await _viewModel.DispatchAsync();
    private async void CancelTransfer_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CancelAsync();
    private void OpenReceiptForm_OnClick(object sender, RoutedEventArgs e) => _viewModel.OpenReceiptForm();
    private void CloseReceiptForm_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseReceiptForm();
    private async void SubmitReceipt_OnClick(object sender, RoutedEventArgs e) => await _viewModel.SubmitReceiptAsync();

    private async void ApproveDamage_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TransferReceiptHistoryRow row }) await _viewModel.ApproveDamageAsync(row);
    }

    private async void ReverseReceipt_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TransferReceiptHistoryRow row }) await _viewModel.ReverseReceiptAsync(row);
    }

    private async void CloseShort_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CloseShortAsync();

    private void PrintTransfer_OnClick(object sender, RoutedEventArgs e)
    {
        var transfer = _viewModel.GetPrintableTransfer();
        if (transfer is null) return;

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        var document = BuildPrintDocument(transfer);
        document.PageHeight = dialog.PrintableAreaHeight;
        document.PageWidth = dialog.PrintableAreaWidth;
        document.PagePadding = new Thickness(40);
        document.ColumnGap = 0;
        document.ColumnWidth = Math.Max(1, dialog.PrintableAreaWidth - 80);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, transfer.DocumentNumber ?? "Phiếu chuyển kho");
    }

    private static FlowDocument BuildPrintDocument(InventoryTransferData transfer)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11
        };
        document.Blocks.Add(new Paragraph(new Run("PHIẾU CHUYỂN KHO")) { FontSize = 18, FontWeight = FontWeights.SemiBold });
        document.Blocks.Add(new Paragraph(new Run(transfer.DocumentNumber ?? "Phiếu nháp chưa cấp số")));
        document.Blocks.Add(new Paragraph(new Run(
            $"{transfer.SourceWarehouseCode} — {transfer.SourceWarehouseName} → {transfer.DestinationWarehouseCode} — {transfer.DestinationWarehouseName}")));
        document.Blocks.Add(new Paragraph(new Run($"Ngày chuyển: {InventoryPresentation.Date(transfer.TransferDate)}")));
        if (!string.IsNullOrWhiteSpace(transfer.Note))
            document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {transfer.Note}")));

        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(35) });
        table.Columns.Add(new TableColumn { Width = new GridLength(150) });
        table.Columns.Add(new TableColumn { Width = new GridLength(220) });
        table.Columns.Add(new TableColumn { Width = new GridLength(100) });
        table.Columns.Add(new TableColumn { Width = new GridLength(100) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        group.Rows.Add(Row("STT", "SKU", "Sản phẩm", "Số lượng", "Lô", true));
        foreach (var line in transfer.Lines)
            group.Rows.Add(Row(
                line.LineNumber.ToString(),
                line.SourceSku,
                line.ItemName,
                $"{InventoryPresentation.Quantity(line.SourceQuantity)} {line.SourceUnitCode}",
                InventoryPresentation.First(line.LotCode, "Không lô"),
                false));
        document.Blocks.Add(table);
        return document;
    }

    private static TableRow Row(string a, string b, string c, string d, string e, bool header)
    {
        var row = new TableRow();
        foreach (var text in new[] { a, b, c, d, e })
        {
            var paragraph = new Paragraph(new Run(text)) { Margin = new Thickness(4) };
            if (header) paragraph.FontWeight = FontWeights.SemiBold;
            row.Cells.Add(new TableCell(paragraph)
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            });
        }
        return row;
    }

    private async void TransferView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_viewModel.IsReceiptFormOpen)
            {
                _viewModel.CloseReceiptForm();
                e.Handled = true;
                return;
            }
            if (_viewModel.IsCreateOpen)
            {
                _viewModel.CloseCreate();
                e.Handled = true;
                return;
            }
            if (_viewModel.HasSelectedTransfer)
            {
                _viewModel.CloseDetail();
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshAsync();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            e.Handled = true;
            TransferSearchBox.Focus();
            TransferSearchBox.SelectAll();
        }
    }
}
