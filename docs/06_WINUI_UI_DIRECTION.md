# WinUI UI Direction: Fluent App Shell và Feature UI

## 1. Vai trò của tài liệu này

Tài liệu này bổ sung định hướng nếu app không đi theo MVP WinForms tối giản mà chuyển sang **WinUI 3 / Windows App SDK** ngay từ đầu.

Mục tiêu không phải chỉ đổi framework UI. Mục tiêu là thiết kế giao diện theo hướng:

```text
native Windows
Fluent
module-based
clean tool UI
không hardcode một feature
```

Tài liệu này áp dụng cho app suite `File Utility Hub` và đặc biệt là cách dựng UI cho Feature 01 `Image -> AVIF -> PDF Optimizer` trong WinUI.

## 2. Kết luận chính

WinUI không tự làm app đẹp chỉ vì dùng template WinUI.

WinUI cung cấp:

- Native Fluent controls.
- Theme resources.
- Visual states.
- Accessibility/focus/keyboard behavior cơ bản.
- Styling system bằng XAML.

Nhưng app có đẹp như Windows hay không phụ thuộc vào việc:

- Dùng đúng control WinUI có sẵn.
- Compose layout đúng bằng XAML.
- Dùng `ThemeResource` thay vì hardcode màu/font quá nhiều.
- Thiết kế `DataTemplate` cho list/card tử tế.
- Có spacing, typography, visual hierarchy nhất quán.
- Tách shell UI, shared controls và feature UI rõ ràng.

Câu ngắn gọn:

```text
WinUI cho bộ xương Fluent; app đẹp hay không nằm ở cách compose XAML, binding data, style resource và component design.
```

## 3. Không cần kéo toàn bộ component WinUI về

Không cần copy source hoặc tự viết lại toàn bộ component của WinUI.

Khi dùng WinUI 3 / Windows App SDK, app nên dùng các control native có sẵn như:

- `NavigationView`
- `ListView`
- `GridView`
- `ItemsRepeater`
- `CommandBar`
- `InfoBar`
- `ProgressBar`
- `ProgressRing`
- `ContentDialog`
- `TeachingTip`
- `NumberBox`
- `Slider`
- `ComboBox`
- `ToggleSwitch`
- `ScrollViewer`
- `Expander`

Nguyên tắc:

- Control phổ thông thì dùng native WinUI control.
- Component domain-specific thì tự viết `UserControl`, nhưng compose từ WinUI primitives.
- Không tự vẽ lại những thứ WinUI đã làm tốt như navigation, dialog, warning bar, progress, list container.

## 4. Vì sao khung giống nhưng bên trong vẫn không giống Windows

Một màn hình WinUI thường có ba lớp:

### 4.1. App shell

Gồm:

- Window.
- Header.
- Navigation.
- Feature host.
- Global status/log/warning.

Nếu chỉ dựng được shell giống Windows nhưng feature content bên trong sơ sài, app vẫn nhìn không native.

### 4.2. Native controls

Ví dụ `ListView`, `NavigationView`, `InfoBar`, `CommandBar`.

Các control này có sẵn:

- Hover state.
- Pressed state.
- Selected state.
- Focus visual.
- Keyboard behavior.
- Theme integration.

Đây là phần nên tận dụng thay vì tự custom từ đầu.

### 4.3. Data template và composition bên trong

Đây là phần quyết định app nhìn chuyên nghiệp hay không.

Ví dụ `ListView` chỉ cung cấp list container và item behavior. Nội dung từng row vẫn phải tự thiết kế:

- Thumbnail.
- File name.
- Dung lượng gốc/output.
- Status badge.
- Warning icon.
- Progress mini.
- Action buttons.
- Padding/spacing.
- Text trimming.

Nếu row template quá thô, app vẫn xấu dù đang dùng WinUI.

## 5. Control mapping cho File Utility Hub

### 5.1. App suite shell

| Khu vực | Control/Pattern WinUI đề xuất | Ghi chú |
|---|---|---|
| Main window | `Window` + root `Grid` | Có thể bổ sung custom title bar sau |
| Module navigation | `NavigationView` | Dùng cho Image PDF, PDF Compressor, Converter, Combine/Split, Word, Excel |
| Feature host | `Frame` hoặc `ContentControl` | Render module đang chọn |
| Header actions | `CommandBar` hoặc `Grid` custom | Tool status, settings, log center, open output |
| Global warning | `InfoBar` | FFmpeg missing, codec warning, job error |
| Global progress | `ProgressBar` | Job hiện tại |
| Settings dialog | `ContentDialog` | Global settings |
| Log center | `ContentDialog` hoặc page/flyout riêng | Có copy log |

