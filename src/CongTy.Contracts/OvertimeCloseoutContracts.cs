using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record OvertimeRequestData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("work_date")] public string WorkDate { get; init; } = string.Empty;
    [JsonPropertyName("requested_minutes")] public int RequestedMinutes { get; init; }
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("policy_id_snapshot")] public string PolicyIdSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("policy_code_snapshot")] public string PolicyCodeSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("policy_version_snapshot")] public int PolicyVersionSnapshot { get; init; }
    [JsonPropertyName("overtime_requires_approval_snapshot")] public bool OvertimeRequiresApprovalSnapshot { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "SUBMITTED";
    [JsonPropertyName("requested_by_actor_id")] public string RequestedByActorId { get; init; } = string.Empty;
    [JsonPropertyName("requested_by_employee_id")] public string RequestedByEmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("reviewed_by_actor_id")] public string? ReviewedByActorId { get; init; }
    [JsonPropertyName("review_reason")] public string? ReviewReason { get; init; }
    [JsonPropertyName("reviewed_at")] public string? ReviewedAt { get; init; }
    [JsonPropertyName("actual_minutes")] public int? ActualMinutes { get; init; }
    [JsonPropertyName("actual_note")] public string? ActualNote { get; init; }
    [JsonPropertyName("actual_recorded_by_actor_id")] public string? ActualRecordedByActorId { get; init; }
    [JsonPropertyName("actual_recorded_at")] public string? ActualRecordedAt { get; init; }
    [JsonPropertyName("confirmed_minutes")] public int? ConfirmedMinutes { get; init; }
    [JsonPropertyName("confirm_note")] public string? ConfirmNote { get; init; }
    [JsonPropertyName("confirmed_by_actor_id")] public string? ConfirmedByActorId { get; init; }
    [JsonPropertyName("confirmed_at")] public string? ConfirmedAt { get; init; }
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("request_id")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("employee_code")] public string? EmployeeCode { get; init; }
    [JsonPropertyName("employee_name")] public string? EmployeeName { get; init; }
    [JsonPropertyName("employee_branch_id")] public string? EmployeeBranchId { get; init; }
    [JsonPropertyName("branch_code")] public string? BranchCode { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
}

public sealed record OvertimeCapabilitiesData
{
    [JsonPropertyName("selfOnly")] public bool SelfOnly { get; init; }
    [JsonPropertyName("canSubmitOwn")] public bool CanSubmitOwn { get; init; }
    [JsonPropertyName("canApprove")] public bool CanApprove { get; init; }
    [JsonPropertyName("canConfirm")] public bool CanConfirm { get; init; }
}

public sealed record OvertimePaginationData
{
    [JsonPropertyName("limit")] public int Limit { get; init; } = 100;
    [JsonPropertyName("offset")] public int Offset { get; init; }
    [JsonPropertyName("total")] public int Total { get; init; }
    [JsonPropertyName("hasPrevious")] public bool HasPrevious { get; init; }
    [JsonPropertyName("hasNext")] public bool HasNext { get; init; }
}

public sealed record OvertimeListResponseData
{
    [JsonPropertyName("requests")] public OvertimeRequestData[] Requests { get; init; } = [];
    [JsonPropertyName("branches")] public LeaveBranchData[] Branches { get; init; } = [];
    [JsonPropertyName("pagination")] public OvertimePaginationData Pagination { get; init; } = new();
    [JsonPropertyName("capabilities")] public OvertimeCapabilitiesData Capabilities { get; init; } = new();
}

public sealed record SubmitOvertimeRequest
{
    [JsonPropertyName("workDate")] public string WorkDate { get; init; } = string.Empty;
    [JsonPropertyName("requestedMinutes")] public int RequestedMinutes { get; init; }
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
}

public sealed record ReviewOvertimeRequest
{
    [JsonPropertyName("requestId")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("expectedVersion")] public int ExpectedVersion { get; init; }
    [JsonPropertyName("action")] public string Action { get; init; } = string.Empty;
    [JsonPropertyName("reviewReason")] public string? ReviewReason { get; init; }
}

