using CongTy.Desktop.Logistics;

namespace CongTy.UnitTests;

[TestClass]
public sealed class TripDispatchParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeDispatchLabels()
    {
        Assert.AreEqual("Chờ bàn giao", TripDispatchPresentation.Status("locked"));
        Assert.AreEqual("Đã xuất phát", TripDispatchPresentation.Status("dispatched"));
        Assert.AreEqual("Thiếu mã phiếu giao", TripDispatchPresentation.Number(null));
        Assert.IsTrue(TripDispatchPresentation.TryIsoDateTime("2026-09-16 08:30", out var iso));
        Assert.IsTrue(iso.StartsWith("2026-09-16T01:30:00", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Service_UsesCanonicalDispatchRoutesAndSharedIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "TripDispatchService.cs");

        StringAssert.Contains(service, "/api/logistics/trips?status=all");
        StringAssert.Contains(service, "/api/logistics/trips/{Uri.EscapeDataString(RequireId(tripId))}/dispatch");
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid");
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_ReusesSameCanonicalKeyUntilDispatchSucceeds()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripDispatchViewModel.cs");

        StringAssert.Contains(vm, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(vm, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(vm, "_idempotencyKeys.Create($\"trip-dispatch-{prefix}\")");
        StringAssert.Contains(vm, "KeyFor(intent)");
        StringAssert.Contains(vm, "_intentKeys.Remove(intent)");
        StringAssert.Contains(vm, "new TripDispatchRequest(dispatchedAt, receiver");
        Assert.IsFalse(vm.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesQueueDetailReceiverChecklistAndReadOnlyFlow()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripDispatchView.xaml");

        foreach (var text in new[]
        {
            "HÀNG ĐỢI BÀN GIAO",
            "Chuyến đã khóa và đã xuất phát",
            "CHI TIẾT BÀN GIAO",
            "Tài xế chính đã chốt trong kế hoạch",
            "Người nhận bàn giao thực tế",
            "Tài xế chính",
            "Người nhận khác",
            "Người nhận bàn giao khác",
            "Thời điểm xe xuất phát",
            "Ghi chú bàn giao",
            "Trước khi xác nhận",
            "✓ Chuyến đã khóa, không còn sửa xe/tài xế/điểm giao",
            "✓ Toàn bộ phiếu sẽ xuất kho trong một giao dịch",
            "✓ Có lỗi ở một phiếu thì toàn chuyến không thay đổi",
            "CHUYẾN ĐÃ XUẤT PHÁT",
            "Mã bàn giao",
            "XUẤT KHO ĐÃ GHI",
            "Các phiếu đã rời kho",
            "Mã ghi xuất kho",
            "Chọn chuyến bên trái để kiểm tra và bàn giao."
        }) StringAssert.Contains(view, text);

        StringAssert.Contains(view, "Content=\"{Binding DispatchButtonText}\"");
        StringAssert.Contains(view, "Content=\"In\"");

        Assert.IsFalse(view.Contains("Inventory OUT", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("POD", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("GPS", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("COD", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Giao thành công", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Giao thất bại", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Printer_PreservesTripSheetBusinessDocument()
    {
        var printer = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "TripSheetPrinter.cs");
        StringAssert.Contains(printer, "PHIẾU CHUYẾN GIAO HÀNG");
        StringAssert.Contains(printer, "Danh sách bàn giao chuyến");
        StringAssert.Contains(printer, "Kho xuất phát");
        StringAssert.Contains(printer, "Phiếu giao");
        StringAssert.Contains(printer, "Điều phối");
        StringAssert.Contains(printer, "Thủ kho");
        StringAssert.Contains(printer, "Tài xế / Người nhận");
        StringAssert.Contains(printer, "PrintDocument");
    }

    [TestMethod]
    public void Shell_ActivatesFourthLogisticsScreenAndKeepsPreviousIndexes()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "\"logistics.dispatch\" => \"Bàn giao và cho xe xuất phát\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 25");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 23");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 24");
        StringAssert.Contains(shell, "CanViewTripDispatch");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsTripDispatchSelected}\"");
        StringAssert.Contains(xaml, "Click=\"TripDispatch_OnClick\"");
        StringAssert.Contains(xaml, "Content=\"Quay lại lập kế hoạch\"");
        StringAssert.Contains(xaml, "x:Name=\"TripDispatchHost\"");
        StringAssert.Contains(code, "TripDispatchHost.Content = tripDispatchView");
        StringAssert.Contains(code, "TripDispatchBack_OnClick");
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
