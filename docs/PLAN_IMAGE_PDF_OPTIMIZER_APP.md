# Kế hoạch sản phẩm: Image PDF Optimizer App

> Scope MVP đã chốt: chỉ làm AVIF -> PDF RGB -> chỉnh q -> chọn final. Không làm WebP/Both và không làm target size/auto optimize trong MVP. FFmpeg phải được bundle sẵn trong app/package, không yêu cầu user cấu hình path.

## 1. Mục tiêu sản phẩm

Xây dựng một ứng dụng Windows giúp người dùng xử lý ảnh tài liệu theo quy trình tối ưu dung lượng:

1. Chọn hoặc kéo thả folder ảnh.
2. Convert ảnh sang AVIF siêu nhẹ trước.
3. Cho người dùng tinh chỉnh chất lượng và resolution đến khi ảnh nhẹ nhưng vẫn rõ.
4. Combine các ảnh AVIF đã tối ưu thành PDF.
5. Cho người dùng chỉnh chất lượng PDF bằng thanh kéo q cho đến khi file vừa nhẹ vừa đạt chất lượng mong muốn.

Ứng dụng hướng tới người dùng không kỹ thuật, không cần nhớ lệnh FFmpeg, không cần hiểu `JpegQ`, `CRF`, `MaxLongEdge`, AVIF/PDF encode. App phải biến các tham số kỹ thuật thành các lựa chọn dễ hiểu như "Đẹp hơn", "Nhẹ hơn", "Giữ nguyên kích thước", "Tài liệu có con dấu màu".

## 2. Vấn đề cần giải quyết

Hiện tại người dùng có thể nén ảnh rất nhẹ bằng AVIF, ví dụ tổng ảnh chỉ khoảng 1.38 MB. Tuy nhiên khi combine thành PDF, nếu encode sai cách, PDF có thể phình lên 4-5 MB.

Nguyên nhân chính:

- PDF không nhúng AVIF nguyên bản theo cách phổ biến.
- AVIF phải được decode rồi encode lại thành ảnh PDF hỗ trợ, thường là JPEG.
- Nếu JPEG quality quá cao, file PDF sẽ nặng.
- Nếu dùng grayscale, file nhẹ hơn nhưng mất màu con dấu, chữ ký, logo.
- Nếu ép A4, ảnh bị đặt vào trang không đúng ý người dùng.

App cần xử lý đúng bài toán thực tế:

- Giữ màu RGB mặc định để không mất con dấu.
- Giữ kích thước và orientation ảnh gốc khi combine PDF.
- Cho chỉnh q lên xuống trực quan.
- Cho xem kết quả dung lượng sau mỗi lần tạo.
- Cho tạo lại nhanh đến khi người dùng ưng ý.

## 3. Người dùng mục tiêu

### Người dùng chính

Nhân sự văn phòng, tư vấn hồ sơ, editor, admin hoặc người xử lý tài liệu cần:

- Nén ảnh giấy tờ, sao kê, hồ sơ.
- Gộp nhiều ảnh thành một file PDF.
- Giữ file đủ nhẹ để gửi email, upload hệ thống, gửi khách hàng.
- Không làm mất dấu đỏ, chữ ký, logo hoặc chi tiết quan trọng.

### Bối cảnh sử dụng

- Làm việc trên Windows.
- Có nhiều ảnh chụp điện thoại hoặc ảnh scan.
- Cần xuất PDF nhẹ nhưng vẫn nhìn rõ.
- Cần thử nhiều mức chất lượng nhanh.

## 4. Công nghệ đề xuất

### Stack chính

Sử dụng:

```text
C# .NET 8 + WinUI 3 / Windows App SDK + FFmpeg
```

### Lý do chọn

