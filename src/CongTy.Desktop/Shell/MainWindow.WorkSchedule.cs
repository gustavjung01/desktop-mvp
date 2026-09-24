using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Workforce;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _workScheduleShellWired;

    private void WireWorkScheduleWorkspace()
    {
        if (_workScheduleShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeWorkforceNavigationShell();
        _viewModel.InitializeWorkScheduleShell();

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new WorkScheduleService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new WorkScheduleView(new WorkScheduleViewModel(
            service,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>()));

        while (workspaceTabs.Items.Count <= WorkspaceSlots.WorkSchedule)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.WorkSchedule] = new TabItem { Content = view };

        _workScheduleShellWired = true;
    }

    private async void WorkSchedules_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateWorkSchedulesAsync();
        ApplyWorkScheduleHeader();
    }

    private void ApplyWorkScheduleHeader()
    {
        if (!_viewModel.IsWorkScheduleSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Ca / lịch làm việc");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Xem và điều chỉnh lịch làm việc tương lai theo chính sách đã áp dụng.");
    }
}
