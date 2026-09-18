using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CongTy.Desktop.Printing;
using Microsoft.Win32;

namespace CongTy.Desktop.Inventory;

public partial class StocktakeView : UserControl
{
    private readonly StocktakeViewModel _viewModel;

    public StocktakeView(StocktakeViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public Task RefreshAsync() => _viewModel.RefreshAsync();

    public void OpenCreate() => _viewModel.OpenCreate();

    private async void StocktakeView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void StocktakeView_OnPreviewKeyDown(object sender, KeyEventArgs e)
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
            StocktakeSearchBox.Focus();
            StocktakeSearchBox.SelectAll();
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsCreateOpen)
        {
            e.Handled = true;
            _viewModel.CloseCreate();
        }
    }

    private async void StocktakeList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StocktakeList.SelectedItem is StocktakeListRow row)
        {
            await _viewModel.SelectStocktakeAsync(row.Data.Id);
        }
    }

    private void ScopeMode_OnChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string mode })
        {
            _viewModel.ScopeMode = mode;
        }
    }

    private void SelectAllScopeResults_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.SelectAllScopeResults();

    private void ClearScopeResults_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.ClearScopeResults();

    private void LineFilter_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string filter })
        {
            _viewModel.SetLineFilter(filter);
        }
    }

    private void PreviousLinePage_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.PreviousLinePage();

    private void NextLinePage_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.NextLinePage();

    private void CloseCreate_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseCreate();
    private async void Create_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CreateAsync();
    private void ResetFilters_OnClick(object sender, RoutedEventArgs e) => _viewModel.ResetFilters();
    private async void Count_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CompleteCountAsync();
    private async void SaveAnnotations_OnClick(object sender, RoutedEventArgs e) => await _viewModel.SaveAnnotationsAsync();
    private async void Copy_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CopyAsync();
    private async void Submit_OnClick(object sender, RoutedEventArgs e) => await _viewModel.SubmitAsync();
    private async void Recount_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RecountAsync();
    private async void Approve_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ApproveAsync();
    private async void Post_OnClick(object sender, RoutedEventArgs e) => await _viewModel.PostAsync();
    private async void Cancel_OnClick(object sender, RoutedEventArgs e) => await _viewModel.CancelAsync();
    private async void Reverse_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ReverseAsync();

    private void ExportCountFile_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanExportCountFile || _viewModel.SelectedStocktake is not { } stocktake) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx",
            FileName = $"kiem-ke-{SafeFileStem(stocktake.StocktakeNumber)}.xlsx"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            StocktakeFileCodec.ExportCountXlsx(dialog.FileName, stocktake, _viewModel.Lines.ToArray());
            _viewModel.ReportFileNotice("Đã xuất file đếm của phiếu đang mở. File không chứa tồn hệ thống.");
        }
        catch (Exception exception)
        {
            _viewModel.ReportFileError(exception);
        }
    }

    private void ImportCountFile_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanImportCountFile || _viewModel.SelectedStocktake is not { } stocktake) return;

        var dialog = new OpenFileDialog
        {
            Filter = "File kiểm kê (*.xlsx;*.csv)|*.xlsx;*.csv|Excel Workbook (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var imported = StocktakeFileCodec.ImportCountFile(dialog.FileName, stocktake, _viewModel.Lines.ToArray());
            _viewModel.SetLineFilter("all");
            _viewModel.RefreshLinePaging();
            var remaining = _viewModel.Lines.Count(line => string.IsNullOrWhiteSpace(line.CountedQuantity));
            _viewModel.ReportFileNotice(
                $"Đã nhập {imported} dòng vào phiếu {stocktake.StocktakeNumber}. Còn {remaining} dòng chưa kiểm; vẫn có thể sửa tay.");
        }
        catch (Exception exception)
        {
            _viewModel.ReportFileError(exception);
        }
    }

    private void ExportResultExcel_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanExportResults || _viewModel.SelectedStocktake is not { } stocktake) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx",
            FileName = $"kiem-ke-{SafeFileStem(stocktake.StocktakeNumber)}-ket-qua.xlsx"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            StocktakeFileCodec.ExportResultXlsx(dialog.FileName, stocktake, _viewModel.Lines.ToArray());
            _viewModel.ReportFileNotice("Đã xuất Excel kết quả của phiếu kiểm kê đang mở.");
        }
        catch (Exception exception)
        {
            _viewModel.ReportFileError(exception);
        }
    }

    private void ExportResultCsv_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanExportResults || _viewModel.SelectedStocktake is not { } stocktake) return;

        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            AddExtension = true,
            DefaultExt = ".csv",
            FileName = $"kiem-ke-{SafeFileStem(stocktake.StocktakeNumber)}-ket-qua.csv"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            StocktakeFileCodec.ExportResultCsv(dialog.FileName, stocktake, _viewModel.Lines.ToArray());
            _viewModel.ReportFileNotice("Đã xuất CSV kết quả của phiếu kiểm kê đang mở.");
        }
        catch (Exception exception)
        {
            _viewModel.ReportFileError(exception);
        }
    }

    private async void Print_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanPrint || _viewModel.SelectedStocktake is not { } stocktake) return;

        var template = await DocumentPrintTemplateRuntime.LoadForPrintAsync(Window.GetWindow(this), "STOCKTAKE");
        if (template is null) return;
        StocktakePrintPreview.Print(stocktake, _viewModel.Lines.ToArray(), template);
    }

    private static string SafeFileStem(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray();
        return new string(chars);
    }
}