- WinUI 3 / Windows App SDK cho giao diện Fluent native hơn và phù hợp app suite nhiều module.
- Dùng native controls như `NavigationView`, `ListView`, `InfoBar`, `ContentDialog`, `ProgressBar` thay vì tự vẽ lại control phổ thông.
- Gọi FFmpeg bằng `Process` đơn giản và ổn định, nhưng command/process logic phải nằm trong infrastructure/service, không nằm trong XAML code-behind.
- Đóng gói app Windows tốt hơn Python và vẫn có thể phát triển installer sau này.
- Core/shared/infrastructure phải tách khỏi UI để tránh vỏ WinUI nhưng ruột workflow rời rạc.

### Không ưu tiên ở MVP

- Python desktop app: đóng gói nặng, UI kém native hơn.
- Web app: không tối ưu cho xử lý file local hàng loạt trên Windows.

## 5. Quy trình xử lý bắt buộc

App phải xử lý theo đúng thứ tự:

```text
Ảnh gốc
-> Bước 1: Convert sang AVIF siêu nhẹ
-> Bước 2: Người dùng xem dung lượng/tinh chỉnh
-> Bước 3: Combine AVIF thành PDF
-> Bước 4: Người dùng chỉnh q để PDF đạt dung lượng/chất lượng mong muốn
```

Không combine thẳng từ ảnh gốc ở luồng chính, vì mục tiêu sản phẩm là tối ưu theo quy trình đã kiểm chứng: nén AVIF trước, sau đó mới combine PDF.

## 6. Module chức năng

### 6.1. Chọn file/folder

Người dùng có thể:

- Chọn folder ảnh.
- Kéo thả folder vào app.
- Kéo thả nhiều file ảnh.

App hỗ trợ input:

```text
jpg, jpeg, png, avif, bmp, tif, tiff
```

App hiển thị:

- Tổng số ảnh.
- Tổng dung lượng ảnh gốc.
- Danh sách file theo thứ tự tên.
- Cảnh báo nếu có file không hỗ trợ.

### 6.2. Bước 1: Convert sang AVIF

Đây là bước bắt buộc trong workflow chính.

Người dùng có thể chỉnh:

- Định dạng output:
  - AVIF mặc định.
- Chất lượng:
  - High.
  - Small.
  - Lossless.
  - Custom.
- Resolution:
  - Giữ nguyên.
  - 1920px cạnh dài.
  - 2048px cạnh dài.
  - 2560px cạnh dài.
  - Custom.
- Thông số codec nâng cao theo từng định dạng:
  - AVIF: CRF, CPU effort.
  - PDF: JPEG q khi combine.

Giao diện không nên hiển thị quá kỹ thuật ngay từ đầu. Nên dùng nhãn:

```text
Chất lượng ảnh:
[ Đẹp hơn ] ----o---- [ Nhẹ hơn ]

Resolution:
[ Giữ nguyên ] [ 2048 ] [ 2560 ] [ Custom ]
```

Mapping kỹ thuật đề xuất:

| Mode | AVIF CRF | Mục tiêu |
|---|---:|---|
| High | 24 | Gần ảnh gốc, vẫn nhẹ |
| Small | 32 | Ưu tiên nhẹ hơn |
| Lossless | 0 | Giữ dữ liệu tối đa, không chắc nhẹ |
| Custom | Người dùng chỉnh | Cho người dùng nâng/hạ theo thực tế |

Control chỉnh chất lượng AVIF phải dùng cùng logic với PDF để người dùng học một lần là dùng được cả hai bước:

```text
                         [24]
< nút giảm > [ Đẹp hơn ] ----o---- [ Nhẹ hơn ] < nút tăng >
```

Quy tắc control AVIF:

- Số hiển thị là `CRF` hiện hành.
- Mỗi lần bấm tăng hoặc giảm thay đổi 2 đơn vị.
- CRF càng thấp -> ảnh càng đẹp -> file càng nặng.
- CRF càng cao -> ảnh càng nhẹ -> ảnh mềm hơn.
- `CRF 0` là lossless cho AVIF.
- Dải mặc định nên cho chỉnh từ `0` đến `40`, nhưng preset thực dụng nên nằm trong `20-34`.

