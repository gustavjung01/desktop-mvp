using System.Globalization;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Workforce;

public sealed partial class TimesheetViewModel
{
    public async Task<ApiDownloadFile?> ExportTimesheetAsync(CancellationToken cancellationToken = default)
    {
        var (from, to) = ExportPeriod();
        var view = IsMonthlyView ? "monthly" : "employee";
        var limit = IsMonthlyView ? 25 : 100;
        var branchId = OfficeDataExportFile.FilterValue(SelectedBranch?.Id);
        var employeeQuery = string.IsNullOrWhiteSpace(EmployeeQuery) ? null : EmployeeQuery.Trim();

        var rows = new List<AttendanceTimesheetMonthData>();
        var offset = 0;
        for (var page = 0; page < 200; page++)
        {
            var response = await _service.ListAsync(
                view, from, to, employeeQuery,
                string.IsNullOrWhiteSpace(branchId) ? null : branchId,
                limit, offset, cancellationToken).ConfigureAwait(true);
            rows.AddRange(response.Rows);
            if (!response.Pagination.HasNext) break;
            if (response.Rows.Length == 0) throw new InvalidOperationException("Không thể tiếp tục phân trang Bảng công.");
            offset += response.Rows.Length;
            if (page == 199) throw new InvalidOperationException("Bảng công vượt giới hạn xuất an toàn 200 trang.");
        }

        var summaryRows = rows.Select(row => OfficeDataExportFile.Row(
            row.Employee.Code, row.Employee.Name, row.Employee.BranchName,
            TimesheetPresentation.DayCount(row.WorkDays), TimesheetPresentation.DayCount(row.CompletedDays),
            TimesheetPresentation.DayCount(row.ScheduledDaysOff), TimesheetPresentation.DayCount(row.ApprovedLeaveDays),
            TimesheetPresentation.DayCount(row.PendingLeaveDays), TimesheetPresentation.DayCount(row.UnexcusedAbsenceDays),
            TimesheetPresentation.DayCount(row.IncompleteDays), row.ViolationDays.ToString(CultureInfo.InvariantCulture),
            row.CountedMinutes.ToString(CultureInfo.InvariantCulture), row.ActualMinutes.ToString(CultureInfo.InvariantCulture),
            row.LeaveCreditedMinutes.ToString(CultureInfo.InvariantCulture), row.LateMinutes.ToString(CultureInfo.InvariantCulture),
            row.EarlyLeaveMinutes.ToString(CultureInfo.InvariantCulture), row.AdjustedDays.ToString(CultureInfo.InvariantCulture),
            row.PendingAdjustmentDays.ToString(CultureInfo.InvariantCulture))).ToArray();

        var dailyRows = rows.SelectMany(row => row.Days).Select(day => OfficeDataExportFile.Row(
            OfficeDataExportFile.DateLabel(day.WorkDate), day.Employee.Code, day.Employee.Name, day.Employee.BranchName,
            TimesheetPresentation.StatusLabel(day), TimesheetPresentation.Clock(day.CheckInAt, day.Policy?.Timezone),
            TimesheetPresentation.Clock(day.CheckOutAt, day.Policy?.Timezone), day.ActualMinutes.ToString(CultureInfo.InvariantCulture),
            day.CountedMinutes.ToString(CultureInfo.InvariantCulture), day.LateMinutes.ToString(CultureInfo.InvariantCulture),
            day.EarlyLeaveMinutes.ToString(CultureInfo.InvariantCulture), TimesheetPresentation.DayCount(day.Leave.ApprovedFraction),
            TimesheetPresentation.DayCount(day.Leave.PendingFraction), TimesheetPresentation.DayCount(day.UnexcusedAbsenceFraction),
            day.ViolationEvaluation.Items.Length == 0 ? "—" : string.Join(" · ", day.ViolationEvaluation.Items.Select(item => item.Label)))).ToArray();

        var historyRows = rows.SelectMany(row => row.Days).SelectMany(day =>
            day.Events.Select(item => OfficeDataExportFile.Row(
                day.Employee.Code, day.Employee.Name, day.Employee.BranchName,
                WorkSchedulePresentation.DateTimeText(item.OccurredAt, day.Policy?.Timezone),
                OfficeDataExportFile.AttendanceAction(item.EventType), OfficeDataExportFile.MovementReason(item.MovementReason),
                OfficeDataExportFile.AttendanceSource(item.Source), item.PointName,
                OfficeDataExportFile.Validation(item.ValidationStatus), item.Note))).ToArray();

        return OfficeDataExportFile.Xlsx(
            IsMonthlyView ? "bang-cong-thang.xlsx" : "bang-cong-theo-ngay.xlsx",
            new OfficeExportSheet(
                "Tổng hợp nhân sự",
                ["Mã nhân viên", "Nhân viên", "Chi nhánh", "Ngày phải làm", "Ngày đủ công", "Nghỉ theo lịch", "Nghỉ đã duyệt", "Nghỉ chờ duyệt", "Vắng không phép", "Ngày chưa đủ công", "Ngày có vi phạm", "Phút được tính", "Phút thực tế", "Phút nghỉ được tính", "Đi trễ (phút)", "Về sớm (phút)", "Ngày đã điều chỉnh", "Ngày chờ điều chỉnh"],
                summaryRows),
            new OfficeExportSheet(
                "Bảng công từng ngày",
                ["Ngày", "Mã nhân viên", "Nhân viên", "Chi nhánh", "Trạng thái", "Giờ vào", "Giờ ra", "Phút thực tế", "Phút được tính", "Đi trễ (phút)", "Về sớm (phút)", "Nghỉ đã duyệt", "Nghỉ chờ duyệt", "Vắng không phép", "Vi phạm"],
                dailyRows),
            new OfficeExportSheet(
                "Lịch sử chấm công",
                ["Mã nhân viên", "Nhân viên", "Chi nhánh", "Thời điểm", "Thao tác", "Lý do ra ngoài", "Hình thức ghi nhận", "Nơi ghi nhận", "Kết quả xác minh", "Ghi chú"],
                historyRows));
    }

    private (string From, string To) ExportPeriod()
    {
        if (IsMonthlyView)
        {
            if (MonthDate is null) throw new InvalidOperationException("Chọn tháng cần xuất.");
            var start = new DateTime(MonthDate.Value.Year, MonthDate.Value.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (FromDate is null || ToDate is null) throw new InvalidOperationException("Chọn kỳ Bảng công cần xuất.");
        return (
            FromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ToDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
