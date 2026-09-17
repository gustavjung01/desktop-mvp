using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Accounting;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _customerPaymentShellWired;

    private void WireCustomerPaymentsWorkspace()
    {
        if (_customerPaymentShellWired) return;

        var sidebarButton = FindVisualChildren<Button>(this)
            .FirstOrDefault(button =>
                FindVisualChildren<TextBlock>(button)
                    .Any(text => text.Text == "Thu tiền khách hàng"));
        if (sidebarButton is null) return;

        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeCustomerPaymentShell();

        sidebarButton.IsEnabled = true;
        BindingOperations.SetBinding(
            sidebarButton,
            Button.TagProperty,
            new Binding(nameof(ShellViewModel.IsCustomerPaymentsSelected)));
        BindingOperations.SetBinding(
            sidebarButton,
            UIElement.VisibilityProperty,
            new Binding(nameof(ShellViewModel.CanViewCustomerPayments))
            {
                Converter = (IValueConverter)FindResource("BooleanToVisibilityConverter")
            });
        sidebarButton.Click += CustomerPayments_OnClick;

        var app = (CongTy.Desktop.App)Application.Current;
        var idempotency = app.ResolveRequired<ICanonicalIdempotencyKeyProvider>();
        var service = new CustomerPaymentService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>(),
            idempotency);
        var viewModel = new CustomerPaymentViewModel(
            service,
            app.ResolveRequired<IAccessStateService>(),
            idempotency);
        var view = new CustomerPaymentView(viewModel);

        while (workspaceTabs.Items.Count <= 40)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[40] = new TabItem { Content = view };

        _customerPaymentShellWired = true;
    }

    private async void CustomerPayments_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateCustomerPaymentsAsync();
        ApplyCustomerPaymentsHeader();
    }

    private void ApplyCustomerPaymentsHeader()
    {
        if (!_viewModel.IsCustomerPaymentsSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "KẾ TOÁN BÁN HÀNG");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Thu tiền khách hàng");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Lập phiếu thu, ghi tiền vào đúng đơn hàng và theo dõi số khách còn phải trả.");
    }
}