Preset AVIF đề xuất:

| Preset | CRF | Mục tiêu |
|---|---:|---|
| Lossless | 0 | Giữ tối đa dữ liệu ảnh |
| Rất đẹp | 18 | File lớn hơn, chi tiết tốt |
| Đẹp | 22 | Chất lượng cao |
| High | 24 | Mặc định khuyến nghị |
| Cân bằng | 28 | Nhẹ hơn nhưng vẫn ổn |
| Small | 32 | Ưu tiên nhẹ |
| Rất nhẹ | 36 | File nhỏ, ảnh mềm hơn |

### 6.2.1. WebP

Không làm WebP/Both trong MVP đầu tiên.

Lý do:

- Bộ test thực tế đã chứng minh AVIF đạt hiệu quả tốt.
- MVP cần tập trung workflow đã kiểm chứng: AVIF -> PDF RGB -> chỉnh q -> final.
- Nếu sau này có nhu cầu tương thích định dạng khác, WebP sẽ được đánh giá lại ở V2.

App cần hiển thị kết quả sau convert:

- Dung lượng gốc.
- Dung lượng AVIF.
- Tỷ lệ tiết kiệm.
- File nào bị nặng hơn gốc.
- File nào lỗi convert.

Nếu file output nặng hơn file gốc, app không được im lặng bỏ qua. UI phải hiển thị warning rõ:

```text
File output đang nặng hơn file gốc. Hãy tăng mức nén, giảm resolution hoặc đổi preset.
```

Tuỳ chọn xử lý:

- Giữ file output nếu người dùng bật "vẫn giữ file nặng hơn".
- Bỏ qua output và giữ file gốc nếu người dùng bật "chỉ giữ file nhẹ hơn".
- Cho nút "Thử nén mạnh hơn" để tăng CRF/q hoặc giảm quality theo đúng định dạng đang dùng.

### 6.3. Bước 2: Review ảnh AVIF đã nén

Sau khi convert, app mở màn review:

- Danh sách ảnh AVIF.
- Preview ảnh đang chọn.
- Dung lượng từng ảnh.
- Tổng dung lượng folder AVIF.
- Nút "Tạo lại AVIF với cấu hình khác".
- Nút "Tiếp tục combine PDF".

Người dùng có thể quay lại chỉnh:

- Chất lượng AVIF.
- Resolution.
- Convert lại toàn bộ hoặc chỉ ảnh được chọn.

### 6.4. Bước 3: Combine AVIF thành PDF

App combine từ folder AVIF đã tối ưu.

Yêu cầu quan trọng:

- Không ép A4 mặc định.
- Không thêm margin mặc định.
- Không đổi orientation ảnh.
- Không dùng grayscale mặc định.
- Giữ màu RGB mặc định để không mất con dấu.

Page mode mặc định:

```text
Theo kích thước ảnh
```

Tức là:

- Ảnh dọc -> trang PDF dọc.
- Ảnh ngang -> trang PDF ngang.
- Trang PDF lấy kích thước/tỷ lệ từ ảnh.
- Không tự đặt ảnh vào khung A4.

Các mode phụ:

- Theo kích thước ảnh: mặc định.
- Fit A4: thấy trọn ảnh nhưng có thể có viền.
- Full A4: phủ kín A4, có thể crop nhẹ.

### 6.5. Bước 4: Chỉnh q cho PDF

Đây là tính năng rất quan trọng vì người dùng đã kiểm chứng thực tế rằng chỉnh q giúp đạt dung lượng mong muốn.

Không hiển thị `JpegQ` như thuật ngữ chính. Giao diện nên là:

```text
Chất lượng PDF:
[ Đẹp hơn ] ----o---- [ Nhẹ hơn ]
```

