# Issue #85 — Lô 2: Workforce/Master export + File mẫu

## Baseline
- Desktop main: `6c5311c4a24aec49f23a4c7150d489bce08c420c`.
- Web authority: `384c58186067d0bc4bc0705c6442d33775df6695`.
- Web contract: Issue #1190 Lô 4.

## Phạm vi
- Export theo filter/scope: Nhân viên, Chấm công hôm nay, Bảng công ngày/tháng, Nghỉ phép, Tăng ca, Vi phạm, Ca & lịch làm việc, Khách hàng, Nhà cung cấp.
- Bảng công có 3 sheet: Tổng hợp nhân sự, Bảng công từng ngày, Lịch sử chấm công.
- Ca & lịch làm việc có 3 sheet: Lịch làm việc, Ca mẫu, Lịch tuần.
- API có pagination được đọc đủ trang; master Khách/NCC/Nhân viên không cắt ở 1.000 dòng.
- File mẫu Khách hàng XLSX + CSV.
- Opening Balance đọc XLSX + CSV và có mẫu XLSX + CSV.
- Manual inbound và Inventory Adjustment bulk có mẫu XLSX, vẫn giữ CSV.
- ClosedXML hiện có được dùng chung; dữ liệu bắt đầu bằng = + - @ được chặn formula injection.

## Boundary
- Không thêm Supplier/Employee bulk import.
- Không thêm export riêng Điều chỉnh công.
- Không làm lại Payroll/Payslip.
- Không sửa Web/backend/DB/migration.
- Không deploy production.
