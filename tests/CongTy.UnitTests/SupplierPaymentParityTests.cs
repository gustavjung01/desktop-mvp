using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Accounting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class SupplierPaymentParityTests
{
    [TestMethod]
    public void Presentation_UsesCurrentWebOfficeLanguage()
    {
        Assert.AreEqual("Chưa phân bổ", SupplierPaymentPresentation.Status("open"));
        Assert.AreEqual("Đã phân bổ một phần", SupplierPaymentPresentation.Status("partially_allocated"));
        Assert.AreEqual("Đã phân bổ hết", SupplierPaymentPresentation.Status("settled"));
        Assert.AreEqual("Đã đảo", SupplierPaymentPresentation.Status("reversed"));
        Assert.AreEqual("Chuyển khoản", SupplierPaymentPresentation.PaymentMethod("BANK_TRANSFER"));
        Assert.AreEqual("Tiền mặt", SupplierPaymentPresentation.PaymentMethod("CASH"));
        Assert.AreEqual("Khác", SupplierPaymentPresentation.PaymentMethod("OTHER"));
    }

    [TestMethod]
    public void Service_UsesCanonicalSupplierPaymentAndAllocationRoutes()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "SupplierPaymentService.cs");
        foreach (var route in new[]
        {
            "\"/api/supplier-payments?limit=1000\"",
            "\"/api/supplier-payments/allocation-targets\"",
            "\"/api/supplier-payments\"",
            "\"/api/payable-allocations\"",
            "/api/payable-allocations/{Uri.EscapeDataString",
            "/api/supplier-payments/{Uri.EscapeDataString"
        }) StringAssert.Contains(source, route);

        StringAssert.Contains(source, "PostIdempotentDataAsync");
        StringAssert.Contains(source, "idempotencyKeys.IsValid");
    }

    [TestMethod]
    public void ViewModel_UsesExactPermissionsAndReusesMutationKeysUntilSuccess()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "SupplierPaymentViewModel.cs");
        foreach (var permission in new[]
        {
            "core.supplier-payment.read",
            "core.supplier-payment.create",
            "core.supplier-payment.reverse",
            "core.payable.read",
            "core.payable-allocation.create",
            "core.payable-allocation.reverse"
        }) StringAssert.Contains(source, $"\"{permission}\"");

        StringAssert.Contains(source, "_mutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(source, "_idempotencyKeys.Create(scope)");
        StringAssert.Contains(source, "_mutationKeys.Remove(slot)");
        StringAssert.Contains(source, "item.SupplierId == _selectedPayment.SupplierId");
        StringAssert.Contains(source, "item.WarehouseId == _selectedPayment.WarehouseId");
        StringAssert.Contains(source, "string.Equals(item.CurrencyCode, _selectedPayment.CurrencyCode");
    }

    [TestMethod]
    public void View_PreservesCurrentWebSectionsFieldsActionsAndOfficeLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "SupplierPaymentView.xaml");
        foreach (var text in new[]
        {
            "Ghi nhận thanh toán", "Nhà cung cấp", "Kho", "Ngày thanh toán", "Số tiền",
            "Phương thức", "Tham chiếu ngân hàng", "Ghi chú", "Phiếu thanh toán",
            "Số phiếu", "Trạng thái", "Chi tiết và phân bổ", "Phân bổ vào chứng từ phải trả",
            "Lý do đảo", "Bắt buộc khi đảo phân bổ hoặc phiếu", "Đảo phiếu thanh toán",
            "Lịch sử phân bổ", "Chứng từ đích", "Ngày"
        }) StringAssert.Contains(view, text);

        Assert.IsFalse(view.Contains("In phiếu", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains(">API<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Shell_WiresUi77AfterPayablesAtWorkspace43()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.SupplierPayments.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.SupplierPayments.cs");
        var hook = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "\"accounting.supplier-payments\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 43");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 43");
        StringAssert.Contains(host, "workspaceTabs.Items[43]");
        StringAssert.Contains(host, "\"KẾ TOÁN MUA HÀNG\"");
        StringAssert.Contains(host, "\"Thanh toán nhà cung cấp\"");
        StringAssert.Contains(hook, "WirePayablesWorkspace();");
        StringAssert.Contains(hook, "WireSupplierPaymentsWorkspace();");
        StringAssert.Contains(xaml, "IsEnabled=\"False\"><TextBlock Text=\"Thanh toán nhà cung cấp\" /></Button>");
    }

    [TestMethod]
    public void Contracts_DeserializeCurrentSupplierPaymentShape()
    {
        const string json = """
        {
          "id":"11111111-1111-1111-1111-111111111111",
          "supplierId":"22222222-2222-2222-2222-222222222222",
          "warehouseId":"33333333-3333-3333-3333-333333333333",
          "documentNumber":"TTNCC-0001",
          "paymentDate":"2026-09-17",
          "currencyCode":"VND",
          "paymentMethod":"BANK_TRANSFER",
          "originalAmount":"1000000",
          "allocatedAmount":"400000",
          "remainingAmount":"600000",
          "status":"partially_allocated",
          "revision":2,
          "allocations":[{
            "id":"44444444-4444-4444-4444-444444444444",
            "sourcePayableDocumentId":"11111111-1111-1111-1111-111111111111",
            "targetPayableDocumentId":"55555555-5555-5555-5555-555555555555",
            "targetDocumentNumber":"PN-0001",
            "amount":"400000",
            "allocationDate":"2026-09-17",
            "reversed":false
          }]
        }
        """;

        var data = JsonSerializer.Deserialize<SupplierPaymentData>(json)
            ?? throw new InvalidOperationException("Không đọc được hợp đồng thanh toán nhà cung cấp.");
        Assert.AreEqual("TTNCC-0001", data.DocumentNumber);
        Assert.AreEqual("partially_allocated", data.Status);
        var allocation = data.Allocations.Single();
        Assert.AreEqual("PN-0001", allocation.TargetDocumentNumber);
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
