using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Purchasing;

public partial class SupplierReturnView : UserControl
{
    private readonly SupplierReturnViewModel _viewModel;

    public SupplierReturnView(SupplierReturnViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public Task OpenCreateFromReceiptAsync(string receiptId) =>
        _viewModel.BeginCreateAsync(receiptId);

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync().ConfigureAwait(true);

    private async void Create_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.BeginCreateAsync().ConfigureAwait(true);

    private async void SourceReceipt_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        await _viewModel.LoadSelectedReceiptAsync().ConfigureAwait(true);

    private async void View_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is SupplierReturnRow row)
            await _viewModel.ShowDetailAsync(row).ConfigureAwait(true);
    }

    private async void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is SupplierReturnRow row)
            await _viewModel.BeginEditAsync(row).ConfigureAwait(true);
    }

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not SupplierReturnRow row) return;
        var item = await _viewModel.GetForPrintAsync(row).ConfigureAwait(true);
        if (item is null) return;
        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "SUPPLIER_RETURN");
        if (template is null) return;
        SupplierReturnPrintPreview.Print(Window.GetWindow(this), item, template);
    }

    private void Submit_OnClick(object sender, RoutedEventArgs e) => BeginAction(sender, "submit");
    private void Approve_OnClick(object sender, RoutedEventArgs e) => BeginAction(sender, "approve");
    private void Cancel_OnClick(object sender, RoutedEventArgs e) => BeginAction(sender, "cancel");
    private void Post_OnClick(object sender, RoutedEventArgs e) => BeginAction(sender, "post");
    private void Reverse_OnClick(object sender, RoutedEventArgs e) => BeginAction(sender, "reverse");

    private void BeginAction(object sender, string action)
    {
        if ((sender as FrameworkElement)?.Tag is SupplierReturnRow row)
            _viewModel.BeginAction(row, action);
    }

    private void CloseEditor_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseEditor();

    private async void Save_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveAsync().ConfigureAwait(true);

    private void CloseDetail_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseDetail();

    private void CloseAction_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseAction();

    private async void ConfirmAction_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ConfirmActionAsync().ConfigureAwait(true);
}
