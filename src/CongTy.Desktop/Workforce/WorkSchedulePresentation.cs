using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class WorkSchedulePresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static string DateText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        var candidate = value.Trim();
        if (candidate.Length >= 10) candidate = candidate[..10];
        return DateOnly.TryParseExact(candidate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString("dd/MM/yyyy", Vietnamese)
            : value.Trim();
    }

    public static string DateTimeText(string? value, string? timeZone)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        var local = ConvertToZone(parsed, timeZone);
        return local.ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }

    public static string ClockText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        var candidate = value.Trim();
        return candidate.Length >= 5 ? candidate[..5] : candidate;
    }

    public static DateTimeOffset ConvertToZone(DateTimeOffset value, string? timeZone)
    {
        var zone = ResolveTimeZone(timeZone);
        return zone is null
            ? value.ToOffset(VietnamOffset)
            : TimeZoneInfo.ConvertTime(value, zone);
    }

    public static TimeZoneInfo? ResolveTimeZone(string? timeZone)
    {
        var candidate = string.IsNullOrWhiteSpace(timeZone) ? "Asia/Ho_Chi_Minh" : timeZone.Trim();
        try { return TimeZoneInfo.FindSystemTimeZoneById(candidate); }
        catch (TimeZoneNotFoundException)
        {
            if (string.Equals(candidate, "Asia/Ho_Chi_Minh", StringComparison.OrdinalIgnoreCase))
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
                catch (TimeZoneNotFoundException) { return null; }
            }
            return null;
        }
    }

    public static string ToIso(DateTime workDate, string clock, string? timeZone, bool allowNextDay)
    {
        if (!TimeOnly.TryParseExact(clock.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            throw new InvalidOperationException("Giờ làm việc phải theo định dạng HH:mm.");

        var local = new DateTime(
            workDate.Year,
            workDate.Month,
            workDate.Day,
            time.Hour,
            time.Minute,
            0,
            DateTimeKind.Unspecified);

        if (allowNextDay) local = local.AddDays(1);

        var zone = ResolveTimeZone(timeZone);
        if (zone is null)
            return new DateTimeOffset(local, VietnamOffset).UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

        return TimeZoneInfo.ConvertTimeToUtc(local, zone).ToString("O", CultureInfo.InvariantCulture);
    }

    public static DateTime BusinessToday()
    {
        var zone = ResolveTimeZone("Asia/Ho_Chi_Minh");
        return zone is null
            ? DateTimeOffset.UtcNow.ToOffset(VietnamOffset).Date
            : TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date;
    }
}

public sealed record WorkScheduleRowView(
    WorkScheduleData Source,
    string DateText,
    string EmployeeText,
    string PolicyText,
    string StatusText,
    string StartText,
    string EndText,
    string SourceText,
    string ReasonText,
    bool CanEdit);

public sealed record WorkScheduleEmployeeOption(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record WorkSchedulePolicyOption(string Id, string Label, string Timezone)
{
    public override string ToString() => Label;
}

public sealed record WorkScheduleKindOption(string Key, string Label)
{
    public override string ToString() => Label;
}

public sealed record ShiftTemplateRowView(
    WorkShiftTemplateData Source,
    string Code,
    string Name,
    string HoursText,
    string BreakText,
    string StatusText);

public sealed record WeekTemplateRowView(
    WorkWeekTemplateData Source,
    string Code,
    string Name,
    string SummaryText,
    string StatusText);

public sealed record CalendarDayRowView(
    CompanyCalendarDayData Source,
    string DateText,
    string Name,
    string KindText,
    string StatusText,
    bool CanEdit);
