using System.Globalization;
using System.Text;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public static class EmployeeDirectoryPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static IReadOnlyList<EmployeeEmploymentTypeOption> EmploymentTypes { get; } =
    [
        new("PROBATION", "Thử việc"),
        new("PERMANENT", "Chính thức"),
        new("FIXED_TERM", "Hợp đồng xác định thời hạn"),
        new("PART_TIME", "Bán thời gian"),
        new("TEMPORARY", "Thời vụ"),
        new("OTHER", "Khác")
    ];

    public static string DateTimeText(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        return parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }

    public static string DateText(string? value)
    {
        var canonical = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        if (canonical.Length >= 10) canonical = canonical[..10];
        return DateOnly.TryParseExact(canonical, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("dd/MM/yyyy", Vietnamese)
            : string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
    }

    public static string EmploymentTypeLabel(string? value) =>
        EmploymentTypes.FirstOrDefault(item => string.Equals(item.Key, value, StringComparison.OrdinalIgnoreCase))?.Label
        ?? "Khác";

    public static string HistoryQualityLabel(string? value) =>
        value switch
        {
            "CONFIRMED" => "Đã xác nhận",
            "AUDIT_DERIVED" => "Khôi phục từ lịch sử hệ thống",
            _ => "Cần HR xác nhận"
        };

    public static bool HistoryNeedsConfirmation(string? value) =>
        !string.Equals(value, "CONFIRMED", StringComparison.Ordinal)
        && !string.Equals(value, "AUDIT_DERIVED", StringComparison.Ordinal);

    public static string NormalizeSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(character == 'đ' ? 'd' : character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string BranchLabel(EmployeeDirectoryBranchData branch) =>
        $"{branch.Code} · {branch.Name}";

    public static string AssignmentBranchLabel(EmployeeAssignmentData assignment, IReadOnlyDictionary<string, EmployeeDirectoryBranchData> branches)
    {
        if (!string.IsNullOrWhiteSpace(assignment.BranchCode) || !string.IsNullOrWhiteSpace(assignment.BranchName))
            return $"{assignment.BranchCode ?? "—"} · {assignment.BranchName ?? "Chưa rõ tên"}";
        if (string.IsNullOrWhiteSpace(assignment.BranchId)) return "Chưa phân công chi nhánh";
        return branches.TryGetValue(assignment.BranchId, out var branch)
            ? BranchLabel(branch)
            : "Chi nhánh lịch sử không còn trong danh mục hiện tại";
    }
}

public sealed record EmployeeDirectoryRowView(
    EmployeeDirectoryData Source,
    string Code,
    string FullName,
    string PositionText,
    string DepartmentText,
    string BranchText,
    string ManagerText,
    string PhoneText,
    string EmailText,
    string StatusText,
    string ToggleActionText,
    bool IsActive,
    string UpdatedAtText);

public sealed record EmployeeEmploymentHistoryRowView(
    string EffectiveFromText,
    string EffectiveToText,
    string EmploymentTypeText,
    string QualityText,
    string EndReasonText,
    bool NeedsConfirmation);

public sealed record EmployeeAssignmentHistoryRowView(
    string EffectiveFromText,
    string EffectiveToText,
    string BranchText,
    string DepartmentText,
    string PositionText,
    string ManagerText,
    string QualityText,
    string ReasonText,
    bool NeedsConfirmation);

public sealed record EmployeeEmploymentTypeOption(string Key, string Label)
{
    public override string ToString() => Label;
}

public sealed record EmployeeStatusFilterOption(string Key, string Label)
{
    public override string ToString() => Label;
}

public sealed record EmployeeBranchFilterOption(string Key, string Label)
{
    public override string ToString() => Label;
}

public sealed record EmployeeBranchOption(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record EmployeeDepartmentOption(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record EmployeePositionOption(string Id, string Label, string? DepartmentId, string Name)
{
    public override string ToString() => Label;
}

public sealed record EmployeeManagerOption(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record EmployeeDepartmentRowView(
    HrDepartmentData Source,
    string Code,
    string Name,
    string ParentText,
    string StatusText,
    string ToggleActionText);

public sealed record EmployeePositionRowView(
    HrPositionData Source,
    string Code,
    string Name,
    string DepartmentText,
    string StatusText,
    string ToggleActionText);
