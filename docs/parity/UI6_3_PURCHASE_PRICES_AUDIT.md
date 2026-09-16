# UI-6.3 — Audit Bảng giá mua

Ngày audit: 2026-09-16

## Baseline

- Desktop: `main@17c4c20d42d620a4589f62a74fb7062b62d3cd39`.
- Không có PR Desktop mở khi bắt đầu.
- Công Ty Web/backend: `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Web chuẩn: `/purchasing/purchase-prices`.
- Backend hiện có đủ contract; không cần backend, DB hoặc migration mới.

## Nguồn sự thật

- Web: `PurchasePriceWorkspace.tsx`
- Types: `purchase-order-types.ts`
- Backend: `supplier-purchase-prices.js`, `supplier-purchase-price.js`
- Decision: `phase-5-7-supplier-purchase-pricing-decisions.md`

## Matrix Web → Desktop

| Web | Desktop UI-6.3 |
| --- | --- |
| Tổng dòng giá | KPI gọn |
| Đang hiệu lực quản trị | KPI gọn |
| Nhà cung cấp có giá | KPI gọn |
| Lọc theo nhà cung cấp | ComboBox lọc |
| Cập nhật dữ liệu | Nút secondary |
| Thêm giá mua | Editor thêm |
| Danh sách giá | DataGrid |
| Sửa | Editor sửa |
| Nhà cung cấp | ComboBox active supplier |
| Tìm SKU mua hàng | Dùng canonical `/api/purchase-orders/sku-search` |
| Giá mua | Decimal > 0 |
| Tiền tệ | 3 chữ cái |
| Số lượng tối thiểu | Decimal >= 0 |
| Hiệu lực từ/đến | Date range |
| Mã SKU NCC | max 128 |
| Tham chiếu thỏa thuận | max 256 |
| Ghi chú | max 2000 |
| Đang sử dụng | bool |

## API và quyền

- `GET /api/supplier-purchase-prices?limit=1000&offset=0` — `core.supplier-purchase-price.read`
- `POST /api/supplier-purchase-prices` — `core.supplier-purchase-price.manage`, bắt buộc Idempotency-Key
- `PATCH /api/supplier-purchase-prices/:id` — `core.supplier-purchase-price.manage`, bắt buộc `expectedRevision`
- SKU search dùng contract mua hàng hiện có.

Create dùng shared `CanonicalIdempotencyKeyProvider`; retry cùng logical attempt/payload reuse key. Không tự ghép key.
Update không gửi idempotency key vì backend PATCH contract không dùng idempotency; concurrency khóa bằng `expectedRevision`.

## Quy tắc nghiệp vụ

- Giá mua thuộc purchasing, không lấy Sales Pricing làm fallback.
- Business key gồm installation + supplier + variant + unit + currency + min quantity + effective from.
- Giá phải > 0, tối đa 6 chữ số thập phân.
- Số lượng tối thiểu >= 0.
- Hiệu lực đến không được trước hiệu lực từ.
- Deactivate bằng `isActive=false`; không có DELETE route.
