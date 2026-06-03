# App Suite Master Plan: File Utility Hub

## 1. Vai trò của tài liệu này

Đây là master plan cấp app cho một bộ công cụ Windows xử lý file. Feature đang được plan chi tiết hiện tại chỉ là **Feature 01**, không phải toàn bộ core app.

Tên app tạm thời:

```text
File Utility Hub
```

Tên có thể đổi sau. Điều quan trọng là kiến trúc phải hỗ trợ nhiều feature dạng tab/module.

## 2. Định hướng sản phẩm

App là một bộ công cụ xử lý file local trên Windows, dùng cho người không kỹ thuật nhưng cần thao tác nhanh với tài liệu, ảnh, PDF, Word, Excel.

App không phải chỉ là công cụ image-to-PDF. Feature image/PDF optimizer là feature đầu tiên vì đã có workflow thực tế, script thử nghiệm, output sếp hài lòng và bộ test `Sao ke GD`.

## 3. Feature tabs dự kiến

MVP app nên thiết kế theo tab/module:

| Tab | Tên feature | Trạng thái | Ghi chú |
|---|---|---|---|
| 01 | Image -> AVIF -> PDF Optimizer | Làm trước | Plan hiện tại |
| 02 | PDF Compressor | Sau Feature 01 | Chuyên nén PDF sẵn có |
| 03 | PDF Converter | Sau Feature 01 | PDF sang ảnh, ảnh sang PDF, format khác |
| 04 | PDF Combine/Split | Sau Feature 01 | Gộp/tách PDF |
| 05 | Word Tools | Sau MVP | Combine/convert Word |
| 06 | Excel Tools | Sau MVP | Combine/convert Excel |
| 07 | Batch Workspace | Sau MVP | Xử lý nhiều folder/job |

## 4. Feature 01 làm trước

Feature đầu tiên:

```text
Image -> AVIF -> PDF Optimizer
```

Workflow:

```text
Ảnh gốc
-> Nén AVIF
-> Review dung lượng
-> Combine PDF màu
-> Chỉnh q
-> Chọn final
```

Lý do làm trước:

- Đã test thực tế bằng FFmpeg/script.
- Đã có bộ file `Sao ke GD`.
- Đã kiểm chứng thực tế: 10 ảnh JPG gốc khoảng 8.32 MiB, 10 ảnh AVIF khoảng 1.36 MiB, PDF RGB q12/q13/q15 nằm trong vùng khoảng 1.2-1.5 MiB.
- Đã xác minh rule quan trọng: không Gray mặc định, không ép A4, chỉnh q để đạt dung lượng.
- Có giá trị dùng ngay.

## 5. Core app không được phụ thuộc vào một feature

Core app phải là shell chung, không hardcode riêng cho Image PDF Optimizer.

Core app gồm:

- Main window.
- Tab navigation hoặc module navigation.
- Global settings.
- FFmpeg/tool detection.
- Job runner.
- Progress/log system.
- File picker/dropzone cơ bản.
- Output folder manager.
- Error/warning/toast system.
- Shared controls như stepper-slider.

Feature-specific logic nằm trong từng module riêng.

## 5.1. UI framework decision

Nếu chọn làm UI bằng WinUI ngay, quyết định này áp dụng cho cả shell và feature content:

- Shell dùng WinUI native controls/patterns.
- Feature 01 dùng WinUI `UserControl`, `ListView.ItemTemplate`, `InfoBar`, `ContentDialog`, shared controls và MVVM.
- Core/shared/infrastructure vẫn tách khỏi UI framework để không khóa business logic vào XAML/code-behind.

Hai tài liệu bổ sung bắt buộc đọc trước khi implement WinUI:

```text
06_WINUI_UI_DIRECTION.md
07_CORE_TECHNICAL_DIRECTION.md
```

## 6. Kiến trúc module đề xuất

```text
FileUtilityHub.App
├── Shell
│   ├── MainForm
│   ├── TabNavigation
│   ├── GlobalStatusBar
│   └── GlobalSettings
├── Shared
│   ├── Controls
│   ├── Models
│   ├── Services
│   └── Utilities
├── Features
│   ├── ImagePdfOptimizer
│   ├── PdfCompressor
│   ├── PdfConverter
│   ├── PdfCombineSplit
│   ├── WordTools
│   └── ExcelTools
└── Infrastructure
    ├── Ffmpeg
    ├── ProcessRunner
    ├── Logging
    └── FileSystem
```

## 6.1. UI shell cấp suite

UI shell cấp suite được mô tả riêng trong:

```text
00_SUITE_UI_UX_SHELL_PLAN.md
```

File đó là nguồn chính cho:

- Header chung.
- Module navigation.
- Global settings.
- Tool status.
- Job queue/progress chung.
- Log center chung.
- Placeholder cho các module chưa implement.
- Quy tắc render feature module trong shell.

## 7. Shared services

Shared services nên viết một lần để dùng cho nhiều feature:

- `ToolLocator`: tìm FFmpeg và tool phụ.
- `ProcessRunner`: chạy command, progress, cancel, log.
- `FileScanService`: scan file/folder theo extension.
- `OutputManager`: tạo folder output, đặt tên file, xử lý overwrite.
- `JobProgressService`: quản lý progress chung.
- `LogService`: lưu/copy log.
- `WarningService`: chuẩn hoá warning.

## 8. Shared UI controls

Các control dùng chung:

- `DropZone`.
- `FileTable`.
- `ProgressPanel`.
- `WarningBox`.
- `ErrorBox`.
- `LogDialog`.
- `StepperSlider`.
- `OutputVersionList`.
- `StatusBadge`.

Feature 01 dùng nhiều control này, nhưng không được viết theo kiểu chỉ phục vụ Feature 01.

## 9. MVP app scope

MVP app gồm:

- App shell có tab/module navigation.
- Global FFmpeg detection.
- Global log/status.
- Feature 01 hoàn chỉnh.
- Các tab còn lại có thể ở trạng thái placeholder/coming soon nếu chưa implement.

Không nên build toàn bộ PDF/Word/Excel ngay. Làm vậy sẽ vỡ scope.

## 10. Thứ tự implement đúng

1. Dựng app shell theo module/tab.
2. Dựng shared infrastructure: FFmpeg locator, process runner, output manager, log.
3. Dựng shared controls cơ bản.
4. Implement Feature 01.
5. QA Feature 01 bằng bộ `Sao ke GD`.
6. Sau khi Feature 01 ổn, mới mở Feature 02: PDF Compressor.

## 11. Tài liệu liên quan

Bộ plan hiện tại `00_MASTER_PLAN.md` đến `05_MVP_ACCEPTANCE_CHECKLIST.md` được hiểu là plan cho:

```text
Feature 01: Image -> AVIF -> PDF Optimizer
```

Không dùng các file đó để định nghĩa toàn bộ app suite.

## 12. Quyết định chốt

- Có. App nên phân theo tab/feature.
- Có. Feature hiện tại làm trước.
- Có. Cần master plan cấp app để tránh hiểu nhầm scope.
- Không. Không nên implement ngay app chỉ có một feature theo kiểu hardcode.
- Không. Không nên mở PDF/Word/Excel cùng lúc trong MVP đầu tiên.
