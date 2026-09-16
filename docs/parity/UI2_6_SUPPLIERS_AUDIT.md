# UI-2.6 — Audit Nhà cung cấp theo Công Ty Web

Ngày chốt audit: 2026-09-15.

## Baseline
- Desktop: `main@fd0dbffc5775962bc542568f9904c8ad18cffa1a`.
- Công Ty Web: `NPP-Platform/main@9e5ec08793bc482f1179173276dbe44372526643`.
- Web chuẩn: `npp-core/web/app/suppliers/supplier-workspace.tsx`.
- Backend canonical giữ nguyên; không DB/migration/deploy.

## Kết quả audit ban đầu
Desktop cũ chưa đạt:
- Có 3 thẻ tổng thay vì 2 số tổng hợp + nút thêm.
- Bảng thiếu STT, thừa cột Phụ trách mua.
- Action bị gom menu thay vì Sửa + đổi trạng thái trực tiếp.
- Tự thêm tab Hồ sơ nhà cung cấp với Liên hệ / Địa chỉ / Điều khoản thanh toán, không tồn tại trong Web hiện hành.
- Form tạo thừa Nhân viên phụ trách mua, Loại địa chỉ, Mã bưu chính, Quốc gia.
- Địa chỉ hành chính dùng text tự do thay vì luồng Tỉnh/thành phố → Xã/phường/đặc khu.

## Matrix sau sửa
1. Tổng nhà cung cấp.
2. Đang hoạt động.
3. Thêm nhà cung cấp.
4. Tìm kiếm: mã / tên / mã số thuế / ngân hàng.
5. Trạng thái: Tất cả / Đang hoạt động / Ngừng sử dụng.
6. Cập nhật dữ liệu.
7. Bảng: STT → Mã → Tên → Mã số thuế → Ngân hàng → Giao hàng → Trạng thái → Thao tác.
8. Thao tác dòng: Sửa; Ngừng sử dụng / Đưa vào sử dụng.
9. Empty state: Không có nhà cung cấp phù hợp.
10. Modal tạo/sửa: Mã, Tên, MST, Số tài khoản, Tên ngân hàng, Thời gian giao hàng trung bình.
11. Tạo mới thêm Địa chỉ chi tiết + Tỉnh/thành phố + Xã/phường/đặc khu.
12. Tỉnh/thành và xã/phường lấy từ canonical `/api/reference/vietnam-administrative-units`.

## Mutation
- Tạo nhà cung cấp và địa chỉ mặc định vẫn dùng shared canonical Idempotency-Key.
- Retry cùng intent giữ key cũ theo PartnerViewModel hiện hành.
- Sửa/trạng thái dùng optimistic version `expectedUpdatedAt` đúng backend.

## Boundary
- Không expose các workflow Liên hệ / Địa chỉ / Điều khoản thanh toán trong UI-2.6 vì Công Ty Web hiện hành không expose chúng tại màn Nhà cung cấp.
- Không sửa backend/DB.
