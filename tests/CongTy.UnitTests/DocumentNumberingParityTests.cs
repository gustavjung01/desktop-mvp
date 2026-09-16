using CongTy.Desktop.DocumentNumbering;

namespace CongTy.UnitTests;

[TestClass]
public sealed class DocumentNumberingParityTests
{
    [TestMethod]
    public void Presentation_PreservesCanonicalDocumentTypesAndCycles()
    {
        Assert.HasCount(13, DocumentNumberingPresentation.DocumentTypes);
        CollectionAssert.AreEqual(
            new[]
            {
                "SALES_ORDER", "PURCHASE_ORDER", "GOODS_RECEIPT", "GOODS_ISSUE", "DELIVERY_ORDER",
                "INVENTORY_TRANSFER", "INVENTORY_ADJUSTMENT", "CUSTOMER_RETURN", "SUPPLIER_RETURN",
                "CUSTOMER_PAYMENT", "SUPPLIER_PAYMENT", "CUSTOMER_REFUND", "INVOICE"
            },
            DocumentNumberingPresentation.DocumentTypes.Select(item => item.Value).ToArray());
        CollectionAssert.AreEqual(
            new[] { "NONE", "YEARLY", "MONTHLY" },
            DocumentNumberingPresentation.ResetPolicies.Select(item => item.Value).ToArray());
    }

    [TestMethod]
    public void Source_MatchesWebHierarchyAndBackendContract()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "DocumentNumbering", "DocumentNumberingView.xaml");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "DocumentNumbering", "DocumentNumberingViewModel.cs");
        var service = ReadRepoFile("src", "CongTy.ApiClient", "DocumentNumberingService.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "DocumentNumberingContracts.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var window = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        var ordered = new[]
        {
            "Quy tắc đánh số dùng chung",
            "Tìm loại chứng từ hoặc tên quy tắc",
            "Cấp số tham chiếu",
            "Tiến độ đánh số",
            "Lịch sử cấp số"
        };
        var previous = -1;
        foreach (var label in ordered)
        {
            var index = view.IndexOf(label, StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, index);
            previous = index;
        }

        foreach (var label in new[]
        {
            "Loại chứng từ", "Tên quy tắc", "Ký hiệu đầu số", "Cấu trúc số", "Chu kỳ đánh lại số",
            "Thành phần có thể dùng: {PREFIX} {YYYY} {YY} {MM} {SEQ}",
            "Cấu trúc số đã được cố định vì quy tắc này đã có lịch sử cấp số."
        })
        {
            StringAssert.Contains(view, label);
        }

        Assert.IsFalse(view.Contains("sequence_width", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("start_counter", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("timezone", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Background=\"#", StringComparison.Ordinal));

        StringAssert.Contains(vm, "\"core.document-number.read\"");
        StringAssert.Contains(vm, "\"core.document-number.write\"");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"document-number-series-create\")");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"document-number-reference\")");
        Assert.IsGreaterThan(vm.IndexOf("await ReloadAndSelectAsync(saved.Id)", StringComparison.Ordinal), vm.IndexOf("CompleteCreateIntent();", StringComparison.Ordinal));
        Assert.IsGreaterThan(vm.IndexOf("await LoadHistoryCoreAsync(_selected.Id)", StringComparison.Ordinal), vm.IndexOf("CompleteAllocationIntent();", StringComparison.Ordinal));
        StringAssert.Contains(contracts, "[property: JsonPropertyName(\"expectedUpdatedAt\")]");

        StringAssert.Contains(service, "/api/document-number-series?limit=1000");
        StringAssert.Contains(service, "/allocations?limit=200");
        StringAssert.Contains(service, "/allocate");
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "PatchDataAsync");

        StringAssert.Contains(shell, "\"catalog.document-numbering\" => \"Số chứng từ\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 17");
        StringAssert.Contains(window, "x:Name=\"DocumentNumberingHost\"");
        StringAssert.Contains(window, "Click=\"CatalogDocumentNumbering_OnClick\"");
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
