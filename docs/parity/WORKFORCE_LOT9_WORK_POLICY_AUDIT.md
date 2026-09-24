# Workforce Desktop — Lô 9 Chính sách làm việc

Ngày audit: 2026-09-24.

## Baseline

- Desktop base: gustavjung01/desktop-mvp@3995249c04677a0fdb1feb2217dc0d2ac621d6d4.
- Web/API authority: binhnxwjfjxm/NPP-Platform@af4acc24bc411d3206d07256f6119472068c01c3.
- Parent nghiệp vụ: Issue Web #1110.
- Trước branch không có PR Desktop đang mở.

## Contract hiện hành

Desktop dùng trực tiếp API hiện có:
- GET /api/workforce/policies;
- POST /api/workforce/policies.

Mutation POST dùng canonical Idempotency-Key. Retry cùng logical payload giữ lại đúng key cũ; chỉ bỏ key sau khi mutation thành công.

Work policy hiện hành bao gồm:
- FIXED / SHIFT / FLEXIBLE / NO_ATTENDANCE;
- working days, break, late/early grace, overtime;
- phương thức QR / FACE / MANUAL và các tổ hợp QR_FACE / BOTH / FACE_MANUAL / ALL / NONE;
- attendance basis TIME / PRESENCE / NONE;
- timezone, rounding, minimum full/half day minutes;
- effective_from/effective_to và lịch sử version.

Web đã thay đổi trong ngày 2026-09-24:
- cho phép Áp dụng ngay hoặc Chọn ngày áp dụng;
- cho phép tạo phiên bản chính sách mới trong cùng ngày;
- phiên bản cũ vẫn giữ để các quan hệ/lịch sử trước đó không bị sửa đè.

Desktop Lô 9 bám đúng contract này.

## Desktop scope

- thay placeholder Chính sách làm việc bằng native WPF workspace thật;
- danh sách phiên bản mới nhất theo mã;
- tab lịch sử phiên bản;
- tạo policy mới;
- cập nhật từ phiên bản mới nhất và để backend tạo version mới;
- lựa chọn phương thức chấm công linh hoạt;
- lựa chọn attendance basis;
- áp dụng ngay hoặc theo ngày;
- permission read/manage deny-by-default;
- loading/empty/error theo ngôn ngữ văn phòng;
- slot workspace riêng, không trùng các màn đã có.

## Boundary

Lô này chỉ sửa repo Desktop.
- không sửa backend;
- không sửa DB;
- không migration;
- không deploy production.

Contract API hiện tại đã đủ.
