using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Products;

public sealed partial class ProductViewModel
{
    private const string TrackingReadPermission = "core.inventory-tracking-policy.read";
    private const string TrackingManagePermission = "core.inventory-tracking-policy.manage";

    private readonly IDataExchangeService _dataExchange;
    private string _productImportFileName = string.Empty;
    private string? _productImportFormat;
    private string? _productImportOperationKey;

    public ObservableCollection<DataExchangeImportRow> ProductImportRows { get; } = [];
    public ObservableCollection<ProductFileExportColumn> ProductFileExportColumns { get; } = [];

    public bool CanProductFileExport =>
        CanRead
        && _access.HasPermission(TrackingReadPermission)
        && !IsBusy;

    public bool CanProductFileImport =>
        CanWrite
        && _access.HasPermission(TrackingManagePermission)
        && !IsBusy;

    public bool CanConfirmProductImport =>
        CanProductFileImport
        && ProductImportRows.Count > 0
        && _productImportFormat is not null
        && _productImportOperationKey is not null;

    public bool HasProductImportPreview => ProductImportRows.Count > 0;

    public string ProductImportFileName
    {
        get => _productImportFileName;
        private set => SetField(ref _productImportFileName, value ?? string.Empty);
    }

    public string ProductImportSummary =>
        ProductImportRows.Count == 0
            ? "Chưa chọn tệp."
            : $"{ProductImportRows.Count:N0} dòng · {ProductImportFileName}";

    public string SelectedProductExportCountText =>
        $"{ProductFileExportColumns.Count(column => column.IsSelected):N0}/{ProductFileExportColumns.Count:N0} cột";

    private void InitializeProductFileWorkspace()
    {
        ProductFileExportColumns.Clear();
        foreach (var column in DataExchangePresentation.ProductColumns)
        {
            var option = new ProductFileExportColumn(
                column,
                DataExchangePresentation.Label(column),
                true);
            option.PropertyChanged += ProductFileExportColumnChanged;
            ProductFileExportColumns.Add(option);
        }
        NotifyProductFileState();
    }

    public async Task PrepareProductImportAsync(string filePath)
    {
        if (!CanProductFileImport) return;

        SetBusy("product-file-import-prepare");
        ClearMessage();
        try
        {
            var rows = await DataExchangeFileHelper.ReadAsync(
                filePath,
                DataExchangePresentation.ProductRequiredColumns).ConfigureAwait(true);

            if (rows.Count > 5_000)
                throw new InvalidOperationException("Mỗi lần chỉ nhập tối đa 5.000 dòng sản phẩm/SKU.");

            ResetProductImportWorkspace();
            foreach (var values in rows)
                ProductImportRows.Add(DataExchangeImportRow.From(values));

            ProductImportFileName = Path.GetFileName(filePath);
            _productImportFormat = filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? "xlsx"
                : "csv";
            _productImportOperationKey = _idempotencyKeys.Create("product-file-import");
            if (!_idempotencyKeys.IsValid(_productImportOperationKey))
                throw new InvalidOperationException("Không tạo được khóa chống xử lý trùng hợp lệ.");

            SetNotice($"Đã đọc {ProductImportRows.Count:N0} dòng từ “{ProductImportFileName}”. Kiểm tra xem trước rồi bấm Xác nhận nhập.");
        }
        catch (Exception exception)
        {
            ResetProductImportWorkspace();
            SetFailure(exception);
        }
        finally
        {
            SetBusy(null);
            NotifyProductFileState();
        }
    }

    public void CancelProductImport()
    {
        ResetProductImportWorkspace();
        ClearMessage();
    }

