using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CongTy.Desktop.Logistics;

public partial class DeliveryOrderView : UserControl
{
    private readonly DeliveryOrderViewModel _viewModel;

    public DeliveryOrderView(DeliveryOrderViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void DeliveryOrderView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void DeliveryOrderView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshAsync();
    private async void Create_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CreateAsync();
    private async void Confirm_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ConfirmAsync();
    private async void Cancel_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CancelAsync();
    private async void Pickup_OnClick(object sender, RoutedEventArgs e) => await _viewModel.PickupHandoverAsync();
    private async void Manual_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ManualHandoverAsync();
    private async void Reverse_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ReverseInventoryIssueAsync();

    private void Print_OnClick(object sender, RoutedEventArgs e)
    {
        var order = _viewModel.SelectedOrderDetail;
        if (order is null || !_viewModel.CanPrintSelected) return;

        var packing = string.Equals(_viewModel.SelectedPrintVariant, "Phiếu đóng gói", StringComparison.Ordinal);
        var title = packing ? "PHIẾU ĐÓNG GÓI" : "PHIẾU GIAO HÀNG";
        var document = new FlowDocument
        {
            PagePadding = new Thickness(42),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11
        };
        document.Blocks.Add(new Paragraph(new Run(title)) { FontSize = 20, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
        document.Blocks.Add(new Paragraph(new Run(packing ? "Danh sách đóng gói" : "Chứng từ giao nhận")) { TextAlignment = TextAlignment.Center });
        document.Blocks.Add(new Paragraph(new Run(
            $"Số phiếu: {order.Number}    Trạng thái: {DeliveryOrderPresentation.Status(order.Status)}\n" +
            $"Đơn bán hàng: {order.SalesOrderNumber ?? "—"}\n" +
            $"Khách hàng: {order.CustomerCode} — {order.CustomerName}\n" +
            $"Kho xuất: {order.WarehouseCode} — {order.WarehouseName}\n" +
            $"Hình thức: {DeliveryOrderPresentation.HandoverMode(order.HandoverMode)}\n" +
            $"Ngày giao dự kiến: {DeliveryOrderPresentation.Date(order.RequestedDeliveryDate)}")));

        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(42) });
        table.Columns.Add(new TableColumn { Width = new GridLength(260) });
        table.Columns.Add(new TableColumn { Width = new GridLength(90) });
        table.Columns.Add(new TableColumn { Width = new GridLength(150) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        group.Rows.Add(Row("STT", "Sản phẩm / SKU", "Số lượng", "Vị trí / lô", true));
        foreach (var line in order.Lines.OrderBy(line => line.LineNumber))
        {
            group.Rows.Add(Row(
                line.LineNumber.ToString(),
                $"{line.ItemName}\n{line.Sku}",
                $"{DeliveryOrderPresentation.Quantity(line.DeliveryBaseQuantity)} {line.UnitCode}",
                $"{line.LocationCode ?? "—"} / {line.LotCode ?? "—"}",
                false));
        }
        document.Blocks.Add(table);
        document.Blocks.Add(new Paragraph(new Run(
            $"{(packing ? "Tổng SL đóng gói" : "Tổng SL giao")}: {DeliveryOrderPresentation.Quantity(order.TotalBaseQuantity)}")) { FontWeight = FontWeights.Bold });
        if (!string.IsNullOrWhiteSpace(order.Note)) document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {order.Note}")));

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"{title} {order.Number}");
        }
    }

    private static TableRow Row(string a, string b, string c, string d, bool header)
    {
        var row = new TableRow { FontWeight = header ? FontWeights.Bold : FontWeights.Normal };
        foreach (var value in new[] { a, b, c, d })
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(value)) { Margin = new Thickness(4) })
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(3)
            });
        }
        return row;
    }
}