public sealed record RecordOvertimeActualRequest
{
    [JsonPropertyName("requestId")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("expectedVersion")] public int ExpectedVersion { get; init; }
    [JsonPropertyName("actualMinutes")] public int ActualMinutes { get; init; }
    [JsonPropertyName("actualNote")] public string? ActualNote { get; init; }
}

public sealed record ConfirmOvertimeRequest
{
    [JsonPropertyName("requestId")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("expectedVersion")] public int ExpectedVersion { get; init; }
    [JsonPropertyName("confirmedMinutes")] public int ConfirmedMinutes { get; init; }
    [JsonPropertyName("confirmNote")] public string? ConfirmNote { get; init; }
}

public sealed record AttendancePeriodIssueBucketData
{
    [JsonPropertyName("configurationIssueDays")] public decimal ConfigurationIssueDays { get; init; }
    [JsonPropertyName("pendingAdjustmentDays")] public decimal PendingAdjustmentDays { get; init; }
    [JsonPropertyName("pendingLeaveDays")] public decimal PendingLeaveDays { get; init; }
    [JsonPropertyName("outstandingOvertimeRequests")] public decimal OutstandingOvertimeRequests { get; init; }
    [JsonPropertyName("incompleteDays")] public decimal IncompleteDays { get; init; }
    [JsonPropertyName("unexcusedAbsenceDays")] public decimal UnexcusedAbsenceDays { get; init; }
    [JsonPropertyName("violationDays")] public decimal ViolationDays { get; init; }
}

public sealed record AttendancePeriodIssueSummaryData
{
    [JsonPropertyName("blockers")] public AttendancePeriodIssueBucketData Blockers { get; init; } = new();
    [JsonPropertyName("warnings")] public AttendancePeriodIssueBucketData Warnings { get; init; } = new();
    [JsonPropertyName("postCloseCorrection")] public bool PostCloseCorrection { get; init; }
}

public sealed record AttendancePeriodData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("scope_key")] public string ScopeKey { get; init; } = string.Empty;
    [JsonPropertyName("period_start")] public string PeriodStart { get; init; } = string.Empty;
    [JsonPropertyName("period_end")] public string PeriodEnd { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = "AGGREGATING";
    [JsonPropertyName("issue_summary")] public AttendancePeriodIssueSummaryData IssueSummary { get; init; } = new();
    [JsonPropertyName("source_fingerprint")] public string? SourceFingerprint { get; init; }
    [JsonPropertyName("reconciled_fingerprint")] public string? ReconciledFingerprint { get; init; }
    [JsonPropertyName("reconciled_by_actor_id")] public string? ReconciledByActorId { get; init; }
    [JsonPropertyName("reconciled_at")] public string? ReconciledAt { get; init; }
    [JsonPropertyName("reconciliation_note")] public string? ReconciliationNote { get; init; }
    [JsonPropertyName("closed_by_actor_id")] public string? ClosedByActorId { get; init; }
    [JsonPropertyName("closed_at")] public string? ClosedAt { get; init; }
    [JsonPropertyName("lock_id")] public string? LockId { get; init; }
    [JsonPropertyName("revision")] public int Revision { get; init; }
    [JsonPropertyName("request_id")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string CreatedBy { get; init; } = string.Empty;
    [JsonPropertyName("updated_by")] public string UpdatedBy { get; init; } = string.Empty;
    [JsonPropertyName("branch_code")] public string? BranchCode { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
}

public sealed record AttendancePeriodCapabilitiesData
{
    [JsonPropertyName("canReconcile")] public bool CanReconcile { get; init; }
    [JsonPropertyName("canClose")] public bool CanClose { get; init; }
}

public sealed record AttendancePeriodListResponseData
{
    [JsonPropertyName("periods")] public AttendancePeriodData[] Periods { get; init; } = [];
    [JsonPropertyName("branches")] public LeaveBranchData[] Branches { get; init; } = [];
    [JsonPropertyName("capabilities")] public AttendancePeriodCapabilitiesData Capabilities { get; init; } = new();
}

public sealed record AttendancePeriodMutationRequest
{
    [JsonPropertyName("action")] public string Action { get; init; } = string.Empty;
    [JsonPropertyName("periodStart")] public string PeriodStart { get; init; } = string.Empty;
    [JsonPropertyName("periodEnd")] public string PeriodEnd { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("acknowledgeWarnings")] public bool AcknowledgeWarnings { get; init; }
}

public sealed record AttendancePeriodSnapshotData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("revision")] public int Revision { get; init; }
    [JsonPropertyName("source_fingerprint")] public string SourceFingerprint { get; init; } = string.Empty;
}

