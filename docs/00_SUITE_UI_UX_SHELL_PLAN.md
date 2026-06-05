# Suite UI/UX Shell Plan: File Utility Hub

## 1. Vai trò của tài liệu này

Tài liệu này định nghĩa UI/UX cấp app suite cho `FileUtilityHub`, tức phần shell chung bao quanh các feature module.

Nó không thay thế UI plan của Feature 01. Feature 01 vẫn có UI plan riêng:

```text
02_UI_UX_IMPLEMENTATION_PLAN.md
```

Quyết định boundary mới phải đọc cùng:

```text
09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md
```

## 2. Nguyên tắc UI cấp suite

App là bộ công cụ xử lý file local trên Windows. UI cấp suite phải:

- Cho người dùng biết họ đang ở feature nào.
- Cho chuyển tab/module rõ ràng.
- Hiển thị tình trạng tool chung như FFmpeg.
- Quản lý job/progress/log chung.
- Cho các feature dùng chung shell mà không phải tự dựng lại header/status/log.
- Không để Feature 01 chi phối toàn bộ layout của app.

## 2.1. WinUI shell decision

Nếu implement theo WinUI 3 / Windows App SDK, shell phải dùng native Fluent pattern làm nền:

- Module navigation ưu tiên `NavigationView`.
- Global warning/error ưu tiên `InfoBar`.
- Confirm/settings/log ưu tiên `ContentDialog` hoặc page/flyout WinUI.
- Ưu tiên WinUI default resources/control style; chỉ thêm shared resources khi thật sự cần dùng lại hành vi/component.

Feature content render trong shell cũng phải theo cùng visual language. Không chấp nhận shell nhìn Fluent nhưng nội dung module là list/label/button tự chế rời rạc.

