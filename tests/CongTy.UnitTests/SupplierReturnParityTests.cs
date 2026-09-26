using CongTy.Desktop.Purchasing;

namespace CongTy.UnitTests;

[TestClass]
public sealed class SupplierReturnParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalEndpointsAndIdempotencyForEveryMutation()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "SupplierReturnService.cs");

        foreach (var marker in new[]
        {
            "/api/supplier-returns?limit=1000&offset=0",
            "/api/supplier-returns/{RequireId(id)}",
            "/api/supplier-returns/source-lines?goodsReceiptId=",
            "\"submit\"",
            "\"approve\"",
            "\"cancel\"",
            "\"post\"",
            "\"reverse\""
        })
        {
            StringAssert.Contains(service, marker);
        }

        StringAssert.Contains(service, "PostIdempotentDataAsync<TRequest, SupplierReturnData>");
        StringAssert.Contains(service, "PatchIdempotentDataAsync<SupplierReturnDraftRequest, SupplierReturnData>");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(key)");
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("new Random", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesEightCanonicalPermissionsConcurrencyAndDispatcher()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "SupplierReturnViewModel.cs");

        foreach (var permission in new[]
        {
            "core.supplier-return.read",
            "core.supplier-return.create",
            "core.supplier-return.update",
            "core.supplier-return.submit",
            "core.supplier-return.approve",
            "core.supplier-return.cancel",
            "core.supplier-return.post",
            "core.supplier-return.reverse"
        })
        {
            StringAssert.Contains(vm, permission);
        }

        StringAssert.Contains(vm, "core.goods-receipt.read");
        StringAssert.Contains(vm, "CanReadSourceReceipts");
        StringAssert.Contains(vm, "ExpectedRevision = _editing?.Revision");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"supplier-return-save\")");
        StringAssert.Contains(vm, "_idempotencyKeys.Create($\"supplier-return-{action}\")");
        StringAssert.Contains(vm, "_actionKeys.TryGetValue(identity, out var key)");
        StringAssert.Contains(vm, "_draftAttemptKey ??=");
        StringAssert.Contains(vm, "CancellationTokenSource? _sourceLinesCts");
        StringAssert.Contains(vm, "requestedReceiptId");
        StringAssert.Contains(vm, "ListSourceLinesAsync(receipt.Id, requestCts.Token)");
        StringAssert.Contains(vm, "IsCurrentSourceRequest");
        StringAssert.Contains(vm, "OperationCanceledException");
        StringAssert.Contains(vm, "BuildActionAttemptIdentity");
        Assert.IsFalse(vm.Contains(
            "var identity = $\"{action}|{target.Id}|{target.Revision}|{ActionDate:yyyy-MM-dd}|{ActionReason.Trim()}\"",
            StringComparison.Ordinal));
        StringAssert.Contains(vm, "Dispatcher.CurrentDispatcher");
        StringAssert.Contains(vm, "_uiDispatcher.BeginInvoke((Action)HandleAccessChanged)");
    }

    [TestMethod]
    public void ViewModel_EnforcesSourceAndReturnableBoundaries()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "SupplierReturnViewModel.cs");

        StringAssert.Contains(vm, "receipt.Status != \"posted\"");
        StringAssert.Contains(vm, "lines.Count == 0");
        StringAssert.Contains(vm, "supplierIds.Length != 1 || warehouseIds.Length != 1");
        StringAssert.Contains(vm, "quantity > returnable");
        StringAssert.Contains(vm, "bắt buộc nhập mã lý do và ghi chú lý do");
        StringAssert.Contains(vm, "SourceSupplierId = line.SourceSupplierId");
        StringAssert.Contains(vm, "SourceWarehouseId = line.SourceWarehouseId");
        Assert.IsFalse(vm.Contains("loadedSource = await _service.ListSourceLinesAsync", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_LocksCanonicalLifecyclePolicy()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "SupplierReturnViewModel.cs");

        StringAssert.Contains(vm, "item.Status == \"draft\" && CanUpdate");
        StringAssert.Contains(vm, "item.Status == \"draft\" && CanSubmit");
        StringAssert.Contains(vm, "item.Status == \"pending_approval\" && CanApprove");
        StringAssert.Contains(vm, "(item.Status is \"draft\" or \"pending_approval\" or \"approved\") && CanCancel");
        StringAssert.Contains(vm, "item.Status == \"approved\" && CanPost");
        StringAssert.Contains(vm, "item.Status == \"posted\" && CanReverse");
        StringAssert.Contains(vm, "Vui lòng nhập lý do trước khi xác nhận.");
    }

    [TestMethod]
    public void Ui_UsesCompactKpisAndCoversAllActions()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "SupplierReturnView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "SupplierReturnView.xaml.cs");

        foreach (var marker in new[]
        {
            "Text=\"Tổng phiếu\"",
            "Text=\"Nháp\"",
            "Text=\"Chờ duyệt\"",
            "Text=\"Đã ghi sổ\"",
            "Text=\"Tìm kiếm\"",
            "Text=\"Trạng thái\"",
            "Text=\"Danh sách phiếu trả\"",
            "Text=\"Dòng hàng có thể trả\"",
            "Header=\"Còn trả\"",
            "Header=\"SL trả\"",
            "Header=\"Mã lý do\"",
            "Header=\"Ghi chú lý do\"",
            "Content=\"Gửi duyệt\"",
            "Content=\"Duyệt\"",
            "Content=\"Hủy\"",
            "Content=\"Ghi sổ\"",
            "Content=\"Đảo\"",
            "Text=\"CHI TIẾT PHIẾU TRẢ NHÀ CUNG CẤP\""
        })
        {
            StringAssert.Contains(view, marker);
        }

        StringAssert.Contains(view, "OfficeSummaryCardStyle");
        StringAssert.Contains(view, "OfficeSummaryLabelStyle");
        StringAssert.Contains(view, "OfficeSummaryValueStyle");
        StringAssert.Contains(
            view,
            "Visibility=\"{Binding ActionReasonRequired, Converter={StaticResource BoolToVisibility}}\"");
        Assert.IsFalse(view.Contains("<UniformGrid", StringComparison.Ordinal));

        StringAssert.Contains(code, "DocumentPrintTemplateRuntime.LoadForPrintAsync");
        StringAssert.Contains(code, "\"SUPPLIER_RETURN\"");
        StringAssert.Contains(code, "SupplierReturnPrintPreview.Print");
        StringAssert.Contains(code, "OpenCreateFromReceiptAsync(string receiptId)");
        StringAssert.Contains(code, "BeginCreateAsync(receiptId)");
        StringAssert.Contains(code, "BeginAction(sender, \"submit\")");
        StringAssert.Contains(code, "BeginAction(sender, \"approve\")");
        StringAssert.Contains(code, "BeginAction(sender, \"cancel\")");
        StringAssert.Contains(code, "BeginAction(sender, \"post\")");
        StringAssert.Contains(code, "BeginAction(sender, \"reverse\")");
    }

    [TestMethod]
    public void Shell_PreservesUi45AndWiresSupplierReturnsAfterIt()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 27");
        StringAssert.Contains(shell, "await _deliveryAttempts.EnsureLoadedAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 28");
        StringAssert.Contains(shell, "await _supplierReturns.EnsureLoadedAsync()");
        StringAssert.Contains(shell, "\"purchasing.supplier-returns\" => \"Phiếu trả nhà cung cấp\"");
        StringAssert.Contains(shell, "CanViewSupplierReturns");

        StringAssert.Contains(xaml, "x:Name=\"DeliveryAttemptHost\"");
        StringAssert.Contains(xaml, "x:Name=\"SupplierReturnHost\"");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsSupplierReturnsSelected}\"");
        StringAssert.Contains(xaml, "Click=\"SupplierReturns_OnClick\"");
        Assert.IsFalse(xaml.Contains("IsEnabled=\"False\"><TextBlock Text=\"Phiếu trả nhà cung cấp\"", StringComparison.Ordinal));

        StringAssert.Contains(code, "DeliveryAttemptHost.Content = deliveryAttemptView");
        StringAssert.Contains(code, "SupplierReturnHost.Content = supplierReturnView");
        StringAssert.Contains(code, "NavigateSupplierReturnsAsync()");

        StringAssert.Contains(app, "AddSingleton<IDeliveryAttemptService, DeliveryAttemptService>()");
        StringAssert.Contains(app, "AddSingleton<ISupplierReturnService, SupplierReturnService>()");
        StringAssert.Contains(app, "AddSingleton<SupplierReturnViewModel>()");
        StringAssert.Contains(app, "AddSingleton<SupplierReturnView>()");
    }

    [TestMethod]
    public void Audit_LocksSupplierReturnBoundaryAndNoDatabaseWork()
    {
        var audit = ReadRepoFile("docs", "parity", "UI6_5_SUPPLIER_RETURNS_AUDIT.md");

        StringAssert.Contains(audit, "core.supplier-return.reverse");
        StringAssert.Contains(audit, "PATCH /api/supplier-returns/:id");
        StringAssert.Contains(audit, "CanonicalIdempotencyKeyProvider");
        StringAssert.Contains(audit, "expectedRevision");
        StringAssert.Contains(audit, "returnableQuantity = acceptedQuantity - postedReturnQuantity");
        StringAssert.Contains(audit, "Không cần backend, DB hoặc migration mới");
        StringAssert.Contains(audit, "Không mở payable/payment/credit-note UI");
    }

    [TestMethod]
    public void Presentation_UsesCanonicalSupplierReturnStatuses()
    {
        Assert.AreEqual("Nháp", SupplierReturnPresentation.Status("draft"));
        Assert.AreEqual("Chờ duyệt", SupplierReturnPresentation.Status("pending_approval"));
        Assert.AreEqual("Đã duyệt", SupplierReturnPresentation.Status("approved"));
        Assert.AreEqual("Đã ghi sổ", SupplierReturnPresentation.Status("posted"));
        Assert.AreEqual("Đã đảo", SupplierReturnPresentation.Status("reversed"));
        Assert.AreEqual("Đã hủy", SupplierReturnPresentation.Status("cancelled"));
        Assert.AreEqual("1.234,5", SupplierReturnPresentation.Number("1234.5"));
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
