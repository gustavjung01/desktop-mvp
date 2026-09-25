using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class LeavePresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string DateText(string? value) => WorkSchedulePresentation.DateText(value);

    public static string StatusLabel(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "SUBMITTED" => "Chờ duyệt",
        "APPROVED" => "Đã duyệt",
        "REJECTED" => "Từ chối",
        "CANCELLED" => "Đã hủy",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string DayPartLabel(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "FULL_DAY" => "Cả ngày",
        "FIRST_HALF" => "Nửa ca đầu",
        "SECOND_HALF" => "Nửa ca sau",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string EntryTypeLabel(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "OPENING_GRANT" => "Cấp đầu kỳ",
        "ACCRUAL" => "Phát sinh định kỳ",
        "USAGE" => "Sử dụng",
        "ADJUSTMENT" => "Điều chỉnh",
        "CARRY_OVER" => "Chuyển năm",
        "EXPIRY" => "Hết hạn",
        "COMPENSATORY" => "Nghỉ bù",
        "REVERSAL" => "Hoàn phép",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string DaysText(decimal days) =>
        days.ToString("0.##", Vietnamese) + " ngày";

    public static string SignedDaysText(decimal days) =>
        (days > 0 ? "+" : string.Empty) + days.ToString("0.##", Vietnamese) + " ngày";

    public static string RequestPeriod(LeaveRequestData request) =>
        string.Equals(request.DateFrom, request.DateTo, StringComparison.Ordinal)
            ? DateText(request.DateFrom)
            : $"{DateText(request.DateFrom)} – {DateText(request.DateTo)}";

    public static string RequestSourceLabel(string? value) =>
        string.Equals(value, "MANUAL_PAPER", StringComparison.OrdinalIgnoreCase)
            ? "Phiếu giấy / nhập thủ công"
            : "Phiếu điện tử";

    public static string ReasonDetails(LeaveRequestData request)
    {
        var details = new List<string> { RequestSourceLabel(request.RequestSource) };
        if (string.Equals(request.RequestSource, "MANUAL_PAPER", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(request.ManualApproverName))
            details.Add($"Duyệt trên giấy: {request.ManualApproverName}");
        if (!string.IsNullOrWhiteSpace(request.AttachmentUrl)
            || !string.IsNullOrWhiteSpace(request.AttachmentReference))
            details.Add("Có chứng từ");
        return $"{request.Reason}\n{string.Join(" · ", details)}";
    }

    public static string LeaveTypeBadges(LeaveTypeData type)
    {
        var values = new List<string>
        {
            type.IsPaid ? "Hưởng lương" : "Không lương",
            type.CountsAsWorkday ? "Tính ngày công" : "Không tính ngày công",
            type.RequiresApproval ? "Cần duyệt" : "Tự động duyệt"
        };
        if (type.AllowsHalfDay) values.Add("Có nửa ngày");
        if (type.RequiresAttachment) values.Add("Cần chứng từ");
        if (type.TracksBalance) values.Add("Theo dõi số dư");
        if (type.AllowNegativeBalance) values.Add("Cho phép âm");
        if (!type.IsActive) values.Add("Ngừng áp dụng");
        return string.Join(" · ", values);
    }
}

public sealed record LeaveOption(string Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record LeaveRequestRowView(
    LeaveRequestData Source,
    string EmployeeText,
    string LeaveTypeText,
    string PeriodText,
    string DayPartText,
    string StatusText,
    string ReasonText,
    string ReviewText,
    bool HasAttachment,
    bool CanReview,
    bool CanCancel);

public sealed record LeaveBalanceRowView(
    LeaveBalanceData Source,
    string EmployeeText,
    string LeaveTypeText,
    string BalanceText,
    string LastActivityText,
    string PolicyText);

public sealed record LeaveBalanceEntryRowView(
    LeaveBalanceEntryData Source,
    string EffectiveDateText,
    string EmployeeText,
    string LeaveTypeText,
    string EntryTypeText,
    string QuantityText,
    string ReasonText);

public sealed record LeaveTypeRowView(
    LeaveTypeData Source,
    string Code,
    string Name,
    string BadgesText,
    string StatusText,
    bool CanEdit);
