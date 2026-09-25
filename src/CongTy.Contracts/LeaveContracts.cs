using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record LeaveTypeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("is_paid")] public bool IsPaid { get; init; }
    [JsonPropertyName("counts_as_workday")] public bool CountsAsWorkday { get; init; }
    [JsonPropertyName("requires_approval")] public bool RequiresApproval { get; init; }
    [JsonPropertyName("allows_full_day")] public bool AllowsFullDay { get; init; }
    [JsonPropertyName("allows_half_day")] public bool AllowsHalfDay { get; init; }
    [JsonPropertyName("requires_attachment")] public bool RequiresAttachment { get; init; }
    [JsonPropertyName("tracks_balance")] public bool TracksBalance { get; init; }
    [JsonPropertyName("allow_negative_balance")] public bool AllowNegativeBalance { get; init; }
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
}

public sealed record LeaveTypeCapabilitiesData
{
    [JsonPropertyName("canManage")] public bool CanManage { get; init; }
}

public sealed record LeaveTypeListResponseData
{
    [JsonPropertyName("leaveTypes")] public LeaveTypeData[] LeaveTypes { get; init; } = [];
    [JsonPropertyName("capabilities")] public LeaveTypeCapabilitiesData Capabilities { get; init; } = new();
}

public sealed record LeaveBranchData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record LeaveSelectedEmployeeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
}

public sealed record LeaveEmployeeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("branchId")] public string? BranchId { get; init; }
    [JsonPropertyName("branchName")] public string? BranchName { get; init; }
}

public sealed record LeavePaginationData
{
    [JsonPropertyName("limit")] public int Limit { get; init; } = 50;
    [JsonPropertyName("offset")] public int Offset { get; init; }
    [JsonPropertyName("total")] public int Total { get; init; }
    [JsonPropertyName("hasPrevious")] public bool HasPrevious { get; init; }
    [JsonPropertyName("hasNext")] public bool HasNext { get; init; }
}

public sealed record LeaveRequestCapabilitiesData
{
    [JsonPropertyName("selfOnly")] public bool SelfOnly { get; init; }
    [JsonPropertyName("canSubmitOwn")] public bool CanSubmitOwn { get; init; }
    [JsonPropertyName("canApprove")] public bool CanApprove { get; init; }
    [JsonPropertyName("canSubmitManual")] public bool CanSubmitManual { get; init; }
    [JsonPropertyName("canManageTypes")] public bool CanManageTypes { get; init; }
}

public sealed record LeaveRequestData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("leave_type_id")] public string LeaveTypeId { get; init; } = string.Empty;
    [JsonPropertyName("leave_type_code_snapshot")] public string LeaveTypeCodeSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("leave_type_name_snapshot")] public string LeaveTypeNameSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("leave_is_paid_snapshot")] public bool LeaveIsPaidSnapshot { get; init; }
    [JsonPropertyName("leave_counts_as_workday_snapshot")] public bool LeaveCountsAsWorkdaySnapshot { get; init; }
    [JsonPropertyName("leave_requires_approval_snapshot")] public bool LeaveRequiresApprovalSnapshot { get; init; }
    [JsonPropertyName("leave_tracks_balance_snapshot")] public bool LeaveTracksBalanceSnapshot { get; init; }
    [JsonPropertyName("leave_allow_negative_balance_snapshot")] public bool LeaveAllowNegativeBalanceSnapshot { get; init; }
    [JsonPropertyName("date_from")] public string DateFrom { get; init; } = string.Empty;
    [JsonPropertyName("date_to")] public string DateTo { get; init; } = string.Empty;
    [JsonPropertyName("day_part")] public string DayPart { get; init; } = "FULL_DAY";
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("attachment_reference")] public string? AttachmentReference { get; init; }
    [JsonPropertyName("attachment_url")] public string? AttachmentUrl { get; init; }
    [JsonPropertyName("request_source")] public string RequestSource { get; init; } = "SELF_SERVICE";
    [JsonPropertyName("manual_approver_name")] public string? ManualApproverName { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "SUBMITTED";
    [JsonPropertyName("requested_by_actor_id")] public string RequestedByActorId { get; init; } = string.Empty;
    [JsonPropertyName("requested_by_employee_id")] public string? RequestedByEmployeeId { get; init; }
    [JsonPropertyName("reviewed_by_actor_id")] public string? ReviewedByActorId { get; init; }
    [JsonPropertyName("review_reason")] public string? ReviewReason { get; init; }
    [JsonPropertyName("reviewed_at")] public string? ReviewedAt { get; init; }
    [JsonPropertyName("cancelled_by_actor_id")] public string? CancelledByActorId { get; init; }
    [JsonPropertyName("cancel_reason")] public string? CancelReason { get; init; }
    [JsonPropertyName("cancelled_at")] public string? CancelledAt { get; init; }
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

public sealed record LeaveBalanceData
{
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("employee_code")] public string EmployeeCode { get; init; } = string.Empty;
    [JsonPropertyName("employee_name")] public string EmployeeName { get; init; } = string.Empty;
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
    [JsonPropertyName("leave_type_id")] public string LeaveTypeId { get; init; } = string.Empty;
    [JsonPropertyName("leave_type_code")] public string LeaveTypeCode { get; init; } = string.Empty;
    [JsonPropertyName("leave_type_name")] public string LeaveTypeName { get; init; } = string.Empty;
    [JsonPropertyName("balance_days")] public decimal BalanceDays { get; init; }
    [JsonPropertyName("allow_negative_balance")] public bool AllowNegativeBalance { get; init; }
    [JsonPropertyName("last_activity_date")] public string? LastActivityDate { get; init; }
}

