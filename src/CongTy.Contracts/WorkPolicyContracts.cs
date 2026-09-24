using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record WorkPolicyDetailData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("work_nature")] public string? WorkNature { get; init; }
    [JsonPropertyName("time_mode")] public string TimeMode { get; init; } = "FIXED";
    [JsonPropertyName("fixed_start_time")] public string? FixedStartTime { get; init; }
    [JsonPropertyName("fixed_end_time")] public string? FixedEndTime { get; init; }
    [JsonPropertyName("working_days")] public int[] WorkingDays { get; init; } = [];
    [JsonPropertyName("break_minutes")] public int BreakMinutes { get; init; }
    [JsonPropertyName("late_grace_minutes")] public int LateGraceMinutes { get; init; }
    [JsonPropertyName("early_leave_grace_minutes")] public int EarlyLeaveGraceMinutes { get; init; }
    [JsonPropertyName("overtime_enabled")] public bool OvertimeEnabled { get; init; }
    [JsonPropertyName("overtime_requires_approval")] public bool OvertimeRequiresApproval { get; init; }
    [JsonPropertyName("attendance_method")] public string AttendanceMethod { get; init; } = "QR";
    [JsonPropertyName("attendance_basis")] public string AttendanceBasis { get; init; } = "TIME";
    [JsonPropertyName("timezone")] public string Timezone { get; init; } = "Asia/Ho_Chi_Minh";
    [JsonPropertyName("rounding_minutes")] public int RoundingMinutes { get; init; }
    [JsonPropertyName("minimum_full_day_minutes")] public int? MinimumFullDayMinutes { get; init; }
    [JsonPropertyName("minimum_half_day_minutes")] public int? MinimumHalfDayMinutes { get; init; }
    [JsonPropertyName("effective_from")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("effective_to")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("supersedes_policy_id")] public string? SupersedesPolicyId { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
}

public sealed record SaveWorkPolicyRequest(
    [property: JsonPropertyName("basePolicyId")] string? BasePolicyId,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("workNature")] string? WorkNature,
    [property: JsonPropertyName("timeMode")] string TimeMode,
    [property: JsonPropertyName("fixedStartTime")] string? FixedStartTime,
    [property: JsonPropertyName("fixedEndTime")] string? FixedEndTime,
    [property: JsonPropertyName("workingDays")] int[] WorkingDays,
    [property: JsonPropertyName("breakMinutes")] int BreakMinutes,
    [property: JsonPropertyName("lateGraceMinutes")] int LateGraceMinutes,
    [property: JsonPropertyName("earlyLeaveGraceMinutes")] int EarlyLeaveGraceMinutes,
    [property: JsonPropertyName("overtimeEnabled")] bool OvertimeEnabled,
    [property: JsonPropertyName("overtimeRequiresApproval")] bool OvertimeRequiresApproval,
    [property: JsonPropertyName("attendanceMethod")] string AttendanceMethod,
    [property: JsonPropertyName("attendanceBasis")] string AttendanceBasis,
    [property: JsonPropertyName("timezone")] string Timezone,
    [property: JsonPropertyName("roundingMinutes")] int RoundingMinutes,
    [property: JsonPropertyName("minimumFullDayMinutes")] int? MinimumFullDayMinutes,
    [property: JsonPropertyName("minimumHalfDayMinutes")] int? MinimumHalfDayMinutes,
    [property: JsonPropertyName("effectiveFrom")] string EffectiveFrom,
    [property: JsonPropertyName("effectiveTo")] string? EffectiveTo = null);
