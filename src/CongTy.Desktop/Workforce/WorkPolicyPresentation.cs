using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class WorkPolicyPresentation
{
    private static readonly IReadOnlyDictionary<string, string> TimeModeLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FIXED"] = "Giờ cố định",
            ["SHIFT"] = "Theo ca",
            ["FLEXIBLE"] = "Linh hoạt",
            ["NO_ATTENDANCE"] = "Không bắt buộc chấm công",
        };

    private static readonly IReadOnlyDictionary<string, string> AttendanceLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["QR"] = "Mã QR",
            ["FACE"] = "Quét khuôn mặt",
            ["MANUAL"] = "Chấm công trực tiếp",
            ["QR_FACE"] = "QR + khuôn mặt",
            ["BOTH"] = "QR + trực tiếp",
            ["FACE_MANUAL"] = "Khuôn mặt + trực tiếp",
            ["ALL"] = "QR + khuôn mặt + trực tiếp",
            ["NONE"] = "Không chấm công",
        };

    private static readonly IReadOnlyDictionary<string, string> BasisLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TIME"] = "Theo giờ vào và giờ ra",
            ["PRESENCE"] = "Chỉ xác nhận có mặt",
            ["NONE"] = "Không chấm công",
        };

    public static WorkPolicyRowView Row(WorkPolicyDetailData policy, bool canEdit) =>
        new(
            policy,
            policy.Code,
            policy.Name,
            $"v{policy.Version}",
            Label(TimeModeLabels, policy.TimeMode),
            HoursText(policy),
            WorkingDaysText(policy.WorkingDays),
            $"{Label(AttendanceLabels, policy.AttendanceMethod)} · {Label(BasisLabels, policy.AttendanceBasis)}",
            EffectiveText(policy),
            canEdit);

    public static string ResolveAttendanceMethod(bool qr, bool face, bool manual)
    {
        if (qr && face && manual) return "ALL";
        if (qr && face) return "QR_FACE";
        if (qr && manual) return "BOTH";
        if (face && manual) return "FACE_MANUAL";
        if (qr) return "QR";
        if (face) return "FACE";
        if (manual) return "MANUAL";
        return "NONE";
    }

    public static (bool Qr, bool Face, bool Manual) AttendanceChoices(string? method) =>
        (method ?? string.Empty).ToUpperInvariant() switch
        {
            "ALL" => (true, true, true),
            "QR_FACE" => (true, true, false),
            "BOTH" => (true, false, true),
            "FACE_MANUAL" => (false, true, true),
            "QR" => (true, false, false),
            "FACE" => (false, true, false),
            "MANUAL" => (false, false, true),
            _ => (false, false, false),
        };

    public static string TimeModeLabel(string value) => Label(TimeModeLabels, value);
    public static string AttendanceBasisLabel(string value) => Label(BasisLabels, value);

    private static string Label(IReadOnlyDictionary<string, string> labels, string? value) =>
        !string.IsNullOrWhiteSpace(value) && labels.TryGetValue(value.Trim(), out var label)
            ? label
            : string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static string HoursText(WorkPolicyDetailData policy)
    {
        if (string.Equals(policy.TimeMode, "NO_ATTENDANCE", StringComparison.OrdinalIgnoreCase))
            return "Không yêu cầu chấm công";
        if (!string.Equals(policy.TimeMode, "FIXED", StringComparison.OrdinalIgnoreCase))
            return TimeModeLabel(policy.TimeMode);
        return $"{WorkSchedulePresentation.ClockText(policy.FixedStartTime)}–{WorkSchedulePresentation.ClockText(policy.FixedEndTime)} · nghỉ {policy.BreakMinutes} phút";
    }

    private static string WorkingDaysText(IEnumerable<int> workingDays)
    {
        var labels = new Dictionary<int, string>
        {
            [0] = "CN", [1] = "T2", [2] = "T3", [3] = "T4", [4] = "T5", [5] = "T6", [6] = "T7"
        };
        var days = workingDays.Distinct().OrderBy(item => item).Select(item => labels.GetValueOrDefault(item, item.ToString())).ToArray();
        return days.Length == 0 ? "—" : string.Join(", ", days);
    }

    private static string EffectiveText(WorkPolicyDetailData policy)
    {
        var from = WorkSchedulePresentation.DateText(policy.EffectiveFrom);
        var to = string.IsNullOrWhiteSpace(policy.EffectiveTo)
            ? "nay"
            : WorkSchedulePresentation.DateText(policy.EffectiveTo);
        return $"{from} → {to}";
    }
}

public sealed record WorkPolicyRowView(
    WorkPolicyDetailData Source,
    string CodeText,
    string NameText,
    string VersionText,
    string TimeModeText,
    string HoursText,
    string WorkingDaysText,
    string AttendanceText,
    string EffectiveText,
    bool CanEdit);

public sealed record WorkPolicyOption(string Key, string Label)
{
    public override string ToString() => Label;
}