### 5.2. Feature 01 UI

| Nhu cầu | Control/Pattern WinUI đề xuất | Ghi chú |
|---|---|---|
| Dropzone | `Border` + drag/drop events + visual states | Domain-specific `DropZoneControl` |
| File list | `ListView` | Dễ selection, keyboard, template |
| Large list custom | `ItemsRepeater` | Dùng nếu cần tối ưu layout sâu hơn |
| Preview ảnh | `Image` trong `ScrollViewer` | Có zoom/pan sau MVP nếu cần |
| Settings panel | `ScrollViewer` + `StackPanel`/`Grid` | Không để mất button ở màn 1366x768 |
| CRF/q control | `Slider` + `NumberBox` + `Button` | Gói thành shared `QualityStepperSlider` |
| Preset | `RadioButtons`, `ComboBox` hoặc pill buttons custom | Tuỳ mật độ UI |
| Warning | `InfoBar` | Output nặng hơn gốc, Gray warning |
| Error | `InfoBar` hoặc `ContentDialog` | Không chỉ show raw exception |
| PDF versions | `ListView` hoặc card list | Có q, size, color mode, final badge |
| Final PDF banner | `InfoBar` hoặc custom card | Hiển thị bản final rõ |
| Progress | `ProgressBar` + text | Hiển thị file hiện tại |

## 6. Component nên tự viết

Các component dưới đây nên là `UserControl` riêng vì gắn với nghiệp vụ app:

- `DropZoneControl`
- `FileTableView`
- `ImagePreviewPanel`
- `QualityStepperSlider`
- `ResolutionSelector`
- `PageModeSelector`
- `ColorModeSelector`
- `PdfVersionList`
- `PdfVersionCard`
- `FinalPdfBanner`
- `ToolStatusBadge`
- `UnsupportedFileList`
- `JobProgressPanel`

Nhưng các component này không nên tự vẽ UI từ đầu. Nên compose từ:

- `Grid`
- `StackPanel`
- `Border`
- `TextBlock`
- `Button`
- `FontIcon`
- `ListView`
- `InfoBar`
- `Slider`
- `NumberBox`
- `ProgressBar`

## 7. Component không nên tự viết lại

Không nên tự viết lại các control phổ thông này nếu không có lý do rất rõ:

- Navigation sidebar.
- Dialog confirm.
- List container cơ bản.
- Progress indicator.
- Combo box.
- Toggle switch.
- Text input/number input.
- Warning/info bar.

Ưu tiên dùng control WinUI để nhận sẵn Fluent state, accessibility và theme integration.

## 8. ListView: điểm dễ hiểu nhầm nhất

`ListView` có thể nhìn đẹp hoặc xấu tuỳ `ItemTemplate`.

`ListView` lo phần:

- Selection.
- Hover.
- Focus.
- Keyboard navigation.
- Scroll behavior.
- Container styling.

Nhưng row item cần tự thiết kế.

Một row file trong app nên có tối thiểu:

```text
[thumbnail]  FileName.ext
             Original 8.32 MB -> AVIF 1.36 MB, saved 83%
             warning nếu có
                                      [status badge] [open]
```

Gợi ý row template:

- Row padding: 10-12px ngang, 8px dọc.
- Thumbnail: 40-48px, bo góc 6-8px.
- File name: `BodyTextBlockStyle`, ellipsis nếu dài.
- Metadata: `CaptionTextBlockStyle`, màu secondary.
- Status badge: pill nhỏ, màu semantic.
- Warning: icon + text ngắn, không chiếm quá nhiều chiều cao.
- Không dùng border đậm cho từng row; để selected/hover state của ListView làm việc.

## 9. ThemeResource và design tokens

Để app nhìn native hơn, nên ưu tiên WinUI theme resources:

```xml
Foreground="{ThemeResource TextFillColorPrimaryBrush}"
Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
```

Text style nên dùng:

```xml
Style="{ThemeResource TitleTextBlockStyle}"
Style="{ThemeResource BodyTextBlockStyle}"
Style="{ThemeResource CaptionTextBlockStyle}"
```

Brand color đỏ đậm của app vẫn có thể dùng cho:

- Primary action.
- Active workflow step.
- Feature accent.
- Header highlight nếu cần.

Nhưng không nên hardcode tất cả background/text/border theo bảng màu riêng. Làm vậy sẽ khiến app ít native hơn và khó hỗ trợ light/dark theme.

## 10. Resource dictionary đề xuất

Nên có resource dictionary để quản lý style/token:

