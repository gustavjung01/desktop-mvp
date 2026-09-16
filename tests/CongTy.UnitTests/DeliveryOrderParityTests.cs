using CongTy.Contracts;
using CongTy.Desktop.Logistics;

namespace CongTy.UnitTests;

[TestClass]
public sealed class DeliveryOrderParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeLabelsAndExactQuantityRules()
    {
        Assert.AreEqual("Sẵn sàng bàn giao", DeliveryOrderPresentation.Status("ready_to_dispatch"));
        Assert.AreEqual("Đã xuất theo chuyến", DeliveryOrderPresentation.Status("dispatched"));
        Assert.AreEqual("Nhận tại quầy", DeliveryOrderPresentation.HandoverMode("PICKUP"));
        Assert.AreEqual("12.34", DeliveryOrderPresentation.Quantity("12.3400"));

        Assert.IsTrue(DeliveryOrderPresentation.TryScaledQuantity("1.000000000001", out var positive));
        Assert.IsTrue(positive > System.Numerics.BigInteger.Zero);
        Assert.IsFalse(DeliveryOrderPresentation.TryScaledQuantity("1.0000000000001", out _));
    }

    [TestMethod]
    public void Service_UsesCanonicalDeliveryOrderRoutesAndIdempotentMutations()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "DeliveryOrderService.cs");
        StringAssert.Contains(service, "/api/delivery-orders/eligibility?limit=1000&offset=0");
        StringAssert.Contains(service, "/api/delivery-orders?limit=500&offset=0");
        StringAssert.Contains(service, "pickup-handover");
        StringAssert.Contains(service, "manual-handover");
        StringAssert.Contains(service, "reverse-inventory-issue");
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid");
    }

    [TestMethod]
    public void ViewModel_ReusesCanonicalGeneratorByIntent()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryOrderViewModel.cs");
        StringAssert.Contains(vm, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(vm, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(vm, "_idempotencyKeys.Create($\"delivery-order-{prefix}\")");
        StringAssert.Contains(vm, "_pickupAttemptAt ??=");
        StringAssert.Contains(vm, "_manualAttemptAt ??=");
        StringAssert.Contains(vm, "KeyFor(\"create\", group.Key, fingerprint)");
        StringAssert.Contains(vm, "ShowDraftActions");
        StringAssert.Contains(vm, "ShowPickupActions");
        StringAssert.Contains(vm, "ShowManualActions");
        StringAssert.Contains(vm, "ShowReverseActions");
        StringAssert.Contains(vm, "ShowPrint");
        Assert.IsFalse(vm.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesTwoWorkflowTabsAndAllLifecycleActions()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryOrderView.xaml");

        var create = view.IndexOf("Header=\"Lập chứng từ\"", StringComparison.Ordinal);
        var manage = view.IndexOf("Header=\"Theo dõi &amp; xử lý\"", StringComparison.Ordinal);
        Assert.IsGreaterThan(create, manage);

        foreach (var text in new[]
        {
            "Đơn còn đóng gói",
            "Chứng từ nháp",
            "Sẵn sàng bàn giao",
            "Đã xuất vật lý",
            "Tạo phiếu giao hàng nháp",
            "Xác nhận sẵn sàng bàn giao",
            "Hủy chứng từ nháp",
            "Xác nhận bàn giao và xuất kho",
            "Xác nhận giao thủ công &amp; xuất kho",
            "Đảo xuất kho sai"
        }) StringAssert.Contains(view, text);

        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryOrderViewModel.cs");
        StringAssert.Contains(vm, "\"Phiếu giao hàng\", \"Phiếu đóng gói\"");
    }

    [TestMethod]
    public void Shell_ActivatesSecondLogisticsScreenOnly()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "\"logistics.delivery-orders\" => \"Bàn giao giao nhận\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 21");
        StringAssert.Contains(shell, "CanViewDeliveryOrders");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsDeliveryOrdersSelected}\"");
        StringAssert.Contains(xaml, "Click=\"DeliveryOrders_OnClick\"");
        StringAssert.Contains(xaml, "x:Name=\"DeliveryOrderHost\"");
        StringAssert.Contains(code, "DeliveryOrderHost.Content = deliveryOrderView");
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
