using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Workforce;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _timesheetShellWired;

    private void WireTimesheetWorkspace()
    {
        if (_timesheetShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeWorkforceNavigationShell();
        _viewModel.InitializeTimesheetShell();

        var app = (CongTy.Desktop.App)Application.Current;
        var service = new TimesheetService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var view = new TimesheetView(new TimesheetViewModel(
            service,
            app.ResolveRequired<IAccessStateService>()));
        view.AdjustmentRequested += Timesheet_AdjustmentRequested;
        view.ViolationRequested += Timesheet_ViolationRequested;

        while (workspaceTabs.Items.Count <= WorkspaceSlots.Timesheet)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[WorkspaceSlots.Timesheet] = new TabItem { Content = view };

        _timesheetShellWired = true;
    }

    private async void Timesheet_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateTimesheetAsync();
        ApplyTimesheetHeader();
    }

    private async void Timesheet_AdjustmentRequested(
        object? sender,
        TimesheetAdjustmentRequestedEventArgs e) =>
        await NavigateAttendanceAdjustmentAsync(e.EmployeeId, e.WorkDate);

    private async void Timesheet_ViolationRequested(object? sender, EventArgs e) =>
        await NavigateAttendanceViolationAsync();

    private void ApplyTimesheetHeader()
    {
        if (!_viewModel.IsTimesheetSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "NHÂN SỰ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Bảng công");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Theo dõi lịch làm, nghỉ, phép, chấm công và tình trạng xử lý theo phạm vi được cấp.");
    }
}
