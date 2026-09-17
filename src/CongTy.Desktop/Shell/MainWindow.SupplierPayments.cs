using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Accounting;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _supplierPaymentShellWired;

    private void WireSupplierPaymentsWorkspace()
    {
        if (_supplierPaymentShellWired) return;

        var sidebarButton = FindVisualChildren<Button>(this)
            .FirstOrDefault(button =>
                FindVisualChildren<TextBlock>(button)
                    .Any(text => text.Text == "Thanh toán nhà cung cấp"));
        if (sidebarButton is null) return;

        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeSupplierPaymentShell();

        sidebarButton.IsEnabled = true;
        BindingOperations.SetBinding(
            sidebarButton,
            Button.TagProperty,
            new Binding(nameof(ShellViewModel.IsSupplierPaymentsSelected)));
        BindingOperations.SetBinding(
            sidebarButton,
            UIElement.VisibilityProperty,
            new Binding(nameof(ShellViewModel.CanViewSupplierPayments))
            {
                Converter = (IValueConverter)FindResource("BooleanToVisibilityConverter")
            });
        sidebarButton.Click += SupplierPayments_OnClick;

        var app = (CongTy.Desktop.App)Application.Current;
        var idempotency = app.ResolveRequired<ICanonicalIdempotencyKeyProvider>();
        var service = new SupplierPaymentService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>(),
            idempotency);
        var viewModel = new SupplierPaymentViewModel(
            service,
            app.ResolveRequired<IAccessStateService>(),
            idempotency);
        var view = new SupplierPaymentView(viewModel);

        while (workspaceTabs.Items.Count <= 43)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[43] = new TabItem { Content = view };

        _supplierPaymentShellWired = true;
    }

    private async void SupplierPayments_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateSupplierPaymentsAsync();
        ApplySupplierPaymentsHeader();
    }

    private void ApplySupplierPaymentsHeader()
    {
        if (!_viewModel.IsSupplierPaymentsSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "KẾ TOÁN MUA HÀNG");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Thanh toán nhà cung cấp");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Ghi nhận thanh toán, phân bổ vào chứng từ phải trả và đảo nghiệp vụ bằng lịch sử bất biến.");
    }
}
