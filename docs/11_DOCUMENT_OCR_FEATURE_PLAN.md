# Plan tính năng OCR tài liệu

## 1. Tên tính năng

### Tên tab đề xuất: `OCR tài liệu`

Mô tả ngắn hiển thị trong giao diện:

```text
PDF/Ảnh scan sang Word chỉnh sửa
```

Lý do chọn:

- Người dùng phổ thông đã quen với từ OCR.
- Không giới hạn tính năng vào một nguồn duy nhất như máy scan.
- Không giới hạn output vào DOCX nếu sau này có thêm preview, JSON hoặc Excel.
- Phù hợp cách đặt tên ngắn của các tab hiện tại: Tối ưu ảnh, Gộp file, Nén PDF, Convert PDF.

Các tên không ưu tiên:

| Tên | Hạn chế |
| --- | --- |
| Scan sang Word | Dễ hiểu nhưng quá hẹp, vì input có thể là PDF hoặc ảnh không đến từ máy scan |
| Tài liệu sang Word | Không thể hiện khả năng OCR và phân tích bảng |
| Local OCR | Tên kỹ thuật, không nói rõ lợi ích cho người dùng |
| OCR Pro | Mang tính marketing nhưng không mô tả workflow |

## 2. Mục tiêu

Tính năng **OCR tài liệu** nhận PDF hoặc ảnh scan, dùng PaddleOCR-VL để nhận diện nội dung và cấu trúc, sau đó xuất thành DOCX sạch, có thể chỉnh sửa.

Mục tiêu đầu ra:

- Giữ đầy đủ nội dung chữ của toàn bộ các trang.
- Chuyển bảng thành bảng Word native, không phải ảnh.
- Giữ cấu trúc tiêu đề, đoạn văn, danh sách và bảng ở mức thực dụng.
- Loại bỏ ảnh, con dấu và các block không cần thiết.
- Nội dung thông thường dùng A4 dọc.
- Bảng rất rộng dùng A4 ngang.
- Không dùng A3.
- Mỗi file đầu vào tạo một DOCX riêng.

Đây là tính năng tạo **bản Word sạch để tiếp tục chỉnh sửa**, không phải tính năng sao chép pixel-perfect bố cục của tài liệu gốc.

## 3. Nền tảng hiện có từ LocalScanner

### OCR service

```text
PaddleOCR-VL API
-> PaddleOCR VLM server dùng vLLM
-> chạy bằng Docker Compose và NVIDIA GPU
```

Endpoint hiện có:

| Mục đích | Method | Endpoint |
| --- | --- | --- |
| Kiểm tra service | `GET` | `/health` |
| OCR và phân tích layout | `POST` | `/layout-parsing` |

API hiện được expose qua:

```text
http://localhost:18080
```

Địa chỉ Tailscale của OCR server đã được tài liệu LocalScanner xác định:

```text
http://100.91.163.48:18080
```

Trong giai đoạn MVP, người dùng nội bộ tại văn phòng HCM là nhóm triển khai và nghiệm thu đầu tiên. Khả năng cho máy tại HN hoặc ngoài mạng văn phòng kết nối đến OCR server qua Tailscale là phase mở rộng sau khi luồng nội bộ HCM đã chạy ổn định.

### Trạng thái đã kiểm tra

- `paddleocr-vl-api`: healthy.
- `paddleocr-vlm-server`: healthy.
- GPU hiện tại: NVIDIA GeForce RTX 5060 Ti, VRAM khoảng 16 GB.
- Khi service đang chạy, mức VRAM sử dụng quan sát được khoảng 7.8 GB.

### Bộ dựng DOCX hiện có

```text
F:\Code\MyWork\LocalScanner\scripts\build_clean_docx.py
```

Khả năng hiện tại:

- Đọc toàn bộ `result.layoutParsingResults`.
- Loại các block `seal`, `number`, `image`.
- Bỏ nội dung báo ảnh quá mờ không nhận diện được.
- Tạo tiêu đề, heading, đoạn văn và bullet.
- Chuyển HTML table thành bảng Word native.
- Hỗ trợ `rowspan`, `colspan`.
- Lặp lại hàng tiêu đề bảng.
- Tự giảm font với bảng nhiều cột.
- Chuyển sang A4 ngang khi bảng có trên 12 cột.
- Thêm số trang.

