using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record PayrollAttendanceSourceData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("scope_key")] public string ScopeKey { get; init; } = string.Empty;
    [JsonPropertyName("period_start")] public string PeriodStart { get; init; } = string.Empty;
    [JsonPropertyName("period_end")] public string PeriodEnd { get; init; } = string.Empty;
    [JsonPropertyName("revision")] public int Revision { get; init; }
    [JsonPropertyName("source_fingerprint")] public string SourceFingerprint { get; init; } = string.Empty;
    [JsonPropertyName("branch_code")] public string? BranchCode { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
}

public sealed record PayrollPeriodData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("attendance_period_id")] public string AttendancePeriodId { get; init; } = string.Empty;
    [JsonPropertyName("attendance_revision")] public int AttendanceRevision { get; init; }
    [JsonPropertyName("attendance_source_fingerprint")] public string AttendanceSourceFingerprint { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("scope_key")] public string ScopeKey { get; init; } = string.Empty;
    [JsonPropertyName("period_start")] public string PeriodStart { get; init; } = string.Empty;
    [JsonPropertyName("period_end")] public string PeriodEnd { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = "AGGREGATING";
    [JsonPropertyName("currency_code")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("branch_code")] public string? BranchCode { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
}

public sealed record PayrollEmployeeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("full_name")] public string FullName { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("branch_code")] public string? BranchCode { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
}

public sealed record PayrollComponentTypeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; init; } = "INCOME";
    [JsonPropertyName("recurrence")] public string Recurrence { get; init; } = "PERIOD";
    [JsonPropertyName("input_mode")] public string InputMode { get; init; } = "MANUAL";
    [JsonPropertyName("prorate_by_workdays")] public bool ProrateByWorkdays { get; init; }
    [JsonPropertyName("include_in_gross")] public bool IncludeInGross { get; init; }
    [JsonPropertyName("include_in_net")] public bool IncludeInNet { get; init; }
    [JsonPropertyName("effective_from")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("effective_to")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record PayrollSalaryProfileData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("employee_code")] public string EmployeeCode { get; init; } = string.Empty;
    [JsonPropertyName("employee_name")] public string EmployeeName { get; init; } = string.Empty;
    [JsonPropertyName("monthly_salary")] public string MonthlySalary { get; init; } = "0.00";
    [JsonPropertyName("currency_code")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("effective_from")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("effective_to")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
}

public sealed record PayrollFixedComponentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("employee_code")] public string EmployeeCode { get; init; } = string.Empty;
    [JsonPropertyName("employee_name")] public string EmployeeName { get; init; } = string.Empty;
    [JsonPropertyName("component_type_id")] public string ComponentTypeId { get; init; } = string.Empty;
    [JsonPropertyName("component_code")] public string ComponentCode { get; init; } = string.Empty;
    [JsonPropertyName("component_name")] public string ComponentName { get; init; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; init; } = "INCOME";
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0.00";
    [JsonPropertyName("effective_from")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("effective_to")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
}

public sealed record PayrollPeriodComponentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("payroll_period_id")] public string PayrollPeriodId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("employee_code")] public string EmployeeCode { get; init; } = string.Empty;
    [JsonPropertyName("employee_name")] public string EmployeeName { get; init; } = string.Empty;
    [JsonPropertyName("component_type_id")] public string ComponentTypeId { get; init; } = string.Empty;
    [JsonPropertyName("component_code")] public string ComponentCode { get; init; } = string.Empty;
    [JsonPropertyName("component_name")] public string ComponentName { get; init; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; init; } = "INCOME";
    [JsonPropertyName("amount")] public string Amount { get; init; } = "0.00";
    [JsonPropertyName("note")] public string Note { get; init; } = string.Empty;
    [JsonPropertyName("source")] public string Source { get; init; } = string.Empty;
    [JsonPropertyName("source_reference")] public string? SourceReference { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
}

public sealed record PayrollCapabilitiesData
{
    [JsonPropertyName("canManage")] public bool CanManage { get; init; }
    [JsonPropertyName("canClose")] public bool CanClose { get; init; }
    [JsonPropertyName("canAdjust")] public bool CanAdjust { get; init; }
    [JsonPropertyName("canExport")] public bool CanExport { get; init; }
}

