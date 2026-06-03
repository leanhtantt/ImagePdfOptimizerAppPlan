# Technical Architecture Plan

> Phạm vi: tài liệu này mô tả kiến trúc kỹ thuật cho app suite `FileUtilityHub` và module đầu tiên `Features/ImagePdfOptimizer`. Không đặt mental model app là `ImagePdfOptimizer` nữa.

## 1. Tổng quan

App dùng kiến trúc WinUI 3 shell + core/shared/infrastructure + feature modules.

```text
FileUtilityHub.App
-> Shared UI controls
-> Feature modules
-> Core contracts
-> Infrastructure services
-> FFmpeg / PDF writer / file system
```

Mục tiêu là tách app shell khỏi feature logic để sau này thêm PDF Compressor, PDF Converter, Word Tools, Excel Tools mà không phải viết lại core app.

## 2. Project structure đề xuất

```text
FileUtilityHub
├── FileUtilityHub.App
│   ├── Shell
│   ├── MainWindow.xaml
│   ├── Views
│   ├── ViewModels
│   ├── Resources
│   └── Program.cs
├── FileUtilityHub.Core
│   ├── Models
│   ├── Contracts
│   ├── Jobs
│   └── Results
├── FileUtilityHub.Shared
│   ├── Controls
│   ├── UI
│   ├── Formatting
│   └── Utilities
├── FileUtilityHub.Infrastructure
│   ├── Ffmpeg
│   ├── ProcessRunner
│   ├── Pdf
│   ├── FileSystem
│   └── Logging
├── Features
│   └── ImagePdfOptimizer
│       ├── Models
│       ├── Services
│       ├── Views
│       ├── Controls
│       └── ImagePdfOptimizerModule.cs
└── FileUtilityHub.Tests
    ├── Core
    ├── Infrastructure
    └── Features
        └── ImagePdfOptimizer
```

Nếu cần MVP nhanh, có thể bắt đầu một solution ít project hơn, nhưng folder/naming vẫn phải theo mental model `FileUtilityHub`, không đặt mọi thứ dưới `ImagePdfOptimizer`.

## 2.1. Ranh giới suite và feature

### App shell

Trách nhiệm:

- Main window.
- Navigation tab/module.
- Global status.
- Global settings.
- Global tool status.
- Render feature module đang chọn.

### Shared/Core/Infrastructure

Trách nhiệm:

- Contracts dùng chung.
- Job progress.
- Process runner.
- FFmpeg locator.
- File system/output manager.
- Log service.
- Shared UI controls.

### Feature: ImagePdfOptimizer

Trách nhiệm:

- Scan ảnh cho feature này.
- Convert AVIF.
- Review dung lượng.
- Combine PDF từ ảnh đã nén.
- PDF versions/final.
- Feature-specific settings và UI panel.

## 2.2. WinUI/core binding rule

Khi code UI bằng WinUI, bắt buộc giữ các ranh giới sau:

- `MainWindow`/shell chỉ render navigation, feature host, global status, global tool status và header actions.
- Feature view chỉ binding ViewModel state/commands; không build FFmpeg command trong XAML code-behind.
- Core models/results/warnings không reference WinUI.
- Infrastructure chạy process/file system/log; không show dialog hoặc toast.
- Shared WinUI controls chỉ nhận state/input và phát command/event; không chứa workflow nghiệp vụ.

Nếu một UI control cần biết path FFmpeg, build command hoặc ghi output file, đó là dấu hiệu vỏ và ruột đã lệch. Đưa logic đó về workflow service/infrastructure.

## 3. Models

### ImageItem

```csharp
public sealed class ImageItem
{
    public string Id { get; init; }
    public string FileName { get; init; }
    public string SourcePath { get; init; }
    public string Format { get; init; }
    public long OriginalSizeBytes { get; init; }
    public string? OutputPath { get; set; }
    public long? OutputSizeBytes { get; set; }
    public ProcessingStatus Status { get; set; }
    public string? Warning { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### PdfVersion

```csharp
public sealed class PdfVersion
{
    public string Id { get; init; }
    public string FileName { get; init; }
    public string Path { get; init; }
    public long SizeBytes { get; init; }
    public int JpegQ { get; init; }
    public string ColorMode { get; init; }
    public string PageMode { get; init; }
    public bool IsFinal { get; set; }
    public List<string> Warnings { get; init; } = new();
}
```

### Config

```csharp
public sealed class ImageConvertConfig
{
    public int AvifCrf { get; init; }
    public int AvifCpuUsed { get; init; }
    public int MaxLongEdge { get; init; }
    public bool SkipIfOutputLarger { get; init; }
}

