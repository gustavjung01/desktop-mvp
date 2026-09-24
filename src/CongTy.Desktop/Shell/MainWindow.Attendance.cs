using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Workforce;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _attendanceShellWired;

    private void WireAttendanceWorkspace()
    {
        if (_attendanceShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeWorkforceNavigationShell();
        _viewModel.InitializeAttendanceShell();

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new AttendanceService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new AttendanceView(new AttendanceViewModel(
            service,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>()));

        while (workspaceTabs.Items.Count <= WorkspaceSlots.Attendance)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.Attendance] = new TabItem { Content = view };

        _attendanceShellWired = true;
    }

    private async void Attendance_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateAttendanceAsync();
        ApplyAttendanceHeader();
    }

    private void ApplyAttendanceHeader()
    {
        if (!_viewModel.IsAttendanceSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Chấm công");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Ghi nhận chấm công theo chính sách và nơi làm việc; quản lý có thể phát mã QR ngắn hạn.");
    }
}
