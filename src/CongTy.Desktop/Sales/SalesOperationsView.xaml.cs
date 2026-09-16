using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Sales;

public partial class SalesOperationsView : UserControl
{
    private readonly SalesOperationsViewModel _viewModel;

    public SalesOperationsView(SalesOperationsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public event EventHandler? SalesOrdersRequested;

    private void OpenSalesOrders_OnClick(object sender, RoutedEventArgs e) =>
        SalesOrdersRequested?.Invoke(this, EventArgs.Empty);

    private async void SalesOperationsView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync().ConfigureAwait(true);
    }
}
