using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public static class PayrollCloseoutPresentation
{
    public static PayrollPayslipRowView ToPayslipRow(PayrollPayslipSnapshotData item) =>
        new(
            item,
            $"{item.Snapshot.Employee.Code} · {item.Snapshot.Employee.Name}",
            PayrollFoundationPresentation.ScopeText(item.Snapshot.Employee.BranchName),
            PayrollFoundationPresentation.MoneyText(item.Snapshot.Pay.GrossIncome),
            PayrollFoundationPresentation.MoneyText(item.Snapshot.Pay.ReimbursementTotal),
            PayrollFoundationPresentation.MoneyText(item.Snapshot.Pay.DeductionTotal),
            PayrollFoundationPresentation.MoneyText(item.Snapshot.Pay.NetPay),
            $"Lần {item.Revision}",
            PeriodText(item.Snapshot.Period),
            $"{item.Snapshot.Pay.PayableWorkDays}/{item.Snapshot.Pay.StandardWorkDays} ngày · {PayrollAggregationPresentation.OvertimeText(item.Snapshot.Pay.ConfirmedOvertimeMinutes)}",
            PayrollFoundationPresentation.MoneyText(item.Snapshot.Pay.SalaryAmount));

    public static PayrollCloseHistoryRowView ToHistoryRow(PayrollCloseSnapshotData item) =>
        new(
            item,
            PeriodText(item.Snapshot.Period),
            $"{item.Snapshot.Totals.EmployeeCount} nhân sự",
            PayrollFoundationPresentation.MoneyText(item.Snapshot.Totals.GrossIncome),
            PayrollFoundationPresentation.MoneyText(item.Snapshot.Totals.DeductionTotal),
            PayrollFoundationPresentation.MoneyText(item.Snapshot.Totals.NetPay));

    public static PayrollPayslipRevisionRowView ToRevisionRow(PayrollPayslipSnapshotData item) =>
        new(
            item,
            $"Lần {item.Revision}",
            string.Equals(item.SourceKind, "CLOSE", StringComparison.Ordinal) ? "Chốt kỳ" : "Điều chỉnh",
            PayrollFoundationPresentation.MoneyText(item.Snapshot.Pay.NetPay),
            DateTimeText(item.CreatedAt));

    public static PayrollPayslipAdjustmentRowView ToAdjustmentRow(PayrollPayslipAdjustmentData item)
    {
        var direction = string.Equals(item.Direction, "REVERSE", StringComparison.Ordinal)
            ? "Ghi giảm / hoàn lại"
            : "Ghi thêm";
        var amount = PayrollFoundationPresentation.MoneyText(item.Amount);
        return new(
            item,
            item.Name,
            direction,
            amount,
            item.Reason,
            $"{item.Name} · {direction} · {amount} · {item.Reason}");
    }

    public static string PeriodText(PayrollClosePeriodSnapshotData period) =>
        $"{PayrollFoundationPresentation.DateText(period.From)} – {PayrollFoundationPresentation.DateText(period.To)}";

    public static string DateTimeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return value.Trim();

        return parsed
            .ToOffset(TimeSpan.FromHours(7))
            .ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
    }
}

public sealed record PayrollPayslipRowView(
    PayrollPayslipSnapshotData Source,
    string EmployeeText,
    string BranchText,
    string GrossIncomeText,
    string ReimbursementText,
    string DeductionText,
    string NetPayText,
    string RevisionText,
    string PeriodText,
    string WorkText,
    string SalaryText);

public sealed record PayrollCloseHistoryRowView(
    PayrollCloseSnapshotData Source,
    string PeriodText,
    string EmployeeCountText,
    string GrossIncomeText,
    string DeductionText,
    string NetPayText);

public sealed record PayrollPayslipRevisionRowView(
    PayrollPayslipSnapshotData Source,
    string RevisionText,
    string SourceText,
    string NetPayText,
    string CreatedText);

public sealed record PayrollPayslipAdjustmentRowView(
    PayrollPayslipAdjustmentData Source,
    string NameText,
    string DirectionText,
    string AmountText,
    string ReasonText,
    string DisplayText);
