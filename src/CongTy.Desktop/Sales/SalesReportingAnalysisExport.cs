using System.Collections.ObjectModel;
using System.ComponentModel;
using CongTy.ApiClient;

namespace CongTy.Desktop.Sales;

public sealed partial class SalesReportingViewModel
{
    private string _exportMode = "list";
    private string _analysisRowDimension = "products";
    private string _analysisColumnDimension = "customerGroups";
    private bool _analysisRevenueSelected = true;
    private bool _analysisQuantitySelected = true;
    private string _analysisQuantityDisplay = "sold";
    private string _analysisSort = "name-asc";

    public IReadOnlyList<SalesReportingOption> AnalysisDimensionOptions => SalesReportingPresentation.AnalysisDimensions;
    public IReadOnlyList<SalesReportingOption> AnalysisQuantityDisplayOptions => SalesReportingPresentation.QuantityDisplays;
    public ObservableCollection<SalesReportingOption> AnalysisSortOptions { get; } = [];
    public ObservableCollection<SalesExportColumnOption> AnalysisExportColumns { get; } = [];

    public string ExportMode
    {
        get => _exportMode;
        set
        {
            var normalized = value == "analysis" ? "analysis" : "list";
            if (!SetField(ref _exportMode, normalized)) return;
            if (normalized == "analysis" && ExportFormat != "xlsx") ExportFormat = "xlsx";
            OnPropertyChanged(nameof(IsListExportMode));
            OnPropertyChanged(nameof(IsAnalysisExportMode));
            OnPropertyChanged(nameof(ExportButtonText));
            OnPropertyChanged(nameof(CanSubmitExport));
        }
    }

    public bool IsListExportMode => ExportMode == "list";
    public bool IsAnalysisExportMode => ExportMode == "analysis";

    public string AnalysisRowDimension
    {
        get => _analysisRowDimension;
        set
        {
            var normalized = NormalizeAnalysisDimension(value, "products");
            if (normalized == AnalysisColumnDimension)
                _analysisColumnDimension = SalesReportingPresentation.AnalysisDimensions
                    .Select(option => option.Key)
                    .First(key => key != normalized);
            if (!SetField(ref _analysisRowDimension, normalized)) return;
            OnPropertyChanged(nameof(AnalysisColumnDimension));
            RebuildAnalysisExportColumns();
        }
    }

    public string AnalysisColumnDimension
    {
        get => _analysisColumnDimension;
        set
        {
            var normalized = NormalizeAnalysisDimension(value, "customerGroups");
            if (normalized == AnalysisRowDimension)
                normalized = SalesReportingPresentation.AnalysisDimensions
                    .Select(option => option.Key)
                    .First(key => key != AnalysisRowDimension);
            if (!SetField(ref _analysisColumnDimension, normalized)) return;
            RebuildAnalysisExportColumns();
        }
    }

    public bool AnalysisRevenueSelected
    {
        get => _analysisRevenueSelected;
        set
        {
            if (!SetField(ref _analysisRevenueSelected, value)) return;
            EnsureMetricSelection(nameof(AnalysisRevenueSelected));
            RebuildAnalysisSortOptions();
            RebuildAnalysisExportColumns();
        }
    }

    public bool AnalysisQuantitySelected
    {
        get => _analysisQuantitySelected;
        set
        {
            if (!SetField(ref _analysisQuantitySelected, value)) return;
            EnsureMetricSelection(nameof(AnalysisQuantitySelected));
            RebuildAnalysisSortOptions();
            RebuildAnalysisExportColumns();
            OnPropertyChanged(nameof(ShowAnalysisQuantityDisplay));
        }
    }

    public bool ShowAnalysisQuantityDisplay => AnalysisQuantitySelected;

    public string AnalysisQuantityDisplay
    {
        get => _analysisQuantityDisplay;
        set
        {
            var normalized = SalesReportingPresentation.QuantityDisplays.Any(option => option.Key == value)
                ? value
                : "sold";
            if (!SetField(ref _analysisQuantityDisplay, normalized)) return;
            OnPropertyChanged(nameof(CanSubmitExport));
        }
    }

    public string AnalysisSort
    {
        get => _analysisSort;
        set
        {
            var normalized = AnalysisSortOptions.Any(option => option.Key == value) ? value : "name-asc";
            if (!SetField(ref _analysisSort, normalized)) return;
            OnPropertyChanged(nameof(CanSubmitExport));
        }
    }

    public string SelectedAnalysisExportCountText =>
        $"{AnalysisExportColumns.Count(column => column.IsSelected):N0}/{AnalysisExportColumns.Count:N0} cột";

    public string AnalysisMetricSummary =>
        AnalysisRevenueSelected && AnalysisQuantitySelected ? "Doanh thu · Sản lượng"
        : AnalysisRevenueSelected ? "Doanh thu"
        : "Sản lượng";

    public void SelectAllAnalysisExportColumns()
    {
        foreach (var column in AnalysisExportColumns) column.IsSelected = true;
        RaiseAnalysisExportSelection();
    }

