# UI-3.4 — Kiểm kê kho: Audit parity Web mới

## Baseline đã audit

- Desktop source of change: `gustavjung01/desktop-mvp`, baseline `090dce4b0109ee1e54fbc520970fbcc2a6e9d68c`.
- Web/API source of truth: `binhnxwjfjxm/NPP-Platform`, baseline `ebbe90ce4559f27e501ad74c48346f09e3e4dcee`.
- Web: `npp-core/web/app/inventory/stocktakes/stocktake-workspace.tsx`, `stocktake-workspace.module.css`, `StocktakePrintDock.tsx`, `npp-core/web/lib/stocktake-types.ts`.
- API: `npp-core/api/src/routes/inventory-stocktakes.js`, `npp-core/api/src/services/inventory-stocktake.js`, `npp-core/api/src/db/repositories/inventory-stocktake.js`.
- Migration liên quan: `database/migrations/inventory/131_warehouse_location_management_mode.sql`, `136_inventory_tracking_policy_backfill.sql`, `138_inventory_stocktake_line_details.sql`, `139_inventory_stocktake_line_annotation.sql`.
- Migration mới nhất của baseline này là 139. Desktop không tạo migration, không sửa backend/DB.

## Kết luận audit cũ

Quy tắc “1–500 exact scope” và việc Desktop biến nhóm lô/vị trí thành `scopes[]` exact đã lỗi thời.

Contract chính của UI hiện là phạm vi ở mức ý định:

- `scopeMode = all`: gửi kho + mode; backend snapshot tồn hợp lệ hiện tại.
- `scopeMode = lot`: gửi `lotSelections[] = { baseVariantId, lotId }`.
- `scopeMode = location`: gửi `locationIds[]`.
- Legacy `exact` vẫn tồn tại trong backend để tương thích, nhưng không phải contract chính của màn Desktop.
- Desktop không hard-code cap 500 và không tự chia phiếu theo số balance. Số dòng snapshot và mọi giới hạn server là authority của backend hiện hành.

## Authority kho, lô và lịch sử

Migration 131 thêm `location_management_mode = MANAGED | UNMANAGED | NULL`. Backfill chỉ suy ra khi dữ liệu legacy cho kết quả đơn nghĩa; kho rỗng hoặc dữ liệu hỗn hợp vẫn để `NULL`. Vì vậy Desktop tuyệt đối không tự chọn MANAGED/UNMANAGED. Khi backend trả `WAREHOUSE_LOCATION_MODE_REQUIRED`, UI phải nói rõ kho cần được cấu hình trên hệ thống.

Migration 136 backfill canonical lot/expiry tracking policy mà không sửa số lượng tồn, lot hay movement. Desktop dùng contract/balance đã canonical, không suy luận policy khác.

Migration 138 thêm `count_reason` tối đa 500 ký tự và `count_note` tối đa 2000 ký tự trên từng dòng; snapshot của vòng đếm bất biến và lịch sử đã khóa không được xóa/sửa tùy ý.

Migration 139 chỉ cho phép write context annotation sửa `reason/note` của vòng hiện tại khi header ở `submitted` hoặc `approved`; số đếm, actor/time đếm, snapshot, final delta và posting data không được thay đổi.

## API và permission

Đọc màn dùng `core.stocktake.read`.

Mutation:

- `POST /api/inventory/stocktakes` — `core.stocktake.create`.
- `POST /api/inventory/stocktakes/{id}/count` — `core.stocktake.count`.
- `POST /api/inventory/stocktakes/{id}/annotate` — `core.stocktake.count`.
- `POST /api/inventory/stocktakes/{id}/copy` — `core.stocktake.create`.
- `POST /api/inventory/stocktakes/{id}/submit` — `core.stocktake.submit`.
- `POST /api/inventory/stocktakes/{id}/recount` — `core.stocktake.approve`.
- `POST /api/inventory/stocktakes/{id}/approve` — `core.stocktake.approve`.
- `POST /api/inventory/stocktakes/{id}/post` — `core.stocktake.post`.
- `POST /api/inventory/stocktakes/{id}/cancel` — `core.stocktake.cancel`.
- `POST /api/inventory/stocktakes/{id}/reverse` — `core.stocktake.reverse`.

Mọi mutation Desktop dùng `ICanonicalIdempotencyKeyProvider`. Cùng intent + revision/fingerprint trong cùng phiên phải reuse đúng key; không tự ghép dữ liệu nghiệp vụ thành raw Idempotency-Key.

## Create flow Desktop

Panel tạo phiếu dùng 3 lựa chọn trực tiếp: **Toàn bộ sản phẩm trong kho / Theo lô / Theo vị trí**.

