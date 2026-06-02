# Product Requirements: Image PDF Optimizer App

## 1. Bài toán

Người dùng cần nén nhiều ảnh tài liệu thành file PDF nhẹ nhưng vẫn rõ, đặc biệt với hồ sơ có con dấu màu, chữ ký, logo hoặc watermark.

Điểm khó là AVIF có thể rất nhẹ, nhưng khi combine thành PDF, ảnh thường phải decode và encode lại. Nếu cấu hình PDF sai, file PDF sẽ phình lên nhiều lần.

## 2. Người dùng mục tiêu

- Nhân sự xử lý hồ sơ.
- Tư vấn viên.
- Admin văn phòng.
- Editor hoặc người cần gửi file nhẹ cho khách/sếp/hệ thống.

Người dùng không cần hiểu codec. App phải cho chỉnh bằng ngôn ngữ dễ hiểu.

## 3. Workflow chính

```text
Mở app
-> Chọn hoặc kéo thả folder ảnh
-> App scan ảnh hợp lệ
-> Người dùng chỉnh cấu hình nén ảnh
-> Convert AVIF
-> Review dung lượng
-> Combine PDF
-> Chỉnh q PDF
-> Tạo lại PDF nếu chưa ưng
-> Chọn bản final
```

## 4. Input hỗ trợ

```text
jpg, jpeg, png, avif, bmp, tif, tiff
```

File không hỗ trợ không làm dừng toàn bộ job. App phải bỏ qua và hiển thị warning.

## 5. Output folder

Với folder input:

```text
Sao ke GD
```

App tạo:

```text
Sao ke GD
├── compressed-avif
└── pdf-output
```

File gốc không bị sửa.

## 6. Convert ảnh

### AVIF

AVIF là luồng chính của MVP.

Thông số người dùng được chỉnh:

- CRF.
- CPU effort.
- Resolution / max long edge.
- Lossless.

Mapping:

| Preset | CRF | Mục tiêu |
|---|---:|---|
| Lossless | 0 | Giữ dữ liệu tối đa |
| Rất đẹp | 18 | Chất lượng cao, file lớn hơn |
| Đẹp | 22 | Rõ, vẫn tối ưu |
| High | 24 | Mặc định tốt |
| Cân bằng | 28 | Nhẹ hơn |
| Small | 32 | Ưu tiên nhẹ |
| Rất nhẹ | 36 | File nhỏ, ảnh mềm hơn |

### WebP

Không làm WebP trong MVP đầu tiên.

Lý do:

- Bộ test thực tế đã chứng minh AVIF đạt hiệu quả tốt.
- MVP cần tập trung luồng đã kiểm chứng: AVIF -> PDF RGB -> chỉnh q -> final.
- Nếu sau này có nhu cầu tương thích định dạng khác, WebP sẽ được đánh giá lại như một scope V2, không phải backlog bắt buộc.

## 7. Resolution

App cho chọn:

- Giữ nguyên.
- 1920px cạnh dài.
- 2048px cạnh dài.
- 2560px cạnh dài.
- Custom.

Không upscale ảnh nhỏ.

Gợi ý:

- 2048px: nhẹ, phù hợp upload/gửi.
- 2560px: rõ hơn cho tài liệu có chữ nhỏ.
- Giữ nguyên: dùng khi cần tối đa chi tiết.

## 8. Combine PDF

PDF combine phải dùng ảnh đã nén ở bước trước.

Mặc định:

- Page mode: theo kích thước ảnh.
- Color mode: RGB.
- Không ép A4.
- Không margin.
- Không đổi orientation.

Mode phụ:

- Fit A4.
- Full A4.

## 9. PDF q

PDF q dùng để kiểm soát dung lượng khi nhúng ảnh vào PDF.

Nguyên tắc:

- q càng thấp -> đẹp hơn -> file nặng hơn.
- q càng cao -> nhẹ hơn -> ảnh mềm hơn.
- Dải nhanh: 4-20.
- Dải advanced: 1-30.
- q 1 là đẹp nhất thực dụng.

Preset:

| Preset | q | Mục tiêu |
|---|---:|---|
| Rất đẹp | 4 | Chữ rõ, file lớn |
| Đẹp | 6 | Chất lượng cao |
| Đẹp nhẹ | 8 | Bắt đầu nhẹ |
| Cân bằng | 10 | Điểm bắt đầu tốt |
| Khuyến nghị | 12 | Mặc định |
| Nhẹ | 14 | Nhẹ hơn |
| Rất nhẹ | 16 | File nhỏ hơn |
| Siêu nhẹ | 18-20 | Ép dung lượng |

## 10. PDF versions

Mỗi lần tạo PDF phải thêm một version vào danh sách:

```text
q12-rgb    1.40 MB
q14-rgb    1.33 MB
q15-rgb    1.21 MB
final      1.40 MB
```

Mỗi version có:

- Tên file.
- q.
- color mode.
- page mode.
- dung lượng.
- nút mở.
- nút đặt final.
- warning nếu có.

## 11. Business rules

- Không tự bật Gray.
- Bật Gray phải confirm.
- Gray version phải có warning badge.
- Output nặng hơn gốc phải warning.
- Thiếu FFmpeg phải chặn job và hướng dẫn cấu hình.
- File output tồn tại phải hỏi overwrite, tạo tên mới hoặc bỏ qua.
- Chỉ chỉnh q PDF thì không cần convert ảnh lại.
- FFmpeg phải được bundle sẵn trong app/package khi giao user; user cuối không cần tự cài FFmpeg hoặc cấu hình path.

## 12. Acceptance product

Product đạt khi người dùng làm được trọn luồng:

```text
Chọn folder -> Nén AVIF -> Review -> Tạo PDF -> Chỉnh q -> Chọn final
```

Với bộ sao kê thực tế, app phải giữ màu con dấu và tạo được PDF khoảng 1.3-1.5 MB nếu cấu hình q phù hợp.

Golden test set bắt buộc:

```text
C:\Users\INEC-EDITOR-2\Desktop\Chi Trieu\Sao ke GD
C:\Users\INEC-EDITOR-2\Desktop\Chi Trieu\Sao ke GD\compressed-avif
```

Kết quả đã kiểm chứng để đối chiếu:

- 10 ảnh JPG gốc tổng khoảng 8.32 MiB.
- 10 ảnh AVIF tổng khoảng 1.36 MiB.
- PDF RGB q12 khoảng 1.40 MiB.
- PDF RGB q13 khoảng 1.33 MiB.
- PDF RGB q15 khoảng 1.21 MiB.
