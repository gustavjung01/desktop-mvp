using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Settings;

public static class EmployeeMcpReportingPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Count(string? value)
    {
        var normalized = (value ?? "0").Trim();
        return long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number.ToString("N0", Vietnamese)
            : normalized;
    }

    public static string Percent(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return "—";
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? $"{number.ToString("0.##", Vietnamese)}%"
            : $"{normalized.Replace('.', ',')}%";
    }

    public static string ActorLabel(string? salesLabel, string? employeeCode, string? employeeName)
    {
        if (!string.IsNullOrWhiteSpace(employeeCode))
            return $"{employeeCode.Trim()} — {Display(employeeName, Display(salesLabel, "Nhân viên"))}";
        return !string.IsNullOrWhiteSpace(salesLabel)
            ? $"{salesLabel.Trim()} — chưa liên kết hồ sơ nhân viên"
            : "Chưa xác định nhân viên";
    }

    public static string SessionStatus(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "open" or "active" or "in_progress" => "Đang thực hiện",
            "completed" or "closed" => "Hoàn tất",
            "cancelled" => "Đã hủy",
            _ => "Trạng thái khác"
        };

    public static string ExceptionLabel(string? value) =>
        value switch
        {
            "MISSING_FIELD_ACTOR_CODE" => "Thiếu thông tin nhân viên",
            "UNMAPPED_EMPLOYEE_CODE" => "Chưa liên kết hồ sơ nhân viên",
            "SESSION_COUNTER_MISMATCH" => "Số liệu phiên cần đối soát",
            _ => "Dữ liệu cần kiểm tra"
        };

    public static DateTime? ParseDate(string? value) =>
        DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date.Date
            : null;

    public static string Display(string? value, string fallback = "—") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    public static string GeneratedAt(string? value, string? timezone)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return $"Thời điểm tạo: {Display(value)} · Múi giờ: {Display(timezone)}.";
        return $"Thời điểm tạo: {parsed.ToOffset(TimeSpan.FromHours(7)):dd/MM/yyyy HH:mm:ss} · Múi giờ: {Display(timezone)}.";
    }
}

public sealed record EmployeeMcpActorRowView(
    string Employee,
    string SessionCount,
    string RouteCount,
    string PlannedOutletCount,
    string VisitedOutletCount,
    string CheckedInOutletCount,
    string VisitCount,
    string OrderIntentCount,
    string OnboardingSubmittedCount,
    string OnboardingConvertedCount,
    string CoreSalesOrderCount,
    string PlannedVisitRatePercent,
    string OrderIntentConversionPercent,
    string CoreOrderConversionPercent)
{
    public static EmployeeMcpActorRowView From(EmployeeMcpActorRowData row) => new(
        EmployeeMcpReportingPresentation.ActorLabel(row.SalesLabel, row.EmployeeCode, row.EmployeeName),
        EmployeeMcpReportingPresentation.Count(row.SessionCount),
        EmployeeMcpReportingPresentation.Count(row.RouteCount),
        EmployeeMcpReportingPresentation.Count(row.PlannedOutletCount),
        EmployeeMcpReportingPresentation.Count(row.VisitedOutletCount),
        EmployeeMcpReportingPresentation.Count(row.CheckedInOutletCount),
        EmployeeMcpReportingPresentation.Count(row.VisitCount),
        EmployeeMcpReportingPresentation.Count(row.OrderIntentCount),
        EmployeeMcpReportingPresentation.Count(row.OnboardingSubmittedCount),
        EmployeeMcpReportingPresentation.Count(row.OnboardingConvertedCount),
        EmployeeMcpReportingPresentation.Count(row.CoreSalesOrderCount),
        EmployeeMcpReportingPresentation.Percent(row.PlannedVisitRatePercent),
        EmployeeMcpReportingPresentation.Percent(row.OrderIntentConversionPercent),
        EmployeeMcpReportingPresentation.Percent(row.CoreOrderConversionPercent));
}

