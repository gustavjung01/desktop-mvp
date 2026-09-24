using System.Globalization;
using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class AttendanceViolationPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string CaseStatus(AttendanceViolationCaseData? value)
    {
        if (value is null) return "Chưa giải trình";
        if (value.Status == "RESOLVED" && !string.IsNullOrWhiteSpace(value.Outcome))
            return $"Đã kết luận · {Outcome(value.Outcome)}";

        return value.Status switch
        {
            "EXPLANATION_SUBMITTED" => "Đã gửi giải trình",
            "UNDER_REVIEW" => "Đang xem xét",
            "RESOLVED" => "Đã kết luận",
            _ => string.IsNullOrWhiteSpace(value.Status) ? "—" : value.Status,
        };
    }

    public static string Outcome(string? value) =>
        value switch
        {
            "CONFIRMED" => "Xác nhận vi phạm",
            "EXCUSED" => "Chấp nhận giải trình",
            _ => "—",
        };

    public static string ViolationLabel(AttendanceViolationHandlingEntryData entry) =>
        entry.Violation?.Label
        ?? entry.Case?.ViolationLabelSnapshot
        ?? "Vi phạm chấm công";

    public static string ViolationDetail(AttendanceViolationHandlingEntryData entry)
    {
        if (entry.Violation is not null)
            return entry.Violation.Detail;

        if (entry.Case is not null)
            return $"{entry.Case.ViolationDetailSnapshot} · Dữ liệu công hiện tại đã thay đổi.";

        return "Không còn dữ liệu vi phạm hiện tại.";
    }

    public static string Metric(AttendanceViolationHandlingEntryData entry)
    {
        if (entry.Violation?.Minutes is int minutes)
            return $"{minutes:N0} phút";
        if (entry.Violation?.DayFraction is decimal fraction)
            return $"{fraction.ToString("0.##", Vietnamese)} ngày";
        if (entry.Case?.ViolationMinutesSnapshot is int snapshotMinutes)
            return $"{snapshotMinutes:N0} phút";
        if (TryDecimal(entry.Case?.ViolationDayFractionSnapshot, out var snapshotFraction))
            return $"{snapshotFraction.ToString("0.##", Vietnamese)} ngày";
        return "—";
    }

    public static string Employee(AttendanceViolationHandlingEntryData entry) =>
        $"{entry.Employee.Code} · {entry.Employee.Name}";

    public static string Branch(AttendanceViolationHandlingEntryData entry) =>
        string.IsNullOrWhiteSpace(entry.Employee.BranchName)
            ? "Chưa gán chi nhánh"
            : entry.Employee.BranchName!;

    public static string Explanation(AttendanceViolationHandlingEntryData entry) =>
        entry.Case is null ? "Chưa có giải trình." : entry.Case.Explanation;

    public static string Conclusion(AttendanceViolationHandlingEntryData entry) =>
        string.IsNullOrWhiteSpace(entry.Case?.ReviewNote) ? "—" : entry.Case!.ReviewNote!.Trim();

    public static string Version(AttendanceViolationHandlingEntryData entry) =>
        entry.Case is null ? "—" : $"Lần xử lý {entry.Case.Version}";

    public static string Period(AttendanceTimesheetPeriodData value) =>
        $"{WorkSchedulePresentation.DateText(value.From)} – {WorkSchedulePresentation.DateText(value.To)}";

    private static bool TryDecimal(JsonElement? value, out decimal result)
    {
        result = 0;
        if (value is null) return false;
        var element = value.Value;
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetDecimal(out result);
        return element.ValueKind == JsonValueKind.String
            && decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
}

public sealed record AttendanceViolationBranchOption(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record AttendanceViolationOutcomeOption(string Key, string Label)
{
    public override string ToString() => Label;
}

public sealed record AttendanceViolationRowView(
    AttendanceViolationHandlingEntryData Source,
    string DateText,
    string EmployeeText,
    string BranchText,
    string ViolationText,
    string FactText,
    string MetricText,
    string StatusText,
    string ExplanationText,
    string ConclusionText,
    string VersionText,
    bool CanExplain,
    bool CanStartReview,
    bool CanConclude,
    bool ReadOnly);
