using System.Collections.ObjectModel;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class PayrollFoundationViewModel
{
    private PayrollCloseoutData _closeout = new();
    private PayrollPayslipRowView? _selectedPayslipRow;
    private PayrollComponentOption? _adjustmentComponent;
    private PayrollChoice? _adjustmentDirection;
    private string _adjustmentAmount = string.Empty;
    private string _adjustmentReason = string.Empty;

    public ObservableCollection<PayrollPayslipRowView> PayslipRows { get; } = [];
    public ObservableCollection<PayrollCloseHistoryRowView> CloseHistoryRows { get; } = [];
    public ObservableCollection<PayrollPayslipRevisionRowView> SelectedPayslipRevisionRows { get; } = [];
    public ObservableCollection<PayrollPayslipAdjustmentRowView> SelectedPayslipAdjustmentRows { get; } = [];
    public ObservableCollection<PayrollComponentOption> AdjustmentComponentOptions { get; } = [];

    public IReadOnlyList<PayrollChoice> AdjustmentDirectionOptions { get; } =
    [
        new PayrollChoice("ADD", "Ghi thêm"),
        new PayrollChoice("REVERSE", "Ghi giảm / hoàn lại")
    ];

    public PayrollCloseoutData Closeout => _closeout;

    public PayrollPayslipRowView? SelectedPayslipRow
    {
        get => _selectedPayslipRow;
        set
        {
            if (!SetField(ref _selectedPayslipRow, value)) return;
            RebuildSelectedPayslipDetails();
            OnPropertyChanged(nameof(HasSelectedPayslip));
            RaiseCloseoutAvailability();
        }
    }

    public PayrollComponentOption? AdjustmentComponent
    {
        get => _adjustmentComponent;
        set
        {
            if (!SetField(ref _adjustmentComponent, value)) return;
            OnPropertyChanged(nameof(CanRecordAdjustment));
        }
    }

    public PayrollChoice? AdjustmentDirection
    {
        get => _adjustmentDirection;
        set
        {
            if (!SetField(ref _adjustmentDirection, value)) return;
            OnPropertyChanged(nameof(CanRecordAdjustment));
        }
    }

    public string AdjustmentAmount
    {
        get => _adjustmentAmount;
        set
        {
            if (!SetField(ref _adjustmentAmount, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanRecordAdjustment));
        }
    }

    public string AdjustmentReason
    {
        get => _adjustmentReason;
        set
        {
            if (!SetField(ref _adjustmentReason, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanRecordAdjustment));
        }
    }

    public bool CanClosePayroll =>
        _capabilities.CanClose
        && _access.HasPermission(PayrollClosePermission)
        && SelectedPeriod?.Status == "RECONCILED"
        && !IsBusy;

    public bool CanAdjustPayroll =>
        _capabilities.CanAdjust
        && _access.HasPermission(PayrollAdjustPermission)
        && SelectedPeriod?.Status == "CLOSED"
        && !IsBusy;

    public bool CanExportPayroll =>
        _capabilities.CanExport
        && _access.HasPermission(PayrollExportPermission)
        && SelectedPeriod?.Status == "CLOSED"
        && _closeout.CloseSnapshot is not null
        && !IsBusy;

    public bool CanRecordAdjustment =>
        CanAdjustPayroll
        && SelectedPayslipRow is not null
        && AdjustmentComponent is not null
        && AdjustmentDirection is not null
        && IsPositiveMoney(AdjustmentAmount)
        && AdjustmentReason.Trim().Length is >= 1 and <= 1000;

    public bool HasPayslips => PayslipRows.Count > 0;
    public bool HasCloseHistory => CloseHistoryRows.Count > 0;
    public bool ShowNoCloseHistory => !HasCloseHistory;
    public bool HasSelectedPayslip => SelectedPayslipRow is not null;
    public bool HasSelectedPayslipAdjustments => SelectedPayslipAdjustmentRows.Count > 0;
    public bool IsPayrollClosed => SelectedPeriod?.Status == "CLOSED";
    public bool CanShowCloseButton => CanClosePayroll;
    public bool ShowCloseReady => SelectedPeriod?.Status == "RECONCILED";
    public bool ShowCloseAwaitingReconcile =>
        SelectedPeriod is not null
        && SelectedPeriod.Status is not "RECONCILED" and not "CLOSED";

    public string CloseoutStatusText => SelectedPeriod?.Status switch
    {
        "CLOSED" => "Kỳ lương đã chốt. Mọi thay đổi sau chốt được ghi bằng Điều chỉnh lương và giữ nguyên phiếu cũ.",
        "RECONCILED" => "Kỳ đã đối soát. Chốt sẽ tạo hồ sơ kỳ và phiếu lương bất biến cho từng nhân sự.",
        null => "Chọn một kỳ lương để hoàn tất kỳ.",
        _ => "Kỳ lương phải được đối soát trên bản tổng hợp hiện tại trước khi chốt."
    };

    public string PayslipAvailabilityText =>
        SelectedPeriod is null
            ? "Chọn một kỳ lương để xem phiếu lương."
            : SelectedPeriod.Status != "CLOSED"
                ? "Phiếu lương chỉ xuất hiện sau khi kỳ lương được chốt."
                : HasPayslips
                    ? string.Empty
                    : "Kỳ đã chốt nhưng chưa có phiếu lương trong phạm vi được cấp.";

    public string SelectedPayslipTitle =>
        SelectedPayslipRow?.EmployeeText ?? "Chưa chọn nhân sự";

    public string SelectedPayslipPeriodText =>
        SelectedPayslipRow?.PeriodText ?? "—";

    public string SelectedPayslipWorkText =>
        SelectedPayslipRow?.WorkText ?? "—";

    public string SelectedPayslipSalaryText =>
        SelectedPayslipRow is null ? "—" : $"Lương theo công: {SelectedPayslipRow.SalaryText}";

    public string SelectedPayslipNetText =>
        SelectedPayslipRow is null ? "—" : $"Thực nhận: {SelectedPayslipRow.NetPayText}";

    public Task OpenCloseHistoryAsync(PayrollCloseHistoryRowView row) =>
        LoadAsync(row.Source.PayrollPeriodId);

    public ApiDownloadFile? CreatePayrollWorkbook()
    {
        if (!CanExportPayroll)
        {
            SetMessage("Bạn không có quyền xuất bảng lương hoặc kỳ lương chưa được chốt.", true);
            return null;
        }

        return PayrollCloseoutExportFile.CreatePayrollWorkbook(_closeout);
    }

    public ApiDownloadFile? CreateSelectedPayslipPdf()
    {
        if (!CanExportPayroll || SelectedPayslipRow is null)
        {
            SetMessage("Vui lòng chọn phiếu lương đã chốt và kiểm tra quyền xuất.", true);
            return null;
        }

        return PayrollCloseoutExportFile.CreatePayslipPdf(SelectedPayslipRow.Source);
    }

    private void ApplyCloseout(PayrollCloseoutData? closeout, IEnumerable<PayrollComponentTypeData> componentTypes)
    {
        var selectedEmployeeId = SelectedPayslipRow?.Source.EmployeeId;
        var selectedComponentId = AdjustmentComponent?.Source.Id;
        _closeout = closeout ?? new PayrollCloseoutData();
        OnPropertyChanged(nameof(Closeout));

        PayslipRows.Clear();
        foreach (var item in _closeout.Payslips ?? [])
            PayslipRows.Add(PayrollCloseoutPresentation.ToPayslipRow(item));

        CloseHistoryRows.Clear();
        foreach (var item in _closeout.History ?? [])
            CloseHistoryRows.Add(PayrollCloseoutPresentation.ToHistoryRow(item));

        AdjustmentComponentOptions.Clear();
        foreach (var item in componentTypes.Where(item => item.IsActive))
        {
            AdjustmentComponentOptions.Add(new PayrollComponentOption(
                item,
                $"{item.Name} · {PayrollFoundationPresentation.CategoryLabel(item.Category)}"));
        }

        _selectedPayslipRow = PayslipRows.FirstOrDefault(item => item.Source.EmployeeId == selectedEmployeeId)
            ?? PayslipRows.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedPayslipRow));
        AdjustmentComponent = AdjustmentComponentOptions.FirstOrDefault(item => item.Source.Id == selectedComponentId)
            ?? AdjustmentComponentOptions.FirstOrDefault();
        AdjustmentDirection ??= AdjustmentDirectionOptions[0];
        RebuildSelectedPayslipDetails();
        RaiseCloseoutState();
    }

    private void ResetCloseout()
    {
        _closeout = new PayrollCloseoutData();
        PayslipRows.Clear();
        CloseHistoryRows.Clear();
        SelectedPayslipRevisionRows.Clear();
        SelectedPayslipAdjustmentRows.Clear();
        AdjustmentComponentOptions.Clear();
        _selectedPayslipRow = null;
        _adjustmentComponent = null;
        _adjustmentDirection = AdjustmentDirectionOptions[0];
        _adjustmentAmount = string.Empty;
        _adjustmentReason = string.Empty;
        RaiseCloseoutState();
        OnPropertyChanged(nameof(Closeout));
        OnPropertyChanged(nameof(SelectedPayslipRow));
        OnPropertyChanged(nameof(AdjustmentComponent));
        OnPropertyChanged(nameof(AdjustmentDirection));
        OnPropertyChanged(nameof(AdjustmentAmount));
        OnPropertyChanged(nameof(AdjustmentReason));
    }

    private void RebuildSelectedPayslipDetails()
    {
        SelectedPayslipRevisionRows.Clear();
        SelectedPayslipAdjustmentRows.Clear();
        var selected = _selectedPayslipRow;
        if (selected is null)
        {
            RaiseSelectedPayslipState();
            return;
        }

        foreach (var revision in (_closeout.PayslipHistory ?? [])
                     .Where(item => item.EmployeeId == selected.Source.EmployeeId)
                     .OrderByDescending(item => item.Revision))
            SelectedPayslipRevisionRows.Add(PayrollCloseoutPresentation.ToRevisionRow(revision));

        foreach (var adjustment in selected.Source.Snapshot.Adjustments ?? [])
            SelectedPayslipAdjustmentRows.Add(PayrollCloseoutPresentation.ToAdjustmentRow(adjustment));

        RaiseSelectedPayslipState();
    }

    private static bool IsPositiveMoney(string value)
    {
        if (!TryMoney(value, out var normalized)) return false;
        return normalized.Any(char.IsDigit)
            && normalized.Any(ch => ch is >= '1' and <= '9');
    }

    private void RaiseSelectedPayslipState()
    {
        OnPropertyChanged(nameof(HasSelectedPayslip));
        OnPropertyChanged(nameof(HasSelectedPayslipAdjustments));
        OnPropertyChanged(nameof(SelectedPayslipTitle));
        OnPropertyChanged(nameof(SelectedPayslipPeriodText));
        OnPropertyChanged(nameof(SelectedPayslipWorkText));
        OnPropertyChanged(nameof(SelectedPayslipSalaryText));
        OnPropertyChanged(nameof(SelectedPayslipNetText));
        OnPropertyChanged(nameof(CanRecordAdjustment));
    }

    private void RaiseCloseoutAvailability()
    {
        OnPropertyChanged(nameof(CanClosePayroll));
        OnPropertyChanged(nameof(CanAdjustPayroll));
        OnPropertyChanged(nameof(CanExportPayroll));
        OnPropertyChanged(nameof(CanRecordAdjustment));
        OnPropertyChanged(nameof(CanShowCloseButton));
    }

    private void RaiseCloseoutState()
    {
        OnPropertyChanged(nameof(HasPayslips));
        OnPropertyChanged(nameof(HasCloseHistory));
        OnPropertyChanged(nameof(ShowNoCloseHistory));
        OnPropertyChanged(nameof(IsPayrollClosed));
        OnPropertyChanged(nameof(ShowCloseReady));
        OnPropertyChanged(nameof(ShowCloseAwaitingReconcile));
        OnPropertyChanged(nameof(CloseoutStatusText));
        OnPropertyChanged(nameof(PayslipAvailabilityText));
        RaiseSelectedPayslipState();
        RaiseCloseoutAvailability();
    }
}
