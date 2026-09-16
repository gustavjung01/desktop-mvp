using CongTy.Contracts;
using CongTy.Desktop.Logistics;

namespace CongTy.UnitTests;

[TestClass]
public sealed class TripPlanningParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeTripLabelsAndCanonicalAddress()
    {
        Assert.AreEqual("Nháp", TripPlanningPresentation.Status("draft"));
        Assert.AreEqual("Đã lập kế hoạch", TripPlanningPresentation.Status("planned"));
        Assert.AreEqual("Đã khóa", TripPlanningPresentation.Status("locked"));
        Assert.AreEqual("12.34", TripPlanningPresentation.Quantity("12.3400"));

        var address = new Dictionary<string, System.Text.Json.JsonElement>
        {
            ["addressLine1"] = System.Text.Json.JsonDocument.Parse("\"12 Nguyễn Huệ\"").RootElement.Clone(),
            ["districtName"] = System.Text.Json.JsonDocument.Parse("\"Quận 1\"").RootElement.Clone()
        };
        Assert.AreEqual("12 Nguyễn Huệ, Quận 1", TripPlanningPresentation.Address(address));
    }

    [TestMethod]
    public void Service_UsesCanonicalPlanningRoutesAndIdempotentWrites()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "TripPlanningService.cs");

        foreach (var path in new[]
        {
            "/api/logistics/warehouses",
            "/api/logistics/routes?active=true",
            "/api/logistics/vehicles?active=true",
            "/api/logistics/drivers?active=true",
            "/api/logistics/driver-employees?limit=1000",
            "/api/logistics/eligible-delivery-orders",
            "/api/logistics/trips"
        }) StringAssert.Contains(service, path);

        foreach (var action in new[] { "assign", "unassign", "reorder", "plan", "reopen", "lock" })
            StringAssert.Contains(service, $"ActionPath(tripId, \"{action}\")");

        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "PutIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid");
    }

    [TestMethod]
    public void ViewModel_ReusesSharedIdempotencyKeyForSameIntent()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripPlanningViewModel.cs");

        StringAssert.Contains(vm, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(vm, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(vm, "_idempotencyKeys.Create($\"trip-planning-{prefix}\")");
        StringAssert.Contains(vm, "await operation(KeyFor(intent))");
        StringAssert.Contains(vm, "_intentKeys.Remove(intent)");
        StringAssert.Contains(vm, "OrderBy(id => id, StringComparer.Ordinal)");
        Assert.IsFalse(vm.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DriverCreation_UsesRealEmployeeInsteadOfFreeTextIdentity()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "TripPlanningContracts.cs");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripPlanningViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripPlanningView.xaml");

        StringAssert.Contains(contracts, "LogisticsDriverCreateRequest");
        StringAssert.Contains(contracts, "EmployeeId");
        StringAssert.Contains(vm, "new LogisticsDriverCreateRequest(employeeId");
        StringAssert.Contains(view, "Text=\"Nhân sự tài xế\"");
        StringAssert.Contains(view, "SelectedDriverEmployeeSummary");
        Assert.IsFalse(view.Contains("Mã tài xế", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Tên tài xế", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesWebTabHierarchyAndPlanningLifecycle()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripPlanningView.xaml");

        var planning = view.IndexOf("Header=\"Lập chuyến\"", StringComparison.Ordinal);
        var assignment = view.IndexOf("Header=\"Gán chuyến\"", StringComparison.Ordinal);
        Assert.IsGreaterThan(planning, assignment);

        var createList = view.IndexOf("Header=\"Tạo &amp; danh sách\"", StringComparison.Ordinal);
        var detail = view.IndexOf("Header=\"Chi tiết chuyến\"", StringComparison.Ordinal);
        Assert.IsGreaterThan(createList, detail);

        foreach (var text in new[]
        {
            "Tuyến, xe và tài xế",
            "Tạo chuyến giao",
            "Các chuyến giao",
            "Tuyến → Chuyến → Phiếu giao",
            "Phiếu sẵn sàng cùng kho",
            "Lưu kế hoạch",
            "Chuyển sang đã lập kế hoạch",
            "Mở lại chỉnh sửa",
            "Khóa kế hoạch",
            "Bỏ khỏi chuyến",
            "Chưa có điểm giao."
        }) StringAssert.Contains(view, text);

        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripPlanningViewModel.cs");
        StringAssert.Contains(vm, "Kế hoạch đã lập và đang chỉ đọc.");
        StringAssert.Contains(vm, "Kế hoạch đã khóa. Xe, tài xế, điểm dừng và phiếu giao chỉ được đọc.");

        Assert.IsFalse(view.Contains("Xuất phát", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Ghi kết quả giao", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Shell_ActivatesThirdLogisticsScreenWithoutChangingExistingIndexes()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "\"logistics.trips\" => \"Điều phối giao hàng\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 23");
        StringAssert.Contains(shell, "CanViewTripPlanning");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 21");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 22");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsTripPlanningSelected}\"");
        StringAssert.Contains(xaml, "Click=\"TripPlanning_OnClick\"");
        StringAssert.Contains(xaml, "x:Name=\"TripPlanningHost\"");
        StringAssert.Contains(code, "TripPlanningHost.Content = tripPlanningView");
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
