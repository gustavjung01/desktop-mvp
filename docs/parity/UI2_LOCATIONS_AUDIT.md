# UI-2.4 — audit Vị trí kho theo Công Ty Web hiện hành

Ngày audit: 2026-09-15.

## Kết luận

Audit source hiện hành cho thấy **Vị trí kho không còn là một màn độc lập trên Công Ty Web**.

Hai bằng chứng canonical trên `NPP-Platform/main@3dafbaa564e2d9715ceb91bff8e35b5dbc344b2c`:

1. `npp-core/web/app/organization/locations/page.tsx` chỉ làm:
   - `redirect('/organization/warehouses?tab=layout')`
2. `npp-core/web/app/components/app-shell-core.tsx` vẫn còn khai báo item `/organization/locations` trong mảng nguồn, nhưng khi render sidebar lại:
   - `organizationItems.filter((item) => item.testId !== 'nav-locations')`

Vì vậy source hiện hành đã thay đổi so với ghi chú cũ trong Issue #31. Theo chính gate của Issue #31, **source Web hiện hành là chuẩn khi tài liệu cũ mâu thuẫn**.

UI-2.4 vì thế không được tạo lại một danh mục Vị trí kho riêng trên Desktop. Hành vi đúng là:
- route/quick-link legacy **Vị trí kho** → mở **Kho hàng → Sơ đồ kho**;
- header/active route cuối cùng là **Kho hàng**, giống URL Web sau redirect;
- không render submenu Vị trí kho độc lập;
- không render thêm toolbar/filter riêng cho một “màn Vị trí kho” mà Web không còn có.

## Baseline

- Công Ty Web/backend: `binhnxwjfjxm/NPP-Platform main@3dafbaa564e2d9715ceb91bff8e35b5dbc344b2c`.
- Desktop base: `binhnxwjfjxm/congty-desktop main@344a0946c2ce5ab8b59cece745206b847ffc89f9`.
- Main CI trước branch: #222 PASS.
- PR UI-3.1 đã merge vào main trước khi tạo branch UI-2.4; không còn PR song song tại thời điểm bắt đầu.

## Matrix Web → Desktop

| Web hiện hành | Desktop trước UI-2.4 | Desktop UI-2.4 |
| --- | --- | --- |
| Sidebar không render `nav-locations` | Có placeholder **Vị trí kho** bị disable | Bỏ placeholder, không tạo dead menu |
| `/organization/locations` redirect → `/organization/warehouses?tab=layout` | Quick link nội bộ mở tab Sơ đồ kho nhưng giữ key/header `catalog.locations` | Quick link mở tab **Sơ đồ kho** và normalize về `catalog.warehouses` |
| Kho hàng → Sơ đồ kho là workflow quản lý vị trí thật | Có thêm filter legacy chỉ hiện cho “standalone locations route” | Bỏ hoàn toàn mode/filter legacy |
| Sơ đồ kho: chọn Kho → Thiết lập sơ đồ / Thêm khu vực | Đã đạt UI-2.3 | Tái sử dụng nguyên flow đã audit |
| Bảng khu vực: Mã khu vực / Tên khu vực / Loại khu vực / Trạng thái / Xử lý | Đã đạt UI-2.3 | Giữ nguyên |
| Editor khu vực + status confirm | Đã đạt UI-2.3 | Giữ nguyên contract |
| Empty/loading/error/disabled | Đã có trong UI-2.3 | Giữ nguyên |

## Backend contract

Không cần backend mới, DB hay migration.

Contract vị trí kho tiếp tục dùng nguồn canonical đã audit ở UI-2.3:
- create `warehouse-locations` dùng shared canonical Idempotency-Key;
- retry cùng thao tác reuse đúng key;
- update/status dùng `expectedUpdatedAt`;
- backend không persist đổi `warehouseId` hay `code` khi edit, nên Desktop giữ hai field này chỉ đọc trong chế độ chỉnh sửa;
- kích hoạt vị trí bị chặn nếu kho cha đang ngừng hoạt động.

## Chỉnh nhẹ canh hàng theo yêu cầu

Ảnh tham chiếu cho thấy tiêu chí mong muốn là **text và nút cùng nằm giữa chiều cao của hàng**. Desktop đã có phần lớn baseline này, nhưng UI-2.4 siết thành rule dùng chung:
- `OfficeGridTextStyle`: text giữ `VerticalAlignment=Center`;
- `OfficeDataGridCellStyle`: cell giữ `VerticalContentAlignment=Center`;
- `OfficeDataGridRowStyle`: bổ sung `VerticalContentAlignment=Center`;
- `OfficeMiniButtonStyle`: bổ sung `VerticalAlignment=Center`, `VerticalContentAlignment=Center`, `HorizontalContentAlignment=Center`.

Không đổi text sang căn giữa ngang toàn cột vì Web dùng text-align trái; chỉ **cân giữa theo chiều dọc của hàng**, còn nội dung vẫn bám layout Web.

## Phạm vi code

UI-2.4 chỉ cần:
- Organization navigation/layout compatibility;
- Shell route normalization;
- shared compact row alignment;
- parity/regression tests;
- audit doc này.

Không sửa Inventory/UI-3, Sales, Partner, backend, contracts hoặc database.
