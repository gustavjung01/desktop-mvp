# Chuẩn bố cục Web ↔ Desktop Công Ty

## Mục tiêu

Công Ty Web là chuẩn bố cục nghiệp vụ. Công Ty Desktop dùng WPF native nhưng người dùng đã quen Web phải nhận ra ngay cùng một nghiệp vụ, cùng thứ tự thông tin và cùng luồng thao tác.

Parity UI/UX ở đây là **information architecture + business flow + relative layout + control order + action placement**. Desktop không cần tái tạo từng pixel của trình duyệt, nhưng người dùng đã học Web phải nhận ra ngay cùng bố cục và không phải học lại vị trí thao tác.

## Ba lớp parity bắt buộc

Parity Công Ty Web ↔ Desktop được kiểm theo ba lớp độc lập:

1. **Đủ tính năng**: mọi thao tác người dùng đang làm được trên Công Ty Web trong phạm vi màn phải được kiểm kê. Có API nhưng Desktop không có đường thao tác vẫn là thiếu tính năng.
2. **Đúng bố cục nghiệp vụ**: người dùng phải tìm đúng khu vực, đúng ngữ cảnh và đúng thứ tự luồng như Web; không dùng tên tab giống nhau để che một workspace thiếu nghiệp vụ.
3. **Tối ưu Desktop**: được nâng cấp typography, DataGrid, phím tắt, mật độ và dialog native nhưng không làm mất hoặc dời khuất nghiệp vụ Web.

Mỗi lô UI phải có feature matrix đối chiếu Web hiện hành. Gate là **0 tính năng bị bỏ sót không có giải thích**; test chỉ kiểm tên tab không đủ để kết luận parity.

## Quy tắc bắt buộc

Desktop phải giữ theo Web:

1. cùng tên khu vực, tab và thao tác nghiệp vụ;
2. cùng thứ tự các khu vực/tab chính;
3. cùng nhóm trường dữ liệu và ý nghĩa;
4. cùng bộ lọc/tìm kiếm chính;
5. cùng luồng danh sách → hồ sơ/chi tiết → thao tác;
6. cùng vị trí tương đối của thao tác chính so với dữ liệu mà thao tác đó tác động;
7. không gom các nghiệp vụ độc lập thành một màn nếu Web đã tách rõ;
8. không tách một hồ sơ nghiệp vụ thành các màn rời khiến người dùng mất ngữ cảnh.

Desktop được phép cải tiến:

- DataGrid native, virtualization và resize cột;
- mật độ dữ liệu phù hợp màn hình văn phòng;
- phím tắt, tab order, keyboard navigation;
- dialog native;
- thao tác hàng loạt;
- typography, spacing, theme sáng/tối;
- panel cố định hoặc resize chỉ khi Web có cấu trúc tương đương hoặc thay đổi đó không làm lệch hierarchy, thứ tự khối và vị trí tương đối của hành động; không tự dùng split view để thiết kế lại màn Web.

## Chuẩn card và mật độ toàn ứng dụng

Đây là **gate bắt buộc cho mọi PR UI**. Không coi là góp ý thẩm mỹ tùy chọn.

Mọi màn Desktop dùng cùng một hệ xám–trắng, không tự đặt màu nền card theo từng màn:

- **Shell sở hữu tiêu đề trang và mô tả trang.** Nếu Shell đã hiển thị title/subtitle thì workspace không được lặp lại cùng title/subtitle thêm lần nữa.
- **Card form, bộ lọc, tiêu đề khu vực và card nội dung** dùng nền trắng theo `CardBodyBrush`, viền xám nhẹ theo `CardFrameBrush`.
- Không dùng **card/dải xám full-width chỉ để chứa một câu giải thích**. Chữ giải thích thông thường phải là text gọn trên nền trang hoặc tooltip; chỉ dùng card khi khối đó thực sự là một vùng nội dung/tương tác độc lập.
- **Card thông tin tổng/KPI** dùng `OfficeSummaryCardStyle`: nền xám nhẹ theo `CardHeaderBrush`, tách độc lập khỏi `OfficeCardHeaderStyle`.
- Card KPI mặc định chỉ có **2 dòng: nhãn + giá trị chính**. Không có dòng mô tả thứ ba trong card. Giải thích dài đưa vào tooltip hoặc vùng mô tả bên ngoài.
- KPI dùng `OfficeSummaryLabelStyle` + `OfficeSummaryValueStyle`; không tự đặt `FontSize`, padding hoặc chiều cao riêng ở từng màn nếu không có lý do nghiệp vụ rõ ràng.
- Nhóm **4–6 KPI phải ưu tiên nằm trong một hàng gọn khi cửa sổ đủ rộng**; dùng `WrapPanel` và width hợp lý. Chỉ xuống hàng khi chiều ngang thực tế không đủ.
- Không dùng `UniformGrid` ép 2–3 cột khiến KPI thành 2 hàng cao khi dữ liệu chỉ là số tổng.
- Không đặt `Height` / `MinHeight` lớn cho KPI. Mật độ chuẩn là card thấp, tương đương các màn báo cáo tồn kho đã duyệt.
- Không khai báo màu xám/trắng riêng trong từng màn cho card thông thường. Nếu cần biến thể mới phải bổ sung vào theme chung và có test khóa quy tắc.
- Nút vẫn phải tách rõ khỏi card bằng `OfficePrimaryButtonStyle` / `OfficeSecondaryButtonStyle`; không dùng màu card để giả nút.