Bên dưới có thể hiển thị nâng cao:

```text
q hiện tại: 12
```

Control PDF phải thể hiện rõ số q hiện hành và cho chỉnh nhanh như sau:

```text
                         [8]
< nút giảm > [ Đẹp hơn ] ----o---- [ Nhẹ hơn ] < nút tăng >
```

Quy tắc control PDF:

- Số phía trên là `q` hiện hành.
- Nút giảm làm q giảm 2 đơn vị.
- Nút tăng làm q tăng 2 đơn vị.
- Slider cũng snap theo bước 2 đơn vị để người dùng không phải chỉnh quá mịn.
- Dải chỉnh nhanh mặc định: `4-20`.
- Dải advanced: `1-30`.
- Với JPEG trong PDF, `q 1` là mức đẹp nhất thực dụng; không nên gọi `q 0` là lossless thật.
- Nếu UI có hiển thị `0`, app nên map nội bộ sang `q 1` và ghi là "Best", không ghi "Lossless".

Quy tắc:

- q càng thấp -> ảnh càng đẹp -> PDF càng nặng.
- q càng cao -> PDF càng nhẹ -> ảnh mềm hơn.

Mapping preset PDF đề xuất:

| Mức UI | q | Mục tiêu |
|---|---:|---|
| Best | 1-2 | Đẹp nhất thực dụng, file rất lớn |
| Rất đẹp | 4 | Chữ rất rõ, file lớn hơn |
| Đẹp | 6 | Chất lượng cao |
| Đẹp nhẹ | 8 | Vẫn rõ, bắt đầu nhẹ |
| Cân bằng | 10 | Điểm bắt đầu tốt |
| Khuyến nghị | 12 | Thường cân bằng đẹp/nhẹ |
| Nhẹ | 14 | Nhẹ hơn, vẫn hợp tài liệu |
| Rất nhẹ | 16 | File nhỏ, ảnh mềm hơn |
| Siêu nhẹ | 18-20 | Khi cần ép dung lượng |
| Advanced | 22-30 | Chỉ dùng khi cần rất nhẹ và chấp nhận giảm rõ |

App cần cho người dùng tạo lại PDF nhanh nhiều lần:

- Tạo PDF.
- Xem dung lượng.
- Nếu chưa ưng, chỉnh thanh q.
- Bấm tạo lại.
- Không cần convert AVIF lại nếu ảnh AVIF đã ổn.

Sau mỗi lần tạo PDF, app phải lưu và hiển thị danh sách bản PDF đã tạo trong phiên làm việc:

```text
PDF versions
- q12-rgb: 1.40 MB
- q14-rgb: 1.33 MB
- q15-rgb: 1.21 MB
- final: 1.40 MB
```

Mỗi bản PDF trong danh sách cần có:

- Tên file.
- q đã dùng.
- Color mode.
- Page mode.
- Dung lượng.
- Nút mở file.
- Nút đặt làm final.
- Warning nếu bản đó dùng Gray mode hoặc có khả năng mất màu dấu.

### 6.6. Target file size

Không làm target size/auto optimize trong MVP đầu tiên.

Nếu sau này có yêu cầu, tính năng này sẽ được đánh giá ở V2:

Người dùng nhập dung lượng mong muốn:

```text
Mục tiêu: 1.5 MB
```

App tự thử q theo vòng lặp:

1. Bắt đầu từ q 12.
2. Nếu PDF lớn hơn target, tăng q.
3. Nếu PDF nhỏ hơn target quá nhiều, giảm q để đẹp hơn.
4. Dừng khi gần target hoặc đạt số lần thử tối đa.

Ví dụ:

```text
Target: 1.5 MB
Thử q12 -> 1.40 MB -> đạt
```

Hoặc:

```text
Target: 1.3 MB
Thử q12 -> 1.40 MB
Thử q13 -> 1.33 MB
Thử q14 -> 1.25 MB
Chọn q13 vì gần target nhất mà chất lượng tốt hơn q14.
```

