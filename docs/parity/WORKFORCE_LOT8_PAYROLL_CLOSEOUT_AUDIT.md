# Workforce Desktop — Lô 8: Chốt lương, điều chỉnh & phiếu lương

## Baseline đã audit

- Desktop repo: gustavjung01/desktop-mvp.
- Desktop main trước Lô 8: 74e33c539d6ae30e4605b79bd763ecd12f34125a.
- PR Lô 7: #66 đã merge.
- Desktop exact-head Lô 7 trước merge: 3595b79da37e05a54c29af3c368e78167b8ad94e, CI success.
- Push-CI của merge commit Lô 7: run 35747836121 đang chạy tại thời điểm bắt đầu audit Lô 8; không suy đoán kết quả.
- Không có PR Desktop mở khi bắt đầu Lô 8.
- Web chuẩn: binhnxwjfjxm/NPP-Platform.
- Web main audit trực tiếp: 4c9d6652d900f883f8c6cf07316dd46fee8715bf.
- Kế hoạch nghiệp vụ: Issue #1140.
- Contract Lô 8 được khóa theo Web PR #1152, head 225196da7e3a619726918009d4a2fc8f1d673f56, merge commit 3b4e62a5cadb76a0895f48aef6df6eab73c84900.
- Migration backend của Web: 156_workforce_payroll_closeout.
- Audit trạng thái production hiện có cho thấy migration 152-156 đã APPLIED + VERIFIED; backend Công Ty hiện hành đã deploy source 4c9d6652d900f883f8c6cf07316dd46fee8715bf. Lô Desktop này không chạy migration hoặc deploy production.

## Phạm vi Lô 8

Desktop hoàn tất lifecycle payroll theo contract Web hiện có:

- CLOSE chỉ dùng cho kỳ RECONCILED và backend tiếp tục là authority kiểm fingerprint;
- chốt kỳ tạo close snapshot và payslip snapshot append-only;
- ADJUST chỉ dùng sau khi kỳ CLOSED;
- điều chỉnh yêu cầu nhân sự, loại khoản, hướng ADD/REVERSE, số tiền dương và lý do;
- mỗi điều chỉnh tạo revision phiếu lương mới, không update đè phiếu cũ;
- Phiếu lương đọc closeout.payslips;
- Lịch sử phiếu lương đọc closeout.payslipHistory;
- Lịch sử kỳ lương đọc closeout.history;
- Excel và PDF được tạo ở Desktop từ chính immutable payslip/close snapshot do backend trả, không tái tính tiền lương.

## Contract API

Tiếp tục dùng duy nhất GET /api/workforce/payroll, GET /api/workforce/payroll?periodId=... và POST /api/workforce/payroll.

CLOSE gửi:

- command = CLOSE;
- payrollPeriodId.

ADJUST gửi:

- command = ADJUST;
- payrollPeriodId;
- employeeId;
- componentTypeId;
- direction = ADD hoặc REVERSE;
- amount;
- reason.

GET trả thêm closeout gồm:

- closeSnapshot;
- payslips;
- payslipHistory;
- adjustments;
- history.

Permission tách riêng theo backend capability + access permission:

- core.payroll.close;
- core.payroll.adjust;
- core.payroll.export.

Desktop giữ deny-by-default bằng cách yêu cầu đồng thời capability backend và permission local trước khi mở action.

## Immutable history

Desktop không dựng lại phiếu từ salary profile/component hiện tại. Mọi số tiền hiển thị và xuất file lấy từ snapshot đã chốt hoặc payslip revision mà backend trả.

- Không mutation ngược Bảng công.
- Không update đè close snapshot hoặc payslip.
- Điều chỉnh sau chốt chỉ gọi ADJUST.
- Lịch sử kỳ đọc hồ sơ chốt.
- Lịch sử phiếu đọc các revision append-only.

## Idempotency

CLOSE và ADJUST dùng lại MutationSlot(payload) + canonical ICanonicalIdempotencyKeyProvider hiện có với prefix payroll-foundation.

- cùng logical payload lỗi rồi retry reuse đúng key;
- chỉ xóa key sau mutation thành công;
- không tự ghép Idempotency-Key;
- không tạo key mới cho cùng logical retry.

## Xuất file Desktop

Web tạo Excel/PDF ở Next routes từ cùng closeout snapshot. Desktop không gọi route Next riêng mà tạo file office tương đương từ immutable snapshot backend đã trả:

- Excel: ClosedXML, cột parity với Web export;
- PDF: SkiaSharp PDF document, nội dung phiếu lương từ payslip snapshot;
- money giữ string decimal từ server;
- Desktop chỉ format để hiển thị/xuất, không cộng/trừ lại gross/net.

## UI Desktop

Giữ đúng một workspace Tính lương và 6 tab:

1. Bảng lương;
2. Đối soát;
3. Thiết lập lương;
4. Khoản thu & khấu trừ;
5. Phiếu lương;
6. Lịch sử kỳ lương.

Bảng lương bổ sung khu vực hoàn tất kỳ và action CHỐT LƯƠNG khi có quyền.

Phiếu lương có:

- danh sách payslip hiện tại;
- chi tiết công/lương/thực nhận;
- lịch sử revision;
- điều chỉnh sau chốt;
- XUẤT PDF;
- XUẤT EXCEL.

Lịch sử kỳ lương đọc closeout history và cho phép mở lại kỳ cũ.

## Ranh giới repository

- Không sửa Web/backend/DB/migration.
- Không chạy migration production.
- Không deploy production.
- Không thêm BHXH, thuế, chuyển khoản tự động hoặc nghiệp vụ ngoài Issue #1140.
- Backend tiếp tục là authority cho fingerprint, close validation, adjustment calculation, permission, audit và immutable lineage.
