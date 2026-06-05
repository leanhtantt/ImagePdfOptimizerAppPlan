# Product Requirements: Image Optimizer

## 1. Bài toán

Người dùng cần nén nhiều ảnh tài liệu thành file ảnh nhẹ hơn nhưng vẫn rõ, đặc biệt với hồ sơ có con dấu màu, chữ ký, logo hoặc watermark.

Điểm khó là người dùng không kỹ thuật cần batch ảnh nhẹ hơn, có thể review chênh lệch dung lượng, rồi gửi batch đó sang feature gộp/nén PDF nếu cần.

Quyết định boundary mới:

```text
Nén ảnh nằm ở Image Optimizer.
Gộp file nằm ở File Merge / PDF Builder.
Nén PDF nằm ở PDF Compressor.
```

Chi tiết xem `09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md`.

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
-> Có thể nén lại ảnh
-> Có thể bấm Gộp file để chuyển batch sang File Merge / PDF Builder
-> Có thể bấm Gộp và nén để chạy automation sang PDF Compressor
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
└── compressed-avif
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
- MVP Image Optimizer cần tập trung luồng đã kiểm chứng: ảnh gốc -> AVIF -> review dung lượng -> handoff nếu cần.
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

## 8. Handoff sang feature khác

Image Optimizer có thể expose top actions:

```text
Gộp file
Gộp và nén
```

`Gộp file` gửi batch ảnh hiện tại hoặc output `compressed-avif` sang `File Merge / PDF Builder`.

`Gộp và nén` chạy automation:

```text
Image Optimizer output
-> File Merge / PDF Builder dùng setting gộp hiện hành nếu có
-> tạo PDF
-> gửi PDF sang PDF Compressor
-> PDF Compressor auto preview
```

Người dùng vẫn có thể quay lại Image Optimizer để nén lại ảnh bất cứ lúc nào.

## 9. Business rules

- Output nặng hơn gốc phải warning.
- Thiếu FFmpeg phải chặn job và hướng dẫn cấu hình.
- File output tồn tại phải hỏi overwrite, tạo tên mới hoặc bỏ qua.
- FFmpeg phải được bundle sẵn trong app/package khi giao user; user cuối không cần tự cài FFmpeg hoặc cấu hình path.
- Image Optimizer không tự gộp PDF hoặc nén PDF trong cùng màn.
- Automation giữa feature phải đi qua job context/handoff rõ ràng.

## 10. Acceptance product

Feature đạt khi người dùng làm được luồng:

```text
Chọn folder -> Nén AVIF -> Review chênh lệch -> Nén lại nếu cần -> Gửi batch sang feature gộp/nén nếu cần
```

Với bộ sao kê thực tế, Image Optimizer phải tạo được output AVIF nhẹ rõ ràng so với ảnh gốc.

Golden test set bắt buộc:

```text
C:\Users\INEC-EDITOR-2\Desktop\Chi Trieu\Sao ke GD
C:\Users\INEC-EDITOR-2\Desktop\Chi Trieu\Sao ke GD\compressed-avif
```

Kết quả đã kiểm chứng để đối chiếu:

- 10 ảnh JPG gốc tổng khoảng 8.32 MiB.
- 10 ảnh AVIF tổng khoảng 1.36 MiB.

Các mốc PDF RGB q12/q13/q15 chuyển sang acceptance của `File Merge / PDF Builder` và `PDF Compressor`.
