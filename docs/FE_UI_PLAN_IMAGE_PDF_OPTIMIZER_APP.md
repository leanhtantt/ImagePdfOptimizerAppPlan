# FE/UI Source Plan: Feature 01 Image PDF Optimizer

> Legacy note: file này là source plan cũ. Quyết định mới nhất nằm ở `09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md`: Feature 01 là `Image Optimizer`; gộp file và nén PDF thuộc feature riêng. Các section trong file này nói `AVIF -> PDF RGB -> chỉnh q -> final` không còn là scope trực tiếp của Feature 01.

> Phạm vi: đây là bản FE plan nguồn cho **Feature 01**, không phải UI plan cho toàn bộ app suite.

> Scope MVP đã chốt: chỉ làm AVIF -> PDF RGB -> chỉnh q -> chọn final. Không làm WebP/Both và không làm target size/auto optimize trong MVP. FFmpeg phải được bundle sẵn trong app/package, không yêu cầu user cấu hình path.

## 1. Phản biện plan

Plan gốc đã rõ về workflow xử lý file và các business rule quan trọng: nén AVIF trước, review dung lượng, combine PDF giữ màu RGB, chỉnh q để tạo lại PDF nhanh, không sửa file gốc.

Các điểm FE cần làm rõ thêm khi triển khai:

- App là tool Windows cho người không kỹ thuật, nên giao diện phải ưu tiên thao tác nhanh hơn là nhiều tuỳ chọn kỹ thuật.
- Các thuật ngữ `CRF`, `JPEG q` không nên hiện như nhãn chính. Chỉ hiện trong khu vực nâng cao.
- Workflow chính không nên bị chia thành quá nhiều màn hình rời. Nên dùng một màn hình chính với sidebar workflow, khu preview/list ở giữa và panel setting bên phải.
- Mọi tác vụ dài như scan folder, convert, combine PDF phải có progress rõ ràng để người dùng không nghĩ app bị treo.
- Gray mode, output nặng hơn file gốc, file không hỗ trợ, thiếu FFmpeg, lỗi ghi file đều phải có warning/error dễ hiểu.
- Cần có danh sách các bản PDF đã tạo trong phiên làm việc để người dùng so sánh và chọn bản final.

Giả định thiết kế:

- MVP hiện đi theo WinUI 3 / Windows App SDK trên Windows; nếu còn ví dụ WinForms thì chỉ là di sản plan cũ và không được dùng làm hướng implement chính.
- Chỉ có một nhóm người dùng chính, không có phân quyền.
- Không cần responsive kiểu web/mobile, nhưng phải dùng tốt trên màn hình tối thiểu `1366x768`.
- Giao diện dùng tiếng Việt có dấu.
- Không dùng palette/brand color riêng trong WinUI plan; dùng control và theme mặc định của WinUI.

## 2. UX flow đề xuất

### Luồng chính

```text
Mở app
-> Chọn folder hoặc kéo thả file/folder
-> App scan và hiển thị danh sách ảnh
-> Người dùng chỉnh preset AVIF/resolution
-> Bấm "Nén AVIF"
-> App convert và hiển thị kết quả dung lượng
-> Người dùng review ảnh đã nén
-> Bấm "Tạo PDF"
-> App tạo PDF với q mặc định
-> Người dùng xem dung lượng PDF
-> Nếu chưa ưng, chỉnh q và bấm "Tạo lại PDF"
-> Chọn bản PDF tốt nhất
-> Bấm "Đặt làm bản final"
-> Mở folder output hoặc mở file PDF
```

### Luồng phụ

- Nếu file output nặng hơn file gốc: hiển thị warning và gợi ý tăng mức nén hoặc giảm resolution.
- Nếu người dùng bật Gray mode: hiện confirm, mặc định khuyến nghị giữ RGB.
- Nếu thiếu FFmpeg: chặn thao tác convert/combine và hiển thị hướng dẫn cấu hình.
- Nếu có file không hỗ trợ: vẫn cho xử lý các file hợp lệ, nhưng hiển thị danh sách file bị bỏ qua.
- Nếu output đã tồn tại: hỏi ghi đè, tạo tên mới hoặc bỏ qua.
- Nếu người dùng chỉ chỉnh q PDF: không bắt convert AVIF lại.

## 3. Danh sách màn hình và vùng giao diện