public sealed record LeaveBalanceEntryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("employee_code")] public string? EmployeeCode { get; init; }
    [JsonPropertyName("employee_name")] public string? EmployeeName { get; init; }
    [JsonPropertyName("leave_type_id")] public string LeaveTypeId { get; init; } = string.Empty;
    [JsonPropertyName("leave_type_code_snapshot")] public string LeaveTypeCodeSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("leave_type_name_snapshot")] public string LeaveTypeNameSnapshot { get; init; } = string.Empty;
    [JsonPropertyName("entry_type")] public string EntryType { get; init; } = string.Empty;
    [JsonPropertyName("quantity_days")] public decimal QuantityDays { get; init; }
    [JsonPropertyName("effective_date")] public string EffectiveDate { get; init; } = string.Empty;
    [JsonPropertyName("source_type")] public string? SourceType { get; init; }
    [JsonPropertyName("source_id")] public string? SourceId { get; init; }
    [JsonPropertyName("reverses_entry_id")] public string? ReversesEntryId { get; init; }
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
}

public sealed record LeaveRequestListResponseData
{
    [JsonPropertyName("selectedEmployee")] public LeaveSelectedEmployeeData? SelectedEmployee { get; init; }
    [JsonPropertyName("branches")] public LeaveBranchData[] Branches { get; init; } = [];
    [JsonPropertyName("employees")] public LeaveEmployeeData[] Employees { get; init; } = [];
    [JsonPropertyName("balanceAsOfDate")] public string? BalanceAsOfDate { get; init; }
    [JsonPropertyName("leaveBalances")] public LeaveBalanceData[] LeaveBalances { get; init; } = [];
    [JsonPropertyName("balanceEntries")] public LeaveBalanceEntryData[] BalanceEntries { get; init; } = [];
    [JsonPropertyName("pagination")] public LeavePaginationData Pagination { get; init; } = new();
    [JsonPropertyName("requests")] public LeaveRequestData[] Requests { get; init; } = [];
    [JsonPropertyName("capabilities")] public LeaveRequestCapabilitiesData Capabilities { get; init; } = new();
}

public sealed record LeaveBalanceCapabilitiesData
{
    [JsonPropertyName("selfOnly")] public bool SelfOnly { get; init; }
    [JsonPropertyName("canManage")] public bool CanManage { get; init; }
}

public sealed record LeaveBalanceResponseData
{
    [JsonPropertyName("asOfDate")] public string AsOfDate { get; init; } = string.Empty;
    [JsonPropertyName("balances")] public LeaveBalanceData[] Balances { get; init; } = [];
    [JsonPropertyName("entries")] public LeaveBalanceEntryData[] Entries { get; init; } = [];
    [JsonPropertyName("capabilities")] public LeaveBalanceCapabilitiesData Capabilities { get; init; } = new();
}

public sealed record SubmitLeaveRequestRequest
{
    [JsonPropertyName("leaveTypeId")] public string LeaveTypeId { get; init; } = string.Empty;
    [JsonPropertyName("dateFrom")] public string DateFrom { get; init; } = string.Empty;
    [JsonPropertyName("dateTo")] public string DateTo { get; init; } = string.Empty;
    [JsonPropertyName("dayPart")] public string DayPart { get; init; } = "FULL_DAY";
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("attachmentReference")] public string? AttachmentReference { get; init; }
}

