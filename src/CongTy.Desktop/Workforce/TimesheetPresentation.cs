using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class TimesheetPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string DayCount(decimal value) =>
        Math.Max(0, value).ToString("0.#", Vietnamese);

    public static string Minutes(int value)
    {
        var safe = Math.Max(0, value);
        var hours = safe / 60;
        var minutes = safe % 60;
        if (hours == 0) return $"{minutes} phút";
        if (minutes == 0) return $"{hours} giờ";
        return $"{hours} giờ {minutes} phút";
    }

    public static string Clock(string? value, string? timeZone)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return "—";
        return WorkSchedulePresentation.ConvertToZone(parsed, timeZone).ToString("HH:mm", Vietnamese);
    }

    public static string DateTimeText(string? value, string? timeZone) =>
        WorkSchedulePresentation.DateTimeText(value, timeZone);

    public static string PeriodText(AttendanceTimesheetPeriodData period) =>
        $"{WorkSchedulePresentation.DateText(period.From)} – {WorkSchedulePresentation.DateText(period.To)}";

    public static string ScopeText(AttendanceTimesheetScopeData scope) =>
        scope.SelfOnly
            ? "Bản thân"
            : scope.CompanyScope
                ? "Toàn Công Ty"
                : $"{scope.Branches.Length} chi nhánh";

    public static string StatusLabel(AttendanceTimesheetDayData day)
    {
        if (day.Status == "APPROVED_LEAVE")
        {
            var label = day.Leave.ApprovedLabels.Length > 0
                ? string.Join(" + ", day.Leave.ApprovedLabels)
                : "Nghỉ được duyệt";
            return day.Leave.ApprovedFraction < 1 ? $"{label} · Nửa ngày" : label;
        }

        if (day.Status == "PENDING_LEAVE")
            return day.Leave.PendingFraction < 1 ? "Chờ duyệt nghỉ · Nửa ngày" : "Chờ duyệt nghỉ";

        return day.Status switch
        {
            "UPCOMING" => "Sắp tới",
            "DAY_OFF" => "Ngày nghỉ",
            "NO_ATTENDANCE_REQUIRED" => "Không yêu cầu chấm công",
            "MISSING_POLICY" => "Thiếu chính sách làm việc",
            "MISSING_SCHEDULE" => "Thiếu lịch làm việc",
            "NOT_STARTED" => "Chưa chấm công",
            "WORKING" => "Đang làm việc",
            "OUTSIDE" => "Đang ra ngoài",
            "MISSING_CHECK_IN" => "Thiếu giờ vào",
            "MISSING_CHECK_OUT" => "Thiếu giờ ra",
            "INCOMPLETE" => "Chấm công chưa đầy đủ",
            "PENDING_ADJUSTMENT" => "Chờ duyệt điều chỉnh",
            "UNEXCUSED_ABSENCE" => "Vắng không phép",
            "LATE_AND_EARLY" => "Đi trễ và về sớm",
            "LATE" => "Đi trễ",
            "EARLY" => "Về sớm",
            "COMPLETE" => "Chấm công đầy đủ",
            _ => string.IsNullOrWhiteSpace(day.Status) ? "—" : day.Status,
        };
    }

    public static string CompactStatus(AttendanceTimesheetDayData day) =>
        day.Status switch
        {
            "COMPLETE" => "✓",
            "LATE" => "Trễ",
            "EARLY" => "Sớm",
            "LATE_AND_EARLY" => "Trễ/sớm",
            "DAY_OFF" => "Nghỉ",
            "APPROVED_LEAVE" => day.Leave.ApprovedFraction < 1 ? "½ phép" : "Phép",
            "PENDING_LEAVE" => "Chờ nghỉ",
            "PENDING_ADJUSTMENT" => "Chờ ĐC",
            "UNEXCUSED_ABSENCE" => "Vắng",
            "NO_ATTENDANCE_REQUIRED" => "Không chấm",
            "WORKING" => "Đang",
            "OUTSIDE" => "Ra ngoài",
            "UPCOMING" => string.Empty,
            "NOT_STARTED" => "Chưa",
            "MISSING_POLICY" => "Thiếu CS",
            "MISSING_SCHEDULE" => "Thiếu lịch",
            _ => "Chưa đủ",
        };

    public static string EmployeeLeaveSummary(AttendanceTimesheetMonthData row)
    {
        var parts = new List<string>();
        if (row.ScheduledDaysOff > 0) parts.Add($"Nghỉ theo lịch {DayCount(row.ScheduledDaysOff)}");
        if (row.ApprovedLeaveDays > 0) parts.Add($"Phép {DayCount(row.ApprovedLeaveDays)}");
        return parts.Count == 0 ? "—" : string.Join(" · ", parts);
    }

    public static string EmployeeAttentionSummary(AttendanceTimesheetMonthData row)
    {
        var parts = new List<string>();
        if (row.UnexcusedAbsenceDays > 0) parts.Add($"Vắng {DayCount(row.UnexcusedAbsenceDays)}");
        if (row.IncompleteDays > 0) parts.Add($"Thiếu chấm công {DayCount(row.IncompleteDays)}");
        if (row.ViolationDays > 0) parts.Add($"Vi phạm {row.ViolationDays}");
        if (row.PendingLeaveDays > 0) parts.Add($"Chờ duyệt nghỉ {DayCount(row.PendingLeaveDays)}");
        if (row.PendingAdjustmentDays > 0) parts.Add($"Chờ điều chỉnh {row.PendingAdjustmentDays}");
        if (row.ConfigurationIssueDays > 0) parts.Add($"Thiếu thiết lập {DayCount(row.ConfigurationIssueDays)}");
        return parts.Count == 0 ? "Không có" : string.Join(" · ", parts);
    }

    public static string AdjustmentSummary(AttendanceTimesheetMonthData row)
    {
        var parts = new List<string>();
        if (row.AdjustedDays > 0) parts.Add($"{row.AdjustedDays} đã duyệt");
        if (row.PendingAdjustmentDays > 0) parts.Add($"{row.PendingAdjustmentDays} chờ duyệt");
        return parts.Count == 0 ? "—" : string.Join(" · ", parts);
    }

    public static string MonthlyViolationSummary(AttendanceTimesheetMonthData row)
    {
        if (row.ViolationDays == 0) return "Không ghi nhận";
        var parts = new List<string> { $"{row.ViolationDays} ngày" };
        if (row.LateViolationDays > 0) parts.Add($"Trễ {row.LateViolationDays}");
        if (row.EarlyLeaveViolationDays > 0) parts.Add($"Sớm {row.EarlyLeaveViolationDays}");
        if (row.MissingAttendanceViolationDays > 0) parts.Add($"Thiếu {row.MissingAttendanceViolationDays}");
        if (row.UnexcusedAbsenceViolationDays > 0) parts.Add($"Vắng {DayCount(row.UnexcusedAbsenceViolationDays)}");
        return string.Join(" · ", parts);
    }

    public static string DayAttentionSummary(AttendanceTimesheetDayData day)
    {
        if (day.ConfigurationIssue == "MISSING_POLICY") return "Thiếu chính sách";
        if (day.ConfigurationIssue == "MISSING_SCHEDULE") return "Thiếu lịch làm việc";
        if (day.UnexcusedAbsenceFraction > 0) return $"Vắng {DayCount(day.UnexcusedAbsenceFraction)} ngày";
        if (day.LateMinutes > 0 && day.EarlyLeaveMinutes > 0) return $"Trễ {day.LateMinutes}′ · Sớm {day.EarlyLeaveMinutes}′";
        if (day.LateMinutes > 0) return $"Trễ {day.LateMinutes}′";
        if (day.EarlyLeaveMinutes > 0) return $"Sớm {day.EarlyLeaveMinutes}′";
        if (day.MissingCheckIn) return "Thiếu giờ vào";
        if (day.MissingCheckOut) return "Thiếu giờ ra";
        if (day.Leave.PendingFraction > 0) return "Chờ duyệt nghỉ";
        if (day.Adjustment?.Status == "SUBMITTED") return "Chờ điều chỉnh";
        return "—";
    }

    public static string RequiredWork(AttendanceTimesheetDayData day)
    {
        if (!day.ScheduledWorkDay || day.Status == "DAY_OFF") return "Không phải làm";
        if (day.Leave.ApprovedFraction >= 1) return "Không phải làm";
        return day.RequiredStartAt is not null && day.RequiredEndAt is not null
            ? $"{Clock(day.RequiredStartAt, day.Policy?.Timezone)} – {Clock(day.RequiredEndAt, day.Policy?.Timezone)}"
            : "Theo chính sách làm việc";
    }

    public static string LeaveSegments(AttendanceTimesheetDayData day)
    {
        var approved = day.Leave.ApprovedSegments.ToHashSet(StringComparer.Ordinal);
        if (approved.Contains("FIRST_HALF") && approved.Contains("SECOND_HALF")) return "Cả ngày";
        if (approved.Contains("FIRST_HALF")) return "Nửa ca đầu";
        if (approved.Contains("SECOND_HALF")) return "Nửa ca sau";

        var pending = day.Leave.PendingSegments.ToHashSet(StringComparer.Ordinal);
        if (pending.Contains("FIRST_HALF") && pending.Contains("SECOND_HALF")) return "Cả ngày đang chờ duyệt";
        if (pending.Contains("FIRST_HALF")) return "Nửa ca đầu đang chờ duyệt";
        if (pending.Contains("SECOND_HALF")) return "Nửa ca sau đang chờ duyệt";
        return "Không có";
    }

    public static string LeaveSummary(AttendanceTimesheetDayData day)
    {
        if (day.Leave.Requests.Length == 0) return "Không có đơn nghỉ";
        var parts = new List<string>();
        if (day.Leave.ApprovedLabels.Length > 0) parts.Add($"Đã duyệt: {string.Join(" + ", day.Leave.ApprovedLabels)}");
        if (day.Leave.PendingLabels.Length > 0) parts.Add($"Chờ duyệt: {string.Join(" + ", day.Leave.PendingLabels)}");
        return parts.Count == 0 ? "Có đơn nghỉ" : string.Join(" · ", parts);
    }

    public static string SourceSummary(AttendanceTimesheetDayData day)
    {
        var values = day.AttendanceSources.Select(SourceLabel).ToList();
        if (day.ScheduleSource == "OVERRIDE") values.Add("Lịch điều chỉnh");
        else if (day.ScheduleSource == "POLICY") values.Add("Lịch theo chính sách");
        if (day.Leave.Requests.Length > 0) values.Add("Đơn nghỉ");
        return values.Distinct(StringComparer.CurrentCultureIgnoreCase).DefaultIfEmpty("Chưa có thông tin").Aggregate((a, b) => $"{a} · {b}");
    }

    public static string SourceLabel(string? value) =>
        (value ?? string.Empty).ToUpperInvariant() switch
        {
            "QR" => "Mã QR",
            "FACE" => "Quét khuôn mặt",
            "MANUAL" => "Chấm trực tiếp",
            "ADJUSTMENT" => "Điều chỉnh",
            "SYSTEM" => "Hệ thống",
            _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
        };

    public static string ValidationLabel(string? value) =>
        (value ?? string.Empty).ToUpperInvariant() switch
        {
            "VALID" => "Hợp lệ",
            "PENDING" => "Chờ xác minh",
            "INVALID" => "Không hợp lệ",
            _ => "—",
        };

    public static string EventLabel(AttendanceEventData value) =>
        value.EventType switch
        {
            "CHECK_IN" => "Giờ vào",
            "CHECK_OUT" => "Giờ ra",
            "TEMP_EXIT" => "Ra tạm thời",
            "RETURN" => "Quay lại",
            _ => value.EventType,
        };
}

public sealed record TimesheetBranchOption(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record TimesheetEmployeeRowView(
    AttendanceTimesheetMonthData Source,
    string EmployeeText,
    string BranchText,
    string WorkDaysText,
    string CompletedDaysText,
    string LeaveText,
    string AttentionText,
    string CountedTimeText,
    string AdjustmentText,
    IReadOnlyList<TimesheetMonthDayCellView> MonthCells,
    string ViolationText,
    string LateText,
    string EarlyText,
    string LockedText);

public sealed record TimesheetMonthDayCellView(
    AttendanceTimesheetDayData? Source,
    int DayNumber,
    string Text,
    string ToolTip,
    bool HasDay);

public sealed record TimesheetEmployeeDayRowView(
    AttendanceTimesheetDayData Source,
    string DateText,
    string StatusText,
    string ScheduleText,
    string InOutText,
    string CountedText,
    string AttentionText);

public sealed record TimesheetEventRowView(
    AttendanceEventData Source,
    string EventText,
    string SourceText,
    string PointText,
    string ValidationText,
    string TimeText,
    string NoteText);