### Checklist review bắt buộc trước merge UI

1. Có title/subtitle bị lặp giữa Shell và workspace không?
2. Có dải xám full-width nào chỉ chứa text giải thích không?
3. KPI có đúng 2 dòng, dùng shared summary styles và không tự đặt chiều cao lớn không?
4. Với 4–6 KPI, khi đủ chiều ngang chúng có nằm thành một hàng gọn thay vì bị ép thành hai hàng không?
5. Card form/filter/content có giữ nền trắng; KPI mới dùng xám nhẹ không?
6. Nếu phá một trong các quy tắc trên, PR phải ghi rõ lý do nghiệp vụ và test cho ngoại lệ đó.

## Contract đã khóa cho dữ liệu nền

### Khách hàng

Màn danh mục giữ hai khu vực chính của Web:

1. Khách hàng
2. Nhóm khách hàng

Từ một khách hàng, thao tác **Hồ sơ 360°** mở cùng ngữ cảnh khách hàng với đúng 6 khu vực:

1. Tổng quan
2. Hàng đã mua
3. Đơn hàng
4. Công nợ & thanh toán
5. Giao hàng / Trả hàng
6. Thông tin & địa chỉ

Ảnh khách hàng là tiện ích Desktop bổ sung và nằm trong **Thông tin & địa chỉ**, không tạo một luồng nghiệp vụ rời.

Nhập/cập nhật hàng loạt là công cụ dữ liệu của danh mục khách hàng, không thay thế hồ sơ 360°.

### Kho hàng

Khu vực Kho hàng giữ đúng 4 tab Web:

1. Kho hàng
2. Thiết lập nhanh
3. Sơ đồ kho
4. Lịch sử

Định nghĩa:

- **Kho hàng**: hồ sơ kho thuộc chi nhánh.
- **Sơ đồ kho**: cách chia khu/kệ/điểm chứa hàng *bên trong* một kho.
- **Khu vực/vị trí**: bản ghi nằm trong sơ đồ kho; không phải địa chỉ vật lý của kho.
- **Lịch sử**: lịch sử thay đổi cách quản lý/sơ đồ kho.

Không dùng lại bố cục mơ hồ kiểu “Kho & vị trí” / “Vị trí kho” làm người dùng hiểu vị trí là một nghiệp vụ ngang hàng với sơ đồ.

### Nhà cung cấp

Danh mục chính giữ bố cục Web: số liệu tổng quan → tìm kiếm/lọc → danh sách → thêm/sửa/trạng thái.

Thông tin giao dịch bổ sung của Desktop (liên hệ, địa chỉ, điều khoản thanh toán) phải mở từ đúng nhà cung cấp đã chọn, không thay đổi luồng danh mục chính.

Khi tạo mới:
- Khách hàng: tạo hồ sơ và địa chỉ mặc định trong cùng luồng như Web.
- Nhà cung cấp: có thể lưu địa chỉ mặc định trong cùng luồng khi người dùng nhập đủ thông tin.

## Gate review

Một PR thay đổi UI nghiệp vụ không được coi là đạt parity nếu:

- thiếu tab/khu vực bắt buộc đang có trên Web;
- đổi thứ tự khiến luồng khác Web mà không có quyết định thiết kế chung;
- chỉ có thẻ tổng hợp nhưng thiếu bảng/lịch sử nghiệp vụ phía sau;
- cùng thuật ngữ nhưng mang ý nghĩa khác;
- chuyển nghiệp vụ bắt buộc sang một nơi khó phát hiện;
- Desktop tự tính business data thay vì đọc canonical backend contract.

Review phải đối chiếu Web hiện hành, backend API và permission trước khi merge.