public sealed record AttendancePeriodMutationResponseData
{
    [JsonPropertyName("period")] public AttendancePeriodData Period { get; init; } = new();
    [JsonPropertyName("issues")] public AttendancePeriodIssueSummaryData Issues { get; init; } = new();
    [JsonPropertyName("snapshot")] public AttendancePeriodSnapshotData? Snapshot { get; init; }
}

public sealed record AttendancePayrollInputPeriodData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
}

public sealed record AttendancePayrollEmployeeData
{
    [JsonPropertyName("employeeId")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("employeeCode")] public string EmployeeCode { get; init; } = string.Empty;
    [JsonPropertyName("employeeName")] public string EmployeeName { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
    [JsonPropertyName("branchCode")] public string? BranchCode { get; init; }
    [JsonPropertyName("branchName")] public string? BranchName { get; init; }
    [JsonPropertyName("workDays")] public decimal WorkDays { get; init; }
    [JsonPropertyName("completedDays")] public decimal CompletedDays { get; init; }
    [JsonPropertyName("countedMinutes")] public int CountedMinutes { get; init; }
    [JsonPropertyName("leaveCreditedMinutes")] public int LeaveCreditedMinutes { get; init; }
    [JsonPropertyName("approvedLeaveDays")] public decimal ApprovedLeaveDays { get; init; }
    [JsonPropertyName("paidLeaveDays")] public decimal PaidLeaveDays { get; init; }
    [JsonPropertyName("unpaidLeaveDays")] public decimal UnpaidLeaveDays { get; init; }
    [JsonPropertyName("unexcusedAbsenceDays")] public decimal UnexcusedAbsenceDays { get; init; }
    [JsonPropertyName("incompleteDays")] public decimal IncompleteDays { get; init; }
    [JsonPropertyName("configurationIssueDays")] public decimal ConfigurationIssueDays { get; init; }
    [JsonPropertyName("violationDays")] public decimal ViolationDays { get; init; }
    [JsonPropertyName("pendingLeaveDays")] public decimal PendingLeaveDays { get; init; }
    [JsonPropertyName("pendingAdjustmentDays")] public decimal PendingAdjustmentDays { get; init; }
    [JsonPropertyName("confirmedOvertimeMinutes")] public int ConfirmedOvertimeMinutes { get; init; }
}

public sealed record AttendancePayrollInputPayloadData
{
    [JsonPropertyName("contractVersion")] public int ContractVersion { get; init; }
    [JsonPropertyName("period")] public AttendancePayrollInputPeriodData Period { get; init; } = new();
    [JsonPropertyName("issueSummary")] public AttendancePeriodIssueSummaryData IssueSummary { get; init; } = new();
    [JsonPropertyName("status")] public string Status { get; init; } = "CLOSED";
    [JsonPropertyName("revision")] public int Revision { get; init; }
    [JsonPropertyName("employees")] public AttendancePayrollEmployeeData[] Employees { get; init; } = [];
}

public sealed record AttendancePayrollInputData
{
    [JsonPropertyName("period")] public AttendancePeriodData Period { get; init; } = new();
    [JsonPropertyName("revision")] public int Revision { get; init; }
    [JsonPropertyName("sourceFingerprint")] public string SourceFingerprint { get; init; } = string.Empty;
    [JsonPropertyName("payrollInput")] public AttendancePayrollInputPayloadData PayrollInput { get; init; } = new();
}