public sealed record SubmitManualLeaveRequest
{
    [JsonPropertyName("employeeId")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("leaveTypeId")] public string LeaveTypeId { get; init; } = string.Empty;
    [JsonPropertyName("dateFrom")] public string DateFrom { get; init; } = string.Empty;
    [JsonPropertyName("dateTo")] public string DateTo { get; init; } = string.Empty;
    [JsonPropertyName("dayPart")] public string DayPart { get; init; } = "FULL_DAY";
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
    [JsonPropertyName("attachmentReference")] public string? AttachmentReference { get; init; }
    [JsonPropertyName("paperApproved")] public bool PaperApproved { get; init; }
    [JsonPropertyName("manualApproverName")] public string? ManualApproverName { get; init; }
    [JsonPropertyName("manualApprovedDate")] public string? ManualApprovedDate { get; init; }
}

public sealed record LeaveAttachmentUploadData
{
    [JsonPropertyName("objectKey")] public string ObjectKey { get; init; } = string.Empty;
    [JsonPropertyName("publicUrl")] public string PublicUrl { get; init; } = string.Empty;
    [JsonPropertyName("fileName")] public string FileName { get; init; } = string.Empty;
    [JsonPropertyName("mimeType")] public string MimeType { get; init; } = string.Empty;
    [JsonPropertyName("byteSize")] public long ByteSize { get; init; }
    [JsonPropertyName("checksumSha256")] public string ChecksumSha256 { get; init; } = string.Empty;
}

public sealed record ReviewLeaveRequestRequest
{
    [JsonPropertyName("requestId")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("action")] public string Action { get; init; } = string.Empty;
    [JsonPropertyName("expectedVersion")] public int ExpectedVersion { get; init; }
    [JsonPropertyName("reviewReason")] public string? ReviewReason { get; init; }
}

public sealed record CancelLeaveRequestRequest
{
    [JsonPropertyName("requestId")] public string RequestId { get; init; } = string.Empty;
    [JsonPropertyName("expectedVersion")] public int ExpectedVersion { get; init; }
    [JsonPropertyName("cancelReason")] public string CancelReason { get; init; } = string.Empty;
}

public sealed record CreateLeaveTypeRequest
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("isActive")] public bool IsActive { get; init; }
    [JsonPropertyName("isPaid")] public bool IsPaid { get; init; }
    [JsonPropertyName("countsAsWorkday")] public bool CountsAsWorkday { get; init; }
    [JsonPropertyName("requiresApproval")] public bool RequiresApproval { get; init; }
    [JsonPropertyName("allowsFullDay")] public bool AllowsFullDay { get; init; }
    [JsonPropertyName("allowsHalfDay")] public bool AllowsHalfDay { get; init; }
    [JsonPropertyName("requiresAttachment")] public bool RequiresAttachment { get; init; }
    [JsonPropertyName("tracksBalance")] public bool TracksBalance { get; init; }
    [JsonPropertyName("allowNegativeBalance")] public bool AllowNegativeBalance { get; init; }
}

public sealed record UpdateLeaveTypeRequest
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("expectedVersion")] public int ExpectedVersion { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("isActive")] public bool IsActive { get; init; }
    [JsonPropertyName("isPaid")] public bool IsPaid { get; init; }
    [JsonPropertyName("countsAsWorkday")] public bool CountsAsWorkday { get; init; }
    [JsonPropertyName("requiresApproval")] public bool RequiresApproval { get; init; }
    [JsonPropertyName("allowsFullDay")] public bool AllowsFullDay { get; init; }
    [JsonPropertyName("allowsHalfDay")] public bool AllowsHalfDay { get; init; }
    [JsonPropertyName("requiresAttachment")] public bool RequiresAttachment { get; init; }
    [JsonPropertyName("tracksBalance")] public bool TracksBalance { get; init; }
    [JsonPropertyName("allowNegativeBalance")] public bool AllowNegativeBalance { get; init; }
}

public sealed record PostLeaveBalanceEntryRequest
{
    [JsonPropertyName("employeeId")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("leaveTypeId")] public string LeaveTypeId { get; init; } = string.Empty;
    [JsonPropertyName("entryType")] public string EntryType { get; init; } = string.Empty;
    [JsonPropertyName("days")] public decimal Days { get; init; }
    [JsonPropertyName("effectiveDate")] public string EffectiveDate { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
}
