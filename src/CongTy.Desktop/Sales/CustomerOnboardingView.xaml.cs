using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Sales;

public partial class CustomerOnboardingView : UserControl
{
    public CustomerOnboardingView(CustomerOnboardingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private CustomerOnboardingViewModel ViewModel => (CustomerOnboardingViewModel)DataContext;

    private async void View_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void CustomerSelection_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { DataContext: CustomerOnboardingRow row })
            await ViewModel.LoadAddressesAsync(row);
    }

    private async void StartReview_OnClick(object sender, RoutedEventArgs e)
    {
        if (Row(sender) is { } row) await ViewModel.StartReviewAsync(row);
    }

    private async void NeedMoreInfo_OnClick(object sender, RoutedEventArgs e)
    {
        if (Row(sender) is { } row) await ViewModel.RequestMoreInfoAsync(row);
    }

    private async void Approve_OnClick(object sender, RoutedEventArgs e)
    {
        if (Row(sender) is { } row) await ViewModel.ApproveAsync(row);
    }

    private async void LinkExisting_OnClick(object sender, RoutedEventArgs e)
    {
        if (Row(sender) is { } row) await ViewModel.LinkExistingAsync(row);
    }

    private async void Reject_OnClick(object sender, RoutedEventArgs e)
    {
        if (Row(sender) is { } row) await ViewModel.RejectAsync(row);
    }

    private static CustomerOnboardingRow? Row(object sender) =>
        sender is FrameworkElement { DataContext: CustomerOnboardingRow row } ? row : null;
}
