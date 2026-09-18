using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using Microsoft.Win32;

namespace CongTy.Desktop.Sales;

public partial class SalesReportingView : UserControl
{
    private readonly SalesReportingViewModel _viewModel;

    public SalesReportingView(SalesReportingViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        BindDynamicColumnHeaders(viewModel);
    }

    private void BindDynamicColumnHeaders(SalesReportingViewModel viewModel)
    {
        BindHeader(AnalysisGrid.Columns[1], viewModel, nameof(SalesReportingViewModel.SelectedDimensionLabel));
        BindHeader(AnalysisGrid.Columns[3], viewModel, nameof(SalesReportingViewModel.MetricHeader));
        BindHeader(TotalGrid.Columns[0], viewModel, nameof(SalesReportingViewModel.SelectedDimensionLabel));
        BindHeader(TotalGrid.Columns[2], viewModel, nameof(SalesReportingViewModel.MetricHeader));
    }

    private static void BindHeader(DataGridColumn column, SalesReportingViewModel viewModel, string propertyName) =>
        BindingOperations.SetBinding(
            column,
            DataGridColumn.HeaderProperty,
            new Binding(propertyName)
            {
                Source = viewModel,
                Mode = BindingMode.OneWay
            });

    public void OpenExportDialog()
    {
        _viewModel.OpenExport();
        ExportListRadio.IsChecked = true;
        ExportXlsxRadio.IsChecked = true;
    }

    private async void Apply_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync().ConfigureAwait(true);

    private async void Reset_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ResetAsync().ConfigureAwait(true);

    private async void SaveView_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveViewAsync().ConfigureAwait(true);

    private async void PeriodPreset_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.SelectedValue is not string key || string.IsNullOrWhiteSpace(key)) return;
        await _viewModel.ApplyPresetAsync(key).ConfigureAwait(true);
        combo.SelectedIndex = 0;
    }

    private void AnalysisDetail_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SalesAnalysisRow row }) _viewModel.SelectAnalysisRow(row);
    }

    private void CloseDetail_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseAnalysisDetail();

    private void CloseExport_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseExport();

    private void ExportListMode_OnChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SalesReportingViewModel) _viewModel.ExportMode = "list";
    }

    private void ExportAnalysisMode_OnChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SalesReportingViewModel) _viewModel.ExportMode = "analysis";
    }

    private void SelectAllAnalysisColumns_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SelectAllAnalysisExportColumns();

    private void ClearAnalysisColumns_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.ClearAnalysisExportColumns();

    private void ExportXlsx_OnChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SalesReportingViewModel) _viewModel.ExportFormat = "xlsx";
    }

    private void ExportCsv_OnChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SalesReportingViewModel) _viewModel.ExportFormat = "csv";
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

    private async void SalesReportingView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await _viewModel.RefreshAsync().ConfigureAwait(true);
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            e.Handled = true;
            AnalysisSearchBox.Focus();
            AnalysisSearchBox.SelectAll();
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (_viewModel.IsExportOpen)
            {
                e.Handled = true;
                _viewModel.CloseExport();
            }
            else if (_viewModel.HasSelectedAnalysisRow)
            {
                e.Handled = true;
                _viewModel.CloseAnalysisDetail();
            }
        }
    }
}
