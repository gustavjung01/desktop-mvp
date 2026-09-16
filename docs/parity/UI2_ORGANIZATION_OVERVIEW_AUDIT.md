# UI-2.1 — audit Tổng quan cơ cấu Desktop theo Công Ty Web

Ngày audit: 2026-09-15.

## Baseline

- Công Ty Web/backend: `NPP-Platform/main@0d25ee474e1062fa279e3ef3f3469628c5a9dcbf`.
- Desktop trước thay đổi: `main@81b3b2886118776e4456c0f3870fbb28bc8793a7`.
- Nguồn Web đối chiếu trực tiếp:
  - `npp-core/web/app/organization/page.tsx`
  - `npp-core/web/app/organization/organization-workspace.tsx`
  - `npp-core/web/app/organization/organization.module.css`
- Backend canonical hiện có đủ dữ liệu qua danh sách chi nhánh, kho và vị trí. Không cần migration hay endpoint mới.

## Matrix 1:1

| Thứ tự Web | Công Ty Web | Desktop UI-2.1 |
| --- | --- | --- |
| Header | Kicker **Báo cáo quản trị**, title **Tổ chức**, subtitle mô tả cơ cấu; action **Cập nhật dữ liệu** | Giữ đúng kicker/title/subtitle và action ở header chính |
| 1 | 3 thẻ **Chi nhánh / Kho hàng / Vị trí kho**, tổng + hoạt động/ngừng hoạt động | 3 thẻ cùng thứ tự, bỏ thẻ Nhân sự khỏi khu vực này |
| 2 | **Danh mục nghiệp vụ → Truy cập nhanh**: Chi nhánh, Kho hàng, Vị trí kho | Cùng thứ tự; chuyển tới workspace hiện có, không tạo dữ liệu giả |
| 3 | **Cơ cấu vận hành → Cơ cấu chi nhánh và kho** | Card theo chi nhánh: mã, tên, trạng thái, địa chỉ, tổng kho, kho hoạt động, số vị trí |
| 4 | **Cập nhật gần đây → Những hồ sơ vừa thay đổi** | 8 hồ sơ mới nhất giữa chi nhánh/kho/vị trí; cột Mã, Tên, Đơn vị liên quan, Trạng thái, Cập nhật |
| State | loading/notice/error/empty | Thông báo dùng header chung; empty state riêng cho cơ cấu và danh sách gần đây; refresh theo trạng thái busy |
| Permission | dữ liệu theo quyền backend | Desktop tiếp tục deny-by-default, chỉ gọi endpoint người dùng được cấp quyền |

## Quyết định bố cục

- `Tổng quan cơ cấu` là một **màn sidebar độc lập**, không phải tab nghiệp vụ bên trong Chi nhánh/Kho.
- TabControl nội bộ cũ chỉ còn làm host kỹ thuật và **không render thanh tab top-level**, vì Web dùng sidebar route cho các màn Chi nhánh/Kho/Vị trí.
- Tab cục bộ của **Kho hàng** vẫn giữ riêng: Kho hàng / Thiết lập nhanh / Sơ đồ kho / Lịch sử.
- Quick link **Vị trí kho** tạm đi vào luồng quản lý vị trí hiện có; màn **Vị trí kho** sẽ được re-audit đầy đủ đúng thứ tự UI-2 sau Chi nhánh và Kho hàng. Không coi việc đi tới luồng cũ là hoàn tất màn Vị trí kho.

## Ranh giới

- Không đổi backend, DB, migration hoặc production.
- Không tự tính nguồn nghiệp vụ mới: tổng và cấu trúc chỉ là projection đọc từ ba danh mục canonical.
- Không làm trước màn Chi nhánh/Kho/Vị trí ngoài phần điều hướng cần thiết cho quick link của Tổng quan cơ cấu.
