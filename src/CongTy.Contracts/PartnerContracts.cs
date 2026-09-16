using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record CustomerGroupData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record CustomerData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("group_id")] public string? GroupId { get; init; }
    [JsonPropertyName("group_name")] public string? GroupName { get; init; }
    [JsonPropertyName("responsible_employee_id")] public string? ResponsibleEmployeeId { get; init; }
    [JsonPropertyName("responsible_employee_name")] public string? ResponsibleEmployeeName { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("tax_code")] public string? TaxCode { get; init; }
    [JsonPropertyName("payment_terms_days")] public int PaymentTermsDays { get; init; }
    [JsonPropertyName("credit_limit")] public string CreditLimit { get; init; } = "0";
    [JsonPropertyName("notes")] public string? Notes { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record CustomerAddressData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("customer_id")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; init; } = string.Empty;
    [JsonPropertyName("recipient_name")] public string? RecipientName { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("address_line1")] public string AddressLine1 { get; init; } = string.Empty;
    [JsonPropertyName("address_line2")] public string? AddressLine2 { get; init; }
    [JsonPropertyName("ward")] public string? Ward { get; init; }
    [JsonPropertyName("district")] public string? District { get; init; }
    [JsonPropertyName("province")] public string? Province { get; init; }
    [JsonPropertyName("postal_code")] public string? PostalCode { get; init; }
    [JsonPropertyName("country_code")] public string CountryCode { get; init; } = "VN";
    [JsonPropertyName("location_url")] public string? LocationUrl { get; init; }
    [JsonPropertyName("is_default")] public bool IsDefault { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record SupplierData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("tax_id")] public string? TaxId { get; init; }
    [JsonPropertyName("bank_account")] public string? BankAccount { get; init; }
    [JsonPropertyName("bank_name")] public string? BankName { get; init; }
    [JsonPropertyName("avg_delivery_days")] public int? AvgDeliveryDays { get; init; }
    [JsonPropertyName("purchase_owner_employee_id")] public string? PurchaseOwnerEmployeeId { get; init; }
    [JsonPropertyName("purchase_owner_employee_name")] public string? PurchaseOwnerEmployeeName { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record SupplierContactData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("supplier_id")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("contact_name")] public string ContactName { get; init; } = string.Empty;
    [JsonPropertyName("contact_title")] public string? ContactTitle { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("is_primary")] public bool IsPrimary { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record SupplierAddressData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("supplier_id")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("address_type")] public string AddressType { get; init; } = "business";
    [JsonPropertyName("street")] public string Street { get; init; } = string.Empty;
    [JsonPropertyName("city")] public string? City { get; init; }
    [JsonPropertyName("province")] public string? Province { get; init; }
    [JsonPropertyName("postal_code")] public string? PostalCode { get; init; }
    [JsonPropertyName("country")] public string Country { get; init; } = "Việt Nam";
    [JsonPropertyName("is_primary")] public bool IsPrimary { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record SupplierPaymentTermData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("supplier_id")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("payment_method")] public string PaymentMethod { get; init; } = string.Empty;
    [JsonPropertyName("term_days")] public int? TermDays { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("is_primary")] public bool IsPrimary { get; init; }
    [JsonPropertyName("is_active")] public bool IsActive { get; init; }
    [JsonPropertyName("created_at")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updated_at")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record CustomerGroupCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description);

public sealed record CustomerGroupUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record CustomerCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("groupId")] string? GroupId,
    [property: JsonPropertyName("responsibleEmployeeId")] string? ResponsibleEmployeeId,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("taxCode")] string? TaxCode,
    [property: JsonPropertyName("paymentTermsDays")] int PaymentTermsDays,
    [property: JsonPropertyName("creditLimit")] string CreditLimit,
    [property: JsonPropertyName("notes")] string? Notes);

