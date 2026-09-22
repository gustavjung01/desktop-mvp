# Workforce Desktop — Lô 0 audit & navigation baseline

Ngày audit: 2026-09-22.

## Nguồn chuẩn

- Web/backend authority: `binhnxwjfjxm/NPP-Platform@4186ea9638470d2f89882f51de8fa0aa51347654`
- Kế hoạch nghiệp vụ: Issue Web #1140 — Công Ty — Hoàn thiện Nhân sự vận hành & Tính lương.
- Desktop base: `gustavjung01/desktop-mvp@26eaa5bffacb98b1d2bac4a0b6b004888027e47b`
- Desktop tại thời điểm bắt đầu không có PR mở.
- CI Desktop gần nhất trên PR #57: `Desktop CI` và CodeRabbit đều success.

## Surface Web hiện hành

Nhóm **Nhân sự** có đúng 10 màn cấp menu, theo thứ tự: Chấm công; Bảng công; Tăng ca & chốt công; Tính lương; Nghỉ và đơn nghỉ; Xử lý vi phạm công; Điều chỉnh công; Danh mục nhân sự; Ca / lịch làm việc; Chính sách làm việc.

`Danh mục nhân sự` đã có native Desktop từ Lô Access trước. Lô 0 chỉ chuyển nó về đúng nhóm **Nhân sự**; không viết lại nghiệp vụ đã có.

Trong **Người dùng & phân quyền** chỉ giữ Vai trò và phân quyền, Người dùng.

## Parity rebaseline

- 83 screens / 331 Web routes / 91 API source files
- 421 endpoint candidates / 233 permissions / 312 mutation candidates
- Web app tree: `cf7f4ff8064217c8ba79f58cc23dac05dd3bc018`
- API routes tree: `eccd477ad730a456283d26ca808230895af8a519`
- Access tree: `79e5e6724b03293cf8ee133ee49edeb57674bdab`

So với baseline cũ `7a6cee4d647b6a639bb8300a6d4dd5beba678045`, Web thêm hai screen `/workforce/overtime` và `/workforce/payroll`; backend route surface đổi đúng ở `employees.js` và `workforce.js`; permission catalog thêm 10 quyền OT/chốt công/payroll. Server registry, shared contracts và canonical idempotency implementation không đổi.

Canonical idempotency blob vẫn là `faba39af51fd81d0b8d778b20f669a17825da3f7`. Các lô mutation sau phải tiếp tục dùng canonical `Idempotency-Key`; retry cùng logical operation phải reuse đúng key cũ.

## Phạm vi Desktop Lô 0

Lô 0 tách **Nhân sự** khỏi **Người dùng & phân quyền**, dựng đủ 10 mục menu theo Web, dùng canonical permission từ auth state và deny-by-default, giữ `Danh mục nhân sự` là workspace thật hiện có, còn Lô 2–9 ở trạng thái chưa tương tác có tooltip chỉ rõ lộ trình.

Lô 0 **không thêm API nghiệp vụ**, không tạo typed client Workforce mới, không có mutation mới, không sửa backend/DB/migration và không deploy production.
