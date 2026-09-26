using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IAttendanceService
{
    Task<AttendanceTodayData> GetTodayAsync(CancellationToken cancellationToken = default);
    Task<AttendancePointManagementData> GetPointManagementAsync(CancellationToken cancellationToken = default);
    Task<AttendanceRecordResultData> RecordAsync(
        AttendanceRecordRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<AttendanceRecordResultData> RecordManagedManualAsync(
        ManagedManualAttendanceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<AttendancePointData> CreatePointAsync(
        CreateAttendancePointRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<AttendanceQrTokenData> IssueQrTokenAsync(
        CreateAttendanceQrTokenRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class AttendanceService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IAttendanceService
{
    public Task<AttendanceTodayData> GetTodayAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<AttendanceTodayData>(
            "/api/workforce/attendance/today",
            RequireToken(),
            cancellationToken);

    public Task<AttendancePointManagementData> GetPointManagementAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<AttendancePointManagementData>(
            "/api/workforce/attendance/points",
            RequireToken(),
            cancellationToken);

    public Task<AttendanceRecordResultData> RecordAsync(
        AttendanceRecordRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<AttendanceRecordRequest, AttendanceRecordResultData>(
            "/api/workforce/attendance/record",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AttendanceRecordResultData> RecordManagedManualAsync(
        ManagedManualAttendanceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<ManagedManualAttendanceRequest, AttendanceRecordResultData>(
            "/api/workforce/attendance/manual",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AttendancePointData> CreatePointAsync(
        CreateAttendancePointRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<CreateAttendancePointRequest, AttendancePointData>(
            "/api/workforce/attendance/points",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<AttendanceQrTokenData> IssueQrTokenAsync(
        CreateAttendanceQrTokenRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<CreateAttendanceQrTokenRequest, AttendanceQrTokenData>(
            "/api/workforce/attendance/qr-token",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