## 7. Giao diện đề xuất

### Layout chính

Ứng dụng có một màn chính chia 3 vùng:

```text
Sidebar trái: Workflow
Khu giữa: Danh sách file + preview
Panel phải: Thiết lập + kết quả dung lượng
```

### Workflow sidebar

```text
1. Chọn ảnh
2. Nén AVIF
3. Review
4. Combine PDF
5. Xuất file
```

Mỗi bước có trạng thái:

- Chưa làm.
- Đang chạy.
- Hoàn tất.
- Có lỗi.

### Trạng thái UI bắt buộc

Mỗi vùng UI chính phải có đủ trạng thái để tránh app trông như bị treo hoặc khó hiểu:

| State | Khi nào dùng | UI cần thể hiện |
|---|---|---|
| Empty | Chưa chọn file/folder | Vùng kéo thả, nút chọn folder, mô tả ngắn |
| Loading | Đang scan, convert hoặc combine | Progress bar, file hiện tại, nút huỷ nếu có thể |
| Success | Chạy xong | Tổng dung lượng, số file xử lý thành công, nút bước tiếp theo |
| Warning | Output nặng hơn gốc, dùng Gray, thiếu FFmpeg, file bị bỏ qua | Icon cảnh báo, lý do, gợi ý xử lý |
| Error | FFmpeg lỗi, file hỏng, không ghi được output | Lỗi rõ ràng, nút retry, copy log |
| Disabled | Chưa đủ điều kiện thao tác | Button mờ, tooltip giải thích điều kiện còn thiếu |

Các button chính phải disabled đúng ngữ cảnh:

- Chưa chọn ảnh thì không cho convert.
- Chưa có AVIF thì không cho combine PDF ở workflow chính.
- Đang chạy FFmpeg thì không cho chạy thêm job mới cùng loại.
- Output đã tồn tại thì yêu cầu overwrite hoặc đổi tên.

### Panel thiết lập AVIF

Các control chính:

- Output format: AVIF.
- Quality stepper-slider: hiển thị CRF hiện hành, nút giảm/tăng mỗi lần 2 đơn vị, nhãn Đẹp hơn -> Nhẹ hơn.
- Resolution: giữ nguyên / 2048 / 2560 / custom.
- Checkbox: bỏ qua file nếu output nặng hơn gốc.

### Panel thiết lập PDF

Các control chính:

- Page mode:
  - Theo kích thước ảnh.
  - Fit A4.
  - Full A4.
- Color mode:
  - RGB mặc định.
  - Gray chỉ dùng khi người dùng tự chọn.
- PDF quality stepper-slider:
  - Hiển thị q hiện hành phía trên.
  - Nút giảm/tăng mỗi lần 2 đơn vị.
  - Nhãn Đẹp hơn -> Nhẹ hơn.
  - Preset nhanh từ q4 đến q20.
  - Advanced range từ q1 đến q30.
- q advanced:
  - Hiện giá trị q.
  - Cho nhập q thủ công nếu bật nâng cao.
- Target size: không làm trong MVP.

### Preview và lịch sử PDF

Panel PDF cần có khu vực preview trạng thái sau mỗi lần tạo:

```text
Các bản PDF đã tạo
q12-rgb    1.40 MB    [Mở] [Đặt final]
q14-rgb    1.33 MB    [Mở] [Đặt final]
q15-rgb    1.21 MB    [Mở] [Đặt final]
final      1.40 MB    [Mở]
```

Mục tiêu là cho người dùng chỉnh q nhiều lần giống workflow thực tế, sau đó chọn bản ưng ý nhất làm final.

### Layout tối thiểu

App phải tối ưu cho màn hình Windows phổ biến:

```text
1366 x 768
```

Yêu cầu layout:

