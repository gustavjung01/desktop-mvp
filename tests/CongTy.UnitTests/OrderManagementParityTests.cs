using CongTy.Desktop.Sales;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class OrderManagementParityTests
{
    [TestMethod]
    public void Presentation_UsesCurrentWebStageLaneSourceAndPaymentLanguage()
    {
        Assert.AreEqual("Đang xử lý",OrderManagementPresentation.StageLabel("active"));
        Assert.AreEqual("Đang chuẩn bị",OrderManagementPresentation.StageLabel("preparing"));
        Assert.AreEqual("Chờ giao",OrderManagementPresentation.StageLabel("waiting_delivery"));
        Assert.AreEqual("Đã hoàn thành",OrderManagementPresentation.StageLabel("completed"));
        Assert.AreEqual("Đã hủy",OrderManagementPresentation.StageLabel("cancelled"));
        Assert.AreEqual("Tại quầy",OrderManagementPresentation.LaneLabel("counter"));
        Assert.AreEqual("Giao thủ công",OrderManagementPresentation.LaneLabel("manual"));
        Assert.AreEqual("Giao theo chuyến",OrderManagementPresentation.LaneLabel("trip"));
        Assert.AreEqual("Công Ty",OrderManagementPresentation.SourceLabel("internal"));
        Assert.AreEqual("Nhân viên thị trường",OrderManagementPresentation.SourceLabel("mcp"));
        Assert.AreEqual("Khách đặt hàng",OrderManagementPresentation.SourceLabel("customer"));
        Assert.AreEqual("Chưa thu",OrderManagementPresentation.PaymentLabel("unpaid"));
        Assert.AreEqual("Thu một phần",OrderManagementPresentation.PaymentLabel("partial"));
        Assert.AreEqual("Đã thu",OrderManagementPresentation.PaymentLabel("paid"));
    }

    [TestMethod]
    public void Presentation_MatchesCurrentWebBucketsAndPrintableRule()
    {
        var pickup=new SalesOrderData{DeliveryMode="PICKUP",Status="confirmed",Number="SO-260917-001234"};
        Assert.AreEqual("counter",OrderManagementPresentation.DeliveryLane(pickup));
        Assert.AreEqual("SO001234",OrderManagementPresentation.CompactNumber(pickup.Number));
        Assert.IsTrue(OrderManagementPresentation.IsPrintable(pickup));

        var cancelled=new SalesOrderData{Status="cancelled",DeliveryStatus="cancelled",Number="SO-1"};
        Assert.AreEqual("cancelled",OrderManagementPresentation.WorkStage(cancelled));
        Assert.IsFalse(OrderManagementPresentation.IsPrintable(cancelled));

        var customer=new SalesOrderData{SourceType="API",SourceId="CUSTOMER_PORTAL:abc"};
        Assert.AreEqual("customer",OrderManagementPresentation.SourceBucket(customer));
    }

    [TestMethod]
    public void View_PreservesRequiredFiltersColumnsAndBatchActions()
    {
        var view=ReadRepoFile("src","CongTy.Desktop","Sales","OrderManagementView.xaml");
        foreach(var text in new[]{"Tìm kiếm","Từ ngày","Từ giờ","Đến ngày","Đến giờ","Trạng thái đơn","Thanh toán","Luồng giao","Nguồn đơn","Số đơn","Ngày tạo","Khách hàng","Giá trị đơn","Xuất/chuẩn bị hàng","Giao hàng","Chọn tất cả","In đơn đã chọn","Tạo đơn bán hàng"})
            StringAssert.Contains(view,text);
        Assert.IsFalse(view.Contains("Nhập file",StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Xuất file",StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Thao tác ▾",StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_ClearsSelectionWhenFiltersChangeAndSelectsWholeFilteredSet()
    {
        var source=ReadRepoFile("src","CongTy.Desktop","Sales","OrderManagementViewModel.cs");
        StringAssert.Contains(source,"private void FilterChanged()");
        StringAssert.Contains(source,"ClearSelection(false)");
        StringAssert.Contains(source,"foreach(var order in _filtered)_selectedIds.Add(order.Id)");
        StringAssert.Contains(source,"foreach(var row in Rows)row.IsSelected=_selectedIds.Contains(row.Id)");
    }

    [TestMethod]
    public void Query_LoadsAllSalesOrderPagesWithoutBackendChange()
    {
        var source=ReadRepoFile("src","CongTy.Desktop","Sales","OrderManagementQueryService.cs");
        StringAssert.Contains(source,"/api/sales-orders?limit={pageSize}&offset={offset}");
        StringAssert.Contains(source,"while(true)");
        StringAssert.Contains(source,"page.Length<pageSize");
        Assert.IsFalse(source.Contains("Post",StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(source.Contains("Idempotency-Key",StringComparison.Ordinal));
    }

    [TestMethod]
    public void BatchPrint_ReusesWarehouseSlipAndOneBatchPrintCall()
    {
        var source=ReadRepoFile("src","CongTy.Desktop","Sales","SalesOrderPrintPreview.cs");
        StringAssert.Contains(source,"ShowBatch");
        StringAssert.Contains(source,"BuildBatchDocument");
        StringAssert.Contains(source,"AppendOrder(document, item.Order, item.Version, template, index > 0)");
        StringAssert.Contains(source,"PHIẾU XUẤT KHO");
        StringAssert.Contains(source,"TryPrintBatch(window, document, items.Count, template)");
        StringAssert.Contains(source,"Phiếu xuất kho · {count:N0} đơn");
    }

    [TestMethod]
    public void Shell_WiresUi56WithoutRewritingMainShellXaml()
    {
        var shell=ReadRepoFile("src","CongTy.Desktop","Shell","ShellViewModel.OrderManagement.cs");
        var host=ReadRepoFile("src","CongTy.Desktop","Shell","MainWindow.OrderManagement.cs");
        var xaml=ReadRepoFile("src","CongTy.Desktop","Shell","MainWindow.xaml");
        StringAssert.Contains(shell,"sales.order-management");
        StringAssert.Contains(shell,"SelectedWorkspaceIndex=37");
        StringAssert.Contains(shell,"NavigateOrderManagementAsync");
        StringAssert.Contains(shell,"IsOrderManagementSelected");
        Assert.IsFalse(host.Contains("sidebarButton", StringComparison.Ordinal));
        StringAssert.Contains(host,"new OrderManagementView()");
        StringAssert.Contains(host,"workspaceTabs.Items[37]");
        StringAssert.Contains(xaml,"Click=\"OrderManagement_OnClick\"");
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory is not null)
        {
            var candidate=Path.Combine(new[]{directory.FullName}.Concat(parts).ToArray());
            if(File.Exists(candidate))return File.ReadAllText(candidate);
            directory=directory.Parent;
        }
        Assert.Fail($"Không tìm thấy tệp trong repo: {string.Join("/",parts)}");
        return string.Empty;
    }
}
