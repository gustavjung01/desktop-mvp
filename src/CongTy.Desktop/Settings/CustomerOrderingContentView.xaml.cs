using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Products;
using Microsoft.Win32;

namespace CongTy.Desktop.Settings;

public partial class CustomerOrderingContentView : UserControl
{
    private readonly CustomerOrderingContentViewModel _viewModel;

    public CustomerOrderingContentView(CustomerOrderingContentViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void CustomerOrderingContentView_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private async void Save_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveAsync();

    private async void ChooseBanner_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh banner",
            Filter = "Ảnh hỗ trợ (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var bytes = await ProductImageProcessor.ToWebpAsync(dialog.FileName);
            await _viewModel.UploadBannerAsync(bytes);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Nội dung đặt hàng",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
