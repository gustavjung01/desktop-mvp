# Workforce Desktop — Lô 3 ca mẫu, lịch tuần và ngày nghỉ

Ngày audit: 2026-09-22.

## Baseline và nguồn chuẩn

- Desktop base: `gustavjung01/desktop-mvp@48076e3d9a81bb4da44ac85972cdaba7d68b6f4b`.
- Web/backend authority khi bắt đầu code: `binhnxwjfjxm/NPP-Platform@9cd5ed9c52932d3b078647e8e754b927af0bd27c`.
- Kế hoạch: Issue #1140, **Lô 3 — Ca mẫu, lịch tuần, ngày lễ**.
- Web implementation Lô 3: PR #1146, merge commit `3bf4bac7071f2a4f1be3e296050b300cb2b4a288`.
- Migration 151 và backend runtime đã tồn tại phía NPP-Platform; Desktop không tạo migration riêng.

## Contract canonical

Desktop chỉ tiêu thụ contract hiện có:

- `GET /api/workforce/schedules` — xem lịch theo `from/to`, tùy chọn `employeeId`;
- `POST /api/workforce/schedules` — xếp/điều chỉnh lịch cá nhân;
- `GET /api/workforce/schedule-planning` — catalog ca mẫu, lịch tuần, ngày lễ/ngày nghỉ;
- `POST /api/workforce/schedule-planning` — mutation planning;
- `GET /api/workforce/policies` — policy dùng khi xếp lịch cá nhân;
- danh sách nhân sự dùng API employees hiện hành.

Permission canonical:

- `core.work-schedule.read` cho GET;
- `core.work-schedule.manage` cho POST.

Backend tiếp tục là authority của scope, policy hiệu lực, optimistic concurrency và audit/outbox.

## Lịch cá nhân

Payload canonical gồm:

- `employeeId`, `workPolicyId`, `workDate`;
- `scheduleKind = WORK | OFF`;
- `scheduledStartAt / scheduledEndAt`;
- `overrideReason`;
- `expectedUpdatedAt` khi cập nhật.

Desktop khóa UX giống Web:

- hôm nay và quá khứ chỉ xem;
- điều chỉnh bắt đầu từ ngày mai;
- lý do bắt buộc, tối đa 512 ký tự;
- tra cứu tối đa 94 ngày;
- update dùng `expectedUpdatedAt`, backend xử lý `SCHEDULE_CONFLICT`.

## Ca mẫu và lịch tuần

Planning actions:

- `SAVE_SHIFT_TEMPLATE`;
- `SAVE_WEEK_TEMPLATE`;
- `SAVE_CALENDAR_DAY`;
- `APPLY_WEEK_TEMPLATE`;
- `COPY_SCHEDULE`.

Ca mẫu:

- mã in hoa, tối đa 64;
- tên tối đa 256;
- giờ bắt đầu/kết thúc hợp lệ và không bằng nhau;
- nghỉ giữa ca từ 0 đến 720 phút;
- có trạng thái đang sử dụng/ngừng dùng.

Lịch tuần:

- đúng 7 ngày;
- mỗi ngày là WORK hoặc OFF;
- ngày WORK phải dùng ca mẫu đang hoạt động.

## Ngày lễ/ngày nghỉ và xếp hàng loạt

Ngày nghỉ Công Ty chỉ cấu hình cho ngày tương lai.
Khi xếp hàng loạt, ngày nghỉ Công Ty là lớp ưu tiên; khi ngừng áp dụng, lịch nền bên dưới tiếp tục là nguồn dữ liệu.

Xếp từ lịch tuần:

- chọn từ **1 đến 500 nhân sự**;
- khoảng đích bắt đầu từ ngày mai;
- tối đa **93 ngày**;
- lý do bắt buộc;
- thiếu policy hiệu lực thì backend chặn.

Sao chép lịch:

- lịch nguồn tối đa **31 ngày**;
- lịch đích bắt đầu từ ngày mai;
- lý do bắt buộc.

Cả APPLY và COPY đều **giữ nguyên ngoại lệ cá nhân** đã điều chỉnh theo người/ngày; kết quả backend trả `skippedOverrides` để Desktop hiển thị rõ số dòng được giữ.

## Idempotency

Mọi mutation schedules/planning dùng canonical `Idempotency-Key`.
Desktop tạo attempt slot từ toàn bộ logical payload; retry cùng payload reuse đúng key cũ.
Key chỉ bị loại khỏi slot sau khi mutation thành công.

## Phạm vi Desktop

Desktop dựng một workspace **Ca / lịch làm việc** phù hợp app văn phòng:

- bảng lịch theo ngày + filter nhân sự/từ ngày/đến ngày;
- modal xếp/điều chỉnh lịch cá nhân;
- bốn tab nội bộ: Ca mẫu, Lịch tuần, Ngày lễ & ngày nghỉ, Xếp hàng loạt;
- loading / empty / error state;
- không tạo sidebar lớn mới.

## Không làm

- không sửa Web/backend/DB/migration;
- không tự viết lại scope/policy resolver;
- không kéo Chấm công, Bảng công hay Chính sách làm việc sang Lô 3 này;
- không deploy production.
