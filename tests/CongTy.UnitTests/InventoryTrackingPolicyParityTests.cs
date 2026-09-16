using CongTy.Contracts;
using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class InventoryTrackingPolicyParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeLabelsAndCandidateSearch()
    {
        Assert.AreEqual("Không quản lý theo lô", InventoryTrackingPolicyPresentation.LotLabel("NONE"));
        Assert.AreEqual("Bắt buộc quản lý theo lô", InventoryTrackingPolicyPresentation.LotLabel("REQUIRED"));
        Assert.AreEqual("Không quản lý hạn sử dụng", InventoryTrackingPolicyPresentation.ExpiryLabel("NONE"));
        Assert.AreEqual("Có thể nhập hạn sử dụng", InventoryTrackingPolicyPresentation.ExpiryLabel("OPTIONAL"));
        Assert.AreEqual("Bắt buộc nhập hạn sử dụng", InventoryTrackingPolicyPresentation.ExpiryLabel("REQUIRED"));

        var candidate = new InventoryTrackingPolicyCandidateData
        {
            BaseVariantId = "11111111-1111-4111-8111-111111111111",
            BaseSku = "SKU-BASE",
            ProductCode = "SP01",
            ProductName = "Nước uống",
            RelatedVariantSearchText = "SKU-THUNG SKU-LE",
            HasPolicy = true
        };

        StringAssert.Contains(InventoryTrackingPolicyPresentation.SearchText(candidate), "sku-thung");
        StringAssert.Contains(InventoryTrackingPolicyPresentation.CandidateLabel(candidate), "đã thiết lập");
    }

    [TestMethod]
    public void Service_UsesCanonicalEndpointsAndIdempotentPut()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "InventoryTrackingPolicyService.cs");

        StringAssert.Contains(service, "/api/inventory/tracking-policies?limit=1000&offset=0");
        StringAssert.Contains(service, "/api/inventory/tracking-policies/candidates?limit=2000&offset=0");
        StringAssert.Contains(service, "/api/inventory/tracking-policies/{Uri.EscapeDataString");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(idempotencyKey)");
        StringAssert.Contains(service, "PutIdempotentDataAsync<InventoryTrackingPolicySaveRequest, InventoryTrackingPolicyData>");
    }

    [TestMethod]
    public void ViewModel_UsesCanonicalPermissionsExpectedVersionAndRetryKeyReuse()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryTrackingPolicyViewModel.cs");

        StringAssert.Contains(vm, "_access.HasPermission(\"core.inventory.tracking-policy.read\")");
        StringAssert.Contains(vm, "_access.HasPermission(\"core.inventory.tracking-policy.manage\")");
        StringAssert.Contains(vm, "ExpectedVersion = _expectedVersion");
        StringAssert.Contains(vm, "_pendingSaveFingerprint");
        StringAssert.Contains(vm, "_pendingSaveKey");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"inventory-policy-save\")");
        StringAssert.Contains(vm, "if (next == \"NONE\") ExpiryTrackingMode = \"NONE\"");
        StringAssert.Contains(vm, "\"TRACKING_POLICY_CONFLICT\"");
    }

    [TestMethod]
    public void Ui_PreservesWebTwoColumnFormAndControlOrder()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryTrackingPolicyView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryTrackingPolicyView.xaml.cs");

        foreach (var text in new[]
        {
            "SKU tồn chuẩn và chính sách",
            "Thiết lập quản lý lô và hạn dùng",
            "Tìm SKU bất kỳ, SKU tồn chuẩn hoặc tên hàng",
            "Header=\"SKU tồn chuẩn\"",
            "Header=\"Trạng thái\"",
            "Header=\"Lô\"",
            "Header=\"Hạn dùng\"",
            "Text=\"Quản lý lô\"",
            "Text=\"Hạn sử dụng\"",
            "Vị trí được quản lý tại Cơ cấu Công Ty → Kho hàng",
            "Content=\"{Binding SaveText}\""
        })
        {
            StringAssert.Contains(view, text);
        }

        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.F");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.S");
        Assert.IsFalse(view.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("expectedVersion", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("UUID", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_EnablesDedicatedTrackingPolicyWorkspace()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "\"inventory.tracking-policies\" => \"Chính sách quản lý lô\"");
        StringAssert.Contains(shell, "public async Task NavigateInventoryTrackingPolicyAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 14");
        StringAssert.Contains(shell, "CanViewInventoryTrackingPolicy");

        StringAssert.Contains(main, "Tag=\"{Binding IsInventoryTrackingPolicySelected}\"");
        StringAssert.Contains(main, "Click=\"InventoryTrackingPolicy_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"InventoryTrackingPolicyHost\"");
        Assert.IsFalse(main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Chính sách lô\"", StringComparison.Ordinal));

        StringAssert.Contains(mainCode, "InventoryTrackingPolicyHost.Content = inventoryTrackingPolicyView");
        StringAssert.Contains(mainCode, "await _viewModel.NavigateInventoryTrackingPolicyAsync()");
        StringAssert.Contains(app, "AddSingleton<IInventoryTrackingPolicyService, InventoryTrackingPolicyService>()");
        StringAssert.Contains(app, "AddSingleton<InventoryTrackingPolicyViewModel>()");
        StringAssert.Contains(app, "AddSingleton<InventoryTrackingPolicyView>()");
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