public sealed class PdfConfig
{
    public string PageMode { get; init; }
    public string ColorMode { get; init; }
    public int JpegQ { get; init; }
}
```

## 4. Services

### FileScanService

Trách nhiệm:

- Scan folder/file.
- Lọc file ảnh hỗ trợ.
- Sort theo tên.
- Trả danh sách file hợp lệ và file bị bỏ qua.

### FfmpegLocator

Trách nhiệm:

- Tìm FFmpeg bundled trong app/package trước, ví dụ `tools/ffmpeg/bin/ffmpeg.exe`.
- Validate `ffmpeg.exe` bundled.
- Kiểm tra FFmpeg có hỗ trợ AVIF encode/decode cần thiết.
- Nếu thiếu hoặc lỗi, trả lỗi cài đặt thiếu thành phần xử lý; không hỏi user cuối nhập path thủ công trong MVP.

### FfmpegRunner

Trách nhiệm:

- Build command an toàn.
- Run process.
- Parse progress nếu có thể.
- Trả stdout/stderr.
- Hỗ trợ cancel.

### ImageConvertService

Trách nhiệm:

- Convert AVIF.
- Apply max long edge.
- Check output size.
- Warning nếu output nặng hơn gốc.
- Không sửa file gốc.

### PdfCombineService

Trách nhiệm:

- Decode ảnh đã nén sang JPEG temp.
- Nhúng JPEG vào PDF.
- Giữ page size theo ảnh mặc định.
- RGB mặc định.
- Tạo PDF version theo q.
- Cleanup temp.

### OutputManager

Trách nhiệm:

- Tạo `compressed-avif`, `pdf-output`.
- Đặt tên file output.
- Xử lý overwrite/tên mới.
- Đặt final.

## 5. FFmpeg command mapping

### AVIF

```text
ffmpeg -i input -frames:v 1 -c:v libaom-av1 -crf <crf> -cpu-used <cpu> -pix_fmt yuv420p output.avif
```

Lossless:

```text
ffmpeg -i input -frames:v 1 -c:v libaom-av1 -crf 0 -cpu-used 4 output.avif
```

### Resize filter

```text
scale='if(gt(iw,ih),min(iw,<max>),-2)':'if(gt(iw,ih),-2,min(ih,<max>))'
```

### PDF temp JPEG

```text
ffmpeg -i input.avif -frames:v 1 -q:v <jpegQ> -pix_fmt yuvj420p temp.jpg
```

Gray optional:

```text
ffmpeg -i input.avif -vf format=gray -frames:v 1 -q:v <jpegQ> -pix_fmt gray temp.jpg
```

## 6. PDF writer

MVP có thể dùng PDF writer nội bộ đơn giản:

- PDF 1.4.
- Mỗi trang là một image XObject.
- Image stream dùng DCTDecode JPEG.
- Page size mặc định bằng kích thước ảnh.
- A4 fit/full là option phụ.

Không cần thư viện PDF nặng nếu chỉ nhúng ảnh.

## 7. Progress và logging

Mỗi job phát event:

```csharp
public sealed class JobProgress
{
    public string JobType { get; init; }
    public string Status { get; init; }
    public string? CurrentFile { get; init; }
    public int ProcessedCount { get; init; }
    public int TotalCount { get; init; }
    public string Message { get; init; }
}
```

Log cần lưu:

- Command đã chạy.
- Exit code.
- stderr rút gọn.
- File lỗi.
- Thời gian xử lý.

Không hiển thị raw exception làm thông báo chính cho người dùng.

## 8. Error handling

Các lỗi chính:

- Không tìm thấy FFmpeg.
- File ảnh không đọc được.
- File output đã tồn tại.
- Không tạo được folder output.
- FFmpeg exit code khác 0.
- PDF writer lỗi.

UI message phải dễ hiểu và có nút xem log.

## 9. Test kỹ thuật

Test tối thiểu:

- Scan folder chỉ lấy file ảnh hợp lệ.
- Sort file đúng.
- Build command AVIF đúng CRF.
- Build command PDF temp JPEG đúng q/color mode.
- Output nặng hơn gốc tạo warning.
- Gray mode tạo warning.
- PDF page mode image không ép A4.
- Final PDF copy/rename đúng.

## 10. Rủi ro kỹ thuật

- FFmpeg bundled không tồn tại, sai path đóng gói, hoặc không hỗ trợ `libaom-av1`.
- AVIF decode chậm.
- WinUI image preview/bitmap loading cần kiểm tra memory usage với ảnh lớn; tránh giữ toàn bộ ảnh full-resolution trong UI nếu không cần.
- PDF writer nội bộ cần test kỹ với nhiều ảnh.
- Unicode path tiếng Việt phải được xử lý đúng.

Giảm rủi ro:

- Detect codec support khi app mở.
- Bundle FFmpeg cùng package release và test lại trên máy không cài FFmpeg hệ thống.
- Log command đầy đủ.
- Test với folder có dấu tiếng Việt.
- Không truyền command qua shell string; dùng `ProcessStartInfo.ArgumentList`.
