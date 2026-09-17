using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed class ManagementProposalViewModel : INotifyPropertyChanged
{
    private const string SubmitPermission = "core.management-proposal.submit";
    private static readonly HashSet<string> ManagerRoles =
        new(["bootstrap", "system:security-owner", "system:implementation-owner"], StringComparer.Ordinal);

    private readonly IManagementProposalService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _keys;
    private readonly Dictionary<string, (string Signature, string Key)> _resubmitIntents = new(StringComparer.Ordinal);

    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _createError = string.Empty;
    private string _title = string.Empty;
    private string _content = string.Empty;
    private string _domain = "commercial";
    private string _priority = "normal";
    private string _entityType = "other";
    private string _entityId = string.Empty;
    private string _entityLabel = string.Empty;
    private string _impact = string.Empty;
    private string _reason = string.Empty;
    private string _rule = string.Empty;
    private string _evidenceText = string.Empty;
    private string? _createIntentSignature;
    private string? _createIntentKey;
    private int _accessVersion;

    public ManagementProposalViewModel(
        IManagementProposalService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider keys)
    {
        _service = service;
        _access = access;
        _keys = keys;
        _access.Changed += (_, _) => DispatchAccessChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ManagementProposalRow> Proposals { get; } = [];

    public IReadOnlyList<ManagementProposalOption> Domains { get; } =
    [
        new("commercial", "Thương mại"),
        new("customer-debt", "Khách hàng & công nợ"),
        new("operations", "Vận hành")
    ];

    public IReadOnlyList<ManagementProposalOption> Priorities { get; } =
    [
        new("normal", "Bình thường"),
        new("high", "Cần xử lý sớm"),
        new("critical", "Ưu tiên cao")
    ];

    public IReadOnlyList<ManagementProposalOption> EntityTypes { get; } =
    [
        new("other", "Khác / chưa xác định"),
        new("customer", "Khách hàng"),
        new("sales-order", "Đơn bán hàng"),
        new("purchase-order", "Đơn mua hàng"),
        new("document", "Chứng từ"),
        new("route", "Tuyến"),
        new("employee", "Nhân viên"),
        new("outlet", "Điểm bán")
    ];

    public bool CanUse => _access.HasPermission(SubmitPermission)
        || _access.Current.Roles.Any(role => ManagerRoles.Contains(role));

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanSubmit));
            OnPropertyChanged(nameof(SubmitButtonText));
            OnPropertyChanged(nameof(LoadingText));
        }
    }

    public string Message { get => _message; private set { if (SetField(ref _message, value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool MessageIsError { get => _messageIsError; private set => SetField(ref _messageIsError, value); }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public string LoadingText => IsBusy ? "Đang cập nhật Đề xuất…" : string.Empty;

    public string CreateError
    {
        get => _createError;
        private set
        {
            if (SetField(ref _createError, value)) OnPropertyChanged(nameof(HasCreateError));
        }
    }

    public bool HasCreateError => !string.IsNullOrWhiteSpace(CreateError);
    public string CountText => $"{Proposals.Count:N0} phiếu";
    public string EmptyText => !IsBusy && Proposals.Count == 0 && !MessageIsError ? "Chưa có đề xuất nào." : string.Empty;

    public string Title { get => _title; set { if (SetField(ref _title, value ?? string.Empty)) OnPropertyChanged(nameof(CanSubmit)); } }
    public string Content { get => _content; set { if (SetField(ref _content, value ?? string.Empty)) OnPropertyChanged(nameof(CanSubmit)); } }
    public string Domain { get => _domain; set => SetField(ref _domain, value ?? "commercial"); }
    public string Priority { get => _priority; set => SetField(ref _priority, value ?? "normal"); }
    public string EntityType { get => _entityType; set => SetField(ref _entityType, value ?? "other"); }
    public string EntityId { get => _entityId; set => SetField(ref _entityId, value ?? string.Empty); }
    public string EntityLabel { get => _entityLabel; set => SetField(ref _entityLabel, value ?? string.Empty); }
    public string Impact { get => _impact; set => SetField(ref _impact, value ?? string.Empty); }
    public string Reason { get => _reason; set => SetField(ref _reason, value ?? string.Empty); }
    public string Rule { get => _rule; set => SetField(ref _rule, value ?? string.Empty); }
    public string EvidenceText { get => _evidenceText; set => SetField(ref _evidenceText, value ?? string.Empty); }

    public bool CanSubmit => CanUse && !IsBusy && !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Content);
    public string SubmitButtonText => IsBusy ? "Đang gửi…" : "Gửi Đề xuất";

    public Task EnsureLoadedAsync() => RefreshAsync();

    public async Task RefreshAsync()
    {
        if (!CanUse)
        {
            ClearItems();
            SetMessage("Tài khoản hiện tại chưa được cấp quyền gửi Đề xuất.", true);
            return;
        }

        if (IsBusy) return;
        var version = _accessVersion;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await LoadItemsCoreAsync(version).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            if (version == _accessVersion)
            {
                ClearItems();
                SetMessage(ReadError(exception), true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SubmitAsync()
    {
        if (!CanUse || IsBusy) return;
        CreateError = string.Empty;

        if (!TryBuildCreateRequest(out var request, out var error))
        {
            CreateError = error;
            return;
        }

        var signature = CreateSignature(request);
        var key = CreateKey(signature);
        var version = _accessVersion;
        IsBusy = true;
        try
        {
            await _service.CreateAsync(request, key).ConfigureAwait(true);
            if (version != _accessVersion) return;

            CompleteCreateIntent();
            ResetCreateForm();
            await LoadItemsCoreAsync(version).ConfigureAwait(true);
            SetMessage("Đề xuất đã được gửi đến Admin.", false);
        }
        catch (Exception exception)
        {
            if (version == _accessVersion)
                CreateError = MutationError(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ResubmitAsync(ManagementProposalRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (!CanUse || IsBusy || !row.CanResubmit || row.IsResubmitting) return;

        row.Error = string.Empty;
        if (!TryBuildResubmitRequest(row, out var request, out var error))
        {
            row.Error = error;
            return;
        }

        var signature = ResubmitSignature(row.Id, request);
        var key = ResubmitKey(row.Id, signature);
        var version = _accessVersion;
        IsBusy = true;
        row.IsResubmitting = true;
        try
        {
            await _service.ResubmitAsync(row.Id, request, key).ConfigureAwait(true);
            if (version != _accessVersion) return;

            _resubmitIntents.Remove(row.Id);
            await LoadItemsCoreAsync(version).ConfigureAwait(true);
            SetMessage("Nội dung bổ sung đã được gửi lại.", false);
        }
        catch (Exception exception)
        {
            if (version == _accessVersion)
                row.Error = MutationError(exception);
        }
        finally
        {
            row.IsResubmitting = false;
            IsBusy = false;
        }
    }

    private async Task LoadItemsCoreAsync(int version)
    {
        var items = await _service.ListOwnAsync().ConfigureAwait(true);
        if (version != _accessVersion) return;

        Proposals.Clear();
        foreach (var item in items) Proposals.Add(ManagementProposalPresentation.Row(item));
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(EmptyText));
    }

    private bool TryBuildCreateRequest(out ManagementProposalCreateRequest request, out string error)
    {
        var title = Title.Trim();
        var content = Content.Trim();
        if (title.Length == 0 || content.Length == 0)
        {
            request = default!;
            error = "Vui lòng nhập Tiêu đề và Nội dung đề xuất.";
            return false;
        }

        if (title.Length > 240 || content.Length > 4000)
        {
            request = default!;
            error = "Nội dung đang dài hơn giới hạn cho phép. Vui lòng rút gọn rồi gửi lại.";
            return false;
        }

        if (!TryOptional(EntityId, 240, out var entityId)
            || !TryOptional(EntityLabel, 240, out var entityLabel)
            || !TryOptional(Impact, 1000, out var impact)
            || !TryOptional(Reason, 4000, out var reason)
            || !TryOptional(Rule, 1000, out var rule))
        {
            request = default!;
            error = "Thông tin bổ sung đang dài hơn giới hạn cho phép. Vui lòng rút gọn rồi gửi lại.";
            return false;
        }

        if (!TryEvidence(EvidenceText, out var evidence))
        {
            request = default!;
            error = "Phần bằng chứng / ghi chú đang quá dài. Vui lòng rút gọn rồi gửi lại.";
            return false;
        }

        request = new ManagementProposalCreateRequest(
            NormalizeChoice(Domain, ["commercial", "customer-debt", "operations"], "commercial"),
            title,
            content,
            NormalizeChoice(EntityType, ["customer", "sales-order", "purchase-order", "document", "route", "employee", "outlet", "other"], "other"),
            entityId,
            entityLabel,
            impact,
            reason,
            rule,
            evidence,
            NormalizeChoice(Priority, ["normal", "high", "critical"], "normal"));
        error = string.Empty;
        return true;
    }

    private static bool TryBuildResubmitRequest(
        ManagementProposalRow row,
        out ManagementProposalResubmitRequest request,
        out string error)
    {
        var content = row.ResubmitContent.Trim();
        if (content.Length == 0)
        {
            request = default!;
            error = "Vui lòng nhập Nội dung bổ sung.";
            return false;
        }

        if (content.Length > 4000 || !TryOptional(row.ResubmitReason, 4000, out var reason))
        {
            request = default!;
            error = "Nội dung đang dài hơn giới hạn cho phép. Vui lòng rút gọn rồi gửi lại.";
            return false;
        }

        if (!TryEvidence(row.ResubmitEvidence, out var evidence))
        {
            request = default!;
            error = "Phần bằng chứng / ghi chú đang quá dài. Vui lòng rút gọn rồi gửi lại.";
            return false;
        }

        request = new ManagementProposalResubmitRequest(content, reason, evidence);
        error = string.Empty;
        return true;
    }

    private string CreateKey(string signature)
    {
        if (_createIntentKey is not null && string.Equals(_createIntentSignature, signature, StringComparison.Ordinal))
            return _createIntentKey;

        _createIntentSignature = signature;
        _createIntentKey = _keys.Create("company-management-proposal");
        return _createIntentKey;
    }

    private string ResubmitKey(string id, string signature)
    {
        if (_resubmitIntents.TryGetValue(id, out var current)
            && string.Equals(current.Signature, signature, StringComparison.Ordinal))
            return current.Key;

        var key = _keys.Create("company-management-proposal-resubmit");
        _resubmitIntents[id] = (signature, key);
        return key;
    }

    private void CompleteCreateIntent()
    {
        _createIntentSignature = null;
        _createIntentKey = null;
    }

    private void ResetCreateForm()
    {
        Title = string.Empty;
        Content = string.Empty;
        Domain = "commercial";
        Priority = "normal";
        EntityType = "other";
        EntityId = string.Empty;
        EntityLabel = string.Empty;
        Impact = string.Empty;
        Reason = string.Empty;
        Rule = string.Empty;
        EvidenceText = string.Empty;
        CreateError = string.Empty;
    }

    private static string CreateSignature(ManagementProposalCreateRequest request) =>
        string.Join("\u001f",
            request.Domain, request.Title, request.Content, request.EntityType, request.EntityId,
            request.EntityLabel, request.Impact, request.Reason, request.Rule,
            string.Join("\u001e", request.Evidence), request.Priority);

    private static string ResubmitSignature(string id, ManagementProposalResubmitRequest request) =>
        string.Join("\u001f", id, request.Content, request.Reason, string.Join("\u001e", request.Evidence));

    private static bool TryOptional(string value, int max, out string normalized)
    {
        normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= max;
    }

    private static bool TryEvidence(string raw, out IReadOnlyList<string> entries)
    {
        var values = (raw ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();
        entries = values;
        return values.Length <= 50 && values.All(item => item.Length <= 1000);
    }

    private static string NormalizeChoice(string value, IReadOnlyCollection<string> allowed, string fallback)
    {
        var normalized = (value ?? string.Empty).Trim();
        return allowed.Contains(normalized, StringComparer.Ordinal) ? normalized : fallback;
    }

    private static string ReadError(Exception exception)
    {
        if (exception is CanonicalApiException api)
            return string.IsNullOrWhiteSpace(api.Message) ? "Không tải được Đề xuất ở thời điểm hiện tại." : api.Message;
        return "Không tải được Đề xuất ở thời điểm hiện tại.";
    }

    private static string MutationError(Exception exception)
    {
        if (exception is CanonicalApiException api)
        {
            if (api.StatusCode == HttpStatusCode.Forbidden)
                return "Tài khoản hiện tại chưa được cấp quyền gửi Đề xuất.";
            if (api.StatusCode == HttpStatusCode.Conflict)
                return "Đề xuất đã thay đổi trạng thái. Vui lòng tải lại rồi thực hiện lại.";
            if (api.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
                return string.IsNullOrWhiteSpace(api.Message) ? "Thông tin Đề xuất chưa hợp lệ." : api.Message;
            if (api.Retryable || (int)api.StatusCode >= 500)
                return "Chưa gửi được Đề xuất vì dịch vụ tạm thời chưa sẵn sàng. Nội dung vừa nhập vẫn được giữ; vui lòng thử lại.";
            return string.IsNullOrWhiteSpace(api.Message) ? "Chưa gửi được Đề xuất ở thời điểm hiện tại." : api.Message;
        }

        return "Chưa gửi được Đề xuất ở thời điểm hiện tại. Nội dung vừa nhập vẫn được giữ; vui lòng thử lại.";
    }

    private void DispatchAccessChanged()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(RaiseAccessChanged);
            return;
        }
        RaiseAccessChanged();
    }

    private void RaiseAccessChanged()
    {
        _accessVersion++;
        if (!CanUse)
        {
            ClearItems();
            CreateError = string.Empty;
            SetMessage(string.Empty, false);
        }
        OnPropertyChanged(nameof(CanUse));
        OnPropertyChanged(nameof(CanSubmit));
    }

    private void ClearItems()
    {
        Proposals.Clear();
        _resubmitIntents.Clear();
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(EmptyText));
    }

    private void SetMessage(string value, bool isError)
    {
        Message = value;
        MessageIsError = isError;
        OnPropertyChanged(nameof(EmptyText));
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
