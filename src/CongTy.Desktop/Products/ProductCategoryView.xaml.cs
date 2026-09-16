using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Products;

public partial class ProductCategoryView : UserControl
{
    public ProductCategoryView() => InitializeComponent();

    private ProductViewModel ViewModel =>
        DataContext as ProductViewModel
        ?? throw new InvalidOperationException("Màn Loại sản phẩm chưa có dữ liệu làm việc.");

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await ViewModel.RefreshAsync();
    private void Add_OnClick(object sender, RoutedEventArgs e) => ViewModel.OpenCategoryCreate();
    private void Cancel_OnClick(object sender, RoutedEventArgs e) => ViewModel.CancelEditor();
    private async void Save_OnClick(object sender, RoutedEventArgs e) => await ViewModel.SaveEditorAsync();

    private void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) ViewModel.OpenCategoryEdit(id);
    }

    private async void Toggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) await ViewModel.ToggleCategoryAsync(id);
    }

    private static bool TryId(object sender, out string id)
    {
        id = sender is FrameworkElement { Tag: string value } ? value : string.Empty;
        return !string.IsNullOrWhiteSpace(id);
    }
}
