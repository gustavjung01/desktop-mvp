using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace CongTy.Desktop.Inventory;

public partial class InventoryView : UserControl
{
    private readonly InventoryViewModel _viewModel;

    public InventoryView(InventoryViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void RefreshBalances_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshBalancesAsync();
    private async void RefreshLots_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshLotsAsync();
    private async void ApplyReport_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshReportAsync();
    private async void ResetReport_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ResetReportAsync();
    private async void RefreshHistory_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshHistoryAsync();
    private async void HistoryPrevious_OnClick(object sender, RoutedEventArgs e) => await _viewModel.HistoryPreviousAsync();
    private async void HistoryNext_OnClick(object sender, RoutedEventArgs e) => await _viewModel.HistoryNextAsync();

    private async void OpenHistory_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: InventoryBalanceRow row }) await _viewModel.OpenHistoryAsync(row);
    }

    private async void BalanceGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (BalanceGrid.SelectedItem is InventoryBalanceRow row) await _viewModel.OpenHistoryAsync(row);
    }

    private void HistoryGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HistoryGrid.SelectedItem is InventoryHistoryRow row) _viewModel.OpenHistoryDetail(row);
    }

    private void CloseHistoryDetail_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseHistoryDetail();

    private async void OpenReportHolds_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: InventoryPositionRow row }) await _viewModel.OpenReportHoldsAsync(row);
    }

    private void CloseReportHolds_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseReportHolds();

    private void ExportReport_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Xuất báo cáo tồn kho",
                Filter = "Tệp CSV (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = $"bao-cao-ton-kho-{DateTime.Now:yyyyMMdd-HHmm}.csv"
            };
            if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

            File.WriteAllText(dialog.FileName, _viewModel.BuildReportExportCsv(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            MessageBox.Show(
                $"Đã xuất {_viewModel.ExportDescription} vào tệp đã chọn.",
                "Xuất báo cáo tồn kho",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Không xuất được báo cáo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void InventoryView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _viewModel.IsHoldDetailOpen)
        {
            e.Handled = true;
            _viewModel.CloseReportHolds();
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsHistoryDetailOpen)
        {
            e.Handled = true;
            _viewModel.CloseHistoryDetail();
            return;
        }

        if (e.Key == Key.F5)
        {
            e.Handled = true;
            if (_viewModel.MainTabIndex == 0 && _viewModel.LookupTabIndex == 1)
                await _viewModel.RefreshHistoryAsync();
            else if (_viewModel.MainTabIndex == 0)
                await _viewModel.RefreshBalancesAsync();
            else if (_viewModel.MainTabIndex == 1)
                await _viewModel.RefreshReportAsync();
            else
                await _viewModel.RefreshLotsAsync();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            e.Handled = true;
            if (_viewModel.MainTabIndex == 1)
            {
                ReportWarehouseBox.Focus();
                ReportWarehouseBox.IsDropDownOpen = true;
            }
            else if (_viewModel.MainTabIndex == 2)
            {
                LotSearchBox.Focus();
                LotSearchBox.SelectAll();
            }
            else
            {
                BalanceSearchBox.Focus();
                BalanceSearchBox.SelectAll();
            }
            return;
        }

        if (e.Key != Key.Enter) return;

        if (_viewModel.MainTabIndex == 1 && _viewModel.ReportTabIndex == 1
            && PositionGrid.IsKeyboardFocusWithin && PositionGrid.SelectedItem is InventoryPositionRow position)
        {
            e.Handled = true;
            await _viewModel.OpenReportHoldsAsync(position);
        }
        else if (_viewModel.MainTabIndex == 0 && _viewModel.LookupTabIndex == 0
            && BalanceGrid.IsKeyboardFocusWithin && BalanceGrid.SelectedItem is InventoryBalanceRow balance)
        {
            e.Handled = true;
            await _viewModel.OpenHistoryAsync(balance);
        }
        else if (_viewModel.MainTabIndex == 0 && _viewModel.LookupTabIndex == 1
                 && HistoryGrid.IsKeyboardFocusWithin && HistoryGrid.SelectedItem is InventoryHistoryRow history)
        {
            e.Handled = true;
            _viewModel.OpenHistoryDetail(history);
        }
    }
}
