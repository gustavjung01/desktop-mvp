using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record AttendanceTimesheetPeriodData
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = "Asia/Ho_Chi_Minh";
}

public sealed record AttendanceTimesheetScopeData
{
    [JsonPropertyName("companyScope")] public bool CompanyScope { get; init; }
    [JsonPropertyName("selfOnly")] public bool SelfOnly { get; init; }
    [JsonPropertyName("branches")] public AttendanceBranchData[] Branches { get; init; } = [];
}

public sealed record AttendanceTimesheetPaginationData
{
    [JsonPropertyName("limit")] public int Limit { get; init; }
    [JsonPropertyName("offset")] public int Offset { get; init; }
    [JsonPropertyName("total")] public int Total { get; init; }
    [JsonPropertyName("hasPrevious")] public bool HasPrevious { get; init; }
    [JsonPropertyName("hasNext")] public bool HasNext { get; init; }
}

public sealed record AttendanceTimesheetCapabilitiesData
{
    [JsonPropertyName("canSubmitOwn")] public bool CanSubmitOwn { get; init; }
    [JsonPropertyName("canManage")] public bool CanManage { get; init; }
    [JsonPropertyName("canLock")] public bool CanLock { get; init; }
}

public sealed record AttendanceTimesheetEmployeeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
    [JsonPropertyName("branchCode")] public string? BranchCode { get; init; }
    [JsonPropertyName("branchName")] public string? BranchName { get; init; }
}

public sealed record AttendanceTimesheetPolicyData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("timeMode")] public string TimeMode { get; init; } = string.Empty;
    [JsonPropertyName("attendanceBasis")] public string AttendanceBasis { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = "Asia/Ho_Chi_Minh";
    [JsonPropertyName("breakMinutes")] public int BreakMinutes { get; init; }
    [JsonPropertyName("lateGraceMinutes")] public int LateGraceMinutes { get; init; }
    [JsonPropertyName("earlyLeaveGraceMinutes")] public int EarlyLeaveGraceMinutes { get; init; }
}

public sealed record AttendanceTimesheetScheduleData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("kind")] public string Kind { get; init; } = string.Empty;
    [JsonPropertyName("source")] public string Source { get; init; } = string.Empty;
}

public sealed record AttendanceTimesheetLeaveData
{
    [JsonPropertyName("requests")] public JsonElement[] Requests { get; init; } = [];
    [JsonPropertyName("approvedFraction")] public decimal ApprovedFraction { get; init; }
    [JsonPropertyName("pendingFraction")] public decimal PendingFraction { get; init; }
    [JsonPropertyName("countedAsWorkdayFraction")] public decimal CountedAsWorkdayFraction { get; init; }
    [JsonPropertyName("paidFraction")] public decimal PaidFraction { get; init; }
    [JsonPropertyName("approvedSegments")] public string[] ApprovedSegments { get; init; } = [];
    [JsonPropertyName("pendingSegments")] public string[] PendingSegments { get; init; } = [];
    [JsonPropertyName("countedSegments")] public string[] CountedSegments { get; init; } = [];
    [JsonPropertyName("approvedLabels")] public string[] ApprovedLabels { get; init; } = [];
    [JsonPropertyName("pendingLabels")] public string[] PendingLabels { get; init; } = [];
}

public sealed record AttendanceTimesheetAdjustmentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("request_source")] public string RequestSource { get; init; } = string.Empty;
}

public sealed record AttendanceTimesheetPeriodLockData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("period_start")] public string PeriodStart { get; init; } = string.Empty;
    [JsonPropertyName("period_end")] public string PeriodEnd { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("locked_at")] public string LockedAt { get; init; } = string.Empty;
}

public sealed record AttendanceViolationData
{
    [JsonPropertyName("kind")] public string Kind { get; init; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; init; } = string.Empty;
    [JsonPropertyName("detail")] public string Detail { get; init; } = string.Empty;
    [JsonPropertyName("minutes")] public int? Minutes { get; init; }
    [JsonPropertyName("dayFraction")] public decimal? DayFraction { get; init; }
}

public sealed record AttendanceViolationEvaluationData
{
    [JsonPropertyName("state")] public string State { get; init; } = string.Empty;
    [JsonPropertyName("explanation")] public string Explanation { get; init; } = string.Empty;
    [JsonPropertyName("items")] public AttendanceViolationData[] Items { get; init; } = [];
}

