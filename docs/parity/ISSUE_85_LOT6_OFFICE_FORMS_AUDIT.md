# Issue #85 — Lô 6: Audit thư viện 35 biểu mẫu văn phòng

## Baseline live trước khi code

- Desktop `main`: `3d8bfa6bba7496fbdd16c8eaa869fe9abe93a918`.
- Không có PR Desktop mở tại thời điểm audit.
- Push-CI gần nhất trên merge commit trên: run `36242577335`, kết luận `success`.
- Web `main`: `999f8eaac3d2c584016e9e41ca3a3d043affa3f2`.
- Source-of-truth: Desktop Issue #85, Web Issue #1190 và inventory comment #5835190808.

## Delta khóa cho Lô 6

Web hiện triển khai tab `Biểu mẫu văn phòng` tại Nhập/Xuất dữ liệu bằng catalog tĩnh:

- đúng 35 biểu mẫu, 5 nhóm;
- 13 đầu ra XLSX;
- 29 đầu ra PDF/In-lưu PDF;
- một số biểu mẫu có cả hai định dạng;
- không có CSV;
- không có Phiếu lương trống;
- có tìm kiếm và lọc nhóm;
- thư viện không gọi API lấy khách hàng, Nhà cung cấp, sản phẩm, tồn kho hay nhân sự chỉ để dựng form.

Desktop trước Lô 6 chưa có tab/catalog này. Desktop đã có nền XLSX và nền in/PDF, vì vậy Lô 6 chỉ nối parity vào các nền hiện hữu, không tạo API/backend/DB/migration.

## Permission / action

- Không tạo permission mới.
- Tab nằm trong workspace Nhập/Xuất dữ liệu và giữ quyền mở workspace hiện hữu.
- Tải XLSX và xem/in PDF là thao tác local, read-only, không mutation nên không dùng Idempotency-Key.
- Khi mở thẳng tab Biểu mẫu văn phòng, ViewModel không tải reference/master data nền.

## Regression

Phạm vi sửa không thay đổi các builder chứng từ thực tế Lô 3 hoặc các export Lô 2–5. Test Lô 6 khóa lại các marker regression tương ứng.