| Màn hình / vùng | Mục đích | Thành phần chính | State cần có |
| --- | --- | --- | --- |
| Header | Nhận diện app và thao tác nhanh | Tên app, folder đang chọn, nút chọn folder, nút mở output | Default, disabled, processing |
| Workflow sidebar | Cho biết tiến độ xử lý | Stepper: Chọn ảnh, Nén AVIF, Review, Tạo PDF, Xuất file | Chưa làm, đang chạy, hoàn tất, có lỗi |
| Dropzone / input | Nhập file/folder ảnh | Vùng kéo thả, nút chọn folder, thống kê file | Empty, drag over, loading, error |
| File list | Kiểm tra danh sách ảnh | Tên file, định dạng, size gốc, size output, trạng thái | Empty, loaded, warning, error |
| Preview | Xem ảnh hiện tại | Ảnh preview, zoom fit/100%, điều hướng trước/sau | Empty, loading, image loaded, preview error |
| Panel AVIF | Chỉnh cấu hình nén ảnh | Output format, quality slider, resolution, advanced | Default, disabled, processing |
| Panel PDF | Chỉnh cấu hình PDF | Page mode, color mode, q slider | Default, warning, disabled, processing |
| PDF versions | So sánh bản PDF đã tạo | Danh sách q12/q14/final, size, nút mở, đặt final | Empty, loaded, warning |
| Log drawer/modal | Xem lỗi chi tiết | Log ngắn, copy log, mở folder log | Empty, error list |

## 4. Hướng UI

### Visual direction

Giao diện nên theo hướng sạch, chắc, giống tool làm việc nội bộ trên Windows. Không làm landing page, không dùng hero, không dùng trang trí lớn. Trọng tâm là file, preview, dung lượng và nút hành động.

### Bố cục chính

```text
┌────────────────────────────────────────────────────────────────────┐
│ Header: Image Optimizer                   [Chọn folder] [Mở output] │
├───────────────┬──────────────────────────────────┬─────────────────┤
│ Workflow       │ File list + Preview              │ Thiết lập        │
│ 1 Chọn ảnh     │                                  │ AVIF             │
│ 2 Nén AVIF     │ Preview ảnh lớn                  │ PDF              │
│ 3 Review       │                                  │ Kết quả          │
│ 4 Tạo PDF      │ Danh sách file / PDF versions    │                 │
│ 5 Xuất file    │                                  │                 │
└───────────────┴──────────────────────────────────┴─────────────────┘
```

Kích thước gợi ý cho màn `1366x768`:

- Header: `60px`.
- Sidebar trái: `180-220px`.
- Panel phải: `320-360px`, có scroll.
- Khu giữa: chiếm phần còn lại, ưu tiên preview ảnh.
- Min window đề xuất: `1200x720`.

### WinUI default styling

Không mô tả màu UI, hex color, brand color hoặc palette riêng trong plan WinUI.

Giao diện dùng mặc định của WinUI/Fluent:

- Native controls.
- Built-in hover/pressed/focus/selected states.
- System theme light/dark.
- `InfoBar`/`ContentDialog` cho warning/error thay vì label màu tự chế.

### Typography

WinUI nên dùng text style của Fluent và font hệ thống, ưu tiên:

- Tiêu đề header: `TitleTextBlockStyle` hoặc `SubtitleTextBlockStyle`.
- Section title: `BodyStrongTextBlockStyle`.
- Body/input/table: `BodyTextBlockStyle`.
- Helper text: `CaptionTextBlockStyle`.

Không thiết lập font size cứng. Để hệ thống Windows tự xử lý mật độ hiển thị theo cài đặt của người dùng.

### Button

- Primary: Dùng button mặc định/accent button của WinUI theo ngữ cảnh, không override màu riêng trong MVP.
- Secondary: Dùng button mặc định của WinUI (Standard Button) cho `Tạo lại`, `Mở folder`, `Xem log`.
- Warning action: Tránh custom button warning riêng rẽ, nên đặt action trong một `InfoBar`.
- Button disabled phải có tooltip giải thích lý do để tuân thủ accessibility.

### Trang trí

Chỉ dùng trang trí chức năng:

- Badge trạng thái nhỏ.
- Không dùng gradient, background phức tạp hoặc card lồng card.

## 5. Component frontend

### Layout components

- `MainWindow`: Cửa sổ gốc của app, chứa shell.
- `AppShellPage`: Chứa `NavigationView` cho hệ thống đa module.
- `ImagePdfOptimizerPage`: Feature host chính cho module 01.
- `AppHeader` (CommandBar hoặc Grid custom): Hành động nhanh, trạng thái tool.
- `WorkspaceGrid`: Cột/vùng chứa nội dung chính (preview, list).
- `SettingsScrollViewer`: Vùng thiết lập bên phải, bắt buộc cuộn.
- `JobProgressInfoBar`: Tiến trình công việc.

### Input/file components

- `DropZone`: chọn/kéo thả folder/file.
- `FileTable`: danh sách ảnh input/output.
- `FileStatusBadge`: hợp lệ, bỏ qua, lỗi, output nặng hơn gốc.
- `UnsupportedFileList`: danh sách file không hỗ trợ.

