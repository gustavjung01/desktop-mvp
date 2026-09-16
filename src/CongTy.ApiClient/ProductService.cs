using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IProductService
{
    Task<IReadOnlyList<ProductData>> ListProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCategoryData>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductBrandData>> ListBrandsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductUnitData>> ListUnitsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariantData>> ListVariantsAsync(string productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductBarcodeData>> ListBarcodesAsync(string productId, string variantId, CancellationToken cancellationToken = default);
    Task<ProductImageIndexData> GetImageIndexAsync(CancellationToken cancellationToken = default);
    Task<ProductTrackingPolicyData> GetTrackingPolicyAsync(string baseVariantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductPriceListData>> ListPriceListsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductPriceItemData>> ListPriceItemsAsync(string priceListId, CancellationToken cancellationToken = default);

    Task<ProductData> CreateProductAsync(ProductCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProductData> UpdateProductAsync(string productId, ProductUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ProductCategoryData> CreateCategoryAsync(ProductCategoryCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProductCategoryData> UpdateCategoryAsync(string categoryId, ProductCategoryUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ProductBrandData> CreateBrandAsync(ProductBrandCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProductBrandData> UpdateBrandAsync(string brandId, ProductBrandUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ProductVariantData> CreateVariantAsync(string productId, ProductVariantCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProductVariantData> UpdateVariantAsync(string productId, string variantId, ProductVariantUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ProductUnitData> CreateUnitAsync(ProductUnitCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProductUnitData> UpdateUnitAsync(string unitId, ProductUnitUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ProductVariantData> UpdateVariantUnitAsync(string productId, string variantId, ProductVariantUnitUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ProductBarcodeData> CreateBarcodeAsync(string productId, string variantId, ProductBarcodeCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProductBarcodeData> UpdateBarcodeAsync(string productId, string variantId, string barcodeId, ProductBarcodeUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ProductQuantityNormalizationData> NormalizeQuantityAsync(string productId, string variantId, string quantity, CancellationToken cancellationToken = default);
    Task<ProductTrackingPolicyData> UpdateTrackingPolicyAsync(string baseVariantId, ProductTrackingPolicyUpdateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProductPriceItemData> CreatePriceItemAsync(string priceListId, ProductPriceItemCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProductPriceItemData> UpdatePriceItemAsync(string priceListId, string itemId, ProductPriceItemUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ProductBulkIdentificationData> IdentifyBulkAsync(ProductBulkIdentifyRequest request, CancellationToken cancellationToken = default);
    Task<ProductBulkPreviewData> PreviewBulkUpdateAsync(ProductBulkUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ProductBulkPreviewData> ApplyBulkUpdateAsync(ProductBulkUpdateRequest request, string operationKey, CancellationToken cancellationToken = default);

    Task<ProductImageMutationData> UploadWebpAsync(string productId, byte[] bytes, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProductImageMutationData> DeleteImageAsync(string productId, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class ProductService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IProductService
{
    private const int ProductPageSize = 1000;

    public async Task<IReadOnlyList<ProductData>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<ProductData>();
        for (var offset = 0; ; offset += ProductPageSize)
        {
            var page = await apiClient.GetDataAsync<ProductData[]>(
                $"/api/products?limit={ProductPageSize}&offset={offset}",
                RequireToken(),
                cancellationToken).ConfigureAwait(false);
            all.AddRange(page);
            if (page.Length < ProductPageSize) return all;
        }
    }

    public async Task<IReadOnlyList<ProductCategoryData>> ListCategoriesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductCategoryData[]>(
            "/api/product-categories?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductBrandData>> ListBrandsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductBrandData[]>(
            "/api/product-brands?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductUnitData>> ListUnitsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductUnitData[]>(
            "/api/units?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductVariantData>> ListVariantsAsync(
        string productId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductVariantData[]>(
            $"/api/products/{Id(productId, nameof(productId))}/variants",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductBarcodeData>> ListBarcodesAsync(
        string productId,
        string variantId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductBarcodeData[]>(
            $"/api/products/{Id(productId, nameof(productId))}/variants/{Id(variantId, nameof(variantId))}/barcodes",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<ProductImageIndexData> GetImageIndexAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<ProductImageIndexData>("/api/products/images", RequireToken(), cancellationToken);

    public Task<ProductTrackingPolicyData> GetTrackingPolicyAsync(
        string baseVariantId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<ProductTrackingPolicyData>(
            $"/api/inventory/tracking-policies/{Id(baseVariantId, nameof(baseVariantId))}",
            RequireToken(),
            cancellationToken);

    public async Task<IReadOnlyList<ProductPriceListData>> ListPriceListsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductPriceListData[]>(
            "/api/price-lists?limit=1000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductPriceItemData>> ListPriceItemsAsync(
        string priceListId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<ProductPriceItemData[]>(
            $"/api/price-lists/{Id(priceListId, nameof(priceListId))}/items?limit=2000",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<ProductData> CreateProductAsync(
        ProductCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotent<ProductCreateRequest, ProductData>("/api/products", request, idempotencyKey, cancellationToken);

    public Task<ProductData> UpdateProductAsync(
        string productId,
        ProductUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<ProductUpdateRequest, ProductData>(
            $"/api/products/{Id(productId, nameof(productId))}",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductCategoryData> CreateCategoryAsync(
        ProductCategoryCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotent<ProductCategoryCreateRequest, ProductCategoryData>("/api/product-categories", request, idempotencyKey, cancellationToken);

    public Task<ProductCategoryData> UpdateCategoryAsync(
        string categoryId,
        ProductCategoryUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<ProductCategoryUpdateRequest, ProductCategoryData>(
            $"/api/product-categories/{Id(categoryId, nameof(categoryId))}",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductBrandData> CreateBrandAsync(
        ProductBrandCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotent<ProductBrandCreateRequest, ProductBrandData>("/api/product-brands", request, idempotencyKey, cancellationToken);

    public Task<ProductBrandData> UpdateBrandAsync(
        string brandId,
        ProductBrandUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<ProductBrandUpdateRequest, ProductBrandData>(
            $"/api/product-brands/{Id(brandId, nameof(brandId))}",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductVariantData> CreateVariantAsync(
        string productId,
        ProductVariantCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotent<ProductVariantCreateRequest, ProductVariantData>(
            $"/api/products/{Id(productId, nameof(productId))}/variants",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<ProductVariantData> UpdateVariantAsync(
        string productId,
        string variantId,
        ProductVariantUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<ProductVariantUpdateRequest, ProductVariantData>(
            $"/api/products/{Id(productId, nameof(productId))}/variants/{Id(variantId, nameof(variantId))}",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductUnitData> CreateUnitAsync(
        ProductUnitCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotent<ProductUnitCreateRequest, ProductUnitData>("/api/units", request, idempotencyKey, cancellationToken);

    public Task<ProductUnitData> UpdateUnitAsync(
        string unitId,
        ProductUnitUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<ProductUnitUpdateRequest, ProductUnitData>(
            $"/api/units/{Id(unitId, nameof(unitId))}",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductVariantData> UpdateVariantUnitAsync(
        string productId,
        string variantId,
        ProductVariantUnitUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<ProductVariantUnitUpdateRequest, ProductVariantData>(
            $"/api/products/{Id(productId, nameof(productId))}/variants/{Id(variantId, nameof(variantId))}/unit",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductBarcodeData> CreateBarcodeAsync(
        string productId,
        string variantId,
        ProductBarcodeCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotent<ProductBarcodeCreateRequest, ProductBarcodeData>(
            $"/api/products/{Id(productId, nameof(productId))}/variants/{Id(variantId, nameof(variantId))}/barcodes",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<ProductBarcodeData> UpdateBarcodeAsync(
        string productId,
        string variantId,
        string barcodeId,
        ProductBarcodeUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<ProductBarcodeUpdateRequest, ProductBarcodeData>(
            $"/api/products/{Id(productId, nameof(productId))}/variants/{Id(variantId, nameof(variantId))}/barcodes/{Id(barcodeId, nameof(barcodeId))}",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductQuantityNormalizationData> NormalizeQuantityAsync(
        string productId,
        string variantId,
        string quantity,
        CancellationToken cancellationToken = default) =>
        apiClient.PostDataAsync<ProductNormalizeQuantityRequest, ProductQuantityNormalizationData>(
            $"/api/products/{Id(productId, nameof(productId))}/variants/{Id(variantId, nameof(variantId))}/normalize-quantity",
            new ProductNormalizeQuantityRequest(quantity),
            RequireToken(),
            cancellationToken);

    public Task<ProductTrackingPolicyData> UpdateTrackingPolicyAsync(
        string baseVariantId,
        ProductTrackingPolicyUpdateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PutIdempotentDataAsync<ProductTrackingPolicyUpdateRequest, ProductTrackingPolicyData>(
            $"/api/inventory/tracking-policies/{Id(baseVariantId, nameof(baseVariantId))}",
            request,
            Key(idempotencyKey),
            RequireToken(),
            cancellationToken);

    public Task<ProductPriceItemData> CreatePriceItemAsync(
        string priceListId,
        ProductPriceItemCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostIdempotent<ProductPriceItemCreateRequest, ProductPriceItemData>(
            $"/api/price-lists/{Id(priceListId, nameof(priceListId))}/items",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<ProductPriceItemData> UpdatePriceItemAsync(
        string priceListId,
        string itemId,
        ProductPriceItemUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<ProductPriceItemUpdateRequest, ProductPriceItemData>(
            $"/api/price-lists/{Id(priceListId, nameof(priceListId))}/items/{Id(itemId, nameof(itemId))}",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductBulkIdentificationData> IdentifyBulkAsync(
        ProductBulkIdentifyRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostDataAsync<ProductBulkIdentifyRequest, ProductBulkIdentificationData>(
            "/api/products/variants/identify",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductBulkPreviewData> PreviewBulkUpdateAsync(
        ProductBulkUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<ProductBulkUpdateRequest, ProductBulkPreviewData>(
            "/api/products/variants/bulk-update",
            request,
            RequireToken(),
            cancellationToken);

    public Task<ProductBulkPreviewData> ApplyBulkUpdateAsync(
        ProductBulkUpdateRequest request,
        string operationKey,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchIdempotentDataAsync<ProductBulkUpdateRequest, ProductBulkPreviewData>(
            "/api/products/variants/bulk-update",
            request,
            Key(operationKey),
            RequireToken(),
            cancellationToken);

    public Task<ProductImageMutationData> UploadWebpAsync(
        string productId,
        byte[] bytes,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length is < 1 or > 5 * 1024 * 1024)
        {
            throw new InvalidOperationException("Ảnh WebP phải có dung lượng từ 1 byte đến 5 MB.");
        }

        return apiClient.PutBytesIdempotentDataAsync<ProductImageMutationData>(
            $"/api/products/{Id(productId, nameof(productId))}/image/upload",
            bytes,
            "image/webp",
            Key(idempotencyKey),
            RequireToken(),
            cancellationToken);
    }

    public Task<ProductImageMutationData> DeleteImageAsync(
        string productId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        apiClient.DeleteIdempotentDataAsync<ProductImageMutationData>(
            $"/api/products/{Id(productId, nameof(productId))}/image",
            Key(idempotencyKey),
            RequireToken(),
            cancellationToken);

    private Task<TResponse> PostIdempotent<TRequest, TResponse>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        apiClient.PostIdempotentDataAsync<TRequest, TResponse>(
            path,
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
