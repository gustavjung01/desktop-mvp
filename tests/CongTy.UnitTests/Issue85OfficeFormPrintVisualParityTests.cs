namespace CongTy.UnitTests;

[TestClass]
public sealed class Issue85OfficeFormPrintVisualParityTests
{
    [TestMethod]
    public void OfficeFormPrint_MatchesWebBusinessDocumentHierarchy()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "OfficeFormPrintPreview.cs");

        StringAssert.Contains(source, "private const string BrandName = \"HƯNG PHÁT\";");
        StringAssert.Contains(source, "Biểu mẫu văn phòng trống");
        StringAssert.Contains(source, "AddOfficeHeader(document, form.Name)");
        StringAssert.Contains(source, "FontSize = Pt(16)");
        StringAssert.Contains(source, "FontSize = Pt(18)");
        StringAssert.Contains(source, "TextAlignment = TextAlignment.Right");
        Assert.IsFalse(source.Contains("CÔNG TY:", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("ToUpperInvariant()", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OfficeFormPrint_UsesWebScaleMarginsMetaGridAndTableTreatment()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "OfficeFormPrintPreview.cs");

        StringAssert.Contains(source, "Pt(10.5)");
        StringAssert.Contains(source, "new FontFamily(\"Arial\")");
        StringAssert.Contains(source, "AddMetaGrid(document, spec.Meta)");
        StringAssert.Contains(source, "index += 2");
        StringAssert.Contains(source, "HeaderFillBrush");
        StringAssert.Contains(source, "BorderThickness = new Thickness(0.8)");
        StringAssert.Contains(source, "DefaultNote");
        StringAssert.Contains(source, "AddOfficeSignatures");
        StringAssert.Contains(source, "OfficePagePadding(template.PageSize)");
    }

    [TestMethod]
    public void OfficeFormPrint_PreviewShowsWhitePaperOnGrayCanvasAndFitsWholePage()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "OfficeFormPrintPreview.cs");

        StringAssert.Contains(source, "document.Background = PaperBrush");
        StringAssert.Contains(source, "Background = PreviewBackgroundBrush");
        StringAssert.Contains(source, "FitPreview(viewer, document)");
        StringAssert.Contains(source, "Math.Min(widthRatio, heightRatio)");
        StringAssert.Contains(source, "Width = 1180");
        StringAssert.Contains(source, "Height = 860");
    }

    [TestMethod]
    public void OfficeFormPrint_DoesNotInventGenericBodyThatWebDoesNotRender()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "OfficeFormPrintPreview.cs");

        Assert.IsFalse(source.Contains("Nội dung / đề nghị / giải trình:", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Mẫu trống — không chứa số liệu hiện tại của hệ thống.", StringComparison.Ordinal));
        StringAssert.Contains(source, "Biểu mẫu trống — điền đầy đủ thông tin trước khi ký hoặc sử dụng.");
    }

    private static string Read(params string[] parts)
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