### Preview components

- `ImagePreview`: xem ảnh đang chọn.
- `PreviewToolbar`: fit, 100%, trước, sau.
- `EmptyPreview`: trạng thái chưa có ảnh.

### AVIF setting components

- `QualityStepperSlider`: dùng cho CRF/q.
- `ResolutionSelector`: giữ nguyên / 1920 / 2048 / 2560 / custom.
- `AdvancedOptionsPanel`: CRF, CPU effort.

### PDF setting components

- `PageModeSelector`: theo kích thước ảnh / Fit A4 / Full A4.
- `ColorModeSelector`: RGB / Gray.
- `GrayModeConfirmDialog`: cảnh báo mất màu dấu.
- `PdfQualitySlider`: chỉnh q PDF.
- `PdfVersionList`: danh sách PDF đã tạo.
- `FinalPdfBanner`: hiển thị bản final đã chọn.

### Feedback components

- `JobProgressPanel`: Dùng `ProgressBar` và text hiển thị tiến độ.
- `ToastNotification`: Thông báo đẩy (toast) hoặc `InfoBar` nhẹ gọn.
- `WarningInfoBar`: Dùng `InfoBar` với `Severity="Warning"`.
- `ErrorDialog`: Dùng `ContentDialog` để hiển thị lỗi chi tiết có nút xem log.
- `LogCenterDialog`: Dùng `ContentDialog` hoặc một Page riêng mở đè lên.

## 6. State và hành vi UI

### App state chính

```text
NoInput
InputLoaded
AvifConverting
AvifReady
PdfGenerating
PdfReady
FinalSelected
Error
```

### Quy tắc enable/disable

| Action | Điều kiện enabled |
| --- | --- |
| Nén AVIF | Có ít nhất 1 file ảnh hợp lệ và không có job đang chạy |
| Review AVIF | Đã convert thành công ít nhất 1 file |
| Tạo PDF | Có AVIF ready và không có job đang chạy |
| Tạo lại PDF | Có AVIF ready và đã chọn cấu hình PDF hợp lệ |
| Đặt làm final | Có ít nhất 1 PDF version |
| Mở output | Folder output tồn tại |
| Bật Gray | Luôn cho bật, nhưng phải confirm |

### Empty state

Khi chưa chọn file:

```text
Kéo thả folder ảnh vào đây
hoặc
[Chọn folder ảnh]

Hỗ trợ: jpg, jpeg, png, avif, bmp, tif, tiff
```

### Loading state

Khi đang xử lý:

- Hiển thị progress bar.
- Hiển thị file hiện tại.
- Hiển thị số lượng `12/45 ảnh`.
- Disable các action có thể gây chạy song song.
- Nếu hỗ trợ huỷ, dùng nút `Huỷ xử lý`.

### Warning state

Các warning bắt buộc:

- Output nặng hơn file gốc.
- Gray mode có thể mất màu con dấu.
- File không hỗ trợ bị bỏ qua.
- PDF đang lớn hơn mục tiêu.
- FFmpeg chưa cấu hình đúng.

### Error state

Lỗi phải dễ hiểu với người không kỹ thuật:

- "Không đọc được file ảnh này."
- "Không tạo được folder output."
- "Không tìm thấy FFmpeg."
- "Tạo PDF thất bại. Hãy xem log để biết chi tiết."

Không chỉ hiển thị raw exception.

## 7. Mock data và contract cần core/BE hỗ trợ

App desktop có thể không có backend, nhưng frontend vẫn cần contract rõ với tầng xử lý file/core service.

### Image item

```json
{
  "id": "img_001",
  "fileName": "page-001.jpg",
  "sourcePath": "C:\\Input\\page-001.jpg",
  "format": "jpg",
  "originalSizeBytes": 2450000,
  "outputPath": "C:\\Input\\compressed-avif\\page-001.avif",
  "outputSizeBytes": 180000,
  "status": "success",
  "warning": null,
  "errorMessage": null
}
```

### Job progress

```json
{
  "jobType": "convert_avif",
  "status": "running",
  "currentFile": "page-012.jpg",
  "processedCount": 12,
  "totalCount": 45,
  "message": "Đang nén ảnh 12/45"
}
```

### PDF version

```json
{
  "id": "pdf_q12_rgb",
  "fileName": "Sao ke GD-q12-rgb.pdf",
  "path": "C:\\Input\\pdf-output\\Sao ke GD-q12-rgb.pdf",
  "sizeBytes": 1400000,
  "jpegQ": 12,
  "colorMode": "rgb",
  "pageMode": "image_size",
  "isFinal": false,
  "warnings": []
}
```

### Cấu hình AVIF

```json
{
  "format": "avif",
  "crf": 24,
  "cpuUsed": 4,
  "maxLongEdge": 2048,
  "skipIfOutputLarger": true
}
```