public sealed record CustomerUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("groupId")] string? GroupId,
    [property: JsonPropertyName("responsibleEmployeeId")] string? ResponsibleEmployeeId,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("taxCode")] string? TaxCode,
    [property: JsonPropertyName("paymentTermsDays")] int PaymentTermsDays,
    [property: JsonPropertyName("creditLimit")] string CreditLimit,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record CustomerAddressCreateRequest(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("recipientName")] string? RecipientName,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("locationUrl")] string? LocationUrl,
    [property: JsonPropertyName("addressLine1")] string AddressLine1,
    [property: JsonPropertyName("addressLine2")] string? AddressLine2,
    [property: JsonPropertyName("ward")] string? Ward,
    [property: JsonPropertyName("district")] string? District,
    [property: JsonPropertyName("province")] string? Province,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("countryCode")] string CountryCode,
    [property: JsonPropertyName("isDefault")] bool IsDefault);

public sealed record CustomerAddressUpdateRequest(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("recipientName")] string? RecipientName,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("locationUrl")] string? LocationUrl,
    [property: JsonPropertyName("addressLine1")] string AddressLine1,
    [property: JsonPropertyName("addressLine2")] string? AddressLine2,
    [property: JsonPropertyName("ward")] string? Ward,
    [property: JsonPropertyName("district")] string? District,
    [property: JsonPropertyName("province")] string? Province,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("countryCode")] string CountryCode,
    [property: JsonPropertyName("isDefault")] bool IsDefault,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record SupplierCreateRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("taxId")] string? TaxId,
    [property: JsonPropertyName("bankAccount")] string? BankAccount,
    [property: JsonPropertyName("bankName")] string? BankName,
    [property: JsonPropertyName("avgDeliveryDays")] int? AvgDeliveryDays,
    [property: JsonPropertyName("purchaseOwnerEmployeeId")] string? PurchaseOwnerEmployeeId);

public sealed record SupplierUpdateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("taxId")] string? TaxId,
    [property: JsonPropertyName("bankAccount")] string? BankAccount,
    [property: JsonPropertyName("bankName")] string? BankName,
    [property: JsonPropertyName("avgDeliveryDays")] int? AvgDeliveryDays,
    [property: JsonPropertyName("purchaseOwnerEmployeeId")] string? PurchaseOwnerEmployeeId,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record SupplierContactCreateRequest(
    [property: JsonPropertyName("contactName")] string ContactName,
    [property: JsonPropertyName("contactTitle")] string? ContactTitle,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary,
    [property: JsonPropertyName("isActive")] bool IsActive = true);

public sealed record SupplierContactUpdateRequest(
    [property: JsonPropertyName("contactName")] string ContactName,
    [property: JsonPropertyName("contactTitle")] string? ContactTitle,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record SupplierAddressCreateRequest(
    [property: JsonPropertyName("addressType")] string AddressType,
    [property: JsonPropertyName("street")] string Street,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("province")] string? Province,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary,
    [property: JsonPropertyName("isActive")] bool IsActive = true);

public sealed record SupplierAddressUpdateRequest(
    [property: JsonPropertyName("addressType")] string AddressType,
    [property: JsonPropertyName("street")] string Street,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("province")] string? Province,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record SupplierPaymentTermCreateRequest(
    [property: JsonPropertyName("paymentMethod")] string PaymentMethod,
    [property: JsonPropertyName("termDays")] int? TermDays,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary,
    [property: JsonPropertyName("isActive")] bool IsActive = true);

