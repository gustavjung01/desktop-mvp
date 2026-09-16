using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace CongTy.Desktop.Inventory;

public partial class InventoryAdjustmentView : UserControl
{
    private readonly InventoryAdjustmentViewModel _viewModel;

    public InventoryAdjustmentView(InventoryAdjustmentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public Task<bool> RefreshAsync() => _viewModel.RefreshAsync();

    private async void InventoryAdjustmentView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void InventoryAdjustmentView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshAsync();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.D1)
            {
                e.Handled = true;
                _viewModel.SetTab("documents");
            }
            else if (e.Key == Key.D2)
            {
                e.Handled = true;
                _viewModel.SetTab("manual");
            }
            else if (e.Key == Key.D3)
            {
                e.Handled = true;
                _viewModel.SetTab("bulk");
            }
        }
    }

    private void DocumentsTab_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SetTab("documents");

    private void ManualTab_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SetTab("manual");

    private void BulkTab_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SetTab("bulk");

    private void ResetFilters_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.ResetFilters();

    private async void AdjustmentList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AdjustmentList.SelectedItem is InventoryAdjustmentListRow row)
        {
            await _viewModel.SelectAdjustmentAsync(row.Data.Id);
        }
    }

    private async void CreateManual_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CreateManualAsync();

    private async void Submit_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SubmitAsync();

    private async void Approve_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ApproveAsync();

    private async void Post_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PostAsync();

    private async void Cancel_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.CancelAsync();

    private async void Reverse_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ReverseAsync();

    private void DownloadBulkTemplate_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu tệp mẫu điều chỉnh tồn",
            Filter = "CSV (*.csv)|*.csv",
            FileName = "mau-dieu-chinh-ton-hang-loat.csv",
            AddExtension = true,
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() != true) return;

        File.WriteAllText(
            dialog.FileName,
            InventoryAdjustmentBulkFile.TemplateCsv,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private void ChooseBulkFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn tệp điều chỉnh tồn",
            Filter = "Excel hoặc CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;
        _viewModel.LoadBulkFile(dialog.FileName);
    }

    private async void PreviewBulk_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviewBulkAsync();

    private async void ConfirmBulk_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ConfirmBulkAsync();
}
