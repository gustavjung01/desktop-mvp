using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Accounting;

public partial class CustomerReturnCreditView : UserControl
{
    public CustomerReturnCreditView(CustomerReturnCreditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private CustomerReturnCreditViewModel ViewModel => (CustomerReturnCreditViewModel)DataContext;

    private async void CustomerReturnCreditView_OnLoaded(object sender, RoutedEventArgs e) => await ViewModel.EnsureLoadedAsync();
    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await ViewModel.RefreshAsync();

    private async void Credits_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedCreditRow is not { } row) return;
        if (string.Equals(ViewModel.SelectedCredit?.Id, row.Id, StringComparison.Ordinal)) return;
        await ViewModel.OpenCreditAsync(row.Id);
    }

    private async void Allocate_OnClick(object sender, RoutedEventArgs e) => await ViewModel.AllocateAsync();
    private async void Refund_OnClick(object sender, RoutedEventArgs e) => await ViewModel.CreateRefundAsync();

    private async void ReverseRefund_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: CustomerReturnCreditRefundRow row })
            await ViewModel.ReverseRefundAsync(row);
    }

    private async void ReverseCredit_OnClick(object sender, RoutedEventArgs e) => await ViewModel.ReverseCreditAsync();
}
