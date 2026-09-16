using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Products;

public partial class ProductBrandView : UserControl
{
    public ProductBrandView() => InitializeComponent();

    private ProductViewModel ViewModel =>
        DataContext as ProductViewModel
        ?? throw new InvalidOperationException("Màn Nhãn hàng chưa có dữ liệu làm việc.");

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) => await ViewModel.RefreshAsync();
    private void Add_OnClick(object sender, RoutedEventArgs e) => ViewModel.OpenBrandCreate();
    private void Cancel_OnClick(object sender, RoutedEventArgs e) => ViewModel.CancelEditor();
    private async void Save_OnClick(object sender, RoutedEventArgs e) => await ViewModel.SaveEditorAsync();

    private void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) ViewModel.OpenBrandEdit(id);
    }

    private async void Toggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryId(sender, out var id)) await ViewModel.ToggleBrandAsync(id);
    }

    private static bool TryId(object sender, out string id)
    {
        id = sender is FrameworkElement { Tag: string value } ? value : string.Empty;
        return !string.IsNullOrWhiteSpace(id);
    }
}