## 4. Phạm vi MVP

### Trong phạm vi

- Thêm tab mới **OCR tài liệu** vào File Utility Hub.
- Nhận PDF và ảnh.
- Import file, import folder và kéo thả.
- Hiển thị danh sách nhiều file cần OCR.
- Xử lý batch tuần tự.
- Mỗi input tạo một DOCX riêng.
- Có selection thì chỉ OCR các dòng đang chọn.
- Không có selection thì OCR toàn bộ list.
- Kiểm tra trạng thái OCR server trước khi chạy.
- Hiển thị trạng thái, lỗi và output riêng cho từng file.
- Cho phép hủy batch.
- Output nằm cùng thư mục file nguồn.
- Không ghi đè file đã tồn tại.
- MVP triển khai và nghiệm thu trước với người dùng nội bộ văn phòng HCM.

### Ngoài phạm vi MVP

- OCR Word, Excel hoặc PowerPoint.
- Chỉnh sửa trực tiếp nội dung OCR trong app.
- Preview và duyệt từng block trước khi xuất DOCX.
- Giữ hình ảnh hoặc con dấu trong DOCX.
- Nhận diện riêng con dấu.
- Nhận diện biểu đồ.
- Tạo DOCX giống pixel-perfect tài liệu gốc.
- OCR song song nhiều file.
- Tự động retry một OCR request đã được server tiếp nhận.
- App tự quản lý Docker, model hoặc GPU.
- Triển khai cho người dùng HN hoặc ngoài văn phòng qua Tailscale.

## 5. File hỗ trợ và quy tắc output

### Input MVP

| Nhóm | Định dạng |
| --- | --- |
| PDF | `.pdf` |
| Ảnh | `.jpg`, `.jpeg`, `.png`, `.bmp`, `.tif`, `.tiff`, `.webp` |

### Output

Mỗi file đầu vào tạo một DOCX cùng thư mục:

```text
BaoCao.pdf
-> BaoCao_ocr.docx

TrangScan.jpg
-> TrangScan_ocr.docx
```

Nếu output đã tồn tại:

```text
BaoCao_ocr.docx
BaoCao_ocr_1.docx
BaoCao_ocr_2.docx
```

Không ghi đè file cũ.

## 6. Luồng người dùng

1. Người dùng mở tab **OCR tài liệu**.
2. App kiểm tra kết nối OCR server qua `/health`.
3. Người dùng thêm PDF/ảnh, import folder hoặc kéo thả.
4. App hiển thị các file hợp lệ trong ListView.
5. Người dùng có thể chọn một số dòng cần OCR lại.
6. Người dùng nhấn **OCR sang Word**.
7. Nếu có selection, app chỉ xử lý các dòng đang chọn.
8. Nếu không có selection, app xử lý toàn bộ list.
9. App gửi từng file đến backend OCR theo thứ tự.
10. Backend phân tích toàn bộ trang và dựng DOCX sạch.
11. App lưu DOCX cạnh file nguồn và cập nhật kết quả từng dòng.
12. Khi hoàn tất, app hiển thị tổng số thành công, cảnh báo và thất bại.

## 7. Giao diện

### 7.1. Bố cục

Kế thừa ngôn ngữ giao diện từ **Gộp file** và **Convert PDF**:

- `CommandBar` trên cùng.
- ListView file bên trái.
- Settings/status panel bên phải.
- DropZone khi danh sách trống.
- InfoBar cho cảnh báo và lỗi.
- Global status bar hiển thị tiến độ batch.

### 7.2. UI brief bắt buộc

Frontend phải lấy `FileMergerPage` làm màn hình tham chiếu chính, sau đó thay nội dung nghiệp vụ. Không thiết kế một layout OCR hoàn toàn mới.

> **Developer note bắt buộc:** Luôn sử dụng WinUI native components, WinUI theme resources và shared WinUI controls hiện có của app. Giao diện phải mang trải nghiệm Windows native hoàn chỉnh, không được làm theo kiểu giao diện lai hoặc chỉ mô phỏng WinUI.

```text
CommandBar
────────────────────────────────────────
ListView file/job          Panel OCR
                          - Server status
                          - Mô tả output
                          - OCR sang Word
                          - Hủy
────────────────────────────────────────
Global status/progress
```

Quy tắc kế thừa:

