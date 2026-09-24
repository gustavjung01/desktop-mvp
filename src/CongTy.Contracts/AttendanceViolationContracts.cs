using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record AttendanceViolationCaseData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("work_date")] public string WorkDate { get; init; } = string.Empty;
    [JsonPropertyName("violation_kind")] public string ViolationKind { get; init; } = string.Empty;
    [JsonPropertyName("violation_label_snapshot")] public string ViolationLabelSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("violation_detail_snapshot")] public string ViolationDetailSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("violation_minutes_snapshot")] public int? ViolationMinutesSnapshot { get; init; }
    [JsonPropertyName("violation_day_fraction_snapshot")] public JsonElement? ViolationDayFractionSnapshot { get; init; }
    [JsonPropertyName("policy_id_snapshot")] public string? PolicyIdSnapshot { get; init; }
    [JsonPropertyName("policy_version_snapshot")] public int? PolicyVersionSnapshot { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("explanation")] public string Explanation { get; init; } = string.Empty;
    [JsonPropertyName("explained_by_actor_id")] public string ExplainedByActorId { get; init; } = string.Empty;
    [JsonPropertyName("explained_at")] public string ExplainedAt { get; init; } = string.Empty;
    [JsonPropertyName("reviewed_by_actor_id")] public string? ReviewedByActorId { get; init; }
    [JsonPropertyName("review_note")] public string? ReviewNote { get; init; }
    [JsonPropertyName("reviewed_at")] public string? ReviewedAt { get; init; }
    [JsonPropertyName("outcome")] public string? Outcome { get; init; }
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

public sealed record AttendanceViolationHandlingEntryData
{
    [JsonPropertyName("employee")] public AttendanceTimesheetEmployeeData Employee { get; init; } = new();
    [JsonPropertyName("workDate")] public string WorkDate { get; init; } = string.Empty;
    [JsonPropertyName("violation")] public AttendanceViolationData? Violation { get; init; }
    [JsonPropertyName("evaluationState")] public string EvaluationState { get; init; } = string.Empty;
    [JsonPropertyName("case")] public AttendanceViolationCaseData? Case { get; init; }
}

public sealed record AttendanceViolationCapabilitiesData
{
    [JsonPropertyName("selfOnly")] public bool SelfOnly { get; init; }
    [JsonPropertyName("canExplain")] public bool CanExplain { get; init; }
    [JsonPropertyName("canReview")] public bool CanReview { get; init; }
}

public sealed record AttendanceViolationHandlingResponseData
{
    [JsonPropertyName("period")] public AttendanceTimesheetPeriodData Period { get; init; } = new();
    [JsonPropertyName("scope")] public AttendanceTimesheetScopeData Scope { get; init; } = new();
    [JsonPropertyName("pagination")] public AttendanceTimesheetPaginationData Pagination { get; init; } = new();
    [JsonPropertyName("entries")] public AttendanceViolationHandlingEntryData[] Entries { get; init; } = [];
    [JsonPropertyName("capabilities")] public AttendanceViolationCapabilitiesData Capabilities { get; init; } = new();
}

public sealed record SubmitAttendanceViolationExplanationRequest(
    [property: JsonPropertyName("workDate")] string WorkDate,
    [property: JsonPropertyName("violationKind")] string ViolationKind,
    [property: JsonPropertyName("explanation")] string Explanation);

public sealed record ReviewAttendanceViolationRequest(
    [property: JsonPropertyName("caseId")] string CaseId,
    [property: JsonPropertyName("expectedVersion")] int ExpectedVersion,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("outcome")] string? Outcome = null,
    [property: JsonPropertyName("reviewNote")] string? ReviewNote = null);
