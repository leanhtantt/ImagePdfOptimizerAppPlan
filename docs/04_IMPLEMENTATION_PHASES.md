# Feature 01 Implementation Phases

> Phạm vi: các phase dưới đây chỉ áp dụng cho **Feature 01: Image -> AVIF -> PDF Optimizer**. Core app shell và module navigation xem trong `00_APP_SUITE_MASTER_PLAN.md`.

## Phase 0: Chuẩn bị app shell và module Feature 01

Mục tiêu:

- Tạo project `.NET 8 WinForms` theo hướng app nhiều tab/module.
- Tạo shell chung trước, không hardcode app chỉ có một feature.
- Tạo module `ImagePdfOptimizer`.
- Tạo model/core service skeleton.

Deliverables:

- App mở được.
- MainForm có tab/module navigation.
- Tab Feature 01 mở được.
- Các tab khác có thể là placeholder.
- Có folder `Shared`, `Infrastructure`, `Features/ImagePdfOptimizer`.

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

## Phase 3: WinForms UI shell

Mục tiêu:

- Dựng layout chính theo UI plan.

Deliverables:

- Header.
- Workflow sidebar.
- Workspace.
- Settings panel có scroll.
- Status bar.

Acceptance:

- Dùng tốt trên 1366x768.
- Button chính không bị khuất.
- Empty/loading/success/warning/error/disabled state cơ bản.

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

## Phase 5: Combine PDF

Mục tiêu:

- Tạo PDF từ ảnh đã nén.
- Mặc định giữ size/orientation ảnh.
- RGB mặc định.
- q slider.

Deliverables:

- Page mode selector.
- Color mode selector.
- PDF q stepper-slider.
- Tạo PDF.
- Output vào `pdf-output`.

Acceptance:

- Không ép A4 mặc định.
- Không dùng Gray mặc định.
- PDF giữ màu con dấu.
- Chỉnh q tạo được file dung lượng khác nhau.

## Phase 6: PDF versions và final

Mục tiêu:

- Lưu lịch sử PDF trong phiên làm việc.
- Chọn final.

Deliverables:

- `PdfVersionList`.
- Mở PDF.
- Mở folder output.
- Đặt final.
- Badge warning cho Gray.

Acceptance:

- Tạo q12, q14, q15 thấy đủ trong list.
- Chọn một bản làm final được.

## Phase 7: Warning, confirm, log

Mục tiêu:

- Làm app đủ an toàn cho người dùng không kỹ thuật.

Deliverables:

- Gray confirm dialog.
- Warning output nặng hơn gốc.
- Error box tiếng Việt.
- Log dialog.
- Copy log.

Acceptance:

- Không có lỗi quan trọng chỉ hiện raw exception.
- Gray mode không bật im lặng.

## Phase 8: QA thực tế

Mục tiêu:

- Test bằng bộ `Sao ke GD`.

Checklist:

- Convert AVIF chạy được.
- Combine PDF RGB chạy được.
- q12/q14/q15 ra size khác nhau.
- Kết quả đối chiếu thực tế: 10 JPG gốc khoảng 8.32 MiB, 10 AVIF khoảng 1.36 MiB, PDF q12/q13/q15 nằm trong khoảng đã kiểm chứng.
- Con dấu màu còn.
- Không ép A4.
- File gốc không đổi.
- App không treo khi xử lý.

## Phase 9: Polish MVP

Mục tiêu:

- Sửa UI và trải nghiệm cuối.

Tasks:

- Spacing.
- Button text.
- Tooltip disabled.
- Progress message.
- Version naming.
- Final PDF banner.
- Kiểm tra 1366x768.

## Phase 10: Sau MVP

Tính năng sau MVP:

- Target size auto optimize nếu V2 có yêu cầu.
- WebP/Both nếu V2 có nhu cầu tương thích định dạng khác.
- Batch nhiều folder.
- Preset theo loại tài liệu.
- Lưu preset người dùng.
- Installer.
- Context menu Windows.
