using System.Globalization;
using CongTy.ApiClient;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Workforce;

public sealed partial class WorkScheduleViewModel
{
    public async Task<ApiDownloadFile?> ExportSchedulesAsync(CancellationToken cancellationToken = default)
    {
        if (FromDate is null || ToDate is null) throw new InvalidOperationException("Chọn kỳ ca và lịch làm việc cần xuất.");
        var from = FromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = ToDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var employee = OfficeDataExportFile.FilterValue(SelectedEmployeeFilter?.Id);

        var schedules = await _service.ListSchedulesAsync(
            from, to, string.IsNullOrWhiteSpace(employee) ? null : employee, cancellationToken).ConfigureAwait(true);
        var catalog = await _service.GetPlanningCatalogAsync(cancellationToken).ConfigureAwait(true);

        var scheduleRows = schedules.Select(item => OfficeDataExportFile.Row(
            OfficeDataExportFile.DateLabel(item.WorkDate), item.EmployeeCode, item.EmployeeName,
            OfficeDataExportFile.ScheduleKind(item.ScheduleKind),
            TimesheetPresentation.Clock(item.ScheduledStartAt, item.PolicyTimezone),
            TimesheetPresentation.Clock(item.ScheduledEndAt, item.PolicyTimezone),
            OfficeDataExportFile.ScheduleSource(item.Source),
            string.IsNullOrWhiteSpace(item.PolicyName) ? item.PolicyCode : item.PolicyName,
            item.OverrideReason)).ToArray();

        var shiftRows = catalog.ShiftTemplates.Select(item => OfficeDataExportFile.Row(
            item.Code, item.Name, OfficeDataExportFile.ClockText(item.StartTime), OfficeDataExportFile.ClockText(item.EndTime),
            item.BreakMinutes.ToString(CultureInfo.InvariantCulture), item.IsActive ? "Đang dùng" : "Ngừng dùng")).ToArray();

        var weekRows = catalog.WeekTemplates.SelectMany(template =>
            template.Days.OrderBy(day => day.Weekday).Select(day => OfficeDataExportFile.Row(
                template.Code, template.Name, OfficeDataExportFile.Weekday(day.Weekday),
                OfficeDataExportFile.ScheduleKind(day.ScheduleKind),
                string.IsNullOrWhiteSpace(day.ShiftName) ? day.ShiftCode : day.ShiftName,
                OfficeDataExportFile.ClockText(day.ShiftStartTime), OfficeDataExportFile.ClockText(day.ShiftEndTime),
                day.ShiftBreakMinutes?.ToString(CultureInfo.InvariantCulture),
                template.IsActive ? "Đang dùng" : "Ngừng dùng"))).ToArray();

        return OfficeDataExportFile.Xlsx(
            "ca-va-lich-lam-viec.xlsx",
            new OfficeExportSheet(
                "Lịch làm việc",
                ["Ngày", "Mã nhân viên", "Nhân viên", "Loại ngày", "Bắt đầu", "Kết thúc", "Nguồn lịch", "Chính sách", "Lý do điều chỉnh"],
                scheduleRows),
            new OfficeExportSheet(
                "Ca mẫu",
                ["Mã ca", "Tên ca", "Giờ bắt đầu", "Giờ kết thúc", "Nghỉ giữa ca (phút)", "Trạng thái"],
                shiftRows),
            new OfficeExportSheet(
                "Lịch tuần",
                ["Mã lịch tuần", "Tên lịch tuần", "Ngày", "Loại ngày", "Ca làm việc", "Giờ bắt đầu", "Giờ kết thúc", "Nghỉ giữa ca (phút)", "Trạng thái"],
                weekRows));
    }
}
