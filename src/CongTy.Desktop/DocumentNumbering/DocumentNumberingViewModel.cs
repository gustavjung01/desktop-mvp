using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.DocumentNumbering;

public sealed class DocumentNumberingViewModel : INotifyPropertyChanged
{
    private const string ReadPermission = "core.document-number.read";
    private const string WritePermission = "core.document-number.write";
    private static readonly IReadOnlyDictionary<string, string> AllocationMetadata =
        new Dictionary<string, string>(StringComparer.Ordinal) { ["purpose"] = "manual_reference_allocation" };

    private readonly IDocumentNumberingService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly List<DocumentNumberSeriesData> _allSeries = [];

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _search = string.Empty;
    private string _activeFilter = "all";
    private DocumentNumberSeriesData? _selected;
    private bool _isEditorOpen;
    private DocumentNumberSeriesData? _editing;
    private string _formDocumentType = "SALES_ORDER";
    private string _formName = string.Empty;
    private string _formPrefix = "SO-";
    private string _formTemplate = "{PREFIX}{YYYY}{MM}-{SEQ}";
    private string _formResetPolicy = "MONTHLY";
    private string _documentDate = TodayVietnam();
    private DocumentNumberAllocationData? _lastAllocation;
    private string? _createIntentSignature;
    private string? _createIntentKey;
    private string? _allocationIntentSignature;
    private string? _allocationIntentKey;

