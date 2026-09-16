using System.Windows.Data;
using System.Windows.Threading;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Purchasing;

namespace CongTy.UnitTests;

[TestClass]
public sealed class PurchaseOrderParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalPurchaseOrderEndpointsAndIdempotentMutations()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "PurchaseOrderService.cs");

        foreach (var marker in new[]
        {
            "/api/purchase-orders?limit=1000&offset=0",
            "/api/purchase-orders/{RequireId(id)}",
            "/api/purchase-orders/sku-search?",
            "/api/supplier-purchase-prices/resolve",
            "/api/goods-receipts?purchaseOrderId="
        })
        {
            StringAssert.Contains(service, marker);
        }

        StringAssert.Contains(service, "PostIdempotentDataAsync<PurchaseOrderDraftRequest, PurchaseOrderData>");
        StringAssert.Contains(service, "PatchIdempotentDataAsync<PurchaseOrderDraftRequest, PurchaseOrderData>");
        StringAssert.Contains(service, "PostIdempotentDataAsync<PurchaseOrderActionRequest, PurchaseOrderData>");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(key)");
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("new Random", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesCanonicalPermissionsExpectedRevisionAndSharedKeyProvider()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchaseOrderViewModel.cs");

        foreach (var permission in new[]
        {
            "core.purchase-order.read",
            "core.purchase-order.create",
            "core.purchase-order.update",
            "core.purchase-order.submit",
            "core.purchase-order.approve",
            "core.purchase-order.cancel",
            "core.purchase-order.price.read",
            "core.purchase-order.price.override"
        })
        {
            StringAssert.Contains(vm, permission);
        }

        StringAssert.Contains(vm, "ExpectedRevision = _editingOrder?.Revision");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(\"purchase-order-save\")");
        StringAssert.Contains(vm, "_idempotencyKeys.Create($\"purchase-order-{action}\")");
        StringAssert.Contains(vm, "_actionKeys.TryGetValue(identity, out var key)");
        StringAssert.Contains(vm, "_draftAttemptKey ??=");
        StringAssert.Contains(vm, "MarkDraftChanged()");
        Assert.IsFalse(vm.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Ui_FollowsWebOrderAndSharedCompactCardRules()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchaseOrderView.xaml");

        var expected = new[]
        {
            "Text=\"Tổng đơn\"",
            "Text=\"Đơn nháp\"",
            "Text=\"Chờ duyệt\"",
            "Text=\"Tìm kiếm\"",
            "Text=\"Trạng thái\"",
            "Text=\"Danh sách mua hàng\"",
            "Text=\"Đơn mua hàng nhà cung cấp\""
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
        StringAssert.Contains(view, "Style=\"{StaticResource OfficeCardStyle}\"");
        Assert.IsFalse(view.Contains("<UniformGrid", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Text=\"Đơn mua hàng\"\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Ui_CoversListActionsEditorDetailBulkAndPrint()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchaseOrderView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchaseOrderView.xaml.cs");

        foreach (var header in new[]
        {
            "Header=\"STT\"",
            "Header=\"Số đơn\"",
            "Header=\"Ngày đặt\"",
            "Header=\"Nhà cung cấp\"",
            "Header=\"Kho nhận\"",
            "Header=\"Số dòng\"",
            "Header=\"Tổng giá trị\"",
            "Header=\"Trạng thái\"",
            "Header=\"Cập nhật\"",
            "Header=\"Thao tác\""
        })
        {
            StringAssert.Contains(view, header);
        }

        foreach (var action in new[] { "Content=\"Xem\"", "Content=\"In\"", "Content=\"Sửa\"", "Content=\"Gửi duyệt\"", "Content=\"Duyệt\"", "Content=\"Hủy\"" })
        {
            StringAssert.Contains(view, action);
        }

        foreach (var editor in new[]
        {
            "Text=\"Nhà cung cấp\"",
            "Text=\"Kho nhận\"",
            "Text=\"Ngày đặt\"",
            "Text=\"Dự kiến nhận\"",
            "Text=\"Tham chiếu nhà cung cấp\"",
            "Text=\"Ghi chú đơn\"",
            "Text=\"Thêm SKU\"",
            "Content=\"Chọn từ danh mục\"",
            "Header=\"Nhập nhiều dòng\"",
            "Content=\"Tải tệp mẫu XLSX\"",
            "Text=\"Dòng mua hàng\"",
            "Header=\"Số lượng\"",
            "Header=\"Đơn giá\"",
            "Header=\"Chiết khấu\"",
            "Header=\"Thuế %\""
        })
        {
            StringAssert.Contains(view, editor);
        }

        StringAssert.Contains(view, "Text=\"Lịch sử nhận hàng\"");
        StringAssert.Contains(view, "Text=\"Tổng cộng\"");
        StringAssert.Contains(code, "PurchaseOrderPrintPreview.Create(order)");
        StringAssert.Contains(code, "PurchaseOrderBulkImport.CreateTemplate()");
        StringAssert.Contains(code, "PurchaseOrderBulkImport.ReadXlsx");
    }

    [TestMethod]
    public void BulkTemplate_RoundTripsSampleAndTextParser()
    {
        var bytes = PurchaseOrderBulkImport.CreateTemplate();
        Assert.IsGreaterThan(100, bytes.Length);

        var rows = PurchaseOrderBulkImport.ReadXlsx(bytes);
        Assert.HasCount(1, rows);
        Assert.AreEqual("SKU-MAU", rows[0].Sku);
        Assert.AreEqual("1", rows[0].Quantity);
        Assert.AreEqual("TOTAL_AMOUNT", rows[0].DiscountMode);

        var pasted = PurchaseOrderBulkImport.ParseText("SKU\tSố lượng\tĐơn giá thủ công\nABC-01\t2.5\t12000");
        Assert.HasCount(1, pasted);
        Assert.AreEqual("ABC-01", pasted[0].Sku);
        Assert.AreEqual("2.5", pasted[0].Quantity);
        Assert.AreEqual("12000", pasted[0].UnitPrice);
    }

    [TestMethod]
    public void Presentation_UsesOfficeStatusesIncludingReceiptStates()
    {
        Assert.AreEqual("Nháp", PurchaseOrderPresentation.Status("draft"));
        Assert.AreEqual("Chờ duyệt", PurchaseOrderPresentation.Status("pending_approval"));
        Assert.AreEqual("Đã nhận một phần", PurchaseOrderPresentation.Status("partially_received"));
        Assert.AreEqual("Đã ghi sổ", PurchaseOrderPresentation.Status("posted"));
        Assert.AreEqual("Đã đảo", PurchaseOrderPresentation.Status("reversed"));
        Assert.AreEqual("1.234,5", PurchaseOrderPresentation.Number("1234.5"));
    }

    [TestMethod]
    public void AccessChanged_FromWorkerThread_MarshalsCollectionRefreshToOwnerDispatcher()
    {
        Exception? testFailure = null;
        var completed = false;

        var thread = new Thread(() =>
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                var access = new TestAccessStateService();
                var viewModel = new PurchaseOrderViewModel(
                    null!,
                    null!,
                    null!,
                    access,
                    null!);

                _ = CollectionViewSource.GetDefaultView(viewModel.Rows);

                Exception? eventFailure = null;
                Task.Run(() =>
                {
                    try
                    {
                        access.RaiseChanged();
                    }
                    catch (Exception exception)
                    {
                        eventFailure = exception;
                    }
                }).GetAwaiter().GetResult();

                var frame = new DispatcherFrame();
                _ = dispatcher.BeginInvoke(
                    DispatcherPriority.ApplicationIdle,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);

                if (eventFailure is not null)
                {
                    throw new AssertFailedException(
                        $"Access change must not mutate the bound collection from the worker thread: {eventFailure}");
                }

                completed = true;
            }
            catch (Exception exception)
            {
                testFailure = exception;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)), "Dispatcher regression test timed out.");
        if (testFailure is not null)
        {
            Assert.Fail(testFailure.ToString());
        }

        Assert.IsTrue(completed);
    }

    [TestMethod]
    public void Audit_LocksWebBackendMatrixAndNoDatabaseWork()
    {
        var audit = ReadRepoFile("docs", "parity", "UI6_2_PURCHASE_ORDERS_AUDIT.md");
        StringAssert.Contains(audit, "/api/purchase-orders");
        StringAssert.Contains(audit, "CanonicalIdempotencyKeyProvider");
        StringAssert.Contains(audit, "retry cùng payload reuse key");
        StringAssert.Contains(audit, "Không cần backend, DB hoặc migration mới");
    }

    [TestMethod]
    public void Shell_PreservesUi42WorkspaceAndWiresPurchaseOrdersAfterIt()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 21");
        StringAssert.Contains(shell, "await _deliveryOrders.EnsureLoadedAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 22");
        StringAssert.Contains(shell, "await _purchaseOrders.EnsureLoadedAsync()");
        StringAssert.Contains(shell, "\"purchasing.purchase-orders\" => \"Đơn mua hàng\"");
        StringAssert.Contains(shell, "CanViewPurchaseOrders");

        StringAssert.Contains(xaml, "x:Name=\"DeliveryOrderHost\"");
        StringAssert.Contains(xaml, "x:Name=\"PurchaseOrderHost\"");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsPurchaseOrdersSelected}\"");
        StringAssert.Contains(xaml, "Click=\"PurchaseOrders_OnClick\"");

        StringAssert.Contains(code, "DeliveryOrderHost.Content = deliveryOrderView");
        StringAssert.Contains(code, "PurchaseOrderHost.Content = purchaseOrderView");
        StringAssert.Contains(code, "NavigatePurchaseOrdersAsync()");

        StringAssert.Contains(app, "AddSingleton<IDeliveryOrderService, DeliveryOrderService>()");
        StringAssert.Contains(app, "AddSingleton<IPurchaseOrderService, PurchaseOrderService>()");
        StringAssert.Contains(app, "AddSingleton<PurchaseOrderViewModel>()");
        StringAssert.Contains(app, "AddSingleton<PurchaseOrderView>()");
    }

    private sealed class TestAccessStateService : IAccessStateService
    {
        public AccessSnapshot Current { get; private set; } = AccessSnapshot.Anonymous;

        public event EventHandler<AccessSnapshot>? Changed;

        public bool HasPermission(string? permission) => false;

        public bool CanNavigate(string? navigationKey) => false;

        public bool CanUseAction(string? requiredPermission) => false;

        public bool TryApply(InternalMeData data, out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }

        public void Clear()
        {
            Current = AccessSnapshot.Anonymous;
        }

        public void RaiseChanged() => Changed?.Invoke(this, Current);
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
