using System.Collections.ObjectModel;
using System.IO;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class LeaveViewModel
{
    private const long MaxManualAttachmentBytes = 10 * 1024 * 1024;

    private LeaveOption? _selectedManualEmployee;
    private LeaveOption? _selectedManualLeaveType;
    private DateTime? _manualFromDate;
    private DateTime? _manualToDate;
    private LeaveOption? _selectedManualDayPart;
    private string _manualReason = string.Empty;
    private bool _paperApproved;
    private string _manualApproverName = string.Empty;
    private DateTime? _manualApprovedDate;
    private string? _manualAttachmentPath;
    private string _manualAttachmentName = string.Empty;
    private LeaveAttachmentUploadData? _manualUploadedAttachment;

    public ObservableCollection<LeaveOption> ManualEmployeeOptions { get; } = [];

    public LeaveOption? SelectedManualEmployee
    {
        get => _selectedManualEmployee;
        set
        {
            if (!SetField(ref _selectedManualEmployee, value)) return;
            OnPropertyChanged(nameof(CanSubmitManualRequest));
        }
    }

    public LeaveOption? SelectedManualLeaveType
    {
        get => _selectedManualLeaveType;
        set
        {
            if (!SetField(ref _selectedManualLeaveType, value)) return;
            OnPropertyChanged(nameof(ManualTypeHint));
            OnPropertyChanged(nameof(CanSubmitManualRequest));
        }
    }

    public DateTime? ManualFromDate
    {
        get => _manualFromDate;
        set
        {
            if (!SetField(ref _manualFromDate, value)) return;
            if (!string.Equals(SelectedManualDayPart?.Value, "FULL_DAY", StringComparison.Ordinal) && value is not null)
                ManualToDate = value;
            OnPropertyChanged(nameof(CanSubmitManualRequest));
        }
    }

    public DateTime? ManualToDate
    {
        get => _manualToDate;
        set
        {
            if (!SetField(ref _manualToDate, value)) return;
            OnPropertyChanged(nameof(CanSubmitManualRequest));
        }
    }

    public LeaveOption? SelectedManualDayPart
    {
        get => _selectedManualDayPart;
        set
        {
            if (!SetField(ref _selectedManualDayPart, value)) return;
            if (!string.Equals(value?.Value, "FULL_DAY", StringComparison.Ordinal) && ManualFromDate is not null)
                ManualToDate = ManualFromDate;
            OnPropertyChanged(nameof(CanSubmitManualRequest));
        }
    }

    public string ManualReason
    {
        get => _manualReason;
        set
        {
            if (!SetField(ref _manualReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitManualRequest));
        }
    }

    public bool PaperApproved
    {
        get => _paperApproved;
        set
        {
            if (!SetField(ref _paperApproved, value)) return;
            OnPropertyChanged(nameof(CanSubmitManualRequest));
        }
    }

    public string ManualApproverName
    {
        get => _manualApproverName;
        set
        {
            if (!SetField(ref _manualApproverName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitManualRequest));
        }
    }

    public DateTime? ManualApprovedDate
    {
        get => _manualApprovedDate;
        set
        {
            if (!SetField(ref _manualApprovedDate, value)) return;
            OnPropertyChanged(nameof(CanSubmitManualRequest));
        }
    }

    public string ManualAttachmentDisplay =>
        _manualUploadedAttachment is not null
            ? $"Đã tải: {_manualUploadedAttachment.FileName}"
            : string.IsNullOrWhiteSpace(_manualAttachmentName)
                ? "Chưa chọn chứng từ"
                : _manualAttachmentName;

    public bool HasManualAttachment =>
        _manualUploadedAttachment is not null || !string.IsNullOrWhiteSpace(_manualAttachmentPath);

    public string ManualTypeHint
    {
        get
        {
            var type = SelectedManualLeaveTypeData();
            if (type is null) return "Chọn chế độ nghỉ để xem quy tắc áp dụng.";
            return LeavePresentation.LeaveTypeBadges(type);
        }
    }

    public bool CanSubmitManualRequest
    {
        get
        {
            var type = SelectedManualLeaveTypeData();
            if (!CanSubmitManual || IsBusy || SelectedManualEmployee is null || type is null) return false;
            if (ManualFromDate is null || ManualToDate is null || SelectedManualDayPart is null) return false;
            if (string.IsNullOrWhiteSpace(ManualReason)) return false;
            if (type.RequiresAttachment && !HasManualAttachment) return false;
            if (PaperApproved && (string.IsNullOrWhiteSpace(ManualApproverName) || ManualApprovedDate is null)) return false;
            return true;
        }
    }

    public void SelectManualAttachment(string path)
    {
        var normalized = path?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || !File.Exists(normalized))
        {
            SetMessage("Không tìm thấy chứng từ đã chọn.", true);
            return;
        }

        var info = new FileInfo(normalized);
        var mimeType = MimeTypeFor(info.Extension);
        if (mimeType is null)
        {
            SetMessage("Chứng từ chỉ nhận JPG, PNG, WebP hoặc PDF.", true);
            return;
        }
        if (info.Name.Length > 600)
        {
            SetMessage("Tên chứng từ nghỉ quá dài.", true);
            return;
        }
        if (info.Length < 1 || info.Length > MaxManualAttachmentBytes)
        {
            SetMessage("Dung lượng chứng từ nghỉ phải lớn hơn 0 và không vượt quá 10 MB.", true);
            return;
        }

        _manualAttachmentPath = info.FullName;
        _manualAttachmentName = $"{info.Name} · {FormatBytes(info.Length)}";
        _manualUploadedAttachment = null;
        OnPropertyChanged(nameof(ManualAttachmentDisplay));
        OnPropertyChanged(nameof(HasManualAttachment));
        OnPropertyChanged(nameof(CanSubmitManualRequest));
        SetMessage(string.Empty, false);
    }

    public void ClearManualAttachment()
    {
        _manualAttachmentPath = null;
        _manualAttachmentName = string.Empty;
        _manualUploadedAttachment = null;
        OnPropertyChanged(nameof(ManualAttachmentDisplay));
        OnPropertyChanged(nameof(HasManualAttachment));
        OnPropertyChanged(nameof(CanSubmitManualRequest));
    }

    public async Task SubmitManualLeaveAsync()
    {
        if (!CanSubmitManual || IsBusy || SelectedManualEmployee is null)
            return;

        var type = SelectedManualLeaveTypeData();
        if (type is null)
        {
            SetMessage("Vui lòng chọn chế độ nghỉ.", true);
            return;
        }
        if (ManualFromDate is null || ManualToDate is null || ManualToDate.Value.Date < ManualFromDate.Value.Date)
        {
            SetMessage("Khoảng ngày nghỉ không hợp lệ.", true);
            return;
        }
        if ((ManualToDate.Value.Date - ManualFromDate.Value.Date).TotalDays > 365)
        {
            SetMessage("Một đơn nghỉ không được vượt quá 366 ngày.", true);
            return;
        }

        var dayPart = SelectedManualDayPart?.Value ?? string.Empty;
        if (!string.Equals(dayPart, "FULL_DAY", StringComparison.Ordinal)
            && ManualFromDate.Value.Date != ManualToDate.Value.Date)
        {
            SetMessage("Nghỉ nửa ngày chỉ áp dụng cho một ngày.", true);
            return;
        }
        if (string.Equals(dayPart, "FULL_DAY", StringComparison.Ordinal) && !type.AllowsFullDay)
        {
            SetMessage("Chế độ nghỉ này không cho phép nghỉ cả ngày.", true);
            return;
        }
        if (!string.Equals(dayPart, "FULL_DAY", StringComparison.Ordinal) && !type.AllowsHalfDay)
        {
            SetMessage("Chế độ nghỉ này không cho phép nghỉ nửa ngày.", true);
            return;
        }

        var reason = ManualReason.Trim();
        if (reason.Length is < 1 or > 1000)
        {
            SetMessage("Lý do nghỉ là bắt buộc và tối đa 1.000 ký tự.", true);
            return;
        }

        var approver = ManualApproverName.Trim();
        if (PaperApproved && (approver.Length is < 1 or > 150))
        {
            SetMessage("Vui lòng nhập người đã duyệt phiếu giấy, tối đa 150 ký tự.", true);
            return;
        }
        if (PaperApproved
            && (ManualApprovedDate is null || ManualApprovedDate.Value.Date > WorkSchedulePresentation.BusinessToday()))
        {
            SetMessage("Ngày duyệt phiếu giấy không hợp lệ.", true);
            return;
        }

        IsBusy = true;
        SetMessage(string.Empty, false);
        LeaveRequestData? result = null;
        try
        {
            var attachmentReference = await EnsureManualAttachmentUploadedAsync().ConfigureAwait(true);
            if (type.RequiresAttachment && string.IsNullOrWhiteSpace(attachmentReference))
            {
                SetMessage("Chế độ nghỉ này yêu cầu chứng từ.", true);
                return;
            }

            var request = new SubmitManualLeaveRequest
            {
                EmployeeId = SelectedManualEmployee.Value,
                LeaveTypeId = type.Id,
                DateFrom = CanonicalDate(ManualFromDate.Value),
                DateTo = CanonicalDate(ManualToDate.Value),
                DayPart = dayPart,
                Reason = reason,
                AttachmentReference = attachmentReference,
                PaperApproved = PaperApproved,
                ManualApproverName = PaperApproved ? approver : null,
                ManualApprovedDate = PaperApproved && ManualApprovedDate is not null
                    ? CanonicalDate(ManualApprovedDate.Value)
                    : null
            };
            var slot = MutationSlot("leave-request-manual", request);
            var key = MutationKey(slot, "desktop-leave-request-manual");
            result = await _service.SubmitManualLeaveRequestAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);

            ManualReason = string.Empty;
            PaperApproved = false;
            ManualApproverName = string.Empty;
            ClearManualAttachment();
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không ghi nhận được phiếu nghỉ giấy."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (result is null) return;
        await ReloadAsync(0).ConfigureAwait(true);
        SetMessage(
            string.Equals(result.Status, "APPROVED", StringComparison.Ordinal)
                ? "Phiếu nghỉ giấy đã được ghi nhận và đưa vào dữ liệu công."
                : "Phiếu nghỉ giấy đã được ghi nhận để duyệt.",
            false);
    }

    private async Task<string?> EnsureManualAttachmentUploadedAsync()
    {
        if (_manualUploadedAttachment is not null)
            return _manualUploadedAttachment.ObjectKey;
        if (string.IsNullOrWhiteSpace(_manualAttachmentPath))
            return null;

        var info = new FileInfo(_manualAttachmentPath);
        if (!info.Exists)
            throw new InvalidOperationException("Chứng từ đã chọn không còn tồn tại.");

        var mimeType = MimeTypeFor(info.Extension)
            ?? throw new InvalidOperationException("Chứng từ chỉ nhận JPG, PNG, WebP hoặc PDF.");
        if (info.Length < 1 || info.Length > MaxManualAttachmentBytes)
            throw new InvalidOperationException("Dung lượng chứng từ nghỉ phải lớn hơn 0 và không vượt quá 10 MB.");

        var fingerprint = new
        {
            info.Name,
            info.Length,
            MimeType = mimeType,
            LastWriteUtc = info.LastWriteTimeUtc.Ticks
        };
        var slot = MutationSlot("leave-document-upload", fingerprint);
        var key = MutationKey(slot, "desktop-leave-document-upload");
        var bytes = await File.ReadAllBytesAsync(info.FullName).ConfigureAwait(true);
        var uploaded = await _service.UploadLeaveAttachmentAsync(
            bytes,
            info.Name,
            mimeType,
            key).ConfigureAwait(true);
        _mutationKeys.Remove(slot);
        _manualUploadedAttachment = uploaded;
        OnPropertyChanged(nameof(ManualAttachmentDisplay));
        OnPropertyChanged(nameof(HasManualAttachment));
        return uploaded.ObjectKey;
    }

    private void ApplyManualEmployees(IEnumerable<LeaveEmployeeData> employees)
    {
        var selectedId = SelectedManualEmployee?.Value;
        ManualEmployeeOptions.Clear();
        foreach (var employee in employees.OrderBy(item => item.Code, StringComparer.CurrentCultureIgnoreCase))
        {
            var label = $"{employee.Code} · {employee.Name}";
            if (!string.IsNullOrWhiteSpace(employee.BranchName))
                label += $" · {employee.BranchName}";
            ManualEmployeeOptions.Add(new LeaveOption(employee.Id, label));
        }
        SelectedManualEmployee =
            ManualEmployeeOptions.FirstOrDefault(item => item.Value == selectedId)
            ?? ManualEmployeeOptions.FirstOrDefault();
    }

    private LeaveTypeData? SelectedManualLeaveTypeData() =>
        _leaveTypes.FirstOrDefault(item => item.Id == SelectedManualLeaveType?.Value);

    private void InitializeManualLeaveDefaults(DateTime today)
    {
        _manualFromDate = today;
        _manualToDate = today;
        _manualApprovedDate = today;
        _selectedManualDayPart = DayPartOptions[0];
    }

    private void ResetManualLeaveState()
    {
        _selectedManualEmployee = null;
        _selectedManualLeaveType = null;
        _manualReason = string.Empty;
        _paperApproved = false;
        _manualApproverName = string.Empty;
        _manualAttachmentPath = null;
        _manualAttachmentName = string.Empty;
        _manualUploadedAttachment = null;
        OnPropertyChanged(nameof(SelectedManualEmployee));
        OnPropertyChanged(nameof(SelectedManualLeaveType));
        OnPropertyChanged(nameof(ManualReason));
        OnPropertyChanged(nameof(PaperApproved));
        OnPropertyChanged(nameof(ManualApproverName));
        OnPropertyChanged(nameof(ManualAttachmentDisplay));
        OnPropertyChanged(nameof(HasManualAttachment));
        OnPropertyChanged(nameof(CanSubmitManualRequest));
    }

    private static string? MimeTypeFor(string? extension) =>
        extension?.Trim().ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => null
        };

    private static string FormatBytes(long bytes) =>
        bytes >= 1024 * 1024
            ? $"{bytes / (1024d * 1024d):0.##} MB"
            : $"{Math.Max(1d, bytes / 1024d):0.#} KB";
}
