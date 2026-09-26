using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record AttendanceTodayData
{
    [JsonPropertyName("workDate")] public string WorkDate { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = "NOT_STARTED";
    [JsonPropertyName("nextAction")] public string? NextAction { get; init; }
    [JsonPropertyName("tooSoon")] public bool TooSoon { get; init; }
    [JsonPropertyName("employee")] public AttendanceEmployeeData Employee { get; init; } = new();
    [JsonPropertyName("policy")] public AttendancePolicyContextData Policy { get; init; } = new();
    [JsonPropertyName("schedule")] public AttendanceScheduleContextData? Schedule { get; init; }
    [JsonPropertyName("expectedStartAt")] public string? ExpectedStartAt { get; init; }
    [JsonPropertyName("expectedEndAt")] public string? ExpectedEndAt { get; init; }
    [JsonPropertyName("events")] public AttendanceEventData[] Events { get; init; } = [];
}

public sealed record AttendanceEmployeeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("full_name")] public string FullName { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("branch_code")] public string? BranchCode { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record AttendancePolicyContextData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("timeMode")] public string TimeMode { get; init; } = "FIXED";
    [JsonPropertyName("attendanceMethod")] public string AttendanceMethod { get; init; } = "QR";
    [JsonPropertyName("attendanceBasis")] public string AttendanceBasis { get; init; } = "TIME";
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = "Asia/Ho_Chi_Minh";
}

public sealed record AttendanceScheduleContextData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("kind")] public string Kind { get; init; } = "WORK";
    [JsonPropertyName("source")] public string Source { get; init; } = "POLICY";
}

public sealed record AttendanceEventData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("schedule_id")] public string? ScheduleId { get; init; }
    [JsonPropertyName("work_policy_id")] public string? WorkPolicyId { get; init; }
    [JsonPropertyName("attendance_point_id")] public string? AttendancePointId { get; init; }
    [JsonPropertyName("event_type")] public string EventType { get; init; } = string.Empty;
    [JsonPropertyName("movement_reason")] public string? MovementReason { get; init; }
    [JsonPropertyName("occurred_at")] public string OccurredAt { get; init; } = string.Empty;
    [JsonPropertyName("source")] public string Source { get; init; } = string.Empty;
    [JsonPropertyName("validation_status")] public string ValidationStatus { get; init; } = string.Empty;
    [JsonPropertyName("source_reference")] public string? SourceReference { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("recorded_by")] public string? RecordedBy { get; init; }
    [JsonPropertyName("request_id")] public string? RequestId { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("point_code")] public string? PointCode { get; init; }
    [JsonPropertyName("point_name")] public string? PointName { get; init; }
}

public sealed record AttendancePointData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
    [JsonPropertyName("branch_code")] public string? BranchCode { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
}

public sealed record AttendanceBranchData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record AttendancePointManagementData
{
    [JsonPropertyName("points")] public AttendancePointData[] Points { get; init; } = [];
    [JsonPropertyName("branches")] public AttendanceBranchData[] Branches { get; init; } = [];
    [JsonPropertyName("companyScope")] public bool CompanyScope { get; init; }
}

public sealed record AttendanceQrTokenData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("attendancePointId")] public string AttendancePointId { get; init; } = string.Empty;
    [JsonPropertyName("pointCode")] public string PointCode { get; init; } = string.Empty;
    [JsonPropertyName("pointName")] public string PointName { get; init; } = string.Empty;
    [JsonPropertyName("branchName")] public string? BranchName { get; init; }
    [JsonPropertyName("qrPayload")] public string QrPayload { get; init; } = string.Empty;
    [JsonPropertyName("expiresAt")] public string ExpiresAt { get; init; } = string.Empty;
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
}

public sealed record AttendanceRecordPointData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
    [JsonPropertyName("branchName")] public string? BranchName { get; init; }
}

public sealed record AttendanceRecordResultData
{
    [JsonPropertyName("event")] public AttendanceEventData Event { get; init; } = new();
    [JsonPropertyName("workDate")] public string WorkDate { get; init; } = string.Empty;
    [JsonPropertyName("point")] public AttendanceRecordPointData? Point { get; init; }
}

public sealed record AttendanceRecordRequest(
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("qrPayload")] string? QrPayload = null,
    [property: JsonPropertyName("exitReason")] string? ExitReason = null,
    [property: JsonPropertyName("note")] string? Note = null);

public sealed record ManagedManualAttendanceRequest(
    [property: JsonPropertyName("employeeId")] string EmployeeId,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("exitReason")] string? ExitReason = null,
    [property: JsonPropertyName("note")] string? Note = null);

public sealed record CreateAttendancePointRequest(
    [property: JsonPropertyName("branchId")] string BranchId);

public sealed record CreateAttendanceQrTokenRequest(
    [property: JsonPropertyName("attendancePointId")] string AttendancePointId);