- Không được phụ thuộc vào màn hình lớn.
- Sidebar trái gọn, không quá rộng.
- Panel phải có thể scroll.
- Khu preview/list ở giữa co giãn theo màn hình.
- Button quan trọng không bị đẩy khỏi màn hình.
- Text trong button/label không bị tràn.
- Trên màn hình thấp, phần setting nâng cao nằm trong accordion hoặc scroll area.

### Component stepper-slider dùng chung

AVIF và PDF nên dùng cùng một component UI để người dùng không phải học lại:

```text
                         [giá trị hiện hành]
< nút giảm > [ Đẹp hơn ] ----o---- [ Nhẹ hơn ] < nút tăng >
```

Yêu cầu component:

- Có số hiện hành ở phía trên slider.
- Có nút giảm bên trái và nút tăng bên phải.
- Mỗi lần bấm nút thay đổi 2 đơn vị.
- Slider snap theo bước 2 đơn vị.
- Có tooltip giải thích: số thấp hơn đẹp hơn nhưng nặng hơn, số cao hơn nhẹ hơn nhưng mềm hơn.
- Có preset nhanh dưới slider nếu cần: `4`, `8`, `12`, `16`, `20`.
- Có chế độ advanced để nhập số thủ công trong dải cho phép.

Mapping theo từng bước:

| Bước | Giá trị | Dải nhanh | Dải advanced | Ghi chú |
|---|---|---:|---:|---|
| Nén AVIF | CRF | 18-36 | 0-40 | CRF 0 là lossless |
| Combine PDF | JPEG q | 4-20 | 1-30 | q 1 là đẹp nhất thực dụng, không gọi q 0 là lossless |

## 8. Business rules quan trọng

### 8.1. Không tự chuyển gray

Mặc định luôn dùng RGB.

Lý do:

- Hồ sơ có thể có con dấu đỏ.
- Chữ ký, logo, watermark có thể cần màu.
- Gray làm mất dấu và gây rủi ro hồ sơ.

Gray chỉ là optional advanced mode, có cảnh báo:

```text
Chế độ gray có thể làm mất màu con dấu, chữ ký, logo.
```

Khi người dùng bật Gray mode, app phải hiện xác nhận trước khi áp dụng:

```text
Gray mode có thể làm mất màu con dấu đỏ, chữ ký, logo hoặc watermark. Bạn chắc chắn muốn bật?
```

Nút xác nhận:

- "Giữ RGB" là lựa chọn mặc định.
- "Vẫn bật Gray" là lựa chọn phụ.

Sau khi bật Gray, UI phải hiển thị badge cảnh báo trong panel PDF và trong từng PDF version được tạo bằng Gray mode.

### 8.2. Không ép A4 mặc định

Mặc định giữ kích thước và orientation ảnh.

Lý do:

- Người dùng chỉ muốn gộp ảnh thành file PDF.
- Không muốn ảnh bị đặt vào một trang A4 với viền.
- Không muốn ảnh bị crop hoặc scale ngoài ý muốn.

### 8.3. Không sửa file gốc

App luôn tạo output vào folder riêng:

```text
compressed-avif
pdf-output
```

File gốc không bị sửa, xoá, ghi đè.

### 8.4. Cho tạo lại nhanh

Người dùng phải có thể chỉnh q và tạo lại PDF nhiều lần trong vài giây.

Không bắt người dùng quay lại nén AVIF nếu chỉ đang chỉnh PDF.

## 9. Cấu trúc output đề xuất

Với folder input:

```text
Sao ke GD
```

App tạo:

```text
Sao ke GD
├── compressed-avif
│   ├── file-1.avif
│   ├── file-2.avif
│   └── ...
└── pdf-output
    ├── Sao ke GD-q12-rgb.pdf
    ├── Sao ke GD-q13-rgb.pdf
    └── Sao ke GD-final.pdf
```

Tên file PDF nên có thông tin cấu hình:

