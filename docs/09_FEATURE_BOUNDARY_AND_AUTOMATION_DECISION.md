# Feature Boundary & Automation Decision

## 1. Quyết định chốt ngày 2026-06-04

App phải giữ đúng trách nhiệm từng feature:

```text
Nén là nén.
Gộp là gộp.
```

Không gom luồng xử lý ảnh, gộp file và nén PDF vào một màn hình lớn. Làm vậy khiến workflow khó hiểu, khó sửa, khó test và làm sai mental model app suite.

## 2. Ranh giới feature mới

| Feature | Trách nhiệm chính | UI chính |
|---|---|---|
| Image Optimizer | Nén/tối ưu ảnh, ví dụ AVIF | File list + setting nén ảnh |
| File Merge / PDF Builder | Gộp nhiều file/ảnh/PDF theo thứ tự thành một file PDF | List file view + ordering + setting gộp nếu cần |
| PDF Compressor | Nén PDF đã có hoặc PDF vừa được tạo từ feature gộp | PDF preview + setting nén PDF + version/result |

Không feature nào làm thay trách nhiệm cốt lõi của feature khác.

## 3. Luồng người dùng chuẩn

### 3.1. Nén ảnh độc lập

```text
Image Optimizer
-> Import ảnh/folder
-> Chỉnh thông số nén ảnh
-> Nén ảnh
-> Review list file, dung lượng, chênh lệch
-> Có thể nén lại bất cứ lúc nào
```

Sau bước này app chỉ tạo output ảnh đã nén, ví dụ:

```text
compressed-avif
```

Không tự chuyển sang tạo PDF nếu người dùng không chọn.

### 3.2. Gộp file độc lập

```text
File Merge / PDF Builder
-> Import file/folder hoặc nhận batch từ Image Optimizer
-> Hiển thị list file theo thứ tự số tự nhiên
-> Cho chỉnh thứ tự nếu cần
-> Gộp thành PDF
-> Trả ra PDF output
```

Feature này tập trung vào list file và thứ tự gộp. Không biến nó thành màn chỉnh nén PDF.

### 3.3. Nén PDF độc lập

```text
PDF Compressor
-> Import PDF hoặc nhận PDF từ File Merge
-> Auto preview PDF giống như import file mới
-> Chỉnh setting nén PDF
-> Nén lại nhiều lần cho đến khi hài lòng
-> Chọn bản final
```

Feature này tập trung vào preview PDF, setting nén và các phiên bản output.

## 4. Automation/handoff giữa feature

Feature vẫn độc lập nhưng được nối bằng automation rõ ràng.

### Từ Image Optimizer

Top action có thể có:

```text
Gộp file
Gộp và nén
```

Ý nghĩa:

- `Gộp file`: gửi batch ảnh đang chọn/ảnh đã nén sang feature gộp file. App điều hướng sang `File Merge / PDF Builder` và prefill list.
- `Gộp và nén`: chạy pipeline tự động:

```text
Image Optimizer output
-> File Merge / PDF Builder dùng setting gộp hiện hành nếu có
-> tạo PDF
-> gửi PDF sang PDF Compressor
-> PDF Compressor auto preview
```

Sau automation, người dùng ở màn `PDF Compressor`, thấy PDF preview và có thể chỉnh setting nén PDF rồi nén lại đến khi final.

### Từ File Merge / PDF Builder

Sau khi gộp xong:

```text
Nén PDF
```

Action này gửi PDF vừa tạo sang `PDF Compressor`.

### Từ PDF Compressor

Người dùng có thể:

- Import PDF thủ công.
- Nhận PDF từ `File Merge / PDF Builder`.
- Nén lại nhiều lần.
- Mở output/final.

## 5. Không được làm

- Không để Image Optimizer tự gộp PDF và nén PDF trong cùng một màn.
- Không để PDF Compressor nhận ảnh rồi âm thầm gộp nếu UI đang nói là nén PDF.
- Không đặt một nút `Nén` nhưng phía sau vừa gộp, vừa convert, vừa nén.
- Không làm tab setting `Ảnh/PDF` trong cùng một feature nếu hai tác vụ có trách nhiệm khác nhau.
- Không làm mất khả năng quay lại Image Optimizer để nén lại ảnh.

## 6. UI guideline theo quyết định mới

### Image Optimizer

Workspace:

- ListView ảnh.
- Dung lượng gốc/output.
- Chênh lệch.
- Status.

Panel:

- Thiết lập nén ảnh.
- Nút nén ảnh.
- Summary sau nén.

Top action:

- `Gộp file`.
- `Gộp và nén`.

### File Merge / PDF Builder

Workspace:

- List file view.
- Sắp theo thứ tự số tự nhiên.
- Reorder nếu cần.

Panel:

- Setting gộp.
- Page mode nếu input là ảnh.
- Output name/folder.

Primary action:

- `Gộp file`.

### PDF Compressor

Workspace:

- PDF preview.
- Page navigation/zoom.

Panel:

- Thiết lập nén PDF.
- PDF quality/q.
- Color mode nếu có.
- Version/result list.

Primary action:

- `Nén PDF`.
- `Nén lại`.
- `Chọn final`.

## 7. Handoff contract tối thiểu

Các feature không gọi UI trực tiếp của nhau. Chúng truyền dữ liệu qua một contract/job context chung.

```text
FileBatchContext
- BatchId
- SourceFeature
- SourceFolder
- Files
- OutputFolder
- SuggestedOrder
- CreatedAt
```

```text
PdfJobContext
- JobId
- SourceFeature
- PdfPath
- OutputFolder
- SuggestedSettings
- CreatedAt
```

Shell/module router chịu trách nhiệm điều hướng sang feature đích với context phù hợp.

## 8. Điều chỉnh tên/scope docs cũ

Các tài liệu cũ có câu như:

```text
Image -> AVIF -> PDF Optimizer
AVIF -> PDF RGB -> chỉnh q -> final
Combine PDF nằm trong Feature 01
```

phải được hiểu là đã bị thay thế bởi quyết định này.

Tên đúng cho Feature 01 nên là:

```text
Image Optimizer
```

PDF combine và PDF compress không còn là trách nhiệm trực tiếp của Feature 01.