## 3. Layout shell tổng

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ Header: File Utility Hub       [Tool status] [Settings] [Log Center]         │
├──────────────────┬───────────────────────────────────────────────────────────┤
│ Module Navigation│ Feature Host                                              │
│ - Image Optimizer│                                                           │
│ - File Merge     │                                                           │
│ - PDF Compressor │ Nội dung feature đang chọn                                │
│ - PDF Converter  │                                                           │
│ - PDF Split      │                                                           │
│ - Word Tools     │                                                           │
│ - Excel Tools    │                                                           │
├──────────────────┴───────────────────────────────────────────────────────────┤
│ Global Status Bar: job hiện tại, progress, warning/error ngắn                │
└──────────────────────────────────────────────────────────────────────────────┘
```

## 4. Header app chung

Header cấp suite gồm:

- App name: `File Utility Hub`.
- Feature hiện tại.
- Tool status badge:
  - FFmpeg OK.
  - FFmpeg missing.
  - Codec support warning.
- Nút `Settings`.
- Nút `Log Center`.
- Nút `Open Output` nếu feature hiện tại có output folder.

Header không chứa setting chi tiết của từng feature. Setting chi tiết nằm trong feature host.

## 5. Module navigation

Navigation dạng sidebar hoặc tab dọc.

Modules dự kiến:

| Module | Label UI | Trạng thái MVP |
|---|---|---|
| ImageOptimizer | Tối ưu ảnh | Active, làm trước |
| FileMergePdfBuilder | Gộp file | Placeholder sau Feature 01 |
| PdfCompressor | Nén PDF | Placeholder |
| PdfConverter | Convert PDF | Placeholder |
| PdfSplit | Tách PDF | Placeholder |
| WordTools | Word Tools | Placeholder |
| ExcelTools | Excel Tools | Placeholder |
| BatchWorkspace | Batch Workspace | Placeholder |

Placeholder module phải hiển thị rõ:

```text
Tính năng này sẽ được phát triển sau Feature 01.
```

Không để placeholder trông như lỗi hoặc thiếu dữ liệu.

## 6. Feature host

Feature host là vùng render nội dung feature đang chọn.

Mỗi feature module phải expose:

- Tên feature.
- Icon hoặc label ngắn.
- Root control để render.
- Danh sách action chính nếu cần đưa lên header.
- Output folder hiện tại nếu có.
- Job state hiện tại.
- Warning/error summary.

Contract đề xuất:

```csharp
public interface IFeatureModule
{
    string Id { get; }
    string DisplayName { get; }
    Control CreateView();
    FeatureState GetState();
    IReadOnlyList<FeatureAction> GetHeaderActions();
}
```

## 7. Global settings

Global settings dùng cho toàn app:

- FFmpeg bundled status.
- Default output behavior:
  - hỏi overwrite.
  - tự tạo tên mới.
  - bỏ qua file tồn tại.
- Log folder.
- Theme cơ bản nếu cần.
- Ngôn ngữ UI, mặc định tiếng Việt.

Không đưa setting riêng như AVIF CRF hoặc PDF q vào global settings.

## 8. Tool status

Tool status hiển thị tình trạng công cụ ngoài:

- FFmpeg found.
- FFmpeg bundled path nội bộ.
- FFmpeg version.
- AVIF encoder support.
- PDF/image decode support.

Nếu thiếu FFmpeg:

- Badge header chuyển warning.
- Feature cần FFmpeg sẽ disable action xử lý.
- Báo rõ bản cài đặt bị thiếu thành phần xử lý và hướng dẫn cài lại/cập nhật app.
- Không hỏi người dùng cuối paste path FFmpeg trong MVP.

## 9. Job queue/progress chung

MVP có thể chỉ chạy một job tại một thời điểm, nhưng UI shell nên chuẩn bị theo hướng job chung.

Global status bar hiển thị:

- Job đang chạy.
- Feature đang chạy job.
- File hiện tại.
- Progress count.
- Nút huỷ nếu job hỗ trợ.

Ví dụ:

```text
Đang nén AVIF trong Ảnh -> PDF nhẹ: 8/20 file
```

Rule:

- Không chạy song song hai job nặng trong MVP.
- Khi có job đang chạy, disable action gây xung đột ở feature khác.
- Người dùng vẫn có thể xem log/status.

## 10. Log center chung

Log Center gom log từ mọi feature:

- Job logs.
- FFmpeg command.
- Exit code.
- Error summary.
- File lỗi.
- Copy log.
- Open log folder.

Không feature nào tự tạo log UI riêng hoàn toàn tách biệt. Feature có thể có log summary, nhưng log chi tiết phải đi về Log Center.

## 11. Warning/error chung

Warning/error cấp suite:

- Thiếu FFmpeg.
- Không ghi được output folder.
- Không có quyền truy cập file.
- Job đang chạy.
- Tool external lỗi.

Feature-specific warning vẫn nằm trong feature, nhưng shell có thể hiển thị summary.

## 12. Shared controls cấp suite

Các control dùng chung:

- `ModuleNavigation`.
- `ToolStatusBadge`.
- `GlobalStatusBar`.
- `JobProgressBar`.
- `LogCenterDialog`.
- `SettingsDialog`.
- `DropZone`.
- `FileTable`.
- `StepperSlider`.
- `WarningBox`.
- `ErrorBox`.
- `StatusBadge`.

Feature 01 được dùng các control này, nhưng không được làm control chỉ biết riêng AVIF/PDF.

## 13. Layout tối thiểu

Shell phải dùng tốt trên:

```text
1366 x 768
```

Yêu cầu:

- Header không quá 60px.
- Module navigation không quá rộng.
- Feature host co giãn tốt.
- Global status bar không che nội dung.
- Dialog settings/log có scroll nếu nội dung dài.

## 14. Visual direction cấp suite

Không định nghĩa palette, hex color hoặc brand color riêng trong shell plan.

Shell dùng WinUI native controls và Fluent default theme:

- `NavigationView` cho module navigation.
- `CommandBar` hoặc header action area theo WinUI.
- `InfoBar` cho warning/error/info.
- `ProgressBar`/`ProgressRing` cho job progress.
- `ContentDialog` cho confirm/settings/log.

Không mỗi feature tự chọn visual system riêng. Mỗi feature phải dùng control mặc định và trạng thái mặc định của WinUI, chỉ custom khi có lý do chức năng rõ ràng.

## 15. MVP shell acceptance

Shell đạt MVP khi:

- App mở với tên `File Utility Hub` hoặc tên tạm tương đương.
- Có module navigation.
- Feature 01 render trong feature host.
- Các module khác có placeholder.
- Header có tool status.
- Có tool status cho FFmpeg bundled.
- Có global status bar.
- Có log center.
- Job chạy trong Feature 01 cập nhật progress lên shell.
- Dùng tốt trên 1366x768.

## 16. Quan hệ với Feature 01

Feature 01 được implement đầu tiên nhưng chỉ là một module.

Không được:

- Đặt tên solution/project theo riêng `ImageOptimizer` nếu đang build app suite.
- Hardcode shell chỉ có Feature 01.
- Đưa CRF/q/resolution vào global settings.
- Để Feature 01 tự quản lý toàn bộ app status/log nếu shell đã có phần chung.

Được:

- Dùng Feature 01 làm module đầu tiên để kiểm chứng shell.
- Dùng bộ `Sao ke GD` để QA module đầu tiên.
- Reuse shared controls từ Feature 01 cho các module sau.
