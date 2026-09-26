using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using CongTy.Desktop.Operations;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Inventory;

public partial class ManualInboundView : UserControl
{
    private readonly ManualInboundViewModel _viewModel;
    private CancellationTokenSource? _searchCancellation;
    private ManualInboundHistoryRow? _historyPrintSource;

    public ManualInboundView(ManualInboundViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public Task<bool> RefreshAsync() => _viewModel.RefreshAsync();

    private async void ManualInboundView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void ManualInboundView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshAsync();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            e.Handled = true;
            _viewModel.SetEntryMode("direct");
            ProductSearchBox.Focus();
            ProductSearchBox.SelectAll();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D1)
        {
            e.Handled = true;
            _viewModel.SetEntryMode("direct");
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D2)
        {
            e.Handled = true;
            _viewModel.SetEntryMode("file");
        }
    }

    private void DirectTab_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SetEntryMode("direct");

    private void FileTab_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SetEntryMode("file");

    private async void ProductSearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var cancellation = _searchCancellation;

        try
        {
            await Task.Delay(120, cancellation.Token);
            await _viewModel.SearchProductsAsync(ProductSearchBox.Text, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ProductSearchBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && ProductResultsList.Items.Count > 0)
        {
            e.Handled = true;
            ProductResultsList.SelectedIndex = 0;
            ProductResultsList.Focus();
            return;
        }

        if (e.Key == Key.Enter && ProductResultsList.Items.Count > 0)
        {
            e.Handled = true;
            ProductResultsList.SelectedIndex = Math.Max(0, ProductResultsList.SelectedIndex);
            AddSelectedProduct();
        }
    }

    private void ProductResultsList_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            AddSelectedProduct();
            return;
        }

        if (e.Key == Key.Up && ProductResultsList.SelectedIndex <= 0)
        {
            e.Handled = true;
            ProductSearchBox.Focus();
            ProductSearchBox.CaretIndex = ProductSearchBox.Text.Length;
        }
    }

    private void ProductResultsList_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(ProductResultsList, e.OriginalSource as DependencyObject) is not ListBoxItem item)
            return;

        ProductResultsList.SelectedItem = item.DataContext;
        e.Handled = true;
        AddSelectedProduct();
    }

    private void AddSelectedProduct()
    {
        if (ProductResultsList.SelectedItem is not ManualInboundProductResult item) return;
        _viewModel.AddProduct(item);
        ProductSearchBox.Focus();
    }

    private void FocusProductSearch_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SetEntryMode("direct");
        ProductSearchBox.Focus();
        ProductSearchBox.SelectAll();
    }

    private void RemoveRow_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ManualInboundDraftRowView row)
        {
            _viewModel.RemoveRow(row);
        }
    }

    private void AddFileRow_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.AddFileRow();

    private async void Preview_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviewAsync();

    private async void Confirm_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ConfirmAsync();

    private async void DownloadTemplate_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu tệp mẫu Nhập kho thủ công",
            Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            FileName = "mau-nhap-kho-thu-cong.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx"
        };

        if (dialog.ShowDialog() != true) return;
        var file = string.Equals(Path.GetExtension(dialog.FileName), ".csv", StringComparison.OrdinalIgnoreCase)
            ? OfficeDataExportFile.TemplateCsv("mau-nhap-kho-thu-cong.csv", "SKU", "Số lượng", "Giá vốn")
            : OfficeDataExportFile.TemplateXlsx("mau-nhap-kho-thu-cong.xlsx", "Nhập kho thủ công", "SKU", "Số lượng", "Giá vốn");
        await File.WriteAllBytesAsync(dialog.FileName, file.Content);
    }

    private void ChooseFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn tệp Nhập kho thủ công",
            Filter = "Excel hoặc CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;
        _viewModel.LoadFile(dialog.FileName);
    }

    private async void SearchHistory_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SearchHistoryAsync();

    private async void HistoryDetail_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ManualInboundHistoryRow row)
        {
            _historyPrintSource = row;
            await _viewModel.OpenHistoryDetailAsync(row);
        }
    }

    private void OpenReverse_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ManualInboundHistoryRow row)
        {
            _viewModel.OpenReverse(row);
        }
    }

    private void CloseReverse_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseReverse();

    private async void Reverse_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ReverseAsync();

    private async void PrintHistoryDetail_OnClick(object sender, RoutedEventArgs e)
    {
        if (_historyPrintSource is null || _viewModel.HistoryDetail is null) return;
        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "MANUAL_INBOUND");
        if (template is null) return;
        ActualDocumentPrintPreview.PrintManualInbound(Window.GetWindow(this), _historyPrintSource.Data, _viewModel.HistoryDetail, template);
    }

    private void CloseHistoryDetail_OnClick(object sender, RoutedEventArgs e)
    {
        _historyPrintSource = null;
        _viewModel.CloseHistoryDetail();
    }
}
