using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceManualAttendanceLeaveParityTests
{
    [TestMethod]
    public void Contracts_DeserializeManualLeaveSourceEmployeesAndAttachment()
    {
        const string json = """
        {
          "selectedEmployee":null,
          "branches":[],
          "employees":[{
            "id":"10000000-0000-4000-8000-000000000001",
            "code":"NV001",
            "name":"Nguyễn Văn An",
            "branchId":"20000000-0000-4000-8000-000000000001",
            "branchName":"Chi nhánh 01"
          }],
          "pagination":{"limit":50,"offset":0,"total":1,"hasPrevious":false,"hasNext":false},
          "requests":[{
            "id":"30000000-0000-4000-8000-000000000001",
            "employee_id":"10000000-0000-4000-8000-000000000001",
            "leave_type_id":"40000000-0000-4000-8000-000000000001",
            "leave_type_code_snapshot":"AL",
            "leave_type_name_snapshot":"Phép năm",
            "date_from":"2026-09-25",
            "date_to":"2026-09-25",
            "day_part":"FULL_DAY",
            "reason":"Phiếu giấy",
            "attachment_reference":"installation/Tai-lieu/Nhan-su/Phieu-nghi/2026/09/file.pdf",
            "attachment_url":"https://files.example.test/file.pdf",
            "request_source":"MANUAL_PAPER",
            "manual_approver_name":"Trưởng phòng",
            "status":"APPROVED",
            "requested_by_actor_id":"actor",
            "requested_by_employee_id":null,
            "version":1
          }],
          "capabilities":{
            "selfOnly":false,
            "canSubmitOwn":false,
            "canApprove":true,
            "canSubmitManual":true,
            "canManageTypes":false
          }
        }
        """;

        var data = JsonSerializer.Deserialize<LeaveRequestListResponseData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được dữ liệu nghỉ thủ công.");

        Assert.HasCount(1, data.Employees);
        Assert.AreEqual("Chi nhánh 01", data.Employees[0].BranchName);
        Assert.IsTrue(data.Capabilities.CanSubmitManual);
        Assert.HasCount(1, data.Requests);
        Assert.AreEqual("MANUAL_PAPER", data.Requests[0].RequestSource);
        Assert.AreEqual("Trưởng phòng", data.Requests[0].ManualApproverName);
        Assert.IsNull(data.Requests[0].RequestedByEmployeeId);
        Assert.AreEqual("https://files.example.test/file.pdf", data.Requests[0].AttachmentUrl);
    }

    [TestMethod]
    public void LeaveApi_UsesCanonicalManualRequestAndBinaryAttachmentContracts()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "LeaveService.cs");
        var api = ReadRepoFile("src", "CongTy.ApiClient", "CompanyApiClient.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "LeaveContracts.cs");

        StringAssert.Contains(service, "/api/workforce/leave/requests/manual");
        StringAssert.Contains(service, "/api/workforce/leave/attachments");
        StringAssert.Contains(service, "PostIdempotentDataAsync<SubmitManualLeaveRequest");
        StringAssert.Contains(service, "PutBytesIdempotentDataAsync<LeaveAttachmentUploadData>");
        StringAssert.Contains(service, "[\"x-file-name\"] = Uri.EscapeDataString(safeFileName)");
        StringAssert.Contains(api, "IReadOnlyDictionary<string, string>? headers");
        StringAssert.Contains(api, "request.Headers.TryAddWithoutValidation(pair.Key, pair.Value)");
        StringAssert.Contains(contracts, "JsonPropertyName(\"canSubmitManual\")");
        StringAssert.Contains(contracts, "JsonPropertyName(\"request_source\")");
        StringAssert.Contains(contracts, "JsonPropertyName(\"manual_approver_name\")");
        StringAssert.Contains(contracts, "JsonPropertyName(\"attachment_url\")");
    }

    [TestMethod]
    public void AttendanceAdjustment_ProvidesServerNowManualCheckInOutWithStableRetryKey()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "AttendanceAdjustmentContracts.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceAdjustmentViewModel.Manual.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceAdjustmentView.xaml");
        var codeBehind = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceAdjustmentView.xaml.cs");

        StringAssert.Contains(contracts, "JsonPropertyName(\"recordNowAction\")");
        StringAssert.Contains(viewModel, "attendance-manual-now");
        StringAssert.Contains(viewModel, "desktop-attendance-manual-now");
        StringAssert.Contains(viewModel, "new DirectAttendanceAdjustmentRequest(");
        StringAssert.Contains(viewModel, "await _service.DirectAsync(payload, key)");
        StringAssert.Contains(viewModel, "_mutationKeys.Remove(slot)");
        StringAssert.Contains(view, "CHẤM CÔNG TAY VÀ ĐIỀU CHỈNH CÔNG");
        StringAssert.Contains(view, "Chấm công tay theo giờ hệ thống");
        StringAssert.Contains(view, "Không nhập giờ thay cho nhân sự");
        StringAssert.Contains(view, "Click=\"SubmitQuick_OnClick\"");
        StringAssert.Contains(codeBehind, "SubmitQuickAttendanceAsync");
        Assert.IsFalse(viewModel.Contains("DateTime.Now", StringComparison.Ordinal));
        Assert.IsFalse(viewModel.Contains("DateTimeOffset.Now", StringComparison.Ordinal));
    }

    [TestMethod]
    public void LeaveDesktop_CoversPaperFormUploadSourceApproverAndAttachmentLink()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeaveViewModel.Manual.cs");
        var mainViewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeaveViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeaveView.xaml");
        var codeBehind = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeaveView.xaml.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeavePresentation.cs");

        foreach (var label in new[]
        {
            "GHI NHẬN PHIẾU NGHỈ GIẤY",
            "CHỌN ẢNH / PDF",
            "Phiếu giấy đã được duyệt",
            "Người duyệt",
            "Ngày duyệt",
            "Lý do / nguồn",
            "Chứng từ"
        })
            StringAssert.Contains(view, label);

        StringAssert.Contains(mainViewModel, "_requestCapabilities.CanSubmitManual");
        StringAssert.Contains(mainViewModel, "ApplyManualEmployees(response.Employees ?? [])");
        StringAssert.Contains(viewModel, "10 * 1024 * 1024");
        StringAssert.Contains(viewModel, "image/jpeg");
        StringAssert.Contains(viewModel, "application/pdf");
        StringAssert.Contains(viewModel, "leave-document-upload");
        StringAssert.Contains(viewModel, "desktop-leave-document-upload");
        StringAssert.Contains(viewModel, "leave-request-manual");
        StringAssert.Contains(viewModel, "desktop-leave-request-manual");
        StringAssert.Contains(viewModel, "_manualUploadedAttachment is not null");
        StringAssert.Contains(viewModel, "SubmitManualLeaveRequestAsync");
        StringAssert.Contains(codeBehind, "OpenFileDialog");
        StringAssert.Contains(codeBehind, "OpenAttachment_OnClick");
        StringAssert.Contains(presentation, "Phiếu giấy / nhập thủ công");
        StringAssert.Contains(presentation, "Duyệt trên giấy:");
    }

    [TestMethod]
    public void Audit_LocksWeb1175AndDesktopOnlyBoundary()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_MANUAL_ATTENDANCE_LEAVE_AUDIT.md");

        StringAssert.Contains(audit, "aa310c87658da48915ac7cab9ab70b90dbde87bc");
        StringAssert.Contains(audit, "c6a47410a3efbdd4b7b35554c8747d089bfe1bee");
        StringAssert.Contains(audit, "#1175");
        StringAssert.Contains(audit, "/api/workforce/attendance/adjustments/direct");
        StringAssert.Contains(audit, "/api/workforce/leave/requests/manual");
        StringAssert.Contains(audit, "/api/workforce/leave/attachments");
        StringAssert.Contains(audit, "canonical Idempotency-Key");
        StringAssert.Contains(audit, "không sửa Web/backend/DB/migration");
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
