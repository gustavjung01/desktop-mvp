# Workforce Desktop — Chấm công tay và phiếu nghỉ giấy

Ngày audit/triển khai: 2026-09-25.

## Baseline

- Web/API authority: `binhnxwjfjxm/NPP-Platform@aa310c87658da48915ac7cab9ab70b90dbde87bc`.
- Web commit: **feat(workforce): hoàn thiện chấm công tay và phiếu nghỉ giấy (#1175)**.
- Desktop base: `gustavjung01/desktop-mvp@c6a47410a3efbdd4b7b35554c8747d089bfe1bee`.
- Desktop base CI: run `36080465290` — success.
- Parent scope: Workforce Issues #1110 / #1140.

## Web/API delta

1. Điều chỉnh công:
   - quản lý có thể chọn nhân sự và **Chấm vào ngay / Chấm ra ngay**;
   - dùng `POST /api/workforce/attendance/adjustments/direct`;
   - payload mới có `recordNowAction = CHECK_IN | CHECK_OUT`;
   - thời điểm được lấy từ máy chủ, Desktop không tự gửi giờ hiện tại.

2. Nghỉ và đơn nghỉ:
   - capability mới `canSubmitManual`;
   - response có danh sách `employees`;
   - `LeaveRequest` bổ sung `request_source`, `manual_approver_name`, `attachment_url`;
   - ghi phiếu giấy bằng `POST /api/workforce/leave/requests/manual`;
   - phiếu đã duyệt cần người duyệt + ngày duyệt.

3. Chứng từ phiếu nghỉ:
   - upload binary bằng `PUT /api/workforce/leave/attachments`;
   - bắt buộc `Idempotency-Key` + `x-file-name`;
   - JPG/JPEG, PNG, WebP hoặc PDF;
   - tối đa 10 MB;
   - response trả `objectKey` dùng làm `attachmentReference` và `publicUrl` để mở chứng từ.

## Desktop implementation

- Điều chỉnh công:
  - thêm khối **Chấm công tay theo giờ hệ thống** ngay trong panel quản lý;
  - chọn nhân sự dùng catalog đang có;
  - payload quick action không truyền giờ, backend là authority thời gian;
  - retry cùng logical payload reuse đúng canonical Idempotency-Key.

- Nghỉ:
  - thêm form **Ghi nhận phiếu nghỉ giấy**;
  - nhân sự lấy từ chính response Leave để giữ scope backend;
  - hỗ trợ trạng thái phiếu giấy đã duyệt, người duyệt, ngày duyệt;
  - upload chứng từ thật và giữ object đã upload nếu submit phiếu thất bại để retry không upload lại;
  - danh sách hiển thị nguồn phiếu, người duyệt trên giấy và action mở chứng từ.

## Idempotency

- Quick attendance: scope `desktop-attendance-manual-now`.
- Upload chứng từ: scope `desktop-leave-document-upload`.
- Ghi phiếu giấy: scope `desktop-leave-request-manual`.
- Mỗi key được cache theo fingerprint logical payload; retry cùng thao tác reuse key cũ, chỉ bỏ key sau mutation tương ứng thành công.

## Boundary

Không sửa Web/backend/DB/migration. Migration `159_workforce_manual_attendance_leave` thuộc backend/deploy và không được Desktop tự thực hiện.
Không thay đổi permission contract hiện hành.
Không deploy production trong lô Desktop này.


## Parity gate rebaseline

Update #1175 làm Web tree và Workforce route surface thay đổi có chủ đích. Sau khi audit đúng hai Next runtime routes và hai Core mutation candidates mới, Desktop rebaseline parity inventory tại cùng Web SHA `aa310c87658da48915ac7cab9ab70b90dbde87bc`:

- screen surfaces: 83 — không đổi;
- Web routes: 333;
- backend API source files: 92 — không đổi;
- endpoint candidates: 438;
- permissions: 233 — không đổi;
- mutation candidates: 324.
- `/api/workforce/leave/requests/manual` và `/api/workforce/leave/attachments` đã có typed Desktop mapping trong lô này; Next runtime routes tương ứng vẫn `not_applicable` cho Desktop vì Desktop gọi Core API trực tiếp.