public sealed record PayrollFoundationData
{
    [JsonPropertyName("attendanceSources")] public PayrollAttendanceSourceData[] AttendanceSources { get; init; } = [];
    [JsonPropertyName("periods")] public PayrollPeriodData[] Periods { get; init; } = [];
    [JsonPropertyName("selectedPeriod")] public PayrollPeriodData? SelectedPeriod { get; init; }
    [JsonPropertyName("employees")] public PayrollEmployeeData[] Employees { get; init; } = [];
    [JsonPropertyName("periodEmployees")] public PayrollEmployeeData[] PeriodEmployees { get; init; } = [];
    [JsonPropertyName("componentTypes")] public PayrollComponentTypeData[] ComponentTypes { get; init; } = [];
    [JsonPropertyName("salaryProfiles")] public PayrollSalaryProfileData[] SalaryProfiles { get; init; } = [];
    [JsonPropertyName("fixedComponents")] public PayrollFixedComponentData[] FixedComponents { get; init; } = [];
    [JsonPropertyName("periodComponents")] public PayrollPeriodComponentData[] PeriodComponents { get; init; } = [];
    [JsonPropertyName("calculation")] public PayrollCalculationData? Calculation { get; init; }
    [JsonPropertyName("closeout")] public PayrollCloseoutData Closeout { get; init; } = new();
    [JsonPropertyName("asOfDate")] public string AsOfDate { get; init; } = string.Empty;
    [JsonPropertyName("capabilities")] public PayrollCapabilitiesData Capabilities { get; init; } = new();
}

public sealed record CreatePayrollPeriodRequest
{
    [JsonPropertyName("command")] public string Command { get; init; } = "CREATE_PERIOD";
    [JsonPropertyName("attendancePeriodId")] public string AttendancePeriodId { get; init; } = string.Empty;
}

public sealed record SavePayrollSalaryRequest
{
    [JsonPropertyName("command")] public string Command { get; init; } = "SAVE_SALARY";
    [JsonPropertyName("employeeId")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("monthlySalary")] public string MonthlySalary { get; init; } = string.Empty;
    [JsonPropertyName("effectiveFrom")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed record CreatePayrollComponentTypeRequest
{
    [JsonPropertyName("command")] public string Command { get; init; } = "CREATE_COMPONENT_TYPE";
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; init; } = "INCOME";
    [JsonPropertyName("recurrence")] public string Recurrence { get; init; } = "PERIOD";
    [JsonPropertyName("inputMode")] public string InputMode { get; init; } = "MANUAL";
    [JsonPropertyName("prorateByWorkdays")] public bool ProrateByWorkdays { get; init; }
    [JsonPropertyName("includeInGross")] public bool IncludeInGross { get; init; }
    [JsonPropertyName("includeInNet")] public bool IncludeInNet { get; init; } = true;
    [JsonPropertyName("effectiveFrom")] public string EffectiveFrom { get; init; } = string.Empty;
}

public sealed record AssignPayrollFixedComponentRequest
{
    [JsonPropertyName("command")] public string Command { get; init; } = "ASSIGN_FIXED_COMPONENT";
    [JsonPropertyName("employeeId")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("componentTypeId")] public string ComponentTypeId { get; init; } = string.Empty;
    [JsonPropertyName("amount")] public string Amount { get; init; } = string.Empty;
    [JsonPropertyName("effectiveFrom")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed record AddPayrollPeriodComponentRequest
{
    [JsonPropertyName("command")] public string Command { get; init; } = "ADD_PERIOD_COMPONENT";
    [JsonPropertyName("payrollPeriodId")] public string PayrollPeriodId { get; init; } = string.Empty;
    [JsonPropertyName("employeeId")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("componentTypeId")] public string ComponentTypeId { get; init; } = string.Empty;
    [JsonPropertyName("amount")] public string Amount { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string Note { get; init; } = string.Empty;
}
