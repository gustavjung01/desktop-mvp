using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record AttendanceAdjustmentRequestData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("work_date")] public string WorkDate { get; init; } = string.Empty;
    [JsonPropertyName("requested_check_in_at")] public string? RequestedCheckInAt { get; init; }
    [JsonPropertyName("requested_check_out_at")] public string? RequestedCheckOutAt { get; init; }
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("request_source")] public string RequestSource { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("requested_by_actor_id")] public string RequestedByActorId { get; init; } = string.Empty;
    [JsonPropertyName("requested_by_employee_id")] public string? RequestedByEmployeeId { get; init; }
    [JsonPropertyName("reviewed_by_actor_id")] public string? ReviewedByActorId { get; init; }
    [JsonPropertyName("review_reason")] public string? ReviewReason { get; init; }
    [JsonPropertyName("reviewed_at")] public string? ReviewedAt { get; init; }
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

public sealed record AttendanceAdjustmentSelectedEmployeeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
}

public sealed record AttendanceAdjustmentPeriodData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
}

public sealed record AttendanceAdjustmentPaginationData
{
    [JsonPropertyName("limit")] public int Limit { get; init; }
    [JsonPropertyName("offset")] public int Offset { get; init; }
    [JsonPropertyName("total")] public int Total { get; init; }
    [JsonPropertyName("hasPrevious")] public bool HasPrevious { get; init; }
    [JsonPropertyName("hasNext")] public bool HasNext { get; init; }
}

public sealed record AttendanceAdjustmentCapabilitiesData
{
    [JsonPropertyName("selfOnly")] public bool SelfOnly { get; init; }
    [JsonPropertyName("canSubmitOwn")] public bool CanSubmitOwn { get; init; }
    [JsonPropertyName("canManage")] public bool CanManage { get; init; }
    [JsonPropertyName("canLock")] public bool CanLock { get; init; }
}

public sealed record AttendanceAdjustmentListResponseData
{
    [JsonPropertyName("period")] public AttendanceAdjustmentPeriodData Period { get; init; } = new();
    [JsonPropertyName("selectedEmployee")] public AttendanceAdjustmentSelectedEmployeeData? SelectedEmployee { get; init; }
    [JsonPropertyName("branches")] public AttendanceBranchData[] Branches { get; init; } = [];
    [JsonPropertyName("pagination")] public AttendanceAdjustmentPaginationData Pagination { get; init; } = new();
    [JsonPropertyName("requests")] public AttendanceAdjustmentRequestData[] Requests { get; init; } = [];
    [JsonPropertyName("capabilities")] public AttendanceAdjustmentCapabilitiesData Capabilities { get; init; } = new();
}

public sealed record AttendancePeriodLockData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("period_start")] public string PeriodStart { get; init; } = string.Empty;
    [JsonPropertyName("period_end")] public string PeriodEnd { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("locked_by_actor_id")] public string LockedByActorId { get; init; } = string.Empty;
    [JsonPropertyName("request_id")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("locked_at")] public string LockedAt { get; init; } = string.Empty;
    [JsonPropertyName("branch_code")] public string? BranchCode { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
}

public sealed record AttendancePeriodLockListResponseData
{
    [JsonPropertyName("locks")] public AttendancePeriodLockData[] Locks { get; init; } = [];
    [JsonPropertyName("branches")] public AttendanceBranchData[] Branches { get; init; } = [];
    [JsonPropertyName("companyScope")] public bool CompanyScope { get; init; }
    [JsonPropertyName("canLock")] public bool CanLock { get; init; }
}

public sealed record AttendanceAdjustmentMutationResultData
{
    [JsonPropertyName("request")] public AttendanceAdjustmentRequestData Request { get; init; } = new();
    [JsonPropertyName("events")] public AttendanceEventData[] Events { get; init; } = [];
}

public sealed record SubmitAttendanceAdjustmentRequest(
    [property: JsonPropertyName("workDate")] string WorkDate,
    [property: JsonPropertyName("requestedCheckInAt")] string? RequestedCheckInAt,
    [property: JsonPropertyName("requestedCheckOutAt")] string? RequestedCheckOutAt,
    [property: JsonPropertyName("reason")] string Reason);

public sealed record ReviewAttendanceAdjustmentRequest(
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("expectedVersion")] int ExpectedVersion,
    [property: JsonPropertyName("reviewReason")] string? ReviewReason);

public sealed record DirectAttendanceAdjustmentRequest(
    [property: JsonPropertyName("employeeId")] string EmployeeId,
    [property: JsonPropertyName("workDate")] string? WorkDate,
    [property: JsonPropertyName("requestedCheckInAt")] string? RequestedCheckInAt,
    [property: JsonPropertyName("requestedCheckOutAt")] string? RequestedCheckOutAt,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("recordNowAction")] string? RecordNowAction = null);

public sealed record CreateAttendancePeriodLockRequest(
    [property: JsonPropertyName("periodStart")] string PeriodStart,
    [property: JsonPropertyName("periodEnd")] string PeriodEnd,
    [property: JsonPropertyName("branchId")] string? BranchId,
    [property: JsonPropertyName("reason")] string Reason);
