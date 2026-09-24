using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Workforce;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _workPolicyShellWired;

    private void WireWorkPolicyWorkspace()
    {
        if (_workPolicyShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeWorkforceNavigationShell();
        _viewModel.InitializeWorkPolicyShell();

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new WorkPolicyService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new WorkPolicyView(new WorkPolicyViewModel(
            service,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>()));

        while (workspaceTabs.Items.Count <= WorkspaceSlots.WorkPolicy)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.WorkPolicy] = new TabItem { Content = view };

        _workPolicyShellWired = true;
    }

    private async void WorkPolicies_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateWorkPoliciesAsync();
        ApplyWorkPolicyHeader();
    }

    private void ApplyWorkPolicyHeader()
    {
        if (!_viewModel.IsWorkPolicySelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Chính sách làm việc");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Thiết lập giờ làm, phương thức chấm công và lịch sử chính sách theo ngày hiệu lực.");
    }
}
