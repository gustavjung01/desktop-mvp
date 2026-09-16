using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IPartnerService
{
    Task<IReadOnlyList<CustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerGroupData>> ListCustomerGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeData>> ListEmployeesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerAddressData>> ListCustomerAddressesAsync(string customerId, CancellationToken cancellationToken = default);
    Task<CustomerProfileOverviewData> GetCustomerOverviewAsync(string customerId, string period = "90d", CancellationToken cancellationToken = default);
    Task<CustomerPurchasedItemsPageData> GetCustomerPurchasedItemsAsync(string customerId, string period = "90d", string search = "", int limit = 50, int offset = 0, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerSalesOrderData>> ListCustomerOrdersAsync(string customerId, string search = "", int limit = 21, int offset = 0, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerReceivableDocumentData>> ListCustomerReceivablesAsync(string customerId, int limit = 21, int offset = 0, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerPaymentHistoryData>> ListCustomerPaymentsAsync(string customerId, int limit = 21, int offset = 0, CancellationToken cancellationToken = default);
    Task<CustomerDeliveryReturnsData> GetCustomerDeliveryReturnsAsync(string customerId, int deliveryLimit = 20, int deliveryOffset = 0, int returnLimit = 20, int returnOffset = 0, CancellationToken cancellationToken = default);

    Task<CustomerData> CreateCustomerAsync(CustomerCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerData> UpdateCustomerAsync(string id, CustomerUpdateRequest request, CancellationToken cancellationToken = default);
    Task<CustomerData> SetCustomerActiveAsync(string id, bool isActive, string expectedUpdatedAt, CancellationToken cancellationToken = default);

    Task<CustomerGroupData> CreateCustomerGroupAsync(CustomerGroupCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerGroupData> UpdateCustomerGroupAsync(string id, CustomerGroupUpdateRequest request, CancellationToken cancellationToken = default);
    Task<CustomerGroupData> SetCustomerGroupActiveAsync(string id, bool isActive, string expectedUpdatedAt, CancellationToken cancellationToken = default);

    Task<CustomerAddressData> CreateCustomerAddressAsync(string customerId, CustomerAddressCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerAddressData> UpdateCustomerAddressAsync(string customerId, string addressId, CustomerAddressUpdateRequest request, CancellationToken cancellationToken = default);

    Task<CustomerMediaListData> ListCustomerMediaAsync(string customerId, CancellationToken cancellationToken = default);
    Task<CustomerMediaPrepareData> PrepareCustomerMediaAsync(string customerId, CustomerMediaPrepareRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<CustomerMediaMutationData> FinalizeCustomerMediaAsync(string customerId, CustomerMediaFinalizeRequest request, string idempotencyKey, CancellationToken cancellationToken = default);

    Task<CustomerIdentifyResult> IdentifyCustomersAsync(CustomerBulkSourceRow[] rows, CancellationToken cancellationToken = default);
    Task<CustomerBulkResult> PreviewCustomerImportAsync(CustomerBulkRequest request, CancellationToken cancellationToken = default);
    Task<CustomerBulkResult> ApplyCustomerImportAsync(CustomerBulkRequest request, string operationKey, CancellationToken cancellationToken = default);
    Task<CustomerBulkResult> PreviewCustomerBulkUpdateAsync(CustomerBulkRequest request, CancellationToken cancellationToken = default);
    Task<CustomerBulkResult> ApplyCustomerBulkUpdateAsync(CustomerBulkRequest request, string operationKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VietnamProvinceData>> ListVietnamProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VietnamWardData>> ListVietnamWardsAsync(string provinceCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierData>> ListSuppliersAsync(CancellationToken cancellationToken = default);
    Task<SupplierData> CreateSupplierAsync(SupplierCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierData> UpdateSupplierAsync(string id, SupplierUpdateRequest request, CancellationToken cancellationToken = default);
    Task<SupplierData> SetSupplierActiveAsync(string id, bool isActive, string expectedUpdatedAt, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierContactData>> ListSupplierContactsAsync(string supplierId, CancellationToken cancellationToken = default);
    Task<SupplierContactData> CreateSupplierContactAsync(string supplierId, SupplierContactCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierContactData> UpdateSupplierContactAsync(string supplierId, string contactId, SupplierContactUpdateRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierAddressData>> ListSupplierAddressesAsync(string supplierId, CancellationToken cancellationToken = default);
    Task<SupplierAddressData> CreateSupplierAddressAsync(string supplierId, SupplierAddressCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierAddressData> UpdateSupplierAddressAsync(string supplierId, string addressId, SupplierAddressUpdateRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierPaymentTermData>> ListSupplierPaymentTermsAsync(string supplierId, CancellationToken cancellationToken = default);
    Task<SupplierPaymentTermData> CreateSupplierPaymentTermAsync(string supplierId, SupplierPaymentTermCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupplierPaymentTermData> UpdateSupplierPaymentTermAsync(string supplierId, string paymentTermId, SupplierPaymentTermUpdateRequest request, CancellationToken cancellationToken = default);
}

public sealed class PartnerService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IPartnerService
{
    public async Task<IReadOnlyList<CustomerData>> ListCustomersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerData[]>(
            "/api/customers?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<CustomerGroupData>> ListCustomerGroupsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerGroupData[]>(
            "/api/customer-groups?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<EmployeeData>> ListEmployeesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<EmployeeData[]>(
            "/api/employees?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<CustomerAddressData>> ListCustomerAddressesAsync(
        string customerId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerAddressData[]>(
            $"/api/customers/{RequireId(customerId)}/addresses",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<CustomerProfileOverviewData> GetCustomerOverviewAsync(
        string customerId,
        string period = "90d",
        CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = period is "30d" or "90d" or "365d" or "all" ? period : "90d";
        return apiClient.GetDataAsync<CustomerProfileOverviewData>(
            $"/api/customers/{RequireId(customerId)}/overview?period={normalizedPeriod}",
            RequireToken(),
            cancellationToken);
    }

    public Task<CustomerPurchasedItemsPageData> GetCustomerPurchasedItemsAsync(
        string customerId,
        string period = "90d",
        string search = "",
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = period is "30d" or "90d" or "365d" or "all" ? period : "90d";
        var safeLimit = Math.Clamp(limit, 1, 100);
        var safeOffset = Math.Max(0, offset);
        var query = $"/api/customers/{RequireId(customerId)}/purchased-items?period={normalizedPeriod}&limit={safeLimit}&offset={safeOffset}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim()[..Math.Min(120, search.Trim().Length)])}";
        }

        return apiClient.GetDataAsync<CustomerPurchasedItemsPageData>(query, RequireToken(), cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerSalesOrderData>> ListCustomerOrdersAsync(
        string customerId,
        string search = "",
        int limit = 21,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var query = $"/api/sales-orders?customerId={RequireId(customerId)}&limit={Math.Clamp(limit, 1, 100)}&offset={Math.Max(0, offset)}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim()[..Math.Min(120, search.Trim().Length)])}";
        }

        return await apiClient.GetDataAsync<CustomerSalesOrderData[]>(query, RequireToken(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CustomerReceivableDocumentData>> ListCustomerReceivablesAsync(
        string customerId,
        int limit = 21,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerReceivableDocumentData[]>(
            $"/api/receivables?customerId={RequireId(customerId)}&limit={Math.Clamp(limit, 1, 100)}&offset={Math.Max(0, offset)}",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<CustomerPaymentHistoryData>> ListCustomerPaymentsAsync(
        string customerId,
        int limit = 21,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<CustomerPaymentHistoryData[]>(
            $"/api/customer-payments?customerId={RequireId(customerId)}&limit={Math.Clamp(limit, 1, 100)}&offset={Math.Max(0, offset)}",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<CustomerDeliveryReturnsData> GetCustomerDeliveryReturnsAsync(
        string customerId,
        int deliveryLimit = 20,
        int deliveryOffset = 0,
        int returnLimit = 20,
        int returnOffset = 0,
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<CustomerDeliveryReturnsData>(
            $"/api/customers/{RequireId(customerId)}/delivery-returns?deliveryLimit={Math.Clamp(deliveryLimit, 1, 100)}&deliveryOffset={Math.Max(0, deliveryOffset)}&returnLimit={Math.Clamp(returnLimit, 1, 100)}&returnOffset={Math.Max(0, returnOffset)}",
            RequireToken(),
            cancellationToken);

    public Task<CustomerData> CreateCustomerAsync(
        CustomerCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<CustomerCreateRequest, CustomerData>(
            "/api/customers",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerData> UpdateCustomerAsync(
        string id,
        CustomerUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<CustomerUpdateRequest, CustomerData>(
            $"/api/customers/{RequireId(id)}",
            request,
            cancellationToken);

    public Task<CustomerData> SetCustomerActiveAsync(
        string id,
        bool isActive,
        string expectedUpdatedAt,
        CancellationToken cancellationToken = default) =>
        PatchAsync<ActiveStatusUpdateRequest, CustomerData>(
            $"/api/customers/{RequireId(id)}",
            new ActiveStatusUpdateRequest(isActive, expectedUpdatedAt),
            cancellationToken);

    public Task<CustomerGroupData> CreateCustomerGroupAsync(
        CustomerGroupCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<CustomerGroupCreateRequest, CustomerGroupData>(
            "/api/customer-groups",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerGroupData> UpdateCustomerGroupAsync(
        string id,
        CustomerGroupUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<CustomerGroupUpdateRequest, CustomerGroupData>(
            $"/api/customer-groups/{RequireId(id)}",
            request,
            cancellationToken);

    public Task<CustomerGroupData> SetCustomerGroupActiveAsync(
        string id,
        bool isActive,
        string expectedUpdatedAt,
        CancellationToken cancellationToken = default) =>
        PatchAsync<ActiveStatusUpdateRequest, CustomerGroupData>(
            $"/api/customer-groups/{RequireId(id)}",
            new ActiveStatusUpdateRequest(isActive, expectedUpdatedAt),
            cancellationToken);

    public Task<CustomerAddressData> CreateCustomerAddressAsync(
        string customerId,
        CustomerAddressCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<CustomerAddressCreateRequest, CustomerAddressData>(
            $"/api/customers/{RequireId(customerId)}/addresses",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerAddressData> UpdateCustomerAddressAsync(
        string customerId,
        string addressId,
        CustomerAddressUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<CustomerAddressUpdateRequest, CustomerAddressData>(
            $"/api/customers/{RequireId(customerId)}/addresses/{RequireId(addressId)}",
            request,
            cancellationToken);

    public Task<CustomerMediaListData> ListCustomerMediaAsync(
        string customerId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<CustomerMediaListData>(
            $"/api/customers/{RequireId(customerId)}/media",
            RequireToken(),
            cancellationToken);

    public Task<CustomerMediaPrepareData> PrepareCustomerMediaAsync(
        string customerId,
        CustomerMediaPrepareRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<CustomerMediaPrepareRequest, CustomerMediaPrepareData>(
            $"/api/customers/{RequireId(customerId)}/media",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerMediaMutationData> FinalizeCustomerMediaAsync(
        string customerId,
        CustomerMediaFinalizeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<CustomerMediaFinalizeRequest, CustomerMediaMutationData>(
            $"/api/customers/{RequireId(customerId)}/media",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<CustomerIdentifyResult> IdentifyCustomersAsync(
        CustomerBulkSourceRow[] rows,
        CancellationToken cancellationToken = default) =>
        apiClient.PostDataAsync<object, CustomerIdentifyResult>(
            "/api/customers/identify",
            new { rows },
            RequireToken(),
            cancellationToken);

    public Task<CustomerBulkResult> PreviewCustomerImportAsync(
        CustomerBulkRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostDataAsync<CustomerBulkRequest, CustomerBulkResult>(
            "/api/customers/import",
            request with { DryRun = true },
            RequireToken(),
            cancellationToken);

    public Task<CustomerBulkResult> ApplyCustomerImportAsync(
        CustomerBulkRequest request,
        string operationKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<CustomerBulkRequest, CustomerBulkResult>(
            "/api/customers/import",
            request with { DryRun = false },
            operationKey,
            cancellationToken);

    public Task<CustomerBulkResult> PreviewCustomerBulkUpdateAsync(
        CustomerBulkRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PatchDataAsync<CustomerBulkRequest, CustomerBulkResult>(
            "/api/customers/bulk-update",
            request with { DryRun = true },
            RequireToken(),
            cancellationToken);

    public Task<CustomerBulkResult> ApplyCustomerBulkUpdateAsync(
        CustomerBulkRequest request,
        string operationKey,
        CancellationToken cancellationToken = default)
    {
        RequireIdempotencyKey(operationKey);
        return apiClient.PatchIdempotentDataAsync<CustomerBulkRequest, CustomerBulkResult>(
            "/api/customers/bulk-update",
            request with { DryRun = false },
            operationKey,
            RequireToken(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<VietnamProvinceData>> ListVietnamProvincesAsync(CancellationToken cancellationToken = default)
    {
        var data = await apiClient.GetDataAsync<VietnamAdministrativeReferenceData>(
            "/api/reference/vietnam-administrative-units",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
        return data.Provinces;
    }

    public async Task<IReadOnlyList<VietnamWardData>> ListVietnamWardsAsync(
        string provinceCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            return [];
        }

        var data = await apiClient.GetDataAsync<VietnamAdministrativeReferenceData>(
            $"/api/reference/vietnam-administrative-units?provinceCode={Uri.EscapeDataString(provinceCode.Trim())}",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);
        return data.Wards;
    }

    public async Task<IReadOnlyList<SupplierData>> ListSuppliersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierData[]>(
            "/api/suppliers?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<SupplierData> CreateSupplierAsync(
        SupplierCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<SupplierCreateRequest, SupplierData>(
            "/api/suppliers",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<SupplierData> UpdateSupplierAsync(
        string id,
        SupplierUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<SupplierUpdateRequest, SupplierData>(
            $"/api/suppliers/{RequireId(id)}",
            request,
            cancellationToken);

    public Task<SupplierData> SetSupplierActiveAsync(
        string id,
        bool isActive,
        string expectedUpdatedAt,
        CancellationToken cancellationToken = default) =>
        PatchAsync<ActiveStatusUpdateRequest, SupplierData>(
            $"/api/suppliers/{RequireId(id)}",
            new ActiveStatusUpdateRequest(isActive, expectedUpdatedAt),
            cancellationToken);

    public async Task<IReadOnlyList<SupplierContactData>> ListSupplierContactsAsync(
        string supplierId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierContactData[]>(
            $"/api/suppliers/{RequireId(supplierId)}/contacts",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<SupplierContactData> CreateSupplierContactAsync(
        string supplierId,
        SupplierContactCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<SupplierContactCreateRequest, SupplierContactData>(
            $"/api/suppliers/{RequireId(supplierId)}/contacts",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<SupplierContactData> UpdateSupplierContactAsync(
        string supplierId,
        string contactId,
        SupplierContactUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<SupplierContactUpdateRequest, SupplierContactData>(
            $"/api/suppliers/{RequireId(supplierId)}/contacts/{RequireId(contactId)}",
            request,
            cancellationToken);

    public async Task<IReadOnlyList<SupplierAddressData>> ListSupplierAddressesAsync(
        string supplierId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierAddressData[]>(
            $"/api/suppliers/{RequireId(supplierId)}/addresses",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<SupplierAddressData> CreateSupplierAddressAsync(
        string supplierId,
        SupplierAddressCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<SupplierAddressCreateRequest, SupplierAddressData>(
            $"/api/suppliers/{RequireId(supplierId)}/addresses",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<SupplierAddressData> UpdateSupplierAddressAsync(
        string supplierId,
        string addressId,
        SupplierAddressUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<SupplierAddressUpdateRequest, SupplierAddressData>(
            $"/api/suppliers/{RequireId(supplierId)}/addresses/{RequireId(addressId)}",
            request,
            cancellationToken);

    public async Task<IReadOnlyList<SupplierPaymentTermData>> ListSupplierPaymentTermsAsync(
        string supplierId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<SupplierPaymentTermData[]>(
            $"/api/suppliers/{RequireId(supplierId)}/payment-terms",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<SupplierPaymentTermData> CreateSupplierPaymentTermAsync(
        string supplierId,
        SupplierPaymentTermCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<SupplierPaymentTermCreateRequest, SupplierPaymentTermData>(
            $"/api/suppliers/{RequireId(supplierId)}/payment-terms",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<SupplierPaymentTermData> UpdateSupplierPaymentTermAsync(
        string supplierId,
        string paymentTermId,
        SupplierPaymentTermUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<SupplierPaymentTermUpdateRequest, SupplierPaymentTermData>(
            $"/api/suppliers/{RequireId(supplierId)}/payment-terms/{RequireId(paymentTermId)}",
            request,
            cancellationToken);

    private Task<TResponse> PostCreateAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireIdempotencyKey(idempotencyKey);
        return apiClient.PostIdempotentDataAsync<TRequest, TResponse>(
            path,
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    private Task<TResponse> PatchAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken cancellationToken) =>
        apiClient.PatchDataAsync<TRequest, TResponse>(
            path,
            request,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private void RequireIdempotencyKey(string key)
    {
        if (!idempotencyKeys.IsValid(key))
        {
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(key));
        }
    }

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã dữ liệu không hợp lệ.", nameof(value));
}
