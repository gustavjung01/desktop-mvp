using System.Text;
using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Workforce;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforcePayrollCloseoutLot8Tests
{
    [TestMethod]
    public void Lot8_Contracts_DeserializeImmutableCloseoutAndKeepMoneyStrings()
    {
        const string json = """
        {
          "closeout": {
            "closeSnapshot": {
              "id":"c1",
              "payroll_period_id":"p1",
              "calculation_revision":4,
              "created_at":"2026-09-22T10:00:00Z",
              "snapshot":{
                "period":{"from":"2026-09-01","to":"2026-09-30","currencyCode":"VND"},
                "totals":{
                  "employeeCount":1,
                  "grossIncome":"10500000.50",
                  "reimbursementTotal":"200000.00",
                  "deductionTotal":"100000.25",
                  "netPay":"10600000.25"
                }
              }
            },
            "payslips":[{
              "id":"s1",
              "payroll_period_id":"p1",
              "employee_id":"e1",
              "revision":2,
              "source_kind":"ADJUSTMENT",
              "created_at":"2026-09-22T11:00:00Z",
              "snapshot":{
                "period":{"from":"2026-09-01","to":"2026-09-30","currencyCode":"VND"},
                "employee":{"id":"e1","code":"NV001","name":"Nguyễn Văn A","branchName":"Chi nhánh A"},
                "pay":{
                  "employeeId":"e1",
                  "employeeCode":"NV001",
                  "employeeName":"Nguyễn Văn A",
                  "standardWorkDays":26,
                  "payableWorkDays":24,
                  "confirmedOvertimeMinutes":120,
                  "salaryAmount":"10000000.00",
                  "incomeTotal":"500000.50",
                  "reimbursementTotal":"200000.00",
                  "deductionTotal":"100000.25",
                  "grossIncome":"10500000.50",
                  "netPay":"10600000.25"
                },
                "adjustments":[{
                  "id":"a1","name":"Truy lĩnh","category":"INCOME","direction":"ADD",
                  "amount":"500000.50","reason":"Bổ sung kỳ trước"
                }]
              }
            }],
            "payslipHistory":[],
            "adjustments":[],
            "history":[]
          },
          "capabilities":{"canClose":true,"canAdjust":true,"canExport":true}
        }
        """;

        var value = JsonSerializer.Deserialize<PayrollFoundationData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được closeout.");

        Assert.IsNotNull(value.Closeout.CloseSnapshot);
        Assert.AreEqual("10600000.25", value.Closeout.CloseSnapshot.Snapshot.Totals.NetPay);
        Assert.AreEqual("10600000.25", value.Closeout.Payslips[0].Snapshot.Pay.NetPay);
        Assert.AreEqual("500000.50", value.Closeout.Payslips[0].Snapshot.Adjustments[0].Amount);
    }

    [TestMethod]
    public void Lot8_ApiClient_UsesCanonicalPayrollRouteForCloseAndAdjust()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "PayrollFoundationService.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "PayrollCloseoutContracts.cs");

        StringAssert.Contains(service, "CloseAsync");
        StringAssert.Contains(service, "AdjustAsync");
        StringAssert.Contains(service, "PostAsync<ClosePayrollRequest, ClosePayrollMutationData>");
        StringAssert.Contains(service, "PostAsync<AdjustPayrollRequest, AdjustPayrollMutationData>");
        StringAssert.Contains(service, "\"/api/workforce/payroll\"");
        StringAssert.Contains(contracts, "\"CLOSE\"");
        StringAssert.Contains(contracts, "\"ADJUST\"");
        StringAssert.Contains(contracts, "componentTypeId");
        StringAssert.Contains(contracts, "direction");
        StringAssert.Contains(contracts, "reason");
    }

    [TestMethod]
    public void Lot8_Workspace_EnablesClosePayslipAdjustExportAndImmutableHistory()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationView.xaml");

        foreach (var label in new[]
        {
            "CHỐT LƯƠNG",
            "Phiếu lương nhân sự",
            "CHI TIẾT PHIẾU LƯƠNG",
            "Lịch sử phiếu lương",
            "Điều chỉnh lương sau chốt",
            "GHI ĐIỀU CHỈNH",
            "XUẤT PDF",
            "XUẤT EXCEL",
            "Kỳ lương đã chốt",
            "Đọc từ hồ sơ kỳ đã chốt"
        })
            StringAssert.Contains(view, label);

        Assert.AreEqual(1, Count(view, "Header=\"Phiếu lương\""));
        Assert.AreEqual(1, Count(view, "Header=\"Lịch sử kỳ lương\""));
    }

    [TestMethod]
    public void Lot8_Mutations_ReuseCanonicalKeysAndNeverMutateAttendance()
    {
        var actions = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.CloseoutActions.cs");
        var foundation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.cs");

        Assert.AreEqual(2, Count(actions, "MutationSlot(request);"));
        Assert.AreEqual(2, Count(actions, "MutationKey(slot);"));
        Assert.AreEqual(2, Count(actions, "_mutationKeys.Remove(slot);"));
        StringAssert.Contains(foundation, "_idempotencyKeys.Create(\"payroll-foundation\")");
        Assert.IsFalse(actions.Contains("attendance/adjustments", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(actions.Contains("attendance/record", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot8_Permissions_AreSeparatedForCloseAdjustAndExport()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.Closeout.cs");

        StringAssert.Contains(vm, "_capabilities.CanClose");
        StringAssert.Contains(vm, "_access.HasPermission(PayrollClosePermission)");
        StringAssert.Contains(vm, "_capabilities.CanAdjust");
        StringAssert.Contains(vm, "_access.HasPermission(PayrollAdjustPermission)");
        StringAssert.Contains(vm, "_capabilities.CanExport");
        StringAssert.Contains(vm, "_access.HasPermission(PayrollExportPermission)");
        StringAssert.Contains(vm, "SelectedPeriod?.Status == \"RECONCILED\"");
        StringAssert.Contains(vm, "SelectedPeriod?.Status == \"CLOSED\"");
    }

    [TestMethod]
    public void Lot8_Exports_AreActualXlsxAndPdfFromServerSnapshots()
    {
        var payslip = SamplePayslip();
        var closeout = new PayrollCloseoutData
        {
            CloseSnapshot = new PayrollCloseSnapshotData
            {
                PayrollPeriodId = "p1",
                CalculationRevision = 1,
                Snapshot = new PayrollCloseSnapshotPayloadData
                {
                    Period = new PayrollClosePeriodSnapshotData
                    {
                        From = "2026-09-01",
                        To = "2026-09-30",
                        CurrencyCode = "VND"
                    },
                    Totals = new PayrollCloseTotalsData
                    {
                        EmployeeCount = 1,
                        GrossIncome = "10500000.00",
                        ReimbursementTotal = "200000.00",
                        DeductionTotal = "100000.00",
                        NetPay = "10600000.00"
                    }
                }
            },
            Payslips = [payslip]
        };

        var xlsx = PayrollCloseoutExportFile.CreatePayrollWorkbook(closeout);
        var pdf = PayrollCloseoutExportFile.CreatePayslipPdf(payslip);

        Assert.IsTrue(xlsx.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("PK", Encoding.ASCII.GetString(xlsx.Content, 0, 2));
        Assert.IsTrue(pdf.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(Encoding.ASCII.GetString(pdf.Content, 0, Math.Min(8, pdf.Content.Length)).StartsWith("%PDF-", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot8_Audit_LocksWebPr1152AndDesktopBoundary()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT8_PAYROLL_CLOSEOUT_AUDIT.md");
        foreach (var marker in new[]
        {
            "74e33c539d6ae30e4605b79bd763ecd12f34125a",
            "4c9d6652d900f883f8c6cf07316dd46fee8715bf",
            "PR #1152",
            "CLOSE",
            "ADJUST",
            "156_workforce_payroll_closeout",
            "Không sửa Web/backend/DB/migration",
            "Không deploy production"
        })
            StringAssert.Contains(audit, marker);
    }

    private static PayrollPayslipSnapshotData SamplePayslip() =>
        new()
        {
            Id = "s1",
            PayrollPeriodId = "p1",
            EmployeeId = "e1",
            Revision = 2,
            SourceKind = "ADJUSTMENT",
            CreatedAt = "2026-09-22T11:00:00Z",
            Snapshot = new PayrollPayslipSnapshotPayloadData
            {
                Period = new PayrollClosePeriodSnapshotData
                {
                    From = "2026-09-01",
                    To = "2026-09-30",
                    CurrencyCode = "VND"
                },
                Employee = new PayrollPayslipEmployeeData
                {
                    Id = "e1",
                    Code = "NV001",
                    Name = "Nguyễn Văn A",
                    BranchName = "Chi nhánh A"
                },
                Pay = new PayrollPayslipPayData
                {
                    EmployeeId = "e1",
                    EmployeeCode = "NV001",
                    EmployeeName = "Nguyễn Văn A",
                    StandardWorkDays = 26,
                    PayableWorkDays = 24,
                    ConfirmedOvertimeMinutes = 120,
                    SalaryAmount = "10000000.00",
                    IncomeTotal = "500000.00",
                    ReimbursementTotal = "200000.00",
                    DeductionTotal = "100000.00",
                    GrossIncome = "10500000.00",
                    NetPay = "10600000.00"
                },
                Adjustments =
                [
                    new PayrollPayslipAdjustmentData
                    {
                        Id = "a1",
                        Name = "Truy lĩnh",
                        Category = "INCOME",
                        Direction = "ADD",
                        Amount = "500000.00",
                        Reason = "Bổ sung kỳ trước"
                    }
                ]
            }
        };

    private static int Count(string value, string fragment)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(fragment, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += fragment.Length;
        }
        return count;
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
