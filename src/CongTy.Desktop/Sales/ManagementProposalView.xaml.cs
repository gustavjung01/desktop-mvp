using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Sales;

public partial class ManagementProposalView : UserControl
{
    private readonly ManagementProposalViewModel _viewModel;

    public ManagementProposalView(ManagementProposalViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void Submit_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SubmitAsync();

    private async void Resubmit_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ManagementProposalRow row })
            await _viewModel.ResubmitAsync(row);
    }

    private async void ManagementProposalView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await _viewModel.RefreshAsync();
    }
}
