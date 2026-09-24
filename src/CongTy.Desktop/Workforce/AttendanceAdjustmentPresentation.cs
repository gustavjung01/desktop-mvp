using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class AttendanceAdjustmentPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string StatusLabel(string? value) =>
        (value ?? string.Empty).ToUpperInvariant() switch
        {
            "SUBMITTED" => "Chờ duyệt",
            "APPROVED" => "Đã duyệt",
            "REJECTED" => "Từ chối",
            _ => "—",
        };

    public static string SourceLabel(string? value) =>
        string.Equals(value, "DIRECT", StringComparison.OrdinalIgnoreCase)
            ? "Điều chỉnh trực tiếp"
            : "Nhân viên gửi yêu cầu";

    public static string RequestedTimes(AttendanceAdjustmentRequestData row)
    {
        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(row.RequestedCheckInAt))
            values.Add($"Giờ vào {WorkSchedulePresentation.DateTimeText(row.RequestedCheckInAt, "Asia/Ho_Chi_Minh")}");
        if (!string.IsNullOrWhiteSpace(row.RequestedCheckOutAt))
            values.Add($"Giờ ra {WorkSchedulePresentation.DateTimeText(row.RequestedCheckOutAt, "Asia/Ho_Chi_Minh")}");
        return values.Count == 0 ? "—" : string.Join(" · ", values);
    }

    public static string EmployeeText(AttendanceAdjustmentRequestData row, AttendanceAdjustmentSelectedEmployeeData? selected)
    {
        if (!string.IsNullOrWhiteSpace(row.EmployeeCode))
            return $"{row.EmployeeCode} · {row.EmployeeName}";
        if (selected is not null)
            return $"{selected.Code} · {selected.Name}";
        return "Bản thân";
    }

    public static string BranchText(AttendanceAdjustmentRequestData row) =>
        string.IsNullOrWhiteSpace(row.BranchName) ? "Chưa gán chi nhánh" : row.BranchName!;

    public static string ReviewText(AttendanceAdjustmentRequestData row) =>
        row.Status == "SUBMITTED"
            ? "Chưa xử lý"
            : string.IsNullOrWhiteSpace(row.ReviewReason)
                ? "—"
                : row.ReviewReason!.Trim();

    public static string LockScope(AttendancePeriodLockData row) =>
        row.BranchId is null
            ? "Toàn Công Ty"
            : string.Join(" · ", new[] { row.BranchCode, row.BranchName }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public static string Period(string from, string to) =>
        $"{WorkSchedulePresentation.DateText(from)} – {WorkSchedulePresentation.DateText(to)}";

    public static string Count(int value) =>
        Math.Max(0, value).ToString("N0", Vietnamese);
}

public sealed record AttendanceAdjustmentStatusOption(string Key, string Label)
{
    public override string ToString() => Label;
}

public sealed record AttendanceAdjustmentBranchOption(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record AttendanceAdjustmentEmployeeOption(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record AttendanceAdjustmentRowView(
    AttendanceAdjustmentRequestData Source,
    string DateText,
    string EmployeeText,
    string BranchText,
    string RequestedTimesText,
    string SourceText,
    string StatusText,
    string ReasonText,
    string ReviewText,
    bool CanReview);

public sealed record AttendancePeriodLockRowView(
    AttendancePeriodLockData Source,
    string PeriodText,
    string ScopeText,
    string ReasonText,
    string LockedAtText);
