# Feature 01 UI/UX Implementation Plan: Image Optimizer

> Phạm vi: tài liệu này chỉ áp dụng cho **Feature 01: Image Optimizer**. UI shell cấp app suite nằm ở `00_SUITE_UI_UX_SHELL_PLAN.md`.
>
> WinUI decision: khi implement bằng WinUI 3 / Windows App SDK, tài liệu này phải được đọc cùng `06_WINUI_UI_DIRECTION.md`. Các component dưới đây là intent UI; implementation cụ thể phải dùng WinUI native controls, `ThemeResource`, `DataTemplate` và MVVM để tránh vỏ WinUI nhưng ruột là layout/form tự chế.
>
> Quyết định boundary mới nằm ở `09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md`: Feature 01 không làm UI gộp PDF hoặc nén PDF. Các bước đó thuộc `File Merge / PDF Builder` và `PDF Compressor`.

## 1. Hướng UI

Đây là tool Windows nội bộ, không phải landing page. Giao diện cần sạch, chắc, thao tác nhanh, ưu tiên file list, preview, setting và kết quả dung lượng.

Ngôn ngữ UI: tiếng Việt có dấu.

Màn hình tối thiểu:

```text
1366 x 768
```

Panel phải có scroll để không mất button quan trọng.

## 2. Layout chính

```text
Shell NavigationView
├── Top action trong feature: Gộp file, Gộp và nén, Mở output
├── Workspace: dropzone hoặc ListView ảnh
└── Settings panel: thiết lập nén ảnh, summary sau nén
Global/status bar
```

Kích thước gợi ý:

- Header: 56-60px.
- Settings panel: 320-360px, AutoScroll.
- Workspace: chiếm phần còn lại.
- Min window: 1200x720.

## 3. Visual style

Không mô tả palette, hex color hoặc brand color riêng trong plan.

Vì app dùng WinUI 3, giao diện phải ưu tiên:

- Native WinUI controls.
- Fluent default theme.
- System text styles.
- Built-in hover/pressed/focus/selected states.
- Built-in accessibility và density của Windows.

Không tự định nghĩa màu header, màu button, màu sidebar, màu warning/error/success trong plan. Nếu cần trạng thái, dùng control WinUI đúng mục đích như `InfoBar`, `ProgressBar`, `ListView`, `ContentDialog`, `CommandBar`.

Không dùng chữ quá lớn. Đây là tool xử lý hồ sơ, cần mật độ thông tin vừa phải.

## 4. App states

App state chính:

```text
NoInput
InputLoaded
AvifConverting
AvifReady
HandoffReady
Error
```

State UI bắt buộc:

| State | Khi dùng |
|---|---|
| Empty | Chưa chọn folder/file |
| Loading | Đang scan, convert, combine |
| Success | Job hoàn tất |
| Warning | Output nặng hơn gốc, Gray, file bỏ qua, thiếu FFmpeg |
| Error | FFmpeg lỗi, file hỏng, ghi file lỗi |
| Disabled | Chưa đủ điều kiện thao tác |

## 5. Component chính

### Layout

- `MainWindow` / feature `UserControl`
- `AppHeader` hoặc shell header action area
- `WorkspacePanel`
- `SettingsPanel`
- `StatusBar`

### Input/file

- `DropZone`
- `FileTable`
- `FileStatusBadge`
- `UnsupportedFileList`

### Preview

- `ImagePreview`
- `PreviewToolbar`
- `EmptyPreview`

### Settings

- `QualityStepperSlider`
- `ResolutionSelector`
- `AdvancedOptionsPanel`

### Feedback

- `ProgressPanel`
- `WarningBox`
- `ErrorBox`
- `ToastMessage`
- `LogDialog`

## 6. Stepper-slider dùng chung

UI dạng:

```text
                         [giá trị hiện hành]
< nút giảm > [ Đẹp hơn ] ----o---- [ Nhẹ hơn ] < nút tăng >
```

Yêu cầu:

- Có số hiện hành.
- Nút tăng/giảm nhảy 2 đơn vị.
- Slider snap theo bước 2.
- Có preset nhanh.
- Có advanced input.
- Tooltip giải thích số thấp/cao ảnh hưởng thế nào.

Mapping:

| Bước | Giá trị | Dải nhanh | Advanced | Chiều chất lượng |
|---|---|---:|---:|---|
| AVIF | CRF | 18-36 | 0-40 | Thấp hơn đẹp hơn |
| PDF | JPEG q | 4-20 | 1-30 | Thuộc PDF Compressor, không nằm trong Feature 01 |

WebP không thuộc MVP đầu tiên nên không dựng UI WebP/Both.

## 6.1. WinUI implementation binding

Khi code bằng WinUI, các component ở mục trên phải map như sau:

| Component intent | WinUI binding bắt buộc | Không được làm |
|---|---|---|
| Module navigation | `NavigationView` ở shell | Sidebar tự vẽ không có selected/hover/focus Fluent |
| File table | `ListView` với `ItemTemplate` rõ thumbnail/name/size/status | List text thô chỉ hiển thị filename |
| Warning/error | `InfoBar` hoặc `ContentDialog` theo mức độ | Raw exception text hoặc label đỏ tự chế |
| CRF stepper-slider | Shared `UserControl` compose từ `Slider`, `NumberBox`, `Button` | Control rời rạc mỗi nơi một kiểu |
| Settings panel | `ScrollViewer` + section/card style từ resource dictionary | Panel cố định làm mất button ở 1366x768 |
| Handoff actions | CommandBar/Button rõ `Gộp file`, `Gộp và nén` | Nút chung chung `Nén` nhưng phía sau làm nhiều bước |

Trước khi implement một màn, phải xác định ViewModel state, command và DataTemplate tương ứng. Không đưa file scan, FFmpeg command, PDF generation vào code-behind.

## 7. Enable/disable rules

| Action | Enabled khi |
|---|---|
| Nén AVIF | Có ít nhất 1 file ảnh hợp lệ và không có job chạy |
| Review AVIF | Có ít nhất 1 file convert thành công |
| Gộp file | Có ảnh hợp lệ hoặc output AVIF ready |
| Gộp và nén | Có output AVIF ready và không có job chạy |
| Mở output | Folder output tồn tại |

## 8. Warning bắt buộc

- Output nặng hơn file gốc.
- File không hỗ trợ bị bỏ qua.
- Thiếu hoặc lỗi FFmpeg.
- Output đã tồn tại.

Warning phải có gợi ý xử lý, không chỉ báo lỗi.

## 9. Handoff UI

Sau khi có output ảnh đã nén, Feature 01 phải expose:

```text
Gộp file
Gộp và nén
```

`Gộp file` điều hướng sang `File Merge / PDF Builder` với batch ảnh hiện tại.

`Gộp và nén` chạy automation sang `File Merge / PDF Builder`, sau đó sang `PDF Compressor` và auto preview PDF.

Image Optimizer vẫn phải giữ state riêng để người dùng quay lại nén ảnh tiếp bất cứ lúc nào.

## 10. Checklist UI

- Dùng tốt trên 1366x768.
- Panel phải scroll được.
- Header không chiếm quá nhiều chiều cao.
- Button quan trọng không bị khuất.
- Empty state rõ ràng.
- Loading có progress.
- Error không chỉ hiển thị raw exception.
- Disabled button có tooltip hoặc helper text.
- File output nặng hơn gốc có warning rõ.
- Handoff sang feature gộp/nén rõ ràng, không giấu sau một nút `Nén`.
