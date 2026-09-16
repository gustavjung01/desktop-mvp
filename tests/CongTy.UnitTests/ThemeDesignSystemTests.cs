namespace CongTy.UnitTests;

[TestClass]
public sealed class ThemeDesignSystemTests
{
    [TestMethod]
    public void SharedCardTone_KeepsGrayFrameWhiteBodyAndDistinctButtons()
    {
        var light = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Theme.Light.xaml");
        var dark = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Theme.Dark.xaml");
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");

        StringAssert.Contains(light, "<Color x:Key=\"WindowBackgroundColor\">#E9EDF0</Color>");
        StringAssert.Contains(light, "<Color x:Key=\"CardFrameColor\">#D5DBDE</Color>");
        StringAssert.Contains(light, "<Color x:Key=\"CardHeaderColor\">#E2E6E8</Color>");
        StringAssert.Contains(light, "<Color x:Key=\"CardBodyColor\">#FFFFFF</Color>");
        StringAssert.Contains(light, "<Color x:Key=\"SecondaryButtonFaceColor\">#FAFBFB</Color>");

        foreach (var token in new[]
        {
            "CardFrameBrush",
            "CardHeaderBrush",
            "CardBodyBrush",
            "SecondaryButtonFaceBrush"
        })
        {
            StringAssert.Contains(light, $"x:Key=\"{token}\"");
            StringAssert.Contains(dark, $"x:Key=\"{token}\"");
        }

        StringAssert.Contains(controls, "x:Key=\"OfficeCardStyle\"");
        StringAssert.Contains(controls, "Value=\"{DynamicResource CardBodyBrush}\"");
        StringAssert.Contains(controls, "Value=\"{DynamicResource CardFrameBrush}\"");
        StringAssert.Contains(controls, "x:Key=\"OfficeCardHeaderStyle\"");
        StringAssert.Contains(
            controls,
            "<Style x:Key=\"OfficeCardHeaderStyle\" TargetType=\"Border\" BasedOn=\"{StaticResource OfficeCardStyle}\">");
        StringAssert.Contains(
            controls,
            "<Style x:Key=\"OfficeSummaryCardStyle\" TargetType=\"Border\" BasedOn=\"{StaticResource OfficeCardStyle}\">");
        StringAssert.Contains(controls, "<Setter Property=\"Background\" Value=\"{DynamicResource CardHeaderBrush}\" />");
        StringAssert.Contains(controls, "<Setter Property=\"Padding\" Value=\"10,6\" />");
        StringAssert.Contains(controls, "x:Key=\"OfficeSummaryLabelStyle\"");
        StringAssert.Contains(controls, "x:Key=\"OfficeSummaryValueStyle\"");
        Assert.IsFalse(
            controls.Contains(
                "x:Key=\"OfficeSummaryCardStyle\" TargetType=\"Border\" BasedOn=\"{StaticResource OfficeCardHeaderStyle}\"",
                StringComparison.Ordinal));
        StringAssert.Contains(controls, "Value=\"{DynamicResource SecondaryButtonFaceBrush}\"");
        StringAssert.Contains(controls, "<TranslateTransform Y=\"2\" />");

        StringAssert.Contains(controls, "x:Key=\"OfficeTextBoxStyle\"");
        StringAssert.Contains(controls, "Value=\"{DynamicResource InputBackgroundBrush}\"");
        StringAssert.Contains(controls, "x:Key=\"OfficeDataGridHeaderStyle\"");
    }

    [TestMethod]
    public void UiParityStandard_LocksHeaderDensityAndCompactKpiRules()
    {
        var standard = ReadRepoFile("docs", "UI_PARITY_STANDARD.md");

        StringAssert.Contains(standard, "Shell sở hữu tiêu đề trang và mô tả trang.");
        StringAssert.Contains(standard, "không được lặp lại cùng title/subtitle");
        StringAssert.Contains(standard, "Không dùng **card/dải xám full-width chỉ để chứa một câu giải thích**");
        StringAssert.Contains(standard, "Card KPI mặc định chỉ có **2 dòng: nhãn + giá trị chính**");
        StringAssert.Contains(standard, "Nhóm **4–6 KPI phải ưu tiên nằm trong một hàng gọn khi cửa sổ đủ rộng**");
        StringAssert.Contains(standard, "Không dùng `UniformGrid` ép 2–3 cột");
        StringAssert.Contains(standard, "### Checklist review bắt buộc trước merge UI");
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
