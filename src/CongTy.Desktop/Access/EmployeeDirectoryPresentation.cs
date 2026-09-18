using System.Globalization;
using System.Text;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public static class EmployeeDirectoryPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string DateTimeText(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        return parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }

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
}

public sealed record EmployeeDirectoryRowView(
    EmployeeDirectoryData Source,
    string Code,
    string FullName,
    string JobTitleText,
    string BranchText,
    string PhoneText,
    string EmailText,
    string StatusText,
    string ToggleActionText,
    bool IsActive,
    string UpdatedAtText);

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