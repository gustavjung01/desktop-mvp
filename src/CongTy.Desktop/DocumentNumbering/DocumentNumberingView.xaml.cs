using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.DocumentNumbering;

public partial class DocumentNumberingView : UserControl
{
    private readonly DocumentNumberingViewModel _viewModel;
    private bool _loaded;

    public DocumentNumberingView(DocumentNumberingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public Task RefreshAsync() => _viewModel.RefreshAsync();

    private async void DocumentNumberingView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await _viewModel.EnsureLoadedAsync();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshAsync();
    private void Create_OnClick(object sender, RoutedEventArgs e) => _viewModel.OpenCreate();

    private async void Detail_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not NumberSeriesRow row) return;
        SeriesGrid.SelectedItem = row;
        await _viewModel.SelectSeriesAsync(row);
    }

    private void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is NumberSeriesRow row) _viewModel.OpenEdit(row);
    }

    private async void Toggle_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is NumberSeriesRow row) await _viewModel.ToggleActiveAsync(row);
    }

    private async void Allocate_OnClick(object sender, RoutedEventArgs e) => await _viewModel.AllocateReferenceAsync();
    private async void RefreshHistory_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshHistoryAsync();
    private void EditorCancel_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseEditor();
    private async void EditorSave_OnClick(object sender, RoutedEventArgs e) => await _viewModel.SaveEditorAsync();
}
