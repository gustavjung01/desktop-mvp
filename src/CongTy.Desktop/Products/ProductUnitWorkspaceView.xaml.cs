using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CongTy.Desktop.Products;

public partial class ProductUnitWorkspaceView : UserControl
{
    public ProductUnitWorkspaceView() => InitializeComponent();

    private void PageScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) =>
        ProductScrollWheel.Route(PageScrollViewer, e);

    private ProductViewModel ViewModel =>
        DataContext as ProductViewModel
        ?? throw new InvalidOperationException("Màn Đơn vị và quy đổi chưa có dữ liệu làm việc.");

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await ViewModel.RefreshAsync();
    private void AddUnit_OnClick(object sender, RoutedEventArgs e) => ViewModel.OpenUnitCreate();
    private void CancelUnit_OnClick(object sender, RoutedEventArgs e) => ViewModel.CancelEditor();
    private async void SaveUnit_OnClick(object sender, RoutedEventArgs e) => await ViewModel.SaveEditorAsync();
    private async void SaveSetup_OnClick(object sender, RoutedEventArgs e) => await ViewModel.SaveUnitSetupAsync();
    private async void AddBarcode_OnClick(object sender, RoutedEventArgs e) => await ViewModel.AddUnitBarcodeAsync();
    private async void Normalize_OnClick(object sender, RoutedEventArgs e) => await ViewModel.NormalizeUnitQuantityAsync();

    private void EditUnit_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) ViewModel.OpenUnitEdit(id);
    }

    private async void ToggleUnit_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) await ViewModel.ToggleUnitAsync(id);
    }

    private async void Product_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductCombo.SelectedValue is string id && !string.IsNullOrWhiteSpace(id) && id != ViewModel.UnitProductId)
            await ViewModel.SelectUnitProductAsync(id);
    }

    private async void Variant_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VariantCombo.SelectedValue is string id && !string.IsNullOrWhiteSpace(id) && id != ViewModel.UnitVariantId)
            await ViewModel.SelectUnitVariantAsync(id);
    }

    private async void ToggleBarcode_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) await ViewModel.ToggleUnitBarcodeAsync(id);
    }

    private static bool TryId(object sender, out string id)
    {
        id = sender is FrameworkElement { Tag: string value } ? value : string.Empty;
        return !string.IsNullOrWhiteSpace(id);
    }
}
