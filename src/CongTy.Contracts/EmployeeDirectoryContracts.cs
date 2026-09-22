using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record EmployeeEmploymentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("employee_id")] public string EmployeeId { get; init; } = string.Empty;
    [JsonPropertyName("employment_type")] public string EmploymentType { get; init; } = "OTHER";
    [JsonPropertyName("effective_from")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("effective_to")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("end_reason")] public string? EndReason { get; init; }
    [JsonPropertyName("data_quality")] public string? DataQuality { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
    [JsonPropertyName("source_reference")] public string? SourceReference { get; init; }
    [JsonPropertyName("created_at")] public string? CreatedAt { get; init; }
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
}

public sealed record EmployeeAssignmentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string? InstallationId { get; init; }
    [JsonPropertyName("employee_id")] public string? EmployeeId { get; init; }
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("department_id")] public string? DepartmentId { get; init; }
    [JsonPropertyName("position_id")] public string? PositionId { get; init; }
    [JsonPropertyName("manager_employee_id")] public string? ManagerEmployeeId { get; init; }
    [JsonPropertyName("effective_from")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("effective_to")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("data_quality")] public string? DataQuality { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
    [JsonPropertyName("source_reference")] public string? SourceReference { get; init; }
    [JsonPropertyName("created_at")] public string? CreatedAt { get; init; }
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("branch_code")] public string? BranchCode { get; init; }
    [JsonPropertyName("branch_name")] public string? BranchName { get; init; }
    [JsonPropertyName("department_code")] public string? DepartmentCode { get; init; }
    [JsonPropertyName("department_name")] public string? DepartmentName { get; init; }
    [JsonPropertyName("position_code")] public string? PositionCode { get; init; }
    [JsonPropertyName("position_name")] public string? PositionName { get; init; }
    [JsonPropertyName("manager_code")] public string? ManagerCode { get; init; }
    [JsonPropertyName("manager_name")] public string? ManagerName { get; init; }
}

public sealed record EmployeeDirectoryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("full_name")] public string FullName { get; init; } = string.Empty;
    [JsonPropertyName("job_title")] public string? JobTitle { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("branch_id")] public string? BranchId { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
    [JsonPropertyName("employment_history")] public EmployeeEmploymentData[] EmploymentHistory { get; init; } = [];
    [JsonPropertyName("assignment_history")] public EmployeeAssignmentData[] AssignmentHistory { get; init; } = [];
    [JsonPropertyName("current_employment")] public EmployeeEmploymentData? CurrentEmployment { get; init; }
    [JsonPropertyName("current_assignment")] public EmployeeAssignmentData? CurrentAssignment { get; init; }
}

public sealed record EmployeeDirectoryBranchData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
}

public sealed record HrDepartmentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("parent_department_id")] public string? ParentDepartmentId { get; init; }
    [JsonPropertyName("parent_code")] public string? ParentCode { get; init; }
    [JsonPropertyName("parent_name")] public string? ParentName { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
}

public sealed record HrPositionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("installation_id")] public string InstallationId { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("department_id")] public string? DepartmentId { get; init; }
    [JsonPropertyName("department_code")] public string? DepartmentCode { get; init; }
    [JsonPropertyName("department_name")] public string? DepartmentName { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("updated_by")] public string? UpdatedBy { get; init; }
}

public sealed record EmployeeManagerCandidateData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("full_name")] public string FullName { get; init; } = string.Empty;
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("department_id")] public string? DepartmentId { get; init; }
    [JsonPropertyName("department_code")] public string? DepartmentCode { get; init; }
    [JsonPropertyName("department_name")] public string? DepartmentName { get; init; }
    [JsonPropertyName("position_id")] public string? PositionId { get; init; }
    [JsonPropertyName("position_code")] public string? PositionCode { get; init; }
    [JsonPropertyName("position_name")] public string? PositionName { get; init; }
}

public sealed record EmployeeOrganizationCatalogData
{
    [JsonPropertyName("departments")] public HrDepartmentData[] Departments { get; init; } = [];
    [JsonPropertyName("positions")] public HrPositionData[] Positions { get; init; } = [];
    [JsonPropertyName("managers")] public EmployeeManagerCandidateData[] Managers { get; init; } = [];
}

public sealed record EmployeeOrganizationMutationRequest(
    [property: JsonPropertyName("resource")] string Resource,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("parentDepartmentId")] string? ParentDepartmentId = null,
    [property: JsonPropertyName("departmentId")] string? DepartmentId = null,
    [property: JsonPropertyName("isActive")] bool? IsActive = null,
    [property: JsonPropertyName("expectedUpdatedAt")] string? ExpectedUpdatedAt = null);

public sealed record EmployeeOrganizationMutationResult
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("parent_department_id")] public string? ParentDepartmentId { get; init; }
    [JsonPropertyName("department_id")] public string? DepartmentId { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record EmployeeDirectoryCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("jobTitle")] string? JobTitle,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("branchId")] string? BranchId,
    [property: JsonPropertyName("departmentId")] string? DepartmentId = null,
    [property: JsonPropertyName("positionId")] string? PositionId = null,
    [property: JsonPropertyName("managerEmployeeId")] string? ManagerEmployeeId = null,
    [property: JsonPropertyName("employmentStartDate")] string? EmploymentStartDate = null,
    [property: JsonPropertyName("employmentType")] string? EmploymentType = null,
    [property: JsonPropertyName("assignmentEffectiveFrom")] string? AssignmentEffectiveFrom = null,
    [property: JsonPropertyName("assignmentReason")] string? AssignmentReason = null);

public sealed record EmployeeDirectoryUpdateRequest(
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("jobTitle")] string? JobTitle,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("branchId")] string? BranchId,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt,
    [property: JsonPropertyName("departmentId")] string? DepartmentId = null,
    [property: JsonPropertyName("positionId")] string? PositionId = null,
    [property: JsonPropertyName("managerEmployeeId")] string? ManagerEmployeeId = null,
    [property: JsonPropertyName("confirmEmployment")] bool? ConfirmEmployment = null,
    [property: JsonPropertyName("employmentEffectiveFrom")] string? EmploymentEffectiveFrom = null,
    [property: JsonPropertyName("employmentEffectiveTo")] string? EmploymentEffectiveTo = null,
    [property: JsonPropertyName("employmentType")] string? EmploymentType = null,
    [property: JsonPropertyName("employmentEndReason")] string? EmploymentEndReason = null,
    [property: JsonPropertyName("confirmAssignment")] bool? ConfirmAssignment = null,
    [property: JsonPropertyName("assignmentEffectiveFrom")] string? AssignmentEffectiveFrom = null,
    [property: JsonPropertyName("assignmentReason")] string? AssignmentReason = null);

public sealed record EmployeeDirectoryToggleRequest(
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt,
    [property: JsonPropertyName("employmentEffectiveDate")] string? EmploymentEffectiveDate = null,
    [property: JsonPropertyName("employmentReason")] string? EmploymentReason = null,
    [property: JsonPropertyName("employmentType")] string? EmploymentType = null);
