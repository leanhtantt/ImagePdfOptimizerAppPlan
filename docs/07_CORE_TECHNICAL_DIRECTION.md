# Core Technical Direction: File Utility Hub

## 1. Vai trò của tài liệu này

Tài liệu này mô tả định hướng kỹ thuật cốt lõi cho `File Utility Hub` nếu app đi theo hướng suite nhiều module và UI WinUI 3 / Windows App SDK.

Quyết định boundary mới xem `09_FEATURE_BOUNDARY_AND_AUTOMATION_DECISION.md`: không viết một workflow service duy nhất ôm cả nén ảnh, gộp file và nén PDF. Các feature nối nhau bằng job context/handoff.

Mục tiêu là giữ core đủ sạch để:

- Feature 01 `Image Optimizer` chạy tốt.
- Sau này thêm PDF Compressor, PDF Converter, Combine/Split, Word Tools, Excel Tools mà không viết lại app.
- UI framework không nuốt hết business logic.
- FFmpeg/process/file system/log/job có thể dùng lại giữa nhiều feature.

## 2. Nguyên tắc kiến trúc

Core app phải tuân thủ các nguyên tắc:

1. **Shell không biết chi tiết nghiệp vụ của từng feature.**
2. **Feature module không tự quản lý global tool/process/log theo kiểu riêng.**
3. **Infrastructure xử lý external tool và file system, không nhúng UI.**
4. **ViewModel giữ UI state, không trực tiếp build command FFmpeg.**
5. **Service xử lý nghiệp vụ, trả result rõ ràng cho UI.**
6. **File gốc không bao giờ bị sửa trong workflow chính.**
7. **Mọi job dài phải có progress, cancel và log.**
8. **Warning/error phải là object có cấu trúc, không chỉ string raw exception.**

## 3. Project/folder structure đề xuất

Nếu làm WinUI 3 nghiêm túc, cấu trúc nên là:

```text
FileUtilityHub
├── FileUtilityHub.App
│   ├── App.xaml
│   ├── MainWindow.xaml
│   ├── Shell
│   ├── Navigation
│   ├── Resources
│   └── CompositionRoot
├── FileUtilityHub.Core
│   ├── Contracts
│   ├── Models
│   ├── Jobs
│   ├── Results
│   ├── Warnings
│   └── Utilities
├── FileUtilityHub.Shared
│   ├── Controls
│   ├── ViewModels
│   ├── Formatting
│   ├── Validation
│   └── UIState
├── FileUtilityHub.Infrastructure
│   ├── Ffmpeg
│   ├── ProcessRunner
│   ├── FileSystem
│   ├── Logging
│   ├── Pdf
│   └── Tooling
├── Features
│   ├── ImageOptimizer
│   │   ├── Views
│   │   ├── ViewModels
│   │   ├── Models
│   │   ├── Services
│   │   ├── Contracts
│   │   └── Module
│   ├── FileMergePdfBuilder
│   └── PdfCompressor
└── FileUtilityHub.Tests
    ├── Core
    ├── Infrastructure
    └── Features
        └── ImageOptimizer
```

Nếu muốn MVP nhanh hơn, có thể gom thành ít project hơn, nhưng folder và dependency direction vẫn nên giữ như trên.

## 4. Dependency direction

Luồng phụ thuộc nên đi một chiều:

```text
App/UI
-> Feature Modules
-> Core Contracts/Models
-> Infrastructure Implementations
```

Quy tắc:

- `Core` không reference `App`, `WinUI`, `Infrastructure`.
- `Infrastructure` reference `Core` để implement contracts.
- `Feature` reference `Core` và dùng service contracts.
- `App` compose dependency injection, navigation, resources, shell.
- UI không gọi `Process.Start` hoặc build FFmpeg command trực tiếp.

## 5. Module contract

Mỗi feature nên expose một module contract để shell render mà không cần biết chi tiết.

Ví dụ:

```csharp
public interface IFeatureModule
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    string IconKey { get; }
    FeatureAvailability GetAvailability();
    object CreateView();
    FeatureStateSnapshot GetStateSnapshot();
    IReadOnlyList<FeatureActionDescriptor> GetHeaderActions();
}
```

