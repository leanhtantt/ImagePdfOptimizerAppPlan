# WinUI Implementation Handoff: Checklist cho agent/code UI

## 1. Vai trò của tài liệu này

Tài liệu này dùng làm checklist bắt buộc trước khi giao một agent khác bắt đầu code UI WinUI.

Mục tiêu là tránh lỗi:

```text
vỏ app nhìn WinUI/Fluent
nhưng ruột feature vẫn là list/label/button thô, code-behind ôm nghiệp vụ, mỗi màn một style
```

Nếu giao việc cho Gemini/Codex/agent khác, hãy yêu cầu agent đọc file này cùng các file:

```text
00_APP_SUITE_MASTER_PLAN.md
00_SUITE_UI_UX_SHELL_PLAN.md
02_UI_UX_IMPLEMENTATION_PLAN.md
03_TECHNICAL_ARCHITECTURE.md
04_IMPLEMENTATION_PHASES.md
05_MVP_ACCEPTANCE_CHECKLIST.md
06_WINUI_UI_DIRECTION.md
07_CORE_TECHNICAL_DIRECTION.md
09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md
```

## 2. Câu trả lời ngắn: đọc hai file mới thôi đã đủ chưa?

Chưa đủ.

`06_WINUI_UI_DIRECTION.md` và `07_CORE_TECHNICAL_DIRECTION.md` cho biết hướng đúng, nhưng agent implement cần đọc cả plan đã được cập nhật để hiểu:

- App là suite nhiều module, không phải một màn hình Feature 01.
- Workflow Feature 01 là nén/tối ưu ảnh. Gộp file và nén PDF thuộc feature khác.
- Các feature nối nhau bằng automation/handoff rõ ràng.
- UI WinUI phải đồng bộ từ shell đến content.
- Core/process/FFmpeg/file system không nằm trong XAML code-behind.
- Acceptance checklist có gate riêng cho WinUI/core alignment.

Nếu chỉ đọc hai file mới, agent có thể hiểu style nhưng thiếu workflow hoặc acceptance.

## 3. Prompt handoff đề xuất

Khi giao cho agent bắt đầu code UI, nên dùng prompt kiểu:

```text
Đọc toàn bộ docs 00-09 trước khi code. Hướng implement hiện tại là .NET 8 + WinUI 3 / Windows App SDK + FFmpeg bundled.

Không implement theo WinForms. Không tạo app chỉ có một màn ImageOptimizer hardcode.

Quyết định boundary: nén là nén, gộp là gộp. Image Optimizer không tự gộp PDF hoặc nén PDF trong cùng màn.

Trước khi viết code, hãy tóm tắt lại:
1. Suite shell gồm gì.
2. Feature 01 Image Optimizer workflow gồm gì.
3. Những WinUI controls bắt buộc dùng.
4. Ranh giới View/ViewModel/Workflow/Infrastructure.
5. Ranh giới Image Optimizer, File Merge / PDF Builder và PDF Compressor.
6. Checklist để tránh vỏ WinUI nhưng ruột thô.

Sau đó mới scaffold code.
```

## 4. Các điều agent phải xác nhận trước khi code

Agent phải xác nhận được các điểm sau:

- App name/mental model là `File Utility Hub`.
- Feature đầu tiên là `Image Optimizer`.
- `File Merge / PDF Builder` chịu trách nhiệm gộp file/ảnh/PDF.
- `PDF Compressor` chịu trách nhiệm preview PDF, setting nén PDF, nén lại và chọn final.
- Shell phải hỗ trợ nhiều module.
- `NavigationView` hoặc pattern WinUI tương đương dùng cho module navigation.
- Feature content render qua feature host, không hardcode vào `MainWindow`.
- `ListView.ItemTemplate` dùng cho file list trong Image Optimizer; PDF versions thuộc PDF Compressor.
- Warning/error dùng `InfoBar`/`ContentDialog`.
- CRF dùng shared `QualityStepperSlider`; q PDF thuộc PDF Compressor.
- Dùng WinUI default controls/resources; không tạo palette hoặc semantic brush riêng nếu không có lý do chức năng.
- ViewModel expose state/commands.
- Code-behind không build FFmpeg command, không scan file sâu, không tạo PDF.
- Image Optimizer workflow service chỉ điều phối scan/convert/review/handoff.
- Handoff `Gộp file` và `Gộp và nén` đi qua context/job, không gọi UI feature khác trực tiếp.
- Infrastructure xử lý FFmpeg/process/file system/log.

Nếu agent không tóm tắt đúng các điểm này, chưa nên cho code.

## 5. Definition of done cho phase UI đầu tiên

Phase UI đầu tiên chỉ pass khi có tối thiểu:

- WinUI `MainWindow` mở được.
- Suite shell có module navigation và feature host.
- Các module chưa làm hiển thị placeholder rõ, không giống lỗi.
- Feature 01 view có layout workspace + settings panel + status/progress area.
- File list dùng `ListView` với row template gọn: file, path, size, delta, status.
- Có action rõ `Gộp file` và `Gộp và nén` khi đủ điều kiện.
- Warning/error placeholder dùng `InfoBar`.
- CRF control dùng shared component hoặc ít nhất cùng ViewModel/config contract.
- UI dùng WinUI default resources/control style, không hardcode màu riêng từng màn.
- ViewModel có state/commands đủ để mock UI state trước khi nối FFmpeg thật.

Không pass nếu chỉ có:

- Một window đẹp nhưng feature content trống/thô.
- Button và label đặt thủ công không theo shared style.
- File list là text block/log box.
- Business logic nằm trong click handler.
- `MainWindow` biết chi tiết convert AVIF, gộp file hoặc nén PDF.
- Image Optimizer tự gộp/nén PDF trong cùng màn.

## 6. Review checklist sau khi agent code xong

Người review cần kiểm tra:

### Shell

- [ ] Có module navigation.
- [ ] Có feature host.
- [ ] Placeholder module rõ ràng.
- [ ] Header/status/tool area không hardcode riêng Feature 01 quá sâu.

### Feature content

- [ ] Dropzone/empty state nhìn cùng visual language với shell.
- [ ] File list có `ItemTemplate`, không phải text/log thô.
- [ ] Settings panel scroll được.
- [ ] Primary action rõ.
- [ ] Warning/error dùng WinUI pattern.
- [ ] Handoff action sang File Merge/PDF Compressor rõ.

### Architecture

- [ ] ViewModel có state/commands.
- [ ] Core/result/warning model không reference WinUI.
- [ ] FFmpeg command builder/process runner nằm ở infrastructure/service.
- [ ] Không có workflow nghiệp vụ lớn trong XAML code-behind.

## 7. Quy tắc sửa nếu phát hiện vỏ và ruột lệch

Nếu shell đẹp nhưng content thô, ưu tiên sửa theo thứ tự:

1. Đưa UI về WinUI native/default controls trước khi custom style.
2. Chuyển file list sang `ListView.ItemTemplate`.
3. Chuyển warning/error sang `InfoBar`/`ContentDialog`.
4. Tách click handler nghiệp vụ ra ViewModel command.
5. Tạo shared `QualityStepperSlider` thay vì mỗi nơi một slider/button riêng.
6. Đưa process/FFmpeg/file output logic khỏi code-behind.
7. Nếu thấy Image Optimizer tự gộp/nén PDF, tách sang `File Merge / PDF Builder` hoặc `PDF Compressor`.

Không nên vá bằng cách hardcode thêm màu/bo góc từng control riêng lẻ. Dùng mặc định của WinUI trước, chỉ custom khi control mặc định không đáp ứng được hành vi chức năng.