    public DocumentNumberingViewModel(
        IDocumentNumberingService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        foreach (var option in DocumentNumberingPresentation.DocumentTypes) DocumentTypes.Add(option);
        foreach (var option in DocumentNumberingPresentation.ResetPolicies) ResetPolicies.Add(option);
        foreach (var option in DocumentNumberingPresentation.StatusFilters) StatusFilters.Add(option);

        _access.Changed += (_, _) =>
        {
            if (!CanRead)
            {
                _loaded = false;
                ClearData();
            }
            OnPropertyChanged(nameof(CanRead));
            OnPropertyChanged(nameof(CanWrite));
            RaiseActionState();
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<NumberSeriesRow> Series { get; } = [];
    public ObservableCollection<NumberCounterRow> Counters { get; } = [];
    public ObservableCollection<NumberAllocationRow> Allocations { get; } = [];
    public ObservableCollection<NumberingOption> DocumentTypes { get; } = [];
    public ObservableCollection<NumberingOption> ResetPolicies { get; } = [];
    public ObservableCollection<NumberingOption> StatusFilters { get; } = [];

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanWrite => _access.HasPermission(WritePermission);
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) RaiseActionState(); } }
    public bool IsNotBusy => !IsBusy;
    public string Message { get => _message; private set => SetField(ref _message, value); }
    public bool MessageIsError { get => _messageIsError; private set => SetField(ref _messageIsError, value); }

    public string Search { get => _search; set { if (SetField(ref _search, value)) RebuildSeries(); } }
    public string ActiveFilter { get => _activeFilter; set { if (SetField(ref _activeFilter, value)) RebuildSeries(); } }
    public bool HasSeries => Series.Count > 0;
    public bool NoSeries => !HasSeries;

    public bool HasSelected => _selected is not null;
    public string SelectedName => _selected?.Name ?? string.Empty;
    public string SelectedDocumentType => _selected is null ? string.Empty : DocumentNumberingPresentation.DocumentTypeLabel(_selected.DocumentType);
    public string SelectedStatus => _selected?.IsActive == true ? "Đang sử dụng" : "Đã ngừng";
    public bool SelectedIsActive => _selected?.IsActive == true;
    public bool HasCounters => Counters.Count > 0;
    public bool NoCounters => !HasCounters;
    public bool HasAllocations => Allocations.Count > 0;
    public bool NoAllocations => !HasAllocations;

    public string DocumentDate
    {
        get => _documentDate;
        set
        {
            if (SetField(ref _documentDate, value))
            {
                _allocationIntentSignature = null;
                _allocationIntentKey = null;
                OnPropertyChanged(nameof(CanAllocate));
            }
        }
    }

    public bool CanAllocate =>
        CanWrite && IsNotBusy && SelectedIsActive && DateOnly.TryParseExact(DocumentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    public bool HasLastAllocation => _lastAllocation is not null;
    public string LastAllocationCaption => _lastAllocation?.Replayed == true ? "Số đã cấp trước đó" : "Số vừa cấp";
    public string LastAllocationNumber => _lastAllocation?.DocumentNumber ?? string.Empty;
    public string LastAllocationDetail => _lastAllocation is null ? string.Empty : $"Kỳ {_lastAllocation.PeriodKey} · số thứ tự {_lastAllocation.CounterValue}";

    public bool IsEditorOpen => _isEditorOpen;
    public string EditorTitle => _editing is null ? "Tạo quy tắc đánh số" : $"Sửa {_editing.Name}";
    public string EditorDescription => "Chọn loại chứng từ và cấu hình cách hiển thị số. Hệ thống tự liên kết cấu hình cần thiết.";
    public bool IsEditing => _editing is not null;
    public bool IsFormatLocked => _editing?.FormatLocked == true;
    public bool DocumentTypeEnabled => _editing is null;
    public bool FormatFieldsEnabled => _editing?.FormatLocked != true;
    public string FormDocumentType { get => _formDocumentType; set { if (SetField(ref _formDocumentType, value)) ResetCreateIntent(); } }
    public string FormName { get => _formName; set { if (SetField(ref _formName, value)) { ResetCreateIntent(); OnPropertyChanged(nameof(CanSaveEditor)); } } }
    public string FormPrefix { get => _formPrefix; set { if (SetField(ref _formPrefix, (value ?? string.Empty).ToUpperInvariant())) ResetCreateIntent(); } }
    public string FormTemplate { get => _formTemplate; set { if (SetField(ref _formTemplate, (value ?? string.Empty).ToUpperInvariant())) ResetCreateIntent(); } }
    public string FormResetPolicy { get => _formResetPolicy; set { if (SetField(ref _formResetPolicy, value)) ResetCreateIntent(); } }
    public bool CanSaveEditor => CanWrite && IsNotBusy && IsEditorOpen && !string.IsNullOrWhiteSpace(FormName);

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !CanRead) return;
        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync() =>
        await RunBusyAsync(async () =>
        {
            if (!CanRead)
            {
                SetMessage("Tài khoản chưa được cấp quyền xem Số chứng từ.", true);
                return;
            }

            var selectedId = _selected?.Id;
            var rows = await _service.ListSeriesAsync().ConfigureAwait(true);
            _allSeries.Clear();
            _allSeries.AddRange(rows);
            _loaded = true;
            RebuildSeries();

            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                var refreshed = _allSeries.FirstOrDefault(item => item.Id == selectedId);
                if (refreshed is null)
                {
                    ClearSelection();
                }
                else
                {
                    _selected = refreshed;
                    RaiseSelectedState();
                    await LoadHistoryCoreAsync(refreshed.Id).ConfigureAwait(true);
                }
            }

            SetMessage(string.Empty, false);
        }).ConfigureAwait(true);

    public async Task SelectSeriesAsync(NumberSeriesRow row) =>
        await RunBusyAsync(async () =>
        {
            _selected = row.Data;
            RebuildSeries();
            _lastAllocation = null;
            _allocationIntentKey = null;
            _allocationIntentSignature = null;
            RaiseSelectedState();
            RaiseLastAllocation();
            await LoadHistoryCoreAsync(row.Id).ConfigureAwait(true);
            SetMessage(string.Empty, false);
        }).ConfigureAwait(true);

    public void OpenCreate()
    {
        if (!CanWrite || IsBusy) return;
        _editing = null;
        FormDocumentType = "SALES_ORDER";
        FormName = string.Empty;
        FormPrefix = "SO-";
        FormTemplate = "{PREFIX}{YYYY}{MM}-{SEQ}";
        FormResetPolicy = "MONTHLY";
        ResetCreateIntent();
        SetEditor(true);
    }

    public void OpenEdit(NumberSeriesRow row)
    {
        if (!CanWrite || IsBusy) return;
        _editing = row.Data;
        _formDocumentType = row.Data.DocumentType;
        _formName = row.Data.Name;
        _formPrefix = row.Data.Prefix;
        _formTemplate = row.Data.NumberTemplate;
        _formResetPolicy = row.Data.ResetPolicy;
        ResetCreateIntent();
        RaiseEditorFields();
        SetEditor(true);
    }

    public void CloseEditor()
    {
        if (IsBusy) return;
        SetEditor(false);
        _editing = null;
        ResetCreateIntent();
    }

    public async Task SaveEditorAsync()
    {
        if (!CanSaveEditor) return;
        await RunBusyAsync(async () =>
        {
            DocumentNumberSeriesData saved;
            if (_editing is null)
            {
                var request = new DocumentNumberSeriesCreateRequest(
                    FormDocumentType.Trim(),
                    FormName.Trim(),
                    FormPrefix.Trim().ToUpperInvariant(),
                    FormTemplate.Trim().ToUpperInvariant(),
                    FormResetPolicy);
                var signature = $"{request.DocumentType}|{request.Name}|{request.Prefix}|{request.NumberTemplate}|{request.ResetPolicy}";
                saved = await _service.CreateSeriesAsync(request, CreateKey(signature)).ConfigureAwait(true);
            }
            else
            {
                var request = new DocumentNumberSeriesUpdateRequest(
                    _editing.DocumentType,
                    FormName.Trim(),
                    FormPrefix.Trim().ToUpperInvariant(),
                    FormTemplate.Trim().ToUpperInvariant(),
                    FormResetPolicy,
                    _editing.UpdatedAt);
                saved = await _service.UpdateSeriesAsync(_editing.Id, request).ConfigureAwait(true);
            }

            var created = _editing is null;
            await ReloadAndSelectAsync(saved.Id).ConfigureAwait(true);
            if (created) CompleteCreateIntent();
            SetEditor(false);
            _editing = null;
            SetMessage(created ? "Đã tạo quy tắc đánh số." : "Đã cập nhật quy tắc đánh số.", false);
        }).ConfigureAwait(true);
    }

    public async Task ToggleActiveAsync(NumberSeriesRow row)
    {
        if (!CanWrite || IsBusy) return;
        await RunBusyAsync(async () =>
        {
            var saved = await _service.UpdateSeriesStatusAsync(
                row.Id,
                new DocumentNumberSeriesStatusRequest(!row.Data.IsActive, row.Data.UpdatedAt)).ConfigureAwait(true);
            await ReloadAndSelectIfCurrentAsync(saved.Id).ConfigureAwait(true);
            SetMessage(saved.IsActive ? "Đã đưa quy tắc vào sử dụng." : "Đã ngừng sử dụng quy tắc.", false);
        }).ConfigureAwait(true);
    }

    public async Task RefreshHistoryAsync()
    {
        if (_selected is null || IsBusy) return;
        await RunBusyAsync(async () =>
        {
            await LoadHistoryCoreAsync(_selected.Id).ConfigureAwait(true);
            SetMessage(string.Empty, false);
        }).ConfigureAwait(true);
    }

    public async Task AllocateReferenceAsync()
    {
        if (!CanAllocate || _selected is null) return;
        await RunBusyAsync(async () =>
        {
            if (!DateOnly.TryParseExact(DocumentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                SetMessage("Ngày chứng từ chưa hợp lệ.", true);
                return;
            }

            var normalizedDate = date.ToString("yyyy-MM-dd");
            var request = new DocumentNumberAllocationRequest(normalizedDate, AllocationMetadata);
            var signature = $"{_selected.Id}|{normalizedDate}|manual_reference_allocation";
            var saved = await _service.AllocateReferenceAsync(_selected.Id, request, AllocationKey(signature)).ConfigureAwait(true);
            _lastAllocation = saved;
            RaiseLastAllocation();

            await LoadHistoryCoreAsync(_selected.Id).ConfigureAwait(true);
            var selectedId = _selected.Id;
            var rows = await _service.ListSeriesAsync().ConfigureAwait(true);
            _allSeries.Clear();
            _allSeries.AddRange(rows);
            RebuildSeries();
            _selected = _allSeries.FirstOrDefault(item => item.Id == selectedId) ?? _selected;
            RaiseSelectedState();
            CompleteAllocationIntent();

            SetMessage(saved.Replayed
                ? "Thao tác trước đã hoàn tất; hệ thống hiển thị lại đúng số đã cấp."
                : "Đã cấp số tham chiếu và lưu vào lịch sử.", false);
        }).ConfigureAwait(true);
    }

    private async Task ReloadAndSelectAsync(string id)
    {
        var rows = await _service.ListSeriesAsync().ConfigureAwait(true);
        _allSeries.Clear();
        _allSeries.AddRange(rows);
        _selected = _allSeries.FirstOrDefault(item => item.Id == id);
        RebuildSeries();
        _lastAllocation = null;
        RaiseSelectedState();
        RaiseLastAllocation();
        if (_selected is not null) await LoadHistoryCoreAsync(_selected.Id).ConfigureAwait(true);
    }

    private async Task ReloadAndSelectIfCurrentAsync(string changedId)
    {
        var selectedId = _selected?.Id;
        var rows = await _service.ListSeriesAsync().ConfigureAwait(true);
        _allSeries.Clear();
        _allSeries.AddRange(rows);
        RebuildSeries();
        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            _selected = _allSeries.FirstOrDefault(item => item.Id == selectedId);
            RaiseSelectedState();
            if (_selected is not null && selectedId == changedId) await LoadHistoryCoreAsync(selectedId).ConfigureAwait(true);
        }
    }

    private async Task LoadHistoryCoreAsync(string seriesId)
    {
        var history = await _service.GetHistoryAsync(seriesId).ConfigureAwait(true);
        Counters.Clear();
        foreach (var counter in history.Counters) Counters.Add(new NumberCounterRow(counter.PeriodKey, counter.NextCounter));
        Allocations.Clear();
        foreach (var allocation in history.Allocations) Allocations.Add(new NumberAllocationRow(allocation));
        OnPropertyChanged(nameof(HasCounters)); OnPropertyChanged(nameof(NoCounters));
        OnPropertyChanged(nameof(HasAllocations)); OnPropertyChanged(nameof(NoAllocations));
    }

    private void RebuildSeries()
    {
        var query = Search.Trim();
        Series.Clear();
        foreach (var item in _allSeries)
        {
            if (ActiveFilter == "active" && !item.IsActive) continue;
            if (ActiveFilter == "inactive" && item.IsActive) continue;
            if (query.Length > 0)
            {
                var haystack = $"{item.DocumentType} {DocumentNumberingPresentation.DocumentTypeLabel(item.DocumentType)} {item.Name} {item.Prefix}";
                if (!haystack.Contains(query, StringComparison.CurrentCultureIgnoreCase)) continue;
            }
            Series.Add(new NumberSeriesRow(item, item.Id == _selected?.Id));
        }
        OnPropertyChanged(nameof(HasSeries)); OnPropertyChanged(nameof(NoSeries));
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        MessageIsError = false;
        try { await action().ConfigureAwait(true); }
        catch (Exception exception) { SetMessage(exception.Message, true); }
        finally { IsBusy = false; }
    }

    private string CreateKey(string signature)
    {
        if (_createIntentKey is not null && _createIntentSignature == signature) return _createIntentKey;
        _createIntentSignature = signature;
        _createIntentKey = _idempotencyKeys.Create("document-number-series-create");
        return _createIntentKey;
    }

    private string AllocationKey(string signature)
    {
        if (_allocationIntentKey is not null && _allocationIntentSignature == signature) return _allocationIntentKey;
        _allocationIntentSignature = signature;
        _allocationIntentKey = _idempotencyKeys.Create("document-number-reference");
        return _allocationIntentKey;
    }

    private void ResetCreateIntent() { _createIntentSignature = null; _createIntentKey = null; }
    private void CompleteCreateIntent() => ResetCreateIntent();
    private void CompleteAllocationIntent() { _allocationIntentSignature = null; _allocationIntentKey = null; }

    private void SetEditor(bool value)
    {
        _isEditorOpen = value;
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorDescription));
        OnPropertyChanged(nameof(CanSaveEditor));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(IsFormatLocked));
        OnPropertyChanged(nameof(DocumentTypeEnabled));
        OnPropertyChanged(nameof(FormatFieldsEnabled));
    }

    private void RaiseEditorFields()
    {
        OnPropertyChanged(nameof(FormDocumentType)); OnPropertyChanged(nameof(FormName)); OnPropertyChanged(nameof(FormPrefix));
        OnPropertyChanged(nameof(FormTemplate)); OnPropertyChanged(nameof(FormResetPolicy));
        OnPropertyChanged(nameof(IsFormatLocked)); OnPropertyChanged(nameof(DocumentTypeEnabled)); OnPropertyChanged(nameof(FormatFieldsEnabled));
    }

    private void RaiseSelectedState()
    {
        OnPropertyChanged(nameof(HasSelected)); OnPropertyChanged(nameof(SelectedName)); OnPropertyChanged(nameof(SelectedDocumentType));
        OnPropertyChanged(nameof(SelectedStatus)); OnPropertyChanged(nameof(SelectedIsActive)); OnPropertyChanged(nameof(CanAllocate));
    }

    private void RaiseLastAllocation()
    {
        OnPropertyChanged(nameof(HasLastAllocation)); OnPropertyChanged(nameof(LastAllocationCaption));
        OnPropertyChanged(nameof(LastAllocationNumber)); OnPropertyChanged(nameof(LastAllocationDetail));
    }

    private void RaiseActionState()
    {
        OnPropertyChanged(nameof(IsNotBusy)); OnPropertyChanged(nameof(CanSaveEditor)); OnPropertyChanged(nameof(CanAllocate));
    }

    private void ClearSelection()
    {
        _selected = null;
        _lastAllocation = null;
        Counters.Clear();
        Allocations.Clear();
        RaiseSelectedState();
        RaiseLastAllocation();
        OnPropertyChanged(nameof(HasCounters)); OnPropertyChanged(nameof(NoCounters));
        OnPropertyChanged(nameof(HasAllocations)); OnPropertyChanged(nameof(NoAllocations));
    }

    private void ClearData()
    {
        _allSeries.Clear();
        Series.Clear();
        ClearSelection();
        OnPropertyChanged(nameof(HasSeries)); OnPropertyChanged(nameof(NoSeries));
    }

    private void SetMessage(string value, bool isError) { Message = value; MessageIsError = isError; }

    private static string TodayVietnam()
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).ToString("yyyy-MM-dd");
        }
        catch
        {
            return DateTime.Today.ToString("yyyy-MM-dd");
        }
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
