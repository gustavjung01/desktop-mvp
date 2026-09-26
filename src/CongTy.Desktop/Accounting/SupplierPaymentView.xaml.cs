using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Accounting;

public partial class SupplierPaymentView : UserControl
{
    public SupplierPaymentView(SupplierPaymentViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private SupplierPaymentViewModel ViewModel => (SupplierPaymentViewModel)DataContext;

    private async void SupplierPaymentView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void SavePayment_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SavePaymentAsync();

    private async void Allocate_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.AllocateAsync();

    private async void ReversePayment_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReversePaymentAsync();

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        var payment = ViewModel.SelectedPayment;
        if (payment is null || string.IsNullOrWhiteSpace(payment.DocumentNumber)) return;
        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "SUPPLIER_PAYMENT");
        if (template is null) return;
        ActualDocumentPrintPreview.PrintSupplierPayment(Window.GetWindow(this), payment, template);
    }

    private async void ReverseAllocation_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SupplierPaymentAllocationRow row })
            await ViewModel.ReverseAllocationAsync(row);
    }

    private async void Payments_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedPaymentRow is not { } row) return;
        if (string.Equals(row.Id, ViewModel.SelectedPaymentId, StringComparison.Ordinal)) return;
        await ViewModel.OpenPaymentAsync(row.Id);
    }
}