public sealed record SupplierPaymentTermUpdateRequest(
    [property: JsonPropertyName("paymentMethod")] string PaymentMethod,
    [property: JsonPropertyName("termDays")] int? TermDays,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isPrimary")] bool IsPrimary,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("expectedUpdatedAt")] string ExpectedUpdatedAt);

public sealed record CustomerBulkSourceRow
{
    [JsonPropertyName("rowNumber")] public int RowNumber { get; init; }
    [JsonPropertyName("cells")] public string[] Cells { get; init; } = [];
    [JsonPropertyName("expectedUpdatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpectedUpdatedAt { get; init; }
}

public sealed record CustomerBulkRequest
{
    [JsonPropertyName("dryRun")] public bool DryRun { get; init; }
    [JsonPropertyName("mappings")] public string[] Mappings { get; init; } = [];
    [JsonPropertyName("rows")] public CustomerBulkSourceRow[] Rows { get; init; } = [];
}

public sealed record CustomerBulkRowError
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}

public sealed record CustomerBulkChange
{
    [JsonPropertyName("field")] public string Field { get; init; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; init; } = string.Empty;
    [JsonPropertyName("oldValue")] public string OldValue { get; init; } = string.Empty;
    [JsonPropertyName("newValue")] public string NewValue { get; init; } = string.Empty;
}

public sealed record CustomerBulkResultRow
{
    [JsonPropertyName("rowNumber")] public int RowNumber { get; init; }
    [JsonPropertyName("customerCode")] public string CustomerCode { get; init; } = string.Empty;
    [JsonPropertyName("customerName")] public string CustomerName { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string? CustomerId { get; init; }
    [JsonPropertyName("expectedUpdatedAt")] public string? ExpectedUpdatedAt { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("errors")] public CustomerBulkRowError[] Errors { get; init; } = [];
    [JsonPropertyName("warnings")] public CustomerBulkRowError[] Warnings { get; init; } = [];
    [JsonPropertyName("changes")] public CustomerBulkChange[] Changes { get; init; } = [];
    [JsonPropertyName("cells")] public JsonElement[] Cells { get; init; } = [];
}

public sealed record CustomerBulkResult
{
    [JsonPropertyName("created")] public int? Created { get; init; }
    [JsonPropertyName("updated")] public int? Updated { get; init; }
    [JsonPropertyName("ready")] public int? Ready { get; init; }
    [JsonPropertyName("skipped")] public int Skipped { get; init; }
    [JsonPropertyName("unchanged")] public int? Unchanged { get; init; }
    [JsonPropertyName("rows")] public CustomerBulkResultRow[] Rows { get; init; } = [];
    [JsonPropertyName("operationKey")] public string? OperationKey { get; init; }
}

public sealed record CustomerIdentifyResult
{
    [JsonPropertyName("identified")] public int Identified { get; init; }
    [JsonPropertyName("skipped")] public int Skipped { get; init; }
    [JsonPropertyName("rows")] public CustomerBulkResultRow[] Rows { get; init; } = [];
}

public sealed record CustomerMediaData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
    [JsonPropertyName("sourceApp")] public string SourceApp { get; init; } = string.Empty;
    [JsonPropertyName("mimeType")] public string MimeType { get; init; } = string.Empty;
    [JsonPropertyName("actualByteSize")] public long? ActualByteSize { get; init; }
    [JsonPropertyName("width")] public int? Width { get; init; }
    [JsonPropertyName("height")] public int? Height { get; init; }
    [JsonPropertyName("capturedAt")] public string? CapturedAt { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("viewUrl")] public string? ViewUrl { get; init; }
}

public sealed record CustomerMediaListData
{
    [JsonPropertyName("media")] public CustomerMediaData[] Media { get; init; } = [];
    [JsonPropertyName("maxPhotos")] public int MaxPhotos { get; init; }
}

public sealed record CustomerMediaPrepareRequest(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("clientUploadId")] string ClientUploadId,
    [property: JsonPropertyName("mimeType")] string MimeType,
    [property: JsonPropertyName("byteSize")] long ByteSize);

public sealed record CustomerMediaPrepareData
{
    [JsonPropertyName("mediaId")] public string MediaId { get; init; } = string.Empty;
    [JsonPropertyName("putUrl")] public string PutUrl { get; init; } = string.Empty;
    [JsonPropertyName("mimeType")] public string MimeType { get; init; } = string.Empty;
    [JsonPropertyName("expiresIn")] public int ExpiresIn { get; init; }
}

public sealed record CustomerMediaFinalizeRequest(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("mediaId")] string MediaId,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

public sealed record CustomerMediaMutationData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; init; } = string.Empty;
}

