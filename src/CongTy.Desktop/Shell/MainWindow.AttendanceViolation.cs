using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Workforce;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _attendanceViolationShellWired;

    private void WireAttendanceViolationWorkspace()
    {
        if (_attendanceViolationShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeWorkforceNavigationShell();
        _viewModel.InitializeAttendanceViolationShell();

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new AttendanceViolationService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new AttendanceViolationView(new AttendanceViolationViewModel(
            service,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>()));

        while (workspaceTabs.Items.Count <= WorkspaceSlots.AttendanceViolation)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.AttendanceViolation] = new TabItem { Content = view };

        _attendanceViolationShellWired = true;
    }

    private async void AttendanceViolation_OnClick(object sender, RoutedEventArgs e) =>
        await NavigateAttendanceViolationAsync();

    private async Task NavigateAttendanceViolationAsync()
    {
        WireAttendanceViolationWorkspace();
        await _viewModel.NavigateAttendanceViolationAsync();
        ApplyAttendanceViolationHeader();
    }

    private void ApplyAttendanceViolationHeader()
    {
        if (!_viewModel.IsAttendanceViolationSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Xử lý vi phạm chấm công");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Giải trình, xem xét và kết luận các sai lệch đã được Bảng công ghi nhận.");
    }
}
