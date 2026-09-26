namespace CongTy.Desktop.Operations;

internal sealed record OfficeFormXlsxSpec(string[] Headers, int BlankRows = 12);

internal sealed record OfficeFormPdfSpec(
    string[] Meta,
    string[]? Columns = null,
    string[]? Signatures = null,
    string? Note = null,
    string PageSize = "A4");

internal sealed record OfficeFormDefinition(
    string Id,
    string GroupKey,
    string GroupName,
    string Name,
    string Purpose,
    OfficeFormXlsxSpec? Xlsx = null,
    OfficeFormPdfSpec? Pdf = null)
{
    public bool HasXlsx => Xlsx is not null;
    public bool HasPdf => Pdf is not null;
    public string Formats => HasXlsx && HasPdf ? "Excel + PDF" : HasXlsx ? "Excel" : "PDF";
}

internal sealed record OfficeFormGroupFilter(string Key, string Display);

internal static class OfficeFormsCatalog
{
    public const string SalesGroup = "Bán hàng & khách hàng";
    public const string PurchasingGroup = "Mua hàng & Nhà cung cấp";
    public const string InventoryGroup = "Kho";
    public const string LogisticsGroup = "Giao vận & COD";
    public const string WorkforceGroup = "Nhân sự";

    private static readonly string[] ItemHeaders =
        ["STT", "Mã hàng", "Tên hàng", "ĐVT", "Số lượng", "Đơn giá", "Thành tiền", "Ghi chú"];
    private static readonly string[] StockHeaders =
        ["STT", "Mã hàng", "Tên hàng", "ĐVT", "Vị trí", "Lô", "Số lượng", "Ghi chú"];
    private static readonly string[] ReconciliationHeaders =
        ["STT", "Ngày", "Số chứng từ", "Nội dung", "Phát sinh", "Đã thanh toán / đối trừ", "Còn lại", "Ghi chú"];
    private static readonly string[] CodHeaders =
        ["STT", "Ngày", "Chuyến / phiếu giao", "Khách hàng", "Tiền phải thu", "Tiền đã thu", "Chênh lệch", "Ghi chú"];

    public static IReadOnlyList<OfficeFormGroupFilter> Groups { get; } =
    [
        new("all", "Tất cả nhóm"),
        new("sales", SalesGroup),
        new("purchasing", PurchasingGroup),
        new("inventory", InventoryGroup),
        new("logistics", LogisticsGroup),
        new("workforce", WorkforceGroup)
    ];

