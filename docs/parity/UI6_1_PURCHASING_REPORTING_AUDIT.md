# UI-6.1 — Audit Báo cáo mua hàng theo Công Ty Web

Ngày audit: 2026-09-16

## Baseline

- Master issue Desktop: #31.
- Desktop bắt đầu từ `main@24a6eed0b952ea0d3474a7057764198b740ec3b8`.
- PR #57 (UI-3.11) đã merge; exact-head Desktop CI #359: PASS.
- Không có PR Desktop mở và không thấy branch UI-4 tại thời điểm refresh trước khi tích hợp.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Web chuẩn: `/purchasing/reporting`.

## Nguồn sự thật

Web:
- `npp-core/web/app/purchasing/reporting/page.tsx`
- `npp-core/web/app/components/reporting-dashboard-workspace.tsx`
- `npp-core/web/lib/reporting-dashboard-types.ts`

Backend:
- `npp-core/api/src/routes/reporting-sales-purchasing.js`
- `npp-core/api/src/routes/reporting-purchasing.js`
- `npp-core/api/src/routes/reporting-common.js`

Desktop trước UI-6.1:
- chưa có module Mua hàng;
- mục Báo cáo mua hàng trong sidebar đang vô hiệu hóa;
- chưa có contract/service cho `GET /api/reporting/purchasing`.

## Matrix Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-6.1 |
| --- | --- | --- |
| 1 | Kicker Mua hàng | Kicker MUA HÀNG |
| 2 | Báo cáo mua hàng | Cùng tiêu đề và mô tả nghiệp vụ |
| 3 | Kỳ báo cáo | Từ ngày / Đến ngày / Đặt lại / Áp dụng |
| 4 | Mặc định tháng hiện tại | Để trống lần đầu, backend trả kỳ chuẩn rồi Desktop hiển thị lại |
| 5 | Tổng đơn mua trong kỳ | Card tổng số đơn |
| 6 | Đơn mua có hiệu lực | Card + danh sách trạng thái có hiệu lực |
| 7 | Đã hủy | Card riêng, không cộng vào giá trị hiệu lực |
| 8 | Chờ duyệt | Card riêng |
| 9 | Phiếu nhận đã ghi sổ | Card riêng |
| 10 | Phiếu nhận đã đảo | Card riêng |
| 11 | Giá trị theo tiền tệ | Bảng STT / Tiền tệ / Chứng từ hiệu lực / Giá trị |
| 12 | Trạng thái đơn mua | Danh sách trạng thái + số chứng từ |
| 13 | Phiếu nhận hàng | Danh sách trạng thái + số chứng từ |
| 14 | Xu hướng theo ngày | Bảng Ngày / Tiền tệ / Số chứng từ / Giá trị |
| 15 | Top nhà cung cấp | Bảng Nhà cung cấp / Tiền tệ / Chứng từ / Giá trị |
| 16 | Top SKU | Bảng SKU / SL cơ sở / Giá trị / Nguồn |
| 17 | Nguồn số liệu | Dòng giải thích bằng ngôn ngữ văn phòng |
| 18 | Mở đơn đặt hàng | Chưa tạo nút giả trong UI-6.1; nối điều hướng khi UI-6.2 có workspace thật |
| 19 | Mở phiếu nhận hàng | Chưa tạo nút giả trong UI-6.1; nối điều hướng khi UI-6.4 có workspace thật |
| 20 | Link từ nhà cung cấp/SKU đến đơn mua | Chưa tạo link chết; nối khi UI-6.2 có workspace thật |

Không thêm tab, export, kho lọc hoặc workflow ngoài Web.

## API và quyền

- Endpoint đọc: `GET /api/reporting/purchasing`.
- Query Web dùng: `from`, `to`.
- Backend còn hỗ trợ warehouse scope nhưng Web hiện không đưa bộ lọc kho ra màn hình, vì vậy Desktop UI-6.1 cũng không tự thêm.
- Permission: `core.reporting.purchasing.read`.
- Kỳ mặc định: từ ngày đầu tháng hiện tại đến ngày hiện tại theo `Asia/Ho_Chi_Minh`.
- Khoảng báo cáo tối đa: 366 ngày; backend giữ quyền kiểm tra canonical.
- Đây là màn chỉ đọc; không có mutation, idempotency, backend change, DB change hoặc migration.

## Contract báo cáo

- summary:
  - allOrderCount
  - effectiveOrderCount
  - cancelledOrderCount
  - pendingApprovalCount
  - postedReceiptCount
  - reversedReceiptCount
- currencyTotals
- statusBreakdown
- dailyTrend
- topEntities (nhà cung cấp)
- topSkus

Trạng thái đơn mua có hiệu lực theo backend:
`approved`, `partially_received`, `fully_received`, `closed`.

## Ngôn ngữ UI

Desktop không hiển thị tên bảng, schema, permission key, endpoint hoặc thuật ngữ dev. Các mô tả nguồn số liệu được đổi sang cách nói văn phòng như “ngày đặt hàng”, “ngày nhận hàng”, “không gộp các loại tiền tệ”.

## Đồng bộ sau UI-4.1

- UI-4.1 đã merge trước tại `main@c0898651732e3fd14c3af9c764b18fdfdf628d09`.
- UI-4.1 giữ workspace `19` cho **Hiệu suất giao hàng**.
- UI-6.1 dùng workspace `20` cho **Báo cáo mua hàng**.
- Các file dùng chung App/Shell được hợp nhất từ `main` mới, giữ nguyên cả UI-4.1 và UI-6.1; không ghi đè code logistics.
