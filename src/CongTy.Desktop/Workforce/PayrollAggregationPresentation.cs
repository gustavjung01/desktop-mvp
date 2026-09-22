using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class PayrollAggregationPresentation
{
    private static readonly IReadOnlyDictionary<string, string> IssueLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["emptyPayrollPeriod"] = "Kỳ lương chưa có nhân sự từ bản chốt công.",
            ["missingSalaryProfiles"] = "nhân sự chưa có mức lương phủ đủ kỳ",
            ["salaryCoverageConflicts"] = "nhân sự có thay đổi/mâu thuẫn mức lương trong kỳ cần xử lý",
            ["fixedComponentCoverageConflicts"] = "khoản cố định thay đổi giữa kỳ chưa có quy tắc chia kỳ",
            ["zeroStandardWorkdays"] = "nhân sự có mức lương nhưng kỳ không có công chuẩn",
            ["sourceChanged"] = "Dữ liệu nguồn đã thay đổi sau lần tổng hợp trước.",
            ["confirmedOvertimeEmployees"] = "nhân sự có giờ tăng ca đã xác nhận; cần kiểm tra khoản tiền tăng ca trước khi đối soát",
            ["incompleteAttendanceDays"] = "ngày công chưa hoàn chỉnh cần kiểm tra",
            ["unexcusedAbsenceDays"] = "ngày vắng không phép cần kiểm tra",
            ["violationDays"] = "ngày có vi phạm công cần kiểm tra"
        };

    public static string IssueLabel(string key) =>
        IssueLabels.TryGetValue(key, out var label) ? label : key;

    public static string OvertimeText(int minutes)
    {
        var hours = minutes / 60m;
        return $"{hours.ToString("0.##", CultureInfo.GetCultureInfo("vi-VN"))} giờ OT";
    }

    public static PayrollCalculationRowView ToRowView(PayrollCalculationRowData row)
    {
        var components = row.FixedComponents.Concat(row.PeriodComponents).ToArray();
        return new PayrollCalculationRowView(
            row,
            $"{row.EmployeeCode} · {row.EmployeeName}",
            PayrollFoundationPresentation.ScopeText(row.BranchName),
            PayrollFoundationPresentation.MoneyText(row.SalaryAmount),
            $"{row.PayableWorkDays}/{row.StandardWorkDays} ngày · {OvertimeText(row.ConfirmedOvertimeMinutes)}",
            PayrollFoundationPresentation.MoneyText(row.IncomeTotal),
            PayrollFoundationPresentation.MoneyText(row.ReimbursementTotal),
            PayrollFoundationPresentation.MoneyText(row.DeductionTotal),
            PayrollFoundationPresentation.MoneyText(row.NetPay),
            $"Mức lương tháng: {PayrollFoundationPresentation.MoneyText(row.MonthlySalary)}",
            $"Lương theo công được tính: {PayrollFoundationPresentation.MoneyText(row.SalaryAmount)}",
            $"Công chuẩn {row.StandardWorkDays} ngày · Công được tính {row.PayableWorkDays} ngày",
            $"Nghỉ không lương {row.UnpaidLeaveDays} ngày · OT đã xác nhận {OvertimeText(row.ConfirmedOvertimeMinutes)}",
            $"Tổng thu nhập lương {PayrollFoundationPresentation.MoneyText(row.GrossIncome)} · Hoàn chi {PayrollFoundationPresentation.MoneyText(row.ReimbursementTotal)} · Khấu trừ {PayrollFoundationPresentation.MoneyText(row.DeductionTotal)} · Thực nhận {PayrollFoundationPresentation.MoneyText(row.NetPay)}",
            ToComponents(components, "INCOME"),
            ToComponents(components, "REIMBURSEMENT"),
            ToComponents(components, "DEDUCTION"));
    }

    public static PayrollIssueRowView[] ToIssueRows(IReadOnlyDictionary<string, int?> values, string severity) =>
        values
            .Where(item => (item.Value ?? 0) > 0)
            .Select(item => new PayrollIssueRowView(
                item.Key,
                severity,
                item.Value ?? 0,
                $"{IssueLabel(item.Key)}: {item.Value ?? 0}"))
            .ToArray();

    private static PayrollComponentLineView[] ToComponents(
        IEnumerable<PayrollComponentLineData> items,
        string category) =>
        items
            .Where(item => string.Equals(item.Category, category, StringComparison.Ordinal))
            .Select(item =>
            {
                var amount = item.AppliedAmount ?? item.Amount ?? "0.00";
                return new PayrollComponentLineView(
                    item.Name,
                    PayrollFoundationPresentation.MoneyText(amount),
                    $"{item.Name}: {PayrollFoundationPresentation.MoneyText(amount)}");
            })
            .ToArray();
}

public sealed record PayrollComponentLineView(
    string NameText,
    string AmountText,
    string DisplayText);

public sealed record PayrollIssueRowView(
    string Key,
    string Severity,
    int Count,
    string DisplayText);

public sealed record PayrollCalculationRowView(
    PayrollCalculationRowData Source,
    string EmployeeText,
    string BranchText,
    string SalaryAmountText,
    string WorkOvertimeText,
    string IncomeTotalText,
    string ReimbursementTotalText,
    string DeductionTotalText,
    string NetPayText,
    string MonthlySalaryText,
    string SalaryAmountDetailText,
    string WorkdayDetailText,
    string LeaveOvertimeDetailText,
    string NetBreakdownText,
    IReadOnlyList<PayrollComponentLineView> IncomeComponents,
    IReadOnlyList<PayrollComponentLineView> ReimbursementComponents,
    IReadOnlyList<PayrollComponentLineView> DeductionComponents);
