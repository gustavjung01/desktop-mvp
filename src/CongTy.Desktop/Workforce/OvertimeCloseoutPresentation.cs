using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class OvertimeCloseoutPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string DateText(string? value) => WorkSchedulePresentation.DateText(value);

    public static string OvertimeStatusLabel(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "SUBMITTED" => "Chờ duyệt",
        "APPROVED" => "Đã duyệt",
        "REJECTED" => "Từ chối",
        "ACTUAL_RECORDED" => "Đã ghi nhận thực tế",
        "CONFIRMED" => "Đã xác nhận giờ tính",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string PeriodStatusLabel(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "AGGREGATING" => "Đang tổng hợp",
        "NEEDS_ACTION" => "Cần xử lý",
        "RECONCILED" => "Đã đối soát",
        "CLOSED" => "Đã chốt",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string HoursText(int? minutes)
    {
        if (minutes is null) return "—";
        var hours = minutes.Value / 60m;
        return hours.ToString("0.##", Vietnamese) + " giờ";
    }

    public static string DecimalText(decimal value) => value.ToString("0.##", Vietnamese);

    public static decimal BlockerTotal(AttendancePeriodIssueSummaryData? issues)
    {
        var b = issues?.Blockers ?? new AttendancePeriodIssueBucketData();
        return b.ConfigurationIssueDays + b.PendingAdjustmentDays + b.PendingLeaveDays + b.OutstandingOvertimeRequests;
    }

    public static decimal WarningTotal(AttendancePeriodIssueSummaryData? issues)
    {
        var w = issues?.Warnings ?? new AttendancePeriodIssueBucketData();
        return w.IncompleteDays + w.UnexcusedAbsenceDays + w.ViolationDays;
    }

    public static string PeriodText(AttendancePeriodData period) =>
        $"{DateText(period.PeriodStart)} – {DateText(period.PeriodEnd)}";

    public static string ScopeText(AttendancePeriodData period) =>
        string.IsNullOrWhiteSpace(period.BranchName) ? "Toàn Công Ty" : period.BranchName.Trim();
}

public sealed record OvertimeOption(string Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record OvertimeRowView(
    OvertimeRequestData Source,
    string DateText,
    string EmployeeText,
    string BranchText,
    string RequestedText,
    string ActualText,
    string ConfirmedText,
    string StatusText,
    string NoteText,
    bool CanProcess);

public sealed record AttendancePeriodRowView(
    AttendancePeriodData Source,
    string PeriodText,
    string ScopeText,
    string StatusText,
    string BlockerText,
    string WarningText,
    string RevisionText,
    bool CanOpenPayroll);

public sealed record PayrollInputRowView(
    AttendancePayrollEmployeeData Source,
    string EmployeeText,
    string BranchText,
    string WorkDaysText,
    string CountedHoursText,
    string LeaveHoursText,
    string ConfirmedOvertimeText,
    string WarningText);
