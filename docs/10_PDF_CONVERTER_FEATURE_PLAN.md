# Plan tính năng Convert PDF

## 1. Mục tiêu

Tính năng **Convert PDF** nhận nhiều file hỗn hợp và xử lý hàng loạt. Mỗi file đầu vào tạo ra **một file PDF riêng**, ưu tiên:

- Giữ hình thức trang gần với file gốc.
- Mặc định chuyển thành tài liệu đen trắng giống tài liệu vừa scan.
- Không cần giữ nội dung editable, text hoặc vector.
- Dung lượng đầu ra nhẹ nhất có thể nhưng chữ, bảng và hình ảnh vẫn đọc rõ.
- File PDF đầu ra nằm cùng thư mục với file nguồn.

Tính năng này độc lập với:

- **Gộp file**: nhiều file tạo thành một PDF chung.
- **Nén PDF**: tối ưu lại một PDF đã có.
- **Tối ưu ảnh**: xuất ảnh AVIF để sử dụng độc lập.

## 2. Quyết định kỹ thuật chính

### 2.1. Dùng AVIF làm artifact tối ưu trung gian

Convert PDF tái sử dụng quy trình đã được kiểm chứng giữa hai tính năng hiện tại:

```text
Tối ưu ảnh
-> tạo AVIF nhẹ và giữ chất lượng chấp nhận được

Gộp file
-> đọc AVIF
-> chuẩn bị JPEG theo JpegQ và chế độ màu
-> dựng PDF
```

Pipeline đầy đủ của Convert PDF:

```text
File nguồn
-> chuẩn hóa thành từng trang
-> tối ưu từng trang thành AVIF
-> combine các AVIF thành PDF riêng cho file nguồn
-> xóa artifact tạm sau khi output hợp lệ
```

AVIF không được nhúng trực tiếp vào PDF. `PdfBuilderService` hiện tại đọc AVIF qua FFmpeg, chuyển thành JPEG chuẩn bị rồi nhúng vào PDF. Đây là hành vi có chủ đích vì:

- Tái sử dụng pipeline `ImageConvertService` và `PdfBuilderService` đang có.
- AVIF giúp chuẩn hóa và giảm mạnh dung lượng trang trước khi dựng PDF.
- Bước `JpegQ` cuối cho phép kiểm soát dung lượng PDF.
- Quy trình `AVIF -> PDF RGB -> chỉnh q -> final` đã có golden test `Sao ke GD`.
- Không cần phát minh thêm pipeline Group4 hoặc codec PDF mới trong MVP.

### 2.2. Xử lý theo chế độ màu

| Chế độ | Xử lý đề xuất | Mục tiêu |
| --- | --- | --- |
| Đen trắng, mặc định | Render trang, loại bỏ màu, tạo AVIF grayscale/monochrome, sau đó combine PDF với pixel format `gray` | Tài liệu không màu, giống bản scan, dung lượng nhẹ |
| RGB | Render trang giữ màu, tạo AVIF RGB, sau đó combine PDF với pixel format `yuvj420p` | Giữ hình ảnh và màu sắc gần file gốc |

Trong MVP, **Đen trắng** được hiểu là tài liệu không màu. Không bắt buộc threshold thành ảnh 1-bit vì có thể làm mất nét chữ nhỏ, bảng mảnh hoặc chi tiết ảnh. Threshold 1-bit có thể được nghiên cứu ở phase sau.

## 3. Phạm vi MVP

### Trong phạm vi

- Có module riêng **Convert PDF** trên menu công cụ hiện tại.
- Nhận nhiều file và folder bằng File Picker hoặc kéo thả.
- Cho phép danh sách file hỗn hợp.
- Xử lý hàng loạt; mỗi input tạo một PDF output riêng.
- Nếu người dùng chọn một hoặc nhiều dòng, chỉ convert các dòng đang chọn. Nếu không có selection, convert toàn bộ list.
- Chế độ màu:
  - `Đen trắng` mặc định.
  - `RGB`.
