using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class AttendanceAdjustmentViewModel
{
    private static readonly AttendanceAdjustmentStatusOption QuickCheckIn =
        new("CHECK_IN", "Chấm vào ngay");
    private static readonly AttendanceAdjustmentStatusOption QuickCheckOut =
        new("CHECK_OUT", "Chấm ra ngay");

    private AttendanceAdjustmentStatusOption? _selectedQuickAction = QuickCheckIn;
    private string _quickReason = string.Empty;

    public IReadOnlyList<AttendanceAdjustmentStatusOption> QuickActionOptions { get; } =
    [
        QuickCheckIn,
        QuickCheckOut
    ];

    public AttendanceAdjustmentStatusOption? SelectedQuickAction
    {
        get => _selectedQuickAction;
        set
        {
            if (!SetField(ref _selectedQuickAction, value)) return;
            OnPropertyChanged(nameof(QuickSubmitLabel));
            OnPropertyChanged(nameof(CanSubmitQuick));
        }
    }

    public string QuickReason
    {
        get => _quickReason;
        set
        {
            if (!SetField(ref _quickReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSubmitQuick));
        }
    }

    public string QuickSubmitLabel =>
        string.Equals(SelectedQuickAction?.Key, "CHECK_OUT", StringComparison.Ordinal)
            ? "CHẤM RA NGAY"
            : "CHẤM VÀO NGAY";

    public bool CanSubmitQuick =>
        ShowDirectPanel
        && !IsBusy
        && SelectedDirectEmployee is not null
        && SelectedQuickAction is not null
        && !string.IsNullOrWhiteSpace(QuickReason);

    public async Task SubmitQuickAttendanceAsync()
    {
        if (!CanSubmitQuick || SelectedDirectEmployee is null || SelectedQuickAction is null) return;

        var action = SelectedQuickAction.Key.Trim().ToUpperInvariant();
        if (action is not ("CHECK_IN" or "CHECK_OUT"))
        {
            SetMessage("Thao tác chấm công tay không hợp lệ.", true);
            return;
        }

        var reason = QuickReason.Trim();
        if (reason.Length is < 1 or > 1000)
        {
            SetMessage("Lý do chấm tay là bắt buộc và tối đa 1.000 ký tự.", true);
            return;
        }

        var payload = new DirectAttendanceAdjustmentRequest(
            SelectedDirectEmployee.Id,
            null,
            null,
            null,
            reason,
            action);
        var slot = MutationSlot("attendance-manual-now", payload);
        var key = MutationKey(slot, "desktop-attendance-manual-now");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.DirectAsync(payload, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            QuickReason = string.Empty;
            await ReloadAfterMutationAsync().ConfigureAwait(true);
            SetMessage(
                action == "CHECK_IN"
                    ? "Đã chấm vào cho nhân sự theo giờ hệ thống."
                    : "Đã chấm ra cho nhân sự theo giờ hệ thống.",
                false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không ghi nhận được chấm công tay."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetManualAttendanceState()
    {
        _selectedQuickAction = QuickActionOptions[0];
        _quickReason = string.Empty;
        OnPropertyChanged(nameof(SelectedQuickAction));
        OnPropertyChanged(nameof(QuickReason));
        OnPropertyChanged(nameof(QuickSubmitLabel));
        OnPropertyChanged(nameof(CanSubmitQuick));
    }
}
