using System.Collections.ObjectModel;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed partial class PayrollFoundationViewModel
{
    private PayrollCalculationData? _calculation;
    private PayrollCalculationRowView? _selectedCalculationRow;
    private bool _reconcileWarningsAcknowledged;
    private string _reconcileNote = string.Empty;

    public ObservableCollection<PayrollCalculationRowView> CalculationRows { get; } = [];
    public ObservableCollection<PayrollIssueRowView> BlockerRows { get; } = [];
    public ObservableCollection<PayrollIssueRowView> WarningRows { get; } = [];
    public ObservableCollection<PayrollIssueRowView> BoardIssueRows { get; } = [];

    public PayrollCalculationData? Calculation => _calculation;

    public PayrollCalculationRowView? SelectedCalculationRow
    {
        get => _selectedCalculationRow;
        set
        {
            if (!SetField(ref _selectedCalculationRow, value)) return;
            OnPropertyChanged(nameof(HasSelectedCalculationRow));
        }
    }

    public bool ReconcileWarningsAcknowledged
    {
        get => _reconcileWarningsAcknowledged;
        set
        {
            if (!SetField(ref _reconcileWarningsAcknowledged, value)) return;
            OnPropertyChanged(nameof(CanReconcile));
        }
    }

    public string ReconcileNote
    {
        get => _reconcileNote;
        set
        {
            if (!SetField(ref _reconcileNote, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanReconcile));
        }
    }

    public bool HasCalculation => Calculation is not null;
    public bool ShowNoCalculation => SelectedPeriod is not null && Calculation is null;
    public bool HasSelectedCalculationRow => SelectedCalculationRow is not null;
    public bool HasBlockers => BlockerRows.Count > 0;
    public bool HasWarnings => WarningRows.Count > 0;
    public bool HasBoardIssues => BoardIssueRows.Count > 0;
    public bool NoReconciliationIssues => HasCalculation && !HasBlockers && !HasWarnings;
    public bool IsReconciled => HasCalculation && string.Equals(SelectedPeriod?.Status, "RECONCILED", StringComparison.Ordinal);

    public bool CanShowReconcileForm =>
        CanManage
        && HasCalculation
        && SelectedPeriod is not null
        && SelectedPeriod.Status is not "RECONCILED" and not "CLOSED";

    public bool CanAggregate =>
        CanManage
        && SelectedPeriod is not null
        && SelectedPeriod.Status != "CLOSED"
        && !IsBusy;

    public bool CanReconcile =>
        CanShowReconcileForm
        && !HasBlockers
        && (!HasWarnings
            || (ReconcileWarningsAcknowledged
                && ReconcileNote.Trim().Length is >= 1 and <= 1000))
        && !IsBusy;

    public string CalculationRevisionText =>
        Calculation is null ? "Chưa tổng hợp" : $"Bản tổng hợp lần {Calculation.Revision}";

    public string CalculationEmployeeCountText =>
        Calculation is null ? "0 nhân sự" : $"{Calculation.Snapshot.Totals.EmployeeCount} nhân sự";

    public string GrossIncomeText =>
        PayrollFoundationPresentation.MoneyText(Calculation?.Snapshot.Totals.GrossIncome);

    public string ReimbursementTotalText =>
        PayrollFoundationPresentation.MoneyText(Calculation?.Snapshot.Totals.ReimbursementTotal);

    public string DeductionTotalText =>
        PayrollFoundationPresentation.MoneyText(Calculation?.Snapshot.Totals.DeductionTotal);

    public string NetPayText =>
        PayrollFoundationPresentation.MoneyText(Calculation?.Snapshot.Totals.NetPay);

    private void ApplyAggregation(PayrollCalculationData? calculation)
    {
        var selectedEmployeeId = SelectedCalculationRow?.Source.EmployeeId;
        _calculation = calculation;
        OnPropertyChanged(nameof(Calculation));

        CalculationRows.Clear();
        BlockerRows.Clear();
        WarningRows.Clear();
        BoardIssueRows.Clear();

        if (calculation is not null)
        {
            foreach (var row in calculation.Snapshot.Rows ?? [])
                CalculationRows.Add(PayrollAggregationPresentation.ToRowView(row));

            foreach (var issue in PayrollAggregationPresentation.ToIssueRows(
                         calculation.IssueSummary.Blockers ?? [],
                         "BLOCKER"))
            {
                BlockerRows.Add(issue);
                BoardIssueRows.Add(issue);
            }

            foreach (var issue in PayrollAggregationPresentation.ToIssueRows(
                         calculation.IssueSummary.Warnings ?? [],
                         "WARNING"))
            {
                WarningRows.Add(issue);
                BoardIssueRows.Add(issue);
            }
        }

        SelectedCalculationRow = CalculationRows.FirstOrDefault(row => row.Source.EmployeeId == selectedEmployeeId)
            ?? CalculationRows.FirstOrDefault();

        _reconcileWarningsAcknowledged = false;
        _reconcileNote = string.Empty;
        OnPropertyChanged(nameof(ReconcileWarningsAcknowledged));
        OnPropertyChanged(nameof(ReconcileNote));
        RaiseAggregationState();
    }

    private void ResetAggregation()
    {
        _calculation = null;
        CalculationRows.Clear();
        BlockerRows.Clear();
        WarningRows.Clear();
        BoardIssueRows.Clear();
        _selectedCalculationRow = null;
        _reconcileWarningsAcknowledged = false;
        _reconcileNote = string.Empty;
        RaiseAggregationState();
        OnPropertyChanged(nameof(SelectedCalculationRow));
        OnPropertyChanged(nameof(ReconcileWarningsAcknowledged));
        OnPropertyChanged(nameof(ReconcileNote));
    }

    private void RaiseAggregationAvailability()
    {
        OnPropertyChanged(nameof(CanAggregate));
        OnPropertyChanged(nameof(CanReconcile));
        OnPropertyChanged(nameof(CanShowReconcileForm));
    }

    private void RaiseAggregationState()
    {
        OnPropertyChanged(nameof(HasCalculation));
        OnPropertyChanged(nameof(ShowNoCalculation));
        OnPropertyChanged(nameof(HasSelectedCalculationRow));
        OnPropertyChanged(nameof(HasBlockers));
        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(HasBoardIssues));
        OnPropertyChanged(nameof(NoReconciliationIssues));
        OnPropertyChanged(nameof(IsReconciled));
        OnPropertyChanged(nameof(CalculationRevisionText));
        OnPropertyChanged(nameof(CalculationEmployeeCountText));
        OnPropertyChanged(nameof(GrossIncomeText));
        OnPropertyChanged(nameof(ReimbursementTotalText));
        OnPropertyChanged(nameof(DeductionTotalText));
        OnPropertyChanged(nameof(NetPayText));
        RaiseAggregationAvailability();
    }
}