- Hiển thị trạng thái và kết quả riêng cho từng file.
- Cho phép hủy batch đang chạy.
- Output nằm cùng thư mục file nguồn.
- Không làm thay đổi file nguồn.

### Ngoài phạm vi MVP

- Gộp nhiều input thành một PDF.
- OCR hoặc tạo PDF có thể tìm kiếm nội dung.
- Giữ text/vector/editable từ PDF hoặc Office.
- Chỉnh sửa trang, reorder trang hoặc đóng dấu trực tiếp.
- Tự động đóng dấu sau khi convert.
- Handoff sang tính năng đóng dấu.
- Cho người dùng chỉnh sâu DPI, threshold, AVIF CRF hoặc JPEG Quality thủ công.
- Convert PDF sang ảnh hoặc định dạng khác.

## 4. Loại file hỗ trợ

| Nhóm | Định dạng MVP | Cách chuẩn hóa |
| --- | --- | --- |
| PDF | `.pdf` | Render từng trang |
| Ảnh | `.jpg`, `.jpeg`, `.png`, `.bmp`, `.tif`, `.tiff`, `.webp`, `.avif` | Mỗi ảnh trở thành một trang PDF |
| Word | `.doc`, `.docx` | Microsoft Word xuất PDF tạm, sau đó render từng trang |
| Excel | `.xls`, `.xlsx` | Microsoft Excel xuất PDF tạm, sau đó render từng trang |
| PowerPoint | `.ppt`, `.pptx` | Microsoft PowerPoint xuất PDF tạm, sau đó render từng trang |

Office conversion tiếp tục dùng `OfficeConvertService` và `office_to_pdf.py`. Máy người dùng cần cài Microsoft Office phù hợp.

## 5. Quy tắc đầu ra

### 5.1. Tên và vị trí file

Tên output luôn kèm chế độ màu:

```text
C:\TaiLieu\BaoCao.docx
-> C:\TaiLieu\BaoCao_blackwhite.pdf

C:\TaiLieu\BaoCao.docx
-> C:\TaiLieu\BaoCao_rgb.pdf
```

Nếu output đã tồn tại, không ghi đè âm thầm:

```text
BaoCao_blackwhite.pdf
BaoCao_blackwhite_1.pdf
BaoCao_blackwhite_2.pdf
```

### 5.2. Trường hợp input đã là PDF

Input PDF áp dụng cùng quy tắc tên theo chế độ màu, vì vậy không trùng đường dẫn với file gốc:

```text
BaoCao.pdf
-> BaoCao_blackwhite.pdf

BaoCao.pdf
-> BaoCao_rgb.pdf
```

Nếu file đã tồn tại:

```text
BaoCao_blackwhite_1.pdf
```

Không tự động thay thế PDF gốc.

### 5.3. Ảnh đầu vào

Mỗi ảnh đầu vào tạo thành một PDF riêng:

```text
Trang01.jpg -> Trang01_blackwhite.pdf
Trang02.png -> Trang02_blackwhite.pdf
```

Không gộp nhiều ảnh thành một PDF trong tính năng Convert PDF.

## 6. Luồng người dùng

1. Người dùng mở menu **Convert PDF**.
2. Người dùng thêm file, import folder hoặc kéo thả file.
3. App hiển thị toàn bộ file hợp lệ trong ListView.
4. Chế độ màu mặc định là **Đen trắng**.
5. Người dùng có thể đổi toàn batch sang **RGB**.
6. Người dùng có thể chọn lại một số dòng chưa hài lòng để convert riêng.
7. Người dùng nhấn **Convert sang PDF**.
8. Nếu có selection, app chỉ xử lý các dòng đang chọn; nếu không có selection, app xử lý toàn bộ list.
9. App xử lý lần lượt từng file và cập nhật trạng thái từng dòng.
10. File thành công được xuất cạnh file nguồn.
11. Batch hoàn tất hiển thị số file thành công, cảnh báo và thất bại.
12. Người dùng có thể mở thư mục chứa output từ từng dòng hoặc toolbar.

