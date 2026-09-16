using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface ISalesOrderService
{
    Task<IReadOnlyList<SalesOrderData>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesOrderData>> ListOperationsDraftsAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerOnboardingRequestData>> ListOperationsCustomerOnboardingAsync(string status, int limit = 20, CancellationToken cancellationToken = default);
    Task<SalesOrderData> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<SalesOrderEntrySettingsData> GetEntrySettingsAsync(CancellationToken cancellationToken = default);
    Task<SalesOrderEntrySettingsData> UpdateEntrySettingsAsync(SalesOrderEntrySettingsUpdateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderSkuCatalogData> GetLocalSkuCatalogAsync(string? cursor = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerData>> SearchCustomersAsync(string search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerAddressData>> ListCustomerAddressesAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariantData>> ListProductVariantsAsync(string productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesOrderSkuSearchOptionData>> SearchSkuAsync(string search, string warehouseId, string salesChannelId, string? customerId, string priceSelectionMode, string pricingAt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesOrderSkuSearchPreviewData>> GetSkuPreviewsAsync(IReadOnlyList<string> variantIds, string warehouseId, string salesChannelId, string? customerId, string priceSelectionMode, string pricingAt, CancellationToken cancellationToken = default);
    Task<SalesPriceResolutionData> ResolvePriceAsync(SalesPricePreviewRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryBalanceLookupData>> ListInventoryBalancesAsync(string warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryMovementHistoryData>> ListInventoryHistoryAsync(string warehouseId, string baseVariantId, int limit = 100, int offset = 0, CancellationToken cancellationToken = default);
    Task<SalesOrderData> CreateDraftAsync(SalesOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> UpdateDraftAsync(string id, SalesOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> ConfirmAsync(string id, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> CreateAmendmentAsync(string id, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> UpdateAmendmentAsync(string id, string versionNumber, SalesOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> ConfirmAmendmentAsync(string id, string versionNumber, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> ManualEditAsync(string id, SalesOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> IssueStockAsync(string id, string expectedRevision, string? mode, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> CancelAsync(string id, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> CloseExecutionAsync(string id, string reason, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> CompleteManualAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> SettleManualAsync(string id, SalesDirectSettlementRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> CompletePickupAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SalesOrderData> SettlePickupAsync(string id, SalesDirectSettlementRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class SalesOrderService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : ISalesOrderService
{
    public async Task<IReadOnlyList<SalesOrderData>> ListAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SalesOrderData[]>("/api/sales-orders?limit=1000&offset=0", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<SalesOrderData>> ListOperationsDraftsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 1000);
        return await apiClient.GetDataAsync<SalesOrderData[]>(
            $"/api/sales-orders?status=draft&limit={safeLimit}&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CustomerOnboardingRequestData>> ListOperationsCustomerOnboardingAsync(string status, int limit = 20, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = status?.Trim() ?? string.Empty;
        if (normalizedStatus is not ("submitted" or "under_review" or "need_more_info"))
            throw new ArgumentException("Trạng thái đề nghị khách hàng không hợp lệ.", nameof(status));

        var safeLimit = Math.Clamp(limit, 1, 100);
        var data = await apiClient.GetDataAsync<CustomerOnboardingListData>(
            $"/api/customer-onboarding-requests?status={Uri.EscapeDataString(normalizedStatus)}&limit={safeLimit}&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
        return data.CustomerOnboardingRequests;
    }

    public Task<SalesOrderData> GetAsync(string id, CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<SalesOrderData>($"/api/sales-orders/{RequireId(id)}", RequireToken(), cancellationToken);

    public Task<SalesOrderEntrySettingsData> GetEntrySettingsAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<SalesOrderEntrySettingsData>("/api/sales-orders/entry-settings", RequireToken(), cancellationToken);

    public Task<SalesOrderEntrySettingsData> UpdateEntrySettingsAsync(SalesOrderEntrySettingsUpdateRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        RequireKey(idempotencyKey);
        return apiClient.PutIdempotentDataAsync<SalesOrderEntrySettingsUpdateRequest, SalesOrderEntrySettingsData>(
            "/api/sales-orders/entry-settings", request, idempotencyKey, RequireToken(), cancellationToken);
    }

    public Task<SalesOrderSkuCatalogData> GetLocalSkuCatalogAsync(string? cursor = null, CancellationToken cancellationToken = default)
    {
        var suffix=string.IsNullOrWhiteSpace(cursor)?string.Empty:$"?since={Uri.EscapeDataString(cursor.Trim())}";
        return apiClient.GetDataAsync<SalesOrderSkuCatalogData>($"/api/products/sales-order-local-catalog{suffix}",RequireToken(),cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerData[]>("/api/customers?limit=1000&offset=0", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<CustomerData>> SearchCustomersAsync(string search, CancellationToken cancellationToken = default)
    {
        var term=search.Trim();
        if(term.Length==0)return [];
        return await apiClient.GetDataAsync<CustomerData[]>($"/api/customers?search={Uri.EscapeDataString(term)}&active=true&limit=30&offset=0",RequireToken(),cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CustomerAddressData>> ListCustomerAddressesAsync(string customerId, CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerAddressData[]>($"/api/customers/{RequireId(customerId)}/addresses", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<WarehouseData[]>("/api/warehouses?limit=1000&offset=0", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductVariantData>> ListProductVariantsAsync(string productId, CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductVariantData[]>($"/api/products/{RequireId(productId)}/variants", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<SalesOrderSkuSearchOptionData>> SearchSkuAsync(string search, string warehouseId, string salesChannelId, string? customerId, string priceSelectionMode, string pricingAt, CancellationToken cancellationToken = default)
    {
        var query = $"search={Uri.EscapeDataString(search.Trim())}&warehouseId={Uri.EscapeDataString(RequireId(warehouseId))}&salesChannelId={Uri.EscapeDataString(RequireId(salesChannelId))}&priceSelectionMode={Uri.EscapeDataString(priceSelectionMode)}&pricingAt={Uri.EscapeDataString(pricingAt)}&limit=30&offset=0";
        if (!string.IsNullOrWhiteSpace(customerId)) query += $"&customerId={Uri.EscapeDataString(RequireId(customerId))}";
        return await apiClient.GetDataAsync<SalesOrderSkuSearchOptionData[]>($"/api/sales-orders/sku-search?{query}", RequireToken(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SalesOrderSkuSearchPreviewData>> GetSkuPreviewsAsync(IReadOnlyList<string> variantIds, string warehouseId, string salesChannelId, string? customerId, string priceSelectionMode, string pricingAt, CancellationToken cancellationToken = default)
    {
        if (variantIds.Count == 0) return [];
        var ids = string.Join("&", variantIds.Select(id => $"variantId={Uri.EscapeDataString(RequireId(id))}"));
        var query = $"{ids}&warehouseId={Uri.EscapeDataString(RequireId(warehouseId))}&salesChannelId={Uri.EscapeDataString(RequireId(salesChannelId))}&priceSelectionMode={Uri.EscapeDataString(priceSelectionMode)}&pricingAt={Uri.EscapeDataString(pricingAt)}";
        if (!string.IsNullOrWhiteSpace(customerId)) query += $"&customerId={Uri.EscapeDataString(RequireId(customerId))}";
        return await apiClient.GetDataAsync<SalesOrderSkuSearchPreviewData[]>($"/api/sales-orders/sku-previews?{query}", RequireToken(), cancellationToken).ConfigureAwait(false);
    }

    public Task<SalesPriceResolutionData> ResolvePriceAsync(SalesPricePreviewRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostDataAsync<SalesPricePreviewRequest, SalesPriceResolutionData>("/api/sales-orders/price-preview", request, RequireToken(), cancellationToken);

    public async Task<IReadOnlyList<InventoryBalanceLookupData>> ListInventoryBalancesAsync(string warehouseId, CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<InventoryBalanceLookupData[]>($"/api/inventory/balances?warehouseId={Uri.EscapeDataString(RequireId(warehouseId))}&limit=1000&offset=0", RequireToken(), cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<InventoryMovementHistoryData>> ListInventoryHistoryAsync(string warehouseId, string baseVariantId, int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
    {
        var safeLimit=Math.Clamp(limit,1,1000);
        var safeOffset=Math.Clamp(offset,0,100000);
        var path=$"/api/inventory/balances/history?warehouseId={Uri.EscapeDataString(RequireId(warehouseId))}&baseVariantId={Uri.EscapeDataString(RequireId(baseVariantId))}&scope=warehouse&limit={safeLimit}&offset={safeOffset}";
        return await apiClient.GetDataAsync<InventoryMovementHistoryData[]>(path,RequireToken(),cancellationToken).ConfigureAwait(false);
    }

    public Task<SalesOrderData> CreateDraftAsync(SalesOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync("/api/sales-orders", request, idempotencyKey, cancellationToken);

    public Task<SalesOrderData> UpdateDraftAsync(string id, SalesOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PutAsync($"/api/sales-orders/{RequireId(id)}/draft", request, idempotencyKey, cancellationToken);

    public Task<SalesOrderData> ConfirmAsync(string id, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/sales-orders/{RequireId(id)}/confirm", new { }, idempotencyKey, cancellationToken);

    public Task<SalesOrderData> CreateAmendmentAsync(string id, string reason, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/sales-orders/{RequireId(id)}/amendments", new SalesReasonRequest(reason.Trim()), idempotencyKey, cancellationToken);

    public Task<SalesOrderData> UpdateAmendmentAsync(string id, string versionNumber, SalesOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PutAsync($"/api/sales-orders/{RequireId(id)}/amendments/{RequireVersion(versionNumber)}/draft", request, idempotencyKey, cancellationToken);

    public Task<SalesOrderData> ConfirmAmendmentAsync(string id, string versionNumber, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/sales-orders/{RequireId(id)}/amendments/{RequireVersion(versionNumber)}/confirm", new { }, idempotencyKey, cancellationToken);

    public Task<SalesOrderData> ManualEditAsync(string id, SalesOrderDraftRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PutAsync($"/api/sales-orders/{RequireId(id)}/manual-edit", request, idempotencyKey, cancellationToken);

    public Task<SalesOrderData> IssueStockAsync(string id, string expectedRevision, string? mode, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/sales-orders/{RequireId(id)}/issue-stock", new SalesExpectedRevisionRequest(expectedRevision, mode), idempotencyKey, cancellationToken);

    public Task<SalesOrderData> CancelAsync(string id, string reason, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/sales-orders/{RequireId(id)}/cancel", new SalesReasonRequest(reason.Trim()), idempotencyKey, cancellationToken);

    public Task<SalesOrderData> CloseExecutionAsync(string id, string reason, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/sales-orders/{RequireId(id)}/close-execution", new SalesReasonRequest(reason.Trim()), idempotencyKey, cancellationToken);

    public Task<SalesOrderData> CompleteManualAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/manual-sales-orders/{RequireId(id)}/complete", new SalesExpectedRevisionRequest(expectedRevision), idempotencyKey, cancellationToken);

    public Task<SalesOrderData> SettleManualAsync(string id, SalesDirectSettlementRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/manual-sales-orders/{RequireId(id)}/settlement", request, idempotencyKey, cancellationToken);

    public Task<SalesOrderData> CompletePickupAsync(string id, string expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/pickup-sales-orders/{RequireId(id)}/complete", new SalesExpectedRevisionRequest(expectedRevision), idempotencyKey, cancellationToken);

    public Task<SalesOrderData> SettlePickupAsync(string id, SalesDirectSettlementRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        PostAsync($"/api/pickup-sales-orders/{RequireId(id)}/settlement", request, idempotencyKey, cancellationToken);

    private Task<SalesOrderData> PostAsync<T>(string path, T request, string key, CancellationToken cancellationToken)
    {
        RequireKey(key);
        return apiClient.PostIdempotentDataAsync<T, SalesOrderData>(path, request, key, RequireToken(), cancellationToken);
    }

    private Task<SalesOrderData> PutAsync<T>(string path, T request, string key, CancellationToken cancellationToken)
    {
        RequireKey(key);
        return apiClient.PutIdempotentDataAsync<T, SalesOrderData>(path, request, key, RequireToken(), cancellationToken);
    }

    private void RequireKey(string key)
    {
        if (!idempotencyKeys.IsValid(key)) throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(key));
    }

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _) ? value.Trim() : throw new ArgumentException("Mã dữ liệu không hợp lệ.", nameof(value));

    private static string RequireVersion(string value) =>
        int.TryParse(value, out var version) && version > 0
            ? version.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : throw new ArgumentException("Phiên bản đơn không hợp lệ.", nameof(value));
}
