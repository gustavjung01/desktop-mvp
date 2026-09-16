using System.Windows.Controls;

namespace CongTy.Desktop.Products;

public partial class ProductView : UserControl
{
    private readonly ProductViewModel _viewModel;

    public ProductView(ProductViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += ProductView_OnLoaded;
    }

    public Task RefreshAsync() => _viewModel.RefreshAsync();

    private async void ProductView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= ProductView_OnLoaded;
        await _viewModel.EnsureLoadedAsync();
    }
}