## 7. Giao diện

Giao diện kế thừa cấu trúc chính của **Gộp file**:

- Giữ `CommandBar`, import file, import folder, xóa đã chọn, xóa tất cả.
- Giữ DropZone và ngôn ngữ giao diện WinUI hiện tại.
- Giữ bố cục ListView bên trái, settings panel bên phải.
- Giữ InfoBar và global progress/status.

### 7.1. Điểm khác với Gộp file

- Không cho reorder vì thứ tự file không ảnh hưởng kết quả.
- Không có cài đặt trang gộp, tên file PDF chung hoặc nút gộp.
- Mỗi dòng là một conversion job độc lập.

### 7.2. ListView đề xuất

| Cột | Nội dung |
| --- | --- |
| File nguồn | Tên file và đường dẫn |
| Định dạng | PDF, Word, Excel, PowerPoint hoặc ảnh |
| Dung lượng gốc | Dung lượng trước xử lý |
| Kết quả | Tên output hoặc lỗi |
| Dung lượng output | Dung lượng PDF sau xử lý |
| Trạng thái | Chờ xử lý, Đang xử lý, Thành công, Cảnh báo, Lỗi |

### 7.3. Settings panel MVP

**Chế độ màu**

- `Đen trắng` - mặc định.
- `RGB`.

Hiển thị mô tả ngắn:

> Đen trắng phù hợp tài liệu cần đóng dấu, giúp loại bỏ màu và giảm dung lượng. RGB giữ màu gần file gốc nhưng file thường nặng hơn.

Không hiển thị DPI, AVIF CRF và JpegQ trong MVP. App dùng preset nội bộ đã kiểm thử để tránh tạo output quá nặng hoặc quá mờ.

## 8. Pipeline xử lý

### 8.1. Pipeline chung

```text
Validate input
-> xác định loại file
-> chuẩn hóa thành danh sách trang
-> render từng trang
-> xử lý chế độ màu
-> tối ưu từng trang thành AVIF
-> chuyển AVIF thành JPEG chuẩn bị theo JpegQ
-> tạo PDF output tạm
-> validate output
-> chuyển output vào cùng thư mục nguồn
-> dọn file tạm
```

### 8.2. PDF đầu vào

```text
PDF
-> render từng trang ở preset chất lượng tài liệu
-> tạo AVIF Đen trắng hoặc RGB
-> combine các trang AVIF thành PDF mới
```

PDF output không giữ text/vector/editable. Đây là hành vi chủ đích để tài liệu có hình thức giống bản scan.

### 8.3. Office đầu vào

```text
Word / Excel / PowerPoint
-> Office COM xuất PDF tạm
-> render từng trang
-> tạo AVIF Đen trắng hoặc RGB
-> combine các trang AVIF thành PDF output
-> xóa PDF tạm
```

Các Office job nên xử lý tuần tự để tránh nhiều tiến trình Word/Excel/PowerPoint chạy đồng thời gây treo hoặc hiện dialog.

### 8.4. Ảnh đầu vào

```text
Ảnh
-> đọc ảnh và orientation
-> tối ưu thành AVIF Đen trắng hoặc RGB
-> combine thành PDF một trang
```

Ảnh đầu vào cũng đi qua AVIF để toàn bộ loại file dùng chung một pipeline tối ưu và combine.

## 9. Preset chất lượng nội bộ đề xuất

### Preset chung đề xuất

- Render PDF/Office ban đầu khoảng `200 DPI`.
- AVIF kế thừa logic CRF và resize từ `ImageConvertService`.
- Đen trắng: mở rộng lệnh AVIF để áp dụng `format=gray` trước khi encode.
- RGB: giữ pipeline AVIF `yuv420p` hiện tại.
- Combine PDF kế thừa `PdfBuilderService`, dùng JpegQ nội bộ đã kiểm thử.
- Giữ đúng tỷ lệ và kích thước trang; không ép A4 mặc định.

### Preset cần technical spike