- Giữ cùng chiều rộng panel phải, padding, spacing, card, InfoBar và CommandBar như các tính năng trước.
- Giữ DropZone và hành vi import file/folder hiện tại.
- Giữ ListView dạng bảng gọn, mỗi dòng là một file/job.
- Giữ cơ chế selection: có dòng được chọn thì xử lý selection; không chọn thì xử lý toàn bộ.
- Giữ status badge dùng shared control hiện tại.
- Giữ global progress ở đáy app; progress này thể hiện số file đã hoàn tất trong batch.
- Không thêm màn hình wizard nhiều bước.
- Không thêm khu vực preview tài liệu trong MVP.
- Không hiển thị Docker, GPU, model, endpoint hoặc JSON OCR cho người dùng phổ thông.
- Trạng thái server phải được dịch thành ngôn ngữ người dùng: `Sẵn sàng`, `Không kết nối được`, `Đang bận`.

Quy định kỹ thuật giao diện:

- Dùng WinUI `Page`, `Grid`, `CommandBar`, `AppBarButton`, `ListView`, `InfoBar`, `Border`, `ScrollViewer`, `ProgressBar`, `TextBlock`, `Button` và các component native phù hợp.
- Dùng `{ThemeResource ...}` và style WinUI hiện có; không hardcode một bộ màu/style riêng cho màn OCR.
- Tái sử dụng `DropZoneControl`, `FileStatusBadge` và các shared controls hiện có khi phù hợp.
- Không dùng WebView để dựng giao diện chính.
- Không nhúng HTML/CSS/JavaScript để mô phỏng component Windows.
- Không dùng framework UI web hoặc control mang phong cách web trong màn hình WinUI.
- Không tạo toolbar, card, button hoặc dialog custom nếu WinUI đã có component native đáp ứng được.
- Code-behind chỉ xử lý tương tác UI cần thiết; workflow OCR, HTTP và file output phải nằm trong ViewModel/service.

Thứ tự ưu tiên khi dựng UI:

1. Copy cấu trúc XAML và interaction pattern của `FileMergerPage`.
2. Thay model/list columns thành dữ liệu OCR.
3. Thay settings panel thành trạng thái server và mô tả DOCX.
4. Nối command OCR, cancel và health check.
5. Chỉ tạo shared control mới nếu control hiện tại không đáp ứng được.

### 7.3. CommandBar

| Nút | Hành vi |
| --- | --- |
| Thêm file | Chọn nhiều PDF/ảnh |
| Import folder | Thêm các file hợp lệ trong folder |
| Xóa đã chọn | Xóa khỏi list, không xóa file gốc |
| Xóa tất cả | Xóa toàn bộ list |
| Kiểm tra OCR server | Gọi `/health` và hiển thị trạng thái |
| Mở thư mục output | Mở thư mục của output gần nhất hoặc file đang chọn |

### 7.4. ListView

| Cột | Nội dung |
| --- | --- |
| File nguồn | Tên và đường dẫn |
| Định dạng | PDF hoặc ảnh |
| Dung lượng | Dung lượng input |
| Trang | Tổng số trang nếu xác định được |
| Kết quả | Tên DOCX hoặc lỗi |
| Thời gian | Thời gian xử lý file |
| Trạng thái | Chờ OCR, Đang gửi, Đang OCR, Đang tạo Word, Thành công, Lỗi, Đã hủy |

### 7.5. Panel bên phải

MVP không cần nhiều setting kỹ thuật. Panel gồm:

**Trạng thái OCR server**

- Đang kiểm tra.
- Sẵn sàng.
- Không kết nối được.
- Server đang bận.

**Thiết lập đầu ra**

- Loại bỏ ảnh và con dấu: bật cố định trong MVP.
- Khổ trang: A4 dọc, tự chuyển A4 ngang cho bảng rộng.
- Output: DOCX sạch, có thể chỉnh sửa.

**Hành động chính**

- `OCR sang Word`.
- `Hủy`.

## 8. Kiến trúc tích hợp đề xuất

### 8.1. Quyết định chính

File Utility Hub không gọi trực tiếp `/layout-parsing` rồi tự chạy Python.

Khuyến nghị hoàn thiện backend LocalScanner bằng endpoint nghiệp vụ:

```http
POST /api/convert-to-docx
Content-Type: multipart/form-data
```

Backend chịu trách nhiệm toàn bộ:

```text
Nhận file
-> gọi PaddleOCR-VL /layout-parsing
-> giữ toàn bộ layoutParsingResults
-> lưu JSON tạm
-> chạy build_clean_docx.py
-> trả DOCX
-> xóa file tạm
```

Lợi ích:

- WinUI không cần đóng gói Python và `python-docx`.
- Không cần truyền JSON OCR lớn về app rồi gửi ngược lại.
- Quy tắc dựng DOCX nằm ở một nơi duy nhất.
- Dễ kiểm soát timeout, log, cleanup và lỗi.
- Có thể nâng cấp DOCX builder mà không cập nhật app.

### 8.2. Mô hình kết nối theo giai đoạn

#### MVP - người dùng nội bộ HCM

- Triển khai và nghiệm thu trước trong văn phòng HCM.
- Ưu tiên kết nối ổn định đến máy OCR đang chạy LocalScanner.
- Xác minh đầy đủ luồng import, OCR, dựng DOCX, timeout và xử lý lỗi.
- Chưa xem việc máy HN hoặc ngoài văn phòng kết nối được là điều kiện nghiệm thu MVP.

#### Phase sau - HN và người dùng ngoài văn phòng

- Máy người dùng kết nối vào mạng Tailscale được cấp quyền.
- App gọi OCR server qua địa chỉ Tailscale:

```text
http://100.91.163.48:18080
```

- Cần có hướng dẫn cài đặt, đăng nhập và kiểm tra trạng thái Tailscale.
- Cần xác định quyền truy cập theo người dùng/máy, không mở OCR API công khai.
- Cần kiểm tra tốc độ upload file lớn, mất kết nối và timeout giữa các văn phòng.
- Chỉ rollout sau khi người dùng nội bộ HCM đã chạy ổn định.

### 8.3. Component phía File Utility Hub

```text
Features/DocumentOcr/
├── DocumentOcrPage.xaml
├── DocumentOcrPage.xaml.cs
└── DocumentOcrViewModel.cs

Core/Models/
├── DocumentOcrItem.cs
├── DocumentOcrConfig.cs
└── DocumentOcrResult.cs

Core/Services/
├── DocumentOcrWorkflowService.cs
├── OcrApiClient.cs
└── OcrOutputManager.cs
```

Trách nhiệm:

- `DocumentOcrViewModel`: list, selection, command và trạng thái UI.
- `DocumentOcrWorkflowService`: điều phối batch tuần tự, cancel và continue-on-error.
- `OcrApiClient`: health check, upload file, timeout và tải DOCX response.
- `OcrOutputManager`: đặt tên output, ghi file tạm rồi commit an toàn.

### 8.4. Component phía LocalScanner

```text
LocalScanner/
├── api/
│   └── convert_to_docx endpoint
├── scripts/
│   └── build_clean_docx.py
└── docker/
    └── paddleocr-vl-api.Dockerfile
```

Cần bổ sung:

- Copy hoặc mount `build_clean_docx.py` vào container.
- Tạo endpoint `/api/convert-to-docx`.
- Hỗ trợ multipart upload thay vì bắt App encode Base64.
- Trả DOCX trực tiếp khi thành công.
- Trả lỗi có mã và thông báo tiếng Việt có thể hiển thị.
- Dọn file tạm trong cả luồng thành công, lỗi và hủy.

## 9. Contract API đề xuất

### Health check

```http
GET /health
```

### Convert sang DOCX

```http
POST /api/convert-to-docx
Content-Type: multipart/form-data
```

Form fields:

| Field | Giá trị |
| --- | --- |
| `file` | PDF hoặc ảnh gốc |
| `removeImages` | `true` |
| `removeSeals` | `true` |
| `useWidePageForWideTables` | `true` |

Response thành công:

```http
200 OK
Content-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document
Content-Disposition: attachment; filename="BaoCao_ocr.docx"
```

Response lỗi:

```json
{
  "code": "OCR_FAILED",
  "message": "Không thể nhận diện nội dung tài liệu.",
  "retryable": false
}
```

## 10. Quy tắc OCR và DOCX

Request nội bộ đến `/layout-parsing` phải giữ:

```json
{
  "visualize": false,
  "returnMarkdownImages": false,
  "restructurePages": false,
  "useSealRecognition": false,
  "useChartRecognition": false,
  "markdownIgnoreLabels": ["seal", "image"]
}
```