### Cấu hình PDF

```json
{
  "pageMode": "image_size",
  "colorMode": "rgb",
  "jpegQ": 12
}
```

## 8. Cấu trúc màn hình WinUI đề xuất

```text
Window (MainWindow)
└── ShellPage (Page)
    └── NavigationView
        ├── NavigationViewItem (Module Image Optimizer)
        ├── NavigationViewItem (Module khác...)
        └── Frame (Feature Host)
            └── ImageOptimizerPage (Page)
                └── Grid (Root)
                    ├── Row 0: CommandBar (Header, Actions)
                    ├── Row 1: SplitView hoặc Grid chia cột
                    │   ├── Cột trái (Workspace)
                    │   │   ├── DropZoneControl
                    │   │   ├── ImagePreviewControl
                    │   │   └── ListView (FileTable với DataTemplate)
                    │   └── Cột phải (Settings)
                    │       └── ScrollViewer
                    │           └── StackPanel
                    │               ├── AvifSettingsControl
                    │               ├── PdfSettingsControl
                    │               └── PdfVersionListControl
                    └── Row 2: InfoBar (Warnings/Progress)
```

**Nguyên tắc layout XAML:**
- Không dùng các khái niệm của WinForms (như `TableLayoutPanel`, `Panel AutoScroll`, `SplitContainer`).
- Dùng `Grid` cho layout tổng có phân chia dòng, cột cố định hoặc co giãn (*).
- Dùng `StackPanel` cho layout dọc/ngang không quan tâm chia lưới.
- Bao quanh nội dung dài (như Settings) bằng `ScrollViewer`.
- Custom `ListView.ItemTemplate` để dựng các DataTemplate tái sử dụng cho từng file row/PDF version.

## 9. Checklist kiểm tra UI

- Không dùng palette/brand color riêng; giao diện bám WinUI default.
- Nền chính trắng, panel phụ xám nhạt, text dễ đọc.
- App dùng tốt ở `1366x768`, không mất button quan trọng.
- Panel phải scroll được khi nội dung dài.
- Chưa chọn file thì chỉ thấy action chọn/kéo thả, không có nút gây hiểu nhầm.
- Có file không hỗ trợ thì app vẫn xử lý file hợp lệ.
- Convert AVIF có progress rõ ràng.
- Sau convert, người dùng thấy tổng dung lượng gốc, AVIF và tỷ lệ tiết kiệm.
- File output nặng hơn file gốc có warning rõ.
- Chưa có AVIF thì không tạo PDF được.
- PDF mặc định dùng RGB và page mode theo kích thước ảnh.
- Bật Gray mode phải confirm.
- Mỗi lần tạo PDF tạo thêm một item trong danh sách PDF versions.
- Người dùng có thể mở từng PDF version và đặt một bản làm final.
- Slider PDF q có nhãn `Đẹp hơn` và `Nhẹ hơn`, không bắt người dùng hiểu q.
- Advanced setting có thể ẩn/hiện, không làm rối MVP.
- Lỗi FFmpeg/file/folder được hiển thị bằng tiếng Việt dễ hiểu.
- Log chi tiết có thể copy khi cần debug.

## 10. Handoff cho dev

Ưu tiên build MVP theo thứ tự:

1. Dựng layout chính: header, sidebar, workspace, settings panel.
2. Làm input folder/file, file list và empty/loading/error states.
3. Làm AVIF settings và progress convert.
4. Làm review dung lượng AVIF.
5. Làm PDF settings, q slider và tạo PDF.
6. Làm PDF versions list và đặt final.
7. Làm warning/confirm cho Gray mode và output nặng hơn gốc.
8. Polish spacing, disabled state và log dialog theo WinUI default.

Không làm target size auto optimize, WebP/Both, preview trước/sau, batch nhiều folder hoặc drag reorder nâng cao trong MVP đầu tiên.

## 11. Kiến trúc MVVM & WinUI

Mọi trạng thái hiển thị của UI phải được Bind từ **ViewModel**. 
- View (`.xaml`) chỉ làm việc binding dữ liệu và định nghĩa layout.
- Code-behind (`.xaml.cs`) tuyệt đối **không** được chứa business logic (ví dụ: quét file, gọi FFmpeg, tạo file Output). Chỉ dùng để nối các event UI đặc thù như Drag & Drop mà Binding không hỗ trợ tự nhiên.
- Các button sẽ Bind tới các `ICommand` (như `ConvertAvifCommand`) trên ViewModel.

Điều này đảm bảo cho UI Shell đẹp mà phần lõi nghiệp vụ vẫn độc lập đúng chuẩn kiến trúc của file `07_CORE_TECHNICAL_DIRECTION.md`.
