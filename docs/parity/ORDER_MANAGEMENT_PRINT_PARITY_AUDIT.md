# Order management cards + sales-order print parity audit

Baseline Desktop: `1b1b4cd6041598301d9fa1843f3b0c35ba439d05`.

## 1. Card tổng Quản lý đơn

Web tính card trên `baseFilteredOrders`: áp dụng tìm kiếm, ngày giờ, thanh toán, luồng giao, nguồn đơn; riêng stage chỉ dùng để lọc bảng. Tổng tiền dùng `activeVersion(order)?.total ?? order.total ?? '0'`.

Desktop cũ:
- card đếm/tổng trực tiếp trên toàn bộ `_orders`, nên không phản ánh các filter còn lại;
- tổng tiền chỉ đọc `ActiveVersion(order)?.Total`; list API không bắt buộc trả `versions`, vì vậy card có số đơn nhưng tổng thành `0 ₫`.

Sửa:
- thêm `_summaryOrders` tương đương `baseFilteredOrders`;
- card count/value tính từ tập này;
- tổng tiền fallback về `SalesOrderData.Total`.

## 2. Preview Phiếu xuất kho

Web `SalesOrderPrintSheet` + `BusinessDocumentPrint` dùng:
- A4, lề hẹp;
- title/header có đường phân cách;
- meta 2 cột, field full-width khi cần;
- bảng tự chia tỷ lệ cột, tên sản phẩm rộng, số tiền căn phải;
- tiền trong dòng hàng không lặp ký hiệu ₫; tổng cuối có ₫;
- tổng cộng nằm khối bên phải;
- footer khách hàng + ngày đơn.

Desktop cũ dùng FlowDocument nhưng các cột bảng có độ rộng bằng nhau, khiến tên sản phẩm bị bó hẹp, 8 dòng đã có thể tràn sang trang 2; meta là các paragraph xếp dọc và tổng chỉ là paragraph text.

Sửa trong `SalesOrderPrintPreview`:
- giữ canonical print template/field visibility;
- preview và print dùng cùng lề hẹp;
- meta table 2 cột;
- cột hàng hóa dùng tỷ lệ star theo nghiệp vụ;
- header bảng nền xám nhẹ, căn số/căn giữa đúng loại cột;
- tổng tiền thành block bên phải với rule trên TỔNG CỘNG;
- thêm footer khách/ngày;
- preview zoom mặc định 95%.

Không sửa Web/backend/DB/API contract.


## Hotfix sau kiểm tra ảnh thực tế

Ảnh preview thực tế sau lần parity đầu cho thấy meta 4 cột của FlowDocument bị co hai cột value xuống vài pixel khi page viewer scale trang, làm `NHÀ ĐẬU` và `24/09/2026` rơi từng ký tự theo chiều dọc.

Hotfix:
- meta đổi sang table 2 cột 50/50; mỗi cell chứa cả label + value;
- field full-width dùng `ColumnSpan=2`;
- nền trang/body đặt trắng thay vì transparent;
- footer được đẩy sát đáy hơn cho đơn ngắn;
- không đổi card tổng, API hay dữ liệu.