public sealed record CustomerSalesSummaryData
{
    [JsonPropertyName("period")] public string Period { get; init; } = "90d";
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("revenue")] public string Revenue { get; init; } = "0";
    [JsonPropertyName("orderCount")] public string OrderCount { get; init; } = "0";
    [JsonPropertyName("lastPurchaseAt")] public string? LastPurchaseAt { get; init; }
}

public sealed record CustomerReceivableSummaryData
{
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("balance")] public string Balance { get; init; } = "0";
    [JsonPropertyName("openAmount")] public string OpenAmount { get; init; } = "0";
    [JsonPropertyName("openDocumentCount")] public string OpenDocumentCount { get; init; } = "0";
    [JsonPropertyName("updatedAt")] public string? UpdatedAt { get; init; }
}

public sealed record CustomerProfilePermissionsData
{
    [JsonPropertyName("sales")] public bool Sales { get; init; }
    [JsonPropertyName("receivable")] public bool Receivable { get; init; }
}

public sealed record CustomerProfileOverviewData
{
    [JsonPropertyName("customer")] public CustomerData Customer { get; init; } = new();
    [JsonPropertyName("period")] public string Period { get; init; } = "90d";
    [JsonPropertyName("sales")] public CustomerSalesSummaryData? Sales { get; init; }
    [JsonPropertyName("receivable")] public CustomerReceivableSummaryData? Receivable { get; init; }
    [JsonPropertyName("permissions")] public CustomerProfilePermissionsData Permissions { get; init; } = new();
}


public sealed record CustomerPurchasedItemData
{
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("totalQuantity")] public string TotalQuantity { get; init; } = "0";
    [JsonPropertyName("revenue")] public string Revenue { get; init; } = "0";
    [JsonPropertyName("purchaseCount")] public string PurchaseCount { get; init; } = "0";
    [JsonPropertyName("lastUnitPrice")] public string LastUnitPrice { get; init; } = "0";
    [JsonPropertyName("lastPurchaseAt")] public string? LastPurchaseAt { get; init; }
}

public sealed record CustomerPurchasedItemsPageData
{
    [JsonPropertyName("period")] public string Period { get; init; } = "90d";
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("search")] public string Search { get; init; } = string.Empty;
    [JsonPropertyName("limit")] public int Limit { get; init; }
    [JsonPropertyName("offset")] public int Offset { get; init; }
    [JsonPropertyName("total")] public string Total { get; init; } = "0";
    [JsonPropertyName("items")] public CustomerPurchasedItemData[] Items { get; init; } = [];
}

public sealed record CustomerSalesOrderData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string? Number { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("deliveryStatus")] public string DeliveryStatus { get; init; } = string.Empty;
    [JsonPropertyName("settlementStatus")] public string SettlementStatus { get; init; } = string.Empty;
    [JsonPropertyName("receivableRemainingAmount")] public string ReceivableRemainingAmount { get; init; } = "0";
    [JsonPropertyName("confirmedAt")] public string? ConfirmedAt { get; init; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("total")] public string? Total { get; init; }
}

public sealed record CustomerReceivableDocumentData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentNumber")] public string SourceDocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentDate")] public string SourceDocumentDate { get; init; } = string.Empty;
    [JsonPropertyName("originalAmount")] public string OriginalAmount { get; init; } = "0";
    [JsonPropertyName("allocatedAmount")] public string AllocatedAmount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
}

