using System.IO;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ILeaveService
{
    Task<LeaveTypeListResponseData> ListLeaveTypesAsync(CancellationToken cancellationToken = default);
    Task<LeaveRequestListResponseData> ListLeaveRequestsAsync(
        string fromDate,
        string toDate,
        string? status = null,
        string? employeeQuery = null,
        string? branchId = null,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);
    Task<LeaveBalanceResponseData> ListLeaveBalancesAsync(
        string asOfDate,
        string? employeeId = null,
        string? employeeQuery = null,
        string? leaveTypeId = null,
        CancellationToken cancellationToken = default);
    Task<LeaveRequestData> SubmitLeaveRequestAsync(
        SubmitLeaveRequestRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<LeaveRequestData> SubmitManualLeaveRequestAsync(
        SubmitManualLeaveRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<LeaveAttachmentUploadData> UploadLeaveAttachmentAsync(
        byte[] content,
        string fileName,
        string contentType,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<LeaveRequestData> ReviewLeaveRequestAsync(
        ReviewLeaveRequestRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<LeaveRequestData> CancelLeaveRequestAsync(
        CancelLeaveRequestRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<LeaveTypeData> CreateLeaveTypeAsync(
        CreateLeaveTypeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<LeaveTypeData> UpdateLeaveTypeAsync(
        UpdateLeaveTypeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<LeaveBalanceEntryData> PostLeaveBalanceEntryAsync(
        PostLeaveBalanceEntryRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class LeaveService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : ILeaveService
{
    public Task<LeaveTypeListResponseData> ListLeaveTypesAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<LeaveTypeListResponseData>(
            "/api/workforce/leave-types",
            RequireToken(),
            cancellationToken);

    public Task<LeaveRequestListResponseData> ListLeaveRequestsAsync(
        string fromDate,
        string toDate,
        string? status = null,
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
            $"offset={Math.Max(0, offset)}"
        };
        AddQuery(query, "status", status);
        AddQuery(query, "employeeQuery", employeeQuery);
        AddQuery(query, "branchId", branchId);

        return apiClient.GetDataAsync<LeaveRequestListResponseData>(
            "/api/workforce/leave/requests?" + string.Join("&", query),
            RequireToken(),
            cancellationToken);
    }

    public Task<LeaveBalanceResponseData> ListLeaveBalancesAsync(
        string asOfDate,
        string? employeeId = null,
        string? employeeQuery = null,
        string? leaveTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"asOfDate={Uri.EscapeDataString(asOfDate)}"
        };
        AddQuery(query, "employeeId", employeeId);
        AddQuery(query, "employeeQuery", employeeQuery);
        AddQuery(query, "leaveTypeId", leaveTypeId);

        return apiClient.GetDataAsync<LeaveBalanceResponseData>(
            "/api/workforce/leave/balances?" + string.Join("&", query),
            RequireToken(),
            cancellationToken);
    }

    public Task<LeaveRequestData> SubmitLeaveRequestAsync(
        SubmitLeaveRequestRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<SubmitLeaveRequestRequest, LeaveRequestData>(
            "/api/workforce/leave/requests",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<LeaveRequestData> SubmitManualLeaveRequestAsync(
        SubmitManualLeaveRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<SubmitManualLeaveRequest, LeaveRequestData>(
            "/api/workforce/leave/requests/manual",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<LeaveAttachmentUploadData> UploadLeaveAttachmentAsync(
        byte[] content,
        string fileName,
        string contentType,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(fileName?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeFileName))
            throw new ArgumentException("Tên chứng từ nghỉ không hợp lệ.", nameof(fileName));

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["x-file-name"] = Uri.EscapeDataString(safeFileName)
        };
        return apiClient.PutBytesIdempotentDataAsync<LeaveAttachmentUploadData>(
            "/api/workforce/leave/attachments",
            content,
            contentType,
            idempotencyKey,
            RequireToken(),
            cancellationToken,
            headers);
    }

    public Task<LeaveRequestData> ReviewLeaveRequestAsync(
        ReviewLeaveRequestRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<ReviewLeaveRequestRequest, LeaveRequestData>(
            "/api/workforce/leave/requests/review",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<LeaveRequestData> CancelLeaveRequestAsync(
        CancelLeaveRequestRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<CancelLeaveRequestRequest, LeaveRequestData>(
            "/api/workforce/leave/requests/cancel",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<LeaveTypeData> CreateLeaveTypeAsync(
        CreateLeaveTypeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<CreateLeaveTypeRequest, LeaveTypeData>(
            "/api/workforce/leave-types",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<LeaveTypeData> UpdateLeaveTypeAsync(
        UpdateLeaveTypeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<UpdateLeaveTypeRequest, LeaveTypeData>(
            "/api/workforce/leave-types/update",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    public Task<LeaveBalanceEntryData> PostLeaveBalanceEntryAsync(
        PostLeaveBalanceEntryRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<PostLeaveBalanceEntryRequest, LeaveBalanceEntryData>(
            "/api/workforce/leave/balances/entries",
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
