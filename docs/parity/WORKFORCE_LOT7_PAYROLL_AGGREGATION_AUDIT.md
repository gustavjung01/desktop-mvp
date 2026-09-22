# Workforce Desktop — Lô 7: Tổng hợp & đối soát lương

## Baseline đã audit

- Desktop repo: gustavjung01/desktop-mvp.
- Desktop main trước Lô 7: fa39caa805f806dc920b998462b3dd8a005e63ae.
- PR Lô 6: #65 đã merge.
- Desktop push-CI trên baseline: run 35744355750, conclusion success.
- Không có PR Desktop mở khi bắt đầu Lô 7.
- Web chuẩn: binhnxwjfjxm/NPP-Platform.
- Web main audit trực tiếp: 4c9d6652d900f883f8c6cf07316dd46fee8715bf.
- Kế hoạch nghiệp vụ: Issue #1140.
- Contract Lô 7 được khóa theo Web PR #1151, merge commit 55a3e8a633d185526c40d4536ba0b359be3b2ead.
- Web main hiện đã chứa cả Lô 8, vì vậy Desktop Lô 7 chỉ lấy phần contract của PR #1151 và không kéo thao tác Lô 8 vào sớm.

## Phạm vi Lô 7

Desktop bổ sung đúng hai mutation trên route payroll hiện có: AGGREGATE và RECONCILE.

- lấy đúng snapshot kỳ công đã chốt mà kỳ lương đang ghim;
- hiển thị lương theo công, công/OT, khoản cố định, khoản theo kỳ;
- tách Thu nhập lương / Khấu trừ / Hoàn chi phí;
- hiển thị tổng bảng lương và chi tiết từng nhân sự từ calculation snapshot backend trả;
- hiển thị blocker và warning canonical;
- chặn đối soát khi còn blocker;
- warning phải được xác nhận và có ghi chú trước khi đối soát;
- trạng thái RECONCILED hiển thị là Đã đối soát.

## Contract API

Tiếp tục dùng duy nhất GET /api/workforce/payroll, GET /api/workforce/payroll?periodId=... và POST /api/workforce/payroll.

AGGREGATE gửi command + payrollPeriodId. RECONCILE gửi command + payrollPeriodId + acknowledgeWarnings + note.

GET trả thêm calculation gồm revision, sourceFingerprint, issueSummary.blockers, issueSummary.warnings, snapshot.contractVersion, snapshot.totals và snapshot.rows.

Money tiếp tục giữ dưới dạng string decimal ở Desktop. salaryAmount, incomeTotal, reimbursementTotal, deductionTotal, grossIncome và netPay đều được hiển thị trực tiếp từ snapshot canonical; Desktop không tự cộng hay dựng lại tổng lương.

## Blocker / warning

Blocker gồm kỳ không có nhân sự, thiếu mức lương phủ kỳ, mâu thuẫn mức lương, khoản cố định đổi giữa kỳ chưa có rule chia kỳ, không có công chuẩn và nguồn thay đổi sau tổng hợp.

Warning gồm OT đã xác nhận nhưng chưa có rule tiền OT tự động, ngày công chưa hoàn chỉnh, vắng không phép và ngày có vi phạm công. OT chỉ hiển thị thời lượng confirmed; Desktop không tự tạo hệ số hoặc tiền OT.

## Idempotency

Cả AGGREGATE và RECONCILE dùng lại MutationSlot(payload) + canonical ICanonicalIdempotencyKeyProvider. Cùng logical payload thất bại rồi retry reuse đúng key cũ; chỉ xóa key sau mutation thành công; không tự ghép key.

## UI Desktop

Giữ một workspace Tính lương và 6 tab từ Lô 6. Bảng lương dùng DataGrid văn phòng với Nhân sự, Lương theo công, Công / OT, Thu nhập thêm, Hoàn chi, Khấu trừ và Thực nhận. Chọn nhân sự hiển thị vùng chi tiết gồm Lương cố định, Theo công & OT, Thưởng & phụ cấp, Công tác phí & hoàn chi phí, Khấu trừ và Thực nhận. Tab Đối soát hiển thị blocker/warning, xác nhận warning và ghi chú.

## Boundary Lô 8

Lô 7 không triển khai CLOSE / ADJUST, chốt kỳ lương, điều chỉnh sau chốt, phiếu lương thực, PDF, Excel hoặc immutable close snapshot UI. Hai tab Phiếu lương và Lịch sử kỳ lương giữ nền hiện có cho lô sau.

## Ranh giới repository

- Không sửa Web/backend/DB/migration.
- Không chạy migration production.
- Không deploy production.
- Không bổ sung BHXH/thuế/chuyển khoản.
- Backend tiếp tục là authority cho calculation, source fingerprint, permission, audit và business validation.
