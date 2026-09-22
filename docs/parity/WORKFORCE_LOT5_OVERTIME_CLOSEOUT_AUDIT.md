# Workforce Desktop — Lô 5: Tăng ca & chốt công

## Baseline đã audit

- Desktop repo: `gustavjung01/desktop-mvp`.
- Desktop `main` trước Lô 5: `b4b975783b4a28879c2509c5bd08850d92dafef1`.
- Push-CI baseline: run `35735114310`, event `push`, conclusion `success`.
- Web chuẩn: `binhnxwjfjxm/NPP-Platform`.
- Web `main` được audit trực tiếp: `9cd5ed9c52932d3b078647e8e754b927af0bd27c`.
- Nghiệp vụ: Issue #1140 Lô 5; Web PR #1149 đã merge. PR dùng để định vị thay đổi, contract cuối cùng được đọc lại trên Web `main`.

## Phạm vi đúng của Lô 5

Lô 5 là **Tăng ca & chốt công**, không phải Điều chỉnh công của Issue #1110.

Hai luồng nghiệp vụ:

1. Tăng ca: `SUBMITTED → APPROVED/REJECTED → ACTUAL_RECORDED → CONFIRMED`.
2. Kỳ công: `AGGREGATING → NEEDS_ACTION → RECONCILED → CLOSED`.

Chỉ `confirmed_minutes` của hồ sơ OT `CONFIRMED` được đưa vào đầu vào tính lương.

## API hiện hành

Desktop chỉ tiêu thụ contract sẵn có:

- `GET/POST /api/workforce/overtime`
- `POST /api/workforce/overtime/review`
- `POST /api/workforce/overtime/actual`
- `POST /api/workforce/overtime/confirm`
- `GET/POST /api/workforce/attendance/periods`
- `GET /api/workforce/attendance/payroll-input?periodId=...`

Tất cả mutation đi qua canonical `Idempotency-Key`. Retry cùng logical payload reuse cùng key cho đến khi mutation thành công.

## Permission

Permission canonical:

- `core.overtime.self-request`
- `core.overtime.read`
- `core.overtime.approve`
- `core.overtime.confirm`
- `core.attendance.reconcile`
- `core.attendance.lock`

Backend tiếp tục deny-by-default và scope theo assignment/branch tại ngày nghiệp vụ. Desktop không suy quyền từ role name, chức danh hoặc dữ liệu hiển thị.

## Tăng ca

Desktop cho phép:

- nhân sự gửi đăng ký của chính mình khi có quyền;
- duyệt/từ chối `SUBMITTED`;
- ghi nhận thực tế cho `APPROVED`;
- xác nhận phút được tính cho `ACTUAL_RECORDED`.

Mutation manager gửi `expectedVersion`. Policy OT hiệu lực, auto-approval, period hard lock, employee-at-date, scope và duplicate request đều để backend là authority.

Attendance Event là evidence thời gian. Desktop không sửa Attendance Event và không tự suy “có chấm công ngoài giờ = OT được trả lương”.

## Chốt công

Desktop hiển thị blocker canonical:

- thiếu cấu hình;
- điều chỉnh đang chờ;
- đơn nghỉ đang chờ;
- OT chưa xác nhận.

Warning canonical:

- ngày công chưa đủ;
- vắng không phép;
- ngày có vi phạm.

`REFRESH` chỉ tổng hợp lại nguồn. `RECONCILE` yêu cầu không còn blocker; nếu còn warning phải xác nhận đã kiểm tra và có ghi chú. `CLOSE` chỉ hợp lệ khi period đã `RECONCILED`, dữ liệu không đổi sau đối soát và actor có quyền khóa kỳ.

Các fingerprint, build timesheet source, check blocker/warning, period lock và transition thật đều do backend xử lý.

## Snapshot đầu vào lương

Khi `CLOSED`, backend tạo attendance period snapshot append-only theo revision. Desktop chỉ đọc qua `attendance/payroll-input` và không tự dựng payroll input.

UI hiển thị:

- ngày công;
- phút/giờ được tính;
- nghỉ được ghi nhận;
- OT đã xác nhận;
- các chỉ số cảnh báo của snapshot.

Kỳ cũ không được tái tính từ current employee/policy ở Desktop.

## Boundary

- Không kéo nền Payroll Lô 6 vào Lô 5.
- Không tự dựng OT/payroll engine ở Desktop.
- Không update/delete snapshot lịch sử; snapshot là append-only.
- Không sửa Web/backend/DB/migration.
- Không deploy production trong task này.
