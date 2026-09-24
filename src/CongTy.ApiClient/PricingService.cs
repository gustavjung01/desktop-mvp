using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IPricingService
{
    Task<IReadOnlyList<SalesChannelData>> ListChannelsAsync(CancellationToken cancellationToken = default);
    Task<SalesChannelData> CreateChannelAsync(SalesChannelCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesChannelData> UpdateChannelAsync(string channelId, SalesChannelUpdateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceListData>> ListPriceListsAsync(CancellationToken cancellationToken = default);
    Task<PriceListData> CreatePriceListAsync(PriceListCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PriceListData> UpdatePriceListAsync(string priceListId, PriceListUpdateRequest request, CancellationToken cancellationToken = default);
    Task<PriceListData> UpdatePriceListStatusAsync(string priceListId, PriceListStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceListItemData>> ListPriceItemsAsync(string priceListId, CancellationToken cancellationToken = default);
    Task<PriceListItemData> CreatePriceItemAsync(string priceListId, PriceListItemCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PriceListItemData> UpdatePriceItemAsync(string priceListId, string itemId, PriceListItemUpdateRequest request, CancellationToken cancellationToken = default);
    Task<PriceListItemData> UpdatePriceItemStatusAsync(string priceListId, string itemId, PriceListItemStatusUpdateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductData>> ListProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariantData>> ListVariantsAsync(string productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductUnitData>> ListUnitsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerGroupData>> ListCustomerGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default);
    Task<PricingResolutionData> ResolveAsync(PricingResolveRequest request, CancellationToken cancellationToken = default);
    Task<PricingOfficialRowsData> ExportOfficialPricingAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PricingAdjustmentResultData> AdjustPricingAsync(PricingAdjustmentRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class PricingService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IPricingService
{
    private const int PageSize = 1000;

    public async Task<IReadOnlyList<SalesChannelData>> ListChannelsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SalesChannelData[]>("/api/sales-channels?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<SalesChannelData> CreateChannelAsync(SalesChannelCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<SalesChannelCreateRequest, SalesChannelData>("/api/sales-channels", request, Key(idempotencyKey), RequireToken(), cancellationToken);

    public Task<SalesChannelData> UpdateChannelAsync(string channelId, SalesChannelUpdateRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<SalesChannelUpdateRequest, SalesChannelData>($"/api/sales-channels/{Id(channelId, nameof(channelId))}", request, RequireToken(), cancellationToken);

    public async Task<IReadOnlyList<PriceListData>> ListPriceListsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<PriceListData[]>("/api/price-lists?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<PriceListData> CreatePriceListAsync(PriceListCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<PriceListCreateRequest, PriceListData>("/api/price-lists", request, Key(idempotencyKey), RequireToken(), cancellationToken);

    public Task<PriceListData> UpdatePriceListAsync(string priceListId, PriceListUpdateRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<PriceListUpdateRequest, PriceListData>($"/api/price-lists/{Id(priceListId, nameof(priceListId))}", request, RequireToken(), cancellationToken);

    public Task<PriceListData> UpdatePriceListStatusAsync(string priceListId, PriceListStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<PriceListStatusUpdateRequest, PriceListData>($"/api/price-lists/{Id(priceListId, nameof(priceListId))}", request, RequireToken(), cancellationToken);

    public async Task<IReadOnlyList<PriceListItemData>> ListPriceItemsAsync(string priceListId, CancellationToken cancellationToken = default)
    {
        var all = new List<PriceListItemData>();
        for (var offset = 0; ; offset += 2000)
        {
            var page = await apiClient.GetDataAsync<PriceListItemData[]>(
                $"/api/price-lists/{Id(priceListId, nameof(priceListId))}/items?limit=2000&offset={offset}",
                RequireToken(), cancellationToken).ConfigureAwait(false);
            all.AddRange(page);
            if (page.Length < 2000) return all;
            if (offset >= 10000) throw new InvalidOperationException("Bảng giá vượt giới hạn đọc an toàn 12.000 dòng.");
        }
    }

    public Task<PriceListItemData> CreatePriceItemAsync(string priceListId, PriceListItemCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<PriceListItemCreateRequest, PriceListItemData>(
            $"/api/price-lists/{Id(priceListId, nameof(priceListId))}/items", request, Key(idempotencyKey), RequireToken(), cancellationToken);

    public Task<PriceListItemData> UpdatePriceItemAsync(string priceListId, string itemId, PriceListItemUpdateRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<PriceListItemUpdateRequest, PriceListItemData>(
            $"/api/price-lists/{Id(priceListId, nameof(priceListId))}/items/{Id(itemId, nameof(itemId))}", request, RequireToken(), cancellationToken);

    public Task<PriceListItemData> UpdatePriceItemStatusAsync(string priceListId, string itemId, PriceListItemStatusUpdateRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<PriceListItemStatusUpdateRequest, PriceListItemData>(
            $"/api/price-lists/{Id(priceListId, nameof(priceListId))}/items/{Id(itemId, nameof(itemId))}", request, RequireToken(), cancellationToken);

    public async Task<IReadOnlyList<ProductData>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<ProductData>();
        for (var offset = 0; ; offset += PageSize)
        {
            var page = await apiClient.GetDataAsync<ProductData[]>(
                $"/api/products?limit={PageSize}&offset={offset}", RequireToken(), cancellationToken).ConfigureAwait(false);
            all.AddRange(page);
            if (page.Length < PageSize) return all;
        }
    }

    public async Task<IReadOnlyList<ProductVariantData>> ListVariantsAsync(string productId, CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductVariantData[]>($"/api/products/{Id(productId, nameof(productId))}/variants", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductUnitData>> ListUnitsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductUnitData[]>("/api/units?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<CustomerGroupData>> ListCustomerGroupsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerGroupData[]>("/api/customer-groups?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<CustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerData[]>("/api/customers?limit=1000", RequireToken(), cancellationToken).ConfigureAwait(false);

    public Task<PricingResolutionData> ResolveAsync(PricingResolveRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostDataAsync<PricingResolveRequest, PricingResolutionData>("/api/pricing/resolve", request, RequireToken(), cancellationToken);

    public Task<PricingOfficialRowsData> ExportOfficialPricingAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<PricingExportRequest, PricingOfficialRowsData>(
            "/api/file-operations/pricing/export",
            new PricingExportRequest("xlsx"),
            Key(idempotencyKey),
            RequireToken(),
            cancellationToken);

    public Task<PricingAdjustmentResultData> AdjustPricingAsync(
        PricingAdjustmentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PostIdempotentDataAsync<PricingAdjustmentRequest, PricingAdjustmentResultData>(
            "/api/pricing/import",
            request,
            Key(idempotencyKey),
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private string Key(string value) =>
        idempotencyKeys.IsValid(value)
            ? value.Trim()
            : throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(value));

    private static string Id(string value, string name) =>
        Guid.TryParse(value, out _)
            ? Uri.EscapeDataString(value.Trim())
            : throw new ArgumentException("Mã dữ liệu không hợp lệ.", name);
}
