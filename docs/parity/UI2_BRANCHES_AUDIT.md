# UI-2.2 — audit Chi nhánh Desktop theo Công Ty Web

Ngày audit: 2026-09-15.

## Baseline

- Công Ty Web/backend đã đọc lại trên `NPP-Platform/main@3dafbaa564e2d9715ceb91bff8e35b5dbc344b2c`.
- Desktop bắt đầu từ `main@dbe451787973452f65605cc77c07b58c09684846` sau khi UI-2.1 merge.
- Nguồn Web đối chiếu trực tiếp:
  - `npp-core/web/app/organization/branches/page.tsx`
  - `npp-core/web/app/organization/organization-workspace.tsx`
  - `npp-core/web/app/organization/organization.module.css`
  - `npp-core/api/src/routes/organization.js`
  - `npp-core/api/src/services/branch.js`
- Backend canonical hiện có đủ list/create/update/status; không cần endpoint, DB hoặc migration mới.

## Matrix UI/UX và nghiệp vụ

| Thứ tự | Công Ty Web | Desktop UI-2.2 |
| --- | --- | --- |
| Header | Kicker **Danh mục tổ chức và kho**, title **Chi nhánh**, subtitle mô tả vận hành/hạch toán/báo cáo | Giữ đúng nội dung và vị trí header |
| Header actions | **Cập nhật dữ liệu** / **Thêm chi nhánh** | Có đủ hai action ở header chính |
| Tổng quan | 3 thẻ **Chi nhánh / Kho hàng / Vị trí kho** | Tái sử dụng đúng 3 thẻ đã khóa ở UI-2.1 |
| Bộ lọc | **Tra cứu theo mã hoặc tên** + placeholder **Nhập mã hoặc tên…**; trạng thái **Tất cả / Đang hoạt động / Ngừng hoạt động** | Cùng nhãn, thứ tự và filter |
| Toolbar actions | **Cập nhật dữ liệu** / **Thêm chi nhánh** | Cùng thứ tự như Web |
| Section | **Danh mục quản lý → Chi nhánh** + số hồ sơ đang hiển thị | Có header section + bộ đếm sau filter |
| Bảng | **Mã / Tên / Liên hệ / Trạng thái / Cập nhật / Xử lý** | Cùng thứ tự cột |
| Tên | Tên chính + địa chỉ dòng phụ | Cùng cấu trúc |
| Liên hệ | Điện thoại + email | Cùng cấu trúc, có fallback văn phòng |
| Xử lý | **Chỉnh sửa** + **Ngừng sử dụng/Đưa vào sử dụng** hiển thị trực tiếp | Bỏ menu `Thao tác ▾` riêng của Chi nhánh |
| Editor | **Thêm mới/Chỉnh sửa → Chi nhánh**; Mã, Tên, Địa chỉ, Số điện thoại, Email; Hủy + Tạo/Lưu | Modal riêng của Chi nhánh, không thay layout Kho/Vị trí/Nhân sự |
| Status confirm | **Xác nhận trạng thái**; title theo trạng thái đích; Hủy + Xác nhận | Modal trong app; không dùng MessageBox generic |
| Empty/loading | Skeleton/empty ở Web | Desktop hiển thị **Đang tải dữ liệu…** khi chưa có hàng và đang bận, sau đó **Không tìm thấy chi nhánh phù hợp.** |
| Error | banner Web giữ modal khi mutation lỗi | Desktop dùng notice header chung và giữ modal xác nhận khi lỗi |
| Permission | backend deny-by-default | Giữ permission hiện hữu, không tạo action giả |

## Điểm contract cần giữ

- Tạo chi nhánh dùng shared canonical Idempotency-Key; retry cùng thao tác reuse key đã tạo cho editor.
- PATCH update/status dùng `expectedUpdatedAt`.
- Mã chi nhánh là immutable theo backend: service update luôn giữ `existing.code`. Vì vậy Desktop giữ ô Mã **chỉ đọc khi chỉnh sửa** thay vì cho nhập rồi bỏ qua giá trị. Đây là khác biệt hành vi có chủ đích để bám backend contract, không phải tự đổi nghiệp vụ.
- Khi ngừng chi nhánh còn kho hoạt động, backend trả dependency conflict; Desktop hiển thị message canonical và giữ dialog để người dùng xử lý/thử lại.

## Ranh giới chạy song song

- UI-3 đang được xử lý ở luồng khác. UI-2.2 chỉ sửa Organization và ba điểm header tối thiểu cho route Chi nhánh.
- Không sửa InventoryView/InventoryViewModel hoặc contract của UI-3.
- Trước merge phải compare với `main` mới nhất; nếu UI-3 đã merge thì chỉ rebase/merge-safe phần không xung đột, không ghi đè code của luồng kia.
