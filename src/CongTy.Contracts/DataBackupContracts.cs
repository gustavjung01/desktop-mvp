using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record DataBackupArtifactData
{
    [JsonPropertyName("size")] public long? Size { get; init; }
    [JsonPropertyName("sha256")] public string? Sha256 { get; init; }
}

public sealed record DataBackupArtifactsData
{
    [JsonPropertyName("databaseDump")] public DataBackupArtifactData? DatabaseDump { get; init; }
    [JsonPropertyName("csvZip")] public DataBackupArtifactData? CsvZip { get; init; }
    [JsonPropertyName("xlsx")] public DataBackupArtifactData? Xlsx { get; init; }
    [JsonPropertyName("manifest")] public DataBackupArtifactData? Manifest { get; init; }
}

public sealed record DataBackupJobData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("requestedAt")] public string RequestedAt { get; init; } = string.Empty;
    [JsonPropertyName("startedAt")] public string? StartedAt { get; init; }
    [JsonPropertyName("completedAt")] public string? CompletedAt { get; init; }
    [JsonPropertyName("snapshotAt")] public string? SnapshotAt { get; init; }
    [JsonPropertyName("schemaVersion")] public string? SchemaVersion { get; init; }
    [JsonPropertyName("verifiedAt")] public string? VerifiedAt { get; init; }
    [JsonPropertyName("includeXlsx")] public bool IncludeXlsx { get; init; }
    [JsonPropertyName("datasetCount")] public int DatasetCount { get; init; }
    [JsonPropertyName("totalRowCount")] public long TotalRowCount { get; init; }
    [JsonPropertyName("failureCode")] public string? FailureCode { get; init; }
    [JsonPropertyName("failureMessage")] public string? FailureMessage { get; init; }
    [JsonPropertyName("artifacts")] public DataBackupArtifactsData Artifacts { get; init; } = new();
}

public sealed record TechnicalBackupAccessData
{
    [JsonPropertyName("unlocked")] public bool Unlocked { get; init; }
    [JsonPropertyName("expiresAt")] public string? ExpiresAt { get; init; }
}

public sealed record TechnicalBackupChallengeData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("challengeExpiresAt")] public string ChallengeExpiresAt { get; init; } = string.Empty;
    [JsonPropertyName("recipient")] public string Recipient { get; init; } = string.Empty;
}

public sealed record TechnicalBackupUnlockData
{
    [JsonPropertyName("token")] public string Token { get; init; } = string.Empty;
    [JsonPropertyName("expiresAt")] public string ExpiresAt { get; init; } = string.Empty;
}

public sealed record DataBackupDownloadData
{
    [JsonPropertyName("url")] public string Url { get; init; } = string.Empty;
    [JsonPropertyName("expiresIn")] public int ExpiresIn { get; init; }
}

public sealed record DataDeletionSummaryData
{
    [JsonPropertyName("deletedRows")] public long? DeletedRows { get; init; }
    [JsonPropertyName("affectedTableCount")] public int? AffectedTableCount { get; init; }
}

public sealed record DataDeletionIntentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("backupJobId")] public string BackupJobId { get; init; } = string.Empty;
    [JsonPropertyName("targetCode")] public string TargetCode { get; init; } = string.Empty;
    [JsonPropertyName("challengeExpiresAt")] public string? ChallengeExpiresAt { get; init; }
    [JsonPropertyName("ownerRecipientCount")] public int? OwnerRecipientCount { get; init; }
    [JsonPropertyName("authorizedAt")] public string? AuthorizedAt { get; init; }
    [JsonPropertyName("purgeExecuted")] public bool PurgeExecuted { get; init; }
    [JsonPropertyName("purgeStartedAt")] public string? PurgeStartedAt { get; init; }
    [JsonPropertyName("purgeCompletedAt")] public string? PurgeCompletedAt { get; init; }
    [JsonPropertyName("purgeSummary")] public DataDeletionSummaryData? PurgeSummary { get; init; }
}

public sealed record TechnicalBackupCodeRequest
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
}

public sealed record DataBackupDownloadRequest
{
    [JsonPropertyName("artifactType")] public string ArtifactType { get; init; } = string.Empty;
}

public sealed record DataDeletionCreateRequest
{
    [JsonPropertyName("backupJobId")] public string BackupJobId { get; init; } = string.Empty;
    [JsonPropertyName("targetCode")] public string TargetCode { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
}
