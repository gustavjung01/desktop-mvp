using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Sales;

namespace CongTy.UnitTests;

[TestClass]
public sealed class SalesTests
{
    [TestMethod]
    public void Presentation_MatchesWebLaneAndWorkStage()
    {
        var manual = new SalesOrderData
        {
            Status = "confirmed",
            DeliveryMode = "DELIVERY",
            DeliveryExecutionMode = "MANUAL",
            FulfillmentStatus = "issued",
            DeliveryStatus = "pending"
        };

        Assert.AreEqual("manual", SalesPresentation.Lane(manual));
        Assert.AreEqual("Giao thủ công", SalesPresentation.LaneLabel(manual));
        Assert.AreEqual("waiting_delivery", SalesPresentation.Stage(manual));
        Assert.AreEqual("Đã xuất kho", SalesPresentation.StageLabel(manual));

        var completed = manual with { Status = "closed" };
        Assert.AreEqual("completed", SalesPresentation.Stage(completed));
    }

    [TestMethod]
    public void Presentation_PreservesCanonicalSalesLanguage()
    {
        Assert.AreEqual("Đã xác nhận", SalesPresentation.OrderStatus("confirmed"));
        Assert.AreEqual("Bán chịu theo hạn mức", SalesPresentation.CollectionPolicy("CREDIT_TERMS"));
        Assert.AreEqual("Đã thanh toán một phần", SalesPresentation.SettlementStatus("partially_paid"));
        Assert.AreEqual("Đã giữ đủ hàng", SalesPresentation.FulfillmentStatus("reserved"));
    }

    [TestMethod]
    public void ListContract_UsesOrderLevelTotalWithoutLoadingVersions()
    {
        var order=JsonSerializer.Deserialize<SalesOrderData>("""{"id":"00000000-0000-0000-0000-000000000001","status":"confirmed","total":"125000","versions":[]}""")!;

        Assert.AreEqual("125000",order.Total);
        Assert.IsNull(SalesPresentation.ActiveVersion(order));
        Assert.AreEqual("125.000 ₫",SalesPresentation.Money(order.Total));
    }

    [TestMethod]
    public void Presentation_SeparatesActiveAndPreparingStages()
    {
        var active=new SalesOrderData { Status="draft", FulfillmentStatus="unallocated", DeliveryStatus="pending" };
        var preparing=new SalesOrderData { Status="confirmed", FulfillmentStatus="reserved", DeliveryStatus="pending" };

        Assert.AreEqual("active",SalesPresentation.Stage(active));
        Assert.AreEqual("preparing",SalesPresentation.Stage(preparing));
    }

    [TestMethod]
    public void FulfillmentContract_MapsCanonicalStockObservationAndFormatsQuantity()
    {
        var order=JsonSerializer.Deserialize<SalesOrderData>("""{"fulfillment":{"status":"reserved","allowBackorder":false,"lines":[{"id":"f1","salesOrderLineId":"l1","baseUnitCode":"Lon","warehouseOnHandBaseQuantity":"12.000000000000","warehouseHeldByOthersBaseQuantity":"2.000000000000","warehouseAvailableBaseQuantity":"10.500000000000"}]}}""")!;
        var line=order.Fulfillment!.Lines.Single();

        Assert.AreEqual("l1",line.SalesOrderLineId);
        Assert.AreEqual("12",SalesPresentation.Quantity(line.WarehouseOnHandBaseQuantity));
        Assert.AreEqual("10,5",SalesPresentation.Quantity(line.WarehouseAvailableBaseQuantity));
    }

    [TestMethod]
    public void SkuPreviewContract_MapsHeldQuantity()
    {
        var preview=JsonSerializer.Deserialize<SalesOrderSkuSearchPreviewData>("""{"id":"v1","inventoryPreview":{"status":"TRACKED","onHandQuantity":"139","availableQuantity":"129","heldQuantity":"10","unitCode":"CHAI","unitName":"Chai"}}""")!;

        Assert.AreEqual("129 Chai",SalesPresentation.QuantityWithUnit(preview.InventoryPreview.AvailableQuantity,preview.InventoryPreview.UnitName));
        Assert.AreEqual("10 Chai",SalesPresentation.QuantityWithUnit(preview.InventoryPreview.HeldQuantity,preview.InventoryPreview.UnitName));
    }

