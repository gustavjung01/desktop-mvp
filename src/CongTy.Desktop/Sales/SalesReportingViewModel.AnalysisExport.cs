using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CongTy.Desktop.Sales;

public sealed partial class SalesReportingViewModel
{
    private string _selectedBrandId = string.Empty;
    private string _appliedBrandId = string.Empty;
    private string _exportMode = "list";
    private bool _analysisRevenueSelected = true;
    private bool _analysisQuantitySelected = true;
    private string _analysisQuantityDisplay = "sold";
    private string _analysisSort = "name-asc";
    private bool _syncingAnalysisDimensions;

    public ObservableCollection<SalesToggleOption> AnalysisDimensions { get; } = [];
    public ObservableCollection<SalesReportingOption> AnalysisSortOptions { get; } = [];
    public ObservableCollection<SalesExportColumnOption> AnalysisExportColumns { get; } = [];
    public IReadOnlyList<SalesReportingOption> QuantityDisplayOptions => SalesReportingAnalysisPresentation.QuantityDisplays;

    public string ExportMode
    {
        get => _exportMode;
        set
        {
            var normalized = value == "analysis" ? "analysis" : "list";
            if (!SetField(ref _exportMode, normalized)) return;
            if (normalized == "analysis") ExportFormat = "xlsx";
            OnPropertyChanged(nameof(IsListExportMode));
            OnPropertyChanged(nameof(IsAnalysisExportMode));
            OnPropertyChanged(nameof(ExportButtonText));
            RaiseExportSelection();
        }
    }
    public bool IsListExportMode { get => ExportMode == "list"; set { if (value) ExportMode = "list"; } }
    public bool IsAnalysisExportMode { get => ExportMode == "analysis"; set { if (value) ExportMode = "analysis"; } }

    public bool AnalysisRevenueSelected
    {
        get => _analysisRevenueSelected;
        set
        {
            if (!value && !AnalysisQuantitySelected) return;
            if (!SetField(ref _analysisRevenueSelected, value)) return;
            RebuildAnalysisSortOptions(); RefreshAnalysisExportColumns(); RaiseAnalysisState();
        }
    }
    public bool AnalysisQuantitySelected
    {
        get => _analysisQuantitySelected;
        set
        {
            if (!value && !AnalysisRevenueSelected) return;
            if (!SetField(ref _analysisQuantitySelected, value)) return;
            RebuildAnalysisSortOptions(); RefreshAnalysisExportColumns();
            OnPropertyChanged(nameof(HasAnalysisQuantity)); RaiseAnalysisState();
        }
    }
    public bool HasAnalysisQuantity => AnalysisQuantitySelected;

    public string AnalysisQuantityDisplay
    {
        get => _analysisQuantityDisplay;
        set
        {
            var normalized = QuantityDisplayOptions.Any(item => item.Key == value) ? value : "sold";
            if (!SetField(ref _analysisQuantityDisplay, normalized)) return;
            OnPropertyChanged(nameof(AnalysisSummary));
        }
    }
    public string AnalysisSort
    {
        get => _analysisSort;
        set
        {
            var normalized = AnalysisSortOptions.Any(item => item.Key == value) ? value : "name-asc";
            if (!SetField(ref _analysisSort, normalized)) return;
            OnPropertyChanged(nameof(AnalysisSummary));
        }
    }

    public string AnalysisExportDimension
    {
        get
        {
            var selected = SelectedAnalysisDimensions();
            return selected.Count == 2 ? SalesReportingAnalysisPresentation.ExportDimension(selected[0], selected[1], AnalysisRevenueSelected, AnalysisQuantitySelected) : string.Empty;
        }
    }
    public bool AnalysisReady => SelectedAnalysisDimensions().Count == 2 && (AnalysisRevenueSelected || AnalysisQuantitySelected) && AnalysisExportColumns.Any(column => column.IsSelected);
    public string AnalysisSummary
    {
        get
        {
            var selected = SelectedAnalysisDimensions();
            return selected.Count == 2
                ? SalesReportingAnalysisPresentation.Summary(selected[0], selected[1], AnalysisRevenueSelected, AnalysisQuantitySelected, AnalysisQuantityDisplay, AnalysisSort)
                : "Chọn đúng 2 tiêu chí để tạo báo cáo phân tích.";
        }
    }

