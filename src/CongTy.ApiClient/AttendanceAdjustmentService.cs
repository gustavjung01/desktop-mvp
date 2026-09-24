using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IAttendanceAdjustmentService
{
    Task<AttendanceAdjustmentListResponseData> ListAsync(
        string fromDate,
        string toDate,
        string? status = null,
        string? employeeId = null,
        string? employeeQuery = null,
        string? branchId = null,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);
    Task<AttendanceAdjustmentRequestData> SubmitOwnAsync(
        SubmitAttendanceAdjustmentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<AttendanceAdjustmentMutationResultData> ReviewAsync(
        ReviewAttendanceAdjustmentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<AttendanceAdjustmentMutationResultData> DirectAsync(
        DirectAttendanceAdjustmentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<AttendancePeriodLockListResponseData> ListLocksAsync(
        string? fromDate = null,
        string? toDate = null,
        string? branchId = null,
        CancellationToken cancellationToken = default);
    Task<AttendancePeriodLockData> LockPeriodAsync(
        CreateAttendancePeriodLockRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class AttendanceAdjustmentService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IAttendanceAdjustmentService
{
    public Task<AttendanceAdjustmentListResponseData> ListAsync(
        string fromDate,
        string toDate,
        string? status = null,
        string? employeeId = null,
        string? employeeQuery = null,
        string? branchId = null,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"from={Uri.EscapeDataString(fromDate)}",
            $"to={Uri.EscapeDataString(toDate)}",
            $"limit={Math.Clamp(limit, 1, 100)}",
            $"offset={Math.Max(0, offset)}",
        };
        AddQuery(query, "status", status);
        AddQuery(query, "employeeId", employeeId);
        AddQuery(query, "employeeQuery", employeeQuery);
        AddQuery(query, "branchId", branchId);

        return apiClient.GetDataAsync<AttendanceAdjustmentListResponseData>(
            "/api/workforce/attendance/adjustments?" + string.Join("&", query),
            RequireToken(),
            cancellationToken);
    }

    public Task<AttendanceAdjustmentRequestData> SubmitOwnAsync(
        SubmitAttendanceAdjustmentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<SubmitAttendanceAdjustmentRequest, AttendanceAdjustmentRequestData>(
            "/api/workforce/attendance/adjustments",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AttendanceAdjustmentMutationResultData> ReviewAsync(
        ReviewAttendanceAdjustmentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<ReviewAttendanceAdjustmentRequest, AttendanceAdjustmentMutationResultData>(
            "/api/workforce/attendance/adjustments/review",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AttendanceAdjustmentMutationResultData> DirectAsync(
        DirectAttendanceAdjustmentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<DirectAttendanceAdjustmentRequest, AttendanceAdjustmentMutationResultData>(
            "/api/workforce/attendance/adjustments/direct",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AttendancePeriodLockListResponseData> ListLocksAsync(
        string? fromDate = null,
        string? toDate = null,
        string? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddQuery(query, "from", fromDate);
        AddQuery(query, "to", toDate);
        AddQuery(query, "branchId", branchId);
        var suffix = query.Count == 0 ? string.Empty : "?" + string.Join("&", query);
        return apiClient.GetDataAsync<AttendancePeriodLockListResponseData>(
            "/api/workforce/attendance/period-locks" + suffix,
            RequireToken(),
            cancellationToken);
    }

    public Task<AttendancePeriodLockData> LockPeriodAsync(
        CreateAttendancePeriodLockRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<CreateAttendancePeriodLockRequest, AttendancePeriodLockData>(
            "/api/workforce/attendance/period-locks",
            request,
            idempotencyKey,
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
