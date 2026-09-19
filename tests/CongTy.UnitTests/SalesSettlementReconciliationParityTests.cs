using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Accounting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class SalesSettlementReconciliationParityTests
{
    [TestMethod]
    public void Presentation_UsesWebStatusLabelsAndOfficeFormatting()
    {
        Assert.AreEqual("Khớp", SalesSettlementReconciliationPresentation.Status("matched"));
        Assert.AreEqual("Lệch", SalesSettlementReconciliationPresentation.Status("mismatch"));
        Assert.AreEqual("Đã xác nhận", SalesSettlementReconciliationPresentation.Status("confirmed"));
        Assert.AreEqual("Đã giữ hàng", SalesSettlementReconciliationPresentation.Status("allocated"));
        Assert.AreEqual("Giao một phần", SalesSettlementReconciliationPresentation.Status("partially_delivered"));
        Assert.AreEqual("Tài xế đang giữ", SalesSettlementReconciliationPresentation.Status("driver_custody"));
        Assert.AreEqual("Có chênh lệch", SalesSettlementReconciliationPresentation.Status("discrepancy"));
        Assert.AreEqual("1.234.568 VND", SalesSettlementReconciliationPresentation.Money("1234567.5"));
        Assert.AreEqual("sales-orders", SalesSettlementReconciliationPresentation.AnomalyDestination("sales_order_status"));
        Assert.AreEqual("cod", SalesSettlementReconciliationPresentation.AnomalyDestination("cod_collection"));
        Assert.AreEqual("receivables", SalesSettlementReconciliationPresentation.AnomalyDestination("receivable_document"));
    }

    [TestMethod]
    public void Service_UsesCanonicalReadOnlyReconciliationContract()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "SalesSettlementReconciliationService.cs");

        StringAssert.Contains(service, "/api/accounting/reconciliation?");
        StringAssert.Contains(service, "limit=100");
        StringAssert.Contains(service, "search=");
        StringAssert.Contains(service, "status=");
        StringAssert.Contains(service, "matched");
        StringAssert.Contains(service, "mismatch");
        StringAssert.Contains(service, "GetDataAsync<SalesSettlementReconciliationData>");
        StringAssert.Contains(service, "normalizedSearch.Length > 160");
        Assert.IsFalse(service.Contains("Post", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Put", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Patch", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Delete", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(service.Contains("warehouseId", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ViewModel_UsesReceivableReadPermissionServerScopeAndCanonicalErrors()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "SalesSettlementReconciliationViewModel.cs");

        StringAssert.Contains(vm, "core.receivable.read");
        StringAssert.Contains(vm, "_service.GetAsync(");
        StringAssert.Contains(vm, "_accessGeneration");
        StringAssert.Contains(vm, "_loadGeneration");
        StringAssert.Contains(vm, "CanonicalApiException");
        StringAssert.Contains(vm, "ToOfficeMessage");
        StringAssert.Contains(vm, "BuildCsv()");
        foreach (var group in new[] { "Khách hàng", "Chứng từ", "Đơn bán hàng", "Thu COD", "Bàn giao COD", "Bất thường" })
            StringAssert.Contains(vm, group);

        Assert.IsFalse(vm.Contains("SelectedWarehouseId", StringComparison.Ordinal));
        Assert.IsFalse(vm.Contains("ICanonicalIdempotencyKeyProvider", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesWebFiltersKpisSectionsAndReadOnlyDrillDown()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "SalesSettlementReconciliationView.xaml");

        StringAssert.Contains(view, "<BooleanToVisibilityConverter x:Key=\"BoolToVisibility\" />");
        foreach (var text in new[]
        {
            "Lọc dữ liệu đối soát",
            "Từ ngày",
            "Đến ngày",
            "Khách, chứng từ, chuyến hoặc tài xế",
            "Kết quả",
            "Đặt lại",
            "Xuất CSV",
            "Dư nợ đang mở",
            "Credit chưa dùng",
            "COD tài xế đang giữ",
            "COD chờ kế toán nhận",
            "COD công ty đã nhận",
            "Bất thường cần xử lý",
            "Số dư công nợ giải thích được",
            "Trạng thái đơn, hàng, giao và tiền",
            "Posting, phân bổ và số dư còn lại",
            "Thu COD và bàn giao cuối chuyến",
            "Bất thường cần truy về nguồn"
        })
            StringAssert.Contains(view, text);

        var customers = view.IndexOf("Khách &amp; kho", StringComparison.Ordinal);
        var orders = view.IndexOf("Đơn bán", StringComparison.Ordinal);
        var documents = view.IndexOf("Chứng từ", StringComparison.Ordinal);
        var cod = view.IndexOf("Header=\"COD\"", StringComparison.Ordinal);
        var anomalies = view.IndexOf("Header=\"Bất thường\"", StringComparison.Ordinal);
        Assert.AreNotEqual(-1, customers);
        Assert.IsLessThan(orders, customers);
        Assert.IsLessThan(documents, orders);
        Assert.IsLessThan(cod, documents);
        Assert.IsLessThan(anomalies, cod);

        foreach (var handler in new[]
        {
            "Receivables_OnClick",
            "CodAccounting_OnClick",
            "SalesOrders_OnClick",
            "TripReconciliation_OnClick",
            "AnomalySource_OnClick"
        })
            StringAssert.Contains(view, handler);
    }

    [TestMethod]
    public void Shell_AddsDedicatedAccountingWorkspaceWithoutMovingExistingIndexes()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.SalesSettlement.cs");
        var shellCore = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.SalesSettlement.cs");
        var bootstrap = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "core.receivable.read");
        StringAssert.Contains(shell, "accounting.reconciliation");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 53");
        StringAssert.Contains(shell, "IsAccountingOpen = true");
        StringAssert.Contains(shellCore, "accounting.reconciliation");
        StringAssert.Contains(shellCore, "Đối soát bán hàng & COD");
        StringAssert.Contains(shellCore, "IsSalesSettlementSelected");
        StringAssert.Contains(shellCore, "CanViewSalesSettlement");
        StringAssert.Contains(shellCore, "OnPropertyChanged(nameof(CanViewSalesSettlement));");
        Assert.IsFalse(shellCore.Contains("nameof(CodAccountingViewModel.CanReadReport))\n            {\n                OnPropertyChanged(nameof(CanViewCodAccounting));\n        OnPropertyChanged(nameof(CanViewSalesSettlement));", StringComparison.Ordinal));

        StringAssert.Contains(host, "workspaceTabs.Items[53]");
        StringAssert.Contains(host, "SalesSettlementReconciliationService");
        StringAssert.Contains(host, "SalesSettlementNavigationRequested");
        StringAssert.Contains(bootstrap, "WireSalesSettlementReconciliationWorkspace()");

        StringAssert.Contains(xaml, "IsSalesSettlementSelected");
        StringAssert.Contains(xaml, "CanViewSalesSettlement");
        StringAssert.Contains(xaml, "AccountingSalesSettlement_OnClick");
        StringAssert.Contains(xaml, "Đối soát tổng hợp");
        Assert.IsFalse(
            xaml.Contains(
                "Màn Đối soát tổng hợp là nghiệp vụ kế toán riêng và sẽ được nối khi màn đó được triển khai.",
                StringComparison.Ordinal));

        StringAssert.Contains(shellCore, "SelectedWorkspaceIndex = 31");
        StringAssert.Contains(shellCore, "SelectedWorkspaceIndex = 34");
    }

    [TestMethod]
    public void Contracts_DeserializeCurrentSalesSettlementWireShape()
    {
        const string json = """
        {
          "generatedAt":"2026-09-19T01:00:00.000Z",
          "filters":{"from":null,"to":null,"search":null,"status":"all","limit":100},
          "summary":{
            "customerGroupCount":"1",
            "debitOutstandingAmount":"150000",
            "unappliedCreditAmount":"10000",
            "ledgerBalance":"140000",
            "documentMismatchCount":"0",
            "anomalyCount":"1",
            "codCustodyAmount":"50000",
            "collectionMismatchCount":"0",
            "codPendingAcceptanceAmount":"20000",
            "codAcceptedAmount":"30000",
            "codVarianceAmount":"0",
            "handoverMismatchCount":"0"
          },
          "customers":[{
            "customerId":"c1","customerCode":"KH001","customerName":"Khách A",
            "warehouseId":"w1","warehouseCode":"K01","warehouseName":"Kho 1","currencyCode":"VND",
            "debitPostedAmount":"150000","creditPostedAmount":"10000","debitOutstandingAmount":"150000",
            "unappliedCreditAmount":"10000","calculatedOpenBalance":"140000","ledgerBalance":"140000",
            "documentMismatchCount":"0","latestDocumentDate":"2026-09-18","reconciliationStatus":"matched"
          }],
          "documents":[],
          "orders":[],
          "codCollections":[],
          "codHandovers":[],
          "anomalies":[{
            "anomalyType":"sales_order_status","sourceId":"s1","sourceNumber":"SO001",
            "reconciliationStatus":"mismatch","details":{"stored":"paid","calculated":"partially_paid"},"warehouseId":"w1"
          }]
        }
        """;

        var report = JsonSerializer.Deserialize<SalesSettlementReconciliationData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được hợp đồng Đối soát bán hàng & COD.");

        Assert.AreEqual("all", report.Filters.Status);
        Assert.AreEqual(100, report.Filters.Limit);
        Assert.AreEqual("150000", report.Summary.DebitOutstandingAmount);
        Assert.HasCount(1, report.Customers);
        Assert.AreEqual("KH001", report.Customers[0].CustomerCode);
        Assert.HasCount(1, report.Anomalies);
        Assert.AreEqual("sales_order_status", report.Anomalies[0].AnomalyType);
        Assert.AreEqual("paid", report.Anomalies[0].Details["stored"].GetString());
    }

    [TestMethod]
    public void View_DoesNotExposeDeveloperLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "SalesSettlementReconciliationView.xaml");

        Assert.IsFalse(view.Contains("/api/", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("permission", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("UUID", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("canonical", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Phase 6F", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            directory = directory.Parent;
        }

        Assert.Fail($"Không tìm thấy tệp trong repo: {string.Join("/", parts)}");
        return string.Empty;
    }
}
