# Issue #85 — Office Forms print visual parity correction

## Lý do sửa sau Lô 7

Ảnh thực tế Desktop cho thấy preview PDF biểu mẫu văn phòng tuy đúng catalog/action nhưng chưa đạt visual parity với Web #1190.
Desktop dùng FlowDocument generic với font 10.5 DIP (~7.9pt), header và meta khác cấu trúc Web nên tài liệu bị nhỏ, thưa và giống preview kỹ thuật.

## Web source-of-truth

- `npp-core/web/app/operations/data-exchange/office-forms-library.tsx`
- `npp-core/web/app/components/business-document-print.tsx`
- `npp-core/web/app/components/business-document-print.module.css`
- `npp-core/web/app/components/print-document.module.css`

Web dùng:
- heading fallback `HƯNG PHÁT`;
- subtitle `Biểu mẫu văn phòng trống`;
- title 18pt, brand 16pt, body 10.5pt;
- meta hai cột;
- bảng full grid, header nền #f1f1f1;
- note có khung;
- chữ ký ba cột;
- A4 clean padding 11mm top / 10mm ngang / 9mm bottom.

## Desktop correction

Chỉ sửa renderer `OfficeFormPrintPreview`:
- dùng Arial và quy đổi point -> WPF DIP đúng vật lý;
- dựng header brand/title riêng theo Web;
- meta 2 cột;
- bảng 8 dòng trống, full grid + header xám;
- note luôn hiển thị như Web, chữ ký tách label/hint;
- bỏ generic body/footer không tồn tại trên Web;
- giấy trắng trên nền preview xám và tự fit toàn trang;
- print vẫn qua canonical `DocumentPrintTemplateRuntime.PrepareDialog/ApplyPrintableArea`.

Không sửa catalog 35 biểu mẫu, không thay API/backend/DB/migration, không thêm mutation và không deploy production.