- So sánh AVIF CRF `20`, `24`, `28`.
- So sánh JpegQ/qscale PDF trên các mức đã có benchmark thực tế.
- So sánh render PDF/Office ở `180`, `200`, `240 DPI`.
- Đo cả dung lượng AVIF trung gian và PDF cuối.
- Kiểm tra riêng Đen trắng và RGB.

Các giá trị trên chưa phải cấu hình chốt. Golden test phải bao gồm `Sao ke GD`, tài liệu chữ nhỏ, bảng Excel, slide, ảnh chụp và PDF nhiều trang.

## 10. Business rules và ngoại lệ

| Tình huống | Cách xử lý | Trạng thái |
| --- | --- | --- |
| File không hỗ trợ | Không thêm vào batch hoặc hiển thị cảnh báo rõ định dạng | Confirmed |
| Output đã tồn tại | Tự thêm hậu tố `_1`, `_2`; không ghi đè | Assumption |
| Tên output | Luôn dùng `<tên>_blackwhite.pdf` hoặc `<tên>_rgb.pdf` theo chế độ màu | Confirmed |
| Input là PDF | Áp dụng cùng quy tắc `_blackwhite`/`_rgb`, không ghi đè PDF gốc | Confirmed |
| Có dòng đang chọn | Chỉ convert các dòng đang chọn | Confirmed |
| Không có dòng đang chọn | Convert toàn bộ list | Confirmed |
| Ảnh đầu vào | Mỗi ảnh tạo một PDF riêng | Confirmed |
| Office chưa cài hoặc COM lỗi | File đó thất bại; batch tiếp tục file tiếp theo | Confirmed |
| Office hiện dialog yêu cầu xác nhận | Hủy job đó, báo lỗi thân thiện, batch tiếp tục | Assumption |
| PDF có mật khẩu | Báo không thể xử lý nếu không mở được | Assumption |
| File đang được ứng dụng khác khóa | Báo lỗi riêng cho file đó | Assumption |
| Người dùng hủy batch | Dừng sau bước an toàn hiện tại, xóa output tạm chưa hoàn tất | Confirmed |
| Output nặng hơn input | Vẫn lưu output nhưng đánh dấu cảnh báo | Confirmed |
| Black & White làm mất chi tiết ảnh | Đây là trade-off của chế độ người dùng chọn; có thể chạy lại bằng RGB | Confirmed |
| Một file thất bại | Không dừng toàn batch | Confirmed |

## 11. Kiến trúc đề xuất

```text
Features/PdfConverter/
├── PdfConverterPage.xaml
├── PdfConverterPage.xaml.cs
└── PdfConverterViewModel.cs

Core/Models/
├── PdfConvertItem.cs
└── PdfConvertConfig.cs

Core/Services/
├── PdfConvertWorkflowService.cs
├── DocumentPageExtractService.cs
├── ImageConvertService.cs
├── PdfBuilderService.cs
└── OfficeConvertService.cs
```

### Trách nhiệm

- `PdfConverterViewModel`: quản lý list, selection, settings, command và trạng thái UI.
- `PdfConvertWorkflowService`: điều phối batch và từng file.
- `OfficeConvertService`: chuẩn hóa Office thành PDF tạm.
- `DocumentPageExtractService`: render PDF thành các ảnh trang đầu vào cho bước AVIF.
- `ImageConvertService`: được mở rộng để tối ưu ảnh trang thành AVIF theo chế độ màu.
- `PdfBuilderService`: được tái sử dụng để đọc AVIF, chuẩn bị JPEG và dựng PDF.
- `OutputManager`: tạo tên output không trùng và commit file tạm sang thư mục nguồn.

Không đặt pipeline convert trong XAML code-behind.

### 11.1. Khoảng trống của code hiện tại cần xử lý

