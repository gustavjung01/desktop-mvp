using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace CongTy.Desktop.Pricing;

public partial class PricingView : UserControl
{
    private readonly PricingViewModel _viewModel;
    private bool _loaded;

    public PricingView(PricingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.OverviewColumnsChanged += (_, _) => Dispatcher.Invoke(RebuildOverviewColumns);
    }

    public Task RefreshAsync() => _viewModel.RefreshAsync();

    private async void PricingView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await _viewModel.EnsureLoadedAsync();
    }

    private async void PricingTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || e.Source != PricingTabs) return;
        await _viewModel.ActivateTabAsync(PricingTabs.SelectedIndex);
        if (PricingTabs.SelectedIndex == 3) RebuildOverviewColumns();
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await _viewModel.RefreshAsync();
    private void ChannelCreate_OnClick(object sender, RoutedEventArgs e) => _viewModel.OpenChannelCreate();
    private void ChannelEdit_OnClick(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is PricingChannelRow row) _viewModel.OpenChannelEdit(row); }
    private async void ChannelToggle_OnClick(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is PricingChannelRow row) await _viewModel.ToggleChannelAsync(row); }
    private void ListCreate_OnClick(object sender, RoutedEventArgs e) => _viewModel.OpenListCreate();
    private void ListEdit_OnClick(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is PricingListRow row) _viewModel.OpenListEdit(row); }
    private async void ListToggle_OnClick(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is PricingListRow row) await _viewModel.ToggleListAsync(row); }
    private async void ListItems_OnClick(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is PricingListRow row) { await _viewModel.OpenListItemsAsync(row); PricingTabs.SelectedIndex = 2; } }
    private void ItemCreate_OnClick(object sender, RoutedEventArgs e) => _viewModel.OpenItemCreate();
    private async void ItemEdit_OnClick(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is PricingItemRow row) await _viewModel.OpenItemEditAsync(row); }
    private async void ItemToggle_OnClick(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is PricingItemRow row) await _viewModel.ToggleItemAsync(row); }

    private async void PriceListSelection_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || sender is not ComboBox combo) return;
        await _viewModel.SelectPriceListAsync(combo.SelectedValue as string);
    }

    private async void ItemProduct_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedValue is string id) await _viewModel.LoadItemVariantsAsync(id);
    }

    private async void ResolverProduct_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedValue is string id) await _viewModel.LoadResolverVariantsAsync(id);
    }

    private async void Resolve_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ResolveAsync();
    private void EditorCancel_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseEditor();
    private async void EditorSave_OnClick(object sender, RoutedEventArgs e) => await _viewModel.SaveEditorAsync();
    private async void OpenAdjustment_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.OpenDirectAdjustmentAsync();

    private void DownloadAdjustmentTemplate_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = "mau-dieu-chinh-gia.xlsx",
            Filter = "Tệp Excel (*.xlsx)|*.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog() == true) _viewModel.ExportAdjustmentTemplate(dialog.FileName);
    }

    private async void ImportAdjustmentFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn file điều chỉnh giá",
            Filter = "Tệp Excel hoặc CSV (*.xlsx;*.csv)|*.xlsx;*.csv",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog() == true) await _viewModel.OpenFileAdjustmentAsync(dialog.FileName);
    }

    private void AdjustmentSelectVisible_OnClick(object sender, RoutedEventArgs e) => _viewModel.SelectVisibleAdjustmentRows();
    private void AdjustmentClearSelection_OnClick(object sender, RoutedEventArgs e) => _viewModel.ClearAdjustmentSelection();
    private void AdjustmentCancel_OnClick(object sender, RoutedEventArgs e) => _viewModel.CloseAdjustment();
    private async void AdjustmentConfirm_OnClick(object sender, RoutedEventArgs e) => await _viewModel.ApplyAdjustmentAsync();

    private void RebuildOverviewColumns()
    {
        while (OverviewGrid.Columns.Count > 6) OverviewGrid.Columns.RemoveAt(OverviewGrid.Columns.Count - 1);
        foreach (var list in _viewModel.VisibleOverviewPriceLists)
        {
            OverviewGrid.Columns.Add(new DataGridTextColumn
            {
                Header = $"{list.Code}\n{list.Name}{(list.IsActive ? string.Empty : " · Ngừng")}",
                Binding = new Binding($"PriceCells[{list.Id}]"),
                Width = new DataGridLength(145)
            });
        }
    }

    private async void ExportSelected_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.VisibleOverviewPriceLists.Count != 1)
        {
            MessageBox.Show("Chọn một bảng giá cụ thể để xuất.", "Bảng giá tổng hợp", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = _viewModel.ExportDefaultFileName(false),
            Filter = "Tệp Excel (*.xlsx)|*.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog() == true) await _viewModel.ExportWorkbookAsync(dialog.FileName, false);
    }

    private async void ExportAll_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = _viewModel.ExportDefaultFileName(true),
            Filter = "Tệp Excel (*.xlsx)|*.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog() == true) await _viewModel.ExportWorkbookAsync(dialog.FileName, true);
    }
}
