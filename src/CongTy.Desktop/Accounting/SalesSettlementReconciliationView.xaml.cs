using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace CongTy.Desktop.Accounting;

public partial class SalesSettlementReconciliationView : UserControl
{
    private readonly SalesSettlementReconciliationViewModel _viewModel;

    public SalesSettlementReconciliationView(SalesSettlementReconciliationViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public event Action<string>? NavigationRequested;

    private async void SalesSettlementReconciliationView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void SalesSettlementReconciliationView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5 || !_viewModel.CanApply) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void Apply_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplyAsync();

    private async void Reset_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ResetAsync();

    private void ExportCsv_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanExport) return;

        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = _viewModel.ExportFileName,
            DefaultExt = ".csv",
            AddExtension = true
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            File.WriteAllText(
                dialog.FileName,
                _viewModel.BuildCsv(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            _viewModel.NotifyExportSaved(Path.GetFileName(dialog.FileName));
        }
        catch (Exception exception)
        {
            _viewModel.NotifyExportError(exception.Message);
        }
    }

    private void Receivables_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("receivables");

    private void CodAccounting_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("cod");

    private void SalesOrders_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("sales-orders");

    private void TripReconciliation_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("trip-reconciliation");

    private void AnomalySource_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SalesSettlementAnomalyRow row })
            NavigationRequested?.Invoke(row.Destination);
    }
}