- `ImageConvertService` hiện luôn xuất vào folder `compressed-avif`. Convert PDF cần cho phép truyền workspace tạm riêng theo từng job để không tạo AVIF phụ cạnh file nguồn.
- `BuildAvifConvertCommand` hiện hardcode `yuv420p`. Cần nhận chế độ màu và áp dụng `format=gray` cho Đen trắng.
- `PdfRenderService` hiện nhận `isGrayscale` nhưng chưa thực sự chuyển bitmap sang grayscale. Không được xem tham số này là đã hoạt động.
- `PdfBuilderService` đã đọc được AVIF và chuẩn bị JPEG, nhưng hiện được thiết kế cho một danh sách gộp. Convert PDF sẽ gọi builder riêng cho từng input để mỗi input tạo một PDF.
- `OfficeConvertService` và `office_to_pdf.py` đã tồn tại, nhưng cần được nối vào workflow Convert PDF và kiểm tra đóng tiến trình Office, cleanup file tạm.
- `FileScanService` hiện chưa hỗ trợ Office trong danh sách extension chung. PdfConverter cần extension contract riêng hoặc mở rộng scanner mà không làm ảnh hưởng các module khác.

## 12. Kế hoạch triển khai

### Phase 1 - Technical spike

- Chạy pipeline thật `PDF/Office -> trang -> AVIF -> PDF`.
- So sánh dung lượng và chất lượng ở các preset AVIF CRF, DPI và JpegQ.
- Xác minh AVIF grayscale/monochrome bằng FFmpeg bundled.
- Kiểm tra Word, Excel, PowerPoint và PDF nhiều trang.
- Dùng `Sao ke GD` làm golden test cùng các tài liệu hỗn hợp.
- Chốt preset nội bộ trước khi dựng UI hoàn chỉnh.

### Phase 2 - Core pipeline

- Tạo model/config/job result.
- Tạo output naming và temp/cleanup an toàn.
- Implement pipeline ảnh, PDF và Office.
- Implement batch tuần tự, cancel và continue-on-error.

### Phase 3 - UI

- Tạo `PdfConverterPage` kế thừa layout File Merger.
- Tạo ListView trạng thái/kết quả theo từng file.
- Tạo settings màu mặc định Đen trắng.
- Nối menu `Convert PDF` với page thật.

### Phase 4 - QA và tối ưu

- Test chất lượng chữ nhỏ, bảng, ảnh chụp và nền giấy.
- Test batch hỗn hợp và lỗi từng file.
- Test dung lượng output.
- Test hủy giữa batch và cleanup temp.

## 13. Tiêu chí nghiệm thu

- Khi thêm nhiều file hỗn hợp, mỗi file hợp lệ xuất thành một PDF riêng.
- Output được tạo trong cùng thư mục với file nguồn.
- Output có hậu tố `_blackwhite` hoặc `_rgb` đúng với chế độ màu.
- Tên output không ghi đè file hiện có.
- Chế độ mặc định là Đen trắng.
- Khi có selection, chỉ các dòng đang chọn được convert.
- Khi không có selection, toàn bộ list được convert.
- Mỗi ảnh đầu vào tạo thành một PDF riêng.
- PDF Đen trắng có nền sạch, chữ và bảng đọc rõ, dung lượng hợp lý.
- Chế độ RGB giữ được màu và bố cục gần file nguồn.
- Mỗi trang được tối ưu thành AVIF trước khi combine thành PDF.
- Office input được chuyển thành PDF tạm, xuất trang, tối ưu AVIF rồi combine.
- PDF output không cần editable hoặc searchable.
- Một file lỗi không làm dừng toàn batch.
- Người dùng có thể hủy batch; file tạm được dọn an toàn.
- ListView hiển thị đúng trạng thái, output path và dung lượng của từng file.
- App build thành công và không làm hỏng các module Tối ưu ảnh, Gộp file, Nén PDF.

## 14. Quyết định đã chốt

1. Tên output dùng hậu tố theo chế độ màu: `<tên>_blackwhite.pdf` hoặc `<tên>_rgb.pdf`.
2. Mỗi ảnh đầu vào tạo thành một PDF riêng.
3. Có selection thì chỉ convert selection; không có selection thì convert toàn bộ list.
4. Không cần handoff sang tính năng đóng dấu.