public sealed record CustomerPaymentHistoryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("documentNumber")] public string DocumentNumber { get; init; } = string.Empty;
    [JsonPropertyName("paymentDate")] public string PaymentDate { get; init; } = string.Empty;
    [JsonPropertyName("paymentMethod")] public string PaymentMethod { get; init; } = string.Empty;
    [JsonPropertyName("originalAmount")] public string OriginalAmount { get; init; } = "0";
    [JsonPropertyName("allocatedAmount")] public string AllocatedAmount { get; init; } = "0";
    [JsonPropertyName("remainingAmount")] public string RemainingAmount { get; init; } = "0";
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
}

public sealed record CustomerDeliveryAttemptSummaryData
{
    [JsonPropertyName("count")] public string Count { get; init; } = "0";
    [JsonPropertyName("deliveredFullCount")] public string DeliveredFullCount { get; init; } = "0";
    [JsonPropertyName("deliveredPartialCount")] public string DeliveredPartialCount { get; init; } = "0";
    [JsonPropertyName("failedCount")] public string FailedCount { get; init; } = "0";
    [JsonPropertyName("rescheduledCount")] public string RescheduledCount { get; init; } = "0";
    [JsonPropertyName("latestResult")] public string? LatestResult { get; init; }
    [JsonPropertyName("latestAttemptAt")] public string? LatestAttemptAt { get; init; }
    [JsonPropertyName("latestNote")] public string? LatestNote { get; init; }
    [JsonPropertyName("latestRescheduledFor")] public string? LatestRescheduledFor { get; init; }
}

public sealed record CustomerDeliveryHistoryItemData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string? Number { get; init; }
    [JsonPropertyName("salesOrderId")] public string SalesOrderId { get; init; } = string.Empty;
    [JsonPropertyName("salesOrderNumber")] public string? SalesOrderNumber { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("handoverMode")] public string HandoverMode { get; init; } = string.Empty;
    [JsonPropertyName("requestedDeliveryDate")] public string? RequestedDeliveryDate { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("lineCount")] public int LineCount { get; init; }
    [JsonPropertyName("createdAt")] public string? CreatedAt { get; init; }
    [JsonPropertyName("updatedAt")] public string? UpdatedAt { get; init; }
    [JsonPropertyName("attempts")] public CustomerDeliveryAttemptSummaryData? Attempts { get; init; }
}

public sealed record CustomerReturnHistoryItemData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string? Number { get; init; }
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string? WarehouseName { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("lineCount")] public int LineCount { get; init; }
    [JsonPropertyName("acceptedLineCount")] public int AcceptedLineCount { get; init; }
    [JsonPropertyName("createdAt")] public string? CreatedAt { get; init; }
    [JsonPropertyName("receivedAt")] public string? ReceivedAt { get; init; }
    [JsonPropertyName("cancelledAt")] public string? CancelledAt { get; init; }
    [JsonPropertyName("cancellationReason")] public string? CancellationReason { get; init; }
}

public sealed record CustomerHistoryPageData<T>
{
    [JsonPropertyName("offset")] public int Offset { get; init; }
    [JsonPropertyName("limit")] public int Limit { get; init; }
    [JsonPropertyName("hasPrevious")] public bool HasPrevious { get; init; }
    [JsonPropertyName("hasNext")] public bool HasNext { get; init; }
    [JsonPropertyName("items")] public T[] Items { get; init; } = [];
}

public sealed record CustomerDeliveryReturnsPermissionsData
{
    [JsonPropertyName("deliveryOrders")] public bool DeliveryOrders { get; init; }
    [JsonPropertyName("deliveryAttempts")] public bool DeliveryAttempts { get; init; }
    [JsonPropertyName("returns")] public bool Returns { get; init; }
}

public sealed record CustomerDeliveryReturnsData
{
    [JsonPropertyName("permissions")] public CustomerDeliveryReturnsPermissionsData Permissions { get; init; } = new();
    [JsonPropertyName("deliveries")] public CustomerHistoryPageData<CustomerDeliveryHistoryItemData> Deliveries { get; init; } = new();
    [JsonPropertyName("returns")] public CustomerHistoryPageData<CustomerReturnHistoryItemData> Returns { get; init; } = new();
}