    [TestMethod]
    public void CopyDraftLine_ResetsManualPriceOverride()
    {
        var source=new SalesOrderLineData { VariantId="v1",Sku="SKU01",ItemName="Sản phẩm",UnitCode="Cái",Quantity="2",SystemUnitPrice="1000",UnitPrice="900",ManualOverrideReason="Ưu đãi",DiscountMode="TOTAL_AMOUNT",DiscountValue="0" };
        var copy=SalesPresentation.DraftLineFromVersion(source,true);

        Assert.AreEqual("v1",copy.VariantId);
        Assert.AreEqual("",copy.ManualUnitPriceMinor);
        Assert.AreEqual("",copy.ManualReason);
        Assert.AreEqual("2",copy.Quantity);
    }

    [TestMethod]
    public void LocalSkuCatalog_RanksExactSkuProductCodeAndBarcodeBeforeAccentInsensitiveName()
    {
        var rows=new[]
        {
            new SalesOrderSkuCatalogRowData{Id="1",ProductId="p1",ProductCode="SP01",ProductName="Nước ngọt Đào",Sku="SKU-DAO",VariantName="Lon",Barcodes=["8930001"]},
            new SalesOrderSkuCatalogRowData{Id="2",ProductId="p2",ProductCode="SKU-DAO",ProductName="Khác",Sku="SKU-OTHER",VariantName="Chai",Barcodes=["8930002"]},
            new SalesOrderSkuCatalogRowData{Id="3",ProductId="p3",ProductCode="SP03",ProductName="Đào đặc biệt",Sku="SKU-3",VariantName="Chai",Barcodes=["SKU-DAO"]}
        };
        var ranked=SalesSkuLocalCatalog.SearchRows(rows,"SKU-DAO");
        Assert.AreEqual("1",ranked[0].Id); Assert.AreEqual("2",ranked[1].Id); Assert.AreEqual("3",ranked[2].Id);
        Assert.AreEqual("1",SalesSkuLocalCatalog.SearchRows(rows,"nuoc dao").First().Id);
        Assert.AreEqual("1",SalesSkuLocalCatalog.SearchRows(rows,"8930001").First().Id);
    }

    [TestMethod]
    public void DraftEstimate_MatchesCommercialDiscountAndTaxRules()
    {
        var line=new SalesDraftLineRow("v","SKU","Sản phẩm","Cái","EXCLUSIVE","10","2","100000","","","TOTAL_AMOUNT","10000");
        var estimate=SalesPresentation.EstimateDraft([line],"NONE","0");
        Assert.IsTrue(estimate.Valid); Assert.AreEqual(200000m,estimate.Gross); Assert.AreEqual(10000m,estimate.Discount); Assert.AreEqual(19000m,estimate.Tax); Assert.AreEqual(209000m,estimate.Total);
        var mixed=SalesPresentation.EstimateDraft([line],"PERCENT","5"); Assert.IsFalse(mixed.Valid); Assert.IsTrue(mixed.MixedScope);
    }

    [TestMethod]
    public void DraftLine_DirectEntryFormatsPriceAndAppliesUnitVariant()
    {
        var line=new SalesDraftLineRow("v1","SKU1","Sản phẩm","Cái","EXCLUSIVE","10","1","145000","","","PERCENT","0","p1","1",false);
        Assert.AreEqual("145.000",line.DirectUnitPriceText);
        line.DirectUnitPriceText="120.000";
        Assert.AreEqual("120000",line.ManualUnitPriceMinor);
        Assert.IsTrue(line.HasManualPrice);

        line.SetUnitOptions([
            new ProductVariantData{Id="v1",ProductId="p1",Sku="SKU1",VariantKind="BASE",IsActive=true,IsSellable=true,UnitId="u1",UnitCode="CAI",UnitName="Cái",ConversionToBase="1"},
            new ProductVariantData{Id="v2",ProductId="p1",Sku="SKU2",VariantKind="CARTON",IsActive=true,IsSellable=true,UnitId="u2",UnitCode="THUNG",UnitName="Thùng",ConversionToBase="24"}
        ]);
        Assert.HasCount(2,line.UnitOptions);
        Assert.IsTrue(line.ApplyVariant(new ProductVariantData{Id="v2",ProductId="p1",Sku="SKU2",UnitCode="THUNG",UnitName="Thùng",ConversionToBase="24"}));
        Assert.AreEqual("v2",line.VariantId);
        Assert.AreEqual("SKU2",line.Sku);
        Assert.AreEqual("Thùng",line.UnitCode);
        Assert.AreEqual("",line.ManualUnitPriceMinor);
    }