    public static IReadOnlyList<OfficeFormDefinition> All { get; } =
    [
        new OfficeFormDefinition(
            "sales-order-blank", "sales", SalesGroup,
            "Mẫu đơn bán hàng / phiếu đặt hàng khách hàng",
            "Ghi nhận yêu cầu đặt hàng ngoài hệ thống trước khi nhập vào Công Ty.",
            Xlsx: new(ItemHeaders)),
        new OfficeFormDefinition(
            "quotation-blank", "sales", SalesGroup,
            "Mẫu báo giá",
            "Lập báo giá trống để điền, gửi khách hoặc in ký.",
            Xlsx: new(["STT", "Mã hàng", "Tên hàng", "ĐVT", "Số lượng", "Đơn giá", "Chiết khấu", "Thành tiền", "Ghi chú"]),
            Pdf: ItemPdf(["Khách hàng", "Ngày báo giá", "Hiệu lực đến", "Người lập"])),
        new OfficeFormDefinition(
            "sales-warehouse-issue-blank", "sales", SalesGroup,
            "Mẫu phiếu xuất kho",
            "Phiếu trống dùng khi cần ghi nhận xuất hàng bán bằng bản giấy.",
            Pdf: StockPdf(["Ngày xuất", "Kho xuất", "Khách hàng / bộ phận nhận", "Người lập"])),
        new OfficeFormDefinition(
            "delivery-note-blank", "sales", SalesGroup,
            "Mẫu phiếu giao hàng",
            "Bản trống để bàn giao hàng và ký nhận ngoài hệ thống.",
            Pdf: ItemPdf(["Ngày giao", "Khách hàng", "Địa chỉ giao", "Người giao"])),
        new OfficeFormDefinition(
            "packing-list-blank", "sales", SalesGroup,
            "Mẫu phiếu đóng gói",
            "Bản trống ghi nội dung kiện hàng trước khi bàn giao.",
            Pdf: ItemPdf(["Ngày đóng gói", "Khách hàng", "Số kiện", "Người đóng gói"], ["Người đóng gói", "Người kiểm tra", "Người nhận"])),
        new OfficeFormDefinition(
            "customer-receipt-blank", "sales", SalesGroup,
            "Mẫu phiếu thu",
            "Phiếu trống ghi nhận khoản tiền khách hàng nộp bằng chứng từ giấy.",
            Pdf: CashPdf(["Ngày thu", "Khách hàng / người nộp", "Số tiền", "Bằng chữ", "Nội dung thu"], "Người nộp tiền")),
        new OfficeFormDefinition(
            "customer-return-receipt-blank", "sales", SalesGroup,
            "Mẫu phiếu nhận hàng khách trả",
            "Ghi nhận hàng khách trả trước khi xử lý nghiệp vụ trên hệ thống.",
            Pdf: ItemPdf(["Ngày nhận", "Khách hàng", "Kho nhận", "Lý do trả"])),
        new OfficeFormDefinition(
            "customer-refund-blank", "sales", SalesGroup,
            "Mẫu phiếu hoàn tiền khách hàng",
            "Phiếu trống xác nhận khoản hoàn tiền cho khách hàng.",
            Pdf: CashPdf(["Ngày hoàn", "Khách hàng / người nhận", "Số tiền", "Bằng chữ", "Lý do hoàn"], "Người nhận tiền")),
        new OfficeFormDefinition(
            "customer-debt-reconciliation-blank", "sales", SalesGroup,
            "Biên bản đối chiếu công nợ khách hàng",
            "Đối chiếu các khoản phải thu, đã thu và số còn lại với khách hàng.",
            Xlsx: new(ReconciliationHeaders),
            Pdf: ReconcilePdf(["Khách hàng", "Từ ngày", "Đến ngày", "Ngày đối chiếu"])),
        new OfficeFormDefinition(
            "customer-information-blank", "sales", SalesGroup,
            "Mẫu khai báo thông tin khách hàng",
            "Thu thập thông tin khách hàng bằng file văn phòng, không phải file nhập hàng loạt.",
            Xlsx: new(["Tên khách hàng", "Tên giao dịch", "Mã số thuế", "Điện thoại", "Email", "Địa chỉ", "Người liên hệ", "Chức vụ", "Điều khoản thanh toán đề nghị", "Ghi chú"])),

        new OfficeFormDefinition(
            "purchase-order-blank", "purchasing", PurchasingGroup,
            "Mẫu đơn mua hàng",
            "Lập yêu cầu mua hàng trống để trao đổi hoặc in ký.",
            Xlsx: new(ItemHeaders),
            Pdf: ItemPdf(["Nhà cung cấp", "Ngày đặt hàng", "Ngày cần hàng", "Người phụ trách"])),
        new OfficeFormDefinition(
            "supplier-receipt-blank", "purchasing", PurchasingGroup,
            "Mẫu phiếu nhận hàng Nhà cung cấp",
            "Ghi nhận hàng nhận từ Nhà cung cấp bằng bản giấy.",
            Pdf: ItemPdf(["Ngày nhận", "Nhà cung cấp", "Kho nhận", "Người nhận"])),
        new OfficeFormDefinition(
            "supplier-return-blank", "purchasing", PurchasingGroup,
            "Mẫu phiếu trả hàng Nhà cung cấp",
            "Ghi nhận hàng trả lại Nhà cung cấp và lý do trả.",
            Pdf: ItemPdf(["Ngày trả", "Nhà cung cấp", "Kho xuất", "Lý do trả"])),
        new OfficeFormDefinition(
            "supplier-payment-blank", "purchasing", PurchasingGroup,
            "Mẫu phiếu chi / thanh toán Nhà cung cấp",
            "Phiếu trống ghi nhận khoản chi hoặc thanh toán cho Nhà cung cấp.",
            Pdf: CashPdf(["Ngày chi", "Nhà cung cấp / người nhận", "Số tiền", "Bằng chữ", "Nội dung chi"], "Người nhận tiền")),
        new OfficeFormDefinition(
            "supplier-debt-reconciliation-blank", "purchasing", PurchasingGroup,
            "Biên bản đối chiếu công nợ Nhà cung cấp",
            "Đối chiếu các khoản phải trả, đã thanh toán và số còn lại.",
            Xlsx: new(ReconciliationHeaders),
            Pdf: ReconcilePdf(["Nhà cung cấp", "Từ ngày", "Đến ngày", "Ngày đối chiếu"])),
        new OfficeFormDefinition(
            "supplier-information-blank", "purchasing", PurchasingGroup,
            "Mẫu khai báo thông tin Nhà cung cấp",
            "Thu thập hồ sơ Nhà cung cấp bằng file văn phòng, không phải file nhập hàng loạt.",
            Xlsx: new(["Tên Nhà cung cấp", "Tên giao dịch", "Mã số thuế", "Điện thoại", "Email", "Địa chỉ", "Người liên hệ", "Chức vụ", "Ngân hàng", "Số tài khoản", "Thời gian giao dự kiến", "Ghi chú"])),

        new OfficeFormDefinition(
            "warehouse-receipt-blank", "inventory", InventoryGroup,
            "Mẫu phiếu nhập kho",
            "Phiếu trống ghi nhận hàng nhập kho bằng chứng từ giấy.",
            Pdf: StockPdf(["Ngày nhập", "Kho nhập", "Nguồn hàng / bộ phận giao", "Người lập"])),
        new OfficeFormDefinition(
            "inventory-issue-blank", "inventory", InventoryGroup,
            "Mẫu phiếu xuất kho",
            "Phiếu trống ghi nhận xuất kho cho nhu cầu nội bộ hoặc nghiệp vụ kho.",
            Pdf: StockPdf(["Ngày xuất", "Kho xuất", "Bộ phận / người nhận", "Lý do xuất"])),
        new OfficeFormDefinition(
            "inventory-transfer-blank", "inventory", InventoryGroup,
            "Mẫu phiếu chuyển kho",
            "Ghi nhận bàn giao hàng giữa kho nguồn và kho đích.",
            Pdf: StockPdf(["Ngày chuyển", "Kho nguồn", "Kho đích", "Người lập"])),
        new OfficeFormDefinition(
            "stocktake-blank", "inventory", InventoryGroup,
            "Mẫu phiếu kiểm kê",
            "Biểu mẫu trống để kiểm đếm thực tế, không chứa SKU hay tồn hiện tại của hệ thống.",
            Xlsx: new(["STT", "Mã hàng", "Tên hàng", "ĐVT", "Vị trí", "Lô", "Số đếm thực tế", "Ghi chú"]),
            Pdf: StockPdf(["Ngày kiểm kê", "Kho", "Khu vực kiểm kê", "Người phụ trách"])),
        new OfficeFormDefinition(
            "inventory-adjustment-blank", "inventory", InventoryGroup,
            "Mẫu phiếu điều chỉnh tồn",
            "Biểu mẫu trống ghi đề nghị điều chỉnh số lượng tồn.",
            Xlsx: new(["STT", "Mã hàng", "Tên hàng", "ĐVT", "Vị trí", "Lô", "Số lượng trước", "Số lượng đề nghị", "Chênh lệch", "Lý do"]),
            Pdf: StockPdf(["Ngày đề nghị", "Kho", "Bộ phận đề nghị", "Lý do chung"])),
        new OfficeFormDefinition(
            "picking-blank", "inventory", InventoryGroup,
            "Mẫu phiếu soạn hàng / cấp hàng",
            "Phiếu trống hướng dẫn soạn hoặc cấp hàng khi cần làm việc bằng giấy.",
            Pdf: StockPdf(["Ngày soạn", "Kho", "Bộ phận / khách nhận", "Người phụ trách"])),

        new OfficeFormDefinition(
            "delivery-trip-blank", "logistics", LogisticsGroup,
            "Mẫu phiếu chuyến giao hàng",
            "Phiếu trống lập chuyến và danh sách điểm giao.",
            Pdf: new(["Ngày giao", "Tài xế", "Xe / biển số", "Kho xuất"], ["STT", "Khách hàng", "Địa chỉ", "Số phiếu giao", "Tiền COD", "Ghi chú"], ["Điều phối", "Tài xế", "Người bàn giao"])),
        new OfficeFormDefinition(
            "driver-handover-blank", "logistics", LogisticsGroup,
            "Biên bản bàn giao hàng cho tài xế",
            "Biên bản trống xác nhận số hàng và chứng từ giao cho tài xế.",
            Pdf: new(["Ngày bàn giao", "Tài xế", "Xe / biển số", "Người bàn giao"], ["STT", "Chứng từ", "Khách hàng", "Số kiện", "Nội dung", "Ghi chú"], ["Người bàn giao", "Tài xế nhận", "Điều phối"])),
        new OfficeFormDefinition(
            "cod-handover-blank", "logistics", LogisticsGroup,
            "Biên bản bàn giao / thu tiền COD",
            "Biên bản trống giao nhận tiền COD giữa tài xế và bộ phận nhận tiền.",
            Pdf: new(["Ngày bàn giao", "Tài xế", "Chuyến giao", "Người nhận tiền"], CodHeaders, ["Tài xế", "Người nhận tiền", "Kế toán / Quản lý"])),
        new OfficeFormDefinition(
            "trip-reconciliation-blank", "logistics", LogisticsGroup,
            "Biên bản đối soát chuyến",
            "Đối chiếu kết quả giao hàng, hàng trả về và các khoản cần xử lý.",
            Pdf: new(["Ngày đối soát", "Chuyến giao", "Tài xế", "Người đối soát"], ["STT", "Phiếu giao", "Khách hàng", "Kết quả giao", "Hàng trả", "COD", "Ghi chú"], ["Điều phối", "Tài xế", "Kế toán / Kho"])),
        new OfficeFormDefinition(
            "cod-reconciliation-blank", "logistics", LogisticsGroup,
            "Biên bản đối soát COD",
            "Đối chiếu tiền COD phải thu, đã thu và chênh lệch.",
            Xlsx: new(CodHeaders),
            Pdf: new(["Từ ngày", "Đến ngày", "Tài xế / đơn vị", "Ngày đối soát"], CodHeaders, ["Người đối soát", "Người bàn giao", "Kế toán / Quản lý"])),

        new OfficeFormDefinition(
            "leave-request-blank", "workforce", WorkforceGroup,
            "Đơn xin nghỉ phép",
            "Đơn giấy để nhân sự đề nghị nghỉ và xin phê duyệt.",
            Pdf: PeoplePdf(["Họ và tên", "Mã nhân viên", "Phòng/Bộ phận", "Từ ngày", "Đến ngày", "Phần ngày", "Lý do nghỉ"], "Ghi rõ người bàn giao công việc khi cần.")),
        new OfficeFormDefinition(
            "overtime-request-blank", "workforce", WorkforceGroup,
            "Phiếu đăng ký tăng ca",
            "Phiếu giấy đăng ký thời gian và lý do tăng ca.",
            Pdf: PeoplePdf(["Họ và tên", "Mã nhân viên", "Phòng/Bộ phận", "Ngày tăng ca", "Từ giờ", "Đến giờ", "Lý do"])),
        new OfficeFormDefinition(
            "attendance-adjustment-blank", "workforce", WorkforceGroup,
            "Phiếu điều chỉnh chấm công",
            "Phiếu giấy đề nghị sửa giờ vào, giờ ra hoặc tình trạng công.",
            Pdf: PeoplePdf(["Họ và tên", "Mã nhân viên", "Ngày công", "Giờ đã ghi nhận", "Giờ đề nghị điều chỉnh", "Lý do"])),
        new OfficeFormDefinition(
            "blank-timesheet-month", "workforce", WorkforceGroup,
            "Bảng chấm công tháng trống",
            "Bảng Excel trống dùng ghi chấm công thủ công theo tháng.",
            Xlsx: new(["Ngày", "Thứ", "Giờ vào", "Giờ ra", "Ra ngoài", "Quay lại", "Tổng giờ", "Nghỉ phép", "Tăng ca", "Ghi chú"], 31)),
        new OfficeFormDefinition(
            "blank-work-schedule-month", "workforce", WorkforceGroup,
            "Lịch làm việc / phân ca tháng trống",
            "Bảng Excel trống để lập lịch và phân ca theo tháng.",
            Xlsx: new(["Ngày", "Thứ", "Mã nhân viên", "Họ và tên", "Ca làm việc", "Giờ bắt đầu", "Giờ kết thúc", "Nghỉ giữa ca", "Ghi chú"], 31)),
        new OfficeFormDefinition(
            "attendance-violation-explanation-blank", "workforce", WorkforceGroup,
            "Bản giải trình vi phạm chấm công",
            "Biểu mẫu giấy để nhân sự giải trình sai lệch chấm công.",
            Pdf: PeoplePdf(["Họ và tên", "Mã nhân viên", "Ngày vi phạm", "Nội dung ghi nhận", "Nội dung giải trình"], "Đính kèm chứng từ hoặc thông tin xác nhận nếu có.")),
        new OfficeFormDefinition(
            "attendance-violation-record-blank", "workforce", WorkforceGroup,
            "Biên bản xử lý vi phạm chấm công",
            "Biên bản giấy ghi nhận kết luận xử lý vi phạm chấm công.",
            Pdf: PeoplePdf(["Họ và tên", "Mã nhân viên", "Ngày vi phạm", "Nội dung vi phạm", "Giải trình", "Kết luận xử lý"])),
        new OfficeFormDefinition(
            "payroll-adjustments-blank", "workforce", WorkforceGroup,
            "Bảng kê khoản lương bổ sung / khấu trừ",
            "Bảng Excel trống tổng hợp khoản cộng thêm hoặc khấu trừ trước khi xử lý lương.",
            Xlsx: new(["STT", "Mã nhân viên", "Họ và tên", "Loại khoản", "Nội dung", "Số tiền", "Kỳ lương", "Ghi chú"]))
    ];

    private static OfficeFormPdfSpec ItemPdf(string[] meta, string[]? signatures = null) =>
        new(meta, ItemHeaders, signatures ?? ["Người lập", "Bộ phận liên quan", "Đối tác / Người nhận"]);

    private static OfficeFormPdfSpec StockPdf(string[] meta) =>
        new(meta, StockHeaders, ["Người lập", "Thủ kho", "Bộ phận liên quan"]);

    private static OfficeFormPdfSpec ReconcilePdf(string[] meta) =>
        new(meta, ReconciliationHeaders, ["Người lập", "Kế toán", "Đối tác xác nhận"]);

    private static OfficeFormPdfSpec PeoplePdf(string[] meta, string? note = null) =>
        new(meta, null, ["Người lập / Nhân sự", "Quản lý trực tiếp", "Bộ phận Nhân sự"], note);

    private static OfficeFormPdfSpec CashPdf(string[] meta, string receiverLabel) =>
        new(meta, null, ["Người lập", receiverLabel, "Kế toán / Quản lý"], "Ghi rõ chứng từ liên quan và phương thức thanh toán khi có.");
}
