using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Settings;

public sealed record DataBackupPurgeTarget(string Code, string Label, string Description)
{
    public override string ToString() => Label;
}

public sealed record DataBackupJobRow(
    string Id,
    string RequestedAt,
    string Status,
    string SnapshotAt,
    string DumpSize,
    string DumpSha256,
    string ManifestSha256,
    string FailureMessage,
    bool IsVerified,
    bool HasManifest);

public static class DataBackupPresentation
{
    private static readonly IReadOnlyDictionary<string, string> StatusLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["QUEUED"] = "Xếp hàng",
            ["SNAPSHOTTING"] = "Chốt thời điểm dữ liệu",
            ["DUMPING_DATABASE"] = "Tạo file .dump",
            ["HASHING"] = "Tính SHA-256",
            ["UPLOADING_R2"] = "Lưu lên kho sao lưu",
            ["VERIFYING_R2"] = "Đối chiếu bản đã lưu",
            ["VERIFIED"] = "Đã xác minh",
            ["FAILED"] = "Thất bại",
            ["EXPORTING_DATASETS"] = "Đang hoàn tất bản sao lưu cũ",
            ["BUILDING_ARCHIVE"] = "Đang hoàn tất bản sao lưu cũ"
        };

    public static string Status(string value) =>
        StatusLabels.TryGetValue(value ?? string.Empty, out var label) ? label : value;

    public static string Time(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return "—";

        return parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static string Bytes(long? value)
    {
        if (value is null or <= 0) return "—";
        var size = (double)value.Value;
        var units = new[] { "B", "KB", "MB", "GB" };
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0
            ? $"{size:0} {units[unit]}"
            : $"{size:0.0} {units[unit]}";
    }

    public static int Progress(string status) =>
        status switch
        {
            "QUEUED" => 0,
            "SNAPSHOTTING" => 17,
            "DUMPING_DATABASE" => 33,
            "HASHING" => 50,
            "UPLOADING_R2" => 67,
            "VERIFYING_R2" => 83,
            "VERIFIED" => 100,
            "EXPORTING_DATASETS" or "BUILDING_ARCHIVE" => 55,
            _ => 0
        };
}

public sealed class DataBackupViewModel : INotifyPropertyChanged
{
    private static readonly HashSet<string> OwnerRoles =
        new(["system:security-owner", "system:implementation-owner"], StringComparer.Ordinal);

    private static readonly HashSet<string> ActiveStatuses =
        new([
            "QUEUED",
            "SNAPSHOTTING",
            "DUMPING_DATABASE",
            "HASHING",
            "UPLOADING_R2",
            "VERIFYING_R2",
            "EXPORTING_DATASETS",
            "BUILDING_ARCHIVE"
        ], StringComparer.Ordinal);

    private readonly IDataBackupService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotency;
    private readonly Dictionary<string, PendingKey> _keys = new(StringComparer.Ordinal);

    private bool _loaded;
    private bool _isBusy;
    private bool _messageIsError;
    private string _message = string.Empty;
    private string? _technicalUnlockToken;
    private string _technicalExpiresAt = string.Empty;
    private TechnicalBackupChallengeData? _technicalChallenge;
    private string _technicalCode = string.Empty;
    private DataBackupPurgeTarget _selectedDeleteTarget;
    private string _deleteReason = string.Empty;
    private string _deleteCode = string.Empty;
    private DataDeletionIntentData? _deleteIntent;
    private DataBackupJobData? _latestVerified;
    private DataBackupJobData? _activeJob;

