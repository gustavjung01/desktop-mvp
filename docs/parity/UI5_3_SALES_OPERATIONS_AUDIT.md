# UI-5.3 — Điều hành bán hàng: audit parity

## Baseline

- Desktop: `gustavjung01/desktop-mvp`, branch `agent/ui5-3-sales-operations` từ `main@3b1d368746d883fc33ce3fc661ba5d274884c258`.
- Công Ty Web/backend: `binhnxwjfjxm/NPP-Platform main@b9d4158f5ad92688cc8df5c95363d0c50e8d7e31`.
- Web chuẩn: `/management`.
- Không thay backend, database, migration hoặc production deploy.
- PR #7 và #8 không bị sửa hoặc revert.

## Xác nhận phạm vi

Navigation Web hiện hành xếp nhóm Bán hàng theo thứ tự: Báo cáo bán hàng → Lãi gộp → Điều hành bán hàng → Đề xuất → Đơn bán hàng → Quản lý đơn hàng → Mở/liên kết mã khách.

Vì vậy UI-5.3 là `/management`. Báo cáo mua hàng và Báo cáo tồn kho không thuộc phạm vi này.

## Source of truth

- `npp-core/web/app/management/page.tsx`
- `npp-core/web/app/management/management.module.css`
- `npp-core/web/app/components/app-shell-core.tsx`
- `npp-core/web/lib/business-language.ts`
- `npp-core/web/lib/sales-order-gateway.ts`
- `npp-core/web/lib/customer-onboarding-gateway.ts`
- `npp-core/api/src/routes/sales-orders.js`
- `npp-core/api/src/routes/customer-onboarding.js`

## Backend contract

UI-5.3 chỉ đọc:

- `GET /api/sales-orders?status=draft&limit=20&offset=0`, quyền `core.sales-order.read`.
- `GET /api/customer-onboarding-requests?status={submitted|under_review|need_more_info}&limit=20&offset=0`, quyền `core.customer-onboarding.read`.
- Danh mục tổ chức dùng endpoint hiện hữu với quyền `core.branch.read`, `core.warehouse.read`, `core.warehouse.location.read`.

Không có mutation, vì vậy UI-5.3 không tạo Idempotency-Key.

## Web → Desktop

| Web `/management` | Desktop UI-5.3 |
| --- | --- |
| Kicker Điều hành bán hàng | Header shell Điều hành bán hàng |
| Tiếp nhận và xử lý nhu cầu bán hàng | Page title giữ nguyên |
| Notice trung tâm điều hành | Notice đầu workspace |
| 4 summary cards | Chi nhánh, kho, vị trí kho, việc đang chờ — cùng thứ tự |
| Partial organization failure | Card nguồn lỗi hiển thị “—”, nguồn còn lại giữ dữ liệu |
| Đơn chờ xác nhận | Queue trái, tối đa 20 đơn draft |
| Nguồn đơn | Nhân viên thị trường / Khách hàng / Công Ty |
| Đề nghị mở/liên kết mã khách | Queue phải, gom 3 trạng thái, sắp `updatedAt` giảm dần, tối đa 20 |
| Partial onboarding failure | Giữ kết quả nguồn thành công và báo lỗi một phần |
| Empty/error/loading | Có trạng thái tương ứng bằng ngôn ngữ văn phòng |
| Mở màn xác nhận | Điều hướng sang Đơn bán hàng hiện có |
| F5 | Cập nhật lại dữ liệu điều hành |

## Ranh giới UI-5.3

Web `/management` có action dẫn sang `/management/proposals` và `/management/customer-onboarding`. Đây là các workspace nghiệp vụ riêng sau UI-5.3 và Desktop hiện chưa có.

UI-5.3 vẫn giữ đúng vị trí và tên action “Gửi Đề xuất” / “Mở màn xử lý”, nhưng không dựng màn Đề xuất hoặc Mở/liên kết mã khách trong PR này. Hai action đó ở trạng thái chưa mở để không code chồng phạm vi lô sau.

Đơn bán hàng đã có workspace thật nên action “Xem đơn bán hàng” và “Mở màn xác nhận” đều điều hướng được.

## Quyền và làm mới dữ liệu

- Menu 5.3 chỉ hiện khi tài khoản có ít nhất một quyền đọc thuộc màn.
- Từng nguồn dữ liệu được kiểm quyền deny-by-default trước khi gọi.
- Mỗi lần mở UI-5.3 đều đọc mới thay vì dùng cache riêng của màn.
- Các nguồn tải song song; lỗi một nguồn không xóa dữ liệu nguồn còn lại.

## Ngôn ngữ văn phòng

UI không hiển thị UUID, permission key, đường dẫn kỹ thuật, tên bảng, “canonical”, “backend” hoặc “Phase”. Nguồn đơn mặc định được hiển thị là “Công Ty”.
