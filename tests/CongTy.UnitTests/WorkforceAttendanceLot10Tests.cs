namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceAttendanceLot10Tests
{
    [TestMethod]
    public void Lot10_UsesCanonicalAttendanceApiAndIdempotentMutations()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "AttendanceService.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceViewModel.cs");

        foreach (var route in new[]
                 {
                     "/api/workforce/attendance/today",
                     "/api/workforce/attendance/record",
                     "/api/workforce/attendance/points",
                     "/api/workforce/attendance/qr-token",
                 })
            StringAssert.Contains(service, route);

        StringAssert.Contains(service, "PostIdempotentDataAsync<AttendanceRecordRequest");
        StringAssert.Contains(service, "PostIdempotentDataAsync<CreateAttendancePointRequest");
        StringAssert.Contains(service, "PostIdempotentDataAsync<CreateAttendanceQrTokenRequest");
        StringAssert.Contains(viewModel, "JsonSerializer.Serialize(payload)");
        StringAssert.Contains(viewModel, "_mutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(viewModel, "_mutationKeys.Remove(slot)");
    }

    [TestMethod]
    public void Lot10_CoversEmployeeQrManualFaceAndExitSemantics()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "AttendanceContracts.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendancePresentation.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceView.xaml");

        foreach (var status in new[] { "NOT_STARTED", "WORKING", "OUTSIDE", "COMPLETE" })
            StringAssert.Contains(presentation, status);

        foreach (var method in new[] { "QR_FACE", "FACE_MANUAL", "ALL", "BOTH", "MANUAL", "FACE" })
            StringAssert.Contains(presentation, method);

        foreach (var reason in new[] { "END_WORK", "WORK_BUSINESS", "PERSONAL", "BREAK", "OTHER" })
            StringAssert.Contains(viewModel, reason);

        StringAssert.Contains(contracts, "movement_reason");
        StringAssert.Contains(view, "CHẤM CÔNG TRỰC TIẾP");
        StringAssert.Contains(view, "máy quét QR dạng bàn phím");
        StringAssert.Contains(view, "Quét khuôn mặt được thực hiện tại máy chấm công");
        StringAssert.Contains(view, "Chính sách này chỉ xác nhận có mặt");
        StringAssert.Contains(view, "Attendance Event là lịch sử gốc");
    }

    [TestMethod]
    public void Lot10_ImplementsWorkplaceQrManagementAndRealQrRendering()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "AttendanceService.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendancePresentation.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceView.xaml");
        var project = ReadRepoFile("src", "CongTy.Desktop", "CongTy.Desktop.csproj");

        StringAssert.Contains(service, "CreatePointAsync");
        StringAssert.Contains(service, "IssueQrTokenAsync");
        StringAssert.Contains(viewModel, "desktop-attendance-point-create");
        StringAssert.Contains(viewModel, "desktop-attendance-qr-token");
        StringAssert.Contains(viewModel, "remaining > 15");
        StringAssert.Contains(view, "QR nơi làm việc");
        StringAssert.Contains(view, "Mã tự làm mới trước khi hết hạn");
        StringAssert.Contains(presentation, "new PngByteQRCode");
        StringAssert.Contains(presentation, "qrCode.GetGraphic(8)");
        StringAssert.Contains(project, "QRCoder");
    }

    [TestMethod]
    public void Lot10_WiresAttendanceWorkspaceAndCanonicalPermissions()
    {
        var nav = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkforceNavigation.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.Attendance.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.Attendance.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(nav, "core.attendance.self.read");
        StringAssert.Contains(nav, "core.attendance.self.record");
        StringAssert.Contains(nav, "core.attendance-point.manage");
        StringAssert.Contains(shell, "workforce.attendance");
        StringAssert.Contains(host, "WorkspaceSlots.Attendance");
        StringAssert.Contains(xaml, "Attendance_OnClick");
        StringAssert.Contains(xaml, "IsAttendanceSelected");
        Assert.IsFalse(xaml.Contains("Sẽ được triển khai ở Lô 3", StringComparison.Ordinal)
                       && xaml.Contains("Chấm công", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot10_DoesNotModifyBackendOrDatabase()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT10_ATTENDANCE_AUDIT.md");
        StringAssert.Contains(audit, "af4acc24bc411d3206d07256f6119472068c01c3");
        StringAssert.Contains(audit, "không sửa Web/backend");
        StringAssert.Contains(audit, "không sửa DB");
        StringAssert.Contains(audit, "không migration");
        StringAssert.Contains(audit, "không deploy production");
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
