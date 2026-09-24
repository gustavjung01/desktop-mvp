using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Workforce;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _attendanceAdjustmentShellWired;
    private AttendanceAdjustmentViewModel? _attendanceAdjustmentViewModel;

    private void WireAttendanceAdjustmentWorkspace()
    {
        if (_attendanceAdjustmentShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeWorkforceNavigationShell();
        _viewModel.InitializeAttendanceAdjustmentShell();

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new AttendanceAdjustmentService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var employeeRead = new EmployeeDirectoryReadService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());

        _attendanceAdjustmentViewModel = new AttendanceAdjustmentViewModel(
            service,
            employeeRead,
            app.ResolveRequired<ICanonicalIdempotencyKeyProvider>(),
            app.ResolveRequired<IAccessStateService>());

        var view = new AttendanceAdjustmentView(_attendanceAdjustmentViewModel);
        view.OpenTimesheetRequested += AttendanceAdjustment_OpenTimesheetRequested;

        while (workspaceTabs.Items.Count <= WorkspaceSlots.AttendanceAdjustment)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.AttendanceAdjustment] = new TabItem { Content = view };

        _attendanceAdjustmentShellWired = true;
    }

    private async void AttendanceAdjustment_OnClick(object sender, RoutedEventArgs e)
    {
        await NavigateAttendanceAdjustmentAsync(null, null);
    }

    private async Task NavigateAttendanceAdjustmentAsync(string? employeeId, string? workDate)
    {
        WireAttendanceAdjustmentWorkspace();
        if (_attendanceAdjustmentViewModel is not null)
            await _attendanceAdjustmentViewModel.PrepareAsync(employeeId, workDate);
        await _viewModel.NavigateAttendanceAdjustmentAsync();
        ApplyAttendanceAdjustmentHeader();
    }

    private async void AttendanceAdjustment_OpenTimesheetRequested(object? sender, EventArgs e)
    {
        await _viewModel.NavigateTimesheetAsync();
        ApplyTimesheetHeader();
    }

    private void ApplyAttendanceAdjustmentHeader()
    {
        if (!_viewModel.IsAttendanceAdjustmentSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Điều chỉnh công");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Gửi, duyệt và theo dõi thay đổi giờ chấm công có lý do và lịch sử xử lý.");
    }
}
