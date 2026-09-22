using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IWorkScheduleService
{
    Task<IReadOnlyList<EmployeeDirectoryData>> ListEmployeesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkPolicyData>> ListPoliciesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkScheduleData>> ListSchedulesAsync(
        string fromDate,
        string toDate,
        string? employeeId = null,
        CancellationToken cancellationToken = default);
    Task<SchedulePlanningCatalogData> GetPlanningCatalogAsync(CancellationToken cancellationToken = default);
    Task<WorkScheduleData> SaveScheduleAsync(
        WorkScheduleMutationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<WorkShiftTemplateData> SaveShiftTemplateAsync(
        SaveShiftTemplateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<WorkWeekTemplateData> SaveWeekTemplateAsync(
        SaveWeekTemplateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<CompanyCalendarDayData> SaveCalendarDayAsync(
        SaveCalendarDayRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<ScheduleBulkResultData> ApplyWeekTemplateAsync(
        ApplyWeekTemplateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<ScheduleBulkResultData> CopyScheduleAsync(
        CopyScheduleRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class WorkScheduleService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IWorkScheduleService
{
    public async Task<IReadOnlyList<EmployeeDirectoryData>> ListEmployeesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<EmployeeDirectoryData[]>(
            "/api/employees?active=true&limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<WorkPolicyData>> ListPoliciesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<WorkPolicyData[]>(
            "/api/workforce/policies",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<WorkScheduleData>> ListSchedulesAsync(
        string fromDate,
        string toDate,
        string? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"/api/workforce/schedules?from={Uri.EscapeDataString(fromDate)}&to={Uri.EscapeDataString(toDate)}";
        if (!string.IsNullOrWhiteSpace(employeeId))
            query += $"&employeeId={Uri.EscapeDataString(employeeId.Trim())}";

        return await apiClient.GetDataAsync<WorkScheduleData[]>(
            query,
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
    }

    public Task<SchedulePlanningCatalogData> GetPlanningCatalogAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<SchedulePlanningCatalogData>(
            "/api/workforce/schedule-planning",
            RequireToken(),
            cancellationToken);

    public Task<WorkScheduleData> SaveScheduleAsync(
        WorkScheduleMutationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<WorkScheduleMutationRequest, WorkScheduleData>(
            "/api/workforce/schedules",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<WorkShiftTemplateData> SaveShiftTemplateAsync(
        SaveShiftTemplateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SavePlanningAsync<SaveShiftTemplateRequest, WorkShiftTemplateData>(request, idempotencyKey, cancellationToken);

    public Task<WorkWeekTemplateData> SaveWeekTemplateAsync(
        SaveWeekTemplateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SavePlanningAsync<SaveWeekTemplateRequest, WorkWeekTemplateData>(request, idempotencyKey, cancellationToken);

    public Task<CompanyCalendarDayData> SaveCalendarDayAsync(
        SaveCalendarDayRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SavePlanningAsync<SaveCalendarDayRequest, CompanyCalendarDayData>(request, idempotencyKey, cancellationToken);

    public Task<ScheduleBulkResultData> ApplyWeekTemplateAsync(
        ApplyWeekTemplateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SavePlanningAsync<ApplyWeekTemplateRequest, ScheduleBulkResultData>(request, idempotencyKey, cancellationToken);

    public Task<ScheduleBulkResultData> CopyScheduleAsync(
        CopyScheduleRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SavePlanningAsync<CopyScheduleRequest, ScheduleBulkResultData>(request, idempotencyKey, cancellationToken);

    private Task<TResponse> SavePlanningAsync<TRequest, TResponse>(
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        apiClient.PostIdempotentDataAsync<TRequest, TResponse>(
            "/api/workforce/schedule-planning",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
