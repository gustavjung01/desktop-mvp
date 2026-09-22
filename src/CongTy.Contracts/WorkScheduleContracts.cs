using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record WorkPolicyData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = "Asia/Ho_Chi_Minh";
    [JsonPropertyName("effective_from")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("effective_to")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record WorkScheduleData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("work_policy_id")] public string? WorkPolicyId { get; init; }
    [JsonPropertyName("work_date")] public string WorkDate { get; init; } = string.Empty;
    [JsonPropertyName("schedule_kind")] public string ScheduleKind { get; init; } = "OFF";
    [JsonPropertyName("scheduled_start_at")] public string? ScheduledStartAt { get; init; }
    [JsonPropertyName("scheduled_end_at")] public string? ScheduledEndAt { get; init; }
    [JsonPropertyName("source")] public string Source { get; init; } = "POLICY";
    [JsonPropertyName("override_reason")] public string? OverrideReason { get; init; }
    [JsonPropertyName("shift_template_id")] public string? ShiftTemplateId { get; init; }
    [JsonPropertyName("week_template_id")] public string? WeekTemplateId { get; init; }
    [JsonPropertyName("company_calendar_day_id")] public string? CompanyCalendarDayId { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
    [JsonPropertyName("employee_code")] public string EmployeeCode { get; init; } = string.Empty;
    [JsonPropertyName("employee_name")] public string EmployeeName { get; init; } = string.Empty;
    [JsonPropertyName("employee_branch_id")] public string? EmployeeBranchId { get; init; }
    [JsonPropertyName("policy_code")] public string? PolicyCode { get; init; }
    [JsonPropertyName("policy_version")] public int? PolicyVersion { get; init; }
    [JsonPropertyName("policy_name")] public string? PolicyName { get; init; }
    [JsonPropertyName("policy_timezone")] public string? PolicyTimezone { get; init; }
}

public sealed record WorkShiftTemplateData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("start_time")] public string StartTime { get; init; } = string.Empty;
    [JsonPropertyName("end_time")] public string EndTime { get; init; } = string.Empty;
    [JsonPropertyName("break_minutes")] public int BreakMinutes { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
}

public sealed record WorkWeekTemplateDayData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("weekday")] public int Weekday { get; init; }
    [JsonPropertyName("schedule_kind")] public string ScheduleKind { get; init; } = "OFF";
    [JsonPropertyName("shift_template_id")] public string? ShiftTemplateId { get; init; }
    [JsonPropertyName("shift_code")] public string? ShiftCode { get; init; }
    [JsonPropertyName("shift_name")] public string? ShiftName { get; init; }
    [JsonPropertyName("shift_start_time")] public string? ShiftStartTime { get; init; }
    [JsonPropertyName("shift_end_time")] public string? ShiftEndTime { get; init; }
    [JsonPropertyName("shift_break_minutes")] public int? ShiftBreakMinutes { get; init; }
}

public sealed record WorkWeekTemplateData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
    [JsonPropertyName("days")] public WorkWeekTemplateDayData[] Days { get; init; } = [];
}

public sealed record CompanyCalendarDayData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("calendar_date")] public string CalendarDate { get; init; } = string.Empty;
    [JsonPropertyName("calendar_kind")] public string CalendarKind { get; init; } = "PUBLIC_HOLIDAY";
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
}

public sealed record SchedulePlanningCatalogData
{
    [JsonPropertyName("shiftTemplates")] public WorkShiftTemplateData[] ShiftTemplates { get; init; } = [];
    [JsonPropertyName("weekTemplates")] public WorkWeekTemplateData[] WeekTemplates { get; init; } = [];
    [JsonPropertyName("calendarDays")] public CompanyCalendarDayData[] CalendarDays { get; init; } = [];
}

public sealed record ScheduleBulkResultData
{
    [JsonPropertyName("requestedCount")] public int RequestedCount { get; init; }
    [JsonPropertyName("affectedCount")] public int AffectedCount { get; init; }
    [JsonPropertyName("skippedOverrides")] public int SkippedOverrides { get; init; }
    [JsonPropertyName("employeeCount")] public int EmployeeCount { get; init; }
    [JsonPropertyName("fromDate")] public string? FromDate { get; init; }
    [JsonPropertyName("toDate")] public string? ToDate { get; init; }
    [JsonPropertyName("sourceFrom")] public string? SourceFrom { get; init; }
    [JsonPropertyName("sourceTo")] public string? SourceTo { get; init; }
    [JsonPropertyName("targetFrom")] public string? TargetFrom { get; init; }
    [JsonPropertyName("targetTo")] public string? TargetTo { get; init; }
}

public sealed record WorkScheduleMutationRequest(
    [property: JsonPropertyName("employeeId")] string EmployeeId,
    [property: JsonPropertyName("workPolicyId")] string? WorkPolicyId,
    [property: JsonPropertyName("workDate")] string WorkDate,
    [property: JsonPropertyName("scheduleKind")] string ScheduleKind,
    [property: JsonPropertyName("scheduledStartAt")] string? ScheduledStartAt,
    [property: JsonPropertyName("scheduledEndAt")] string? ScheduledEndAt,
    [property: JsonPropertyName("overrideReason")] string OverrideReason,
    [property: JsonPropertyName("expectedUpdatedAt")] string? ExpectedUpdatedAt = null);

public sealed record SaveShiftTemplateRequest(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("startTime")] string StartTime,
    [property: JsonPropertyName("endTime")] string EndTime,
    [property: JsonPropertyName("breakMinutes")] int BreakMinutes,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record WorkWeekTemplateDayRequest(
    [property: JsonPropertyName("weekday")] int Weekday,
    [property: JsonPropertyName("scheduleKind")] string ScheduleKind,
    [property: JsonPropertyName("shiftTemplateId")] string? ShiftTemplateId);

public sealed record SaveWeekTemplateRequest(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("days")] WorkWeekTemplateDayRequest[] Days);

public sealed record SaveCalendarDayRequest(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("calendarDate")] string CalendarDate,
    [property: JsonPropertyName("calendarKind")] string CalendarKind,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isActive")] bool IsActive);

public sealed record ApplyWeekTemplateRequest(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("weekTemplateId")] string WeekTemplateId,
    [property: JsonPropertyName("employeeIds")] string[] EmployeeIds,
    [property: JsonPropertyName("fromDate")] string FromDate,
    [property: JsonPropertyName("toDate")] string ToDate,
    [property: JsonPropertyName("reason")] string Reason);

public sealed record CopyScheduleRequest(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("employeeIds")] string[] EmployeeIds,
    [property: JsonPropertyName("sourceFrom")] string SourceFrom,
    [property: JsonPropertyName("sourceTo")] string SourceTo,
    [property: JsonPropertyName("targetFrom")] string TargetFrom,
    [property: JsonPropertyName("reason")] string Reason);
