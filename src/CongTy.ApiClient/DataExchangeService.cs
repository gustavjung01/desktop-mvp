using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IDataExchangeService
{
    Task<IReadOnlyList<ProductData>> ListProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCategoryData>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductBrandData>> ListBrandsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductUnitData>> ListUnitsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceListData>> ListPriceListsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesChannelData>> ListSalesChannelsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataExchangeCustomerGroupData>> ListCustomerGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataExchangeCustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryBalanceData>> ListBalancesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataExchangeProductVariantData>> ListVariantsAsync(string productId, CancellationToken cancellationToken = default);
    Task<DataExchangeOfficialRowsData> ExportProductsAsync(string format, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataExchangeProductImportResultData> ImportProductsAsync(string format, IReadOnlyList<Dictionary<string, string>> rows, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataExchangeOfficialRowsData> ExportPricingAsync(string format, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataExchangePricingImportResultData> ImportPricingAsync(DataExchangePricingImportRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataExchangeOfficialRowsData> ExportStocktakeAsync(string warehouseId, string format, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataExchangeStocktakeImportResultData> ImportStocktakeAsync(string format, IReadOnlyList<Dictionary<string, string>> rows, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DataExchangeOfficialRowsData> BuildQuotationAsync(DataExchangeQuotationRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataExchangeMovementData>> ListMovementsAsync(
        string warehouseId,
        string baseVariantId,
        string? locationId,
        string? lotId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);
}

public sealed class DataExchangeService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IDataExchangeService
{
    private const int PageSize = 1000;

    public async Task<IReadOnlyList<ProductData>> ListProductsAsync(CancellationToken cancellationToken = default) =>
        await ReadAllAsync<ProductData>("/api/products", cancellationToken).ConfigureAwait(false);

    public Task<IReadOnlyList<ProductCategoryData>> ListCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetArrayAsync<ProductCategoryData>("/api/product-categories?limit=1000", cancellationToken);

    public Task<IReadOnlyList<ProductBrandData>> ListBrandsAsync(CancellationToken cancellationToken = default) =>
        GetArrayAsync<ProductBrandData>("/api/product-brands?limit=1000", cancellationToken);

    public Task<IReadOnlyList<ProductUnitData>> ListUnitsAsync(CancellationToken cancellationToken = default) =>
        GetArrayAsync<ProductUnitData>("/api/units?limit=1000", cancellationToken);

    public Task<IReadOnlyList<PriceListData>> ListPriceListsAsync(CancellationToken cancellationToken = default) =>
        GetArrayAsync<PriceListData>("/api/price-lists?limit=1000", cancellationToken);

    public Task<IReadOnlyList<SalesChannelData>> ListSalesChannelsAsync(CancellationToken cancellationToken = default) =>
        GetArrayAsync<SalesChannelData>("/api/sales-channels?limit=1000", cancellationToken);

    public Task<IReadOnlyList<DataExchangeCustomerGroupData>> ListCustomerGroupsAsync(CancellationToken cancellationToken = default) =>
        GetArrayAsync<DataExchangeCustomerGroupData>("/api/customer-groups?limit=1000", cancellationToken);

    public Task<IReadOnlyList<DataExchangeCustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default) =>
        GetArrayAsync<DataExchangeCustomerData>("/api/customers?limit=1000", cancellationToken);

    public async Task<IReadOnlyList<InventoryBalanceData>> ListBalancesAsync(CancellationToken cancellationToken = default)
    {
        var rows = new List<InventoryBalanceData>();
        var offset = 0;
        while (true)
        {
            var page = await apiClient.GetDataAsync<InventoryBalanceData[]>(
                $"/api/inventory/balances?limit={PageSize}&offset={offset}",
                RequireToken(),
                cancellationToken).ConfigureAwait(false);
            rows.AddRange(page);
            if (page.Length < PageSize) return rows;
            offset = checked(offset + PageSize);
            if (offset > 100_000)
                throw new InvalidOperationException("Dữ liệu tồn kho quá lớn. Hãy dùng Tra cứu tồn kho để thu hẹp phạm vi.");
        }
    }

    public Task<IReadOnlyList<DataExchangeProductVariantData>> ListVariantsAsync(
        string productId,
        CancellationToken cancellationToken = default) =>
        GetArrayAsync<DataExchangeProductVariantData>(
            $"/api/products/{RequireId(productId, nameof(productId))}/variants",
            cancellationToken);

    public Task<DataExchangeOfficialRowsData> ExportProductsAsync(
        string format,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostFileOperationAsync<DataExchangeExportRequest, DataExchangeOfficialRowsData>(
            "/api/file-operations/products/export",
            new DataExchangeExportRequest(RequireFormat(format)),
            idempotencyKey,
            cancellationToken);

    public Task<DataExchangeProductImportResultData> ImportProductsAsync(
        string format,
        IReadOnlyList<Dictionary<string, string>> rows,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostFileOperationAsync<DataExchangeFileRowsRequest, DataExchangeProductImportResultData>(
            "/api/file-operations/products/import",
            new DataExchangeFileRowsRequest(RequireFormat(format), rows),
            idempotencyKey,
            cancellationToken);

    public Task<DataExchangeOfficialRowsData> ExportPricingAsync(
        string format,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostFileOperationAsync<DataExchangeExportRequest, DataExchangeOfficialRowsData>(
            "/api/file-operations/pricing/export",
            new DataExchangeExportRequest(RequireFormat(format)),
            idempotencyKey,
            cancellationToken);

    public Task<DataExchangePricingImportResultData> ImportPricingAsync(
        DataExchangePricingImportRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<DataExchangePricingImportRequest, DataExchangePricingImportResultData>(
            "/api/pricing/import",
            request,
            RequireKey(idempotencyKey),
            RequireToken(),
            cancellationToken);

    public Task<DataExchangeOfficialRowsData> ExportStocktakeAsync(
        string warehouseId,
        string format,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostFileOperationAsync<DataExchangeStocktakeExportRequest, DataExchangeOfficialRowsData>(
            "/api/file-operations/stocktake/export",
            new DataExchangeStocktakeExportRequest(
                RequireId(warehouseId, nameof(warehouseId)),
                RequireFormat(format)),
            idempotencyKey,
            cancellationToken);

    public Task<DataExchangeStocktakeImportResultData> ImportStocktakeAsync(
        string format,
        IReadOnlyList<Dictionary<string, string>> rows,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostFileOperationAsync<DataExchangeFileRowsRequest, DataExchangeStocktakeImportResultData>(
            "/api/file-operations/stocktake/import",
            new DataExchangeFileRowsRequest(RequireFormat(format), rows),
            idempotencyKey,
            cancellationToken);

    public Task<DataExchangeOfficialRowsData> BuildQuotationAsync(
        DataExchangeQuotationRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostFileOperationAsync<DataExchangeQuotationRequest, DataExchangeOfficialRowsData>(
            "/api/file-operations/quotation",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<IReadOnlyList<DataExchangeMovementData>> ListMovementsAsync(
        string warehouseId,
        string baseVariantId,
        string? locationId,
        string? lotId,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));

        var query = new List<string>
        {
            $"warehouseId={RequireId(warehouseId, nameof(warehouseId))}",
            $"baseVariantId={RequireId(baseVariantId, nameof(baseVariantId))}",
            $"limit={limit}",
            $"offset={offset}"
        };
        if (!string.IsNullOrWhiteSpace(locationId))
            query.Add($"locationId={RequireId(locationId, nameof(locationId))}");
        if (!string.IsNullOrWhiteSpace(lotId))
            query.Add($"lotId={RequireId(lotId, nameof(lotId))}");

        return GetArrayAsync<DataExchangeMovementData>(
            $"/api/inventory/balances/drill-down?{string.Join("&", query)}",
            cancellationToken);
    }

    private async Task<IReadOnlyList<T>> ReadAllAsync<T>(string path, CancellationToken cancellationToken)
    {
        var rows = new List<T>();
        for (var offset = 0; ; offset += PageSize)
        {
            var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            var page = await apiClient.GetDataAsync<T[]>(
                $"{path}{separator}limit={PageSize}&offset={offset}",
                RequireToken(),
                cancellationToken).ConfigureAwait(false);
            rows.AddRange(page);
            if (page.Length < PageSize) return rows;
        }
    }

    private async Task<IReadOnlyList<T>> GetArrayAsync<T>(string path, CancellationToken cancellationToken) =>
        await apiClient.GetDataAsync<T[]>(path, RequireToken(), cancellationToken).ConfigureAwait(false);

    private Task<TResponse> PostFileOperationAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        apiClient.PostIdempotentDataAsync<TRequest, TResponse>(
            path,
            request,
            RequireKey(idempotencyKey),
            RequireToken(),
            cancellationToken);

    private string RequireKey(string value) =>
        idempotencyKeys.IsValid(value)
            ? value.Trim()
            : throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(value));

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireFormat(string value) =>
        value is "xlsx" or "csv"
            ? value
            : throw new ArgumentException("Định dạng tệp không hợp lệ.", nameof(value));

    private static string RequireId(string value, string parameterName) =>
        Guid.TryParse(value, out _)
            ? Uri.EscapeDataString(value.Trim())
            : throw new ArgumentException("Mã dữ liệu không hợp lệ.", parameterName);
}
