using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record PayrollIssueSummaryData
{
    [JsonPropertyName("blockers")] public Dictionary<string, int?> Blockers { get; init; } = [];
    [JsonPropertyName("warnings")] public Dictionary<string, int?> Warnings { get; init; } = [];
}

public sealed record PayrollComponentLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; init; } = "INCOME";
    [JsonPropertyName("appliedAmount")] public string? AppliedAmount { get; init; }
    [JsonPropertyName("amount")] public string? Amount { get; init; }
    [JsonPropertyName("originalAmount")] public string? OriginalAmount { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("proratedByWorkdays")] public bool ProratedByWorkdays { get; init; }
}

public sealed record PayrollCalculationRowData
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
    [JsonPropertyName("fixedComponents")] public PayrollComponentLineData[] FixedComponents { get; init; } = [];
    [JsonPropertyName("periodComponents")] public PayrollComponentLineData[] PeriodComponents { get; init; } = [];
}

public sealed record PayrollCalculationTotalsData
{
    [JsonPropertyName("employeeCount")] public int EmployeeCount { get; init; }
    [JsonPropertyName("salaryAmount")] public string SalaryAmount { get; init; } = "0.00";
    [JsonPropertyName("incomeTotal")] public string IncomeTotal { get; init; } = "0.00";
    [JsonPropertyName("reimbursementTotal")] public string ReimbursementTotal { get; init; } = "0.00";
    [JsonPropertyName("deductionTotal")] public string DeductionTotal { get; init; } = "0.00";
    [JsonPropertyName("grossIncome")] public string GrossIncome { get; init; } = "0.00";
    [JsonPropertyName("netPay")] public string NetPay { get; init; } = "0.00";
}

public sealed record PayrollCalculationSnapshotData
{
    [JsonPropertyName("contractVersion")] public int ContractVersion { get; init; }
    [JsonPropertyName("totals")] public PayrollCalculationTotalsData Totals { get; init; } = new();
    [JsonPropertyName("rows")] public PayrollCalculationRowData[] Rows { get; init; } = [];
}

public sealed record PayrollCalculationData
{
    [JsonPropertyName("revision")] public int Revision { get; init; }
    [JsonPropertyName("sourceFingerprint")] public string SourceFingerprint { get; init; } = string.Empty;
    [JsonPropertyName("issueSummary")] public PayrollIssueSummaryData IssueSummary { get; init; } = new();
    [JsonPropertyName("snapshot")] public PayrollCalculationSnapshotData Snapshot { get; init; } = new();
}

public sealed record PayrollAggregationMutationData
{
    [JsonPropertyName("period")] public PayrollPeriodData Period { get; init; } = new();
    [JsonPropertyName("calculation")] public PayrollCalculationData Calculation { get; init; } = new();
    [JsonPropertyName("reused")] public bool Reused { get; init; }
}

public sealed record AggregatePayrollRequest
{
    [JsonPropertyName("command")] public string Command { get; init; } = "AGGREGATE";
    [JsonPropertyName("payrollPeriodId")] public string PayrollPeriodId { get; init; } = string.Empty;
}

public sealed record ReconcilePayrollRequest
{
    [JsonPropertyName("command")] public string Command { get; init; } = "RECONCILE";
    [JsonPropertyName("payrollPeriodId")] public string PayrollPeriodId { get; init; } = string.Empty;
    [JsonPropertyName("acknowledgeWarnings")] public bool AcknowledgeWarnings { get; init; }
    [JsonPropertyName("note")] public string Note { get; init; } = string.Empty;
}
