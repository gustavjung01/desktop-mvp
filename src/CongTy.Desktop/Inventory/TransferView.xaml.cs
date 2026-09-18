using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CongTy.Desktop.Printing;

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

    private async void PrintTransfer_OnClick(object sender, RoutedEventArgs e)
    {
        var transfer = _viewModel.GetPrintableTransfer();
        if (transfer is null) return;

        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "INVENTORY_TRANSFER");
        if (template is null) return;
        InventoryTransferPrintPreview.Print(transfer, template);
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
