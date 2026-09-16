using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CongTy.Desktop.Products;

public partial class ProductQuickSetupView : UserControl
{
    private bool _suppressProductSearchPopup;

    public ProductQuickSetupView() => InitializeComponent();

    private ProductViewModel ViewModel =>
        DataContext as ProductViewModel
        ?? throw new InvalidOperationException("Màn Thiết lập nhanh chưa có dữ liệu làm việc.");

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await ViewModel.RefreshAsync();
    private void NewProduct_OnClick(object sender, RoutedEventArgs e) => ViewModel.StartQuickProductCreate();
    private async void SaveProduct_OnClick(object sender, RoutedEventArgs e) => await ViewModel.SaveQuickProductAsync();
    private void NewVariant_OnClick(object sender, RoutedEventArgs e) => ViewModel.StartQuickVariantCreate();
    private async void SaveVariant_OnClick(object sender, RoutedEventArgs e) => await ViewModel.SaveQuickVariantAsync();
    private async void SaveUnit_OnClick(object sender, RoutedEventArgs e) => await ViewModel.SaveQuickUnitAsync();
    private async void AddBarcode_OnClick(object sender, RoutedEventArgs e) => await ViewModel.AddQuickBarcodeAsync();
    private async void SavePrice_OnClick(object sender, RoutedEventArgs e) => await ViewModel.SaveQuickPriceAsync();

    private void ProductSearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressProductSearchPopup) return;
        Dispatcher.BeginInvoke(UpdateProductSearchPopup, DispatcherPriority.Background);
    }

    private void UpdateProductSearchPopup()
    {
        if (_suppressProductSearchPopup) return;
        ProductSearchPopup.Width = Math.Max(280, ProductSearchBox.ActualWidth);
        ProductSearchPopup.IsOpen =
            !string.IsNullOrWhiteSpace(ProductSearchBox.Text)
            && ViewModel.ProductOptions.Count > 0;
    }

    private async void ProductSearchBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && ViewModel.ProductOptions.Count > 0)
        {
            e.Handled = true;
            UpdateProductSearchPopup();
            ProductSearchResults.SelectedIndex = 0;
            ProductSearchResults.Focus();
            return;
        }

        if (e.Key == Key.Enter && ViewModel.ProductOptions.Count > 0)
        {
            e.Handled = true;
            ProductSearchResults.SelectedIndex = Math.Max(0, ProductSearchResults.SelectedIndex);
            await SelectProductSearchResultAsync();
        }
    }

    private async void ProductSearchResults_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SelectProductSearchResultAsync();
            return;
        }

        if (e.Key == Key.Up && ProductSearchResults.SelectedIndex <= 0)
        {
            e.Handled = true;
            ProductSearchBox.Focus();
            ProductSearchBox.CaretIndex = ProductSearchBox.Text.Length;
        }
    }

    private async void ProductSearchResults_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(ProductSearchResults, e.OriginalSource as DependencyObject) is not ListBoxItem item)
            return;

        ProductSearchResults.SelectedItem = item.DataContext;
        e.Handled = true;
        await SelectProductSearchResultAsync();
    }

    private async Task SelectProductSearchResultAsync()
    {
        if (ProductSearchResults.SelectedItem is not ProductLookupOption option) return;

        _suppressProductSearchPopup = true;
        try
        {
            await ViewModel.SelectQuickProductAsync(option.Id);
            ViewModel.QuickSearch = option.Label;
            ProductSearchPopup.IsOpen = false;
            ProductSearchBox.CaretIndex = ProductSearchBox.Text.Length;
            ProductSearchBox.Focus();
        }
        finally
        {
            _suppressProductSearchPopup = false;
        }
    }

    private void PageScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) =>
        ProductScrollWheel.Route(PageScrollViewer, e);

    private async void Variant_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VariantCombo.SelectedValue is string id && !string.IsNullOrWhiteSpace(id) && id != ViewModel.QuickVariantId)
            await ViewModel.SelectQuickVariantAsync(id);
    }

    private async void PriceList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PriceListCombo.SelectedValue is string id && !string.IsNullOrWhiteSpace(id) && id != ViewModel.QuickPriceListId)
            await ViewModel.SelectQuickPriceListAsync(id);
    }

    private async void ToggleBarcode_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string id } && !string.IsNullOrWhiteSpace(id))
            await ViewModel.ToggleQuickBarcodeAsync(id);
    }

    private async void UploadImage_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh sản phẩm",
            Filter = "Ảnh JPG, PNG hoặc WebP (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
            await ViewModel.UploadQuickImageAsync(dialog.FileName);
    }

    private async void DeleteImage_OnClick(object sender, RoutedEventArgs e) => await ViewModel.DeleteQuickImageAsync();
}
