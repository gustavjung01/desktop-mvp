using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Printing;

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

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        var payment = ViewModel.SelectedPayment;
        if (payment is null) return;

        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "CUSTOMER_PAYMENT");
        if (template is null) return;
        CustomerPaymentPrintPreview.Print(Window.GetWindow(this), payment, template);
    }
}
