using CongTy.Contracts;
using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class OpeningBalanceParityTests
{
    [TestMethod]
    public void CsvParser_AcceptsBusinessHeadersAndCommonDelimiters()
    {
        var rows = OpeningBalanceCsv.Parse(
            "SKU;Số lượng;Vị trí;Mã lô;Hạn sử dụng\nSKU-01;12;A-01;LOT-1;2027-09-01");

        Assert.HasCount(1, rows);
        Assert.AreEqual("SKU-01", rows[0].Sku);
        Assert.AreEqual("12", rows[0].SourceQuantity);
        Assert.AreEqual("A-01", rows[0].LocationCode);
        Assert.AreEqual("LOT-1", rows[0].LotCode);
        Assert.AreEqual("2027-09-01", rows[0].ExpiryDate);
    }

    [TestMethod]
    public void Checksum_IsStableAndChangesWithDraft()
    {
        var draft = new OpeningBalanceOperatorDraftData
        {
            WarehouseId = "11111111-1111-4111-8111-111111111111",
            SourceKey = "TONDAUKY-2026",
            SourceFilename = "ton.csv",
            DocumentDate = "2026-09-16",
            Metadata = new OpeningBalanceOperatorMetadataData
            {
                OriginalFilename = "ton.csv",
                DefaultLocationCode = "A-01"
            },
            Rows =
            [
                new OpeningBalanceDraftRowData
                {
                    Sku = "SKU-01",
                    SourceQuantity = "12",
                    LocationCode = "A-01",
                    Metadata = []
                }
            ]
        };

        var first = OpeningBalanceChecksum.Compute(draft);
        var second = OpeningBalanceChecksum.Compute(draft);
        Assert.AreEqual(first, second);
        Assert.AreEqual(64, first.Length);

        var changed = draft with
        {
            Rows =
            [
                draft.Rows[0] with { SourceQuantity = "13" }
            ]
        };
        Assert.AreNotEqual(first, OpeningBalanceChecksum.Compute(changed));
    }

    [TestMethod]
    public void Service_UsesCanonicalOperatorEndpointsAndSharedIdempotentPost()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "OpeningBalanceService.cs");

        StringAssert.Contains(service, "/api/inventory/opening-balances/operator/warehouses");
        StringAssert.Contains(service, "/api/inventory/opening-balances/operator/locations?warehouseId=");
        StringAssert.Contains(service, "/api/inventory/opening-balances/operator/validate");
        StringAssert.Contains(service, "/api/inventory/opening-balances/operator/post");
        StringAssert.Contains(service, "/api/inventory/opening-balances?limit=200");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(idempotencyKey)");
        StringAssert.Contains(service, "PostIdempotentDataAsync<OpeningBalanceOperatorRequestData, OpeningBalancePostResultData>");
    }

    [TestMethod]
    public void ViewModel_UsesCanonicalPermissionsChecksumAndRetryKeyReuse()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "OpeningBalanceViewModel.cs");

        StringAssert.Contains(vm, "_access.HasPermission(\"core.inventory.opening-balance.import\")");
        StringAssert.Contains(vm, "_access.HasPermission(\"core.inventory.read\")");
        StringAssert.Contains(vm, "OpeningBalanceChecksum.Compute(draft)");
        StringAssert.Contains(vm, "_pendingPostFingerprint");
        StringAssert.Contains(vm, "_pendingPostKey");
        StringAssert.Contains(vm, "private long _draftRevision;");
        StringAssert.Contains(vm, "revision != _draftRevision");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"inventory-opening-balance-post\")");
        StringAssert.Contains(vm, "string.Equals(checksum, _validationChecksum");
        StringAssert.Contains(vm, "SourceKey.Trim().ToUpperInvariant()");
    }

    [TestMethod]
    public void Ui_PreservesWebWorkflowSectionsAndPreviewColumnOrder()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "OpeningBalanceView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "OpeningBalanceView.xaml.cs");

        foreach (var text in new[]
        {
            "Text=\"Nhập tồn đầu kỳ\"",
            "Content=\"Về tra cứu tồn kho\"",
            "Text=\"Tải tệp mẫu\"",
            "Content=\"Tải mẫu Excel/CSV\"",
            "Text=\"Chọn tệp đã điền\"",
            "Content=\"{Binding ValidateText}\"",
            "Content=\"{Binding PostText}\"",
            "Text=\"Thông tin đợt nhập\"",
            "Text=\"Kho *\"",
            "Text=\"Vị trí mặc định\"",
            "Text=\"Mã đợt dữ liệu *\"",
            "Text=\"Ngày ghi nhận\"",
            "Text=\"Tệp đã chọn\"",
            "Text=\"Chọn kho\"",
            "Text=\"Không chọn — lấy theo từng dòng CSV\"",
            "Text=\"Xem trước dữ liệu\"",
            "Text=\"Kết quả kiểm tra\"",
            "Text=\"Lịch sử nhập tồn đầu kỳ\""
        })
        {
            StringAssert.Contains(view, text);
        }

        var headers = new[]
        {
            "Header=\"Dòng\"",
            "Header=\"Kho\"",
            "Header=\"Vị trí\"",
            "Header=\"SKU\"",
            "Header=\"Tên hàng\"",
            "Header=\"Số lượng\"",
            "Header=\"Chính sách\"",
            "Header=\"Lô hàng\"",
            "Header=\"Hạn dùng\"",
            "Header=\"Trạng thái\""
        };
        var previous = -1;
        foreach (var header in headers)
        {
            var index = view.IndexOf(header, StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, index);
            previous = index;
        }

        StringAssert.Contains(view, "IsEnabled=\"{Binding CanChooseFile}\"");
        StringAssert.Contains(view, "IsEnabled=\"{Binding CanSelectWarehouse}\"");
        StringAssert.Contains(view, "IsEnabled=\"{Binding CanSelectDefaultLocation}\"");
        StringAssert.Contains(view, "Text=\"{Binding FilenameDisplay, Mode=OneWay}\"");
        StringAssert.Contains(code, "SaveFileDialog");
        StringAssert.Contains(code, "OpenFileDialog");
        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.O");

        Assert.IsFalse(view.Contains("UUID", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("checksum", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_UsesDedicatedOpeningBalanceWorkspace()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "\"inventory.opening-balances\" => \"Thiết lập tồn đầu kỳ\"");
        StringAssert.Contains(shell, "public async Task NavigateOpeningBalanceAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 18");
        StringAssert.Contains(shell, "CanViewOpeningBalance");

        StringAssert.Contains(main, "Tag=\"{Binding IsInventoryOpeningBalancesSelected}\"");
        StringAssert.Contains(main, "Click=\"InventoryOpeningBalances_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"OpeningBalanceHost\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Thiết lập tồn đầu kỳ\"", StringComparison.Ordinal));

        StringAssert.Contains(mainCode, "OpeningBalanceHost.Content = openingBalanceView");
        StringAssert.Contains(mainCode, "openingBalanceView.BackToLookupRequested += OpeningBalanceView_OnBackToLookupRequested");
        StringAssert.Contains(mainCode, "await _viewModel.NavigateOpeningBalanceAsync()");
        StringAssert.Contains(mainCode, "await _viewModel.NavigateInventoryLookupAsync()");

        StringAssert.Contains(app, "AddSingleton<IOpeningBalanceService, OpeningBalanceService>()");
        StringAssert.Contains(app, "AddSingleton<OpeningBalanceViewModel>()");
        StringAssert.Contains(app, "AddSingleton<OpeningBalanceView>()");
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
