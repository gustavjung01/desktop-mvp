# UI-3.4 — Audit Kiểm kê kho theo Công Ty Web

Ngày chốt audit: 2026-09-15

## Baseline

- Master issue Desktop: #31.
- Desktop bắt đầu UI-3.4 trên `main@fd0dbffc5775962bc542568f9904c8ad18cffa1a`, sau khi UI-3.3 đã merge.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@ccd0458b46ca8f3eaf1b9af9582da737d83ffd78`.
- Web chuẩn: `npp-core/web/app/inventory/stocktakes/stocktake-workspace.tsx`.
- Backend chuẩn: `npp-core/api/src/routes/inventory-stocktakes.js`, `npp-core/api/src/services/inventory-stocktake.js`.
- Desktop trước UI-3.4 chưa có màn Kiểm kê kho; submenu đang bị vô hiệu hóa.
- Không sửa backend, DB, migration hoặc production deploy.

## Matrix Công Ty Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-3.4 |
| --- | --- | --- |
| 1 | Header Kiểm kê kho + Tạo đợt kiểm kê | Topbar Desktop giữ cùng hành động chính, có thêm Làm mới |
| 2 | Tạo đợt kiểm kê | Panel native mở/đóng từ topbar |
| 3 | Chọn kho | Kho đang hoạt động từ canonical API |
| 4 | Phạm vi: toàn bộ / theo lô / theo vị trí | Đủ 3 lựa chọn |
| 5 | Chọn nhóm lô/vị trí | Checkbox native; khi gửi chuyển về exact scope |
| 6 | Ghi chú + Tạo và bắt đầu đếm | Đủ |
| 7 | Tìm kiếm + trạng thái | Đúng thứ tự |
| 8 | Danh sách đợt kiểm kê | Số phiếu, kho, trạng thái, lần đếm, số phạm vi, thời gian |
| 9 | Chi tiết + In phiếu | Đủ; dùng PrintDialog native |
| 10 | Đếm mù | Đang đếm không hiển thị tồn hệ thống/chênh lệch |
| 11 | Hoàn tất đếm thực tế | Phải nhập đủ mọi dòng |
| 12 | Gửi duyệt | Đúng lifecycle |
| 13 | Yêu cầu đếm lại | Lý do bắt buộc |
| 14 | Duyệt kết quả | Đúng permission; backend chặn tự duyệt |
| 15 | Cập nhật tồn kho | Chỉ sau approved |
| 16 | Hủy kiểm kê | Chỉ trước submit theo backend |
| 17 | Hoàn tác cập nhật tồn | Chỉ posted và còn đủ điều kiện backend |
| 18 | Lịch sử các lần đếm | Đủ vòng đếm, người thực hiện, thời gian, lý do |
| 19 | Metadata gửi/duyệt/cập nhật | Đủ |
| 20 | Loading / partial error / disabled | Có access lifecycle + partial-load notice |

## Backend contract

### Đọc
- `GET /api/inventory/stocktakes?limit=500&offset=0`
- `GET /api/inventory/stocktakes/{id}`
- Tồn dùng canonical `/api/inventory/balances`.
- Kho dùng canonical `/api/warehouses`.

### Mutation
- `POST /api/inventory/stocktakes` — `core.stocktake.create`
- `POST /api/inventory/stocktakes/{id}/count` — `core.stocktake.count`
- `POST /api/inventory/stocktakes/{id}/submit` — `core.stocktake.submit`
- `POST /api/inventory/stocktakes/{id}/recount` — `core.stocktake.approve`
- `POST /api/inventory/stocktakes/{id}/approve` — `core.stocktake.approve`
- `POST /api/inventory/stocktakes/{id}/post` — `core.stocktake.post`
- `POST /api/inventory/stocktakes/{id}/cancel` — `core.stocktake.cancel`
- `POST /api/inventory/stocktakes/{id}/reverse` — `core.stocktake.reverse`

Đọc màn dùng `core.stocktake.read`.

Desktop dùng `ICanonicalIdempotencyKeyProvider` chung. Retry cùng intent/fingerprint trong cùng phiên reuse đúng key cũ. Header Idempotency-Key do generator chuẩn tạo, không tự ghép dữ liệu nghiệp vụ thành key.

## Quy tắc nghiệp vụ giữ nguyên

- Mỗi đợt kiểm kê có 1 kho và 1–500 exact scope.
- Khi đang đếm, Desktop không hiển thị tồn hệ thống hoặc chênh lệch.
- Mọi scope trong vòng hiện tại phải được nhập số thực đếm đúng một lần.
- Gửi duyệt và duyệt kiểm tra revision/scope watermark ở backend.
- Người gửi không được tự duyệt chính version đã gửi.
- Chỉ trạng thái approved mới được cập nhật tồn kho.
- Cập nhật tồn kho tạo movement append-only; không sửa balance trực tiếp.
- Hoàn tác chỉ khi không có movement downstream trên exact scope.
- Không đưa movement ID, scope version hoặc thuật ngữ kỹ thuật ra giao diện.

## Keyboard Desktop

- `F5`: làm mới.
- `Ctrl+F`: focus tìm kiếm.
- `Esc`: đóng panel tạo đợt kiểm kê.

## Boundary

- Chỉ UI-3.4 Kiểm kê kho.
- Không làm UI-3.5 Điều chỉnh và xử lý tồn.
- Shared Shell chỉ thay phần cần thiết để bật submenu/host/topbar thật của Kiểm kê kho.
- Không sửa backend/DB/migration/deploy.
