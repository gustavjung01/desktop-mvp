using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace CongTy.Desktop.Sales;

public partial class GrossMarginReportingView : UserControl
{
    private readonly GrossMarginReportingViewModel _viewModel;

    public GrossMarginReportingView(GrossMarginReportingViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public event EventHandler? CostingRequested;

    public void OpenExportDialog()
    {
        _viewModel.OpenExport();
        ExportXlsxRadio.IsChecked = true;
    }

    private async void Apply_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync().ConfigureAwait(true);

    private async void Reset_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ResetAsync().ConfigureAwait(true);

    private void Costing_OnClick(object sender, RoutedEventArgs e) =>
        CostingRequested?.Invoke(this, EventArgs.Empty);

    private void CloseExport_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseExport();

    private void ExportXlsx_OnChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is GrossMarginReportingViewModel) _viewModel.ExportFormat = "xlsx";
    }

    private void ExportCsv_OnChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is GrossMarginReportingViewModel) _viewModel.ExportFormat = "csv";
    }

    private void SelectAllColumns_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SelectAllExportColumns();

    private void ClearColumns_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.ClearExportColumns();

    private void DefaultColumns_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.ResetExportColumns();

    private async void Export_OnClick(object sender, RoutedEventArgs e)
    {
        var file = await _viewModel.ExportAsync().ConfigureAwait(true);
        if (file is null) return;

        var extension = Path.GetExtension(file.FileName);
        var dialog = new SaveFileDialog
        {
            FileName = file.FileName,
            DefaultExt = string.IsNullOrWhiteSpace(extension) ? $".{_viewModel.ExportFormat}" : extension,
            Filter = _viewModel.ExportFormat == "csv"
                ? "CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*"
                : "Excel (*.xlsx)|*.xlsx|Tất cả tệp (*.*)|*.*",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            await File.WriteAllBytesAsync(dialog.FileName, file.Content).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Không lưu được file báo cáo.\n\n{exception.Message}",
                "Không lưu được báo cáo",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async void GrossMarginReportingView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshAsync().ConfigureAwait(true);
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsExportOpen)
        {
            e.Handled = true;
            _viewModel.CloseExport();
        }
    }
}
