using System.Text.Json.Serialization;

namespace CongTy.Contracts;

public sealed record PurchaseOrderData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("number")] public string? Number { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "draft";
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("supplierCode")] public string? SupplierCode { get; init; }
    [JsonPropertyName("supplierName")] public string SupplierName { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseCode")] public string? WarehouseCode { get; init; }
    [JsonPropertyName("warehouseName")] public string WarehouseName { get; init; } = string.Empty;
    [JsonPropertyName("placedAt")] public string PlacedAt { get; init; } = string.Empty;
    [JsonPropertyName("expectedAt")] public string? ExpectedAt { get; init; }
    [JsonPropertyName("supplierReference")] public string? SupplierReference { get; init; }
    [JsonPropertyName("currency")] public string Currency { get; init; } = "VND";
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("subtotal")] public string? Subtotal { get; init; }
    [JsonPropertyName("discountTotal")] public string? DiscountTotal { get; init; }
    [JsonPropertyName("taxTotal")] public string? TaxTotal { get; init; }
    [JsonPropertyName("total")] public string? Total { get; init; }
    [JsonPropertyName("priceStatus")] public string? PriceStatus { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("lineCount")] public int LineCount { get; init; }
    [JsonPropertyName("receiptCount")] public int ReceiptCount { get; init; }
    [JsonPropertyName("receivedQuantityTotal")] public string? ReceivedQuantityTotal { get; init; }
    [JsonPropertyName("acceptedQuantityTotal")] public string? AcceptedQuantityTotal { get; init; }
    [JsonPropertyName("rejectedQuantityTotal")] public string? RejectedQuantityTotal { get; init; }
    [JsonPropertyName("shortageClosedQuantityTotal")] public string? ShortageClosedQuantityTotal { get; init; }
    [JsonPropertyName("remainingQuantityTotal")] public string? RemainingQuantityTotal { get; init; }
    [JsonPropertyName("submittedAt")] public string? SubmittedAt { get; init; }
    [JsonPropertyName("approvedAt")] public string? ApprovedAt { get; init; }
    [JsonPropertyName("cancelledAt")] public string? CancelledAt { get; init; }
    [JsonPropertyName("cancellationReason")] public string? CancellationReason { get; init; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
    [JsonPropertyName("lines")] public PurchaseOrderLineData[] Lines { get; init; } = [];
}

public sealed record PurchaseOrderLineData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("lineNumber")] public int LineNumber { get; init; }
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("skuCode")] public string SkuCode { get; init; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; init; } = string.Empty;
    [JsonPropertyName("unitId")] public string UnitId { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("conversionToBase")] public string ConversionToBase { get; init; } = "1";
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "0";
    [JsonPropertyName("baseQuantity")] public string BaseQuantity { get; init; } = "0";
    [JsonPropertyName("receivedQuantity")] public string? ReceivedQuantity { get; init; }
    [JsonPropertyName("acceptedQuantity")] public string? AcceptedQuantity { get; init; }
    [JsonPropertyName("rejectedQuantity")] public string? RejectedQuantity { get; init; }
    [JsonPropertyName("shortageClosedQuantity")] public string? ShortageClosedQuantity { get; init; }
    [JsonPropertyName("remainingQuantity")] public string? RemainingQuantity { get; init; }
    [JsonPropertyName("unitPrice")] public string? UnitPrice { get; init; }
    [JsonPropertyName("discountMode")] public string? DiscountMode { get; init; }
    [JsonPropertyName("discountValue")] public string? DiscountValue { get; init; }
    [JsonPropertyName("discountAmount")] public string? DiscountAmount { get; init; }
    [JsonPropertyName("taxRate")] public string? TaxRate { get; init; }
    [JsonPropertyName("taxAmount")] public string? TaxAmount { get; init; }
    [JsonPropertyName("lineTotal")] public string? LineTotal { get; init; }
    [JsonPropertyName("priceStatus")] public string? PriceStatus { get; init; }
    [JsonPropertyName("purchasePriceSource")] public string? PurchasePriceSource { get; init; }
    [JsonPropertyName("priceOverrideReason")] public string? PriceOverrideReason { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
}

public sealed record PurchaseOrderSkuEligibilityData
{
    [JsonPropertyName("selectable")] public bool Selectable { get; init; }
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}

public sealed record PurchaseOrderSkuSearchOptionData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("productId")] public string ProductId { get; init; } = string.Empty;
    [JsonPropertyName("productCode")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("variantName")] public string VariantName { get; init; } = string.Empty;
    [JsonPropertyName("barcode")] public string? Barcode { get; init; }
    [JsonPropertyName("unitId")] public string? UnitId { get; init; }
    [JsonPropertyName("unitCode")] public string? UnitCode { get; init; }
    [JsonPropertyName("unitName")] public string? UnitName { get; init; }
    [JsonPropertyName("conversionToBase")] public string? ConversionToBase { get; init; }
    [JsonPropertyName("allowsFractional")] public bool? AllowsFractional { get; init; }
    [JsonPropertyName("eligibility")] public PurchaseOrderSkuEligibilityData Eligibility { get; init; } = new();
}