    [TestMethod]
    public void DraftLine_RemainsEditableWithoutChangingCanonicalIdentity()
    {
        var row = new SalesDraftLineRow(
            "variant", "SKU01", "Sản phẩm", "Cái", "EXCLUSIVE", "0",
            "1", "1000", "", "", "TOTAL_AMOUNT", "0");

        row.Quantity = "2";
        row.DiscountValue = "100";

        Assert.AreEqual("variant", row.VariantId);
        Assert.AreEqual("SKU01", row.Sku);
        Assert.AreEqual("2", row.Quantity);
        Assert.AreEqual("100", row.DiscountValue);
    }
    [TestMethod]
    public void SplitDraftLine_UsesNewClientIdentityAndResetsCommercialOverrides()
    {
        var source=new SalesDraftLineRow("v1","SKU1","Sản phẩm","Cái","EXCLUSIVE","10","8","145000","120000","Giá riêng","TOTAL_AMOUNT","5000","p1","1",false);
        source.SetUnitOptions([
            new ProductVariantData{Id="v1",ProductId="p1",Sku="SKU1",VariantKind="BASE",IsActive=true,IsSellable=true,UnitId="u1",UnitCode="CAI",UnitName="Cái",ConversionToBase="1"}
        ]);

        var split=SalesPresentation.SplitDraftLine(source);

        Assert.AreNotEqual(source.ClientLineId,split.ClientLineId);
        Assert.AreEqual(source.VariantId,split.VariantId);
        Assert.AreEqual("1",split.Quantity);
        Assert.AreEqual("",split.ManualUnitPriceMinor);
        Assert.AreEqual("PERCENT",split.DiscountMode);
        Assert.AreEqual("0",split.DiscountValue);
        Assert.HasCount(1,split.UnitOptions);
    }

    [TestMethod]
    public void PricingResolution_MapsCanonicalStepsForOfficeDetail()
    {
        var payload=new SalesPriceResolutionData
        {
            BaseUnitPriceMinor="150000",
            SystemUnitPriceMinor="135000",
            FinalUnitPriceMinor="135000",
            LineTotalMinor="270000",
            ResolutionFingerprint="fp-1",
            PriceSource="PRICE_ENGINE",
            Steps=
            [
                new SalesPriceStepData{Kind="BASE",AfterUnitPriceMinor="150000"},
                new SalesPriceStepData{Kind="RULE",PriceListCode="KM10",RateBps=-1000,AfterUnitPriceMinor="135000"},
                new SalesPriceStepData{Kind="SKIPPED",PriceListType="CUSTOMER",Reason="CUSTOMER_NOT_ELIGIBLE"}
            ]
        };

        var line=new SalesDraftLineRow("v1","SKU1","Sản phẩm","Cái","EXCLUSIVE","10","2","0","","","PERCENT","0");
        line.ApplyPricingResolution(payload);

        Assert.AreEqual("150.000 ₫",line.BasePriceText);
        Assert.AreEqual("135.000 ₫",line.SystemPriceText);
        Assert.AreEqual("fp-1",line.PricingFingerprint);
        Assert.HasCount(3,line.PriceSteps);
        Assert.AreEqual("KM10",line.PriceSteps[1].Label);
        StringAssert.Contains(line.PriceSteps[2].Detail,"Khách hàng");
    }

    [TestMethod]
    public void InventoryHistory_PreservesCanonicalMovementAndQuantityLanguage()
    {
        var row=new InventoryMovementHistoryData
        {
            MovementType="SALES_DELIVERY_ISSUE",PostedAt="2026-09-15T08:00:00Z",
            SourceDocumentNumber="SO00001",BaseQuantityDelta="-10.000000",StockAfter="90.000000",
            WarehouseCode="KHO1",PostedByName="Nhân viên"
        };

        var display=SalesPresentation.InventoryHistoryRow(row,"Chai");

        Assert.AreEqual("Xuất kho giao khách",display.Movement);
        Assert.AreEqual("-10 Chai",display.Quantity);
        Assert.AreEqual("90 Chai",display.StockAfter);
        Assert.AreEqual("SO00001",display.Document);
    }

}