    private void InitializeAnalysisExport()
    {
        foreach (var dimension in SalesReportingAnalysisPresentation.Dimensions)
        {
            var option = new SalesToggleOption(dimension.Key, dimension.Label);
            option.PropertyChanged += AnalysisDimension_OnPropertyChanged;
            AnalysisDimensions.Add(option);
        }
        RebuildAnalysisSortOptions();
    }

    private void ResetAnalysisExportState()
    {
        _analysisRevenueSelected = true; _analysisQuantitySelected = true; _analysisQuantityDisplay = "sold"; _analysisSort = "name-asc";
        var defaults = SalesReportingAnalysisPresentation.DefaultDimensions(SelectedDimensionKey).ToHashSet(StringComparer.Ordinal);
        _syncingAnalysisDimensions = true;
        try { foreach (var option in AnalysisDimensions) option.IsSelected = defaults.Contains(option.Key); }
        finally { _syncingAnalysisDimensions = false; }
        RebuildAnalysisSortOptions(); RefreshAnalysisExportColumns();
        foreach (var property in new[] { nameof(AnalysisRevenueSelected), nameof(AnalysisQuantitySelected), nameof(HasAnalysisQuantity), nameof(AnalysisQuantityDisplay), nameof(AnalysisSort), nameof(AnalysisSummary), nameof(AnalysisReady), nameof(AnalysisExportDimension), nameof(SelectedExportCountText), nameof(CanSubmitExport) }) OnPropertyChanged(property);
    }

    private void AnalysisDimension_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_syncingAnalysisDimensions || e.PropertyName != nameof(SalesToggleOption.IsSelected) || sender is not SalesToggleOption changed) return;
        var selected = AnalysisDimensions.Where(option => option.IsSelected).ToArray();
        if (selected.Length > 2)
        {
            _syncingAnalysisDimensions = true;
            try { changed.IsSelected = false; } finally { _syncingAnalysisDimensions = false; }
        }
        RefreshAnalysisExportColumns(); RaiseAnalysisState();
    }

    private IReadOnlyList<string> SelectedAnalysisDimensions()
    {
        var selected = AnalysisDimensions.Where(option => option.IsSelected).Select(option => option.Key).ToList();
        if (selected.Remove("products")) selected.Insert(0, "products");
        return selected;
    }

    private void RebuildAnalysisSortOptions()
    {
        Replace(AnalysisSortOptions, SalesReportingAnalysisPresentation.SortOptions(AnalysisRevenueSelected, AnalysisQuantitySelected));
        if (!AnalysisSortOptions.Any(item => item.Key == _analysisSort)) _analysisSort = "name-asc";
        OnPropertyChanged(nameof(AnalysisSort)); OnPropertyChanged(nameof(AnalysisSummary));
    }

    private void RefreshAnalysisExportColumns()
    {
        if (AnalysisDimensions.Count == 0) return;
        var selected = SelectedAnalysisDimensions();
        foreach (var existing in AnalysisExportColumns) existing.PropertyChanged -= AnalysisExportColumn_OnPropertyChanged;
        AnalysisExportColumns.Clear();
        if (selected.Count == 2)
        {
            foreach (var definition in SalesReportingAnalysisPresentation.BuildColumns(_report, selected[0], selected[1], AnalysisRevenueSelected, AnalysisQuantitySelected))
            {
                var option = new SalesExportColumnOption(definition.Key, definition.Label, true);
                option.PropertyChanged += AnalysisExportColumn_OnPropertyChanged;
                AnalysisExportColumns.Add(option);
            }
        }
        RaiseAnalysisState();
    }

    private void AnalysisExportColumn_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SalesExportColumnOption.IsSelected)) RaiseAnalysisState();
    }
    private void RaiseAnalysisState()
    {
        foreach (var property in new[] { nameof(AnalysisReady), nameof(AnalysisSummary), nameof(AnalysisExportDimension), nameof(SelectedExportCountText), nameof(CanSubmitExport) }) OnPropertyChanged(property);
    }
}

public sealed class SalesToggleOption : INotifyPropertyChanged
{
    private bool _isSelected;
    public SalesToggleOption(string key, string label) { Key = key; Label = label; }
    public string Key { get; }
    public string Label { get; }
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