    public void ClearAnalysisExportColumns()
    {
        foreach (var column in AnalysisExportColumns) column.IsSelected = false;
        RaiseAnalysisExportSelection();
    }

    private void InitializeAnalysisExport()
    {
        var dimensions = SalesReportingPresentation.DefaultAnalysisDimensions(SelectedDimensionKey);
        _analysisRowDimension = dimensions.Row;
        _analysisColumnDimension = dimensions.Column;
        _analysisRevenueSelected = true;
        _analysisQuantitySelected = true;
        _analysisQuantityDisplay = "sold";
        _analysisSort = "name-asc";

        OnPropertyChanged(nameof(AnalysisRowDimension));
        OnPropertyChanged(nameof(AnalysisColumnDimension));
        OnPropertyChanged(nameof(AnalysisRevenueSelected));
        OnPropertyChanged(nameof(AnalysisQuantitySelected));
        OnPropertyChanged(nameof(AnalysisQuantityDisplay));
        OnPropertyChanged(nameof(ShowAnalysisQuantityDisplay));
        OnPropertyChanged(nameof(AnalysisMetricSummary));

        RebuildAnalysisSortOptions();
        RebuildAnalysisExportColumns();
    }

    private bool CanSubmitCurrentExport()
    {
        if (!CanOpenExport) return false;
        if (IsListExportMode) return ExportColumns.Any(column => column.IsSelected);
        return AnalysisRevenueSelected || AnalysisQuantitySelected
            ? AnalysisRowDimension != AnalysisColumnDimension
              && AnalysisExportColumns.Any(column => column.IsSelected)
            : false;
    }

    private string[] SelectedAnalysisColumns() =>
        AnalysisExportColumns.Where(column => column.IsSelected).Select(column => column.Key).ToArray();

    private Task<ApiDownloadFile> ExportAnalysisAsync(CancellationToken cancellationToken) =>
        _service.ExportAnalysisAsync(
            _appliedFrom,
            _appliedTo,
            _appliedWarehouseId,
            _appliedProductGroupId,
            _appliedCustomerGroupId,
            AnalysisRowDimension,
            AnalysisColumnDimension,
            AnalysisMetrics(),
            AnalysisQuantityDisplay,
            AnalysisSort,
            SelectedAnalysisColumns(),
            cancellationToken);

    private string[] AnalysisMetrics()
    {
        var result = new List<string>(2);
        if (AnalysisRevenueSelected) result.Add("revenue");
        if (AnalysisQuantitySelected) result.Add("quantity");
        return result.ToArray();
    }

    private void EnsureMetricSelection(string changedProperty)
    {
        if (AnalysisRevenueSelected || AnalysisQuantitySelected) return;
        if (changedProperty == nameof(AnalysisRevenueSelected))
        {
            _analysisQuantitySelected = true;
            OnPropertyChanged(nameof(AnalysisQuantitySelected));
            OnPropertyChanged(nameof(ShowAnalysisQuantityDisplay));
        }
        else
        {
            _analysisRevenueSelected = true;
            OnPropertyChanged(nameof(AnalysisRevenueSelected));
        }
    }

    private void RebuildAnalysisSortOptions()
    {
        Replace(AnalysisSortOptions, SalesReportingPresentation.AnalysisSorts(
            AnalysisRevenueSelected,
            AnalysisQuantitySelected));
        if (!AnalysisSortOptions.Any(option => option.Key == _analysisSort))
        {
            _analysisSort = "name-asc";
            OnPropertyChanged(nameof(AnalysisSort));
        }
        OnPropertyChanged(nameof(AnalysisMetricSummary));
    }

    private void RebuildAnalysisExportColumns()
    {
        foreach (var existing in AnalysisExportColumns)
            existing.PropertyChanged -= AnalysisExportColumn_OnPropertyChanged;
        AnalysisExportColumns.Clear();

        if (_report is not null)
        {
            foreach (var definition in SalesReportingPresentation.AnalysisExportColumns(
                         _report,
                         AnalysisRowDimension,
                         AnalysisColumnDimension,
                         AnalysisRevenueSelected,
                         AnalysisQuantitySelected))
            {
                var option = new SalesExportColumnOption(definition.Key, definition.Label, true);
                option.PropertyChanged += AnalysisExportColumn_OnPropertyChanged;
                AnalysisExportColumns.Add(option);
            }
        }

        RaiseAnalysisExportSelection();
    }

    private void AnalysisExportColumn_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SalesExportColumnOption.IsSelected))
            RaiseAnalysisExportSelection();
    }

    private void RaiseAnalysisExportSelection()
    {
        OnPropertyChanged(nameof(SelectedAnalysisExportCountText));
        OnPropertyChanged(nameof(CanSubmitExport));
    }

    private static string NormalizeAnalysisDimension(string? value, string fallback) =>
        SalesReportingPresentation.AnalysisDimensions.Any(option => option.Key == value)
            ? value!
            : fallback;
}
