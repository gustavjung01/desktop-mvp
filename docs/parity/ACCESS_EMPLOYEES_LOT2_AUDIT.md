# Danh mục nhân sự — Lô 2 mutation audit

Nguồn đối chiếu:

- Công Ty backend: `npp-core/api/src/routes/employees.js`
- Công Ty service: `npp-core/api/src/services/employee.js`
- Web hiện hành: `/access/employees`
- Desktop Lô 1: PR #35, merge `845faed0e31aa16d8ea987466d4a8753591bd844`

## Contract mutation

- Tạo: `POST /api/employees`
  - bắt buộc `Idempotency-Key`
  - payload: `code, fullName, jobTitle, phone, email, branchId`
- Cập nhật: `PATCH /api/employees/:id`
  - payload: `fullName, jobTitle, phone, email, branchId, expectedUpdatedAt`
- Trạng thái: `PATCH /api/employees/:id`
  - payload: `isActive, expectedUpdatedAt`
- quyền: `core.employee.write`

## Kiểm soát Desktop

- mọi mutation lấy khóa từ `ICanonicalIdempotencyKeyProvider`; không tự ghép khóa gửi lên API.
- khóa canonical chỉ dùng `[A-Za-z0-9._-]` và cùng thao tác retry sẽ reuse đúng khóa đã tạo.
- `expectedUpdatedAt` lấy từ bản ghi đang hiển thị; `CONFLICT` khóa lưu tiếp và yêu cầu tải lại.
- thay đổi trạng thái có bước xác nhận đúng Web.
- lỗi hồ sơ Chủ sở hữu hệ thống được chuyển sang ngôn ngữ văn phòng.

## Sự thật backend hiện tại

Backend bắt buộc và replay Idempotency-Key cho POST. PATCH hiện dùng optimistic concurrency bằng `expectedUpdatedAt` và chưa lưu/replay Idempotency-Key. Desktop vẫn gửi canonical key cho PATCH để giữ một contract phía client; retry mơ hồ sau khi server đã ghi sẽ được chặn bởi conflict thay vì ghi đè lần hai.

Không sửa backend, database hoặc migration trong lô này.