```text
<folder-name>-q12-rgb.pdf
<folder-name>-q13-rgb.pdf
<folder-name>-q15-rgb.pdf
```

Khi người dùng bấm "Đặt làm bản cuối", app copy/rename thành:

```text
<folder-name>-final.pdf
```

## 10. FFmpeg command mapping

### 10.1. Convert AVIF

High:

```powershell
ffmpeg -i input.jpg -frames:v 1 -c:v libaom-av1 -crf 24 -cpu-used 4 -pix_fmt yuv420p output.avif
```

Small:

```powershell
ffmpeg -i input.jpg -frames:v 1 -c:v libaom-av1 -crf 32 -cpu-used 5 -pix_fmt yuv420p output.avif
```

Lossless:

```powershell
ffmpeg -i input.jpg -frames:v 1 -c:v libaom-av1 -crf 0 -cpu-used 4 output.avif
```

Resize cạnh dài:

```powershell
-vf "scale='if(gt(iw,ih),min(iw,2048),-2)':'if(gt(iw,ih),-2,min(ih,2048))'"
```

### 10.2. Prepare ảnh để nhúng PDF

PDF dùng JPEG stream để dung lượng nhẹ và tương thích rộng.

RGB mặc định:

```powershell
ffmpeg -i input.avif -frames:v 1 -q:v 12 -pix_fmt yuvj420p temp.jpg
```

Không dùng gray mặc định.

Gray optional:

```powershell
ffmpeg -i input.avif -vf format=gray -frames:v 1 -q:v 12 -pix_fmt gray temp.jpg
```

## 11. Thuật toán tự tối ưu dung lượng PDF

Không làm trong MVP đầu tiên. Phần dưới chỉ là ghi chú tham khảo cho V2 nếu sau này có yêu cầu rõ.

Input V2 dự kiến:

- Danh sách ảnh AVIF.
- Target size MB.
- q min, ví dụ 4.
- q max, ví dụ 20.

Quy trình V2 dự kiến:

1. Chọn q mặc định là 12.
2. Tạo PDF thử.
3. Đo dung lượng PDF.
4. Nếu dung lượng <= target và q thấp nhất có thể trong vùng đạt target, chọn file đó.
5. Nếu dung lượng > target, tăng q theo bước 2.
6. Nếu dung lượng nhỏ hơn target quá nhiều, giảm q theo bước 2 để tăng chất lượng.
7. Lưu lại file tốt nhất.

Tiêu chí chọn:

```text
Ưu tiên chất lượng cao nhất trong giới hạn dung lượng mục tiêu.
```

Không chọn file nhỏ nhất nếu file đó xấu hơn không cần thiết.

## 12. MVP scope

### Bản MVP cần có

- Chọn folder ảnh.
- Convert sang AVIF.
- Chỉnh quality AVIF: High / Small / Lossless / Custom.
- Chỉnh resolution: giữ nguyên / 2048 / 2560 / custom.
- Review tổng dung lượng AVIF.
- Combine AVIF thành PDF.
- Page mode mặc định: theo kích thước ảnh.
- Color mode mặc định: RGB.
- Slider PDF q.
- Tạo lại PDF nhanh.
- Hiển thị dung lượng PDF sau mỗi lần tạo.
- Log lỗi rõ ràng.

### Chưa cần ở MVP

- OCR.
- Ký số.
- Upload cloud.
- Batch nhiều folder cùng lúc.
- WebP/Both.
- Target size/auto optimize.
- Theme UI phức tạp.
- Drag reorder nâng cao.

## 13. Version tiếp theo

### V2 nếu có yêu cầu

- Target file size auto optimize.
- Preview trước/sau.
- So sánh nhiều bản PDF.
- Nút "Chọn bản này làm final".

### V2/V3 nếu có yêu cầu