Quy tắc bắt buộc:

- PDF dùng `fileType: 0`.
- Ảnh dùng `fileType: 1`.
- Không chỉ lấy `layoutParsingResults[0]`.
- Giữ toàn bộ `result.layoutParsingResults`.
- Không yêu cầu PaddleOCR xuất DOCX trực tiếp.
- DOCX phải được dựng bằng clean DOCX builder riêng.
- Không ghi Base64, OCR JSON hoặc nội dung tài liệu vào log.

## 11. Batch, timeout và retry

### Batch

- Xử lý tuần tự từng file.
- Một file lỗi không dừng toàn batch.
- Không gửi nhiều OCR job nặng đồng thời trong MVP.
- Có selection thì chỉ xử lý selection; không có selection thì xử lý toàn list.

### Timeout

- Timeout mặc định đề xuất: `2 giờ/file`.
- Không đặt timeout thấp hơn `5 phút`.
- UI phải hiển thị rõ file vẫn đang OCR, không coi request dài là app bị treo.

### Retry

- Không tự động retry sau khi server có thể đã nhận request.
- Chỉ retry lỗi kết nối xảy ra trước khi upload/request được server tiếp nhận.
- Người dùng có thể chọn dòng lỗi và nhấn `OCR sang Word` để chạy lại thủ công.

## 12. Business rules và ngoại lệ

| Tình huống | Cách xử lý | Trạng thái |
| --- | --- | --- |
| OCR server không kết nối được | Khóa nút OCR, hiển thị InfoBar và nút kiểm tra lại | Confirmed |
| File không hỗ trợ | Không thêm hoặc đánh dấu lỗi rõ định dạng | Confirmed |
| Output đã tồn tại | Thêm hậu tố `_1`, `_2`, không ghi đè | Assumption |
| PDF nhiều trang | Phải giữ toàn bộ `layoutParsingResults` | Confirmed |
| File quá lớn | Báo lỗi theo giới hạn backend trước khi gửi OCR | Need technical decision |
| OCR chạy lâu | Giữ trạng thái đang xử lý, không tự retry | Confirmed |
| Một file lỗi | Batch tiếp tục file tiếp theo | Confirmed |
| Người dùng hủy | Dừng sau bước an toàn, dọn file tạm nếu backend hỗ trợ hủy | Need technical decision |
| Tài liệu quá mờ | DOCX có thể thiếu nội dung; đánh dấu cảnh báo | Confirmed |
| Ảnh hoặc con dấu | Loại khỏi DOCX | Confirmed |
| Bảng có trên 12 cột | Chuyển section sang A4 ngang | Confirmed |
| Bảng có merge cell | Dựng bằng `rowspan` và `colspan` | Confirmed |
| Server bận hoặc GPU thiếu VRAM | Không chạy song song; báo lỗi thân thiện | Assumption |
| Mất mạng sau khi server nhận file | Không tự retry để tránh tạo job trùng | Confirmed |

## 13. Khoảng trống kỹ thuật hiện tại

### LocalScanner

- `/layout-parsing` chỉ OCR, chưa có endpoint trả DOCX hoàn chỉnh.
- `build_clean_docx.py` chưa được copy hoặc mount vào container.
- Chưa có API hủy job.
- Chưa có job ID hoặc endpoint xem trạng thái.
- Chưa có giới hạn file được công bố.
- Chưa có cơ chế chống gửi trùng cùng một file.
- Chưa có auth cho OCR API.

### Clean DOCX builder

- Khi đã chuyển sang landscape do bảng rộng, script hiện chưa chuyển lại portrait cho phần nội dung sau.
- Script không giữ ranh giới trang gốc; nội dung được dựng thành tài liệu Word sạch liên tục.
- Không có preview/review trước khi xuất.
- Không lưu OCR JSON như artifact có thể dùng để dựng lại DOCX mà không OCR lại.
- Không có báo cáo block nào bị bỏ hoặc không nhận diện được.

### File Utility Hub

- Chưa có `HttpClient`/OCR API client.
- Chưa có model trạng thái OCR dài hạn.
- Global progress hiện thiên về số file, chưa mô tả tiến độ bên trong một file OCR dài.
- Chưa có cấu hình `OCR_API_URL` và timeout.

## 14. Kế hoạch triển khai

