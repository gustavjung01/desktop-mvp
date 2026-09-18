using CongTy.Desktop.Purchasing;

namespace CongTy.UnitTests;

[TestClass]
public sealed class GoodsReceiptParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalEndpointsAndIdempotencyForEveryMutation()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "GoodsReceiptService.cs");

        foreach (var endpoint in new[]
        {
            "/api/goods-receipts?limit=1000&offset=0",
            "/api/goods-receipts/{RequireId(id)}",
            "/api/goods-receipts/tracking-requirements?purchaseOrderId=",
            "/api/goods-receipts/{RequireId(id)}/post",
            "/api/goods-receipts/{RequireId(id)}/reverse"
        })
        {
            StringAssert.Contains(service, endpoint);
        }

        StringAssert.Contains(service, "PostIdempotentDataAsync<TRequest, GoodsReceiptData>");
        StringAssert.Contains(service, "PatchIdempotentDataAsync<GoodsReceiptDraftRequest, GoodsReceiptData>");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(key)");
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("new Random", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesCanonicalPermissionsConcurrencyAndDispatcher()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptViewModel.cs");

        foreach (var permission in new[]
        {
            "core.goods-receipt.read",
            "core.goods-receipt.create",
            "core.goods-receipt.update",
            "core.goods-receipt.post",
            "core.goods-receipt.reverse",
            "core.goods-receipt.variance",
            "core.purchase-order.read"
        })
        {
            StringAssert.Contains(vm, permission);
        }

        StringAssert.Contains(vm, "ExpectedRevision = _editing?.Revision");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"goods-receipt-save\")");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"goods-receipt-post\")");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"goods-receipt-reverse\")");
        StringAssert.Contains(vm, "_actionKeys.TryGetValue(identity, out var key)");
        StringAssert.Contains(vm, "_draftAttemptKey ??=");
        StringAssert.Contains(vm, "Dispatcher.CurrentDispatcher");
        StringAssert.Contains(vm, "_uiDispatcher.BeginInvoke((Action)HandleAccessChanged)");
    }

    [TestMethod]
    public void ViewModel_EnforcesReceiptVarianceAndTrackingBoundaries()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptViewModel.cs");

        StringAssert.Contains(vm, "order.Status is not (\"approved\" or \"partially_received\")");
        StringAssert.Contains(vm, "accepted > remaining");
        StringAssert.Contains(vm, "rejected > 0 || line.FinalizeLine");
        StringAssert.Contains(vm, "QualityReasonCode");
        StringAssert.Contains(vm, "QualityNote");
        StringAssert.Contains(vm, "policy.LocationRequired");
        StringAssert.Contains(vm, "policy.LotTrackingMode");
        StringAssert.Contains(vm, "policy.ExpiryTrackingMode");

        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptView.xaml");
        StringAssert.Contains(view, "Chỉ phần Chấp nhận được ghi vào tồn kho.");
    }

    [TestMethod]
    public void Ui_FollowsWebOrderAndCompactCardRule()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptView.xaml");

        var expected = new[]
        {
            "Text=\"Tổng phiếu\"",
            "Text=\"Nháp\"",
            "Text=\"Đã ghi sổ\"",
            "Text=\"Đã đảo\"",
            "Text=\"Tìm kiếm\"",
            "Text=\"Trạng thái\"",
            "Text=\"Danh sách phiếu\"",
            "Text=\"Nhập hàng và ghi sổ tồn kho\""
        };

        var cursor = -1;
        foreach (var marker in expected)
        {
            var next = view.IndexOf(marker, cursor + 1, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự: {marker}");
            cursor = next;
        }

        StringAssert.Contains(view, "OfficeSummaryCardStyle");
        StringAssert.Contains(view, "OfficeSummaryLabelStyle");
        StringAssert.Contains(view, "OfficeSummaryValueStyle");
        Assert.IsFalse(view.Contains("<UniformGrid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Ui_CoversListEditorDetailActionsAndPrint()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptView.xaml.cs");

        foreach (var marker in new[]
        {
            "Header=\"STT\"",
            "Header=\"Số phiếu\"",
            "Header=\"Đơn mua hàng\"",
            "Header=\"Kho nhận\"",
            "Header=\"Ngày nhận\"",
            "Header=\"Trạng thái\"",
            "Header=\"Số dòng\"",
            "Header=\"Tổng SL\"",
            "Content=\"Xem\"",
            "Content=\"In\"",
            "Content=\"Sửa\"",
            "Content=\"Ghi sổ\"",
            "Content=\"Đảo phiếu\"",
            "Text=\"Dòng nhận hàng\"",
            "Header=\"Chấp nhận\"",
            "Header=\"Loại\"",
            "Header=\"Chốt thiếu\"",
            "Header=\"Mã lý do\"",
            "Header=\"Vị trí kho\"",
            "Header=\"Số lô\"",
            "Header=\"Ngày SX\"",
            "Header=\"HSD\"",
            "Text=\"CHI TIẾT PHIẾU NHẬN HÀNG\"",
            "Content=\"Tạo phiếu trả NCC\""
        })
        {
            StringAssert.Contains(view, marker);
        }

        StringAssert.Contains(code, "DocumentPrintTemplateRuntime.LoadForPrintAsync");
        StringAssert.Contains(code, "\"GOODS_RECEIPT\"");
        StringAssert.Contains(code, "GoodsReceiptPrintPreview.Create(receipt, template)");
        StringAssert.Contains(code, "PreparePostAsync(row)");
        StringAssert.Contains(code, "ConfirmReverseAsync()");
        StringAssert.Contains(code, "SupplierReturnRequested?.Invoke(receiptId)");
        StringAssert.Contains(
            ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptViewModel.cs"),
            "CanStartSupplierReturnFromDetail => CanRead && _detail?.Status == \"posted\"");
    }

    [TestMethod]
    public void Shell_WiresGoodsReceiptsAndSupplierReturnsWithoutBreakingWorkspaceOrder()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 24");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 25");
        StringAssert.Contains(shell, "await _tripDispatch.EnsureLoadedAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 26");
        StringAssert.Contains(shell, "\"purchasing.goods-receipts\" => \"Phiếu nhận hàng\"");
        StringAssert.Contains(shell, "CanViewGoodsReceipts");
        StringAssert.Contains(shell, "NavigateGoodsReceiptsAsync");

        StringAssert.Contains(xaml, "Tag=\"{Binding IsGoodsReceiptsSelected}\"");
        StringAssert.Contains(xaml, "Click=\"GoodsReceipts_OnClick\"");
        StringAssert.Contains(xaml, "x:Name=\"GoodsReceiptHost\"");
        Assert.IsFalse(xaml.Contains("IsEnabled=\"False\"><TextBlock Text=\"Phiếu nhận hàng\"", StringComparison.Ordinal));
        StringAssert.Contains(xaml, "Tag=\"{Binding IsSupplierReturnsSelected}\"");
        StringAssert.Contains(xaml, "Click=\"SupplierReturns_OnClick\"");
        Assert.IsFalse(
            xaml.Contains("IsEnabled=\"False\"><TextBlock Text=\"Phiếu trả nhà cung cấp\"", StringComparison.Ordinal));

        StringAssert.Contains(code, "GoodsReceiptHost.Content = goodsReceiptView");
        StringAssert.Contains(code, "goodsReceiptView.SupplierReturnRequested += GoodsReceiptView_OnSupplierReturnRequested");
        StringAssert.Contains(code, "NavigateGoodsReceiptsAsync()");
        StringAssert.Contains(code, "OpenCreateFromReceiptAsync(receiptId)");
        StringAssert.Contains(app, "AddSingleton<IGoodsReceiptService, GoodsReceiptService>()");
        StringAssert.Contains(app, "AddSingleton<GoodsReceiptViewModel>()");
        StringAssert.Contains(app, "AddSingleton<GoodsReceiptView>()");
    }

    [TestMethod]
    public void Audit_LocksCanonicalReceiptBoundaryAndNoDatabaseWork()
    {
        var audit = ReadRepoFile("docs", "parity", "UI6_4_GOODS_RECEIPTS_AUDIT.md");

        StringAssert.Contains(audit, "core.goods-receipt.variance");
        StringAssert.Contains(audit, "PATCH /api/goods-receipts/:id");
        StringAssert.Contains(audit, "CanonicalIdempotencyKeyProvider");
        StringAssert.Contains(audit, "expectedRevision");
        StringAssert.Contains(audit, "Accepted mới post vào inventory");
        StringAssert.Contains(audit, "Phiếu trả NCC thuộc UI-6.5");
        StringAssert.Contains(audit, "Không cần backend, DB hoặc migration mới");
    }

    [TestMethod]
    public void Presentation_UsesCanonicalReceiptStatuses()
    {
        Assert.AreEqual("Nháp", GoodsReceiptPresentation.Status("draft"));
        Assert.AreEqual("Đã ghi sổ", GoodsReceiptPresentation.Status("posted"));
        Assert.AreEqual("Đã đảo", GoodsReceiptPresentation.Status("reversed"));
        Assert.AreEqual("1.234,5", GoodsReceiptPresentation.Number("1234.5"));
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
