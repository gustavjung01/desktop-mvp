using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Inventory;

public partial class OpeningBalanceView : UserControl
{
    private readonly OpeningBalanceViewModel _viewModel;

    public OpeningBalanceView(OpeningBalanceViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public event Action? BackToLookupRequested;

    private async void OpeningBalanceView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void OpeningBalanceView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshAsync();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            e.Handled = true;
            await ChooseFileAsync();
        }
    }

    private void BackToLookup_OnClick(object sender, RoutedEventArgs e) =>
        BackToLookupRequested?.Invoke();

    private async void DownloadTemplate_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu tệp mẫu tồn đầu kỳ",
            Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            DefaultExt = ".xlsx",
            AddExtension = true,
            FileName = "mau-ton-dau-ky.xlsx"
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        var file = string.Equals(Path.GetExtension(dialog.FileName), ".csv", StringComparison.OrdinalIgnoreCase)
            ? OfficeDataExportFile.TemplateCsv("mau-ton-dau-ky.csv", "SKU", "Số lượng", "Vị trí")
            : OfficeDataExportFile.TemplateXlsx("mau-ton-dau-ky.xlsx", "Tồn đầu kỳ", "SKU", "Số lượng", "Vị trí");
        await File.WriteAllBytesAsync(dialog.FileName, file.Content);
    }

    private async void ChooseFile_OnClick(object sender, RoutedEventArgs e) =>
        await ChooseFileAsync();

    private async Task ChooseFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn tệp tồn đầu kỳ",
            Filter = "Excel hoặc CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            Multiselect = false,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        await _viewModel.LoadCsvFileAsync(dialog.FileName);
    }

    private async void Validate_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ValidateAsync();

    private async void Post_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PostAsync();

    private void PreviewPrevious_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.PreviewPrevious();

    private void PreviewNext_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.PreviewNext();
}