    public DataBackupViewModel(
        IDataBackupService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotency)
    {
        _service = service;
        _access = access;
        _idempotency = idempotency;
        _selectedDeleteTarget = DeleteTargets[0];

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            ClearSensitiveState();
            _loaded = false;
            Jobs.Clear();
            Message = string.Empty;
            RaiseAccess();
            if (_access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DataBackupJobRow> Jobs { get; } = [];

    public IReadOnlyList<DataBackupPurgeTarget> DeleteTargets { get; } =
    [
        new(
            "ALL_BUSINESS_DATA",
            "Toàn bộ dữ liệu nghiệp vụ",
            "Dùng khi dọn dữ liệu test để bàn giao. Giữ tài khoản đăng nhập, phân quyền, cấu hình tổ chức, cấu hình nền và bản sao lưu kỹ thuật."),
        new(
            "OPERATIONS_ONLY",
            "Dữ liệu phát sinh",
            "Xóa đơn hàng, kho, công nợ, giao hàng, báo cáo và hoạt động MCP; giữ danh mục khách hàng, nhà cung cấp và sản phẩm."),
        new(
            "CUSTOMERS_AND_SALES",
            "Khách hàng & bán hàng",
            "Xóa khách hàng và toàn bộ bán hàng, giao hàng, công nợ và dữ liệu phụ thuộc liên quan."),
        new(
            "SUPPLIERS_AND_PURCHASING",
            "Nhà cung cấp & mua hàng",
            "Xóa nhà cung cấp, mua hàng và dữ liệu phụ thuộc liên quan."),
        new(
            "PRODUCTS_AND_INVENTORY",
            "Sản phẩm & kho",
            "Xóa danh mục sản phẩm, bảng giá, tồn kho và các giao dịch phụ thuộc."),
        new(
            "MCP_ONLY",
            "Dữ liệu MCP",
            "Xóa tuyến, phiên, ghé điểm, báo cáo và dữ liệu hoạt động MCP; giữ cấu hình MCP và dữ liệu Công Ty.")
    ];

    public bool IsAuthenticated => _access.Current.IsAuthenticated;
    public bool IsOwner => _access.Current.Roles.Any(OwnerRoles.Contains);
    public bool CanExportBusiness => _access.HasPermission("core.reporting.export");
    public bool CanReadBackup => IsOwner && _access.HasPermission("core.backup.read");
    public bool CanCreateBackup => IsOwner && _access.HasPermission("core.backup.create");
    public bool CanDownloadBackup => IsOwner && _access.HasPermission("core.backup.download");
    public bool CanAuthorizeDeletion => IsOwner && _access.HasPermission("core.data-deletion.authorize");
    public bool CanUseTechnicalArea => CanReadBackup && (CanCreateBackup || CanDownloadBackup);
    public bool IsTechnicalUnlocked => !string.IsNullOrWhiteSpace(_technicalUnlockToken);
    public bool HasJobs => Jobs.Count > 0;
    public bool ShowEmptyHistory => CanReadBackup && IsTechnicalUnlocked && !IsBusy && !HasJobs;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool ShouldPoll => _activeJob is not null && ActiveStatuses.Contains(_activeJob.Status) && !IsBusy;
    public bool HasLatestVerified => _latestVerified is not null;
    public bool HasActiveBackup => _activeJob is not null && ActiveStatuses.Contains(_activeJob.Status);
    public bool CanShowTechnical => CanReadBackup && IsTechnicalUnlocked;
    public bool CanShowDelete => CanAuthorizeDeletion;
    public string TechnicalAreaTitle => IsTechnicalUnlocked ? "Bản kỹ thuật PostgreSQL" : "Khu vực kỹ thuật";
    public string TechnicalAreaDescription => IsTechnicalUnlocked
        ? "File .dump là dữ liệu khôi phục chính. Mỗi bản mới có thêm tệp thông tin kỹ thuật để phục vụ di chuyển và đối soát."
        : "Nhập mã xác nhận để mở chức năng sao lưu hệ thống.";
    public bool CanStartBackup => CanCreateBackup && IsTechnicalUnlocked && !HasActiveBackup && !IsBusy;
    public bool CanRequestTechnicalUnlock => CanUseTechnicalArea && !IsTechnicalUnlocked && !IsBusy;
    public bool HasTechnicalChallenge => _technicalChallenge is not null;
    public bool CanVerifyTechnicalUnlock => _technicalChallenge is not null && TechnicalCode.Length == 6 && !IsBusy;
    public bool CanEditDeleteRequest =>
        CanAuthorizeDeletion
        && IsTechnicalUnlocked
        && _latestVerified is not null
        && _deleteIntent is null
        && !IsBusy;
    public bool CanRequestDeleteChallenge =>
        CanEditDeleteRequest
        && !string.IsNullOrWhiteSpace(DeleteReason);
    public bool DeleteAwaitingCode => _deleteIntent is not null
        && !string.Equals(_deleteIntent.Status, "AUTHORIZED", StringComparison.Ordinal)
        && !string.Equals(_deleteIntent.Status, "PURGED", StringComparison.Ordinal);
    public bool DeleteAuthorized => string.Equals(_deleteIntent?.Status, "AUTHORIZED", StringComparison.Ordinal);
    public bool DeletePurged => string.Equals(_deleteIntent?.Status, "PURGED", StringComparison.Ordinal);
    public bool CanVerifyDelete => DeleteAwaitingCode && DeleteCode.Length == 6 && !IsBusy;
    public bool CanExecuteDelete => DeleteAuthorized && !IsBusy;

    public string TechnicalExpiresText =>
        IsTechnicalUnlocked ? $"Hết hạn: {DataBackupPresentation.Time(_technicalExpiresAt)}" : string.Empty;

    public string LatestVerifiedAtText =>
        _latestVerified is null ? "Chưa có" : DataBackupPresentation.Time(_latestVerified.VerifiedAt);

    public string LatestSnapshotText =>
        _latestVerified is null ? "—" : DataBackupPresentation.Time(_latestVerified.SnapshotAt);

    public string LatestSchemaText =>
        string.IsNullOrWhiteSpace(_latestVerified?.SchemaVersion) ? "Chưa có" : "Đã ghi nhận";

    public string LatestManifestText =>
        _latestVerified?.Artifacts.Manifest is null ? "Chưa có" : "Sẵn sàng";

    public string ActiveStatusText =>
        _activeJob is null ? string.Empty : DataBackupPresentation.Status(_activeJob.Status);

    public int ActiveProgress =>
        _activeJob is null ? 0 : DataBackupPresentation.Progress(_activeJob.Status);

    public string TechnicalChallengeText =>
        _technicalChallenge is null
            ? string.Empty
            : $"Mã mở khóa đã được gửi tới {_technicalChallenge.Recipient}. Hết hạn: {DataBackupPresentation.Time(_technicalChallenge.ChallengeExpiresAt)}";

    public string DeleteChallengeText =>
        _deleteIntent?.ChallengeExpiresAt is null
            ? string.Empty
            : $"Mã xác nhận đã được gửi theo chính sách hiện tại. Hết hạn: {DataBackupPresentation.Time(_deleteIntent.ChallengeExpiresAt)}";

    public string DeleteProtectedBackupText =>
        _latestVerified is null ? "—" : DataBackupPresentation.Time(_latestVerified.VerifiedAt);

    public string DeleteProtectedBackupLabelText =>
        $"Bản sao lưu bảo vệ: {DeleteProtectedBackupText}";

    public string DeleteResultText =>
        _deleteIntent?.PurgeSummary is null
            ? string.Empty
            : $"Đã xóa {(_deleteIntent.PurgeSummary.DeletedRows ?? 0):N0} bản ghi trong {(_deleteIntent.PurgeSummary.AffectedTableCount ?? 0):N0} bảng nghiệp vụ.";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaiseActions();
            OnPropertyChanged(nameof(ShowEmptyHistory));
            OnPropertyChanged(nameof(ShouldPoll));
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public string Message
    {
        get => _message;
        private set
        {
            if (!SetField(ref _message, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasMessage));
        }
    }

    public string TechnicalCode
    {
        get => _technicalCode;
        set
        {
            var normalized = Digits(value);
            if (!SetField(ref _technicalCode, normalized)) return;
            OnPropertyChanged(nameof(CanVerifyTechnicalUnlock));
        }
    }

    public DataBackupPurgeTarget SelectedDeleteTarget
    {
        get => _selectedDeleteTarget;
        set
        {
            if (!SetField(ref _selectedDeleteTarget, value ?? DeleteTargets[0])) return;
            _deleteIntent = null;
            DeleteCode = string.Empty;
            OnPropertyChanged(nameof(DeleteTargetDescription));
            RaiseDeleteState();
        }
    }

    public string DeleteTargetDescription => SelectedDeleteTarget.Description;

    public string DeleteReason
    {
        get => _deleteReason;
        set
        {
            var normalized = value?.Length > 1000 ? value[..1000] : value ?? string.Empty;
            if (!SetField(ref _deleteReason, normalized)) return;
            OnPropertyChanged(nameof(CanRequestDeleteChallenge));
        }
    }

    public string DeleteCode
    {
        get => _deleteCode;
        set
        {
            if (!SetField(ref _deleteCode, Digits(value))) return;
            OnPropertyChanged(nameof(CanVerifyDelete));
        }
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || IsBusy || !IsAuthenticated) return;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (!IsAuthenticated || IsBusy) return;
        Begin();
        try
        {
            if (!CanReadBackup)
            {
                Jobs.Clear();
                _latestVerified = null;
                _activeJob = null;
                ClearTechnicalUnlockOnly();
                _loaded = true;
                RaiseBackupState();
                return;
            }

            var access = await _service.GetTechnicalAccessAsync(_technicalUnlockToken);
            if (!access.Unlocked)
            {
                Jobs.Clear();
                _latestVerified = null;
                _activeJob = null;
                ClearTechnicalUnlockOnly();
                _loaded = true;
                RaiseBackupState();
                return;
            }

            _technicalExpiresAt = access.ExpiresAt ?? _technicalExpiresAt;
            var jobs = await _service.ListBackupsAsync(RequireUnlock());
            ApplyJobs(jobs);
            _loaded = true;
            SetMessage(string.Empty);
        }
        catch (CanonicalApiException exception) when ((int)exception.StatusCode == 423)
        {
            Jobs.Clear();
            _latestVerified = null;
            _activeJob = null;
            ClearTechnicalUnlockOnly();
            _loaded = true;
            SetMessage("Khu vực kỹ thuật đã khóa. Mở khóa lại để tiếp tục.");
            RaiseBackupState();
        }
        catch (Exception exception)
        {
            SetMessage(PublicMessage(exception, "Không tải được thông tin sao lưu."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportBusinessAsync(string path)
    {
        if (!CanExportBusiness || IsBusy) return;
        Begin();
        try
        {
            var file = await _service.ExportBusinessDataAsync();
            await File.WriteAllBytesAsync(path, file.Content);
            SetMessage("Đã xuất số liệu doanh nghiệp.");
        }
        catch (Exception exception)
        {
            SetMessage(PublicMessage(exception, "Không xuất được số liệu doanh nghiệp."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RequestTechnicalUnlockAsync()
    {
        if (!CanRequestTechnicalUnlock) return;
        const string slot = "technical-challenge";
        var key = KeyFor(slot, "technical-challenge");
        Begin();
        try
        {
            _technicalChallenge = await _service.RequestTechnicalChallengeAsync(key);
            CompleteKey(slot);
            TechnicalCode = string.Empty;
            OnPropertyChanged(nameof(HasTechnicalChallenge));
            OnPropertyChanged(nameof(TechnicalChallengeText));
            SetMessage("Đã gửi mã mở khóa Khu vực kỹ thuật.");
        }
        catch (Exception exception)
        {
            FailKey(slot, exception);
            SetMessage(PublicMessage(exception, "Không gửi được mã mở khóa."), true);
        }
        finally
        {
            IsBusy = false;
            RaiseActions();
        }
    }

    public async Task VerifyTechnicalUnlockAsync()
    {
        if (!CanVerifyTechnicalUnlock || _technicalChallenge is null) return;
        var slot = $"technical-verify-{_technicalChallenge.Id}";
        var key = KeyFor(slot, $"{_technicalChallenge.Id}|{TechnicalCode}");
        Begin();
        try
        {
            var unlocked = await _service.VerifyTechnicalChallengeAsync(
                _technicalChallenge.Id,
                TechnicalCode,
                key);
            _technicalUnlockToken = unlocked.Token;
            _technicalExpiresAt = unlocked.ExpiresAt;
            _technicalChallenge = null;
            TechnicalCode = string.Empty;
            CompleteKey(slot);
            OnPropertyChanged(nameof(HasTechnicalChallenge));
            OnPropertyChanged(nameof(TechnicalChallengeText));
            RaiseBackupState();
            await RefreshUnlockedAsync();
            SetMessage($"Khu vực kỹ thuật đã mở đến {DataBackupPresentation.Time(_technicalExpiresAt)}.");
        }
        catch (Exception exception)
        {
            FailKey(slot, exception);
            SetMessage(PublicMessage(exception, "Mã mở khóa không hợp lệ."), true);
        }
        finally
        {
            IsBusy = false;
            RaiseActions();
        }
    }

    public async Task StartBackupAsync()
    {
        if (!CanStartBackup) return;
        const string slot = "backup-create";
        var key = KeyFor(slot, "backup-create");
        Begin();
        try
        {
            var job = await _service.CreateBackupAsync(RequireUnlock(), key);
            CompleteKey(slot);
            UpsertJob(job);
            SetMessage("Đã tiếp nhận yêu cầu sao lưu hệ thống.");
            await RefreshUnlockedAsync();
        }
        catch (Exception exception)
        {
            FailKey(slot, exception);
            SetMessage(PublicMessage(exception, "Không tạo được bản sao lưu hệ thống."), true);
        }
        finally
        {
            IsBusy = false;
            RaiseActions();
        }
    }

    public async Task<string?> CreateDownloadUrlAsync(string jobId, string artifactType)
    {
        if (!CanDownloadBackup || !IsTechnicalUnlocked || IsBusy) return null;
        var slot = $"backup-download-{artifactType}";
        var fingerprint = $"{jobId}|{artifactType}";
        var key = KeyFor(slot, fingerprint);
        Begin();
        try
        {
            var download = await _service.CreateDownloadAsync(
                jobId,
                artifactType,
                RequireUnlock(),
                key);
            CompleteKey(slot);
            if (!Uri.TryCreate(download.Url, UriKind.Absolute, out var url)
                || url.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Liên kết tải bản sao lưu không hợp lệ.");
            SetMessage("Đã tạo liên kết tải bản sao lưu.");
            return url.AbsoluteUri;
        }
        catch (Exception exception)
        {
            FailKey(slot, exception);
            SetMessage(PublicMessage(exception, "Không tạo được liên kết tải bản sao lưu."), true);
            return null;
        }
        finally
        {
            IsBusy = false;
            RaiseActions();
        }
    }

    public async Task RequestDeleteChallengeAsync()
    {
        if (!CanRequestDeleteChallenge || _latestVerified is null) return;
        const string slot = "delete-create";
        var fingerprint = $"{_latestVerified.Id}|{SelectedDeleteTarget.Code}|{DeleteReason.Trim()}";
        var key = KeyFor(slot, Fingerprint(fingerprint));
        Begin();
        try
        {
            _deleteIntent = await _service.CreateDeletionIntentAsync(
                _latestVerified.Id,
                SelectedDeleteTarget.Code,
                DeleteReason,
                key);
            CompleteKey(slot);
            DeleteCode = string.Empty;
            RaiseDeleteState();
            SetMessage("Đã gửi mã xác nhận xóa dữ liệu.");
        }
        catch (Exception exception)
        {
            FailKey(slot, exception);
            SetMessage(PublicMessage(exception, "Không gửi được mã xác nhận xóa dữ liệu."), true);
        }
        finally
        {
            IsBusy = false;
            RaiseActions();
        }
    }

    public async Task VerifyDeleteChallengeAsync()
    {
        if (!CanVerifyDelete || _deleteIntent is null) return;
        var slot = $"delete-verify-{_deleteIntent.Id}";
        var key = KeyFor(slot, Fingerprint($"{_deleteIntent.Id}|{DeleteCode}"));
        Begin();
        try
        {
            _deleteIntent = await _service.VerifyDeletionIntentAsync(
                _deleteIntent.Id,
                DeleteCode,
                key);
            CompleteKey(slot);
            DeleteCode = string.Empty;
            RaiseDeleteState();
            SetMessage("Đã xác minh quyền xóa dữ liệu.");
        }
        catch (Exception exception)
        {
            FailKey(slot, exception);
            SetMessage(PublicMessage(exception, "Mã xác nhận không hợp lệ."), true);
        }
        finally
        {
            IsBusy = false;
            RaiseActions();
        }
    }

    public async Task ExecuteDeleteAsync()
    {
        if (!CanExecuteDelete || _deleteIntent is null) return;
        var slot = $"delete-execute-{_deleteIntent.Id}";
        var key = KeyFor(slot, _deleteIntent.Id);
        Begin();
        try
        {
            _deleteIntent = await _service.ExecuteDeletionIntentAsync(_deleteIntent.Id, key);
            CompleteKey(slot);
            RaiseDeleteState();
            SetMessage(
                $"Đã xóa {SelectedDeleteTarget.Label.ToLowerInvariant()}. Bản sao lưu kỹ thuật vẫn được giữ nguyên.");
        }
        catch (Exception exception)
        {
            FailKey(slot, exception);
            SetMessage(PublicMessage(exception, "Không thể thực hiện xóa dữ liệu."), true);
        }
        finally
        {
            IsBusy = false;
            RaiseActions();
        }
    }

    public void ResetDeleteFlow()
    {
        _deleteIntent = null;
        DeleteCode = string.Empty;
        DeleteReason = string.Empty;
        SelectedDeleteTarget = DeleteTargets[0];
        RaiseDeleteState();
    }

    private async Task RefreshUnlockedAsync()
    {
        if (!IsTechnicalUnlocked || !CanReadBackup) return;
        var jobs = await _service.ListBackupsAsync(RequireUnlock());
        ApplyJobs(jobs);
    }

    private void ApplyJobs(IReadOnlyList<DataBackupJobData> jobs)
    {
        Jobs.Clear();
        foreach (var job in jobs)
            Jobs.Add(Map(job));

        _latestVerified = jobs.FirstOrDefault(job => string.Equals(job.Status, "VERIFIED", StringComparison.Ordinal));
        _activeJob = jobs.FirstOrDefault(job => ActiveStatuses.Contains(job.Status));
        RaiseBackupState();
    }

    private void UpsertJob(DataBackupJobData job)
    {
        var rows = Jobs.ToList();
        rows.RemoveAll(item => string.Equals(item.Id, job.Id, StringComparison.Ordinal));
        rows.Insert(0, Map(job));
        Jobs.Clear();
        foreach (var row in rows) Jobs.Add(row);
        if (ActiveStatuses.Contains(job.Status)) _activeJob = job;
        RaiseBackupState();
    }

    private static DataBackupJobRow Map(DataBackupJobData job) =>
        new(
            job.Id,
            DataBackupPresentation.Time(job.RequestedAt),
            DataBackupPresentation.Status(job.Status),
            DataBackupPresentation.Time(job.SnapshotAt),
            DataBackupPresentation.Bytes(job.Artifacts.DatabaseDump?.Size),
            job.Artifacts.DatabaseDump?.Sha256 ?? string.Empty,
            job.Artifacts.Manifest?.Sha256 ?? string.Empty,
            job.FailureMessage ?? string.Empty,
            string.Equals(job.Status, "VERIFIED", StringComparison.Ordinal),
            job.Artifacts.Manifest is not null);

    private void ClearSensitiveState()
    {
        _keys.Clear();
        _technicalChallenge = null;
        OnPropertyChanged(nameof(HasTechnicalChallenge));
        _deleteIntent = null;
        TechnicalCode = string.Empty;
        DeleteCode = string.Empty;
        DeleteReason = string.Empty;
        ClearTechnicalUnlockOnly();
        RaiseDeleteState();
    }

    private void ClearTechnicalUnlockOnly()
    {
        _technicalUnlockToken = null;
        _technicalExpiresAt = string.Empty;
        OnPropertyChanged(nameof(IsTechnicalUnlocked));
        OnPropertyChanged(nameof(TechnicalExpiresText));
        OnPropertyChanged(nameof(CanShowTechnical));
        OnPropertyChanged(nameof(TechnicalAreaTitle));
        OnPropertyChanged(nameof(TechnicalAreaDescription));
        RaiseActions();
    }

    private string RequireUnlock() =>
        string.IsNullOrWhiteSpace(_technicalUnlockToken)
            ? throw new InvalidOperationException("Cần mở khóa Khu vực kỹ thuật trước khi thao tác.")
            : _technicalUnlockToken;

    private string KeyFor(string slot, string fingerprint)
    {
        if (_keys.TryGetValue(slot, out var pending)
            && string.Equals(pending.Fingerprint, fingerprint, StringComparison.Ordinal))
            return pending.Key;

        var key = _idempotency.Create($"settings-{slot}");
        if (!_idempotency.IsValid(key))
            throw new InvalidOperationException("Khóa chống xử lý trùng không hợp lệ.");
        _keys[slot] = new PendingKey(fingerprint, key);
        return key;
    }

    private void CompleteKey(string slot) => _keys.Remove(slot);

    private void FailKey(string slot, Exception exception)
    {
        if (exception is not CanonicalApiException { Retryable: true })
            _keys.Remove(slot);
    }

    private static string Fingerprint(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static string Digits(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return new string(value.Where(char.IsDigit).Take(6).ToArray());
    }

    private static string PublicMessage(Exception exception, string fallback) =>
        string.IsNullOrWhiteSpace(exception.Message) ? fallback : exception.Message;

    private void Begin()
    {
        IsBusy = true;
        SetMessage(string.Empty);
    }

    private void SetMessage(string value, bool isError = false)
    {
        MessageIsError = isError;
        Message = value;
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(IsOwner));
        OnPropertyChanged(nameof(CanExportBusiness));
        OnPropertyChanged(nameof(CanReadBackup));
        OnPropertyChanged(nameof(CanCreateBackup));
        OnPropertyChanged(nameof(CanDownloadBackup));
        OnPropertyChanged(nameof(CanAuthorizeDeletion));
        OnPropertyChanged(nameof(CanUseTechnicalArea));
        RaiseActions();
    }

    private void RaiseBackupState()
    {
        OnPropertyChanged(nameof(HasJobs));
        OnPropertyChanged(nameof(ShowEmptyHistory));
        OnPropertyChanged(nameof(ShouldPoll));
        OnPropertyChanged(nameof(HasLatestVerified));
        OnPropertyChanged(nameof(HasActiveBackup));
        OnPropertyChanged(nameof(CanShowTechnical));
        OnPropertyChanged(nameof(CanShowDelete));
        OnPropertyChanged(nameof(TechnicalAreaTitle));
        OnPropertyChanged(nameof(TechnicalAreaDescription));
        OnPropertyChanged(nameof(LatestVerifiedAtText));
        OnPropertyChanged(nameof(LatestSnapshotText));
        OnPropertyChanged(nameof(LatestSchemaText));
        OnPropertyChanged(nameof(LatestManifestText));
        OnPropertyChanged(nameof(ActiveStatusText));
        OnPropertyChanged(nameof(ActiveProgress));
        OnPropertyChanged(nameof(IsTechnicalUnlocked));
        OnPropertyChanged(nameof(TechnicalExpiresText));
        OnPropertyChanged(nameof(DeleteProtectedBackupText));
        OnPropertyChanged(nameof(DeleteProtectedBackupLabelText));
        RaiseActions();
    }

    private void RaiseDeleteState()
    {
        OnPropertyChanged(nameof(DeleteAwaitingCode));
        OnPropertyChanged(nameof(DeleteAuthorized));
        OnPropertyChanged(nameof(DeletePurged));
        OnPropertyChanged(nameof(CanVerifyDelete));
        OnPropertyChanged(nameof(CanExecuteDelete));
        OnPropertyChanged(nameof(DeleteChallengeText));
        OnPropertyChanged(nameof(DeleteResultText));
        OnPropertyChanged(nameof(CanEditDeleteRequest));
        OnPropertyChanged(nameof(CanRequestDeleteChallenge));
    }

    private void RaiseActions()
    {
        OnPropertyChanged(nameof(CanStartBackup));
        OnPropertyChanged(nameof(CanRequestTechnicalUnlock));
        OnPropertyChanged(nameof(CanVerifyTechnicalUnlock));
        OnPropertyChanged(nameof(CanEditDeleteRequest));
        OnPropertyChanged(nameof(CanRequestDeleteChallenge));
        OnPropertyChanged(nameof(CanVerifyDelete));
        OnPropertyChanged(nameof(CanExecuteDelete));
    }

    private void RunOnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher is null || Application.Current.Dispatcher.CheckAccess())
            action();
        else
            Application.Current.Dispatcher.Invoke(action);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed record PendingKey(string Fingerprint, string Key);
}
