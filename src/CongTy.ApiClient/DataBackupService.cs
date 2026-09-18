using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IDataBackupService
{
    Task<ApiDownloadFile> ExportBusinessDataAsync(CancellationToken cancellationToken = default);
    Task<TechnicalBackupAccessData> GetTechnicalAccessAsync(string? unlockToken, CancellationToken cancellationToken = default);
    Task<TechnicalBackupChallengeData> RequestTechnicalChallengeAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TechnicalBackupUnlockData> VerifyTechnicalChallengeAsync(string challengeId, string code, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataBackupJobData>> ListBackupsAsync(string unlockToken, CancellationToken cancellationToken = default);
    Task<DataBackupJobData> CreateBackupAsync(string unlockToken, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataBackupDownloadData> CreateDownloadAsync(string jobId, string artifactType, string unlockToken, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataDeletionIntentData> CreateDeletionIntentAsync(string backupJobId, string targetCode, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataDeletionIntentData> VerifyDeletionIntentAsync(string intentId, string code, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataDeletionIntentData> ExecuteDeletionIntentAsync(string intentId, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class DataBackupService(
    CompanyApiClient apiClient,
    ICompanyEndpointProvider endpointProvider,
    IAuthenticatedSessionAccessor sessionAccessor) : IDataBackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string TechnicalUnlockHeader = "x-technical-backup-unlock";

    public Task<ApiDownloadFile> ExportBusinessDataAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetFileAsync("/api/reporting/business-export", RequireToken(), cancellationToken);

    public Task<TechnicalBackupAccessData> GetTechnicalAccessAsync(
        string? unlockToken,
        CancellationToken cancellationToken = default) =>
        SendAsync<TechnicalBackupAccessData>(
            HttpMethod.Get,
            "/api/backups/technical-access",
            payload: null,
            idempotencyKey: null,
            unlockToken: unlockToken,
            cancellationToken: cancellationToken);

    public Task<TechnicalBackupChallengeData> RequestTechnicalChallengeAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SendAsync<TechnicalBackupChallengeData>(
            HttpMethod.Post,
            "/api/backups/technical-access/challenges",
            new { },
            idempotencyKey,
            unlockToken: null,
            cancellationToken: cancellationToken);

    public Task<TechnicalBackupUnlockData> VerifyTechnicalChallengeAsync(
        string challengeId,
        string code,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SendAsync<TechnicalBackupUnlockData>(
            HttpMethod.Post,
            $"/api/backups/technical-access/challenges/{RequiredId(challengeId)}/verify",
            new TechnicalBackupCodeRequest { Code = code },
            idempotencyKey,
            unlockToken: null,
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<DataBackupJobData>> ListBackupsAsync(
        string unlockToken,
        CancellationToken cancellationToken = default) =>
        await SendAsync<DataBackupJobData[]>(
            HttpMethod.Get,
            "/api/backups?limit=20",
            payload: null,
            idempotencyKey: null,
            unlockToken: RequiredUnlock(unlockToken),
            cancellationToken: cancellationToken).ConfigureAwait(false);

    public Task<DataBackupJobData> CreateBackupAsync(
        string unlockToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SendAsync<DataBackupJobData>(
            HttpMethod.Post,
            "/api/backups",
            new { },
            idempotencyKey,
            unlockToken: RequiredUnlock(unlockToken),
            cancellationToken: cancellationToken);

    public Task<DataBackupDownloadData> CreateDownloadAsync(
        string jobId,
        string artifactType,
        string unlockToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = artifactType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedType is not ("database" or "manifest"))
            throw new ArgumentException("Loại tệp sao lưu không hợp lệ.", nameof(artifactType));

        return SendAsync<DataBackupDownloadData>(
            HttpMethod.Post,
            $"/api/backups/{RequiredId(jobId)}/download",
            new DataBackupDownloadRequest { ArtifactType = normalizedType },
            idempotencyKey,
            unlockToken: RequiredUnlock(unlockToken),
            cancellationToken: cancellationToken);
    }

    public Task<DataDeletionIntentData> CreateDeletionIntentAsync(
        string backupJobId,
        string targetCode,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SendAsync<DataDeletionIntentData>(
            HttpMethod.Post,
            "/api/data-deletions",
            new DataDeletionCreateRequest
            {
                BackupJobId = RequiredId(backupJobId),
                TargetCode = targetCode?.Trim() ?? string.Empty,
                Reason = reason?.Trim() ?? string.Empty
            },
            idempotencyKey,
            unlockToken: null,
            cancellationToken: cancellationToken);

    public Task<DataDeletionIntentData> VerifyDeletionIntentAsync(
        string intentId,
        string code,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SendAsync<DataDeletionIntentData>(
            HttpMethod.Post,
            $"/api/data-deletions/{RequiredId(intentId)}/verify",
            new TechnicalBackupCodeRequest { Code = code },
            idempotencyKey,
            unlockToken: null,
            cancellationToken: cancellationToken);

    public Task<DataDeletionIntentData> ExecuteDeletionIntentAsync(
        string intentId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        SendAsync<DataDeletionIntentData>(
            HttpMethod.Post,
            $"/api/data-deletions/{RequiredId(intentId)}/execute",
            new { },
            idempotencyKey,
            unlockToken: null,
            cancellationToken: cancellationToken);

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string relativePath,
        object? payload,
        string? idempotencyKey,
        string? unlockToken,
        CancellationToken cancellationToken)
    {
        var baseUri = endpointProvider.BaseUri
            ?? throw new InvalidOperationException("Kết nối Công Ty chưa được cấu hình.");

        using var request = new HttpRequestMessage(method, new Uri(baseUri, relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", RequireToken());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey.Trim());
        if (!string.IsNullOrWhiteSpace(unlockToken))
            request.Headers.TryAddWithoutValidation(TechnicalUnlockHeader, unlockToken.Trim());
        if (payload is not null)
            request.Content = JsonContent.Create(payload, payload.GetType(), options: JsonOptions);

        using var response = await apiClient.HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var envelope = await response.Content
                .ReadFromJsonAsync<ApiSuccessEnvelope<T>>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (envelope is null)
                throw new InvalidOperationException("Phản hồi sao lưu không đúng định dạng.");
            return envelope.Data;
        }

        ApiErrorEnvelope? error = null;
        try
        {
            error = await response.Content
                .ReadFromJsonAsync<ApiErrorEnvelope>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
        }

        if (error is null)
            throw new InvalidOperationException("Hệ thống trả về lỗi nhưng nội dung phản hồi không đúng định dạng.");

        throw new CanonicalApiException(
            response.StatusCode,
            error.Error.Code,
            error.Error.Message,
            error.RequestId,
            error.Error.Retryable,
            error.Error.Details);
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequiredUnlock(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("Cần mở khóa Khu vực kỹ thuật trước khi thao tác.")
            : value.Trim();

    private static string RequiredId(string value)
    {
        if (!Guid.TryParse(value, out var parsed))
            throw new ArgumentException("Mã tác vụ không hợp lệ.", nameof(value));
        return parsed.ToString("D");
    }
}
