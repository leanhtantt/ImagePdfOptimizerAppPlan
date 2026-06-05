# MVP Acceptance Checklist: Image Optimizer

> Quyết định boundary mới: xem `09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md`. Checklist này áp dụng cho Feature 01 Image Optimizer. Gộp file và nén PDF không còn là tiêu chí pass trực tiếp của Feature 01.

## 0. WinUI/core alignment

- [ ] App dùng WinUI 3 / Windows App SDK nếu đã chọn hướng WinUI.
- [ ] Shell dùng module navigation/feature host, không hardcode riêng Feature 01 vào toàn bộ app.
- [ ] Module navigation dùng `NavigationView` hoặc pattern WinUI tương đương.
- [ ] Feature content dùng `ListView.ItemTemplate`, `InfoBar`, `ContentDialog`, shared `QualityStepperSlider` và resource dictionary; không chỉ có vỏ shell Fluent.
- [ ] ViewModel chỉ giữ state/commands; code-behind không build FFmpeg command hoặc xử lý workflow nghiệp vụ.
- [ ] Core/infrastructure không reference WinUI.

## 1. File input

- [ ] Chọn folder ảnh được.
- [ ] Kéo thả folder/file được.
- [ ] App nhận đúng `jpg, jpeg, png, avif, bmp, tif, tiff`.
- [ ] File không hỗ trợ được liệt kê rõ.
- [ ] Folder có dấu tiếng Việt xử lý được.
- [ ] File gốc không bị sửa.

## 2. FFmpeg bundled

- [ ] Package/app có sẵn FFmpeg bundled, ví dụ `tools/ffmpeg/bin/ffmpeg.exe`.
- [ ] App dùng FFmpeg bundled, không yêu cầu user tự cài FFmpeg.
- [ ] App không yêu cầu user nhập path FFmpeg thủ công trong MVP.
- [ ] Nếu FFmpeg bundled thiếu/lỗi, UI báo lỗi dễ hiểu rằng bản cài đặt thiếu thành phần xử lý.
- [ ] Log command/exit code khi lỗi.

## 3. Convert AVIF

- [ ] Convert AVIF được.
- [ ] CRF stepper-slider hoạt động.
- [ ] CRF giảm thì đẹp hơn/nặng hơn.
- [ ] CRF tăng thì nhẹ hơn/mềm hơn.
- [ ] CRF 0 là lossless AVIF.
- [ ] Resolution selector hoạt động.
- [ ] Không upscale ảnh nhỏ.
- [ ] Output nặng hơn gốc có warning.
- [ ] Có option bỏ qua output nặng hơn gốc.

## 4. Review sau nén

- [ ] Hiển thị tổng dung lượng gốc.
- [ ] Hiển thị tổng dung lượng output.
- [ ] Hiển thị tỷ lệ tiết kiệm.
- [ ] File list có status success/warning/error.
- [ ] Preview ảnh đang chọn.
- [ ] Có thể tạo lại AVIF với cấu hình khác.

## 5. Handoff sang feature gộp/nén

- [ ] Có top action `Gộp file` khi có file ảnh hợp lệ hoặc output AVIF.
- [ ] `Gộp file` gửi batch sang `File Merge / PDF Builder`.
- [ ] Có top action `Gộp và nén` khi đã có output AVIF.
- [ ] `Gộp và nén` chạy automation: Image Optimizer -> File Merge / PDF Builder -> PDF Compressor.
- [ ] Automation không làm mất state của Image Optimizer; người dùng quay lại vẫn thấy list ảnh và có thể nén lại.
- [ ] Handoff dùng context rõ ràng, không gọi UI của feature khác trực tiếp.
- [ ] Không đặt một nút `Nén` nhưng phía sau vừa gộp vừa nén PDF.

## 6. Scope chuyển sang feature khác

Các mục dưới đây không còn là tiêu chí pass của Image Optimizer, nhưng phải được giữ làm acceptance cho `File Merge / PDF Builder` và `PDF Compressor`:

- [ ] Gộp từ ảnh đã nén.
- [ ] Không ép A4 mặc định.
- [ ] Page mode mặc định theo kích thước ảnh.
- [ ] Ảnh dọc ra trang dọc, ảnh ngang ra trang ngang.
- [ ] RGB là mặc định.
- [ ] Gray không bật mặc định.
- [ ] Bật Gray phải confirm.
- [ ] PDF giữ màu con dấu/chữ ký/logo.
- [ ] PDF q stepper-slider hoạt động.
- [ ] Mỗi lần tạo/nén PDF thêm một version.
- [ ] Tạo được q12, q14, q15.
- [ ] Mở từng PDF version được.
- [ ] Đặt một bản làm final được.

## 7. UI state

- [ ] Empty state rõ khi chưa chọn file.
- [ ] Loading state có progress.
- [ ] Success state có kết quả và bước tiếp theo.
- [ ] Warning state có lý do và gợi ý xử lý.
- [ ] Error state dễ hiểu, không chỉ raw exception.
- [ ] Disabled state đúng ngữ cảnh.
- [ ] Disabled button có tooltip/helper.

## 8. Layout

- [ ] Chạy tốt trên 1366x768.
- [ ] Panel phải scroll được.
- [ ] Button chính không bị khuất.
- [ ] Text không tràn khỏi control.
- [ ] Header không quá cao.
- [ ] Preview/list còn đủ không gian làm việc.

## 9. Bộ test Sao ke GD

- [ ] Dùng được folder `Sao ke GD`.
- [ ] Dùng được folder `Sao ke GD\compressed-avif`.
- [ ] 10 ảnh JPG gốc được scan đúng.
- [ ] 10 ảnh AVIF được tạo/đọc đúng.
- [ ] Tổng AVIF sau nén nằm gần mốc đã kiểm chứng khoảng 1.36 MiB.
- [ ] Handoff sang feature gộp/nén dùng được với folder `Sao ke GD\compressed-avif`.

Các mốc PDF RGB q12/q13/q15 thuộc checklist của `File Merge / PDF Builder` và `PDF Compressor`.

## 10. Tiêu chí pass MVP

MVP pass khi toàn bộ mục bắt buộc sau đạt:

- [ ] Chọn folder.
- [ ] Convert AVIF.
- [ ] Review dung lượng.
- [ ] Nén lại ảnh với setting khác được.
- [ ] Có handoff `Gộp file`.
- [ ] Có handoff/automation `Gộp và nén`.
- [ ] Không sửa file gốc.
- [ ] UI đủ state.
- [ ] Test thực tế với `Sao ke GD` đạt.
