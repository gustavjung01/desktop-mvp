# Workforce Desktop — Lô 10 Chấm công

Ngày audit: 2026-09-24.

## Baseline

- Desktop base: gustavjung01/desktop-mvp@7efc7f66f04d545913cbb2d4965872600d5fdc34.
- Desktop main push CI: run 35965901170 — success.
- Web/API authority: binhnxwjfjxm/NPP-Platform@af4acc24bc411d3206d07256f6119472068c01c3.
- Parent nghiệp vụ: Issue Web #1110.
- Trước branch không có PR Desktop đang mở.

## Web surface đã audit

Màn `/workforce/attendance` hiện có hai surface nghiệp vụ:

1. Nhân viên:
   - GET `/api/workforce/attendance/today`;
   - POST `/api/workforce/attendance/record`;
   - trạng thái NOT_STARTED / WORKING / OUTSIDE / COMPLETE;
   - next action CHECK_IN / EXIT / RETURN;
   - QR / MANUAL theo Work Policy;
   - FACE chỉ hướng dẫn dùng máy chấm công tại nơi làm việc;
   - khi EXIT phải phân biệt END_WORK / WORK_BUSINESS / PERSONAL / BREAK / OTHER;
   - PRESENCE chỉ xác nhận có mặt;
   - lịch sử Attendance Event append-only trong ngày.

2. Quản lý điểm chấm công:
   - GET/POST `/api/workforce/attendance/points`;
   - POST `/api/workforce/attendance/qr-token`;
   - chọn nơi làm việc có sẵn;
   - backend tự tạo/reuse attendance point theo branch;
   - QR token ngắn hạn và tự làm mới trước khi hết hạn.

Permission canonical:
- `core.attendance.self.read` cho trạng thái hôm nay;
- `core.attendance.self.record` cho record;
- `core.attendance-point.manage` cho quản lý điểm và phát QR.

## Desktop implementation

Desktop thay placeholder Chấm công bằng native WPF workspace thật.

Khác biệt trình bày phù hợp Desktop văn phòng:
- Web dùng camera trình duyệt để đọc QR;
- Desktop nhận mã từ máy quét QR dạng keyboard-wedge hoặc thao tác dán mã;
- payload QR và validation vẫn đi qua đúng backend canonical;
- quản lý vẫn phát và hiển thị QR thật để thiết bị khác quét;
- FACE không giả lập trên Desktop vì contract hiện hành thực hiện tại máy chấm công riêng.

Mutation:
- attendance record;
- create/reuse attendance point;
- issue QR token.

Tất cả dùng canonical `Idempotency-Key`. Retry cùng logical payload reuse đúng key cũ; key chỉ bỏ sau mutation thành công. QR refresh thành công là logical operation mới nên tạo key mới.

## Boundary

Lô này chỉ sửa Desktop.
- không sửa Web/backend;
- không sửa DB;
- không migration;
- không deploy production.

Contract hiện tại đã đủ.
