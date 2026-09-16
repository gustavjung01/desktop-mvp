# UI-3.11 — Audit Thiết lập tồn đầu kỳ theo Công Ty Web

Ngày audit: 2026-09-16

## Baseline

- Master issue Desktop: #31.
- Desktop bắt đầu từ `main@e1a29f123c46e611c775db1b070795b5a42984a8`.
- Main CI #349: PASS.
- Không có PR Desktop mở tại thời điểm bắt đầu.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Web chuẩn:
  - `npp-core/web/app/inventory/opening-balances/page.tsx`
  - `npp-core/web/app/inventory/opening-balances/opening-balance-csv-workspace.tsx`
  - `npp-core/web/lib/opening-balance-operator-gateway.ts`
  - `npp-core/web/lib/inventory-types.ts`
- Backend chuẩn:
  - `npp-core/api/src/routes/opening-balance-operator.js`
  - `npp-core/api/src/routes/inventory-core.js`
  - `npp-core/api/src/services/opening-balance.js`

## Kết luận Desktop trước UI-3.11

Sidebar có **Thiết lập tồn đầu kỳ** nhưng đang bị vô hiệu hóa. Desktop chưa có workspace, contract, service, CSV operator, preview/validation hoặc mutation cho tồn đầu kỳ.

UI-3.11 phải dùng flow operator hiện hành của Web; không dùng JSON thô và không nhập ID hệ thống.

## Matrix Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-3.11 |
| --- | --- | --- |
| 1 | App title | Thiết lập tồn đầu kỳ |
| 2 | Header | Kicker TỒN KHO / Nhập tồn đầu kỳ / mô tả nghiệp vụ |
| 3 | Back action | Về tra cứu tồn kho |
| 4 | Bước 1 | Tải mẫu Excel/CSV |
| 5 | Bước 2 | Chọn tệp CSV đã điền |
| 6 | Bước 3 | Kiểm tra tệp |
| 7 | Bước 4 | Xác nhận nhập tồn |
| 8 | Thông tin đợt nhập | Kho / Vị trí mặc định / Mã đợt dữ liệu / Ngày ghi nhận / Tệp đã chọn |
| 9 | Default location | Dòng CSV có Vị trí ưu tiên vị trí của dòng |
| 10 | CSV template | SKU / Số lượng / Vị trí |
| 11 | CSV input | Chấp nhận header tiếng Việt hoặc key canonical; dấu phân cách comma/semicolon/tab |
| 12 | Preview | Dòng / Kho / Vị trí / SKU / Tên hàng / Số lượng / Chính sách / Lô hàng / Hạn dùng / Trạng thái |
| 13 | Preview paging | 100 dòng/trang, Trang trước / Trang sau |
| 14 | Policy resolve | Sau validate tự hiện quản lý lô, hạn dùng và yêu cầu vị trí |
| 15 | Lot correction | Chỉ nhập mã lô khi policy REQUIRED |
| 16 | Expiry correction | Chỉ nhập hạn khi policy OPTIONAL/REQUIRED |
| 17 | Edit after validate | Bất kỳ thay đổi draft nào đều hủy kết quả validate hiện tại |
| 18 | Result | Danh sách lỗi hoặc rowCount/source total/base total/kho |
| 19 | History | STT / Mã đợt / Tệp nguồn / Số dòng / Thời gian |
| 20 | Success | Clear draft + mã đợt, giữ kho đang chọn |
| 21 | Keyboard Desktop | F5 làm mới; Ctrl+O chọn CSV |

## API và permission

### Operator

Permission bắt buộc: `core.inventory.opening-balance.import`.

- `GET /api/inventory/opening-balances/operator/warehouses`
- `GET /api/inventory/opening-balances/operator/locations?warehouseId=...`
- `POST /api/inventory/opening-balances/operator/validate`
- `POST /api/inventory/opening-balances/operator/post`

### History

Permission: `core.inventory.read`.

- `GET /api/inventory/opening-balances?limit=200`

Nếu tài khoản có quyền nhập nhưng không có quyền đọc tồn kho, workspace vẫn dùng operator được nhưng lịch sử không hiển thị.

## Payload operator

Top-level:
- `warehouseId`
- `sourceKey` — uppercase khi gửi
- `sourceFilename`
- `documentDate` — YYYY-MM-DD
- `metadata.importMethod = csv-upload-operator`
- `metadata.originalFilename`
- `metadata.defaultLocationCode`
- `rows[]`
- `contentChecksum`

Mỗi dòng:
- SKU
- sourceQuantity
- locationCode
- lotCode
- manufacturedDate
- expiryDate
- supplierLotReference
- sourceLineReference
- metadata

Desktop tính SHA-256 trên canonical JSON của draft trước khi thêm `contentChecksum`. Nếu draft thay đổi sau lần kiểm tra thì không cho post cho tới khi kiểm tra lại.

## Idempotency

Mutation duy nhất là **Xác nhận nhập tồn**.

- dùng shared `ICanonicalIdempotencyKeyProvider`;
- scope `inventory-opening-balance-post`;
- fingerprint retry là `contentChecksum`;
- cùng thao tác + cùng payload lỗi rồi retry phải reuse đúng key cũ;
- thay đổi kho, vị trí, mã đợt, ngày, tệp, mã lô hoặc hạn dùng thì intent đổi và key cũ bị hủy;
- key chỉ dùng contract `[A-Za-z0-9._-]`; không tự ghép key từ sourceKey/checksum.

## Boundary

- Đây là flow khởi tạo/chuyển dữ liệu đầu kỳ, không phải nhập kho thủ công hoặc kiểm kê.
- Không ghi trực tiếp balance.
- Không backend/DB/migration/deploy.
- Không tự thêm create/edit/delete lịch sử.
- UI không hiện UUID, permission key, checksum hay Idempotency-Key.
- Dữ liệu lô/hạn dùng được backend kiểm theo chính sách SKU canonical.