```text
Resources
├── AppThemeResources.xaml
├── Spacing.xaml
├── Typography.xaml
├── SemanticBrushes.xaml
├── ControlStyles.xaml
└── Icons.xaml
```

Vai trò:

- `AppThemeResources.xaml`: brand color, theme override tối thiểu.
- `Spacing.xaml`: spacing chung như 4, 8, 12, 16, 24.
- `Typography.xaml`: style text dùng lại nếu cần.
- `SemanticBrushes.xaml`: success/warning/error/info brushes.
- `ControlStyles.xaml`: style cho badge, card, primary button.
- `Icons.xaml`: icon key dùng chung.

## 11. Layout direction cho WinUI

Layout suite nên giữ mental model:

```text
Window
└── Root Grid
    ├── NavigationView
    │   └── Feature Host
    └── Global overlays/dialogs/toasts nếu cần
```

Feature 01 nên có layout:

```text
Feature Root Grid
├── Top workflow/status strip
├── Main content grid
│   ├── Workspace
│   │   ├── Dropzone hoặc preview
│   │   └── File list
│   └── Settings panel scrollable
└── Bottom progress/log summary nếu cần
```

Nguyên tắc:

- Settings panel phải scroll.
- Primary button luôn dễ thấy.
- File list và preview phải có đủ diện tích.
- Không nhồi quá nhiều advanced option lên màn đầu.
- Warning/error đặt gần nơi người dùng cần quyết định.

## 12. State UI bắt buộc

Các state cần có trong WinUI cũng giống plan hiện tại:

- `NoInput`
- `InputLoaded`
- `AvifConverting`
- `AvifReady`
- `PdfGenerating`
- `PdfReady`
- `FinalSelected`
- `Error`

Mỗi state cần xác định:

- Control nào enabled/disabled.
- Primary action hiện là gì.
- Message nào hiển thị.
- Progress có determinate hay indeterminate.
- Warning nào cần giữ trên màn.

Không để app chỉ disable button mà không giải thích. Disabled button nên có helper text hoặc tooltip.

## 13. WinUI và MVVM

Nếu chọn WinUI, nên đi theo MVVM đủ nghiêm túc.

Tối thiểu nên có:

- View: XAML/UserControl.
- ViewModel: state, command, binding property.
- Service: xử lý file/FFmpeg/PDF.
- Model: dữ liệu thuần.

Không nên nhồi process runner, file scan, convert logic vào code-behind của XAML.

Code-behind chỉ nên lo:

- Wiring UI-specific events khó binding, ví dụ drag/drop.
- Small visual behavior.
- Call command từ ViewModel nếu cần.

## 14. Quy tắc thiết kế để app nhìn giống Windows

Checklist nhanh:

- Dùng `NavigationView` cho suite navigation.
- Dùng `InfoBar` cho warning/error/info.
- Dùng `ContentDialog` cho confirm quan trọng như bật Gray.
- Dùng `ThemeResource` cho màu/text/border nền tảng.
- Dùng `ListView.ItemTemplate` tử tế cho file rows.
- Dùng spacing 4/8/12/16/24 nhất quán.
- Dùng `TextTrimming="CharacterEllipsis"` cho file name/path.
- Không dùng font quá lớn.
- Không hardcode quá nhiều hex color.
- Không custom lại control phổ thông.
- Không để settings panel làm mất workspace.
- Không để error chỉ là raw exception.

## 15. Những rủi ro khi chọn WinUI ngay

WinUI hợp định hướng app suite lâu dài, nhưng cần chấp nhận:

- XAML layout cần nhiều công hơn WinForms.
- Binding/MVVM phải làm cẩn thận.
- Packaging Windows App SDK cần setup kỹ.
- Custom title bar/Mica/Acrylic không nên làm quá sớm nếu chưa ổn core workflow.
- Một số control domain-specific vẫn phải tự compose.
- UI đẹp phụ thuộc nhiều vào template/style, không tự xuất hiện chỉ vì đổi framework.

## 16. Khuyến nghị chốt

Nếu mục tiêu là app nội bộ nhanh nhất, WinForms vẫn đơn giản.

Nếu mục tiêu là app suite dùng lâu dài, nhìn native Windows hơn và có nhiều module sau này, WinUI 3 là hướng hợp lý hơn.

Khi chọn WinUI, không nên nghĩ là “kéo component về cho đẹp”. Nên nghĩ là:

```text
Dùng native WinUI controls cho nền tảng.
Tự viết UserControl cho nghiệp vụ.
Quản lý style bằng ThemeResource/resource dictionary.
Thiết kế DataTemplate tử tế cho list/card.
Giữ core logic tách khỏi UI.
```