Trong WinUI, `CreateView()` có thể trả `UserControl` hoặc một descriptor để App resolve view bằng DI.

Không nên để shell hardcode:

```text
if feature == ImageOptimizer then show button Convert AVIF
```

Shell chỉ hiển thị action do module expose.

## 6. Composition root và dependency injection

Nên có một nơi duy nhất để đăng ký service:

```text
FileUtilityHub.App/CompositionRoot
```

Service nhóm core/infrastructure:

- `IToolLocator`
- `IFfmpegLocator`
- `IProcessRunner`
- `IFileScanService`
- `IOutputManager`
- `ILogService`
- `IJobRunner`
- `IWarningService`

Service nhóm Feature 01:

- `IImageInputScanner`
- `IImageConvertService`
- `IImageReviewService`
- `IImageOptimizerWorkflow`
- `IFeatureHandoffService`

Service nhóm File Merge / PDF Builder:

- `IFileMergePlanner`
- `IPdfBuildService`

Service nhóm PDF Compressor:

- `IPdfCompressService`
- `IPdfVersionService`

ViewModel nên nhận service qua constructor.

## 7. Core contracts đề xuất

### 7.1. Tool locator

```csharp
public interface IToolLocator
{
    Task<ToolStatus> GetToolStatusAsync(ToolId toolId, CancellationToken cancellationToken);
}
```

`ToolStatus` nên gồm:

- Tool found/missing.
- Path.
- Version.
- Supported capabilities.
- Warning/error list.

### 7.2. Process runner

```csharp
public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(ProcessRunRequest request, IProgress<ProcessProgress>? progress, CancellationToken cancellationToken);
}
```

`ProcessRunRequest` nên gồm:

- Executable path.
- Arguments dạng list/token, không phải string ghép tay.
- Working directory.
- Environment variables nếu cần.
- Timeout nếu cần.
- Log redaction policy nếu cần.

### 7.3. Output manager

```csharp
public interface IOutputManager
{
    Task<OutputPlan> PrepareOutputAsync(OutputRequest request, CancellationToken cancellationToken);
}
```

Trách nhiệm:

- Tạo output folder theo feature, ví dụ `compressed-avif`, `merged-pdf`, `compressed-pdf`.
- Xử lý overwrite/new name/skip.
- Không sửa file gốc.
- Hỗ trợ folder có tiếng Việt.

### 7.4. Job runner

```csharp
public interface IJobRunner
{
    Task<JobResult<T>> RunAsync<T>(JobRequest<T> request, CancellationToken cancellationToken);
}
```

Trách nhiệm:

- Quản lý trạng thái job.
- Emit progress.
- Hỗ trợ cancel.
- Ghi log.
- Chuẩn hoá error/warning.

## 8. Result và warning model

Không nên trả về `bool success` hoặc string lỗi rời rạc.

Nên dùng result có cấu trúc:

```csharp
public sealed class OperationResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public IReadOnlyList<AppWarning> Warnings { get; init; }
    public AppError? Error { get; init; }
}
```

`AppWarning` nên gồm:

- Code.
- Severity.
- User-facing message tiếng Việt.
- Technical detail cho log.
- Related file/path nếu có.
- Suggested action.

Ví dụ warning codes:

- `UnsupportedFileSkipped`
- `OutputLargerThanOriginal`
- `FfmpegMissing`
- `AvifCodecUnsupported`
- `GrayModeEnabled`
- `PdfOutputExists`
- `FileWriteFailed`

## 9. Job state model

Một job dài như convert AVIF, gộp file hoặc nén PDF cần state rõ:

```text
Queued
Running
Cancelling
Succeeded
SucceededWithWarnings
Failed
Cancelled
```

Progress nên gồm:

- Total items.
- Completed items.
- Current item name.
- Percent nếu tính được.
- Current stage.
- Short message tiếng Việt.

Ví dụ stage cho Feature 01:

