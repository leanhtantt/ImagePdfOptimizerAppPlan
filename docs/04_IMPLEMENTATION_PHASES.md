# Feature 01 Implementation Phases: Image Optimizer

> Phạm vi: các phase dưới đây chỉ áp dụng cho **Feature 01: Image Optimizer**. Core app shell và module navigation xem trong `00_APP_SUITE_MASTER_PLAN.md`.
>
> Quyết định boundary mới xem `09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md`: gộp file và nén PDF không còn là phase nội bộ của Feature 01.

## Phase 0: Chuẩn bị app shell và module Feature 01

Mục tiêu:

- Tạo project `.NET 8 + WinUI 3 / Windows App SDK` theo hướng app nhiều tab/module.
- Tạo shell chung trước, không hardcode app chỉ có một feature.
- Tạo module `ImageOptimizer`.
- Tạo model/core service skeleton.

Deliverables:

- App mở được.
- `MainWindow` có `NavigationView`/module navigation và feature host.
- Tab Feature 01 mở được.
- Các tab khác có thể là placeholder.
- Có folder `Shared`, `Infrastructure`, `Features/ImageOptimizer`.

## Phase 1: Core file scan và FFmpeg

Mục tiêu:

- Scan folder ảnh.
- Detect FFmpeg bundled theo app/package.
- Tạo output folders.

Deliverables:

- Chọn folder.
- Hiển thị số ảnh hợp lệ.
- Hiển thị file không hỗ trợ.
- Báo lỗi nếu package thiếu hoặc lỗi FFmpeg bundled.

Acceptance:

- Folder có tiếng Việt vẫn scan đúng.
- File không hỗ trợ không làm app dừng.
- Không yêu cầu user cuối tự nhập path FFmpeg.

## Phase 2: Convert AVIF

Mục tiêu:

- Convert ảnh sang AVIF.
- Apply CRF/resolution.
- Hiển thị progress.

Deliverables:

- Nút `Nén AVIF`.
- Stepper-slider AVIF CRF.
- Resolution selector.
- Output `compressed-avif`.

Acceptance:

- File gốc không bị sửa.
- Output nặng hơn gốc có warning.
- Có tổng dung lượng trước/sau.

## Phase 3: WinUI UI shell

Mục tiêu:

- Dựng layout chính theo UI plan.

Deliverables:

- Shell `NavigationView`.
- Header/action area.
- Feature host render `ImageOptimizer` view.
- Workspace.
- Settings panel có scroll.
- Status/progress area.
- Dùng WinUI default resources/controls; không dựng palette riêng.

Acceptance:

- Dùng tốt trên 1366x768.
- Button chính không bị khuất.
- Empty/loading/success/warning/error/disabled state cơ bản.
- Shell và feature content dùng chung visual language, không để navigation/header Fluent nhưng list/settings là control thô.

## Phase 3.1: WinUI-content integration gate

Mục tiêu:

- Chặn tình trạng chỉ có vỏ WinUI còn ruột feature không theo Fluent/MVVM.

Deliverables:

- `DropZoneControl` dùng WinUI visual states.
- `FileTableView` dùng `ListView.ItemTemplate`, có file/path/size/delta/status/warning.
- `QualityStepperSlider` dùng cho AVIF CRF; PDF q thuộc `PDF Compressor`.
- Warning/error dùng `InfoBar`/`ContentDialog`.
- ViewModel expose state/commands trước khi nối FFmpeg thật.

Acceptance:

- Không có file scan/FFmpeg/PDF logic trong code-behind.
- Không có list file dạng text thô thay cho `ListView.ItemTemplate`.
- Không hardcode màu/style từng màn; dùng WinUI default controls trước.

## Phase 4: Preview và review AVIF

Mục tiêu:

- Xem ảnh và danh sách ảnh sau nén.

Deliverables:

- File table.
- Image preview.
- Size gốc/output.
- Status badge.
- Warning output nặng hơn gốc.

Acceptance:

- Người dùng biết ảnh nào nén thành công, ảnh nào lỗi, ảnh nào nặng hơn.

## Phase 5: Handoff sang File Merge / PDF Builder

Mục tiêu:

- Cho người dùng đi tiếp từ ảnh đã nén sang feature gộp file hoặc automation gộp và nén.
- Không gộp PDF trực tiếp trong màn Image Optimizer.

Deliverables:

- Top action `Gộp file`.
- Top action `Gộp và nén`.
- `FileBatchContext` chứa danh sách ảnh, output folder và suggested order.
- Điều hướng sang `File Merge / PDF Builder` với context.
- Automation `Gộp và nén`: Image Optimizer -> File Merge / PDF Builder -> PDF Compressor.

Acceptance:

- Gửi đúng batch ảnh đã chọn hoặc output `compressed-avif`.
- Người dùng quay lại Image Optimizer vẫn thấy state/list để nén lại.
- Không có nút `Nén` chung chung nhưng chạy nhiều bước ẩn.

## Phase 6: Warning, confirm, log

Mục tiêu:

- Làm app đủ an toàn cho người dùng không kỹ thuật.

Deliverables:

- Warning output nặng hơn gốc.
- Error box tiếng Việt.
- Log dialog.
- Copy log.

Acceptance:

- Không có lỗi quan trọng chỉ hiện raw exception.
- Output nặng hơn gốc có gợi ý xử lý rõ.

## Phase 7: QA thực tế

Mục tiêu:

- Test bằng bộ `Sao ke GD`.

Checklist:

- Convert AVIF chạy được.
- Kết quả đối chiếu thực tế: 10 JPG gốc khoảng 8.32 MiB, 10 AVIF khoảng 1.36 MiB.
- Handoff sang feature gộp/nén nhận đúng `compressed-avif`.
- File gốc không đổi.
- App không treo khi xử lý.

## Phase 8: Polish MVP

Mục tiêu:

- Sửa UI và trải nghiệm cuối.

Tasks:

- Spacing.
- Button text.
- Tooltip disabled.
- Progress message.
- Kiểm tra 1366x768.

## Phase 9: Sau MVP

Tính năng sau MVP:

- Implement đầy đủ `File Merge / PDF Builder`.
- Implement đầy đủ `PDF Compressor`.
- Target size auto optimize nếu V2 có yêu cầu.
- WebP/Both nếu V2 có nhu cầu tương thích định dạng khác.
- Batch nhiều folder.
- Preset theo loại tài liệu.
- Lưu preset người dùng.
- Installer.
- Context menu Windows.
