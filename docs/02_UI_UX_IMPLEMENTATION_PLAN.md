# Feature 01 UI/UX Implementation Plan

> Phạm vi: tài liệu này chỉ áp dụng cho **Feature 01: Image -> AVIF -> PDF Optimizer**. UI shell cấp app suite nằm ở `00_SUITE_UI_UX_SHELL_PLAN.md`.

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
Header
├── Workflow sidebar
├── Workspace: dropzone, preview, file list
└── Settings panel: AVIF/PDF settings, PDF versions, log
Status bar
```

Kích thước gợi ý:

- Header: 56-60px.
- Sidebar: 180-220px.
- Settings panel: 320-360px, AutoScroll.
- Workspace: chiếm phần còn lại.
- Min window: 1200x720.

## 3. Visual style

Màu:

| Vai trò | Hex | Dùng cho |
|---|---|---|
| Primary đỏ đậm | `#8B1E2D` | Header, button chính, step active |
| Primary hover | `#6F1724` | Hover/pressed |
| Primary soft | `#F8E8EA` | Dropzone hover, active nhẹ |
| Nền chính | `#FFFFFF` | Workspace |
| Nền phụ | `#F6F4F2` | Sidebar/panel |
| Border | `#E5E7EB` | Divider/input |
| Text chính | `#252525` | Label/title |
| Text phụ | `#6B7280` | Helper/metadata |
| Success | `#1F8A5B` | Hoàn tất |
| Warning | `#B7791F` | Cảnh báo |
| Error | `#C2410C` | Lỗi |
| Info | `#2563EB` | Link/info |

Font:

```text
Segoe UI
```

Không dùng chữ quá lớn. Đây là tool xử lý hồ sơ, cần mật độ thông tin vừa phải.

## 4. App states

App state chính:

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

- `MainForm`
- `AppHeader`
- `WorkflowSidebar`
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
- `PageModeSelector`
- `ColorModeSelector`
- `GrayModeConfirmDialog`
- `PdfQualitySlider`

### Feedback

- `ProgressPanel`
- `WarningBox`
- `ErrorBox`
- `ToastMessage`
- `LogDialog`
- `PdfVersionList`
- `FinalPdfBanner`

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
| PDF | JPEG q | 4-20 | 1-30 | Thấp hơn đẹp hơn |

WebP không thuộc MVP đầu tiên nên không dựng UI WebP/Both.

## 7. Enable/disable rules

| Action | Enabled khi |
|---|---|
| Nén AVIF | Có ít nhất 1 file ảnh hợp lệ và không có job chạy |
| Review AVIF | Có ít nhất 1 file convert thành công |
| Tạo PDF | Có AVIF ready và không có job chạy |
| Tạo lại PDF | Có AVIF ready và cấu hình PDF hợp lệ |
| Đặt final | Có ít nhất 1 PDF version |
| Mở output | Folder output tồn tại |
| Bật Gray | Cho bật nhưng phải confirm |

## 8. Warning bắt buộc

- Output nặng hơn file gốc.
- Gray mode có thể mất màu con dấu.
- File không hỗ trợ bị bỏ qua.
- Thiếu hoặc lỗi FFmpeg.
- Output đã tồn tại.

Warning phải có gợi ý xử lý, không chỉ báo lỗi.

## 9. PDF versions UI

Sau mỗi lần tạo PDF, thêm item:

```text
q12-rgb    1.40 MB    [Mở] [Đặt final]
q14-rgb    1.33 MB    [Mở] [Đặt final]
q15-rgb    1.21 MB    [Mở] [Đặt final]
final      1.40 MB    [Mở]
```

Nếu version dùng Gray:

```text
q12-gray   1.10 MB    Warning: Có thể mất màu con dấu
```

## 10. Checklist UI

- Dùng tốt trên 1366x768.
- Panel phải scroll được.
- Header không chiếm quá nhiều chiều cao.
- Button quan trọng không bị khuất.
- Empty state rõ ràng.
- Loading có progress.
- Error không chỉ hiển thị raw exception.
- Disabled button có tooltip hoặc helper text.
- Gray mode confirm trước khi bật.
- PDF versions list cập nhật sau mỗi lần tạo.
- File output nặng hơn gốc có warning rõ.
