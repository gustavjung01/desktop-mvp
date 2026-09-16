using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Accounting;

public partial class CodAccountingView : UserControl
{
    private readonly CodAccountingViewModel _viewModel;

    public CodAccountingView(CodAccountingViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void CodAccountingView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void CodAccountingView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
        if (_viewModel.ActiveTabIndex == 3)
            await _viewModel.RefreshReconciliationAsync();
    }

    private async void Apply_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplyAsync();

    private async void RefreshReconciliation_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshReconciliationAsync();

    private async void OpenPending_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: CodPendingHandoverRow row })
            await _viewModel.OpenPendingHandoverAsync(row);
    }

    private async void OpenException_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: CodExceptionRow row })
            await _viewModel.OpenExceptionAsync(row);
    }

    private async void Accept_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.AcceptAsync();

    private async void ReverseAcceptance_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ReverseAcceptanceAsync();

    private async void ReverseHandover_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ReverseHandoverAsync();

    private async void ReverseCollection_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: CodHandoverLineRow line })
            await _viewModel.ReverseCollectionAsync(line);
    }
}
