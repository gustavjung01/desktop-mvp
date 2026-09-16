using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Products;

public partial class ProductCatalogView : UserControl
{
    public ProductCatalogView() => InitializeComponent();

    private ProductViewModel ViewModel =>
        DataContext as ProductViewModel
        ?? throw new InvalidOperationException("Màn Sản phẩm chưa có dữ liệu làm việc.");

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await ViewModel.RefreshAsync();
    private void ResetFilters_OnClick(object sender, RoutedEventArgs e) => ViewModel.ResetFilters();
    private void AddProduct_OnClick(object sender, RoutedEventArgs e) => ViewModel.OpenProductCreate();

    private async void EditProduct_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) await ViewModel.OpenProductEditAsync(id);
    }

    private async void ManageVariants_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) await ViewModel.OpenVariantManagerAsync(id);
    }

    private async void ToggleProduct_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) await ViewModel.ToggleProductAsync(id);
    }

    private void AddVariant_OnClick(object sender, RoutedEventArgs e) => ViewModel.OpenVariantCreate();

    private void EditVariant_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) ViewModel.OpenVariantEdit(id);
    }

    private async void VariantUnits_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryId(sender, out var variantId) || ViewModel.VariantProduct is not { } product) return;
        ViewModel.CloseVariantManager();
        ViewModel.TabIndex = 5;
        ViewModel.UnitWorkspaceTabIndex = 1;
        await ViewModel.SelectUnitProductAsync(product.Id);
        await ViewModel.SelectUnitVariantAsync(variantId);
    }

    private void CloseVariants_OnClick(object sender, RoutedEventArgs e) => ViewModel.CloseVariantManager();
    private void CancelEditor_OnClick(object sender, RoutedEventArgs e) => ViewModel.CancelEditor();
    private async void SaveEditor_OnClick(object sender, RoutedEventArgs e) => await ViewModel.SaveEditorAsync();

    private static bool TryId(object sender, out string id)
    {
        id = sender is FrameworkElement { Tag: string value } ? value : string.Empty;
        return !string.IsNullOrWhiteSpace(id);
    }
}
