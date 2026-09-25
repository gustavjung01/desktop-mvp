# Desktop audit cleanup — Lô 1

Ngày audit: 2026-09-25.

## Baseline

- Desktop: `gustavjung01/desktop-mvp@9c85be7308b960f0ed83f71b83cb6a1a7f6763e8`.
- Desktop exact-head CI trước lô: run `36021763958` — success.
- Web/API authority: `binhnxwjfjxm/NPP-Platform@af4acc24bc411d3206d07256f6119472068c01c3`.
- Web không có PR mở tại thời điểm audit.
- Parent Workforce: Web Issues #1110 và #1140.

## Phạm vi

1. Điều hành bán hàng:
   - Web `/management` có link thật **Mở màn xử lý** tới `/management/customer-onboarding`.
   - Desktop bỏ nút chết, nối sang workspace **Mở/liên kết mã khách** hiện có.
   - Giữ permission `core.customer-onboarding.read`.

2. Địa chỉ khách hàng:
   - Web hiển thị action trực tiếp **Sửa / Đặt mặc định / Ngừng sử dụng hoặc Đưa vào sử dụng** và link định vị.
   - Desktop bỏ menu **Thao tác ▾** ở cả hai bảng địa chỉ, dùng action trực tiếp; mutation vẫn dùng service hiện có và backend canonical.

3. Organization legacy:
   - Web Organization chỉ sở hữu Chi nhánh / Kho hàng / Vị trí trong sơ đồ kho.
   - Desktop bỏ tab **Nhân sự** legacy khỏi host Organization, ngừng tải danh mục nhân sự trong refresh Organization; Danh mục nhân sự tiếp tục thuộc workspace Workforce riêng.

4. Ngôn ngữ Workforce:
   - chuẩn hóa theo Web: **Tăng ca và chốt công**, **Xử lý vi phạm chấm công**, **Ca và lịch làm việc**.

## Contract/API

Không đổi backend, database, migration hoặc API contract.
Customer address mutation vẫn qua `PATCH /api/customers/{customerId}/addresses/{addressId}` với quyền canonical.
Customer onboarding vẫn dùng permission/read contract hiện hành.
Không phát sinh mutation mới nên không thay đổi canonical Idempotency-Key behavior.

## Gate

- Không còn tooltip/nút chết “sẽ được nối” ở Điều hành bán hàng.
- Không còn `Thao tác ▾` ở địa chỉ khách.
- Không còn tab Nhân sự legacy trong Organization.
- Ba nhãn Workforce khớp Web.
- CI exact-head phải xanh trước merge.
