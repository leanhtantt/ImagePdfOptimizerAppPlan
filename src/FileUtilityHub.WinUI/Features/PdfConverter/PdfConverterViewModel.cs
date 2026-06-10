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

namespace FileUtilityHub_WinUI.Features.PdfConverter;

public partial class PdfConverterViewModel : ObservableObject
{
    private readonly PdfConvertWorkflowService _workflowService;
    private readonly AppStatusService _statusService;
    private readonly IFilePickerService _filePickerService;
    private readonly INotificationService _notificationService;
    private CancellationTokenSource? _cancellationTokenSource;

    private static readonly string[] SupportedExtensions =
        [".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".webp", ".avif",
         ".pdf",
         ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"];

    // --- Observable Properties ---

    [ObservableProperty]
    private bool _isConverting = false;

    [ObservableProperty]
    private bool _hasFiles = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBlackWhite))]
    [NotifyPropertyChangedFor(nameof(IsRgb))]
    private int _colorModeIndex = 0; // 0=Đen trắng (default), 1=RGB

    /// <summary>
    /// When true, skip AVIF intermediate step for 5-10x faster conversion.
    /// </summary>
    [ObservableProperty]
    private bool _fastMode = true;

    public bool IsBlackWhite => ColorModeIndex == 0;
    public bool IsRgb => ColorModeIndex == 1;

    // Warning state
    [ObservableProperty]
    private string _warningTitle = string.Empty;

    [ObservableProperty]
    private string _warningMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveWarning = false;

    [ObservableProperty]
    private WarningSeverityLevel _warningSeverity = WarningSeverityLevel.Warning;

    public ObservableCollection<PdfConvertItem> ConvertItems { get; } = new();

    public PdfConverterViewModel(
        PdfConvertWorkflowService workflowService,
        AppStatusService statusService,
        IFilePickerService filePickerService,
        INotificationService notificationService)
    {
        _workflowService = workflowService;
        _statusService = statusService;
        _filePickerService = filePickerService;
        _notificationService = notificationService;

        LoadSettings();
        PropertyChanged += OnPropertyChanged_SaveSettings;
    }

    private void OnPropertyChanged_SaveSettings(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ColorModeIndex) or nameof(FastMode))
        {
            SaveSettings();
        }
    }

    private void LoadSettings()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values["PdfConverter_ColorModeIndex"] is int colorMode)
                ColorModeIndex = colorMode;
            if (localSettings.Values["PdfConverter_FastMode"] is bool fast)
                FastMode = fast;
        }
        catch { /* Ignore if unpackaged */ }
    }

    private void SaveSettings()
    {
        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["PdfConverter_ColorModeIndex"] = ColorModeIndex;
            localSettings.Values["PdfConverter_FastMode"] = FastMode;
        }
        catch { /* Ignore if unpackaged */ }
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var paths = await _filePickerService.PickFilesAsync(SupportedExtensions);
        foreach (var path in paths)
        {
            AddFileIfSupported(path);
        }
        UpdateStatus();
    }

    [RelayCommand]
    private async Task ChooseFolderAsync()
    {
        var folderPath = await _filePickerService.PickFolderAsync();
        if (folderPath != null)
        {
            var files = Directory.GetFiles(folderPath);
            foreach (var file in files.OrderBy(f => f))
            {
                AddFileIfSupported(file);
            }
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void RemoveSelected(object selectedItems)
    {
        if (selectedItems is IEnumerable<object> list)
        {
            var itemsToRemove = list.Cast<PdfConvertItem>().ToList();
            foreach (var item in itemsToRemove)
            {
                ConvertItems.Remove(item);
            }
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        ConvertItems.Clear();
        HasActiveWarning = false;
        UpdateStatus();
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelConvert()
    {
        _cancellationTokenSource?.Cancel();
    }

    private bool CanCancel() => IsConverting;

    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task ConvertToPdfAsync(object? selectedItemsParam)
    {
        if (ConvertItems.Count == 0) return;

        // Determine which items to process
        List<PdfConvertItem> itemsToProcess;

        if (selectedItemsParam is IEnumerable<object> selectedList)
        {
            var selected = selectedList.Cast<PdfConvertItem>().ToList();
            itemsToProcess = selected.Count > 0 ? selected : ConvertItems.ToList();
        }
        else
        {
            itemsToProcess = ConvertItems.ToList();
        }

        IsConverting = true;
        HasActiveWarning = false;
        NotifyCommandsCanExecuteChanged();

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        var config = BuildConfig();

        _statusService.StartProcessing("Đang convert sang PDF...", itemsToProcess.Count);

        int successCount = 0;
        int warningCount = 0;
        int errorCount = 0;

        try
        {
            for (int i = 0; i < itemsToProcess.Count; i++)
            {
                if (token.IsCancellationRequested)
                {
                    _statusService.StopProcessing("Đã dừng convert.");
                    break;
                }

                var item = itemsToProcess[i];
                _statusService.ReportProgress(i, itemsToProcess.Count, $"Đang xử lý: {item.FileName}");

                var success = await _workflowService.ConvertFileAsync(item, config, token);

                if (success)
                {
                    if (item.Status == ProcessingStatus.Warning)
                        warningCount++;
                    else
                        successCount++;
                }
                else if (item.Status == ProcessingStatus.Error)
                {
                    errorCount++;
                }

                _statusService.ReportProgress(i + 1, itemsToProcess.Count, $"Xong: {item.FileName}");
            }

            if (!token.IsCancellationRequested)
            {
                var summary = $"{successCount} thành công";
                if (warningCount > 0) summary += $", {warningCount} cảnh báo";
                if (errorCount > 0) summary += $", {errorCount} lỗi";

                _statusService.StopProcessing($"Convert hoàn tất: {summary}");

                _notificationService.ShowToast(
                    "Convert PDF hoàn tất",
                    $"{summary} / {itemsToProcess.Count} file.",
                    "ms-appx:///Assets/Square44x44Logo.scale-200.png");

                if (errorCount > 0)
                {
                    HasActiveWarning = true;
                    WarningTitle = "Một số file bị lỗi";
                    WarningMessage = $"{errorCount} file không thể convert. {successCount + warningCount}/{itemsToProcess.Count} file đã hoàn tất.";
                    WarningSeverity = WarningSeverityLevel.Warning;
                }
            }
        }
        catch (Exception ex)
        {
            HasActiveWarning = true;
            WarningTitle = "Lỗi khi convert PDF";
            WarningMessage = ex.Message;
            WarningSeverity = WarningSeverityLevel.Error;
            _statusService.StopProcessing("Lỗi khi convert PDF.");
        }
        finally
        {
            IsConverting = false;
            NotifyCommandsCanExecuteChanged();
        }
    }

    private bool CanConvert() => !IsConverting && ConvertItems.Count > 0;

    [RelayCommand]
    private void HandleDrop(object parameter)
    {
        if (parameter is IReadOnlyList<IStorageItem> items)
        {
            foreach (var storageItem in items)
            {
                if (storageItem is StorageFolder folder)
                {
                    var files = Directory.GetFiles(folder.Path);
                    foreach (var file in files.OrderBy(f => f))
                    {
                        AddFileIfSupported(file);
                    }
                }
                else if (storageItem is StorageFile file)
                {
                    AddFileIfSupported(file.Path);
                }
            }
            UpdateStatus();
        }
    }

    [RelayCommand]
    private void OpenOutputFolder(PdfConvertItem? item)
    {
        string? dir = null;

        if (item != null && !string.IsNullOrEmpty(item.OutputPath))
        {
            dir = Path.GetDirectoryName(item.OutputPath);
        }
        else if (ConvertItems.Count > 0)
        {
            // Open the first item's source folder
            dir = Path.GetDirectoryName(ConvertItems.First().SourcePath);
        }

        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            System.Diagnostics.Process.Start("explorer.exe", dir);
        }
    }

    private void AddFileIfSupported(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(ext)) return;

        // Avoid duplicate paths
        if (ConvertItems.Any(i => i.SourcePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
            return;

        var fi = new FileInfo(filePath);
        ConvertItems.Add(new PdfConvertItem
        {
            FileName = fi.Name,
            SourcePath = fi.FullName,
            Format = ext.TrimStart('.'),
            OriginalSizeBytes = fi.Length
        });
    }

    private PdfConvertConfig BuildConfig()
    {
        var config = new PdfConvertConfig
        {
            ColorMode = ColorModeIndex == 0 ? PdfConvertColorMode.BlackWhite : PdfConvertColorMode.Rgb,
            SkipAvif = FastMode
        };

        if (FastMode)
        {
            // Fast mode: lower DPI + aggressive JPEG compression = small file, fast speed
            config.RenderDpi = 150;
            config.JpegQScale = 12;
        }

        return config;
    }

    private void UpdateStatus()
    {
        HasFiles = ConvertItems.Count > 0;
        _statusService.CurrentItemCount = ConvertItems.Count;
        _statusService.StatusMessage = $"Đã tải {ConvertItems.Count} file vào danh sách convert.";
        NotifyCommandsCanExecuteChanged();
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        ConvertToPdfCommand.NotifyCanExecuteChanged();
        CancelConvertCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsConvertingChanged(bool value)
    {
        NotifyCommandsCanExecuteChanged();
    }
}
