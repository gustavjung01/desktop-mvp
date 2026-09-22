using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class LeaveViewModel
{
    public async Task SubmitRequestAsync()
    {
        if (!CanSubmitOwn || IsBusy) return;
        var type = SelectedRequestTypeData();
        if (type is null)
        {
            SetMessage("Vui lòng chọn chế độ nghỉ.", true);
            return;
        }
        if (RequestFromDate is null || RequestToDate is null || RequestToDate.Value.Date < RequestFromDate.Value.Date)
        {
            SetMessage("Khoảng ngày nghỉ không hợp lệ.", true);
            return;
        }
        if ((RequestToDate.Value.Date - RequestFromDate.Value.Date).TotalDays > 365)
        {
            SetMessage("Một đơn nghỉ không được vượt quá 366 ngày.", true);
            return;
        }

        var dayPart = SelectedDayPart?.Value ?? string.Empty;
        if (!string.Equals(dayPart, "FULL_DAY", StringComparison.Ordinal)
            && RequestFromDate.Value.Date != RequestToDate.Value.Date)
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

        var reason = RequestReason.Trim();
        if (reason.Length is 0 or > 1000)
        {
            SetMessage("Lý do nghỉ là bắt buộc và tối đa 1.000 ký tự.", true);
            return;
        }
        var attachment = EmptyToNull(AttachmentReference);
        if (type.RequiresAttachment && attachment is null)
        {
            SetMessage("Chế độ nghỉ này yêu cầu thông tin chứng từ.", true);
            return;
        }
        if (attachment?.Length > 1000)
        {
            SetMessage("Thông tin chứng từ tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new SubmitLeaveRequestRequest
        {
            LeaveTypeId = type.Id,
            DateFrom = CanonicalDate(RequestFromDate.Value),
            DateTo = CanonicalDate(RequestToDate.Value),
            DayPart = dayPart,
            Reason = reason,
            AttachmentReference = attachment
        };
        var slot = MutationSlot("leave-request-submit", request);
        var key = MutationKey(slot, "leave-request-submit");

        LeaveRequestData? result = null;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            result = await _service.SubmitLeaveRequestAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            RequestReason = string.Empty;
            AttachmentReference = string.Empty;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không gửi được đơn nghỉ."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (result is null) return;
        await ReloadAsync(0).ConfigureAwait(true);
        SetMessage(
            string.Equals(result.Status, "APPROVED", StringComparison.Ordinal)
                ? "Đơn nghỉ đã được ghi nhận và tự động duyệt theo chế độ nghỉ."
                : "Đơn nghỉ đã được gửi để duyệt.",
            false);
    }

    public void OpenReview(LeaveRequestRowView row)
    {
        if (!row.CanReview || IsBusy) return;
        ReviewTarget = row;
        ReviewReason = string.Empty;
        CancelTarget = null;
    }

    public void CloseReview()
    {
        ReviewTarget = null;
        ReviewReason = string.Empty;
    }

    public async Task ReviewAsync(string action)
    {
        var target = ReviewTarget;
        var normalizedAction = action.Trim().ToUpperInvariant();
        if (target is null || !target.CanReview || IsBusy) return;
        if (normalizedAction is not ("APPROVE" or "REJECT"))
        {
            SetMessage("Thao tác xử lý đơn nghỉ không hợp lệ.", true);
            return;
        }

        var reviewReason = EmptyToNull(ReviewReason);
        if (normalizedAction == "REJECT" && reviewReason is null)
        {
            SetMessage("Vui lòng nhập ý kiến khi từ chối đơn nghỉ.", true);
            return;
        }
        if (reviewReason?.Length > 1000)
        {
            SetMessage("Ý kiến xử lý tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new ReviewLeaveRequestRequest
        {
            RequestId = target.Source.Id,
            Action = normalizedAction,
            ExpectedVersion = target.Source.Version,
            ReviewReason = reviewReason
        };
        var slot = MutationSlot("leave-request-review", request);
        var key = MutationKey(slot, "leave-request-review");

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.ReviewLeaveRequestAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            CloseReview();
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không xử lý được đơn nghỉ."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await ReloadAsync(_pagination.Offset).ConfigureAwait(true);
        SetMessage(normalizedAction == "APPROVE" ? "Đơn nghỉ đã được duyệt." : "Đơn nghỉ đã được từ chối.", false);
    }

    public void OpenCancel(LeaveRequestRowView row)
    {
        if (!row.CanCancel || IsBusy) return;
        CancelTarget = row;
        CancelReason = string.Empty;
        ReviewTarget = null;
    }

    public void CloseCancel()
    {
        CancelTarget = null;
        CancelReason = string.Empty;
    }

    public async Task CancelRequestAsync()
    {
        var target = CancelTarget;
        if (target is null || !target.CanCancel || IsBusy) return;
        var reason = CancelReason.Trim();
        if (reason.Length is 0 or > 1000)
        {
            SetMessage("Lý do hủy là bắt buộc và tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new CancelLeaveRequestRequest
        {
            RequestId = target.Source.Id,
            ExpectedVersion = target.Source.Version,
            CancelReason = reason
        };
        var slot = MutationSlot("leave-request-cancel", request);
        var key = MutationKey(slot, "leave-request-cancel");

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.CancelLeaveRequestAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            CloseCancel();
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không hủy được đơn nghỉ."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await ReloadAsync(_pagination.Offset).ConfigureAwait(true);
        SetMessage("Đơn nghỉ đã được hủy.", false);
    }

    public async Task ViewBalanceAsync(LeaveBalanceRowView row)
    {
        if (IsBusy) return;
        SelectedBalanceEmployee = BalanceEmployeeOptions.FirstOrDefault(item => item.Value == row.Source.EmployeeId)
            ?? new LeaveOption(row.Source.EmployeeId, row.EmployeeText);
        SelectedBalanceLeaveType = BalanceLeaveTypeOptions.FirstOrDefault(item => item.Value == row.Source.LeaveTypeId)
            ?? new LeaveOption(row.Source.LeaveTypeId, row.LeaveTypeText);
        await LoadBalanceHistoryAsync(row.Source.EmployeeId, row.Source.LeaveTypeId).ConfigureAwait(true);
    }

    private async Task LoadBalanceHistoryAsync(string employeeId, string leaveTypeId)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(leaveTypeId)) return;
        var asOf = CanonicalDate(ToDate ?? WorkSchedulePresentation.BusinessToday());

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var response = await _service.ListLeaveBalancesAsync(
                asOf,
                employeeId,
                null,
                leaveTypeId).ConfigureAwait(true);
            _balanceCapabilities = response.Capabilities ?? new LeaveBalanceCapabilitiesData();
            BalanceAsOfText = LeavePresentation.DateText(response.AsOfDate);
            BalanceEntryRows.Clear();
            foreach (var entry in response.Entries ?? [])
                BalanceEntryRows.Add(ToBalanceEntryRow(entry));

            var employee = SelectedBalanceEmployee?.Label ?? "nhân sự";
            var leaveType = SelectedBalanceLeaveType?.Label ?? "chế độ nghỉ";
            BalanceLedgerTitle = $"Lịch sử sổ phép · {employee} · {leaveType}";
            RaiseAccess();
            RaiseEmptyStates();
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tải được lịch sử sổ phép."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveBalanceEntryAsync()
    {
        if (!CanManageBalances || IsBusy) return;
        var employeeId = SelectedBalanceEmployee?.Value?.Trim() ?? string.Empty;
        var leaveTypeId = SelectedBalanceLeaveType?.Value?.Trim() ?? string.Empty;
        var entryType = SelectedBalanceEntryType?.Value?.Trim() ?? string.Empty;
        if (employeeId.Length == 0)
        {
            SetMessage("Vui lòng chọn nhân sự cần cập nhật sổ phép.", true);
            return;
        }
        if (leaveTypeId.Length == 0)
        {
            SetMessage("Vui lòng chọn chế độ nghỉ có theo dõi số dư.", true);
            return;
        }
        if (BalanceEffectiveDate is null)
        {
            SetMessage("Vui lòng chọn ngày hiệu lực.", true);
            return;
        }
        if (!TryParseDays(BalanceDays, out var days)
            || days == 0
            || Math.Abs(days) > 3660
            || decimal.Round(days, 2) != days)
        {
            SetMessage("Số ngày phải khác 0, tối đa 3.660 ngày và có tối đa 2 chữ số thập phân.", true);
            return;
        }
        if (!string.Equals(entryType, "ADJUSTMENT", StringComparison.Ordinal) && days < 0)
        {
            SetMessage("Phát sinh này phải nhập số ngày lớn hơn 0.", true);
            return;
        }

        var reason = BalanceReason.Trim();
        if (reason.Length is 0 or > 1000)
        {
            SetMessage("Lý do cập nhật sổ phép là bắt buộc và tối đa 1.000 ký tự.", true);
            return;
        }

        var request = new PostLeaveBalanceEntryRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            EntryType = entryType,
            Days = days,
            EffectiveDate = CanonicalDate(BalanceEffectiveDate.Value),
            Reason = reason
        };
        var slot = MutationSlot("leave-balance-entry", request);
        var key = MutationKey(slot, "leave-balance-entry");

        var success = false;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            await _service.PostLeaveBalanceEntryAsync(request, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            BalanceDays = string.Empty;
            BalanceReason = string.Empty;
            success = true;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không cập nhật được sổ phép."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await ReloadAsync(_pagination.Offset).ConfigureAwait(true);
        await LoadBalanceHistoryAsync(employeeId, leaveTypeId).ConfigureAwait(true);
        SetMessage("Sổ phép đã được cập nhật.", false);
    }

    public void NewType()
    {
        if (!CanManageTypes || IsBusy) return;
        _editingType = null;
        TypeCode = string.Empty;
        TypeName = string.Empty;
        TypeIsActive = true;
        TypeIsPaid = true;
        TypeCountsAsWorkday = true;
        TypeRequiresApproval = true;
        TypeAllowsFullDay = true;
        TypeAllowsHalfDay = true;
        TypeRequiresAttachment = false;
        TypeTracksBalance = false;
        TypeAllowNegativeBalance = false;
        IsTypeEditorOpen = true;
    }

    public void EditType(LeaveTypeRowView row)
    {
        if (!CanManageTypes || IsBusy || !row.CanEdit) return;
        _editingType = row.Source;
        TypeCode = row.Source.Code;
        TypeName = row.Source.Name;
        TypeIsActive = row.Source.IsActive;
        TypeIsPaid = row.Source.IsPaid;
        TypeCountsAsWorkday = row.Source.CountsAsWorkday;
        TypeRequiresApproval = row.Source.RequiresApproval;
        TypeAllowsFullDay = row.Source.AllowsFullDay;
        TypeAllowsHalfDay = row.Source.AllowsHalfDay;
        TypeRequiresAttachment = row.Source.RequiresAttachment;
        TypeTracksBalance = row.Source.TracksBalance;
        TypeAllowNegativeBalance = row.Source.AllowNegativeBalance;
        IsTypeEditorOpen = true;
        OnPropertyChanged(nameof(CanEditTypeCode));
        OnPropertyChanged(nameof(TypeEditorTitle));
    }

    public void CloseTypeEditor()
    {
        _editingType = null;
        IsTypeEditorOpen = false;
        OnPropertyChanged(nameof(CanEditTypeCode));
        OnPropertyChanged(nameof(TypeEditorTitle));
    }

    public async Task SaveTypeAsync()
    {
        if (!CanManageTypes || !IsTypeEditorOpen || IsBusy) return;
        var name = TypeName.Trim();
        if (name.Length is 0 or > 100)
        {
            SetMessage("Tên chế độ nghỉ là bắt buộc và tối đa 100 ký tự.", true);
            return;
        }
        if (!TypeAllowsFullDay && !TypeAllowsHalfDay)
        {
            SetMessage("Chế độ nghỉ phải cho phép nghỉ cả ngày hoặc nửa ngày.", true);
            return;
        }
        if (TypeAllowNegativeBalance && !TypeTracksBalance)
        {
            SetMessage("Chỉ được cho phép âm khi chế độ nghỉ có theo dõi số dư.", true);
            return;
        }

        object request;
        string scope;
        if (_editingType is null)
        {
            var code = TypeCode.Trim().ToUpperInvariant();
            if (code.Length is 0 or > 32)
            {
                SetMessage("Mã chế độ nghỉ là bắt buộc và tối đa 32 ký tự.", true);
                return;
            }
            request = new CreateLeaveTypeRequest
            {
                Code = code,
                Name = name,
                IsActive = TypeIsActive,
                IsPaid = TypeIsPaid,
                CountsAsWorkday = TypeCountsAsWorkday,
                RequiresApproval = TypeRequiresApproval,
                AllowsFullDay = TypeAllowsFullDay,
                AllowsHalfDay = TypeAllowsHalfDay,
                RequiresAttachment = TypeRequiresAttachment,
                TracksBalance = TypeTracksBalance,
                AllowNegativeBalance = TypeAllowNegativeBalance
            };
            scope = "leave-type-create";
        }
        else
        {
            request = new UpdateLeaveTypeRequest
            {
                Id = _editingType.Id,
                ExpectedVersion = _editingType.Version,
                Name = name,
                IsActive = TypeIsActive,
                IsPaid = TypeIsPaid,
                CountsAsWorkday = TypeCountsAsWorkday,
                RequiresApproval = TypeRequiresApproval,
                AllowsFullDay = TypeAllowsFullDay,
                AllowsHalfDay = TypeAllowsHalfDay,
                RequiresAttachment = TypeRequiresAttachment,
                TracksBalance = TypeTracksBalance,
                AllowNegativeBalance = TypeAllowNegativeBalance
            };
            scope = "leave-type-update";
        }

        var slot = MutationSlot(scope, request);
        var key = MutationKey(slot, scope);
        var editing = _editingType is not null;
        var success = false;

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            if (request is CreateLeaveTypeRequest create)
                await _service.CreateLeaveTypeAsync(create, key).ConfigureAwait(true);
            else if (request is UpdateLeaveTypeRequest update)
                await _service.UpdateLeaveTypeAsync(update, key).ConfigureAwait(true);
            _mutationKeys.Remove(slot);
            success = true;
            CloseTypeEditor();
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không lưu được chế độ nghỉ."), true);
        }
        finally
        {
            IsBusy = false;
        }

        if (!success) return;
        await ReloadAsync(_pagination.Offset).ConfigureAwait(true);
        SetMessage(editing ? "Chế độ nghỉ đã được cập nhật." : "Chế độ nghỉ đã được tạo.", false);
    }

    private static bool TryParseDays(string value, out decimal days)
    {
        var candidate = value?.Trim() ?? string.Empty;
        return decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out days)
            || decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.InvariantCulture, out days);
    }
}