- `all` không render hàng trăm checkbox.
- `lot/location` có tìm kiếm, chọn tất cả kết quả đang hiển thị, bỏ chọn kết quả, tổng số mục đã chọn và tóm tắt/chip lựa chọn.
- Picker chỉ render tối đa 60 kết quả một lần để giữ UI nhẹ; đây là giới hạn trình bày, không phải cap nghiệp vụ.
- Lô hiển thị sản phẩm, SKU, mã lô, HSD.
- Vị trí hiển thị mã và tên vị trí.
- Request create gửi selector canonical; backend snapshot tồn hiện tại.

## Dòng kiểm kê và blind count

Trong `draft/recount_required`:

- Cho nhập **Số thực đếm / Lý do / Ghi chú**.
- Không bind hoặc render tồn hệ thống, chênh lệch hay count status.
- File đếm cũng không chứa tồn hệ thống.
- Lý do tối đa 500, ghi chú tối đa 2000.

Sau khi backend reveal:

- Bảng có **Tồn hệ thống / Thực đếm / Chênh lệch / Trạng thái / Lý do / Ghi chú**.
- `submitted/approved` + `core.stocktake.count` cho sửa riêng reason/note và gọi `annotate`.
- Trạng thái khác reason/note chỉ đọc.

Filter dòng: **Tất cả / Chưa kiểm / Khớp / Lệch**, có count từng nhóm. Khớp/Lệch bị disable trong blind count. Paging cố định 100 dòng/trang với Trước/Sau và x/y.

## File flow tại phiếu

Toolbar detail có:

- `Xuất file phiếu`.
- `Nhập file` chỉ trong bước blind count và khi có quyền count.
- `Sao chép phiếu` khi có quyền create.
- `Kết quả Excel / Kết quả CSV` chỉ sau reveal.
- `In` không hiện ở draft.
- Trạng thái đặt cạnh nhóm tool; dùng style Office Desktop hiện có.

File đếm có đúng các cột:

`Phiếu kiểm kê, SKU, Tên sản phẩm, ĐVT, Mã lô, Mã vị trí, Số đếm thực tế, Lý do, Ghi chú`.

Import map theo phiếu + SKU + lô + vị trí. Nếu thiếu lô/vị trí mà cùng SKU tạo nhiều candidate thì báo ambiguity; một dòng phiếu không được map lặp trong file. Import chỉ cập nhật buffer count/reason/note, không tự submit/post.

Kết quả sau reveal có:

`SKU, Tên sản phẩm, ĐVT, Mã lô, Mã vị trí, Tồn hệ thống, Thực đếm, Chênh lệch, Lý do, Ghi chú`.

CSV prefix dấu nháy đơn trước giá trị bắt đầu bằng `=`, `+`, `@` hoặc dấu `-` không phải số âm, rồi quote/escape toàn bộ cell; file có UTF-8 BOM.

## Copy

`Sao chép phiếu` gửi `expectedRevision` tới endpoint `copy`. Backend tạo phiếu mới và snapshot tồn hiện tại. Desktop không clone expected quantity/snapshot cũ ở client; response mới được mở/chọn ngay.

## Danh sách và error UX

Danh sách hiển thị thông tin tạo và, nếu có `currentCountedAt/currentCountedBy`, thêm `Kiểm: <người> · <thời gian>`.

Lỗi scope/location dùng message backend/canonical; riêng `WAREHOUSE_LOCATION_MODE_REQUIRED` phải nói rõ kho chưa thiết lập chế độ quản lý vị trí và Desktop không tự chọn mode.

## Regression checklist

- [x] create all/lot/location dùng intent selector.
- [x] bỏ cap 500/exact-scope client behavior cũ.
- [x] blind count không render expected/delta/status.
- [x] count gửi reason/note và validate 500/2000.
- [x] annotate submitted/approved, permission count.
- [x] copy expectedRevision, permission create, mở response mới.
- [x] canonical idempotency reuse theo intent/revision/fingerprint.
- [x] filter + count + paging 100.
- [x] import xlsx/csv map phiếu/SKU/lô/vị trí, chặn ambiguity/duplicate.
- [x] export file đếm không có tồn hệ thống.
- [x] Excel/CSV kết quả chỉ sau reveal; CSV chống formula injection.
- [x] permission visibility cho count/annotate/copy/import/export.
- [x] error `WAREHOUSE_LOCATION_MODE_REQUIRED` rõ nghĩa.
- [x] In không hiện ở draft.
- [x] list có current counted metadata.
- [x] không sửa Web/backend/DB/migration và không production deploy.
