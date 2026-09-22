using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Workforce;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforcePayrollAggregationLot7Tests
{
    [TestMethod]
    public void Lot7_Contracts_DeserializeServerCalculationWithoutChangingMoneyStrings()
    {
        const string json = """
        {
          "calculation": {
            "revision": 3,
            "sourceFingerprint": "abc",
            "issueSummary": {
              "blockers": {"missingSalaryProfiles": 1},
              "warnings": {"confirmedOvertimeEmployees": 2}
            },
            "snapshot": {
              "contractVersion": 1,
              "totals": {
                "employeeCount": 2,
                "salaryAmount": "15000000.00",
                "incomeTotal": "1250000.50",
                "reimbursementTotal": "300000.00",
                "deductionTotal": "250000.25",
                "grossIncome": "16250000.50",
                "netPay": "16300000.25"
              },
              "rows": [{
                "employeeId": "e1",
                "employeeCode": "NV001",
                "employeeName": "Nguyễn Văn An",
                "branchName": "Chi nhánh A",
                "standardWorkDays": 26,
                "payableWorkDays": 24,
                "unpaidLeaveDays": 1,
                "confirmedOvertimeMinutes": 120,
                "monthlySalary": "13000000.00",
                "salaryAmount": "12000000.00",
                "incomeTotal": "1000000.50",
                "reimbursementTotal": "300000.00",
                "deductionTotal": "200000.25",
                "grossIncome": "13000000.50",
                "netPay": "13100000.25",
                "fixedComponents": [],
                "periodComponents": []
              }]
            }
          },
          "capabilities": {"canManage": true}
        }
        """;

        var value = JsonSerializer.Deserialize<PayrollFoundationData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được calculation.");

        Assert.IsNotNull(value.Calculation);
        Assert.AreEqual("16300000.25", value.Calculation.Snapshot.Totals.NetPay);
        Assert.AreEqual("13100000.25", value.Calculation.Snapshot.Rows[0].NetPay);
        Assert.AreEqual("16.300.000,25 ₫", PayrollFoundationPresentation.MoneyText(value.Calculation.Snapshot.Totals.NetPay));
    }

    [TestMethod]
    public void Lot7_ApiClient_UsesSamePayrollRouteForAggregateAndReconcile()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "PayrollFoundationService.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "PayrollAggregationContracts.cs");

        StringAssert.Contains(service, "AggregateAsync");
        StringAssert.Contains(service, "ReconcileAsync");
        StringAssert.Contains(service, "PostAsync<AggregatePayrollRequest, PayrollAggregationMutationData>");
        StringAssert.Contains(service, "PostAsync<ReconcilePayrollRequest, PayrollAggregationMutationData>");
        StringAssert.Contains(service, "\"/api/workforce/payroll\"");
        StringAssert.Contains(contracts, "\"AGGREGATE\"");
        StringAssert.Contains(contracts, "\"RECONCILE\"");
        StringAssert.Contains(contracts, "acknowledgeWarnings");
        StringAssert.Contains(contracts, "payrollPeriodId");
    }

    [TestMethod]
    public void Lot7_Workspace_ShowsCanonicalBoardDetailWarningsAndReconciliation()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationView.xaml");

        foreach (var label in new[]
        {
            "TỔNG HỢP LƯƠNG",
            "Lương theo công",
            "Công / OT",
            "Thu nhập thêm",
            "Hoàn chi",
            "Khấu trừ",
            "Thực nhận",
            "CHI TIẾT LƯƠNG",
            "Lương cố định",
            "Theo công &amp; OT",
            "Thưởng &amp; phụ cấp",
            "Công tác phí &amp; hoàn chi phí",
            "Cần xử lý trước khi đối soát",
            "Cần kiểm tra",
            "XÁC NHẬN ĐỐI SOÁT"
        })
            StringAssert.Contains(view, label);

        Assert.IsFalse(view.Contains("CHỐT LƯƠNG", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("XUẤT PDF", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("XUẤT EXCEL", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot7_Desktop_NeverRecalculatesPayrollMoney()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.Aggregation.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollAggregationPresentation.cs");

        StringAssert.Contains(vm, "Calculation?.Snapshot.Totals.GrossIncome");
        StringAssert.Contains(vm, "Calculation?.Snapshot.Totals.NetPay");
        Assert.IsFalse(vm.Contains(".Sum(", StringComparison.Ordinal));
        Assert.IsFalse(presentation.Contains(".Sum(", StringComparison.Ordinal));
        Assert.IsFalse(vm.Contains("decimal.Parse", StringComparison.Ordinal));
        Assert.IsFalse(vm.Contains("double.Parse", StringComparison.Ordinal));
        Assert.IsFalse(presentation.Contains("decimal.Parse", StringComparison.Ordinal));
        Assert.IsFalse(presentation.Contains("double.Parse", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot7_Mutations_ReuseCanonicalKeyUntilSuccessful()
    {
        var actions = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.AggregationActions.cs");
        var foundation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.cs");

        Assert.AreEqual(2, Count(actions, "MutationSlot(request);"));
        Assert.AreEqual(2, Count(actions, "MutationKey(slot);"));
        Assert.AreEqual(2, Count(actions, "_mutationKeys.Remove(slot);"));
        StringAssert.Contains(foundation, "_idempotencyKeys.Create(\"payroll-foundation\")");
        StringAssert.Contains(foundation, "if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;");
    }

    [TestMethod]
    public void Lot7_Reconciliation_RequiresCleanBlockersAndAcknowledgedWarnings()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.Aggregation.cs");
        var actions = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.AggregationActions.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollAggregationPresentation.cs");

        StringAssert.Contains(vm, "!HasBlockers");
        StringAssert.Contains(vm, "ReconcileWarningsAcknowledged");
        StringAssert.Contains(vm, "ReconcileNote.Trim().Length is >= 1 and <= 1000");
        StringAssert.Contains(actions, "Kỳ lương còn dữ liệu bắt buộc phải xử lý trước khi đối soát.");
        StringAssert.Contains(actions, "Vui lòng ghi chú kết quả kiểm tra cảnh báo.");
        StringAssert.Contains(presentation, "giờ tăng ca đã xác nhận; cần kiểm tra khoản tiền tăng ca trước khi đối soát");
    }

    [TestMethod]
    public void Lot7_Audit_LocksWebPr1151AndExcludesLot8()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT7_PAYROLL_AGGREGATION_AUDIT.md");

        foreach (var marker in new[]
        {
            "fa39caa805f806dc920b998462b3dd8a005e63ae",
            "4c9d6652d900f883f8c6cf07316dd46fee8715bf",
            "PR #1151",
            "AGGREGATE",
            "RECONCILE",
            "CLOSE / ADJUST",
            "Không sửa Web/backend/DB/migration",
            "Không deploy production"
        })
            StringAssert.Contains(audit, marker);
    }

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
