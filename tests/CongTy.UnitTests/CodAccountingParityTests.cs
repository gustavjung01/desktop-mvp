using CongTy.Desktop.Accounting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class CodAccountingParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeLabelsAndExactMoney()
    {
        Assert.AreEqual("Tiền mặt", CodAccountingPresentation.CollectionMethod("cash"));
        Assert.AreEqual("Thu khi giao hàng", CodAccountingPresentation.CollectionMethod("cash_on_delivery"));
        Assert.AreEqual("Chuyển khoản", CodAccountingPresentation.CollectionMethod("bank_transfer"));
        Assert.AreEqual("Thu một phần", CodAccountingPresentation.CollectionStatus("partially_collected"));
        Assert.AreEqual("Chờ xác nhận", CodAccountingPresentation.HandoverStatus("submitted"));
        Assert.AreEqual("Có chênh lệch", CodAccountingPresentation.HandoverStatus("discrepancy"));
        Assert.AreEqual("Đã đảo xác nhận", CodAccountingPresentation.HandoverStatus("acceptance_reversed"));
        Assert.AreEqual("1.234.567,89 VND", CodAccountingPresentation.Money("1234567.890000"));
        Assert.IsTrue(CodAccountingPresentation.TryScaled("12.345678", out var scaled));
        Assert.AreEqual("12.345678", CodAccountingPresentation.Decimal(scaled));
    }

    [TestMethod]
    public void Service_UsesCurrentReportingAndReconciliationContracts()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "CodAccountingService.cs");

        StringAssert.Contains(service, "\"/api/reporting/cod\"");
        StringAssert.Contains(service, "from=");
        StringAssert.Contains(service, "to=");
        StringAssert.Contains(service, "warehouseId=");
        StringAssert.Contains(service, "\"/api/cod-reconciliation?limit=1000\"");
        StringAssert.Contains(service, "/accept");
        StringAssert.Contains(service, "/collections/");
        StringAssert.Contains(service, "/handovers/");
        StringAssert.Contains(service, "/acceptances/");
        StringAssert.Contains(service, "/reverse");
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid");
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_SplitsPermissionsAndReusesKeyAndAcceptanceTimestamp()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CodAccountingViewModel.cs");

        StringAssert.Contains(vm, "core.reporting.cod.read");
        StringAssert.Contains(vm, "core.cod-reconciliation.read");
        StringAssert.Contains(vm, "core.cod-reconciliation.accept");
        StringAssert.Contains(vm, "core.cod-adjustment.create");

        StringAssert.Contains(vm, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(vm, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(vm, "_idempotencyKeys.Create($\"cod-accounting-{prefix}\")");
        StringAssert.Contains(vm, "_intentTimestamps.TryGetValue(intent, out var existing)");
        StringAssert.Contains(vm, "TimestampFor(intent)");
        StringAssert.Contains(vm, "CompleteIntent(intent)");
        StringAssert.Contains(vm, "var ownsBusy = _busyAction is null");
        Assert.IsFalse(vm.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesWebFiltersCardsAndSixTabOrder()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CodAccountingView.xaml");

        foreach (var text in new[]
        {
            "Từ ngày",
            "Đến ngày",
            "Kho",
            "Khoản tiền tài xế đang giữ",
            "Bàn giao chờ kế toán nhận",
            "Lời hẹn thu đã quá hạn",
            "Cần kiểm tra / chênh lệch",
            "Header=\"Tài xế giữ tiền\"",
            "Header=\"Thu trong kỳ\"",
            "Header=\"Bàn giao &amp; kế toán\"",
            "Header=\"Kế toán xác nhận\"",
            "Header=\"Hẹn thu quá hạn\"",
            "Header=\"Cần kiểm tra\""
        }) StringAssert.Contains(view, text);

        var custody = view.IndexOf("Header=\"Tài xế giữ tiền\"", StringComparison.Ordinal);
        var collections = view.IndexOf("Header=\"Thu trong kỳ\"", StringComparison.Ordinal);
        var handover = view.IndexOf("Header=\"Bàn giao &amp; kế toán\"", StringComparison.Ordinal);
        var accounting = view.IndexOf("Header=\"Kế toán xác nhận\"", StringComparison.Ordinal);
        var promises = view.IndexOf("Header=\"Hẹn thu quá hạn\"", StringComparison.Ordinal);
        var exceptions = view.IndexOf("Header=\"Cần kiểm tra\"", StringComparison.Ordinal);

        Assert.IsLessThan(custody, collections);
        Assert.IsLessThan(collections, handover);
        Assert.IsLessThan(handover, accounting);
        Assert.IsLessThan(accounting, promises);
        Assert.IsLessThan(promises, exceptions);
    }

    [TestMethod]
    public void View_PreservesWebSectionsAccountingActionsAndEmptyStates()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CodAccountingView.xaml");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CodAccountingViewModel.cs");

        foreach (var text in new[]
        {
            "Tiền mặt đang ở tài xế",
            "Hoạt động thu trong kỳ",
            "Bàn giao trong kỳ",
            "Kế toán tiếp nhận trong kỳ",
            "Bàn giao chờ kế toán tiếp nhận",
            "Bàn giao COD",
            "Đối chiếu và xác nhận",
            "Tài xế khai bàn giao",
            "Tiền thừa chưa gắn phiếu",
            "Chênh lệch lúc bàn giao",
            "Kế toán xác nhận tiền thực nhận",
            "Số tiền Công Ty thực nhận",
            "Lý do chênh lệch",
            "Điều chỉnh bằng bản ghi bù",
            "Đảo thu",
            "Lời hẹn thu đã quá hạn",
            "Cần kiểm tra và chênh lệch"
        }) StringAssert.Contains(view, text);

        StringAssert.Contains(view, "Content=\"{Binding AcceptButtonText}\"");
        StringAssert.Contains(view, "Content=\"{Binding ReverseAcceptanceButtonText}\"");
        StringAssert.Contains(view, "Content=\"{Binding ReverseHandoverButtonText}\"");
        StringAssert.Contains(view, "Click=\"OpenPending_OnClick\"");
        StringAssert.Contains(view, "Click=\"OpenException_OnClick\"");

        foreach (var text in new[]
        {
            "Không có tiền mặt COD đang nằm ở tài xế.",
            "Không có hoạt động thu COD trong kỳ.",
            "Không có bàn giao COD trong kỳ.",
            "Không có kế toán tiếp nhận COD trong kỳ.",
            "Không có bàn giao đang chờ tiếp nhận.",
            "Không có lời hẹn thu quá hạn.",
            "Không có trường hợp COD cần kiểm tra hiện tại.",
            "Chưa có bàn giao COD."
        }) StringAssert.Contains(vm, text);
    }

    [TestMethod]
    public void View_DoesNotExposeDeveloperLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Accounting", "CodAccountingView.xaml");

        Assert.IsFalse(view.Contains("/api/", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("permission", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("UUID", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("canonical", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_ActivatesUi72AtIndex34AndKeepsParallelWorkspaces()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "\"accounting.cod-reporting\" => \"COD & đối soát\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 31");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 32");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 34");
        StringAssert.Contains(shell, "CanViewCodAccounting");
        StringAssert.Contains(shell, "IsAccountingOpen = true");

        StringAssert.Contains(xaml, "Tag=\"{Binding IsAgingSelected}\"");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsCodAccountingSelected}\"");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsSalesReportingSelected}\"");
        StringAssert.Contains(xaml, "Click=\"AccountingCod_OnClick\"");
        StringAssert.Contains(xaml, "x:Name=\"AgingReportingHost\"");
        StringAssert.Contains(xaml, "x:Name=\"SalesReportingHost\"");
        StringAssert.Contains(xaml, "x:Name=\"CodAccountingHost\"");
        StringAssert.Contains(xaml, "Content=\"Đối soát tổng hợp\"");
        StringAssert.Contains(code, "CodAccountingHost.Content = codAccountingView");
        StringAssert.Contains(code, "AccountingCod_OnClick");
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
