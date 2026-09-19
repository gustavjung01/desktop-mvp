using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Accounting;

public partial class AgingReportingView : UserControl
{
    private readonly AgingReportingViewModel _viewModel;

    public event Action? ReceivablesRequested;
    public event Action? PayablesRequested;

    public AgingReportingView(AgingReportingViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void AgingReportingView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void AgingReportingView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }

    private async void Apply_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplyAsync();

    private async void Reset_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ResetAsync();

    private void Receivables_OnClick(object sender, RoutedEventArgs e) =>
        ReceivablesRequested?.Invoke();

    private void Payables_OnClick(object sender, RoutedEventArgs e) =>
        PayablesRequested?.Invoke();
}