    public async Task ConfirmProductImportAsync()
    {
        if (!CanConfirmProductImport || _productImportFormat is null || _productImportOperationKey is null) return;

        var operationKey = _productImportOperationKey;
        SetBusy("product-file-import");
        ClearMessage();
        try
        {
            var rows = ProductImportRows.Select(row => row.ToDictionary()).ToArray();
            var result = await _dataExchange.ImportProductsAsync(
                _productImportFormat,
                rows,
                operationKey).ConfigureAwait(true);

            var imported = result.Import?.Imported ?? rows.Length;
            var configured = result.Onboarding?.VariantsConfigured ?? 0;
            var policies = result.Onboarding?.PoliciesConfigured ?? 0;

            ResetProductImportWorkspace();
            await LoadAsync(preserveMessage: true).ConfigureAwait(true);
            SetNotice($"Đã nhập {imported:N0} sản phẩm/SKU; đã gắn đơn vị cho {configured:N0} SKU và thiết lập chính sách kho cho {policies:N0} SKU.");
        }
        catch (Exception exception)
        {
            SetFailure(exception);
        }
        finally
        {
            SetBusy(null);
            NotifyProductFileState();
        }
    }

    public void WriteProductImportTemplate(string format, string filePath)
    {
        DataExchangeFileHelper.Write(
            filePath,
            "Sản phẩm SKU",
            DataExchangePresentation.ProductColumns,
            [],
            format);
        SetNotice($"Đã tạo mẫu {format.ToUpperInvariant()} cho sản phẩm/SKU.");
    }

    public async Task ExportProductFileAsync(string format, string filePath)
    {
        if (!CanProductFileExport) return;

        SetBusy("product-file-export");
        ClearMessage();
        try
        {
            var selected = ProductFileExportColumns
                .Where(column => column.IsSelected)
                .Select(column => column.Id)
                .ToArray();
            if (selected.Length == 0)
                throw new InvalidOperationException("Chọn ít nhất một thông tin để xuất.");

            var key = KeyFor(
                "file-export",
                "products",
                $"{format}|{string.Join(",", selected)}");
            var result = await _dataExchange.ExportProductsAsync(format, key).ConfigureAwait(true);
            selected = selected
                .Where(column => result.Columns.Contains(column, StringComparer.Ordinal))
                .ToArray();
            if (selected.Length == 0)
                throw new InvalidOperationException("File nguồn không có cột đã chọn.");

            var rows = result.Rows
                .Select(row => selected.Select(column =>
                    row.TryGetValue(column, out var value)
                        ? DataExchangePresentation.DisplayCell(column, value)
                        : string.Empty).ToArray())
                .ToArray();

            DataExchangeFileHelper.Write(filePath, "Sản phẩm SKU", selected, rows, format);
            SetNotice($"Đã xuất {rows.Length:N0} dòng SKU.");
        }
        catch (Exception exception)
        {
            SetFailure(exception);
        }
        finally
        {
            SetBusy(null);
            NotifyProductFileState();
        }
    }

    public void SelectAllProductExportColumns()
    {
        foreach (var column in ProductFileExportColumns) column.IsSelected = true;
        NotifyProductFileState();
    }

    public void ClearProductExportColumns()
    {
        foreach (var column in ProductFileExportColumns) column.IsSelected = false;
        NotifyProductFileState();
    }

    private void ResetProductImportWorkspace()
    {
        ProductImportRows.Clear();
        ProductImportFileName = string.Empty;
        _productImportFormat = null;
        _productImportOperationKey = null;
        NotifyProductFileState();
    }

    private void ProductFileExportColumnChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductFileExportColumn.IsSelected))
            NotifyProductFileState();
    }

    private void NotifyProductFileState()
    {
        OnPropertyChanged(nameof(CanProductFileExport));
        OnPropertyChanged(nameof(CanProductFileImport));
        OnPropertyChanged(nameof(CanConfirmProductImport));
        OnPropertyChanged(nameof(HasProductImportPreview));
        OnPropertyChanged(nameof(ProductImportSummary));
        OnPropertyChanged(nameof(SelectedProductExportCountText));
    }
}

public sealed class ProductFileExportColumn : INotifyPropertyChanged
{
    private bool _isSelected;

    public ProductFileExportColumn(string id, string label, bool isSelected)
    {
        Id = id;
        Label = label;
        _isSelected = isSelected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }
    public string Label { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}
