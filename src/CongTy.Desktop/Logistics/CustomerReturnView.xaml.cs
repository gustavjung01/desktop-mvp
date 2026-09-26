using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Logistics;

public partial class CustomerReturnView : UserControl
{
    private readonly CustomerReturnViewModel _viewModel;

    public CustomerReturnView(CustomerReturnViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public event Action? TripReconciliationRequested;

    private async void CustomerReturnView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void CustomerReturnView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private async void Create_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CreateAsync();

    private async void Receive_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ReceiveAsync();

    private async void Cancel_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CancelAsync();

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        var item = _viewModel.SelectedReturn;
        if (item is null || !string.Equals(item.Status, "received", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(item.Number)) return;
        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "CUSTOMER_RETURN");
        if (template is null) return;
        ActualDocumentPrintPreview.PrintCustomerReturn(Window.GetWindow(this), item, template);
    }

    private void TripReconciliation_OnClick(object sender, RoutedEventArgs e) =>
        TripReconciliationRequested?.Invoke();
}
