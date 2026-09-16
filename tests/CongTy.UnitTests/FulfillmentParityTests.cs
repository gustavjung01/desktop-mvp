using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class FulfillmentParityTests
{
    [TestMethod]
    public void Presentation_PreservesFulfillmentQuantityAndStatusContract()
    {
        var item = new FulfillmentWorkItemData
        {
            OrderedQuantity = "2",
            OrderedUnitCode = "Thùng",
            OrderedBaseQuantity = "48",
            BaseUnitCode = "Chai"
        };

        Assert.AreEqual("6.5", FulfillmentPresentation.QuantityDifference("10", "3.5"));
        Assert.AreEqual("2 Thùng → 48 Chai", FulfillmentPresentation.OrderedQuantityLabel(item));
        Assert.AreEqual("Chờ phân bổ", FulfillmentPresentation.StatusLabel("reserved"));
        Assert.AreEqual("Đang soạn", FulfillmentPresentation.StatusLabel("partially_picked"));
        Assert.AreEqual("packing", FulfillmentPresentation.StatusBucket("packed"));
        Assert.IsTrue(FulfillmentPresentation.IsValidPositiveQuantity("1.25"));
        Assert.IsFalse(FulfillmentPresentation.IsValidPositiveQuantity("0"));
        Assert.IsTrue(FulfillmentPresentation.IsGreaterThan("2", "1.5"));
    }

    [TestMethod]
    public async Task Lifecycle_DoesNotCachePreLoginDenialAndReloadsAfterAccessChanges()
    {
        var access = new TestAccessStateService();
        var service = new TestFulfillmentService();
        var viewModel = new FulfillmentViewModel(service, access, new CanonicalIdempotencyKeyProvider());

        await viewModel.EnsureLoadedAsync();
        Assert.AreEqual(0, service.ListWorkCalls);
        Assert.AreEqual(string.Empty, viewModel.Message);

        access.ApplyAuthenticated("PERMANENT");
        await viewModel.EnsureLoadedAsync();
        Assert.AreEqual("Tài khoản chưa được cấp quyền xem Chuẩn bị hàng.", viewModel.Message);
        Assert.AreEqual(0, service.ListWorkCalls);

        access.ApplyAuthenticated("PERMANENT", "core.fulfillment.read");
        Assert.AreEqual(string.Empty, viewModel.Message);

        await viewModel.EnsureLoadedAsync();
        Assert.AreEqual(1, service.ListWorkCalls);
        await viewModel.EnsureLoadedAsync();
        Assert.AreEqual(1, service.ListWorkCalls);

        access.Clear();
        access.ApplyAuthenticated("PERMANENT", "core.fulfillment.read");
        await viewModel.EnsureLoadedAsync();
        Assert.AreEqual(2, service.ListWorkCalls);
    }

    [TestMethod]
    public void Source_PreservesFulfillmentWebLayoutActionsAndCanonicalIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "FulfillmentService.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "FulfillmentViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "FulfillmentView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "FulfillmentView.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");
        var access = ReadRepoFile("src", "CongTy.ApiClient", "AccessStateService.cs");

        StringAssert.Contains(service, "/api/inventory/fulfillment-work?limit=500");
        StringAssert.Contains(service, "/api/inventory/fulfillment-demands/");
        StringAssert.Contains(service, "/suggestions");
        StringAssert.Contains(service, "/api/inventory/fulfillment-orders/");
        StringAssert.Contains(service, "/allocate");
        StringAssert.Contains(service, "/api/inventory/fulfillment-allocations/");
        StringAssert.Contains(service, "/api/inventory/holds?");
        StringAssert.Contains(service, "excludeSalesOrderId=");
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(value)");

        StringAssert.Contains(viewModel, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(viewModel, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create($\"fulfillment-{prefix}\")");
        StringAssert.Contains(viewModel, "KeyFor(\"allocate-order\"");
        StringAssert.Contains(viewModel, "KeyFor(\"allocate\"");
        StringAssert.Contains(viewModel, "KeyFor(action");

        StringAssert.Contains(xaml, "ColumnDefinition Width=\"320\"");
        var sections = new[]
        {
            "Đơn cần chuẩn bị",
            "Phân bổ toàn đơn",
            "Sản phẩm trong đơn",
            "CHI TIẾT SẢN PHẨM",
            "Vị trí có thể lấy",
            "Hàng đã phân bổ"
        };
        var cursor = -1;
        foreach (var section in sections)
        {
            var next = xaml.IndexOf(section, cursor + 1, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự khối: {section}");
            cursor = next;
        }

        StringAssert.Contains(xaml, "Text=\"KÊNH BÁN\"");
        StringAssert.Contains(xaml, "Text=\"TRẠNG THÁI\"");
        StringAssert.Contains(xaml, "Text=\"KHO\"");
        StringAssert.Contains(xaml, "Content=\"Xóa lọc\"");
        StringAssert.Contains(xaml, "Header=\"Khách đặt → Kho\"");
        StringAssert.Contains(xaml, "Header=\"Đơn khác giữ\"");
        StringAssert.Contains(xaml, "Content=\"PB\"");
        StringAssert.Contains(xaml, "Content=\"PB đủ\"");
        StringAssert.Contains(xaml, "Text=\"Đơn khác đang giữ hàng\"");
        StringAssert.Contains(xaml, "Không có đơn hàng phù hợp.");
        StringAssert.Contains(xaml, "Chọn một đơn để xem chi tiết chuẩn bị hàng.");
        StringAssert.Contains(xaml, "Không còn vị trí/lô khả dụng.");
        StringAssert.Contains(xaml, "Chưa có vị trí/lô đã phân bổ.");

        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.F");
        StringAssert.Contains(code, "Key.Escape");
        StringAssert.Contains(code, "SearchBox.Focus()");
        StringAssert.Contains(code, "CloseHolds()");

        StringAssert.Contains(shell, "\"inventory.fulfillment\" => \"Chuẩn bị hàng\"");
        StringAssert.Contains(shell, "IsInventoryFulfillmentSelected");
        StringAssert.Contains(shell, "CanViewFulfillment");
        StringAssert.Contains(shell, "Navigate(\"fulfillment\", 6)");
        StringAssert.Contains(shell, "public async Task NavigateFulfillmentAsync()");
        StringAssert.Contains(shell, "await _fulfillment.EnsureLoadedAsync().ConfigureAwait(true)");
        StringAssert.Contains(viewModel, "if (_loaded) return;");
        StringAssert.Contains(viewModel, "var loadTask = _initialLoadTask ??= LoadWorkAsync();");
        StringAssert.Contains(viewModel, "_loaded = await loadTask.ConfigureAwait(true)");
        StringAssert.Contains(viewModel, "ResetSessionData()");
        Assert.IsFalse(viewModel.Contains("_loaded = true;\n        await LoadWorkAsync()", StringComparison.Ordinal));
        StringAssert.Contains(main, "Tag=\"{Binding IsInventoryFulfillmentSelected}\"");
        StringAssert.Contains(main, "Click=\"InventoryFulfillment_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"FulfillmentHost\"");
        Assert.IsFalse(main.Contains(
            "IsEnabled=\"False\"><TextBlock Text=\"Chuẩn bị hàng\"",
            StringComparison.Ordinal));

        StringAssert.Contains(access, "\"core.fulfillment.read\"");
        StringAssert.Contains(access, "\"core.fulfillment.pick\"");
        StringAssert.Contains(app, "AddSingleton<IFulfillmentService, FulfillmentService>()");
        StringAssert.Contains(app, "AddSingleton<FulfillmentViewModel>()");
        StringAssert.Contains(app, "AddSingleton<FulfillmentView>()");
    }

    [TestMethod]
    public void InventorySummaryCards_AreCompactTwoLineCardsWithHintsPreserved()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryView.xaml");

        StringAssert.Contains(xaml, "ToolTip=\"Đếm mã hàng có số tồn thực tế lớn hơn 0.\"");
        StringAssert.Contains(xaml, "ToolTip=\"Theo kho và mã hàng sau khi gộp vị trí, lô.\"");
        StringAssert.Contains(xaml, "ToolTip=\"Số vị thế đang có hàng được giữ cho đơn.\"");
        StringAssert.Contains(xaml, "ToolTip=\"Chỉ tính các lô còn số lượng thực tế.\"");
        StringAssert.Contains(xaml, "ToolTip=\"Chỉ cộng các vị thế đã tính được giá vốn.\"");
        StringAssert.Contains(xaml, "ToolTip=\"Số vị thế có chênh lệch số lượng hoặc chưa tính đủ giá vốn.\"");

        Assert.IsFalse(xaml.Contains(
            "Text=\"Đếm mã hàng có số tồn thực tế lớn hơn 0.\"",
            StringComparison.Ordinal));
        Assert.IsFalse(xaml.Contains(
            "Text=\"Số vị thế có chênh lệch số lượng hoặc chưa tính đủ giá vốn.\"",
            StringComparison.Ordinal));
    }

    private sealed class TestAccessStateService : IAccessStateService
    {
        public AccessSnapshot Current { get; private set; } = AccessSnapshot.Anonymous;
        public event EventHandler<AccessSnapshot>? Changed;

        public bool HasPermission(string? permission) =>
            Current.IsAuthenticated
            && !string.IsNullOrWhiteSpace(permission)
            && Current.Permissions.Contains(permission, StringComparer.Ordinal);

        public bool CanNavigate(string? navigationKey) => false;
        public bool CanUseAction(string? requiredPermission) => HasPermission(requiredPermission);
        public bool TryApply(InternalMeData data, out string errorMessage)
        {
            errorMessage = string.Empty;
            return false;
        }

        public void ApplyAuthenticated(string ownerKind, params string[] permissions)
        {
            Current = new AccessSnapshot(
                true,
                "user:test",
                "employee-test",
                "owner.test",
                "Owner Test",
                ownerKind,
                ["system:security-owner"],
                permissions,
                new AccessScopesData(),
                DateTimeOffset.UtcNow.AddHours(1));
            Changed?.Invoke(this, Current);
        }

        public void Clear()
        {
            Current = AccessSnapshot.Anonymous;
            Changed?.Invoke(this, Current);
        }
    }

    private sealed class TestFulfillmentService : IFulfillmentService
    {
        public int ListWorkCalls { get; private set; }

        public Task<IReadOnlyList<FulfillmentWorkItemData>> ListWorkAsync(CancellationToken cancellationToken = default)
        {
            ListWorkCalls++;
            return Task.FromResult<IReadOnlyList<FulfillmentWorkItemData>>([]);
        }

        public Task<FulfillmentSuggestionData> GetSuggestionsAsync(string demandId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FulfillmentSuggestionData());

        public Task<FulfillmentOrderAllocationResultData> AllocateOrderAsync(string salesOrderId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FulfillmentOrderAllocationResultData());

        public Task<FulfillmentMutationResultData> AllocateDemandAsync(string demandId, string mode, string? quantity, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FulfillmentMutationResultData());

        public Task<FulfillmentMutationResultData> PickAsync(string allocationId, string quantity, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FulfillmentMutationResultData());

        public Task<FulfillmentMutationResultData> PackAsync(string allocationId, string quantity, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FulfillmentMutationResultData());

        public Task<InventoryHoldBreakdownData> GetHoldBreakdownAsync(string warehouseId, string baseVariantId, string salesOrderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new InventoryHoldBreakdownData());
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