- Batch nhiều folder.
- Preset theo loại tài liệu:
  - Sao kê.
  - Hồ sơ tài chính.
  - Ảnh giấy tờ có dấu.
  - Ảnh web.
- Lưu preset người dùng.
- WebP/Both nếu cần tương thích định dạng khác.

### Đóng gói release

- Installer Windows.
- Bundle FFmpeg sẵn trong app/package.
- Tích hợp menu chuột phải: "Optimize images to PDF".

## 14. Rủi ro và cách xử lý

### Rủi ro: PDF lớn hơn tổng AVIF

Nguyên nhân:

- PDF không giữ AVIF nguyên bản.
- Ảnh bị re-encode thành JPEG.

Cách xử lý:

- Cho chỉnh q.
- Cho chỉnh q thủ công nhiều lần; target size chỉ làm ở V2 nếu có yêu cầu.
- Dùng RGB nhưng nén hợp lý.
- Không dùng JPEG quality quá cao mặc định.

### Rủi ro: Mất màu con dấu

Nguyên nhân:

- Dùng grayscale.

Cách xử lý:

- RGB là mặc định.
- Gray là advanced mode có cảnh báo.

### Rủi ro: Ảnh bị ép vào A4

Nguyên nhân:

- Page mode mặc định sai.

Cách xử lý:

- Mặc định dùng page theo kích thước ảnh.
- A4 chỉ là option phụ.

### Rủi ro: Người dùng không hiểu q

Nguyên nhân:

- q là thuật ngữ kỹ thuật.

Cách xử lý:

- Hiển thị slider "Đẹp hơn" và "Nhẹ hơn".
- q chỉ hiện trong advanced mode.

## 15. Acceptance criteria

App được xem là đạt MVP khi:

- Người dùng chọn một folder ảnh và convert được sang AVIF.
- Tổng dung lượng AVIF giảm rõ rệt so với ảnh gốc.
- Người dùng combine AVIF thành PDF mà không bị ép A4.
- PDF giữ màu con dấu/chữ ký/logo.
- Người dùng chỉnh slider PDF quality và tạo lại PDF nhanh.
- App hiển thị dung lượng từng bản PDF.
- App hiển thị danh sách các bản PDF đã tạo như q12, q14, q15 và final.
- App có đủ trạng thái empty, loading, success, warning, error, disabled.
- Khi output nặng hơn file gốc, app hiển thị warning và gợi ý tăng mức nén hoặc giảm resolution.
- Khi bật Gray mode, app yêu cầu xác nhận vì có thể mất màu con dấu/chữ ký/logo.
- Layout dùng tốt trên màn hình 1366x768, panel phải có thể scroll.
- Có thể tạo bản PDF khoảng 1.3-1.5 MB từ bộ ảnh AVIF khoảng 1.38 MB nếu chất lượng cho phép.
- File gốc không bị sửa.
- Log dễ hiểu khi có lỗi.

## 16. Kết luận

Sản phẩm nên bắt đầu bằng một app Windows native nhỏ, tập trung đúng workflow đã kiểm chứng:

```text
Nén AVIF trước -> Review dung lượng -> Combine PDF màu -> Chỉnh q đến khi ưng -> Xuất bản final
```

Điểm mạnh của app không nằm ở việc có nhiều thuật toán phức tạp, mà nằm ở việc che bớt tham số kỹ thuật và cho người dùng kiểm soát kết quả bằng các thanh chỉnh dễ hiểu. Người dùng chỉ cần biết:

```text
Đẹp hơn hay nhẹ hơn?
Giữ nguyên hay giảm resolution?
PDF đã đạt dung lượng mong muốn chưa?
```

Đây là hướng sản phẩm phù hợp để build bằng `.NET 8 + WinUI 3 / Windows App SDK + FFmpeg`, với điều kiện UI WinUI và core workflow được thiết kế đồng bộ để tránh tình trạng vỏ Fluent nhưng ruột vẫn là form/tool rời rạc.