public sealed record SupplierPurchasePriceData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("supplierCode")] public string SupplierCode { get; init; } = string.Empty;
    [JsonPropertyName("supplierName")] public string SupplierName { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("sku")] public string Sku { get; init; } = string.Empty;
    [JsonPropertyName("variantName")] public string VariantName { get; init; } = string.Empty;
    [JsonPropertyName("productCode")] public string ProductCode { get; init; } = string.Empty;
    [JsonPropertyName("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonPropertyName("unitId")] public string UnitId { get; init; } = string.Empty;
    [JsonPropertyName("unitCode")] public string UnitCode { get; init; } = string.Empty;
    [JsonPropertyName("unitName")] public string UnitName { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("unitPrice")] public string UnitPrice { get; init; } = "0";
    [JsonPropertyName("minQuantity")] public string MinQuantity { get; init; } = "0";
    [JsonPropertyName("effectiveFrom")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("effectiveTo")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("supplierSku")] public string? SupplierSku { get; init; }
    [JsonPropertyName("sourceReference")] public string? SourceReference { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("isActive")] public bool IsActive { get; init; }
    [JsonPropertyName("revision")] public string Revision { get; init; } = string.Empty;
    [JsonPropertyName("createdAt")] public string CreatedAt { get; init; } = string.Empty;
    [JsonPropertyName("updatedAt")] public string UpdatedAt { get; init; } = string.Empty;
}

public sealed record SupplierPurchasePriceRequest
{
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("unitId")] public string UnitId { get; init; } = string.Empty;
    [JsonPropertyName("unitPrice")] public string UnitPrice { get; init; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("minQuantity")] public string MinQuantity { get; init; } = "0";
    [JsonPropertyName("effectiveFrom")] public string EffectiveFrom { get; init; } = string.Empty;
    [JsonPropertyName("effectiveTo")] public string? EffectiveTo { get; init; }
    [JsonPropertyName("supplierSku")] public string? SupplierSku { get; init; }
    [JsonPropertyName("sourceReference")] public string? SourceReference { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("isActive")] public bool IsActive { get; init; } = true;
    [JsonPropertyName("expectedRevision"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExpectedRevision { get; init; }
}

public sealed record SupplierPurchasePriceResolutionData
{
    [JsonPropertyName("status")] public string Status { get; init; } = "NOT_FOUND";
    [JsonPropertyName("price")] public SupplierPurchasePriceData? Price { get; init; }
}

public sealed record SupplierPurchasePriceResolveRequest(
    [property: JsonPropertyName("supplierId")] string SupplierId,
    [property: JsonPropertyName("variantId")] string VariantId,
    [property: JsonPropertyName("unitId")] string UnitId,
    [property: JsonPropertyName("quantity")] string Quantity,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("orderDate")] string OrderDate);

public sealed record PurchaseOrderDraftLineRequest
{
    [JsonPropertyName("variantId")] public string VariantId { get; init; } = string.Empty;
    [JsonPropertyName("quantity")] public string Quantity { get; init; } = "1";
    [JsonPropertyName("unitPrice"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? UnitPrice { get; init; }
    [JsonPropertyName("discountMode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? DiscountMode { get; init; }
    [JsonPropertyName("discountValue"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? DiscountValue { get; init; }
    [JsonPropertyName("taxRate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? TaxRate { get; init; }
    [JsonPropertyName("priceOverrideReason"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? PriceOverrideReason { get; init; }
    [JsonPropertyName("note")] public string Note { get; init; } = string.Empty;
}

public sealed record PurchaseOrderDraftRequest
{
    [JsonPropertyName("supplierId")] public string SupplierId { get; init; } = string.Empty;
    [JsonPropertyName("warehouseId")] public string WarehouseId { get; init; } = string.Empty;
    [JsonPropertyName("orderDate")] public string OrderDate { get; init; } = string.Empty;
    [JsonPropertyName("expectedDate")] public string? ExpectedDate { get; init; }
    [JsonPropertyName("supplierReference")] public string? SupplierReference { get; init; }
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; init; } = "VND";
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("expectedRevision"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ExpectedRevision { get; init; }
    [JsonPropertyName("lines")] public PurchaseOrderDraftLineRequest[] Lines { get; init; } = [];
}

public sealed record PurchaseOrderActionRequest(
    [property: JsonPropertyName("expectedRevision")] string ExpectedRevision,
    [property: JsonPropertyName("reason"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Reason = null);

public sealed record GoodsReceiptPurchaseOrderSummaryData
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("documentNumber")] public string? DocumentNumber { get; init; }
    [JsonPropertyName("receiptDate")] public string ReceiptDate { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("supplierDeliveryReference")] public string? SupplierDeliveryReference { get; init; }
    [JsonPropertyName("receivedQuantityTotal")] public string ReceivedQuantityTotal { get; init; } = "0";
}