public sealed record EmployeeMcpRouteRowView(
    string Route,
    string Area,
    string Employee,
    string SessionCount,
    string PlannedOutletCount,
    string VisitedOutletCount,
    string CheckedInOutletCount,
    string OrderIntentCount,
    string CoreSalesOrderCount,
    string PlannedVisitRatePercent)
{
    public static EmployeeMcpRouteRowView From(EmployeeMcpRouteRowData row) => new(
        $"{EmployeeMcpReportingPresentation.Display(row.RouteCode, "Chưa có mã tuyến")} — {EmployeeMcpReportingPresentation.Display(row.RouteName)}",
        EmployeeMcpReportingPresentation.Display(row.Area),
        EmployeeMcpReportingPresentation.ActorLabel(row.SalesLabel, row.EmployeeCode, row.EmployeeName),
        EmployeeMcpReportingPresentation.Count(row.SessionCount),
        EmployeeMcpReportingPresentation.Count(row.PlannedOutletCount),
        EmployeeMcpReportingPresentation.Count(row.VisitedOutletCount),
        EmployeeMcpReportingPresentation.Count(row.CheckedInOutletCount),
        EmployeeMcpReportingPresentation.Count(row.OrderIntentCount),
        EmployeeMcpReportingPresentation.Count(row.CoreSalesOrderCount),
        EmployeeMcpReportingPresentation.Percent(row.PlannedVisitRatePercent));
}

public sealed record EmployeeMcpSessionRowView(
    string Date,
    string Route,
    string Employee,
    string Status,
    string PlannedOutletCount,
    string VisitedOutletCount,
    string CheckedInOutletCount,
    string VisitCount,
    string OrderIntentCount,
    string OnboardingCount,
    string CoreSalesOrderCount,
    bool NeedsReconciliation)
{
    public static EmployeeMcpSessionRowView From(EmployeeMcpSessionRowData row) => new(
        EmployeeMcpReportingPresentation.Display(row.SessionDate),
        $"{EmployeeMcpReportingPresentation.Display(row.RouteCode, "Chưa có mã tuyến")} — {EmployeeMcpReportingPresentation.Display(row.RouteName)}",
        EmployeeMcpReportingPresentation.ActorLabel(row.SalesLabel, row.EmployeeCode, row.EmployeeName),
        EmployeeMcpReportingPresentation.SessionStatus(row.Status),
        EmployeeMcpReportingPresentation.Count(row.PlannedOutletCount),
        EmployeeMcpReportingPresentation.Count(row.VisitedOutletCount),
        EmployeeMcpReportingPresentation.Count(row.CheckedInOutletCount),
        EmployeeMcpReportingPresentation.Count(row.VisitCount),
        EmployeeMcpReportingPresentation.Count(row.OrderIntentCount),
        $"{EmployeeMcpReportingPresentation.Count(row.OnboardingConvertedCount)} / {EmployeeMcpReportingPresentation.Count(row.OnboardingSubmittedCount)}",
        EmployeeMcpReportingPresentation.Count(row.CoreSalesOrderCount),
        row.StoredCounterMismatch);
}

public sealed record EmployeeMcpQualityRowView(
    string Type,
    string Employee,
    string PeriodRoute,
    string Recorded,
    string Compared)
{
    public static EmployeeMcpQualityRowView From(EmployeeMcpUnmappedActorData row) => new(
        EmployeeMcpReportingPresentation.ExceptionLabel(row.ExceptionCode),
        EmployeeMcpReportingPresentation.Display(row.SalesLabel),
        $"{EmployeeMcpReportingPresentation.Display(row.FirstSessionDate)} → {EmployeeMcpReportingPresentation.Display(row.LastSessionDate)}",
        $"{EmployeeMcpReportingPresentation.Count(row.SessionCount)} phiên",
        "—");

    public static EmployeeMcpQualityRowView From(EmployeeMcpCounterMismatchData row) => new(
        EmployeeMcpReportingPresentation.ExceptionLabel(row.ExceptionCode),
        EmployeeMcpReportingPresentation.Display(row.SalesLabel),
        $"{EmployeeMcpReportingPresentation.Display(row.SessionDate)} · {EmployeeMcpReportingPresentation.Display(row.RouteCode, "Chưa có mã tuyến")}",
        $"KH {EmployeeMcpReportingPresentation.Count(row.StoredPlannedCustomers)} · ghé {EmployeeMcpReportingPresentation.Count(row.StoredVisitedCustomers)} · nhu cầu {EmployeeMcpReportingPresentation.Count(row.StoredOrderCount)}",
        $"KH {EmployeeMcpReportingPresentation.Count(row.DerivedPlannedOutletCount)} · ghé {EmployeeMcpReportingPresentation.Count(row.DerivedVisitedOutletCount)} · nhu cầu {EmployeeMcpReportingPresentation.Count(row.DerivedOrderIntentCount)}");
}