### Phase 1 - Hoàn thiện LocalScanner backend

- Đưa `build_clean_docx.py` vào container/backend runtime.
- Tạo endpoint `/api/convert-to-docx`.
- Nhận multipart file.
- Gọi `/layout-parsing` đúng contract.
- Dựng và trả DOCX.
- Chuẩn hóa lỗi, timeout và cleanup.
- Test một ảnh, PDF ngắn và PDF nhiều trang có bảng rộng.

### Phase 2 - Technical hardening

- Sửa clean DOCX builder để chuyển lại portrait sau bảng rộng khi cần.
- Thêm giới hạn file và validation.
- Thêm request/job ID vào log, không log nội dung tài liệu.
- Xác định cơ chế hủy hoặc trạng thái job nếu cần.
- Kiểm tra server bận, thiếu VRAM và restart giữa job.

### Phase 3 - Tích hợp File Utility Hub

- Thêm menu `OCR tài liệu`.
- Tạo page, ViewModel, model và API client.
- Tạo import/list/selection/batch tuần tự.
- Tạo health check và trạng thái server.
- Lưu DOCX cùng thư mục nguồn.
- Cho phép chọn dòng lỗi/chưa hài lòng để OCR lại.

### Phase 4 - QA

- Test PDF một trang, nhiều trang.
- Test ảnh rõ, ảnh nghiêng và ảnh mờ.
- Test bảng thường, bảng merge cell và bảng trên 12 cột.
- Test mất mạng, timeout, server down và GPU lỗi.
- Test hủy batch và chạy lại selection.
- So sánh DOCX output với nội dung nguồn.

### Phase sau

- Rollout cho máy HN và người dùng ngoài văn phòng qua Tailscale.
- Thêm hướng dẫn/trạng thái kết nối Tailscale thân thiện với người dùng.
- Bổ sung kiểm soát quyền truy cập OCR server theo người dùng hoặc thiết bị.
- Lưu OCR JSON để dựng lại DOCX không cần OCR lại.
- Preview/review nội dung OCR trong app.
- Cho phép bật/tắt giữ ảnh hoặc con dấu.
- Job queue có resume/history.
- Xuất thêm HTML, JSON hoặc Excel cho bảng.

## 15. Tiêu chí nghiệm thu MVP

- Tab **OCR tài liệu** xuất hiện trong File Utility Hub.
- Màn OCR sử dụng WinUI native components và cùng visual language với các module hiện có.
- Không có WebView, HTML/CSS UI hoặc component web giả lập WinUI.
- App kiểm tra được `/health` và hiển thị trạng thái server.
- App nhận được PDF và ảnh.
- Có selection thì chỉ OCR selection; không có selection thì OCR toàn list.
- Mỗi input tạo một file `<tên>_ocr.docx` riêng.
- Output nằm cùng thư mục nguồn và không ghi đè file cũ.
- PDF nhiều trang xuất đủ nội dung của toàn bộ `layoutParsingResults`.
- Bảng được tạo thành bảng Word native.
- Bảng merge cell được dựng đúng ở mức chấp nhận được.
- Nội dung thường dùng A4 dọc; bảng rộng dùng A4 ngang; không có A3.
- Ảnh và con dấu không xuất hiện trong DOCX.
- Một file lỗi không làm dừng toàn batch.
- Không tự retry request OCR đã có thể được server tiếp nhận.
- Thông báo lỗi bằng tiếng Việt và không làm lộ nội dung tài liệu.
- MVP được nghiệm thu với người dùng nội bộ tại văn phòng HCM; kết nối HN/ngoài văn phòng qua Tailscale không phải điều kiện MVP.

## 16. Quyết định đề xuất cần chốt

1. Tên tab: **OCR tài liệu**.
2. Output MVP: chỉ DOCX sạch, editable.
3. Mỗi input tạo một DOCX riêng.
4. Batch xử lý tuần tự để bảo vệ GPU.
5. Output đặt cạnh file nguồn với hậu tố `_ocr.docx`.
6. File Utility Hub gọi endpoint nghiệp vụ `/api/convert-to-docx`, không tự chạy Python.
7. Không làm preview/review và history trong MVP.
8. Rollout Tailscale cho HN/người dùng ngoài văn phòng thuộc phase sau, chỉ thực hiện khi nhóm nội bộ HCM đã chạy ổn định.