```text
ScanningInput
PreparingOutput
CheckingFfmpeg
ConvertingAvif
ReviewingOutput
PreparingHandoff
```

Các stage như `CombiningPdf`, `WritingPdfVersion`, `SelectingFinal` thuộc `File Merge / PDF Builder` và `PDF Compressor`, không thuộc Feature 01.

## 10. Feature workflow và handoff service

Feature 01 chỉ nên có workflow service điều phối luồng ảnh:

```text
Scan input
-> Prepare output folders
-> Check FFmpeg
-> Convert AVIF
-> Review result
-> Prepare handoff context nếu người dùng chọn Gộp file hoặc Gộp và nén
```

ViewModel gọi workflow service, không tự gọi từng infrastructure service trực tiếp quá sâu.

Gợi ý:

```csharp
public interface IImageOptimizerWorkflow
{
    Task<OperationResult<InputScanResult>> ScanInputAsync(InputScanRequest request, CancellationToken cancellationToken);
    Task<OperationResult<AvifConvertBatchResult>> ConvertAvifAsync(AvifConvertBatchRequest request, IProgress<JobProgress> progress, CancellationToken cancellationToken);
    Task<OperationResult<FileBatchContext>> CreateMergeHandoffAsync(CreateMergeHandoffRequest request, CancellationToken cancellationToken);
}

public interface IFeatureHandoffService
{
    Task<OperationResult<NavigationHandoff>> SendToMergeAsync(FileBatchContext context, CancellationToken cancellationToken);
    Task<OperationResult<NavigationHandoff>> MergeAndSendToPdfCompressorAsync(FileBatchContext context, CancellationToken cancellationToken);
}
```

## 11. FFmpeg core direction

FFmpeg là external tool quan trọng, nên xử lý như infrastructure chính thức, không phải helper tạm.

### 11.1. Locator

Thứ tự tìm trong MVP/package:

1. Bundled path trong app, ví dụ `tools/ffmpeg/bin/ffmpeg.exe`.
2. Development override chỉ dành cho developer nếu cần.
3. Không yêu cầu user cuối nhập path FFmpeg thủ công trong MVP.

### 11.2. Capability check

Cần check:

- `ffmpeg.exe` tồn tại.
- Chạy được `-version`.
- Có encoder/decode cần cho AVIF.
- Có khả năng xử lý input format cần thiết.

### 11.3. Command builder

Không ghép command bằng string dài trong ViewModel.

Nên có:

```text
FfmpegCommandBuilder
├── BuildAvifConvertCommand
├── BuildImageProbeCommand nếu cần
└── BuildPdfImagePrepareCommand nếu cần
```

Arguments nên là list để tránh lỗi quote path có dấu cách/tiếng Việt.

## 12. PDF generation direction

Có hai hướng kỹ thuật:

### Hướng A: FFmpeg-centric

Dùng FFmpeg nhiều nhất có thể để tạo/encode asset phục vụ PDF.

Ưu điểm:

- Ít dependency hơn.
- Dựa vào tool đã bundle.

Nhược điểm:

- Kiểm soát PDF page/version/final có thể hạn chế.
- Một số requirement như page size/orientation/color warning cần test kỹ.

### Hướng B: PDF library + FFmpeg image processing

Dùng FFmpeg cho AVIF/image processing, dùng PDF library để tạo PDF.

Ưu điểm:

- Kiểm soát page size/orientation tốt hơn.
- Dễ quản lý PDF version/final metadata hơn.

Nhược điểm:

- Thêm dependency.
- Cần kiểm tra license và packaging.

Khuyến nghị:

- MVP có thể bắt đầu bằng hướng đơn giản nhất chạy được.
- Nhưng abstraction nên là `IPdfBuildService` trong `File Merge / PDF Builder` để sau này đổi backend mà UI/core workflow không đổi.

## 13. File system rules

Quy tắc bắt buộc:

- Không sửa file gốc.
- Output nằm trong folder riêng.
- Tên output deterministic nhưng tránh overwrite im lặng.
- Folder tiếng Việt phải chạy.
- Path có dấu cách phải chạy.
- Unsupported file không làm chết toàn job.
- Mọi file write failure phải có user-facing error.