public sealed record AttendanceTimesheetDayData
{
    [JsonPropertyName("workDate")] public string WorkDate { get; init; } = string.Empty;
    [JsonPropertyName("employee")] public AttendanceTimesheetEmployeeData Employee { get; init; } = new();
    [JsonPropertyName("policy")] public AttendanceTimesheetPolicyData? Policy { get; init; }
    [JsonPropertyName("schedule")] public AttendanceTimesheetScheduleData? Schedule { get; init; }
    [JsonPropertyName("expectedStartAt")] public string? ExpectedStartAt { get; init; }
    [JsonPropertyName("expectedEndAt")] public string? ExpectedEndAt { get; init; }
    [JsonPropertyName("requiredStartAt")] public string? RequiredStartAt { get; init; }
    [JsonPropertyName("requiredEndAt")] public string? RequiredEndAt { get; init; }
    [JsonPropertyName("checkInAt")] public string? CheckInAt { get; init; }
    [JsonPropertyName("checkOutAt")] public string? CheckOutAt { get; init; }
    [JsonPropertyName("actualMinutes")] public int ActualMinutes { get; init; }
    [JsonPropertyName("countedMinutes")] public int CountedMinutes { get; init; }
    [JsonPropertyName("leaveCreditedMinutes")] public int LeaveCreditedMinutes { get; init; }
    [JsonPropertyName("lateMinutes")] public int LateMinutes { get; init; }
    [JsonPropertyName("earlyLeaveMinutes")] public int EarlyLeaveMinutes { get; init; }
    [JsonPropertyName("missingCheckIn")] public bool MissingCheckIn { get; init; }
    [JsonPropertyName("missingCheckOut")] public bool MissingCheckOut { get; init; }
    [JsonPropertyName("scheduledWorkDay")] public bool ScheduledWorkDay { get; init; }
    [JsonPropertyName("validWork")] public bool ValidWork { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("attendanceStatus")] public string AttendanceStatus { get; init; } = string.Empty;
    [JsonPropertyName("configurationIssue")] public string? ConfigurationIssue { get; init; }
    [JsonPropertyName("unexcusedAbsenceFraction")] public decimal UnexcusedAbsenceFraction { get; init; }
    [JsonPropertyName("violationEvaluation")] public AttendanceViolationEvaluationData ViolationEvaluation { get; init; } = new();
    [JsonPropertyName("leave")] public AttendanceTimesheetLeaveData Leave { get; init; } = new();
    [JsonPropertyName("attendanceSources")] public string[] AttendanceSources { get; init; } = [];
    [JsonPropertyName("scheduleSource")] public string? ScheduleSource { get; init; }
    [JsonPropertyName("adjustment")] public AttendanceTimesheetAdjustmentData? Adjustment { get; init; }
    [JsonPropertyName("periodLock")] public AttendanceTimesheetPeriodLockData? PeriodLock { get; init; }
    [JsonPropertyName("events")] public AttendanceEventData[] Events { get; init; } = [];
}

public sealed record AttendanceTimesheetMonthData
{
    [JsonPropertyName("employee")] public AttendanceTimesheetEmployeeData Employee { get; init; } = new();
    [JsonPropertyName("period")] public AttendanceTimesheetPeriodData Period { get; init; } = new();
    [JsonPropertyName("workDays")] public decimal WorkDays { get; init; }
    [JsonPropertyName("completedDays")] public decimal CompletedDays { get; init; }
    [JsonPropertyName("scheduledDaysOff")] public decimal ScheduledDaysOff { get; init; }
    [JsonPropertyName("approvedLeaveDays")] public decimal ApprovedLeaveDays { get; init; }
    [JsonPropertyName("pendingLeaveDays")] public decimal PendingLeaveDays { get; init; }
    [JsonPropertyName("unexcusedAbsenceDays")] public decimal UnexcusedAbsenceDays { get; init; }
    [JsonPropertyName("incompleteDays")] public decimal IncompleteDays { get; init; }
    [JsonPropertyName("configurationIssueDays")] public decimal ConfigurationIssueDays { get; init; }
    [JsonPropertyName("violationDays")] public int ViolationDays { get; init; }
    [JsonPropertyName("lateViolationDays")] public int LateViolationDays { get; init; }
    [JsonPropertyName("earlyLeaveViolationDays")] public int EarlyLeaveViolationDays { get; init; }
    [JsonPropertyName("missingAttendanceViolationDays")] public int MissingAttendanceViolationDays { get; init; }
    [JsonPropertyName("unexcusedAbsenceViolationDays")] public decimal UnexcusedAbsenceViolationDays { get; init; }
    [JsonPropertyName("missingDays")] public decimal MissingDays { get; init; }
    [JsonPropertyName("actualMinutes")] public int ActualMinutes { get; init; }
    [JsonPropertyName("countedMinutes")] public int CountedMinutes { get; init; }
    [JsonPropertyName("leaveCreditedMinutes")] public int LeaveCreditedMinutes { get; init; }
    [JsonPropertyName("lateMinutes")] public int LateMinutes { get; init; }
    [JsonPropertyName("earlyLeaveMinutes")] public int EarlyLeaveMinutes { get; init; }
    [JsonPropertyName("adjustedDays")] public int AdjustedDays { get; init; }
    [JsonPropertyName("pendingAdjustmentDays")] public int PendingAdjustmentDays { get; init; }
    [JsonPropertyName("lockedDays")] public int LockedDays { get; init; }
    [JsonPropertyName("attendanceSources")] public string[] AttendanceSources { get; init; } = [];
    [JsonPropertyName("scheduleSources")] public string[] ScheduleSources { get; init; } = [];
    [JsonPropertyName("days")] public AttendanceTimesheetDayData[] Days { get; init; } = [];
}

public sealed record AttendanceTimesheetResponseData
{
    [JsonPropertyName("view")] public string View { get; init; } = string.Empty;
    [JsonPropertyName("period")] public AttendanceTimesheetPeriodData Period { get; init; } = new();
    [JsonPropertyName("scope")] public AttendanceTimesheetScopeData Scope { get; init; } = new();
    [JsonPropertyName("pagination")] public AttendanceTimesheetPaginationData Pagination { get; init; } = new();
    [JsonPropertyName("capabilities")] public AttendanceTimesheetCapabilitiesData Capabilities { get; init; } = new();
    [JsonPropertyName("rows")] public AttendanceTimesheetMonthData[] Rows { get; init; } = [];
}
