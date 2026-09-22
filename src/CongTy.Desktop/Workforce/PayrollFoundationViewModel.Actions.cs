using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class PayrollFoundationViewModel
{
    public async Task CreatePeriodAsync()
    {
        var source = SelectedAttendanceSource?.Source;
        if (source is null || !CanCreatePeriod || IsBusy) return;

        var request = new CreatePayrollPeriodRequest
        {
            AttendancePeriodId = source.Id
        };
        var slot = MutationSlot(request);
        var key = MutationKey(slot);

        PayrollPeriodData? result = null;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            result = await _service.CreatePeriodAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tạo được kỳ lương từ bản chốt kỳ công."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (result is null) return;
        await LoadAsync(result.Id).ConfigureAwait(true);
        SetMessage("Đã tạo kỳ lương từ bản chốt kỳ công.", false);
    }

    public async Task SaveSalaryAsync()
    {
        var employee = SalaryEmployee?.Source;
        if (employee is null || SalaryEffectiveDate is null || !CanManage || IsBusy) return;
        if (!TryMoney(SalaryAmount, out var amount))
        {
            SetMessage("Mức lương phải là số tiền hợp lệ, tối đa 2 chữ số thập phân.", true);
            return;
        }

        var note = EmptyToNull(SalaryNote);
        if (note?.Length > 1000)
        {
            SetMessage("Ghi chú tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new SavePayrollSalaryRequest
        {
            EmployeeId = employee.Id,
            MonthlySalary = amount,
            EffectiveFrom = CanonicalDate(SalaryEffectiveDate.Value),
            Note = note
        };
        var slot = MutationSlot(request);
        var key = MutationKey(slot);

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.SaveSalaryAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            SalaryAmount = string.Empty;
            SalaryNote = string.Empty;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không lưu được mức lương."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await LoadAsync(SelectedPeriod?.Id).ConfigureAwait(true);
        SetMessage("Đã lưu mức lương mới theo ngày áp dụng.", false);
    }

    public async Task CreateComponentTypeAsync()
    {
        if (!CanManage || IsBusy || ComponentEffectiveDate is null) return;

        var code = ComponentCode.Trim().ToUpperInvariant();
        var name = ComponentName.Trim();
        if (!ComponentCodePattern.IsMatch(code))
        {
            SetMessage("Mã khoản phải có 2–32 ký tự: chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.", true);
            return;
        }
        if (name.Length is < 1 or > 120)
        {
            SetMessage("Tên khoản phải từ 1 đến 120 ký tự.", true);
            return;
        }
        if (ComponentCategory is null || ComponentRecurrence is null || ComponentInputMode is null)
        {
            SetMessage("Vui lòng chọn đầy đủ nhóm, cách áp dụng và cách ghi nhận khoản.", true);
            return;
        }

        var request = new CreatePayrollComponentTypeRequest
        {
            Code = code,
            Name = name,
            Category = ComponentCategory.Value,
            Recurrence = ComponentRecurrence.Value,
            InputMode = ComponentInputMode.Value,
            ProrateByWorkdays = ComponentProrate,
            IncludeInGross = ComponentIncludeGross,
            IncludeInNet = ComponentIncludeNet,
            EffectiveFrom = CanonicalDate(ComponentEffectiveDate.Value)
        };
        var slot = MutationSlot(request);
        var key = MutationKey(slot);

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.CreateComponentTypeAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            ComponentCode = string.Empty;
            ComponentName = string.Empty;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không thêm được khoản dùng cho tính lương."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await LoadAsync(SelectedPeriod?.Id).ConfigureAwait(true);
        SetMessage("Đã thêm khoản dùng cho tính lương.", false);
    }

    public async Task AssignFixedComponentAsync()
    {
        var employee = FixedEmployee?.Source;
        var component = FixedComponent?.Source;
        if (employee is null || component is null || FixedEffectiveDate is null || !CanManage || IsBusy) return;
        if (!TryMoney(FixedAmount, out var amount))
        {
            SetMessage("Số tiền phải hợp lệ và có tối đa 2 chữ số thập phân.", true);
            return;
        }

        var note = EmptyToNull(FixedNote);
        if (note?.Length > 1000)
        {
            SetMessage("Ghi chú tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new AssignPayrollFixedComponentRequest
        {
            EmployeeId = employee.Id,
            ComponentTypeId = component.Id,
            Amount = amount,
            EffectiveFrom = CanonicalDate(FixedEffectiveDate.Value),
            Note = note
        };
        var slot = MutationSlot(request);
        var key = MutationKey(slot);

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.AssignFixedComponentAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            FixedAmount = string.Empty;
            FixedNote = string.Empty;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không lưu được khoản cố định."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await LoadAsync(SelectedPeriod?.Id).ConfigureAwait(true);
        SetMessage("Đã lưu khoản cố định theo ngày áp dụng.", false);
    }

    public async Task AddPeriodComponentAsync()
    {
        var period = SelectedPeriod;
        var employee = PeriodEmployee?.Source;
        var component = PeriodComponent?.Source;
        if (period is null || employee is null || component is null || !CanManage || IsBusy) return;
        if (period.Status == "CLOSED")
        {
            SetMessage("Kỳ lương đã chốt, không thể thêm khoản phát sinh trực tiếp.", true);
            return;
        }
        if (!TryMoney(PeriodAmount, out var amount))
        {
            SetMessage("Số tiền phải hợp lệ và có tối đa 2 chữ số thập phân.", true);
            return;
        }

        var note = PeriodNote.Trim();
        if (note.Length is < 1 or > 1000)
        {
            SetMessage("Lý do / ghi chú là bắt buộc và tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new AddPayrollPeriodComponentRequest
        {
            PayrollPeriodId = period.Id,
            EmployeeId = employee.Id,
            ComponentTypeId = component.Id,
            Amount = amount,
            Note = note
        };
        var slot = MutationSlot(request);
        var key = MutationKey(slot);

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.AddPeriodComponentAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            PeriodAmount = string.Empty;
            PeriodNote = string.Empty;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không ghi được khoản phát sinh vào kỳ lương."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await LoadAsync(period.Id).ConfigureAwait(true);
        SetMessage("Đã ghi khoản phát sinh vào kỳ lương.", false);
    }
}
