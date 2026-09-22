using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class PayrollFoundationViewModel
{
    public async Task ClosePayrollAsync()
    {
        var period = SelectedPeriod;
        if (period is null || !CanClosePayroll || IsBusy) return;

        var request = new ClosePayrollRequest
        {
            PayrollPeriodId = period.Id
        };
        var slot = MutationSlot(request);
        var key = MutationKey(slot);

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.CloseAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không chốt được kỳ lương."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await LoadAsync(period.Id).ConfigureAwait(true);
        SetMessage("Đã chốt kỳ lương và tạo phiếu lương bất biến.", false);
    }

    public async Task RecordPayrollAdjustmentAsync()
    {
        var period = SelectedPeriod;
        var payslip = SelectedPayslipRow?.Source;
        var component = AdjustmentComponent?.Source;
        var direction = AdjustmentDirection?.Value;
        if (period is null
            || payslip is null
            || component is null
            || string.IsNullOrWhiteSpace(direction)
            || !CanAdjustPayroll
            || IsBusy)
            return;

        if (!TryMoney(AdjustmentAmount, out var amount) || !IsPositiveMoney(amount))
        {
            SetMessage("Số tiền điều chỉnh phải lớn hơn 0 và có tối đa 2 chữ số thập phân.", true);
            return;
        }

        var reason = AdjustmentReason.Trim();
        if (reason.Length is < 1 or > 1000)
        {
            SetMessage("Lý do điều chỉnh là bắt buộc và tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new AdjustPayrollRequest
        {
            PayrollPeriodId = period.Id,
            EmployeeId = payslip.EmployeeId,
            ComponentTypeId = component.Id,
            Direction = direction,
            Amount = amount,
            Reason = reason
        };
        var slot = MutationSlot(request);
        var key = MutationKey(slot);

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.AdjustAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không ghi được điều chỉnh lương sau chốt."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        AdjustmentAmount = string.Empty;
        AdjustmentReason = string.Empty;
        await LoadAsync(period.Id).ConfigureAwait(true);
        SetMessage("Đã ghi điều chỉnh lương và tạo phiên bản phiếu lương mới.", false);
    }
}
