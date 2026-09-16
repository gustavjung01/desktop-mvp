using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record TrackingPolicyCandidateOption(string Id, string Label);

public sealed class InventoryTrackingPolicyViewModel : INotifyPropertyChanged
{
    private readonly IInventoryTrackingPolicyService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly List<InventoryTrackingPolicyData> _policies = [];
    private readonly List<InventoryTrackingPolicyCandidateData> _candidates = [];

    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _search = string.Empty;
    private string _selectedBaseVariantId = string.Empty;
    private string _lotTrackingMode = "NONE";
    private string _expiryTrackingMode = "NONE";
    private int? _expectedVersion;
    private string? _pendingSaveFingerprint;
    private string? _pendingSaveKey;

    public InventoryTrackingPolicyViewModel(
        IInventoryTrackingPolicyService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        LotModeOptions =
        [
            new("NONE", "Không quản lý theo lô"),
            new("REQUIRED", "Bắt buộc quản lý theo lô")
        ];
        ExpiryModeOptions =
        [
            new("NONE", "Không quản lý hạn sử dụng"),
            new("OPTIONAL", "Có thể nhập hạn sử dụng"),
            new("REQUIRED", "Bắt buộc nhập hạn sử dụng")
        ];

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            _pendingSaveFingerprint = null;
            _pendingSaveKey = null;
            ResetData();
            RaisePermissions();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TrackingPolicyCandidateRow> Rows { get; } = [];
    public ObservableCollection<TrackingPolicyCandidateOption> CandidateOptions { get; } = [];

    public IReadOnlyList<TrackingPolicyModeOption> LotModeOptions { get; }
    public IReadOnlyList<TrackingPolicyModeOption> ExpiryModeOptions { get; }

    public bool CanRead => _access.HasPermission("core.inventory.tracking-policy.read");
    public bool CanManage => _access.HasPermission("core.inventory.tracking-policy.manage");
    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool CanEditPolicy => CanManage && IsNotBusy && !string.IsNullOrWhiteSpace(SelectedBaseVariantId);
    public bool CanEditExpiry => CanEditPolicy && LotTrackingMode == "REQUIRED";
    public bool CanSave => CanEditPolicy;

    public string RefreshText => _busyAction == "load" ? "Đang xử lý..." : "Làm mới dữ liệu";
    public string SaveText => _busyAction == "save" ? "Đang lưu..." : "Lưu chính sách";

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public string Search
    {
        get => _search;
        set
        {
            if (SetField(ref _search, value ?? string.Empty)) ApplyFilter();
        }
    }

    public string SelectedBaseVariantId
    {
        get => _selectedBaseVariantId;
        set
        {
            var next = value?.Trim() ?? string.Empty;
            if (!SetField(ref _selectedBaseVariantId, next)) return;
            LoadDraft(next);
            RaiseEditorState();
        }
    }

    public string LotTrackingMode
    {
        get => _lotTrackingMode;
        set
        {
            var next = value == "REQUIRED" ? "REQUIRED" : "NONE";
            if (!SetField(ref _lotTrackingMode, next)) return;
            if (next == "NONE") ExpiryTrackingMode = "NONE";
            RaiseEditorState();
        }
    }

    public string ExpiryTrackingMode
    {
        get => _expiryTrackingMode;
        set
        {
            var next = value is "OPTIONAL" or "REQUIRED" ? value : "NONE";
            if (LotTrackingMode == "NONE") next = "NONE";
            if (SetField(ref _expiryTrackingMode, next)) RaiseEditorState();
        }
    }

    public string SelectedSku
    {
        get
        {
            var candidate = SelectedCandidate();
            return candidate?.BaseSku ?? "Chưa chọn SKU tồn chuẩn";
        }
    }

    public string SelectedProduct
    {
        get
        {
            var candidate = SelectedCandidate();
            return candidate is null ? string.Empty : $"{candidate.ProductCode} · {candidate.ProductName}";
        }
    }

    public string CurrentSetupStatus =>
        string.IsNullOrWhiteSpace(SelectedBaseVariantId)
            ? "Chọn SKU tồn chuẩn để thiết lập."
            : _expectedVersion.HasValue
                ? "SKU này đã có chính sách. Lưu sẽ cập nhật phiên bản hiện tại."
                : "SKU này chưa có chính sách. Lưu sẽ tạo thiết lập đầu tiên.";

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);
        _initialLoadTask = LoadAsync();
        try { return await _initialLoadTask.ConfigureAwait(true); }
        finally { _initialLoadTask = null; }
    }

    public Task<bool> RefreshAsync() => LoadAsync();

    public void Choose(TrackingPolicyCandidateRow row)
    {
        SelectedBaseVariantId = row.Candidate.BaseVariantId;
    }

    public async Task<bool> SaveAsync()
    {
        if (!CanManage)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền thay đổi Chính sách lô.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedBaseVariantId))
        {
            SetErrorMessage("Hãy chọn SKU tồn chuẩn trước khi lưu chính sách.");
            return false;
        }

        var request = new InventoryTrackingPolicySaveRequest
        {
            BaseVariantId = SelectedBaseVariantId,
            LotTrackingMode = LotTrackingMode,
            ExpiryTrackingMode = LotTrackingMode == "NONE" ? "NONE" : ExpiryTrackingMode,
            ExpectedVersion = _expectedVersion
        };

        var fingerprint = $"{request.BaseVariantId}|{request.LotTrackingMode}|{request.ExpiryTrackingMode}|{request.ExpectedVersion?.ToString() ?? "new"}";
        if (!string.Equals(_pendingSaveFingerprint, fingerprint, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(_pendingSaveKey))
        {
            _pendingSaveFingerprint = fingerprint;
            _pendingSaveKey = _idempotencyKeys.Create("inventory-policy-save");
        }

        var generation = _accessGeneration;
        SetBusy("save");
        ClearMessage();
        try
        {
            var saved = await _service.SaveAsync(request, _pendingSaveKey!).ConfigureAwait(true);
            if (generation != _accessGeneration || !CanRead) return false;

            _pendingSaveFingerprint = null;
            _pendingSaveKey = null;

            var existingIndex = _policies.FindIndex(item => item.BaseVariantId == saved.BaseVariantId);
            if (existingIndex >= 0) _policies[existingIndex] = saved;
            else _policies.Add(saved);

            var candidateIndex = _candidates.FindIndex(item => item.BaseVariantId == saved.BaseVariantId);
            if (candidateIndex >= 0 && !_candidates[candidateIndex].HasPolicy)
            {
                _candidates[candidateIndex] = _candidates[candidateIndex] with { HasPolicy = true };
            }

            RebuildCandidateOptions();
            ApplyFilter();
            _expectedVersion = saved.Version;
            _lotTrackingMode = saved.LotTrackingMode == "REQUIRED" ? "REQUIRED" : "NONE";
            _expiryTrackingMode = _lotTrackingMode == "NONE"
                ? "NONE"
                : saved.ExpiryTrackingMode is "OPTIONAL" or "REQUIRED" ? saved.ExpiryTrackingMode : "NONE";
            OnPropertyChanged(nameof(LotTrackingMode));
            OnPropertyChanged(nameof(ExpiryTrackingMode));
            RaiseEditorState();
            SetNotice("Chính sách lô và hạn sử dụng đã được lưu.");
            return true;
        }
        catch (Exception exception)
        {
            SetError(exception);
            return false;
        }
        finally
        {
            SetBusy(null);
        }
    }

    private async Task<bool> LoadAsync()
    {
        if (!CanRead)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền xem Chính sách lô.");
            return false;
        }

        var generation = _accessGeneration;
        var selectedId = SelectedBaseVariantId;
        SetBusy("load");
        ClearMessage();
        try
        {
            var policyTask = _service.ListPoliciesAsync();
            var candidateTask = _service.ListCandidatesAsync();
            await Task.WhenAll(policyTask, candidateTask).ConfigureAwait(true);
            if (generation != _accessGeneration || !CanRead) return false;

            _policies.Clear();
            _policies.AddRange(await policyTask.ConfigureAwait(true));
            _candidates.Clear();
            _candidates.AddRange(await candidateTask.ConfigureAwait(true));

            RebuildCandidateOptions();
            ApplyFilter();

            if (!string.IsNullOrWhiteSpace(selectedId) && _candidates.Any(item => item.BaseVariantId == selectedId))
                SelectedBaseVariantId = selectedId;
            else
                SelectedBaseVariantId = string.Empty;

            _loaded = true;
            SetNotice("Dữ liệu đã được làm mới.");
            return true;
        }
        catch (Exception exception)
        {
            SetError(exception);
            return false;
        }
        finally
        {
            SetBusy(null);
        }
    }

    private void LoadDraft(string baseVariantId)
    {
        var policy = _policies.FirstOrDefault(item => item.BaseVariantId == baseVariantId);
        _expectedVersion = policy?.Version;
        _lotTrackingMode = policy?.LotTrackingMode == "REQUIRED" ? "REQUIRED" : "NONE";
        _expiryTrackingMode = _lotTrackingMode == "NONE"
            ? "NONE"
            : policy?.ExpiryTrackingMode is "OPTIONAL" or "REQUIRED" ? policy.ExpiryTrackingMode : "NONE";
        OnPropertyChanged(nameof(LotTrackingMode));
        OnPropertyChanged(nameof(ExpiryTrackingMode));
        OnPropertyChanged(nameof(SelectedSku));
        OnPropertyChanged(nameof(SelectedProduct));
        OnPropertyChanged(nameof(CurrentSetupStatus));
    }

    private void ApplyFilter()
    {
        var policyById = _policies.ToDictionary(item => item.BaseVariantId, StringComparer.Ordinal);
        var term = Search.Trim().ToLowerInvariant();
        var filtered = _candidates
            .Where(item => term.Length == 0 || InventoryTrackingPolicyPresentation.SearchText(item).Contains(term, StringComparison.Ordinal))
            .ToArray();

        Rows.Clear();
        for (var i = 0; i < filtered.Length; i++)
        {
            policyById.TryGetValue(filtered[i].BaseVariantId, out var policy);
            Rows.Add(new TrackingPolicyCandidateRow(i + 1, filtered[i], policy));
        }
    }

    private void RebuildCandidateOptions()
    {
        CandidateOptions.Clear();
        foreach (var candidate in _candidates)
        {
            CandidateOptions.Add(new TrackingPolicyCandidateOption(
                candidate.BaseVariantId,
                InventoryTrackingPolicyPresentation.CandidateLabel(candidate)));
        }
    }

    private InventoryTrackingPolicyCandidateData? SelectedCandidate() =>
        _candidates.FirstOrDefault(item => item.BaseVariantId == SelectedBaseVariantId);

    private void RaiseEditorState()
    {
        OnPropertyChanged(nameof(CanEditPolicy));
        OnPropertyChanged(nameof(CanEditExpiry));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(SelectedSku));
        OnPropertyChanged(nameof(SelectedProduct));
        OnPropertyChanged(nameof(CurrentSetupStatus));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanManage));
        RaiseEditorState();
    }

    private void SetBusy(string? action)
    {
        if (_busyAction == action) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(RefreshText));
        OnPropertyChanged(nameof(SaveText));
        RaiseEditorState();
    }

    private void ResetData()
    {
        _policies.Clear();
        _candidates.Clear();
        Rows.Clear();
        CandidateOptions.Clear();
        _selectedBaseVariantId = string.Empty;
        _lotTrackingMode = "NONE";
        _expiryTrackingMode = "NONE";
        _expectedVersion = null;
        Message = string.Empty;
        MessageIsError = false;
        OnPropertyChanged(nameof(SelectedBaseVariantId));
        OnPropertyChanged(nameof(LotTrackingMode));
        OnPropertyChanged(nameof(ExpiryTrackingMode));
        RaiseEditorState();
    }

    private void ClearMessage()
    {
        MessageIsError = false;
        Message = string.Empty;
    }

    private void SetNotice(string message)
    {
        MessageIsError = false;
        Message = message;
    }

    private void SetErrorMessage(string message)
    {
        MessageIsError = true;
        Message = message;
    }

    private void SetError(Exception exception)
    {
        if (exception is CanonicalApiException apiException)
        {
            var office = apiException.Code switch
            {
                "TRACKING_POLICY_CONFLICT" => "Chính sách vừa thay đổi hoặc không thể nới lỏng vì SKU đã có dữ liệu lô/hạn sử dụng. Hãy làm mới và kiểm tra lại.",
                "BASE_VARIANT_NOT_AVAILABLE" => "SKU tồn chuẩn hiện không còn hoạt động hoặc không thể dùng cho tồn kho.",
                "BASE_VARIANT_NOT_FOUND" => "Không tìm thấy SKU tồn chuẩn đã chọn.",
                _ => CanonicalErrorMessages.ToOfficeMessage(apiException)
            };
            SetErrorMessage(CanonicalErrorMessages.WithRequestId(office, apiException.RequestId));
            return;
        }

        SetErrorMessage(exception.Message);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