Output mặc định:

```text
InputFolder
├── compressed-avif
├── merged-pdf
└── compressed-pdf
```

## 14. Logging direction

Log phải đủ để debug nhưng không làm người dùng sợ.

Log nên gồm:

- Timestamp.
- Job id.
- Stage.
- File hiện tại.
- Tool path/version.
- Process command đã redacted nếu cần.
- Exit code.
- Stdout/stderr.
- Warning/error codes.

UI chỉ hiển thị message dễ hiểu. Technical detail nằm trong log center.

## 15. ViewModel state direction

Feature ViewModel nên có state dạng:

```text
ObservableCollection<ImageItemViewModel> Images
ImageOptimizerState CurrentState
JobProgressViewModel? CurrentProgress
IReadOnlyList<AppWarningViewModel> Warnings
AppErrorViewModel? Error
```

Commands:

- `SelectFolderCommand`
- `AddFilesCommand`
- `ConvertAvifCommand`
- `SendToMergeCommand`
- `MergeAndCompressCommand`
- `CancelJobCommand`
- `OpenOutputFolderCommand`
- `CopyLogCommand`

Command enabled/disabled phải bám vào `CurrentState`.

## 16. Testing direction

Nên có test ở ba lớp:

### 16.1. Core unit tests

- Size formatting.
- File extension filtering.
- Warning mapping.
- Output naming.
- Config validation.

### 16.2. Infrastructure tests

- Process argument quoting.
- FFmpeg locator với fake path.
- Output folder creation.
- File overwrite policy.

### 16.3. Feature workflow tests

- Scan folder trả đúng valid/unsupported.
- Convert result warning khi output nặng hơn gốc.
- Handoff context chứa đúng output `compressed-avif`.

PDF version/final tests thuộc `PDF Compressor`.

Golden test `Sao ke GD` là QA thực tế, không nhất thiết là automated test trong repo nếu dữ liệu không nằm trong repo.

## 17. Các quyết định nên tránh

Không nên:

- Đặt mọi thứ dưới namespace `ImageOptimizer`.
- Để MainWindow biết chi tiết convert AVIF.
- Để ViewModel tự ghép FFmpeg command string.
- Để service trả lỗi chỉ bằng raw exception.
- Hardcode output path tuyệt đối.
- Bắt user cuối cấu hình FFmpeg path trong MVP.
- Để Image Optimizer ôm cả logic gộp/nén PDF.
- Convert lại AVIF khi người dùng đang thao tác trong PDF Compressor.
- Bật Gray mặc định.
- Ép A4 mặc định.
- Im lặng bỏ qua file lỗi hoặc output nặng hơn gốc.

## 18. Roadmap kỹ thuật đề xuất

Nếu chuyển sang WinUI ngay, thứ tự nên là:

1. Tạo solution/project skeleton đúng dependency direction.
2. Dựng composition root và DI.
3. Dựng shell `NavigationView` + placeholder modules.
4. Dựng core contracts/result/warning/job models.
5. Implement file scan + output manager.
6. Implement FFmpeg locator + process runner.
7. Implement Feature 01 ViewModel state và basic views.
8. Implement AVIF convert workflow.
9. Implement review/result UI.
10. Implement handoff actions `Gộp file` và `Gộp và nén`.
11. Polish warning/log/cancel.
12. QA Image Optimizer bằng `Sao ke GD`.
13. Implement `File Merge / PDF Builder`.
14. Implement `PDF Compressor`.

## 19. Khuyến nghị chốt

Core kỹ thuật nên được thiết kế như một file utility platform nhỏ, không phải một app một màn hình.

Cách nghĩ đúng:

```text
Shell render module.
Module expose state/action/view.
Workflow service điều phối nghiệp vụ.
Infrastructure chạy tool/file/process/log.
UI chỉ binding state và gửi command.
```

Nếu giữ được ranh giới này, WinUI có thể đẹp hơn mà không làm business logic bị mắc kẹt trong XAML/code-behind.
