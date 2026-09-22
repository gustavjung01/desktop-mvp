using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record PayrollClosePeriodSnapshotData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
    [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; init; }
}

public sealed record PayrollCloseTotalsData
{
    [JsonPropertyName("employeeCount")] public int EmployeeCount { get; init; }
    [JsonPropertyName("grossIncome")] public string GrossIncome { get; init; } = "0.00";
    [JsonPropertyName("reimbursementTotal")] public string ReimbursementTotal { get; init; } = "0.00";
    [JsonPropertyName("deductionTotal")] public string DeductionTotal { get; init; } = "0.00";
    [JsonPropertyName("netPay")] public string NetPay { get; init; } = "0.00";
}

public sealed record PayrollCloseSnapshotPayloadData
{
    [JsonPropertyName("period")] public PayrollClosePeriodSnapshotData Period { get; init; } = new();
    [JsonPropertyName("totals")] public PayrollCloseTotalsData Totals { get; init; } = new();
}

public sealed record PayrollCloseSnapshotData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("payroll_period_id")] public string PayrollPeriodId { get; init; } = string.Empty;
    [JsonPropertyName("calculation_revision")] public int CalculationRevision { get; init; }
    [JsonPropertyName("calculation_fingerprint")] public string? CalculationFingerprint { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("snapshot")] public PayrollCloseSnapshotPayloadData Snapshot { get; init; } = new();
}

public sealed record PayrollPayslipEmployeeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
    [JsonPropertyName("branchCode")] public string? BranchCode { get; init; }
    [JsonPropertyName("branchName")] public string? BranchName { get; init; }
}

public sealed record PayrollPayslipPayData
{
    [JsonPropertyName("employeeId")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("employeeCode")] public string EmployeeCode { get; init; } = string.Empty;
    [JsonPropertyName("employeeName")] public string EmployeeName { get; init; } = string.Empty;
    [JsonPropertyName("branchName")] public string? BranchName { get; init; }
    [JsonPropertyName("standardWorkDays")] public int StandardWorkDays { get; init; }
    [JsonPropertyName("payableWorkDays")] public int PayableWorkDays { get; init; }
    [JsonPropertyName("unpaidLeaveDays")] public int UnpaidLeaveDays { get; init; }
    [JsonPropertyName("confirmedOvertimeMinutes")] public int ConfirmedOvertimeMinutes { get; init; }
    [JsonPropertyName("monthlySalary")] public string MonthlySalary { get; init; } = "0.00";
    [JsonPropertyName("salaryAmount")] public string SalaryAmount { get; init; } = "0.00";
    [JsonPropertyName("incomeTotal")] public string IncomeTotal { get; init; } = "0.00";
    [JsonPropertyName("reimbursementTotal")] public string ReimbursementTotal { get; init; } = "0.00";
    [JsonPropertyName("deductionTotal")] public string DeductionTotal { get; init; } = "0.00";
    [JsonPropertyName("grossIncome")] public string GrossIncome { get; init; } = "0.00";
    [JsonPropertyName("netPay")] public string NetPay { get; init; } = "0.00";
}

public sealed record PayrollPayslipAdjustmentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("componentTypeId")] public string? ComponentTypeId { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; init; } = string.Empty;
    [JsonPropertyName("direction")] public string Direction { get; init; } = "ADD";
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0.00";
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("createdBy")] public string? CreatedBy { get; init; }
    [JsonPropertyName("requestId")] public string? RequestId { get; init; }
}

public sealed record PayrollPayslipSnapshotPayloadData
{
    [JsonPropertyName("period")] public PayrollClosePeriodSnapshotData Period { get; init; } = new();
    [JsonPropertyName("employee")] public PayrollPayslipEmployeeData Employee { get; init; } = new();
    [JsonPropertyName("pay")] public PayrollPayslipPayData Pay { get; init; } = new();
    [JsonPropertyName("adjustments")] public PayrollPayslipAdjustmentData[] Adjustments { get; init; } = [];
}

public sealed record PayrollPayslipSnapshotData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("payroll_period_id")] public string PayrollPeriodId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("revision")] public int Revision { get; init; }
    [JsonPropertyName("source_kind")] public string SourceKind { get; init; } = "CLOSE";
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("snapshot")] public PayrollPayslipSnapshotPayloadData Snapshot { get; init; } = new();
}

public sealed record PayrollAdjustmentRecordData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("component_name")] public string ComponentName { get; init; } = string.Empty;
    [JsonPropertyName("direction")] public string Direction { get; init; } = "ADD";
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0.00";
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("resulting_revision")] public int ResultingRevision { get; init; }
    [JsonPropertyName("created_by")] public string CreatedBy { get; init; } = string.Empty;
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
}

public sealed record PayrollCloseoutData
{
    [JsonPropertyName("closeSnapshot")] public PayrollCloseSnapshotData? CloseSnapshot { get; init; }
    [JsonPropertyName("payslips")] public PayrollPayslipSnapshotData[] Payslips { get; init; } = [];
    [JsonPropertyName("payslipHistory")] public PayrollPayslipSnapshotData[] PayslipHistory { get; init; } = [];
    [JsonPropertyName("adjustments")] public PayrollAdjustmentRecordData[] Adjustments { get; init; } = [];
    [JsonPropertyName("history")] public PayrollCloseSnapshotData[] History { get; init; } = [];
}

public sealed record ClosePayrollRequest
{
    [JsonPropertyName("command")] public string Command { get; init; } = "CLOSE";
    [JsonPropertyName("payrollPeriodId")] public string PayrollPeriodId { get; init; } = string.Empty;
}

public sealed record AdjustPayrollRequest
{
    [JsonPropertyName("command")] public string Command { get; init; } = "ADJUST";
    [JsonPropertyName("payrollPeriodId")] public string PayrollPeriodId { get; init; } = string.Empty;
    [JsonPropertyName("employeeId")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("componentTypeId")] public string ComponentTypeId { get; init; } = string.Empty;
    [JsonPropertyName("direction")] public string Direction { get; init; } = "ADD";
    [JsonPropertyName("amount")] public string Amount { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
}

public sealed record ClosePayrollMutationData
{
    [JsonPropertyName("period")] public PayrollPeriodData Period { get; init; } = new();
    [JsonPropertyName("closeSnapshot")] public PayrollCloseSnapshotData CloseSnapshot { get; init; } = new();
}

public sealed record AdjustPayrollMutationData
{
    [JsonPropertyName("adjustment")] public PayrollAdjustmentRecordData Adjustment { get; init; } = new();
    [JsonPropertyName("payslip")] public PayrollPayslipSnapshotData Payslip { get; init; } = new();
}
