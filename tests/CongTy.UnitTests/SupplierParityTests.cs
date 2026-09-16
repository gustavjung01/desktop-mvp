using CongTy.Desktop.Partners;

namespace CongTy.UnitTests;

[TestClass]
public sealed class SupplierParityTests
{
    [TestMethod]
    public void SupplierPresentation_UsesWebRowShapeAndOfficeLabels()
    {
        var row = new SupplierRow(
            1,
            "supplier-id",
            "NCC001",
            "Công ty Minh Anh",
            "0312345678",
            "Ngân hàng ACB",
            "3 ngày",
            "Đang hoạt động",
            "Ngừng sử dụng",
            new CongTy.Contracts.SupplierData());

        Assert.AreEqual(1, row.Stt);
        Assert.AreEqual("NCC001", row.Code);
        Assert.AreEqual("Ngừng sử dụng", row.ToggleAction);
    }

    [TestMethod]
    public void Source_SupplierWorkspaceMatchesCurrentWebStructure()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerViewModel.cs");
        var service = ReadRepoFile("src", "CongTy.ApiClient", "PartnerService.cs");

        var supplierStart = xaml.IndexOf("<TabItem Header=\"Nhà cung cấp\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, supplierStart);
        var supplierEnd = xaml.IndexOf("</TabItem>", supplierStart, StringComparison.Ordinal);
        Assert.IsGreaterThan(supplierStart, supplierEnd);
        var supplierArea = xaml[supplierStart..Math.Min(xaml.Length, supplierEnd + 10_000)];

        StringAssert.Contains(supplierArea, "Text=\"Tổng nhà cung cấp\"");
        StringAssert.Contains(supplierArea, "Text=\"Đang hoạt động\"");
        StringAssert.Contains(supplierArea, "Content=\"Thêm nhà cung cấp\"");
        StringAssert.Contains(supplierArea, "ToolTip=\"Ví dụ: NCC001, Công ty Minh Anh, 0312345678\"");
        StringAssert.Contains(supplierArea, "Header=\"STT\"");
        StringAssert.Contains(supplierArea, "Header=\"Mã\"");
        StringAssert.Contains(supplierArea, "Header=\"Tên nhà cung cấp\"");
        StringAssert.Contains(supplierArea, "Header=\"Mã số thuế\"");
        StringAssert.Contains(supplierArea, "Header=\"Ngân hàng\"");
        StringAssert.Contains(supplierArea, "Header=\"Giao hàng\"");
        StringAssert.Contains(supplierArea, "Header=\"Trạng thái\"");
        StringAssert.Contains(supplierArea, "Content=\"Sửa\"");
        StringAssert.Contains(supplierArea, "Content=\"{Binding ToggleAction}\"");
        StringAssert.Contains(supplierArea, "Không có nhà cung cấp phù hợp.");

        Assert.IsFalse(supplierArea.Contains("Phụ trách mua", StringComparison.Ordinal));
        Assert.IsFalse(supplierArea.Contains("Hồ sơ nhà cung cấp", StringComparison.Ordinal));
        Assert.IsFalse(supplierArea.Contains("Điều khoản thanh toán", StringComparison.Ordinal));

        var editorStart = xaml.IndexOf("<StackPanel Visibility=\"{Binding IsSupplierEditor", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, editorStart);
        var nextEditor = xaml.IndexOf("<StackPanel Visibility=\"{Binding IsSupplierContactEditor", editorStart, StringComparison.Ordinal);
        Assert.IsGreaterThan(editorStart, nextEditor);
        var editorArea = xaml[editorStart..nextEditor];
        StringAssert.Contains(editorArea, "Text=\"Mã nhà cung cấp\"");
        StringAssert.Contains(editorArea, "Text=\"Tên nhà cung cấp\"");
        StringAssert.Contains(editorArea, "Text=\"Mã số thuế\"");
        StringAssert.Contains(editorArea, "Text=\"Số tài khoản\"");
        StringAssert.Contains(editorArea, "Text=\"Tên ngân hàng\"");
        StringAssert.Contains(editorArea, "Text=\"Thời gian giao hàng trung bình\"");
        StringAssert.Contains(editorArea, "Text=\"Địa chỉ chi tiết\"");
        StringAssert.Contains(editorArea, "Text=\"Tỉnh/thành phố\"");
        StringAssert.Contains(editorArea, "Text=\"Xã/phường/đặc khu\"");
        Assert.IsFalse(editorArea.Contains("Nhân viên phụ trách mua", StringComparison.Ordinal));
        Assert.IsFalse(editorArea.Contains("Loại địa chỉ", StringComparison.Ordinal));
        Assert.IsFalse(editorArea.Contains("Mã bưu chính", StringComparison.Ordinal));

        StringAssert.Contains(code, "await _viewModel.OpenCreateSupplierAsync()");
        StringAssert.Contains(code, "SupplierProvince_OnSelectionChanged");
        StringAssert.Contains(code, "await _viewModel.LoadSupplierWardsAsync()");

        StringAssert.Contains(viewModel, "SupplierProvinceOptions");
        StringAssert.Contains(viewModel, "SupplierWardOptions");
        StringAssert.Contains(viewModel, "DraftWard.Trim()");
        StringAssert.Contains(viewModel, "supplier.IsActive ? \"Ngừng sử dụng\" : \"Đưa vào sử dụng\"");
        Assert.IsFalse(viewModel.Contains("supplier.PurchaseOwnerEmployeeName ?? \"Chưa giao phụ trách\"", StringComparison.Ordinal));

        StringAssert.Contains(service, "/api/reference/vietnam-administrative-units");
        StringAssert.Contains(service, "?provinceCode=");
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
