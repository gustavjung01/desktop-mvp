using CongTy.Desktop.Logistics;

namespace CongTy.UnitTests;

[TestClass]
public sealed class DeliveryAttemptParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeAttemptAndProofLabels()
    {
        Assert.AreEqual("Giao đủ", DeliveryAttemptPresentation.Result("delivered_full"));
        Assert.AreEqual("Giao một phần", DeliveryAttemptPresentation.Result("delivered_partial"));
        Assert.AreEqual("Không giao được", DeliveryAttemptPresentation.Result("failed"));
        Assert.AreEqual("Hẹn giao lại", DeliveryAttemptPresentation.Result("rescheduled"));
        Assert.AreEqual("Ảnh giao hàng", DeliveryAttemptPresentation.ProofType("photo"));
        Assert.AreEqual("Tham chiếu chữ ký", DeliveryAttemptPresentation.ProofType("signature"));
        Assert.AreEqual("Tham chiếu mã xác nhận", DeliveryAttemptPresentation.ProofType("otp"));
        Assert.AreEqual("Xác nhận thủ công", DeliveryAttemptPresentation.ProofType("manual_confirm"));
    }

    [TestMethod]
    public void Service_IsReadOnlyAndUsesCanonicalAttemptEndpoints()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "DeliveryAttemptService.cs");

        StringAssert.Contains(service, "/api/logistics/trips?status=all");
        StringAssert.Contains(service, "/attempts");
        StringAssert.Contains(service, "/pod");
        StringAssert.Contains(service, "GetDataAsync");
        Assert.IsFalse(service.Contains("PostIdempotentDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("PutIdempotentDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Delete", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ViewModel_RequiresReadPermissionsAndFiltersDispatchedTrips()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryAttemptViewModel.cs");

        StringAssert.Contains(vm, "core.delivery-trip.read");
        StringAssert.Contains(vm, "core.delivery-attempt.read");
        StringAssert.Contains(vm, "core.pod.read");
        StringAssert.Contains(vm, "Where(row => row.Status == \"dispatched\")");
        Assert.IsFalse(vm.Contains("delivery-attempt.record", StringComparison.Ordinal));
        Assert.IsFalse(vm.Contains("ICanonicalIdempotencyKeyProvider", StringComparison.Ordinal));
    }

    [TestMethod]
    public void View_PreservesWebQueueDetailProgressAndReadOnlyBoundary()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryAttemptView.xaml");

        foreach (var text in new[]
        {
            "CHUYẾN ĐÃ XUẤT PHÁT",
            "Chọn chuyến cần theo dõi",
            "KẾT QUẢ CHỈ ĐỌC",
            "Thời điểm",
            "Lý do",
            "Giao lại",
            "Ghi chú",
            "Không có bằng chứng đính kèm; kết quả giao vẫn hợp lệ.",
            "Chọn chuyến bên trái để xem lần giao đã ghi.",
            "Tài xế chưa ghi kết quả cho phiếu nào trong chuyến này.",
            "Phiếu chưa giao đủ vẫn là hàng đang theo xe; màn này không tự nhập hàng về kho.",
            "Xem ảnh"
        }) StringAssert.Contains(view, text);

        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "DeliveryAttemptPresentation.cs");
        StringAssert.Contains(presentation, "Xem bằng chứng tùy chọn");
        StringAssert.Contains(presentation, "Ẩn bằng chứng");
        var loadingSetter = presentation[
            presentation.IndexOf("public bool IsProofLoading", StringComparison.Ordinal)..
            presentation.IndexOf("public IReadOnlyList<DeliveryProofRow> Proofs", StringComparison.Ordinal)];
        StringAssert.Contains(loadingSetter, "OnPropertyChanged(nameof(ShowNoProof))");

        Assert.IsFalse(view.Contains("Content=\"Ghi kết quả", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Click=\"Record", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Inventory IN", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("dispatch lineage", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_ActivatesFifthLogisticsScreenWithoutChangingExistingIndexes()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(shell, "\"logistics.delivery-attempts\" => \"Theo dõi kết quả lần giao\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 27");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 25");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 26");
        StringAssert.Contains(shell, "CanViewDeliveryAttempts");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsDeliveryAttemptsSelected}\"");
        StringAssert.Contains(xaml, "Click=\"DeliveryAttempts_OnClick\"");
        StringAssert.Contains(xaml, "Content=\"Bàn giao chuyến\"");
        StringAssert.Contains(xaml, "x:Name=\"DeliveryAttemptHost\"");
        StringAssert.Contains(code, "DeliveryAttemptHost.Content = deliveryAttemptView");
        StringAssert.Contains(code, "DeliveryAttemptsBack_OnClick");
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
