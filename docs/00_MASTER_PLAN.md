# Feature 01 Master Plan: Image PDF Optimizer

> Lưu ý phạm vi: tài liệu này mô tả **Feature 01** của app lớn hơn, không phải toàn bộ core app. Master plan cấp app nằm ở `00_APP_SUITE_MASTER_PLAN.md`.

## 1. Vai trò của tài liệu này

Đây là kế hoạch tổng hợp để điều phối việc build feature xử lý ảnh tài liệu trong app Windows dạng nhiều tab/module:

```text
Ảnh gốc -> Nén AVIF -> Review dung lượng -> Combine PDF màu -> Chỉnh q -> Chọn final
```

Tài liệu này là nguồn điều phối chính. Các file plan nhỏ đi kèm sẽ tách rõ phần product, UI, kỹ thuật, lộ trình implement và checklist nghiệm thu.

## 2. Nguồn đầu vào

- `PLAN_IMAGE_PDF_OPTIMIZER_APP.md`: product plan gốc, mô tả workflow xử lý ảnh/PDF và business rules.
- `FE_UI_PLAN_IMAGE_PDF_OPTIMIZER_APP.md`: UI/FE plan, mô tả layout, state, component, contract và checklist giao diện.

## 3. Mục tiêu MVP

Build một app Windows dùng `.NET 8 WinForms + FFmpeg` để người dùng không kỹ thuật có thể:

1. Chọn hoặc kéo thả folder ảnh.
2. Convert ảnh sang AVIF siêu nhẹ trước.
3. Tinh chỉnh chất lượng ảnh, resolution và thông số codec.
4. Review dung lượng ảnh đã nén.
5. Combine ảnh đã nén thành PDF.
6. Chỉnh `q` PDF nhiều lần để đạt file vừa nhẹ vừa rõ.
7. Chọn một bản PDF làm final.

## 4. Nguyên tắc sản phẩm không được phá

- Workflow chính phải nén AVIF trước rồi mới combine PDF.
- Không sửa, xoá hoặc ghi đè file gốc.
- RGB là mặc định, không tự chuyển Gray.
- Bật Gray phải có xác nhận vì có thể mất màu con dấu, chữ ký, logo.
- Page mode mặc định là theo kích thước ảnh, không ép A4.
- Nếu output nặng hơn file gốc, phải warning rõ và gợi ý tăng mức nén hoặc giảm resolution.
- Các thông số kỹ thuật phải được dịch thành UI dễ hiểu: `Đẹp hơn`, `Nhẹ hơn`, `Nén mạnh hơn`, `Nhanh hơn`.
- Người dùng phải tạo lại PDF nhanh nhiều lần mà không cần convert AVIF lại.

## 5. Stack kỹ thuật chốt

```text
C# .NET 8
WinForms
FFmpeg bundled theo app/package
PDF writer nội bộ hoặc service tự viết bằng C#
```

Lý do:

- Tối ưu cho Windows.
- Đóng gói `.exe` dễ hơn Python.
- Gọi FFmpeg ổn định.
- User cuối không cần tự cài FFmpeg hoặc cấu hình path.
- UI WinForms đủ tốt cho tool nội bộ.
- Không cần WinUI 3 ở MVP.

## 6. Cấu trúc tài liệu triển khai

```text
ImagePdfOptimizerAppPlan
├── 00_MASTER_PLAN.md
├── 01_PRODUCT_REQUIREMENTS.md
├── 02_UI_UX_IMPLEMENTATION_PLAN.md
├── 03_TECHNICAL_ARCHITECTURE.md
├── 04_IMPLEMENTATION_PHASES.md
├── 05_MVP_ACCEPTANCE_CHECKLIST.md
├── FE_UI_PLAN_IMAGE_PDF_OPTIMIZER_APP.md
└── PLAN_IMAGE_PDF_OPTIMIZER_APP.md
```

## 7. Phạm vi MVP

MVP bắt buộc có:

- Chọn folder/file ảnh.
- Scan và hiển thị danh sách ảnh hợp lệ.
- Convert AVIF.
- Stepper-slider cho AVIF CRF.
- Resolution selector.
- Review dung lượng sau nén.
- Combine PDF từ ảnh đã nén.
- PDF page mode mặc định theo kích thước ảnh.
- RGB mặc định.
- PDF q stepper-slider.
- PDF versions list: q12, q14, q15, final.
- Open file/folder output.
- Warning/error state rõ ràng.

Chưa cần ở MVP:

- OCR.
- Ký số.
- Upload cloud.
- Batch nhiều folder.
- WebP/Both.
- Drag reorder nâng cao.
- Target size/auto optimize. Nếu có yêu cầu sau này sẽ làm ở V2.

## 8. Thứ tự implement

1. Core models và service contract.
2. FFmpeg detection và runner.
3. Image scan và output folder structure.
4. Convert AVIF.
5. PDF combine engine.
6. WinForms shell layout.
7. File list, preview, settings panel.
8. Progress/state/warning/error.
9. PDF versions và final selection.
10. QA bằng bộ `Sao ke GD`.

## 9. Bộ test thực tế

Bộ dữ liệu ưu tiên dùng để QA:

```text
C:\Users\INEC-EDITOR-2\Desktop\Chi Trieu\Sao ke GD
C:\Users\INEC-EDITOR-2\Desktop\Chi Trieu\Sao ke GD\compressed-avif
```

Mục tiêu kiểm chứng:

- 10 ảnh JPG gốc tổng khoảng 8.32 MiB.
- 10 ảnh AVIF tổng khoảng 1.36 MiB.
- PDF RGB q12 khoảng 1.40 MiB.
- PDF RGB q13 khoảng 1.33 MiB.
- PDF RGB q15 khoảng 1.21 MiB.
- Con dấu màu không bị mất.
- Không bị ép vào A4.
- Người dùng có thể chỉnh q nhiều lần và chọn final.

## 10. Quyết định cần giữ nhất quán

- AVIF CRF: số thấp hơn đẹp hơn, số cao hơn nhẹ hơn.
- PDF JPEG q: số thấp hơn đẹp hơn, số cao hơn nhẹ hơn.
- AVIF `CRF 0` là lossless.
- PDF `q 1` là đẹp nhất thực dụng; không gọi q 0 là lossless.

## 11. Tiêu chí hoàn tất MVP

MVP được xem là hoàn tất khi:

- Chạy được trên Windows.
- Chọn folder ảnh và convert AVIF được.
- Combine PDF từ ảnh nén được.
- PDF giữ màu và orientation.
- Có slider/stepper q để tạo nhiều bản PDF.
- Có danh sách PDF versions và chọn final.
- Có đầy đủ state empty/loading/success/warning/error/disabled.
- App dùng tốt trên màn hình 1366x768.
- Không sửa file gốc.
