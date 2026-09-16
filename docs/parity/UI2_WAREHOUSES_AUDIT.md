# UI-2.3 — audit Kho hàng Desktop theo Công Ty Web

Ngày audit: 2026-09-15.

## Kết luận trước khi sửa

Nhận định “Kho hàng hình như đã làm rồi” là **đúng một phần**: Desktop đã có sẵn gần đủ nghiệp vụ nền từ lô tổ chức cũ, gồm 4 tab **Kho hàng / Thiết lập nhanh / Sơ đồ kho / Lịch sử**, tạo kho, sửa kho, quản lý khu vực, đổi trạng thái, preview/convert chế độ sơ đồ và xem lịch sử.

Tuy nhiên code cũ **chưa đạt gate UI-2.3** vì bố cục/action/field vẫn lệch Web hiện hành khá rõ:
- tab Kho hàng dùng menu `Thao tác ▾` thay vì 3 action trực tiếp;
- toolbar dùng “Tìm kiếm kho”, “Cập nhật”, “Thêm kho” thay vì **Tra cứu kho / Tạo kho nhanh**;
- thiếu section header + bộ đếm và cấu trúc tên + thời gian cập nhật;
- nhãn cột chưa đúng Web;
- Thiết lập nhanh chia field khác thứ tự, dùng checkbox và có nút “Làm lại” mà Web không có;
- Sơ đồ kho có thêm filter và cột Kho/Chi nhánh/Cập nhật không thuộc local workflow Web;
- Lịch sử thiếu hai panel **Lịch sử sơ đồ kho / Chi tiết lần thay đổi** đúng cấu trúc Web;
- đổi trạng thái Kho/Khu vực và xác nhận convert còn dùng MessageBox native ngoài flow modal của Web.

Vì vậy UI-2.3 **không viết lại nghiệp vụ**; tái sử dụng backend/service/viewmodel đã đúng và chỉ tái cấu trúc phần UI/flow cần thiết.

## Baseline

- Công Ty Web/backend: `binhnxwjfjxm/NPP-Platform main@3dafbaa564e2d9715ceb91bff8e35b5dbc344b2c`.
- Desktop base: `binhnxwjfjxm/congty-desktop main@3acd7799ac15be929cb6fecc426bc51126c655b6`.
- Nguồn Web đọc trực tiếp:
  - `npp-core/web/app/organization/warehouses/page.tsx`
  - `npp-core/web/app/organization/warehouses/warehouse-tabs.tsx`
  - `npp-core/web/app/organization/warehouses/warehouse-workspace.tsx`
  - `npp-core/web/app/organization/warehouses/location-mode-history/page.tsx`
  - `npp-core/web/app/organization/warehouses/warehouse-workspace.module.css`
- Backend contract đọc trực tiếp:
  - `npp-core/api/src/routes/organization.js`
  - `npp-core/api/src/services/warehouse.js`
  - `npp-core/api/src/services/location.js`
  - `npp-core/api/src/routes/warehouse-location-mode.js`

Không cần backend mới, DB hay migration.

## Matrix Web → Desktop UI-2.3

