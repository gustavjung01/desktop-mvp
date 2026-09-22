# Workforce Desktop — Lô 6: Nền Tính lương

## Baseline đã audit

- Desktop repo: `gustavjung01/desktop-mvp`.
- Desktop `main` trước Lô 6: `5974a4850a8560d47d1074fbfbb1fc38361a2009`.
- Push-CI sau merge Lô 5: run `35738667638`, conclusion `success`, đã qua build, unit tests, package verify, installed package smoke và native startup smoke.
- Không có PR Desktop mở khi bắt đầu Lô 6.
- Web chuẩn: `binhnxwjfjxm/NPP-Platform`.
- Web `main` được audit trực tiếp: `4c9d6652d900f883f8c6cf07316dd46fee8715bf`.
- Nghiệp vụ: Issue #1140, Lô 6 Nền Tính lương.
- Web PR #1150 đã merge; contract cuối cùng được đọc lại trên Web `main`.

## Phạm vi Lô 6

Desktop kích hoạt đúng một mục sidebar lớn **Tính lương**, bên trong có 6 tab:

1. Bảng lương
2. Đối soát
3. Thiết lập lương
4. Khoản thu & khấu trừ
5. Phiếu lương
6. Lịch sử kỳ lương

Lô 6 chỉ dựng nền. Không triển khai sớm tổng hợp/đối soát/chốt lương, điều chỉnh sau chốt, PDF hoặc Excel.

Web `main` hiện đã có thêm contract các lô sau trên cùng route payroll. Desktop Lô 6 chủ động **không gọi** các command `AGGREGATE`, `RECONCILE`, `CLOSE`, `ADJUST` dù backend hiện tại có thể nhận chúng.

## API contract

Desktop chỉ gọi:

- `GET /api/workforce/payroll`
- `GET /api/workforce/payroll?periodId=...`
- `POST /api/workforce/payroll`

Năm command nền được phép trong Lô 6:

- `CREATE_PERIOD`
- `SAVE_SALARY`
- `CREATE_COMPONENT_TYPE`
- `ASSIGN_FIXED_COMPONENT`
- `ADD_PERIOD_COMPONENT`

Mọi POST dùng shared canonical `Idempotency-Key`. Retry cùng logical payload reuse đúng key cũ cho đến khi mutation thành công.

## Kỳ lương và nguồn công

`CREATE_PERIOD` chỉ nhận một attendance period đã `CLOSED` có snapshot hợp lệ.

Payroll period giữ:

- `attendance_period_id`;
- `attendance_revision`;
- `attendance_source_fingerprint`;
- khoảng ngày;
- branch/scope tại nguồn;
- trạng thái kỳ.

Desktop không sửa ngược Attendance Event, bảng công, snapshot hoặc fingerprint. Backend là authority cho kiểm tra closed source, scope, duplicate source và immutable history.

## Tiền và ngày hiệu lực

Schema Web dùng `numeric(18,2)`. API serialize amount thành **string**; Desktop cũng giữ amount dưới dạng string và chỉ chuẩn hóa dấu thập phân trước khi gửi.

Desktop không dùng floating-point để tính tiền và không tự tính tổng lương/net/gross trong Lô 6.

Mức lương và khoản cố định là effective-dated:

- bản mới có `effectiveFrom`;
- backend đóng khoảng bản hiện hành bằng ngày trước ngày áp dụng mới;
- không update đè làm mất lịch sử.

## Danh mục khoản

Công Ty tự định nghĩa khoản, không hard-code tên thưởng/phụ cấp.

Nhóm canonical:

- `INCOME` — Thu nhập lương;
- `DEDUCTION` — Khấu trừ;
- `REIMBURSEMENT` — Hoàn chi phí.

Cách áp dụng:

- `FIXED`;
- `PERIOD`.

Cách ghi nhận:

- `AUTOMATIC`;
- `MANUAL`.

Các cờ nền:

- tính theo ngày công;
- vào tổng thu nhập;
- vào thực nhận.

Hoàn chi phí luôn được hiển thị tách bản chất với thu nhập lương.

## Khoản cố định và khoản theo kỳ

`ASSIGN_FIXED_COMPONENT` chỉ dùng component `FIXED + MANUAL`, có ngày áp dụng và lịch sử hiệu lực.

`ADD_PERIOD_COMPONENT` chỉ dùng component `PERIOD + MANUAL`, yêu cầu:

- kỳ lương;
- nhân sự thuộc đúng snapshot kỳ;
- loại khoản;
- amount;
- lý do/ghi chú.

Period component là append-only theo contract backend. Desktop không cung cấp update/delete.

## Permission

Route GET cho actor có ít nhất một quyền payroll canonical:

- `core.payroll.read`
- `core.payroll.manage`
- `core.payroll.close`
- `core.payroll.adjust`
- `core.payroll.export`

Mutation nền Lô 6 chỉ bật khi backend capability `canManage` và client có `core.payroll.manage`.

Deny-by-default và branch/company scope tiếp tục do backend authority quyết định.

## Boundary

- Không sửa Web/backend/DB/migration.
- Không chạy migration `154_workforce_payroll_foundation` từ Desktop task.
- Không deploy production.
- Không thêm logic BHXH/thuế/chuyển khoản.
- Không tính tổng bảng lương ở Desktop.
- Không sửa ngược bảng công hoặc snapshot nguồn.


## Re-audit Web main trước CI cuối

Trong lúc PR Desktop #65 đang chạy, Web `main` tiến từ `9cd5ed9c52932d3b078647e8e754b927af0bd27c` lên `4c9d6652d900f883f8c6cf07316dd46fee8715bf`.

Diff Web giữa hai revision chỉ thay đổi báo cáo bán hàng và export báo cáo; không thay đổi route, service, repository, permission hay UI payroll/Workforce dùng cho Lô 6. Contract payroll đã audit ở trên giữ nguyên.

Parity baseline được refresh theo Web main mới:
- `webAppTree`: `4b9df64b76532a790f2ffcd7ccf166474dc13f10`;
- `apiRoutesTree`: `6d3fa036f5f69c03983dd7107d9179791bf81573`.

Các fingerprint permission, server, shared contracts và idempotency không đổi.
