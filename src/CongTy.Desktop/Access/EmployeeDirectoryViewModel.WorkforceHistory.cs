using System.Collections.ObjectModel;
using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed partial class EmployeeDirectoryViewModel
{
    private EmployeeDirectoryData? _editorDetail;
    private DateTime? _draftEmploymentStartDate;
    private DateTime? _draftEmploymentEndDate;
    private string _draftEmploymentType = "PERMANENT";
    private string _draftEmploymentEndReason = string.Empty;
    private bool _draftConfirmEmployment;
    private DateTime? _draftAssignmentEffectiveFrom;
    private string _draftAssignmentReason = string.Empty;
    private bool _draftConfirmAssignment;
    private DateTime? _toggleEffectiveDate;
    private string _toggleReason = string.Empty;
    private string _toggleEmploymentType = "OTHER";
    private bool _hasHistoryRequiringConfirmation;

    public IReadOnlyList<EmployeeEmploymentTypeOption> EmploymentTypeOptions => EmployeeDirectoryPresentation.EmploymentTypes;
    public ObservableCollection<EmployeeEmploymentHistoryRowView> EmploymentHistoryRows { get; } = [];
    public ObservableCollection<EmployeeAssignmentHistoryRowView> AssignmentHistoryRows { get; } = [];

    public bool ShowHistory => IsEditorOpen && !IsCreateMode;

    public DateTime? DraftEmploymentStartDate
    {
        get => _draftEmploymentStartDate;
        set { if (SetField(ref _draftEmploymentStartDate, value)) OnPropertyChanged(nameof(CanPersist)); }
    }

    public DateTime? DraftEmploymentEndDate
    {
        get => _draftEmploymentEndDate;
        set { if (SetField(ref _draftEmploymentEndDate, value)) OnPropertyChanged(nameof(CanPersist)); }
    }

    public string DraftEmploymentType
    {
        get => _draftEmploymentType;
        set { if (SetField(ref _draftEmploymentType, value ?? "OTHER")) OnPropertyChanged(nameof(CanPersist)); }
    }

    public string DraftEmploymentEndReason
    {
        get => _draftEmploymentEndReason;
        set { if (SetField(ref _draftEmploymentEndReason, value ?? string.Empty)) OnPropertyChanged(nameof(CanPersist)); }
    }

    public bool DraftConfirmEmployment
    {
        get => _draftConfirmEmployment;
        set { if (SetField(ref _draftConfirmEmployment, value)) OnPropertyChanged(nameof(CanPersist)); }
    }

    public DateTime? DraftAssignmentEffectiveFrom
    {
        get => _draftAssignmentEffectiveFrom;
        set { if (SetField(ref _draftAssignmentEffectiveFrom, value)) OnPropertyChanged(nameof(CanPersist)); }
    }

    public string DraftAssignmentReason
    {
        get => _draftAssignmentReason;
        set { if (SetField(ref _draftAssignmentReason, value ?? string.Empty)) OnPropertyChanged(nameof(CanPersist)); }
    }

    public bool DraftConfirmAssignment
    {
        get => _draftConfirmAssignment;
        set { if (SetField(ref _draftConfirmAssignment, value)) OnPropertyChanged(nameof(CanPersist)); }
    }

    public DateTime? ToggleEffectiveDate
    {
        get => _toggleEffectiveDate;
        set { if (SetField(ref _toggleEffectiveDate, value)) OnPropertyChanged(nameof(CanConfirmToggle)); }
    }

    public string ToggleReason
    {
        get => _toggleReason;
        set { if (SetField(ref _toggleReason, value ?? string.Empty)) OnPropertyChanged(nameof(CanConfirmToggle)); }
    }

    public string ToggleEmploymentType
    {
        get => _toggleEmploymentType;
        set { if (SetField(ref _toggleEmploymentType, value ?? "OTHER")) OnPropertyChanged(nameof(CanConfirmToggle)); }
    }

    public bool HasHistoryRequiringConfirmation
    {
        get => _hasHistoryRequiringConfirmation;
        private set => SetField(ref _hasHistoryRequiringConfirmation, value);
    }

    public bool ShowToggleEmploymentType => _pendingToggleNextActive;

    private void PrepareCreateWorkforceHistory()
    {
        var today = BusinessToday();
        _editorDetail = null;
        DraftEmploymentStartDate = today;
        DraftEmploymentEndDate = null;
        DraftEmploymentType = "PERMANENT";
        DraftEmploymentEndReason = string.Empty;
        DraftConfirmEmployment = true;
        DraftAssignmentEffectiveFrom = today;
        DraftAssignmentReason = string.Empty;
        DraftConfirmAssignment = true;
        EmploymentHistoryRows.Clear();
        AssignmentHistoryRows.Clear();
        HasHistoryRequiringConfirmation = false;
    }

    private void PrepareEditWorkforceHistory(EmployeeDirectoryData detail)
    {
        var employment = detail.CurrentEmployment ?? detail.EmploymentHistory.FirstOrDefault();
        var assignment = detail.CurrentAssignment ?? detail.AssignmentHistory.FirstOrDefault();

        DraftEmploymentStartDate = ParseBusinessDate(employment?.EffectiveFrom) ?? BusinessToday();
        DraftEmploymentEndDate = ParseBusinessDate(employment?.EffectiveTo);
        DraftEmploymentType = string.IsNullOrWhiteSpace(employment?.EmploymentType) ? "OTHER" : employment.EmploymentType;
        DraftEmploymentEndReason = employment?.EndReason ?? string.Empty;
        DraftConfirmEmployment = string.Equals(employment?.DataQuality, "CONFIRMED", StringComparison.Ordinal);

        DraftAssignmentEffectiveFrom = ParseBusinessDate(assignment?.EffectiveFrom) ?? BusinessToday();
        DraftAssignmentReason = assignment?.Reason ?? string.Empty;
        DraftConfirmAssignment = false;
        RebuildHistoryRows(detail);
    }

    private void PrepareToggleWorkforceHistory(EmployeeDirectoryData detail)
    {
        var employment = detail.CurrentEmployment ?? detail.EmploymentHistory.FirstOrDefault();
        ToggleEffectiveDate = BusinessToday();
        ToggleReason = string.Empty;
        ToggleEmploymentType = string.IsNullOrWhiteSpace(employment?.EmploymentType) ? "OTHER" : employment.EmploymentType;
    }

    private void ClearToggleWorkforceHistory()
    {
        ToggleEffectiveDate = null;
        ToggleReason = string.Empty;
        ToggleEmploymentType = "OTHER";
    }

    private void ClearWorkforceHistory()
    {
        DraftEmploymentStartDate = null;
        DraftEmploymentEndDate = null;
        DraftEmploymentType = "PERMANENT";
        DraftEmploymentEndReason = string.Empty;
        DraftConfirmEmployment = false;
        DraftAssignmentEffectiveFrom = null;
        DraftAssignmentReason = string.Empty;
        DraftConfirmAssignment = false;
        EmploymentHistoryRows.Clear();
        AssignmentHistoryRows.Clear();
        HasHistoryRequiringConfirmation = false;
    }

    private void RebuildHistoryRows(EmployeeDirectoryData detail)
    {
        var branchMap = _branches.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

        EmploymentHistoryRows.Clear();
        foreach (var item in detail.EmploymentHistory.OrderByDescending(item => item.EffectiveFrom, StringComparer.Ordinal))
        {
            EmploymentHistoryRows.Add(new EmployeeEmploymentHistoryRowView(
                EmployeeDirectoryPresentation.DateText(item.EffectiveFrom),
                EmployeeDirectoryPresentation.DateText(item.EffectiveTo),
                EmployeeDirectoryPresentation.EmploymentTypeLabel(item.EmploymentType),
                EmployeeDirectoryPresentation.HistoryQualityLabel(item.DataQuality),
                string.IsNullOrWhiteSpace(item.EndReason) ? "—" : item.EndReason.Trim(),
                EmployeeDirectoryPresentation.HistoryNeedsConfirmation(item.DataQuality)));
        }

        AssignmentHistoryRows.Clear();
        foreach (var item in detail.AssignmentHistory.OrderByDescending(item => item.EffectiveFrom, StringComparer.Ordinal))
        {
            AssignmentHistoryRows.Add(new EmployeeAssignmentHistoryRowView(
                EmployeeDirectoryPresentation.DateText(item.EffectiveFrom),
                EmployeeDirectoryPresentation.DateText(item.EffectiveTo),
                EmployeeDirectoryPresentation.AssignmentBranchLabel(item, branchMap),
                EmployeeDirectoryPresentation.HistoryQualityLabel(item.DataQuality),
                string.IsNullOrWhiteSpace(item.Reason) ? "—" : item.Reason.Trim(),
                EmployeeDirectoryPresentation.HistoryNeedsConfirmation(item.DataQuality)));
        }

        HasHistoryRequiringConfirmation =
            detail.EmploymentHistory.Any(item => EmployeeDirectoryPresentation.HistoryNeedsConfirmation(item.DataQuality))
            || detail.AssignmentHistory.Any(item => EmployeeDirectoryPresentation.HistoryNeedsConfirmation(item.DataQuality));
    }

    private bool WorkforceHistoryDraftValid()
    {
        var today = BusinessToday();
        var employmentTypeValid = EmployeeDirectoryPresentation.EmploymentTypes.Any(item => item.Key == DraftEmploymentType);

        if (IsCreateMode)
        {
            if (DraftEmploymentStartDate is null || DraftEmploymentStartDate.Value.Date > today) return false;
            if (!employmentTypeValid) return false;
            if (DraftAssignmentEffectiveFrom is null || DraftAssignmentEffectiveFrom.Value.Date > today) return false;
            if (DraftAssignmentEffectiveFrom.Value.Date < DraftEmploymentStartDate.Value.Date) return false;
            return true;
        }

        if (DraftConfirmEmployment)
        {
            if (DraftEmploymentStartDate is null || !employmentTypeValid) return false;
            if (DraftEmploymentEndDate is { } end && end.Date < DraftEmploymentStartDate.Value.Date) return false;
        }

        if (_editingEmployee is null) return false;
        var selectedBranch = string.IsNullOrWhiteSpace(DraftBranchId) ? null : DraftBranchId.Trim();
        var branchChanged = !string.Equals(selectedBranch, _editingEmployee.BranchId, StringComparison.Ordinal);
        if (branchChanged || DraftConfirmAssignment)
        {
            if (DraftAssignmentEffectiveFrom is null || DraftAssignmentEffectiveFrom.Value.Date > today) return false;
            if (DraftEmploymentStartDate is { } start && DraftAssignmentEffectiveFrom.Value.Date < start.Date) return false;
            if (string.IsNullOrWhiteSpace(DraftAssignmentReason)) return false;
        }

        return true;
    }

    private bool ToggleDraftValid()
    {
        if (ToggleEffectiveDate is null || ToggleEffectiveDate.Value.Date > BusinessToday()) return false;
        if (string.IsNullOrWhiteSpace(ToggleReason)) return false;
        return !ShowToggleEmploymentType
            || EmployeeDirectoryPresentation.EmploymentTypes.Any(item => item.Key == ToggleEmploymentType);
    }

    private static string? CanonicalDate(DateTime? value) =>
        value?.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime? ParseBusinessDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var canonical = value.Trim();
        if (canonical.Length >= 10) canonical = canonical[..10];
        return DateTime.TryParseExact(canonical, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null;
    }

    private static DateTime BusinessToday()
    {
        try
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "SE Asia Standard Time").Date;
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;
        }
    }
}
