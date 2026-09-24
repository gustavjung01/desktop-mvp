using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IAttendanceViolationService
{
    Task<AttendanceViolationHandlingResponseData> ListAsync(
        string fromDate,
        string toDate,
        string? employeeQuery = null,
        string? branchId = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default);
    Task<AttendanceViolationCaseData> SubmitExplanationAsync(
        SubmitAttendanceViolationExplanationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<AttendanceViolationCaseData> ReviewAsync(
        ReviewAttendanceViolationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class AttendanceViolationService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IAttendanceViolationService
{
    public Task<AttendanceViolationHandlingResponseData> ListAsync(
        string fromDate,
        string toDate,
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
            $"offset={Math.Max(0, offset)}",
        };
        AddQuery(query, "employeeQuery", employeeQuery);
        AddQuery(query, "branchId", branchId);

        return apiClient.GetDataAsync<AttendanceViolationHandlingResponseData>(
            "/api/workforce/attendance/violations?" + string.Join("&", query),
            RequireToken(),
            cancellationToken);
    }

    public Task<AttendanceViolationCaseData> SubmitExplanationAsync(
        SubmitAttendanceViolationExplanationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<SubmitAttendanceViolationExplanationRequest, AttendanceViolationCaseData>(
            "/api/workforce/attendance/violations/explain",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AttendanceViolationCaseData> ReviewAsync(
        ReviewAttendanceViolationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<ReviewAttendanceViolationRequest, AttendanceViolationCaseData>(
            "/api/workforce/attendance/violations/review",
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
