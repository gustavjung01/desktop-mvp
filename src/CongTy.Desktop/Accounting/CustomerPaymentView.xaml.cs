using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CongTy.Desktop.Accounting;

public partial class CustomerPaymentView : UserControl
{
    public CustomerPaymentView(CustomerPaymentViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private CustomerPaymentViewModel ViewModel => (CustomerPaymentViewModel)DataContext;

    private async void CustomerPaymentView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void SavePayment_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SavePaymentAsync();

    private async void Allocate_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.AllocateSelectedAsync();

    private async void ReversePayment_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReversePaymentAsync();

    private async void ReverseAllocation_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: CustomerPaymentAllocationRow row })
            await ViewModel.ReverseAllocationAsync(row);
    }

    private async void Payments_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedPaymentRow is not { } row) return;
        if (string.Equals(ViewModel.SelectedPayment?.Id, row.Id, StringComparison.Ordinal)) return;
        await ViewModel.OpenPaymentAsync(row.Id);
    }

    private void Print_OnClick(object sender, RoutedEventArgs e)
    {
        var payment = ViewModel.SelectedPayment;
        if (payment is null) return;

        var document = new FlowDocument
        {
            PageWidth = 559,
            PageHeight = 794,
            PagePadding = new Thickness(34),
            ColumnGap = 0,
            ColumnWidth = double.PositiveInfinity,
            FontFamily = FontFamily,
            FontSize = 12
        };

        document.Blocks.Add(new Paragraph(new Run("PHIẾU THU"))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });
        document.Blocks.Add(new Paragraph(new Run("Chứng từ thu tiền khách hàng"))
        {
            TextAlignment = TextAlignment.Center
        });
        document.Blocks.Add(Line($"Số phiếu: {payment.DocumentNumber}"));
        document.Blocks.Add(Line($"Khách hàng: {CustomerPaymentPresentation.Party(payment.CustomerCode, payment.CustomerName)}"));
        document.Blocks.Add(Line($"Đơn vị nhận tiền: {CustomerPaymentPresentation.Party(payment.WarehouseCode, payment.WarehouseName)}"));
        document.Blocks.Add(Line($"Ngày thu: {CustomerPaymentPresentation.Date(payment.PaymentDate)}"));
        document.Blocks.Add(Line($"Hình thức nhận tiền: {CustomerPaymentPresentation.PaymentMethod(payment.PaymentMethod)}"));
        document.Blocks.Add(Line($"Kết quả: {CustomerPaymentPresentation.Status(payment.Status)}"));
        document.Blocks.Add(Line($"Mã giao dịch ngân hàng: {payment.ExternalReference ?? "—"}"));
        document.Blocks.Add(Line($"Nhân viên nộp tiền: {(string.IsNullOrWhiteSpace(payment.RemittingEmployeeName) ? "—" : CustomerPaymentPresentation.Party(payment.RemittingEmployeeCode, payment.RemittingEmployeeName))}"));
        document.Blocks.Add(Line($"Người ghi nhận: {payment.PostedBy}"));
        document.Blocks.Add(new Paragraph(new Run($"SỐ TIỀN ĐÃ NHẬN: {CustomerPaymentPresentation.Money(payment.OriginalAmount, payment.CurrencyCode)}"))
        {
            FontWeight = FontWeights.Bold,
            FontSize = 15
        });
        document.Blocks.Add(Line($"Đã ghi vào đơn: {CustomerPaymentPresentation.Money(payment.AllocatedAmount, payment.CurrencyCode)}"));
        document.Blocks.Add(Line($"Chưa gắn với đơn: {CustomerPaymentPresentation.Money(payment.RemainingAmount, payment.CurrencyCode)}"));
        if (!string.IsNullOrWhiteSpace(payment.Note))
            document.Blocks.Add(Line($"Ghi chú: {payment.Note}"));
        if (payment.Status == "reversed")
            document.Blocks.Add(Line($"ĐÃ HỦY{(string.IsNullOrWhiteSpace(payment.ReversalReason) ? string.Empty : $" — {payment.ReversalReason}")}"));

        var signatures = new Table { CellSpacing = 0 };
        signatures.Columns.Add(new TableColumn());
        signatures.Columns.Add(new TableColumn());
        signatures.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        var row = new TableRow();
        row.Cells.Add(SignatureCell("Người nộp tiền"));
        row.Cells.Add(SignatureCell("Người lập phiếu"));
        row.Cells.Add(SignatureCell("Thủ quỹ / Kế toán"));
        group.Rows.Add(row);
        signatures.RowGroups.Add(group);
        document.Blocks.Add(signatures);

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Phiếu thu {payment.DocumentNumber}");
    }

    private static Paragraph Line(string text) => new(new Run(text)) { Margin = new Thickness(0, 4, 0, 4) };

    private static TableCell SignatureCell(string title) =>
        new(new Paragraph(new Run($"{title}\n\n\n(Ký, ghi rõ họ tên)"))
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(4, 18, 4, 0)
        });
}