| Phần | Công Ty Web hiện hành | Desktop UI-2.3 |
| --- | --- | --- |
| Header | Kicker **Danh mục quản lý**, title **Kho hàng**, subtitle “Quản lý kho, thiết lập nhanh và sơ đồ hàng hóa bên trong từng kho.” | Dùng đúng nội dung; header action **Cập nhật dữ liệu** |
| Local tabs | **Kho hàng / Thiết lập nhanh / Sơ đồ kho / Lịch sử** | Giữ đúng 4 tab và đúng thứ tự |
| Kho hàng — filter | **Tra cứu kho**, placeholder **Tên kho, mã kho hoặc chi nhánh**, trạng thái **Tất cả / Đang hoạt động / Ngừng hoạt động** | Cùng nhãn/placeholder/options |
| Kho hàng — action | **Tạo kho nhanh** → tab Thiết lập nhanh | Cùng flow; bỏ “Thêm kho” modal khỏi đường chính |
| Kho hàng — section | **Danh mục quản lý → Kho hàng** + `N kho` | Có section header + count sau filter |
| Kho hàng — cột | **Mã / Tên / Thuộc chi nhánh / Loại kho / Sơ đồ kho / Xuất vượt tồn / Trạng thái / Xử lý** | Cùng thứ tự và tên cột |
| Kho hàng — Tên | Tên + thời gian cập nhật dòng phụ | Cùng cấu trúc |
| Kho hàng — Xử lý | **Chỉnh sửa / Quản lý sơ đồ / Ngừng sử dụng hoặc Đưa vào sử dụng** | Action trực tiếp, bỏ dropdown |
| Thiết lập nhanh | Branch + Loại kho; Mã + Tên; Xuất vượt tồn dạng select; chỉ nút **Tạo kho** | Cùng thứ tự và control; bỏ checkbox/Làm lại |
| Thiết lập nhanh — copy | Ví dụ mã, tên gợi ý, help chính sách | Cùng text Web |
| Sơ đồ kho — header | **Bố trí hàng hóa → Sơ đồ kho** | Cùng hierarchy |
| Sơ đồ kho — toolbar | Chọn Kho + **Thiết lập sơ đồ / Thêm khu vực** | Cùng thứ tự |
| Sơ đồ kho — summary | **Kho / Chế độ sơ đồ / Khu vực** + help | Cùng 3 khối |
| Sơ đồ kho — bảng | **Mã khu vực / Tên khu vực / Loại khu vực / Trạng thái / Xử lý** | Cùng 5 cột, action trực tiếp |
| Sơ đồ kho — empty | “Chọn kho để xem sơ đồ.” hoặc “Kho này chưa có khu vực trong sơ đồ.” | Cùng state |
| Editor kho | **Chỉnh sửa → Kho hàng**; Chi nhánh, Mã, Tên, Loại, Xuất vượt tồn | Cùng field/order. Chi nhánh + Mã khóa khi edit theo backend canonical |
| Editor khu vực | **Thêm mới/Chỉnh sửa → Khu vực trong kho**; Kho, Mã, Tên, Loại | Cùng field/order. Kho khóa và Mã khóa khi edit theo backend canonical |
| Status confirm | Modal **Xác nhận trạng thái** | Modal trong app, không MessageBox |
| Thiết lập sơ đồ | Modal chọn mode, destination nếu MANAGED, preview, blockers, **Xem trước thay đổi / Xác nhận thay đổi** | Cùng flow; bỏ MessageBox xác nhận lần hai |
| Lịch sử — selector | Kho đang hoạt động + **Xem lịch sử** | Chỉ active warehouses |
| Lịch sử — trái | **Lịch sử sơ đồ kho**; Thời gian, Thay đổi + người thực hiện, Trước → Sau, SKU, Xem chi tiết | Cùng cấu trúc |
| Lịch sử — phải | **Chi tiết lần thay đổi** + 2 summary banner + lines SKU/Lô/Từ/Đến/SL | Cùng cấu trúc + empty state |

## Contract/logic giữ nguyên

### Warehouse
- Create dùng canonical Idempotency-Key.
- Retry cùng thao tác tạo nhanh reuse cùng key cho tới khi thành công/reset.
- PATCH edit/status dùng `expectedUpdatedAt`.
- Backend update Warehouse chỉ persist **name / warehouseType / allowNegativeStock**; `branchId` và `code` không được đổi. Desktop vì vậy giữ hai field này để người dùng nhận biết hồ sơ nhưng **khóa khi chỉnh sửa**, tránh field giả cho nhập rồi backend bỏ qua.

### Warehouse location
- Create dùng canonical Idempotency-Key.
- PATCH edit/status dùng `expectedUpdatedAt`.
- Backend update location chỉ persist **name / locationType**; `warehouseId` và `code` immutable. Desktop khóa Kho và khóa Mã lúc edit.

### Location mode
- Preview/convert yêu cầu `core.warehouse.write` + warehouse scope.
- Convert dùng canonical Idempotency-Key và reuse đúng key cho cùng preview.
- Lịch sử cũng yêu cầu warehouse write theo backend hiện hành; tab History tiếp tục deny-by-default theo contract.

## Ranh giới với UI-2.4

Sidebar **Vị trí kho** là màn độc lập và sẽ được audit ở UI-2.4. Trong UI-2.3 chỉ hoàn thiện **Sơ đồ kho là local workflow của Kho hàng**.

Để không làm mất chức năng route cũ trong lúc UI-2.4 chưa được làm, Desktop vẫn giữ một lớp filter legacy chỉ hiển thị khi vào route Vị trí kho; lớp này **ẩn hoàn toàn khi vào Kho hàng → Sơ đồ kho** nên không làm lệch UI-2.3.

## Chạy song song UI-3

- UI-3 dùng branch `agent/ui3-inventory-reporting-parity`.
- UI-2.3 chỉ sửa Organization + ba điểm header route Kho hàng + tests/docs.
- Không sửa `InventoryView`, `InventoryViewModel`, inventory contracts/service.
- Trước merge phải compare lại `main`, PR/branch UI-3; nếu UI-3 đã vào main thì giữ code của UI-3 và chỉ resolve phần header nếu thật sự chạm cùng dòng.
