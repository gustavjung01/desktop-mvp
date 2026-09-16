using CongTy.Desktop.Logistics;

namespace CongTy.UnitTests;

[TestClass]
public sealed class TripReconciliationParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeResultsAndExactQuantities()
    {
        Assert.AreEqual("Giao đủ", TripReconciliationPresentation.Result("delivered_full"));
        Assert.AreEqual("Giao một phần", TripReconciliationPresentation.Result("delivered_partial"));
        Assert.AreEqual("Không giao được", TripReconciliationPresentation.Result("failed"));
        Assert.AreEqual("Hẹn giao lại", TripReconciliationPresentation.Result("rescheduled"));
        Assert.AreEqual("Chưa có kết quả", TripReconciliationPresentation.Result(null));
        Assert.AreEqual("12.34", TripReconciliationPresentation.Quantity("12.340000000000"));
        Assert.IsTrue(TripReconciliationPresentation.IsPositive("0.000000000001"));
    }

    [TestMethod]
    public void Service_UsesCanonicalReconciliationEndpointsAndIdempotentWrites()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "TripReconciliationService.cs");

        StringAssert.Contains(service, "/api/logistics/trips?status=all");
        StringAssert.Contains(service, "/reconciliation");
        StringAssert.Contains(service, "/return-receipts");
        StringAssert.Contains(service, "/close");
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid");
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_ReusesCanonicalKeyUntilMutationSucceeds()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripReconciliationViewModel.cs");

        StringAssert.Contains(vm, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(vm, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(vm, "_idempotencyKeys.Create($\"trip-reconciliation-{prefix}\")");
        StringAssert.Contains(vm, "_intentKeys.Remove(intent)");
        StringAssert.Contains(vm, "OrderBy(row => row.InventoryIssueLineId, StringComparer.Ordinal)");
        Assert.IsFalse(vm.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesFourStepFlowAndWebActionOrder()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripReconciliationView.xaml");

        foreach (var text in new[]
        {
            "1 · Chọn chuyến",
            "2 · Kiểm tra chênh lệch",
            "3 · Nhận hàng trả về",
            "4 · Đóng chuyến",
            "Chọn chuyến",
            "VIỆC TIẾP THEO",
            "BƯỚC 2 · KIỂM TRA CHÊNH LỆCH",
            "Phiếu / hàng",
            "Kết quả",
            "Xuất",
            "Đã giao",
            "Đã về",
            "Còn xe",
            "Nhận hàng trả về",
            "Chốt đối soát &amp; đóng chuyến",
            "LỊCH SỬ KHO NHẬN LẠI",
            "Mở kết quả lần giao",
            "In đối soát"
        }) StringAssert.Contains(view, text);

        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripReconciliationViewModel.cs");
        StringAssert.Contains(vm, "\"Xác nhận nhập hàng về kho\"");
        StringAssert.Contains(vm, "\"Đang nhập kho...\"");

        var receive = view.IndexOf("BƯỚC 3", StringComparison.Ordinal);
        var close = view.IndexOf("BƯỚC 4", StringComparison.Ordinal);
        Assert.IsGreaterThan(receive, close);
    }

    [TestMethod]
    public void Shell_ActivatesSixthLogisticsScreenWithoutChangingParallelIndexes()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "\"logistics.trip-reconciliation\" => \"Đối soát cuối chuyến\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 29");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 27");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 28");
        StringAssert.Contains(shell, "CanViewTripReconciliation");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsTripReconciliationSelected}\"");
        StringAssert.Contains(xaml, "Click=\"TripReconciliation_OnClick\"");
        StringAssert.Contains(xaml, "Content=\"Kết quả lần giao\"");
        StringAssert.Contains(xaml, "x:Name=\"TripReconciliationHost\"");
        StringAssert.Contains(code, "TripReconciliationHost.Content = tripReconciliationView");
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
