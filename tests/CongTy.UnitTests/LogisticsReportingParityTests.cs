using CongTy.Contracts;
using CongTy.Desktop.Logistics;

namespace CongTy.UnitTests;

[TestClass]
public sealed class LogisticsReportingParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeLabelsAndVietnameseFormatting()
    {
        Assert.AreEqual("1.234", LogisticsReportingPresentation.Count("1234"));
        Assert.AreEqual("98,5%", LogisticsReportingPresentation.Percent("98.5"));
        Assert.AreEqual("Giao không thành công", LogisticsReportingPresentation.ResultLabel("failed"));
        Assert.AreEqual("Hẹn giao lại", LogisticsReportingPresentation.ResultLabel("rescheduled"));
        Assert.AreEqual("Đã đóng chuyến", LogisticsReportingPresentation.StatusLabel("closed"));
        Assert.AreEqual("Khách đóng cửa", LogisticsReportingPresentation.ReasonLabel("CUSTOMER_CLOSED"));
        Assert.AreEqual("Khách từ chối nhận", LogisticsReportingPresentation.ReasonLabel("CUSTOMER_REFUSED"));
        Assert.AreEqual("Không xác định được địa chỉ", LogisticsReportingPresentation.ReasonLabel("ADDRESS_ISSUE"));
        Assert.AreEqual("Khách yêu cầu thời gian giao khác", LogisticsReportingPresentation.ReasonLabel("REQUESTED_NEW_TIME"));
        Assert.AreEqual("Lý do khác", LogisticsReportingPresentation.ReasonLabel("SOME_NEW_CODE"));
        Assert.AreEqual(
            "Thiếu giờ dự kiến tại điểm giao",
            LogisticsReportingPresentation.ExceptionLabel("MISSING_PLANNED_ARRIVAL"));
    }

    [TestMethod]
    public void Service_UsesCanonicalReadOnlyLogisticsEndpoint()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "LogisticsReportingService.cs");
        StringAssert.Contains(service, "/api/reporting/logistics");
        StringAssert.Contains(service, "warehouseId=");
        Assert.IsFalse(service.Contains("PostDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ViewModel_UsesDedicatedPermissionAndFiveCanonicalTabs()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "LogisticsReportingViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "LogisticsReportingView.xaml");

        StringAssert.Contains(vm, "_access.HasPermission(\"core.reporting.logistics.read\")");
        StringAssert.Contains(vm, "ResetToCurrentMonthAsync");
        StringAssert.Contains(vm, "Math.Clamp(value, 0, 4)");
        StringAssert.Contains(vm, "OnPropertyChanged(nameof(ShowInitialLoading))");

        var tabs = new[]
        {
            "Header=\"Tài xế\"",
            "Header=\"Phương tiện\"",
            "Header=\"Kết quả giao\"",
            "Header=\"Chuyến gần nhất\"",
            "Header=\"Ngoại lệ\""
        };
        var previous = -1;
        foreach (var tab in tabs)
        {
            var index = view.IndexOf(tab, StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, index);
            previous = index;
        }
    }

    [TestMethod]
    public void Ui_PreservesFiltersKpisActionsAndOfficeLanguage()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "LogisticsReportingView.xaml");

        foreach (var text in new[]
        {
            "Text=\"Từ ngày\"",
            "Text=\"Đến ngày\"",
            "Text=\"Kho\"",
            "Content=\"Áp dụng\"",
            "Content=\"Tháng hiện tại\"",
            "Content=\"Mở chuyến giao\"",
            "Content=\"Kết quả lần giao\"",
            "Text=\"Chuyến trong kỳ\"",
            "Text=\"Điểm giao / phiếu giao\"",
            "Text=\"Giao đủ\"",
            "Text=\"Đủ dữ liệu SLA\"",
            "Text=\"Giao một phần / thất bại / hẹn lại\"",
            "Text=\"Thời lượng chuyến đã đóng\""
        })
        {
            StringAssert.Contains(view, text);
        }

        Assert.IsFalse(view.Contains("canonical", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("source ID", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("UUID", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Ui_UsesCompactSharedSummaryCardsAndNoDuplicatePageHeader()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Logistics", "LogisticsReportingView.xaml");

        Assert.IsFalse(view.Contains("Text=\"GIAO NHẬN &amp; ĐIỀU PHỐI\"", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Text=\"Hiệu suất giao hàng / Logistics\"", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("<UniformGrid Columns=\"3\"", StringComparison.Ordinal));

        StringAssert.Contains(view, "<WrapPanel Margin=\"0,0,0,8\">");
        StringAssert.Contains(view, "Style=\"{StaticResource OfficeSummaryCardStyle}\"");
        StringAssert.Contains(view, "Style=\"{StaticResource OfficeSummaryLabelStyle}\"");
        StringAssert.Contains(view, "Style=\"{StaticResource OfficeSummaryValueStyle}\"");
        StringAssert.Contains(view, "ToolTip=\"{Binding DeliveredFullHint}\"");
        StringAssert.Contains(view, "ToolTip=\"{Binding SlaCoverageHint}\"");

        Assert.IsFalse(
            view.Contains(
                "<Border Margin=\"0,0,0,10\" Style=\"{StaticResource OfficeCardStyle}\">\n                    <TextBlock Text=\"Tỷ lệ đúng hạn",
                StringComparison.Ordinal));
    }

    [TestMethod]
    public void Shell_ActivatesOnlyTheFirstLogisticsScreen()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "\"logistics.reporting\" => \"Hiệu suất giao hàng / Logistics\"");
        StringAssert.Contains(shell, "public async Task NavigateLogisticsReportingAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 19");
        StringAssert.Contains(shell, "CanViewLogisticsReporting");

        StringAssert.Contains(main, "Tag=\"{Binding IsLogisticsReportingSelected}\"");
        StringAssert.Contains(main, "Click=\"LogisticsReporting_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"LogisticsReportingHost\"");

        StringAssert.Contains(mainCode, "LogisticsReportingHost.Content = logisticsReportingView");
        StringAssert.Contains(mainCode, "await _viewModel.NavigateLogisticsReportingAsync()");
        StringAssert.Contains(app, "AddSingleton<ILogisticsReportingService, LogisticsReportingService>()");
        StringAssert.Contains(app, "AddSingleton<LogisticsReportingViewModel>()");
        StringAssert.Contains(app, "AddSingleton<LogisticsReportingView>()");
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
