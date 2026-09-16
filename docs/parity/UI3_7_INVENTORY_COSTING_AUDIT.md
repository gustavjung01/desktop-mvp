# UI-3.7 — Audit Giá vốn tồn kho theo Công Ty Web

Ngày chốt audit: 2026-09-15

## Baseline

- Master issue Desktop: #31.
- Desktop bắt đầu UI-3.7 từ `main@5c65a52fc5d3909728fd2ff0f7cd666dee01a079`.
- UI-3.6 đã nằm trên `main`; sau đó PR #48 và #49 cũng đã merge, nên UI-3.7 lấy **main mới nhất** làm chuẩn và giữ nguyên Product / Nhập kho thủ công / Shell của các lô song song.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@a25c3db2655c79468e287f3261b5435133ff2a29`.
- Web chuẩn:
  - `npp-core/web/app/inventory/costing/page.tsx`
  - `npp-core/web/app/inventory/costing/workspace.tsx`
  - `npp-core/web/lib/inventory-costing-types.ts`
  - `npp-core/web/app/api/inventory/costing/[[...segments]]/route.ts`
- Backend chuẩn:
  - `npp-core/api/src/routes/inventory-costing.js`
  - `npp-core/api/src/routes/inventory-costing-periods.js`
- Desktop trước UI-3.7 chỉ có submenu **Giá vốn tồn kho** bị vô hiệu hóa, chưa có workspace nghiệp vụ.
- Không sửa backend, DB, migration, provider hoặc production deploy.

## Thứ tự và bố cục Công Ty Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-3.7 |
| --- | --- | --- |
| 1 | Header Giá vốn tồn kho | Shell hiện hành, cùng tên và nhóm Tồn kho và lô hàng |
| 2 | Dựng lại giá vốn | Nút chính ở topbar, đúng vị trí tương đối của action Web |
| 3 | 4 số tóm tắt | Nhóm giá vốn / Đã tính giá / Thiếu nguồn giá / Chờ đối soát |
| 4 | Thẻ phương pháp + kỳ/run | Bình quân gia quyền di động; kỳ đang mở; lần tổng hợp gần nhất |
| 5 | Tab Giá trị tồn | Bảng Kho / SKU / Số lượng / Giá trị tồn / Giá bình quân / Trạng thái |
| 6 | Tab Kỳ giá vốn | Card mở/khóa kỳ + bảng lịch sử kỳ |
| 7 | Tab Đối soát | Bảng Sổ kho / Giá vốn / Chênh lệch / Kết quả |
| 8 | Tab Chờ xử lý | Danh sách chênh lệch đang mở/đã xử lý |
| 9 | Tab Điều chỉnh giá | Bảng ngày, kho/SKU, loại, số lượng, giá trị, nguồn |
| 10 | Tab Bất thường | Danh sách lỗi nguồn giá |
| 11 | Tab Dữ liệu giá vốn | Bảng thời điểm, kho/SKU, loại, SL, đơn giá, giá trị, nguồn |

Thứ tự tab khóa theo Web:
1. Giá trị tồn
2. Kỳ giá vốn
3. Đối soát
4. Chờ xử lý
5. Điều chỉnh giá
6. Bất thường
7. Dữ liệu giá vốn

Desktop bổ sung F5 để làm mới và Ctrl+1…Ctrl+7 để chuyển tab, không đổi flow Web.

## Backend contract thật

### Đọc dữ liệu

- `GET /api/inventory/costing/balances`
- `GET /api/inventory/costing/facts?limit=100`
- `GET /api/inventory/costing/anomalies?limit=100`
- `GET /api/inventory/costing/reconciliation?limit=500`
- `GET /api/inventory/costing/run`
- `GET /api/inventory/costing/periods`
- `GET /api/inventory/costing/adjustments`
- `GET /api/inventory/costing/discrepancies`

### Mutation hiện có trên Web

- `POST /api/inventory/costing/rebuild` — dựng lại phần giá vốn còn mở.
- `POST /api/inventory/costing/periods/open` — mở kỳ.
- `POST /api/inventory/costing/periods/close` — khóa kỳ sau đối soát.

Backend còn có `POST /api/inventory/costing/adjustments`, nhưng **Web hiện hành không có form tạo điều chỉnh giá trong workspace này**. Desktop không tự phát minh form/action mới.

### Permission

- `core.inventory-cost.read`
- `core.inventory-cost.reconcile`
- `core.inventory-cost.rebuild`

Dữ liệu đọc thường dùng quyền `read`. Đối soát + chờ xử lý dùng `reconcile`. Dựng lại và quản lý kỳ dùng `rebuild`.

## Idempotency

Mọi mutation dùng `ICanonicalIdempotencyKeyProvider` chung:

- rebuild: scope `inventory-costing-rebuild`;
- mở kỳ: scope `inventory-costing-period-open`;
- khóa kỳ: scope `inventory-costing-period-close`.

Nếu cùng thao tác bị lỗi mạng/API và người dùng thử lại mà payload chưa đổi, Desktop **reuse đúng key cũ**. Chỉ cấp key mới sau khi thao tác thành công hoặc thao tác/payload đổi.

## Ngôn ngữ văn phòng

Web hiện còn một số nhãn kỹ thuật như `Cost pool`, `Anomaly`, `MWA_V1` và mã event/source. Desktop giữ đúng ý nghĩa nghiệp vụ nhưng hiển thị:

- Cost pool → **Nhóm giá vốn**
- Anomaly → **Thiếu nguồn giá**
- MWA_V1 → **Bình quân gia quyền di động**
- event/source code → nhãn nghiệp vụ tiếng Việt khi có mapping
- không đưa rebuild ID, movement UUID hoặc thuật ngữ kỹ thuật lên màn hình

Trạng thái dùng **màu trên text**, không bọc pill/badge màu.

## Boundary

- Chỉ UI-3.7 — Giá vốn tồn kho.
- Không làm UI-3.8 — Tra cứu tồn kho.
- Shared Shell chỉ thay phần bật submenu, topbar action và host thật.
- Không sửa backend, DB, migration hoặc deploy production.
