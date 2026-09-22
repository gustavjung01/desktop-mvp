using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class PayrollFoundationViewModel
{
    public async Task AggregatePayrollAsync()
    {
        var period = SelectedPeriod;
        if (period is null || !CanAggregate || IsBusy) return;

        var request = new AggregatePayrollRequest { PayrollPeriodId = period.Id };
        var slot = MutationSlot(request);
        var key = MutationKey(slot);

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.AggregateAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tổng hợp được bảng lương từ dữ liệu kỳ hiện tại."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await LoadAsync(period.Id).ConfigureAwait(true);
        SetMessage("Đã tổng hợp lại bảng lương từ dữ liệu hiện hành của kỳ.", false);
    }

    public async Task ReconcilePayrollAsync()
    {
        var period = SelectedPeriod;
        if (period is null || Calculation is null || !CanShowReconcileForm || IsBusy) return;
        if (HasBlockers)
        {
            SetMessage("Kỳ lương còn dữ liệu bắt buộc phải xử lý trước khi đối soát.", true);
            return;
        }

        var note = ReconcileNote.Trim();
        if (HasWarnings && !ReconcileWarningsAcknowledged)
        {
            SetMessage("Vui lòng xác nhận đã kiểm tra các cảnh báo trước khi đối soát.", true);
            return;
        }
        if (HasWarnings && note.Length == 0)
        {
            SetMessage("Vui lòng ghi chú kết quả kiểm tra cảnh báo.", true);
            return;
        }
        if (note.Length > 1000)
        {
            SetMessage("Ghi chú đối soát tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new ReconcilePayrollRequest
        {
            PayrollPeriodId = period.Id,
            AcknowledgeWarnings = ReconcileWarningsAcknowledged,
            Note = note
        };
        var slot = MutationSlot(request);
        var key = MutationKey(slot);

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.ReconcileAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không xác nhận được kết quả đối soát kỳ lương."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await LoadAsync(period.Id).ConfigureAwait(true);
        SetMessage("Đã xác nhận đối soát kỳ lương trên bản tổng hợp hiện tại.", false);
    }
}
