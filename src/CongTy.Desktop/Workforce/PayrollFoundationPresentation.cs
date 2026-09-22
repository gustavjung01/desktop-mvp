using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class PayrollFoundationPresentation
{
    public static string DateText(string? value) => WorkSchedulePresentation.DateText(value);

    public static string PeriodStatusLabel(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "AGGREGATING" => "Đang tổng hợp",
        "NEEDS_ACTION" => "Cần xử lý",
        "RECONCILED" => "Đã đối soát",
        "CLOSED" => "Đã chốt",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string CategoryLabel(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "INCOME" => "Thu nhập lương",
        "DEDUCTION" => "Khấu trừ",
        "REIMBURSEMENT" => "Hoàn chi phí",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string RecurrenceLabel(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "FIXED" => "Cố định",
        "PERIOD" => "Theo kỳ",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string InputModeLabel(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "AUTOMATIC" => "Tự động",
        "MANUAL" => "Nhập tay",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()
    };

    public static string MoneyText(string? value)
    {
        var raw = (value ?? "0").Trim();
        if (raw.Length == 0) raw = "0";

        var negative = raw.StartsWith("-", StringComparison.Ordinal);
        var unsigned = negative ? raw[1..] : raw;
        var parts = unsigned.Split('.', 2);
        var whole = parts[0].TrimStart('0');
        if (whole.Length == 0) whole = "0";

        var grouped = new List<char>();
        for (var i = 0; i < whole.Length; i++)
        {
            if (i > 0 && (whole.Length - i) % 3 == 0) grouped.Add('.');
            grouped.Add(whole[i]);
        }

        var fraction = parts.Length > 1 ? parts[1].TrimEnd('0') : string.Empty;
        return $"{(negative ? "-" : string.Empty)}{new string(grouped.ToArray())}{(fraction.Length > 0 ? "," + fraction : string.Empty)} ₫";
    }

    public static string EffectiveText(string from, string? to) =>
        $"{DateText(from)} – {DateText(to)}";

    public static string ScopeText(string? branchName) =>
        string.IsNullOrWhiteSpace(branchName) ? "Toàn Công Ty" : branchName.Trim();

    public static string PeriodText(string start, string end, string? branchName) =>
        $"{DateText(start)} – {DateText(end)} · {ScopeText(branchName)}";
}

public sealed record PayrollAttendanceSourceOption(PayrollAttendanceSourceData Source, string Label)
{
    public override string ToString() => Label;
}

public sealed record PayrollEmployeeOption(PayrollEmployeeData Source, string Label)
{
    public override string ToString() => Label;
}

public sealed record PayrollComponentOption(PayrollComponentTypeData Source, string Label)
{
    public override string ToString() => Label;
}

public sealed record PayrollPeriodRowView(
    PayrollPeriodData Source,
    string PeriodText,
    string SourceText,
    string StatusText,
    string CurrencyText);

public sealed record PayrollSalaryRowView(
    PayrollSalaryProfileData Source,
    string EmployeeText,
    string BranchText,
    string AmountText,
    string EffectiveText,
    string NoteText);

public sealed record PayrollFixedComponentRowView(
    PayrollFixedComponentData Source,
    string EmployeeText,
    string ComponentText,
    string CategoryText,
    string AmountText,
    string EffectiveText);

public sealed record PayrollComponentTypeRowView(
    PayrollComponentTypeData Source,
    string NameText,
    string CategoryText,
    string RecurrenceText,
    string InputText,
    string IncludedText,
    string EffectiveText);

public sealed record PayrollPeriodComponentRowView(
    PayrollPeriodComponentData Source,
    string EmployeeText,
    string ComponentText,
    string CategoryText,
    string AmountText,
    string NoteText);
