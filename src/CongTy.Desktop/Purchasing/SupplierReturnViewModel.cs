using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public sealed partial class SupplierReturnViewModel : INotifyPropertyChanged
{
    private const string Read = "core.supplier-return.read";
    private const string Create = "core.supplier-return.create";
    private const string Update = "core.supplier-return.update";
    private const string Submit = "core.supplier-return.submit";
    private const string Approve = "core.supplier-return.approve";
    private const string Cancel = "core.supplier-return.cancel";
    private const string Post = "core.supplier-return.post";
    private const string Reverse = "core.supplier-return.reverse";
    private const string GoodsReceiptRead = "core.goods-receipt.read";

    private readonly ISupplierReturnService _service;
    private readonly IGoodsReceiptService _goodsReceipts;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dispatcher _uiDispatcher;
    private readonly List<SupplierReturnData> _allReturns = [];
    private readonly List<GoodsReceiptData> _postedReceipts = [];
    private readonly Dictionary<string, string> _actionKeys = new(StringComparer.Ordinal);
    private CancellationTokenSource? _sourceLinesCts;

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _searchText = string.Empty;
    private string _selectedStatus = "all";

    private bool _isEditorOpen;
    private SupplierReturnData? _editing;
    private string? _draftAttemptKey;
    private string _selectedGoodsReceiptId = string.Empty;
    private DateTime? _returnDate = DateTime.Today;
    private string _editorNote = string.Empty;
    private string _editorSupplier = "—";
    private string _editorWarehouse = "—";

    private bool _isDetailOpen;
    private string _detailTitle = string.Empty;
    private string _detailStatus = string.Empty;
    private string _detailSupplier = string.Empty;
    private string _detailWarehouse = string.Empty;
    private string _detailReturnDate = string.Empty;
    private string _detailQuantity = string.Empty;
    private string _detailMovement = string.Empty;
    private string _detailLifecycleNote = string.Empty;

    private bool _isActionOpen;
    private SupplierReturnData? _actionTarget;
    private string _actionKind = string.Empty;
    private string _actionTitle = string.Empty;
    private string _actionPrompt = string.Empty;
    private DateTime? _actionDate = DateTime.Today;
    private string _actionReason = string.Empty;
    private bool _actionDateVisible;
    private bool _actionReasonVisible;
    private bool _actionReasonRequired;

    public SupplierReturnViewModel(
        ISupplierReturnService service,
        IGoodsReceiptService goodsReceipts,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _goodsReceipts = goodsReceipts;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        _uiDispatcher = Dispatcher.CurrentDispatcher;

        StatusOptions.Add(new SupplierReturnStatusOption("all", "Tất cả trạng thái"));
        foreach (var pair in SupplierReturnPresentation.StatusLabels)
        {
            StatusOptions.Add(new SupplierReturnStatusOption(pair.Key, pair.Value));
        }

        _access.Changed += (_, _) =>
        {
            if (_uiDispatcher.CheckAccess())
            {
                HandleAccessChanged();
                return;
            }

            _ = _uiDispatcher.BeginInvoke((Action)HandleAccessChanged);
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SupplierReturnRow> Rows { get; } = [];
    public ObservableCollection<SupplierReturnStatusOption> StatusOptions { get; } = [];
    public ObservableCollection<SupplierReturnReceiptOption> EligibleReceipts { get; } = [];
    public ObservableCollection<SupplierReturnEditorLine> EditorLines { get; } = [];
    public ObservableCollection<SupplierReturnDetailLineRow> DetailLines { get; } = [];

    public bool CanRead => _access.HasPermission(Read);
    public bool CanCreate => _access.HasPermission(Create);
    public bool CanUpdate => _access.HasPermission(Update);
    public bool CanSubmit => _access.HasPermission(Submit);
    public bool CanApprove => _access.HasPermission(Approve);
    public bool CanCancel => _access.HasPermission(Cancel);
    public bool CanPost => _access.HasPermission(Post);
    public bool CanReverse => _access.HasPermission(Reverse);
    public bool CanReadSourceReceipts => _access.HasPermission(GoodsReceiptRead);
    public bool CanCreateReturn => CanCreate && CanReadSourceReceipts && EligibleReceipts.Count > 0;
    public bool CanSave => IsEditorOpen
        && !IsBusy
        && (_editing is null ? CanCreate && CanReadSourceReceipts : CanUpdate);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!Set(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(RefreshButtonText));
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }

    public string Message { get => _message; private set => Set(ref _message, value ?? string.Empty); }
    public bool MessageIsError { get => _messageIsError; private set => Set(ref _messageIsError, value); }
    public string RefreshButtonText => IsBusy ? "Đang cập nhật…" : "Cập nhật dữ liệu";
    public string SaveButtonText => IsBusy ? "Đang lưu…" : _editing is null ? "Tạo phiếu" : "Lưu phiếu";
    public string EditorTitle => _editing?.DocumentNumber ?? "Phiếu trả nhà cung cấp nháp";
    public string EditorModeText => _editing is null ? "TẠO PHIẾU TRẢ NHÀ CUNG CẤP" : "SỬA PHIẾU TRẢ NHÀ CUNG CẤP NHÁP";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!Set(ref _searchText, value ?? string.Empty)) return;
            ApplyFilter();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (!Set(ref _selectedStatus, value ?? "all")) return;
            ApplyFilter();
        }
    }

    public bool IsEditorOpen { get => _isEditorOpen; private set { if (Set(ref _isEditorOpen, value)) OnPropertyChanged(nameof(CanSave)); } }
    public string SelectedGoodsReceiptId { get => _selectedGoodsReceiptId; set { if (Set(ref _selectedGoodsReceiptId, value ?? string.Empty)) MarkDraftChanged(); } }
    public DateTime? ReturnDate { get => _returnDate; set { if (Set(ref _returnDate, value)) MarkDraftChanged(); } }
    public string EditorNote { get => _editorNote; set { if (Set(ref _editorNote, value ?? string.Empty)) MarkDraftChanged(); } }
    public string EditorSupplier { get => _editorSupplier; private set => Set(ref _editorSupplier, value); }
    public string EditorWarehouse { get => _editorWarehouse; private set => Set(ref _editorWarehouse, value); }

    public bool IsDetailOpen { get => _isDetailOpen; private set => Set(ref _isDetailOpen, value); }
    public string DetailTitle { get => _detailTitle; private set => Set(ref _detailTitle, value); }
    public string DetailStatus { get => _detailStatus; private set => Set(ref _detailStatus, value); }
    public string DetailSupplier { get => _detailSupplier; private set => Set(ref _detailSupplier, value); }
    public string DetailWarehouse { get => _detailWarehouse; private set => Set(ref _detailWarehouse, value); }
    public string DetailReturnDate { get => _detailReturnDate; private set => Set(ref _detailReturnDate, value); }
    public string DetailQuantity { get => _detailQuantity; private set => Set(ref _detailQuantity, value); }
    public string DetailMovement { get => _detailMovement; private set => Set(ref _detailMovement, value); }
    public string DetailLifecycleNote { get => _detailLifecycleNote; private set => Set(ref _detailLifecycleNote, value); }

    public bool IsActionOpen { get => _isActionOpen; private set => Set(ref _isActionOpen, value); }
    public string ActionTitle { get => _actionTitle; private set => Set(ref _actionTitle, value); }
    public string ActionPrompt { get => _actionPrompt; private set => Set(ref _actionPrompt, value); }
    public DateTime? ActionDate { get => _actionDate; set => Set(ref _actionDate, value); }
    public string ActionReason { get => _actionReason; set => Set(ref _actionReason, value ?? string.Empty); }
    public bool ActionDateVisible { get => _actionDateVisible; private set => Set(ref _actionDateVisible, value); }
    public bool ActionReasonVisible { get => _actionReasonVisible; private set => Set(ref _actionReasonVisible, value); }
    public bool ActionReasonRequired { get => _actionReasonRequired; private set => Set(ref _actionReasonRequired, value); }

    public string TotalCount => SupplierReturnPresentation.Number(_allReturns.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string DraftCount => SupplierReturnPresentation.Number(_allReturns.Count(x => x.Status == "draft").ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string PendingCount => SupplierReturnPresentation.Number(_allReturns.Count(x => x.Status == "pending_approval").ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string PostedCount => SupplierReturnPresentation.Number(_allReturns.Count(x => x.Status == "posted").ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string VisibleCountText => $"{Rows.Count} phiếu";

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        if (!CanRead)
        {
            SetError("Tài khoản chưa được cấp quyền xem Phiếu trả nhà cung cấp.");
            return;
        }

        IsBusy = true;
        var problems = new List<string>();
        var returnsLoaded = false;

        try
        {
            var items = await _service.ListAsync().ConfigureAwait(true);
            _allReturns.Clear();
            _allReturns.AddRange(items);
            returnsLoaded = true;
        }
        catch (Exception exception)
        {
            problems.Add($"Phiếu trả: {OfficeMessage(exception)}");
        }

        if (CanReadSourceReceipts)
        {
            try
            {
                var receipts = await _goodsReceipts.ListAsync().ConfigureAwait(true);
                _postedReceipts.Clear();
                _postedReceipts.AddRange(receipts.Where(x => x.Status == "posted"));
                RebuildEligibleReceipts();
            }
            catch (Exception exception)
            {
                problems.Add($"Phiếu nhận hàng: {OfficeMessage(exception)}");
            }
        }
        else
        {
            _postedReceipts.Clear();
            RebuildEligibleReceipts();
            if (CanCreate)
            {
                problems.Add("Cần quyền xem Phiếu nhận hàng để chọn chứng từ nguồn khi tạo phiếu trả.");
            }
        }

        ApplyFilter();
        _loaded = returnsLoaded;
        if (problems.Count == 0) SetNotice("Danh sách phiếu trả nhà cung cấp đã được cập nhật.");
        else SetError($"Một phần dữ liệu chưa cập nhật; đang giữ dữ liệu gần nhất. {string.Join(" ", problems)}");
        IsBusy = false;
    }

    public async Task BeginCreateAsync(string? preferredReceiptId = null)
    {
        if (!CanCreate)
        {
            SetError("Tài khoản chưa được cấp quyền tạo Phiếu trả nhà cung cấp.");
            return;
        }

        if (!CanReadSourceReceipts)
        {
            SetError("Tài khoản cần quyền xem Phiếu nhận hàng để chọn chứng từ nguồn khi tạo Phiếu trả nhà cung cấp.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(preferredReceiptId)
            && EligibleReceipts.All(x => !string.Equals(x.Id, preferredReceiptId, StringComparison.Ordinal)))
        {
            await RefreshAsync().ConfigureAwait(true);
        }

        SupplierReturnReceiptOption? targetReceipt;
        if (string.IsNullOrWhiteSpace(preferredReceiptId))
        {
            targetReceipt = EligibleReceipts.FirstOrDefault();
        }
        else
        {
            targetReceipt = EligibleReceipts.FirstOrDefault(
                x => string.Equals(x.Id, preferredReceiptId, StringComparison.Ordinal));
        }

        if (targetReceipt is null)
        {
            SetError(string.IsNullOrWhiteSpace(preferredReceiptId)
                ? "Chưa có Phiếu nhận hàng đã ghi sổ để tạo phiếu trả."
                : "Không tìm thấy Phiếu nhận hàng nguồn trong phạm vi truy cập hoặc phiếu không còn ở trạng thái Đã ghi sổ.");
            return;
        }

        CancelSourceLineLoad();
        _editing = null;
        _selectedGoodsReceiptId = targetReceipt.Id;
        _returnDate = DateTime.Today;
        _editorNote = string.Empty;
        EditorSupplier = "—";
        EditorWarehouse = "—";
        ClearEditorLines();
        _draftAttemptKey = _idempotencyKeys.Create("supplier-return-save");
        IsEditorOpen = true;
        RaiseEditorFields();
        Message = string.Empty;
        await LoadSelectedReceiptAsync().ConfigureAwait(true);
    }

    public async Task LoadSelectedReceiptAsync()
    {
        if (!IsEditorOpen || _editing is not null || string.IsNullOrWhiteSpace(SelectedGoodsReceiptId)) return;
        if (!CanReadSourceReceipts)
        {
            SetError("Tài khoản cần quyền xem Phiếu nhận hàng để tải chứng từ nguồn.");
            ClearEditorSource();
            return;
        }

        var requestedReceiptId = SelectedGoodsReceiptId;
        CancelSourceLineLoad();
        var requestCts = new CancellationTokenSource();
        _sourceLinesCts = requestCts;
        IsBusy = true;

        try
        {
            var receipt = _postedReceipts.FirstOrDefault(
                x => string.Equals(x.Id, requestedReceiptId, StringComparison.Ordinal));
            if (receipt is null || receipt.Status != "posted")
            {
                if (!IsCurrentSourceRequest(requestedReceiptId, requestCts)) return;
                SetError("Phiếu nhận hàng nguồn không còn ở trạng thái Đã ghi sổ.");
                ClearEditorSource();
                return;
            }

            var lines = await _service.ListSourceLinesAsync(receipt.Id, requestCts.Token).ConfigureAwait(true);
            if (!IsCurrentSourceRequest(requestedReceiptId, requestCts)) return;

            if (lines.Count == 0)
            {
                SetError("Phiếu nhận hàng này không còn dòng có thể trả nhà cung cấp.");
                ClearEditorSource();
                return;
            }

            var supplierIds = lines.Select(x => x.SourceSupplierId).Distinct(StringComparer.Ordinal).ToArray();
            var warehouseIds = lines.Select(x => x.SourceWarehouseId).Distinct(StringComparer.Ordinal).ToArray();
            if (supplierIds.Length != 1 || warehouseIds.Length != 1)
            {
                SetError("Dòng nguồn không đồng nhất nhà cung cấp hoặc kho. Không thể lập phiếu trả.");
                ClearEditorSource();
                return;
            }

            EditorSupplier = $"{lines[0].SourceSupplierCode} — {lines[0].SourceSupplierName}";
            EditorWarehouse = $"{lines[0].SourceWarehouseCode} — {lines[0].SourceWarehouseName}";
            LoadSourceLines(lines, false);
            MarkDraftChanged();
            SetNotice("Đã nạp các dòng còn có thể trả.");
        }
        catch (OperationCanceledException) when (requestCts.IsCancellationRequested)
        {
            // Lựa chọn mới đã thay thế yêu cầu này.
        }
        catch (Exception exception)
        {
            if (!IsCurrentSourceRequest(requestedReceiptId, requestCts)) return;
            SetError(OfficeMessage(exception));
            ClearEditorSource();
        }
        finally
        {
            if (ReferenceEquals(_sourceLinesCts, requestCts))
            {
                _sourceLinesCts = null;
                IsBusy = false;
            }

            requestCts.Dispose();
        }
    }

    public async Task BeginEditAsync(SupplierReturnRow row)
    {
        if (!row.CanEdit) return;
        IsBusy = true;
        try
        {
            var detail = await _service.GetAsync(row.Data.Id).ConfigureAwait(true);
            _editing = detail;
            _selectedGoodsReceiptId = detail.Lines.FirstOrDefault()?.SourceGoodsReceiptId ?? string.Empty;
            _returnDate = DateTime.TryParse(detail.ReturnDate, out var date) ? date : DateTime.Today;
            _editorNote = detail.Note ?? string.Empty;
            EditorSupplier = $"{detail.SupplierCode} — {detail.SupplierName}";
            EditorWarehouse = $"{detail.WarehouseCode} — {detail.WarehouseName}";
            _draftAttemptKey = _idempotencyKeys.Create("supplier-return-save");
            LoadSourceLines(detail.Lines, true);
            IsEditorOpen = true;
            RaiseEditorFields();
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CloseEditor()
    {
        if (IsBusy) return;
        IsEditorOpen = false;
        _editing = null;
        _draftAttemptKey = null;
        ClearEditorLines();
    }

    public async Task SaveAsync()
    {
        if (!CanSave) return;
        var validation = ValidateDraft();
        if (validation is not null)
        {
            SetError(validation);
            return;
        }

        var selected = EditorLines
            .Where(x => SupplierReturnPresentation.TryPositiveDecimal(x.ReturnQuantity, out _))
            .ToArray();

        var first = selected[0];

        var request = new SupplierReturnDraftRequest
        {
            SupplierId = first.SourceSupplierId,
            WarehouseId = first.SourceWarehouseId,
            ReturnDate = ReturnDate!.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            Note = EditorNote.Trim(),
            ExpectedRevision = _editing?.Revision,
            Lines = selected.Select(line => new SupplierReturnDraftLineRequest
            {
                SourceGoodsReceiptLineId = line.SourceGoodsReceiptLineId,
                ReturnQuantity = NormalizePositive(line.ReturnQuantity),
                ReasonCode = line.ReasonCode.Trim().ToUpperInvariant(),
                ReasonNote = line.ReasonNote.Trim(),
                Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim()
            }).ToArray()
        };

        _draftAttemptKey ??= _idempotencyKeys.Create("supplier-return-save");
        IsBusy = true;
        try
        {
            var saved = _editing is null
                ? await _service.CreateAsync(request, _draftAttemptKey).ConfigureAwait(true)
                : await _service.UpdateAsync(_editing.Id, request, _draftAttemptKey).ConfigureAwait(true);
            Upsert(saved);
            IsEditorOpen = false;
            _editing = null;
            _draftAttemptKey = null;
            ClearEditorLines();
            ApplyFilter();
            SetNotice("Phiếu trả nhà cung cấp nháp đã được lưu.");
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ShowDetailAsync(SupplierReturnRow row)
    {
        IsBusy = true;
        try
        {
            var detail = await _service.GetAsync(row.Data.Id).ConfigureAwait(true);
            DetailTitle = detail.DocumentNumber ?? "Phiếu chưa cấp số";
            DetailStatus = SupplierReturnPresentation.Status(detail.Status);
            DetailSupplier = $"{detail.SupplierCode} — {detail.SupplierName}";
            DetailWarehouse = $"{detail.WarehouseCode} — {detail.WarehouseName}";
            DetailReturnDate = SupplierReturnPresentation.Date(detail.ReturnDate);
            DetailQuantity = SupplierReturnPresentation.Number(detail.ReturnQuantityTotal);
            DetailMovement = detail.InventoryMovementId ?? "Chưa ghi sổ kho";
            DetailLifecycleNote = detail.Status switch
            {
                "cancelled" => detail.CancellationReason ?? "Đã hủy",
                "reversed" => detail.ReversalReason ?? "Đã đảo",
                _ => detail.Note ?? "Không có ghi chú"
            };

            DetailLines.Clear();
            foreach (var line in detail.Lines)
            {
                DetailLines.Add(new SupplierReturnDetailLineRow(
                    line.SourceGoodsReceiptNumber,
                    line.SourceGoodsReceiptLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    line.SourcePurchaseOrderNumber,
                    line.SourceSku,
                    line.SourceItemName,
                    line.SourceUnitCode,
                    SupplierReturnPresentation.Number(line.SourceAcceptedQuantity),
                    SupplierReturnPresentation.Number(line.PostedReturnQuantity),
                    SupplierReturnPresentation.Number(line.ReturnableQuantity),
                    SupplierReturnPresentation.Number(line.ReturnQuantity),
                    $"{line.ReasonCode} — {line.ReasonNote}",
                    LocationLot(line),
                    line.Note ?? "—"));
            }

            IsDetailOpen = true;
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CloseDetail() => IsDetailOpen = false;

    public async Task<SupplierReturnData?> GetForPrintAsync(SupplierReturnRow row)
    {
        try
        {
            return await _service.GetAsync(row.Data.Id).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
            return null;
        }
    }

    public void BeginAction(SupplierReturnRow row, string action)
    {
        if (!CanRunAction(row, action)) return;
        _actionTarget = row.Data;
        _actionKind = action;
        ActionDate = DateTime.Today;
        ActionReason = string.Empty;
        ActionDateVisible = action is "post" or "reverse";
        ActionReasonVisible = action is "cancel" or "post" or "reverse";
        ActionReasonRequired = action is "cancel" or "reverse";
        ActionTitle = action switch
        {
            "submit" => "Gửi duyệt phiếu trả",
            "approve" => "Duyệt phiếu trả",
            "cancel" => "Hủy phiếu trả",
            "post" => "Ghi sổ phiếu trả",
            "reverse" => "Đảo phiếu trả",
            _ => "Xác nhận thao tác"
        };
        ActionPrompt = action switch
        {
            "submit" => "Phiếu sẽ chuyển sang Chờ duyệt và khóa nội dung.",
            "approve" => "Phiếu sẽ chuyển sang Đã duyệt, sẵn sàng ghi sổ.",
            "cancel" => "Phiếu sẽ bị hủy và không thể tiếp tục xử lý.",
            "post" => "Hệ thống sẽ cấp số phiếu và phát sinh xuất kho trả nhà cung cấp.",
            "reverse" => "Hệ thống sẽ tạo chứng từ bù để đảo xuất kho đã ghi sổ.",
            _ => string.Empty
        };
        IsActionOpen = true;
    }

    public void CloseAction()
    {
        if (IsBusy) return;
        IsActionOpen = false;
        _actionTarget = null;
        _actionKind = string.Empty;
        ActionReason = string.Empty;
    }

    public async Task ConfirmActionAsync()
    {
        if (_actionTarget is null || string.IsNullOrWhiteSpace(_actionKind)) return;
        if (ActionReasonRequired && string.IsNullOrWhiteSpace(ActionReason))
        {
            SetError("Vui lòng nhập lý do trước khi xác nhận.");
            return;
        }

        if (ActionDateVisible && ActionDate is null)
        {
            SetError("Vui lòng chọn ngày chứng từ.");
            return;
        }

        var target = _actionTarget;
        var action = _actionKind;
        var identity = BuildActionAttemptIdentity(action, target, ActionDate, ActionReason);
        if (!_actionKeys.TryGetValue(identity, out var key))
        {
            key = _idempotencyKeys.Create($"supplier-return-{action}");
            _actionKeys[identity] = key;
        }

        IsBusy = true;
        try
        {
            SupplierReturnData updated = action switch
            {
                "submit" => await _service.SubmitAsync(target.Id, target.Revision, key).ConfigureAwait(true),
                "approve" => await _service.ApproveAsync(target.Id, target.Revision, key).ConfigureAwait(true),
                "cancel" => await _service.CancelAsync(target.Id, target.Revision, ActionReason, key).ConfigureAwait(true),
                "post" => await _service.PostAsync(target.Id, target.Revision, ActionDate!.Value, ActionReason, key).ConfigureAwait(true),
                "reverse" => await _service.ReverseAsync(target.Id, target.Revision, ActionDate!.Value, ActionReason, key).ConfigureAwait(true),
                _ => throw new InvalidOperationException("Thao tác phiếu trả không hợp lệ.")
            };

            _actionKeys.Remove(identity);
            Upsert(updated);
            ApplyFilter();
            IsActionOpen = false;
            _actionTarget = null;
            SetNotice(action switch
            {
                "submit" => "Phiếu trả đã được gửi duyệt.",
                "approve" => "Phiếu trả đã được duyệt.",
                "cancel" => "Phiếu trả đã được hủy.",
                "post" => $"Phiếu trả đã ghi sổ với số {updated.DocumentNumber}.",
                "reverse" => "Phiếu trả đã được đảo.",
                _ => "Đã hoàn tất thao tác."
            });
        }
        catch (Exception exception)
        {
            SetError(OfficeMessage(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void MarkDraftChanged()
    {
        if (!IsEditorOpen) return;
        _draftAttemptKey = _idempotencyKeys.Create("supplier-return-save");
    }

    private string? ValidateDraft()
    {
        if (string.IsNullOrWhiteSpace(SelectedGoodsReceiptId)) return "Vui lòng chọn Phiếu nhận hàng nguồn.";
        if (ReturnDate is null) return "Vui lòng chọn ngày trả.";
        var selected = EditorLines.Where(x => !string.IsNullOrWhiteSpace(x.ReturnQuantity)).ToArray();
        if (selected.Length == 0) return "Vui lòng nhập số lượng trả cho ít nhất một dòng.";

        foreach (var line in selected)
        {
            if (!SupplierReturnPresentation.TryPositiveDecimal(line.ReturnQuantity, out var quantity))
                return $"SKU {line.SourceSku}: số lượng trả phải lớn hơn 0.";
            if (!SupplierReturnPresentation.TryNonNegativeDecimal(line.ReturnableQuantity, out var returnable) || quantity > returnable)
                return $"SKU {line.SourceSku}: số lượng trả vượt quá số lượng còn có thể trả.";
            if (string.IsNullOrWhiteSpace(line.ReasonCode) || string.IsNullOrWhiteSpace(line.ReasonNote))
                return $"SKU {line.SourceSku}: bắt buộc nhập mã lý do và ghi chú lý do.";
        }

        return null;
    }

    private void LoadSourceLines(IEnumerable<SupplierReturnLineData> lines, bool editing)
    {
        ClearEditorLines();
        foreach (var line in lines.OrderBy(x => x.SourceGoodsReceiptLineNumber))
        {
            var editorLine = new SupplierReturnEditorLine
            {
                SourceGoodsReceiptLineId = line.SourceGoodsReceiptLineId,
                SourceSupplierId = line.SourceSupplierId,
                SourceWarehouseId = line.SourceWarehouseId,
                SourceGoodsReceiptLineNumber = line.SourceGoodsReceiptLineNumber,
                SourceGoodsReceiptNumber = line.SourceGoodsReceiptNumber,
                SourcePurchaseOrderNumber = line.SourcePurchaseOrderNumber,
                SourceSku = line.SourceSku,
                SourceItemName = line.SourceItemName,
                SourceUnitCode = line.SourceUnitCode,
                SourceAcceptedQuantity = line.SourceAcceptedQuantity,
                PostedReturnQuantity = line.PostedReturnQuantity ?? "0",
                ReturnableQuantity = line.ReturnableQuantity ?? line.SourceAcceptedQuantity,
                LocationLot = LocationLot(line),
                ReturnQuantity = editing ? line.ReturnQuantity : string.Empty,
                ReasonCode = editing ? line.ReasonCode : string.Empty,
                ReasonNote = editing ? line.ReasonNote : string.Empty,
                Note = editing ? line.Note ?? string.Empty : string.Empty
            };
            editorLine.Changed += EditorLineChanged;
            EditorLines.Add(editorLine);
        }
    }

    private bool CanRunAction(SupplierReturnRow row, string action) => action switch
    {
        "submit" => row.CanSubmit,
        "approve" => row.CanApprove,
        "cancel" => row.CanCancel,
        "post" => row.CanPost,
        "reverse" => row.CanReverse,
        _ => false
    };

    private void RebuildEligibleReceipts()
    {
        EligibleReceipts.Clear();
        foreach (var receipt in _postedReceipts.OrderByDescending(x => x.UpdatedAt, StringComparer.Ordinal))
        {
            EligibleReceipts.Add(new SupplierReturnReceiptOption(
                receipt.Id,
                $"{receipt.DocumentNumber ?? "Chưa cấp số"} — {receipt.SupplierName} — {receipt.WarehouseName}"));
        }

        OnPropertyChanged(nameof(CanCreateReturn));
    }

    private void ApplyFilter()
    {
        var term = SearchText.Trim();
        var visible = _allReturns
            .Where(x => SelectedStatus == "all" || x.Status == SelectedStatus)
            .Where(x =>
            {
                if (term.Length == 0) return true;
                var searchable = string.Join(" ", new[]
                {
                    x.DocumentNumber, x.SupplierCode, x.SupplierName, x.WarehouseCode, x.WarehouseName,
                    x.CancellationReason, x.ReversalReason
                }.Where(value => !string.IsNullOrWhiteSpace(value)))
                + " " + string.Join(" ", x.Lines.SelectMany(line => new[]
                {
                    line.SourceGoodsReceiptNumber, line.SourceSku, line.SourceItemName, line.ReasonCode
                }));
                return searchable.Contains(term, StringComparison.OrdinalIgnoreCase);
            })
            .OrderByDescending(x => x.UpdatedAt, StringComparer.Ordinal)
            .ToArray();

        Rows.Clear();
        for (var i = 0; i < visible.Length; i++)
        {
            var item = visible[i];
            Rows.Add(new SupplierReturnRow(
                item,
                i + 1,
                item.DocumentNumber ?? "Chưa cấp số",
                $"{item.SupplierCode} — {item.SupplierName}",
                $"{item.WarehouseCode} — {item.WarehouseName}",
                SupplierReturnPresentation.Date(item.ReturnDate),
                SupplierReturnPresentation.Status(item.Status),
                SupplierReturnPresentation.Number(item.LineCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                SupplierReturnPresentation.Number(item.ReturnQuantityTotal),
                CanRead,
                CanRead && item.Status != "draft" && !string.IsNullOrWhiteSpace(item.DocumentNumber),
                item.Status == "draft" && CanUpdate,
                item.Status == "draft" && CanSubmit,
                item.Status == "pending_approval" && CanApprove,
                (item.Status is "draft" or "pending_approval" or "approved") && CanCancel,
                item.Status == "approved" && CanPost,
                item.Status == "posted" && CanReverse));
        }

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(DraftCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(PostedCount));
        OnPropertyChanged(nameof(VisibleCountText));
    }

    private void ClearEditorSource()
    {
        EditorSupplier = "—";
        EditorWarehouse = "—";
        ClearEditorLines();
    }

    private void ClearEditorLines()
    {
        foreach (var line in EditorLines) line.Changed -= EditorLineChanged;
        EditorLines.Clear();
    }

    private void EditorLineChanged(object? sender, EventArgs e) => MarkDraftChanged();

    private void Upsert(SupplierReturnData item)
    {
        var index = _allReturns.FindIndex(x => x.Id == item.Id);
        if (index < 0) _allReturns.Insert(0, item);
        else _allReturns[index] = item;
    }

    private static string BuildActionAttemptIdentity(
        string action,
        SupplierReturnData target,
        DateTime? actionDate,
        string reason)
    {
        var normalizedReason = reason.Trim();
        return action switch
        {
            "submit" or "approve" => $"{action}|{target.Id}|{target.Revision}",
            "cancel" => $"{action}|{target.Id}|{target.Revision}|{normalizedReason}",
            "post" or "reverse" => $"{action}|{target.Id}|{target.Revision}|{actionDate:yyyy-MM-dd}|{normalizedReason}",
            _ => $"{action}|{target.Id}|{target.Revision}"
        };
    }

    private bool IsCurrentSourceRequest(string requestedReceiptId, CancellationTokenSource requestCts) =>
        ReferenceEquals(_sourceLinesCts, requestCts)
        && !requestCts.IsCancellationRequested
        && IsEditorOpen
        && _editing is null
        && string.Equals(SelectedGoodsReceiptId, requestedReceiptId, StringComparison.Ordinal);

    private void CancelSourceLineLoad()
    {
        var activeRequest = _sourceLinesCts;
        if (activeRequest is null) return;
        _sourceLinesCts = null;
        activeRequest.Cancel();
    }

    private void HandleAccessChanged()
    {
        _loaded = false;
        if (!CanReadSourceReceipts)
        {
            CancelSourceLineLoad();
            _postedReceipts.Clear();
            RebuildEligibleReceipts();
        }

        foreach (var name in new[]
        {
            nameof(CanRead), nameof(CanCreate), nameof(CanUpdate), nameof(CanSubmit),
            nameof(CanApprove), nameof(CanCancel), nameof(CanPost), nameof(CanReverse),
            nameof(CanReadSourceReceipts), nameof(CanCreateReturn), nameof(CanSave)
        })
        {
            OnPropertyChanged(name);
        }
        ApplyFilter();
    }

    private void RaiseEditorFields()
    {
        OnPropertyChanged(nameof(SelectedGoodsReceiptId));
        OnPropertyChanged(nameof(ReturnDate));
        OnPropertyChanged(nameof(EditorNote));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorModeText));
        OnPropertyChanged(nameof(CanSave));
    }

    private static string NormalizePositive(string value)
    {
        SupplierReturnPresentation.TryPositiveDecimal(value, out var number);
        return SupplierReturnPresentation.ApiDecimal(number);
    }

    private static string LocationLot(SupplierReturnLineData line)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(line.LocationId)) parts.Add($"Vị trí {line.LocationId}");
        if (!string.IsNullOrWhiteSpace(line.LotCode)) parts.Add($"Lô {line.LotCode}");
        return parts.Count == 0 ? "—" : string.Join(" · ", parts);
    }

    private void SetNotice(string message) { Message = message; MessageIsError = false; }
    private void SetError(string message) { Message = message; MessageIsError = true; }

    private static string OfficeMessage(Exception exception)
    {
        if (exception is CanonicalApiException api)
            return CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(api), api.RequestId);
        return string.IsNullOrWhiteSpace(exception.Message) ? "Không thực hiện được thao tác." : exception.Message;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
