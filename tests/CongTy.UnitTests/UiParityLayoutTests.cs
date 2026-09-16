using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class UiParityLayoutTests
{
    [TestMethod]
    public void CustomerProfile_KeepsWebBusinessTabOrder()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");
        var profileStart = xaml.IndexOf("x:Name=\"CustomerProfileTabs\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, profileStart);

        var expected = new[]
        {
            "Header=\"Tổng quan\"",
            "Header=\"Hàng đã mua\"",
            "Header=\"Đơn hàng\"",
            "Header=\"Công nợ &amp; thanh toán\"",
            "Header=\"Giao hàng / Trả hàng\"",
            "Header=\"Thông tin &amp; địa chỉ\""
        };

        var cursor = profileStart;
        foreach (var marker in expected)
        {
            var next = xaml.IndexOf(marker, cursor, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự tab Hồ sơ 360°: {marker}");
            cursor = next;
        }

        Assert.IsFalse(xaml.Contains("Header=\"Hồ sơ 360°\"", StringComparison.Ordinal));
        StringAssert.Contains(xaml, "Header=\"{x:Null}\" Width=\"0\"");
        StringAssert.Contains(xaml, "Text=\"{Binding CustomerOverviewName}\"");
        StringAssert.Contains(xaml, "Text=\"{Binding CustomerOverviewAddressText}\"");
        foreach (var readOnlyRun in new[] { "CustomerOverviewGroup", "CustomerOverviewEmployee", "CustomerOverviewPhone", "CustomerOverviewName" })
        {
            StringAssert.Contains(xaml, $"<Run Text=\"{{Binding {readOnlyRun}, Mode=OneWay}}\" />");
            Assert.IsFalse(xaml.Contains($"<Run Text=\"{{Binding {readOnlyRun}}}\" />", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void CustomerWorkspace_KeepsWebManagementAreasAndQuickSetup()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");
        var customerStart = xaml.IndexOf("x:Name=\"CustomerTabs\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, customerStart);

        var expected = new[]
        {
            "Header=\"Khách hàng\"",
            "Header=\"Thiết lập nhanh\"",
            "Header=\"Nhập KH\"",
            "Header=\"Cập nhật KH\"",
            "Header=\"Nhóm khách hàng\""
        };

        var cursor = customerStart;
        foreach (var marker in expected)
        {
            var next = xaml.IndexOf(marker, cursor, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự khu vực khách hàng: {marker}");
            cursor = next;
        }

        Assert.IsFalse(xaml.Contains("Header=\"Nhập / cập nhật\"", StringComparison.Ordinal));
        StringAssert.Contains(xaml, "x:Name=\"QuickCustomerList\"");
        StringAssert.Contains(xaml, "Text=\"1  Thông tin khách hàng\"");
        StringAssert.Contains(xaml, "Text=\"2  Địa chỉ\"");
        StringAssert.Contains(xaml, "Text=\"3  Ảnh khách hàng\"");
        StringAssert.Contains(xaml, "Click=\"CustomerAddresses_OnClick\"");
        StringAssert.Contains(xaml, "Click=\"SetDefaultCustomerAddress_OnClick\"");
        StringAssert.Contains(xaml, "Click=\"ToggleCustomerAddress_OnClick\"");
        StringAssert.Contains(xaml, "Click=\"OpenCustomerAddressLocation_OnClick\"");
        StringAssert.Contains(xaml, "Click=\"PreviewCustomerMedia_OnClick\"");
        StringAssert.Contains(xaml, "ItemsSource=\"{Binding BulkSourceRows}\"");
        StringAssert.Contains(xaml, "Header=\"{Binding ToggleAction}\"");
        StringAssert.Contains(xaml, "Visibility=\"{Binding CanSetDefault, Converter={StaticResource BooleanToVisibilityConverter}}\"");
        StringAssert.Contains(xaml, "Visibility=\"{Binding HasLocation, Converter={StaticResource BooleanToVisibilityConverter}}\"");
    }

    [TestMethod]
    public void CustomerWorkspace_UsesSharedOfficeVisualSystem()
    {
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");
        var customer = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");

        StringAssert.Contains(controls, "x:Key=\"OfficePrimaryButtonStyle\"");
        StringAssert.Contains(controls, "x:Key=\"OfficeTextBoxStyle\"");
        StringAssert.Contains(controls, "x:Key=\"OfficeDataGridStyle\"");
        StringAssert.Contains(controls, "x:Key=\"OfficeTabItemStyle\"");
        StringAssert.Contains(controls, "x:Key=\"OfficeListBoxItemStyle\"");
        StringAssert.Contains(customer, "BasedOn=\"{StaticResource OfficeDataGridStyle}\"");
        StringAssert.Contains(customer, "ItemContainerStyle=\"{StaticResource OfficeTabItemStyle}\"");
    }

    [TestMethod]
    public void CustomerBulkUpdate_IdentifiesSourceRowsAndMediaRetryKeepsAttemptKeys()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerViewModel.cs");

        StringAssert.Contains(viewModel, "IdentifyCustomersAsync(");
        StringAssert.Contains(viewModel, "BulkSourceRows");
        StringAssert.Contains(viewModel, "_bulkIdentificationReady");
        StringAssert.Contains(viewModel, "_customerMediaUploadAttempts");
        StringAssert.Contains(viewModel, "attempt.PrepareKey");
        StringAssert.Contains(viewModel, "attempt.FinalizeKey");
        StringAssert.Contains(viewModel, "_customerMediaUploadAttempts.Remove(fingerprint)");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerPresentation.cs");
        StringAssert.Contains(presentation, "public bool CanSetDefault");
        StringAssert.Contains(presentation, "public bool HasLocation");
        StringAssert.Contains(presentation, "public string ToggleAction");
    }

    [TestMethod]
    public void CustomerParityMatrix_TracksFeatureCompletenessNotOnlyTabs()
    {
        var matrix = ReadRepoFile("docs", "parity", "customer-feature-matrix.md");
        StringAssert.Contains(matrix, "NPP-Platform/main@3dafbaa564e2d9715ceb91bff8e35b5dbc344b2c");
        StringAssert.Contains(matrix, "Thiết lập nhanh");
        StringAssert.Contains(matrix, "Nhập KH");
        StringAssert.Contains(matrix, "Cập nhật KH");
        StringAssert.Contains(matrix, "Hồ sơ 360°");
        StringAssert.Contains(matrix, "Không bỏ sót im lặng");
    }


    [TestMethod]
    public void DesktopShell_MatchesWebNavigationHierarchyAndMovesUtilitiesIntoSettings()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");

        StringAssert.Contains(shell, "Text=\"ĐIỀU HÀNH\"");
        StringAssert.Contains(shell, "Text=\"DANH MỤC QUẢN LÝ\"");
        StringAssert.Contains(shell, "Text=\"Danh mục nghiệp vụ\"");
        StringAssert.Contains(shell, "Text=\"CÀI ĐẶT CÔNG TY\"");
        StringAssert.Contains(shell, "Text=\"Cài đặt Công Ty\"");
        StringAssert.Contains(shell, "ToolTip=\"Cài đặt ứng dụng\"");
        StringAssert.Contains(shell, "Header=\"Tài khoản &amp; quyền\"");
        StringAssert.Contains(shell, "Header=\"Kết nối\"");
        StringAssert.Contains(shell, "Header=\"Giao diện\"");
        Assert.IsFalse(shell.Contains("Content=\"Quyền truy cập\"", StringComparison.Ordinal));
        Assert.IsFalse(shell.Contains("Content=\"Tình trạng hệ thống\"", StringComparison.Ordinal));

        var settingsStart = shell.IndexOf("Header=\"Giao diện\"", StringComparison.Ordinal);
        var themeButton = shell.IndexOf("Click=\"ToggleTheme_OnClick\"", StringComparison.Ordinal);
        Assert.IsGreaterThan(settingsStart, themeButton, "Đổi giao diện phải nằm trong Cài đặt, không nằm trên topbar.");

        StringAssert.Contains(viewModel, "public void NavigateSettings()");
        StringAssert.Contains(viewModel, "public GridLength SidebarWidth");
        StringAssert.Contains(viewModel, "public bool IsSettingsSelected");
    }

    [TestMethod]
    public void CompactDesktopVisualSystem_PrioritizesWorkspaceDensity()
    {
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");
        var customer = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");

        StringAssert.Contains(controls, "x:Key=\"OfficeMiniButtonStyle\"");
        StringAssert.Contains(controls, "<Setter Property=\"MinHeight\" Value=\"30\" />");
        StringAssert.Contains(controls, "<Setter Property=\"MinHeight\" Value=\"33\" />");
        Assert.IsFalse(controls.Contains("<Setter Property=\"Height\" Value=\"33\" />", StringComparison.Ordinal), "Row style không được khóa cứng 33px vì sẽ cắt text/nút ở các bảng cần hàng cao.");
        StringAssert.Contains(controls, "<Style TargetType=\"ScrollBar\">");
        StringAssert.Contains(controls, "<TranslateTransform Y=\"1\" />");
        StringAssert.Contains(customer, "<Grid Margin=\"14\">");
        Assert.IsFalse(customer.Contains("<UniformGrid Grid.Row=\"1\" Columns=\"2\"", StringComparison.Ordinal));
        StringAssert.Contains(customer, "LastChildFill=\"False\"");
        StringAssert.Contains(customer, "Style=\"{StaticResource OfficeMiniButtonStyle}\"");
    }


    [TestMethod]
    public void CompactControls_KeepTextAndActionsOnTheSameVisualBaseline()
    {
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");
        var customer = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");

        StringAssert.Contains(controls, "<Setter Property=\"Padding\" Value=\"10,0\" />");
        StringAssert.Contains(controls, "<Setter Property=\"VerticalContentAlignment\" Value=\"Center\" />");
        StringAssert.Contains(controls, "<TranslateTransform Y=\"1\" />");
        StringAssert.Contains(controls, "<Setter Property=\"Padding\" Value=\"7,0\" />");
        StringAssert.Contains(controls, "<Style x:Key=\"OfficeMiniButtonStyle\"");
        StringAssert.Contains(controls, "<Setter Property=\"VerticalAlignment\" Value=\"Center\" />");
        StringAssert.Contains(controls, "<Style x:Key=\"OfficeDataGridRowStyle\"");
        Assert.IsGreaterThanOrEqualTo(
            2,
            CountOccurrences(controls, "<Setter Property=\"VerticalContentAlignment\" Value=\"Center\" />"),
            "Text và nút trong bảng phải luôn cân giữa theo chiều dọc của hàng.");
        StringAssert.Contains(customer, "<Style x:Key=\"FieldTextBoxStyle\"");
        StringAssert.Contains(customer, "<Style x:Key=\"FieldComboStyle\"");
        Assert.AreEqual(2, CountOccurrences(customer, "<Setter Property=\"Margin\" Value=\"0,3,0,8\" />"), "TextBox và ComboBox phải cùng baseline/margin trong toolbar.");
        StringAssert.Contains(customer, "VerticalAlignment=\"Bottom\"");
        StringAssert.Contains(customer, "Margin=\"0,0,0,8\"");
        StringAssert.Contains(customer, "VerticalAlignment=\"Center\" />");
    }


    [TestMethod]
    public void TabStrips_UseOneSharedNonOverlappingTemplate()
    {
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");
        var customer = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");

        StringAssert.Contains(controls, "x:Key=\"OfficeTabControlStyle\"");
        StringAssert.Contains(controls, "<TabPanel IsItemsHost=\"True\"");
        StringAssert.Contains(controls, "HorizontalScrollBarVisibility=\"Auto\"");
        StringAssert.Contains(customer, "<TabControl x:Name=\"PartnerTabs\" Background=\"Transparent\" BorderThickness=\"0\">");
        StringAssert.Contains(customer, "<TabControl.Template>");
        StringAssert.Contains(customer, "x:Name=\"CustomerTabs\"");
        StringAssert.Contains(customer, "Style=\"{StaticResource OfficeTabControlStyle}\" x:Name=\"CustomerTabs\"");
        StringAssert.Contains(organization, "<TabControl x:Name=\"OrganizationTabs\" Background=\"Transparent\"");
        StringAssert.Contains(organization, "<TabControl.Template>");
        Assert.AreEqual(
            1,
            CountOccurrences(organization, "Style=\"{StaticResource OfficeTabControlStyle}\""),
            "Chỉ tab cục bộ Kho hàng hiển thị tab strip; host route Tổ chức phải ẩn chrome để bám sidebar Web.");
        Assert.IsFalse(organization.Contains("Header=\"Tổng quan Công Ty\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DenseTables_UseSingleRowActionMenuInsteadOfButtonClusters()
    {
        var customer = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");

        Assert.AreEqual(2, CountOccurrences(customer, "Content=\"Thao tác ▾\""), "Khách hàng/Nhóm khách hàng/Nhà cung cấp dùng action trực tiếp như Web; menu còn lại thuộc địa chỉ hồ sơ chưa đến lượt re-audit.");
        Assert.AreEqual(1, CountOccurrences(organization, "Content=\"Thao tác ▾\""), "UI-2.2/UI-2.3 dùng action trực tiếp như Web; menu còn lại chỉ thuộc màn Nhân sự chưa đến lượt re-audit.");
        Assert.IsFalse(customer.Contains("Header=\"Hành động\" Width=\"330\"", StringComparison.Ordinal));
        Assert.IsFalse(customer.Contains("Header=\"Hành động\" Width=\"430\"", StringComparison.Ordinal));
        Assert.IsFalse(customer.Contains("Header=\"Thao tác\" Width=\"190\"", StringComparison.Ordinal));
        Assert.IsFalse(customer.Contains("Header=\"Thao tác\" Width=\"255\"", StringComparison.Ordinal));
        Assert.IsFalse(organization.Contains("Header=\"Thao tác\" Width=\"190\"", StringComparison.Ordinal));
        Assert.IsFalse(organization.Contains("Header=\"Thao tác\" Width=\"250\"", StringComparison.Ordinal));
        StringAssert.Contains(customer, "Style=\"{StaticResource OfficeRowContextMenuStyle}\"");
        StringAssert.Contains(organization, "Style=\"{StaticResource OfficeRowContextMenuStyle}\"");
    }

    [TestMethod]
    public void InternalOrganization_UsesSharedCompactDesktopControls()
    {
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");

        StringAssert.Contains(organization, "BasedOn=\"{StaticResource OfficeCardStyle}\"");
        StringAssert.Contains(organization, "BasedOn=\"{StaticResource OfficePrimaryButtonStyle}\"");
        StringAssert.Contains(organization, "BasedOn=\"{StaticResource OfficeDataGridStyle}\"");
        StringAssert.Contains(organization, "<Grid Margin=\"14\">");
        StringAssert.Contains(organization, "OfficeSummaryCardStyle");
        Assert.IsFalse(organization.Contains("<Setter Property=\"Padding\" Value=\"14,8\" />", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DesktopUiFourPointPolish_RemovesTabRingCentersGridTextAndAddsBranding()
    {
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");
        var customer = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(controls, "x:Name=\"Indicator\"");
        StringAssert.Contains(controls, "FocusVisualStyle\" Value=\"{x:Null}");
        Assert.IsFalse(controls.Contains("Setter TargetName=\"Surface\" Property=\"BorderBrush\"", StringComparison.Ordinal));
        StringAssert.Contains(controls, "x:Key=\"OfficeGridTextStyle\"");
        StringAssert.Contains(controls, "VerticalAlignment\" Value=\"Center\"");

        Assert.AreEqual(
            CountOccurrences(customer, "<DataGridTextColumn"),
            CountOccurrences(customer, "ElementStyle=\"{StaticResource OfficeGridTextStyle}\""),
            "Mọi cột text Khách hàng phải dùng cùng baseline giữa.");
        Assert.AreEqual(
            CountOccurrences(organization, "<DataGridTextColumn"),
            CountOccurrences(organization, "ElementStyle=\"{StaticResource OfficeGridTextStyle}\""),
            "Mọi cột text Tổ chức nội bộ phải dùng cùng baseline giữa.");

        StringAssert.Contains(shell, "https://retail.nguyenlieuhungphat.com/logo-transparent.png");
        Assert.IsFalse(shell.Contains("01-hero-nganh-hang.webp", StringComparison.Ordinal));
        Assert.IsFalse(shell.Contains("<BlurEffect Radius=\"2.4\" />", StringComparison.Ordinal));
        Assert.IsFalse(shell.Contains("<Rectangle Fill=\"#B8321F17\" />", StringComparison.Ordinal));
        StringAssert.Contains(shell, "Text=\"{Binding HeaderKicker}\"");
        StringAssert.Contains(shell, "Text=\"{Binding PageTitle}\"");
    }

    [TestMethod]
    public void SupplierWorkspace_Lot3Polish_UsesOfficeMasterDataLanguageAndSafeActions()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerViewModel.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerPresentation.cs");

        StringAssert.Contains(xaml, "Text=\"{Binding SupplierTotal}\"");
        StringAssert.Contains(xaml, "Text=\"{Binding SupplierActive}\"");
        StringAssert.Contains(xaml, "Header=\"STT\"");
        StringAssert.Contains(xaml, "Header=\"Tên nhà cung cấp\"");
        StringAssert.Contains(xaml, "Header=\"Ngân hàng\"");
        StringAssert.Contains(xaml, "Content=\"Sửa\"");
        StringAssert.Contains(xaml, "Content=\"{Binding ToggleAction}\"");
        StringAssert.Contains(xaml, "ItemsSource=\"{Binding SupplierStatusOptions}\"");
        StringAssert.Contains(xaml, "Text=\"Địa chỉ mặc định\"");
        StringAssert.Contains(xaml, "Text=\"Tỉnh/thành phố\"");
        StringAssert.Contains(xaml, "Text=\"Xã/phường/đặc khu\"");
        Assert.IsFalse(xaml.Contains("x:Name=\"SupplierTabs\"", StringComparison.Ordinal));
        Assert.IsFalse(xaml.Contains("Header=\"Hồ sơ nhà cung cấp\"", StringComparison.Ordinal));
        Assert.IsFalse(xaml.Contains("Header=\"Hồ sơ NCC\"", StringComparison.Ordinal));

        StringAssert.Contains(viewModel, "SupplierProvinceOptions");
        StringAssert.Contains(viewModel, "SupplierWardOptions");
        StringAssert.Contains(viewModel, "OpenCreateSupplierAsync");
        StringAssert.Contains(viewModel, "LoadSupplierWardsAsync");
        StringAssert.Contains(viewModel, "IsCreateMode && IsSupplierEditor");
        StringAssert.Contains(viewModel, "Địa chỉ mặc định cần địa chỉ chi tiết");
        StringAssert.Contains(presentation, "SupplierStatusOptions");
        StringAssert.Contains(presentation, "SupplierStatus(bool active)");
        StringAssert.Contains(presentation, "\"Ngừng sử dụng\"");
    }

    [TestMethod]
    public void WarehouseWorkspace_DistinguishesWarehouseLayoutAndLocations()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");
        var warehouseStart = xaml.IndexOf("x:Name=\"WarehouseTabs\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, warehouseStart);

        var expected = new[]
        {
            "Header=\"Kho hàng\"",
            "Header=\"Thiết lập nhanh\"",
            "Header=\"Sơ đồ kho\"",
            "Header=\"Lịch sử\""
        };

        var cursor = warehouseStart;
        foreach (var marker in expected)
        {
            var next = xaml.IndexOf(marker, cursor, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự tab Kho hàng: {marker}");
            cursor = next;
        }

        StringAssert.Contains(
            xaml,
            "Sơ đồ kho là cách chia khu/kệ/điểm chứa hàng bên trong kho; không phải địa chỉ vật lý của kho.");
        Assert.IsFalse(xaml.Contains("Header=\"Kho &amp; vị trí\"", StringComparison.Ordinal));
        Assert.IsFalse(xaml.Contains("Header=\"Vị trí kho\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void InternalOrganization_Lot2Polish_KeepsWarehouseWorkflowOfficeReady()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationViewModel.cs");

        StringAssert.Contains(xaml, "Margin=\"8,4,0,0\"");
        StringAssert.Contains(xaml, "Text=\"Thiết lập nhanh kho hàng\"");
        StringAssert.Contains(xaml, "Text=\"Sơ đồ kho là cách chia khu/kệ/điểm chứa hàng bên trong kho; không phải địa chỉ vật lý của kho.\"");
        StringAssert.Contains(xaml, "Text=\"{Binding CompletedBy}\"");
        StringAssert.Contains(xaml, "Text=\"{Binding EditorNameLabel}\"");
        StringAssert.Contains(xaml, "Content=\"{Binding EditorPrimaryActionLabel}\"");
        StringAssert.Contains(xaml, "IsEnabled=\"{Binding CanAddLocationToSelectedWarehouse}\"");
        Assert.IsFalse(xaml.Contains("Tên / Họ và tên", StringComparison.Ordinal));

        var historyStart = xaml.IndexOf("Header=\"Lịch sử\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, historyStart);
        var historyEnd = xaml.IndexOf("</TabItem>", historyStart, StringComparison.Ordinal);
        Assert.IsGreaterThan(historyStart, historyEnd);
        var history = xaml[historyStart..historyEnd];
        StringAssert.Contains(history, "ItemsSource=\"{Binding ActiveWarehouseOptions}\"");
        StringAssert.Contains(history, "SelectedValue=\"{Binding SelectedWarehouseId}\"");

        StringAssert.Contains(viewModel, "return OrganizationPresentation.LayoutMode(warehouse.LocationManagementMode);");
        StringAssert.Contains(viewModel, "public bool CanAddLocationToSelectedWarehouse");
        StringAssert.Contains(viewModel, "EditorKind.Warehouse => \"Tên kho\"");
        StringAssert.Contains(viewModel, "EditorKind.Location => \"Tên khu vực\"");
    }

    [TestMethod]
    public void OrganizationWarehousesUi23_MatchesCurrentWebFourTabWorkflow()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationViewModel.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Organization", "OrganizationPresentation.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shellCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var shellVm = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");

        StringAssert.Contains(shellVm, "\"catalog.warehouses\" => \"Kho hàng\"");
        StringAssert.Contains(shellVm, "\"catalog.warehouses\" => \"Quản lý kho, thiết lập nhanh và sơ đồ hàng hóa bên trong từng kho.\"");
        StringAssert.Contains(shellVm, "\"catalog.warehouses\" => \"DANH MỤC QUẢN LÝ\"");
        StringAssert.Contains(shell, "Click=\"CatalogWarehousesRefresh_OnClick\"");
        StringAssert.Contains(shellCode, "await _internalOrganizationView.RefreshAsync()");

        var warehouseStart = xaml.IndexOf("x:Name=\"WarehouseTabs\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, warehouseStart);
        var tabMarkers = new[]
        {
            "Header=\"Kho hàng\"",
            "Header=\"Thiết lập nhanh\"",
            "Header=\"Sơ đồ kho\"",
            "Header=\"Lịch sử\""
        };
        var cursor = warehouseStart;
        foreach (var marker in tabMarkers)
        {
            var next = xaml.IndexOf(marker, cursor, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự tab Kho hàng: {marker}");
            cursor = next;
        }

        StringAssert.Contains(xaml, "Text=\"Tra cứu kho\"");
        StringAssert.Contains(xaml, "Text=\"Tên kho, mã kho hoặc chi nhánh\"");
        StringAssert.Contains(xaml, "ItemsSource=\"{Binding WarehouseStatusOptions}\"");
        StringAssert.Contains(xaml, "Content=\"Tạo kho nhanh\"");
        StringAssert.Contains(xaml, "Click=\"OpenQuickWarehouse_OnClick\"");
        StringAssert.Contains(xaml, "Text=\"DANH MỤC QUẢN LÝ\"");
        StringAssert.Contains(xaml, "Text=\"{Binding WarehouseVisibleSummary, Mode=OneWay}\"");
        foreach (var header in new[] { "Mã", "Tên", "Thuộc chi nhánh", "Loại kho", "Sơ đồ kho", "Xuất vượt tồn", "Trạng thái", "Xử lý" })
        {
            StringAssert.Contains(xaml, $"Header=\"{header}\"");
        }
        StringAssert.Contains(xaml, "Content=\"Chỉnh sửa\"");
        StringAssert.Contains(xaml, "Content=\"Quản lý sơ đồ\"");
        StringAssert.Contains(xaml, "Content=\"{Binding StatusAction}\"");
        StringAssert.Contains(xaml, "Text=\"{Binding NegativeStockStatus}\"");
        StringAssert.Contains(xaml, "Text=\"{Binding WarehouseEmptyMessage, Mode=OneWay}\"");

        StringAssert.Contains(xaml, "Text=\"THAO TÁC NHANH\"");
        StringAssert.Contains(xaml, "Text=\"Ví dụ: YS-003\"");
        StringAssert.Contains(xaml, "Text=\"Tên kho dễ nhận biết\"");
        StringAssert.Contains(xaml, "ItemsSource=\"{Binding NegativeStockOptions}\"");
        StringAssert.Contains(xaml, "SelectedValue=\"{Binding QuickWarehouseNegativeStockPolicy}\"");
        StringAssert.Contains(xaml, "Mặc định tắt. Bật chính sách không tự cấp quyền cho người dùng.");
        Assert.IsFalse(xaml.Contains("Click=\"ResetQuickWarehouse_OnClick\"", StringComparison.Ordinal));

        StringAssert.Contains(xaml, "Text=\"BỐ TRÍ HÀNG HÓA\"");
        StringAssert.Contains(xaml, "Text=\"Sơ đồ kho là cách chia khu/kệ/điểm chứa hàng bên trong kho; không phải địa chỉ vật lý của kho.\"");
        StringAssert.Contains(xaml, "Text=\"{Binding SelectedWarehouseLocationSummary, Mode=OneWay}\"");
        foreach (var header in new[] { "Mã khu vực", "Tên khu vực", "Loại khu vực" })
        {
            StringAssert.Contains(xaml, $"Header=\"{header}\"");
        }
        Assert.IsFalse(xaml.Contains("IsStandaloneLocationsRoute", StringComparison.Ordinal));
        StringAssert.Contains(code, "_viewModel.EnterWarehouseLayout()");
        Assert.IsFalse(code.Contains("standaloneLocationsRoute", StringComparison.Ordinal));

        StringAssert.Contains(xaml, "ItemsSource=\"{Binding ActiveWarehouseOptions}\"");
        StringAssert.Contains(xaml, "Text=\"Lịch sử sơ đồ kho\"");
        StringAssert.Contains(xaml, "Text=\"Chi tiết lần thay đổi\"");
        StringAssert.Contains(xaml, "Content=\"Xem chi tiết\"");
        StringAssert.Contains(xaml, "Text=\"{Binding LocationModeDetailMeta}\"");
        StringAssert.Contains(xaml, "Text=\"{Binding LocationModeDetailSummary}\"");

        StringAssert.Contains(xaml, "Visibility=\"{Binding IsWarehouseEditor, Converter={StaticResource BooleanToVisibilityConverter}}\"");
        StringAssert.Contains(xaml, "ItemsSource=\"{Binding WarehouseBranchOptions}\"");
        StringAssert.Contains(xaml, "SelectedValue=\"{Binding DraftNegativeStockPolicy}\"");
        StringAssert.Contains(xaml, "Visibility=\"{Binding IsLocationEditor, Converter={StaticResource BooleanToVisibilityConverter}}\"");
        StringAssert.Contains(xaml, "Text=\"Khu vực trong kho\"");
        StringAssert.Contains(xaml, "Visibility=\"{Binding IsWarehouseStatusConfirmOpen, Converter={StaticResource BooleanToVisibilityConverter}}\"");
        StringAssert.Contains(code, "_viewModel.OpenWarehouseStatusConfirm(id)");
        StringAssert.Contains(code, "_viewModel.OpenLocationStatusConfirm(id)");
        Assert.IsFalse(code.Contains("ConfirmStatusChange(\"kho\")", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("ConfirmStatusChange(\"khu vực kho\")", StringComparison.Ordinal));

        StringAssert.Contains(xaml, "Text=\"THIẾT LẬP KHO\"");
        StringAssert.Contains(xaml, "Text=\"{Binding LayoutCurrentDescription, Mode=OneWay}\"");
        StringAssert.Contains(xaml, "Visibility=\"{Binding HasLayoutPreview, Converter={StaticResource BooleanToVisibilityConverter}}\"");
        StringAssert.Contains(xaml, "Text=\"{Binding LayoutPreviewDestinationText, Mode=OneWay}\"");
        StringAssert.Contains(code, "await _viewModel.ConfirmLayoutAsync()");
        Assert.IsFalse(code.Contains("Xác nhận thay đổi cách quản lý vị trí của kho?", StringComparison.Ordinal));

        StringAssert.Contains(viewModel, "public string WarehouseVisibleSummary");
        StringAssert.Contains(viewModel, "public string QuickWarehouseNegativeStockPolicy");
        StringAssert.Contains(viewModel, "public bool HasLayoutPreview");
        StringAssert.Contains(viewModel, "public bool LocationModeHistoryHasRuns");
        StringAssert.Contains(viewModel, "public void OpenWarehouseStatusConfirm(string id)");
        StringAssert.Contains(presentation, "public static readonly IReadOnlyList<LookupOption> WarehouseLocationTypeOptions");
        StringAssert.Contains(presentation, "public static string WarehouseNegativeStockStatus(bool enabled)");
    }

    [TestMethod]
    public void OrganizationLocationsUi24_FollowsCurrentWebRedirectToWarehouseLayout()
    {
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");
        var organizationCode = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml.cs");
        var organizationVm = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationViewModel.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shellCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var shellVm = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");

        // Current Công Ty Web source:
        // /organization/locations redirects to /organization/warehouses?tab=layout,
        // and app-shell-core filters nav-locations out of the rendered sidebar.
        Assert.IsFalse(
            shell.Contains("IsEnabled=\"False\"><TextBlock Text=\"Vị trí kho\" /></Button>", StringComparison.Ordinal),
            "Không giữ placeholder Vị trí kho khi Web hiện hành không render submenu này.");

        StringAssert.Contains(organizationCode, "if (string.Equals(target, \"locations\", StringComparison.Ordinal))");
        StringAssert.Contains(organizationCode, "WarehouseTabs.SelectedIndex = 2;");
        StringAssert.Contains(organizationCode, "_viewModel.EnterWarehouseLayout();");
        Assert.IsFalse(organizationCode.Contains("standaloneLocationsRoute", StringComparison.Ordinal));
        Assert.IsFalse(organization.Contains("IsStandaloneLocationsRoute", StringComparison.Ordinal));
        Assert.IsFalse(organizationVm.Contains("IsStandaloneLocationsRoute", StringComparison.Ordinal));

        StringAssert.Contains(shellCode, "case \"locations\":");
        StringAssert.Contains(shellCode, "await _viewModel.NavigateInternalOrganizationAsync(\"catalog.warehouses\");");
        StringAssert.Contains(shellVm, "if (string.Equals(navigationKey, \"catalog.locations\", StringComparison.Ordinal))");
        StringAssert.Contains(shellVm, "navigationKey = \"catalog.warehouses\";");

        // Redirect lands on the already-audited local Sơ đồ kho workflow.
        StringAssert.Contains(organization, "Header=\"Sơ đồ kho\"");
        StringAssert.Contains(organization, "Text=\"BỐ TRÍ HÀNG HÓA\"");
        StringAssert.Contains(organization, "Content=\"Thiết lập sơ đồ\"");
        StringAssert.Contains(organization, "Content=\"Thêm khu vực\"");
        StringAssert.Contains(organization, "Header=\"Mã khu vực\"");
        StringAssert.Contains(organization, "Header=\"Tên khu vực\"");
        StringAssert.Contains(organization, "Header=\"Loại khu vực\"");
        StringAssert.Contains(organization, "Content=\"{Binding StatusAction}\"");
        StringAssert.Contains(organization, "Text=\"{Binding WarehouseLayoutEmptyMessage, Mode=OneWay}\"");
    }

    [TestMethod]
    public void CustomersUi25_MatchesCurrentWebListGroupsDetailAndRetryContract()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerViewModel.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerPresentation.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shellVm = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");

        StringAssert.Contains(shellVm, "_partners.IsCustomerProfileOpen ? \"Chi tiết khách hàng\" : \"Khách hàng\"");
        StringAssert.Contains(shellVm, "Quản lý nhóm, hồ sơ khách hàng, điều khoản thanh toán, hạn mức và địa chỉ giao dịch.");
        StringAssert.Contains(shellVm, "Theo dõi thông tin, giao dịch và tình hình hiện tại của một khách hàng.");
        StringAssert.Contains(shellVm, "_partners.IsCustomerProfileOpen ? \"KHÁCH HÀNG\" : \"QUẢN LÝ KHÁCH HÀNG\"");
        StringAssert.Contains(shell, "Click=\"CatalogCustomersRefresh_OnClick\"");
        StringAssert.Contains(shell, "Click=\"CatalogCustomersCreate_OnClick\"");
        StringAssert.Contains(shell, "Content=\"Danh sách khách hàng\"");
        StringAssert.Contains(shell, "Content=\"Sửa thông tin\"");

        var customerStart = xaml.IndexOf("x:Name=\"CustomerTabs\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, customerStart);
        var visibleTabs = new[]
        {
            "Header=\"Khách hàng\"",
            "Header=\"Thiết lập nhanh\"",
            "Header=\"Nhập KH\"",
            "Header=\"Cập nhật KH\"",
            "Header=\"Nhóm khách hàng\""
        };
        var cursor = customerStart;
        foreach (var marker in visibleTabs)
        {
            var next = xaml.IndexOf(marker, cursor, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự tab Khách hàng: {marker}");
            cursor = next;
        }
        Assert.IsFalse(xaml.Contains("Header=\"Hồ sơ 360°\"", StringComparison.Ordinal));

        foreach (var text in new[]
        {
            "Tổng khách hàng", "Toàn bộ hồ sơ hiện có", "Đang hoạt động", "Hồ sơ đang được sử dụng",
            "Không hoạt động", "Hồ sơ đã ngừng sử dụng", "Tìm kiếm", "Mã, tên, liên hệ…",
            "Nhóm khách hàng", "Nhân viên phụ trách", "DANH SÁCH"
        })
        {
            StringAssert.Contains(xaml, $"Text=\"{text}\"");
        }

        foreach (var header in new[] { "STT", "Mã / tên", "Nhóm / phụ trách", "Liên hệ", "Thanh toán", "Trạng thái", "Hành động" })
        {
            StringAssert.Contains(xaml, $"Header=\"{header}\"");
        }
        StringAssert.Contains(xaml, "Click=\"CustomerName_OnClick\"");
        StringAssert.Contains(xaml, "Content=\"Sửa\"");
        StringAssert.Contains(xaml, "Content=\"Địa chỉ\"");
        StringAssert.Contains(xaml, "Content=\"{Binding ToggleAction}\"");
        StringAssert.Contains(xaml, "Text=\"Không có khách hàng phù hợp.\"");

        StringAssert.Contains(xaml, "Text=\"PHÂN LOẠI\"");
        StringAssert.Contains(xaml, "Text=\"{Binding CustomerGroupSummary, Mode=OneWay}\"");
        StringAssert.Contains(xaml, "Text=\"Chưa có nhóm khách hàng.\"");
        StringAssert.Contains(code, "await _viewModel.ToggleCustomerAsync(id)");
        StringAssert.Contains(code, "await _viewModel.ToggleGroupAsync(id)");
        Assert.IsFalse(code.Contains("ConfirmStatusChange(\"khách hàng\")", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("ConfirmStatusChange(\"nhóm khách hàng\")", StringComparison.Ordinal));

        StringAssert.Contains(xaml, "Visibility=\"{Binding IsCustomerAddressManagerOpen, Converter={StaticResource BooleanToVisibilityConverter}}\"");
        StringAssert.Contains(xaml, "Content=\"Thêm địa chỉ\"");
        StringAssert.Contains(code, "await _viewModel.OpenCustomerAddressesAsync(id)");
        StringAssert.Contains(viewModel, "public bool CanAddSelectedCustomerAddress");
        StringAssert.Contains(viewModel, "FindCustomer(SelectedCustomerId)?.IsActive == true");

        foreach (var profileTab in new[] { "Tổng quan", "Hàng đã mua", "Đơn hàng", "Công nợ &amp; thanh toán", "Giao hàng / Trả hàng", "Thông tin &amp; địa chỉ" })
        {
            StringAssert.Contains(xaml, $"Header=\"{profileTab}\"");
        }
        StringAssert.Contains(xaml, "Text=\"Hạn mức tín dụng\"");
        StringAssert.Contains(xaml, "Text=\"Tình hình khách hàng\"");
        StringAssert.Contains(xaml, "Header=\"Sản phẩm\"");
        StringAssert.Contains(xaml, "Header=\"ĐVT\"");
        StringAssert.Contains(xaml, "Text=\"{Binding CustomerOverviewAddressText}\"");
        Assert.AreEqual(1, CountOccurrences(xaml, "Text=\"3  Ảnh khách hàng\""), "Ảnh khách chỉ ở Thiết lập nhanh, không lặp trong hồ sơ chi tiết.");

        StringAssert.Contains(viewModel, "_pendingCreatedCustomer ?? await _service.CreateCustomerAsync(");
        StringAssert.Contains(viewModel, "RaisePendingCustomerState()");
        StringAssert.Contains(viewModel, "Lưu lại địa chỉ");
        StringAssert.Contains(viewModel, "RequireEditorAddressKey()");
        StringAssert.Contains(presentation, "new(\"all\", \"Tất cả\")");
        StringAssert.Contains(presentation, "active ? \"Đang hoạt động\" : \"Không hoạt động\"");
    }

    [TestMethod]
    public void ActiveStateColumns_UseTextColorOnlyWithoutDecorativePills()
    {
        foreach (var parts in new[]
        {
            new[] { "src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml" },
            new[] { "src", "CongTy.Desktop", "Partners", "PartnerView.xaml" }
        })
        {
            var xaml = ReadRepoFile(parts);
            var cursor = 0;
            while ((cursor = xaml.IndexOf("<DataGridTemplateColumn Header=\"Trạng thái\"", cursor, StringComparison.Ordinal)) >= 0)
            {
                var end = xaml.IndexOf("</DataGridTemplateColumn>", cursor, StringComparison.Ordinal);
                Assert.IsGreaterThan(cursor, end);
                var segment = xaml[cursor..(end + "</DataGridTemplateColumn>".Length)];

                if (segment.Contains("Text=\"{Binding Status}\"", StringComparison.Ordinal)
                    && segment.Contains("IsActive", StringComparison.Ordinal))
                {
                    Assert.IsFalse(segment.Contains("<Border", StringComparison.Ordinal),
                        $"Trạng thái chỉ được tô màu chữ, không bọc nền/viền: {string.Join("/", parts)}");
                    StringAssert.Contains(segment, "SuccessBrush");
                    StringAssert.Contains(segment, "DangerBrush");
                    StringAssert.Contains(segment, "VerticalAlignment=\"Center\"");
                }

                cursor = end + 1;
            }
        }
    }

    [TestMethod]
    public void AllDesktopBusinessTableTemplates_CenterTextAndActionsVertically()
    {
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");
        StringAssert.Contains(controls, "x:Key=\"OfficeDataGridRowStyle\"");
        StringAssert.Contains(controls, "<Setter Property=\"VerticalContentAlignment\" Value=\"Center\" />");
        StringAssert.Contains(controls, "x:Key=\"OfficeRowActionButtonStyle\"");

        foreach (var parts in new[]
        {
            new[] { "src", "CongTy.Desktop", "Inventory", "InventoryView.xaml" },
            new[] { "src", "CongTy.Desktop", "Inventory", "FulfillmentView.xaml" },
            new[] { "src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml" },
            new[] { "src", "CongTy.Desktop", "Partners", "PartnerView.xaml" },
            new[] { "src", "CongTy.Desktop", "Sales", "SalesView.xaml" }
        })
        {
            var xaml = ReadRepoFile(parts);
            var cursor = 0;
            while ((cursor = xaml.IndexOf("<DataGridTemplateColumn.CellTemplate>", cursor, StringComparison.Ordinal)) >= 0)
            {
                var template = xaml.IndexOf("<DataTemplate>", cursor, StringComparison.Ordinal);
                Assert.IsGreaterThanOrEqualTo(0, template);
                var rootStart = xaml.IndexOf('<', template + "<DataTemplate>".Length);
                var rootEnd = xaml.IndexOf('>', rootStart);
                Assert.IsGreaterThan(rootStart, rootEnd);
                var root = xaml[rootStart..(rootEnd + 1)];
                StringAssert.Contains(root, "VerticalAlignment=\"Center\"", $"Template cell chưa cân giữa hàng: {string.Join("/", parts)}");
                cursor = rootEnd + 1;
            }

            if (parts[^1] == "InventoryView.xaml")
            {
                StringAssert.Contains(xaml, "x:Key=\"InventoryCellTextStyle\"");
                StringAssert.Contains(xaml, "BasedOn=\"{StaticResource OfficeGridTextStyle}\"");
                StringAssert.Contains(xaml, "x:Key=\"NumericCellTextStyle\"");
                continue;
            }

            if (parts[^1] == "FulfillmentView.xaml")
            {
                StringAssert.Contains(xaml, "x:Key=\"FulfillmentCellTextStyle\"");
                StringAssert.Contains(xaml, "BasedOn=\"{StaticResource OfficeGridTextStyle}\"");
                StringAssert.Contains(xaml, "<Setter Property=\"VerticalAlignment\" Value=\"Center\" />");
                continue;
            }

            Assert.AreEqual(
                CountOccurrences(xaml, "<DataGridTextColumn"),
                CountOccurrences(xaml, "ElementStyle=\"{StaticResource OfficeGridTextStyle}\""),
                $"Mọi cột text phải dùng baseline giữa: {string.Join("/", parts)}");
        }
    }

    [TestMethod]
    public void ReportingGrids_KeepDynamicHeadersAndNumericAlignmentContract()
    {
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");
        var sales = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesReportingView.xaml");
        var salesCodeBehind = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesReportingView.xaml.cs");
        var inventory = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryView.xaml");

        StringAssert.Contains(controls, "x:Key=\"OfficeDataGridNumericHeaderStyle\"");
        StringAssert.Contains(controls, "x:Key=\"OfficeDataGridActionHeaderStyle\"");
        StringAssert.Contains(controls, "<Setter Property=\"HorizontalContentAlignment\" Value=\"Right\" />");
        StringAssert.Contains(controls, "<Setter Property=\"HorizontalContentAlignment\" Value=\"Center\" />");

        StringAssert.Contains(sales, "x:Name=\"AnalysisGrid\"");
        StringAssert.Contains(sales, "x:Name=\"TotalGrid\"");
        StringAssert.Contains(sales, "HeaderStyle=\"{StaticResource OfficeDataGridNumericHeaderStyle}\"");
        StringAssert.Contains(sales, "HeaderStyle=\"{StaticResource OfficeDataGridActionHeaderStyle}\"");

        StringAssert.Contains(salesCodeBehind, "BindDynamicColumnHeaders(viewModel);");
        StringAssert.Contains(salesCodeBehind, "AnalysisGrid.Columns[1]");
        StringAssert.Contains(salesCodeBehind, "AnalysisGrid.Columns[3]");
        StringAssert.Contains(salesCodeBehind, "TotalGrid.Columns[0]");
        StringAssert.Contains(salesCodeBehind, "TotalGrid.Columns[2]");
        StringAssert.Contains(salesCodeBehind, "nameof(SalesReportingViewModel.SelectedDimensionLabel)");
        StringAssert.Contains(salesCodeBehind, "nameof(SalesReportingViewModel.MetricHeader)");
        StringAssert.Contains(salesCodeBehind, "BindingOperations.SetBinding(");

        StringAssert.Contains(inventory, "Header=\"Đã giữ\"");
        StringAssert.Contains(inventory, "Header=\"Chi tiết\" Width=\"70\" HeaderStyle=\"{StaticResource OfficeDataGridActionHeaderStyle}\"");
        StringAssert.Contains(inventory, "Header=\"Có thể xuất\" Binding=\"{Binding Available}\" Width=\"0.8*\" HeaderStyle=\"{StaticResource OfficeDataGridNumericHeaderStyle}\"");
        StringAssert.Contains(inventory, "Header=\"Giá bình quân\" Binding=\"{Binding AverageCost}\" Width=\"1*\" HeaderStyle=\"{StaticResource OfficeDataGridNumericHeaderStyle}\"");
    }

    [TestMethod]
    public void Ui53_PurchasingReporting_UsesCurrentOperationalDrillAndHeaderContract()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shellCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var shellViewModel = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchasingReportingView.xaml");
        var viewCode = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchasingReportingView.xaml.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Purchasing", "PurchasingReportingViewModel.cs");

        StringAssert.Contains(viewModel, "core.reporting.purchasing.read");
        StringAssert.Contains(viewModel, "core.purchase-order.read");
        StringAssert.Contains(viewModel, "core.goods-receipt.read");
        StringAssert.Contains(viewModel, "public bool CanOpenPurchaseOrders");
        StringAssert.Contains(viewModel, "public bool CanOpenGoodsReceipts");

        Assert.IsGreaterThanOrEqualTo(
            12,
            CountOccurrences(view, "HeaderStyle=\"{StaticResource OfficeDataGridNumericHeaderStyle}\""));
        Assert.AreEqual(2, CountOccurrences(view, "Content=\"Xem đơn\""));
        Assert.AreEqual(2, CountOccurrences(view, "Header=\"Chi tiết\" Width=\"82\" HeaderStyle=\"{StaticResource OfficeDataGridActionHeaderStyle}\""));
        StringAssert.Contains(view, "OpenSupplierOrders_OnClick");
        StringAssert.Contains(view, "OpenSkuOrders_OnClick");

        StringAssert.Contains(viewCode, "PurchaseOrdersRequested");
        StringAssert.Contains(viewCode, "new PurchaseOrderSearchRequestedEventArgs(row.Code)");
        StringAssert.Contains(viewCode, "new PurchaseOrderSearchRequestedEventArgs(row.SourceDocument)");

        StringAssert.Contains(shell, "Click=\"PurchasingReportingOrders_OnClick\"");
        StringAssert.Contains(shell, "Content=\"Đơn mua hàng\"");
        StringAssert.Contains(shell, "Click=\"PurchasingReportingReceipts_OnClick\"");
        StringAssert.Contains(shell, "Content=\"Phiếu nhận hàng\"");
        StringAssert.Contains(shellCode, "purchasingReportingView.PurchaseOrdersRequested += PurchasingReportingView_OnPurchaseOrdersRequested;");
        StringAssert.Contains(shellViewModel, "NavigatePurchaseOrdersSearchAsync");
        StringAssert.Contains(shellViewModel, "_purchaseOrders.SearchText = searchText?.Trim() ?? string.Empty;");
    }

    [TestMethod]
    public void MasterPlan_RequiresBusinessLayoutParityWithoutPixelCopy()
    {
        var masterPlan = ReadRepoFile("DESKTOP_MASTER_PLAN.md");
        StringAssert.Contains(masterPlan, "Công Ty Web là chuẩn bố cục nghiệp vụ");
        StringAssert.Contains(masterPlan, "information architecture + business flow");
        StringAssert.Contains(masterPlan, "không bê pixel giao diện web sang desktop một cách máy móc");
    }

    [TestMethod]
    public void SalesLot4_UsesCanonicalWorkspaceAndLifecycleActions()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var sales = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesView.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesViewModel.cs");
        var service = ReadRepoFile("src", "CongTy.ApiClient", "SalesOrderService.cs");
        var plan = ReadRepoFile("DESKTOP_MASTER_PLAN.md");

        StringAssert.Contains(shell, "Text=\"Đơn bán hàng\"");
        StringAssert.Contains(shell, "x:Name=\"SalesHost\"");
        StringAssert.Contains(sales, "Text=\"Tổng số đơn\"");
        StringAssert.Contains(sales, "Text=\"Đang xử lý\"");
        StringAssert.Contains(sales, "Text=\"Chờ giao\"");
        StringAssert.Contains(sales, "Content=\"Tạo đơn bán hàng\"");
        StringAssert.Contains(sales, "Content=\"Xác nhận &amp; cấp số\"");
        StringAssert.Contains(sales, "Content=\"Xuất kho\"");
        StringAssert.Contains(sales, "Content=\"Tạo bản điều chỉnh\"");
        StringAssert.Contains(sales, "Text=\"Hoàn thành đơn và Nộp tiền / Nợ\"");

        StringAssert.Contains(viewModel, "core.sales-order.read");
        StringAssert.Contains(viewModel, "core.sales-order.price.override");
        StringAssert.Contains(viewModel, "core.sales-order.discount.override");
        StringAssert.Contains(viewModel, "core.customer-payment.create");
        StringAssert.Contains(viewModel, "_actionKeys");

        StringAssert.Contains(service, "/api/sales-orders/sku-search");
        StringAssert.Contains(service, "/api/sales-orders/price-preview");
        StringAssert.Contains(service, "/issue-stock");
        StringAssert.Contains(service, "/close-execution");
        StringAssert.Contains(service, "/api/manual-sales-orders/");

        StringAssert.Contains(plan, "Migration source head đã audit: **136**");
        StringAssert.Contains(plan, "không mặc định production đã chạy 136 chỉ từ source");
    }

    [TestMethod]
    public void SalesLot4A_ClosesCanonicalCorrectnessGaps()
    {
        var contracts=ReadRepoFile("src","CongTy.Contracts","SalesContracts.cs");
        var service=ReadRepoFile("src","CongTy.ApiClient","SalesOrderService.cs");
        var presentation=ReadRepoFile("src","CongTy.Desktop","Sales","SalesPresentation.cs");
        var viewModel=ReadRepoFile("src","CongTy.Desktop","Sales","SalesViewModel.cs");
        var sales=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml");
        var print=ReadRepoFile("src","CongTy.Desktop","Sales","SalesOrderPrintPreview.cs");

        StringAssert.Contains(contracts,"[JsonPropertyName(\"total\")] public string? Total");
        StringAssert.Contains(contracts,"salesOrderLineId");
        StringAssert.Contains(contracts,"warehouseOnHandBaseQuantity");
        StringAssert.Contains(viewModel,"public int PreparingCount");
        StringAssert.Contains(viewModel,"SalesPresentation.Money(o.Total)");
        Assert.IsFalse(viewModel.Contains("Stage(o) is \"active\" or \"preparing\"",StringComparison.Ordinal));
        StringAssert.Contains(service,"/api/pickup-sales-orders/{RequireId(id)}/complete");
        StringAssert.Contains(service,"/api/pickup-sales-orders/{RequireId(id)}/settlement");
        StringAssert.Contains(service,"new SalesExpectedRevisionRequest(expectedRevision, mode)");
        StringAssert.Contains(viewModel,"pickup?\"PICKUP\":null");
        StringAssert.Contains(viewModel,"_service.CompletePickupAsync");
        StringAssert.Contains(viewModel,"_service.SettlePickupAsync");
        StringAssert.Contains(viewModel,"OpenEditor(EditorMode.Create,null,null,\"sales-order-create\")");
        StringAssert.Contains(presentation,"resetManualPrice?string.Empty");
        StringAssert.Contains(sales,"Content=\"Sao chép đơn\"");
        StringAssert.Contains(sales,"Content=\"In đơn\"");
        StringAssert.Contains(sales,"Header=\"Tồn thực tế\"");
        StringAssert.Contains(sales,"Header=\"Đơn khác đang giữ\"");
        StringAssert.Contains(sales,"Header=\"Khả dụng cho đơn này\"");
        StringAssert.Contains(print,"PHIẾU XUẤT KHO");
        StringAssert.Contains(print,"DocumentViewer");
        StringAssert.Contains(print,"PrintDialog");
    }

    [TestMethod]
    public void SalesLot4B_UsesFastOfficeEntryAndOneGoodsTable()
    {
        var contracts=ReadRepoFile("src","CongTy.Contracts","SalesContracts.cs");
        var service=ReadRepoFile("src","CongTy.ApiClient","SalesOrderService.cs");
        var cache=ReadRepoFile("src","CongTy.Desktop","Sales","SalesSkuLocalCatalog.cs");
        var viewModel=ReadRepoFile("src","CongTy.Desktop","Sales","SalesViewModel.cs");
        var sales=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml");
        StringAssert.Contains(contracts,"SalesOrderEntrySettingsUpdateRequest");
        StringAssert.Contains(service,"/api/products/sales-order-local-catalog");
        StringAssert.Contains(service,"UpdateEntrySettingsAsync");
        StringAssert.Contains(cache,"NormalizeSearchText"); StringAssert.Contains(cache,"SearchRank"); StringAssert.Contains(cache,"sales-sku-catalog-v2.json");
        StringAssert.Contains(viewModel,"core.customer.write"); StringAssert.Contains(viewModel,"CreateQuickCustomerAsync"); StringAssert.Contains(viewModel,"sales-quick-customer"); StringAssert.Contains(viewModel,"PersistEntryDefaultsAsync");
        StringAssert.Contains(sales,"x:Name=\"CustomerSearchBox\""); StringAssert.Contains(sales,"Content=\"+ Tạo nhanh\""); StringAssert.Contains(sales,"Content=\"Nhớ kho\""); StringAssert.Contains(sales,"Content=\"Nhớ giao\""); StringAssert.Contains(sales,"x:Name=\"SkuSearchBox\"");
        StringAssert.Contains(sales,"ItemsSource=\"{Binding SkuRows}\""); StringAssert.Contains(sales,"ItemsSource=\"{Binding DraftLines}\"");
        Assert.IsFalse(sales.Contains("<DataGrid Height=\"125\" Style=\"{StaticResource ListGridStyle}\" ItemsSource=\"{Binding SkuRows}\"",StringComparison.Ordinal));
        StringAssert.Contains(sales,"x:Name=\"MoreInfoPopup\""); StringAssert.Contains(sales,"Text=\"Tiền hàng\""); StringAssert.Contains(sales,"Text=\"Chiết khấu\""); StringAssert.Contains(sales,"Text=\"Thuế\""); StringAssert.Contains(sales,"Text=\"Tổng thanh toán dự kiến\"");
    }

    [TestMethod]
    public void SalesLot4B_UxParity_UsesSearchPopupsDirectRowEditingAndFocus()
    {
        var service=ReadRepoFile("src","CongTy.ApiClient","SalesOrderService.cs");
        var presentation=ReadRepoFile("src","CongTy.Desktop","Sales","SalesPresentation.cs");
        var viewModel=ReadRepoFile("src","CongTy.Desktop","Sales","SalesViewModel.cs");
        var sales=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml");
        var codeBehind=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml.cs");

        StringAssert.Contains(service,"/api/products/{RequireId(productId)}/variants");
        StringAssert.Contains(sales,"IsOpen=\"{Binding ShowCustomerResults, Mode=OneWay}\"");
        StringAssert.Contains(sales,"x:Name=\"CustomerResultsList\"");
        StringAssert.Contains(sales,"MouseLeftButtonUp=\"CustomerResults_OnMouseLeftButtonUp\"");
        Assert.IsFalse(sales.Contains("ItemsSource=\"{Binding CustomerOptions}\" DisplayMemberPath=\"Label\" SelectedValuePath=\"Id\" SelectedValue=\"{Binding DraftCustomerId}\"",StringComparison.Ordinal));
        StringAssert.Contains(sales,"IsOpen=\"{Binding ShowSkuResults, Mode=OneWay}\"");
        StringAssert.Contains(sales,"x:Name=\"OrderLinesGrid\"");
        StringAssert.Contains(sales,"Text=\"{Binding Quantity, UpdateSourceTrigger=PropertyChanged}\"");
        StringAssert.Contains(sales,"Text=\"{Binding DirectUnitPriceText, UpdateSourceTrigger=LostFocus}\"");
        StringAssert.Contains(sales,"ItemsSource=\"{Binding UnitOptions}\"");
        StringAssert.Contains(sales,"SelectedValue=\"{Binding DiscountMode, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"");
        StringAssert.Contains(codeBehind,"FocusQuantity(line)");
        StringAssert.Contains(codeBehind,"textBox.SelectAll()");
        StringAssert.Contains(codeBehind,"QuantityPlus_OnClick");
        StringAssert.Contains(codeBehind,"UnitCombo_OnDropDownOpened");
        StringAssert.Contains(codeBehind,"MoreInfo_OnClick");
        StringAssert.Contains(sales,"x:Name=\"MoreInfoPopup\"");
        Assert.IsFalse(sales.Contains("Grid.Row=\"2\" Grid.ColumnSpan=\"7\"",StringComparison.Ordinal));
        StringAssert.Contains(presentation,"new(\"PERCENT\",\"%\")");
        StringAssert.Contains(presentation,"new(\"PER_UNIT\",\"đ/ĐVT\")");
        StringAssert.Contains(presentation,"new(\"TOTAL_AMOUNT\",\"Tổng đ\")");
        StringAssert.Contains(viewModel,"ChangeLineVariantAsync");
        StringAssert.Contains(viewModel,"RepriceLineAsync");
        Assert.IsFalse(viewModel.Contains("số tiền hợp lệ và lý do",StringComparison.Ordinal));
        StringAssert.Contains(presentation,"DirectUnitPriceText");
        Assert.IsFalse(sales.Contains("Height=\"270\"",StringComparison.Ordinal));
    }

    [TestMethod]
    public void SalesPolish_UsesCardSearchUnderlineEditorsAndCompactOrderList()
    {
        var sales=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml");
        var presentation=ReadRepoFile("src","CongTy.Desktop","Sales","SalesPresentation.cs");
        var viewModel=ReadRepoFile("src","CongTy.Desktop","Sales","SalesViewModel.cs");

        StringAssert.Contains(sales,"x:Key=\"SearchResultListBoxItemStyle\"");
        StringAssert.Contains(sales,"MaxHeight=\"440\"");
        StringAssert.Contains(sales,"MaxHeight=\"520\"");
        StringAssert.Contains(sales,"ItemContainerStyle=\"{StaticResource SearchResultListBoxItemStyle}\"");
        StringAssert.Contains(sales,"BorderThickness\" Value=\"0,0,0,1\"");
        StringAssert.Contains(sales,"Header=\"STT\" Binding=\"{Binding Stt}\"");
        Assert.IsFalse(sales.Contains("Header=\"Kho\" Binding=\"{Binding Warehouse}\"",StringComparison.Ordinal));
        StringAssert.Contains(sales,"Header=\"Khách hàng\" Binding=\"{Binding Customer}\" Width=\"2*\"");
        StringAssert.Contains(presentation,"ListNumber");
        StringAssert.Contains(viewModel,"Select((o,index)=>new SalesOrderRow");
    }

    [TestMethod]
    public void SalesList_UsesHeldStockAndTextOnlyLaneStageTones()
    {
        var contracts=ReadRepoFile("src","CongTy.Contracts","SalesContracts.cs");
        var presentation=ReadRepoFile("src","CongTy.Desktop","Sales","SalesPresentation.cs");
        var viewModel=ReadRepoFile("src","CongTy.Desktop","Sales","SalesViewModel.cs");
        var sales=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml");

        StringAssert.Contains(contracts,"heldQuantity");
        StringAssert.Contains(presentation,"LaneKey");
        StringAssert.Contains(presentation,"StageKey");
        StringAssert.Contains(viewModel,"InventoryPreview.HeldQuantity");
        StringAssert.Contains(sales,"Text=\"  |  giữ \"");
        StringAssert.Contains(sales,"x:Key=\"OrderLaneTextStyle\"");
        StringAssert.Contains(sales,"x:Key=\"OrderStageTextStyle\"");
        StringAssert.Contains(sales,"DataTrigger Binding=\"{Binding LaneKey}\" Value=\"manual\"");
        StringAssert.Contains(sales,"DataTrigger Binding=\"{Binding StageKey}\" Value=\"preparing\"");
        StringAssert.Contains(sales,"<TextBlock VerticalAlignment=\"Center\" Style=\"{StaticResource OrderLaneTextStyle}\" Text=\"{Binding Lane}\" />");
        StringAssert.Contains(sales,"<TextBlock VerticalAlignment=\"Center\" Style=\"{StaticResource OrderStageTextStyle}\" Text=\"{Binding Stage}\" />");
    }

    [TestMethod]
    public void SalesLot4C_ClosesCommercialParityAndKeyboardWorkflow()
    {
        var contracts=ReadRepoFile("src","CongTy.Contracts","SalesContracts.cs");
        var service=ReadRepoFile("src","CongTy.ApiClient","SalesOrderService.cs");
        var presentation=ReadRepoFile("src","CongTy.Desktop","Sales","SalesPresentation.cs");
        var viewModel=ReadRepoFile("src","CongTy.Desktop","Sales","SalesViewModel.cs");
        var sales=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml");
        var codeBehind=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml.cs");

        StringAssert.Contains(contracts,"SalesPriceStepData");
        StringAssert.Contains(contracts,"InventoryMovementHistoryData");
        StringAssert.Contains(service,"/api/inventory/balances?warehouseId=");
        StringAssert.Contains(service,"/api/inventory/balances/history?warehouseId=");
        StringAssert.Contains(service,"scope=warehouse");

        StringAssert.Contains(presentation,"ClientLineId");
        StringAssert.Contains(presentation,"SplitDraftLine");
        StringAssert.Contains(presentation,"PriceStepRows");
        StringAssert.Contains(viewModel,"SplitLineAsync");
        StringAssert.Contains(viewModel,"OpenInventoryHistoryAsync");
        StringAssert.Contains(viewModel,"core.inventory.read");
        StringAssert.Contains(viewModel,"SALES_PRICE_CHANGED");
        StringAssert.Contains(viewModel,"RepriceAllAsync");
        StringAssert.Contains(viewModel,"ResetEditorMutationKeys");
        StringAssert.Contains(viewModel,"HasUnsavedEditorChanges");
        StringAssert.Contains(viewModel,"CurrentPricingAt()");
        Assert.IsFalse(viewModel.Contains("DraftPriceSelectionMode,DateTimeOffset.UtcNow.ToString",StringComparison.Ordinal));

        StringAssert.Contains(sales,"MinRowHeight=\"64\"");
        Assert.IsFalse(sales.Contains("RowHeight=\"46\"",StringComparison.Ordinal));
        StringAssert.Contains(sales,"TextWrapping=\"Wrap\"");
        StringAssert.Contains(sales,"Click=\"OpenInventoryHistory_OnClick\"");
        StringAssert.Contains(sales,"Click=\"SplitLine_OnClick\"");
        StringAssert.Contains(sales,"Click=\"TogglePriceDetail_OnClick\"");
        StringAssert.Contains(sales,"ItemsSource=\"{Binding PriceSteps}\"");
        StringAssert.Contains(sales,"ItemsSource=\"{Binding InventoryHistoryRows}\"");
        StringAssert.Contains(sales,"Text=\"{Binding InventoryHistorySku, Mode=OneWay}\"");
        StringAssert.Contains(sales,"Tag=\"{Binding ClientLineId}\"");
        StringAssert.Contains(sales,"Uid=\"QuantityInput\"");
        StringAssert.Contains(sales,"Uid=\"PriceInput\"");
        StringAssert.Contains(sales,"x:Name=\"CustomerResultsList\"");
        StringAssert.Contains(sales,"x:Name=\"SkuResultsList\"");

        StringAssert.Contains(codeBehind,"Key.F3");
        StringAssert.Contains(codeBehind,"Key.F4");
        StringAssert.Contains(codeBehind,"CustomerResults_OnMouseLeftButtonUp");
        StringAssert.Contains(codeBehind,"SkuResults_OnMouseLeftButtonUp");
        StringAssert.Contains(sales,"PreviewKeyDown=\"CustomerSearch_OnPreviewKeyDown\"");
        StringAssert.Contains(sales,"PreviewKeyDown=\"SkuSearch_OnPreviewKeyDown\"");
        StringAssert.Contains(codeBehind,"CustomerSearch_OnPreviewKeyDown");
        StringAssert.Contains(codeBehind,"SkuSearch_OnPreviewKeyDown");
        StringAssert.Contains(codeBehind,"MoveListSelection(CustomerResultsList");
        StringAssert.Contains(codeBehind,"MoveListSelection(SkuResultsList");
        StringAssert.Contains(codeBehind,"LineQuantity_OnPreviewKeyDown");
        StringAssert.Contains(codeBehind,"FocusPrice(row)");
        StringAssert.Contains(codeBehind,"FocusUnit(split)");
        StringAssert.Contains(codeBehind,"Đơn có thay đổi chưa lưu. Đóng và bỏ thay đổi?");
    }

    [TestMethod]
    public void SalesCreate_RowCardsExpandAndDropdownKeyboardUsesRealSelection()
    {
        var sales=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml");
        var codeBehind=ReadRepoFile("src","CongTy.Desktop","Sales","SalesView.xaml.cs");

        StringAssert.Contains(sales,"x:Key=\"OrderLineRowStyle\"");
        StringAssert.Contains(sales,"<Setter Property=\"Height\" Value=\"Auto\" />");
        StringAssert.Contains(sales,"RowStyle=\"{StaticResource OrderLineRowStyle}\"");
        StringAssert.Contains(sales,"x:Name=\"CustomerResultsList\" Style=\"{StaticResource SearchResultListBoxStyle}\"");
        StringAssert.Contains(sales,"x:Name=\"SkuResultsList\" Style=\"{StaticResource SearchResultListBoxStyle}\"");
        StringAssert.Contains(sales,"ItemContainerStyle=\"{StaticResource SkuSearchResultListBoxItemStyle}\"");
        StringAssert.Contains(sales,"<Trigger Property=\"IsSelected\" Value=\"True\">");

        StringAssert.Contains(codeBehind,"MoveListSelection(CustomerResultsList");
        StringAssert.Contains(codeBehind,"MoveListSelection(SkuResultsList");
        StringAssert.Contains(codeBehind,"CustomerResultsList.SelectedItem as SalesLookupOption");
        StringAssert.Contains(codeBehind,"SkuResultsList.SelectedItem as SalesSkuSearchRow");
        StringAssert.Contains(codeBehind,"list.ScrollIntoView(item)");
        Assert.IsFalse(codeBehind.Contains("FocusResult(",StringComparison.Ordinal));
        Assert.IsFalse(codeBehind.Contains("MoveResultFocus(",StringComparison.Ordinal));
        Assert.IsFalse(sales.Contains("KeyDown=\"CustomerSearch_OnKeyDown\"",StringComparison.Ordinal));
        Assert.IsFalse(sales.Contains("KeyDown=\"SkuSearch_OnKeyDown\"",StringComparison.Ordinal));
    }

    [TestMethod]
    public void DashboardUi1_MatchesWebLaunchpadAndMovesNoticesIntoMainHeader()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "DashboardContracts.cs");
        var service = ReadRepoFile("src", "CongTy.ApiClient", "DashboardService.cs");
        var dashboard = ReadRepoFile("src", "CongTy.Desktop", "Dashboard", "DashboardView.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Dashboard", "DashboardViewModel.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shellVm = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");
        var partners = ReadRepoFile("src", "CongTy.Desktop", "Partners", "PartnerView.xaml");
        var sales = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesView.xaml");
        var inventory = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryView.xaml");

        StringAssert.Contains(service, "/api/reporting/sales");
        StringAssert.Contains(service, "/api/reporting/inventory");
        StringAssert.Contains(service, "/api/reporting/logistics");
        StringAssert.Contains(service, "/api/reporting/aging");
        StringAssert.Contains(contracts, "effectiveOrderCount");
        StringAssert.Contains(contracts, "onTimeFullRatePercent");
        StringAssert.Contains(contracts, "remainingAmount");

        foreach (var label in new[]
        {
            "Chi nhánh",
            "Kho hàng",
            "Vị trí kho",
            "Đơn bán hiệu lực",
            "Giá trị tồn kho",
            "Giao đủ đúng hạn",
            "Công nợ phải thu"
        })
        {
            StringAssert.Contains(viewModel, $"\"{label}\"");
        }

        StringAssert.Contains(dashboard, "Text=\"Chỉ số cần nhìn ngay\"");
        StringAssert.Contains(dashboard, "Text=\"Mở đúng việc, không qua màn trung gian\"");
        StringAssert.Contains(dashboard, "Text=\"Theo dõi xu hướng và điểm cần chú ý\"");
        StringAssert.Contains(dashboard, "Text=\"Đơn bán hàng\"");
        StringAssert.Contains(dashboard, "Text=\"Khách hàng\"");
        StringAssert.Contains(dashboard, "Text=\"Tra cứu tồn kho\"");
        StringAssert.Contains(dashboard, "Text=\"Báo cáo tồn kho\"");
        StringAssert.Contains(dashboard, "Text=\"Phiếu giao hàng\"");
        StringAssert.Contains(dashboard, "Text=\"Công nợ phải thu\"");
        StringAssert.Contains(dashboard, "Text=\"Tuổi nợ\"");

        StringAssert.Contains(shell, "Text=\"{Binding ActiveNotice}\"");
        StringAssert.Contains(shell, "Visibility=\"{Binding HasActiveNotice, Converter={StaticResource BooleanToVisibilityConverter}}\"");
        Assert.IsFalse(shell.Contains("Text=\"{Binding WorkspaceMessage}\"", StringComparison.Ordinal));
        StringAssert.Contains(shellVm, "public string ActiveNotice =>");
        StringAssert.Contains(shellVm, "RaiseActiveNotice();");
        StringAssert.Contains(shell, "x:Name=\"HomeHost\"");

        Assert.IsFalse(organization.Contains("Style=\"{StaticResource MessageBorderStyle}\"\n                Margin=\"0,0,0,8\"", StringComparison.Ordinal));
        Assert.IsFalse(partners.Contains("Style=\"{StaticResource MessageBorderStyle}\"\n                Margin=\"0,0,0,8\"", StringComparison.Ordinal));
        Assert.IsFalse(sales.Contains("Grid.Row=\"0\" Style=\"{StaticResource MessageBorderStyle}\"", StringComparison.Ordinal));
        Assert.IsFalse(inventory.Contains("VerticalAlignment=\"Bottom\"\n                   Text=\"{Binding Message}\"", StringComparison.Ordinal));

        StringAssert.Contains(viewModel, "core.reporting.sales.read");
        StringAssert.Contains(viewModel, "core.reporting.inventory.read");
        StringAssert.Contains(viewModel, "core.reporting.logistics.read");
        StringAssert.Contains(viewModel, "core.reporting.aging.read");
    }

    [TestMethod]
    public void OrganizationOverviewUi2_MatchesCurrentWebHierarchyAndActions()
    {
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");
        var organizationCode = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml.cs");
        var organizationVm = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationViewModel.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Organization", "OrganizationPresentation.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shellVm = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");

        StringAssert.Contains(organization, "Columns=\"3\"");
        StringAssert.Contains(organization, "Text=\"DANH MỤC NGHIỆP VỤ\"");
        StringAssert.Contains(organization, "Text=\"Truy cập nhanh\"");
        StringAssert.Contains(organization, "Text=\"CƠ CẤU VẬN HÀNH\"");
        StringAssert.Contains(organization, "Text=\"Cơ cấu chi nhánh và kho\"");
        StringAssert.Contains(organization, "Text=\"CẬP NHẬT GẦN ĐÂY\"");
        StringAssert.Contains(organization, "Text=\"Những hồ sơ vừa thay đổi\"");
        StringAssert.Contains(organization, "Header=\"Đơn vị liên quan\"");
        StringAssert.Contains(organization, "ItemsSource=\"{Binding OverviewHierarchyRows}\"");
        StringAssert.Contains(organization, "ItemsSource=\"{Binding OverviewRecentRows}\"");
        StringAssert.Contains(organization, "Click=\"OverviewBranches_OnClick\"");
        StringAssert.Contains(organization, "Click=\"OverviewWarehouses_OnClick\"");
        StringAssert.Contains(organization, "Click=\"OverviewLocations_OnClick\"");
        StringAssert.Contains(organization, "<TabControl.Template>");
        Assert.IsFalse(organization.Contains("x:Name=\"OrganizationTabs\" Style=\"{StaticResource OfficeTabControlStyle}\"", StringComparison.Ordinal));

        StringAssert.Contains(organization, "Text=\"{Binding OverviewRecordTotal, Mode=OneWay}\"");
        StringAssert.Contains(organization, "Text=\"{Binding WarehouseCount, Mode=OneWay}\"");
        StringAssert.Contains(organizationVm, "OverviewRecordTotal");
        StringAssert.Contains(organizationVm, "BranchStatusSummary");
        StringAssert.Contains(organizationVm, "WarehouseStatusSummary");
        StringAssert.Contains(organizationVm, "LocationStatusSummary");
        StringAssert.Contains(presentation, "BuildOverviewHierarchy");
        StringAssert.Contains(presentation, "BuildOverviewRecent");
        StringAssert.Contains(organizationCode, "NavigationRequested?.Invoke(\"branches\")");
        StringAssert.Contains(organizationCode, "NavigationRequested?.Invoke(\"warehouses\")");
        StringAssert.Contains(organizationCode, "NavigationRequested?.Invoke(\"locations\")");

        StringAssert.Contains(shell, "Click=\"CatalogOverview_OnClick\"");
        StringAssert.Contains(shell, "Click=\"CatalogOverviewRefresh_OnClick\"");
        Assert.IsFalse(shell.Contains("IsEnabled=\"False\"><TextBlock Text=\"Tổng quan cơ cấu\"", StringComparison.Ordinal));
        StringAssert.Contains(shellVm, "\"catalog.overview\" => \"Tổ chức\"");
        StringAssert.Contains(shellVm, "\"catalog.overview\" => \"Theo dõi cơ cấu chi nhánh, kho hàng và vị trí lưu trữ trong toàn hệ thống.\"");
        StringAssert.Contains(shellVm, "\"catalog.overview\" => \"BÁO CÁO QUẢN TRỊ\"");
    }

    [TestMethod]
    public void OrganizationBranchesUi2_MatchesCurrentWebLayoutAndWorkflow()
    {
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml");
        var organizationCode = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationView.xaml.cs");
        var organizationVm = ReadRepoFile("src", "CongTy.Desktop", "Organization", "InternalOrganizationViewModel.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Organization", "OrganizationPresentation.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shellCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var shellVm = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");

        StringAssert.Contains(shellVm, "\"catalog.branches\" => \"Chi nhánh\"");
        StringAssert.Contains(shellVm, "\"catalog.branches\" => \"Quản lý danh mục chi nhánh và thông tin liên hệ phục vụ vận hành, hạch toán và báo cáo.\"");
        StringAssert.Contains(shellVm, "\"catalog.branches\" => \"DANH MỤC TỔ CHỨC VÀ KHO\"");
        StringAssert.Contains(shell, "Click=\"CatalogBranchesRefresh_OnClick\"");
        StringAssert.Contains(shell, "Click=\"CatalogBranchesAdd_OnClick\"");
        StringAssert.Contains(shellCode, "_internalOrganizationView.OpenCreateBranch()");

        StringAssert.Contains(organization, "Text=\"Tra cứu theo mã hoặc tên\"");
        StringAssert.Contains(organization, "Text=\"Nhập mã hoặc tên…\"");
        StringAssert.Contains(organization, "ItemsSource=\"{Binding BranchStatusOptions}\"");
        StringAssert.Contains(organization, "Content=\"Cập nhật dữ liệu\"");
        StringAssert.Contains(organization, "Content=\"Thêm chi nhánh\"");
        StringAssert.Contains(organization, "Text=\"DANH MỤC QUẢN LÝ\"");
        StringAssert.Contains(organization, "Text=\"{Binding BranchVisibleSummary, Mode=OneWay}\"");
        StringAssert.Contains(organization, "Header=\"Mã\"");
        StringAssert.Contains(organization, "Header=\"Tên\"");
        StringAssert.Contains(organization, "Header=\"Liên hệ\"");
        StringAssert.Contains(organization, "Header=\"Trạng thái\"");
        StringAssert.Contains(organization, "Header=\"Cập nhật\"");
        StringAssert.Contains(organization, "Header=\"Xử lý\"");
        StringAssert.Contains(organization, "Content=\"Chỉnh sửa\"");
        StringAssert.Contains(organization, "Content=\"{Binding StatusAction}\"");
        StringAssert.Contains(organization, "Text=\"{Binding Address}\"");
        StringAssert.Contains(organization, "Text=\"{Binding Phone}\"");
        StringAssert.Contains(organization, "Text=\"{Binding Email}\"");
        StringAssert.Contains(organization, "Text=\"{Binding BranchEmptyMessage, Mode=OneWay}\"");

        StringAssert.Contains(organization, "Text=\"Mã chi nhánh\"");
        StringAssert.Contains(organization, "Text=\"Tên chi nhánh\"");
        StringAssert.Contains(organization, "Text=\"Địa chỉ\"");
        StringAssert.Contains(organization, "Text=\"Số điện thoại\"");
        StringAssert.Contains(organization, "Text=\"Email\"");
        StringAssert.Contains(organization, "Content=\"{Binding EditorPrimaryActionLabel}\"");
        StringAssert.Contains(organizationVm, "EditorKind.Branch when IsCreateMode => \"Tạo chi nhánh\"");
        StringAssert.Contains(organizationVm, "public bool IsSharedEditorOpen => IsEditorOpen && !IsBranchEditor");

        StringAssert.Contains(organization, "Text=\"XÁC NHẬN TRẠNG THÁI\"");
        StringAssert.Contains(organization, "Text=\"{Binding BranchStatusConfirmTitle}\"");
        StringAssert.Contains(organization, "Text=\"{Binding BranchStatusConfirmText}\"");
        StringAssert.Contains(organization, "Click=\"ConfirmBranchStatus_OnClick\"");
        StringAssert.Contains(organizationCode, "_viewModel.OpenBranchStatusConfirm(id)");
        StringAssert.Contains(organizationVm, "if (!MessageIsError)");
        StringAssert.Contains(presentation, "active ? \"Ngừng sử dụng\" : \"Đưa vào sử dụng\"");

        Assert.IsFalse(organization.Contains("Header=\"Tên chi nhánh\" Binding=\"{Binding Name}\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ShellUi0_FollowsWebNavigationHierarchyAndDesktopSettingsBoundary()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var controls = ReadRepoFile("src", "CongTy.Desktop", "Themes", "Controls.xaml");
        var standard = ReadRepoFile("docs", "UI_PARITY_STANDARD.md");

        var expected = new[]
        {
            "ĐIỀU HÀNH",
            "DANH MỤC QUẢN LÝ",
            "TỒN KHO VÀ LÔ HÀNG",
            "GIAO NHẬN VÀ ĐIỀU PHỐI",
            "BÁN HÀNG",
            "MUA HÀNG",
            "KẾ TOÁN VÀ CÔNG NỢ",
            "VẬN HÀNH HỆ THỐNG",
            "CÀI ĐẶT CÔNG TY",
            "QUẢN TRỊ HỆ THỐNG"
        };

        var cursor = -1;
        foreach (var label in expected)
        {
            var next = shell.IndexOf($"Text=\"{label}\"", cursor + 1, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Sai thứ tự nhóm sidebar: {label}");
            cursor = next;
        }

        StringAssert.Contains(shell, "Text=\"Danh mục nghiệp vụ\"");
        StringAssert.Contains(shell, "Text=\"Tồn kho và lô hàng\"");
        StringAssert.Contains(shell, "Text=\"Giao nhận và điều phối\"");
        StringAssert.Contains(shell, "Text=\"Nhân sự và phân quyền\"");
        StringAssert.Contains(shell, "ToolTip=\"Cài đặt ứng dụng\"");
        Assert.IsFalse(shell.Contains("01-hero-nganh-hang.webp", StringComparison.Ordinal));
        StringAssert.Contains(viewModel, "SidebarWidth => new(IsSidebarExpanded ? 280 : 84)");
        StringAssert.Contains(viewModel, "ToggleNavigationGroup");
        StringAssert.Contains(viewModel, "\"desktop.settings\" => \"Cài đặt ứng dụng\"");
        StringAssert.Contains(controls, "x:Key=\"NavSubItemStyle\"");
        StringAssert.Contains(controls, "<Setter Property=\"MinHeight\" Value=\"48\" />");
        StringAssert.Contains(standard, "relative layout + control order + action placement");
        Assert.IsFalse(standard.Contains("panel cố định, split view hoặc resize", StringComparison.Ordinal));
    }

    [TestMethod]
    public void MainWindow_StartupFailure_IsDiagnosedWithoutClosingApplication()
    {
        var code = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");

        StringAssert.Contains(code, "try");
        StringAssert.Contains(code, "await _viewModel.InitializeAsync()");
        StringAssert.Contains(code, "catch (Exception exception)");
        StringAssert.Contains(code, "WriteStartupDiagnostic(exception)");
        StringAssert.Contains(code, "startup-error.txt");
        StringAssert.Contains(code, "sẽ không tự đóng");
    }

    [TestMethod]
    public void SalesAccessChange_MarshalsCollectionRefreshToUiDispatcher()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Sales", "SalesViewModel.cs");

        StringAssert.Contains(viewModel, "_access.Changed += (_, _) => RunOnUiThread(RaisePermissions)");
        StringAssert.Contains(viewModel, "System.Windows.Application.Current?.Dispatcher");
        StringAssert.Contains(viewModel, "dispatcher.CheckAccess()");
        StringAssert.Contains(viewModel, "dispatcher.Invoke(action)");
        StringAssert.Contains(viewModel, "RaisePermissions()");
        StringAssert.Contains(viewModel, "RefreshSelected()");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        Assert.Fail($"Không tìm thấy tệp trong repo: {string.Join("/", parts)}");
        return string.Empty;
    }
}
