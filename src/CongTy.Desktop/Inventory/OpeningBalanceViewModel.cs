using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed partial class OpeningBalanceViewModel : INotifyPropertyChanged
{
    private const int PreviewPageSize = 100;

    private readonly IOpeningBalanceService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly List<OpeningBalancePreviewRow> _allRows = [];

    private bool _loaded;
    private long _accessGeneration;
    private long _locationGeneration;
    private long _draftRevision;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _selectedWarehouseId = string.Empty;
    private string _defaultLocationCode = string.Empty;
    private string _sourceKey = string.Empty;
    private DateTime? _documentDate = DateTime.Today;
    private string _filename = string.Empty;
    private int _previewPage;
    private OpeningBalanceValidationResultData? _validation;
    private string? _validationChecksum;
    private string? _pendingPostFingerprint;
    private string? _pendingPostKey;

    public OpeningBalanceViewModel(
        IOpeningBalanceService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _locationGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            _pendingPostFingerprint = null;
            _pendingPostKey = null;
            ResetData();
            RaisePermissions();
            if (CanImport && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<OpeningBalanceOption> Warehouses { get; } = [];
    public ObservableCollection<OpeningBalanceOption> Locations { get; } = [];
    public ObservableCollection<OpeningBalancePreviewRow> PreviewRows { get; } = [];
    public ObservableCollection<string> ValidationErrors { get; } = [];
    public ObservableCollection<OpeningBalanceHistoryRow> HistoryRows { get; } = [];

    public bool CanImport => _access.HasPermission("core.inventory.opening-balance.import");
    public bool CanReadHistory => _access.HasPermission("core.inventory.read");
    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool CanChooseFile => CanImport;
    public bool CanSelectWarehouse => CanImport && _busyAction != "bootstrap";
    public bool CanSelectDefaultLocation => CanImport
        && !string.IsNullOrWhiteSpace(SelectedWarehouseId)
        && _busyAction is not "bootstrap" and not "locations";
    public bool CanValidate => CanImport && IsNotBusy;
    public bool CanPost => CanImport
        && IsNotBusy
        && _validation is not null
        && !string.IsNullOrWhiteSpace(_validationChecksum)
        && _validation.RowErrors.Length == 0;
    public bool HasRows => _allRows.Count > 0;
    public bool HasNoRows => _allRows.Count == 0;
    public bool HasValidation => _validation is not null;
    public bool ValidationSucceeded => _validation is { RowErrors.Length: 0 };
    public bool ValidationFailed => _validation is { RowErrors.Length: > 0 };
    public bool HasHistory => HistoryRows.Count > 0;
    public bool HasNoHistory => CanReadHistory && HistoryRows.Count == 0;

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

    public string SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            var next = value?.Trim() ?? string.Empty;
            if (!SetField(ref _selectedWarehouseId, next)) return;

            _locationGeneration++;
            Locations.Clear();
            _defaultLocationCode = string.Empty;
            OnPropertyChanged(nameof(DefaultLocationCode));
            OnPropertyChanged(nameof(CanSelectDefaultLocation));
            InvalidateDraft(intentChanged: true);
            UpdateRowContext();
            _ = LoadLocationsAsync(next, _locationGeneration);
        }
    }

    public string DefaultLocationCode
    {
        get => _defaultLocationCode;
        set
        {
            var next = value?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!SetField(ref _defaultLocationCode, next)) return;
            InvalidateDraft(intentChanged: true);
            UpdateRowContext();
        }
    }

    public string SourceKey
    {
        get => _sourceKey;
        set
        {
            if (!SetField(ref _sourceKey, value ?? string.Empty)) return;
            InvalidateDraft(intentChanged: true);
        }
    }

    public DateTime? DocumentDate
    {
        get => _documentDate;
        set
        {
            if (!SetField(ref _documentDate, value)) return;
            InvalidateDraft(intentChanged: true);
        }
    }

    public string Filename
    {
        get => _filename;
        private set
        {
            if (!SetField(ref _filename, value)) return;
            OnPropertyChanged(nameof(FilenameDisplay));
        }
    }

    public string FilenameDisplay => string.IsNullOrWhiteSpace(Filename) ? "Chưa chọn tệp" : Filename;
    public string SelectedWarehouseLabel =>
        Warehouses.FirstOrDefault(item => item.Id == SelectedWarehouseId)?.Label ?? "Chưa chọn";
    public int RowCount => _allRows.Count;
    public int LocalErrorCount => _allRows.Count(row => !string.IsNullOrWhiteSpace(row.LocalError));
    public string PreviewTitle => HasRows
        ? $"{RowCount} dòng trong {Filename}"
        : "Chưa có dữ liệu để xem trước.";
    public string PreviewReadiness => LocalErrorCount > 0
        ? $"{LocalErrorCount} dòng thiếu dữ liệu"
        : HasRows ? "Sẵn sàng kiểm tra" : string.Empty;
    public int PreviewPageCount => Math.Max(1, (int)Math.Ceiling(RowCount / (double)PreviewPageSize));
    public int PreviewPageNumber => Math.Min(_previewPage + 1, PreviewPageCount);
    public bool HasPreviewPrevious => _previewPage > 0 && IsNotBusy;
    public bool HasPreviewNext => _previewPage + 1 < PreviewPageCount && IsNotBusy;
    public string PreviewPageText => $"Trang {PreviewPageNumber}/{PreviewPageCount}";
    public string PreviewRange
    {
        get
        {
            if (!HasRows) return string.Empty;
            var start = _previewPage * PreviewPageSize + 1;
            var end = Math.Min(RowCount, start + PreviewRows.Count - 1);
            return $"Đang xem {start}–{end} / {RowCount}";
        }
    }

    public string ValidateText => _busyAction == "validate" ? "Đang kiểm tra…" : "Kiểm tra tệp";
    public string PostText => _busyAction == "post" ? "Đang ghi nhận…" : "Xác nhận nhập tồn";
    public string ValidationHeadline => _validation is null
        ? "Chưa kiểm tra tệp hoặc dữ liệu vừa được chỉnh sửa."
        : _validation.RowErrors.Length > 0
            ? $"{_validation.RowErrors.Length} dòng cần sửa"
            : $"{_validation.Totals.RowCount} dòng hợp lệ";
    public string ValidationSourceTotal => _validation is null
        ? string.Empty
        : $"Tổng số lượng theo đơn vị nhập: {OpeningBalancePresentation.DisplayQuantity(_validation.Totals.SourceQuantityTotal)}";
    public string ValidationBaseTotal => _validation is null
        ? string.Empty
        : $"Quy đổi tồn kho: {OpeningBalancePresentation.DisplayQuantity(_validation.Totals.BaseQuantityTotal)}";
    public string ValidationWarehouse => _validation is null ? string.Empty : $"Kho: {SelectedWarehouseLabel}";
    public string HistoryEmptyText => CanReadHistory
        ? "Chưa có lần nhập nào."
        : "Tài khoản chưa được cấp quyền xem lịch sử nhập tồn đầu kỳ.";

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);

        _initialLoadTask = LoadBootstrapAsync();
        try { return await _initialLoadTask.ConfigureAwait(true); }
        finally { _initialLoadTask = null; }
    }

    public Task<bool> RefreshAsync() => LoadBootstrapAsync();

    public async Task<bool> LoadCsvFileAsync(string path)
    {
        if (!CanImport)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền Thiết lập tồn đầu kỳ.");
            return false;
        }

        ResetDraftRows();
        if (!string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            SetErrorMessage("Chỉ nhận tệp CSV UTF-8. Trong Excel, chọn “Lưu thành CSV UTF-8” rồi tải lại.");
            return false;
        }

        try
        {
            var parsed = OpeningBalanceCsv.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(true));
            if (parsed.Count == 0)
            {
                SetErrorMessage("Tệp cần đúng mẫu và có ít nhất một dòng dữ liệu. Hai cột bắt buộc là SKU và Số lượng.");
                return false;
            }

            Filename = Path.GetFileName(path);
            for (var index = 0; index < parsed.Count; index++)
            {
                _allRows.Add(new OpeningBalancePreviewRow(index, parsed[index], OnRowDraftChanged));
            }

            _previewPage = 0;
            UpdateRowContext();
            ApplyPreviewPage();
            ClearValidation();
            SetNotice($"Đã đọc {parsed.Count} dòng. Bấm “Kiểm tra tệp” để đối chiếu SKU và tự áp dụng chính sách lô/hạn dùng.");
            RaiseDraftState();
            return true;
        }
        catch
        {
            SetErrorMessage("Không đọc được tệp CSV. Hãy lưu lại dưới dạng CSV UTF-8 rồi thử lại.");
            return false;
        }
    }

    public async Task<bool> ValidateAsync()
    {
        if (!CanImport)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền Thiết lập tồn đầu kỳ.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedWarehouseId))
        {
            SetErrorMessage("Chọn kho trước khi kiểm tra tệp.");
            return false;
        }

        var normalizedSourceKey = SourceKey.Trim().ToUpperInvariant();
        if (normalizedSourceKey.Length == 0)
        {
            SetErrorMessage("Nhập mã đợt dữ liệu để tránh nhập trùng.");
            return false;
        }

        if (!SourceKeyPattern().IsMatch(normalizedSourceKey))
        {
            SetErrorMessage("Mã đợt dữ liệu chỉ dùng chữ không dấu, số và các ký tự . _ : - (tối đa 128 ký tự).");
            return false;
        }

        if (!HasRows)
        {
            SetErrorMessage("Chọn tệp CSV trước khi kiểm tra.");
            return false;
        }

        if (LocalErrorCount > 0)
        {
            SetErrorMessage("Tệp còn dòng thiếu SKU hoặc số lượng. Sửa các dòng báo lỗi trước.");
            return false;
        }

        if (DocumentDate is null)
        {
            SetErrorMessage("Chọn ngày ghi nhận trước khi kiểm tra tệp.");
            return false;
        }

        var generation = _accessGeneration;
        var revision = _draftRevision;
        var draft = BuildDraft();
        var checksum = OpeningBalanceChecksum.Compute(draft);
        var request = ToRequest(draft, checksum);

        SetBusy("validate");
        ClearMessage();
        ClearValidation();
        try
        {
            var result = await _service.ValidateAsync(request).ConfigureAwait(true);
            if (generation != _accessGeneration || revision != _draftRevision || !CanImport) return false;

            _validation = result;
            _validationChecksum = checksum;
            ApplyValidationToRows(result);
            RefreshValidationErrors();
            RaiseValidationState();

            if (result.RowErrors.Length > 0)
            {
                SetErrorMessage("Có dòng chưa hợp lệ. Chính sách SKU đã được đối chiếu; điền dữ liệu lô/hạn dùng được yêu cầu rồi kiểm tra lại.");
            }
            else
            {
                SetNotice("Dữ liệu đã khớp kho, vị trí, SKU và chính sách tồn kho. Có thể xác nhận nhập tồn.");
            }

            return result.RowErrors.Length == 0;
        }
        catch (Exception exception)
        {
            if (generation != _accessGeneration || revision != _draftRevision || !CanImport) return false;
            SetError(exception);
            return false;
        }
        finally
        {
            SetBusy(null);
        }
    }

    public async Task<bool> PostAsync()
    {
        if (!CanPost || _validationChecksum is null) return false;

        var draft = BuildDraft();
        var checksum = OpeningBalanceChecksum.Compute(draft);
        if (!string.Equals(checksum, _validationChecksum, StringComparison.Ordinal))
        {
            InvalidateDraft(intentChanged: true);
            SetErrorMessage("Kho, vị trí hoặc dữ liệu đã thay đổi sau lần kiểm tra. Vui lòng kiểm tra lại trước khi xác nhận.");
            return false;
        }

        if (!string.Equals(_pendingPostFingerprint, checksum, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(_pendingPostKey))
        {
            _pendingPostFingerprint = checksum;
            _pendingPostKey = _idempotencyKeys.Create("inventory-opening-balance-post");
        }

        var request = ToRequest(draft, checksum);
        var generation = _accessGeneration;
        SetBusy("post");
        ClearMessage();
        try
        {
            await _service.PostAsync(request, _pendingPostKey!).ConfigureAwait(true);
            if (generation != _accessGeneration || !CanImport) return false;

            _pendingPostFingerprint = null;
            _pendingPostKey = null;
            ResetDraftRows();
            SourceKey = string.Empty;
            if (CanReadHistory) await LoadHistoryAsync(generation).ConfigureAwait(true);
            SetNotice("Đã ghi nhận tồn đầu kỳ thành công. Kho đang chọn được giữ lại cho đợt tiếp theo.");
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

    public void PreviewPrevious()
    {
        if (!HasPreviewPrevious) return;
        _previewPage--;
        ApplyPreviewPage();
    }

    public void PreviewNext()
    {
        if (!HasPreviewNext) return;
        _previewPage++;
        ApplyPreviewPage();
    }

    private async Task<bool> LoadBootstrapAsync()
    {
        if (!CanImport)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền Thiết lập tồn đầu kỳ.");
            return false;
        }

        var generation = _accessGeneration;
        SetBusy("bootstrap");
        ClearMessage();
        try
        {
            var warehouses = await _service.ListWarehousesAsync().ConfigureAwait(true);
            if (generation != _accessGeneration || !CanImport) return false;

            Replace(Warehouses, warehouses.Select(item =>
                new OpeningBalanceOption(item.Id, $"{item.Code} — {item.Name}")));

            if (!string.IsNullOrWhiteSpace(_selectedWarehouseId)
                && Warehouses.All(item => item.Id != _selectedWarehouseId))
            {
                _selectedWarehouseId = string.Empty;
                OnPropertyChanged(nameof(SelectedWarehouseId));
                OnPropertyChanged(nameof(CanSelectDefaultLocation));
            }

            if (Warehouses.Count == 1 && string.IsNullOrWhiteSpace(_selectedWarehouseId))
            {
                _selectedWarehouseId = Warehouses[0].Id;
                OnPropertyChanged(nameof(SelectedWarehouseId));
                OnPropertyChanged(nameof(CanSelectDefaultLocation));
            }

            Locations.Clear();
            _defaultLocationCode = string.Empty;
            OnPropertyChanged(nameof(DefaultLocationCode));
            if (!string.IsNullOrWhiteSpace(_selectedWarehouseId))
            {
                var locationGeneration = ++_locationGeneration;
                await LoadLocationsCoreAsync(_selectedWarehouseId, generation, locationGeneration).ConfigureAwait(true);
            }

            if (CanReadHistory)
            {
                await LoadHistoryAsync(generation).ConfigureAwait(true);
            }
            else
            {
                HistoryRows.Clear();
                OnPropertyChanged(nameof(HasHistory));
                OnPropertyChanged(nameof(HasNoHistory));
                OnPropertyChanged(nameof(HistoryEmptyText));
            }

            UpdateRowContext();
            _loaded = true;
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

    private async Task LoadLocationsAsync(string warehouseId, long locationGeneration)
    {
        if (!CanImport || string.IsNullOrWhiteSpace(warehouseId)) return;
        var generation = _accessGeneration;
        SetBusy("locations");
        try
        {
            await LoadLocationsCoreAsync(warehouseId, generation, locationGeneration).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
        }
    }

    private async Task LoadLocationsCoreAsync(
        string warehouseId,
        long generation,
        long locationGeneration)
    {
        var result = await _service.ListLocationsAsync(warehouseId).ConfigureAwait(true);
        if (generation != _accessGeneration
            || locationGeneration != _locationGeneration
            || !string.Equals(_selectedWarehouseId, warehouseId, StringComparison.Ordinal))
        {
            return;
        }

        Replace(Locations, result.Locations.Select(item =>
            new OpeningBalanceOption(item.Code, $"{item.Code} — {item.Name}")));
        OnPropertyChanged(nameof(SelectedWarehouseLabel));
        UpdateRowContext();
    }

    private async Task LoadHistoryAsync(long generation)
    {
        try
        {
            var imports = await _service.ListImportsAsync().ConfigureAwait(true);
            if (generation != _accessGeneration || !CanReadHistory) return;

            Replace(HistoryRows, imports.Select((item, index) => new OpeningBalanceHistoryRow(
                index + 1,
                item.SourceKey,
                string.IsNullOrWhiteSpace(item.SourceFilename) ? "—" : item.SourceFilename,
                item.RowCount,
                OpeningBalancePresentation.DateTimeText(item.CreatedAt))));
            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(HasNoHistory));
            OnPropertyChanged(nameof(HistoryEmptyText));
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    private OpeningBalanceOperatorDraftData BuildDraft() => new()
    {
        WarehouseId = SelectedWarehouseId,
        SourceKey = SourceKey.Trim().ToUpperInvariant(),
        SourceFilename = string.IsNullOrWhiteSpace(Filename) ? null : Filename,
        DocumentDate = (DocumentDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Metadata = new OpeningBalanceOperatorMetadataData
        {
            ImportMethod = "csv-upload-operator",
            OriginalFilename = Filename,
            DefaultLocationCode = string.IsNullOrWhiteSpace(DefaultLocationCode) ? null : DefaultLocationCode
        },
        Rows = _allRows.Select(row => row.ToRequestRow()).ToArray()
    };

    private static OpeningBalanceOperatorRequestData ToRequest(
        OpeningBalanceOperatorDraftData draft,
        string checksum) => new()
    {
        WarehouseId = draft.WarehouseId,
        SourceKey = draft.SourceKey,
        SourceFilename = draft.SourceFilename,
        DocumentDate = draft.DocumentDate,
        Metadata = draft.Metadata,
        Rows = draft.Rows,
        ContentChecksum = checksum
    };

    private void ApplyValidationToRows(OpeningBalanceValidationResultData result)
    {
        var resolvedByLine = result.Rows.ToDictionary(row => row.LineNumber);
        var errorsByLine = result.RowErrors
            .GroupBy(error => error.LineNumber)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var row in _allRows)
        {
            resolvedByLine.TryGetValue(row.ApiLine, out var resolved);
            errorsByLine.TryGetValue(row.ApiLine, out var error);
            row.ApplyValidation(resolved, error, current: true);
        }

        ApplyPreviewPage();
    }

    private void RefreshValidationErrors()
    {
        ValidationErrors.Clear();
        if (_validation is null) return;

        foreach (var error in _validation.RowErrors)
        {
            ValidationErrors.Add($"Dòng {error.LineNumber + 1}: {OpeningBalancePresentation.RowError(error)}");
        }
    }

    private void OnRowDraftChanged()
    {
        InvalidateDraft(intentChanged: true);
        RaiseDraftState();
    }

    private void InvalidateDraft(bool intentChanged)
    {
        _draftRevision++;
        foreach (var row in _allRows) row.InvalidateValidation();
        ClearValidation();
        if (intentChanged)
        {
            _pendingPostFingerprint = null;
            _pendingPostKey = null;
        }
        RaiseValidationState();
    }

    private void ClearValidation()
    {
        _validation = null;
        _validationChecksum = null;
        ValidationErrors.Clear();
        RaiseValidationState();
    }

    private void ResetDraftRows()
    {
        _draftRevision++;
        _allRows.Clear();
        PreviewRows.Clear();
        Filename = string.Empty;
        _previewPage = 0;
        ClearValidation();
        _pendingPostFingerprint = null;
        _pendingPostKey = null;
        RaiseDraftState();
    }

    private void ApplyPreviewPage()
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(RowCount / (double)PreviewPageSize));
        _previewPage = Math.Min(_previewPage, pageCount - 1);
        Replace(
            PreviewRows,
            _allRows.Skip(_previewPage * PreviewPageSize).Take(PreviewPageSize));
        RaisePreviewState();
    }

    private void UpdateRowContext()
    {
        var warehouseLabel = SelectedWarehouseLabel;
        foreach (var row in _allRows)
        {
            row.SetContext(warehouseLabel, DefaultLocationCode);
        }

        OnPropertyChanged(nameof(SelectedWarehouseLabel));
    }

    private void RaiseDraftState()
    {
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(HasNoRows));
        OnPropertyChanged(nameof(RowCount));
        OnPropertyChanged(nameof(LocalErrorCount));
        OnPropertyChanged(nameof(PreviewTitle));
        OnPropertyChanged(nameof(PreviewReadiness));
        ApplyPreviewPage();
    }

    private void RaisePreviewState()
    {
        OnPropertyChanged(nameof(PreviewPageCount));
        OnPropertyChanged(nameof(PreviewPageNumber));
        OnPropertyChanged(nameof(HasPreviewPrevious));
        OnPropertyChanged(nameof(HasPreviewNext));
        OnPropertyChanged(nameof(PreviewPageText));
        OnPropertyChanged(nameof(PreviewRange));
    }

    private void RaiseValidationState()
    {
        OnPropertyChanged(nameof(HasValidation));
        OnPropertyChanged(nameof(ValidationSucceeded));
        OnPropertyChanged(nameof(ValidationFailed));
        OnPropertyChanged(nameof(ValidationHeadline));
        OnPropertyChanged(nameof(ValidationSourceTotal));
        OnPropertyChanged(nameof(ValidationBaseTotal));
        OnPropertyChanged(nameof(ValidationWarehouse));
        OnPropertyChanged(nameof(CanPost));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanImport));
        OnPropertyChanged(nameof(CanReadHistory));
        OnPropertyChanged(nameof(CanChooseFile));
        OnPropertyChanged(nameof(CanSelectWarehouse));
        OnPropertyChanged(nameof(CanSelectDefaultLocation));
        OnPropertyChanged(nameof(CanValidate));
        OnPropertyChanged(nameof(CanPost));
        OnPropertyChanged(nameof(HistoryEmptyText));
    }

    private void SetBusy(string? action)
    {
        if (_busyAction == action) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(CanSelectWarehouse));
        OnPropertyChanged(nameof(CanSelectDefaultLocation));
        OnPropertyChanged(nameof(CanValidate));
        OnPropertyChanged(nameof(CanPost));
        OnPropertyChanged(nameof(ValidateText));
        OnPropertyChanged(nameof(PostText));
        RaisePreviewState();
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
            SetErrorMessage(CanonicalErrorMessages.WithRequestId(
                OpeningBalancePresentation.ApiError(apiException),
                apiException.RequestId));
            return;
        }

        SetErrorMessage("Không thực hiện được thao tác. Vui lòng thử lại.");
    }

    private void ResetData()
    {
        Warehouses.Clear();
        Locations.Clear();
        HistoryRows.Clear();
        _selectedWarehouseId = string.Empty;
        _defaultLocationCode = string.Empty;
        _sourceKey = string.Empty;
        _documentDate = DateTime.Today;
        ResetDraftRows();
        Message = string.Empty;
        MessageIsError = false;
        OnPropertyChanged(nameof(SelectedWarehouseId));
        OnPropertyChanged(nameof(CanSelectDefaultLocation));
        OnPropertyChanged(nameof(DefaultLocationCode));
        OnPropertyChanged(nameof(SourceKey));
        OnPropertyChanged(nameof(DocumentDate));
        OnPropertyChanged(nameof(SelectedWarehouseLabel));
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(HasNoHistory));
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    [GeneratedRegex("^[A-Za-z0-9._:-]{1,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceKeyPattern();
}
