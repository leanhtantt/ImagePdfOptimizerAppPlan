# MVP Acceptance Checklist

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

## 5. Combine PDF

- [ ] Combine từ ảnh đã nén.
- [ ] Không ép A4 mặc định.
- [ ] Page mode mặc định theo kích thước ảnh.
- [ ] Ảnh dọc ra trang dọc.
- [ ] Ảnh ngang ra trang ngang.
- [ ] RGB là mặc định.
- [ ] Gray không bật mặc định.
- [ ] Bật Gray phải confirm.
- [ ] PDF giữ màu con dấu/chữ ký/logo.
- [ ] PDF q stepper-slider hoạt động.
- [ ] q giảm thì đẹp hơn/nặng hơn.
- [ ] q tăng thì nhẹ hơn/mềm hơn.

## 6. PDF versions

- [ ] Mỗi lần tạo PDF thêm một item trong list.
- [ ] List hiển thị q, color mode, page mode, dung lượng.
- [ ] Tạo được q12, q14, q15.
- [ ] Mở từng PDF version được.
- [ ] Đặt một bản làm final được.
- [ ] Final PDF hiển thị rõ.
- [ ] Gray version có warning badge.

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
- [ ] PDF RGB q12 nằm gần mốc đã kiểm chứng khoảng 1.40 MiB.
- [ ] PDF RGB q13 nằm gần mốc đã kiểm chứng khoảng 1.33 MiB.
- [ ] PDF RGB q15 nằm gần mốc đã kiểm chứng khoảng 1.21 MiB.
- [ ] Không mất con dấu màu.
- [ ] Không bị đặt vào trang A4 ngoài ý muốn.
- [ ] Có thể chỉnh q nhiều lần đến khi ưng ý.

## 10. Tiêu chí pass MVP

MVP pass khi toàn bộ mục bắt buộc sau đạt:

- [ ] Chọn folder.
- [ ] Convert AVIF.
- [ ] Review dung lượng.
- [ ] Combine PDF RGB.
- [ ] Chỉnh q.
- [ ] Tạo nhiều PDF versions.
- [ ] Chọn final.
- [ ] Không sửa file gốc.
- [ ] UI đủ state.
- [ ] Test thực tế với `Sao ke GD` đạt.
