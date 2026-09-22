using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IOvertimeCloseoutService
{
    Task<OvertimeListResponseData> ListOvertimeAsync(
        string fromDate,
        string toDate,
        string? status = null,
        string? employeeId = null,
        string? employeeQuery = null,
        string? branchId = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default);
    Task<OvertimeRequestData> SubmitOvertimeAsync(
        SubmitOvertimeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<OvertimeRequestData> ReviewOvertimeAsync(
        ReviewOvertimeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<OvertimeRequestData> RecordOvertimeActualAsync(
        RecordOvertimeActualRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<OvertimeRequestData> ConfirmOvertimeAsync(
        ConfirmOvertimeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<AttendancePeriodListResponseData> ListAttendancePeriodsAsync(
        string fromDate,
        string toDate,
        string? branchId = null,
        CancellationToken cancellationToken = default);
    Task<AttendancePeriodMutationResponseData> MutateAttendancePeriodAsync(
        AttendancePeriodMutationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<AttendancePayrollInputData> GetAttendancePayrollInputAsync(
        string periodId,
        CancellationToken cancellationToken = default);
}

public sealed class OvertimeCloseoutService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IOvertimeCloseoutService
{
    public Task<OvertimeListResponseData> ListOvertimeAsync(
        string fromDate,
        string toDate,
        string? status = null,
        string? employeeId = null,
        string? employeeQuery = null,
        string? branchId = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"from={Uri.EscapeDataString(fromDate)}",
            $"to={Uri.EscapeDataString(toDate)}",
            $"limit={Math.Clamp(limit, 1, 100)}",
            $"offset={Math.Max(0, offset)}"
        };
        AddQuery(query, "status", status);
        AddQuery(query, "employeeId", employeeId);
        AddQuery(query, "employeeQuery", employeeQuery);
        AddQuery(query, "branchId", branchId);

        return apiClient.GetDataAsync<OvertimeListResponseData>(
            "/api/workforce/overtime?" + string.Join("&", query),
            RequireToken(),
            cancellationToken);
    }

    public Task<OvertimeRequestData> SubmitOvertimeAsync(
        SubmitOvertimeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<SubmitOvertimeRequest, OvertimeRequestData>(
            "/api/workforce/overtime",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<OvertimeRequestData> ReviewOvertimeAsync(
        ReviewOvertimeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<ReviewOvertimeRequest, OvertimeRequestData>(
            "/api/workforce/overtime/review",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<OvertimeRequestData> RecordOvertimeActualAsync(
        RecordOvertimeActualRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<RecordOvertimeActualRequest, OvertimeRequestData>(
            "/api/workforce/overtime/actual",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<OvertimeRequestData> ConfirmOvertimeAsync(
        ConfirmOvertimeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<ConfirmOvertimeRequest, OvertimeRequestData>(
            "/api/workforce/overtime/confirm",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AttendancePeriodListResponseData> ListAttendancePeriodsAsync(
        string fromDate,
        string toDate,
        string? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"from={Uri.EscapeDataString(fromDate)}",
            $"to={Uri.EscapeDataString(toDate)}"
        };
        AddQuery(query, "branchId", branchId);

        return apiClient.GetDataAsync<AttendancePeriodListResponseData>(
            "/api/workforce/attendance/periods?" + string.Join("&", query),
            RequireToken(),
            cancellationToken);
    }

    public Task<AttendancePeriodMutationResponseData> MutateAttendancePeriodAsync(
        AttendancePeriodMutationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<AttendancePeriodMutationRequest, AttendancePeriodMutationResponseData>(
            "/api/workforce/attendance/periods",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AttendancePayrollInputData> GetAttendancePayrollInputAsync(
        string periodId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<AttendancePayrollInputData>(
            "/api/workforce/attendance/payroll-input?periodId=" + Uri.EscapeDataString(periodId.Trim()),
            RequireToken(),
            cancellationToken);

    private static void AddQuery(List<string> query, string key, string? value)
    {
        var normalized = value?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
            query.Add($"{key}={Uri.EscapeDataString(normalized)}");
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
