using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Accounting;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _customerReturnCreditShellWired;

    private void WireCustomerReturnCreditsWorkspace()
    {
        if (_customerReturnCreditShellWired) return;

        var sidebarButton = FindVisualChildren<Button>(this)
            .FirstOrDefault(button => FindVisualChildren<TextBlock>(button)
                .Any(text => text.Text == "Điều chỉnh công nợ hàng trả"));
        if (sidebarButton is null) return;

        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeCustomerReturnCreditShell();
        sidebarButton.IsEnabled = true;
        BindingOperations.SetBinding(sidebarButton, Button.TagProperty, new Binding(nameof(ShellViewModel.IsCustomerReturnCreditsSelected)));
        BindingOperations.SetBinding(sidebarButton, UIElement.VisibilityProperty, new Binding(nameof(ShellViewModel.CanViewCustomerReturnCredits))
        {
            Converter = (IValueConverter)FindResource("BooleanToVisibilityConverter")
        });
        sidebarButton.Click += CustomerReturnCredits_OnClick;

        var app = (CongTy.Desktop.App)Application.Current;
        var idempotency = app.ResolveRequired<ICanonicalIdempotencyKeyProvider>();
        var service = new CustomerReturnCreditService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>(),
            idempotency);
        var viewModel = new CustomerReturnCreditViewModel(service, app.ResolveRequired<IAccessStateService>(), idempotency);
        var view = new CustomerReturnCreditView(viewModel);

        while (workspaceTabs.Items.Count <= 41) workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[41] = new TabItem { Content = view };
        _customerReturnCreditShellWired = true;
    }

    private async void CustomerReturnCredits_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateCustomerReturnCreditsAsync();
        ApplyCustomerReturnCreditsHeader();
    }

    private void ApplyCustomerReturnCreditsHeader()
    {
        if (!_viewModel.IsCustomerReturnCreditsSelected) return;
        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "KẾ TOÁN BÁN HÀNG");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Điều chỉnh công nợ hàng trả");
        SetShellHeaderText(nameof(ShellViewModel.PageSubtitle), "Phân bổ khoản giảm công nợ từ hàng khách trả, hoàn phần chưa sử dụng và theo dõi lịch sử điều chỉnh.");
    }
}
