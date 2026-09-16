# UI-2.5 — Audit Khách hàng theo Công Ty Web hiện hành

Ngày audit: 2026-09-15.

## Baseline

- Công Ty Web/backend: `binhnxwjfjxm/NPP-Platform main@3dafbaa564e2d9715ceb91bff8e35b5dbc344b2c`.
- Desktop: `binhnxwjfjxm/congty-desktop main@b54dd78bd23a9ee2b5acfa4a7495617306a439ce`.
- Main CI trước branch: #226 PASS.
- Branch: `agent/ui2-customers-parity`.
- Không có PR khác đang mở khi bắt đầu audit.

Nguồn Web đọc trực tiếp:
- `npp-core/web/app/customers/page.tsx`
- `npp-core/web/app/customers/customer-workspace.tsx`
- `npp-core/web/app/customers/customer-bulk-tabs-launcher.tsx`
- `npp-core/web/app/customers/customer-bulk-workspace.tsx`
- `npp-core/web/app/customers/customer-quick-setup-workspace.tsx`
- `npp-core/web/app/customers/[id]/page.tsx`
- `npp-core/web/app/customers/[id]/customer-detail-view.tsx`
- `npp-core/web/app/customers/[id]/customer-history-sections.tsx`
- `npp-core/web/app/customers/[id]/customer-delivery-returns-section.tsx`

Backend:
- `npp-core/api/src/routes/customers.js`
- `npp-core/api/src/routes/customers-existing.js`
- `npp-core/api/src/routes/customer-profile.js`
- `npp-core/api/src/services/customer.js`
- `npp-core/api/src/services/customer-profile.js`
- `npp-core/api/src/services/customer-profile-delivery-returns.js`

## Factual correction sau audit source

Ghi chú cũ trong Issue #31 nói màn danh mục Khách hàng có 2 tab. Source hiện hành đã đi xa hơn: launcher hiện chèn **Thiết lập nhanh / Nhập KH / Cập nhật KH** vào giữa Khách hàng và Nhóm khách hàng.

Do đó current Web thực tế có 5 tab danh mục:
`Khách hàng → Thiết lập nhanh → Nhập KH → Cập nhật KH → Nhóm khách hàng`.

Hồ sơ khách hàng là route riêng `/customers/[id]`, không phải tab thứ 6. Desktop cũ render thêm tab **Hồ sơ 360°**, nên lệch IA dù dữ liệu bên trong khá đầy đủ.

## Matrix danh mục

| Web | Desktop trước UI-2.5 | UI-2.5 |
| --- | --- | --- |
| Header kicker Quản lý khách hàng | Hệ thống Công Ty | Đúng copy Web |
| Subtitle canonical | Copy rút gọn | Đúng Web |
| Topbar Cập nhật + Thêm theo section | Refresh/Add nằm trong toolbar | Đưa lên shell; create ẩn ở quick/import/update |
| 3 summary cards | Có nhưng copy khác | Đúng copy Web |
| 4 filters | Có | Đúng label/placeholder/order |
| Bảng 7 cột có STT | Nhiều cột tách, thiếu STT | Đúng 7 cột |
| Mã/tên stacked + name mở detail | Không theo route Web | Có |
| Direct actions Sửa/Địa chỉ/status | Menu `Thao tác ▾` | Direct |
| Group table 6 cột | Có filter riêng + UpdatedAt + menu | Bỏ filter thừa, đúng 6 cột, direct actions |
| Empty state | Khác copy | Đúng copy |

## Matrix route chi tiết

| Web | UI-2.5 |
| --- | --- |
| Chi tiết khách hàng / Khách hàng | Shell đổi header theo detail state |
| Danh sách khách hàng / Sửa thông tin | Topbar exact actions |
| Hero code + status + name + meta + default address | Có |
| 6 detail tabs | Có đúng thứ tự |
| Overview period controls | Có |
| 5 summary: Doanh số / Số đơn / Lần mua / Công nợ / Hạn mức | Có |
| Purchased product name + SKU stacked | Có |
| Orders lifecycle history | Tái sử dụng canonical service |
| Finance permission-gated | Tái sử dụng canonical service |
| Delivery/returns permission-gated | Tái sử dụng canonical service |
| Info facts + addresses | Có |
| Photos | Chỉ ở Thiết lập nhanh; bỏ duplicate khỏi detail |

## Create/edit contract

Desktop trước UI-2.5 đã dùng canonical idempotency key, nhưng có một correctness gap:
- customer create thành công,
- default-address create lỗi,
- editor giữ mở,
- retry lại gọi create customer lần nữa.

Web canonical giữ `pendingCreatedCustomer` và retry đúng bước address. UI-2.5 sửa Desktop cùng logic:
- key tạo khách giữ cho operation ban đầu;
- sau khi customer đã tạo, lưu pending customer;
- address key giữ nguyên và được reuse;
- retry chỉ gọi create address;
- đóng editor trong trạng thái này báo rõ khách đã tạo nhưng địa chỉ chưa lưu;
- không phát sinh customer trùng.

Update customer/group/address tiếp tục dùng `expectedUpdatedAt`.
Customer/group code immutable theo backend.
Không sửa backend, DB hay migration.

## Alignment hard gate từ ảnh tham chiếu

Yêu cầu bổ sung: **không được còn bảng nào có text hoặc nút bị lệch lên/xuống trong hàng**.

Audit shared style cho thấy DataGridTextColumn đã có vertical center, nhưng nhiều `DataGridTemplateColumn` có root Button/Grid/ComboBox/TextBlock không khai báo alignment rõ, nên cell nhiều kiểu vẫn có thể nhìn lệch.

UI-2.5 khóa lại toàn app:
- `OfficeDataGridRowStyle.VerticalContentAlignment=Center`;
- `OfficeDataGridCellStyle` + `OfficeGridTextStyle` giữ center;
- `OfficeMiniButtonStyle` và `OfficeRowActionButtonStyle` center cả control/content;
- mọi root visual trong `DataGridTemplateColumn.CellTemplate` của Inventory, Organization, Partner, Sales có `VerticalAlignment="Center"`;
- regression test quét các bảng này, không chỉ màn Khách hàng.

Căn ngang nội dung vẫn bám Web (đa số text trái); yêu cầu “cân giữa hàng” được hiểu là giữa theo chiều dọc, không tự đổi table thành text-center ngang.

## Ranh giới

- Không tạo link giả tới Accounting/Logistics nếu screen Desktop đích chưa được mở trong master plan.
- UI-2.5 không sửa nghiệp vụ Nhà cung cấp ngoài shared alignment.
- Không deploy/migrate production.
