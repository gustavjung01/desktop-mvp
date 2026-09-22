namespace CongTy.Desktop.Workforce;

using CongTy.Contracts;

public sealed partial class OvertimeCloseoutViewModel
{
    public async Task SubmitOvertimeAsync()
    {
        if (!CanSubmitOwn || IsBusy) return;
        if (RequestWorkDate is null)
        {
            SetMessage("Vui lòng chọn ngày tăng ca.", true);
            return;
        }
        if (!TryHours(RequestHours, allowZero: false, out var requestedMinutes))
        {
            SetMessage("Số giờ đăng ký phải lớn hơn 0, không quá 24 giờ và quy đổi được thành số phút nguyên.", true);
            return;
        }

        var reason = RequestReason.Trim();
        if (reason.Length is 0 or > 1000)
        {
            SetMessage("Lý do tăng ca là bắt buộc và tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new SubmitOvertimeRequest
        {
            WorkDate = CanonicalDate(RequestWorkDate.Value),
            RequestedMinutes = requestedMinutes,
            Reason = reason
        };
        var slot = MutationSlot("overtime-submit", request);
        var key = MutationKey(slot, "overtime-submit");

        OvertimeRequestData? result = null;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            result = await _service.SubmitOvertimeAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            RequestReason = string.Empty;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không gửi được đăng ký tăng ca."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (result is null) return;
        await ReloadAsync().ConfigureAwait(true);
        SetMessage(
            result.Status == "APPROVED"
                ? "Đăng ký tăng ca đã được ghi nhận và tự động duyệt theo chính sách hiệu lực."
                : "Đăng ký tăng ca đã được ghi nhận.",
            false);
    }

    public void OpenOvertime(OvertimeRowView row)
    {
        if (!row.CanProcess || IsBusy) return;
        SelectedOvertime = row;
        ActionNote = string.Empty;
        var minutes = row.Source.ActualMinutes ?? row.Source.RequestedMinutes;
        ActionHours = (minutes / 60m).ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        SetMessage(string.Empty, false);
    }

    public void CloseOvertime()
    {
        SelectedOvertime = null;
        ActionNote = string.Empty;
        ActionHours = string.Empty;
    }

    public async Task ReviewOvertimeAsync(string action)
    {
        var selected = SelectedOvertime;
        var normalizedAction = action.Trim().ToUpperInvariant();
        if (selected is null || !CanReviewSelected || IsBusy) return;
        if (normalizedAction is not ("APPROVE" or "REJECT")) return;

        var note = EmptyToNull(ActionNote);
        if (normalizedAction == "REJECT" && note is null)
        {
            SetMessage("Vui lòng nhập lý do từ chối.", true);
            return;
        }
        if (note?.Length > 1000)
        {
            SetMessage("Ý kiến xử lý tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new ReviewOvertimeRequest
        {
            RequestId = selected.Source.Id,
            ExpectedVersion = selected.Source.Version,
            Action = normalizedAction,
            ReviewReason = note
        };
        var slot = MutationSlot("overtime-review", request);
        var key = MutationKey(slot, "overtime-review");

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.ReviewOvertimeAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            CloseOvertime();
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không xử lý được tăng ca."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await ReloadAsync().ConfigureAwait(true);
        SetMessage(normalizedAction == "APPROVE"
            ? "Đăng ký tăng ca đã được duyệt."
            : "Đăng ký tăng ca đã bị từ chối.", false);
    }

    public async Task RecordActualAsync()
    {
        var selected = SelectedOvertime;
        if (selected is null || !CanRecordActualSelected || IsBusy) return;
        if (!TryHours(ActionHours, allowZero: false, out var actualMinutes))
        {
            SetMessage("Số giờ thực tế phải lớn hơn 0, không quá 24 giờ và quy đổi được thành số phút nguyên.", true);
            return;
        }
        var note = EmptyToNull(ActionNote);
        if (note?.Length > 1000)
        {
            SetMessage("Ghi chú thực tế tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new RecordOvertimeActualRequest
        {
            RequestId = selected.Source.Id,
            ExpectedVersion = selected.Source.Version,
            ActualMinutes = actualMinutes,
            ActualNote = note
        };
        var slot = MutationSlot("overtime-actual", request);
        var key = MutationKey(slot, "overtime-actual");

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.RecordOvertimeActualAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            CloseOvertime();
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không ghi nhận được thời gian tăng ca thực tế."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await ReloadAsync().ConfigureAwait(true);
        SetMessage("Thời gian tăng ca thực tế đã được ghi nhận.", false);
    }

    public async Task ConfirmHoursAsync()
    {
        var selected = SelectedOvertime;
        if (selected is null || !CanConfirmSelected || IsBusy) return;
        if (!TryHours(ActionHours, allowZero: true, out var confirmedMinutes))
        {
            SetMessage("Số giờ được tính phải từ 0 đến 24 giờ và quy đổi được thành số phút nguyên.", true);
            return;
        }
        if (selected.Source.ActualMinutes is not null && confirmedMinutes > selected.Source.ActualMinutes.Value)
        {
            SetMessage("Giờ tăng ca được tính không được lớn hơn thời gian thực tế.", true);
            return;
        }
        var note = EmptyToNull(ActionNote);
        if (note?.Length > 1000)
        {
            SetMessage("Ghi chú xác nhận tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new ConfirmOvertimeRequest
        {
            RequestId = selected.Source.Id,
            ExpectedVersion = selected.Source.Version,
            ConfirmedMinutes = confirmedMinutes,
            ConfirmNote = note
        };
        var slot = MutationSlot("overtime-confirm", request);
        var key = MutationKey(slot, "overtime-confirm");

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.ConfirmOvertimeAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            CloseOvertime();
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không xác nhận được giờ tăng ca."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await ReloadAsync().ConfigureAwait(true);
        SetMessage("Giờ tăng ca được tính đã được xác nhận.", false);
    }

    public void OpenPeriod(AttendancePeriodRowView row)
    {
        if (IsBusy) return;
        SelectedPeriod = row;
        PeriodNote = string.Empty;
        AcknowledgeWarnings = false;
        _payrollInput = null;
        PayrollRows.Clear();
        PayrollTitle = "Chọn kỳ đã chốt để xem đầu vào tính lương.";
        OnPropertyChanged(nameof(HasPayrollInput));
        RaiseEmptyStates();
        SetMessage(string.Empty, false);
    }

    public void ClosePeriod()
    {
        SelectedPeriod = null;
        PeriodNote = string.Empty;
        AcknowledgeWarnings = false;
        _payrollInput = null;
        PayrollRows.Clear();
        PayrollTitle = "Chọn kỳ đã chốt để xem đầu vào tính lương.";
        OnPropertyChanged(nameof(HasPayrollInput));
        RaiseEmptyStates();
    }

    public async Task RefreshSelectedRangeAsync()
    {
        if (!CanReconcile || IsBusy) return;
        if (!TryDateRange(out var from, out var to, out var validation))
        {
            SetMessage(validation, true);
            return;
        }

        var request = new AttendancePeriodMutationRequest
        {
            Action = "REFRESH",
            PeriodStart = CanonicalDate(from),
            PeriodEnd = CanonicalDate(to),
            BranchId = EmptyToNull(SelectedBranch?.Value),
            Note = EmptyToNull(PeriodNote),
            AcknowledgeWarnings = AcknowledgeWarnings
        };
        await MutatePeriodAsync(request, "Dữ liệu kỳ công đã được tổng hợp lại.").ConfigureAwait(true);
    }

    public async Task RefreshSelectedPeriodAsync()
    {
        var row = SelectedPeriod;
        if (row is null || !CanReconcile || IsBusy) return;
        await MutatePeriodAsync(PeriodRequest(row.Source, "REFRESH"), "Dữ liệu kỳ công đã được tổng hợp lại.").ConfigureAwait(true);
    }

    public async Task ReconcileSelectedPeriodAsync()
    {
        var row = SelectedPeriod;
        if (row is null || !CanReconcileSelected || IsBusy) return;

        var warnings = OvertimeCloseoutPresentation.WarningTotal(row.Source.IssueSummary);
        if (warnings > 0 && !AcknowledgeWarnings)
        {
            SetMessage("Kỳ công còn cảnh báo; cần xác nhận đã kiểm tra trước khi đối soát.", true);
            return;
        }
        if (warnings > 0 && string.IsNullOrWhiteSpace(PeriodNote))
        {
            SetMessage("Vui lòng ghi chú kết quả kiểm tra cảnh báo.", true);
            return;
        }

        await MutatePeriodAsync(PeriodRequest(row.Source, "RECONCILE"), "Kỳ công đã được đối soát.").ConfigureAwait(true);
    }

    public async Task CloseSelectedPeriodAsync()
    {
        var row = SelectedPeriod;
        if (row is null || !CanCloseSelected || IsBusy) return;
        await MutatePeriodAsync(PeriodRequest(row.Source, "CLOSE"), "Kỳ công đã được chốt.").ConfigureAwait(true);
    }

    private AttendancePeriodMutationRequest PeriodRequest(AttendancePeriodData period, string action) =>
        new()
        {
            Action = action,
            PeriodStart = period.PeriodStart,
            PeriodEnd = period.PeriodEnd,
            BranchId = period.BranchId,
            Note = EmptyToNull(PeriodNote),
            AcknowledgeWarnings = AcknowledgeWarnings
        };

    private async Task MutatePeriodAsync(AttendancePeriodMutationRequest request, string successMessage)
    {
        var note = request.Note;
        if (note?.Length > 1000)
        {
            SetMessage("Ghi chú đối soát tối đa 1.000 ký tự.", true);
            return;
        }

        var slot = MutationSlot("attendance-period-action", request);
        var key = MutationKey(slot, "attendance-period-action");

        AttendancePeriodMutationResponseData? result = null;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            result = await _service.MutateAttendancePeriodAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không cập nhật được kỳ công."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (result is null) return;
        await ReloadAsync().ConfigureAwait(true);
        SelectedPeriod = PeriodRows.FirstOrDefault(item => item.Source.Id == result.Period.Id)
            ?? new AttendancePeriodRowView(
                result.Period,
                OvertimeCloseoutPresentation.PeriodText(result.Period),
                OvertimeCloseoutPresentation.ScopeText(result.Period),
                OvertimeCloseoutPresentation.PeriodStatusLabel(result.Period.Status),
                OvertimeCloseoutPresentation.DecimalText(OvertimeCloseoutPresentation.BlockerTotal(result.Issues)),
                OvertimeCloseoutPresentation.DecimalText(OvertimeCloseoutPresentation.WarningTotal(result.Issues)),
                result.Period.Revision > 0 ? result.Period.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture) : "—",
                result.Period.Status == "CLOSED");
        SetMessage(successMessage, false);
    }

    public async Task ViewPayrollAsync(AttendancePeriodRowView? row = null)
    {
        var target = row ?? SelectedPeriod;
        if (target is null || target.Source.Status != "CLOSED" || !HasPeriodReadAccess || IsBusy) return;

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var input = await _service.GetAttendancePayrollInputAsync(target.Source.Id).ConfigureAwait(true);
            SelectedPeriod = target;
            ApplyPayroll(input);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tải được đầu vào tính lương."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
