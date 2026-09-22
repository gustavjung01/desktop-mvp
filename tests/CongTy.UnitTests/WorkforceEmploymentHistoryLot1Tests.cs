using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Access;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceEmploymentHistoryLot1Tests
{
    [TestMethod]
    public void Lot1_Contracts_DeserializeCanonicalEmploymentAndAssignmentHistory()
    {
        const string json = """
        {
          "id":"00000000-0000-4000-8000-000000000001",
          "installation_id":"demo",
          "code":"NV001",
          "full_name":"Nguyễn Văn An",
          "job_title":"Kế toán",
          "phone":null,
          "email":null,
          "branch_id":"00000000-0000-4000-8000-000000000010",
          "is_active":true,
          "created_at":"2026-09-01T00:00:00.000Z",
          "updated_at":"2026-09-22T00:00:00.000Z",
          "employment_history":[{
            "id":"00000000-0000-4000-8000-000000000002",
            "installation_id":"demo",
            "employee_id":"00000000-0000-4000-8000-000000000001",
            "employment_type":"PERMANENT",
            "effective_from":"2026-08-10",
            "effective_to":null,
            "end_reason":null,
            "data_quality":"CONFIRMED",
            "source":"HR",
            "source_reference":"employee-create"
          }],
          "assignment_history":[{
            "id":"00000000-0000-4000-8000-000000000003",
            "employee_id":"00000000-0000-4000-8000-000000000001",
            "branch_id":"00000000-0000-4000-8000-000000000010",
            "effective_from":"2026-09-01",
            "effective_to":null,
            "reason":"Điều chuyển",
            "data_quality":"CONFIRMED",
            "branch_code":"CN02",
            "branch_name":"Chi nhánh 02"
          }],
          "current_employment":{
            "id":"00000000-0000-4000-8000-000000000002",
            "employment_type":"PERMANENT",
            "effective_from":"2026-08-10",
            "effective_to":null,
            "data_quality":"CONFIRMED"
          },
          "current_assignment":{
            "id":"00000000-0000-4000-8000-000000000003",
            "branch_id":"00000000-0000-4000-8000-000000000010",
            "effective_from":"2026-09-01",
            "effective_to":null,
            "branch_code":"CN02",
            "branch_name":"Chi nhánh 02"
          }
        }
        """;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var employee = JsonSerializer.Deserialize<EmployeeDirectoryData>(json, options)
            ?? throw new InvalidOperationException("Không đọc được lịch sử nhân sự.");

        Assert.HasCount(1, employee.EmploymentHistory);
        Assert.HasCount(1, employee.AssignmentHistory);
        Assert.AreEqual("PERMANENT", employee.CurrentEmployment?.EmploymentType);
        Assert.AreEqual("2026-08-10", employee.CurrentEmployment?.EffectiveFrom);
        Assert.AreEqual("CN02", employee.CurrentAssignment?.BranchCode);
        Assert.AreEqual("10/08/2026", EmployeeDirectoryPresentation.DateText(employee.CurrentEmployment?.EffectiveFrom));
        Assert.AreEqual("Chính thức", EmployeeDirectoryPresentation.EmploymentTypeLabel(employee.CurrentEmployment?.EmploymentType));
    }

    [TestMethod]
    public void Lot1_Editor_LoadsDetailBeforeRenderingCanonicalHistory()
    {
        var readService = ReadRepoFile("src", "CongTy.ApiClient", "EmployeeDirectoryReadService.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.cs");
        var history = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.WorkforceHistory.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryView.xaml");

        StringAssert.Contains(readService, "\"/api/employees/{Uri.EscapeDataString(employeeId.Trim())}\"");
        StringAssert.Contains(viewModel, "_readService.GetEmployeeAsync(employee.Id)");
        StringAssert.Contains(history, "detail.EmploymentHistory");
        StringAssert.Contains(history, "detail.AssignmentHistory");
        StringAssert.Contains(history, "AssignmentBranchLabel(item, branchMap)");

        foreach (var label in new[]
        {
            "Ngày bắt đầu làm việc",
            "Hình thức lao động",
            "Ngày kết thúc/nghỉ việc",
            "Lịch sử lao động",
            "Lịch sử điều chuyển",
            "Trạng thái dữ liệu",
            "Cần HR xác nhận"
        })
            StringAssert.Contains(view, label);
    }

    [TestMethod]
    public void Lot1_Mutations_CarryEffectiveDatesReasonsAndReuseCanonicalIdempotencySlots()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "EmployeeDirectoryContracts.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.cs");
        var history = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.WorkforceHistory.cs");

        foreach (var wireName in new[]
        {
            "employmentStartDate",
            "employmentType",
            "confirmEmployment",
            "employmentEffectiveFrom",
            "employmentEffectiveTo",
            "employmentEndReason",
            "confirmAssignment",
            "assignmentEffectiveFrom",
            "assignmentReason",
            "employmentEffectiveDate",
            "employmentReason"
        })
            StringAssert.Contains(contracts, $"JsonPropertyName(\"{wireName}\")");

        StringAssert.Contains(viewModel, "CanonicalDate(DraftEmploymentStartDate)");
        StringAssert.Contains(viewModel, "CanonicalDate(DraftAssignmentEffectiveFrom)");
        StringAssert.Contains(viewModel, "CanonicalDate(ToggleEffectiveDate)");
        StringAssert.Contains(viewModel, "MutationKey(slot, \"employee-status\")");
        StringAssert.Contains(viewModel, "request.EmploymentEffectiveDate");
        StringAssert.Contains(viewModel, "request.AssignmentEffectiveFrom");
        StringAssert.Contains(history, "string.IsNullOrWhiteSpace(ToggleReason)");
        StringAssert.Contains(history, "BusinessToday()");
    }

    [TestMethod]
    public void Lot1_HistoryQuality_DoesNotPresentLegacyBackfillAsConfirmedHrFact()
    {
        Assert.AreEqual("Đã xác nhận", EmployeeDirectoryPresentation.HistoryQualityLabel("CONFIRMED"));
        Assert.AreEqual("Khôi phục từ lịch sử hệ thống", EmployeeDirectoryPresentation.HistoryQualityLabel("AUDIT_DERIVED"));
        Assert.AreEqual("Cần HR xác nhận", EmployeeDirectoryPresentation.HistoryQualityLabel("LEGACY_ESTIMATED"));
        Assert.AreEqual("Cần HR xác nhận", EmployeeDirectoryPresentation.HistoryQualityLabel("LEGACY_CURRENT_ONLY"));
        Assert.IsFalse(EmployeeDirectoryPresentation.HistoryNeedsConfirmation("CONFIRMED"));
        Assert.IsFalse(EmployeeDirectoryPresentation.HistoryNeedsConfirmation("AUDIT_DERIVED"));
    }

    [TestMethod]
    public void Lot1_Audit_KeepsHistoricalTimesheetResolutionServerOwnedUntilDesktopTimesheetExists()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT1_EMPLOYMENT_HISTORY_AUDIT.md");

        StringAssert.Contains(audit, "resolveEmployeeAtDate");
        StringAssert.Contains(audit, "branch-at-date");
        StringAssert.Contains(audit, "Desktop hiện chưa có workspace Bảng công");
        StringAssert.Contains(audit, "không suy lịch sử từ shared.employees.branch_id");
        StringAssert.Contains(audit, "không sửa Web/backend/DB/migration");
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
