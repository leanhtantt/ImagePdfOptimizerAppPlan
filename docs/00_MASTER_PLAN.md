# Feature 01 Master Plan: Image Optimizer

> Lưu ý phạm vi: tài liệu này mô tả **Feature 01 Image Optimizer** của app lớn hơn, không phải toàn bộ core app. Master plan cấp app nằm ở `00_APP_SUITE_MASTER_PLAN.md`.
>
> Quyết định chốt mới nằm ở `09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md`: **nén là nén, gộp là gộp**. Feature 01 không còn ôm bước gộp PDF hoặc nén PDF.

## 1. Vai trò của tài liệu này

Đây là kế hoạch tổng hợp để điều phối việc build feature xử lý ảnh tài liệu trong app Windows dạng nhiều tab/module:

```text
Ảnh gốc -> Nén AVIF -> Review dung lượng -> Có thể nén lại ảnh -> Gửi batch sang feature gộp/nén nếu cần
```

Tài liệu này là nguồn điều phối chính. Các file plan nhỏ đi kèm sẽ tách rõ phần product, UI, kỹ thuật, lộ trình implement và checklist nghiệm thu.

## 2. Nguồn đầu vào

- `09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md`: quyết định mới về ranh giới Image Optimizer, File Merge / PDF Builder và PDF Compressor.
- `PLAN_IMAGE_PDF_OPTIMIZER_APP.md`: product plan gốc, có nhiều phần PDF cũ đã bị supersede bởi file `09`.
- `FE_UI_PLAN_IMAGE_PDF_OPTIMIZER_APP.md`: UI/FE plan gốc, có nhiều phần workflow cũ đã bị supersede bởi file `09`.

## 3. Mục tiêu MVP

Build một module Windows dùng `.NET 8 + WinUI 3 / Windows App SDK + FFmpeg` để người dùng không kỹ thuật có thể:

1. Chọn hoặc kéo thả folder ảnh.
2. Convert ảnh sang AVIF siêu nhẹ trước.
3. Tinh chỉnh chất lượng ảnh, resolution và thông số codec.
4. Review dung lượng ảnh đã nén.
5. Nén lại ảnh với setting khác nếu chưa hài lòng.
6. Gửi batch ảnh đã nén sang `File Merge / PDF Builder`.
7. Chạy automation `Gộp và nén` để gửi output sang `PDF Compressor` nếu người dùng muốn đi tiếp.

## 4. Nguyên tắc sản phẩm không được phá

- Feature 01 chỉ chịu trách nhiệm nén/tối ưu ảnh.
- Workflow nối sang PDF phải đi qua automation/handoff sang feature gộp và nén PDF.
- Không sửa, xoá hoặc ghi đè file gốc.
- Nếu output nặng hơn file gốc, phải warning rõ và gợi ý tăng mức nén hoặc giảm resolution.
- Các thông số kỹ thuật phải được dịch thành UI dễ hiểu: `Đẹp hơn`, `Nhẹ hơn`, `Nén mạnh hơn`, `Nhanh hơn`.
- Người dùng phải quay lại nén lại ảnh bất cứ lúc nào mà không làm đứt workflow ở các feature khác.

## 5. Stack kỹ thuật chốt

```text
C# .NET 8
WinUI 3 / Windows App SDK
FFmpeg bundled theo app/package
PDF writer nội bộ hoặc service tự viết bằng C#
```

Lý do:

- Tối ưu cho Windows.
- Gọi FFmpeg ổn định.
- User cuối không cần tự cài FFmpeg hoặc cấu hình path.
- WinUI 3 cho native Fluent controls, `ThemeResource`, `NavigationView`, `InfoBar`, `ListView` và layout hợp với app suite nhiều module.
- Quyết định hiện tại: nếu đã chọn làm WinUI ngay thì các plan UI/kỹ thuật phải bám theo `06_WINUI_UI_DIRECTION.md` và `07_CORE_TECHNICAL_DIRECTION.md`, không quay lại mental model WinForms MVP tối giản.

## 6. Cấu trúc tài liệu triển khai

```text
ImagePdfOptimizerAppPlan
├── 00_MASTER_PLAN.md
├── 01_PRODUCT_REQUIREMENTS.md
├── 02_UI_UX_IMPLEMENTATION_PLAN.md
├── 03_TECHNICAL_ARCHITECTURE.md
├── 04_IMPLEMENTATION_PHASES.md
├── 05_MVP_ACCEPTANCE_CHECKLIST.md
├── 06_WINUI_UI_DIRECTION.md
├── 07_CORE_TECHNICAL_DIRECTION.md
├── 08_WINUI_IMPLEMENTATION_HANDOFF.md
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
- Top action `Gộp file` gửi batch sang `File Merge / PDF Builder`.
- Top action `Gộp và nén` chạy automation: Image Optimizer -> File Merge / PDF Builder -> PDF Compressor.
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
5. WinUI shell layout bằng `NavigationView` + feature host + shared resources.
6. File list, preview/list state, settings panel.
7. Progress/state/warning/error.
8. Handoff actions `Gộp file` và `Gộp và nén`.
9. QA Image Optimizer bằng bộ `Sao ke GD`.
10. Sau đó implement `File Merge / PDF Builder` và `PDF Compressor`.

## 9. Bộ test thực tế

Bộ dữ liệu ưu tiên dùng để QA:

```text
C:\Users\INEC-EDITOR-2\Desktop\Chi Trieu\Sao ke GD
C:\Users\INEC-EDITOR-2\Desktop\Chi Trieu\Sao ke GD\compressed-avif
```

Mục tiêu kiểm chứng:

- 10 ảnh JPG gốc tổng khoảng 8.32 MiB.
- 10 ảnh AVIF tổng khoảng 1.36 MiB.
- Các mốc PDF RGB q12/q13/q15 vẫn là benchmark cho feature `PDF Compressor`, không phải acceptance trực tiếp của Feature 01.

## 10. Quyết định cần giữ nhất quán

- AVIF CRF: số thấp hơn đẹp hơn, số cao hơn nhẹ hơn.
- PDF JPEG q: số thấp hơn đẹp hơn, số cao hơn nhẹ hơn.
- AVIF `CRF 0` là lossless.
- PDF `q 1` là đẹp nhất thực dụng; không gọi q 0 là lossless.

## 11. Tiêu chí hoàn tất MVP

MVP được xem là hoàn tất khi:

- Chạy được trên Windows.
- Chọn folder ảnh và convert AVIF được.
- Review dung lượng ảnh đã nén được.
- Có action handoff sang feature gộp/nén PDF.
- Có đầy đủ state empty/loading/success/warning/error/disabled.
- App dùng tốt trên màn hình 1366x768.
- Không sửa file gốc.
