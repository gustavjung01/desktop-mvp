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
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeCustomerReturnCreditShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var idempotency = app.ResolveRequired<ICanonicalIdempotencyKeyProvider>();
        var service = new CustomerReturnCreditService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>(),
            idempotency);
        var viewModel = new CustomerReturnCreditViewModel(
            service,
            app.ResolveRequired<IAccessStateService>(),
            idempotency);
        var view = new CustomerReturnCreditView(viewModel);

        while (workspaceTabs.Items.Count <= 41)
            workspaceTabs.Items.Add(new TabItem());
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
        SetShellHeaderText(nameof(ShellViewModel.PageSubtitle),
            "Khoản giảm công nợ chỉ phát sinh khi kho đã nhận phiếu hàng khách trả; phần chưa sử dụng có thể phân bổ hoặc hoàn tiền bằng chứng từ riêng.");
    }
}
