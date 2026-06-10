using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileUtilityHub_WinUI.Core.Contracts;
using FileUtilityHub_WinUI.Core.Models;
using FileUtilityHub_WinUI.Core.Services;
using Windows.Storage;

namespace FileUtilityHub_WinUI.Features.FileMerger;

public partial class FileMergerViewModel : ObservableObject
{
    private readonly FileScanService _scanService;
    private readonly PdfBuilderService _pdfBuilderService;
    private readonly AppStatusService _statusService;
    private readonly IFilePickerService _filePickerService;
    private readonly INotificationService _notificationService;
    private readonly IFeatureHandoffService _handoffService;
    private CancellationTokenSource? _cancellationTokenSource;

    private static readonly string[] SupportedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".avif", ".bmp", ".tif", ".tiff", ".pdf"];

    // --- Observable Properties ---

    [ObservableProperty]
    private string _currentFolder = string.Empty;

    [ObservableProperty]
    private bool _isMerging = false;

    [ObservableProperty]
    private bool _hasFiles = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImageOnly))]
    [NotifyPropertyChangedFor(nameof(IsScannedDocument))]
    private MergeInputProfile _currentProfile = MergeInputProfile.ImageOnly;

    public bool IsImageOnly => CurrentProfile == MergeInputProfile.ImageOnly;
    public bool IsScannedDocument => CurrentProfile == MergeInputProfile.ScannedDocuments;


    [ObservableProperty]
    private int _pageModeIndex = 0; // 0=ImageSize, 1=A4Full, 2=A4Fit

    [ObservableProperty]
    private int _colorModeIndex = 0; // 0=RGB, 1=Grayscale

    [ObservableProperty]
    private double _jpegQScale = 0; // 0=auto, 1-31=manual

    [ObservableProperty]
    private int _resolutionIndex = 0; // 0=Keep, 1=1920, 2=2048, 3=2560

    [ObservableProperty]
    private int _dpi = 200; // For PDF/Office

    [ObservableProperty]
    private int _jpegQuality = 80; // For PDF/Office

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPdfRasterizationEnabled))]
    private bool _preservePdfPages = true;

    public bool IsPdfRasterizationEnabled => !PreservePdfPages;

    [ObservableProperty]
    private bool _batchMergeMode = false;

    [ObservableProperty]
    private string _outputFileName = "combined-documents.pdf";

    // Warning state
    [ObservableProperty]
    private string _warningTitle = string.Empty;

    [ObservableProperty]
    private string _warningMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveWarning = false;

    [ObservableProperty]
    private WarningSeverityLevel _warningSeverity = WarningSeverityLevel.Warning;

    public ObservableCollection<MergeFileItem> MergeItems { get; } = new();

    public FileMergerViewModel(
        FileScanService scanService,
        PdfBuilderService pdfBuilderService,
        AppStatusService statusService,
        IFilePickerService filePickerService,
        INotificationService notificationService,
        IFeatureHandoffService handoffService)
    {
        _scanService = scanService;
        _pdfBuilderService = pdfBuilderService;
        _statusService = statusService;
        _filePickerService = filePickerService;
        _notificationService = notificationService;
        _handoffService = handoffService;

        MergeItems.CollectionChanged += (s, e) => UpdateProfile();
        
        LoadSettings();
        PropertyChanged += FileMergerViewModel_PropertyChanged;
    }

    private void FileMergerViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Dpi) or nameof(JpegQuality) or nameof(PreservePdfPages) or nameof(PageModeIndex) or nameof(ColorModeIndex) or nameof(JpegQScale) or nameof(ResolutionIndex) or nameof(BatchMergeMode))
        {
            SaveSettings();
        }
    }

    private void LoadSettings()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values["FileMerger_Dpi"] is int dpi) Dpi = dpi;
            if (localSettings.Values["FileMerger_JpegQuality"] is int quality) JpegQuality = quality;
            if (localSettings.Values["FileMerger_PreservePdfPages"] is bool preservePdfPages) PreservePdfPages = preservePdfPages;
            if (localSettings.Values["FileMerger_PageModeIndex"] is int pageMode) PageModeIndex = pageMode;
            if (localSettings.Values["FileMerger_ColorModeIndex"] is int colorMode) ColorModeIndex = colorMode;
            if (localSettings.Values["FileMerger_JpegQScale"] is double qscale) JpegQScale = qscale;
            if (localSettings.Values["FileMerger_ResolutionIndex"] is int resIndex) ResolutionIndex = resIndex;
            if (localSettings.Values["FileMerger_BatchMergeMode"] is bool batchMerge) BatchMergeMode = batchMerge;
        }
        catch { /* Ignore if unpackaged */ }
    }

    private void SaveSettings()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["FileMerger_Dpi"] = Dpi;
            localSettings.Values["FileMerger_JpegQuality"] = JpegQuality;
            localSettings.Values["FileMerger_PreservePdfPages"] = PreservePdfPages;
            localSettings.Values["FileMerger_PageModeIndex"] = PageModeIndex;
            localSettings.Values["FileMerger_ColorModeIndex"] = ColorModeIndex;
            localSettings.Values["FileMerger_JpegQScale"] = JpegQScale;
            localSettings.Values["FileMerger_ResolutionIndex"] = ResolutionIndex;
            localSettings.Values["FileMerger_BatchMergeMode"] = BatchMergeMode;
        }
        catch { /* Ignore if unpackaged */ }
    }

    /// <summary>
    /// Called by page on load to check for handoff context from Image Optimizer.
    /// </summary>
    public void CheckForHandoff()
    {
        if (_handoffService is Shell.Services.FeatureHandoffService handoff &&
            handoff.PendingMergeContext is { } context)
        {
            MergeItems.Clear();
            int order = 1;
            foreach (var filePath in context.Files)
            {
                if (File.Exists(filePath))
                {
                    var fi = new FileInfo(filePath);
                    MergeItems.Add(new MergeFileItem
                    {
                        FileName = fi.Name,
                        SourcePath = fi.FullName,
                        Format = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),
                        OriginalSizeBytes = fi.Length,
                        OrderIndex = order++
                    });
                }
            }

            if (!string.IsNullOrEmpty(context.SourceFolder))
                CurrentFolder = context.SourceFolder;

            // Clear the pending context
            handoff.PendingMergeContext = null;
            UpdateStatus();
        }
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var paths = await _filePickerService.PickFilesAsync(SupportedExtensions);
        if (paths.Count > 0 && MergeItems.Count == 0) 
            CurrentFolder = string.Empty;
            
        int nextOrder = MergeItems.Count + 1;
        foreach (var path in paths)
        {
            var fi = new FileInfo(path);
            MergeItems.Add(new MergeFileItem
            {
                FileName = fi.Name,
                SourcePath = fi.FullName,
                Format = Path.GetExtension(path).TrimStart('.').ToLowerInvariant(),
                OriginalSizeBytes = fi.Length,
                OrderIndex = nextOrder++
            });
        }
        UpdateStatus();
    }

    [RelayCommand]
    private async Task ChooseFolderAsync()
    {
        var folderPath = await _filePickerService.PickFolderAsync();
        if (folderPath != null)
        {
            CurrentFolder = folderPath;
            var result = _scanService.ScanDirectory(CurrentFolder);
            MergeItems.Clear();
            int order = 1;
            foreach (var item in result.ValidFiles)
            {
                if (SupportedExtensions.Contains("." + item.Format))
                {
                    MergeItems.Add(new MergeFileItem
                    {
                        FileName = item.FileName,
                        SourcePath = item.SourcePath,
                        Format = item.Format,
                        OriginalSizeBytes = item.OriginalSizeBytes,
                        OrderIndex = order++
                    });
                }
            }
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void RemoveSelected(object selectedItems)
    {
        if (selectedItems is System.Collections.Generic.IEnumerable<object> list)
        {
            var itemsToRemove = list.Cast<MergeFileItem>().ToList();
            foreach (var item in itemsToRemove)
            {
                MergeItems.Remove(item);
            }
            ReindexItems();
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        MergeItems.Clear();
        CurrentFolder = string.Empty;
        HasActiveWarning = false;
        UpdateStatus();
    }

    [RelayCommand]
    private void OpenOutput()
    {
        var outputDir = GetOutputDirectory();
        if (Directory.Exists(outputDir))
        {
            System.Diagnostics.Process.Start("explorer.exe", outputDir);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelMerge()
    {
        _cancellationTokenSource?.Cancel();
    }

    private bool CanCancel() => IsMerging;

    [RelayCommand(CanExecute = nameof(CanMerge))]
    private async Task MergeToPdfAsync()
    {
        if (MergeItems.Count == 0) return;

        IsMerging = true;
        HasActiveWarning = false;
        NotifyCommandsCanExecuteChanged();

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        // Build config from UI state
        var config = BuildConfig();

        // Determine output directory
        var outputDir = GetOutputDirectory();
        Directory.CreateDirectory(outputDir);

        var outputPath = GetAvailableOutputPath(outputDir, config.OutputFileName);
        config.OutputFileName = Path.GetFileName(outputPath);

        // Create temp directory
        var tempDir = Path.Combine(outputDir, $"_pdf_temp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        _statusService.StartProcessing("Đang xử lý tài liệu...", MergeItems.Count);

        try
        {
            // Step 1: Prepare each item (convert to JPEGs)
            foreach (var item in MergeItems)
            {
                item.Status = ProcessingStatus.Pending;
                item.ErrorMessage = null;
                item.PreparedJpegPaths.Clear();
                item.ImageWidth = null;
                item.ImageHeight = null;
                item.PageCount = null;
            }

            int successCount = 0;
            for (int i = 0; i < MergeItems.Count; i++)
            {
                if (token.IsCancellationRequested)
                {
                    _statusService.StopProcessing("Đã dừng gộp.");
                    break;
                }

                var item = MergeItems[i];
                item.Status = ProcessingStatus.Processing;
                _statusService.ReportProgress(i, MergeItems.Count, $"Xử lý: {item.FileName}");

                var success = await _pdfBuilderService.PrepareItemAsync(item, config, tempDir, token);
                if (success) successCount++;

                _statusService.ReportProgress(i + 1, MergeItems.Count, $"Xử lý xong: {item.FileName}");
            }

            if (token.IsCancellationRequested) return;

            // Step 2: Build PDF from prepared images
            var preparedItems = MergeItems
                .Where(i => i.Status == ProcessingStatus.Success
                    && (i.PreparedJpegPaths.Count > 0
                        || (config.PreservePdfPages && i.Format.Equals("pdf", StringComparison.OrdinalIgnoreCase))))
                .OrderBy(i => i.OrderIndex)
                .ToList();

            if (preparedItems.Count == 0)
            {
                HasActiveWarning = true;
                WarningTitle = "Không có tài liệu nào được chuẩn bị thành công";
                WarningMessage = "Kiểm tra lại file input hoặc thành phần xử lý của ứng dụng.";
                WarningSeverity = WarningSeverityLevel.Error;
                return;
            }

            _statusService.ReportProgress(MergeItems.Count, MergeItems.Count, "Đang xuất file PDF...");

            if (BatchMergeMode)
            {
                var groupedItems = preparedItems.GroupBy(i => Path.GetDirectoryName(i.SourcePath));
                int groupCount = groupedItems.Count();
                int processedGroups = 0;
                
                foreach (var group in groupedItems)
                {
                    var folderPath = group.Key;
                    var folderName = string.IsNullOrEmpty(folderPath) ? "combined-documents" : Path.GetFileName(folderPath);
                    if (string.IsNullOrEmpty(folderName)) folderName = "combined-documents";
                    
                    // Put the output PDF in the parent directory of the folder (next to the folder)
                    string parentDir = string.IsNullOrEmpty(folderPath) ? null : Path.GetDirectoryName(folderPath);
                    if (string.IsNullOrEmpty(parentDir) || !Directory.Exists(parentDir))
                        parentDir = outputDir; // fallback
                        
                    var groupOutputPath = GetAvailableOutputPath(parentDir, $"{folderName}.pdf");
                    
                    _pdfBuilderService.BuildPdf(groupOutputPath, group.ToList(), config);
                    processedGroups++;
                    
                    _statusService.ReportProgress(MergeItems.Count, MergeItems.Count, $"Đang xuất file PDF... ({processedGroups}/{groupCount})");
                }
                
                _statusService.StopProcessing("Đã gộp thành công!");

                _notificationService.ShowToast(
                    "Gộp PDF (Batch Mode) hoàn tất",
                    $"{preparedItems.Count} file → {groupCount} file PDF riêng biệt",
                    "ms-appx:///Assets/Square44x44Logo.scale-200.png");
            }
            else
            {
                _pdfBuilderService.BuildPdf(outputPath, preparedItems, config);

                _statusService.StopProcessing("Đã gộp thành công!");

                // Show results
                var pdfSize = new FileInfo(outputPath).Length;
                var pdfSizeMb = pdfSize / 1048576.0;

                _notificationService.ShowToast(
                    "Gộp PDF hoàn tất",
                    $"{preparedItems.Count} file → {config.OutputFileName} ({pdfSizeMb:F2} MB)",
                    "ms-appx:///Assets/Square44x44Logo.scale-200.png");
            }

            // Handle errors
            var errorItems = MergeItems.Where(i => i.Status == ProcessingStatus.Error).ToList();
            if (errorItems.Count > 0)
            {
                HasActiveWarning = true;
                WarningTitle = "Một số file bị lỗi";
                WarningMessage = $"{errorItems.Count} file không thể xử lý. PDF đã gộp {preparedItems.Count}/{MergeItems.Count} file.";
                WarningSeverity = WarningSeverityLevel.Warning;
            }
        }
        catch (Exception ex)
        {
            HasActiveWarning = true;
            WarningTitle = "Lỗi khi gộp PDF";
            WarningMessage = ex.Message;
            WarningSeverity = WarningSeverityLevel.Error;
            _statusService.StopProcessing("Lỗi khi gộp PDF.");
        }
        finally
        {
            // Cleanup temp directory
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch { /* Ignore cleanup errors */ }

            IsMerging = false;
            NotifyCommandsCanExecuteChanged();
        }
    }

    private bool CanMerge() => !IsMerging && MergeItems.Count > 0;

    [RelayCommand]
    private void HandleDrop(object parameter)
    {
        if (parameter is IReadOnlyList<IStorageItem> items)
        {
            if (MergeItems.Count == 0) 
                CurrentFolder = string.Empty;
                
            int nextOrder = MergeItems.Count + 1;
            foreach (var storageItem in items)
            {
                if (storageItem is StorageFolder folder)
                {
                    CurrentFolder = folder.Path;
                    var result = _scanService.ScanDirectory(folder.Path);
                    foreach (var item in result.ValidFiles)
                    {
                        if (SupportedExtensions.Contains("." + item.Format))
                        {
                            MergeItems.Add(new MergeFileItem
                            {
                                FileName = item.FileName,
                                SourcePath = item.SourcePath,
                                Format = item.Format,
                                OriginalSizeBytes = item.OriginalSizeBytes,
                                OrderIndex = nextOrder++
                            });
                        }
                    }
                }
                else if (storageItem is StorageFile file)
                {
                    var ext = Path.GetExtension(file.Path).ToLowerInvariant();
                    if (SupportedExtensions.Contains(ext))
                    {
                        var fi = new FileInfo(file.Path);
                        MergeItems.Add(new MergeFileItem
                        {
                            FileName = fi.Name,
                            SourcePath = fi.FullName,
                            Format = ext.TrimStart('.'),
                            OriginalSizeBytes = fi.Length,
                            OrderIndex = nextOrder++
                        });
                    }
                }
            }
            UpdateStatus();
        }
    }

    private PdfMergeConfig BuildConfig()
    {
        return new PdfMergeConfig
        {
            InputProfile = CurrentProfile,
            PreservePdfPages = PreservePdfPages,
            PageMode = PageModeIndex switch
            {
                1 => PdfPageMode.A4Full,
                2 => PdfPageMode.A4Fit,
                _ => PdfPageMode.ImageSize
            },
            ColorMode = ColorModeIndex == 1 ? PdfColorMode.Grayscale : PdfColorMode.Rgb,
            JpegQScale = (int)JpegQScale,
            MaxLongEdge = ResolutionIndex switch
            {
                1 => 1920,
                2 => 2048,
                3 => 2560,
                _ => 0
            },
            Dpi = Dpi,
            JpegQuality = JpegQuality,
            OutputFileName = NormalizeOutputFileName(OutputFileName)
        };
    }

    private static string NormalizeOutputFileName(string? value)
    {
        var fileName = Path.GetFileName(value?.Trim());
        if (string.IsNullOrWhiteSpace(fileName))
            return "combined-documents.pdf";

        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.pdf";
    }

    private static string GetAvailableOutputPath(string outputDir, string fileName)
    {
        var outputPath = Path.Combine(outputDir, fileName);
        if (!File.Exists(outputPath))
            return outputPath;

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        for (var suffix = 1; ; suffix++)
        {
            outputPath = Path.Combine(outputDir, $"{baseName}_{suffix}.pdf");
            if (!File.Exists(outputPath))
                return outputPath;
        }
    }

    private string GetOutputDirectory()
    {
        if (!string.IsNullOrEmpty(CurrentFolder))
        {
            // If they imported a folder, place the output NEXT TO that folder (in its parent directory)
            var parentDir = Path.GetDirectoryName(CurrentFolder);
            if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
                return parentDir;
                
            return CurrentFolder;
        }

        if (MergeItems.Count > 0)
        {
            var firstDir = Path.GetDirectoryName(MergeItems.First().SourcePath);
            if (!string.IsNullOrEmpty(firstDir))
                return firstDir;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    private void ReindexItems()
    {
        for (int i = 0; i < MergeItems.Count; i++)
        {
            MergeItems[i].OrderIndex = i + 1;
        }
    }

    private void UpdateProfile()
    {
        bool hasOfficeOrPdf = MergeItems.Any(i => i.Format is "pdf");

        CurrentProfile = hasOfficeOrPdf ? MergeInputProfile.ScannedDocuments : MergeInputProfile.ImageOnly;
    }

    private void UpdateStatus()
    {
        HasFiles = MergeItems.Count > 0;
        ReindexItems();
        UpdateProfile();
        _statusService.CurrentItemCount = MergeItems.Count;
        _statusService.StatusMessage = $"Đã tải {MergeItems.Count} tài liệu vào danh sách.";
        NotifyCommandsCanExecuteChanged();
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        MergeToPdfCommand.NotifyCanExecuteChanged();
        CancelMergeCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsMergingChanged(bool value)
    {
        NotifyCommandsCanExecuteChanged();
    }
}
