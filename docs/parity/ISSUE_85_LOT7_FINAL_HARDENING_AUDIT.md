# Issue #85 — Lô 7: Final parity / hardening audit

## Live baseline trước khi sửa

- Desktop `main`: `5299bd3bad2a3558c0d49b04b858d33d1d579b2f`.
- Không có PR Desktop mở.
- Push-CI trên merge commit Lô 6: run `36244795905` — `success`.
- Web `main`: `999f8eaac3d2c584016e9e41ca3a3d043affa3f2`.
- Compare Web từ baseline Issue #85 `384c5818...` đến Web live chỉ đổi `retail/web`; không đổi Công Ty Web/API contract của #1190.
- Parity baseline CI vẫn PASS với 83 screens / 333 Web routes / 93 API source files / 441 endpoint candidates / 233 permissions / 327 mutation candidates. Vì fingerprint source-of-truth của Công Ty không đổi, không sửa SHA manifest chỉ để “làm mới”.

## Re-audit Lô 0–6

| Hạng mục | Kết quả |
|---|---|
| Bảng công quick actions | Đủ 5 action, 4 lý do ra ngoài, ngày cũ chỉ adjustment, permission/capability deny-by-default |
| Direct adjustment | Có reason + audit contract, canonical Idempotency-Key |
| Workforce/Master export | Đủ phạm vi Lô 2; paging đầy đủ cho dataset phân trang; Bảng công có lịch sử; lịch làm việc có ca mẫu + lịch tuần |
| Actual print | Đủ 7 chứng từ Lô 3, dùng `DocumentPrintTemplateRuntime`, read-only |
| Operational export | Đủ 11 nhóm; CSV chỉ ở nhóm Web cho phép; spreadsheet formula guard dùng chung |
| Report export | Đủ 5 báo cáo Lô 5; workbook nhiều sheet theo Web; không lộ technical ID |
| Office forms | Đúng 35 form / 5 nhóm / 13 XLSX / 29 PDF, không CSV, không phiếu lương trống, không fetch production |
| Backend/DB | Không cần thay đổi backend, DB hay migration |

## Hai gap hardening thực tế phát hiện

1. **Retry key quick attendance chưa phân biệt ngày công.** Payload API không chứa `workDate`, nên một request thất bại có thể giữ key rồi vô tình reuse cho cùng nhân viên/action ở ngày khác. Fix: fingerprint logical operation bằng `workDate + payload`; retry cùng ngày/payload vẫn reuse chính key cũ.
2. **Refresh sau mutation làm mất selection ngày công.** `LoadAsync()` rebuild dataset rồi đóng employee/day detail. Fix: chụp `employeeId + workDate`, reload đúng page/filter hiện tại, sau đó mở lại đúng nhân viên và ngày nếu vẫn nằm trong dataset.
3. **Fallback enum trong Workforce/Master export có thể lộ token kỹ thuật khi backend bổ sung enum mới.** Fix: enum không nhận biết hiển thị `Cần kiểm tra`/nhãn văn phòng thay vì raw token; dữ liệu text nghiệp vụ không bị đổi.

## Acceptance cuối

- Quick attendance chỉ chạy cho ngày hiện tại và manager capability; ngày cũ chỉ adjustment.
- Retry cùng logical mutation reuse key; logical operation khác ngày không reuse nhầm key.
- Sau quick action/direct adjustment, popup tiếp tục trỏ đúng employee + workDate nếu record còn trong scope/filter.
- Export theo filter/server scope hiện hành, không chỉ page đối với các contract có pagination.
- Permission guard vẫn ở ViewModel/export builder; không mở rộng quyền.
- XLSX/CSV có formula-injection guard.
- File/worksheet wording là ngôn ngữ văn phòng; enum lạ không bị đẩy thô ra file.
- Không thêm backend/API/DB/migration và không deploy production.
